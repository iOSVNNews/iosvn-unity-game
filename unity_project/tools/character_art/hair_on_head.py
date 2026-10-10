"""Hair painted straight onto the real head template (ArtSource/ModularTemplates/headtpl_<g>.png).
The painter may rescale or crop the canvas, so the result is first registered back onto the template
by matching the untouched face and body (SIFT + RANSAC similarity), then the hair is whatever differs
from the template: over the green it is keyed, over the head/body it is the colour difference.
Usage: python hair_on_head.py <painted.png> <m|f> <index>"""
import os, sys
import numpy as np, cv2
from PIL import Image
from scipy import ndimage
import prep_gemini as pg

def register_face(out_rgb, tpl_rgb):
    """Multi-scale template match of the template's face (brows to mouth) inside the painting."""
    t = tpl_rgb.astype(np.float32) / 255
    skin = (t.max(-1) - t.min(-1) > 0.08) & (t[..., 0] > t[..., 2]) & ~((t[..., 1] > .6) & (t[..., 0] < .4))
    ys, xs = np.nonzero(skin[:1100])
    top, bot = ys.min(), ys.max()
    cx = int(np.median(xs))
    y0, y1 = int(top + 0.30 * (bot - top)), int(top + 0.62 * (bot - top))
    half = int(0.30 * (bot - top))
    patch = cv2.cvtColor(tpl_rgb[y0:y1, cx - half:cx + half], cv2.COLOR_RGB2GRAY)
    img = cv2.cvtColor(out_rgb, cv2.COLOR_RGB2GRAY)
    best = (-1, None, None)
    for s in np.arange(0.55, 1.8, 0.02):
        # scale the painting so its face matches the template face size
        im = cv2.resize(img, None, fx=1 / s, fy=1 / s, interpolation=cv2.INTER_AREA)
        if im.shape[0] < patch.shape[0] or im.shape[1] < patch.shape[1]:
            continue
        r = cv2.matchTemplate(im, patch, cv2.TM_CCOEFF_NORMED)
        _, v, _, loc = cv2.minMaxLoc(r)
        if v > best[0]:
            best = (v, s, loc)
    v, s, loc = best
    k = 1 / s
    # painting pixel p maps to template: (p * k) - loc + (cx - half, y0)
    M = np.float32([[k, 0, cx - half - loc[0]], [0, k, y0 - loc[1]]])
    print("   face match", round(v, 3), "scale", round(k, 4))
    return M, v


def register(out_rgb, tpl_rgb, tpl_fg):
    M, v = register_face(out_rgb, tpl_rgb)
    H, W = tpl_rgb.shape[:2]
    if v > 0.5:
        return cv2.warpAffine(out_rgb, M, (W, H), flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 255, 0))
    return register_sift(out_rgb, tpl_rgb, tpl_fg)


def register_sift(out_rgb, tpl_rgb, tpl_fg):
    sift = cv2.SIFT_create(4000)
    g1 = cv2.cvtColor(out_rgb, cv2.COLOR_RGB2GRAY)
    g2 = cv2.cvtColor(tpl_rgb, cv2.COLOR_RGB2GRAY)
    k1, d1 = sift.detectAndCompute(g1, None)
    k2, d2 = sift.detectAndCompute(g2, (tpl_fg * 255).astype(np.uint8))
    m = cv2.BFMatcher().knnMatch(d1, d2, k=2)
    good = [a for a, b in m if a.distance < 0.75 * b.distance]
    src = np.float32([k1[a.queryIdx].pt for a in good])
    dst = np.float32([k2[a.trainIdx].pt for a in good])
    M, inl = cv2.estimateAffinePartial2D(src, dst, method=cv2.RANSAC, ransacReprojThreshold=4.0)
    print("   matches", len(good), "inliers", int(inl.sum()), "scale", round(float(np.hypot(M[0, 0], M[1, 0])), 4))
    H, W = tpl_rgb.shape[:2]
    warped = cv2.warpAffine(out_rgb, M, (W, H), flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 255, 0))
    return warped

def extract(path, g, idx):
    G = "male" if g == "m" else "female"
    tpl = np.asarray(Image.open(os.path.join(pg.TPL, f"headtpl_{G}.png")).convert("RGB"))
    out = np.asarray(Image.open(path).convert("RGB"))
    t = tpl.astype(np.float32) / 255
    tpl_fg = ~((t[..., 1] > .6) & (t[..., 0] < .4) & (t[..., 2] < .4))
    # features only from the face and upper body (the part the painter must leave alone)
    feat = tpl_fg.copy(); feat[1100:] = False
    w = register(out, tpl, ndimage.binary_erosion(feat, iterations=6)).astype(np.float32) / 255
    r, gg, b = w[..., 0], w[..., 1], w[..., 2]
    green = np.clip((gg - np.maximum(r, b)) / 0.35, 0, 1)
    key_alpha = 1 - green                                  # hair over the background
    dist = np.sqrt(((w - t) ** 2).sum(-1))
    sat = w.max(-1) - w.min(-1)
    lum0 = 0.3 * r + 0.59 * gg + 0.11 * b
    tl = 0.3 * t[..., 0] + 0.59 * t[..., 1] + 0.11 * t[..., 2]
    # over the head and body the hair is the dark, unsaturated paint that was not there before
    diff_alpha = (np.clip((dist - 0.08) / 0.12, 0, 1) * np.clip((0.22 - sat) / 0.1, 0, 1)
                  * np.clip((0.62 - lum0) / 0.18, 0, 1))
    # the template's own dark strokes (brows, lashes, outlines) differ only by a slight misfit
    strokes = ndimage.binary_dilation(tpl_fg & (tl < 0.5), iterations=5)
    skin = (t.max(-1) - t.min(-1) > 0.08) & tpl_fg & (t[..., 0] > t[..., 2])
    sy = np.nonzero(skin[:900].any(1))[0]
    brow_top = int(sy.min() + 0.33 * (sy.max() - sy.min())) if len(sy) else 0
    strokes[:brow_top] = False          # above the brows the skull outline is simply covered by hair
    # over bare skin the hair is told apart by its lack of colour, so light highlights stay opaque
    over_skin = np.clip((dist - 0.06) / 0.1, 0, 1) * np.clip((0.11 - sat) / 0.05, 0, 1)
    diff_alpha = np.where(ndimage.binary_dilation(skin, iterations=2), np.maximum(diff_alpha, over_skin), diff_alpha)
    diff_alpha[strokes & (lum0 > tl - 0.25)] = 0
    # a painter may desaturate the whole face: inside the face only dark strands count as hair
    inner = ndimage.binary_erosion(skin, iterations=6)
    inner[: int(sy.min() + 0.12 * (sy.max() - sy.min()))] = False
    diff_alpha[inner & (lum0 > 0.32)] = 0
    diff_alpha = diff_alpha * ndimage.binary_opening(diff_alpha > 0.3, iterations=2)
    alpha = np.where(tpl_fg, diff_alpha, key_alpha)
    alpha[~tpl_fg & (sat > 0.25) & (green > 0.3)] = 0
    # keep the hair mass: the big connected pieces
    lab, n = ndimage.label(alpha > 0.4)
    if n:
        sizes = ndimage.sum(np.ones_like(alpha), lab, range(1, n + 1))
        keep = np.isin(lab, np.nonzero(sizes > max(400, sizes.max() * 0.004))[0] + 1)
        keep = ndimage.binary_dilation(keep, iterations=3)
        alpha *= keep
    alpha = ndimage.gaussian_filter(alpha, 0.6)
    # despill and gray
    lum = np.clip(0.3 * r + 0.59 * np.minimum(gg, np.maximum(r, b)) + 0.11 * b, 0, 1)
    rgba = np.dstack([lum, lum, lum, alpha])
    img = Image.fromarray((rgba * 255 + .5).astype(np.uint8), "RGBA")
    dst = os.path.join(pg.DST, G, f"hair_{idx}.png")
    os.makedirs(os.path.join(pg.DST, "_old", G), exist_ok=True)
    if os.path.exists(dst):
        Image.open(dst).save(os.path.join(pg.DST, "_old", G, f"hair_{idx}_prehead.png"))
    img.save(dst)
    return dst

if __name__ == "__main__":
    print(extract(sys.argv[1], sys.argv[2], int(sys.argv[3])))
