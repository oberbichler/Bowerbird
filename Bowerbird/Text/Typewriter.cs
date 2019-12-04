using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bowerbird.Text
{
    internal class Typewriter
    {
        static Typewriter()
        {
            var regularFont = Font.Load(new StringReader(Bowerbird.Properties.Resources.bowerbird_regular));
            var boldFont = Font.Load(new StringReader(Bowerbird.Properties.Resources.bowerbird_bold));

            Regular = new Typewriter(regularFont);
            Bold = new Typewriter(boldFont);
        }

        public static readonly Typewriter Regular;

        public static readonly Typewriter Bold;


        public Typewriter(Font font)
        {
            Letters = font.Letters.ToDictionary(o => o.Character);
        }
        
        private Dictionary<char, Letter> Letters { get; set; }

        public IEnumerable<Curve> Write(string text, Point3d position, Vector3d unitX, Vector3d unitY, int hAlign, int vAlign)
        {
            var hSpacing = 0.1;
            var vSpacing = 1.4;

            var lines = System.Text.RegularExpressions.Regex.Split(text, "\r\n|\r|\n");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                var lineLetters = new List<Letter>(line.Length);

                foreach (var c in line)
                {
                    var letter = default(Letter);

                    if (!Letters.TryGetValue(c, out letter))
                        letter = Letters['\0'];

                    lineLetters.Add(letter);
                }

                var lineWidth = lineLetters.Sum(o => o.Width) + (lineLetters.Count - 1) * hSpacing;

                var linePosition = position;

                switch (vAlign)
                {
                    case 1:
                        linePosition -= (i * vSpacing + 1) * unitY;
                        break;
                    case 2:
                        linePosition += (lines.Length - i - 1) * vSpacing * unitY;
                        break;
                    case 3:
                        linePosition -= i * vSpacing * unitY;
                        break;
                    default:
                        linePosition += ((-1 - 2 * i + lines.Length) * vSpacing - 1) / 2.0 * unitY;
                        break;
                }

                switch (hAlign)
                {
                    case 1:
                        break;
                    case 2:
                        linePosition -= lineWidth * unitX;
                        break;
                    default:
                        linePosition -= lineWidth / 2 * unitX;
                        break;
                }

                foreach (var letter in lineLetters)
                {
                    foreach (var curve in letter.Write(ref linePosition, unitX, unitY))
                        yield return curve;

                    linePosition += unitX * hSpacing;
                }
            }
        }
    }
}
