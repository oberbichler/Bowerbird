using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Bowerbird.Curvature
{
    class TrimmedBoundary : IBoundary
    {
        readonly BrepFace _face;

        public BrepEdge BoundingEdge { get; private set; }

        public BrepFace AdjacentFace { get; private set; }

        public Vector3d AdjacentTangent { get; private set; }

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
            if (_face.IsPointOnFace(b.X, b.Y) == PointFaceRelation.Interior)
                return false;

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

                    return true;
                }
            }

            return false;
        }
    }
}
