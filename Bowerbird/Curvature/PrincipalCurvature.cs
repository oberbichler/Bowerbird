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

            var eps = Max(Abs(K11), Abs(K22)) * 1e-10;

            if (Abs(K12) < eps && Abs(K21) < eps)
            {
                K1 = K11;
                K2 = K22;

                var l1 = Sqrt(G11);
                var l2 = Sqrt(G22);

                Debug.Assert(Abs(A1.Length / l1 - 1) < 1e-8);
                Debug.Assert(Abs(A2.Length / l2 - 1) < 1e-8);

                var du1 = 1 / l1;
                var dv2 = 1 / l2;

                U1 = new Vector3d(du1, 0, 0);
                U2 = new Vector3d(0, dv2, 0);

                D1 = du1 * A1;
                D2 = dv2 * A2;
            }
            else
            {
                var det = 4 * K12 * K21 + Pow(K11 - K22, 2);

                if (det < 0)
                    return false;

                K1 = 0.5 * (K11 + K22 + Sqrt(det));
                K2 = 0.5 * (K11 + K22 - Sqrt(det));

                double du1, dv1, du2, dv2;

                if (Abs(K12) > Abs(K21))
                {
                    du1 = K12;
                    dv1 = K1 - K11;

                    du2 = K12;
                    dv2 = K2 - K11;
                }
                else // K21 != 0
                {
                    du1 = K1 - K22;
                    dv1 = K21;

                    du2 = K2 - K22;
                    dv2 = K21;
                }

                // first direction

                var l1 = Sqrt(G11 * du1 * du1 + 2 * G12 * du1 * dv1 + G22 * dv1 * dv1);

                Debug.Assert(Abs(l1 / (du1 * A1 + dv1 * A2).Length - 1) < 1e-8);

                du1 /= l1;
                dv1 /= l1;

                U1 = new Vector3d(du1, dv1, 0);

                D1 = du1 * A1 + dv1 * A2;

                // second direction

                var l2 = Sqrt(G11 * du2 * du2 + 2 * G12 * du2 * dv2 + G22 * dv2 * dv2);

                Debug.Assert(Abs(l2 / (du2 * A1 + dv2 * A2).Length - 1) < 1e-8);

                du2 /= l2;
                dv2 /= l2;

                U2 = new Vector3d(du2, dv2, 0);

                D2 = du2 * A1 + dv2 * A2;
            }

            Debug.Assert(D1.IsValid && !D1.IsZero);
            Debug.Assert(D2.IsValid && !D2.IsZero);

            Debug.Assert(U1.IsValid && !U1.IsZero);
            Debug.Assert(U2.IsValid && !U2.IsZero);

            Debug.Assert(Abs(D1 * D2) < 1e-4);

            return true;
        }


        public bool FindNormalCurvature(double value, double angleOffset, out Vector3d u1, out Vector3d u2, out Vector3d dir1, out Vector3d dir2)
        {
            var t = (2 * value - K1 - K2) / (K1 - K2);

            if (Abs(t) > 1 || double.IsNaN(t))
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

            if (Abs(t) > 1 || double.IsNaN(t))
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
