using Clipper2Lib;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Generic;
using System.Linq;

namespace Bowerbird.Crafting;

public static class Util
{
    public const string InfoString = "\n\nBowerbird\nby Thomas J. Oberbichler";

    public static Box Box(this Mesh mesh, Plane plane)
    {
        mesh.GetBoundingBox(plane, out var box);
        return box;
    }

    public static Point2d Map2D(this Point3d point, Plane plane)
    {
        var p = point - plane.Origin;
        var x = p * plane.XAxis;
        var y = p * plane.YAxis;
        return new Point2d(x, y);
    }

    public static Point2d Map2D(this Vector3d vector, Plane plane)
    {
        var p = vector - (Vector3d)plane.Origin;
        var x = p * plane.XAxis;
        var y = p * plane.YAxis;
        return new Point2d(x, y);
    }

    public static Paths64 Section(this Mesh mesh, Plane plane, double tolerance, List<Curve>? curves = null)
    {
        var sections = Intersection.MeshPlane(mesh, plane);
        if (sections == null || sections.Length == 0)
            return new Paths64();

        if (curves != null)
        {
            var joined = Curve.JoinCurves(sections.Select(o => new PolylineCurve(o)));
            curves.AddRange(joined);
        }

        var paths = new Paths64(sections.Length);
        for (int i = 0; i < sections.Length; i++)
        {
            var poly = sections[i];
            var path = poly.ToPolygon(plane, tolerance);
            if (path != null && path.Count > 0)
            {
                paths.Add(path);
            }
        }
        return paths;
    }

    public static Paths64 ToPolygons(this IEnumerable<Curve> curves, ref Plane? plane, double unit)
    {
        var polygons = new Paths64();
        foreach (var curve in curves)
        {
            if (!curve.TryGetPlane(out var curvePlane))
                continue;

            if (!plane.HasValue)
                plane = curvePlane;

            var polygon = curve.ToPolygon(plane.Value, unit);
            if (polygon == null || polygon.Count == 0)
                continue;

            polygons.Add(polygon);
        }
        return polygons;
    }

    public static Paths64 ToPolygons(this IEnumerable<Curve> curves, Plane plane, double unit)
    {
        var list = curves as IList<Curve> ?? curves.ToList();
        var polygons = new Paths64(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var poly = list[i].ToPolygon(plane, unit);
            if (poly != null && poly.Count > 0)
                polygons.Add(poly);
        }
        return polygons;
    }

    public static Path64? ToPolygon(this Curve curve, Plane plane, double unit)
    {
        if (!curve.TryGetPolyline(out var polyline))
            return null;

        return polyline.ToPolygon(plane, unit);
    }

    public static Path64 ToPolygon(this IEnumerable<Point3d> polyline, Plane plane, double unit)
    {
        var list = polyline as IList<Point3d> ?? polyline.ToList();
        var path = new Path64(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var o = list[i] - plane.Origin;
            var x = (o * plane.XAxis) / unit;
            var y = (o * plane.YAxis) / unit;
            path.Add(new Point64(x, y));
        }
        return path;
    }

    public static Curve ToCurve(this Path64 polygon, Plane plane, double unit)
    {
        var polyline = polygon.ToPolyline(plane, unit);
        return new PolylineCurve(polyline);
    }

    public static Polyline ToPolyline(this Path64 polygon, Plane plane, double unit)
    {
        var points = new List<Point3d>(polygon.Count + 1);
        for (int i = 0; i < polygon.Count; i++)
        {
            var pt = polygon[i];
            points.Add(plane.Origin + pt.X * unit * plane.XAxis + pt.Y * unit * plane.YAxis);
        }

        if (points.Count > 0 && points[0] != points[points.Count - 1])
            points.Add(points[0]);

        return new Polyline(points);
    }

    public static List<Curve> ToCurves(this Paths64 polygons, Plane plane, double unit)
    {
        var curves = new List<Curve>(polygons.Count);
        for (int i = 0; i < polygons.Count; i++)
        {
            curves.Add(polygons[i].ToCurve(plane, unit));
        }
        return curves;
    }

    public static Vector3d ToVector3d(this Vector2d self) => new Vector3d(self.X, self.Y, 0);
}
