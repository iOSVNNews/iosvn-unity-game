"""Cut the painted HUD kit (ChatGPT sheet on #00FF00) into separate transparent sprites."""
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage

def key(path):
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32) / 255
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    spill = g - np.maximum(r, b)
    bg = (g > 0.62) & (spill > 0.38)
    # soft edge: partial alpha where the green spill fades
    alpha = np.clip(1 - (spill - 0.12) / 0.3, 0, 1)
    alpha = np.where(bg, 0, np.maximum(alpha, 0.0))
    alpha = np.where(spill < 0.12, 1, alpha)
    # despill: clamp green to the max of red/blue where it leaks
    g2 = np.where(alpha < 1, np.minimum(g, np.maximum(r, b) + 0.04), g)
    rgba = np.dstack([r, g2, b, alpha])
    return rgba

def pieces(rgba):
    m = rgba[..., 3] > 0.3
    m = ndimage.binary_closing(m, iterations=3)
    lab, n = ndimage.label(m)
    out = []
    for i, sl in enumerate(ndimage.find_objects(lab)):
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if h * w < 3000:
            continue
        out.append((sl, (sl[1].start + sl[1].stop) / 2, (sl[0].start + sl[0].stop) / 2, w, h))
    return out

def save(rgba, sl, path, pad=4, square=False):
    y0, y1 = max(0, sl[0].start - pad), sl[0].stop + pad
    x0, x1 = max(0, sl[1].start - pad), sl[1].stop + pad
    crop = rgba[y0:y1, x0:x1]
    im = Image.fromarray((np.clip(crop, 0, 1) * 255 + .5).astype(np.uint8), "RGBA")
    if square:
        s = max(im.size); c = Image.new("RGBA", (s, s), (0, 0, 0, 0)); c.paste(im, ((s - im.size[0]) // 2, (s - im.size[1]) // 2)); im = c
    im.save(path)
    print(os.path.basename(path), im.size)

if __name__ == "__main__":
    src_a, src_b, out = sys.argv[1], sys.argv[2], sys.argv[3]
    A = key(src_a); B = key(src_b)
    pa = sorted(pieces(A), key=lambda p: (round(p[2] / 300), p[1]))
    pb = sorted(pieces(B), key=lambda p: (round(p[2] / 300), p[1]))
    for name, (img, lst) in {"a": (A, pa), "b": (B, pb)}.items():
        print(name, [(int(p[1]), int(p[2]), p[3], p[4]) for p in lst])
    # sheet a: [stick_base, knob, attack_ring, skill_ring, item_slot, bar]; same order on b
    names = ["stick_base", "stick_knob", "attack_ring", "skill_ring", "item_slot", "bar_frame"]
    pick = {"stick_base": ("a", 0), "stick_knob": ("b", 2), "attack_ring": ("a", 2), "skill_ring": ("b", 4), "item_slot": ("a", 4), "bar_frame": ("a", 5)}
    for nm, (which, k) in pick.items():
        img, lst = (A, pa) if which == "a" else (B, pb)
        save(img, lst[k][0], os.path.join(out, nm + ".png"), square=nm != "bar_frame" and nm != "item_slot")
