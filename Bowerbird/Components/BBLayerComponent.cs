using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bowerbird.Components
{
    public class BBLayerComponent : GH_Component
    {
        public BBLayerComponent() : base("BB Layer", "BBLayer", "Create a layer model from a mesh" + Util.InfoString, "Bowerbird", "Crafting") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh of the model", GH_ParamAccess.item);
            pManager.AddNumberParameter("Thickness", "T", "Thickness of the parts", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Base plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("Border", "B", "Overlapping of the layers", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Outlines", "O", "Outer contours of the layers", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Inlines", "I", "Inner contours of the layers", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("Planes", "P", "Layer planes", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var mesh = default(Mesh);
            var thickness = default(double);
            var plane = default(Plane);
            var border = default(double);

            DA.GetData(0, ref mesh);
            DA.GetData(1, ref thickness);
            DA.GetData(2, ref plane);
            DA.GetData(3, ref border);


            // --- Check

            if (thickness <= 0)
                throw new Exception(@"Thickness must be a positive value!");

            if (border < 0)
                throw new Exception(@"Border must be a positive value!");

            if (!mesh.IsValid)
                throw new Exception(@"Mesh is not valid!");

            if (!mesh.IsClosed)
                throw new Exception(@"Mesh is not closed!");


            // --- Execute

            var unit = DocumentTolerance();

            var box = mesh.Box(plane);

            var corners = box.GetCorners();

            var origin = corners[0];
            var unitX = box.Plane.XAxis;
            var unitY = box.Plane.YAxis;
            var unitZ = box.Plane.ZAxis;

            var layerCount = (int)Math.Round(box.Z.Length / thickness);

            var olines = new List<List<IntPoint>>[layerCount];
            var offset = new List<List<IntPoint>>[layerCount];
            var ilines = new List<List<IntPoint>>[layerCount];
            var planes = new Plane[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                var z = (i + 0.5) * thickness;

                var sectionOrigin = origin + z * unitZ;
                var sectionPlane = new Plane(sectionOrigin, unitZ);

                planes[i] = sectionPlane;

                olines[i] = mesh.Section(sectionPlane, unit);
                offset[i] = Offset(olines[i], -border / unit);
            }

            if (border > 0)
            {
                ilines[0] = new List<List<IntPoint>>();

                for (int i = 1; i < layerCount - 1; i++)
                    ilines[i] = Intersect(Intersect(offset[i], offset[i - 1]), offset[i + 1]);

                ilines[layerCount - 1] = new List<List<IntPoint>>();
            }


            // --- Output

            DA.SetEnum2D(0, olines.Select((o, i) => o.ToCurves(planes[i], unit)));
            if (border > 0) DA.SetEnum2D(1, ilines.Select((o, i) => o.ToCurves(planes[i], unit)));
            DA.SetEnum1D(2, planes);
        }

        List<List<IntPoint>> Intersect(List<List<IntPoint>> a, List<List<IntPoint>> b)
        {
            var clipper = new Clipper();

            clipper.AddPaths(a, PolyType.ptSubject, true);
            clipper.AddPaths(b, PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();

            clipper.Execute(ClipType.ctIntersection, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            return solution;
        }

        List<List<IntPoint>> Offset(List<List<IntPoint>> polygons, double delta)
        {
            var result = new List<List<IntPoint>>();

            var clipper = new ClipperOffset();
            clipper.AddPaths(polygons, JoinType.jtSquare, EndType.etClosedPolygon);
            clipper.Execute(ref result, delta);

            return result;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.icon_layer; }
        }
        
        public override Guid ComponentGuid
        {
            get { return new Guid("{40C0EE3D-E7F9-4B18-97D6-802B99A910D4}"); }
        }
    }
}
