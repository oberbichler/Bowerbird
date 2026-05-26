using Clipper2Lib;

namespace Bowerbird.Migration;

/// <summary>
/// Maps legacy ClipperLib (Clipper 1) enum integer values to Clipper2Lib enum values.
/// Required for backward compatibility when opening .gh files saved with the old Bowerbird plugin.
/// </summary>
public static class LegacyEnumMapping
{
    /// <summary>
    /// Remaps old ClipperLib ClipType integer values to Clipper2 ClipType.
    /// Old ClipperLib:  ctIntersection=0, ctUnion=1, ctDifference=2, ctXor=3
    /// New Clipper2Lib:  NoClip=0, Intersection=1, Union=2, Difference=3, Xor=4
    /// </summary>
    public static ClipType RemapClipType(int legacyValue) => legacyValue switch
    {
        0 => ClipType.Intersection,
        1 => ClipType.Union,
        2 => ClipType.Difference,
        3 => ClipType.Xor,
        _ => ClipType.Union
    };

    /// <summary>
    /// Remaps old ClipperLib PolyFillType integer values to Clipper2 FillRule.
    /// Values are identical between versions:
    /// Old: pftEvenOdd=0, pftNonZero=1, pftPositive=2, pftNegative=3
    /// New: EvenOdd=0, NonZero=1, Positive=2, Negative=3
    /// </summary>
    public static FillRule RemapFillRule(int legacyValue) =>
        Enum.IsDefined(typeof(FillRule), legacyValue)
            ? (FillRule)legacyValue
            : FillRule.EvenOdd;
}
