using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Bowerbird.Crafting;

public class Waffle : CraftingBase
{
    protected Waffle(Mesh mesh, Plane plane, double thickness, double deeper, int countX, int countY, double unit, bool project, double projectSpace)
    {
        var box = mesh.Box(plane);

        var unitX = box.Plane.XAxis;
        var unitY = box.Plane.YAxis;
        var unitZ = box.Plane.ZAxis;

        var origin = box.PointAt(0.0, 0.0, 0.0);

        var lenX = Math.Abs(box.X.Length);
        var lenY = Math.Abs(box.Y.Length);
        var lenZ = Math.Abs(box.Z.Length);

        var valX = Enumerable.Range(1, countX).Select(o => o * lenX / (countX + 1)).ToArray();
        var vecX = valX.Select(o => o * unitX).ToArray();
        var plnX = vecX.Select(o => new Plane(origin + o, unitY, unitZ)).ToArray();
        
        // Ultra-Performance Parallelized Slicing for X-Planes
        var sltX = plnX.AsParallel().AsOrdered().Select(o => new SlitPlane(mesh, o, unit)).ToArray();

        var valY = Enumerable.Range(1, countY).Select(o => o * lenY / (countY + 1)).ToArray();
        var vecY = valY.Select(o => o * unitY).ToArray();
        var plnY = vecY.Select(o => new Plane(origin + o, unitX, unitZ)).ToArray();

        // Ultra-Performance Parallelized Slicing for Y-Planes
        var sltY = plnY.AsParallel().AsOrdered().Select(o => new SlitPlane(mesh, o, unit)).ToArray();

        for (var i = 0; i < sltX.Length; i++)
        {
            for (var j = 0; j < sltY.Length; j++)
            {
                var p0 = origin + vecX[i] + vecY[j];
                var p1 = p0 + lenZ * unitZ;

                sltX[i].AddSlit(p0, p1, thickness, deeper);
                sltY[j].AddSlit(p1, p0, thickness, deeper);
            }
        }

        CurvesX = sltX.Select(o => o.GetResult()).ToArray();
        CurvesY = sltY.Select(o => o.GetResult()).ToArray();
        PlanesX = plnX;
        PlanesY = plnY;

        if (!project)
            return;

        for (var i = 0; i < CurvesX.Length; i++)
        {
            Project(CurvesX[i], PlanesX[i], i * (lenY + projectSpace), 0.0);
        }

        for (var i = 0; i < CurvesY.Length; i++)
        {
            Project(CurvesY[i], PlanesY[i], i * (lenX + projectSpace), lenZ + projectSpace);
        }
    }

    public static Waffle Create(Mesh mesh, Plane plane, double thickness, double deeper, int countA, int countZ, double unit, bool project = false, double projectSpace = 0.0)
    {
        if (thickness <= 0)
            throw new ArgumentException(@"Thickness must be a positive value!");

        if (deeper < 0)
            throw new ArgumentException(@"Border must be a positive value!");

        if (!mesh.IsValid)
            throw new ArgumentException(@"Mesh is not valid!");

        if (!mesh.IsClosed)
            throw new ArgumentException(@"Mesh is not closed!");

        return new Waffle(mesh, plane, thickness, deeper, countA, countZ, unit, project, projectSpace);
    }

    public List<Curve>[] CurvesX { get; private set; }

    public List<Curve>[] CurvesY { get; private set; }

    public Plane[] PlanesX { get; private set; }

    public Plane[] PlanesY { get; private set; }
}
