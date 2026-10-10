"""Packs every character part into Unity atlases.

Output (per gender g = m / f):
  Assets/Resources/Characters/Modular/<g>_<page>.png   2048x2048 pages
  Assets/Resources/Characters/Modular/<g>.json          bones, face anchors, layer table, sprite rects

Painted parts in ArtSource/ModularParts (see import_painted.py) replace the code-drawn
ones with the same name.  Run:  python build_atlas.py
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image
import rig
import compose

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Characters", "Modular")
PAGE = 2048
PAD = 3


def procedural(g):
    import face, hair, body
    out = {}
    for p in face.all_parts(g) + hair.all_parts(g) + body.all_parts(g):
        img, origin = p.render()
        if img.size[0] <= 2 and img.size[1] <= 2:
            continue
        out[p.name] = (img, origin, p.scale)
    return out


def pack(sprites):
    """Shelf packer over several pages. Returns name -> (page, x, y_top) and the page images."""
    items = sorted(sprites.items(), key=lambda kv: (-kv[1][0].size[1], -kv[1][0].size[0]))
    pages = []
    placed = {}
    shelves = []  # per page: list of [y, height, x_cursor]
    for name, (img, origin, scale) in items:
        w, h = img.size
        if w + 2 * PAD > PAGE or h + 2 * PAD > PAGE:
            raise ValueError(f"{name} too large: {img.size}")
        done = False
        for pi, page_shelves in enumerate(shelves):
            for shelf in page_shelves:
                if h + PAD <= shelf[1] and shelf[2] + w + PAD <= PAGE:
                    placed[name] = (pi, shelf[2], shelf[0])
                    shelf[2] += w + PAD
                    done = True
                    break
            if done:
                break
            used = sum(s[1] for s in page_shelves)
            if used + h + PAD <= PAGE:
                page_shelves.append([used, h + PAD, PAD + w + PAD])
                placed[name] = (pi, PAD, used)
                done = True
                break
        if not done:
            shelves.append([[0, h + PAD, PAD + w + PAD]])
            placed[name] = (len(shelves) - 1, PAD, 0)
    for _ in shelves:
        pages.append(Image.new("RGBA", (PAGE, PAGE), (0, 0, 0, 0)))
    for name, (pi, x, y) in placed.items():
        pages[pi].paste(sprites[name][0], (x, y + PAD))
    return placed, pages


def build(g, sprites):
    placed, pages = pack(sprites)
    os.makedirs(OUT, exist_ok=True)
    for i, page in enumerate(pages):
        page.save(os.path.join(OUT, f"{g}_{i}.png"), optimize=True)
    import re
    head_pat = re.compile("^(" + "|".join(
        re.sub(r"\\\{\w+\\\}", r"\\d+", re.escape(opt))
        for l in compose.LAYERS if l[1] == "head" for opt in l[2].split("|")) + ")$")
    hs = rig.HEAD_SCALE.get(g, 1.0)
    rects = {}
    for name, (pi, x, y) in placed.items():
        img, (ox, oy), scale = sprites[name]
        if head_pat.match(name):
            scale = scale / hs          # drawn larger: fewer pixels per unit

        w, h = img.size
        # Unity texture coordinates start bottom-left
        uy = PAGE - (y + PAD) - h
        rects[name] = [pi, x, uy, w, h, round(float(ox), 2), round(float(oy), 2), round(float(scale), 4)]
    loc = dict(rig.local(g))
    hx, hy = loc["head"]
    loc["head"] = (hx, hy - rig.HEAD_DROP.get(g, 0.0))
    data = {
        "gender": g,
        "pages": len(pages),
        "bones": [{"name": n, "parent": p or "", "x": round(loc[n][0], 3), "y": round(loc[n][1], 3), "a": rig.local_angle(n)} for n, p in rig.BONES],
        "face": {k: [round(c * hs, 3) for c in v] if isinstance(v, tuple) else v for k, v in rig.FACE[g].items()},
        "layers": [{"id": l[0], "bone": l[1], "sprite": l[2], "tint": l[3], "flags": l[4]} for l in compose.LAYERS],
        "sprites": rects,
    }
    with open(os.path.join(OUT, f"{g}.json"), "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, separators=(",", ":"))
    return len(pages), len(rects)


def main(genders=("m", "f")):
    painted = None
    try:
        import import_painted
        painted = import_painted
    except Exception as ex:  # pragma: no cover
        print("painted import unavailable:", ex)
    for g in genders:
        sprites = procedural(g)
        if painted is not None:
            over, drop = painted.load(g)
            for name in drop:
                sprites.pop(name, None)
            sprites.update(over)
            if over:
                print(g, "painted parts:", len(over))
        pages, count = build(g, sprites)
        print(g, "pages", pages, "sprites", count)


if __name__ == "__main__":
    main(tuple(sys.argv[1:]) or ("m", "f"))
