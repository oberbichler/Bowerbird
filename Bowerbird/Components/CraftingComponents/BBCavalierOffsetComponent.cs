using Bowerbird.Crafting;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Bowerbird.Components.CraftingComponents;

public class BBCavalierOffsetComponent : GH_Component
{
    public BBCavalierOffsetComponent()
        : base("BB Cavalier Contours Offset", "BBCavalierOffset", "Offset lines and arcs, keeping arcs as arcs, using Cavalier Contours. Closed curves are offset together with their nesting detected automatically, so an inner loop acts as a hole; open curves are offset individually" + Util.InfoString, "Bowerbird", "Polyline")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "C", "Curves to offset", GH_ParamAccess.list);
        pManager.AddNumberParameter("Distance", "D", "Offset distance, positive grows the filled area outward and shrinks holes", GH_ParamAccess.item);
        pManager.AddPlaneParameter("Plane", "P", "Projection plane, taken from the first planar curve when left empty and WorldXY if none is planar", GH_ParamAccess.item);
        pManager.AddNumberParameter("Tolerance", "T", "Simplification/flattening tolerance", GH_ParamAccess.item, 0.01);
        pManager.AddBooleanParameter("Handle Self-Intersections", "H", "Resolve self-intersections. Has no effect on closed curves once there is more than one of them, because they are offset as one shape whose intersections are always resolved across all loops", GH_ParamAccess.item, true);

        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "C", "Offset results", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var curves = new List<Curve>();
        var distance = default(double);
        var planeInput = default(Plane);
        var tolerance = default(double);
        var handleSelfIntersects = default(bool);

        if (!DA.GetDataList(0, curves)) return;
        if (!DA.GetData(1, ref distance)) return;

        Plane? plane = null;
        if (DA.GetData(2, ref planeInput))
        {
            plane = planeInput;
        }

        if (!DA.GetData(3, ref tolerance)) return;
        if (!DA.GetData(4, ref handleSelfIntersects)) return;

        // Several closed curves go through Cavalier's shape offset, which resolves intersections
        // globally across all loops and exposes no per-loop switch. Turning the flag off therefore
        // cannot be honoured there, and staying silent about it would misreport what was computed.
        if (!handleSelfIntersects && curves.Count(c => c is not null && c.IsClosed) > 1)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Warning,
                "Self-intersections are always resolved when more than one closed curve is offset together.");
        }

        try
        {
            var result = BBCavalier.Offset(curves, distance, plane, tolerance, handleSelfIntersects);
            DA.SetDataList(0, result);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.icon_cavalier_offset;

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid { get; } = new("{7D391A2E-2A4B-4D1D-9B8A-EC9D5D38A201}");
}
