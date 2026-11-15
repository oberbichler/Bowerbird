using Bowerbird.Crafting;
using CavalierContours.Polyline;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents;

public class BBCavalierUnionComponent : GH_Component
{
    public BBCavalierUnionComponent()
        : base("BB Cavalier Contours Union", "BBCavalierUnion", "Union of a set of planar closed curves preserving circular arcs using Cavalier Contours" + Util.InfoString, "Bowerbird", "Polyline")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "C", "Planar closed curves to union", GH_ParamAccess.list);
        pManager.AddPlaneParameter("Plane", "P", "Projection plane (optional, auto-detected if not specified)", GH_ParamAccess.item);
        pManager.AddNumberParameter("Tolerance", "T", "Simplification/flattening tolerance", GH_ParamAccess.item, 0.01);

        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "C", "Resulting boolean curves", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var curves = new List<Curve>();
        var planeInput = default(Plane);
        var tolerance = default(double);

        if (!DA.GetDataList(0, curves)) return;

        Plane? plane = null;
        if (DA.GetData(1, ref planeInput))
        {
            plane = planeInput;
        }

        if (!DA.GetData(2, ref tolerance)) return;

        this.WarnAboutOpenCurves(curves);

        try
        {
            var result = BBCavalier.Boolean(BooleanOp.Or, curves, Array.Empty<Curve>(), plane, tolerance);
            DA.SetDataList(0, result);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.icon_cavalier_or;

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public override Guid ComponentGuid { get; } = new("{3B19DA0B-2617-4E54-987F-608E35C5AACA}");
}
