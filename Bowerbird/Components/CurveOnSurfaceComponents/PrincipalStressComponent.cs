using Bowerbird.Curvature;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Diagnostics;
using System.Drawing;

using static System.Math;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class PrincipalStressComponent : GH_Component
    {
        public PrincipalStressComponent() : base("BB Principal Stress", "σ", "", "Bowerbird", "Stress")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Reference Surface", "S", "", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Actual Surface", "s", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Parameter UV", "UV", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Pricipal Stress 1", "σ1", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Pricipal Stress 2", "σ2", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Pricipal Stress Direction 1", "D1", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Pricipal Stress Direction 2", "D2", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var refSurface = default(Surface);
            var actSurface = default(Surface);
            var uv = default(Vector3d);

            if (!DA.GetData(0, ref refSurface)) return;
            if (!DA.GetData(1, ref actSurface)) return;
            if (!DA.GetData(2, ref uv)) return;


            // --- Execute

            var u = uv.X;
            var v = uv.Y;

            var ps = new PrincipalStress(refSurface, actSurface, 1, 1, 0);
            ps.Compute(u, v);

            Dirty(refSurface, actSurface, u, v, out var es1, out var es2, out var ed1, out var ed2);

            Debug.Assert(Abs(ps.S1 - es1) < 1e-10);
            Debug.Assert(Abs(ps.S2 - es2) < 1e-10);
            Debug.Assert((ps.D1 - ed1).Length < 1e-10);
            Debug.Assert((ps.D2 - ed2).Length < 1e-10);

            // --- Output

            DA.SetData(0, ps.S1);
            DA.SetData(1, ps.S2);
            DA.SetData(2, ps.D1);
            DA.SetData(3, ps.D2);
        }

        private static void Dirty(Surface refSurface, Surface actSurface, double u, double v, out double sigma1, out double sigma2, out Vector3d d1, out Vector3d d2)
        {
            if (!refSurface.Evaluate(u, v, 2, out var _, out var refDers))
                throw new Exception();

            if (!actSurface.Evaluate(u, v, 2, out var _, out var actDers))
                throw new Exception();

            var youngsModulus = 1;
            var thickness = 1;
            var poissonsRatio = 0;

            var dm = new Transform();
            {
                var f = youngsModulus * thickness / (1.0 - Pow(poissonsRatio, 2));
                dm[0, 0] = f;
                dm[0, 1] = poissonsRatio * f;
                dm[0, 2] = 0;
                dm[1, 0] = poissonsRatio * f;
                dm[1, 1] = f;
                dm[1, 2] = 0;
                dm[2, 0] = 0;
                dm[2, 1] = 0;
                dm[2, 2] = 0.5 * (1 - poissonsRatio) * f;
            }

            var db = new Transform();
            {
                var f = youngsModulus * Pow(thickness, 3) / (12 * (1 - Pow(poissonsRatio, 2)));

                db[0, 0] = f;
                db[0, 1] = poissonsRatio * f;
                db[0, 2] = 0;
                db[1, 0] = poissonsRatio * f;
                db[1, 1] = f;
                db[1, 2] = 0;
                db[2, 0] = 0;
                db[2, 1] = 0;
                db[2, 2] = 0.5 * (1 - poissonsRatio) * f;
            }



            var A1 = refDers[0];
            var A2 = refDers[1];
            var A11 = refDers[2];
            var A12 = refDers[3];
            var A22 = refDers[4];

            var a1 = actDers[0];
            var a2 = actDers[1];
            var a11 = actDers[2];
            var a12 = actDers[3];
            var a22 = actDers[4];

            var g1 = A1;
            var g2 = A2;

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

            g1 = a1;
            g2 = a2;

            var gab = new Vector3d(g1 * g1, g2 * g2, g1 * g2);

            det = gab[0] * gab[1] - gab[2] * gab[2];

            var gabcon = new Vector3d(gab[1] / det, gab[0] / det, -gab[2] / det);

            gCon1 = gabcon[0] * g1 + gabcon[2] * g2;
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

            var A3 = Vector3d.CrossProduct(A1, A2);
            var a3 = Vector3d.CrossProduct(a1, a2);

            var dA = A3.Length;
            var da = a3.Length;

            var detF = da / dA;

            A3 /= dA;
            a3 /= da;

            // ---

            var d = Vector3d.Zero;
            d[0] = a1 * a1 - A1 * A1;
            d[1] = a2 * a2 - A2 * A2;
            d[2] = a1 * a2 - A1 * A2;

            var eps_cu = 0.5 * d;
            var eps_ca = TGcE * eps_cu;

            var n_pk2_ca = dm * eps_ca;

            var n_pk2_cu = TEG * n_pk2_ca;
            var n_cau_cu = n_pk2_cu / detF;

            var n = Tge * n_cau_cu;

            var n3 = 0.5 * (n[0] + n[1] + Sqrt(Pow(n[0] - n[1], 2) + 4 * Pow(n[2], 2)));
            var n4 = 0.5 * (n[0] + n[1] - Sqrt(Pow(n[0] - n[1], 2) + 4 * Pow(n[2], 2)));

            // ---

            var bv = new Vector3d(a11 * a3, a22 * a3, a12 * a3);
            var b_ca = TGcE * bv;

            var b0 = -a11 * a3 + A11 * A3;
            var b1 = -a22 * a3 + A22 * A3;
            var b2 = -a12 * a3 + A12 * A3;

            var kap_cu = new Vector3d(b0, b1, b2);

            var kap_ca = TGcE * kap_cu;

            var m_n_pk2_ca = db * kap_ca;
            var m_n_pk2_cu = TEG * m_n_pk2_ca;
            var m_n_cau_cu = m_n_pk2_cu / detF;

            var m = Tge * m_n_cau_cu;

            var m3 = 0.5 * (m[0] + m[1] + Sqrt(Pow(m[0] - m[1], 2) + 4 * Pow(m[2], 2)));
            var m4 = 0.5 * (m[0] + m[1] - Sqrt(Pow(m[0] - m[1], 2) + 4 * Pow(m[2], 2)));

            var prestress_crv = 0;

            if (Abs(m[0]) > 0 && Abs(m[1]) > 0 && prestress_crv == 0)
            {
                var alpham = 0.0;

                if (m[0] > m[1])
                    alpham = 0.5 * Atan(2 * m[2] / (m[0] - m[1]));
                else if (m[0] < m[1])
                    alpham = PI * 0.5 - 0.5 * Atan(2 * m[2] / (m[1] - m[0]));
                else  // m[0] == m[1]
                    alpham = PI * 0.5;

                // normal force in principal moment direction
                var n_pm = Vector3d.Zero;
                n_pm[0] = n[0] * Pow(Cos(alpham), 2) + n[1] * Pow(Sin(alpham), 2) + 2 * n[2] * Sin(alpham) * Cos(alpham);
                n_pm[1] = n[0] * Pow(Sin(alpham), 2) + n[1] * Pow(Cos(alpham), 2) - 2 * n[2] * Sin(alpham) * Cos(alpham);
                n_pm[2] = -(n[0] - n[1]) * Sin(alpham) * Cos(alpham) - n[2] * (Pow(Cos(alpham), 2) - Pow(Sin(alpham), 2));

                // curvature in principal moment direction
                var b_pm = Vector3d.Zero;
                b_pm[0] = b_ca[0] * Pow(Cos(alpham), 2) + b_ca[1] * Pow(Sin(alpham), 2) + 2 * b_ca[2] * Sin(alpham) * Cos(alpham);
                b_pm[1] = b_ca[0] * Pow(Sin(alpham), 2) + b_ca[1] * Pow(Cos(alpham), 2) - 2 * b_ca[2] * Sin(alpham) * Cos(alpham);
                b_pm[2] = -(b_ca[0] - b_ca[1]) * Sin(alpham) * Cos(alpham) - b_ca[2] * (Pow(Cos(alpham), 2) - Pow(Sin(alpham), 2));

                n_pm[0] -= m3 * b_pm[0];
                n_pm[1] -= m4 * b_pm[1];

                var _n_cor = Vector3d.Zero;
                _n_cor[0] = n_pm[0] * Pow(Cos(-alpham), 2) + n_pm[1] * Pow(Sin(-alpham), 2) + 2 * n_pm[2] * Sin(-alpham) * Cos(-alpham);
                _n_cor[1] = n_pm[0] * Pow(Sin(-alpham), 2) + n_pm[1] * Pow(Cos(-alpham), 2) - 2 * n_pm[2] * Sin(-alpham) * Cos(-alpham);
                _n_cor[2] = -(n_pm[0] - n_pm[1]) * Sin(-alpham) * Cos(-alpham) - n_pm[2] * (Pow(Cos(-alpham), 2) - Pow(Sin(-alpham), 2));

                n[0] = _n_cor[0];
                n[1] = _n_cor[1];
                n[2] = _n_cor[2];


                n3 = 0.5 * (n[0] + n[1] + Sqrt(Pow(n[0] - n[1], 2) + 4 * Pow(n[2], 2)));
                n4 = 0.5 * (n[0] + n[1] - Sqrt(Pow(n[0] - n[1], 2) + 4 * Pow(n[2], 2)));
            }

            sigma1 = n3;
            sigma2 = n4;

            var s1 = n[0];
            var s2 = n[1];
            var s12 = n[2];

            var rot_angle = 0.0;

            if (Abs(s12) > 1e-14)
                rot_angle = Atan(2 * s12 / (s1 - s2)) * 0.5;

            if (s1 < s2)
                rot_angle += PI * 0.5 * Sign(s12);

            g1 = new Vector3d(A1);
            g1.Rotate(rot_angle, Vector3d.CrossProduct(A1, A2)).AssertTrue();
            g1.Unitize();

            Vector3d g2_tmp = new Vector3d(A1);
            g2_tmp.Rotate(PI * 0.5, Vector3d.CrossProduct(A1, A2));
            g2 = g2_tmp;
            g2.Rotate(rot_angle, Vector3d.CrossProduct(A1, A2)).AssertTrue();
            g2.Unitize();

            d1 = g1;
            d2 = g2;
        }

        protected override Bitmap Icon { get; } = Properties.Resources.icon_curve_on_surface_curvature;

        public override GH_Exposure Exposure { get; } = GH_Exposure.secondary;

        public override Guid ComponentGuid { get; } = new Guid("{F23CF650-B288-4E6C-968D-F3BCD64C64E9}");
    }
}