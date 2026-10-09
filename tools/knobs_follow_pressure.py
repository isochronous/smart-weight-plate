"""Rewrite smart_weight_plate_anim.bytes so the knobs follow pressed/released and the button draws behind.

Klei anim format: "ANIM", int version, int elementCount, int frameCount, int animCount,
  per anim: str name, uint hash, float rate, int frameCount,
    per frame: float x, y, w, h, int elementCount,
      per element: uint symbol, int frame, uint folder, int flags, float a, b, g, r, float m1..m6, float order
  int maxVisSymbolFrames, int hashCount, per hash: uint hash, str name.
Strings are int length + bytes.
"""
import struct, sys

path = sys.argv[1]
b = open(path, "rb").read()
o = 0

def rs():
    global o
    n = struct.unpack_from("<i", b, o)[0]; o += 4
    s = b[o:o+n].decode(); o += n
    return s

assert b[:4] == b"ANIM"; o = 4
version, element_count, frame_count, anim_count = struct.unpack_from("<iiii", b, o); o += 16
anims = []
for _ in range(anim_count):
    name = rs()
    hsh, rate, nframes = struct.unpack_from("<Ifi", b, o); o += 12
    frames = []
    for _f in range(nframes):
        x, y, w, h, nel = struct.unpack_from("<ffffi", b, o); o += 20
        els = []
        for _e in range(nel):
            el = list(struct.unpack_from("<IiIifffffffffff", b, o)); o += 60
            els.append(el)   # [0 sym, 1 frame, 2 folder, 3 flags, 4-7 a b g r, 8-13 m1..m6, 14 order]
        frames.append([x, y, w, h, els])
    anims.append([name, hsh, rate, frames])
max_vis = struct.unpack_from("<i", b, o)[0]; o += 4
nh = struct.unpack_from("<i", b, o)[0]; o += 4
hashes = []
for _ in range(nh):
    h = struct.unpack_from("<I", b, o)[0]; o += 4
    hashes.append((h, rs()))
assert o == len(b), (o, len(b))
names = dict(hashes)
by_name = {a[0]: a for a in anims}

def sym_is(el, n): return names.get(el[0]) == n
LIGHT_OFF, LIGHT_ON = 0, 1

def copy_frames(frames, light_frame):
    out = []
    for x, y, w, h, els in frames:
        new_els = []
        for el in els:
            el = list(el)
            if sym_is(el, "light_fg"):
                el[1] = light_frame
            new_els.append(el)
        out.append([x, y, w, h, new_els])
    return out

# 1. Knobs follow pressed: the off_ transitions get the on_ transitions' 20 frames with the red light.
by_name["off_down_pre"][3] = copy_frames(by_name["on_down_pre"][3], LIGHT_OFF)
by_name["off_up_pre"][3] = copy_frames(by_name["on_up_pre"][3], LIGHT_OFF)

# 2. Both pressed idles hold the end-of-press knob positions.
end_of_press = {el[1]: el[12] for el in by_name["on_down_pre"][3][-1][4] if sym_is(el, "slider_fg")}  # knob frame -> m5
for idle in ("on_down", "off_down"):
    for el in by_name[idle][3][0][4]:
        if sym_is(el, "slider_fg"):
            el[12] = end_of_press[el[1]]

# 3. Button behind everything: first in every frame's element list.
for name, hsh, rate, frames in anims:
    for fr in frames:
        els = fr[4]
        btn = [el for el in els if sym_is(el, "button")]
        rest = [el for el in els if not sym_is(el, "button")]
        fr[4] = btn + rest

# Serialise.
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
open(path, "wb").write(out)
print("rewritten: elements %d -> %d, frames %d -> %d" % (element_count, total_elements, frame_count, total_frames))
for a in anims:
    first = a[3][0][4]
    print("  %-13s %2d frames  order: %s" % (a[0], len(a[3]), [names.get(el[0]) for el in first]))
