using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Bowerbird
{
    public class DeconstructCurveOnSurfaceComponent : GH_Component
    {
        public DeconstructCurveOnSurfaceComponent() : base("BB Deconstruct CurveOnSurface", "Deconstruct", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Approximation", "A", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curveOnSurface = default(CurveOnSurface);

            if (!DA.GetData(0, ref curveOnSurface)) return;


            // --- Execute

            var surface = curveOnSurface.Surface;
            var curve = curveOnSurface.Curve;


            // --- Output

            DA.SetData(0, surface);
            DA.SetData(1, curve);
            DA.SetData(2, curveOnSurface.ToCurve(DocumentTolerance()));
        }

        protected override Bitmap Icon => Properties.Resources.icon_curve_on_surface_deconstruct;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{27762E37-A208-4C07-BA02-DA650F7052B1}");
    }
}