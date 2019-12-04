using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
            surface.Evaluate(u, v, 2, out var x, out var derivatives);

            var a1 = derivatives[0];
            var a2 = derivatives[1];
            var a1_1 = derivatives[2];
            var a1_2 = derivatives[3];
            var a2_2 = derivatives[4];

            var n = Vector3d.CrossProduct(a1, a2);
            n.Unitize();

            var a11 = a1 * a1;
            var a12 = a1 * a2;
            var a22 = a2 * a2;

            var b11 = a1_1 * n;
            var b12 = a1_2 * n;
            var b22 = a2_2 * n;

            var aDet = a12 * a12 - a11 * a22;

            var p11 = (a12 * b12 - a22 * b11) / aDet;
            var p12 = (a12 * b22 - a22 * b12) / aDet;
            var p22 = (a12 * b12 - a11 * b22) / aDet;
            var p21 = (a12 * b11 - a11 * b12) / aDet;

            var d = Math.Sqrt(Math.Pow(p11 - p22, 2) + 4 * p12 * p21);

            var k1 = 0.5 * (p11 + p22 - d);
            var k2 = 0.5 * (p11 + p22 + d);

            var k1ParameterDirection = new Vector2d((p11 - p22 - d) / (2 * p21), 1);
            var k2ParameterDirection = new Vector2d((p11 - p22 + d) / (2 * p21), 1);

            k1ParameterDirection.Unitize();
            k2ParameterDirection.Unitize();

            var k1Direction = k1ParameterDirection.X * a1 + k1ParameterDirection.Y * a2;
            var k2Direction = k2ParameterDirection.X * a1 + k2ParameterDirection.Y * a2;

            k1Direction.Unitize();
            k2Direction.Unitize();

            return new SurfaceCurvature(x, a1, a2, n, k1, k2, k1ParameterDirection, k2ParameterDirection, k1Direction, k2Direction);
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

        public bool AngleByCurvature(double k, out double angle1, out double angle2)
        {
            var c = Complex.Sqrt(K2 - k) / Complex.Sqrt(K2 - K1);
            var s = Complex.Sqrt(K1 - k) / Complex.Sqrt(K1 - K2);

            if (c.Imaginary != 0 || s.Imaginary != 0)
            {
                angle1 = default;
                angle2 = default;
                return false;
            }

            angle1 = Math.Atan2(s.Real, c.Real);
            angle2 = Math.Atan2(s.Real, -c.Real);

            return true;
        }
    }
}
