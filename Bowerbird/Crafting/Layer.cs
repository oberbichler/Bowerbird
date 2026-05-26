using Clipper2Lib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bowerbird.Crafting;

public class Layer : CraftingBase
{
    protected Layer(Mesh mesh, Plane plane, double thickness, double border, double unit)
    {
        var box = mesh.Box(plane);
        var corners = box.GetCorners();

        var origin = corners[0];
        var unitZ = box.Plane.ZAxis;

        var layerCount = (int)Math.Round(box.Z.Length / thickness);
        if (layerCount < 1)
            layerCount = 1;

        var olines = new Paths64[layerCount];
        var offset = new Paths64[layerCount];
        var ilines = new Paths64[layerCount];
        var planes = new Plane[layerCount];

        // 1. Parallelized Mesh Slicing & Offsetting
        Parallel.For(0, layerCount, i =>
        {
            var z = (i + 0.5) * thickness;
            var sectionOrigin = origin + z * unitZ;
            var sectionPlane = new Plane(sectionOrigin, unitZ);
            planes[i] = sectionPlane;

            var ol = mesh.Section(sectionPlane, unit);
            olines[i] = ol;

            if (border > 0)
            {
                offset[i] = Clipper.InflatePaths(ol, -border / unit, JoinType.Square, EndType.Polygon);
            }
        });

        // 2. Parallelized Inline Intersections
        if (border > 0)
        {
            ilines[0] = new Paths64();

            if (layerCount > 2)
            {
                Parallel.For(1, layerCount - 1, i =>
                {
                    var inter1 = Clipper.Intersect(offset[i], offset[i - 1], FillRule.NonZero);
                    ilines[i] = Clipper.Intersect(inter1, offset[i + 1], FillRule.NonZero);
                });
            }

            if (layerCount > 1)
            {
                ilines[layerCount - 1] = new Paths64();
            }
        }

        CurvesO = new List<Curve>[layerCount];
        CurvesI = new List<Curve>[layerCount];
        Planes = planes;

        // 3. Parallelized Curves Reconstruction
        Parallel.For(0, layerCount, i =>
        {
            CurvesO[i] = olines[i].ToCurves(planes[i], unit);
            if (border > 0 && ilines[i] != null)
            {
                CurvesI[i] = ilines[i].ToCurves(planes[i], unit);
            }
            else
            {
                CurvesI[i] = new List<Curve>();
            }
        });
    }

    public static Layer Create(Mesh mesh, Plane plane, double thickness, double border, double unit)
    {
        if (thickness <= 0)
            throw new ArgumentException(@"Thickness must be a positive value!");

        if (border < 0)
            throw new ArgumentException(@"Border must be a positive value!");

        if (!mesh.IsValid)
            throw new ArgumentException(@"Mesh is not valid!");

        if (!mesh.IsClosed)
            throw new ArgumentException(@"Mesh is not closed!");

        return new Layer(mesh, plane, thickness, border, unit);
    }

    public List<Curve>[] CurvesO { get; }

    public List<Curve>[] CurvesI { get; }

    public Plane[] Planes { get; }
}
