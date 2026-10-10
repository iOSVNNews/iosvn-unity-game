"""Close bald gaps: every hair gets the skull above its hairline covered.  The skull is the union
of all face bases of that gender (composed straight onto the body canvas); the gap is inpainted
from the surrounding hair so it keeps the painting's tone."""
import os, sys
import numpy as np, cv2
from PIL import Image
from scipy import ndimage
import compose
import prep_gemini as pg

def skull(g):
    lib = compose.Library(g)
    un = np.zeros((2048, 1024), bool)
    for fa in range(4):
        look = dict(compose.DEFAULT_LOOK[g]); look["fa"] = fa
        im = compose.compose(lib, look, size=(1024, 2048), ppu=1.5, origin=(512, 1990), only=("face",), bg=(0, 0, 0, 0))
        un |= np.asarray(im)[..., 3] > 128
    ys, xs = np.nonzero(un)
    top, chin = ys.min(), ys.max()
    cx = (xs.min() + xs.max()) / 2
    half = (xs.max() - xs.min()) / 2
    hl = top + 0.13 * (chin - top)   # only the crown: the painted hairline stays as painted
    Y, X = np.mgrid[0:2048, 0:1024]
    line = hl + 0.05 * (chin - top) * ((X - cx) / half) ** 2
    # hair-tip edge instead of a ruler line
    rng = np.random.default_rng(7)
    jag = ndimage.gaussian_filter1d(rng.normal(0, 1, 1024), 1.5) * 9 + np.abs(ndimage.gaussian_filter1d(rng.normal(0, 1, 1024), 3)) * 6
    line = line - jag[None, :]
    cap = un & (Y < line)
    skull.edge = np.clip((line - Y) / 14.0, 0, 1)
    skull.face, skull.eye = un, top + 0.42 * (chin - top)
    cap = ndimage.binary_dilation(cap, iterations=4) & (Y < line)
    return cap

def widen(a, g, n):
    """A face opening painted narrower than the face bases would leave hair lying on the cheeks:
    stretch the hair sideways (about the head centre) until the opening clears the face at eye level."""
    import rig
    hx, hy, _ = compose.world_bones(g)["head"]
    eye = int(1990 - (hy + rig.FACE[g]["eye"][1]) * 1.5)
    cx = int(512 + hx * 1.5)
    hair = a[..., 3] > 128
    face = skull.face
    gaps, faces = [], []
    for y in range(eye - 6, eye + 26, 4):
        row = hair[y]
        if row[cx]:
            continue
        l = np.nonzero(row[:cx])[0]; r = np.nonzero(row[cx:])[0]
        f = np.nonzero(face[y])[0]
        if len(l) and len(r) and len(f):
            gaps.append(cx + r[0] - l[-1]); faces.append(f[-1] - f[0])
    if not gaps:
        return a
    sx = float(np.clip(np.median(faces) * 1.03 / np.median(gaps), 1.0, 1.22))
    if sx <= 1.01:
        return a
    print("   widen", g, n, round(sx, 3))
    im = Image.fromarray(a, "RGBA")
    im = im.transform(im.size, Image.AFFINE, (1 / sx, 0, cx - cx / sx, 0, 1, 0), resample=Image.BICUBIC)
    return np.asarray(im).copy()


def fix(g):
    G = "male" if g == "m" else "female"
    cap = skull(g)
    d = os.path.join(pg.DST, G)
    for n in range(10):
        p = os.path.join(d, f"hair_{n}.png")
        src = os.path.join(pg.DST, "_prescalp", G, f"hair_{n}.png")
        a = np.asarray(Image.open(src if os.path.exists(src) else p).convert("RGBA")).copy()
        a = widen(a, g, n)
        al = a[..., 3].astype(np.float32) / 255
        gap = cap & (al < 0.6)
        # pockets of background enclosed by hair and face (an opening that runs up past the skull)
        solid = (al > 0.5) | skull.face
        pocket = ndimage.binary_fill_holes(solid) & ~solid
        pocket[int(skull.eye):] = False
        gap |= ndimage.binary_dilation(pocket, iterations=2) & (al < 0.6)
        gap = ndimage.binary_opening(gap, iterations=1)
        area = int(gap.sum())
        if area < 80:
            print(g, n, "ok"); continue
        gray = a[..., 0].astype(np.float32)
        # the fill takes the tone of the hair around it (normalised blur over the opaque hair)
        w = (al >= 0.5).astype(np.float32)
        fill = ndimage.gaussian_filter(gray * w, 18) / np.maximum(ndimage.gaussian_filter(w, 18), 1e-3)
        fill = np.where(ndimage.gaussian_filter(w, 18) > 0.02, fill, np.median(gray[al >= 0.5]))
        # hair-like texture: fine streaks running back from the hairline
        rng = np.random.default_rng(n)
        streak = ndimage.gaussian_filter(rng.normal(0, 1, (2048, 1024)), (10, 1.0)) * 70
        fill = np.clip(fill * 0.92 + streak, 0, 255)
        soft = np.clip(ndimage.gaussian_filter(gap.astype(np.float32), 1.2) * 1.5, 0, 1)
        # the lower edge thins out into strands instead of ending in a blob
        tips = np.clip(skull.edge + ndimage.gaussian_filter(rng.normal(0, 1, (2048, 1024)), (6, 0.8)) * 1.2 - 0.2, 0, 1)
        not_pocket = ~ndimage.binary_dilation(pocket, iterations=2)
        soft = np.where(not_pocket, soft * tips, soft)
        new_al = np.maximum(al, soft)
        g2 = np.where(new_al > 0, (gray * al + fill * (new_al - al)) / np.maximum(new_al, 1e-3), gray)
        out = np.dstack([g2, g2, g2, new_al * 255]).clip(0, 255).astype(np.uint8)
        Image.fromarray(out, "RGBA").save(p)
        print(g, n, "filled", area)

if __name__ == "__main__":
    for g in sys.argv[1:] or ("m", "f"):
        fix(g)
