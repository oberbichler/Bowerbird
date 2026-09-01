using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CavalierContours.Polyline;
using Bowerbird.Crafting;

namespace Bowerbird.Tests;

/// <summary>
/// Two curves taken from a real Grasshopper document, one of which carries a 217 degree arc.
/// Reference areas come from integrating the source NURBS curves on a 1600x1600 grid, so they are
/// independent of CavalierContours.
/// </summary>
public class BBCavalierRealGeometryTests
{
    private static Polyline<double> CurveA()
    {
        var p = new Polyline<double>();
        p.SetIsClosed(true);
        p.AddVertex(new PlineVertex<double>(-18.081998323944717, -76.723553836851096, 0));
        p.AddVertex(new PlineVertex<double>(-24.209682610269169, -38.331735961308112, 0));
        p.AddVertex(new PlineVertex<double>(-1.5747671852747516, -31.078558642801621, 0));
        p.AddVertex(new PlineVertex<double>(-3.9508080309924192, -47.210625437410897, 1.391202620198527));
        p.AddVertex(new PlineVertex<double>(8.5546701043636091, -44.959639373046805, -0.11647644536222089));
        p.AddVertex(new PlineVertex<double>(4.4278623196961178, -30.078120391973137, -1.0856171317967487));
        p.AddVertex(new PlineVertex<double>(21.685422146487454, -30.828449080094497, -0.15456950678939191));
        p.AddVertex(new PlineVertex<double>(2.3019310366856018, -72.846855614890728, -1.457659914495893));
        p.AddVertex(new PlineVertex<double>(-6.0767393140029355, -56.089514913513661, -0.91078921695615245));
        p.AddVertex(new PlineVertex<double>(-3.9508080309924196, -67.219390453980509, 0));
        p.AddVertex(new PlineVertex<double>(-0.82443849715339468, -72.471691270830064, 0));
        p.AddVertex(new PlineVertex<double>(6.0535744772924147, -60.466432260888269, 0));
        p.AddVertex(new PlineVertex<double>(-12.82969750709519, -48.586228032300049, 0));
        p.AddVertex(new PlineVertex<double>(-17.206614854469791, -58.465555759231307, 0));
        if (p.Orientation() == PlineOrientation.Clockwise) p.InvertDirection();
        return p;
    }

    private static Polyline<double> CurveB()
    {
        var p = new Polyline<double>();
        p.SetIsClosed(true);
        p.AddVertex(new PlineVertex<double>(20.595989191613775, -48.34680359224788, 0));
        p.AddVertex(new PlineVertex<double>(-16.893914830289866, -58.641816507741609, 0));
        p.AddVertex(new PlineVertex<double>(-26.582157785538975, -36.937302554408923, 0));
        p.AddVertex(new PlineVertex<double>(-10.28691561989004, -37.532476419015708, 1.3912026201985261));
        p.AddVertex(new PlineVertex<double>(-13.893804164275295, -25.348705381926596, -0.11647644536222093));
        p.AddVertex(new PlineVertex<double>(-28.2339105574362, -31.080328041215495, -1.085617131796748));
        p.AddVertex(new PlineVertex<double>(-29.377921412854658, -13.844388743394063, -0.15456950678939174));
        p.AddVertex(new PlineVertex<double>(14.510418378614602, -28.509989504212214, -1.4576599144958926));
        p.AddVertex(new PlineVertex<double>(-1.2286189799091662, -38.673320024003878, -0.91078921695615289));
        p.AddVertex(new PlineVertex<double>(9.60151673203314, -35.341372571345481, 0));
        p.AddVertex(new PlineVertex<double>(14.479870248214546, -31.658640256344022, 0));
        p.AddVertex(new PlineVertex<double>(1.7936188894239606, -26.136653943972561, 0));
        p.AddVertex(new PlineVertex<double>(-7.9472835632916627, -46.207329885654296, 0));
        p.AddVertex(new PlineVertex<double>(2.3519338771482836, -49.476066937850312, 0));
        if (p.Orientation() == PlineOrientation.Clockwise) p.InvertDirection();
        return p;
    }

    private static double Covered(IEnumerable<Polyline<double>> loops) => Math.Abs(loops.Sum(p => p.Area()));

    private static double MaxAbsBulge(Polyline<double> p) =>
        Enumerable.Range(0, p.VertexCount).Max(i => Math.Abs(p.Get(i).Bulge));

    /// <summary>
    /// Cavalier Contours requires every bulge to sit within -1 and 1. Rhino does not, so the
    /// conversion has to split anything wider before handing it over.
    /// </summary>
    [Fact]
    public void ConversionSplitsArcsWiderThanHalfCircle()
    {
        var raw = CurveA();
        Assert.True(MaxAbsBulge(raw) > 1.0, "this fixture is meant to contain a reflex arc");

        var split = BBCavalier.SplitArcsWiderThanHalfCircle(raw);

        Assert.True(MaxAbsBulge(split) <= 1.0, $"bulge still out of range: {MaxAbsBulge(split)}");
        Assert.Equal(raw.VertexCount + 3, split.VertexCount);
        Assert.Equal(raw.Area(), split.Area(), 1e-9);
    }

    [Fact]
    public void SplittingLeavesArcsWithinRangeUntouched()
    {
        // Semicircle, bulge exactly 1, which is the widest sweep the library accepts.
        var p = new Polyline<double>();
        p.SetIsClosed(true);
        p.AddVertex(new PlineVertex<double>(-1, 0, 1.0));
        p.AddVertex(new PlineVertex<double>(1, 0, 1.0));

        Assert.Same(p, BBCavalier.SplitArcsWiderThanHalfCircle(p));
    }

    [Fact]
    public void BooleanMatchesIndependentlyMeasuredGeometry()
    {
        // Mirrors what ToPolyline does, which cannot run here because RhinoCommon needs its
        // native library and that only loads inside the Rhino host process.
        var a = new List<Polyline<double>> { BBCavalier.SplitArcsWiderThanHalfCircle(CurveA()) };
        var b = new List<Polyline<double>> { BBCavalier.SplitArcsWiderThanHalfCircle(CurveB()) };

        Assert.Equal(1200.231, a[0].Area(), 1e-3);
        Assert.Equal(1200.231, b[0].Area(), 1e-3);

        Assert.Equal(1916.713, Covered(BBCavalier.BooleanPlines(BooleanOp.Or, a, b)), 0.05);
        Assert.Equal(483.723, Covered(BBCavalier.BooleanPlines(BooleanOp.And, a, b)), 0.05);
        Assert.Equal(716.478, Covered(BBCavalier.BooleanPlines(BooleanOp.Not, a, b)), 0.05);
        Assert.Equal(1432.990, Covered(BBCavalier.BooleanPlines(BooleanOp.Xor, a, b)), 0.05);
    }
}
