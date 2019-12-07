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
    public class BBNormalCurvaturePathComponent : GH_Component
    {
        public BBNormalCurvaturePathComponent() : base("BB Normal Curvature Path", "BBNCrv", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Angle", "A", "", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path", "P", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var value = default(double);
            var angle = default(double);

            DA.GetData(0, ref value);
            DA.GetData(1, ref angle);

            // --- Execute

            var path = NormalCurvaturePath.Create(value, angle);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => Properties.Resources.icon_asymptotic_path;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{F3B20CCE-C077-480E-A03D-48845AF2EF6F}");
    }
}