using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bowerbird.Text;

public class Typewriter
{
    static Typewriter()
    {
        var assembly = typeof(Typewriter).Assembly;

        using (var regularStream = assembly.GetManifestResourceStream("Bowerbird.Resources.Fonts.bowerbird_regular.json"))
        {
            if (regularStream == null)
                throw new FileNotFoundException("Could not find embedded resource: bowerbird_regular.json");
            using var reader = new StreamReader(regularStream);
            var regularFont = Font.Load(reader);
            Regular = new Typewriter(regularFont);
        }

        using (var boldStream = assembly.GetManifestResourceStream("Bowerbird.Resources.Fonts.bowerbird_bold.json"))
        {
            if (boldStream == null)
                throw new FileNotFoundException("Could not find embedded resource: bowerbird_bold.json");
            using var reader = new StreamReader(boldStream);
            var boldFont = Font.Load(reader);
            Bold = new Typewriter(boldFont);
        }
    }

    public static readonly Typewriter Regular;

    public static readonly Typewriter Bold;

    public Typewriter(Font font)
    {
        Letters = font.Letters.ToDictionary(o => o.Character);
    }

    private Dictionary<char, Letter> Letters { get; set; }

    public IEnumerable<Curve> Write(string text, Point3d position, Vector3d unitX, Vector3d unitY, HorizontalAlignment hAlign, VerticalAlignment vAlign)
    {
        const double hSpacing = 0.1;
        const double vSpacing = 1.4;

        var lines = System.Text.RegularExpressions.Regex.Split(text, "\r\n|\r|\n");

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineLetters = new List<Letter>(line.Length);

            foreach (var c in line)
            {
                if (!Letters.TryGetValue(c, out var letter))
                    letter = Letters['\0'];

                lineLetters.Add(letter);
            }

            var lineWidth = lineLetters.Sum(o => o.Width) + (lineLetters.Count - 1) * hSpacing;
            var linePosition = position;

            linePosition += vAlign switch
            {
                VerticalAlignment.Top => -(i * vSpacing + 1.0) * unitY,
                VerticalAlignment.Bottom => (lines.Length - i - 1) * vSpacing * unitY,
                VerticalAlignment.Baseline => -i * vSpacing * unitY,
                _ => ((-1.0 - 2.0 * i + lines.Length) * vSpacing - 1.0) / 2.0 * unitY
            };

            linePosition -= hAlign switch
            {
                HorizontalAlignment.Left => Vector3d.Zero,
                HorizontalAlignment.Right => lineWidth * unitX,
                _ => lineWidth / 2.0 * unitX
            };

            foreach (var letter in lineLetters)
            {
                foreach (var curve in letter.Write(ref linePosition, unitX, unitY))
                    yield return curve;

                linePosition += unitX * hSpacing;
            }
        }
    }
}
