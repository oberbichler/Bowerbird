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
    public class BBPathfinderComponent : GH_Component
    {
        public BBPathfinderComponent() : base("BB Pathfinder", "Pathfinder", "", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddParameter(new PathParameter(), "Path", "P", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Parameter Point", "uv", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points 1", "P1", "", GH_ParamAccess.list);
            pManager.AddPointParameter("Points 2", "P2", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var surface = default(Surface);
            var path = default(Path);
            var uv = default(Vector3d);

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref path)) return;
            if (!DA.GetData(2, ref uv)) return;

            // --- Execute

            var pathFinder = Pathfinder.Create(path, surface, uv);

            // --- Output

            DA.SetDataList(0, pathFinder.Points);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{7492BB23-F1BC-4C31-8F60-FC91C3DE4A0C}");
    }
}