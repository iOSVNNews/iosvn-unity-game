"""Cuts Gemini feature sheets (ArtSource/GeminiRaw/<g>_eyesA.png, _eyesB, _brows, _noses, _mouths) into
head-canvas layers (ArtSource/ModularParts/<male|female>/eyes_N.png, brows_N, nose_N, mouth_N) that
import_painted.py already understands, and turns face bases / beards painted on the head template
(<g>_face_N.png, m_beard_N.png) into registered head-canvas layers.

Sheet items are found as blobs on the green, read in rows, and each one is scaled to the template's
feature size and centred on its face anchor.  Painters like to put skin under small features, so
anything lighter than the feature itself is unmixed against that skin and becomes see-through.

Eyes keep their irises painted blue: the blue is split into its own grey layer (eyesI_N) so the
creator can recolour it.
"""
import os
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

import import_painted as ip
import compose
import rig

ROOT = ip.ROOT
RAW = os.path.join(ROOT, "ArtSource", "GeminiRaw")
DST = os.path.join(ROOT, "ArtSource", "ModularParts")
TPL = os.path.join(ROOT, "ArtSource", "ModularTemplates")
HC = 1024  # head canvas size

# Target sizes in rig units, matched to the template face
SIZE = {
    "m": {"eye": 35, "brow": 43, "nose": 14, "mouth": 30},
    "f": {"eye": 36, "brow": 40, "nose": 12, "mouth": 26},
}


def load_rgb(path):
    return np.asarray(Image.open(path).convert("RGB"), np.float32) / 255.0


def green_alpha(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    spill = g - np.maximum(r, b)
    a = np.clip(1.0 - (spill - 0.12) / 0.30, 0, 1)
    rgb = np.dstack([r, np.minimum(g, np.maximum(r, b) + 0.04), b])
    return rgb, a


def items(alpha, grow):
    """Blobs (as bounding boxes) in reading order; the corner sparkle and dust are dropped."""
    H, W = alpha.shape
    m = alpha > 0.25
    # cell borders some painters add: long thin straight lines across the sheet
    def thin_runs(full, maxlen=8):
        out = np.zeros_like(full)
        idx = np.nonzero(full)[0]
        if len(idx):
            for run in np.split(idx, np.nonzero(np.diff(idx) > 1)[0] + 1):
                if len(run) <= maxlen:
                    out[run] = True
        return out
    rows_full = thin_runs(m.mean(1) > 0.45)
    cols_full = thin_runs(m.mean(0) > 0.45)
    m[rows_full, :] = False
    m[:, cols_full] = False
    lab, n = ndimage.label(ndimage.binary_dilation(m, iterations=grow))
    out = []
    for i, sl in enumerate(ndimage.find_objects(lab)):
        y0, y1, x0, x1 = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        area = int((m[sl] & (lab[sl] == i + 1)).sum())
        if area < 400:
            continue
        if x0 > W * 0.85 and y0 > H * 0.85 and (x1 - x0) < W * 0.08:
            continue
        out.append([y0, y1, x0, x1, i + 1])
    if not out:
        return out, lab
    hts = np.median([b[1] - b[0] for b in out])
    out.sort(key=lambda b: (b[0] + b[1]) / 2)
    rows, cur = [], [out[0]]
    for b in out[1:]:
        if abs((b[0] + b[1]) / 2 - np.mean([(c[0] + c[1]) / 2 for c in cur])) < hts * 0.7:
            cur.append(b)
        else:
            rows.append(cur); cur = [b]
    rows.append(cur)
    res = []
    for r in rows:
        res += sorted(r, key=lambda b: b[2])
    return res, lab


def place(canvas_rgba, crop, centre_units, width_units):
    """Paste an RGBA crop scaled to width_units, centred at centre_units (head canvas)."""
    h, w = crop.shape[:2]
    px_w = width_units * ip.HEAD_PPU
    s = px_w / max(1, w)
    im = Image.fromarray((np.clip(crop, 0, 1) * 255 + .5).astype(np.uint8), "RGBA")
    im = im.resize((max(1, int(round(w * s))), max(1, int(round(h * s)))), Image.LANCZOS)
    cx, cy = ip.head_px(centre_units)
    canvas_rgba.alpha_composite(im, (int(round(cx - im.size[0] / 2)), int(round(cy - im.size[1] / 2))))


def crop_of(rgb, alpha, box, lab=None, pad=6):
    y0, y1, x0, x1, k = box
    H, W = alpha.shape
    y0, y1, x0, x1 = max(0, y0 - pad), min(H, y1 + pad), max(0, x0 - pad), min(W, x1 + pad)
    a = alpha[y0:y1, x0:x1].copy()
    if lab is not None:
        a *= ndimage.binary_dilation(lab[y0:y1, x0:x1] == k, iterations=2)
    return rgb[y0:y1, x0:x1].copy(), a


def unmix(rgb, a, base, ink, gain=1.0):
    """Treat light paint around a feature as the skin it was painted on: the over-operator
    P = base*(1-t) + ink*t solved for t, per pixel, keeping only darkening."""
    lum = rgb[..., 0] * .3 + rgb[..., 1] * .59 + rgb[..., 2] * .11
    t = np.clip((base - lum) / max(1e-3, base - ink) * gain, 0, 1)
    return t * a


def trim(crop_rgb, crop_a, thr=0.03):
    ys, xs = np.nonzero(crop_a > thr)
    if len(xs) == 0:
        return crop_rgb, crop_a
    return crop_rgb[ys.min():ys.max() + 1, xs.min():xs.max() + 1], crop_a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def new_canvas():
    return Image.new("RGBA", (HC, HC), (0, 0, 0, 0))


def save(canvas, g, name):
    folder = os.path.join(DST, "male" if g == "m" else "female")
    old = os.path.join(DST, "_old", "male" if g == "m" else "female")
    out = os.path.join(folder, name + ".png")
    if os.path.exists(out) and not os.path.exists(os.path.join(old, name + ".png")):
        os.makedirs(old, exist_ok=True)
        import shutil
        shutil.copy2(out, old)
    canvas.save(out)


def eye_layers(rgb, a):
    """The eye white is the band under the upper lash line down to the lower lid line, found column
    by column; it, the iris and the lash line stay opaque.  Everything else (lid skin, creases,
    halo) is unmixed against the skin so only its dark strokes remain."""
    lum = rgb[..., 0] * .3 + rgb[..., 1] * .59 + rgb[..., 2] * .11
    blue = ((rgb[..., 2] - np.maximum(rgb[..., 0], rgb[..., 1])) > 0.10) & (a > 0.5)
    iris = ndimage.binary_opening(blue, iterations=1)
    dark = (lum < 0.33) & (a > 0.5)
    thick = ndimage.binary_opening(dark, iterations=2)
    lab, n = ndimage.label(thick)
    H, W = a.shape
    sclera = np.zeros_like(dark)
    lid = np.zeros_like(dark)
    if n:
        sizes = ndimage.sum(np.ones_like(lum), lab, range(1, n + 1))
        # the lash line: the biggest thick dark stroke touching the iris rows
        iy = np.nonzero(iris.any(1))[0]
        best, bestv = 0, -1
        for k in range(1, n + 1):
            ys_k = np.nonzero((lab == k).any(1))[0]
            near = len(iy) and ys_k.max() >= iy.min() - H * 0.15 and ys_k.min() <= iy.max()
            v = sizes[k - 1] * (2.0 if near else 1.0)
            if v > bestv:
                best, bestv = k, v
        lid = lab == best
        ih = (iy.max() - iy.min() + 1) if len(iy) else H * 0.3
        for x in range(W):
            col = np.nonzero(lid[:, x])[0]
            if len(col) == 0:
                continue
            yu = col.max() + 1
            yl = None
            for y in range(yu + 3, min(H, int(yu + ih * 1.05))):
                if lum[y, x] < 0.66 and not blue[y, x]:
                    yl = y
                    break
            if yl is None:
                # no lower lid in reach: only follow the iris down in its own columns
                ib = np.nonzero(iris[:, x])[0]
                if len(ib):
                    yl = ib.max() + 1
            if yl is not None:
                sclera[yu:yl, x] = True
        # columns where the lower lid was missed run too far down: clamp each column's bottom to
        # the smoothed bottom edge of its neighbours
        cols = np.nonzero(sclera.any(0))[0]
        if len(cols) > 5:
            bot = np.full(W, -1)
            for x in cols:
                bot[x] = np.nonzero(sclera[:, x])[0].max()
            sm = ndimage.median_filter(np.where(bot >= 0, bot, 0), size=max(5, len(cols) // 6))
            for x in cols:
                sclera[sm[x] + 2:, x] = False
        sclera = ndimage.binary_opening(ndimage.binary_closing(sclera, iterations=2), iterations=1)
    core = sclera | iris | ndimage.binary_dilation(lid, iterations=1)
    soft = ndimage.gaussian_filter(core.astype(np.float32), 0.8)
    out_mask = (a > 0.5) & ~core
    base = float(np.percentile(lum[out_mask], 75)) if out_mask.any() else 0.9
    outside = unmix(rgb, a, base, 0.08)
    # faint smudges become a grey film over the skin: only real strokes survive outside the eye
    outside = np.clip((outside - 0.22) / 0.78, 0, 1)
    # strokes painted well above the lash line read as a second eyebrow: drop them
    if lid.any():
        ly = np.nonzero(lid.any(1))[0].min()
        ih2 = (np.ptp(np.nonzero(iris.any(1))[0]) + 1) if iris.any() else H * 0.3
        cut = int(max(0, ly - 0.45 * ih2))
        outside[:cut] = 0
    alpha = np.maximum(soft * a, outside)
    gray = np.dstack([lum, lum, lum])
    gray = np.where(gray < 0.45, gray * 0.6, gray)
    brown = np.array([0.16, 0.10, 0.09], np.float32)
    ink = np.where(soft[..., None] > 0.5, gray, brown)
    iris_a = ndimage.gaussian_filter(iris.astype(np.float32), 0.8) * a
    ilum = np.clip(rgb[..., 2] * 0.85 + 0.1, 0, 1)
    iris_rgb = np.dstack([ilum, ilum, ilum])
    return np.dstack([ink, alpha]), np.dstack([iris_rgb, iris_a])


def do_eyes(g):
    anc = compose.anchors(g, {})
    pairs = []
    for sheet in ("eyesA", "eyesB"):
        p = os.path.join(RAW, f"{g}_{sheet}.png")
        if not os.path.exists(p):
            continue
        rgb, a = green_alpha(load_rgb(p))
        boxes, lab = items(a, max(8, int(a.shape[1] * 0.012)))
        singles = []
        for b in boxes:
            y0, y1, x0, x1, k = b
            if (x1 - x0) > 1.8 * (y1 - y0):
                # one blob holding both eyes: cut at the faintest column of its middle third
                sub = a[y0:y1, x0:x1] * (lab[y0:y1, x0:x1] == k)
                prof = sub.sum(0)
                w3 = (x1 - x0) // 3
                cut = x0 + w3 + int(np.argmin(prof[w3:2 * w3]))
                singles.append([y0, y1, x0, cut, k])
                singles.append([y0, y1, cut, x1, k])
            else:
                singles.append(b)
        for i in range(0, len(singles) - 1, 2):
            pairs.append([(rgb, a, singles[i], lab), (rgb, a, singles[i + 1], lab)])
    n = 0
    for pair in pairs:
        cv, ci = new_canvas(), new_canvas()
        for (rgb, a, box, lab), side in zip(pair, ("eyeL", "eyeR")):
            c_rgb, c_a = crop_of(rgb, a, box, lab)
            main, iris = eye_layers(c_rgb, c_a)
            # trim both on the main layer's box
            ys, xs = np.nonzero(main[..., 3] > 0.03)
            if len(xs) == 0:
                continue
            y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
            # width measured on the opaque eye itself (not the faint halo)
            ys2, xs2 = np.nonzero(main[..., 3] > 0.5)
            if len(xs2) == 0:
                continue
            wfrac = (xs.max() - xs.min() + 1) / max(1, xs2.max() - xs2.min() + 1)
            wu = SIZE[g]["eye"] * wfrac
            place(cv, main[y0:y1, x0:x1], anc[side], wu)
            place(ci, iris[y0:y1, x0:x1], anc[side], wu)
        save(cv, g, f"eyes_{n}")
        save(ci, g, f"eyesI_{n}")
        n += 1
    print(g, "eyes", n)


def do_brows(g):
    anc = compose.anchors(g, {})
    p = os.path.join(RAW, f"{g}_brows.png")
    if not os.path.exists(p):
        return
    rgb, a = green_alpha(load_rgb(p))
    boxes, lab = items(a, 6)
    n = 0
    for i in range(0, len(boxes) - 1, 2):
        cv = new_canvas()
        for box, side in ((boxes[i], "browL"), (boxes[i + 1], "browR")):
            c_rgb, c_a = crop_of(rgb, a, box, lab)
            lum = c_rgb[..., 0] * .3 + c_rgb[..., 1] * .59 + c_rgb[..., 2] * .11
            crop = np.dstack([lum, lum, lum, c_a])
            crop = np.dstack(trim(crop[..., :3], crop[..., 3]))
            place(cv, crop, anc[side], SIZE[g]["brow"])
        save(cv, g, f"brows_{n}")
        n += 1
    print(g, "brows", n)


def do_noses(g):
    anc = compose.anchors(g, {})
    p = os.path.join(RAW, f"{g}_noses.png")
    if not os.path.exists(p):
        return
    rgb, a = green_alpha(load_rgb(p))
    boxes, lab = items(a, 8)
    for n, box in enumerate(boxes):
        c_rgb, c_a = crop_of(rgb, a, box, lab)
        lum = c_rgb[..., 0] * .3 + c_rgb[..., 1] * .59 + c_rgb[..., 2] * .11
        base = float(np.percentile(lum[c_a > 0.5], 85)) if (c_a > 0.5).any() else 0.95
        t = unmix(c_rgb, c_a, base, 0.25)
        # keep the tip and nostrils; the bridge and the sides of the painted patch fade to a whisper
        ys_ = np.nonzero((t > 0.08).any(1))[0]
        if len(ys_):
            y0_, y1_ = ys_.min(), ys_.max()
            v = (np.arange(t.shape[0]) - y0_) / max(1, y1_ - y0_)
            w = np.clip((v - 0.55) / 0.2, 0, 1) * 0.95 + 0.05
            t = t * w[:, None]
        crop = np.dstack([np.full_like(lum, 0.25)] * 3 + [t])
        crop = np.dstack(trim(crop[..., :3], crop[..., 3], 0.06))
        cv = new_canvas()
        x, y = anc["nose"]
        h_u = SIZE[g]["nose"] * crop.shape[0] / max(1, crop.shape[1])
        # the nostrils sit a little below the nose anchor; the bridge runs up from there
        place(cv, crop, (x, y - 4 + h_u / 2 - h_u * 0.75), SIZE[g]["nose"])
        save(cv, g, f"nose_{n}")
    print(g, "noses", len(boxes))


def do_mouths(g):
    anc = compose.anchors(g, {})
    p = os.path.join(RAW, f"{g}_mouths.png")
    if not os.path.exists(p):
        return
    rgb, a = green_alpha(load_rgb(p))
    boxes, lab = items(a, 4)
    for n, box in enumerate(boxes):
        c_rgb, c_a = crop_of(rgb, a, box, lab, pad=0)
        solid = c_a > 0.9
        # skin colour: the border of the patch
        border = np.zeros_like(solid)
        b = max(3, int(min(solid.shape) * 0.08))
        border[:b] = border[-b:] = True
        border[:, :b] = border[:, -b:] = True
        sk = np.median(c_rgb[border & solid], axis=0) if (border & solid).any() else np.array([0.95, 0.88, 0.84])
        dist = np.sqrt(((c_rgb - sk) ** 2).sum(-1))
        t = np.clip((dist - 0.03) / 0.14, 0, 1) * c_a
        t = ndimage.gaussian_filter(t, 0.6)
        col = (c_rgb - sk * (1 - t[..., None])) / np.maximum(t[..., None], 1e-3)
        col = np.clip(col, 0, 1)
        crop = np.dstack([col, t])
        crop = np.dstack(trim(crop[..., :3], crop[..., 3], 0.08))
        cv = new_canvas()
        place(cv, crop, anc["mouth"], SIZE[g]["mouth"])
        save(cv, g, f"mouth_{n}")
    print(g, "mouths", len(boxes))


def template_head_box(g):
    a = np.asarray(Image.open(os.path.join(TPL, f"head_{'male' if g == 'm' else 'female'}.png")).convert("RGBA"), np.float32) / 255.0
    rgb, al = green_alpha(a[..., :3])
    return head_box(al)


def head_box(alpha):
    """Top of skull, widest row above the chin (ears), its centre."""
    m = alpha > 0.5
    rows = np.nonzero(m.any(1))[0]
    top = rows[0]
    widths = [(np.nonzero(m[y])[0].max() - np.nonzero(m[y])[0].min()) if m[y].any() else 0 for y in range(top, top + 520)]
    widths = np.array(widths)
    k = int(np.argmax(widths[:420]))
    xs = np.nonzero(m[top + k])[0]
    return top, top + k, widths[k], (xs.min() + xs.max()) / 2


def do_faces(g):
    gd = "male" if g == "m" else "female"
    t_top, t_wy, t_w, t_cx = template_head_box(g)
    for i in range(4):
        p = os.path.join(RAW, f"{g}_face_{i}.png")
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGB").resize((HC, HC), Image.LANCZOS)
        rgb, a = green_alpha(np.asarray(im, np.float32) / 255.0)
        top, wy, w, cx = head_box(a)
        s = (t_wy - t_top) / max(1, wy - top)
        rgb2, a2 = warp_sq(rgb, a, s, (cx, top), (t_cx, t_top))
        tpl = np.asarray(Image.open(os.path.join(TPL, f"head_{gd}.png")).convert("RGB"), np.float32) / 255.0
        _, t_a = green_alpha(tpl)
        t_skin = (t_a > 0.5) & ~((np.abs(tpl[..., 0] - tpl[..., 1]) < 0.06) & (np.abs(tpl[..., 1] - tpl[..., 2]) < 0.06) & (tpl[..., 0] < 0.75))
        def chin_row(mask):
            w = mask.sum(1).astype(np.float32)
            k = int(np.argmax(w[:int(HC * 0.8)]))
            below_ = np.nonzero(w[k:] < 0.5 * w[k])[0]
            return k + int(below_[0]) if len(below_) else HC
        # the painted chin: the outline crossing the centre column below the mouth
        lum2 = rgb2[..., 0] * .3 + rgb2[..., 1] * .59 + rgb2[..., 2] * .11
        cxi = int(t_cx)
        y_from = int(ip.head_px((0, 22))[1])
        y_to = min(HC - 2, int(ip.head_px((0, -40))[1]))
        colv = ndimage.uniform_filter1d(lum2[:, cxi - 6:cxi + 7].mean(1), 3)
        seg = colv[y_from:y_to] - np.maximum.accumulate(colv[y_from:y_to])
        dip = y_from + int(np.argmin(colv[y_from:y_to])) if y_to > y_from else chin_row(t_skin)
        chin_y = dip + 3
        below = np.zeros_like(a2, bool); below[chin_y:] = True
        neck = ndimage.gaussian_filter(ndimage.binary_dilation(t_skin, iterations=2).astype(np.float32), 2.0)
        a2 = np.where(below, a2 * neck, a2)
        print("   chin row", chin_y)
        lum = rgb2[..., 0] * .3 + rgb2[..., 1] * .59 + rgb2[..., 2] * .11
        lum = flatten_face(lum, a2, g)
        out = Image.fromarray((np.dstack([lum, lum, lum, a2]) * 255 + .5).astype(np.uint8), "RGBA")
        save(out, g, f"face_{i}")
        print(g, "face", i, f"scale {s:.3f}")


def flatten_face(lum, a, g):
    """Painters sculpt eyes, nose and lips into a "blank" head.  Inside the face (between the brows
    and the chin, away from the outline) the shading is replaced by a heavily blurred copy, so only
    soft volume is left under the features that are layered on top."""
    m = a > 0.5
    inner = ndimage.binary_erosion(m, iterations=28)
    # only the face area, not the skull top or the ears
    yb = int(ip.head_px((0, 112))[1])     # above the brows
    inner[:yb] = False
    w = ndimage.gaussian_filter(inner.astype(np.float32), 10)
    num = ndimage.gaussian_filter(lum * m, 22)
    den = ndimage.gaussian_filter(m.astype(np.float32), 22)
    smooth = num / np.maximum(den, 1e-3)
    # keep the lit look of the skin: lift towards the face's bright level
    hi = float(np.percentile(lum[m], 90))
    smooth = np.clip(smooth + (hi - smooth) * 0.35, 0, 1)
    return lum * (1 - w) + smooth * w


def do_beards(g):
    if g != "m":
        return
    t_top, t_wy, t_w, t_cx = template_head_box(g)
    for i in range(1, 5):
        p = os.path.join(RAW, f"m_beard_{i}.png")
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGB").resize((HC, HC), Image.LANCZOS)
        rgb, a = green_alpha(np.asarray(im, np.float32) / 255.0)
        # a beard painted with a mouth opening (mustache joined to the beard) is placed by that opening:
        # its centre goes on the mouth anchor and its width becomes a little wider than the mouth
        solid = a > 0.5
        holes = ndimage.binary_fill_holes(solid) & ~solid
        lab, n = ndimage.label(holes)
        if n:
            sizes = ndimage.sum(np.ones_like(a), lab, range(1, n + 1))
            k = int(np.argmax(sizes)) + 1
            if sizes[k - 1] > 400:
                ys, xs = np.nonzero(lab == k)
                hw = xs.max() - xs.min() + 1
                anc = compose.anchors(g, {})
                mx, my = ip.head_px(anc["mouth"])
                s = SIZE[g]["mouth"] * 1.35 * ip.HEAD_PPU / hw
                bx = np.nonzero(solid.any(0))[0]
                by = np.nonzero(solid.any(1))[0]
                s = min(s, 340 / (bx[-1] - bx[0] + 1))                       # no wider than the jaw
                s = min(s, (1000 - my) / max(1, by[-1] - ys.mean()))         # hangs inside the head canvas
                rgb, a = warp_sq(rgb, a, s, (xs.mean(), ys.mean()), (mx, my))
                print("   beard", i, "placed by its mouth opening, scale", round(s, 3))
        lum = rgb[..., 0] * .3 + rgb[..., 1] * .59 + rgb[..., 2] * .11
        out = Image.fromarray((np.dstack([lum, lum, lum, a]) * 255 + .5).astype(np.uint8), "RGBA")
        save(out, g, f"beard_{i}")
        print(g, "beard", i)


def warp_sq(rgb, alpha, s, src, dst):
    a = 1 / s
    c = src[0] - dst[0] / s
    f = src[1] - dst[1] / s

    def one(ch):
        im = Image.fromarray((np.clip(ch, 0, 1) * 255).astype(np.uint8))
        return np.asarray(im.transform((HC, HC), Image.AFFINE, (a, 0, c, 0, a, f), resample=Image.BICUBIC), np.float32) / 255.0
    return np.dstack([one(rgb[..., k]) for k in range(3)]), one(alpha)


if __name__ == "__main__":
    for g in (sys.argv[1:] or ["m", "f"]):
        do_eyes(g)
        do_brows(g)
        do_noses(g)
        do_mouths(g)
        do_faces(g)
        do_beards(g)
