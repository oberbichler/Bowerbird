using Bowerbird.Parameters;
using GH_IO.Serialization;
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
using System.Windows.Forms;

namespace Bowerbird.Components
{
    public class BBPathfinderComponent : GH_Component
    {
        public BBPathfinderComponent() : base("BB Pathfinder", "BBPathfinder", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
            UpdateMessage();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Point", "P", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step Size", "H", "", GH_ParamAccess.item, 0.1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Paths", "P", "", GH_ParamAccess.list);
        }
        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var path = default(Path);
            var surface = default(Surface);
            var startingPoint = default(Vector3d);
            var stepSize = default(double);

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref surface)) return;
            if (!DA.GetData(2, ref startingPoint)) return;
            if (!DA.GetData(3, ref stepSize)) return;

            // --- Execute

            if (StartPointType == StartPointTypes.UV)
            {
                var u = surface.Domain(0).NormalizedParameterAt(startingPoint.X);
                var v = surface.Domain(1).NormalizedParameterAt(startingPoint.Y);
                startingPoint = new Vector3d(u, v, 0);
            }

            surface = (Surface)surface.Duplicate();
            surface.SetDomain(0, new Interval(0, 1));
            surface.SetDomain(1, new Interval(0, 1));

            if (StartPointType == StartPointTypes.XYZ)
            {
                surface.ClosestPoint((Point3d)startingPoint, out double u, out double v);
                startingPoint = new Vector3d(u, v, 0);
            }

            var paths = new List<Polyline>();
            
            var type = (path as NormalCurvaturePath)?.Type ?? (path as GeodesicTorsionPath).Type;

            if (type.HasFlag(Path.Types.First))
            {
                var pathfinder = Pathfinder.Create(path, surface, startingPoint, false, stepSize);
                var polyline = new Polyline(pathfinder.Points);
                paths.Add(polyline);
            }

            if (type.HasFlag(Path.Types.Second))
            {
                var pathfinder = Pathfinder.Create(path, surface, startingPoint, true, stepSize);
                var polyline = new Polyline(pathfinder.Points);
                paths.Add(polyline);
            }

            // --- Output

            var curves = new List<CurveOnSurface>();

            foreach (var p in paths)
            {
                var curve = new PolylineCurve(p.Select(o => { surface.ClosestPoint(o, out var u, out var v); return new Point3d(u, v, 0); }));
                curves.Add(CurveOnSurface.Create(surface, curve));
            }

            DA.SetDataList(0, curves);
        }

        public enum StartPointTypes
        {
            XYZ = 0,
            UV = 1
        }

        private StartPointTypes _startPointType;

        public StartPointTypes StartPointType
        {
            get
            {
                return _startPointType;
            }
            set
            {
                _startPointType = value;
                UpdateMessage();
            }
        }

        private void UpdateMessage()
        {
            Message = StartPointType.ToString();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Utility.SetMenuList(this, menu, "", () => StartPointType, o => StartPointType = o);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.Set("StartPointType", StartPointType);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            StartPointType = reader.GetOrDefault("StartPointType", StartPointTypes.XYZ);

            return base.Read(reader);
        }

        protected override Bitmap Icon => Properties.Resources.icon_pathfinder;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{7492BB23-F1BC-4C31-8F60-FC91C3DE4A0C}");
    }
}
