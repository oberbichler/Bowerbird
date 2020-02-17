using Rhino.Geometry;

namespace Bowerbird
{
    public class GeodesicTorsionPath : Path
    {
        public double Value { get; private set; }

        public double Angle { get; private set; }

        public Types Type { get; private set; }

        private GeodesicTorsionPath(double value, double angle, Types type)
        {
            Value = value;
            Angle = angle;
            Type = type;
        }

        public static GeodesicTorsionPath Create(double value, double angle, Types type)
        {
            return new GeodesicTorsionPath(value, angle, type);
        }

        public override Vector3d InitialDirection(Surface surface, Vector2d uv, bool type)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            if (!curvature.FindGeodesicTorsion(Value, out var angle1, out var angle2))
                return Vector3d.Unset;

            var dir1 = curvature.K1Direction;
            var dir2 = curvature.K2Direction;

            dir1.Rotate(angle1 + Angle, curvature.N);
            dir2.Rotate(angle1 + Angle, curvature.N);

            return !type ? dir1 : dir2;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection, double stepSize)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            if (!curvature.FindGeodesicTorsion(Value, out var angle1, out var angle2))
                return Vector2d.Unset;

            var dir1 = curvature.K1Direction;
            var dir2 = curvature.K2Direction;

            dir1.Rotate(angle1, curvature.N);
            dir2.Rotate(angle2, curvature.N);

            var direction = Choose(dir1, dir2, lastDirection);

            direction = Align(direction, lastDirection, stepSize);

            return ToUV(curvature.A1, curvature.A2, direction);
        }
    }

}
