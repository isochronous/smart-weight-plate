"""Rework 3GuB's smart_weight_plate kanim so it plays correctly in the game.

Run from the repo root:  python tools/knobs_follow_pressure.py

What it changes, and why:
- The knob motion follows pressure, not the signal. The artist keyed it to the on_ transitions,
  but the plate's signal and its pressure are independent (the invert option), so the off_
  transitions get the same motion with the red light, and both pressed idles hold the swapped pose.
- The transitions are faster, 10 frames at 30 fps instead of 20, with an ease-in curve (the knobs
  start slowly and accelerate), generated from the artist's start and end poses.
- The button draws behind the plate: it is the last element of every frame. Kanimal's convention,
  which the game follows, is that the first element of a frame is the frontmost.
- The _fg suffix the artist used makes kanimal set the Foreground flag on those symbols, which
  the game renders through a separate layering path that behaves differently in overlays; the
  flag is cleared so every view draws the frame in plain element order.

Klei anim format: "ANIM", int version, int elementCount, int frameCount, int animCount,
  per anim: str name, uint hash, float rate, int frameCount,
    per frame: float x, y, w, h, int elementCount,
      per element (60 bytes): uint symbol, int frame, uint folder, int flags, float a, b, g, r,
                              float m1..m6, float order
  int maxVisSymbolFrames, int hashCount, per hash: uint hash, str name.
Build format: "BILD", int version, int symbolCount, int frameCount, str name,
  per symbol: uint hash, uint path, uint color, int flags, int frameCount, per frame 44 bytes,
  int hashCount, per hash: uint hash, str name.
Strings are int length + bytes. The game's parser ignores the two anim header counts.
"""
import struct, os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src", "SmartWeightPlate", "anim", "assets", "smart_weight_plate")
ANIM = os.path.join(ROOT, "smart_weight_plate_anim.bytes")
BUILD = os.path.join(ROOT, "smart_weight_plate_build.bytes")
TRANSITION_FRAMES = 10
FG_FLAG = 8

# ---------------------------------------------------------------- anim
b = open(ANIM, "rb").read()
o = 0
def rs():
    global o
    n = struct.unpack_from("<i", b, o)[0]; o += 4
    s = b[o:o+n].decode(); o += n
    return s
assert b[:4] == b"ANIM"; o = 4
version, _ec, _fc, anim_count = struct.unpack_from("<iiii", b, o); o += 16
anims = []
for _ in range(anim_count):
    name = rs()
    hsh, rate, nframes = struct.unpack_from("<Ifi", b, o); o += 12
    frames = []
    for _f in range(nframes):
        x, y, w, h, nel = struct.unpack_from("<ffffi", b, o); o += 20
        els = []
        for _e in range(nel):
            els.append(list(struct.unpack_from("<IiIifffffffffff", b, o))); o += 60   # [0 sym,1 frame,2 folder,3 flags,4-7 abgr,8-13 m1..m6,14 order]
        frames.append([x, y, w, h, els])
    anims.append([name, hsh, rate, frames])
max_vis = struct.unpack_from("<i", b, o)[0]; o += 4
nh = struct.unpack_from("<i", b, o)[0]; o += 4
hashes = []
for _ in range(nh):
    h = struct.unpack_from("<I", b, o)[0]; o += 4
    hashes.append((h, rs()))
assert o == len(b)
names = dict(hashes)
by_name = {a[0]: a for a in anims}
def sym_is(el, n): return names.get(el[0]) == n
LIGHT_OFF, LIGHT_ON = 0, 1

def ease_in(t): return t * t

def transition(first, last, light_frame):
    """Frames from the first pose to the last pose: knobs eased in, everything else from the last pose."""
    out = []
    for i in range(TRANSITION_FRAMES):
        t = ease_in(i / (TRANSITION_FRAMES - 1))
        els = []
        for a, z in zip(first[4], last[4]):
            el = list(z)
            if sym_is(el, "slider_fg"):
                for k in (12, 13):   # m5, m6: translation
                    el[k] = a[k] + (z[k] - a[k]) * t
            if sym_is(el, "light_fg"):
                el[1] = light_frame
            els.append(el)
        out.append([last[0], last[1], last[2], last[3], els])
    return out

press_first, press_last = by_name["on_down_pre"][3][0], by_name["on_down_pre"][3][-1]
release_first, release_last = by_name["on_up_pre"][3][0], by_name["on_up_pre"][3][-1]
# The release should end exactly on the idle pose and the press should start from it.
rest = {el[1]: (el[12], el[13]) for el in by_name["on_up"][3][0][4] if sym_is(el, "slider_fg")}
for el in release_last[4]:
    if sym_is(el, "slider_fg"): el[12], el[13] = rest[el[1]]
for el in press_first[4]:
    if sym_is(el, "slider_fg"): el[12], el[13] = rest[el[1]]
by_name["on_down_pre"][3] = transition(press_first, press_last, LIGHT_ON)
by_name["off_down_pre"][3] = transition(press_first, press_last, LIGHT_OFF)
by_name["on_up_pre"][3] = transition(release_first, release_last, LIGHT_ON)
by_name["off_up_pre"][3] = transition(release_first, release_last, LIGHT_OFF)

# Pressed idles hold the end-of-press knob positions.
end_of_press = {el[1]: (el[12], el[13]) for el in press_last[4] if sym_is(el, "slider_fg")}
for idle in ("on_down", "off_down"):
    for el in by_name[idle][3][0][4]:
        if sym_is(el, "slider_fg"): el[12], el[13] = end_of_press[el[1]]

# Button behind everything: last in every frame (first element is the frontmost).
for name, hsh, rate, frames in anims:
    for fr in frames:
        btn = [el for el in fr[4] if sym_is(el, "button")]
        fr[4] = [el for el in fr[4] if not sym_is(el, "button")] + btn

out = bytearray(b"ANIM")
def ws(s):
    e = s.encode(); out.extend(struct.pack("<i", len(e))); out.extend(e)
total_frames = sum(len(a[3]) for a in anims)
total_elements = sum(len(fr[4]) for a in anims for fr in a[3])
out.extend(struct.pack("<iiii", version, total_elements, total_frames, len(anims)))
for name, hsh, rate, frames in anims:
    ws(name); out.extend(struct.pack("<Ifi", hsh, rate, len(frames)))
    for x, y, w, h, els in frames:
        out.extend(struct.pack("<ffffi", x, y, w, h, len(els)))
        for el in els:
            out.extend(struct.pack("<IiIifffffffffff", *el))
out.extend(struct.pack("<ii", max_vis, len(hashes)))
for h, s in hashes:
    out.extend(struct.pack("<I", h)); ws(s)
open(ANIM, "wb").write(out)
for a in anims:
    print("  %-13s %2d frames  front-to-back: %s" % (a[0], len(a[3]), [names.get(el[0]) for el in a[3][0][4]]))

# ---------------------------------------------------------------- build: clear the Foreground flag
bb = bytearray(open(BUILD, "rb").read())
o = 4
version, nsym, nfr = struct.unpack_from("<iii", bb, o); o += 12
n = struct.unpack_from("<i", bb, o)[0]; o += 4 + n
sym_offsets = []
for _ in range(nsym):
    sym_offsets.append(o)
    h, path, color, flags, nf = struct.unpack_from("<IIIii", bb, o); o += 20 + 44 * nf
nh = struct.unpack_from("<i", bb, o)[0]; o += 4
bnames = {}
for _ in range(nh):
    h = struct.unpack_from("<I", bb, o)[0]; o += 4
    n = struct.unpack_from("<i", bb, o)[0]; o += 4
    bnames[h] = bb[o:o+n].decode(); o += n
for so in sym_offsets:
    h, flags = struct.unpack_from("<I", bb, so)[0], struct.unpack_from("<i", bb, so + 12)[0]
    if flags & FG_FLAG:
        struct.pack_into("<i", bb, so + 12, flags & ~FG_FLAG)
        print("  cleared Foreground on", bnames.get(h, hex(h)))
open(BUILD, "wb").write(bb)
print("done")
