using Bowerbird.Crafting;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bowerbird.Components.CraftingComponents
{
    public class BBSectionComponent : GH_Component
    {
        public BBSectionComponent() : base("BB Section", "BBSection", "Create a section model from a mesh" + Util.InfoString, "Bowerbird", "Crafting") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh of the model", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Planes", "P", "Section Planes", GH_ParamAccess.list);
            pManager.AddNumberParameter("Thickness", "T", "Thickness of the parts", GH_ParamAccess.item);
            pManager.AddNumberParameter("Deeper", "D", "Makes the slits deeper", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Section Curves", "C", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var mesh = default(Mesh);
            var planes = new List<Plane>();
            var thickness = default(double);
            var deeper = default(double);

            DA.GetData(0, ref mesh);
            DA.GetDataList(1, planes);
            DA.GetData(2, ref thickness);
            DA.GetData(3, ref deeper);


            // --- Execute

            var tolerance = DocumentTolerance();

            var slitPlanes = new SlitPlane[planes.Count];

            for (int i = 0; i < planes.Count; i++)
                slitPlanes[i] = new SlitPlane(mesh, planes[i], tolerance);

            var bbox = mesh.GetBoundingBox(false);
            var dmax = bbox.Diagonal.Length;

            for (int i = 0; i < planes.Count; i++)
            {
                for (int j = i + 1; j < planes.Count; j++)
                {
                    var a = planes[i];
                    var b = planes[j];

                    if (a.Normal.IsParallelTo(b.Normal) != 0)
                        continue;

                    IntersectPlanes(a, b, out var origin, out var direction);

                    var cPlane = new Plane(bbox.Center, direction);
                    origin = (Vector3d)cPlane.ClosestPoint((Point3d)origin);

                    var originA = origin.Map2D(a);
                    var directionA = direction.Map2D(a);

                    var originB = origin.Map2D(b);
                    var directionB = direction.Map2D(b);

                    var line = new LineCurve((Point3d)(origin - dmax * direction), ((Point3d)origin + dmax * direction));

                    var alpha = Vector3d.VectorAngle(a.Normal, b.Normal);
                    var t = thickness * Math.Tan(alpha / 2);

                    slitPlanes[i].AddSlit(line.PointAtStart, line.PointAtEnd, t, deeper);
                    slitPlanes[j].AddSlit(line.PointAtEnd, line.PointAtStart, t, deeper);
                }
            }


            // --- Output

            DA.SetEnum2D(0, slitPlanes.Select(o => o.GetResult()));
        }

        void IntersectPlanes(Plane a, Plane b, out Vector3d origin, out Vector3d direction)
        {
            var a1 = a.Normal.X;
            var b1 = a.Normal.Y;
            var c1 = a.Normal.Z;
            var d1 = (Vector3d)a.Origin * a.Normal;

            var a2 = b.Normal.X;
            var b2 = b.Normal.Y;
            var c2 = b.Normal.Z;
            var d2 = (Vector3d)b.Origin * b.Normal;

            direction = Vector3d.CrossProduct(a.Normal, b.Normal);

            var a3 = direction.X;
            var b3 = direction.Y;
            var c3 = direction.Z;

            var x = (b3 * c2 * d1 - b2 * c3 * d1 - b3 * c1 * d2 + b1 * c3 * d2) / (a3 * b2 * c1 - a2 * b3 * c1 - a3 * b1 * c2 + a1 * b3 * c2 + a2 * b1 * c3 - a1 * b2 * c3);
            var y = (a3 * c2 * d1 - a2 * c3 * d1 - a3 * c1 * d2 + a1 * c3 * d2) / (-(a3 * b2 * c1) + a2 * b3 * c1 + a3 * b1 * c2 - a1 * b3 * c2 - a2 * b1 * c3 + a1 * b2 * c3);
            var z = (a3 * b2 * d1 - a2 * b3 * d1 - a3 * b1 * d2 + a1 * b3 * d2) / (a3 * b2 * c1 - a2 * b3 * c1 - a3 * b1 * c2 + a1 * b3 * c2 + a2 * b1 * c3 - a1 * b2 * c3);

            origin = new Vector3d(x, y, z);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.icon_section;

        public override Guid ComponentGuid => new Guid("{63013336-F9FB-4902-B63C-6B13C8A2F2DA}");
    }
}
