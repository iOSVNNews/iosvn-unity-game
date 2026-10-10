"""Prepares raw Gemini paintings (ArtSource/GeminiRaw/<m|f>_<outfit|hair>_<n>.png, <m|f>_hands.png) for
import_painted.py: resize to the template canvas, key the green, strip the generator's corner
sparkle, and register hair onto the template skull (Gemini tends to zoom in on the head).
Results go to ArtSource/ModularParts/<male|female>/ as transparent PNGs; anything they replace is
moved to ArtSource/ModularParts/_old/<male|female>/ the first time.

    python prep_gemini.py            # all files
    python prep_gemini.py m_hair_3   # just one
"""
import os
import re
import sys
import shutil
import numpy as np
from PIL import Image
from scipy import ndimage

import import_painted as ip

ROOT = ip.ROOT
RAW = os.path.join(ROOT, "ArtSource", "GeminiRaw")
DST = os.path.join(ROOT, "ArtSource", "ModularParts")
TPL = os.path.join(ROOT, "ArtSource", "ModularTemplates")
W, H = 1024, 2048


def load(path, size=(W, H)):
    im = Image.open(path).convert("RGB")
    if im.size != size:
        im = im.resize(size, Image.LANCZOS)
    a = np.asarray(im, np.float32) / 255.0
    load.spill = np.clip(a[..., 1] - np.maximum(a[..., 0], a[..., 2]), 0, 1)
    rgb, alpha = ip.key(im, gray=False)
    return rgb, alpha


def strip_sparkle(alpha):
    """Drop small isolated blobs in the bottom-right corner (the generator's watermark)."""
    lab, n = ndimage.label(alpha > 0.15)
    if n == 0:
        return alpha
    sizes = ndimage.sum(np.ones_like(alpha), lab, range(1, n + 1))
    objs = ndimage.find_objects(lab)
    big = sizes.max()
    out = alpha.copy()
    Hh, Ww = alpha.shape
    for i, (sl, size) in enumerate(zip(objs, sizes)):
        y0, x0 = sl[0].start, sl[1].start
        if size < max(4000 * Ww * Hh / (W * H), big * 0.01) and x0 > Ww * 0.70 and y0 > Hh * 0.82:
            out[lab == i + 1] = 0
    # specks anywhere (dust, stray strokes far from everything)
    for i, size in enumerate(sizes):
        if size < 40:
            out[lab == i + 1] = 0
    return out


def template_head(g):
    a = np.asarray(Image.open(os.path.join(TPL, f"body_{'male' if g == 'm' else 'female'}.png")).convert("RGBA"),
                   np.float32) / 255.0
    r, gg, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    fg = (al > 0.5) & ~((gg > 0.6) & (r < 0.4) & (b < 0.4))
    rows = np.nonzero(fg.any(1))[0]
    top = rows[0]
    widths, cents = [], []
    for y in range(top, top + 400):
        xs = np.nonzero(fg[y])[0]
        widths.append(xs.max() - xs.min() if len(xs) else 0)
        cents.append((xs.max() + xs.min()) / 2 if len(xs) else W / 2)
    widths = np.array(widths, np.float32)
    # the head ends where the row width collapses to the neck
    wmax = widths[:250].max()
    end = top + int(np.argmax((np.arange(len(widths)) > 60) & (widths < wmax * 0.62)))
    hw = widths[: end - top]
    band = np.nonzero(hw >= 0.8 * np.percentile(hw, 90))[0]
    mid = top + float(np.median(band))
    cx = float(np.median(np.array(cents)[band]))
    template_head.top, template_head.chin = float(top), float(end)
    return float(np.percentile(hw, 90)), mid, cx


def face_gap(alpha):
    """The face opening: per row the widest background run enclosed by hair, grouped into a
    continuous region; the group with the largest area is the face."""
    fg = ndimage.binary_closing(alpha > 0.5, iterations=2)
    ys, xs = np.nonzero(fg)
    if len(ys) < 100:
        return None
    y_lim = min(ys.max(), max(ys.min() + (ys.max() - ys.min()) * 0.6, ys.min() + 800))
    rows = []
    for y in range(ys.min(), int(y_lim)):
        on = np.nonzero(fg[y])[0]
        if len(on) < 2:
            continue
        d = np.diff(on)
        k = int(np.argmax(d))
        if d[k] < 12:
            continue
        l, r = on[k], on[k + 1]
        rows.append((y, r - l, (l + r) / 2))
    if len(rows) < 10:
        return None
    rows = np.array(rows, np.float32)
    groups, cur = [], [rows[0]]
    for a_, b_ in zip(rows[:-1], rows[1:]):
        if b_[0] - a_[0] <= 6 and abs(b_[2] - a_[2]) < 60:
            cur.append(b_)
        else:
            groups.append(np.array(cur)); cur = [b_]
    groups.append(np.array(cur))
    run = max(groups, key=lambda g_: float(g_[:, 1].sum()))
    if len(run) < 10:
        return None
    # an opening that stays open below the chin (hair parted over the shoulders) keeps widening;
    # only the first face-height of it is the face
    L = 150.0
    for _ in range(6):
        head = run[run[:, 0] < run[0, 0] + L]
        L = 1.25 * float(np.percentile(head[:, 1], 90))
    run = run[run[:, 0] < run[0, 0] + L]
    w90 = float(np.percentile(run[:, 1], 90))
    band = run[run[:, 1] >= 0.8 * w90]
    face_gap.hairline = float(run[run[:, 1] >= 0.5 * w90][0, 0])
    return w90, float(np.median(band[:, 0])), float(np.median(band[:, 2]))


def warp(rgb, alpha, s, src_pt, dst_pt, theta=0.0):
    """Scale by s (and turn by theta radians, clockwise on screen) about src_pt and move it onto
    dst_pt (both in canvas pixels)."""
    import math
    cs, sn = math.cos(theta) / s, math.sin(theta) / s
    a, b_, d, e = cs, sn, -sn, cs
    c = src_pt[0] - (a * dst_pt[0] + b_ * dst_pt[1])
    f = src_pt[1] - (d * dst_pt[0] + e * dst_pt[1])

    size = (alpha.shape[1], alpha.shape[0])

    def one(ch):
        im = Image.fromarray((np.clip(ch, 0, 1) * 255).astype(np.uint8))
        return np.asarray(im.transform(size, Image.AFFINE, (a, b_, c, d, e, f), resample=Image.BICUBIC),
                          np.float32) / 255.0
    return np.dstack([one(rgb[..., i]) for i in range(rgb.shape[2])]), one(alpha)


HAIR_DY = 0.0   # fine vertical correction (template px) after the gap fit
HAIRLINE = 0.24  # where the hairline sits, as a fraction of the head from skull top to chin
HAIR_FIT = 0.9  # the painted opening runs a little wide of the skull; keep the hair snug on the head


def register_hair(rgb, alpha, g):
    gap = face_gap(alpha)
    if gap is None:
        print("   ! no face opening found, hair left as painted")
        return rgb, alpha
    tw, ty, tx = template_head(g)
    gw, gy, gx = gap
    s = tw / gw * HAIR_FIT
    print(f"   hair fit: gap {gw:.0f}px @({gx:.0f},{gy:.0f}) -> skull {tw:.0f}px @({tx:.0f},{ty:.0f})  scale {s:.3f}")
    # vertical placement: the painted hairline sits a quarter of the way down the template head
    # (the opening's widest band is less reliable: short cuts leave it open below the cheeks)
    hl = face_gap.hairline
    t_hl = template_head.top + HAIRLINE * (template_head.chin - template_head.top)
    print(f"   hairline {hl:.0f} -> {t_hl:.0f} (band rule would put it at {ty + (hl - gy) * s:.0f})")
    rgb, alpha = warp(rgb, alpha, s, (gx, hl), (tx, t_hl + HAIR_DY))
    return rgb, alpha


HAND_UNITS = {"m": 108, "f": 96}   # wrist to fingertips, in rig units


def register_hands(rgb, alpha, g):
    """Each painted hand is cut at its wrist (the narrowest row of the upper blob, dropping any
    forearm or cuff the painter added), scaled to a natural hand length, turned to continue the
    forearm and pinned with its wrist just inside the sleeve at the hand bone."""
    import math
    lab, n = ndimage.label(alpha > 0.4)
    if n < 2:
        return rgb, alpha
    sizes = ndimage.sum(np.ones_like(alpha), lab, range(1, n + 1))
    idx = np.argsort(sizes)[::-1][:2] + 1
    Wb = ip.compose.world_bones(g)
    out_rgb = np.zeros_like(rgb)
    out_a = np.zeros_like(alpha)
    objs = ndimage.find_objects(lab)
    for i in idx:
        sl = objs[i - 1]
        y0, y1, x0, x1 = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        side = "N" if (x0 + x1) / 2 < W / 2 else "F"
        blob = lab == i
        h = y1 - y0
        widths = blob[y0:y1].sum(1).astype(np.float32)
        # the palm is the widest part of the upper blob; the wrist is the narrowest row above it,
        # once past the thin tip where the blob starts
        upper = widths[: max(8, int(h * 0.7))]
        palm = int(np.argmax(upper))
        start = int(np.argmax(upper >= 0.6 * upper[palm]))
        wrist = y0 + start + int(np.argmin(widths[start: max(start + 1, palm)]))
        keep = blob.copy()
        keep[: max(0, wrist - 6)] = False
        keep = ndimage.binary_dilation(keep, iterations=2) & (np.arange(H)[:, None] >= wrist - 6)
        cols = np.nonzero(blob[wrist])[0]
        cw = float(cols.mean()) if len(cols) else (x0 + x1) / 2
        ys_, xs_ = np.nonzero(blob & (np.arange(H)[:, None] > wrist))
        phi = math.atan2(xs_.mean() - cw, ys_.mean() - wrist) if len(xs_) else 0.0
        tau = math.radians(-22.0 if side == "N" else 22.0)
        s = HAND_UNITS[g] * ip.BODY_PPU / max(1, y1 - wrist)
        hx, hy = Wb["hand" + side][0], Wb["hand" + side][1]
        ang = math.radians(rig_angle(side))
        up = 8.0   # the wrist sits just inside the cuff
        dst = ip.body_px((hx - math.sin(ang) * up, hy + math.cos(ang) * up))
        r2, a2 = warp(rgb, alpha * keep, s, (cw, float(wrist)), dst, theta=phi - tau)
        out_rgb = np.where(a2[..., None] > out_a[..., None], r2, out_rgb)
        out_a = np.maximum(out_a, a2)
        print(f"   hand {side}: wrist row {wrist - y0}/{h}, scale {s:.3f}, turn {math.degrees(phi - tau):.1f}")
    return out_rgb, out_a


def rig_angle(side):
    import rig
    return rig.world_angle("hand" + side)


def save(rgb, alpha, g, name, color=False):
    folder = os.path.join(DST, "male" if g == "m" else "female")
    os.makedirs(folder, exist_ok=True)
    out = os.path.join(folder, name + ".png")
    old = os.path.join(DST, "_old", "male" if g == "m" else "female")
    if os.path.exists(out) and not os.path.exists(os.path.join(old, name + ".png")):
        os.makedirs(old, exist_ok=True)
        shutil.copy2(out, old)
    if color:
        arr = np.dstack([rgb, alpha])
    else:
        lum = rgb[..., 0] * .3 + rgb[..., 1] * .59 + rgb[..., 2] * .11
        arr = np.dstack([lum, lum, lum, alpha])
    Image.fromarray((np.clip(arr, 0, 1) * 255 + .5).astype(np.uint8), "RGBA").save(out)
    print("   ->", os.path.relpath(out, ROOT))


PAT = re.compile(r"^([mf])_(outfit|hair)_(\d+)\.png$|^([mf])_hands\.png$")
HEAD_PAT = re.compile(r"^([mf])_(face|eyes|brows|nose|mouth|beard)_(\d+)\.png$")
HS = 1024   # head canvas


def head_tpl_sil(g):
    a = np.asarray(Image.open(os.path.join(TPL, f"head_{'male' if g == 'm' else 'female'}.png")).convert("RGBA"),
                   np.float32) / 255.0
    r, gg, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    return (al > 0.5) & ~((gg > 0.6) & (r < 0.4) & (b < 0.4))


def _head_box(sil):
    """Top of the skull, centre line and widest span (ears) of a head silhouette above the jaw."""
    rows = np.nonzero(sil.any(1))[0]
    top = int(rows[0])
    best, cx = 0, sil.shape[1] / 2
    for y in range(top, min(sil.shape[0], top + int(sil.shape[0] * 0.55))):
        xs = np.nonzero(sil[y])[0]
        if len(xs) and xs[-1] - xs[0] > best:
            best, cx = xs[-1] - xs[0], (xs[-1] + xs[0]) / 2
    return top, cx, best


def register_face(rgb, alpha):
    """Fit the painted bare head onto the template head: skull top and ear-to-ear width."""
    g = register_face.g
    t_top, t_cx, t_w = _head_box(head_tpl_sil(g))
    # the painted head: largest blob
    lab, n = ndimage.label(alpha > 0.5)
    if n == 0:
        return rgb, alpha
    sizes = ndimage.sum(np.ones_like(alpha), lab, range(1, n + 1))
    blob = lab == (int(np.argmax(sizes)) + 1)
    p_top, p_cx, p_w = _head_box(blob)
    s = t_w / max(1, p_w)
    print(f"   face fit: scale {s:.3f}")
    return warp(rgb, alpha, s, (p_cx, p_top), (t_cx, t_top))


def _pair(alpha):
    """Centroids of the two largest blobs (left, right)."""
    lab, n = ndimage.label(ndimage.binary_closing(alpha > 0.25, iterations=3))
    if n < 2:
        return None
    sizes = ndimage.sum(np.ones_like(alpha), lab, range(1, n + 1))
    idx = np.argsort(sizes)[::-1][:2] + 1
    cs = ndimage.center_of_mass(np.ones_like(alpha), lab, idx)
    return sorted(cs, key=lambda c: c[1])


def head_anchor_px(g, key, side=None):
    import rig
    x, y = rig.FACE[g][key]
    if side == "L":
        x = -x
    hx = 512 + x * ip.HEAD_PPU
    hy = 780 - y * ip.HEAD_PPU
    return hx, hy


HEAD_FIT = {}   # per gender: the (scale, src mid, dst mid) found on the eye paintings


def register_pair(rgb, alpha, g, key):
    """Eyes / brows: scale by the spacing of the pair, centre them between the template anchors."""
    pr = _pair(alpha)
    if pr is None:
        return rgb, alpha, None
    (ly, lx), (ry, rx) = pr
    tl, tr = head_anchor_px(g, key, "L"), head_anchor_px(g, key)
    s = (tr[0] - tl[0]) / max(1.0, rx - lx)
    src = ((lx + rx) / 2, (ly + ry) / 2)
    dst = ((tl[0] + tr[0]) / 2, (tl[1] + tr[1]) / 2)
    print(f"   {key} fit: scale {s:.3f}")
    return (*warp(rgb, alpha, s, src, dst), (s, src, dst))


# natural widths (rig units) of single features, per variant
SINGLE_W = {
    ("m", "nose"): [19, 20, 21, 25], ("f", "nose"): [15, 16, 17, 19],
    ("m", "mouth"): [32, 33, 30, 33, 32], ("f", "mouth"): [27, 29, 25, 29, 27],
}
EYE_GROW = 1.12   # painted eyes come out a little small for the face; grow each about its own centre


def grow_pair(rgb, alpha, g, key, k):
    out_r, out_a = np.zeros_like(rgb), np.zeros_like(alpha)
    half = np.zeros(alpha.shape, np.float32)
    half[:, : alpha.shape[1] // 2] = 1
    for side, m in (("L", half), ("R", 1 - half)):
        c = head_anchor_px(g, key, "L" if side == "L" else None)
        r2, a2 = warp(rgb * m[..., None], alpha * m, k, c, c)
        out_r += r2
        out_a += a2
    return out_r, np.clip(out_a, 0, 1)


def shading_only(rgb, alpha):
    """A painted nose comes with a patch of skin around it; keep only the shading darker than that
    skin, as a soft dark overlay, so it sits on any face colour without a visible patch."""
    lum = rgb[..., 0] * .3 + rgb[..., 1] * .59 + rgb[..., 2] * .11
    m = alpha > 0.6
    if m.sum() < 50:
        return rgb, alpha
    skin = float(np.percentile(lum[m], 85))
    dark = np.clip((skin - lum) / max(0.05, skin), 0, 1)
    a2 = np.clip(dark * 1.1, 0, 0.55) * alpha
    tone = np.full(lum.shape, 0.35, np.float32)
    return np.dstack([tone, tone, tone, rgb[..., 3]]) if rgb.shape[2] == 4 else np.dstack([tone, tone, tone]), a2


def register_single(rgb, alpha, g, key, i=0):
    """Nose / mouth: normalise the width of the painted feature and put it on its anchor.
    Beards: Gemini frames the head like it did for the eyes, so reuse that transform."""
    if (g, key) in SINGLE_W:
        lab, n = ndimage.label(ndimage.binary_closing(alpha > 0.3, iterations=4))
        if n == 0:
            return rgb, alpha
        sizes = ndimage.sum(np.ones_like(alpha), lab, range(1, n + 1))
        big = lab == (int(np.argmax(sizes)) + 1)
        ys, xs = np.nonzero(big)
        w = xs.max() - xs.min() + 1
        widths = SINGLE_W[(g, key)]
        target = widths[min(i, len(widths) - 1)] * ip.HEAD_PPU
        s = target / max(1, w)
        cy, cx = (ys.min() + ys.max()) / 2, (xs.min() + xs.max()) / 2
        ax, ay = head_anchor_px(g, key)
        if key == "nose":
            # the nose hangs from its tip: pin the bottom of the painting just below the anchor
            cy = float(ys.max())
            ay += 8 * ip.HEAD_PPU
        print(f"   {key} fit: width {w}px -> scale {s:.3f}")
        return warp(rgb, alpha * big, s, (cx, cy), (ax, ay))
    if key == "beard":
        return register_beard(rgb, alpha, g)
    return register_single_framed(rgb, alpha, g, key)


def register_beard(rgb, alpha, g):
    """Facial hair hangs from just under the nose.  Gemini sometimes zooms in: judge that by the
    width of the moustache (the top rows) against a natural one, then pin its top centre."""
    mask = ndimage.binary_closing(alpha > 0.3, iterations=3)
    ys, xs = np.nonzero(mask)
    if len(ys) < 50:
        return rgb, alpha
    top = ys.min()
    band = mask[top: top + max(8, int((ys.max() - top) * 0.25))]
    bx = np.nonzero(band.any(0))[0]
    w = bx.max() - bx.min() + 1
    expect = 52 * ip.HEAD_PPU
    r = w / expect
    s = 1.0 / r if r > 1.1 else 1.0
    cx = (bx.max() + bx.min()) / 2
    nx, ny = head_anchor_px(g, "nose")
    print(f"   beard fit: moustache {w}px, scale {s:.3f}")
    return warp(rgb, alpha, s, (cx, float(top)), (512.0, ny + 14 * ip.HEAD_PPU))


def register_single_framed(rgb, alpha, g, key):
    """Nose / mouth / beard: Gemini frames the head the same way on every painting, so reuse the
    transform measured on the eyes; then centre the part on the face's centre line."""
    fit = HEAD_FIT.get(g)
    if fit is None:
        return rgb, alpha
    s, src, dst = fit
    rgb2, a2 = warp(rgb, alpha, s, src, dst)
    ys, xs = np.nonzero(a2 > 0.3)
    if len(xs):
        lab, n = ndimage.label(a2 > 0.3)
        sizes = ndimage.sum(np.ones_like(a2), lab, range(1, n + 1))
        big = lab == (int(np.argmax(sizes)) + 1)
        cy, cx = ndimage.center_of_mass(big)
        if key in ("nose", "mouth"):
            ax, ay = head_anchor_px(g, key)
            rgb2, a2 = warp(rgb2, a2, 1.0, (cx, cy), (ax, ay))
        else:
            rgb2, a2 = warp(rgb2, a2, 1.0, (cx, cy), (512, cy))
    return rgb2, a2


def despill(rgb, alpha, spill):
    """Shading painted into the green (eyeshadow, soft lid shadows) keeps a green cast after keying:
    turn it into a warm skin shadow and fade it."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    lum = r * .3 + g * .59 + b * .11
    warm = np.dstack([lum * 1.0, lum * 0.82, lum * 0.72])
    k = np.clip((spill - 0.02) * 10, 0, 1)[..., None]
    rgb2 = rgb * (1 - k) + warm * k
    a2 = alpha * (1 - 0.7 * k[..., 0])
    return rgb2, a2


def main_head(only=None):
    files = sorted(n for n in os.listdir(RAW) if HEAD_PAT.match(n) and (not only or n.startswith(tuple(only))))
    # eyes first: they give the framing used by the single parts
    order = {"eyes": 0, "brows": 1, "face": 2, "nose": 3, "mouth": 3, "beard": 3}
    files.sort(key=lambda n: order[HEAD_PAT.match(n).group(2)])
    fits = {}
    for n in files:
        g, kind, i = HEAD_PAT.match(n).groups()
        i = int(i)
        print(n)
        rgb, alpha = load(os.path.join(RAW, n), (HS, HS))
        alpha = strip_sparkle(alpha)
        sp = load.spill
        rgb = np.dstack([rgb, sp])   # carry the spill map through the warps as a 4th channel
        if kind == "face":
            register_face.g = g
            rgb, alpha = register_face(rgb, alpha)
        elif kind in ("eyes", "brows"):
            rgb, alpha, fit = register_pair(rgb, alpha, g, "eye" if kind == "eyes" else "brow")
            if kind == "eyes":
                rgb, alpha = grow_pair(rgb, alpha, g, "eye", EYE_GROW)
            if fit and kind == "eyes":
                fits.setdefault(g, []).append(fit)
                ss = sorted(fits[g], key=lambda f: f[0])
                HEAD_FIT[g] = ss[len(ss) // 2]
        else:
            rgb, alpha = register_single(rgb, alpha, g, kind, i)
            if kind == "nose":
                rgb, alpha = shading_only(rgb, alpha)
        spill, rgb = rgb[..., 3], rgb[..., :3]
        if kind == "face":
            rgb = ip.normalise(rgb, alpha)
        if kind in ("eyes", "mouth"):
            rgb, alpha = despill(rgb, alpha, spill)
        save(rgb, alpha, g, f"{kind}_{i}", color=kind in ("eyes", "mouth"))


def main(only=None):
    for n in sorted(os.listdir(RAW)):
        m = PAT.match(n)
        if not m or (only and not n.startswith(tuple(only))):
            continue
        print(n)
        rgb, alpha = load(os.path.join(RAW, n))
        alpha = strip_sparkle(alpha)
        if m.group(1):
            g, kind, i = m.group(1), m.group(2), int(m.group(3))
            if kind == "hair":
                rgb, alpha = register_hair(rgb, alpha, g)
            save(rgb, alpha, g, f"{kind}_{i}")
        else:
            g = m.group(4)
            r2, a2 = register_hands(rgb, alpha, g)
            save(r2, a2, g, "hands")
            # until the other body has its own painting, it borrows these hands (rescaled to its frame)
            other = "f" if g == "m" else "m"
            if not os.path.exists(os.path.join(RAW, f"{other}_hands.png")):
                r2, a2 = register_hands(rgb, alpha, other)
                save(r2, a2, other, "hands")


if __name__ == "__main__":
    main(sys.argv[1:] or None)
    main_head(sys.argv[1:] or None)
