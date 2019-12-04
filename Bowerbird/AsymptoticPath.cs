using Rhino.Geometry;
using System;

namespace Bowerbird
{
    public class AsymptoticPath : Path
    {
        private double _stepSize;
        private double _angle;
        private double _curvature;

        private AsymptoticPath(double stepSize, double angle, double curvature)
        {
            _stepSize = stepSize;
            _angle = angle;
            _curvature = curvature;
        }

        public static AsymptoticPath Create(double stepSize, double angle, double curvature)
        {
            return new AsymptoticPath(stepSize, angle, curvature);
        }

        public override Vector3d InitialDirection(Surface surface, Vector2d uv, bool type)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            if (!curvature.AngleByCurvature(_curvature, out var angle1, out var angle2))
                return Vector3d.Unset;

            var dir1 = curvature.K1Direction;
            var dir2 = curvature.K1Direction;

            dir1.Rotate(angle1 + _angle, curvature.N);
            dir2.Rotate(angle2 + _angle, curvature.N);

            return !type ? dir1 : dir2;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            if (!curvature.AngleByCurvature(_curvature, out var angle1, out var angle2))
                return Vector2d.Unset;

            var dir1 = curvature.K1Direction;
            var dir2 = curvature.K1Direction;

            dir1.Rotate(angle1 + _angle, curvature.N);
            dir2.Rotate(angle2 + _angle, curvature.N);

            var direction = Choose(dir1, dir2, lastDirection);

            direction = Align(direction, lastDirection, _stepSize);

            return ToUV(curvature.A1, curvature.A2, direction);
        }
    }
}
