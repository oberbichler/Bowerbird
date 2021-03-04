using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class EvaluateComponent : GH_Component
    {
        public EvaluateComponent() : base("BB Evaluate CurveOnSurface", "BBEval", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Parameter", "t", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Tangent", "T", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Binormal", "B", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Normal", "N", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curveOnSurface = default(IOrientableCurve);
            var t = default(double);

            if (!DA.GetData(0, ref curveOnSurface)) return;
            if (!DA.GetData(1, ref t)) return;


            // --- Execute

            var point = curveOnSurface.PointAt(t);
            var tangent = curveOnSurface.TangentAt(t);
            var binormal = curveOnSurface.BinormalAt(t);
            var normal = curveOnSurface.NormalAt(t);


            // --- Output

            DA.SetData(0, point);
            DA.SetData(1, tangent);
            DA.SetData(2, binormal);
            DA.SetData(3, normal);
        }

        protected override Bitmap Icon => Properties.Resources.icon_curve_on_surface_evaluate;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{B5702539-C9E3-4EFB-9DEF-445515F304D7}");
    }
}