using Bowerbird.Crafting;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CraftingComponents;

public class BBWaffleComponent : GH_Component
{
    public BBWaffleComponent()
        : base("BB Waffle", "BBWaffle", "Create a waffle structure from a mesh" + Util.InfoString, "Bowerbird", "Crafting")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Mesh of the model", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Count X", "X", "Slices in x direction", GH_ParamAccess.item, 2);
        pManager.AddIntegerParameter("Count Y", "Y", "Slices in y direction", GH_ParamAccess.item, 2);
        pManager.AddNumberParameter("Thickness", "T", "Thickness of the parts", GH_ParamAccess.item);
        pManager.AddPlaneParameter("Plane", "P", "Base Plane", GH_ParamAccess.item, Plane.WorldXY);
        pManager.AddNumberParameter("Deeper", "D", "Makes the slits deeper", GH_ParamAccess.item, 0);
        pManager.AddBooleanParameter("Project", "p", "Project slices to xy plane", GH_ParamAccess.item, false);
        pManager.AddNumberParameter("Distance", "d", "Minimal distance between projected slices", GH_ParamAccess.item, 1.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Slices U", "U", "", GH_ParamAccess.tree);
        pManager.AddCurveParameter("Slices V", "V", "", GH_ParamAccess.tree);
        pManager.AddPlaneParameter("Section planes U", "PU", "", GH_ParamAccess.tree);
        pManager.AddPlaneParameter("Section planes V", "PV", "", GH_ParamAccess.tree);

        pManager[2].Locked = true;
        pManager[3].Locked = true;
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var mesh = default(Mesh);
        var countX = default(int);
        var countY = default(int);
        var thickness = default(double);
        var plane = default(Plane);
        var deeper = default(double);
        var project = default(bool);
        var projectDistance = default(double);

        if (!DA.GetData(0, ref mesh)) return;
        if (!DA.GetData(1, ref countX)) return;
        if (!DA.GetData(2, ref countY)) return;
        if (!DA.GetData(3, ref thickness)) return;
        if (!DA.GetData(4, ref plane)) return;
        if (!DA.GetData(5, ref deeper)) return;
        if (!DA.GetData(6, ref project)) return;
        if (!DA.GetData(7, ref projectDistance)) return;

        if (mesh == null) return;

        var unit = DocumentTolerance();

        var result = Waffle.Create(mesh, plane, thickness, deeper, countX, countY, unit, project, projectDistance);

        DA.SetEnum2D(0, result.CurvesX);
        DA.SetEnum2D(1, result.CurvesY);
        DA.SetEnum1D(2, result.PlanesX);
        DA.SetEnum1D(3, result.PlanesY);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_waffle;

    public override Guid ComponentGuid { get; } = new ("{FE7B90FF-E3A6-4AE7-8097-C81D34257756}");
}
