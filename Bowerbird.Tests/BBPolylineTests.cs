using Clipper2Lib;
using Xunit;

namespace Bowerbird.Tests;

public class BBPolylineTests
{
    private static PathsD OffsetPathsD(
        PathsD closedPaths,
        PathsD openPaths,
        double distance,
        JoinType joinType,
        EndType endType,
        double miterLimit,
        double arcTolerance,
        int precision = 3)
    {
        var scale = Math.Pow(10, precision);
        var offset = new ClipperOffset(miterLimit, arcTolerance * scale);

        var effectiveClosedEndType = (endType == EndType.Joined) ? EndType.Joined : EndType.Polygon;
        var effectiveOpenEndType = (endType == EndType.Polygon || endType == EndType.Joined) ? EndType.Round : endType;

        if (closedPaths != null && closedPaths.Count > 0)
        {
            var scaledClosed = Clipper.ScalePaths64(closedPaths, scale);
            offset.AddPaths(scaledClosed, joinType, effectiveClosedEndType);
        }

        if (openPaths != null && openPaths.Count > 0)
        {
            var scaledOpen = Clipper.ScalePaths64(openPaths, scale);
            offset.AddPaths(scaledOpen, joinType, effectiveOpenEndType);
        }

        var solution64 = new Paths64();
        offset.Execute(distance * scale, solution64);

        return Clipper.ScalePathsD(solution64, 1.0 / scale);
    }

    [Fact]
    public void TestClosedRectangle_OutwardOffset_WithDefaultEndTypeRound()
    {
        var closed = new PathsD
        {
            new PathD
            {
                new PointD(0, 0),
                new PointD(10, 0),
                new PointD(10, 10),
                new PointD(0, 10)
            }
        };

        // GH component default is EndType.Round
        var solution = OffsetPathsD(closed, new PathsD(), 1.0, JoinType.Miter, EndType.Round, 2.0, 0.25);

        Assert.Single(solution);
        // 12x12 square with sharp miter corners has area = 144
        Assert.Equal(144.0, Math.Abs(Clipper.Area(solution[0])), 0.1);
    }

    [Fact]
    public void TestClosedRectangle_InwardOffset_WithDefaultEndTypeRound()
    {
        var closed = new PathsD
        {
            new PathD
            {
                new PointD(0, 0),
                new PointD(10, 0),
                new PointD(10, 10),
                new PointD(0, 10)
            }
        };

        // Inward offset of -1.0 on 10x10 square
        var solution = OffsetPathsD(closed, new PathsD(), -1.0, JoinType.Miter, EndType.Round, 2.0, 0.25);

        Assert.Single(solution);
        // 8x8 square has area = 64
        Assert.Equal(64.0, Math.Abs(Clipper.Area(solution[0])), 0.1);
    }

    [Fact]
    public void TestOpenLine_Offset_WithEndTypeRound()
    {
        var open = new PathsD
        {
            new PathD
            {
                new PointD(0, 0),
                new PointD(10, 0)
            }
        };

        var solution = OffsetPathsD(new PathsD(), open, 1.0, JoinType.Round, EndType.Round, 2.0, 0.001);

        Assert.Single(solution);
        // Line length 10, radius 1 -> area is rectangle (10 * 2) + circle (pi * 1^2) ≈ 20 + 3.1415 = 23.14
        Assert.Equal(20.0 + Math.PI, Math.Abs(Clipper.Area(solution[0])), 0.1);
    }

    [Fact]
    public void TestMixed_ClosedAndOpenCurves_InSinglePass()
    {
        var closed = new PathsD
        {
            new PathD
            {
                new PointD(0, 0),
                new PointD(10, 0),
                new PointD(10, 10),
                new PointD(0, 10)
            }
        };
        var open = new PathsD
        {
            new PathD
            {
                new PointD(20, 0),
                new PointD(30, 0)
            }
        };

        var solution = OffsetPathsD(closed, open, 1.0, JoinType.Miter, EndType.Round, 2.0, 0.25);

        // Produces both the offset polygon and the inflated line
        Assert.Equal(2, solution.Count);
    }

    [Fact]
    public void TestOpenLine_Offset_WithEndTypeButt()
    {
        var open = new PathsD
        {
            new PathD
            {
                new PointD(0, 0),
                new PointD(10, 0)
            }
        };

        var solution = OffsetPathsD(new PathsD(), open, 1.0, JoinType.Square, EndType.Butt, 2.0, 0.25);

        Assert.Single(solution);
        // Line length 10, width 2 -> rectangle 10x2 = 20
        Assert.Equal(20.0, Math.Abs(Clipper.Area(solution[0])), 0.1);
    }

    [Fact]
    public void TestOpenLine_Offset_WithEndTypeSquare()
    {
        var open = new PathsD
        {
            new PathD
            {
                new PointD(0, 0),
                new PointD(10, 0)
            }
        };

        var solution = OffsetPathsD(new PathsD(), open, 1.0, JoinType.Square, EndType.Square, 2.0, 0.25);

        Assert.Single(solution);
        // Line length 10 + 2 (1 on each end), width 2 -> rectangle 12x2 = 24
        Assert.Equal(24.0, Math.Abs(Clipper.Area(solution[0])), 0.1);
    }
}
