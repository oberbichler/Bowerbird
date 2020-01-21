using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace Bowerbird
{
    public class ConstructCurveOnSurfaceComponent : GH_Component
    {
        public ConstructCurveOnSurfaceComponent() : base("BB Construct CurveOnSurface", "BBCrvOnSrf", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
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
            pManager.AddCurveParameter("Approximation", "A", "", GH_ParamAccess.item);
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

            var curveOnSurface = CurveOnSurface.Create(surface, curve);


            // --- Output

            DA.SetData(0, curveOnSurface);
            DA.SetData(1, curveOnSurface.ToCurve(DocumentTolerance()));
        }

        protected override Bitmap Icon => Properties.Resources.icon_curve_on_surface_construct;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{DE62F1CD-5E5C-4372-B1BB-2494ED62D349}");
    }
}