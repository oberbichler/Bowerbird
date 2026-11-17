using System.Linq;
using Bowerbird.Clipper2Lib;
using Bowerbird.Crafting;
using Rhino.Geometry;
using Xunit;

namespace Bowerbird.Tests;

/// <summary>
/// Section points that lie closer than one unit collapse onto the same grid point when converted to
/// a Path64. The conversions must not keep the duplicates: zero-length edges are reported as
/// self-intersections by Rhino and make planar breps ignore their holes.
/// </summary>
public class PolygonConversionTests
{
    private const double Unit = 0.01;

    [Fact]
    public void ToPolygon_DropsPointsThatCollapseOntoTheSameGridPoint()
    {
        // A contour passing a mesh vertex 0.001 below the cutting plane: four crossings within 0.004.
        var points = new[]
        {
            new Point3d(-1.0, -0.3, 0),
            new Point3d(-0.001, -0.001, 0),
            new Point3d(0.000, -0.001, 0),
            new Point3d(0.001, -0.001, 0),
            new Point3d(0.004, 0.000, 0),
            new Point3d(1.0, 0.3, 0),
            new Point3d(0.0, 2.0, 0),
            new Point3d(-1.0, -0.3, 0),
        };

        var path = points.ToPolygon(Plane.WorldXY, Unit);

        for (int i = 0; i < path.Count; i++)
            Assert.NotEqual(path[i], path[(i + 1) % path.Count]);

        Assert.Equal(4, path.Count);
    }

    [Fact]
    public void ToPolygon_DropsTheClosingPoint()
    {
        var points = new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(1, 1, 0), new Point3d(0, 0, 0) };

        var path = points.ToPolygon(Plane.WorldXY, Unit);

        Assert.Equal(3, path.Count);
    }

    [Fact]
    public void ToPolyline_SkipsRepeatedPointsAndCloses()
    {
        var path = new Path64
        {
            new Point64(0, 0), new Point64(100, 0), new Point64(100, 0), new Point64(100, 100), new Point64(100, 100),
        };

        var polyline = path.ToPolyline(Plane.WorldXY, Unit);

        Assert.Equal(4, polyline.Count);
        Assert.Equal(polyline[0], polyline[polyline.Count - 1]);
        Assert.True(Enumerable.Range(1, polyline.Count - 1).All(i => polyline[i] != polyline[i - 1]));
    }
}
