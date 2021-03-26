using Rhino.Geometry;
using System;
using System.Diagnostics;

namespace Bowerbird.Curvature
{
    class UntrimmedBoundary : IBoundary
    {
        readonly BrepFace _face;

        readonly double _xMin;
        readonly double _xMax;
        readonly double _yMin;
        readonly double _yMax;

        readonly Vector3d _x0;
        readonly Vector3d _x1;
        readonly Vector3d _x2;
        readonly Vector3d _x3;

        static readonly int[] mask = new int[] { 0, 1, 2, 2, 4, 0, 4, 4, 8, 1, 0, 2, 8, 1, 8, 0 };

        public BrepFace AdjacentFace { get; private set; }

        public Vector2d AdjacentUV { get; private set; }

        public Vector3d AdjacentTangent { get; private set; }

        private UntrimmedBoundary(BrepFace face, Interval intervalX, Interval intervalY)
        {
            _face = face;

            intervalX.MakeIncreasing();

            _xMin = intervalX.T0;
            _xMax = intervalX.T1;

            intervalY.MakeIncreasing();

            _yMin = intervalY.T0;
            _yMax = intervalY.T1;

            _x0 = new Vector3d(_xMin, _yMin, 1);
            _x1 = new Vector3d(_xMax, _yMin, 1);
            _x2 = new Vector3d(_xMax, _yMax, 1);
            _x3 = new Vector3d(_xMin, _yMax, 1);
        }

        private void SetAdjacentFace(Vector2d uv)
        {
            AdjacentFace = null;

            var location = _face.PointAt(uv.X, uv.Y);
            var uvLocation = new Point3d(uv.X, uv.Y, 0);
            var boundingTrim = default(BrepTrim);

            {
                var trims = _face.OuterLoop.Trims;

                foreach (var trim in trims)
                {
                    var trimU = trim.PointAtStart.X;
                    var trimV = trim.PointAtStart.Y;

                    // Check if trim is u/v boundary
                    var isoU = trimU == trim.PointAtEnd.X;

                    // Check if trim is active
                    if (isoU ? Math.Abs(trimU - uv.X) > 1e-6 : Math.Abs(trimV - uv.Y) > 1e-6)
                        continue;

                    // Prefer trim with more adjacent trims (for corners)
                    if (boundingTrim != null && trim.Edge.TrimCount < boundingTrim.Edge.TrimCount)
                        continue;

                    boundingTrim = trim;
                }

                if (!boundingTrim.ClosestPoint(uvLocation, out var t))
                    throw new Exception("Projection failed");

                AdjacentTangent = boundingTrim.TangentAt(t);

                Debug.Assert(boundingTrim.PointAt(t).DistanceTo(uvLocation) < 1e-6);
            }

            var adjacentTrims = boundingTrim.Edge.TrimIndices();

            if (adjacentTrims.Length > 1)
            {
                var adjacentTrimIndex = adjacentTrims[0] == boundingTrim.TrimIndex ? adjacentTrims[1] : adjacentTrims[0];
                var adjacentTrim = _face.Brep.Trims[adjacentTrimIndex];

                AdjacentFace = adjacentTrim.Face;

                // Check for loop on same face
                if (AdjacentFace.FaceIndex == boundingTrim.Face.FaceIndex)
                {
                    var mid = boundingTrim.PointAt(boundingTrim.Domain.ParameterAt(0.5));

                    var u = mid.X == _xMin ? _xMax : mid.X == _xMax ? _xMin : uv.X;
                    var v = mid.Y == _yMin ? _yMax : mid.Y == _yMax ? _yMin : uv.Y;

                    AdjacentUV = new Vector2d(u, v);
                }
                else
                {
                    if (!AdjacentFace.ClosestPoint(location, out var adjacentU, out var adjacentV))
                        throw new Exception("Projection failed");

                    var uvLocationAdjacent = new Point3d(adjacentU, adjacentV, 0);

                    if (!adjacentTrim.ClosestPoint(uvLocationAdjacent, out var t))
                        throw new Exception("Projection failed");
                    
                    AdjacentUV = new Vector2d(adjacentU, adjacentV);

                    Debug.Assert(adjacentTrim.PointAt(t).DistanceTo(new Point3d(adjacentU, adjacentV, 0)) < 1e-3);
                }
            }

            Debug.Assert(AdjacentFace == null || AdjacentFace.PointAt(AdjacentUV.X, AdjacentUV.Y).DistanceTo(location) < 1e-3);
        }

        public static UntrimmedBoundary Create(BrepFace face)
        {
            face.IsSurface.AssertTrue();

            var surface = face.UnderlyingSurface();

            var intervalX = surface.Domain(0);
            var intervalY = surface.Domain(1);

            return new UntrimmedBoundary(face, intervalX, intervalY);
        }

        public bool Clip(Vector2d a, ref Vector2d b)
        {
            var cB = default(int);

            if (b.X < _xMin)
                cB = 8;
            else if (b.X > _xMax)
                cB = 2;

            if (b.Y < _yMin)
                cB |= 1;
            else if (b.Y > _yMax)
                cB |= 4;

            if (cB == 0)
                return false;

            var p = new Vector3d(a.Y - b.Y, b.X - a.X, a.X * b.Y - a.Y * b.X);

            var c = 0;

            if (p * _x0 <= 0) c |= (1 << 0);
            if (p * _x1 <= 0) c |= (1 << 1);
            if (p * _x2 <= 0) c |= (1 << 2);
            if (p * _x3 <= 0) c |= (1 << 3);

            if (c == 0 || c == 15)
                return false;

            if ((cB & mask[c]) == 0)
            {
                switch (c)
                {
                    case 2:
                    case 6:
                    case 14:
                        b.X = -(p.Z + p.Y * _yMin) / p.X;
                        b.Y = _yMin;
                        break;
                    case 4:
                    case 12:
                    case 13:
                        b.X = _xMax;
                        b.Y = -(p.Z + p.X * _xMax) / p.Y;
                        break;
                    case 8:
                    case 9:
                    case 11:
                        b.X = -(p.Z + p.Y * _yMax) / p.X;
                        b.Y = _yMax;
                        break;
                    case 1:
                    case 3:
                    case 7:
                        b.X = _xMin;
                        b.Y = -(p.Z + p.X * _xMin) / p.Y;
                        break;
                }
            }
            else
            {
                switch (c)
                {
                    case 1:
                    case 9:
                    case 13:
                        b.X = -(p.Z + p.Y * _yMin) / p.X;
                        b.Y = _yMin;
                        break;
                    case 2:
                    case 3:
                    case 11:
                        b.X = _xMax;
                        b.Y = -(p.Z + p.X * _xMax) / p.Y;
                        break;
                    case 4:
                    case 6:
                    case 7:
                        b.X = -(p.Z + p.Y * _yMax) / p.X;
                        b.Y = _yMax;
                        break;
                    case 8:
                    case 12:
                    case 14:
                        b.X = _xMin;
                        b.Y = -(p.Z + p.X * _xMin) / p.Y;
                        break;
                }
            }

            SetAdjacentFace(b);
            return true;
        }
    }
}
