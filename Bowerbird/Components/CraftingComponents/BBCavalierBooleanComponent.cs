using Bowerbird.Crafting;
using CavalierContours.Polyline;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Windows.Forms;

namespace Bowerbird.Components.CraftingComponents;

public class BBCavalierBooleanComponent : GH_Component
{
    public BBCavalierBooleanComponent()
        : base("BB Cavalier Contours Boolean", "BBCavalierBoolean", "Perform high-precision 2D boolean operations preserving circular arcs using Cavalier Contours" + Util.InfoString, "Bowerbird", "Polyline")
    {
        UpdateMessage();
    }

    private BooleanOp _operation = BooleanOp.Or;

    public BooleanOp Operation
    {
        get => _operation;
        set
        {
            _operation = value;
            UpdateMessage();
        }
    }

    private void UpdateMessage() => Message = Operation.ToString().ToUpperInvariant();

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

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        GHUtility.SetMenuList(this, menu, "Change operation", () => Operation, o => Operation = o);
    }

    public override bool Write(GH_IWriter writer)
    {
        writer.Set("Operation", Operation);
        return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
        Operation = reader.GetOrDefault("Operation", BooleanOp.Or);
        return base.Read(reader);
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

        try
        {
            var result = BBCavalier.Boolean(Operation, curvesA, curvesB, plane, tolerance);
            DA.SetDataList(0, result);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.icon_cavalier_boolean;

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid { get; } = new("{4F14E363-2287-4CE3-B974-C567FDC7B4F9}");
}
