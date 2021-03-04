using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class ProjectCurveOnSurfaceComponent : GH_Component
    {
        public ProjectCurveOnSurfaceComponent() : base("BB Project CurveOnSurface", "ProjCrvOnSrf", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var surface = default(Surface);
            var curve = default(Curve);

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref curve)) return;


            // --- Execute

            surface = (Surface)surface.Duplicate();
            curve = surface.Pullback(curve, DocumentTolerance());

            var curveOnSurface = CurveOnSurface.Create(surface, curve);


            // --- Output

            DA.SetData(0, curveOnSurface);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{DD3B810B-4292-45C0-B35C-C8252E7552FD}");
    }
}