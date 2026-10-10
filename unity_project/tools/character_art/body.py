"""Robes, sleeves, hands, legs, boots, capes and hats.  Each part is drawn in the
local space of its bone (see rig.py); light comes from the upper left."""
import math
from engine import Part, shift, scale_pts, mirror, spline, rotate_pts
import rig

BODY_SCALE = 1.25
CLOTH = 0.93
CLOTH_SHADE = 0.70
CLOTH_DEEP = 0.55
FOLD = 0.45
LINE = 0.22
SKIN = 0.965
SKIN_SHADE = 0.82
SKIN_LINE = 0.50

ROBE_NAMES = ["Võ phục ngắn", "Đạo bào", "Kiếm khách bào", "Giáp nhẹ", "Nho sam", "Áo choàng"]
HAT_NAMES = ["Không", "Ngọc quan", "Đấu lạp", "Liên hoa quan", "Mạt ngạch", "Kim quan"]

# skirt length below the waist, hem flare, sleeve kind
ROBES = {
    0: dict(skirt=330, flare=1.05, sleeve="narrow", slit=True),
    1: dict(skirt=665, flare=1.30, sleeve="wide", slit=False),
    2: dict(skirt=525, flare=1.15, sleeve="bracer", slit=True),
    3: dict(skirt=395, flare=1.10, sleeve="bracer", slit=True, armor=True),
    4: dict(skirt=680, flare=1.40, sleeve="huge", slit=False),
    5: dict(skirt=630, flare=1.22, sleeve="narrow", slit=False, cape=True),
}


def dims(g):
    if g == "m":
        return dict(waist=66, chest=98, shoulder=124, neck=30, hip=74, top=255, collar=220)
    return dict(waist=48, chest=74, shoulder=90, neck=22, hip=70, top=245, collar=210)


def _folds(p, xs, y0, y1, clip, value=FOLD, alpha=0.55, wobble=6):
    for i, x in enumerate(xs):
        p.stroke([(x, y0), (x + wobble * (1 if i % 2 else -1), (y0 + y1) / 2), (x + wobble * .4, y1)], 1.6, value, alpha, taper='both', shade_only=True)


def torso(g, s):
    d = dims(g)
    r = ROBES[s]
    p = Part(f"torso_{s}", BODY_SCALE)
    W, C, S, T = d["waist"], d["chest"], d["shoulder"], d["top"]
    H = d["hip"]
    out = [(-H - 2, -92), (0, -104), (H + 2, -92), (H, -40), (W + 2, 30), (C, T * .5), (S, T - 22), (S - 10, T - 6), (d["neck"] + 6, T),
           (0, T - 18), (-d["neck"] - 4, T), (-S + 10, T - 6), (-S, T - 22), (-C, T * .5), (-W - 2, 30), (-H, -40)]
    if g == "f":
        out[5] = (C, T * .56)
        out[-3] = (-C, T * .56)
    p.fill(out, CLOTH)
    p.shade([(C * .25, -40), (S + 30, -40), (S + 30, T + 20), (C * .45, T + 20)], CLOTH_SHADE, 0.75, blur=18)
    p.shade([(-S - 30, -40), (-C * .8, -40), (-C * .8, T + 20), (-S - 30, T + 20)], CLOTH_SHADE, 0.3, blur=12)
    if g == "f":
        p.shade([(-C, T * .56), (C, T * .56), (C, T * .5), (-C, T * .5)], CLOTH_SHADE, 0.35, blur=8)
    _folds(p, [-W * .6, -W * .1, W * .45], 10, 110, out, alpha=.35)
    p.texture(0.05, 0.05, seed=20 + s)
    p.edge(0.3, 6, CLOTH_DEEP)
    p.stroke(out, 1.8, LINE, 0.8, closed=True)
    if r.get("armor"):
        plate = [(-C + 6, 60), (C - 6, 60), (C - 2, 140), (d["neck"] + 8, T - 26), (-d["neck"] - 8, T - 26), (-C + 2, 140)]
        p.fill(plate, 0.78)
        p.shade(shift(plate, 20, -10), 0.55, 0.6, blur=10, clip=plate)
        for k in range(4):
            y = 74 + k * 18
            p.stroke([(-C + 8, y), (0, y - 4), (C - 8, y)], 1.6, 0.30, 0.8)
        p.stroke(plate, 1.8, LINE, 0.9, closed=True)
    return p


def torso_trim(g, s):
    """Inner robe at the neck and the crossed lapels (tinted with the trim colour)."""
    d = dims(g)
    p = Part(f"torsoT_{s}", BODY_SCALE)
    T, n = d["top"], d["neck"]
    band = 10 if s != 4 else 16
    # inner robe visible inside the V
    inner = [(-n - 2, T - 2), (n + 2, T - 2), (n - 4, T - 40), (0, T - 74), (-n + 4, T - 40)]
    p.fill(inner, 0.98)
    p.shade(shift(inner, 0, 30), 0.80, 0.5, blur=8, clip=inner)
    # right-over-left crossed lapel bands down to the belt
    lap1 = [(-n - 4, T + 1), (-n - 4 + band, T + 1), (d["waist"] * .55 + band, 22), (d["waist"] * .55, 22)]
    lap2 = [(n + 4 - band, T + 1), (n + 4, T + 1), (-d["waist"] * .2 + band * .4, T - 92), (-d["waist"] * .2 - band * .6, T - 96)]
    p.fill(lap2, 0.92, smooth=False)
    p.fill(lap1, 0.96, smooth=False)
    p.stroke([lap1[0], lap1[3]], 1.4, LINE, 0.8, smooth=False)
    p.stroke([lap1[1], lap1[2]], 1.4, LINE, 0.8, smooth=False)
    p.stroke([lap2[1], lap2[2]], 1.2, LINE, 0.6, smooth=False)
    if s == 4:  # scholar robe: wide collar band
        p.stroke([(-d["shoulder"] + 16, T - 10), (-n - 6, T + 2)], 6, 0.92, 1.0)
    return p


def belt(g, s):
    d = dims(g)
    p = Part(f"belt_{s}", BODY_SCALE)
    W = d["waist"] + 6
    h = 26 if s not in (3,) else 22
    band = [(-W, -6), (W, -6), (W + 1, h - 6), (-W - 1, h - 6)]
    p.fill(band, 0.90, smooth=False)
    p.shade([(-W, -10), (W + 4, -10), (W + 4, 2), (-W, 2)], 0.65, 0.6, blur=4, clip=band)
    p.shade([(W * .4, -10), (W + 6, -10), (W + 6, 30), (W * .4, 30)], 0.7, 0.6, blur=8, clip=band)
    p.stroke(band, 1.4, LINE, 0.8, closed=True, smooth=False)
    if s in (1, 2, 4, 5):   # knotted sash ends hanging on the near side
        tail = [(-W * .35, 2), (-W * .15, 2), (-W * .05, -70), (-W * .2, -96, 'c'), (-W * .35, -70)]
        tail2 = [(-W * .2, 2), (0, 2), (W * .15, -60), (W * .05, -82, 'c'), (-W * .1, -60)]
        p.fill(tail2, 0.80)
        p.fill(tail, 0.88)
        p.stroke(tail, 1.2, LINE, 0.7, closed=True)
        p.ellipse(-W * .2, 6, 7, 6, 0.85)
    if s in (0, 3):   # metal buckle
        p.ellipse(0, h / 2 - 6, 8, 7, 1.0)
        p.stroke([(-6, h / 2 - 6), (6, h / 2 - 6)], 1.2, 0.3, 0.8)
    if s == 2:   # jade pendant
        p.stroke([(W * .45, 0), (W * .5, -40)], 1.0, 0.4, 0.8)
        p.ellipse(W * .5, -48, 6, 8, 0.95)
    return p


def skirt_outline(g, s, back=False):
    d = dims(g)
    r = ROBES[s]
    L = r["skirt"] * (0.95 if g == "f" else 1.0)
    W = d["waist"] + 4
    hem = d["hip"] * r["flare"] * (1.08 if g == "f" else 1.0)
    if back:
        hem *= 1.08
    pts = [(-W, 6), (W, 6), (d["hip"] + 2, -60), (hem, -L + 12), (hem * .55, -L - 2), (0, -L + 4), (-hem * .55, -L - 2), (-hem, -L + 12), (-d["hip"] - 2, -60)]
    return pts, L, hem


def skirt_front(g, s):
    p = Part(f"skirtF_{s}", BODY_SCALE)
    r = ROBES[s]
    pts, L, hem = skirt_outline(g, s)
    if r["slit"]:
        # split front: two panels with the legs showing in between
        left = [pts[0], (4, 6), (6, -L * .35), (-hem * .15, -L + 2), (-hem * .55, -L - 2), (-hem, -L + 12), (-dims(g)["hip"] - 2, -60)]
        right = [(10, 6), pts[1], pts[2], (hem, -L + 12), (hem * .6, -L - 2), (hem * .2, -L + 4), (14, -L * .35)]
        p.fill(right, CLOTH * .96)
        p.fill(left, CLOTH)
        outline = left
        p.shade([(0, 20), (hem + 20, 20), (hem + 20, -L - 20), (0, -L - 20)], CLOTH_SHADE, 0.55, blur=16, clip=right)
        p.stroke(right, 1.6, LINE, 0.8, closed=True)
        p.stroke(left, 1.6, LINE, 0.8, closed=True)
    else:
        p.fill(pts, CLOTH)
        p.shade([(hem * .2, 20), (hem + 30, 20), (hem + 30, -L - 20), (hem * .3, -L - 20)], CLOTH_SHADE, 0.6, blur=18)
        p.stroke(pts, 1.6, LINE, 0.8, closed=True)
        # wrap-over seam of the crossed robe
        p.stroke([(dims(g)["waist"] * .55, 4), (dims(g)["waist"] * .7, -L * .5), (hem * .55, -L)], 1.4, LINE, 0.6)
    _folds(p, [-hem * .55, -hem * .2, hem * .25, hem * .6], -30, -L + 20, pts, alpha=.45, wobble=10)
    p.shade([(-200, 30), (200, 30), (200, -10), (-200, -10)], CLOTH_SHADE, 0.45, blur=8)  # under the belt
    p.texture(0.05, 0.05, seed=40 + s)
    p.edge(0.3, 6, CLOTH_DEEP)
    return p


def skirt_trim(g, s):
    """Hem band (trim colour)."""
    p = Part(f"skirtT_{s}", BODY_SCALE)
    pts, L, hem = skirt_outline(g, s)
    band = 14 if s in (1, 4, 5) else 10
    hemline = [(-hem, -L + 12), (-hem * .55, -L - 2), (0, -L + 4), (hem * .55, -L - 2), (hem, -L + 12)]
    upper = [(x * .985, y + band) for x, y in reversed(hemline)]
    shape = hemline + upper
    if ROBES[s]["slit"]:
        p.fill(hemline[:2] + [(-hem * .15, -L + 2), (-hem * .15, -L + 2 + band)] + [(x * .985, y + band) for x, y in reversed(hemline[:2])], 0.95)
        p.fill([(hem * .2, -L + 4), (hem * .6, -L - 2), (hem, -L + 12), (hem * .985, -L + 12 + band), (hem * .6, -L - 2 + band), (hem * .2, -L + 4 + band)], 0.9)
    else:
        p.fill(shape, 0.95)
    p.shade([(0, -L - 30), (hem + 20, -L - 30), (hem + 20, -L + 40), (0, -L + 40)], 0.75, 0.5, blur=10)
    return p


def skirt_back(g, s):
    p = Part(f"skirtB_{s}", BODY_SCALE)
    pts, L, hem = skirt_outline(g, s, back=True)
    pts = [(x, y - 4 if y < -40 else y) for x, y in pts]
    p.fill(pts, CLOTH_SHADE)
    p.shade([(-300, -L + 80), (300, -L + 80), (300, 40), (-300, 40)], CLOTH_DEEP, 0.4, blur=30)
    _folds(p, [-hem * .4, 0, hem * .4], -40, -L + 20, pts, value=0.4, alpha=.5)
    p.stroke(pts, 1.4, LINE, 0.6, closed=True)
    return p


def sleeve_upper(g, s):
    p = Part(f"sleeveU_{s}", BODY_SCALE)
    r = ROBES[s]
    L = rig.length(g, "armN_up", "armN_lo")
    w0 = 31 if g == "m" else 25
    w1 = {"narrow": 18, "bracer": 18, "wide": 30, "huge": 36}[r["sleeve"]] * (1 if g == "m" else .85)
    pts = [(-w0 + 2, 10), (0, 18), (w0 - 2, 10), (w1, -L), (-w1, -L - 4)]
    p.fill(pts, CLOTH)
    p.shade([(w0 * .2, 30), (w0 + 30, 30), (w1 + 30, -L - 10), (w1 * .2, -L - 10)], CLOTH_SHADE, 0.6, blur=10)
    _folds(p, [-w0 * .3, w0 * .3], 0, -L + 10, pts, alpha=.4, wobble=3)
    p.edge(0.3, 5, CLOTH_DEEP)
    p.stroke(pts, 1.6, LINE, 0.8, closed=True)
    return p


def sleeve_lower(g, s):
    """Forearm sleeve; wide sleeves hang in a bell below the wrist."""
    p = Part(f"sleeveL_{s}", BODY_SCALE)
    r = ROBES[s]
    L = rig.length(g, "armN_lo", "handN")
    k = 1 if g == "m" else .85
    kind = r["sleeve"]
    if kind in ("narrow", "bracer"):
        w0, w1 = 23 * k, 19 * k
        pts = [(-w0, 8), (w0, 8), (w1, -L + 6), (-w1, -L + 6)]
    elif kind == "wide":
        w0 = 30 * k
        pts = [(-w0, 8), (w0, 8), (w0 + 14, -L * .5), (w0 + 18, -L - 20), (w0 - 4, -L - 52), (-w0 * .2, -L - 40, 'c'), (-w0 + 4, -L + 4)]
    else:  # huge scholar sleeve
        w0 = 36 * k
        pts = [(-w0, 8), (w0, 8), (w0 + 24, -L * .5), (w0 + 30, -L - 30), (w0, -L - 80), (-w0 * .1, -L - 64, 'c'), (-w0 + 6, -L + 10)]
    p.fill(pts, CLOTH)
    p.shade([(0, 20), (90, 20), (90, -L - 120), (0, -L - 120)], CLOTH_SHADE, 0.6, blur=12)
    _folds(p, [-6, 8], 0, -L - 10, pts, alpha=.45, wobble=5)
    p.edge(0.3, 5, CLOTH_DEEP)
    p.stroke(pts, 1.6, LINE, 0.8, closed=True)
    return p


def cuff(g, s):
    p = Part(f"cuffT_{s}", BODY_SCALE)
    r = ROBES[s]
    L = rig.length(g, "armN_lo", "handN")
    k = 1 if g == "m" else .85
    if r["sleeve"] == "bracer":
        b = [(-16 * k, -L * .25), (16 * k, -L * .25), (15 * k, -L + 4), (-15 * k, -L + 4)]
        p.fill(b, 0.85, smooth=False)
        for j in range(3):
            y = -L * .35 - j * 24
            p.stroke([(-15 * k, y), (15 * k, y - 2)], 1.4, 0.35, 0.8)
        p.stroke(b, 1.4, LINE, 0.8, closed=True, smooth=False)
    elif r["sleeve"] == "narrow":
        b = [(-16 * k, -L + 22), (16 * k, -L + 22), (15 * k, -L + 6), (-15 * k, -L + 6)]
        p.fill(b, 0.95, smooth=False)
        p.stroke(b, 1.2, LINE, 0.7, closed=True, smooth=False)
    else:
        w0 = (30 if r["sleeve"] == "wide" else 36) * k
        off = 52 if r["sleeve"] == "wide" else 80
        edge = [(w0 + (18 if off == 52 else 30), -L - (20 if off == 52 else 30)), (w0 - 4 if off == 52 else w0, -L - off), (-w0 * .2 if off == 52 else -w0 * .1, -L - off + 12)]
        p.stroke(edge, 9, 0.95, 1.0)
    return p


def hand(g, kind):
    p = Part(f"hand_{kind}", BODY_SCALE)
    k = 1 if g == "m" else .86
    if kind == "open":
        pts = [(-9 * k, 4), (9 * k, 4), (11 * k, -18 * k), (8 * k, -40 * k), (2 * k, -46 * k), (-5 * k, -40 * k), (-10 * k, -20 * k)]
        thumb = [(7 * k, -6 * k), (15 * k, -16 * k), (14 * k, -30 * k), (9 * k, -24 * k)]
        p.fill(pts, SKIN)
        p.fill(thumb, SKIN)
        p.shade(shift(pts, 8, 0), SKIN_SHADE, 0.6, blur=4, clip=pts)
        p.stroke(pts, 1.3, SKIN_LINE, 0.8, closed=True)
        p.stroke(thumb, 1.1, SKIN_LINE, 0.7, closed=True)
        for x in (-4, 1, 5):
            p.stroke([(x * k, -26 * k), (x * k, -42 * k)], 0.8, SKIN_LINE, 0.5, taper='both')
    else:  # fist gripping a handle (handle runs along x through (0,-22))
        pts = [(-11 * k, 4), (11 * k, 4), (14 * k, -14 * k), (13 * k, -32 * k), (-4 * k, -36 * k), (-13 * k, -26 * k), (-13 * k, -10 * k)]
        p.fill(pts, SKIN)
        p.shade(shift(pts, 8, -4), SKIN_SHADE, 0.6, blur=4, clip=pts)
        p.stroke(pts, 1.3, SKIN_LINE, 0.8, closed=True)
        for y in (-14, -22, -30):
            p.stroke([(-12 * k, y * k), (0, (y - 1) * k), (12 * k, y * k)], 0.9, SKIN_LINE, 0.55, taper='both')
    return p


def thigh(g):
    p = Part("thigh", BODY_SCALE)
    L = rig.length(g, "legN_up", "legN_lo")
    w0, w1 = (42, 30) if g == "m" else (38, 27)
    pts = [(-w0, 16), (w0, 16), (w1 + 2, -L * .6), (w1, -L - 6), (-w1, -L - 6), (-w1 - 2, -L * .6)]
    p.fill(pts, CLOTH)
    p.shade([(w0 * .1, 30), (60, 30), (60, -L - 20), (w1 * .1, -L - 20)], CLOTH_SHADE, 0.6, blur=10)
    _folds(p, [-8, 8], 0, -L, pts, alpha=.35, wobble=4)
    p.edge(0.3, 5, CLOTH_DEEP)
    p.stroke(pts, 1.5, LINE, 0.8, closed=True)
    return p


def shin(g):
    p = Part("shin", BODY_SCALE)
    L = rig.length(g, "legN_lo", "legN_lo") or 0
    A = rig.WORLD[g]["legN_lo"][1] - 50
    w0, w1 = (30, 22) if g == "m" else (26, 19)
    pts = [(-w0, 12), (w0, 12), (w1 + 2, -A * .5), (w1, -A + 40), (-w1, -A + 40), (-w1 - 1, -A * .5)]
    p.fill(pts, CLOTH)
    p.shade([(w0 * .1, 30), (60, 30), (60, -A), (w1 * .1, -A)], CLOTH_SHADE, 0.6, blur=10)
    p.edge(0.3, 5, CLOTH_DEEP)
    p.stroke(pts, 1.5, LINE, 0.8, closed=True)
    return p


def boot(g):
    p = Part("boot", BODY_SCALE)
    A = rig.WORLD[g]["legN_lo"][1] - 50
    sole = -A - 40 + (0 if g == "m" else 2)
    k = 1 if g == "m" else .88
    pts = [(-17 * k, -A + 70), (17 * k, -A + 70), (16 * k, -A + 10), (40 * k, sole + 16), (44 * k, sole + 4), (38 * k, sole), (-18 * k, sole), (-19 * k, sole + 14)]
    p.fill(pts, 0.88)
    p.shade(shift(pts, 10, 0), 0.6, 0.6, blur=6, clip=pts)
    p.fill([(-19 * k, sole + 6), (44 * k, sole + 6), (44 * k, sole), (-19 * k, sole)], 0.35, smooth=False, clip=pts)
    p.stroke([(-17 * k, -A + 60), (17 * k, -A + 62)], 1.4, LINE, 0.6)
    p.stroke(pts, 1.5, LINE, 0.85, closed=True)
    return p


def cape(g, s):
    p = Part(f"cape_{s}", BODY_SCALE)
    if not ROBES[s].get("cape"):
        p.pad([(0, 0), (1, 1)])
        return p
    d = dims(g)
    S = d["shoulder"] + 8
    L = 730 if g == "m" else 690
    pts = [(-S + 6, 50), (S - 6, 50), (S + 26, -L * .5), (S + 40, -L + 30), (S * .3, -L - 6), (-S * .4, -L + 10), (-S - 30, -L + 30), (-S - 18, -L * .5)]
    p.fill(pts, CLOTH * .9)
    p.shade([(-300, -L * .2), (300, -L * .2), (300, -L - 40), (-300, -L - 40)], CLOTH_DEEP, 0.35, blur=40)
    _folds(p, [-S * .6, -S * .1, S * .4, S * .8], 30, -L + 30, pts, alpha=.5, wobble=12)
    p.edge(0.3, 6, CLOTH_DEEP)
    p.stroke(pts, 1.6, LINE, 0.8, closed=True)
    return p


def shoulder_plate(g, s):
    p = Part(f"plate_{s}", BODY_SCALE)
    if not ROBES[s].get("armor"):
        p.pad([(0, 0), (1, 1)])
        return p
    k = 1 if g == "m" else .85
    pts = [(-30 * k, 18), (0, 30 * k), (30 * k, 18), (34 * k, -14), (22 * k, -36 * k), (-22 * k, -36 * k), (-34 * k, -14)]
    p.fill(pts, 0.82)
    p.shade(shift(pts, 14, -6), 0.55, 0.6, blur=6, clip=pts)
    for y in (-6, -20):
        p.stroke([(-30 * k, y), (0, y + 8), (30 * k, y)], 1.4, 0.3, 0.8)
    p.stroke(pts, 1.6, LINE, 0.9, closed=True)
    return p


# ---------------------------------------------------------------- hats (head-bone space, tinted "hac")

def hat(g, h):
    p = Part(f"hat_{h}", 1.7)
    top = 192 if g == "m" else 180
    if h == 0:
        p.pad([(0, 0), (1, 1)])
        return p
    if h == 1:   # small jade crown over the topknot
        c = [(-16, top), (16, top), (14, top + 22), (6, top + 30), (0, top + 40, 'c'), (-6, top + 30), (-14, top + 22)]
        p.fill(c, 0.92)
        p.shade(shift(c, 8, 0), 0.65, 0.6, blur=4, clip=c)
        p.stroke(c, 1.4, LINE, 0.8, closed=True)
        p.stroke([(-34, top + 12), (34, top + 14)], 2.4, 0.95, 1.0)
        p.ellipse(36, top + 14, 4, 4, 0.95)
    elif h == 2:  # conical bamboo hat
        c = [(-118, top - 52), (0, top + 26, 'c'), (118, top - 52), (100, top - 60), (0, top - 50), (-100, top - 60)]
        p.fill(c, 0.90)
        p.shade([(0, top + 40), (140, top + 40), (140, top - 80), (0, top - 80)], 0.68, 0.6, blur=16, clip=c)
        for a in range(-100, 101, 20):
            p.stroke([(0, top + 22), (a, top - 54)], 1.0, 0.45, 0.6, taper='start', shade_only=True)
        p.stroke(c, 1.6, LINE, 0.85, closed=True)
        p.stroke([(-56, top - 54), (-58, top - 120)], 1.0, 0.6, 0.6)
        p.stroke([(56, top - 54), (58, top - 120)], 1.0, 0.6, 0.6)
    elif h == 3:  # lotus crown
        for a, sc in ((-50, .8), (50, .8), (-24, 1), (24, 1), (0, 1.15)):
            petal = rotate_pts([(0, top + 36 * sc, 'c'), (10 * sc, top + 14), (0, top - 4, 'c'), (-10 * sc, top + 14)], a, 0, top)
            p.fill(petal, 0.92)
            p.stroke(petal, 1.2, LINE, 0.7, closed=True)
        p.fill([(-24, top - 6), (24, top - 6), (20, top + 6), (-20, top + 6)], 0.85)
    elif h == 4:  # headband with ribbon tails
        y = (150 if g == "m" else 140)
        r = 70 if g == "m" else 62
        band = [(-r, y - 10), (-r * .5, y + 4), (0, y + 7), (r * .5, y + 4), (r, y - 10), (r, y - 24), (r * .5, y - 10), (0, y - 7), (-r * .5, y - 10), (-r, y - 24)]
        p.fill(band, 0.92)
        p.shade([(0, y - 40), (90, y - 40), (90, y + 20), (0, y + 20)], 0.7, 0.5, blur=8, clip=band)
        p.ellipse(0, y - 2, 7, 7, 0.98)
        p.stroke(band, 1.2, LINE, 0.7, closed=True)
        tail = [(-r + 2, y - 14), (-r - 26, y - 70), (-r - 18, y - 120, 'c'), (-r - 8, y - 66), (-r + 8, y - 22)]
        p.fill(tail, 0.86)
        p.stroke(tail, 1.0, LINE, 0.6, closed=True)
    elif h == 5:  # gold crown with long hairpin
        c = [(-22, top - 4), (22, top - 4), (26, top + 18), (14, top + 26, 'c'), (8, top + 16), (0, top + 34, 'c'), (-8, top + 16), (-14, top + 26, 'c'), (-26, top + 18)]
        p.fill(c, 0.95)
        p.shade(shift(c, 10, -4), 0.62, 0.6, blur=5, clip=c)
        p.stroke(c, 1.4, LINE, 0.85, closed=True)
        p.stroke([(-52, top + 2), (54, top + 10)], 3.0, 0.92, 1.0)
        p.ellipse(56, top + 10, 5, 5, 0.95)
        p.ellipse(0, top + 8, 4, 4, 0.75)
    return p


def all_parts(g):
    parts = []
    for s in range(6):
        parts += [torso(g, s), torso_trim(g, s), belt(g, s), skirt_front(g, s), skirt_trim(g, s), skirt_back(g, s),
                  sleeve_upper(g, s), sleeve_lower(g, s), cuff(g, s), cape(g, s), shoulder_plate(g, s)]
    parts += [hand(g, "open"), hand(g, "fist"), thigh(g), shin(g), boot(g)]
    for h in range(6):
        parts.append(hat(g, h))
    return parts
