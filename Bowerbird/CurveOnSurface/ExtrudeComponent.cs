using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bowerbird
{
    public class ExtrudeComponent : GH_Component
    {
        public ExtrudeComponent() : base("BB Extrude CurveOnSurface", "Extrude", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Thickness", "t", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Offset", "o", "", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curveOnSurface = default(CurveOnSurface);
            var thickness = default(double);
            var offset = default(double);

            if (!DA.GetData(0, ref curveOnSurface)) return;
            if (!DA.GetData(1, ref thickness)) return;
            if (!DA.GetData(2, ref offset)) return;


            // --- Execute

            var polyline = default(PolylineCurve);

            if (curveOnSurface.Curve.TryGetPolyline(out var points))
                polyline = new PolylineCurve(points);
            else
                polyline = curveOnSurface.Curve.ToPolyline(DocumentTolerance(), DocumentAngleTolerance(), 0, 0);
            
            var points3d = new List<Point3d>(polyline.PointCount);

            var mesh = new Mesh();

            for (int i = 0; i < polyline.PointCount; i++)
            {
                var point2d = polyline.Point(i);
                curveOnSurface.Curve.ClosestPoint(point2d, out var t);
                var point = curveOnSurface.PointAt(t);
                points3d.Add(point);
                var normal = curveOnSurface.NormalAt(t);
                var d = 0.5 * thickness;
                mesh.Vertices.Add(point + (offset + d) * normal);
                mesh.Vertices.Add(point + (offset - d) * normal);
            }

            for (int i = 1; i < polyline.PointCount; i++)
            {
                var a = (i - 1) * 2;
                var b = (i - 1) * 2 + 1;
                var c = i * 2 + 1;
                var d = i * 2;
                mesh.Faces.AddFace(a, b, c, d);
            }

            if (Unroll)
            {
                var m = mesh.DuplicateMesh();

                var p = points3d[0];

                var oldA = m.Vertices[0];
                var oldB = m.Vertices[1];

                var newA = new Point3d(0, offset + 0.5 * thickness, 0);
                var newB = new Point3d(0, offset - 0.5 * thickness, 0);

                m.Vertices.SetVertex(0, newA);
                m.Vertices.SetVertex(1, newB);

                var outOfPlane = 0.0;

                for (int i = 1; i < polyline.PointCount; i++)
                {
                    var t = points3d[i] - points3d[i - 1];

                    var n = new Vector3d(oldA.X - oldB.X, oldA.Y - oldB.Y, oldA.Z - oldB.Z);

                    var c = i * 2 + 1;
                    var d = i * 2;

                    var oldC = m.Vertices[c];
                    var oldD = m.Vertices[d];

                    var oldPlane = new Plane(p, t, n);
                    var newPlane = new Plane(newB + (0.5 * thickness - offset) * (newA - newB) / (newA - newB).Length, Vector3d.CrossProduct(newA - newB, Vector3d.ZAxis), newA - newB);

                    var xf = Transform.PlaneToPlane(oldPlane, newPlane);

                    var newC = new Point3f(oldC.X, oldC.Y, oldC.Z);
                    newC.Transform(xf);
                    outOfPlane = Math.Max(outOfPlane, Math.Abs(newC.Z));
                    newC.Z = 0;

                    var newD = new Point3f(oldD.X, oldD.Y, oldD.Z);
                    newD.Transform(xf);
                    outOfPlane = Math.Max(outOfPlane, Math.Abs(newD.Z));
                    newD.Z = 0;

                    m.Vertices.SetVertex(c, newC);
                    m.Vertices.SetVertex(d, newD);

                    oldA = oldD;
                    oldB = oldC;
                    newA = newD;
                    newB = newC;

                    p = points3d[i];
                }

                mesh = m;
            }

            // --- Output

            DA.SetData(0, mesh);
        }

        private bool _unroll;

        public bool Unroll
        { 
            get
            {
                return _unroll;
            }
            set
            {
                _unroll = value;
                UpdateMessage();
            }
        }

        private void UpdateMessage()
        {
            Message = Unroll ? "Unroll" : null;
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Utility.SetMenuToggle(this, menu, "Unroll", () => Unroll, o => Unroll = o);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.Set("Unroll", Unroll);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            Unroll = reader.GetOrDefault("Unroll", false);

            return base.Read(reader);
        }

        protected override Bitmap Icon => Properties.Resources.icon_curve_on_surface_extrude;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{860BD3B6-41C7-4B72-8E6B-C9F1E602238E}");
    }
}