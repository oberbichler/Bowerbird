using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace Bowerbird.Crafting;

public class Radial : CraftingBase
{
    protected Radial(Mesh mesh, Plane plane, double thickness, double deeper, double radius, int countA, int countZ, double unit, bool project, double projectSpace)
        : this(mesh, new MeshSlicer(mesh, unit), plane, thickness, deeper, radius, countA, countZ, unit, project, projectSpace)
    {
    }

    protected Radial(Mesh mesh, MeshSlicer slicer, Plane plane, double thickness, double deeper, double radius, int countA, int countZ, double unit, bool project, double projectSpace)
    {
        var box = mesh.Box(plane);

        var unitX = box.Plane.XAxis;
        var unitY = box.Plane.YAxis;
        var unitZ = box.Plane.ZAxis;
        var center = box.Plane.Origin + box.Z.Min * unitZ;

        var corners = box.GetCorners();

        var r1 = Math.Max(radius, thickness / 2.0 / Math.Tan(Math.PI / countA));
        var r2 = Enumerable.Range(0, 4).Select(i => corners[i].DistanceTo(center)).Max();

        var lenX = Math.Abs(box.X.Length);
        var lenZ = Math.Abs(box.Z.Length);
        var lenR = r2 - r1;

        var valA = Enumerable.Range(0, countA).Select(o => Math.PI * 2.0 / countA * o).ToArray();
        var rayA = valA.Select(o => unitX * Math.Cos(o) + unitY * Math.Sin(o)).ToArray();
        var plnA = rayA.Select(o => new Plane(center + o * r1, o, unitZ)).ToArray();

        var sltA = plnA.AsParallel().AsOrdered().Select(o => new SlitPlane(slicer, o, unit)).ToArray();

        var valZ = Enumerable.Range(1, countZ).Select(o => o * lenZ / (countZ + 1)).ToArray();
        var pntZ = valZ.Select(o => corners[0] + o * unitZ).ToArray();
        var plnZ = pntZ.Select(o => new Plane(o, unitX, unitY)).ToArray();

        var sltZ = plnZ.AsParallel().AsOrdered().Select(o => new SlitPlane(slicer, o, unit)).ToArray();

        // Each slit plane only receives its own slits, so the planes can be processed independently.
        // The order of clip and slits within a plane is the same as in a sequential i/j loop.
        Parallel.For(0, sltA.Length, i =>
        {
            sltA[i].Clip(-unit, lenR + unit, -unit, lenZ + unit);

            for (var j = 0; j < sltZ.Length; j++)
            {
                var c = center + valZ[j] * unitZ;
                sltA[i].AddSlit(c + rayA[i] * (r1 - unit), c + rayA[i] * r2, thickness, deeper);
            }
        });

        Parallel.For(0, sltZ.Length, j =>
        {
            var c = center + valZ[j] * unitZ;

            for (var i = 0; i < sltA.Length; i++)
            {
                sltZ[j].AddSlit(c + rayA[i] * r2, c + rayA[i] * (r1 - unit), thickness, deeper);
            }
        });

        CurvesA = sltA.AsParallel().AsOrdered().Select(o => o.GetResult()).ToArray();
        CurvesZ = sltZ.AsParallel().AsOrdered().Select(o => o.GetResult()).ToArray();
        PlanesA = plnA;
        PlanesZ = plnZ;

        if (!project)
            return;

        for (var i = 0; i < CurvesA.Length; i++)
        {
            Project(CurvesA[i], PlanesA[i], i * (lenR + projectSpace), 0);
        }

        for (var i = 0; i < CurvesZ.Length; i++)
        {
            Project(CurvesZ[i], PlanesZ[i], i * (lenX + projectSpace), lenZ + projectSpace);
        }
    }

    public static Radial Create(Mesh mesh, Plane plane, double thickness, double deeper, double radius, int countA, int countZ, double unit, bool project = false, double projectSpace = 0.0)
    {
        if (thickness <= 0)
            throw new ArgumentException(@"Thickness must be a positive value!");

        if (deeper < 0)
            throw new ArgumentException(@"Border must be a positive value!");

        if (!mesh.IsValid)
            throw new ArgumentException(@"Mesh is not valid!");

        // Slicing needs the welded mesh anyway; checking closedness on it avoids building the
        // mesh topology, which is one of the most expensive steps for large meshes.
        var slicer = new MeshSlicer(mesh, unit);

        if (!slicer.IsClosed)
            throw new ArgumentException(@"Mesh is not closed!");

        return new Radial(mesh, slicer, plane, thickness, deeper, radius, countA, countZ, unit, project, projectSpace);
    }

    public List<Curve>[] CurvesA { get; private set; }

    public List<Curve>[] CurvesZ { get; private set; }

    public Plane[] PlanesA { get; private set; }

    public Plane[] PlanesZ { get; private set; }
}
