using Rhino.Geometry;

namespace Bowerbird
{
    public class AsymptoticPath : Path
    {
        private double _stepSize;
        private bool _type;
        private double _angle;
        private double _curvature;

        private AsymptoticPath(double stepSize, bool type, double angle, double curvature)
        {
            _stepSize = stepSize;
            _type = type;
            _angle = angle;
            _curvature = curvature;
        }

        public static AsymptoticPath Create(double stepSize, bool type, double angle, double curvature)
        {
            return new AsymptoticPath(stepSize, type, angle, curvature);
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            var direction3d = curvature.K1Direction;

            if (!curvature.AngleByCurvature(_curvature, out var angle1, out var angle2))
                return Vector2d.Unset;

            direction3d.Rotate((!_type ? angle1 : angle2) + _angle, curvature.N);

            direction3d = Align(direction3d, lastDirection, _stepSize);

            return ToUV(curvature.A1, curvature.A2, direction3d);
        }
    }
}
