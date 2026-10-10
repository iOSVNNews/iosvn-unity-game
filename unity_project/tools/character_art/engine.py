"""Tiny vector painter for the modular cultivator parts.

Every part is authored in *rig units* (the character is about 1000 units tall,
y points up) in the local space of the bone it hangs from, so the bone origin
becomes the sprite pivot.  Parts are painted mostly in grey values: the game
multiplies them by the player's chosen colour, the same way layered paper-doll
characters keep their painted light and shadow while changing hue.
"""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SS = 3  # supersampling factor


def _c(v):
    """Grey value or RGB tuple -> RGB float array."""
    if isinstance(v, (int, float)):
        return np.array([v, v, v], dtype=np.float32)
    return np.array(v[:3], dtype=np.float32)


def spline(points, closed=True, samples=10, tension=0.5):
    """Catmull-Rom through points; a point given as (x, y, 'c') is a sharp corner."""
    pts = [(p[0], p[1]) for p in points]
    corner = [len(p) > 2 and p[2] == 'c' for p in points]
    n = len(pts)
    if n < 3:
        return [tuple(p) for p in pts]
    P = np.array(pts, dtype=np.float64)
    tang = []
    for i in range(n):
        if corner[i]:
            tang.append(np.zeros(2))
        elif not closed and i == 0:
            tang.append((P[1] - P[0]) * tension * 2)
        elif not closed and i == n - 1:
            tang.append((P[n - 1] - P[n - 2]) * tension * 2)
        else:
            tang.append((P[(i + 1) % n] - P[(i - 1) % n]) * tension)
    out = []
    segs = n if closed else n - 1
    for i in range(segs):
        p0, p1 = P[i], P[(i + 1) % n]
        m0, m1 = tang[i], tang[(i + 1) % n]
        for s in range(samples):
            t = s / samples
            h00 = 2 * t ** 3 - 3 * t ** 2 + 1
            h10 = t ** 3 - 2 * t ** 2 + t
            h01 = -2 * t ** 3 + 3 * t ** 2
            h11 = t ** 3 - t ** 2
            q = h00 * p0 + h10 * m0 + h01 * p1 + h11 * m1
            out.append((q[0], q[1]))
    if not closed:
        out.append(tuple(P[-1]))
    return out


def mirror(points, cx=0.0):
    return [(2 * cx - p[0], p[1]) + tuple(p[2:]) for p in points]


def shift(points, dx=0.0, dy=0.0):
    return [(p[0] + dx, p[1] + dy) + tuple(p[2:]) for p in points]


def scale_pts(points, sx, sy=None, cx=0.0, cy=0.0):
    sy = sx if sy is None else sy
    return [((p[0] - cx) * sx + cx, (p[1] - cy) * sy + cy) + tuple(p[2:]) for p in points]


def rotate_pts(points, deg, cx=0.0, cy=0.0):
    a = math.radians(deg)
    ca, sa = math.cos(a), math.sin(a)
    return [(cx + (p[0] - cx) * ca - (p[1] - cy) * sa, cy + (p[0] - cx) * sa + (p[1] - cy) * ca) + tuple(p[2:]) for p in points]


def lerp(a, b, t):
    return a + (b - a) * t


class Part:
    """Records painting operations, then renders them to an RGBA sprite."""

    def __init__(self, name, scale=1.0):
        self.name = name
        self.scale = scale  # sprite pixels per rig unit
        self.ops = []
        self.extent = []
        self.margin = 2.0

    # -- recording ---------------------------------------------------------
    def _poly(self, pts, smooth=True, closed=True, samples=10):
        poly = spline(pts, closed=closed, samples=samples) if smooth else [(p[0], p[1]) for p in pts]
        self.extent.extend(poly)
        return poly

    def fill(self, pts, color, alpha=1.0, smooth=True, clip=None):
        cpoly = None if clip is None else spline(clip, True)
        self.ops.append(('fill', self._poly(pts, smooth), _c(color), alpha, cpoly))
        return self

    def grad(self, pts, c0, c1, p0, p1, alpha=1.0, smooth=True):
        """Linear gradient fill from point p0 (colour c0) to p1 (colour c1)."""
        self.ops.append(('grad', self._poly(pts, smooth), _c(c0), _c(c1), p0, p1, alpha))
        return self

    def shade(self, pts, color, alpha=1.0, blur=4.0, clip=None, smooth=True):
        """Paint inside existing pixels only (alpha kept), optionally clipped to a shape."""
        poly = spline(pts, True) if smooth else [(p[0], p[1]) for p in pts]
        cpoly = None if clip is None else (spline(clip, True) if smooth else [(p[0], p[1]) for p in clip])
        self.ops.append(('shade', poly, _c(color), alpha, blur, cpoly))
        return self

    def stroke(self, pts, width, color=0.12, alpha=1.0, width_end=None, closed=False, smooth=True, taper=None, shade_only=False, samples=10):
        poly = spline(pts, closed=closed, samples=samples) if smooth else [(p[0], p[1]) for p in pts]
        if closed:
            poly = poly + [poly[0]]
        self.extent.extend(poly)
        self.margin = max(self.margin, width * 0.6 + 2)
        self.ops.append(('stroke', poly, width, width if width_end is None else width_end, _c(color), alpha, taper, shade_only))
        return self

    def ellipse(self, cx, cy, rx, ry, color, alpha=1.0, rot=0.0):
        pts = [(cx + rx * math.cos(a), cy + ry * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 48, endpoint=False)]
        if rot:
            pts = rotate_pts(pts, rot, cx, cy)
        self.ops.append(('fill', pts, _c(color), alpha, None))
        self.extent.extend(pts)
        return self

    def edge(self, strength=0.25, width=3.0, color=0.0):
        """Watercolour edge darkening of everything painted so far."""
        self.ops.append(('edge', strength, width, _c(color)))
        return self

    def texture(self, strength=0.06, freq=0.06, seed=1, clip=None):
        cpoly = None if clip is None else spline(clip, True)
        self.ops.append(('texture', strength, freq, seed, cpoly))
        return self

    def soft(self, blur=0.6):
        self.ops.append(('soft', blur))
        return self

    def pad(self, points):
        self.extent.extend([(p[0], p[1]) for p in points])
        return self

    # -- rendering ---------------------------------------------------------
    def render(self):
        ext = np.array(self.extent, dtype=np.float64)
        m = self.margin + 3
        minx, miny = ext.min(0) - m
        maxx, maxy = ext.max(0) + m
        k = self.scale * SS
        W = int(math.ceil((maxx - minx) * self.scale)) + 1
        H = int(math.ceil((maxy - miny) * self.scale)) + 1
        Ws, Hs = W * SS, H * SS
        maxx = minx + W / self.scale
        maxy = miny + H / self.scale

        def tx(poly):
            return [((x - minx) * k, (maxy - y) * k) for x, y in poly]

        col = np.zeros((Hs, Ws, 3), np.float32)
        alp = np.zeros((Hs, Ws), np.float32)

        def raster(poly):
            im = Image.new('L', (Ws, Hs), 0)
            if len(poly) >= 3:
                ImageDraw.Draw(im).polygon(tx(poly), fill=255)
            return np.asarray(im, np.float32) / 255.0

        def over(mask, c):
            nonlocal col, alp
            na = mask + alp * (1 - mask)
            safe = np.maximum(na, 1e-6)[..., None]
            c = c if c.ndim == 3 else c[None, None, :]
            col = (c * mask[..., None] + col * (alp * (1 - mask))[..., None]) / safe
            alp = na

        for op in self.ops:
            kind = op[0]
            if kind == 'fill':
                _, poly, c, a, cpoly = op
                mask = raster(poly) * a
                if cpoly is not None:
                    mask *= raster(cpoly)
                over(mask, c)
            elif kind == 'grad':
                _, poly, c0, c1, p0, p1, a = op
                mask = raster(poly) * a
                ys, xs = np.mgrid[0:Hs, 0:Ws].astype(np.float32)
                ux = xs / k + minx
                uy = maxy - ys / k
                d = np.array(p1, np.float32) - np.array(p0, np.float32)
                t = ((ux - p0[0]) * d[0] + (uy - p0[1]) * d[1]) / max(1e-6, float(d @ d))
                t = np.clip(t, 0, 1)[..., None]
                over(mask, c0[None, None, :] * (1 - t) + c1[None, None, :] * t)
            elif kind == 'shade':
                _, poly, c, a, blur, cpoly = op
                im = Image.new('L', (Ws, Hs), 0)
                ImageDraw.Draw(im).polygon(tx(poly), fill=255)
                if blur > 0:
                    im = im.filter(ImageFilter.GaussianBlur(blur * k))
                mask = np.asarray(im, np.float32) / 255.0 * a
                if cpoly is not None:
                    mask *= raster(cpoly)
                col = col * (1 - mask[..., None]) + c[None, None, :] * mask[..., None]
            elif kind == 'stroke':
                _, poly, w0, w1, c, a, taper, shade_only = op
                im = Image.new('L', (Ws, Hs), 0)
                dr = ImageDraw.Draw(im)
                P = tx(poly)
                n = len(P)
                L = [0.0]
                for i in range(1, n):
                    L.append(L[-1] + math.dist(P[i - 1], P[i]))
                total = max(L[-1], 1e-6)

                def width_at(i):
                    t = L[i] / total
                    w = w0 + (w1 - w0) * t
                    if taper == 'both':
                        w *= min(1.0, 0.25 + 1.5 * min(t, 1 - t) * 2)
                    elif taper == 'end':
                        w *= max(0.15, 1 - t)
                    elif taper == 'start':
                        w *= max(0.15, t)
                    return max(0.3, w * k * 0.5)
                for i in range(n - 1):
                    (x0, y0), (x1, y1) = P[i], P[i + 1]
                    dx, dy = x1 - x0, y1 - y0
                    ln = math.hypot(dx, dy)
                    if ln < 1e-6:
                        continue
                    nx, ny = -dy / ln, dx / ln
                    r0, r1 = width_at(i), width_at(i + 1)
                    dr.polygon([(x0 + nx * r0, y0 + ny * r0), (x1 + nx * r1, y1 + ny * r1),
                                (x1 - nx * r1, y1 - ny * r1), (x0 - nx * r0, y0 - ny * r0)], fill=255)
                    dr.ellipse([x1 - r1, y1 - r1, x1 + r1, y1 + r1], fill=255)
                if n:
                    r = width_at(0)
                    dr.ellipse([P[0][0] - r, P[0][1] - r, P[0][0] + r, P[0][1] + r], fill=255)
                mask = np.asarray(im, np.float32) / 255.0 * a
                if shade_only:
                    mask = mask * (alp > 0.01)
                    col = col * (1 - mask[..., None]) + c[None, None, :] * mask[..., None]
                else:
                    over(mask, c)
            elif kind == 'edge':
                _, s, w, c = op
                im = Image.fromarray((alp * 255).astype(np.uint8))
                inner = np.asarray(im.filter(ImageFilter.GaussianBlur(w * k)), np.float32) / 255.0
                e = np.clip(alp - inner, 0, 1) * 2.0 * s
                e = np.clip(e, 0, 1)
                col = col * (1 - e[..., None]) + c[None, None, :] * e[..., None]
            elif kind == 'texture':
                _, s, f, seed, cpoly = op
                rng = np.random.default_rng(seed)
                gw = max(2, int(Ws / (k / max(f, 1e-3))) + 2)
                gh = max(2, int(Hs / (k / max(f, 1e-3))) + 2)
                nz = rng.random((gh, gw)).astype(np.float32)
                nz = np.asarray(Image.fromarray((nz * 255).astype(np.uint8)).resize((Ws, Hs), Image.BICUBIC), np.float32) / 255.0
                fine = rng.random((Hs // 2 + 1, Ws // 2 + 1)).astype(np.float32)
                fine = np.asarray(Image.fromarray((fine * 255).astype(np.uint8)).resize((Ws, Hs), Image.BILINEAR), np.float32) / 255.0
                nz = (nz - 0.5) * 2 * 0.75 + (fine - 0.5) * 2 * 0.25
                mask = np.ones((Hs, Ws), np.float32) if cpoly is None else raster(cpoly)
                col = np.clip(col * (1 + s * nz * mask)[..., None], 0, 1)
            elif kind == 'soft':
                _, b = op
                from scipy.ndimage import gaussian_filter
                pre = np.dstack([col * alp[..., None], alp])
                sig = b * k
                arr = np.dstack([gaussian_filter(pre[..., i], sig) for i in range(4)])
                alp = arr[..., 3]
                col = arr[..., :3] / np.maximum(alp, 1e-6)[..., None]

        pre = np.dstack([np.clip(col, 0, 1) * alp[..., None], alp]).astype(np.float32)
        # box-downsample in float, then dither to avoid banding in soft gradients
        pre = pre[:H * SS, :W * SS].reshape(H, SS, W, SS, 4).mean(axis=(1, 3))
        rng = np.random.default_rng(7)
        pre = pre + (rng.random(pre.shape, dtype=np.float32) - 0.5) / 255.0
        arr = np.clip(pre, 0, 1)
        a = arr[..., 3]
        rgb = np.where(a[..., None] > 1e-4, arr[..., :3] / np.maximum(a, 1e-4)[..., None], 0)
        out = np.dstack([np.clip(rgb, 0, 1), a])
        img = Image.fromarray((out * 255 + .5).astype(np.uint8), 'RGBA')
        # trim fully transparent borders but keep pivot bookkeeping
        bbox = img.getbbox()
        if bbox is None:
            bbox = (0, 0, 1, 1)
        x0, y0, x1, y1 = bbox
        x0 = max(0, x0 - 1); y0 = max(0, y0 - 1); x1 = min(W, x1 + 1); y1 = min(H, y1 + 1)
        img = img.crop((x0, y0, x1, y1))
        # origin (0,0) in sprite pixels from bottom-left
        ox = (0 - minx) * self.scale - x0
        oy = (0 - miny) * self.scale - (H - y1)
        return img, (ox, oy)
