using Bowerbird.Migration;
using Clipper2Lib;
using Xunit;

namespace Bowerbird.Tests;

public class LegacyEnumMappingTests
{
    // --- ClipType migration ---

    [Theory]
    [InlineData(0, ClipType.Intersection)]  // old ctIntersection=0
    [InlineData(1, ClipType.Union)]          // old ctUnion=1
    [InlineData(2, ClipType.Difference)]     // old ctDifference=2
    [InlineData(3, ClipType.Xor)]            // old ctXor=3
    public void RemapClipType_LegacyValues_MapsCorrectly(int legacy, ClipType expected)
        => Assert.Equal(expected, LegacyEnumMapping.RemapClipType(legacy));

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(4)]
    public void RemapClipType_UnknownValue_DefaultsToUnion(int legacy)
        => Assert.Equal(ClipType.Union, LegacyEnumMapping.RemapClipType(legacy));

    // --- FillRule migration ---

    [Theory]
    [InlineData(0, FillRule.EvenOdd)]   // old pftEvenOdd=0
    [InlineData(1, FillRule.NonZero)]   // old pftNonZero=1
    [InlineData(2, FillRule.Positive)]  // old pftPositive=2
    [InlineData(3, FillRule.Negative)]  // old pftNegative=3
    public void RemapFillRule_LegacyValues_MapsCorrectly(int legacy, FillRule expected)
        => Assert.Equal(expected, LegacyEnumMapping.RemapFillRule(legacy));

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void RemapFillRule_UnknownValue_DefaultsToEvenOdd(int legacy)
        => Assert.Equal(FillRule.EvenOdd, LegacyEnumMapping.RemapFillRule(legacy));

    // --- Clipper2 enum value stability (guard against Clipper2 updates) ---

    [Fact]
    public void Clipper2_ClipType_IntegerValuesStable()
    {
        Assert.Equal(0, (int)ClipType.None);
        Assert.Equal(1, (int)ClipType.Intersection);
        Assert.Equal(2, (int)ClipType.Union);
        Assert.Equal(3, (int)ClipType.Difference);
        Assert.Equal(4, (int)ClipType.Xor);
    }

    [Fact]
    public void Clipper2_FillRule_IntegerValuesStable()
    {
        Assert.Equal(0, (int)FillRule.EvenOdd);
        Assert.Equal(1, (int)FillRule.NonZero);
        Assert.Equal(2, (int)FillRule.Positive);
        Assert.Equal(3, (int)FillRule.Negative);
    }

    [Fact]
    public void Clipper2_JoinType_IntegerValuesStable()
    {
        Assert.Equal(0, (int)JoinType.Miter);
        Assert.Equal(1, (int)JoinType.Square);
        Assert.Equal(2, (int)JoinType.Bevel);
        Assert.Equal(3, (int)JoinType.Round);
    }

    [Fact]
    public void Clipper2_EndType_IntegerValuesStable()
    {
        Assert.Equal(0, (int)EndType.Polygon);
        Assert.Equal(1, (int)EndType.Joined);
        Assert.Equal(2, (int)EndType.Butt);
        Assert.Equal(3, (int)EndType.Square);
        Assert.Equal(4, (int)EndType.Round);
    }
}
