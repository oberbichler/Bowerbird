using Bowerbird.Crafting;
using CavalierContours.Polyline;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents;

public class BBCavalierIntersectionComponent : GH_Component
{
    public BBCavalierIntersectionComponent()
        : base("BB Cavalier Contours Intersection", "BBCavalierInt", "Intersection of two sets of planar closed curves preserving circular arcs using Cavalier Contours" + Util.InfoString, "Bowerbird", "Polyline")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves A", "A", "First set of planar closed curves", GH_ParamAccess.list);
        pManager.AddCurveParameter("Curves B", "B", "Second set of planar closed curves", GH_ParamAccess.list);
        pManager.AddPlaneParameter("Plane", "P", "Projection plane (optional, auto-detected if not specified)", GH_ParamAccess.item);
        pManager.AddNumberParameter("Tolerance", "T", "Simplification/flattening tolerance", GH_ParamAccess.item, 0.01);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "C", "Resulting boolean curves", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var curvesA = new List<Curve>();
        var curvesB = new List<Curve>();
        var planeInput = default(Plane);
        var tolerance = default(double);

        if (!DA.GetDataList(0, curvesA)) return;
        DA.GetDataList(1, curvesB);

        Plane? plane = null;
        if (DA.GetData(2, ref planeInput))
        {
            plane = planeInput;
        }

        if (!DA.GetData(3, ref tolerance)) return;

        this.WarnAboutOpenCurves(curvesA.Concat(curvesB));

        try
        {
            var result = BBCavalier.Boolean(BooleanOp.And, curvesA, curvesB, plane, tolerance);
            DA.SetDataList(0, result);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.icon_cavalier_and;

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public override Guid ComponentGuid { get; } = new("{D47A07DB-C3D8-4984-AAF2-751A636F2AC6}");
}
