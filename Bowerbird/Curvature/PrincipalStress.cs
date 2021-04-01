using Rhino.Geometry;
using System;
using System.Diagnostics;

using static System.Math;

namespace Bowerbird.Curvature
{
    class PrincipalStress
    {
        readonly Transform Dm;
        readonly Transform Db;
        readonly Surface RefSurface;
        readonly Surface ActSurface;

        public double S1;
        public double S2;
        public Vector3d U1;
        public Vector3d U2;
        public Vector3d D1;
        public Vector3d D2;
        public Vector3d A1;
        public Vector3d A2;
        public Vector3d N;

        public PrincipalStress(Surface refSurface, Surface actSurface, double thickness, double youngsModulus, double poissonsRatio)
        {
            RefSurface = refSurface ?? throw new ArgumentNullException(nameof(refSurface));
            ActSurface = actSurface ?? throw new ArgumentNullException(nameof(actSurface));

            Dm = new Transform();
            {
                var f = youngsModulus * thickness / (1.0 - Pow(poissonsRatio, 2));
                Dm[0, 0] = f;
                Dm[0, 1] = poissonsRatio * f;
                Dm[0, 2] = 0;
                Dm[1, 0] = poissonsRatio * f;
                Dm[1, 1] = f;
                Dm[1, 2] = 0;
                Dm[2, 0] = 0;
                Dm[2, 1] = 0;
                Dm[2, 2] = 0.5 * (1 - poissonsRatio) * f;
            }

            Db = new Transform();
            {
                var f = youngsModulus * Pow(thickness, 3) / (12 * (1 - Pow(poissonsRatio, 2)));

                Db[0, 0] = f;
                Db[0, 1] = poissonsRatio * f;
                Db[0, 2] = 0;
                Db[1, 0] = poissonsRatio * f;
                Db[1, 1] = f;
                Db[1, 2] = 0;
                Db[2, 0] = 0;
                Db[2, 1] = 0;
                Db[2, 2] = 0.5 * (1 - poissonsRatio) * f;
            }
        }

        public bool Compute(double u, double v)
        {
            if (!RefSurface.Evaluate(u, v, 2, out var _, out var refDers))
                return false;

            if (!ActSurface.Evaluate(u, v, 2, out var _, out var actDers))
                return false;

            var refA1 = refDers[0];
            var refA2 = refDers[1];
            var refA1_1 = refDers[2];
            var refA1_2 = refDers[3];
            var refA2_2 = refDers[4];

            var actA1 = actDers[0];
            var actA2 = actDers[1];
            var actA1_1 = actDers[2];
            var actA1_2 = actDers[3];
            var actA2_2 = actDers[4];

            var g1 = refA1;
            var g2 = refA2;

            var gAB = new Vector3d(g1 * g1, g2 * g2, g1 * g2);

            var e1 = g1 / g1.Length;
            var e2 = g2 - (g2 * e1) * e1;
            e2 /= e2.Length;

            var det = gAB[0] * gAB[1] - gAB[2] * gAB[2];

            var gABCon = new Vector3d(gAB[1] / det, gAB[0] / det, -gAB[2] / det);

            var gCon1 = gABCon[0] * g1 + gABCon[2] * g2;
            var gCon2 = gABCon[2] * g1 + gABCon[1] * g2;

            var eg11 = e1 * gCon1;
            var eg12 = e1 * gCon2;
            var eg21 = e2 * gCon1;
            var eg22 = e2 * gCon2;

            // G_con -> E
            var TGcE = new Transform();
            TGcE[0, 0] = eg11 * eg11;
            TGcE[0, 1] = 0;
            TGcE[0, 2] = 0;
            TGcE[1, 0] = eg21 * eg21;
            TGcE[1, 1] = eg22 * eg22;
            TGcE[1, 2] = 2 * eg21 * eg22;
            TGcE[2, 0] = 2 * eg11 * eg21;
            TGcE[2, 1] = 0;
            TGcE[2, 2] = 2 * eg11 * eg22;
            TGcE[3, 3] = 1;

            // # E -> G
            var TEG = new Transform();
            TEG[0, 0] = eg11 * eg11;
            TEG[0, 1] = eg21 * eg21;
            TEG[0, 2] = 2.0 * eg11 * eg21;
            TEG[1, 0] = eg12 * eg12;
            TEG[1, 1] = eg22 * eg22;
            TEG[1, 2] = 2.0 * eg12 * eg22;
            TEG[2, 0] = eg11 * eg12;
            TEG[2, 1] = eg21 * eg22;
            TEG[2, 2] = eg11 * eg22 + eg21 * eg12;
            TEG[3, 3] = 1;

            // ---

            g1 = actA1;
            g2 = actA2;

            var gab = new Vector3d(g1 * g1, g2 * g2, g1 * g2);

            det = gab[0] * gab[1] - gab[2] * gab[2];

            var gabcon = new Vector3d(gab[1] / det, gab[0] / det, -gab[2] / det);

            // gCon1 = gabcon[0] * g1 + gabcon[2] * g2;
            gCon2 = gabcon[2] * g1 + gabcon[1] * g2;

            e1 = g1 / g1.Length;
            e2 = gCon2 / gCon2.Length;

            eg11 = e1 * g1;
            eg12 = e1 * g2;
            eg21 = e2 * g1;
            eg22 = e2 * g2;

            // g_con -> e
            var Tge = new Transform();
            Tge[0, 0] = eg11 * eg11;
            Tge[0, 1] = eg12 * eg12;
            Tge[0, 2] = 2.0 * eg11 * eg12;
            Tge[1, 0] = eg21 * eg21;
            Tge[1, 1] = eg22 * eg22;
            Tge[1, 2] = 2.0 * eg21 * eg22;
            Tge[2, 0] = eg11 * eg21;
            Tge[2, 1] = eg12 * eg22;
            Tge[2, 2] = eg11 * eg22 + eg12 * eg21;

            // ---

            var refA3 = Vector3d.CrossProduct(refA1, refA2);
            var actA3 = Vector3d.CrossProduct(actA1, actA2);

            var dA = refA3.Length;
            var da = actA3.Length;

            var detF = da / dA;

            refA3 /= dA;
            actA3 /= da;

            // ---

            var refA = new Vector3d(refA1 * refA1, refA2 * refA2, refA1 * refA2);
            var actA = new Vector3d(actA1 * actA1, actA2 * actA2, actA1 * actA2);

            var epsCur = 0.5 * (actA - refA);
            var epsCar = TGcE * epsCur;

            var nPk2Car = Dm * epsCar;

            var nPk2Cur = TEG * nPk2Car;
            var nCauCur = nPk2Cur / detF;

            var n = Tge * nCauCur;

            // ---

            var refB = new Vector3d(refA1_1 * refA3, refA2_2 * refA3, refA1_2 * refA3);
            var actB = new Vector3d(actA1_1 * actA3, actA2_2 * actA3, actA1_2 * actA3);

            var actBCar = TGcE * actB;

            var kapCur = refB - actB;
            var kapCar = TGcE * kapCur;

            var mPk2Car = Db * kapCar;
            var mPk2Cur = TEG * mPk2Car;
            var mCauCur = mPk2Cur / detF;

            var m = Tge * mCauCur;

            // ---

            S1 = 0.5 * (m[0] + m[1] + Sqrt(Pow(m[0] - m[1], 2) + 4 * Pow(m[2], 2)));
            S2 = 0.5 * (m[0] + m[1] - Sqrt(Pow(m[0] - m[1], 2) + 4 * Pow(m[2], 2)));

            if (m[0] != 0 && m[1] != 0)
            {
                var alpha = 0.5 * Atan2(2 * m[2], m[0] - m[1]);

                var cosAlpha = Cos(alpha);
                var sinAlpha = Sin(alpha);
                var sin2Alpha = Sin(2 * alpha);
                var cosAlpha2 = cosAlpha * cosAlpha;
                var sinAlpha2 = sinAlpha * sinAlpha;

                var pmN11 = cosAlpha2 * n[0] + sinAlpha2 * n[1] + sin2Alpha * n[2];
                var pmN22 = cosAlpha2 * n[1] + sinAlpha2 * n[0] - sin2Alpha * n[2];
                var pmN12 = cosAlpha * sinAlpha * (n[1] - n[0]) + (sinAlpha2 - cosAlpha2) * n[2];

                var pmB11 = cosAlpha2 * actBCar[0] + sinAlpha2 * actBCar[1] + sin2Alpha * actBCar[2];
                var pmB22 = sinAlpha2 * actBCar[0] + cosAlpha2 * actBCar[1] - sin2Alpha * actBCar[2];

                pmN11 -= S1 * pmB11;
                pmN22 -= S2 * pmB22;

                n[0] = cosAlpha2 * pmN11 + sinAlpha2 * pmN22 - sin2Alpha * pmN12;
                n[1] = sinAlpha2 * pmN11 + cosAlpha2 * pmN22 + sin2Alpha * pmN12;
                n[2] = cosAlpha * sinAlpha * (pmN11 - pmN22) + (sinAlpha2 - cosAlpha2) * pmN12;

                var det2 = Pow(n[0] - n[1], 2) + 4 * Pow(n[2], 2);

                S1 = 0.5 * (n[0] + n[1] + Sqrt(det2));
                S2 = 0.5 * (n[0] + n[1] - Sqrt(det2));
            }

            {
                var alpha = 0.5 * Atan2(2 * n[2], n[0] - n[1]);

                D1 = new Vector3d(refA1);
                D1.Unitize();
                D1.Rotate(alpha, Vector3d.CrossProduct(refA1, refA2)).AssertTrue();

                D2 = D1;
                D2.Rotate(PI / 2, Vector3d.CrossProduct(refA1, refA2)).AssertTrue();

                var cosAlpha = Cos(alpha);
                var sinAlpha = Sin(alpha);

                var E1 = refA1 / refA1.Length;
                var E2 = refA2 - (refA2 * E1) * E1;
                E2 /= E2.Length;

                var du1 = refA2 * E2 * cosAlpha - refA2 * E1 * sinAlpha;
                var dv1 = refA1 * E1 * sinAlpha - refA1 * E2 * cosAlpha;

                D1 = du1 * refA1 + dv1 * refA2;

                du1 /= D1.Length;
                dv1 /= D1.Length;

                D1 = du1 * refA1 + dv1 * refA2;

                var du2 = -refA2 * E2 * sinAlpha - refA2 * E1 * cosAlpha;
                var dv2 = refA1 * E1 * cosAlpha + refA1 * E2 * sinAlpha;

                D2 = du2 * refA1 + dv2 * refA2;

                du2 /= D2.Length;
                dv2 /= D2.Length;

                D2 = du2 * refA1 + dv2 * refA2;

                Debug.Assert(Abs(D1.Length - 1) < 1e-8);
                Debug.Assert(Abs(D2.Length - 1) < 1e-8);
                Debug.Assert(Abs(D1 * D2) < 1e-8);

                A1 = refA1;
                A2 = refA2;
                N = refA3 / refA3.Length;

                U1 = new Vector3d(du1, dv1, 0);
                U2 = new Vector3d(du2, dv2, 0);
            }

            return true;
        }
    }
}
