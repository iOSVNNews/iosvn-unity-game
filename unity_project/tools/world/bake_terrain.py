"""Bakes the realm world map painting (Resources/World/<world>_terrain.bytes) from the same
collision masks the game walks on, with relief shading, deep water, grounded mountains and towns.

Port of Assets/Editor/WorldTerrainBake.cs with these differences:
  * a height field from the blocked (mountain / forest) mask lights every range from the upper left,
    so ranges read as raised land instead of stickers on a flat sheet
  * mountains are packed into ranges, colour matched to the ground they stand on, their pale fog
    removed, and each one gets a soft contact shadow and a foot that fades into the grass
  * water gets depth (darker toward the middle), a wet-sand shore and a lighter shallow rim,
    with a noise-warped edge so lakes are not smooth blobs

usage: python bake_terrain.py <world_pham|world_tien> <src dir with map_N.json and Art/> <out.png> [mountain sheet png]
"""
import base64, json, math, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

PPT = 6           # pixels per tile, as the Unity bake
PW, PH = 256, 160


def bits(b64, n):
    if not b64: return np.zeros(n, bool)
    raw = np.frombuffer(base64.b64decode(b64), np.uint8)
    out = np.unpackbits(raw, bitorder='little')[:n].astype(bool)
    if out.size < n: out = np.concatenate([out, np.zeros(n - out.size, bool)])
    return out


def load_world(world, src):
    immortal = world == 'world_tien'
    cols, rows = (4, 3) if immortal else (3, 3)
    first, count = (9, 11) if immortal else (1, 8)
    W, H = cols * PW, rows * PH
    blocked = np.zeros((H, W), bool); water = np.zeros((H, W), bool); road = np.zeros((H, W), bool); border = np.zeros((H, W), bool)
    regions, towns, pois, zones = [], [], [], []
    for idx in range(count):
        d = json.load(open(f"{src}/map_{first + idx}.json", encoding='utf-8'))
        ox, oy = (idx % cols) * PW, (idx // cols) * PH
        n = d['w'] * d['h']
        sl = (slice(oy, oy + PH), slice(ox, ox + PW))
        blocked[sl] = bits(d.get('block'), n).reshape(PH, PW)
        water[sl] = bits(d.get('water'), n).reshape(PH, PW)
        road[sl] = bits(d.get('road'), n).reshape(PH, PW)
        if d.get('border'): border[sl] = bits(d['border'], n).reshape(PH, PW)
        else:
            e = np.zeros((PH, PW), bool); e[:3] = e[-3:] = True; e[:, :3] = e[:, -3:] = True; border[sl] = e
        regions.append(dict(id=d['id'], biome=d.get('biome', ''), x=ox, y=oy, w=PW, h=PH))
        for t in d.get('towns') or []:
            t = dict(t); t['x'] += ox; t['y'] += oy
            t['gate'] = [t['gate'][0] + ox, t['gate'][1] + oy] if t.get('gate') else None
            t['spawn'] = [t['spawn'][0] + ox, t['spawn'][1] + oy] if t.get('spawn') else None
            t['regionId'] = d['id']; towns.append(t)
        for p in d.get('pois') or []:
            if p.get('kind') == 'portal': continue
            p = dict(p); p['x'] += ox; p['y'] += oy; p['regionId'] = d['id']; pois.append(p)
        for z in d.get('zones') or []:
            z = dict(z); z['x'] += ox; z['y'] += oy; z['regionId'] = d['id']; zones.append(z)
    # paired province gates (same as WorldMapData.AddProvinceGate)
    for i in range(count):
        c = i % cols
        def gate(fr, to, horiz):
            x = fr['x'] + fr['w'] - 5 if horiz else fr['x'] + fr['w'] // 2
            y = fr['y'] + fr['h'] // 2 if horiz else fr['y'] + fr['h'] - 5
            pois.append(dict(kind='province_gate', x=x, y=y, regionId=fr['id']))
            x = to['x'] + 4 if horiz else to['x'] + to['w'] // 2
            y = to['y'] + to['h'] // 2 if horiz else to['y'] + 4
            pois.append(dict(kind='province_gate', x=x, y=y, regionId=to['id']))
        if c + 1 < cols and i + 1 < count: gate(regions[i], regions[i + 1], True)
        if i + cols < count: gate(regions[i], regions[i + cols], False)
    asc = (14, 80) if immortal else (502, 400)
    pois.append(dict(kind='ascension_gate', x=asc[0], y=asc[1], regionId='map_9' if immortal else 'map_8'))

    def open_(x, y):
        if 0 <= x < W and 0 <= y < H:
            blocked[y, x] = False; border[y, x] = False; water[y, x] = False; road[y, x] = True

    def carve(a, b):
        x, y = a; sx = 1 if b[0] >= x else -1
        while x != b[0]:
            for oy in range(-2, 3):
                for ox in range(-2, 3): open_(x + ox, y + oy)
            x += sx
        sy = 1 if b[1] >= y else -1
        while y != b[1]:
            for oy in range(-2, 3):
                for ox in range(-2, 3): open_(x + ox, y + oy)
            y += sy
        for oy in range(-2, 3):
            for ox in range(-2, 3): open_(x + ox, y + oy)
    for r in regions:
        c = (r['x'] + r['w'] // 2, r['y'] + r['h'] // 2)
        for t in towns:
            if t['regionId'] == r['id']:
                if t.get('gate'): carve(c, t['gate'])
                if t.get('spawn'): carve(c, t['spawn'])
        for p in pois:
            if p['regionId'] == r['id']: carve(c, (p['x'], p['y']))
        for z in zones:
            if z['regionId'] == r['id']: carve(c, (z['x'] + z['w'] // 2, z['y'] + z['h'] // 2))
    return dict(W=W, H=H, blocked=blocked, water=water, road=road, border=border, regions=regions, towns=towns, pois=pois, zones=zones)


TINT = {'snow': (.84, .89, .86), 'frost': (.84, .89, .86), 'desert': (.80, .73, .58), 'wasteland': (.80, .73, .58),
        'wetland': (.65, .77, .67), 'misty': (.75, .82, .75), 'highland': (.79, .79, .67), 'coast': (.78, .83, .70),
        'storm': (.68, .75, .75), 'volcanic': (.81, .70, .57), 'canyon': (.83, .76, .62), 'jungle': (.64, .77, .61),
        'celestial': (.77, .84, .78)}


def hash2(x, y):
    h = (x * 374761393 + y * 668265263) & 0xffffffff
    h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff
    return h ^ (h >> 16)


def fbm(h, w, scale, seed, octaves=3):
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32); amp = 1; tot = 0
    for o in range(octaves):
        s = max(2, int(scale / (2 ** o)))
        g = rng.random((h // s + 3, w // s + 3)).astype(np.float32)
        up = np.array(Image.fromarray(g).resize(((w // s + 3) * s, (h // s + 3) * s), Image.BICUBIC))[:h, :w]
        out += up * amp; tot += amp; amp *= .5
    return out / tot


def key_green(arr):
    a = arr.astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    gr = g - np.maximum(r, b)
    alpha = 1 - np.clip((gr - 35) / 70, 0, 1)
    m = np.maximum(r, b)
    g2 = np.where(gr > 0, np.minimum(g, m + (g - m) * .25), g)
    return np.dstack([r, g2, b, alpha * 255]).clip(0, 255).astype(np.uint8)


def sheet_sprites(path, cols, rows, keyed):
    im = Image.open(path).convert('RGBA')
    arr = np.asarray(im)
    if keyed: arr = key_green(arr[..., :3])
    H, W = arr.shape[:2]
    out = []
    A = arr[..., 3] > 100
    lab, n = ndimage.label(ndimage.binary_closing(A, iterations=2))
    objs = ndimage.find_objects(lab)
    cells = {}
    for i, sl in enumerate(objs):
        area = int((lab[sl] == i + 1).sum())
        if area < 400: continue
        cy = (sl[0].start + sl[0].stop) / 2; cx = (sl[1].start + sl[1].stop) / 2
        k = min(rows - 1, int(cy / (H / rows))) * cols + min(cols - 1, int(cx / (W / cols)))
        cells.setdefault(k, []).append((sl, i + 1, area))
    for k in sorted(cells):
        parts = cells[k]; big = max(p[2] for p in parts)
        parts = [p for p in parts if p[2] > big * .03]
        y0 = min(p[0][0].start for p in parts); y1 = max(p[0][0].stop for p in parts)
        x0 = min(p[0][1].start for p in parts); x1 = max(p[0][1].stop for p in parts)
        crop = arr[y0:y1, x0:x1].astype(np.float32)
        mask = np.isin(lab[y0:y1, x0:x1], [p[1] for p in parts])
        crop[..., 3] *= ndimage.binary_dilation(mask, iterations=2)
        if not keyed:
            # old ink atlases: remove the pale fog at the feet entirely
            pale = crop[..., :3].min(-1) / 255
            crop[..., 3] *= 1 - np.clip((pale - .66) / .26, 0, 1)
        out.append(crop)
    return out


def bake(world, src, out_png, mountain_sheets):
    D = load_world(world, src)
    W, H = D['W'], D['H']
    w, h = W * PPT, H * PPT
    print('world', W, H, '->', w, h)
    # ---------------- ground
    ground = np.asarray(Image.open(f'{src}/Art/ink_ground_v1.png').convert('RGB'), np.float32) / 255
    gy = np.arange(h) % ground.shape[0]; gx = np.arange(w) % ground.shape[1]
    img = ground[gy[:, None], gx[None, :]]
    gray = img.mean(-1, keepdims=True)
    img = img * .88 + gray * .12
    # biome tint per province, smoothly blended (in tile space then upsampled)
    tint_t = np.zeros((H, W, 3), np.float32); tint_t[:] = TINT['misty']
    for r in D['regions']:
        tint_t[r['y']:r['y'] + r['h'], r['x']:r['x'] + r['w']] = TINT.get(r['biome'], (.73, .80, .63))
    tint_t = ndimage.gaussian_filter(tint_t, (40, 60, 0))
    tint = np.array(Image.fromarray((tint_t * 255).astype(np.uint8)).resize((w, h), Image.BILINEAR), np.float32) / 255
    img = img * tint / np.array([.73, .80, .63], np.float32)
    wash = fbm(h, w, 690, 7) * .12 + fbm(h, w, 100, 8) * .025
    img *= (1 - .06 + wash[..., None])
    # ---------------- relief: blocked land rises, water sinks
    blocked = D['blocked'] & ~D['water']
    height_t = ndimage.gaussian_filter(blocked.astype(np.float32), 2.2) * 1.0
    height_t -= ndimage.gaussian_filter(D['water'].astype(np.float32), 1.5) * .35
    height = np.array(Image.fromarray(height_t).resize((w, h), Image.BICUBIC), np.float32)
    height += (fbm(h, w, 160, 11, 4) - .5) * .25
    gyy, gxx = np.gradient(height * 26)
    # light from the upper left (image y grows downward)
    shade = (-gxx * .7 - gyy * .7)
    shade = np.clip(shade, -1, 1)
    img *= (1 + shade[..., None] * .16)
    # ---------------- water
    cover_t = ndimage.gaussian_filter(D['water'].astype(np.float32), 1.9)
    cover = np.array(Image.fromarray(cover_t).resize((w, h), Image.BICUBIC), np.float32)
    warp = (fbm(h, w, 40, 21, 3) - .5) * .22 + (fbm(h, w, 12, 22, 2) - .5) * .06
    cover = np.clip(cover + warp, 0, 1)
    depth_t = ndimage.distance_transform_edt(D['water'])
    depth = np.array(Image.fromarray(np.clip(depth_t / 7, 0, 1).astype(np.float32)).resize((w, h), Image.BICUBIC), np.float32)
    wat = np.asarray(Image.open(f'{src}/Art/ink_water_v1.png').convert('RGB'), np.float32) / 255
    ripple = wat[np.arange(h)[:, None] % wat.shape[0], np.arange(w)[None, :] % wat.shape[1]]
    shallow = np.array([.70, .78, .70], np.float32); deep = np.array([.42, .56, .56], np.float32)
    wcol = shallow[None, None] * (1 - depth[..., None]) + deep[None, None] * depth[..., None]
    rl = ripple.mean(-1, keepdims=True)
    wcol = wcol * (.80 + (rl - rl.mean()) * 1.1)
    wcol = np.clip(wcol, 0, 1)
    inside = np.clip((cover - .26) / .28, 0, 1) ** 1.2
    # wet sand band just outside the water, darker wet line at the edge
    band = np.clip(1 - np.abs(cover - .26) / .12, 0, 1)
    img = img * (1 - band[..., None] * .10) + np.array([.80, .74, .58], np.float32) * band[..., None] * .10
    edge = np.clip(1 - np.abs(cover - .40) / .07, 0, 1)
    img = img * (1 - inside[..., None]) + wcol * inside[..., None]
    img *= (1 - edge[..., None] * .07)
    img = np.clip(img, 0, 1)
    canvas = Image.fromarray((img * 255).astype(np.uint8)).convert('RGBA')
    # ---------------- decorations
    reserved = np.zeros((H, W), bool)
    for t in D['towns']:
        reserved[max(0, t['y'] - 14):t['y'] + t['h'] + 10, max(0, t['x'] - 10):t['x'] + t['w'] + 10] = True
    for p in D['pois']:
        reserved[max(0, p['y'] - 5):p['y'] + 6, max(0, p['x'] - 5):p['x'] + 6] = True
    open_t = ~(D['blocked']) | D['water'] | D['road'] | reserved
    mountains = []
    for path, keyed in mountain_sheets:
        mountains.append(sheet_sprites(path, 4, 4, keyed))
    trees = sheet_sprites(f'{src}/Art/ink_trees.png', 4, 4, False)
    biome_of = np.zeros((H, W), object)
    for r in D['regions']: biome_of[r['y']:r['y'] + r['h'], r['x']:r['x'] + r['w']] = r['biome']
    mnoise = fbm(H, W, 30, 31, 2)
    placements = []
    occupied = np.zeros((H, W), bool)
    for y in range(6, H - 6, 3):
        for x in range(6, W - 6, 3):
            hsh = hash2(x, y)
            tx = x + (hsh % 3) - 1; ty = y + ((hsh >> 3) % 3) - 1
            if open_t[ty, tx] or occupied[ty, tx]: continue
            mountain = mnoise[ty, tx] > .48
            if mountain:
                sc = .85 + ((hsh >> 9) % 100) / 100 * .7
                tw, th_ = int(20 * sc), int(17 * sc)
            else:
                sc = .8 + ((hsh >> 9) % 100) / 100 * .5
                tw, th_ = int(7 * sc), int(9 * sc)
            # the whole footprint row band must be on blocked land (keeps passes, roads and water clear)
            x0, x1 = tx - tw // 2, tx + tw // 2
            y0, y1 = ty - 2, ty + 2
            if x0 < 0 or x1 >= W or y1 >= H: continue
            if open_t[y0:y1 + 1, x0:x1 + 1].mean() > .08: continue
            # the body above the foot must not cover roads, towns or water
            top = max(0, ty - th_)
            if (D['road'][top:ty, x0:x1 + 1] | reserved[top:ty, x0:x1 + 1] | D['water'][top:ty, x0:x1 + 1]).mean() > .12: continue
            placements.append((ty, tx, mountain, hsh, tw, th_))
            occupied[max(0, ty - 2):ty + 2, x0 + tw // 4:x1 - tw // 4 + 1] = True
    placements.sort(key=lambda p: p[0])
    print('decorations', len(placements), 'mountains', sum(1 for p in placements if p[2]))
    shadow_layer = Image.new('L', (w, h), 0)
    from PIL import ImageDraw, ImageFilter
    sd = ImageDraw.Draw(shadow_layer)
    for ty, tx, mountain, hsh, tw, th_ in placements:
        cx, cy = (tx + .5) * PPT, (ty + 1.2) * PPT
        rx, ry = tw * PPT * .48, th_ * PPT * .16
        sd.ellipse([cx - rx + rx * .25, cy - ry * .6, cx + rx + rx * .25, cy + ry * 1.2], fill=110 if mountain else 70)
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(PPT * 2.2))
    base = np.asarray(canvas, np.float32)
    sh = np.asarray(shadow_layer, np.float32)[..., None] / 255
    base[..., :3] *= (1 - sh * .35)
    canvas = Image.fromarray(base.clip(0, 255).astype(np.uint8))
    groundcol = np.asarray(canvas.resize((W, H), Image.BOX), np.float32)
    for ty, tx, mountain, hsh, tw, th_ in placements:
        biome = biome_of[ty, tx]
        if mountain:
            pool = mountains[0]
            if len(mountains) > 1 and len(mountains[1]) >= 16:
                b = mountains[1]
                if biome in ('snow', 'frost'): pool = b[:8]
                elif biome in ('desert', 'wasteland', 'canyon'): pool = b[8:12]
                elif biome == 'volcanic': pool = b[12:16]
                elif biome in ('highland', 'storm') and (hsh >> 14) % 3 == 0: pool = b[4:8]
            spr = pool[(hsh >> 12) % len(pool)]
        else:
            spr = trees[(hsh >> 12) % len(trees)]
        dw = tw * PPT; dh = int(dw * spr.shape[0] / spr.shape[1])
        im = Image.fromarray(spr.clip(0, 255).astype(np.uint8)).resize((max(2, dw), max(2, dh)), Image.LANCZOS)
        a = np.asarray(im, np.float32)
        if (hsh >> 20) & 1: a = a[:, ::-1]
        # colour match: pull the sprite a little toward the ground colour under it
        g = groundcol[min(H - 1, ty), min(W - 1, tx), :3]
        lum = a[..., :3].mean(-1, keepdims=True)
        a[..., :3] = a[..., :3] * .82 + (g[None, None] * lum / max(1, g.mean())) * .18
        # the foot fades into the grass
        rows = np.arange(a.shape[0], dtype=np.float32)[:, None]
        foot = np.clip((a.shape[0] - rows) / (a.shape[0] * .16), 0, 1)
        a[..., 3] *= foot ** 1.2
        left = int((tx + .5) * PPT - a.shape[1] / 2)
        bottom = int((ty + 1.6) * PPT)
        canvas.alpha_composite(Image.fromarray(a.clip(0, 255).astype(np.uint8)), (left, bottom - a.shape[0]))
    # ---------------- towns (same placement as the Unity bake)
    towns_art = sheet_sprites(f'{src}/Art/grounded_towns_v1.png', 4, 4, False) if False else None
    tw_img = Image.open(f'{src}/Art/grounded_towns_v1.png').convert('RGBA')
    cw, ch = tw_img.width // 4, tw_img.height // 4
    land = [0, 1, 2, 4, 5, 6, 7, 9, 10, 11, 12, 14, 15]
    for i, t in enumerate(D['towns']):
        if not t.get('gate'): continue
        size = 28 if t.get('big') else 24
        cell = land[i % len(land)]
        tile = tw_img.crop(((cell % 4) * cw, (cell // 4) * ch, (cell % 4 + 1) * cw, (cell // 4 + 1) * ch))
        d = size * PPT
        tile = tile.resize((d, d), Image.LANCZOS)
        a = np.asarray(tile, np.float32)
        e = np.minimum(np.minimum(np.arange(d)[None, :], d - 1 - np.arange(d)[None, :]), np.minimum(np.arange(d)[:, None], d - 1 - np.arange(d)[:, None]))
        a[..., 3] *= np.clip(e / 3, 0, 1)
        gx_, gy_ = t['gate']
        left = int(gx_ * PPT - d / 2)
        top = int((gy_ + .5) * PPT + d * .14 - d)
        canvas.alpha_composite(Image.fromarray(a.clip(0, 255).astype(np.uint8)), (left, top))
    canvas.convert('RGB').save(out_png, optimize=False)
    print('saved', out_png)


if __name__ == '__main__':
    world, src, out = sys.argv[1:4]
    sheets = []
    for s in sys.argv[4:]:
        sheets.append((s, True))
    if not sheets:
        sheets = [(f'{src}/Art/mountain_ridges.png', False)]
    bake(world, src, out, sheets)
