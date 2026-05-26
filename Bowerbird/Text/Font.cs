using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Bowerbird.Text;

public class Font
{
    public Font()
    {
        Letters = new List<Letter>();
        Name = string.Empty;
        Style = string.Empty;
        Version = string.Empty;
    }

    public string Name { get; set; }

    public string Style { get; set; }

    public string Version { get; set; }

    public List<Letter> Letters { get; set; }

    public static Font Load(TextReader reader)
    {
        var jsonString = reader.ReadToEnd();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var fontDto = JsonSerializer.Deserialize<FontDto>(jsonString, options);
        if (fontDto == null)
            throw new InvalidDataException("Invalid font JSON: Deserialize returned null.");

        var font = new Font
        {
            Name = fontDto.Name ?? "Unnamed",
            Style = fontDto.Style ?? "Regular",
            Version = fontDto.Version ?? "1.0",
            Letters = new List<Letter>(fontDto.Letters?.Count ?? 0)
        };

        if (fontDto.Letters != null)
        {
            foreach (var letterDto in fontDto.Letters)
            {
                var letter = new Letter
                {
                    Character = (char)letterDto.Code,
                    Start = letterDto.Start,
                    End = letterDto.End,
                    Vectors = new List<List<Vector3d>>(letterDto.Paths?.Count ?? 0)
                };

                if (letterDto.Paths != null)
                {
                    foreach (var pathDto in letterDto.Paths)
                    {
                        var path = new List<Vector3d>();
                        var lastX = pathDto.X;
                        var lastY = pathDto.Y;

                        path.Add(new Vector3d(lastX, lastY, 0));

                        if (pathDto.To != null)
                        {
                            foreach (var toDto in pathDto.To)
                            {
                                var toX = toDto.X;
                                var toY = toDto.Y;

                                if (!toDto.B.HasValue)
                                {
                                    path.Add(Vector3d.Zero);
                                }
                                else
                                {
                                    var b = toDto.B.Value;

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
                        }

                        letter.Vectors.Add(path);
                    }
                }

                font.Letters.Add(letter);
            }
        }

        return font;
    }

    private class FontDto
    {
        public string? Name { get; set; }
        public string? Style { get; set; }
        public string? Version { get; set; }
        public List<LetterDto>? Letters { get; set; }
    }

    private class LetterDto
    {
        public int Code { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public List<PathDto>? Paths { get; set; }
    }

    private class PathDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public List<ToDto>? To { get; set; }
    }

    private class ToDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double? B { get; set; }
    }
}
