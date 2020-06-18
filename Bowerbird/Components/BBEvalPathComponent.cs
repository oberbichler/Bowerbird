using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Bowerbird.Components
{
    public class BBEvalPathComponent : GH_Component
    {
        public BBEvalPathComponent() : base("BB Evaluate Path", "BBEvalPath", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
            UpdateMessage();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Point", "P", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Direction 1", "D1", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Direction 2", "D2", "", GH_ParamAccess.item);
        }
        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var path = default(Path);
            var surface = default(Surface);
            var startingPoint = default(Vector3d);

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref surface)) return;
            if (!DA.GetData(2, ref startingPoint)) return;

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

            var d1 = path.InitialDirection(surface, new Vector2d(startingPoint.X, startingPoint.Y), false);
            var d2 = path.InitialDirection(surface, new Vector2d(startingPoint.X, startingPoint.Y), true);

            // --- Output

            DA.SetData(0, surface.PointAt(startingPoint.X, startingPoint.Y));
            DA.SetData(1, d1);
            DA.SetData(2, d2);
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

        public override Guid ComponentGuid => new Guid("{C4549D21-3262-45B9-A15E-F7CBED1111FB}");
    }
}
