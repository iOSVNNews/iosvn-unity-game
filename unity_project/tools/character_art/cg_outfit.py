"""A full outfit painted (by ChatGPT) onto the real full-body template ArtSource/ModularTemplates/fulltpl_<g>.png.
The result is registered back onto the template through the untouched face, then split:
clothing (grey) -> ModularParts/<g>/outfit_N.png, bare hands (skin) -> ModularParts/<g>/hands.png (optional).
Usage: python cg_outfit.py <painted.png> <m|f> <N> [hands]"""
import os, sys
import numpy as np, cv2
from PIL import Image
from scipy import ndimage
import prep_gemini as pg
import compose

def register(out_rgb, tpl_rgb, mask):
    sift = cv2.SIFT_create(6000)
    k1, d1 = sift.detectAndCompute(cv2.cvtColor(out_rgb, cv2.COLOR_RGB2GRAY), None)
    k2, d2 = sift.detectAndCompute(cv2.cvtColor(tpl_rgb, cv2.COLOR_RGB2GRAY), (mask * 255).astype(np.uint8))
    m = cv2.BFMatcher().knnMatch(d1, d2, k=2)
    good = [a for a, b in m if a.distance < 0.78 * b.distance]
    src = np.float32([k1[a.queryIdx].pt for a in good]); dst = np.float32([k2[a.trainIdx].pt for a in good])
    M, inl = cv2.estimateAffinePartial2D(src, dst, method=cv2.RANSAC, ransacReprojThreshold=4.0)
    print("   matches", len(good), "inliers", int(inl.sum()), "scale", round(float(np.hypot(M[0, 0], M[1, 0])), 4),
          "shift", np.round(M[:, 2], 1))
    H, W = tpl_rgb.shape[:2]
    # a taller canvas: the painter often draws longer legs than the template, fitted below
    return cv2.warpAffine(out_rgb, M, (W, int(H * 1.4)), flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 255, 0))


def fit_legs(w, tpl_rgb, g):
    """Keep everything above the hips as registered by the face and stretch/squeeze the part below so
    the soles land on the template's soles (the rig's legs)."""
    H = tpl_rgb.shape[0]
    def sole(img):
        f = img.astype(np.float32) / 255
        fg = ~((f[..., 1] > .55) & (f[..., 0] < .45) & (f[..., 2] < .45))
        rows = np.nonzero(fg.sum(1) > 6)[0]
        return int(rows.max())
    hip = int(1990 - compose.world_bones(g)["hips"][1] * 1.5)
    s_t, s_o = sole(tpl_rgb), sole(w)
    k = (s_o - hip) / max(1, s_t - hip)
    print("   legs: sole", s_o, "->", s_t, "factor", round(k, 3))
    ys = np.arange(H, dtype=np.float32)
    src_y = np.where(ys <= hip, ys, hip + (ys - hip) * k)
    mapy = np.repeat(src_y[:, None], w.shape[1], 1).astype(np.float32)
    mapx = np.repeat(np.arange(w.shape[1], dtype=np.float32)[None, :], H, 0)
    return cv2.remap(w, mapx, mapy, cv2.INTER_CUBIC, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 255, 0))

def head_mask(t):
    sat = t.max(-1) - t.min(-1)
    skin = (sat > 0.08) & (t[..., 0] > t[..., 2]) & ~((t[..., 1] > .6) & (t[..., 0] < .4))
    skin[1100:] = False
    return skin

def run(path, g, n, want_hands=False):
    G = "male" if g == "m" else "female"
    tpl = np.asarray(Image.open(os.path.join(pg.TPL, f"fulltpl_{G}.png")).convert("RGB"))
    t = tpl.astype(np.float32) / 255
    face = head_mask(t)
    feat = ndimage.binary_dilation(face, iterations=4)
    w = register(np.asarray(Image.open(path).convert("RGB")), tpl, feat)
    w = fit_legs(w, tpl, g).astype(np.float32) / 255
    r, gg, b = w[..., 0], w[..., 1], w[..., 2]
    green = np.clip((gg - np.maximum(r, b)) / 0.30, 0, 1)
    alpha = 1 - green
    sat = w.max(-1) - w.min(-1)
    skin = (sat > 0.07) & (r > b + 0.04) & (alpha > 0.5)
    skin = ndimage.binary_opening(skin, iterations=1)
    # the head and neck belong to the head layers; clothing is everything else that is not skin
    Wb = compose.world_bones(g)
    neck_y = int(1990 - Wb["neck"][1] * 1.5)
    head_zone = np.zeros_like(skin); head_zone[: neck_y + 40] = True
    head = ndimage.binary_dilation(skin & head_zone, iterations=2)
    # bare hands only near the hand bones (skin seen through sheer sleeves stays with the cloth)
    H_, W_ = skin.shape
    yy, xx = np.mgrid[0:H_, 0:W_].astype(np.float32)
    near_hand = np.zeros_like(skin)
    import math
    for side in ("N", "F"):
        hx, hy, ha = Wb["hand" + side]
        px, py = 512 + hx * 1.5, 1990 - hy * 1.5
        # the hand hangs from the bone along the arm direction
        lx, ly = Wb["arm" + side + "_lo"][:2]
        dx, dy = (hx - lx), (hy - ly); n_ = math.hypot(dx, dy) or 1
        ex, ey = px + dx / n_ * 120 * 1.5, py - dy / n_ * 120 * 1.5
        sx, sy = px - dx / n_ * 25 * 1.5, py + dy / n_ * 25 * 1.5
        vx, vy = ex - sx, ey - sy; L2 = vx * vx + vy * vy
        t_ = np.clip(((xx - sx) * vx + (yy - sy) * vy) / L2, 0, 1)
        d = np.hypot(xx - (sx + t_ * vx), yy - (sy + t_ * vy))
        near_hand |= d < 48 * 1.5
    hands = skin & ~head_zone & near_hand
    lab_h, nh = ndimage.label(hands)
    if nh:
        hs = ndimage.sum(np.ones_like(lum0 := w[..., 0]), lab_h, range(1, nh + 1))
        hands = np.isin(lab_h, np.nonzero(hs > 150)[0] + 1)
    cloth = (alpha > 0.02) & ~head & ~hands
    lum = np.clip(0.3 * r + 0.59 * np.minimum(gg, np.maximum(r, b)) + 0.11 * b, 0, 1)
    a_cloth = np.where(cloth, alpha, 0) * ~ndimage.binary_dilation(hands | head, iterations=1)
    lab, k = ndimage.label(a_cloth > 0.3)
    if k:
        sizes = ndimage.sum(np.ones_like(lum), lab, range(1, k + 1))
        a_cloth *= ndimage.binary_dilation(np.isin(lab, np.nonzero(sizes > 300)[0] + 1), iterations=2)
    out = np.dstack([lum, lum, lum, a_cloth])
    dst = os.path.join(pg.DST, G, f"outfit_{n}.png")
    os.makedirs(os.path.join(pg.DST, "_old", G), exist_ok=True)
    if os.path.exists(dst):
        Image.open(dst).save(os.path.join(pg.DST, "_old", G, f"outfit_{n}_pre_cg.png"))
    Image.fromarray((out * 255 + .5).astype(np.uint8), "RGBA").save(dst)
    if want_hands:
        hl = np.clip(lum / max(1e-3, np.percentile(lum[hands], 90)) * 0.95, 0, 1) if hands.any() else lum
        ha = ndimage.gaussian_filter(ndimage.binary_dilation(hands, iterations=1).astype(np.float32), 0.7)
        hp = os.path.join(pg.DST, G, f"hands_{n}.png")
        if os.path.exists(hp):
            Image.open(hp).save(os.path.join(pg.DST, "_old", G, "hands_pre_cg.png"))
        Image.fromarray((np.dstack([hl, hl, hl, ha]) * 255 + .5).astype(np.uint8), "RGBA").save(hp)
    # registration check sheet
    chk = Image.fromarray(tpl).convert("RGBA")
    o = np.dstack([lum, lum, lum, a_cloth * .85])
    chk.alpha_composite(Image.fromarray((o * 255).astype(np.uint8), "RGBA"))
    chk.convert("RGB").resize((512, 1024)).save(f"/tmp/claude-0/gem/chk_{g}_{n}.jpg")
    return dst

if __name__ == "__main__":
    print(run(sys.argv[1], sys.argv[2], int(sys.argv[3]), len(sys.argv) > 4))
