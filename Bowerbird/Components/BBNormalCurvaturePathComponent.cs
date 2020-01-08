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
        public BBNormalCurvaturePathComponent() : base("BB Normal Curvature Path", "BBκn", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Angle", "A", "", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("Direction", "D", "", GH_ParamAccess.item, 3);
            Utility.AddNamedValues<Path.Types>(Params.Input[2]);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            Utility.SetInputValueList<Path.Types>(Params.Input[2]);
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var value = default(double);
            var angle = default(double);
            var directionValue = default(int);

            if (!DA.GetData(0, ref value)) return;
            if (!DA.GetData(1, ref angle)) return;
            if (!DA.GetData(2, ref directionValue)) return;

            var direction = (Path.Types)directionValue;

            // --- Execute

            var path = NormalCurvaturePath.Create(value, angle, direction);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => Properties.Resources.icon_asymptotic_path;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{F3B20CCE-C077-480E-A03D-48845AF2EF6F}");
    }
}