using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;

namespace Bowerbird.Curvature
{
    class TrimmedBoundary : IBoundary
    {
        readonly BrepFace _face;

        public BrepEdge BoundingEdge { get; private set; }

        public BrepFace AdjacentFace { get; private set; }

        public Vector3d AdjacentTangent { get; private set; }

        public Vector2d AdjacentUV { get; private set; }

        private TrimmedBoundary(BrepFace face)
        {
            _face = face;
        }

        public static TrimmedBoundary Create(BrepFace face)
        {
            return new TrimmedBoundary(face);
        }

        public bool Clip(Vector2d a, ref Vector2d b)
        {
            // No clip if B is within trimmed region
            if (_face.IsPointOnFace(b.X, b.Y) == PointFaceRelation.Interior)
                return false;

            // Check if A is already on boundary
            if (_face.IsPointOnFace(a.X, a.Y) == PointFaceRelation.Boundary)
            {
                b = a;

                foreach (var loop in _face.Loops)
                {
                    foreach (var trim in loop.Trims)
                    {
                        if (!trim.TrimCurve.ClosestPoint(new Point3d(a.X, a.Y, 0), out var t))
                            continue;

                        var intersection = trim.TrimCurve.PointAt(t);

                        b.X = intersection.X;
                        b.Y = intersection.Y;

                        BoundingEdge = trim.Edge;

                        var adjacentFaces = trim.Edge.AdjacentFaces();

                        AdjacentFace = adjacentFaces.Length == 1 ? null : _face.Brep.Faces[adjacentFaces[0] == _face.FaceIndex ? adjacentFaces[1] : adjacentFaces[0]];

                        AdjacentTangent = trim.TangentAt(t);

                        return true;
                    }
                }

                throw new Exception("Trimming failed");
            }

            // Create 2D-Segment
            var line = new LineCurve(new Line(a.X, a.Y, 0, b.X, b.Y, 0));
            line.ChangeDimension(2);

            // Check for intersection with trimming segments
            foreach (var loop in _face.Loops)
            {
                foreach (var trim in loop.Trims)
                {
                    var intersections = Intersection.CurveCurve(trim, line, 1e-4, 1e-4);

                    if (intersections.Count == 0)
                        continue;

                    var intersection = intersections[0];

                    b.X = intersection.PointA.X;
                    b.Y = intersection.PointA.Y;

                    BoundingEdge = trim.Edge;

                    var adjacentFaces = trim.Edge.AdjacentFaces();

                    AdjacentFace = adjacentFaces.Length == 1 ? null : _face.Brep.Faces[adjacentFaces[0] == _face.FaceIndex ? adjacentFaces[1] : adjacentFaces[0]];

                    AdjacentTangent = trim.TangentAt(intersection.ParameterA);

                    AdjacentUV = new Vector2d(b.X, b.Y);

                    return true;
                }
            }

            throw new Exception("Trimming failed");
        }
    }
}
