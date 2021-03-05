using Rhino.Geometry;
using System.Diagnostics;
using static System.Math;

namespace Bowerbird.Curvature
{
    struct PrincipalCurvature
    {
        public bool Compute(Surface surface, double u, double v)
        {
            surface.Evaluate(u, v, 2, out X, out var ders);

            A1 = ders[0];
            A2 = ders[1];
            A1_1 = ders[2];
            A1_2 = ders[3];
            A2_2 = ders[4];

            G11 = A1 * A1;
            G12 = A1 * A2;
            G22 = A2 * A2;

            N = Vector3d.CrossProduct(A1, A2);
            N /= N.Length;

            H11 = N * A1_1;
            H12 = N * A1_2;
            H22 = N * A2_2;

            {
                var det = G11 * G22 - G12 * G12;

                if (det == 0)
                    return false;

                K11 = (G22 * H11 - G12 * H12) / det;
                K12 = (G22 * H12 - G12 * H22) / det;
                K21 = (G11 * H12 - G12 * H11) / det;
                K22 = (G11 * H22 - G12 * H12) / det;
            }

            {
                var det = 4 * K12 * K21 + Pow(K11 - K22, 2);

                if (det < 0)
                    return false;

                K1 = 0.5 * (K11 + K22 - Sqrt(det));
                K2 = 0.5 * (K11 + K22 + Sqrt(det));
            }

            D1 = K12 * A1 + (K1 - K11) * A2;
            D1 /= D1.Length;

            Debug.Assert(D1.IsValid);

            D2 = (K2 - K22) * A1 + K21 * A2;
            D2 /= D2.Length;

            Debug.Assert(D1.IsValid);

            return true;
        }


        public bool FindNormalCurvature(double value, double angleOffset, out Vector3d dir1, out Vector3d dir2)
        {
            var t = (2 * value - K1 - K2) / (K1 - K2);

            if (Abs(t) > 1)
            {
                dir1 = default;
                dir2 = default;
                return false;
            }

            var alpha = 0.5 * Acos(t);

            {
                var cosAlpha = Cos(alpha + angleOffset);
                var sinAlpha = Sin(alpha + angleOffset);

                var du = A2 * D2 * cosAlpha - A2 * D1 * sinAlpha;
                var dv = A1 * D1 * sinAlpha - A1 * D2 * cosAlpha;

                dir1 = du * A1 + dv * A2;

                Debug.Assert(dir1.Length > 0);

                dir1 /= dir1.Length;

                Debug.Assert(dir1.IsValid);
            }

            {
                var cosAlpha = Cos(alpha - angleOffset);
                var sinAlpha = Sin(alpha - angleOffset);

                var du = A2 * D2 * cosAlpha + A2 * D1 * sinAlpha;
                var dv = -A1 * D1 * sinAlpha - A1 * D2 * cosAlpha;

                dir2 = du * A1 + dv * A2;

                Debug.Assert(dir2.Length > 0);

                dir2 /= dir2.Length;

                Debug.Assert(dir2.IsValid);
            }

            return true;
        }

        public bool FindGeodesicTorsion(double value, double angleOffset, out Vector3d dir1, out Vector3d dir2)
        {
            var t = 2 * value / (K2 - K1);

            if (Abs(t) > 1)
            {
                dir1 = default;
                dir2 = default;
                return false;
            }

            var alpha = 0.5 * Asin(t);

            {
                var cosAlpha = Cos(alpha + angleOffset);
                var sinAlpha = Sin(alpha + angleOffset);

                var du = A2 * D2 * cosAlpha - A2 * D1 * sinAlpha;
                var dv = A1 * D1 * sinAlpha - A1 * D2 * cosAlpha;

                dir1 = du * A1 + dv * A2;

                Debug.Assert(dir1.Length > 0);

                dir1 /= dir1.Length;

                Debug.Assert(dir1.IsValid);
            }

            {
                var cosAlpha = Cos(alpha - angleOffset);
                var sinAlpha = Sin(alpha - angleOffset);

                var du = A2 * D2 * cosAlpha + A2 * D1 * sinAlpha;
                var dv = -A1 * D1 * sinAlpha - A1 * D2 * cosAlpha;

                dir2 = du * A1 + dv * A2;

                Debug.Assert(dir2.Length > 0);

                dir2 /= dir2.Length;

                Debug.Assert(dir2.IsValid);
            }

            return true;
        }


        public Point3d X;

        public Vector3d A1;

        public Vector3d A2;

        public Vector3d A1_1;

        public Vector3d A1_2;

        public Vector3d A2_2;

        public Vector3d N;

        public double G11;

        public double G12;

        public double G22;

        public double H11;

        public double H12;

        public double H22;

        public double K11;

        public double K12;

        public double K21;

        public double K22;

        public double K1;

        public double K2;

        public Vector3d D1;

        public Vector3d D2;

        public Vector3d U1 => new Vector3d(K12, K1 - K11, 0);

        public Vector3d U2 => new Vector3d(K2 - K22, K21, 0);
    }
}
