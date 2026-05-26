using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Bowerbird.Crafting;

public class Section : CraftingBase
{
    protected Section(Mesh mesh, List<Plane> planes, double thickness, double deeper, double unit)
    {
        // 1. Ultra-Performance Parallelized Slicing for All Section Planes
        var slitPlanes = planes.AsParallel().AsOrdered().Select(p => new SlitPlane(mesh, p, unit)).ToArray();

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

        Curves = slitPlanes.Select(o => o.GetResult()).ToArray();
    }

    public static Section Create(Mesh mesh, List<Plane> planes, double thickness, double deeper, double unit)
    {
        if (thickness <= 0)
            throw new ArgumentException(@"Thickness must be a positive value!");

        if (deeper < 0)
            throw new ArgumentException(@"Border must be a positive value!");

        if (!mesh.IsValid)
            throw new ArgumentException(@"Mesh is not valid!");

        if (!mesh.IsClosed)
            throw new ArgumentException(@"Mesh is not closed!");

        return new Section(mesh, planes, thickness, deeper, unit);
    }

    public List<Curve>[] Curves { get; }
}
