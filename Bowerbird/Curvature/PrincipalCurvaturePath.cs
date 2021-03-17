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

            var d = Choose(crv.U1, crv.U2, crv.D1, crv.D2, lastDirection, stepSize);

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

            u1 = crv.U1;
            u2 = crv.U2;
            d1 = crv.D1;
            d2 = crv.D2;

            return true;
        }
    }
}
