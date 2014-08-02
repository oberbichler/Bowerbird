using ClipperLib;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bowerbird
{
    static class Util
    {
        public const string InfoString = "\n\nBowerbird\nby Thomas J. Oberbichler";


        public static Box Box(this Mesh mesh, Plane plane)
        {
            var box = default(Box);
            mesh.GetBoundingBox(plane, out box);
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


        public static List<List<IntPoint>> Section(this Mesh mesh, Plane plane, double tolerance, List<Curve> curves = null)
        {
            var sections = Intersection.MeshPlane(mesh, plane);

            if (sections == null)
                return new List<List<IntPoint>>();

            var joined = Curve.JoinCurves(sections.Select(o => new PolylineCurve(o)));

            if (curves != null)
                curves.AddRange(joined);

            return sections.ToPolygons(plane, tolerance).ToList();
        }


        public static List<List<IntPoint>> ToPolygons(this IEnumerable<Curve> curves, ref Plane? plane, double unit)
        {
            var polygons = new List<List<IntPoint>>();

            foreach (var curve in curves)
            {
                var curvePlane = default(Plane);

                if (!curve.TryGetPlane(out curvePlane))
                    continue;

                if (!plane.HasValue)
                    plane = curvePlane;

                var polygon = curve.ToPolygon(plane.Value, unit);

                if (polygon == null)
                    continue;

                polygons.Add(polygon);
            }

            return polygons;
        }

        public static List<List<IntPoint>> ToPolygons(this IEnumerable<Curve> curves, Plane plane, double unit)
        {
            var polygons = new List<List<IntPoint>>();

            foreach (var curve in curves)
            {
                var polygon = curve.ToPolygon(plane, unit);

                if (polygon == null)
                    continue;

                polygons.Add(polygon);
            }

            return polygons;
        }

        public static List<List<IntPoint>> ToPolygons(this IEnumerable<Polyline> polylines, Plane plane, double unit)
        {
            var polygons = new List<List<IntPoint>>();

            foreach (var polyline in polylines)
            {
                var polygon = polyline.ToPolygon(plane, unit);

                if (polygon == null)
                    continue;

                polygons.Add(polygon);
            }

            return polygons;
        }

        public static List<IntPoint> ToPolygon(this Curve curve, Plane plane, double unit)
        {
            var polyline = default(Polyline);

            if (!curve.TryGetPolyline(out polyline))
                return null;

            return polyline.ToPolygon(plane, unit);
        }

        public static List<IntPoint> ToPolygon(this IEnumerable<Point3d> polyline, Plane plane, double unit)
        {
            return polyline.Select(o => o - plane.Origin)
                           .Select(o => new IntPoint((o * plane.XAxis) / unit, o * (plane.YAxis) / unit))
                           .ToList();
        }


        public static Curve ToCurve(this List<IntPoint> polygon, Plane plane, double unit)
        {
            var polyline = polygon.ToPolyline(plane, unit);

            return new PolylineCurve(polyline);
        }

        public static Polyline ToPolyline(this List<IntPoint> polygon, Plane plane, double unit)
        {
            var points = polygon.Select(o => plane.Origin + o.X * unit * plane.XAxis + o.Y * unit * plane.YAxis)
                                .ToList();

            if (points.Count > 0 && points.First() != points.Last())
                points.Add(points[0]);

            return new Polyline(points);
        }

        public static List<Curve> ToCurves(this List<List<IntPoint>> polygons, Plane plane, double unit)
        {
            return polygons.Select(o => o.ToCurve(plane, unit)).ToList();
        }
        

        public static void SetEnum2D<T>(this Grasshopper.Kernel.IGH_DataAccess DA, int index, IEnumerable<IEnumerable<T>> data)
        {
            var tree = new DataTree<T>();

            var basePath = DA.ParameterTargetPath(index);

            foreach (var entry in data.Select((o, i) => new { Index = i, Item = o }))
            {
                var path = basePath.AppendElement(entry.Index);

                tree.AddRange(entry.Item, path);
            }

            DA.SetDataTree(index, tree);
        }

        public static void SetEnum1D<T>(this Grasshopper.Kernel.IGH_DataAccess DA, int index, IEnumerable<T> data)
        {
            var tree = new DataTree<T>();

            var basePath = DA.ParameterTargetPath(index);

            foreach (var entry in data.Select((o, i) => new { Index = i, Item = o }))
            {
                var path = basePath.AppendElement(entry.Index);

                tree.Add(entry.Item, path);
            }

            DA.SetDataTree(index, tree);
        }
    }
}
