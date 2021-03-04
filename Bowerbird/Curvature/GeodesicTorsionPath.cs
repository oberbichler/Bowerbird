using Rhino.Geometry;
using System.Diagnostics;
using static System.Math;

namespace Bowerbird.Curvature
{
    public class GeodesicTorsionPath : Path
    {
        public double Value { get; private set; }

        public double Angle { get; private set; }

        public Types Type { get; private set; }

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

            var t = 2 * Value / (crv.K2 - crv.K1);

            if (Abs(t) > 1)
                return Vector3d.Zero;

            var alpha = 0.5 * Asin(t);

            double du, dv;

            if (type)
            {
                var cosAlpha = Cos(alpha + Angle);
                var sinAlpha = Sin(alpha + Angle);

                du = crv.A2 * crv.D2 * cosAlpha - crv.A2 * crv.D1 * sinAlpha;
                dv = crv.A1 * crv.D1 * sinAlpha - crv.A1 * crv.D2 * cosAlpha;
            }
            else
            {
                var cosAlpha = Cos(alpha - Angle);
                var sinAlpha = Sin(alpha - Angle);

                du = crv.A2 * crv.D2 * cosAlpha + crv.A2 * crv.D1 * sinAlpha;
                dv = -crv.A1 * crv.D1 * sinAlpha - crv.A1 * crv.D2 * cosAlpha;
            }

            var dir = du * crv.A1 + dv * crv.A2;
            dir /= dir.Length;

            return dir;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection, double stepSize)
        {
            var u = uv.X;
            var v = uv.Y;

            var crv = new PrincipalCurvature();

            if (!crv.Compute(surface, u, v) || crv.K1 * crv.K2 > 0)
                return Vector2d.Zero;

            var t = 2 * Value / (crv.K2 - crv.K1);

            if (Abs(t) > 1)
                return Vector2d.Zero;

            var alpha = 0.5 * Asin(t);

            Vector3d dir1, dir2;

            {
                var cosAlpha = Cos(alpha + Angle);
                var sinAlpha = Sin(alpha + Angle);

                var du = crv.A2 * crv.D2 * cosAlpha - crv.A2 * crv.D1 * sinAlpha;
                var dv = crv.A1 * crv.D1 * sinAlpha - crv.A1 * crv.D2 * cosAlpha;

                dir1 = du * crv.A1 + dv * crv.A2;

                Debug.Assert(dir1.Length > 0);

                dir1 /= dir1.Length;

                Debug.Assert(dir1.IsValid);
            }

            {
                var cosAlpha = Cos(alpha - Angle);
                var sinAlpha = Sin(alpha - Angle);

                var du = crv.A2 * crv.D2 * cosAlpha + crv.A2 * crv.D1 * sinAlpha;
                var dv = -crv.A1 * crv.D1 * sinAlpha - crv.A1 * crv.D2 * cosAlpha;

                dir2 = du * crv.A1 + dv * crv.A2;

                Debug.Assert(dir2.Length > 0);

                dir2 /= dir2.Length;

                Debug.Assert(dir2.IsValid);
            }

            var a = Choose(dir1, dir2, lastDirection, stepSize);

            Debug.Assert(a.IsValid);
            Debug.Assert(a.Length > 0);

            var d = ToUV(crv.A1, crv.A2, a);

            Debug.Assert(d.IsValid);

            return d;
        }
    }

}
