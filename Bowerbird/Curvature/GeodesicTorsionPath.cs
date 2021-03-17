using Rhino.Geometry;
using System.Diagnostics;

namespace Bowerbird.Curvature
{
    public class GeodesicTorsionPath : Path
    {
        public double Value { get; private set; }

        public double Angle { get; private set; }

        private GeodesicTorsionPath(double value, double angle, Types type)
        {
            Value = value;
            Angle = angle;
            Type = type;
        }

        public static GeodesicTorsionPath Create(double value, double angle, Types type)
        {
            return new GeodesicTorsionPath(value, angle, type);
        }

        public override Vector3d InitialDirection(Surface surface, Vector2d uv, bool type)
        {
            var u = uv.X;
            var v = uv.Y;

            var crv = new PrincipalCurvature();

            if (!crv.Compute(surface, u, v))
                return Vector3d.Zero;

            if (!crv.FindGeodesicTorsion(Value, Angle, out var _, out var _, out var dir1, out var dir2))
                return Vector3d.Zero;

            return type ? dir1 : dir2;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection, double stepSize)
        {
            var u = uv.X;
            var v = uv.Y;

            var crv = new PrincipalCurvature();

            if (!crv.Compute(surface, u, v))
                return Vector2d.Zero;

            if (!crv.FindGeodesicTorsion(Value, Angle, out var u1, out var u2, out var dir1, out var dir2))
                return Vector2d.Zero;

            var d = Choose(u1, u2, dir1, dir2, lastDirection, stepSize);

            Debug.Assert(d.IsValid);
            Debug.Assert(d.Length > 0);

            return new Vector2d(d.X, d.Y);
        }


        public override bool Directions(Surface surface, Vector2d uv, out Vector3d u1, out Vector3d u2, out Vector3d d1, out Vector3d d2)
        {
            var u = uv.X;
            var v = uv.Y;

            var crv = new PrincipalCurvature();

            if (!crv.Compute(surface, u, v))
            {
                u1 = default;
                u2 = default;
                d1 = default;
                d2 = default;
                return false;
            }

            if (!crv.FindGeodesicTorsion(Value, Angle, out u1, out u2, out d1, out d2))
                return false;

            return true;
        }
    }
}
