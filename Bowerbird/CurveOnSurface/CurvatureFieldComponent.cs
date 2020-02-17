using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Bowerbird
{
    public class CurvatureFieldComponent : GH_Component
    {
        public CurvatureFieldComponent() : base("BB Curvature Field", "Field", "Beta! Interface might change!", "Bowerbird", "Curvature")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("U Count", "U", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("V Count", "V", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Value", "v", "", GH_ParamAccess.item);
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

        private SpaceTypes _space;

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
            Utility.SetMenuList(this, menu, "", () => Space, o => Space = o);
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

            var surface = default(Surface);
            var uCount = default(int);
            var vCount = default(int);
            var value = default(double);

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref uCount)) return;
            if (!DA.GetData(2, ref vCount)) return;
            if (!DA.GetData(3, ref value)) return;


            // --- Execute

            var aList = new List<Line>();
            var bList = new List<Line>();

            for (int i = 0; i < uCount; i++)
            {
                var u = surface.Domain(0).ParameterAt(1.0 / (uCount - 1) * i);

                for (int j = 0; j < vCount; j++)
                {
                    var v = surface.Domain(1).ParameterAt(1.0 / (vCount - 1) * j);

                    var crv = Curvature.SurfaceCurvature.Create(surface, u, v);

                    var x = Space == SpaceTypes.UV ? new Point3d(u, v, 0) : crv.X;

                    if (!crv.FindNormalCurvature(value, out double angle1, out double angle2))
                        continue;

                    crv.ComputeDirections(angle1, angle2, out var a, out var b);

                    if (Space == SpaceTypes.UV)
                    {
                        a = ToUV(crv.A1, crv.A2, a);
                        b = ToUV(crv.A1, crv.A2, b);
                    }

                    aList.Add(new Line(x - a / 2, x + a / 2));
                    bList.Add(new Line(x - b / 2, x + b / 2));
                }
            }


            // --- Output

            DA.SetDataList(0, aList);
            DA.SetDataList(1, bList);
        }

        protected static Vector3d ToUV(Vector3d a1, Vector3d a2, Vector3d d)
        {
            var det = a1.X * a2.Y - a2.X * a1.Y;
            var u = (d.X * a2.Y - d.Y * a2.X) / det;
            var v = (d.Y * a1.X - d.X * a1.Y) / det;
            return new Vector3d(u, v, 0);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{E0660D1B-F785-4242-813F-CAB730C93DE5}");
    }
}