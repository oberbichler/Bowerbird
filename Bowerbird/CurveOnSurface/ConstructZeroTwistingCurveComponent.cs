using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace Bowerbird.Components
{
    class ConstructZeroTwistingCurveComponent : GH_Component
    {
        public ConstructZeroTwistingCurveComponent() : base("BB Construct Zero-Twisting Curve", "Zero-Twist", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Orientable Curve", "C", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curve = default(Curve);

            if (!DA.GetData(0, ref curve)) return;


            // --- Execute

            var crv = OrientedCurve.Create(curve);


            // --- Output

            DA.SetData(0, crv);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{8C91CB72-DA0E-456E-914A-42B02D299681}");
    }
}