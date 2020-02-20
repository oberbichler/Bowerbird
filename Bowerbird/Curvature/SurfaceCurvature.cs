using Rhino.Geometry;
using System;

namespace Bowerbird.Curvature
{
    struct SurfaceCurvature
    {
        private SurfaceCurvature(
            Point3d x,
            Vector3d a1,
            Vector3d a2,
            Vector3d n,
            double k1,
            double k2,
            Vector2d k1ParameterDirection,
            Vector2d k2ParameterDirection,
            Vector3d k1Direction,
            Vector3d k2Direction)
        {
            X = x;
            A1 = a1;
            A2 = a2;
            N = n;
            K1 = k1;
            K2 = k2;
            K1ParameterDirection = k1ParameterDirection;
            K2ParameterDirection = k2ParameterDirection;
            K1Direction = k1Direction;
            K2Direction = k2Direction;
        }

        public static SurfaceCurvature Create(Surface surface, double u, double v)
        {
            var curvature = surface.CurvatureAt(u, v);
            surface.Evaluate(u, v, 2, out var x, out var derivatives);

            var a1 = derivatives[0];
            var a2 = derivatives[1];

            var n = Vector3d.CrossProduct(a1, a2);
            n.Unitize();

            var k1 = curvature?.Kappa(0) ?? double.NaN;
            var k2 = curvature?.Kappa(1) ?? double.NaN;

            var k1Direction = Vector3d.Unset;
            var k2Direction = Vector3d.Unset;

            var k1ParameterDirection = Vector2d.Unset;
            var k2ParameterDirection = Vector2d.Unset;

            if (!double.IsNaN(k1))
            {
                k1Direction = curvature.Direction(0);
                k1Direction.Unitize();
                k1ParameterDirection = ToUV(a1, a2, k1Direction);
                k1ParameterDirection.Unitize();
            }

            if (!double.IsNaN(k2))
            {
                k2Direction = curvature.Direction(1);
                k2Direction.Unitize();
                k2ParameterDirection = ToUV(a1, a2, k2Direction);
                k2ParameterDirection.Unitize();
            }

            return new SurfaceCurvature(x, a1, a2, n, k1, k2, k1ParameterDirection, k2ParameterDirection, k1Direction, k2Direction);
        }

        private static Vector2d ToUV(Vector3d a1, Vector3d a2, Vector3d d)
        {
            var det = a1.X * a2.Y - a2.X * a1.Y;
            var u = (d.X * a2.Y - d.Y * a2.X) / det;
            var v = (d.Y * a1.X - d.X * a1.Y) / det;
            return new Vector2d(u, v);
        }

        public Point3d X { get; private set; }

        public Vector3d A1 { get; private set; }

        public Vector3d A2 { get; private set; }

        public Vector3d N { get; private set; }

        public double K1 { get; private set; }

        public double K2 { get; private set; }

        public Vector2d K1ParameterDirection { get; private set; }

        public Vector2d K2ParameterDirection { get; private set; }

        public Vector3d K1Direction { get; private set; }

        public Vector3d K2Direction { get; private set; }

        public Vector3d MinDirection => K1 < K2 ? K1Direction : K2Direction;

        public Vector3d MaxDirection => K1 < K2 ? K2Direction : K1Direction;

        private bool Within(double bound1, double bound2, double value)
        {
            if (bound1 <= value && value <= bound2)
                return true;
            if (bound2 <= value && value <= bound1)
                return true;
            return false;
        }

        public bool FindNormalCurvature(double k, out double angle1, out double angle2)
        {
            if (!Within(K1, K2, k))
            {
                angle1 = default;
                angle2 = default;
                return false;
            }

            angle2 = 0.5 * Math.Acos((2 * k - K1 - K2) / (K1 - K2));
            angle1 = -angle2;

            return true;
        }

        public void ComputeDirections(double angle1, double angle2, out Vector3d direction1, out Vector3d direction2)
        {
            direction1 = K1Direction;
            direction2 = K1Direction;

            direction1.Rotate(angle1, N);
            direction2.Rotate(angle2, N);
        }

        public bool FindGeodesicTorsion(double k, out double angle1, out double angle2)
        {
            var d = 2 * k / (K2 - K1);

            if (Math.Abs(d) > 1)
            {
                angle1 = default;
                angle2 = default;
                return false;
            }

            angle2 = 0.5 * Math.Asin(-d);
            angle1 = -angle2;

            return true;
        }
    }
}
