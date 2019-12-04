using Rhino.Geometry;

namespace Bowerbird
{
    public class PrincipalCurvaturePath : Path
    {
        private bool _type;
        private double _stepSize = 0.1;
        private double _angle;

        private PrincipalCurvaturePath(double stepSize, bool type, double angle)
        {
            _stepSize = stepSize;
            _type = type;
            _angle = angle;
        }

        public static PrincipalCurvaturePath Create(double stepSize, bool type, double angle)
        {
            return new PrincipalCurvaturePath(stepSize, type, angle);
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection)
        {
            var u = uv.X;
            var v = uv.Y;

            var curvature = Curvature.SurfaceCurvature.Create(surface, u, v);

            var direction3d = _type ? curvature.K2Direction : curvature.K1Direction;

            direction3d = Align(direction3d, lastDirection, _stepSize);

            return ToUV(curvature.A1, curvature.A2, direction3d);
        }
    }

}
