using Rhino.Geometry;

namespace Bowerbird.Curvature;

public interface IBoundary
{
    BrepFace? AdjacentFace { get; }

    Vector3d AdjacentTangent { get; }

    Vector2d AdjacentUV { get; }

    bool Clip(Vector2d a, ref Vector2d b);
}
