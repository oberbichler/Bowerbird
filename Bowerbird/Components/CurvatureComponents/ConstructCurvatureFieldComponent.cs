using Bowerbird.Curvature;
using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class ConstructCurvatureFieldComponent : GH_Component
    {
        public ConstructCurvatureFieldComponent() : base("BB Curvature Field", "Field", "Show the direction field to a given path type. The field can be displayed in the geometry or parameter space (see context menu).", "Bowerbird", "Paths")
        {
            UpdateMessage();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "Path type to be displayed as a vector field.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Surface", "S", "Surface or Brep for which the field is to be displayed.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "D", "Approximate distance between the evaluation points.", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Scale", "s", "Size of the cross axes.", GH_ParamAccess.item, 0.1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("First Directions", "A", "List of the first directions.", GH_ParamAccess.list);
            pManager.AddLineParameter("Second Directions", "B", "List of the second directions.", GH_ParamAccess.list);
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
            var brep = default(Brep);
            var distance = default(double);
            var scale = default(double);

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref brep)) return;
            if (!DA.GetData(2, ref distance)) return;
            if (!DA.GetData(3, ref scale)) return;


            // --- Execute

            if (brep.Faces.Count != 1 && Space != SpaceTypes.XYZ)
                throw new Exception("Multipatches only support XYZ space");

            var size = scale * 0.5;

            var aList = new List<Line>();
            var bList = new List<Line>();

            foreach (var face in brep.Faces)
            {
                face.SetDomain(0, new Interval(0, 1)).AssertTrue();
                face.SetDomain(1, new Interval(0, 1)).AssertTrue();

                var surface = face.UnderlyingSurface();

                if (face.IsSurface)
                {
                    var us = face.IsoCurve(0, face.Domain(1).ParameterAt(0.5)).DivideByLength(distance, true).ToList();
                    var vs = face.IsoCurve(1, face.Domain(0).ParameterAt(0.5)).DivideByLength(distance, true).ToList();

                    // add last points
                    us.Add(1);
                    vs.Add(1);

                    foreach (var u in us)
                    {
                        foreach (var v in vs)
                        {
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
                }
                else
                {
                    var faceBrep = face.ToBrep();

                    var parameters = MeshingParameters.QualityRenderMesh;
                    parameters.MaximumEdgeLength = distance;

                    var meshs = Mesh.CreateFromBrep(faceBrep, parameters);

                    foreach (var mesh in meshs)
                    {
                        foreach (var vertex in mesh.TopologyVertices)
                        {
                            if (!face.ClosestPoint(vertex, out var u, out var v))
                                continue;

                            var uv = new Vector2d(u, v);

                            if (!path.Directions(surface, uv, out var _, out var _, out var d1, out var d2))
                                continue;

                            var x = surface.PointAt(u, v);

                            aList.Add(new Line(x - d1 * size, x + d1 * size));
                            bList.Add(new Line(x - d2 * size, x + d2 * size));
                        }
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