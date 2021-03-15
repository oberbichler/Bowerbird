using Rhino.Geometry;
using System.Diagnostics;

namespace Bowerbird.Curvature
{
    public class PrincipalCurvaturePath : Path
    {
        public double Angle { get; private set; }

        private PrincipalCurvaturePath(double angle, Types type)
        {
            Angle = angle;
            Type = type;
        }

        public static PrincipalCurvaturePath Create(double angle, Types type)
        {
            return new PrincipalCurvaturePath(angle, type);
        }

        public override Vector3d InitialDirection(Surface surface, Vector2d uv, bool type)
        {
            var u = uv.X;
            var v = uv.Y;

            var crv = new PrincipalCurvature();

            if (!crv.Compute(surface, u, v))
                return Vector3d.Zero;

            return type ? crv.D1 : crv.D2;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection, double stepSize)
        {
            var u = uv.X;
            var v = uv.Y;

            var crv = new PrincipalCurvature();

            if (!crv.Compute(surface, u, v))
                return Vector2d.Zero;

            var a = Choose(crv.D1, crv.D2, lastDirection, stepSize);

            Debug.Assert(a.IsValid);
            Debug.Assert(a.Length > 0);

            var d = ToUV(crv.A1, crv.A2, a);

            Debug.Assert(d.IsValid);

            return d;
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

            u1 = crv.U1;
            u2 = crv.U2;
            d1 = crv.D1;
            d2 = crv.D2;

            return true;
        }
    }
}
