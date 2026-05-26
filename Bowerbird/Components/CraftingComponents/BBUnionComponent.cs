using Bowerbird.Crafting;
using Bowerbird.Components;
using Clipper2Lib;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Bowerbird.Components.CraftingComponents;

public class BBUnionComponent : GH_Component
{
    public BBUnionComponent() 
        : base("BB Union", "BBUnion", "Union of a set of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline")
    {
        UpdateMessage();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "C", "", GH_ParamAccess.list);
        pManager.AddPlaneParameter("Plane", "P", "", GH_ParamAccess.item);

        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
    }

    private FillRule _fillRule = FillRule.NonZero;

    public FillRule FillRule
    {
        get => _fillRule;
        set
        {
            _fillRule = value;
            UpdateMessage();
        }
    }

    private void UpdateMessage()
    {
        Message = FillRule.ToString();
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        GHUtility.SetMenuList(this, menu, "Change fill rule", () => FillRule, o => FillRule = o);
    }

    public override bool Write(GH_IWriter writer)
    {
        writer.Set("FillRule", FillRule);
        return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
        FillRule = reader.GetOrDefault("FillRule", FillRule.NonZero);
        return base.Read(reader);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var curves = new List<Curve>();
        var plane = default(Plane?);

        if (!DA.GetDataList(0, curves)) return;
        DA.GetData(1, ref plane);

        var result = BBPolyline.Boolean(ClipType.Union, FillRule, curves, Array.Empty<Curve>(), plane);

        DA.SetDataList(0, result);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_union;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new ("{5585F2E8-4EAC-45A9-BBE6-6BD40379B7A4}");
}
