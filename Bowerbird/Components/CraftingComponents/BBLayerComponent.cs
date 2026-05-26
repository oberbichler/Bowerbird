using Bowerbird.Crafting;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CraftingComponents;

public class BBLayerComponent : GH_Component
{
    public BBLayerComponent() 
        : base("BB Layer", "BBLayer", "Create a layer model from a mesh" + Util.InfoString, "Bowerbird", "Crafting") 
    { 
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Mesh of the model", GH_ParamAccess.item);
        pManager.AddNumberParameter("Thickness", "T", "Thickness of the parts", GH_ParamAccess.item);
        pManager.AddPlaneParameter("Plane", "P", "Base plane", GH_ParamAccess.item, Plane.WorldXY);
        pManager.AddNumberParameter("Border", "B", "Overlapping of the layers", GH_ParamAccess.item, 0.0);

        pManager[2].Optional = true;
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Outlines", "O", "Outer contours of the layers", GH_ParamAccess.tree);
        pManager.AddCurveParameter("Inlines", "I", "Inner contours of the layers", GH_ParamAccess.tree);
        pManager.AddPlaneParameter("Planes", "P", "Layer planes", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var mesh = default(Mesh);
        var thickness = default(double);
        var plane = default(Plane);
        var border = default(double);

        if (!DA.GetData(0, ref mesh)) return;
        if (!DA.GetData(1, ref thickness)) return;
        if (!DA.GetData(2, ref plane)) return;
        if (!DA.GetData(3, ref border)) return;

        if (mesh == null) return;

        var unit = DocumentTolerance();

        var result = Layer.Create(mesh, plane, thickness, border, unit);

        DA.SetEnum2D(0, result.CurvesO);
        DA.SetEnum2D(1, result.CurvesI);
        DA.SetEnum1D(2, result.Planes);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_layer;

    public override Guid ComponentGuid => new ("{40C0EE3D-E7F9-4B18-97D6-802B99A910D4}");
}
