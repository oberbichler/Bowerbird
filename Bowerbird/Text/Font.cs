using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Bowerbird.Text
{
    internal class Font
    {
        public Font()
        {
            Letters = new List<Letter>();
        }


        public string Name { get; set; }

        public string Style { get; set; }

        public string Version { get; set; }

        public List<Letter> Letters { get; private set; }


        public static Font Load(TextReader reader)
        {
            var document = XDocument.Load(reader);

            var fontNode = document.Root;

            var font = new Font();

            font.Name = fontNode.Attribute("name").Value;
            font.Style = fontNode.Attribute("style").Value;
            font.Version = fontNode.Attribute("version").Value;

            foreach (var letterNode in fontNode.Elements("letter"))
            {
                var letter = new Letter();

                letter.Character = (char)(int)letterNode.Attribute("code");
                letter.Start = (double)letterNode.Attribute("start");
                letter.End = (double)letterNode.Attribute("end");
                letter.Vectors = new List<List<Vector3d>>();

                foreach (var pathNode in letterNode.Elements("path"))
                {
                    var path = new List<Vector3d>();

                    var lastX = (double)pathNode.Attribute("x");
                    var lastY = (double)pathNode.Attribute("y");

                    path.Add(new Vector3d(lastX, lastY, 0));

                    foreach (var toNode in pathNode.Elements("to"))
                    {
                        var toX = (double)toNode.Attribute("x");
                        var toY = (double)toNode.Attribute("y");

                        if (toNode.Attribute("b") == null)
                        {
                            path.Add(Vector3d.Zero);
                        }
                        else
                        {
                            var b = (double)toNode.Attribute("b");

                            var chordX = toX - lastX;
                            var chordY = toY - lastY;

                            var deltaOver2 = 2 * Math.Atan(b);

                            var cs = Math.Cos(deltaOver2);
                            var sn = Math.Sin(deltaOver2);

                            var tangentX = chordX * cs - chordY * sn;
                            var tangentY = chordX * sn + chordY * cs;

                            var tangent = new Vector3d(tangentX, tangentY, 0);

                            path.Add(tangent);
                        }

                        path.Add(new Vector3d(toX, toY, 0));

                        lastX = toX;
                        lastY = toY;
                    }

                    letter.Vectors.Add(path);
                }

                font.Letters.Add(letter);
            }

            return font;
        }

        public static void Save(Stream stream, Font font)
        {
            var xml = new XElement("bbfont",
                                   new XAttribute("name", font.Name),
                                   new XAttribute("style", font.Style),
                                   new XAttribute("version", font.Version),
                                   LettersToXml(font.Letters));

            var document = new XDocument(xml);

            document.Save(stream);
        }

        private static IEnumerable<XNode> LettersToXml(IEnumerable<Letter> letters)
        {
            foreach (var letter in letters)
            {
                yield return new XElement("letter",
                                          new XAttribute("unicode", (int)letter.Character),
                                          new XAttribute("start", letter.Start),
                                          new XAttribute("end", letter.End),
                                          PathsToXml(letter.Vectors));
            }
        }

        private static IEnumerable<XElement> PathsToXml(IEnumerable<List<Vector3d>> paths)
        {
            Func<double, string> format = new Func<double, string>((o) => string.Format("{0:0.00############}", o));

            foreach (var path in paths)
            {
                if (path.Count < 3)
                    continue;

                var xml = new XElement("path",
                                       new XAttribute("x", format(path[0].X)),
                                       new XAttribute("y", format(path[0].Y)));

                for (int i = 1; i < path.Count; i += 2)
                {
                    var a = path[i - 1];
                    var d = path[i];
                    var b = path[i + 1];

                    var toXml = new XElement("to",
                                             new XAttribute("x", format(b.X)),
                                             new XAttribute("y", format(b.Y)));

                    if (!d.IsZero)
                    {
                        var s = (b - a);

                        var theta = 2 * (Math.Atan2(d.Y, d.X) - Math.Atan2(s.Y, s.X));

                        var bulge = Math.Tan(theta / 4);

                        toXml.Add(new XAttribute("b", format(bulge)));
                    }

                    xml.Add(toXml);
                }

                yield return xml;
            }
        }
    }
}
