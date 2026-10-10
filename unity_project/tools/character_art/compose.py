"""Layer order and a pose-aware compositor used for previews and templates.
The Unity runtime reads the same layer table from the exported JSON."""
import math
import numpy as np
from PIL import Image
import rig

# (layer id, bone, part name pattern, tint key, flags)
# patterns use {to} robe, {ha} hair, {fa} face, {ey} eyes, {br} brows, {no} nose, {mo} mouth, {bd} beard, {ma} mark, {hat} hat
LAYERS = [
    ("cape", "back", "cape_{to}", "oc", "flex"),
    ("hairB", "head", "hairB_{ha}", "hc", "flex"),
    ("handF", "handF", "handPF_{to}|handPF|hand_open", "sk", "far"),
    ("armF_up", "armF_up", "sleeveUF_{to}|sleeveU_{to}", "oc", "far"),
    ("plateF", "armF_up", "plate_{to}", "tc", "far"),
    ("armF_lo", "armF_lo", "sleeveLF_{to}|sleeveL_{to}", "oc", "far flex"),
    ("cuffF", "armF_lo", "cuffF_{to}|cuffT_{to}", "tc", "far"),
    ("skirtB", "skirt", "skirtB_{to}", "oc", "flex far"),
    ("thighF", "legF_up", "thighF_{to}|thigh", "pc", "far"),
    ("shinF", "legF_lo", "shinF_{to}|shin", "pc", "far"),
    ("bootF", "legF_lo", "bootF_{to}|boot", "sc", "far"),
    ("thighN", "legN_up", "thighN_{to}|thigh", "pc", ""),
    ("shinN", "legN_lo", "shinN_{to}|shin", "pc", ""),
    ("bootN", "legN_lo", "bootN_{to}|boot", "sc", ""),
    ("skirtF", "skirt", "skirtF_{to}", "oc", "flex"),
    ("skirtT", "skirt", "skirtT_{to}", "tc", "flex"),
    ("neck", "neck", "neck", "sk", ""),
    ("torso", "torso", "torso_{to}", "oc", ""),
    ("torsoT", "torso", "torsoT_{to}", "tc", ""),
    ("belt", "torso", "belt_{to}", "bc", ""),
    ("ears", "head", "ears_{fa}", "sk", ""),
    ("face", "head", "head_{fa}", "sk", ""),
    ("blush", "head", "blush", "", ""),
    ("mark", "head", "mark_{ma}", "mc", "anchor:mark"),
    ("eyeW_L", "head", "eyeW_{ey}", "", "anchor:eyeL mirror eye"),
    ("eyeI_L", "head", "eyeI_{ey}", "ec", "anchor:eyeL mirror eye"),
    ("eyeL_L", "head", "eyeL_{ey}", "", "anchor:eyeL mirror eye"),
    ("eyeW_R", "head", "eyeW_{ey}", "", "anchor:eyeR eye"),
    ("eyeI_R", "head", "eyeI_{ey}", "ec", "anchor:eyeR eye"),
    ("eyeL_R", "head", "eyeL_{ey}", "", "anchor:eyeR eye"),
    ("eyeP_L", "head", "eyePL_{ey}", "", "anchor:eyeL eye"),
    ("eyeP_R", "head", "eyePR_{ey}", "", "anchor:eyeR eye"),
    ("eyePI_L", "head", "eyePIL_{ey}", "ec", "anchor:eyeL eye"),
    ("eyePI_R", "head", "eyePIR_{ey}", "ec", "anchor:eyeR eye"),
    ("brow_L", "head", "browPL_{br}|brow_{br}", "hc", "anchor:browL mirror"),
    ("brow_R", "head", "browPR_{br}|brow_{br}", "hc", "anchor:browR"),
    ("nose", "head", "nose_{no}", "sk", "anchor:nose"),
    ("mouth", "head", "mouth_{mo}", "", "anchor:mouth"),
    ("beard", "head", "beard_{bd}", "hc", ""),
    ("hairF", "head", "hairF_{ha}", "hc", ""),
    ("hairS", "head", "hairS_{ha}", "", ""),
    ("hat", "head", "hat_{hat}", "hac", ""),
    ("handN", "handN", "handPN_{to}|handPN|hand_open", "sk", ""),
    ("armN_up", "armN_up", "sleeveUN_{to}|sleeveU_{to}", "oc", ""),
    ("plateN", "armN_up", "plate_{to}", "tc", ""),
    ("armN_lo", "armN_lo", "sleeveLN_{to}|sleeveL_{to}", "oc", "flex"),
    ("cuffN", "armN_lo", "cuffN_{to}|cuffT_{to}", "tc", ""),
]

DEFAULT_LOOK = {
    "m": dict(fa=1, ey=3, br=1, no=1, mo=0, bd=0, ha=0, to=1, hat=1, ma=0, sk="#f2d8be", hc="#1e1a1e", ec="#4a3020",
              oc="#2f5f63", tc="#e8e4dc", pc="#20242a", sc="#24282e", bc="#262a30", hac="#d8b46a", mc="#c8303c"),
    "f": dict(fa=1, ey=4, br=1, no=0, mo=3, bd=0, ha=0, to=1, hat=0, ma=1, sk="#f6dcc4", hc="#24202c", ec="#3a2a28",
              oc="#b0c8ea", tc="#f4eef6", pc="#e8e2ea", sc="#c8b8d0", bc="#8a3a5a", hac="#e2c57b", mc="#c8303c"),
}


def hex_rgb(h):
    return np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], np.float32) / 255.0


def anchors(g, look):
    F = rig.FACE[g]
    es = (look.get("es", 10) - 10) * .6
    eh = (look.get("eh", 10) - 10) * .5
    bh = (look.get("bh", 10) - 10) * .5
    nh = (look.get("nh", 10) - 10) * .4
    mh = (look.get("mh", 10) - 10) * .4
    ex, ey = F["eye"]
    bx, by = F["brow"]
    return {
        "eyeL": (-ex - es, ey + eh), "eyeR": (ex + es, ey + eh),
        "browL": (-bx - es * .8, by + eh + bh), "browR": (bx + es * .8, by + eh + bh),
        "nose": (F["nose"][0], F["nose"][1] + nh), "mouth": (F["mouth"][0], F["mouth"][1] + mh), "mark": F["mark"],
    }


def world_bones(g, pose=None):
    """pose: bone -> (angle degrees, (dx, dy)).  Returns bone -> (x, y, angle)."""
    loc = rig.local(g)
    out = {}
    for name, parent in rig.BONES:
        lx, ly = loc[name]
        a, off = (pose or {}).get(name, (0.0, (0.0, 0.0)))
        a += rig.local_angle(name)
        lx += off[0]; ly += off[1]
        if parent is None:
            out[name] = (lx, ly, a)
        else:
            px, py, pa = out[parent]
            r = math.radians(pa)
            out[name] = (px + lx * math.cos(r) - ly * math.sin(r), py + lx * math.sin(r) + ly * math.cos(r), pa + a)
    return out


class Library:
    def __init__(self, g, painted=True):
        import face, hair, body
        self.g = g
        self.parts = {}
        for p in face.all_parts(g) + hair.all_parts(g) + body.all_parts(g):
            self.parts[p.name] = p
        self.cache = {}
        if painted:
            try:
                import import_painted
                over, drop = import_painted.load(g)
                for name in drop:
                    self.parts.pop(name, None)
                for name, spr in over.items():
                    self.cache[name] = (spr[0], spr[1], spr[2])
            except Exception as ex:
                print("painted parts skipped:", ex)

    def get(self, name):
        if name not in self.cache:
            p = self.parts.get(name)
            if p is None:
                return None
            img, origin = p.render()
            self.cache[name] = (img, origin, p.scale)
        return self.cache[name]


def compose(lib, look, size=(700, 1150), ppu=1.0, origin=(350, 1100), pose=None, skip=(), only=None, bg=(232, 226, 210, 255), weapon=None):
    g = lib.g
    bones = world_bones(g, pose)
    hs, hd = rig.HEAD_SCALE.get(g, 1.0), rig.HEAD_DROP.get(g, 0.0)
    hx, hy, ha = bones["head"]
    bones["head"] = (hx + hd * math.sin(math.radians(ha)), hy - hd * math.cos(math.radians(ha)), ha)
    anc = {k: (v[0] * hs, v[1] * hs) for k, v in anchors(g, look).items()}
    canvas = Image.new("RGBA", size, bg)
    for layer, bone, pattern, tint, flags in LAYERS:
        if layer in skip or (only is not None and layer not in only):
            continue
        got = None
        options = pattern.split("|")
        last = False
        for oi, option in enumerate(options):
            last = oi == len(options) - 1
            name = option.format(**{k: look.get(k, 0) for k in ("to", "ha", "fa", "ey", "br", "no", "mo", "bd", "ma", "hat")})
            got = lib.get(name)
            if got is not None:
                break
        if got is None:
            continue
        img, (ox, oy), scale = got
        if img.size[0] <= 2:
            continue
        if tint:
            arr = np.asarray(img, np.float32) / 255.0
            c = hex_rgb(look[tint])
            if tint == "hc":
                c = c + (1 - c) * 0.16     # same lift as the Unity figure: keeps painted highlights on dark hair
            if "far" in flags:
                c = c * .82
            arr[..., :3] *= c
            img = Image.fromarray((arr * 255 + .5).astype(np.uint8), "RGBA")
        bx, by, ba = bones[bone]
        ax, ay = 0.0, 0.0
        mirror = "mirror" in flags and last
        for f in flags.split():
            if f.startswith("anchor:"):
                ax, ay = anc[f[7:]]
        k = ppu / scale * (hs if bone == "head" else 1.0)
        w, h = img.size
        img = img.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS)
        ox, oy = ox * k, oy * k
        if mirror:
            img = img.transpose(Image.FLIP_LEFT_RIGHT)
            ox = img.size[0] - ox
        # rotate around the pivot: place pivot at the centre of a padded image
        W, H = img.size
        pad = int(math.hypot(W, H) + abs(ox) + abs(oy)) + 4
        big = Image.new("RGBA", (pad * 2, pad * 2), (0, 0, 0, 0))
        big.paste(img, (int(pad - ox), int(pad - (H - oy))))
        r = math.radians(ba)
        # anchor offset is in bone space
        wx = bx + ax * math.cos(r) - ay * math.sin(r)
        wy = by + ax * math.sin(r) + ay * math.cos(r)
        if abs(ba) > 1e-3:
            big = big.rotate(ba, resample=Image.BICUBIC, center=(pad, pad))
        px = int(origin[0] + wx * ppu - pad)
        py = int(origin[1] - wy * ppu - pad)
        tmp = Image.new("RGBA", size, (0, 0, 0, 0))
        tmp.paste(big, (px, py))
        canvas.alpha_composite(tmp)
    return canvas
