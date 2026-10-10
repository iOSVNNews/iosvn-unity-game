"""Turns painted full-canvas layers (ArtSource/ModularParts/<male|female>/*.png, drawn on the
templates from make_templates.py) into bone-space sprites for build_atlas.py.

Files and what becomes of them:
  face_N   head canvas  -> head_N  (ears and neck included)
  eyes_N   head canvas  -> eyePL_N / eyePR_N  (split at the face centre line)
  brows_N  head canvas  -> browPL_N / browPR_N
  nose_N, mouth_N, mark_N, beard_N, hat_N   head canvas
  hair_N   body canvas  -> hairF_N (around the head) + hairB_N (the rest, sways)
  outfit_N body canvas  -> torso/torsoT, skirtF/skirtT, sleeves near/far, trousers and boots per leg
Green (#00FF00) or transparent backgrounds are both accepted.
"""
import os
import math
import numpy as np
from PIL import Image
from scipy import ndimage
import rig
import compose

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "ArtSource", "ModularParts")
HEAD_PPU, HEAD_O = 3.6, (512, 780)
SIDE_CUT = False   # locks framing the face stay in front of it down to the chin
BODY_PPU, BODY_O = 1.5, (512, 1990)
DEBUG = False


def key(img, gray):
    a = np.asarray(img.convert("RGBA"), np.float32) / 255.0
    rgb, alpha = a[..., :3], a[..., 3]
    if alpha.min() > 0.99:   # chroma green background
        r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
        spill = g - np.maximum(r, b)
        alpha = np.clip(1.0 - (spill - 0.12) / 0.30, 0, 1)
        g2 = np.minimum(g, np.maximum(r, b) + 0.04)
        rgb = np.dstack([r, g2, b])
    if gray:
        lum = rgb[..., 0] * .3 + rgb[..., 1] * .59 + rgb[..., 2] * .11
        rgb = np.dstack([lum, lum, lum])
    return rgb, alpha


def normalise(rgb, alpha, top=0.97):
    """Skin is tinted by multiplication, so its lit areas must be near white."""
    m = alpha > 0.5
    if m.sum() < 50:
        return rgb
    hi = float(np.percentile(rgb[..., 0][m], 95))
    return np.clip(rgb * (top / max(0.2, hi)), 0, 1) if hi < top else rgb


def sprite(rgb, alpha, mask, bone_px, ppu, min_px=30, angle=0.0):
    """Crop the masked layer; the bone origin becomes the pivot.  `angle` is the bone's world angle in
    the bind pose: the image is turned back by it so the runtime can rotate it with the bone."""
    a = alpha * mask
    ys, xs = np.nonzero(a > 0.02)
    if len(xs) < min_px:
        return None
    x0, x1 = max(0, xs.min() - 2), min(a.shape[1], xs.max() + 3)
    y0, y1 = max(0, ys.min() - 2), min(a.shape[0], ys.max() + 3)
    out = np.dstack([rgb[y0:y1, x0:x1], a[y0:y1, x0:x1]])
    img = Image.fromarray((np.clip(out, 0, 1) * 255 + .5).astype(np.uint8), "RGBA")
    ox = bone_px[0] - x0
    oy = y1 - bone_px[1]
    if abs(angle) > 1e-3:
        w, h = img.size
        pad = int(math.hypot(w, h) + abs(ox) + abs(oy)) + 4
        big = Image.new("RGBA", (pad * 2, pad * 2), (0, 0, 0, 0))
        big.paste(img, (int(round(pad - ox)), int(round(pad - (h - oy)))))
        big = big.rotate(-angle, resample=Image.BICUBIC, center=(pad, pad))
        bb = big.getbbox() or (pad, pad, pad + 1, pad + 1)
        img = big.crop(bb)
        ox = pad - bb[0]
        oy = bb[3] - pad
    return img, (ox, oy), ppu


# Expected feature widths in rig units; painted features far larger than this are scaled down
# around their anchor so a generous brush stroke does not run off the face.
FIT = {"browP": 46, "eyeP": 36, "nose": 24, "mouth": 26}


def fit(name, spr):
    if spr is None:
        return spr
    img, origin, ppu = spr
    for prefix, width in FIT.items():
        if name.startswith(prefix):
            units = img.size[0] / ppu
            if units > width * 1.25:
                ppu = ppu * units / (width * 1.1)
            break
    return img, origin, ppu


def trim_bands(mask, knee_py=None):
    """Keep only light regions shaped like trim (long, thin bands: collars, hems, cuffs); light blobs
    are highlights on the cloth and stay with the cloth so they take its colour."""
    lab, n = ndimage.label(mask)
    if n == 0:
        return mask
    dt = ndimage.distance_transform_edt(mask)
    keep = np.zeros(n + 1, bool)
    areas = ndimage.sum(np.ones_like(dt), lab, range(1, n + 1))
    thick = ndimage.maximum(dt, lab, range(1, n + 1))
    for k in range(n):
        w = max(1.0, thick[k])
        keep[k + 1] = w <= 9 and areas[k] / (w * w) >= 30
    if knee_py is not None:
        # below the knee only hems that run across the skirt count; a light band around one ankle is
        # the trouser cuff and stays with the trousers
        for k, sl in enumerate(ndimage.find_objects(lab)):
            if keep[k + 1] and (sl[0].start + sl[0].stop) / 2 > knee_py and (sl[1].stop - sl[1].start) < 110:
                keep[k + 1] = False
    return keep[lab]


def face_union(folder, head, shape):
    """Union of the painted face bases, mapped from the head canvas onto the body canvas."""
    acc = None
    for n in range(8):
        p = os.path.join(folder, f"face_{n}.png")
        if not os.path.exists(p):
            continue
        a = np.asarray(Image.open(p).convert("RGBA"))[..., 3] > 128
        acc = a if acc is None else (acc | a)
    if acc is None:
        return None
    k = BODY_PPU / HEAD_PPU
    # body px = BODY_O + (head + (hp - HEAD_O) * (1, -1) / HEAD_PPU) * BODY_PPU * (1, -1)
    ox = BODY_O[0] + head[0] * BODY_PPU - HEAD_O[0] * k
    oy = BODY_O[1] - head[1] * BODY_PPU - HEAD_O[1] * k
    im = Image.fromarray((acc * 255).astype(np.uint8))
    im = im.transform((shape[1], shape[0]), Image.AFFINE, (1 / k, 0, -ox / k, 0, 1 / k, -oy / k), resample=Image.BILINEAR)
    return np.asarray(im) > 128


def head_px(units):
    return HEAD_O[0] + units[0] * HEAD_PPU, HEAD_O[1] - units[1] * HEAD_PPU


def body_px(units):
    return BODY_O[0] + units[0] * BODY_PPU, BODY_O[1] - units[1] * BODY_PPU


def register_body(rgb, alpha, g):
    """Painted outfits rarely land exactly on the template: scale/shift so the soles sit on the
    ground line and the collar top at the neck, centred on the body axis."""
    ys, xs = np.nonzero(alpha > 0.5)
    if len(xs) < 100:
        return rgb, alpha
    X = (xs - BODY_O[0]) / BODY_PPU
    Y = (BODY_O[1] - ys) / BODY_PPU
    bottom = Y[np.abs(X) < 150].min()
    top = Y[np.abs(X) < 40].max()
    chest = (Y > top - 260) & (Y < top - 120)
    cx = float(np.median(X[chest])) if chest.any() else 0.0
    target_top = rig.WORLD[g]["neck"][1] + 14
    s = target_top / max(1.0, top - bottom)
    if abs(s - 1) < 0.01 and abs(bottom) < 2 and abs(cx) < 2:
        return rgb, alpha
    # output pixel (u, v) samples input at X = u_units / s + cx, Y = v_units / s + bottom
    H, W = alpha.shape
    def warp(channel):
        im = Image.fromarray((np.clip(channel, 0, 1) * 255).astype(np.uint8))
        # PIL affine maps output -> input pixel coordinates
        a = 1 / s
        c = BODY_O[0] + cx * BODY_PPU - BODY_O[0] / s
        e = 1 / s
        f = BODY_O[1] - bottom * BODY_PPU - BODY_O[1] / s
        return np.asarray(im.transform((W, H), Image.AFFINE, (a, 0, c, 0, e, f), resample=Image.BICUBIC), np.float32) / 255.0
    rgb2 = np.dstack([warp(rgb[..., i]) for i in range(3)])
    return rgb2, warp(alpha)


def _seg_dist(px, py, a, b):
    ax, ay = a; bx, by = b
    dx, dy = bx - ax, by - ay
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / max(1e-6, dx * dx + dy * dy), 0, 1)
    return np.hypot(px - (ax + t * dx), py - (ay + t * dy))


def load(g):
    folder = os.path.join(SRC, "male" if g == "m" else "female")
    over, drop = {}, []
    if not os.path.isdir(folder):
        return over, drop
    W = compose.world_bones(g)
    F = rig.FACE[g]
    anc = compose.anchors(g, {})

    def files(prefix):
        for n in sorted(os.listdir(folder)):
            if n.startswith(prefix + "_") and n.endswith(".png"):
                try:
                    yield int(n[len(prefix) + 1:-4]), os.path.join(folder, n)
                except ValueError:
                    pass

    full = lambda shape: np.ones(shape, np.float32)
    # ---- head canvas parts
    for prefix, gray in (("face", True), ("eyes", False), ("eyesI", True), ("brows", True), ("nose", True), ("mouth", False),
                         ("mark", True), ("beard", True), ("hat", True)):
        for i, path in files(prefix):
            rgb, alpha = key(Image.open(path), gray)
            H, Wd = alpha.shape
            if prefix == "face":
                rgb = normalise(rgb, alpha)
                s = sprite(rgb, alpha, full(alpha.shape), head_px((0, 0)), HEAD_PPU)
                if s: over[f"head_{i}"] = s; drop.append(f"ears_{i}")
            elif prefix in ("eyes", "eyesI", "brows"):
                left = np.zeros(alpha.shape, np.float32); left[:, :Wd // 2] = 1
                aL, aR = ("eyeL", "eyeR") if prefix != "brows" else ("browL", "browR")
                base = {"eyes": "eyeP", "eyesI": "eyePI", "brows": "browP"}[prefix]
                sL = sprite(rgb, alpha, left, head_px(anc[aL]), HEAD_PPU)
                sR = sprite(rgb, alpha, 1 - left, head_px(anc[aR]), HEAD_PPU)
                if sL and sR:
                    over[f"{base}L_{i}"] = fit(base, sL); over[f"{base}R_{i}"] = fit(base, sR)
                    if prefix == "eyes":
                        drop += [f"eyeW_{i}", f"eyeI_{i}", f"eyeL_{i}"]
                    else:
                        drop.append(f"brow_{i}")
            else:
                at = {"nose": anc["nose"], "mouth": anc["mouth"], "mark": anc["mark"]}.get(prefix, (0, 0))
                s = fit(prefix, sprite(rgb, alpha, full(alpha.shape), head_px(at), HEAD_PPU))
                if s: over[f"{prefix}_{i}"] = s
    # ---- hair: split around the head
    hx, hy, _ = W["head"]
    for i, path in files("hair"):
        rgb, alpha = key(Image.open(path), True)
        H, Wd = alpha.shape
        ys, xs = np.mgrid[0:H, 0:Wd].astype(np.float32)
        ux = (xs - BODY_O[0]) / BODY_PPU - hx
        uy = (BODY_O[1] - ys) / BODY_PPU - hy
        # everything at or above the jaw belongs over the face; what falls below the chin hangs behind the body
        chin = 14.0
        front = (uy > chin) & (np.abs(ux) < 150)
        # hair painted around the jaw and neck is hair seen behind the head: below the eyes, anything
        # over the bare head goes to the back piece so the front locks don't end in a cut line
        tpl = np.asarray(Image.open(os.path.join(ROOT, "ArtSource", "ModularTemplates",
                                                  f"body_{'male' if g == 'm' else 'female'}.png")).convert("RGBA"), np.float32) / 255.0
        sil = (tpl[..., 3] > 0.5) & ~((tpl[..., 1] > 0.6) & (tpl[..., 0] < 0.4) & (tpl[..., 2] < 0.4))
        if sil.shape == alpha.shape and SIDE_CUT:
            front &= ~(sil & (uy < F["eye"][1]))
        front = front.astype(np.float32)
        # hair painted across the lower face is hair seen behind the head: it goes to the back piece.
        # The face is taken a little inside its outline and the hand-over is a soft ramp below the
        # eyes, so locks framing the face stay in front and nothing ends in a straight cut.
        fu = face_union(folder, (hx, hy), alpha.shape)
        if fu is not None:
            inner = ndimage.binary_erosion(fu, iterations=2).astype(np.float32)
            inner = ndimage.gaussian_filter(inner, 1.0)
            xs_f = np.nonzero(fu.any(0))[0]
            half = max(1.0, (xs_f[-1] - xs_f[0]) / 2 / BODY_PPU)
            # the hand-over line dips from the eyes at the centre to the cheeks at the sides
            line = F["eye"][1] + 10 - 6 * np.clip(np.abs(ux) / half, 0, 1) ** 2
            ramp = np.clip((line - uy) / 2.0, 0, 1)
            front *= 1 - inner * ramp
        bone = body_px((hx, hy))
        sf = sprite(rgb, alpha, front, bone, BODY_PPU)
        # the back piece keeps the whole painting: it sits behind the head and body, so the part over the
        # face is hidden, and there is no cut line where the front piece ends
        sb = sprite(rgb, alpha, full(alpha.shape), bone, BODY_PPU)
        if sf: over[f"hairF_{i}"] = sf
        if sb: over[f"hairB_{i}"] = sb
        drop += [f"hairF_{i}", f"hairB_{i}", f"hairS_{i}"] if (sf or sb) else []
        if not sb: drop.append(f"hairB_{i}")
    # ---- hands
    import glob, re as _re
    hand_files = [p for p in [os.path.join(folder, "hands.png")] if os.path.exists(p)] + sorted(glob.glob(os.path.join(folder, "hands_*.png")))
    for path in hand_files:
        mm = _re.search(r"hands_(\d+)\.png$", path)
        suffix = "_" + mm.group(1) if mm else ""      # hands painted with one outfit belong to that outfit
        rgb, alpha = key(Image.open(path), True)
        rgb = normalise(rgb, alpha)
        H, Wd = alpha.shape
        xs = (np.arange(Wd, dtype=np.float32) - BODY_O[0]) / BODY_PPU
        near = np.tile((xs < 0).astype(np.float32), (H, 1))
        for side, m in (("N", near), ("F", 1 - near)):
            bone = "hand" + side
            spr = sprite(rgb, alpha, m, body_px((W[bone][0], W[bone][1])), BODY_PPU, angle=rig.world_angle(bone))
            if spr: over["handP" + side + suffix] = spr
    # ---- outfits: nearest-bone assignment
    for i, path in files("outfit"):
        rgb, alpha = key(Image.open(path), True)
        cg = os.path.join(ROOT, "ArtSource", "ChatGPTRaw", f"{g}_outfit_{i}.png")
        if not os.path.exists(cg):          # outfits painted on the real template are registered already
            rgb, alpha = register_body(rgb, alpha, g)
        H, Wd = alpha.shape
        ys, xs = np.mgrid[0:H, 0:Wd].astype(np.float32)
        X = (xs - BODY_O[0]) / BODY_PPU
        Y = (BODY_O[1] - ys) / BODY_PPU
        lum = rgb[..., 0]
        P = lambda n: (W[n][0], W[n][1])
        waist = W["torso"][1]
        segs = {
            "torso": (P("torso"), (W["neck"][0], W["neck"][1] - 10), 76 if g == "m" else 58),
            "armN_up": (P("armN_up"), P("armN_lo"), 26), "armN_lo": (P("armN_lo"), (W["handN"][0], W["handN"][1] - 60), 32),
            "armF_up": (P("armF_up"), P("armF_lo"), 26), "armF_lo": (P("armF_lo"), (W["handF"][0], W["handF"][1] - 60), 32),
            "legN_up": (P("legN_up"), P("legN_lo"), 30), "legN_lo": (P("legN_lo"), (W["legN_lo"][0], 0), 26),
            "legF_up": (P("legF_up"), P("legF_lo"), 30), "legF_lo": (P("legF_lo"), (W["legF_lo"][0], 0), 26),
        }
        dist = {k: _seg_dist(X, Y, a, b) - r for k, (a, b, r) in segs.items()}
        names = list(dist)
        stack = np.stack([dist[k] for k in names])
        best = np.array(names)[np.argmin(stack, axis=0)]
        # wide sleeves: anything outside the body silhouette, between the shoulder and a little below
        # the hand, belongs to the arm on that side (upper or lower half by its position along the arm)
        sh_y = W["armN_up"][1]
        if g == "m":
            half = np.interp(Y, [0, waist - 380, waist, waist + 120, sh_y], [112, 112, 54, 70, 84])
        else:
            half = np.interp(Y, [0, waist - 360, waist, waist + 110, sh_y], [104, 104, 41, 56, 64])
        for side, sign in (("N", -1.0), ("F", 1.0)):
            s0 = np.array(P(f"arm{side}_up")); s1 = np.array(P(f"hand{side}"))
            outside = (sign * X > half + 4) & (Y < sh_y + 24) & (Y > s1[1] - 120)
            d = s1 - s0
            t = ((X - s0[0]) * d[0] + (Y - s0[1]) * d[1]) / float(d @ d)
            best = np.where(outside & (t < 0.5), f"arm{side}_up", best)
            best = np.where(outside & (t >= 0.5), f"arm{side}_lo", best)
        below = Y < waist - 6
        opaque = alpha > 0.5
        # legs: follow each leg up from the feet.  A row's leg span may only grow a little from the row
        # below, so a robe panel hanging beside the trousers is left to the skirt; the climb stops at the
        # hem, where cloth suddenly spreads well past the leg on both sides (or both legs merge).
        centre = (W["legN_lo"][0] + W["legF_lo"][0]) / 2
        solid = alpha > 0.35
        legs = np.zeros(alpha.shape, bool)
        cpx = int(round(BODY_O[0] + centre * BODY_PPU))
        top_py = int(BODY_O[1] - (waist - 6) * BODY_PPU)
        ankle_py = int(BODY_O[1] - 4 * BODY_PPU)
        grow = 3
        spread = int(26 * BODY_PPU)
        for side in ("N", "F"):
            lo, hi = (0, cpx) if side == "N" else (cpx, Wd)
            bx = int(round(BODY_O[0] + W[f"leg{side}_lo"][0] * BODY_PPU))
            row = solid[ankle_py, lo:hi]
            on = np.nonzero(row)[0]
            if len(on) == 0:
                continue
            # the run nearest the leg bone at the ankle
            runs = np.split(on, np.nonzero(np.diff(on) > 1)[0] + 1)
            run = min(runs, key=lambda r_: min(abs(lo + r_[0] - bx), abs(lo + r_[-1] - bx)))
            l, r = lo + run[0], lo + run[-1]
            hist = []
            sidemask = np.zeros(Wd, bool); sidemask[lo:hi] = True
            # feet: everything below the ankle on this side
            legs[ankle_py:, lo:hi] |= solid[ankle_py:, lo:hi]
            for py in range(ankle_py - 1, top_py, -1):
                row = solid[py]
                L0, R0 = max(lo, l - grow), min(hi - 1, r + grow)
                seg = np.nonzero(row[L0:R0 + 1])[0]
                if len(seg) == 0:
                    if DEBUG: print('stop empty', side, (BODY_O[1]-py)/BODY_PPU)
                    break
                # how far does the cloth containing this span reach on either side?
                ext_l = l
                while ext_l > 0 and row[ext_l - 1]:
                    ext_l -= 1
                ext_r = r
                while ext_r < Wd - 1 and row[ext_r + 1]:
                    ext_r += 1
                wide_l = l - ext_l > spread
                wide_r = ext_r - r > spread
                if wide_l and wide_r:
                    if DEBUG: print('stop wide', side, (BODY_O[1]-py)/BODY_PPU)
                    break
                nl, nr = L0 + seg[0], L0 + seg[-1]
                # the legs meet at the crotch, or a robe closes over them
                if (BODY_O[1] - py) / BODY_PPU > W["hips"][1] - 90 and (side == "N" and nr >= hi - 1 and row[min(Wd - 1, hi)]) or (BODY_O[1] - py) / BODY_PPU > W["hips"][1] - 90 and (side == "F" and nl <= lo and row[max(0, lo - 1)]):
                    if DEBUG: print('stop crotch', side, (BODY_O[1]-py)/BODY_PPU)
                    break
                # a lighter robe hem over darker trousers: stop where the cloth turns clearly lighter
                m_ = float(lum[py, nl:nr + 1][row[nl:nr + 1]].mean()) if row[nl:nr + 1].any() else 0.0
                hist.append(m_)
                crosses = (ext_r >= cpx) if side == "N" else (ext_l <= cpx)
                if crosses and len(hist) > 12 and min(hist[-4:]) > float(np.median(hist[:-4])) + 0.12:
                    legs[py:py + 4, :] &= ~sidemask
                    if DEBUG: print('stop robe', side, (BODY_O[1]-py)/BODY_PPU)
                    break
                if len(hist) > 30 and (BODY_O[1] - py) / BODY_PPU > W[f"leg{side}_lo"][1] + 20:
                    ref = float(np.median(hist[-30:-6]))
                    if min(hist[-5:]) > ref + 0.12:
                        legs[py:py + 5, :] &= ~sidemask
                        if DEBUG: print('stop lum', side, (BODY_O[1]-py)/BODY_PPU)
                        break
                l, r = nl, nr
                legs[py, l:r + 1] = row[l:r + 1]
        legs = ndimage.binary_closing(legs, iterations=2) & (alpha > 0.03)
        hem_rows = np.nonzero(legs.any(1))[0]
        hem = (BODY_O[1] - hem_rows.min()) / BODY_PPU if len(hem_rows) else 0.0
        boots = legs & (Y < W["legN_lo"][1] * 0.55)
        trousers = legs & ~boots
        skirt = below & ~legs & ~np.isin(best, ["armN_lo", "armF_lo"])

        trim = ndimage.gaussian_filter(lum, 1.5) > 0.84
        trim = ndimage.binary_opening(trim, iterations=2) & (alpha > 0.3) & ~legs
        trim = trim_bands(trim, knee_py=body_px((0, W["legN_lo"][1]))[1]) & ~ndimage.binary_dilation(legs, iterations=10)
        out = {}

        legs_zone = ndimage.binary_dilation(legs, iterations=4)

        def add(name, m, bone):
            # body pieces overlap by a few pixels so no hairline gap opens at the cuts; legs move on their
            # own, so nothing is allowed to grow into them
            if m.any():
                grown = ndimage.binary_dilation(m, iterations=3)
                if name.startswith(("thigh", "shin", "boot")):
                    m = grown & legs_zone            # close the knee and centre seams inside the legs
                elif name.startswith("skirt"):
                    m = grown & ~legs_zone           # a robe drawn over moving legs must not carry their outline
                else:
                    m = grown                        # torso / sleeves may tuck a little over the trousers
            s = sprite(rgb, alpha, m.astype(np.float32), body_px(P(bone)), BODY_PPU, angle=rig.world_angle(bone))
            if s: over[name] = s

        upper = ~below
        add(f"torso_{i}", upper & (best == "torso") & ~trim, "torso")
        add(f"torsoT_{i}", upper & (best == "torso") & trim, "torso")
        add(f"skirtF_{i}", skirt & ~trim, "skirt")
        add(f"skirtT_{i}", skirt & trim, "skirt")
        for side in ("N", "F"):
            add(f"sleeveU{side}_{i}", (best == f"arm{side}_up") & ~trim, f"arm{side}_up")
            add(f"sleeveL{side}_{i}", (best == f"arm{side}_lo") & ~trim & ~skirt, f"arm{side}_lo")
            add(f"cuff{side}_{i}", (np.isin(best, [f"arm{side}_up", f"arm{side}_lo"])) & trim, f"arm{side}_lo")
            mine = (X < centre) if side == "N" else (X >= centre)
            knee = W[f"leg{side}_lo"][1]
            add(f"thigh{side}_{i}", trousers & mine & (Y >= knee), f"leg{side}_up")
            add(f"shin{side}_{i}", trousers & mine & (Y < knee), f"leg{side}_lo")
            add(f"boot{side}_{i}", boots & mine, f"leg{side}_lo")
        # a painted outfit owns its legs: empty placeholders stop the code-drawn trousers showing through
        blank = Image.new("RGBA", (2, 2), (0, 0, 0, 0))
        for side in ("N", "F"):
            for part in ("thigh", "shin", "boot"):
                over.setdefault(f"{part}{side}_{i}", (blank, (1.0, 1.0), BODY_PPU))
        # same for the light trim pieces: without trim of its own an outfit must not inherit the drawn ones
        for name in (f"torsoT_{i}", f"skirtT_{i}", f"cuffN_{i}", f"cuffF_{i}"):
            over.setdefault(name, (blank, (1.0, 1.0), BODY_PPU))
        drop += [f"skirtB_{i}", f"belt_{i}", f"cape_{i}", f"plate_{i}", f"sleeveU_{i}", f"sleeveL_{i}", f"cuffT_{i}"]
    return over, drop
