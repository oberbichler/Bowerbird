using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Bowerbird.Crafting;

public class Section : CraftingBase
{
    protected Section(Mesh mesh, List<Plane> planes, double thickness, double deeper, double unit)
        : this(mesh, new MeshSlicer(mesh, unit), planes, thickness, deeper, unit)
    {
    }

    protected Section(Mesh mesh, MeshSlicer slicer, List<Plane> planes, double thickness, double deeper, double unit)
    {
        var slitPlanes = planes.AsParallel().AsOrdered().Select(p => new SlitPlane(slicer, p, unit)).ToArray();

        var bbox = mesh.GetBoundingBox(false);
        var dmax = bbox.Diagonal.Length;

        // 2. High-Performance, Numerically Stable Plane-Plane Intersections
        for (int i = 0; i < planes.Count; i++)
        {
            for (int j = i + 1; j < planes.Count; j++)
            {
                var a = planes[i];
                var b = planes[j];

                // Native C++ robust plane-plane intersection
                if (!Rhino.Geometry.Intersect.Intersection.PlanePlane(a, b, out var intersectionLine))
                    continue;

                var direction = intersectionLine.UnitTangent;
                var origin = intersectionLine.From;

                var cPlane = new Plane(bbox.Center, direction);
                origin = cPlane.ClosestPoint(origin);

                var pointAtStart = origin - dmax * direction;
                var pointAtEnd = origin + dmax * direction;
                var alpha = Vector3d.VectorAngle(a.Normal, b.Normal);
                var t = thickness * Math.Tan(alpha / 2.0);

                slitPlanes[i].AddSlit(pointAtStart, pointAtEnd, t, deeper);
                slitPlanes[j].AddSlit(pointAtEnd, pointAtStart, t, deeper);
            }
        }

        Curves = slitPlanes.AsParallel().AsOrdered().Select(o => o.GetResult()).ToArray();
    }

    public static Section Create(Mesh mesh, List<Plane> planes, double thickness, double deeper, double unit)
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

        return new Section(mesh, slicer, planes, thickness, deeper, unit);
    }

    public List<Curve>[] Curves { get; }
}
