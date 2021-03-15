﻿using Bowerbird.Crafting;
using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents
{
    public class BBXorComponent : GH_Component
    {
        public BBXorComponent() : base("BB XOR", "BBXor", "XOR of a set of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "A", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curve", "B", "", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "P", "", GH_ParamAccess.item);

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curvesA = new List<Curve>();
            var curvesB = new List<Curve>();
            var plane = default(Plane?);

            DA.GetDataList(0, curvesA);
            DA.GetDataList(1, curvesB);
            DA.GetData(2, ref plane);


            // --- Execute

            var result = BBPolyline.Boolean(ClipType.ctXor, PolyFillType.pftNonZero, curvesA, curvesB, plane);


            // --- Output

            DA.SetDataList(0, result);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.icon_xor;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{08B6E938-38EB-41DB-8E67-EC14EEB4D258}");
    }
}
