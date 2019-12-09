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
    public class BBGeodesicTorsionPathComponent : GH_Component
    {
        public BBGeodesicTorsionPathComponent() : base("BB Geodesic Torsion Path", "BBGTrs", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path", "P", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var value = default(double);

            DA.GetData(0, ref value);

            // --- Execute

            var path = GeodesicTorsionPath.Create(value);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => Properties.Resources.icon_principal_path;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{E540E209-EF00-4E66-9460-CC62C1D394AB}");
    }
}