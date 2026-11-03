#!/usr/bin/env python3
"""Draws the mod's cover: its name over a speed dial with the needle far over
to the fast side, in the brush strokes of Timber Empires' icons
(Models/Icons/make_icons.py, found next to this repo), on the blueprint-paper
background of Game Modes' and the Mod Uploader's covers.

    python3 Tools/make_cover.py [Timber Empires checkout]

Writes Media/cover.png (1920x1080, for the Workshop and mod.io pages) and
TimberPerf/thumbnail.png (800x450, the one the game's mod list shows).
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
TE = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT.parent / "TimberEmpires"
sys.path.insert(0, str(TE / "Models" / "Icons"))
import make_icons as mi  # noqa: E402

W, H = 1920, 1080
ART = 1000                      # the drawing's square
mi.SIZE = ART
mi.SCALE = ART * mi.SS / 100

TITLE = "TIMBERPERF"

PAPER = (28, 62, 104)
PAPER_LIGHT = (52, 102, 156)
LINE = (150, 190, 228)


def background():
    rnd = random.Random(3)
    # Paper lit from the middle
    cx, cy = W * 0.5, H * 0.46
    img = Image.new("RGB", (W, H))
    px = img.load()
    maxd = math.hypot(W / 2, H / 2)
    for y in range(H):
        for x in range(W):
            k = min(1, math.hypot(x - cx, (y - cy) * 1.25) / maxd)
            k = k ** 1.4
            px[x, y] = tuple(int(PAPER_LIGHT[i] + (PAPER[i] - PAPER_LIGHT[i]) * k) for i in range(3))
    img = img.convert("RGBA")

    # The grid: fine every 30px, heavier every 150px, drawn a touch uneven
    grid = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(grid)
    for n, x in enumerate(range(0, W + 1, 30)):
        major = n % 5 == 0
        d.line([(x, 0), (x, H)], fill=LINE + (70 if major else 26,), width=2 if major else 1)
    for n, y in enumerate(range(0, H + 1, 30)):
        major = n % 5 == 0
        d.line([(0, y), (W, y)], fill=LINE + (70 if major else 26,), width=2 if major else 1)
    img.alpha_composite(grid)

    # Drafting doodles off to the sides: a compass arc, a dimension line, a title block
    doo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(doo)
    ink = LINE + (95,)
    d.arc([-260, 620, 340, 1220], 270, 360, fill=ink, width=3)
    d.arc([-200, 680, 280, 1160], 270, 360, fill=ink, width=2)
    d.arc([1560, -300, 2160, 300], 90, 180, fill=ink, width=3)
    d.arc([1620, -240, 2100, 240], 90, 180, fill=ink, width=2)
    for x0, y0, x1 in ((120, 130, 520), (1400, 960, 1800)):
        d.line([(x0, y0), (x1, y0)], fill=ink, width=3)
        for x in (x0, x1):
            d.line([(x, y0 - 14), (x, y0 + 14)], fill=ink, width=3)
            d.line([(x - 8, y0 + 8), (x + 8, y0 - 8)], fill=ink, width=3)
    d.rectangle([1450, 860, 1830, 940], outline=ink, width=3)
    d.line([(1450, 900), (1830, 900)], fill=ink, width=2)
    d.line([(1600, 860), (1600, 940)], fill=ink, width=2)
    img.alpha_composite(doo)
    return img


# The capitals as strokes on a unit box, each a list of strokes through its points
LETTERS = {
    "T": [[(0, 0), (1, 0)], [(.5, 0), (.5, 1)]],
    "I": [[(.5, 0), (.5, 1)], [(.2, 0), (.8, 0)], [(.2, 1), (.8, 1)]],
    "M": [[(0, 1), (0, 0), (.5, .62), (1, 0), (1, 1)]],
    "B": [[(0, 0), (0, 1), (.7, 1), (1, .85), (1, .62), (.7, .5), (0, .5)], [(0, 0), (.65, 0), (.92, .14), (.92, .36), (.65, .5)]],
    "E": [[(1, 0), (0, 0), (0, 1), (1, 1)], [(0, .5), (.75, .5)]],
    "R": [[(0, 1), (0, 0), (.7, 0), (1, .15), (1, .38), (.7, .52), (0, .52)], [(.5, .52), (1, 1)]],
    "P": [[(0, 1), (0, 0), (.7, 0), (1, .15), (1, .38), (.7, .52), (0, .52)]],
    "F": [[(1, 0), (0, 0), (0, 1)], [(0, .5), (.75, .5)]],
}


def title():
    """The mod's name in the same brush strokes as the picture, across the top of its own 100 grid."""
    g = mi.Glyph()
    height, width, track, space = 13, 6.6, 2.2, 6
    total = sum(space if ch == " " else width for ch in TITLE) + track * (len(TITLE) - 1)
    x = (100 - total) / 2
    for ch in TITLE:
        if ch == " ":
            x += space + track
            continue
        for stroke in LETTERS[ch]:
            g.line(*[(x + px * width, 8 + py * height) for px, py in stroke], w=2.5, taper=False)
        x += width + track
    return g


def arc(cx, cy, r, a0, a1, steps=40):
    """Points round an arc, angles in degrees counterclockwise from the right, y down."""
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / steps)), cy - r * math.sin(math.radians(a0 + (a1 - a0) * i / steps)))
            for i in range(steps + 1)]


def drawing():
    """A speed dial: its face, the ticks round it, the needle far over to fast with speed lines past it. 100 grid.
    Returns the strokes and the dial's fill."""
    g = mi.Glyph()
    cx, cy, r = 50, 78, 42
    g.line(*arc(cx, cy, r, 180, 0), w=3.4)
    g.line((cx - r, cy), (cx + r, cy), taper=False, w=3.4)
    for n in range(11):
        a = 180 - 18 * n
        inner = r - (7 if n % 5 == 0 else 4)
        g.line(arc(cx, cy, inner, a, a, 1)[0], arc(cx, cy, r - 1.5, a, a, 1)[0], w=2.8 if n % 5 == 0 else 2)
    # Speed lines off the needle's tip
    for a, k in ((34, 0.55), (26, 0.75), (18, 0.55)):
        g.line(arc(cx, cy, r + 4, a, a, 1)[0], arc(cx, cy, r + 4 + 9 * k, a, a, 1)[0], w=2.6)
    # The needle, from its hub to the fast side
    g.line((cx, cy), arc(cx, cy, r - 6, 22, 22, 1)[0], w=3.8)
    g.circle(cx, cy, 4, w=3)
    return g, [arc(cx, cy, r, 180, 0)]


def main():
    img = background()
    g, sheet = drawing()
    SS = mi.SS
    strokes = g.mask.resize((ART, ART), Image.LANCZOS)

    # The dial's face, lighter than the board, and its soft shadow
    paper = Image.new("L", (ART * SS, ART * SS), 0)
    for face in sheet:
        ImageDraw.Draw(paper).polygon([(x * mi.SCALE, y * mi.SCALE) for x, y in face], fill=255)
    paper = paper.resize((ART, ART), Image.LANCZOS)

    ox, oy = (W - ART) // 2, (H - ART) // 2 + 55
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    shadow = Image.new("L", (W, H), 0)
    shadow.paste(ImageChops.lighter(paper, strokes), (ox + 10, oy + 22))
    shadow = shadow.filter(ImageFilter.GaussianBlur(22)).point(lambda v: int(v * 0.55))
    black = Image.new("RGBA", (W, H), (6, 14, 30, 0))
    black.putalpha(shadow)
    img.alpha_composite(black)

    sheet_col = Image.new("RGBA", (ART, ART), (74, 128, 184, 0))
    sheet_col.putalpha(paper.point(lambda v: int(v * 0.55)))
    layer.alpha_composite(sheet_col, (ox, oy))

    # A glow at the fast end of the dial, where the needle points
    glow = Image.new("L", (W, H), 0)
    gx, gy = ox + ART * 0.8, oy + ART * 0.6
    ImageDraw.Draw(glow).ellipse([gx - 220, gy - 220, gx + 220, gy + 220], fill=120)
    glow = glow.filter(ImageFilter.GaussianBlur(70))
    warm = Image.new("RGBA", (W, H), (230, 200, 130, 0))
    warm.putalpha(glow.point(lambda v: int(v * 0.5)))
    layer.alpha_composite(warm)

    gold = Image.new("RGBA", (ART, ART), mi.GOLD + (0,))
    gold.putalpha(strokes)
    layer.alpha_composite(gold, (ox, oy))

    # The title, in strokes too, above the dial
    toy = 30
    lettering = title().mask.resize((ART, ART), Image.LANCZOS)
    tshadow = Image.new("L", (W, H), 0)
    tshadow.paste(lettering, (ox + 6, toy + 12))
    tshadow = tshadow.filter(ImageFilter.GaussianBlur(14)).point(lambda v: int(v * 0.6))
    tblack = Image.new("RGBA", (W, H), (6, 14, 30, 0))
    tblack.putalpha(tshadow)
    img.alpha_composite(tblack)
    tgold = Image.new("RGBA", (ART, ART), mi.GOLD + (0,))
    tgold.putalpha(lettering)
    layer.alpha_composite(tgold, (ox, toy))
    img.alpha_composite(layer)

    # Grain, then the vignette
    rnd = random.Random(11)
    noise = Image.effect_noise((W, H), 38).convert("L")
    grain = Image.new("RGBA", (W, H), (255, 255, 255, 0))
    grain.putalpha(noise.point(lambda v: max(0, v - 128) // 6))
    img.alpha_composite(grain)
    vig = Image.new("L", (W // 4, H // 4), 0)
    vp = vig.load()
    for y in range(vig.height):
        for x in range(vig.width):
            dx, dy = (x / vig.width - 0.5) * 2, (y / vig.height - 0.5) * 2
            vp[x, y] = int(255 * min(1, max(0, (math.hypot(dx, dy * 1.1) - 0.55) / 0.95)) ** 1.6 * 0.7)
    vig = vig.resize((W, H), Image.BICUBIC)
    dark = Image.new("RGBA", (W, H), (4, 10, 22, 0))
    dark.putalpha(vig)
    img.alpha_composite(dark)

    out = img.convert("RGB")
    (ROOT / "Media").mkdir(exist_ok=True)
    out.save(ROOT / "Media" / "cover.png")
    out.resize((800, 450), Image.LANCZOS).save(ROOT / "TimberPerf" / "thumbnail.png")


main()
