"""Build the Smart Weight Plate kanim from publish/sprite.png.

    python tools/make_art.py

The source sprite is a single opaque drawing (white background, grey drop shadow, red panel).
This script cuts it into the same parts the vanilla Weight Plate uses, so the building can
behave the same way:

  body   frame 0 = red panel (red signal), frame 1 = green panel (green signal)
  cap    the button on top, drawn behind the body; it slides down while the plate is pressed
  place  the whole plate, for the construction ghost
  ui     the whole plate, for the build menu

and writes the kanim to src/SmartWeightPlate/anim/assets/smart_weight_plate/ with the anim
names the vanilla plate has (on/off x up/down, each with a one-frame _pre).
"""
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "common", "tools", "MakeKanim"))
from kanim_writer import Sprite, write_kanim  # noqa: E402

NAME = "smart_weight_plate"
OUT = os.path.join(ROOT, "src", "SmartWeightPlate", "anim", "assets", NAME)

BODY_UNITS = 224.0      # display width of the body, same as the vanilla plate (cell = 200)
BODY_BOTTOM = 7.0       # the vanilla body hangs this far below the cell's bottom edge
CAP_TRAVEL = 0.7        # fraction of the cap's height it sinks when pressed
TEXTURE_SCALE = 0.75    # texture pixels per anim unit (vanilla art is about 0.5)
CAP_SPLIT = 204         # source row where the cap ends; the body starts a little above it
BODY_START = 190


def remove_background(im):
    """Flood-fills the light, unsaturated surround (white page and grey shadow) to transparent."""
    r, g, b = im.convert("RGB").split()
    darkest = ImageChops.darker(ImageChops.darker(r, g), b)
    lightest = ImageChops.lighter(ImageChops.lighter(r, g), b)
    light = darkest.point(lambda v: 255 if v > 110 else 0)
    grey = ImageChops.subtract(lightest, darkest).point(lambda v: 255 if v < 40 else 0)
    candidate = ImageChops.multiply(light, grey)
    w, h = im.size
    for seed in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1), (w // 2, h - 3)):
        if candidate.getpixel(seed) == 255:
            ImageDraw.floodfill(candidate, seed, 128)
    background = candidate.point(lambda v: 255 if v == 128 else 0)
    # Eat two pixels into the outline so no light fringe survives the downscale.
    background = background.filter(ImageFilter.MaxFilter(5))
    out = im.convert("RGBA")
    out.putalpha(background.point(lambda v: 255 - v))
    return out


def green_variant(im):
    """Turns the red panel green (vanilla's signal colours) by swapping the red and green channels."""
    r, g, b, a = im.split()
    reddish = ImageChops.subtract(r, ImageChops.lighter(g, b)).point(lambda v: 255 if v > 60 else 0)
    swapped = Image.merge("RGBA", (g, r.point(lambda v: int(v * 0.8)), b, a))
    return Image.composite(swapped, im, reddish)


def shrink(im, units_per_px):
    scale = units_per_px * TEXTURE_SCALE
    size = (max(1, round(im.width * scale)), max(1, round(im.height * scale)))
    # Premultiply so transparent pixels do not bleed their colour into the edges.
    pre = im.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")
    return Sprite(pre, im.width * units_per_px, im.height * units_per_px)


def main():
    src = remove_background(Image.open(os.path.join(ROOT, "publish", "sprite.png")))
    src = src.crop(src.getbbox())
    w, h = src.size

    body = src.crop((0, BODY_START, w, h))
    body = body.crop(body.getbbox())
    # The cap is narrower than the body; keep only its own columns above the split.
    cap = src.crop((0, 0, w, CAP_SPLIT))
    cap_cols = src.crop((0, 0, w, BODY_START - 10)).getbbox()
    cap = cap.crop((cap_cols[0], 0, cap_cols[2], CAP_SPLIT))

    k = BODY_UNITS / body.width
    body_h, cap_h, full_h = body.height * k, cap.height * k, h * k
    body_y = BODY_BOTTOM - body_h / 2
    full_y = BODY_BOTTOM - full_h / 2
    cap_up = BODY_BOTTOM - full_h + cap_h / 2
    cap_down = cap_up + cap_h * CAP_TRAVEL
    cap_x = ((cap_cols[0] + cap_cols[2]) / 2 - w / 2) * k

    whole = shrink(src, k)
    symbols = {
        "body": [shrink(body, k), shrink(green_variant(body), k)],
        "cap": [shrink(cap, k)],
        "place": [whole],
        "ui": [whole],
    }
    anims = {}
    for signal, frame in (("off", 0), ("on", 1)):
        for position, cap_y in (("up", cap_up), ("down", cap_down)):
            elements = [("body", frame, 0.0, body_y), ("cap", 0, cap_x, cap_y)]
            anims["%s_%s_pre" % (signal, position)] = [elements]
            anims["%s_%s" % (signal, position)] = [elements]
    anims["place"] = [[("place", 0, 0.0, full_y)]]
    anims["ui"] = [[("ui", 0, 0.0, full_y)]]
    write_kanim(OUT, NAME, symbols, anims)
    print("wrote %s: body %.0fx%.0f units, cap %.0fx%.0f, cap travel %.0f" % (OUT, BODY_UNITS, body_h, cap.width * k, cap_h, cap_h * CAP_TRAVEL))


if __name__ == "__main__":
    main()
