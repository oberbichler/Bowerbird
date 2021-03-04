using Rhino.Geometry;

namespace Bowerbird.Curvature
{
    interface IBoundary
    {
        bool Clip(Vector2d a, ref Vector2d b);
    }
}