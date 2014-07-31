using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Text
{
    internal class Letter
    {
        public char Character { get; set; }

        public double Start { get; set; }

        public double End { get; set; }

        public double Width 
        {
            get { return End - Start; }
        }

        public List<List<Vector3d>> Vectors { get; set; }


        public List<Curve> Write(ref Point3d position, Vector3d unitX, Vector3d unitY)
        {
            position -= Start * unitX;

            var curves = new List<Curve>(Vectors.Count);

            for (int i = 0; i < Vectors.Count; i++)
            {
                var pointCount = (Vectors[i].Count + 1) / 2;

                var curve = new PolyCurve();

                var localP = Vectors[i][0];

                var lastPoint = position + localP.X * unitX + localP.Y * unitY;

                for (int j = 1; j < pointCount; j++)
                {
                    localP = Vectors[i][j * 2];

                    var nextPoint = position + localP.X * unitX + localP.Y * unitY;

                    var localT = Vectors[i][j * 2 - 1];

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

                curves.Add( curve);
            }

            position += End * unitX;
            
            return curves;
        }
    }
}
