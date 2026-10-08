import numpy as np
from sdf import Prim, Material, norm, rot_axis


def hash2(a, b, seed=0.0):
    return np.modf(np.abs(np.sin(a * 12.9898 + b * 78.233 + seed * 37.719) * 43758.5453))[0]


def uv(p, n):
    ax = np.abs(n)
    top = ax[:, 1] > 0.75
    side = (~top) & (ax[:, 0] > ax[:, 2])
    u = np.where(top, p[:, 0], np.where(side, p[:, 2], p[:, 0]))
    v = np.where(top, p[:, 2], p[:, 1])
    return u, v, top


def t_planks(w=4.0, vertical=True, gap=0.32, var=0.12, seed=0.0):
    def f(p, n, val):
        u, v, top = uv(p, n)
        a, b = (u, v) if vertical else (v, u)
        col = np.floor(a / w)
        line = (a / w - col) * w < 0.9
        val = val + (hash2(col, 3.1, seed) - 0.5) * var
        val = np.where(line, val - gap, val)
        return val, None
    return f


def t_bricks(bw=8.0, bh=4.0, gap=0.3, var=0.14, seed=0.0, jitter=0.0):
    def f(p, n, val):
        u, v, top = uv(p, n)
        row = np.floor(v / bh)
        off = (row % 2) * bw * 0.5 + hash2(row, 1.7, seed) * jitter
        col = np.floor((u + off) / bw)
        mv = (v / bh - row) * bh < 0.9
        mu = ((u + off) / bw - col) * bw < 0.9
        val = val + (hash2(row, col, seed) - 0.5) * var
        val = np.where(mv | mu, val - gap, val)
        return val, None
    return f


def t_shingles(w=6.0, h=3.0, gap=0.28, var=0.12, seed=0.0):
    def f(p, n, val):
        ax = np.abs(n)
        side = ax[:, 0] > ax[:, 2] + 0.2
        u = np.where(side, p[:, 2], p[:, 0])
        v = p[:, 1]
        row = np.floor(v / h)
        off = (row % 2) * w * 0.5
        col = np.floor((u + off) / w)
        mv = (v / h - row) * h < 0.85
        mu = ((u + off) / w - col) * w < 0.7
        val = val + (hash2(row, col, seed) - 0.5) * var
        val = np.where(mv, val - gap, np.where(mu, val - gap * 0.6, val))
        return val, None
    return f


def t_thatch(h=3.2, seed=0.0):
    def f(p, n, val):
        ax = np.abs(n)
        side = ax[:, 0] > ax[:, 2] + 0.2
        u = np.where(side, p[:, 2], p[:, 0])
        v = p[:, 1]
        cu = np.floor(u / 1.5)
        jag = hash2(cu, 5.3, seed) * 1.2
        row = np.floor((v + jag) / h)
        edge = ((v + jag) / h - row) * h < 0.9
        val = val + (hash2(np.floor(u), row, seed) - 0.5) * 0.2
        val = np.where(edge, val - 0.3, val)
        return val, None
    return f


def t_noise(var=0.15, cell=1.0, seed=0.0):
    def f(p, n, val):
        u, v, top = uv(p, n)
        return val + (hash2(np.floor(u / cell), np.floor(v / cell), seed) - 0.5) * var, None
    return f


def t_rows(period=6.0, axis=0, gap=0.35, seed=0.0, var=0.1):
    def f(p, n, val):
        a = p[:, axis]
        r = np.floor(a / period)
        fr = (a / period - r)
        val = val + (hash2(np.floor(p[:, 2 - axis] / 2), r, seed) - 0.5) * var
        val = np.where((fr < 0.22) | (fr > 0.82), val - gap, val)
        return val, None
    return f


def t_flame(y0, y1):
    cols = np.array([[255, 246, 196], [255, 205, 72], [246, 132, 38], [196, 62, 30]], float)

    def f(p, n, val):
        t = np.clip((p[:, 1] - y0) / max(y1 - y0, 1e-3), 0, 0.999)
        idx = np.clip((t * 4 + (1 - val) * 0.8).astype(int), 0, 3)
        return val, (np.ones(len(p), bool), cols[idx])
    return f


def t_window(frame_mat_color=None):
    def f(p, n, val):
        return val, None
    return f


class Kit:
    def __init__(self):
        self.prims = []
        self.decals = []

    def add(self, kind, mat, tex=None, group=None, **kw):
        pr = Prim(kind, mat, tex=tex, group=group if group is not None else "g%d" % len(self.prims), **kw)
        self.prims.append(pr)
        return pr

    def box(self, c, h, mat, R=None, rnd=0.0, tex=None, group=None, clips=()):
        return self.add("box", mat, tex, group, c=np.asarray(c, float), h=np.asarray(h, float), R=R, round=rnd, clips=clips)

    def box_mm(self, lo, hi, mat, rnd=0.0, tex=None, group=None, R=None):
        lo, hi = np.asarray(lo, float), np.asarray(hi, float)
        return self.box((lo + hi) / 2, (hi - lo) / 2, mat, R=R, rnd=rnd, tex=tex, group=group)

    def sphere(self, c, r, mat, tex=None, group=None):
        return self.add("sphere", mat, tex, group, c=np.asarray(c, float), r=r)

    def ell(self, c, r, mat, R=None, tex=None, group=None, clips=()):
        return self.add("ellipsoid", mat, tex, group, c=np.asarray(c, float), r=np.asarray(r, float), R=R, clips=clips)

    def cap(self, a, b, r, mat, r2=None, tex=None, group=None):
        return self.add("capsule", mat, tex, group, a=np.asarray(a, float), b=np.asarray(b, float), r=r, r2=r if r2 is None else r2)

    def cyl(self, c, r, h, mat, R=None, rnd=0.0, tex=None, group=None, clips=()):
        return self.add("cyl", mat, tex, group, c=np.asarray(c, float), r=r, h=h, R=R, round=rnd, clips=clips)

    def cone(self, c, h, r1, r2, mat, R=None, tex=None, group=None, clips=()):
        return self.add("cone", mat, tex, group, c=np.asarray(c, float), h=h, r1=r1, r2=r2, R=R, clips=clips)

    def prism(self, base_c, w, h, l, mat, along_x=True, tex=None, group=None):
        R = rot_axis([0, 1, 0], np.pi / 2) if along_x else None
        return self.add("prism", mat, tex, group, c=np.asarray(base_c, float), w=w, h=h, l=l, R=R)

    def torus(self, c, major, r, mat, R=None, tex=None, group=None):
        return self.add("torus", mat, tex, group, c=np.asarray(c, float), RR=major, r=r, R=R)

    def decal(self, p, color, tol=2.0):
        self.decals.append((np.asarray(p, float), color, tol))


def lean_frame(axis_dir):
    y = norm(axis_dir)
    x = np.cross(y, [0, 0, 1.0])
    if np.linalg.norm(x) < 1e-6:
        x = np.array([1.0, 0, 0])
    x = norm(x)
    z = np.cross(x, y)
    return np.stack([x, y, z], 1)
