# Splits ChatGPT-painted sheets (flat green background) into game assets.
#   skill  -> Art/Skills/<id>.bytes          256x256 icon
#   weapon -> Art/Items/<id>.bytes           256x256 icon (replaces the pixel icon)
#   monster-> Art/MonsterPoses/<id>.bytes    1024x256: idle, run, attack, pounce (facing left)
#             Art/Monsters/<id>.bytes        1024x256: 4 breathing frames of the idle pose
import io, json, math, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

def key(im):
    a = np.asarray(im.convert('RGB')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    gr = g - np.maximum(r, b)
    alpha = 1 - np.clip((gr - 35) / 70, 0, 1)
    m = np.maximum(r, b)
    g2 = np.where(gr > 0, np.minimum(g, m + (g - m) * .25), g)
    return np.dstack([r, g2, b, alpha * 255]).clip(0, 255).astype(np.uint8)

def cells(arr, cols, rows):
    H, W = arr.shape[:2]
    A = arr[..., 3] > 110
    lab, n = ndimage.label(ndimage.binary_closing(A, iterations=2))
    objs = ndimage.find_objects(lab)
    out = {}
    for i, sl in enumerate(objs):
        area = int((lab[sl] == i + 1).sum())
        if area < 120: continue
        cy = (sl[0].start + sl[0].stop) / 2; cx = (sl[1].start + sl[1].stop) / 2
        c = min(cols - 1, int(cx / (W / cols))); r = min(rows - 1, int(cy / (H / rows)))
        out.setdefault(r * cols + c, []).append((sl, i + 1, area))
    res = {}
    for idx, parts in out.items():
        big = max(p[2] for p in parts)
        parts = [p for p in parts if p[2] >= big * .02]
        y0 = min(p[0][0].start for p in parts); y1 = max(p[0][0].stop for p in parts)
        x0 = min(p[0][1].start for p in parts); x1 = max(p[0][1].stop for p in parts)
        mask = np.isin(lab[y0:y1, x0:x1], [p[1] for p in parts])
        crop = arr[y0:y1, x0:x1].copy()
        crop[..., 3] = (crop[..., 3] * ndimage.binary_dilation(mask, iterations=2)).astype(np.uint8)
        res[idx] = Image.fromarray(crop, 'RGBA')
    return res

def icon(img, size=256, fill=.9):
    s = (size * fill) / max(img.size)
    img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    c = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    c.paste(img, ((size - img.width) // 2, (size - img.height) // 2), img)
    return c

def png_bytes(img):
    b = io.BytesIO(); img.save(b, 'PNG', optimize=True); return b.getvalue()

def bbox_h(img):
    a = np.asarray(img)[..., 3] > 128
    ys = np.where(a.any(1))[0]; xs = np.where(a.any(0))[0]
    if len(ys) == 0: return None
    return ys[0], ys[-1], xs[0], xs[-1]

def main(sheet_png, manifest_path, res_root, backup_root):
    man = {m['sheet']: m for m in json.load(open(manifest_path))}
    name = os.path.basename(sheet_png)[:-4]
    sh = man[name]
    arr = key(Image.open(sheet_png))
    kind = sh['kind']
    if kind in ('skill', 'weapon'):
        got = cells(arr, sh['cols'], sh['rows'])
        sub = 'Skills' if kind == 'skill' else 'Items'
        os.makedirs(f'{res_root}/Art/{sub}', exist_ok=True)
        done = []
        for i, it in enumerate(sh['items']):
            if i not in got: print('MISSING', it['id']); continue
            dst = f"{res_root}/Art/{sub}/{it['id']}.bytes"
            if kind == 'weapon' and os.path.exists(dst):
                os.makedirs(f'{backup_root}/Items', exist_ok=True)
                bk = f"{backup_root}/Items/{it['id']}.bytes"
                if not os.path.exists(bk): open(bk, 'wb').write(open(dst, 'rb').read())
            open(dst, 'wb').write(png_bytes(icon(got[i])))
            done.append(it['id'])
        print(name, len(done), '/', len(sh['items']))
        return
    # monsters: rows = creatures, 4 pose columns
    rows = len(sh['items'])
    got = cells(arr, 4, rows)
    os.makedirs(f'{res_root}/Art/MonsterPoses', exist_ok=True)
    os.makedirs(f'{backup_root}/Monsters', exist_ok=True)
    for r, it in enumerate(sh['items']):
        poses = [got.get(r * 4 + c) for c in range(4)]
        if poses[0] is None or sum(p is not None for p in poses) < 3:
            print('MISSING', it['id'], [p is not None for p in poses]); continue
        poses = [p if p is not None else poses[0] for p in poses]
        src = f"{res_root}/Art/Monsters/{it['id']}.bytes"
        target_h = 200
        if os.path.exists(src):
            bk = f"{backup_root}/Monsters/{it['id']}.bytes"
            if not os.path.exists(bk): open(bk, 'wb').write(open(src, 'rb').read())
            old = Image.open(io.BytesIO(open(bk, 'rb').read())).convert('RGBA').crop((0, 0, 256, 256))
            bb = bbox_h(old)
            if bb: target_h = max(110, min(236, bb[1] - bb[0] + 1))
        idle = poses[0]
        s = target_h / idle.height
        s = min(s, 246 / idle.width)
        frames = []
        for p in poses:
            k = min(s, 250 / p.width, 250 / p.height)
            q = p.resize((max(1, round(p.width * k)), max(1, round(p.height * k))), Image.LANCZOS)
            frames.append(q)
        if it['id'] == 'da_lang':     # this one is drawn facing right in the game code
            frames = [f.transpose(Image.FLIP_LEFT_RIGHT) for f in frames]
        strip = Image.new('RGBA', (1024, 256), (0, 0, 0, 0))
        for c, f in enumerate(frames):
            strip.paste(f, (c * 256 + (256 - f.width) // 2, 248 - f.height), f)
        open(f"{res_root}/Art/MonsterPoses/{it['id']}.bytes", 'wb').write(png_bytes(strip))
        # breathing loop from the idle pose
        base = frames[0]
        body = Image.new('RGBA', (1024, 256), (0, 0, 0, 0))
        for c in range(4):
            t = math.sin(c / 4 * 2 * math.pi)
            sy = 1 + .025 * t; sx = 1 - .012 * t
            f = base.resize((max(1, round(base.width * sx)), max(1, round(base.height * sy))), Image.LANCZOS)
            body.paste(f, (c * 256 + (256 - f.width) // 2, 248 - f.height), f)
        open(src, 'wb').write(png_bytes(body))
        print('ok', it['id'])

if __name__ == '__main__':
    main(*sys.argv[1:5])
