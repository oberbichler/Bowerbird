using Bowerbird.Crafting;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CraftingComponents;

public class BBRadialComponent : GH_Component
{
    public BBRadialComponent() 
        : base("BB Radial", "BBRadial", "Create a radial waffle structure from a mesh" + Util.InfoString, "Bowerbird", "Crafting") 
    { 
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Mesh of the model", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Count A", "A", "Slices around z axis", GH_ParamAccess.item, 3);
        pManager.AddIntegerParameter("Count Z", "Z", "Slices in z direction", GH_ParamAccess.item, 3);
        pManager.AddNumberParameter("Thickness", "T", "Thickness of the parts", GH_ParamAccess.item, 1.0);
        pManager.AddPlaneParameter("Plane", "P", "Base Plane", GH_ParamAccess.item, Plane.WorldXY);
        pManager.AddNumberParameter("Radius", "R", "Inner radius", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Deeper", "D", "Makes the slits deeper", GH_ParamAccess.item, 0.0);
        pManager.AddBooleanParameter("Project", "p", "Project slices to xy plane", GH_ParamAccess.item, false);
        pManager.AddNumberParameter("Distance", "d", "Minimal distance between projected slices", GH_ParamAccess.item, 1.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Slices A", "A", "", GH_ParamAccess.tree);
        pManager.AddCurveParameter("Slices Z", "Z", "", GH_ParamAccess.tree);
        pManager.AddPlaneParameter("Section planes A", "PA", "", GH_ParamAccess.tree);
        pManager.AddPlaneParameter("Section planes Z", "PZ", "", GH_ParamAccess.tree);

        pManager[2].Locked = true;
        pManager[3].Locked = true;
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var mesh = default(Mesh);
        var countA = default(int);
        var countZ = default(int);
        var thickness = default(double);
        var plane = default(Plane);
        var radius = default(double);
        var deeper = default(double);
        var project = default(bool);
        var projectDistance = default(double);

        if (!DA.GetData(0, ref mesh)) return;
        if (!DA.GetData(1, ref countA)) return;
        if (!DA.GetData(2, ref countZ)) return;
        if (!DA.GetData(3, ref thickness)) return;
        if (!DA.GetData(4, ref plane)) return;
        if (!DA.GetData(5, ref radius)) return;
        if (!DA.GetData(6, ref deeper)) return;
        if (!DA.GetData(7, ref project)) return;
        if (!DA.GetData(8, ref projectDistance)) return;

        if (mesh == null) return;

        var unit = DocumentTolerance();

        var result = Radial.Create(mesh, plane, thickness, deeper, radius, countA, countZ, unit, project, projectDistance);

        DA.SetEnum2D(0, result.CurvesA);
        DA.SetEnum2D(1, result.CurvesZ);
        DA.SetEnum1D(2, result.PlanesA);
        DA.SetEnum1D(3, result.PlanesZ);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_radial;

    public override Guid ComponentGuid => new Guid("{08B4F9CF-7EA1-4A47-BC4E-FD761FA327E0}");
}
