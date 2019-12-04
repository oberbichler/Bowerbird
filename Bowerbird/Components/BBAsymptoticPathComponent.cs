using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Components
{
    public class BBAsymptoticPathComponent : GH_Component
    {
        public BBAsymptoticPathComponent() : base("BB Asymptotic Path", "BBAsym", "", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Curvature", "K", "", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Angle", "A", "", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Step Size", "H", "", GH_ParamAccess.item, 0.1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path", "P", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curvature = default(double);
            var stepSize = default(double);
            var angle = default(double);

            DA.GetData(0, ref curvature);
            DA.GetData(1, ref angle);
            DA.GetData(2, ref stepSize);

            // --- Execute

            var path = AsymptoticPath.Create(stepSize, angle, curvature);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{F3B20CCE-C077-480E-A03D-48845AF2EF6F}");
    }
}