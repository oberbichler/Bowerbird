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
    public class DeconstructCurveOnSurfaceComponent : GH_Component
    {
        public DeconstructCurveOnSurfaceComponent() : base("BB Deconstruct CurveOnSurface", "Deconstruct", "", "Bowerbird", "Curve on Surface")
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
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{27762E37-A208-4C07-BA02-DA650F7052B1}");
    }
}