using Rhino.Geometry;
using System;

namespace Bowerbird
{
    public class PrincipalCurvaturePath : Path
    {
        private double _stepSize = 0.1;
        private double _angle;

        private PrincipalCurvaturePath(double stepSize, double angle)
        {
            _stepSize = stepSize;
            _angle = angle;
        }

        public static PrincipalCurvaturePath Create(double stepSize, double angle)
        {
            return new PrincipalCurvaturePath(stepSize, angle);
        }

        public override Vector3d InitialDirection(Surface surface, Vector2d uv, bool type)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            return !type ? curvature.K1Direction : curvature.K2Direction;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            var dir1 = curvature.K1Direction;
            var dir2 = curvature.K2Direction;

            dir1.Rotate(_angle, curvature.N);
            dir2.Rotate(_angle, curvature.N);

            var direction = Choose(dir1, dir2, lastDirection);

            direction = Align(direction, lastDirection, _stepSize);

            return ToUV(curvature.A1, curvature.A2, direction);
        }
    }

}
