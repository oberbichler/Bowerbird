using Rhino.Geometry;
using System.Collections.Generic;

namespace Bowerbird.Text;

public class Letter
{
    public char Character { get; set; }

    public double Start { get; set; }

    public double End { get; set; }

    public double Width => End - Start;

    public List<List<Vector3d>> Vectors { get; set; } = new();

    public List<Curve> Write(ref Point3d position, Vector3d unitX, Vector3d unitY)
    {
        position -= Start * unitX;

        var curves = new List<Curve>(Vectors.Count);

        for (int i = 0; i < Vectors.Count; i++)
        {
            var currentPath = Vectors[i];
            if (currentPath.Count == 0) continue;

            var pointCount = (currentPath.Count + 1) / 2;
            var curve = new PolyCurve();

            var localP = currentPath[0];
            var lastPoint = position + localP.X * unitX + localP.Y * unitY;

            for (int j = 1; j < pointCount; j++)
            {
                localP = currentPath[j * 2];
                var nextPoint = position + localP.X * unitX + localP.Y * unitY;
                var localT = currentPath[j * 2 - 1];

                if (localT.IsZero)
                {
                    curve.Append(new Line(lastPoint, nextPoint));
                }
                else
                {
                    var tangent = localT.X * unitX + localT.Y * unitY;
                    curve.Append(new Arc(lastPoint, tangent, nextPoint));
                }

                lastPoint = nextPoint;
            }

            curves.Add(curve);
        }

        position += End * unitX;

        return curves;
    }
}
