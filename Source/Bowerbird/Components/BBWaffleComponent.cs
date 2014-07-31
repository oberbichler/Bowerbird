using Bowerbird.Crafting;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bowerbird.Components
{
    public class BBWaffleComponent : GH_Component
    {
        public BBWaffleComponent() : base("BB Waffle", "BBWaffle", "Create a waffle structure from a mesh" + Util.InfoString, "Bowerbird", "Crafting") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh of the model", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Division X", "X", "Divisions in x axis", GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("Division Y", "Y", "Divisions in y axis", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Thickness", "T", "Thickness of the parts", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Base Plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("Deeper", "D", "Makes the slits deeper", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Slices U", "U", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Slices V", "V", "", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("Section planes U", "PU", "", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("Section planes V", "PV", "", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var mesh = default(Mesh);
            var divisionX = default(int);
            var divisionY = default(int);
            var thickness = default(double);
            var plane = default(Plane);
            var deeper = default(double);

            DA.GetData(0, ref mesh);
            DA.GetData(1, ref divisionX);
            DA.GetData(2, ref divisionY);
            DA.GetData(3, ref thickness);
            DA.GetData(4, ref plane);
            DA.GetData(5, ref deeper);


            // --- Check

            if (thickness <= 0)
                throw new Exception(@"Thickness must be a positive value!");

            if (deeper < 0)
                throw new Exception(@"Border must be a positive value!");

            if (!mesh.IsValid)
                throw new Exception(@"Mesh is not a valid!");

            if (!mesh.IsClosed)
                throw new Exception(@"Mesh is not closed!");


            // --- Execute

            var unit = DocumentTolerance();

            var box = mesh.Box(plane);
            box.X.Grow(1);
            box.Y.Grow(1);
            box.Z.Grow(1);

            var corners = box.GetCorners();

            var origin = corners[0];
            var unitX = box.Plane.XAxis;
            var unitY = box.Plane.YAxis;
            var unitZ = box.Plane.ZAxis;

            var sliceCountU = divisionX - 1;
            var sliceCountV = divisionY - 1;

            var slitPlanesU = new SlitPlane[sliceCountU];
            var slitPlanesV = new SlitPlane[sliceCountV];

            var u = new double[sliceCountU];
            var v = new double[sliceCountV];


            for (int i = 0; i < sliceCountU; i++)
            {
                u[i] = (1 + i) * box.X.Length / divisionX;

                var sectionOrigin = origin + u[i] * unitX;
                var sectionPlane = new Plane(sectionOrigin, unitY, unitZ);

                slitPlanesU[i] = new SlitPlane(mesh, sectionPlane, unit);
            }

            for (int i = 0; i < sliceCountV; i++)
            {
                v[i] = (1 + i) * box.Y.Length / divisionY;

                var secOrigin = origin + v[i] * unitY;
                var secPlane = new Plane(secOrigin, unitX, unitZ);

                slitPlanesV[i] = new SlitPlane(mesh, secPlane, unit);
            }

            for (int i = 0; i < sliceCountU; i++)
            {
                for (int j = 0; j < sliceCountV; j++)
                {
                    var p0 = origin + u[i] * unitX + v[j] * unitY - unit * unitZ;
                    var p1 = p0 + (box.Z.Length + unit) * unitZ;

                    slitPlanesU[i].AddSlit(p0, p1, thickness, deeper);
                    slitPlanesV[j].AddSlit(p1, p0, thickness, deeper);
                }
            }


            // --- Output
                     
            DA.SetEnum2D(0, slitPlanesU.Select(o => o.GetResult()));
            DA.SetEnum2D(1, slitPlanesV.Select(o => o.GetResult()));
            DA.SetEnum1D(2, slitPlanesU.Select(o => o.Plane));
            DA.SetEnum1D(3, slitPlanesV.Select(o => o.Plane));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.icon_waffle; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{FE7B90FF-E3A6-4AE7-8097-C81D34257756}"); }
        }
    }
}
