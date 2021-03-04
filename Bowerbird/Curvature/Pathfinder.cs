using Rhino.Geometry;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bowerbird.Curvature
{
    public class Pathfinder
    {
        public List<Point3d> Parameters { get; private set; }

        public List<Point3d> Points { get; private set; }

        public Pathfinder(List<Point3d> parameters, List<Point3d> points)
        {
            Parameters = parameters;
            Points = points;
        }

        public static Pathfinder Create(Path path, BrepFace face, Vector2d uv, bool type, double stepSize, double tolerance, int maxPoints)
        {
            var parameters = new List<Point3d>();
            var points = new List<Point3d>();

            parameters.Add(new Point3d(uv.X, uv.Y, 0));
            points.Add(face.PointAt(uv.X, uv.Y));

            var direction = path.InitialDirection(face, uv, type);

            if (direction.IsZero)
                return new Pathfinder(parameters, points);

            IBoundary boundary;

            if (face.IsSurface)
            {
                var surface = face.UnderlyingSurface();
                boundary = UntrimmedBoundary.Create(surface);
            }
            else
                boundary = TrimmedBoundary.Create(face);

            FindPath(parameters, points, path, face, uv, direction, boundary, stepSize, tolerance, maxPoints);

            parameters.Reverse();
            points.Reverse();

            FindPath(parameters, points, path, face, uv, -direction, boundary, stepSize, tolerance, maxPoints);

            return new Pathfinder(parameters, points);
        }

        private static void FindPath(List<Point3d> parameters, List<Point3d> points, Path path, Surface surface, Vector2d uv, Vector3d direction, IBoundary boundary, double stepSize, double tolerance, int maxPoints)
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

                direction = x - points[points.Count - 1];

                // Break if no progress in geometry space

                if (direction.SquareLength < tolerance2)
                    break;

                // Add point

                parameters.Add(new Point3d(uv.X, uv.Y, 0));
                points.Add(x);

                // Break if boundary reached

                if (isBoundary)
                    break;
            }
        }
    }
}
