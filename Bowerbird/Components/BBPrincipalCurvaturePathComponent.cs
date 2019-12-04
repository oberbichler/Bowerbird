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
    public class BBPrincipalCurvaturePathComponent : GH_Component
    {
        public BBPrincipalCurvaturePathComponent() : base("BB Principal Curvature Path", "BBPrin", "", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Type", "T", "", GH_ParamAccess.item, true);
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

            var stepSize = default(double);
            var type = default(bool);
            var angle = default(double);

            DA.GetData(0, ref type);
            DA.GetData(1, ref angle);
            DA.GetData(2, ref stepSize);

            // --- Execute

            var path = PrincipalCurvaturePath.Create(stepSize, type, angle);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{E540E209-EF00-4E66-9460-CC62C1D394AB}");
    }
}