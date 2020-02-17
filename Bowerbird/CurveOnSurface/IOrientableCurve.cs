using Rhino.Geometry;

namespace Bowerbird
{
    public interface IOrientableCurve
    {
        Interval Domain { get; }

        Point3d PointAt(double t);

        Vector3d TangentAt(double t);

        Vector3d NormalAt(double t);

        Vector3d BinormalAt(double t);

        Vector3d CurvatureAt(double t);

        bool ClosestPoint(Point3d sample, out double t);

        Vector3d NormalCurvatureAt(double t);

        Vector3d GeodesicCurvatureAt(double t);

        Vector3d GeodesicTorsionAt(double t);

        double TorsionAt(double t);

        Curve ToCurve(double tolerance);

        IOrientableCurve Reparameterized();

        IOrientableCurve Transform(Transform xform);

        IOrientableCurve Morph(SpaceMorph xmorph);
    }
}