using System;
using Xunit;
using CavalierContours;
using CavalierContours.Polyline;
using CavalierContours.Shape;
using Bowerbird.Crafting;

namespace Bowerbird.Tests;

public class CavalierReflectionTest
{
    [Fact]
    public void TestOpenPolylineOffset()
    {
        var polyline = new Polyline<double>();
        polyline.SetIsClosed(false);

        // Add 2 vertices representing an open line segment from (0,0) to (10,0)
        polyline.AddVertex(new PlineVertex<double>(0.0, 0.0, 0.0));
        polyline.AddVertex(new PlineVertex<double>(10.0, 0.0, 0.0));

        var options = new PlineOffsetOptions<double>
        {
            HandleSelfIntersects = true,
            PosEqualEps = 1e-5,
            SliceJoinEps = 1e-4,
            OffsetDistEps = 1e-4
        };

        // Offset with positive distance (2.0)
        var offsetPlinesPos = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, 2.0, options);

        // Offset with negative distance (-2.0)
        var offsetPlinesNeg = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, -2.0, options);

        Assert.NotNull(offsetPlinesPos);
        Assert.Single(offsetPlinesPos);
        Assert.NotNull(offsetPlinesNeg);
        Assert.Single(offsetPlinesNeg);
    }

    [Fact]
    public void TestClosedCircleOffset()
    {
        // 2-vertex closed circle with radius 10 (bulge = 1.0 represents a semicircle)
        var polyline = new Polyline<double>();
        polyline.SetIsClosed(true);
        polyline.AddVertex(new PlineVertex<double>(-10.0, 0.0, 1.0));
        polyline.AddVertex(new PlineVertex<double>(10.0, 0.0, 1.0));

        var options = new PlineOffsetOptions<double>
        {
            HandleSelfIntersects = true
        };

        // Outward offset by 2.0 -> radius becomes 12.0
        var outward = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, 2.0, options);
        Assert.NotNull(outward);
        Assert.Single(outward);

        // Inward offset by -2.0 -> radius becomes 8.0
        var inward = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, -2.0, options);
        Assert.NotNull(inward);
        Assert.Single(inward);
    }

    [Fact]
    public void TestDonutShapeOffset()
    {
        // Outer square: [-10, 10] x [-10, 10] (CCW)
        var outer = new Polyline<double>();
        outer.SetIsClosed(true);
        outer.AddVertex(new PlineVertex<double>(-10.0, -10.0, 0.0));
        outer.AddVertex(new PlineVertex<double>(10.0, -10.0, 0.0));
        outer.AddVertex(new PlineVertex<double>(10.0, 10.0, 0.0));
        outer.AddVertex(new PlineVertex<double>(-10.0, 10.0, 0.0));

        // Inner square hole: [-4, 4] x [-4, 4] (CW or CCW - auto oriented)
        var inner = new Polyline<double>();
        inner.SetIsClosed(true);
        inner.AddVertex(new PlineVertex<double>(-4.0, -4.0, 0.0));
        inner.AddVertex(new PlineVertex<double>(-4.0, 4.0, 0.0));
        inner.AddVertex(new PlineVertex<double>(4.0, 4.0, 0.0));
        inner.AddVertex(new PlineVertex<double>(4.0, -4.0, 0.0));

        var shape = BBCavalier.CreateShape([outer, inner]);
        Assert.Single(shape.CcwPlines);
        Assert.Single(shape.CwPlines);

        var shapeOpts = new ShapeOffsetOptions<double>();
        // Offset by 1.0 (positive expands outer island, contracts inner hole)
        var offsetShape = shape.ParallelOffset(1.0, shapeOpts);
        Assert.NotEmpty(offsetShape.CcwPlines);
        Assert.NotEmpty(offsetShape.CwPlines);

        Polyline<double> ccwPline = offsetShape.CcwPlines[0].Polyline;
        Polyline<double> cwPline = offsetShape.CwPlines[0].Polyline;
        Assert.NotNull(ccwPline);
        Assert.NotNull(cwPline);
    }

    [Fact]
    public void TestNestedIslandsAndHolesShapeOffset()
    {
        // Level 0: Outer square [-20, 20]
        var l0 = new Polyline<double>();
        l0.SetIsClosed(true);
        l0.AddVertex(new PlineVertex<double>(-20.0, -20.0, 0.0));
        l0.AddVertex(new PlineVertex<double>(20.0, -20.0, 0.0));
        l0.AddVertex(new PlineVertex<double>(20.0, 20.0, 0.0));
        l0.AddVertex(new PlineVertex<double>(-20.0, 20.0, 0.0));

        // Level 1: Hole [-14, 14]
        var l1 = new Polyline<double>();
        l1.SetIsClosed(true);
        l1.AddVertex(new PlineVertex<double>(-14.0, -14.0, 0.0));
        l1.AddVertex(new PlineVertex<double>(14.0, -14.0, 0.0));
        l1.AddVertex(new PlineVertex<double>(14.0, 14.0, 0.0));
        l1.AddVertex(new PlineVertex<double>(-14.0, 14.0, 0.0));

        // Level 2: Nested Island [-6, 6]
        var l2 = new Polyline<double>();
        l2.SetIsClosed(true);
        l2.AddVertex(new PlineVertex<double>(-6.0, -6.0, 0.0));
        l2.AddVertex(new PlineVertex<double>(6.0, -6.0, 0.0));
        l2.AddVertex(new PlineVertex<double>(6.0, 6.0, 0.0));
        l2.AddVertex(new PlineVertex<double>(-6.0, 6.0, 0.0));

        // Level 3: Nested Hole [-2, 2]
        var l3 = new Polyline<double>();
        l3.SetIsClosed(true);
        l3.AddVertex(new PlineVertex<double>(-2.0, -2.0, 0.0));
        l3.AddVertex(new PlineVertex<double>(2.0, -2.0, 0.0));
        l3.AddVertex(new PlineVertex<double>(2.0, 2.0, 0.0));
        l3.AddVertex(new PlineVertex<double>(-2.0, 2.0, 0.0));

        var shape = BBCavalier.CreateShape([l0, l1, l2, l3]);
        Assert.Equal(2, shape.CcwPlines.Count); // Level 0 and Level 2
        Assert.Equal(2, shape.CwPlines.Count);  // Level 1 and Level 3

        var shapeOpts = new ShapeOffsetOptions<double>();
        var offsetShape = shape.ParallelOffset(1.0, shapeOpts);
        Assert.NotEmpty(offsetShape.CcwPlines);
        Assert.NotEmpty(offsetShape.CwPlines);
    }
}
