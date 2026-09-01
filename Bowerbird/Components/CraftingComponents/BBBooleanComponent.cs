using Bowerbird.Crafting;
using Bowerbird.Components;
using Bowerbird.Migration;
using Clipper2Lib;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Bowerbird.Components.CraftingComponents;

public class BBBooleanComponent : GH_Component
{
    public BBBooleanComponent() 
        : base("BB Boolean", "BBBool", "Boolean operation between two sets of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline")
    {
        UpdateMessage();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "A", "", GH_ParamAccess.list);
        pManager.AddCurveParameter("Curves", "B", "", GH_ParamAccess.list);
        pManager.AddPlaneParameter("Plane", "P", "", GH_ParamAccess.item);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
    }

    private ClipType _operation = ClipType.Union;

    public ClipType Operation
    {
        get => _operation;
        set
        {
            _operation = value;
            UpdateMessage();
        }
    }

    private FillRule _fillRule = FillRule.EvenOdd;

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
        Message = $"{Operation} / {FillRule}";
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        GHUtility.SetMenuList(this, menu, "Change operation", () => Operation, o => Operation = o,
            o => o != ClipType.None);
        GH_DocumentObject.Menu_AppendSeparator(menu);
        GHUtility.SetMenuList(this, menu, "Change fill rule", () => FillRule, o => FillRule = o);
    }

    public override bool Write(GH_IWriter writer)
    {
        writer.Set("Operation", Operation);
        writer.Set("FillRule", FillRule);
        return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
        var opValue = 0;
        reader.TryGetInt32("Operation", ref opValue);

        if (reader.ItemExists("FillType"))
        {
            // Legacy format (old ClipperLib): remap shifted integer values
            Operation = LegacyEnumMapping.RemapClipType(opValue);

            var fillValue = 0;
            reader.TryGetInt32("FillType", ref fillValue);
            FillRule = LegacyEnumMapping.RemapFillRule(fillValue);
        }
        else
        {
            // Current format (Clipper2)
            Operation = (ClipType)opValue;
            FillRule = reader.GetOrDefault("FillRule", FillRule.EvenOdd);
        }

        return base.Read(reader);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var curvesA = new List<Curve>();
        var curvesB = new List<Curve>();
        var planeInput = default(Plane);

        DA.GetDataList(0, curvesA);
        DA.GetDataList(1, curvesB);

        Plane? plane = null;
        if (DA.GetData(2, ref planeInput))
        {
            plane = planeInput;
        }

        try
        {
            var result = BBPolyline.Boolean(Operation, FillRule, curvesA, curvesB, plane);
            DA.SetDataList(0, result);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_boolean;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new ("{192AB461-640A-4786-998E-662B3DF2B9C2}");
}
