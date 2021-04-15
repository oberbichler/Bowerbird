using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class DeconstructCurveOnSurfaceComponent : GH_Component
    {
        public DeconstructCurveOnSurfaceComponent() : base("BB Deconstruct CurveOnSurface", "Deconstruct", "Deconstruct an embedded curve into its components.", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "Embeded curve to deconstruct", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Surface on which the curve is embedded", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "Parameter curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("Approximation", "A", "Approximation of the embedded curve as ordinary Rhino curve", GH_ParamAccess.item);
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

        protected override Bitmap Icon { get; } = Properties.Resources.icon_curve_on_surface_deconstruct;

        public override GH_Exposure Exposure { get; } = GH_Exposure.primary;

        public override Guid ComponentGuid { get; } = new Guid("{27762E37-A208-4C07-BA02-DA650F7052B1}");
    }
}