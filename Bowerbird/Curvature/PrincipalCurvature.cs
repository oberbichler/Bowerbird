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

            if (K12 * K21 < 1e-10)
            {
                K1 = K11;
                K2 = K22;

                var l1 = A1.Length;
                var l2 = A2.Length;

                Debug.Assert(l1 > 0);
                Debug.Assert(l2 > 0);

                D1 = A1 / l1;
                D2 = A2 / l2;

                U1 = new Vector3d(1 / l1, 0, 0);
                U2 = new Vector3d(0, 1 / l2, 0);
            }
            else
            {
                var det = 4 * K12 * K21 + Pow(K11 - K22, 2);

                if (det < 0)
                    return false;

                K1 = 0.5 * (K11 + K22 + Sqrt(det));
                K2 = 0.5 * (K11 + K22 - Sqrt(det));

                D1 = K12 * A1 + (K1 - K11) * A2;
                D2 = (K2 - K22) * A1 + K21 * A2;

                var l1 = D1.Length;
                var l2 = D2.Length;

                Debug.Assert(l1 > 0);
                Debug.Assert(l2 > 0);

                D1 /= l1;
                D2 /= l2;

                U1 = new Vector3d(K12 / l1, (K1 - K11) / l1, 0);
                U2 = new Vector3d((K2 - K22) / l2, K21 / l2, 0);
            }

            Debug.Assert(D1.IsValid && !D1.IsZero);
            Debug.Assert(D2.IsValid && !D2.IsZero);

            Debug.Assert(U1.IsValid && !U1.IsZero);
            Debug.Assert(U2.IsValid && !U2.IsZero);

            return true;
        }


        public bool FindNormalCurvature(double value, double angleOffset, out Vector3d u1, out Vector3d u2, out Vector3d dir1, out Vector3d dir2)
        {
            var t = (2 * value - K1 - K2) / (K1 - K2);

            if (Abs(t) > 1)
            {
                u1 = default;
                u2 = default;
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

                u1 = new Vector3d(du, dv, 0);

                dir1 = du * A1 + dv * A2;

                Debug.Assert(dir1.Length > 0);

                var l = dir1.Length;

                u1 /= l;
                dir1 /= l;

                Debug.Assert(dir1.IsValid);
            }

            {
                var cosAlpha = Cos(alpha - angleOffset);
                var sinAlpha = Sin(alpha - angleOffset);

                var du = A2 * D2 * cosAlpha + A2 * D1 * sinAlpha;
                var dv = -A1 * D1 * sinAlpha - A1 * D2 * cosAlpha;

                u2 = new Vector3d(du, dv, 0);

                dir2 = du * A1 + dv * A2;

                Debug.Assert(dir2.Length > 0);

                var l = dir2.Length;

                u2 /= l;
                dir2 /= l;

                Debug.Assert(dir2.IsValid);
            }

            return true;
        }

        public bool FindGeodesicTorsion(double value, double angleOffset, out Vector3d u1, out Vector3d u2, out Vector3d dir1, out Vector3d dir2)
        {
            var t = 2 * value / (K2 - K1);

            if (Abs(t) > 1)
            {
                u1 = default;
                u2 = default;
                dir1 = default;
                dir2 = default;
                return false;
            }

            var alpha = 0.5 * Asin(t) + angleOffset;

            var cosAlpha = Cos(alpha);
            var sinAlpha = Sin(alpha);

            {

                var du = A2 * D2 * cosAlpha - A2 * D1 * sinAlpha;
                var dv = A1 * D1 * sinAlpha - A1 * D2 * cosAlpha;

                u1 = new Vector3d(du, dv, 0);

                dir1 = du * A1 + dv * A2;

                Debug.Assert(dir1.Length > 0);

                var l = dir1.Length;

                dir1 /= l;
                u1 /= l;

                Debug.Assert(dir1.IsValid);
            }

            {
                var du = -A2 * D2 * sinAlpha - A2 * D1 * cosAlpha;
                var dv = A1 * D1 * cosAlpha + A1 * D2 * sinAlpha;

                u2 = new Vector3d(du, dv, 0);

                dir2 = du * A1 + dv * A2;

                Debug.Assert(dir2.Length > 0);

                var l = dir2.Length;

                dir2 /= l;
                u2 /= l;

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

        public Vector3d U1;

        public Vector3d U2;
    }
}
