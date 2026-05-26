using Bowerbird.Crafting;
using Clipper2Lib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents;

public class BBDifferenceComponent : GH_Component
{
    public BBDifferenceComponent() 
        : base("BB Difference", "BBDiff", "Difference of a set of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline") 
    { 
    }

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
        var curvesA = new List<Curve>();
        var curvesB = new List<Curve>();
        var plane = default(Plane?);

        if (!DA.GetDataList(0, curvesA)) return;
        if (!DA.GetDataList(1, curvesB)) return;
        DA.GetData(2, ref plane);

        var result = BBPolyline.Boolean(ClipType.Difference, FillRule.NonZero, curvesA, curvesB, plane);

        DA.SetDataList(0, result);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_difference;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new ("{EA7CEFD1-6123-453E-9A8D-5F2D1CE3610A}");
}
