using Bowerbird.Curvature;
using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class ConstructCurvatureFieldComponent : GH_Component
    {
        public ConstructCurvatureFieldComponent() : base("BB Curvature Field", "Field", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
            UpdateMessage();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("U Count", "U", "", GH_ParamAccess.item, 10);
            pManager.AddIntegerParameter("V Count", "V", "", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Scale", "s", "", GH_ParamAccess.item, 0.1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("A", "A", "", GH_ParamAccess.list);
            pManager.AddLineParameter("B", "B", "", GH_ParamAccess.list);
        }

        public enum SpaceTypes
        {
            XYZ = 0,
            UV = 1
        }

        private SpaceTypes _space = SpaceTypes.XYZ;

        public SpaceTypes Space
        {
            get
            {
                return _space;
            }
            set
            {
                _space = value;
                UpdateMessage();
            }
        }

        private void UpdateMessage()
        {
            Message = Space.ToString();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Utility.SetMenuList(this, menu, "Change space", () => Space, o => Space = o);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.Set("Space", Space);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            Space = reader.GetOrDefault("Space", SpaceTypes.XYZ);

            return base.Read(reader);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var path = default(Path);
            var surface = default(Surface);
            var uCount = default(int);
            var vCount = default(int);
            var scale = default(double);

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref surface)) return;
            if (!DA.GetData(2, ref uCount)) return;
            if (!DA.GetData(3, ref vCount)) return;
            if (!DA.GetData(4, ref scale)) return;


            // --- Execute

            var aList = new List<Line>();
            var bList = new List<Line>();

            var size = scale * 0.5;

            for (int i = 0; i < uCount; i++)
            {
                var u = surface.Domain(0).ParameterAt(1.0 / (uCount - 1) * i);

                for (int j = 0; j < vCount; j++)
                {
                    var v = surface.Domain(1).ParameterAt(1.0 / (vCount - 1) * j);

                    var uv = new Vector2d(u, v);

                    if (!path.Directions(surface, uv, out var u1, out var u2, out var d1, out var d2))
                        continue;

                    var x = Space == SpaceTypes.UV ? new Point3d(u, v, 0) : surface.PointAt(u, v);

                    if (Space == SpaceTypes.UV)
                    {
                        aList.Add(new Line(x - u1 * size, x + u1 * size));
                        bList.Add(new Line(x - u2 * size, x + u2 * size));
                    }
                    else
                    {
                        aList.Add(new Line(x - d1 * size, x + d1 * size));
                        bList.Add(new Line(x - d2 * size, x + d2 * size));
                    }
                }
            }


            // --- Output

            DA.SetDataList(0, aList);
            DA.SetDataList(1, bList);
        }

        protected override Bitmap Icon => Properties.Resources.icon_field;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{E0660D1B-F785-4242-813F-CAB730C93DE5}");
    }
}