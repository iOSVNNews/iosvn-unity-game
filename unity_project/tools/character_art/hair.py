"""Hairstyles: a front layer (over the face), a back layer (behind head and body,
sways) and an untinted sheen.  Head-bone space, front view, crown near y=180."""
import math
from engine import Part, shift, scale_pts, mirror, spline

HAIR_SCALE = 1.7
BASE = 0.86
DARK = 0.52
STRAND = 0.40

MALE_NAMES = ["Búi tóc dài", "Đuôi ngựa cao", "Tóc ngắn", "Xõa rẽ ngôi", "Nửa búi",
              "Búi gọn", "Mái lệch dài", "Đuôi thấp", "Tóc hoang dã", "Đầu trọc"]
FEMALE_NAMES = ["Xõa dài", "Búi cao cài trâm", "Song búi", "Đuôi ngựa cao", "Nửa búi xõa",
                "Mái bằng dài", "Đuôi lệch vai", "Tóc ngang vai", "Bím tóc dài", "Búi thấp"]

# front fringe, back length, extras
STYLES = {
    "m": [("slick", "long", ["knot"]), ("part", "pony", []), ("messy", "short", []), ("part", "long", ["locks"]),
          ("part", "long", ["smallknot"]), ("slick", "short", ["knot"]), ("side", "long", []), ("slick", "lowtail", ["lock1"]),
          ("wild", "wild", []), ("shaved", None, [])],
    "f": [("curtain", "long", ["locks"]), ("slick", "short", ["bigbun", "pin"]), ("bangs", "mid", ["twinbuns"]),
          ("curtain", "pony", []), ("part", "long", ["smallknot", "pin"]), ("bangs", "long", ["locks"]),
          ("side", "sidepony", []), ("bangs", "bob", []), ("part", "braid", []), ("slick", "short", ["lowbun", "pin"])],
}


def _arc(cx, cy, rx, ry, a0, a1, n=24):
    return [(cx + rx * math.cos(math.radians(a)), cy + ry * math.sin(math.radians(a))) for a in
            [a0 + (a1 - a0) * i / (n - 1) for i in range(n)]]


def dims(g):
    if g == "m":
        return dict(cx=0, cy=100, rx=67, ry=92, temple=64, hairline=150, ear=86)
    return dict(cx=0, cy=96, rx=60, ry=86, temple=57, hairline=140, ear=82)


def _fringe(kind, d):
    """Inner edge of the front hair from the right temple to the left temple."""
    t, hl = d["temple"], d["hairline"]
    if kind == "slick":
        return [(t - 8, 112), (t - 20, 138), (22, hl + 2), (0, hl - 3), (-22, hl + 2), (-t + 20, 138), (-t + 8, 112)]
    if kind == "part":
        return [(t - 6, 92), (t - 14, 118), (t - 30, 140), (8, hl + 4), (2, hl - 4, 'c'), (-4, hl + 4), (-t + 28, 140), (-t + 14, 118), (-t + 6, 92)]
    if kind == "curtain":
        return [(t - 4, 70), (t - 10, 98), (t - 22, 124), (12, hl - 6), (2, hl - 2, 'c'), (-8, hl - 6), (-t + 22, 124), (-t + 10, 98), (-t + 4, 70)]
    if kind == "bangs":
        pts = [(t - 4, 66), (t - 7, 96), (t - 12, hl - 30)]
        n = 6
        for i in range(n + 1):
            x = (t - 16) - (2 * t - 32) * i / n
            pts.append((x, hl - 33 + (1.5 if i % 2 else -1.5)))
        pts += [(-t + 12, hl - 30), (-t + 7, 96), (-t + 4, 66)]
        return pts
    if kind == "side":
        return [(t - 4, 82), (t - 8, 104), (t - 22, 110, 'c'), (10, 122), (-14, 140), (-30, hl - 4), (-t + 16, 132), (-t + 6, 104)]
    if kind == "messy":
        pts = [(t - 6, 100)]
        n = 8
        for i in range(n + 1):
            x = (t - 14) - (2 * t - 28) * i / n
            y = hl - 18 + (8 if i % 2 else -6) + (4 if i in (3, 5) else 0)
            pts.append((x, y, 'c'))
        pts.append((-t + 6, 100))
        return pts
    if kind == "wild":
        pts = [(t - 2, 94)]
        n = 7
        for i in range(n + 1):
            x = (t - 10) - (2 * t - 20) * i / n
            y = hl - 26 + (14 if i % 2 else -4)
            pts.append((x, y, 'c'))
        pts.append((-t + 2, 94))
        return pts
    return []


def _locks(d, side, length):
    t = d["temple"]
    sx = 1 if side > 0 else -1
    return [(sx * (t + 2), 104), (sx * (t + 4), 70), (sx * (t + 1), length + 16), (sx * (t - 4), length, 'c'),
            (sx * (t - 9), length + 22), (sx * (t - 9), 74), (sx * (t - 6), 104)]


def front(g, idx):
    kind, back_kind, extras = STYLES[g][idx]
    d = dims(g)
    p = Part(f"hairF_{idx}", HAIR_SCALE)
    if kind == "shaved":
        cap = _arc(0, d["cy"], d["rx"] - 7, d["ry"] - 8, -12, 192)
        cap += [(-d["temple"] + 10, 118), (0, d["hairline"] - 4), (d["temple"] - 10, 118)]
        p.fill(cap, 0.70, 0.22)
        p.texture(0.4, 1.2, seed=idx)
        return p
    t = d["temple"]
    top = _arc(0, d["cy"], d["rx"], d["ry"] + (12 if kind == "wild" else 0), 6, 174)
    fr = _fringe(kind, d)
    low = min(fr[0][1], fr[-1][1], 96)
    right_side = [(t - 3, low - 6), (t + 2, (low + 112) / 2)]
    left_side = [(-t - 2, (low + 112) / 2), (-t + 3, low - 6)]
    outline = top + left_side + list(reversed(fr)) + right_side
    if kind == "wild":
        spikes = []
        for i, (x, y) in enumerate(top):
            if i % 3 == 1:
                spikes.append((x * 1.12, d["cy"] + (y - d["cy"]) * 1.12, 'c'))
            else:
                spikes.append((x, y))
        outline = spikes + left_side + list(reversed(fr)) + right_side
    p.fill(outline, BASE)
    # under-shadow along the fringe edge and the far (right) side
    p.shade(shift(fr, 0, -14), DARK, 0.75, blur=8, clip=outline)
    p.shade([(d["rx"] * .4, -20), (d["rx"] + 20, -20), (d["rx"] + 20, 260), (d["rx"] * .5, 260)], DARK, 0.45, blur=14, clip=outline)
    # strands from the crown / parting toward the fringe
    origin = (0, d["cy"] + d["ry"] - 6) if kind not in ("side",) else (-30, d["cy"] + d["ry"] - 10)
    pts = spline(fr, closed=False, samples=6)
    for i in range(0, len(pts), 2):
        x, y = pts[i]
        mid = ((origin[0] + x) / 2 + (x * .25), (origin[1] + y) / 2 + 18)
        p.stroke([origin, mid, (x, y)], 1.3, STRAND, 0.55, taper='both', shade_only=True)
    for a in range(-10, 195, 14):
        x = math.cos(math.radians(a)) * d["rx"] * .9
        y = d["cy"] + math.sin(math.radians(a)) * d["ry"] * .9
        p.stroke([origin, ((origin[0] + x) / 2, (origin[1] + y) / 2 + 10), (x, y)], 1.0, STRAND, 0.35, taper='both', shade_only=True)
    if "locks" in extras or "lock1" in extras:
        for side in ((1, -1) if "locks" in extras else (1,)):
            lk = _locks(d, side, 20 if g == "m" else 4)
            p.fill(lk, BASE)
            p.shade(shift(lk, side * 6, 0), DARK, 0.6, blur=5, clip=lk)
            p.stroke([(side * (d["temple"] + 1), 100), (side * (d["temple"] - 2), 60), (side * (d["temple"] - 4), 26)], 1.0, STRAND, 0.6, taper='both', shade_only=True)
    # top ornaments that belong to the hair
    top_y = d["cy"] + d["ry"]
    if "knot" in extras or "smallknot" in extras or "bigbun" in extras:
        s = 1.0 if "knot" in extras else (0.75 if "smallknot" in extras else 1.35)
        k = [(-18 * s, top_y - 6), (-20 * s, top_y + 10 * s), (-12 * s, top_y + 24 * s), (0, top_y + 28 * s), (12 * s, top_y + 24 * s),
             (20 * s, top_y + 10 * s), (18 * s, top_y - 6)]
        p.fill(k, BASE)
        p.shade(shift(k, 8, -6), DARK, 0.6, blur=5, clip=k)
        for j in range(5):
            x = -14 * s + j * 7 * s
            p.stroke([(x, top_y + 2), (x * .5, top_y + 16 * s), (0, top_y + 26 * s)], 1.0, STRAND, 0.5, taper='both', shade_only=True)
        p.stroke([(-17 * s, top_y + 2), (0, top_y - 2), (17 * s, top_y + 2)], 2.4, 0.25, 0.9)   # tie
    if "twinbuns" in extras:
        for sx in (-1, 1):
            cx, cy = sx * 42, top_y - 14
            b = [(cx + 20 * math.cos(a / 12 * 6.283), cy + 18 * math.sin(a / 12 * 6.283)) for a in range(12)]
            p.fill(b, BASE)
            p.shade(shift(b, 8, -8), DARK, 0.6, blur=5, clip=b)
            for j in range(4):
                p.stroke(_arc(cx, cy, 14 - j * 3, 12 - j * 3, 30 + j * 40, 230 + j * 40, 10), 1.0, STRAND, 0.5, shade_only=True)
    if "pin" in extras:
        p.stroke([(-34, top_y - 2), (36, top_y + 22)], 2.2, 0.95, 1.0)
        p.ellipse(38, top_y + 23, 5, 5, 0.95)
    p.edge(0.25, 3, 0.25)
    p.stroke(outline, 1.1, 0.30, 0.55, closed=True)
    return p


def sheen(g, idx):
    kind, back_kind, extras = STYLES[g][idx]
    d = dims(g)
    p = Part(f"hairS_{idx}", HAIR_SCALE)
    if kind == "shaved":
        p.pad([(0, 0), (1, 1)])
        return p
    band = _arc(-6, d["cy"] + 10, d["rx"] * .78, d["ry"] * .72, 40, 150, 14)
    band2 = list(reversed(_arc(-6, d["cy"] + 10, d["rx"] * .66, d["ry"] * .58, 40, 150, 14)))
    p.fill(band + band2, (1, 1, 1), 0.20)
    p.soft(3)
    return p


def back(g, idx):
    kind, back_kind, extras = STYLES[g][idx]
    d = dims(g)
    p = Part(f"hairB_{idx}", HAIR_SCALE)
    if back_kind is None:
        p.pad([(0, 0), (1, 1)])
        return p
    rx, cy, ry = d["rx"], d["cy"], d["ry"]
    top = _arc(0, cy, rx + 2, ry + 2, 4, 176)
    L = -250 if g == "m" else -290
    if back_kind == "long":
        body = [(-rx - 2, 80), (-rx - 6, -20), (-rx - 2, L * .55), (-rx + 10, L * .85), (-40, L, 'c'), (-26, L * .92),
                (-12, L + 8, 'c'), (2, L * .94), (16, L + 2, 'c'), (30, L * .9), (rx - 18, L * .8), (rx - 8, L * .5), (rx - 2, -20), (rx, 80)]
    elif back_kind == "short":
        body = [(-rx - 2, 70), (-rx + 6, 30, 'c'), (-30, 46), (0, 40), (30, 46), (rx - 6, 30, 'c'), (rx + 2, 70)]
    elif back_kind == "mid":
        body = [(-rx - 6, 60), (-rx - 6, -40), (-rx + 6, -100, 'c'), (-24, -84), (0, -96, 'c'), (24, -84), (rx - 6, -100, 'c'), (rx + 6, -40), (rx + 6, 60)]
    elif back_kind == "bob":
        body = [(-rx - 8, 70), (-rx - 6, 10), (-rx, -10, 'c'), (-30, 0), (0, -6), (30, 0), (rx, -10, 'c'), (rx + 6, 10), (rx + 8, 70)]
    elif back_kind == "wild":
        body = [(-rx - 10, 60), (-rx - 24, -40, 'c'), (-rx - 8, -60), (-rx - 16, -150, 'c'), (-40, -130), (-30, -210, 'c'), (-10, -150),
                (6, -200, 'c'), (20, -140), (40, -170, 'c'), (rx - 4, -100), (rx + 12, -50, 'c'), (rx, 0), (rx + 8, 60)]
    elif back_kind in ("pony", "lowtail", "sidepony", "braid"):
        body = [(-rx + 2, 80), (-rx + 10, 40, 'c'), (0, 56), (rx - 10, 40, 'c'), (rx - 2, 80)]
    outline = top + body
    p.fill(outline, BASE * .92)
    p.shade([(-200, 70), (200, 70), (200, -400), (-200, -400)], DARK, 0.35, blur=40, clip=outline)
    p.shade([(rx * .3, -400), (300, -400), (300, 300), (rx * .4, 300)], DARK, 0.4, blur=16, clip=outline)
    for i in range(14):
        x = -rx + (2 * rx) * i / 13
        p.stroke([(x * .4, cy + ry - 4), (x * 1.05, 40), (x * .95, L * .7 if back_kind in ("long", "wild") else 20)], 1.0, STRAND, 0.45, taper='both', shade_only=True)
    # tails
    if back_kind in ("pony", "lowtail", "sidepony", "braid"):
        if back_kind == "pony":
            root, tip, w = (-34, cy + ry - 14), (-58, -220 if g == "m" else -250), 20
        elif back_kind == "lowtail":
            root, tip, w = (-30, 60), (-46, -230), 14
        elif back_kind == "sidepony":
            root, tip, w = (-50, 70), (-74, -170), 18
        else:
            root, tip, w = (-26, 50), (-40, -290), 13
        mid = ((root[0] + tip[0]) / 2 - 18, (root[1] + tip[1]) / 2)
        tail = [(root[0] - w * .6, root[1] + 8), (mid[0] - w, mid[1]), (tip[0] - 4, tip[1] + 30), (tip[0], tip[1], 'c'),
                (tip[0] + 8, tip[1] + 34), (mid[0] + w, mid[1]), (root[0] + w * .6, root[1] + 8)]
        p.fill(tail, BASE * .95)
        p.shade(shift(tail, 10, 0), DARK, 0.5, blur=6, clip=tail)
        if back_kind == "braid":
            n = 9
            for j in range(n):
                t = j / n
                x = root[0] + (tip[0] - root[0]) * t - math.sin(t * math.pi) * 14
                y = root[1] + (tip[1] - root[1]) * t
                p.stroke(_arc(x, y, w * .9 * (1 - t * .5), 10, 200, 340, 8), 1.4, STRAND, 0.8, shade_only=True)
        else:
            for j in range(6):
                o = (j - 2.5) * w * .25
                p.stroke([(root[0] + o * .4, root[1]), (mid[0] + o, mid[1]), (tip[0] + o * .2, tip[1] + 20)], 1.0, STRAND, 0.5, taper='both', shade_only=True)
        p.ellipse(root[0], root[1] + 4, w * .55, 5, 0.3)   # tie
        p.stroke(tail, 1.0, 0.3, 0.5, closed=True)
    if "lowbun" in extras:
        b = [(-30 + 26 * math.cos(a / 14 * 6.283), 70 + 20 * math.sin(a / 14 * 6.283)) for a in range(14)]
        p.fill(b, BASE)
        p.shade(shift(b, 8, -6), DARK, 0.6, blur=5, clip=b)
    p.edge(0.25, 4, 0.25)
    return p


def all_parts(g):
    parts = []
    for i in range(10):
        parts += [front(g, i), back(g, i), sheen(g, i)]
    return parts
