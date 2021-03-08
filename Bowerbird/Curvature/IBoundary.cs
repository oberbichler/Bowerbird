using Rhino.Geometry;

namespace Bowerbird.Curvature
{
    interface IBoundary
    {
        BrepFace AdjacentFace { get; }

        Vector3d AdjacentTangent { get; }

        bool Clip(Vector2d a, ref Vector2d b);
    }
}