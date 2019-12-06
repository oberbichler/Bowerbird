using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
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
        public BBPathfinderComponent() : base("BB Pathfinder", "BBPathfinder", "", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddParameter(new PathParameter(), "Path", "P", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Point", "xyz", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Type", "T", "", GH_ParamAccess.item, 3);
            (pManager[3] as Param_Integer).AddNamedValue("First", 1);
            (pManager[3] as Param_Integer).AddNamedValue("Second", 2);
            (pManager[3] as Param_Integer).AddNamedValue("Both", 3);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Paths", "P", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var surface = default(Surface);
            var path = default(Path);
            var startingPoint = default(Vector3d);
            var typeValue = default(int);

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref path)) return;
            if (!DA.GetData(2, ref startingPoint)) return;
            if (!DA.GetData(3, ref typeValue)) return;

            var type = (Path.Type)typeValue;

            // --- Execute

            surface = (Surface)surface.Duplicate();
            surface.SetDomain(0, new Interval(0, 1));
            surface.SetDomain(1, new Interval(0, 1));

            surface.ClosestPoint((Point3d)startingPoint, out double u, out double v);

            var uv = new Vector3d(u, v, 0);

            var paths = new List<Polyline>();

            if (type.HasFlag(Path.Type.First))
            {
                var pathfinder = Pathfinder.Create(path, surface, uv, false);
                var polyline = new Polyline(pathfinder.Points);
                paths.Add(polyline);
            }

            if (type.HasFlag(Path.Type.Second))
            {
                var pathfinder = Pathfinder.Create(path, surface, uv, true);
                var polyline = new Polyline(pathfinder.Points);
                paths.Add(polyline);
            }

            // --- Output

            DA.SetDataList(0, paths);
        }

        protected override Bitmap Icon => Properties.Resources.icon_pathfinder;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{7492BB23-F1BC-4C31-8F60-FC91C3DE4A0C}");
    }
}
