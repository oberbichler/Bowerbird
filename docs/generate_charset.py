#!/usr/bin/env -S uv run
# /// script
# requires-python = ">=3.9"
# ///
"""
generate_charset.py – Generate a glyph-chart SVG for a Bowerbird font JSON.

Usage:
    uv run docs/generate_charset.py                          # defaults
    uv run docs/generate_charset.py --font path/to/font.json
    uv run docs/generate_charset.py --font regular.json --out chart.svg

Defaults:
    --font  Bowerbird/Resources/Fonts/bowerbird_regular.json
    --out   Bowerbird/Resources/Fonts/bowerbird_regular_charset.svg

Arc math
--------
The font stores circular arcs as a "bulge" value  b = tan(delta/4),
where delta is the signed central angle.

    delta      = 4 · atan(b)
    half_delta = delta / 2
    R          = chord / (2 · |sin(half_delta)|)
    large_arc  = 1  if |delta| > π  else 0
    sweep      = 0  if b < 0        else 1   # CCW / CW in SVG (y-down)

After the y-flip (svg_y = baseline – font_y · scale) the signed
rotation direction in font space (y-up) is preserved in SVG screen
space, so b < 0 stays counter-clockwise (sweep=0) and b > 0 stays
clockwise (sweep=1).
"""

import argparse
import html
import json
import math
import os
import sys

# ── character groups shown in the chart ──────────────────────────────────────

# fmt: off
GROUPS = [
    ("Uppercase",            list(range(65, 91))),
    ("Lowercase",            list(range(97, 123))),
    ("Digits & Symbols",     [*range(48, 58), *range(33, 48), *range(58, 65),
                              *range(91, 97), *range(123, 127)]),
    ("Math & Currency",      [162, 163, 165, 177, 178, 179, 247,
                              8230, 8240, 8364, 8721, 8730, 8734, 8738, 8747,
                              8776, 8793, 8800, 8801, 8804, 8805, 8960,
                              9633, 9657, 9667, 9669, 9727]),
    ("International",        [169, 171, 174, 176, 187, 223]),
    ("Accented Latin (UC)",  [*range(192, 207), 209, *range(210, 215), 216,
                              *range(217, 221),
                              256, 258, 260, 262, 264, 266, 268, 270, 272,
                              274, 278, 280, 282, 284, 292, 296, 308, 310,
                              313, 315, 323, 325, 332, 336, 340, 344, 346, 348,
                              352, 354, 356, 358, 360, 362, 364, 366, 368, 370,
                              377, 379, 381]),
    ("Accented Latin (LC)",  [224, 225, 228, 229, 230, 232, 233, 236, 237,
                              242, 243, 246, 248, 249, 250, 252, 732]),
    ("Greek (UC)",           list(range(913, 938))),
    ("Greek (LC)",           list(range(945, 970))),
]
# fmt: on

# ── layout ────────────────────────────────────────────────────────────────────

# fmt: off
SCALE    = 55   # pixels per font unit
CELL_W   = 80   # cell width  (px)
CELL_H   = 130  # cell height (px) – tall enough for descenders + diacritics
LABEL_H  = 22   # label strip below each cell
LABEL_DY = 15   # vertical offset of label text within the label strip
PER_ROW  = 16   # glyphs per row
MARGIN   = 24   # outer margin (px)
BASELINE = 95   # distance from cell top to font y=0  (px)
# fmt: on

CSS = """\
  .lbl  { font: 10px monospace; fill: #555; }
  .ttl  { font: bold 13px sans-serif; fill: #111; }
  .cell { fill: #fafafa; stroke: #e0e0e0; stroke-width: 0.5; }
  .g    { fill: none; stroke: #1a1a2e; stroke-width: 1.8;
          stroke-linecap: round; stroke-linejoin: round; }
  .bl   { stroke: #c8c8c8; stroke-width: 0.5; }
  .cl   { stroke: #e4e4e4; stroke-width: 0.5; stroke-dasharray: 2,3; }
"""

# ── arc conversion ────────────────────────────────────────────────────────────


def _arc_cmd(x1, y1, x2, y2, b, scale, ox, baseline_y):
    """Return an SVG 'A …' command for one font arc segment."""
    delta = 4.0 * math.atan(b)
    half_d = delta / 2.0
    dx, dy = x2 - x1, y2 - y1
    chord = math.sqrt(dx * dx + dy * dy)
    sin_hd = abs(math.sin(half_d))
    ex = ox + x2 * scale
    ey = baseline_y - y2 * scale
    if sin_hd < 1e-10 or chord < 1e-10:  # degenerate arc (b≈0) → straight line
        return f"L {ex:.4f} {ey:.4f}"
    R = chord / (2.0 * sin_hd) * scale
    large_arc = 1 if abs(delta) > math.pi else 0
    sweep = 0 if b < 0 else 1
    return f"A {R:.4f} {R:.4f} 0 {large_arc} {sweep} {ex:.4f} {ey:.4f}"


def _glyph_paths(paths_data, scale, ox, baseline_y):
    """Yield SVG path 'd' strings for all strokes of one glyph."""
    for path in paths_data:
        lx, ly = path["x"], path["y"]
        d = f"M {ox + lx * scale:.4f} {baseline_y - ly * scale:.4f}"
        for to in path.get("to", []):
            tx, ty = to["x"], to["y"]
            b = to.get("b")
            if b is None:
                d += f" L {ox + tx * scale:.4f} {baseline_y - ty * scale:.4f}"
            else:
                d += " " + _arc_cmd(lx, ly, tx, ty, b, scale, ox, baseline_y)
            lx, ly = tx, ty
        yield d


# ── SVG builder ───────────────────────────────────────────────────────────────


def generate(font_path: str, out_path: str) -> None:
    with open(font_path, encoding="utf-8") as fh:
        font = json.load(fh)

    by_code = {letter["code"]: letter for letter in font["letters"]}

    # filter each group to codes actually present in the font
    groups = [(name, [c for c in codes if c in by_code]) for name, codes in GROUPS]
    groups = [(name, codes) for name, codes in groups if codes]

    TITLE_H = 28
    total_w = PER_ROW * CELL_W + 2 * MARGIN
    total_h = (
        MARGIN
        + sum(
            TITLE_H + math.ceil(len(codes) / PER_ROW) * (CELL_H + LABEL_H)
            for _, codes in groups
        )
        + MARGIN
    )

    lines = []
    lines.append('<?xml version="1.0" encoding="UTF-8"?>')
    lines.append(
        f'<svg xmlns="http://www.w3.org/2000/svg" '
        f'width="{total_w}" height="{total_h}" '
        f'viewBox="0 0 {total_w} {total_h}">'
    )
    lines.append(f"<style>{CSS}</style>")
    lines.append(f'<rect width="{total_w}" height="{total_h}" fill="white"/>')

    cy = MARGIN
    for group_name, codes in groups:
        lines.append(
            f'<text x="{MARGIN}" y="{cy + 18}" class="ttl">'
            f"{html.escape(group_name)}</text>"
        )
        cy += TITLE_H

        for i, code in enumerate(codes):
            col = i % PER_ROW
            row = i // PER_ROW
            cx = MARGIN + col * CELL_W
            top = cy + row * (CELL_H + LABEL_H)
            bl = top + BASELINE

            # cell background + guide lines
            lines.append(
                f'<rect x="{cx}" y="{top}" width="{CELL_W}" '
                f'height="{CELL_H}" class="cell"/>'
            )
            lines.append(
                f'<line x1="{cx}" y1="{bl}" x2="{cx + CELL_W}" y2="{bl}" class="bl"/>'
            )
            lines.append(
                f'<line x1="{cx}" y1="{bl - SCALE}" x2="{cx + CELL_W}" '
                f'y2="{bl - SCALE}" class="cl"/>'
            )

            # centre glyph horizontally within the cell
            letter = by_code[code]
            start, end = letter["start"], letter["end"]
            glyph_w = (end - start) * SCALE
            ox = cx + (CELL_W - glyph_w) / 2.0 - start * SCALE

            for d in _glyph_paths(letter.get("paths", []), SCALE, ox, bl):
                lines.append(f'<path d="{d}" class="g"/>')

            # label: character + code point
            try:
                ch = html.escape(chr(code)) if code != 32 else "SP"
            except ValueError:
                ch = ""
            lines.append(
                f'<text x="{cx + CELL_W // 2}" y="{top + CELL_H + LABEL_DY}" '
                f'class="lbl" text-anchor="middle">'
                f"{ch} U+{code:04X}</text>"
            )

        cy += math.ceil(len(codes) / PER_ROW) * (CELL_H + LABEL_H)

    lines.append("</svg>")

    os.makedirs(os.path.dirname(os.path.abspath(out_path)), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines))

    print(f"Written: {out_path}  ({total_w}×{total_h} px)")


# ── CLI ───────────────────────────────────────────────────────────────────────


def _default_paths():
    """Return (font_path, out_path) relative to the repo root."""
    here = os.path.dirname(os.path.abspath(__file__))
    repo = os.path.dirname(here)
    font = os.path.join(
        repo, "Bowerbird", "Resources", "Fonts", "bowerbird_regular.json"
    )
    out = os.path.join(
        repo, "Bowerbird", "Resources", "Fonts", "bowerbird_regular_charset.svg"
    )
    return font, out


if __name__ == "__main__":
    default_font, default_out = _default_paths()

    parser = argparse.ArgumentParser(
        description="Generate a glyph-chart SVG for a Bowerbird font JSON."
    )
    parser.add_argument(
        "--font",
        default=default_font,
        metavar="PATH",
        help=f"Font JSON file  (default: {default_font})",
    )
    parser.add_argument(
        "--out",
        default=default_out,
        metavar="PATH",
        help=f"Output SVG file  (default: {default_out})",
    )
    args = parser.parse_args()

    if not os.path.isfile(args.font):
        print(f"Error: font file not found: {args.font}", file=sys.stderr)
        sys.exit(1)

    generate(args.font, args.out)
