using Rhino.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bowerbird.Curvature
{
    public class Pathfinder
    {
        public BrepFace Face { get; private set; }

        public List<Point3d> Parameters { get; private set; }

        public List<Point3d> Points { get; private set; }

        public Pathfinder(BrepFace face, List<Point3d> parameters, List<Point3d> points)
        {
            Face = face;
            Parameters = parameters;
            Points = points;
        }

        struct Task
        {
            public Task(BrepFace face, Vector2d parameter, Point3d location, Vector3d direction)
            {
                Face = face;
                Parameter = parameter;
                Location = location;
                Direction = direction;
            }

            public BrepFace Face;

            public Vector2d Parameter;

            public Point3d Location;

            public Vector3d Direction;
        }

        private static void AddTask(Queue<Task> tasks, BrepFace adjacentFace, Vector2d adjacentUV, Point3d location, Vector3d direction)
        {
            if (adjacentFace == null)
                return;

            tasks.Enqueue(new Task(adjacentFace, adjacentUV, location, direction));
        }

        public static List<Pathfinder> Create(Path path, BrepFace face, Vector2d uv, bool type, double stepSize, double tolerance, int maxPoints, double loopTolerance)
        {
            if (path == null)
                throw new System.ArgumentException("No path type specified", nameof(path));
            if (face == null)
                throw new System.ArgumentException("No face specified", nameof(face));
            if (stepSize <= 0)
                throw new System.ArgumentException("Step size must be > 0", nameof(stepSize));
            if (tolerance <= 0)
                throw new System.ArgumentException("Tolerance must be > 0", nameof(tolerance));
            if (loopTolerance <= 0)
                throw new System.ArgumentException("Loop tolerance must be > 0", nameof(loopTolerance));
            if (maxPoints <= 0)
                throw new System.ArgumentException("Maximum points must be > 0", nameof(loopTolerance));

            var tasks = new Queue<Task>();
            var results = new List<Pathfinder>();

            var breakpoints = new List<Point3d>();

            // Initial face
            {
                var parameters = new List<Point3d>();
                var points = new List<Point3d>();

                parameters.Add(new Point3d(uv.X, uv.Y, 0));
                points.Add(face.PointAt(uv.X, uv.Y));

                var direction = path.InitialDirection(face, uv, type);

                if (direction.IsZero)
                    return new List<Pathfinder>();

                IBoundary boundary;

                if (face.IsSurface)
                    boundary = UntrimmedBoundary.Create(face);
                else
                    boundary = TrimmedBoundary.Create(face);

                // First branch
                {
                    var endDirection = FindPath(parameters, points, path, face, uv, direction, boundary, stepSize, tolerance, maxPoints);

                    var endLocation = points[points.Count - 1];
                    var adjacentFace = boundary.AdjacentFace;

                    AddTask(tasks, adjacentFace, boundary.AdjacentUV, endLocation, endDirection);

                    if (points.Count == 1)
                        AddTask(tasks, adjacentFace, boundary.AdjacentUV, endLocation, -endDirection);

                    breakpoints.Add(endLocation);
                }

                parameters.Reverse();
                points.Reverse();

                // Second branch
                {
                    var endDirection = FindPath(parameters, points, path, face, uv, -direction, boundary, stepSize, tolerance, maxPoints);

                    var endLocation = points[points.Count - 1];
                    var adjacentFace = boundary.AdjacentFace;

                    AddTask(tasks, adjacentFace, boundary.AdjacentUV, endLocation, endDirection);

                    if (points.Count == 1)
                        AddTask(tasks, adjacentFace, boundary.AdjacentUV, endLocation, -endDirection);

                    breakpoints.Add(endLocation);
                }

                results.Add(new Pathfinder(face, parameters, points));

                maxPoints -= points.Count;
            }

            // Adjacent faces

            while (tasks.Count != 0)
            {
                var task = tasks.Dequeue();

                var parameters = new List<Point3d>();
                var points = new List<Point3d>();

                parameters.Add(new Point3d(task.Parameter.X, task.Parameter.Y, 0));
                points.Add(task.Location);

                IBoundary boundary;

                if (task.Face.IsSurface)
                    boundary = UntrimmedBoundary.Create(task.Face);
                else
                    boundary = TrimmedBoundary.Create(task.Face);

                var endDirection = FindPath(parameters, points, path, task.Face, task.Parameter, task.Direction, boundary, stepSize, tolerance, maxPoints);

                // Skip empty paths
                if (points.Count < 2)
                    continue;

                results.Add(new Pathfinder(task.Face, parameters, points));

                var endLocation = points[points.Count - 1];
                var adjacentFace = boundary.AdjacentFace;

                if (breakpoints.Any(o => o.DistanceTo(endLocation) < loopTolerance))
                    break;

                AddTask(tasks, adjacentFace, boundary.AdjacentUV, endLocation, endDirection);
                breakpoints.Add(endLocation);

                maxPoints -= points.Count;
            }

            return results;
        }

        private static Vector3d FindPath(List<Point3d> parameters, List<Point3d> points, Path path, Surface surface, Vector2d uv, Vector3d direction, IBoundary boundary, double stepSize, double tolerance, int maxPoints)
        {
            var tolerance2 = tolerance * tolerance;

            while (points.Count < maxPoints)
            {
                // Fourth-order Runge-Kutta

                var d0 = path.Direction(surface, uv, direction, stepSize);

                var uv1 = uv + d0 * 0.5;
                var d1 = path.Direction(surface, uv1, direction, stepSize);

                var uv2 = uv + d1 * 0.5;
                var d2 = path.Direction(surface, uv2, direction, stepSize);

                var uv3 = uv + d2;
                var d3 = path.Direction(surface, uv3, direction, stepSize);

                var delta = (d0 + 2 * d1 + 2 * d2 + d3) / 6;

                Debug.Assert(delta.IsValid);

                // Break if no valid direction

                if (delta.X == 0 && delta.Y == 0)
                    break;

                // Compute next point

                var uvNext = uv + delta;

                var isBoundary = boundary.Clip(uv, ref uvNext);

                Debug.Assert(uvNext.IsValid);

                uv = uvNext;

                var x = surface.PointAt(uv.X, uv.Y);

                // Update direction

                var newDirection = x - points[points.Count - 1];

                // Break if no progress in geometry space

                if (newDirection.SquareLength < tolerance2)
                    break;

                direction = newDirection;

                // Add point

                parameters.Add(new Point3d(uv.X, uv.Y, 0));
                points.Add(x);

                // Break if boundary reached

                if (isBoundary)
                    break;
            }

            return direction;
        }
    }
}
