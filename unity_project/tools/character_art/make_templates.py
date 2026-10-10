"""Writes the fixed canvases that painted parts must line up with.

head canvas 1024x1024: 3.6 px per rig unit, head-bone origin at pixel (512, 780)
body canvas 1024x2048: 1.5 px per rig unit, feet (rig origin) at pixel (512, 1990)
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image, ImageDraw
import compose, rig

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "ArtSource", "ModularTemplates")
HEAD = dict(size=(1024, 1024), ppu=3.6, origin=(512, 780))
BODY = dict(size=(1024, 2048), ppu=1.5, origin=(512, 1990))
BG = (0, 255, 0, 255)
MANNEQUIN = ["armF_up", "armF_lo", "handF", "thighF", "shinF", "bootF", "thighN", "shinN", "bootN", "neck", "torso",
             "ears", "face", "eyeW_L", "eyeI_L", "eyeL_L", "eyeW_R", "eyeI_R", "eyeL_R", "brow_L", "brow_R", "nose", "mouth",
             "armN_up", "armN_lo", "handN"]


def look(g):
    l = dict(compose.DEFAULT_LOOK[g])
    l.update(to=0, sk="#e8d8c8", oc="#c4c4c4", pc="#b0b0b0", sc="#909090", hc="#303030", ec="#3a2a24", ey=3, br=1, no=1, mo=0)
    return l


def head_offset(g):
    """Pixel shift that puts the head bone at the head canvas origin."""
    b = compose.world_bones(g)["head"]
    return b[0], b[1]


def main():
    os.makedirs(OUT, exist_ok=True)
    for g, label in (("m", "male"), ("f", "female")):
        lib = compose.Library(g, painted=False)
        lk = look(g)
        body = compose.compose(lib, lk, size=BODY["size"], ppu=BODY["ppu"], origin=BODY["origin"], only=MANNEQUIN, bg=BG)
        body.save(os.path.join(OUT, f"body_{label}.png"))
        hx, hy = head_offset(g)
        origin = (HEAD["origin"][0] - hx * HEAD["ppu"], HEAD["origin"][1] + hy * HEAD["ppu"])
        head = compose.compose(lib, lk, size=HEAD["size"], ppu=HEAD["ppu"], origin=origin,
                               only=["neck", "torso", "ears", "face", "eyeW_L", "eyeI_L", "eyeL_L", "eyeW_R", "eyeI_R", "eyeL_R",
                                     "brow_L", "brow_R", "nose", "mouth", "armN_up", "armF_up"], bg=BG)
        head.save(os.path.join(OUT, f"head_{label}.png"))
        # guide copies for people (not for the image model)
        for name, img, spec in (("body", body, BODY), ("head", head, HEAD)):
            gimg = img.convert("RGB").copy()
            d = ImageDraw.Draw(gimg)
            W, H = gimg.size
            d.line([(W // 2, 0), (W // 2, H)], fill=(255, 0, 0), width=1)
            if name == "body":
                for y_units, txt in ((0, "feet"), (rig.WORLD[g]["legN_lo"][1], "knee"), (rig.WORLD[g]["hips"][1], "hip"),
                                     (rig.WORLD[g]["torso"][1], "waist"), (rig.WORLD[g]["armN_up"][1], "shoulder"), (rig.WORLD[g]["head"][1], "chin")):
                    y = spec["origin"][1] - y_units * spec["ppu"]
                    d.line([(0, y), (W, y)], fill=(255, 0, 0), width=1)
                    d.text((6, y - 12), txt, fill=(255, 0, 0))
            else:
                F = rig.FACE[g]
                for y_units, txt in ((0, "chin"), (F["eye"][1], "eyes"), (F["brow"][1], "brows"), (F["nose"][1], "nose"), (F["mouth"][1], "mouth"), (180 if g == "m" else 170, "crown")):
                    y = spec["origin"][1] - y_units * spec["ppu"]
                    d.line([(0, y), (W, y)], fill=(255, 0, 0), width=1)
                    d.text((6, y - 12), txt, fill=(255, 0, 0))
            gimg.save(os.path.join(OUT, f"{name}_{label}_guides.png"))


if __name__ == "__main__":
    main()
