"""Heads, ears, necks and facial features, drawn front-on and symmetric.

Coordinates are head-bone space (origin under the chin, y up).  Paired features
(eyes, brows) are painted once for the screen-right side; the runtime mirrors
them for the other side, so faces are always symmetric.
"""
import math
from engine import Part, mirror, shift, scale_pts, rotate_pts, spline

SKIN = 0.965
SKIN_LIGHT = 1.0
SKIN_SHADE = 0.86
SKIN_DEEP = 0.76
SKIN_LINE = 0.50
INK = (0.11, 0.08, 0.08)
HEAD_SCALE = 2.6
FEATURE_SCALE = 3.2


def _sym(right):
    """Right half from the chin (x=0) up to the crown (x=0) -> closed outline."""
    left = [(-x, y) for x, y in reversed(right[1:-1])]
    return right + left


def head_half(g, fa):
    R = [(0, -2), (10, 2), (26, 14), (40, 33), (49, 56), (54, 82), (56, 104), (54, 132), (44, 158), (24, 174), (0, 180)]
    if fa == 0:    # oval, tapering chin
        R = [(0, -6), (8, -2), (23, 12), (37, 32), (47, 56), (53, 82), (55, 104), (53, 132), (43, 158), (24, 174), (0, 180)]
    elif fa == 2:  # broad, square jaw
        R = [(0, 1), (16, 3), (36, 13), (50, 32), (56, 56), (59, 82), (59, 104), (56, 132), (45, 158), (24, 174), (0, 180)]
    elif fa == 3:  # round, full cheeks
        R = [(0, 3), (16, 7), (36, 21), (50, 42), (57, 64), (59, 86), (59, 106), (56, 132), (45, 158), (24, 174), (0, 180)]
    if g == "f":
        R = [(x * .91, (y - 90) * .94 + 88) for x, y in R]
        R[0] = (0, R[0][1] + 1)
        R[1] = (R[1][0] * .8, R[1][1] + 1)
    return R


def head_outline(g, fa):
    return _sym(head_half(g, fa))


def head(g, fa):
    p = Part(f"head_{fa}", HEAD_SCALE)
    R = head_half(g, fa)
    poly = spline(head_outline(g, fa), True, samples=12)
    p.fill(poly, SKIN, smooth=False)
    w = R[6][0]
    # light from the upper left: right cheek and temple turn into shade
    p.shade([(w * .35, -20), (w + 30, -20), (w + 30, 190), (w * .55, 190), (w * .62, 100)], SKIN_SHADE, 0.75, blur=12)
    p.shade([(-w - 30, -20), (-w * .6, -20), (-w * .78, 100), (-w * .7, 190), (-w - 30, 190)], SKIN_SHADE, 0.35, blur=10)
    # soft sockets under the brow ridge
    eye_y = 86 if g == "m" else 82
    for sx in (-1, 1):
        p.shade([(sx * 8, eye_y + 8), (sx * 40, eye_y + 12), (sx * 44, eye_y - 2), (sx * 10, eye_y - 4)], SKIN_SHADE, 0.30 if sx > 0 else 0.18, blur=7)
    # forehead / nose-bridge / chin highlights
    p.shade([(-24, 118), (14, 120), (12, 150), (-22, 148)], SKIN_LIGHT, 0.55, blur=12)
    p.shade([(-6, 60), (4, 60), (4, 100), (-6, 100)], SKIN_LIGHT, 0.6, blur=5)
    p.shade([(-10, 6), (8, 6), (8, 16), (-10, 16)], SKIN_LIGHT, 0.5, blur=4)
    # under the jaw
    p.shade([(-60, -30), (60, -30), (56, 20), (20, 4), (-20, 4), (-56, 20)], SKIN_DEEP, 0.55, blur=6)
    p.texture(0.02, 0.08, seed=11 + fa)
    p.edge(0.30, 4, SKIN_DEEP)
    # ink contour: firm along the jaw, fading toward the hairline
    n = len(poly)
    jaw = poly[-n // 3:] + poly[:n // 3]
    p.stroke(jaw, 1.6, SKIN_LINE, taper='both', smooth=False)
    # cheekbone shading for a leaner face
    for sx in (-1, 1):
        p.shade([(sx * 26, 44), (sx * 52, 60), (sx * 52, 34), (sx * 30, 24)], SKIN_SHADE, 0.35, blur=9)
    return p


def ears(g, fa):
    p = Part(f"ears_{fa}", HEAD_SCALE)
    R = head_half(g, fa)
    x0 = R[6][0] - 6
    cy = 88 if g == "m" else 84
    s = 1.0 if g == "m" else .88
    for sx in (-1, 1):
        pts = [(sx * x0, cy + 16 * s), (sx * (x0 + 9 * s), cy + 20 * s), (sx * (x0 + 14 * s), cy + 8 * s),
               (sx * (x0 + 12 * s), cy - 8 * s), (sx * (x0 + 6 * s), cy - 18 * s), (sx * (x0 - 2), cy - 20 * s)]
        p.fill(pts, SKIN)
        p.shade(shift(scale_pts(pts, .5, .62, sx * (x0 + 6 * s), cy), -sx * 1, 0), SKIN_DEEP, 0.55, blur=2)
        p.stroke(pts[:-1], 1.5, SKIN_LINE, 0.8, taper='end')
    return p


def neck(g):
    p = Part("neck", HEAD_SCALE)
    hw = 29 if g == "m" else 22
    pts = [(-hw - 4, -30), (hw + 4, -30), (hw + 1, 0), (hw - 1, 30), (hw, 50), (-hw, 50), (-hw + 1, 30), (-hw - 1, 0)]
    p.fill(pts, SKIN)
    p.shade([(-50, 26), (50, 26), (50, 70), (-50, 70)], SKIN_DEEP, 0.8, blur=7)   # under the chin
    p.shade([(hw * .3, -30), (hw + 10, -30), (hw + 10, 60), (hw * .3, 60)], SKIN_SHADE, 0.55, blur=5)
    p.stroke([(hw + 2, 50), (hw, 10), (hw + 3, -22)], 1.4, SKIN_LINE, 0.6, taper='both')
    p.stroke([(-hw - 2, 50), (-hw, 10), (-hw - 3, -22)], 1.4, SKIN_LINE, 0.4, taper='both')
    if g == "m":
        p.stroke([(-3, 20), (0, 14), (3, 20)], 1.0, SKIN_LINE, 0.35, taper='both')
    return p


def blush(g):
    p = Part("blush", HEAD_SCALE)
    a = 0.04 if g == "m" else 0.16
    y = 56 if g == "m" else 54
    for sx in (-1, 1):
        cheek = [(sx * 30 + 13 * math.cos(t / 20 * 6.283), y + 7 * math.sin(t / 20 * 6.283)) for t in range(20)]
        p.fill(cheek, (0.96, 0.56, 0.52), a)
    p.ellipse(0, 62 if g == "m" else 58, 4, 4, (0.96, 0.60, 0.56), a * .6)
    p.soft(5.0)
    return p


# ---------------------------------------------------------------- eyes (screen-right eye: inner corner -x, outer +x)

EYE_STYLES = [  # width, opening, outer-corner lift, peak position, lid weight
    (30, 7.5, 3.0, -.10, 2.4),  # 0 narrow
    (26, 8.0, 1.5, -.08, 2.3),  # 1 small
    (31, 9.0, 4.0, -.12, 2.5),  # 2 almond
    (30, 9.5, 2.0, -.08, 2.5),  # 3 balanced
    (31, 10.5, 2.5, -.06, 2.6),  # 4 bright
    (32, 11.5, 2.0, -.04, 2.7),  # 5 large
    (28, 12.0, 0.5, .00, 2.5),  # 6 round
    (34, 8.0, 6.5, -.16, 2.8),  # 7 phoenix
]


def eye_geometry(g, ey):
    w, h, tilt, peak, lid = EYE_STYLES[ey]
    if g == "f":
        h *= 1.12; w *= 1.02
    I = (-w / 2, 0.0)
    O = (w / 2, tilt)
    top = h * 0.66
    bot = h * 0.34
    upper = [I, (-w * .28, top * .82), (w * peak, top + tilt * .3), (w * .26, top * .78 + tilt * .7), O]
    lower = [O, (w * .30, -bot * .55 + tilt * .5), (w * .02, -bot), (-w * .30, -bot * .75), I]
    shape = [(I[0], I[1], 'c')] + upper[1:-1] + [(O[0], O[1], 'c')] + lower[1:-1]
    return w, h, top, bot, upper, lower, shape, lid


def eye_white(g, ey):
    p = Part(f"eyeW_{ey}", FEATURE_SCALE)
    w, h, top, bot, upper, lower, shape, lid = eye_geometry(g, ey)
    p.fill(shape, (0.93, 0.91, 0.89))
    p.shade(shift(shape, 0, top * .6), (0.78, 0.75, 0.76), 0.6, blur=2.0)   # lid shadow
    p.shade(shift(shape, w * .45, 0), (0.86, 0.84, 0.84), 0.4, blur=2.0)
    p.shade(shift(shape, -w * .45, 0), (0.86, 0.84, 0.84), 0.4, blur=2.0)
    return p


def iris_geometry(g, ey):
    w, h, top, bot, upper, lower, shape, lid = eye_geometry(g, ey)
    r = (top + bot) * (0.56 if g == "m" else 0.58)
    cx, cy = -w * 0.03, top - r * 0.70
    return r, cx, cy, shape


def eye_iris(g, ey):
    p = Part(f"eyeI_{ey}", FEATURE_SCALE)
    r, cx, cy, shape = iris_geometry(g, ey)
    circle = [(cx + r * math.cos(a / 28 * 2 * math.pi), cy + r * math.sin(a / 28 * 2 * math.pi)) for a in range(28)]
    p.fill(circle, 0.50, clip=shape)
    p.fill(shift(scale_pts(circle, .80, .80, cx, cy), 0, -r * .12), 0.95, 0.85, clip=shape)
    p.shade(shift(circle, 0, r * .95), 0.25, 0.85, blur=1.6, clip=shape)
    p.fill(scale_pts(circle, .40, .40, cx, cy), 0.05, clip=shape)
    p.stroke(circle, 0.9, 0.22, closed=True, shade_only=True)
    p.pad(shape)
    return p


def eye_lines(g, ey):
    p = Part(f"eyeL_{ey}", FEATURE_SCALE)
    w, h, top, bot, upper, lower, shape, lid = eye_geometry(g, ey)
    O = upper[-1]
    wing = (O[0] + w * (0.10 if ey != 7 else 0.24), O[1] + h * (0.10 if ey != 7 else 0.28))
    # upper lid: thin at the inner corner, heavier and flicked at the outer corner
    p.stroke(upper + [wing], lid * .45, INK, width_end=lid * (1.15 if g == "f" else .95))
    # double-lid crease
    crease = [(upper[1][0] + 2, upper[1][1] + 3.2), (upper[2][0], upper[2][1] + 4.2), (upper[3][0], upper[3][1] + 3.6), (O[0] - 1, O[1] + 2.4)]
    p.stroke(crease, 0.9, (0.42, 0.30, 0.28), 0.55, taper='both')
    # lower lid: faint, outer two thirds
    p.stroke(lower[:3], 0.9, (0.36, 0.26, 0.25), 0.55, taper='end')
    p.stroke([(-w / 2 - 1.5, 0.5), (-w / 2 + 1.5, -0.6)], 1.0, (0.62, 0.36, 0.34), 0.6, taper='both')  # tear duct
    if g == "f":
        for i, t in enumerate((0.55, 0.75, 0.92)):
            a = upper[2] if t < .8 else upper[3]
            x0 = a[0] + (O[0] - a[0]) * t
            y0 = a[1] + (O[1] - a[1]) * t
            p.stroke([(x0, y0), (x0 + 2 + i, y0 + 2.4), (x0 + 3.5 + i * 1.5, y0 + 3.0)], 0.8, INK, width_end=.15)
    r, cx, cy, _ = iris_geometry(g, ey)
    p.ellipse(cx + r * .32, cy + r * .30, max(1.4, r * .2), max(1.4, r * .2), (1, 1, 1), 0.95)
    p.ellipse(cx - r * .30, cy - r * .32, max(.7, r * .09), max(.7, r * .09), (1, 1, 1), 0.6)
    return p


# ---------------------------------------------------------------- brows (screen-right brow: inner end -x)

BROW_STYLES = [  # length, thickness, arch, outer drop/lift, kind
    (38, 2.6, 1.5, 0, 'straight'),
    (38, 3.2, 3.5, 1, 'natural'),
    (42, 3.8, 3.0, 8, 'sword'),
    (36, 2.8, 6.0, -2, 'arched'),
    (40, 4.8, 3.0, 3, 'bold'),
]


def brow(g, br):
    p = Part(f"brow_{br}", FEATURE_SCALE)
    L, t, arch, lift, kind = BROW_STYLES[br]
    if g == "f":
        t *= .68; arch += 1.5; L *= .96
    I = (-L * .45, -1.0)
    O = (L * .55, lift)
    peak_x = L * (.22 if kind != 'sword' else .30)
    top = [I, (-L * .25, t + arch * .55), (peak_x, arch + t * .55 + lift * .5), O]
    bottom = [O, (peak_x - 2, arch + lift * .45 - t * .45), (-L * .25, arch * .3 - t * .2), (I[0] + 1, -t * .6)]
    if kind == 'sword':
        top = [I, (-L * .2, t * 1.1 + arch * .3), (peak_x, arch + t * .4 + lift * .55), O]
    shape = [(I[0], I[1], 'c')] + top[1:-1] + [(O[0], O[1], 'c')] + bottom[1:-1]
    p.fill(shape, 0.50, 0.85)
    p.shade(shift(shape, L * .55, 0), 0.72, 0.65, blur=5)   # tail fades out
    for i in range(10):
        k = i / 9
        x = I[0] + (O[0] - I[0]) * k
        y = I[1] + (O[1] - I[1]) * k + math.sin(k * math.pi) * arch * .8
        ang = 1.0 - k  # inner hairs stand up, tail hairs lie along the brow
        p.stroke([(x - 1.2, y - t * .45), (x + 1.5 + 2.5 * (1 - ang), y + t * .45 * (0.6 + .4 * ang))], 0.9, 0.24, 0.85,
                 taper='end', shade_only=True)
    return p


# ---------------------------------------------------------------- nose (origin at the nostrils)

NOSE_STYLES = [(16, 4.5, 6.0), (22, 5.0, 7.0), (28, 5.5, 7.0), (20, 6.0, 9.0)]


def nose(g, no):
    p = Part(f"nose_{no}", FEATURE_SCALE)
    L, tip, wing = NOSE_STYLES[no]
    if g == "f":
        L *= .82; tip *= .85; wing *= .85
    p.pad([(-wing - 4, -8), (wing + 4, L + 6)])
    # bridge shadow on the shaded (right) side
    p.fill([(2, L), (6, L * .6), (7, tip * .9), (3, tip * .7), (3, L * .5)], SKIN_DEEP, 0.35)
    p.stroke([(4, L * .9), (5.5, L * .45), (5.2, tip * 1.1)], 1.1, SKIN_LINE, 0.35, taper='both')
    # tip and wings
    p.stroke([(-tip * .7, 1.6), (-tip * .3, -0.8), (tip * .3, -0.8), (tip * .7, 1.6)], 1.2, SKIN_LINE, 0.55, taper='both')
    for sx in (-1, 1):
        p.stroke([(sx * wing * .55, 4), (sx * wing, 1.5), (sx * (wing - 1), -2.4), (sx * wing * .55, -3.2)], 1.4, SKIN_LINE, 0.85 if sx > 0 else .65,
                 taper='both')
        p.ellipse(sx * wing * .42, -1.8, 2.3, 1.2, (0.36, 0.22, 0.20), 0.75, rot=sx * 18)
    p.ellipse(0, -6, wing * .9, 2.4, SKIN_DEEP, 0.25)
    p.soft(0.3)
    return p


# ---------------------------------------------------------------- mouth (origin at the lip line)

MOUTH_STYLES = [  # width, smile, lip fullness
    (18, 0.2, 0.35), (19, 1.6, 0.4), (17, -1.0, 0.2), (18, 0.5, 0.75), (19, 1.0, 0.45),
]


def mouth(g, mo):
    p = Part(f"mouth_{mo}", FEATURE_SCALE)
    w, smile, lip = MOUTH_STYLES[mo]
    smirk = 1.6 if mo == 4 else 0.0
    if g == "f":
        w *= .86; lip = min(1.0, lip + .3)
    Lc = (-w / 2, smile)
    Rc = (w / 2, smile + smirk)
    lipc = (0.86, 0.46, 0.45) if g == "f" else (0.80, 0.54, 0.50)
    up = 2.2 + lip * 2.0
    lo = 4.5 + lip * 3.0
    upper = [Lc, (-w * .22, up), (-w * .06, up * .78), (0, up * .62), (w * .06, up * .78), (w * .22, up), Rc]
    lower = [Rc, (w * .2, -lo * .9), (0, -lo), (-w * .2, -lo * .9), Lc]
    p.fill(upper + [(w * .2, 0), (-w * .2, 0)], lipc, .30 + .40 * lip)
    p.fill(lower + [(-w * .2, 0), (w * .2, 0)], lipc, .25 + .35 * lip)
    p.shade([(-w, -lo * .2), (w, -lo * .2), (w, -lo * 2), (-w, -lo * 2)], (0.98, 0.80, 0.76), 0.25, blur=2, clip=lower)
    line = [Lc, (-w * .25, -0.3), (0, 0.3), (w * .25, -0.3), Rc]
    p.stroke(line, 1.5, (0.32, 0.16, 0.15), taper='both')
    for c in (Lc, Rc):
        p.ellipse(c[0], c[1], 1.3, 1.0, (0.38, 0.22, 0.20), 0.6)
    p.ellipse(0, -lo - 4, w * .28, 1.8, (0.55, 0.38, 0.34), 0.18)   # shadow under the lower lip
    p.soft(0.3)
    return p


# ---------------------------------------------------------------- beards (tinted with hair colour)

def _moustache(p, g, droop=8.0, length=16.0, thick=3.2, mouth_y=30, nose_y=54):
    y0 = (mouth_y + nose_y) / 2 + 1   # philtrum
    for sx in (-1, 1):
        stroke = [(sx * 2.5, y0 + 1), (sx * 9, y0 - .5), (sx * length * .9, mouth_y + 1.5), (sx * (length + 2), mouth_y - droop)]
        p.stroke(stroke, thick, 0.32, width_end=0.6)
        for k in range(4):
            x = sx * (4 + k * 3.2)
            p.stroke([(x, y0 + .6), (x + sx * 2.4, y0 - 2.6)], 0.8, 0.14, 0.6, taper='end', shade_only=True)


def beard(g, bd):
    p = Part(f"beard_{bd}", HEAD_SCALE)
    if bd == 0:
        p.pad([(0, 0), (1, 1)])
        return p
    if bd == 1:      # thin moustache
        _moustache(p, g, droop=5, length=14, thick=2.6)
    elif bd == 2:    # moustache + goatee
        _moustache(p, g, droop=6, length=13, thick=2.8)
        gt = [(0, 21), (6, 18), (8, 6), (4, -8), (0, -16, 'c'), (-4, -8), (-8, 6), (-6, 18)]
        p.fill(gt, 0.34)
        for x in (-4, -1.5, 1.5, 4):
            p.stroke([(x, 17), (x * .6, 2), (x * .2, -12)], 0.8, 0.16, 0.6, taper='end', shade_only=True)
    elif bd == 3:    # long sage beard from the chin and jaw
        lb = [(-30, 34), (-20, 16), (-8, 12), (8, 12), (20, 16), (30, 34), (28, 0), (20, -30), (12, -62), (4, -92), (0, -104, 'c'),
              (-5, -88), (-14, -60), (-22, -28), (-28, 2)]
        p.fill(lb, 0.80)
        p.texture(0.12, 0.35, seed=3)
        p.shade([(-60, 50), (-10, 50), (-14, -130), (-60, -130)], 0.72, 0.5, blur=10, clip=lb)
        p.shade([(-40, 50), (40, 50), (40, 22), (-40, 22)], 0.62, 0.6, blur=6, clip=lb)
        for i in range(13):
            k = i / 12
            x0 = -26 + 52 * k
            p.stroke([(x0, 18 - 10 * math.sin(k * math.pi)), (x0 * .75 + math.sin(i) * 2, -40), (x0 * .15, -98 + abs(x0) * 1.6)],
                     0.9 + (i % 3) * .3, 0.50 if i % 2 else 0.64, 0.75, taper='end', shade_only=True)
        p.stroke(lb, 1.0, 0.38, 0.55, closed=True)
        # hole for the mouth so the lips stay visible
        _moustache(p, g, droop=14, length=17, thick=3.6)
    elif bd == 4:    # short full beard along the jaw
        fb = [(-55, 58), (-50, 36), (-36, 14), (-14, 0), (0, -4), (14, 0), (36, 14), (50, 36), (55, 58),
              (47, 50), (36, 32), (18, 16), (8, 14), (0, 15), (-8, 14), (-18, 16), (-36, 32), (-47, 50)]
        p.fill(fb, 0.62, 0.92)
        p.texture(0.30, 0.5, seed=5)
        p.shade(shift(fb, 0, 40), 0.8, 0.6, blur=10)
        for i in range(22):
            k = i / 21
            x = -50 + k * 100
            y = 46 - math.sin(k * math.pi) * 46
            p.stroke([(x, y + 6), (x * .96, y - 5)], 0.9, 0.20, 0.6, taper='end', shade_only=True)
        _moustache(p, g, droop=6, length=15, thick=3.2)
    p.edge(0.25, 2, 0.2)
    return p


# ---------------------------------------------------------------- forehead marks (tinted with the mark colour)

def mark(g, ma):
    p = Part(f"mark_{ma}", FEATURE_SCALE)
    if ma == 0:
        p.pad([(0, 0), (1, 1)])
        return p
    if ma == 1:  # flame
        p.fill([(0, 12, 'c'), (4, 3), (4.5, -4), (0, -10, 'c'), (-4.5, -4), (-4, 3)], 0.95)
        p.fill([(0, 4, 'c'), (2, -2), (0, -7, 'c'), (-2, -2)], 0.62)
    elif ma == 2:  # vertical eye
        p.fill([(0, 11, 'c'), (3, 0), (0, -11, 'c'), (-3, 0)], 0.95)
        p.ellipse(0, 0, 1.3, 3, 0.4)
    elif ma == 3:  # lotus
        for a in (-42, 0, 42):
            p.fill(rotate_pts([(0, 11, 'c'), (3.6, 4), (0, -2, 'c'), (-3.6, 4)], a, 0, -2), 0.95)
    elif ma == 4:  # crescent
        p.fill([(2, 7, 'c'), (-3.5, 4), (-4.5, -1), (-1, -6.5, 'c'), (-1.6, -2), (-1.4, 3)], 0.95)
    elif ma == 5:  # dot
        p.ellipse(0, 0, 3.2, 3.2, 0.95)
    p.edge(0.3, 1.2, 0.55)
    return p


def all_parts(g):
    parts = []
    for fa in range(4):
        parts.append(head(g, fa))
        parts.append(ears(g, fa))
    parts.append(neck(g))
    parts.append(blush(g))
    for ey in range(8):
        parts += [eye_white(g, ey), eye_iris(g, ey), eye_lines(g, ey)]
    for br in range(5):
        parts.append(brow(g, br))
    for no in range(4):
        parts.append(nose(g, no))
    for mo in range(5):
        parts.append(mouth(g, mo))
    for bd in range(5):
        parts.append(beard(g, bd))
    for ma in range(6):
        parts.append(mark(g, ma))
    return parts
