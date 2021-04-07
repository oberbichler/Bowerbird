using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Diagnostics;

using static System.Math;

namespace Bowerbird.Curvature
{
    class TrimmedBoundary : IBoundary
    {
        readonly BrepFace _face;

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

            var boundingTrim = default(BrepTrim);
            var boundingTrimParameter = default(double);

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

                        var pointAtTrim = trim.TrimCurve.PointAt(t);

                        var distance = Sqrt(Pow(a.X - pointAtTrim.X, 2) + Pow(a.Y - pointAtTrim.Y, 2));

                        if (distance > 1e-4)
                            continue;

                        if (boundingTrim != null && boundingTrim.Edge.TrimCount >= trim.Edge.TrimCount)
                            continue;

                        boundingTrim = trim;
                        boundingTrimParameter = t;
                    }
                }
            }
            else
            {
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

                        if (boundingTrim != null && boundingTrim.Edge.TrimCount >= trim.Edge.TrimCount)
                            continue;

                        var intersection = intersections[0];

                        b.X = intersection.PointA.X;
                        b.Y = intersection.PointA.Y;

                        boundingTrim = trim;
                        boundingTrimParameter = intersection.ParameterA;

                        break;
                    }
                }
            }

            if (boundingTrim == null)
                throw new Exception("Trimming failed");

            Debug.Assert(boundingTrim.PointAt(boundingTrimParameter).DistanceTo(new Point3d(b.X, b.Y, 0)) < 1e-4);

            AdjacentTangent = boundingTrim.TangentAt(boundingTrimParameter);

            if (boundingTrim.Edge.TrimCount < 2)
            {
                AdjacentFace = null;
                return true;
            }

            var trims = boundingTrim.Edge.TrimIndices();

            var adjacentTrimIndex = trims[0] == boundingTrim.TrimIndex ? trims[1] : trims[0];
            var adjacentTrim = boundingTrim.Brep.Trims[adjacentTrimIndex];

            AdjacentFace = adjacentTrim.Face;

            var curveOnSurface = CurveOnSurface.Create(AdjacentFace, adjacentTrim);

            var location = _face.PointAt(b.X, b.Y);

            var closestPoint = curveOnSurface.Invert(location, 1e-4);

            Debug.Assert(closestPoint.Item2.DistanceTo(location) < 1e-3);

            var adjacentUV = adjacentTrim.PointAt(closestPoint.Item1);

            Debug.Assert(AdjacentFace.PointAt(adjacentUV.X, adjacentUV.Y).DistanceTo(location) < 1e-3);

            AdjacentUV = new Vector2d(adjacentUV.X, adjacentUV.Y);

            return true;
        }
    }
}
