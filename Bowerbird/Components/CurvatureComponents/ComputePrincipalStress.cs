using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Diagnostics;
using System.Drawing;

using static System.Math;

namespace Bowerbird.Components.CurvatureComponents
{
    public class ComputePrincipalStress : GH_Component
    {
        public ComputePrincipalStress() : base("BB Principal Stress", "BB PS", "", "Bowerbird", "Curvature")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Undeformed Surface", "S", "", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Deformed Surface", "s", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Parameter Point", "uv", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Youngs Modulus", "E", "", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Poissons Ration", "ν", "", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Thickness", "t", "", GH_ParamAccess.item, 1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("S1", "S1", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("S2", "S2", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("S1 Parameter Direction", "d1", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("S2 Parameter Direction", "d2", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("S1 Direction", "D1", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("S2 Direction", "D2", "", GH_ParamAccess.item);
        }

        static Transform TransformToLocalCartesian(Vector3d a1, Vector3d a2)
        {
            var g11 = a1 * a1;
            var g12 = a1 * a2;
            var g22 = a2 * a2;

            var det = g11 * g22 - g12 * g12;
            var gCon = new Vector3d(g22 / det, g11 / det, -g12 / det);

            var gCon1 = gCon[0] * a1 + gCon[2] * a2;
            var gCon2 = gCon[2] * a1 + gCon[1] * a2;

            var e1 = a1 / a1.Length;
            var e2 = gCon2 / gCon2.Length;

            var eg11 = e1 * gCon1;
            var eg12 = e1 * gCon2;
            var eg21 = e2 * gCon1;
            var eg22 = e2 * gCon2;

            var transformation = new Transform();
            transformation[0, 0] = eg11 * eg11;
            transformation[0, 1] = eg12 * eg12;
            transformation[0, 2] = 2 * eg11 * eg12;
            transformation[1, 0] = eg21 * eg21;
            transformation[1, 1] = eg22 * eg22;
            transformation[1, 2] = 2 * eg21 * eg22;
            transformation[2, 0] = 2 * eg11 * eg21;
            transformation[2, 1] = 2 * eg12 * eg22;
            transformation[2, 2] = 2 * (eg11 * eg22 + eg12 * eg21);

            return transformation;
        }

        static Transform MaterialMatrix(double youngsModulus, double poissonsRatio, double thickness)
        {
            var f = youngsModulus * thickness / (1.0 - Math.Pow(poissonsRatio, 2));

            var dm = new Transform();
            dm[0, 0] = f;
            dm[0, 1] = poissonsRatio * f;
            dm[0, 2] = 0;
            dm[1, 0] = poissonsRatio * f;
            dm[1, 1] = f;
            dm[1, 2] = 0;
            dm[2, 0] = 0;
            dm[2, 1] = 0;
            dm[2, 2] = 0.5 * (1 - poissonsRatio) * f;

            return dm;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var refSurface = default(Surface);
            var actSurface = default(Surface);
            var uv = default(Point3d);
            var youngsModulus = default(double);
            var poissonsRatio = default(double);
            var thickness = default(double);

            if (!DA.GetData(0, ref refSurface)) return;
            if (!DA.GetData(1, ref actSurface)) return;
            if (!DA.GetData(2, ref uv)) return;
            if (!DA.GetData(3, ref youngsModulus)) return;
            if (!DA.GetData(4, ref poissonsRatio)) return;
            if (!DA.GetData(5, ref thickness)) return;


            // --- Execute

            var u = uv.X;
            var v = uv.Y;

            refSurface.Evaluate(u, v, 1, out var _, out var refDerivatives);
            var refA1 = refDerivatives[0];
            var refA2 = refDerivatives[1];

            actSurface.Evaluate(u, v, 1, out var _, out var actDerivatives);
            var actA1 = actDerivatives[0];
            var actA2 = actDerivatives[1];

            var e = default(Vector3d);
            e[0] = actA1 * refA1 - refA1.SquareLength;
            e[1] = actA2 * refA2 - refA2.SquareLength;
            e[2] = 0.5 * (actA1 * refA2 + actA2 * refA1) - refA1 * refA2;

            var material = MaterialMatrix(youngsModulus, poissonsRatio, thickness);

            e.Transform(TransformToLocalCartesian(refA1, refA2));
            e.Transform(material);

            var K11 = e[0];
            var K12 = e[2];
            var K21 = e[2];
            var K22 = e[1];

            var K1 = default(double);
            var K2 = default(double);

            {
                var det = 4 * K12 * K21 + Pow(K11 - K22, 2);

                Debug.Assert(det >= 0);

                K1 = 0.5 * (K11 + K22 - Sqrt(det));
                K2 = 0.5 * (K11 + K22 + Sqrt(det));
            }

            var D1 = K12 * refA1 + (K1 - K11) * refA2;
            D1 /= D1.Length;

            Debug.Assert(D1.IsValid);

            var D2 = (K2 - K22) * refA1 + K21 * refA2;
            D2 /= D2.Length;

            Debug.Assert(D1.IsValid);

            // --- Output

            DA.SetData(0, K1);
            DA.SetData(1, K2);
            DA.SetData(4, D1);
            DA.SetData(5, D2);
        }

        protected override Bitmap Icon { get; } = null;

        public override GH_Exposure Exposure { get; } = Info.Experimental ? GH_Exposure.primary : GH_Exposure.hidden;

        public override Guid ComponentGuid { get; } = new Guid("{393BFCE6-1A65-4FAD-8621-1BE1FF6D5EF9}");
    }
}