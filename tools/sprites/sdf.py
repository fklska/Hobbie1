import numpy as np

UP = np.array([0.0, 1.0, 0.0])


def norm(v):
    v = np.asarray(v, float)
    n = np.linalg.norm(v)
    return v / n if n > 1e-9 else v


def rot_axis(axis, ang):
    a = norm(axis)
    x, y, z = a
    c, s = np.cos(ang), np.sin(ang)
    C = 1 - c
    return np.array([
        [c + x * x * C, x * y * C - z * s, x * z * C + y * s],
        [y * x * C + z * s, c + y * y * C, y * z * C - x * s],
        [z * x * C - y * s, z * y * C + x * s, c + z * z * C],
    ])


def frame_from(fwd, up=UP):
    f = norm(fwd)
    r = norm(np.cross(f, up))
    if np.linalg.norm(r) < 1e-6:
        r = norm(np.cross(f, [0, 0, 1]))
    u = np.cross(r, f)
    return np.stack([r, u, f], axis=1)


def lerp(a, b, t):
    return np.asarray(a, float) * (1 - t) + np.asarray(b, float) * t


class Prim:
    def __init__(self, kind, mat, layer="body", group=None, clips=(), tex=None, **kw):
        self.kind = kind
        self.mat = mat
        self.layer = layer
        self.group = group
        self.clips = list(clips)
        self.tex = tex
        self.kw = kw
        if "R" in kw and kw["R"] is not None:
            self.Rt = np.asarray(kw["R"]).T
        else:
            self.Rt = None

    def local(self, p, c):
        q = p - c
        return q @ self.Rt.T if self.Rt is not None else q

    def dist(self, p):
        k = self.kw
        t = self.kind
        if t == "sphere":
            d = np.linalg.norm(p - k["c"], axis=1) - k["r"]
        elif t == "ellipsoid":
            q = self.local(p, k["c"])
            r = np.asarray(k["r"], float)
            k0 = np.linalg.norm(q / r, axis=1)
            k1 = np.linalg.norm(q / (r * r), axis=1)
            d = k0 * (k0 - 1.0) / np.maximum(k1, 1e-6)
        elif t == "capsule":
            a, b = np.asarray(k["a"], float), np.asarray(k["b"], float)
            ra, rb = k["r"], k.get("r2", k["r"])
            d = _round_cone(p, a, b, ra, rb)
        elif t == "box":
            q = np.abs(self.local(p, k["c"])) - (np.asarray(k["h"], float) - k.get("round", 0.0))
            d = np.linalg.norm(np.maximum(q, 0), axis=1) + np.minimum(np.max(q, axis=1), 0) - k.get("round", 0.0)
        elif t == "cyl":
            q = self.local(p, k["c"])
            dxz = np.sqrt(q[:, 0] ** 2 + q[:, 2] ** 2) - k["r"]
            dy = np.abs(q[:, 1]) - k["h"]
            rr = k.get("round", 0.0)
            dxz = dxz + rr
            dy = dy + rr
            d = np.minimum(np.maximum(dxz, dy), 0) + np.sqrt(np.maximum(dxz, 0) ** 2 + np.maximum(dy, 0) ** 2) - rr
        elif t == "cone":
            q = self.local(p, k["c"])
            h = k["h"]
            r1, r2 = k["r1"], k["r2"]
            d = _capped_cone(q, h, r1, r2)
        elif t == "prism":
            q = self.local(p, k["c"])
            d = _roof_prism(q, k["w"], k["h"], k["l"])
        elif t == "torus":
            q = self.local(p, k["c"])
            qx = np.sqrt(q[:, 0] ** 2 + q[:, 2] ** 2) - k["R"]
            d = np.sqrt(qx ** 2 + q[:, 1] ** 2) - k["r"]
        elif t == "custom":
            d = k["fn"](p)
        else:
            raise ValueError(t)
        if "shell" in k:
            d = np.abs(d) - k["shell"]
        for n, o in self.clips:
            d = np.maximum(d, (p - o) @ n)
        return d


def _round_cone(p, a, b, r1, r2):
    ba = b - a
    l2 = ba @ ba
    if l2 < 1e-9:
        return np.linalg.norm(p - a, axis=1) - max(r1, r2)
    pa = p - a
    h = np.clip((pa @ ba) / l2, 0, 1)
    r = r1 + (r2 - r1) * h
    return np.linalg.norm(pa - h[:, None] * ba, axis=1) - r


def _capped_cone(q, h, r1, r2):
    qx = np.sqrt(q[:, 0] ** 2 + q[:, 2] ** 2)
    qy = q[:, 1]
    k1 = np.array([r2, h])
    k2 = np.array([r2 - r1, 2 * h])
    cax = qx - np.minimum(qx, np.where(qy < 0, r1, r2))
    cay = np.abs(qy) - h
    kq = np.stack([k1[0] - qx, k1[1] - qy], 1)
    t = np.clip((kq @ k2) / (k2 @ k2), 0, 1)
    cbx = qx - k1[0] + k2[0] * t
    cby = qy - k1[1] + k2[1] * t
    s = np.where((cbx < 0) & (cay < 0), -1.0, 1.0)
    return s * np.sqrt(np.minimum(cax ** 2 + cay ** 2, cbx ** 2 + cby ** 2))


def _roof_prism(q, w, h, l):
    # gable roof: ridge along local z, half width w, height h, half length l, base at y=0
    x = np.abs(q[:, 0])
    y = q[:, 1]
    n = norm([h, w])
    slope = x * n[0] + (y - h) * n[1]
    d = np.maximum(slope, -y)
    d = np.maximum(d, np.abs(q[:, 2]) - l)
    return d


class Camera:
    def __init__(self, kind="ortho", pitch=35.0, k=0.8, w=64, h=64, anchor=(32, 54)):
        self.kind = kind
        self.w, self.h = w, h
        self.anchor = anchor
        if kind == "ortho":
            th = np.radians(pitch)
            self.right = np.array([1.0, 0, 0])
            self.up = np.array([0, np.cos(th), -np.sin(th)])
            self.d = np.array([0, -np.sin(th), -np.cos(th)])
            self.D = 400.0
        else:
            self.k = k
            self.d = norm([0, -1.0, -k])
            self.Y0 = 300.0

    def rays(self):
        jj, ii = np.meshgrid(np.arange(self.w), np.arange(self.h))
        sx = (jj + 0.5 - self.anchor[0]).ravel()
        sy = (ii + 0.5 - self.anchor[1]).ravel()
        if self.kind == "ortho":
            o = sx[:, None] * self.right - sy[:, None] * self.up - self.d * self.D
        else:
            o = np.stack([sx, np.full_like(sx, self.Y0), sy + self.k * self.Y0], 1)
        return o

    def project(self, p):
        p = np.asarray(p, float)
        if self.kind == "ortho":
            sx = p @ self.right
            sy = -(p @ self.up)
            t = p @ self.d + self.D
        else:
            sx = p[..., 0]
            sy = p[..., 2] - self.k * p[..., 1]
            t = (self.Y0 - p[..., 1]) * np.sqrt(1 + self.k ** 2)
        return sx + self.anchor[0], sy + self.anchor[1], t


def hexc(s):
    s = s.lstrip("#")
    return np.array([int(s[i:i + 2], 16) for i in (0, 2, 4)], float)


class Material:
    def __init__(self, ramp, line=None, spec=None, emissive=False, bias=0.0, ao=True, shadow=True):
        self.ramp = [hexc(c) for c in ramp]
        self.line = hexc(line) if line else self.ramp[0] * 0.55
        self.spec = hexc(spec) if spec else None
        self.emissive = emissive
        self.bias = bias
        self.ao = ao
        self.shadow = shadow


LIGHT = norm([-0.55, 0.85, 0.45])


def scene_dist(prims, p):
    best = np.full(len(p), 1e9)
    idx = np.zeros(len(p), int)
    for i, pr in enumerate(prims):
        d = pr.dist(p)
        m = d < best
        best[m] = d[m]
        idx[m] = i
    return best, idx


def march(prims, o, d, tmax, steps=110, eps=0.03):
    n = len(o)
    t = np.zeros(n)
    hit = np.full(n, -1)
    active = np.arange(n)
    for _ in range(steps):
        if len(active) == 0:
            break
        p = o[active] + t[active, None] * d
        dist, idx = scene_dist(prims, p)
        done = dist < eps
        hit[active[done]] = idx[done]
        t[active] += np.maximum(dist, eps * 0.5)
        keep = (~done) & (t[active] < tmax)
        active = active[keep]
    return t, hit


def shadow_ray(prims, p, steps=40, tmax=60.0):
    n = len(p)
    t = np.full(n, 0.6)
    lit = np.ones(n, bool)
    res = np.ones(n)
    active = np.arange(n)
    for _ in range(steps):
        if len(active) == 0:
            break
        q = p[active] + t[active, None] * LIGHT
        dist, _ = scene_dist(prims, q)
        hitm = dist < 0.05
        lit[active[hitm]] = False
        res[active] = np.minimum(res[active], np.clip(6.0 * dist / t[active], 0, 1))
        t[active] += np.maximum(dist, 0.25)
        keep = (~hitm) & (t[active] < tmax)
        active = active[keep]
    res[~lit] = 0
    return res


def ao_term(prims, p, n, step=1.2, count=4):
    occ = np.zeros(len(p))
    w = 1.0
    for i in range(1, count + 1):
        h = step * i
        dist, _ = scene_dist(prims, p + n * h)
        occ += w * np.clip(h - dist, 0, None) / h
        w *= 0.6
    return np.clip(1 - occ * 0.55, 0, 1)


def render(prims, mats, cam, layers=None, decals=(), shadows=True, ao=True, outline=True,
           inner_lines=True, line_depth=2.5, light_steps=None, bands=None):
    o = cam.rays()
    tmax = cam.D * 2 if cam.kind == "ortho" else cam.Y0 * 3
    t, hit = march(prims, o, cam.d, tmax)
    H, W = cam.h, cam.w
    img = np.zeros((H * W, 4))
    depth = np.full(H * W, np.inf)
    pid = np.full(H * W, -1)
    hm = np.where(hit >= 0)[0]
    if len(hm):
        p = o[hm] + t[hm, None] * cam.d
        ids = hit[hm]
        nrm = np.zeros_like(p)
        e = 0.05
        for i in np.unique(ids):
            sel = ids == i
            q = p[sel]
            pr = prims[i]
            g = np.stack([
                pr.dist(q + [e, 0, 0]) - pr.dist(q - [e, 0, 0]),
                pr.dist(q + [0, e, 0]) - pr.dist(q - [0, e, 0]),
                pr.dist(q + [0, 0, e]) - pr.dist(q - [0, 0, e]),
            ], 1)
            g /= np.maximum(np.linalg.norm(g, axis=1, keepdims=True), 1e-9)
            nrm[sel] = g
        lam = nrm @ LIGHT
        val = 0.5 + 0.5 * lam
        val = np.clip(val, 0, 1)
        if shadows:
            sh = shadow_ray(prims, p + nrm * 0.3)
            castm = np.array([mats[prims[i].mat].shadow for i in ids])
            val = np.where(castm, val * (0.55 + 0.45 * sh), val)
        if ao:
            a = ao_term(prims, p, nrm)
            aom = np.array([mats[prims[i].mat].ao for i in ids])
            val = np.where(aom, val * (0.6 + 0.4 * a), val)
        view = -cam.d
        for i in np.unique(ids):
            sel = ids == i
            pr = prims[i]
            m = mats[pr.mat]
            v = val[sel] + m.bias
            extra = None
            if pr.tex is not None:
                v, extra = pr.tex(p[sel], nrm[sel], v)
            ramp = m.ramp
            nlev = len(ramp)
            if m.emissive:
                col = np.tile(ramp[-1], (sel.sum(), 1))
            else:
                th = bands if bands is not None else np.linspace(0, 1, nlev + 1)[1:-1]
                li = np.searchsorted(th, v)
                li = np.clip(li, 0, nlev - 1)
                col = np.array(ramp)[li]
                if m.spec is not None:
                    hv = norm(LIGHT + view)
                    sp = (nrm[sel] @ hv) > 0.93
                    col[sp] = m.spec
            if extra is not None:
                em = extra[0]
                col[em] = extra[1][em] if extra[1].ndim == 2 else extra[1]
            img[hm[sel], :3] = col
            img[hm[sel], 3] = 255
        depth[hm] = t[hm]
        pid[hm] = ids
    img = img.reshape(H, W, 4)
    depth = depth.reshape(H, W)
    pid = pid.reshape(H, W)

    for (pt, color, tol) in decals:
        sx, sy, tt = cam.project(pt)
        j, i = int(np.floor(sx)), int(np.floor(sy))
        if 0 <= i < H and 0 <= j < W and pid[i, j] >= 0 and abs(depth[i, j] - tt) < tol:
            img[i, j, :3] = hexc(color) if isinstance(color, str) else color
            img[i, j, 3] = 255

    if inner_lines:
        out = img.copy()
        for di, dj in ((0, 1), (1, 0), (0, -1), (-1, 0)):
            sh_d = np.full_like(depth, np.inf)
            sh_p = np.full_like(pid, -1)
            si = slice(max(di, 0), H + min(di, 0))
            sj = slice(max(dj, 0), W + min(dj, 0))
            ti = slice(max(-di, 0), H + min(-di, 0))
            tj = slice(max(-dj, 0), W + min(-dj, 0))
            sh_d[ti, tj] = depth[si, sj]
            sh_p[ti, tj] = pid[si, sj]
            mask = (pid >= 0) & (sh_p >= 0) & (depth - sh_d > line_depth)
            grp_a = np.vectorize(lambda x: prims[x].group if x >= 0 else None, otypes=[object])(pid)
            grp_b = np.vectorize(lambda x: prims[x].group if x >= 0 else None, otypes=[object])(sh_p)
            diff = (grp_a != grp_b) | (grp_a == None)  # noqa: E711
            mask &= diff
            for i, j in zip(*np.where(mask)):
                out[i, j, :3] = mats[prims[pid[i, j]].mat].line
        img = out

    layer_imgs = {}
    names = layers or [None]
    for ln in names:
        li = img.copy()
        if ln is not None:
            keep = np.vectorize(lambda x: x >= 0 and prims[x].layer in ln)(pid)
            li[~keep] = 0
            lpid = np.where(keep, pid, -1)
        else:
            lpid = pid
        if outline:
            li = add_outline(li, lpid, prims, mats)
        layer_imgs[ln if ln is None else tuple(ln)] = li
    return layer_imgs if layers else layer_imgs[None], depth, pid


def add_outline(img, pid, prims, mats):
    H, W = pid.shape
    out = img.copy()
    alpha = img[:, :, 3] > 0
    for i, j in zip(*np.where(~alpha)):
        best = None
        bd = None
        for di, dj in ((0, 1), (1, 0), (0, -1), (-1, 0)):
            a, b = i + di, j + dj
            if 0 <= a < H and 0 <= b < W and alpha[a, b] and pid[a, b] >= 0:
                m = mats[prims[pid[a, b]].mat]
                best = m.line
                break
        if best is not None:
            out[i, j, :3] = best
            out[i, j, 3] = 255
    return out


def to_image(arr):
    from PIL import Image
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")
