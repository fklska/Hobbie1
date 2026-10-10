import json
import os
import numpy as np
import sdf
from sdf import Camera, Prim, Material, norm, render, rot_axis
from kit import Kit, hash2
from palette import MATS
from resources import STONES, ROCK_TEX
import godot_res as gr

STAGES = 4
OUT = "Art/resources/world"
TILESET = "Resourses/v2/TileSetResV2.tres"
TILESET_UID = "uid://b4356nalyihnr"
SWAY_MATERIAL = "res://Resourses/v2/tree_material.tres"
TREE_BASE = 12
ROCK_BASE = -2
CUTS = (1.0, 0.8, 0.6, 0.2)
ROCK_SCALE = 1.3

NMATS = dict(MATS, **{
    "bark": Material(["#2a1a10", "#4a2f1d", "#6b4529", "#8a5d38", "#a8774a"], line="#170d08"),
    "bark_pine": Material(["#24140f", "#432519", "#633823", "#814c30", "#9a603d"], line="#140a07"),
    "bark_grey": Material(["#2c2a28", "#4c4844", "#6e6862", "#928a82", "#b4aca2"], line="#191716"),
    "bark_birch": Material(["#4a4640", "#8a857c", "#c4bfb4", "#e6e2d8", "#faf8f2"], line="#2a2724"),
    "bark_palm": Material(["#3a2a16", "#5e4626", "#836638", "#a8884e", "#c8a866"], line="#21180c"),
    "bark_baobab": Material(["#3a2e28", "#5e4c42", "#82705f", "#a6927c", "#c6b29a"], line="#211a16"),
    "fresh": Material(["#6e4a28", "#a87a46", "#d2a868", "#ecc88a", "#f8e2b0"], line="#3e2812"),
    "cactus_cut": Material(["#4a6a2e", "#7a9a48", "#a8c46a", "#cce08e", "#e6f2b4"], line="#26381a"),
    "leaf_oak": Material(["#0e2a22", "#1a4a2a", "#2e6e2e", "#4e9636", "#7cbc3e", "#b2dc52"], line="#08180f"),
    "leaf_birch": Material(["#1a3a14", "#2f5e1c", "#4c8a28", "#74b036", "#a2d04a", "#d0ec72"], line="#0f2309"),
    "leaf_jungle": Material(["#06221a", "#0e3a24", "#185a30", "#25793a", "#3c9a46", "#62b852"], line="#03140e"),
    "leaf_palm": Material(["#183614", "#2a581c", "#457e26", "#68a432", "#94c844", "#c2e260"], line="#0e200a"),
    "leaf_willow": Material(["#1c3018", "#2f4c28", "#486c36", "#668e44", "#88ae58", "#aeca74"], line="#101c0e"),
    "leaf_acacia": Material(["#262e16", "#3e4a22", "#5a682c", "#7a8a38", "#9eaa4a", "#c2c666"], line="#151a0c"),
    "leaf_shrub": Material(["#1e2c18", "#334824", "#4c6630", "#68843c", "#88a24c", "#aabe62"], line="#111a0e"),
    "leaf_baobab": Material(["#1c3216", "#2e5020", "#467228", "#649634", "#88b844"], line="#10200c"),
    "needle": Material(["#06201c", "#0c3326", "#164a30", "#22643a", "#358046", "#52a052"], line="#03120e"),
    "needle_dark": Material(["#041814", "#0a2a20", "#123e2a", "#1c5634", "#2b6e3e", "#428a4a"], line="#020c0a"),
    "cactus": Material(["#122a18", "#1d4424", "#2a6232", "#3c8240", "#58a052", "#80bc68"], line="#0a180c"),
    "vine": Material(["#0e2a14", "#1a4420", "#2a642c", "#3e843a"], line="#06160a"),
    "snow": Material(["#7c8ea8", "#a6b8cc", "#cad8e6", "#e6eef6", "#fafcff"], line="#4a5a72"),
    "moss": Material(["#1c2e14", "#2c4a1c", "#426826", "#5e8a32", "#7eaa44"], line="#0f1a0a"),
    "granite": Material(["#2e2d38", "#4c4b5c", "#6e6d80", "#9493a4", "#bab9c6"], line="#1a1922"),
    "sandstone": Material(["#4a2c1c", "#7a4a2c", "#a86c3e", "#cc9056", "#e6b67a"], line="#2a180e"),
    "basalt": Material(["#1e2220", "#343a36", "#4c5450", "#66706a", "#828c84"], line="#101311"),
    "ore_iron": Material(["#3a1a10", "#6e2e18", "#a44a26", "#d0703a", "#f09a5a"], line="#1c0c06", spec="#ffd0a0"),
    "ore_gold": Material(["#5a3410", "#a86a1c", "#e0a62e", "#f8d65a", "#fff2a0"], line="#2d1806", spec="#ffffff"),
    "flower": Material(["#5a1430", "#9a2a4a", "#d0506a", "#f08aa0"], line="#300a18"),
    "coconut": Material(["#1e1208", "#3a2412", "#58381c", "#784e2a"], line="#120a04"),
})


def phase(c, k=0.0):
    return np.modf(np.abs(np.sin(np.asarray(c, float) @ [12.9898, 78.233, 37.719] + np.array([0.0, 1.7, 3.1]) + k) * 43758.5453))[0] * 6.283


def fib(n):
    i = np.arange(n) + 0.5
    ph = np.arccos(1 - 2 * i / n)
    th = np.pi * (1 + 5 ** 0.5) * i
    return np.stack([np.cos(th) * np.sin(ph), np.cos(ph), np.sin(th) * np.sin(ph)], 1)


def frame_along(t, up=(0, 1, 0)):
    t = norm(t)
    b = np.cross(t, up)
    b = norm(b) if np.linalg.norm(b) > 1e-6 else np.array([0.0, 0.0, 1.0])
    return np.stack([t, np.cross(b, t), b], 1)


def t_speckle(cell=3.0, var=0.12, seed=0.0, shift=0.0):
    def f(p, n, val):
        c = np.floor(p / cell)
        return val + shift + (hash2(c[:, 0] + c[:, 2] * 7.1, c[:, 1], seed) - 0.5) * var, None
    return f


def t_bark(width=2.6, seg=4.0, var=0.22, seed=0.0):
    def f(p, n, val):
        u = (p[:, 0] * 0.8 + p[:, 2] * 0.6) / width
        col = np.floor(u)
        row = np.floor(p[:, 1] / seg + hash2(col, 1.3, seed) * 2.0)
        val = val + (hash2(col, row, seed) - 0.5) * var
        return np.where(u - col < 0.28, val - 0.22, val), None
    return f


def t_birch(seed=0.0):
    black = np.array([34.0, 30.0, 28.0])

    def f(p, n, val):
        row = np.floor(p[:, 1] / 2.5)
        h = hash2(row, np.floor((p[:, 0] + p[:, 2]) / 3.0), seed)
        return val, ((h > 0.74) & (np.abs(n[:, 1]) < 0.85), black)
    return f


def t_rings(step=3.0, seed=0.0):
    def f(p, n, val):
        v = p[:, 1] / step
        return np.where(v - np.floor(v) < 0.32, val - 0.28, val + (hash2(np.floor(v), 2.0, seed) - 0.5) * 0.14), None
    return f


def t_ribs(count=10):
    def f(p, n, val):
        a = np.arctan2(n[:, 2], n[:, 0]) / (2 * np.pi) * count
        return np.where(a - np.floor(a) < 0.3, val - 0.22, val + 0.04), None
    return f


def t_strata(seed=0.0):
    def f(p, n, val):
        v = p[:, 1] / 2.6 + hash2(np.floor(p[:, 0] / 6), 4.0, seed) * 0.6
        val = val + (hash2(np.floor(v), np.floor(p[:, 0] / 5), seed) - 0.5) * 0.2
        return np.where(v - np.floor(v) < 0.22, val - 0.18, val), None
    return f


def _ramp_color(mat, v, lift=0.0):
    ramp = np.array(NMATS[mat].ramp)
    return ramp[np.clip(((np.clip(v, 0, 1) + lift) * len(ramp)).astype(int), 0, len(ramp) - 1)]


def t_cover(base, cover, top=0.75, var=0.3, cell=3.0, seed=0.0, lift=0.25):
    def f(p, n, val):
        v, extra = base(p, n, val) if base else (val, None)
        c = np.floor(p / cell)
        m = n[:, 1] + (hash2(c[:, 0] + c[:, 2] * 5.3, c[:, 1], seed + 3.0) - 0.5) * var > top
        col = _ramp_color(cover, v, lift)
        if extra is not None:
            col = np.where(m[:, None], col, extra[1])
            m = m | extra[0]
        return v, (m, col)
    return f


def t_cut(base, cutter, mat="fresh"):
    def f(p, n, val):
        v, extra = base(p, n, val) if base else (val, None)
        m = np.abs(cutter(p)) < 0.7
        col = _ramp_color(mat, v)
        if extra is not None:
            col = np.where(m[:, None], col, extra[1])
            m = m | extra[0]
        return v, (m, col)
    return f


class Plant(Kit):
    def __init__(self, stage, seed=0):
        super().__init__()
        self.stage = stage
        self.seed = seed
        self.rng = np.random.RandomState(seed)

    def gone(self, rank):
        return rank >= CUTS[self.stage]

    def blob(self, c, r, mat, amp=0.4, freq=1.2, squash=(1, 1, 1), tex=None, group=None):
        c = np.asarray(c, float)
        s = np.asarray(squash, float)
        ph = phase(c)

        def fn(p):
            q = (p - c) / s
            d = np.linalg.norm(q, axis=1) - r
            disp = np.sin(q[:, 0] * freq + ph[0]) * np.sin(q[:, 1] * freq + ph[1]) * np.sin(q[:, 2] * freq + ph[2])
            return (d + amp * disp) * s.min()
        return self.add("custom", mat, tex, group, fn=fn)

    def solid(self, dist, mat, tex=None, group=None, notch=None, cut_mat="fresh"):
        if notch is None:
            return self.add("custom", mat, tex, group, fn=dist)
        return self.add("custom", mat, t_cut(tex, notch, cut_mat), group, fn=lambda p: np.maximum(dist(p), -notch(p)))

    def trunk(self, a, b, r, r2, mat, tex=None, group="trunk", notch=None):
        return self.solid(Prim("capsule", mat, a=np.asarray(a, float), b=np.asarray(b, float), r=r, r2=r2).dist, mat, tex, group, notch)

    def notch(self, y, r, side=(-0.45, 0.0, 0.9), center=(0, 0)):
        depth = (0, 0.3, 0.5, 0.68)[self.stage] * r
        if depth == 0:
            return None
        s = norm(side)
        size = r * 0.9
        R = rot_axis(np.cross([0, 1, 0], s), np.pi / 4)
        c = np.array([center[0], y, center[1]]) + s * (r + size * 0.707 - depth)
        return Prim("box", "fresh", c=c, h=np.full(3, size * 0.5), R=R).dist

    def chips(self, spread=12.0, center=(0, 0)):
        if not self.stage:
            return
        rng = np.random.RandomState(self.seed + 7)
        for i in range(2 + self.stage * 2):
            a = rng.uniform(0, 2 * np.pi)
            d = rng.uniform(0.4, 1.0) * spread
            R = rot_axis([0, 1, 0], rng.uniform(0, 3)) @ rot_axis([1, 0, 0], rng.uniform(-0.3, 0.3))
            self.box([center[0] + np.cos(a) * d, 0.6, center[1] + abs(np.sin(a)) * d * 0.8 + 3],
                     [rng.uniform(1.0, 1.8), 0.5, rng.uniform(0.6, 1.0)], "fresh", R=R, group="chip%d" % i)

    def log(self, x, z, length, r, angle=0.35, mat="bark"):
        if self.stage < 3:
            return
        a = np.array([np.cos(angle), 0, -np.sin(angle)])
        c = np.array([x, r - 0.3, z])
        self.cap(c - a * length / 2, c + a * length / 2, r, mat, group="log")
        F = frame_along(a)
        R = np.stack([F[:, 1], F[:, 0], F[:, 2]], 1)
        for sgn in (1, -1):
            self.cyl(c + sgn * a * (length / 2 + r * 0.6), r - 0.5, 0.4, "fresh", R=R, group="logend")


def crown(k, center, env, n, mat, cr=0.24, bottom=-0.35, sectors=5, branch_from=None, branch_r=1.5, bark="bark",
          squash=(1, 1, 1), keep_top=0.75, hang=None):
    c = np.asarray(center, float)
    env = np.asarray(env, float)
    pts = fib(n)
    pts = pts[pts[:, 1] > bottom]
    off = k.rng.uniform(0, 2 * np.pi)
    ranks = (k.rng.permutation(sectors) + 0.5) / sectors
    scale = k.rng.uniform(0.82, 0.95, len(pts))
    size = k.rng.uniform(0.85, 1.15, len(pts))
    sec = (np.floor(((np.arctan2(pts[:, 2], pts[:, 0]) + off) % (2 * np.pi)) / (2 * np.pi) * sectors)).astype(int)
    top = pts[:, 1] > keep_top
    if branch_from is not None:
        b = np.asarray(branch_from, float)
        for s in range(sectors):
            m = (sec == s) & ~top
            if not m.any():
                continue
            d = norm(pts[m].mean(0) * [1, 0.6, 1])
            tip = c + d * env * (0.62 if k.gone(ranks[s]) else 0.5)
            k.cap(b, tip, branch_r, bark, r2=branch_r * 0.5, group="br%d" % s)
    radius = env[0] * cr
    for i, d in enumerate(pts):
        rank = 0.25 if top[i] else ranks[sec[i]]
        if k.gone(rank):
            continue
        p = c + d * env * scale[i]
        shift = 0.1 * d[1] - 0.07 * d[0] + 0.03 * d[2] - 0.04
        k.blob(p, radius * size[i], mat, squash=squash, tex=t_speckle(2.0, 0.1, k.seed + i, shift), group="crown")
    if hang:
        length, count, hmat, width = hang
        hs = k.rng.uniform(0, 1, (count, 4))
        for j in range(count):
            a = 2 * np.pi * (j + hs[j, 0] * 0.6) / count
            s = int(np.floor(((a + off) % (2 * np.pi)) / (2 * np.pi) * sectors))
            if k.gone(ranks[s]):
                continue
            rr = 0.92 + hs[j, 1] * 0.14
            x, z = np.cos(a) * env[0] * rr, np.sin(a) * env[2] * rr
            ln = length * (0.6 + 0.5 * hs[j, 2])
            top_y = c[1] + env[1] * (0.1 - 0.2 * hs[j, 3])
            k.blob([c[0] + x, top_y - ln * 0.5, c[2] + z], 1.0, hmat, amp=0.25, freq=1.6, squash=(width, ln * 0.5, width * 0.9),
                   tex=t_speckle(2.0, 0.1, k.seed + 50 + j, 0.05 - 0.06 * np.cos(a)), group="hang")
    return ranks


def deciduous(stage, seed, R, H, trunk, n, mat="leaf_oak", bark="bark", roots=3, ry=0.78, cr=0.24):
    k = Plant(stage, seed)
    k.trunk([0, -2, 0], [0, H * 0.8, 0], trunk, trunk * 0.6, bark, tex=t_bark(seed=seed), notch=k.notch(trunk * 1.7, trunk))
    ra = k.rng.uniform(-0.3, 0.3, roots)
    for i in range(roots):
        a = 0.5 + i * 2 * np.pi / roots + ra[i]
        k.cap([0, 1, 0], [np.cos(a) * trunk * 1.9, 0.2, np.sin(a) * trunk * 1.9], trunk * 0.55, bark, r2=trunk * 0.25, group="root%d" % i)
    crown(k, [0, H, 0], [R, R * ry, R * 0.85], n, mat, cr=cr, branch_from=[0, H * 0.55, 0], branch_r=max(trunk * 0.42, 1.1), bark=bark)
    k.chips(trunk * 3 + 4)
    k.log(trunk * 3.2 + 4, 8, R * 0.6, trunk * 0.7, mat=bark)
    return k


def oak_small(stage):
    return deciduous(stage, 11, R=16, H=32, trunk=2.4, n=34, cr=0.3)


def oak(stage):
    return deciduous(stage, 12, R=29, H=54, trunk=4.0, n=70)


def oak_giant(stage):
    return deciduous(stage, 13, R=44, H=78, trunk=6.2, n=120, roots=4, cr=0.21)


def birch(stage):
    k = Plant(stage, 14)
    trunk = 2.8
    k.trunk([0, -2, 0], [1.0, 88, 0], trunk, trunk * 0.5, "bark_birch", tex=t_birch(14), notch=k.notch(trunk * 1.8, trunk))
    crown(k, [0.6, 72, 0], [19, 26, 17], 56, "leaf_birch", cr=0.27, bottom=-0.6, branch_from=[0.4, 50, 0], branch_r=1.0, bark="bark_birch")
    k.chips(12)
    k.log(13, 8, 24, 2.2, mat="bark_birch")
    return k


def tier(y0, r, h, lobes, jag, spike=0.18, under=0.35, droop=2.0):
    def fn(p):
        q = p - [0, y0, 0]
        rad = np.sqrt(q[:, 0] ** 2 + q[:, 2] ** 2)
        f = np.arctan2(q[:, 2], q[:, 0]) * lobes / (2 * np.pi) + jag
        tri = 1 - 2 * np.abs(f - np.floor(f) - 0.5)
        re = r * (1 - spike + spike * 2 * tri ** 2)
        y = q[:, 1] + droop * tri * np.clip(rad / r, 0, 1) ** 2
        side = (rad - re * (1 - np.clip(y / h, 0, 1))) * 0.7
        bottom = under * h * (1 - np.clip(rad / re, 0, 1)) - y
        return np.maximum(np.maximum(side, bottom), y - h)
    return fn


def conifer(stage, seed, H, R, tiers, trunk, mat="needle", snow=False, bark="bark_pine"):
    k = Plant(stage, seed)
    removed = (0, 1, max(2, tiers // 2), tiers - 2)[stage]
    k.trunk([0, -2, 0], [0, H * 0.9, 0], trunk, trunk * 0.4, bark, tex=t_bark(2.0, 3.0, seed=seed), notch=k.notch(trunk * 1.5, trunk))
    base = H * 0.1
    overlap = 1.7
    step = (H - base) / (tiers + overlap - 1)
    jags = k.rng.uniform(0, 1, tiers)
    stubs = k.rng.uniform(0, 2 * np.pi, (tiers, 3))
    tex = t_speckle(2.0, 0.14, seed)
    for i in range(tiers):
        y0 = base + i * step
        frac = 1 - i / tiers
        h = step * overlap * (1.25 if i == tiers - 1 else 1)
        if i < removed:
            for a in stubs[i]:
                k.cap([0, y0 + h * 0.2, 0], [np.cos(a) * trunk * 2.4, y0 + h * 0.1, np.sin(a) * trunk * 2.4], trunk * 0.35, bark, group="stub%d" % i)
            continue
        ttex = t_cover(tex, "snow", top=0.62, var=0.35, cell=3.0, seed=seed + i, lift=0.2) if snow else tex
        k.add("custom", mat, ttex, "tier%d" % i, fn=tier(y0, R * (0.25 + 0.75 * frac), h, 9 + i % 2, jags[i], droop=2.0 * frac))
    if snow:
        k.blob([1, -0.6, 3], R * 0.5, "snow", amp=0.6, freq=0.5, squash=(1, 0.12, 0.7), tex=t_speckle(2.0, 0.1, seed), group="mound")
    k.chips(trunk * 3 + 5)
    k.log(trunk * 3.4 + 4, 9, H * 0.3, trunk * 0.75, mat=bark)
    return k


def pine_small(stage):
    return conifer(stage, 21, H=46, R=14, tiers=4, trunk=2.2)


def pine(stage):
    return conifer(stage, 22, H=90, R=24, tiers=6, trunk=3.4)


def pine_giant(stage):
    return conifer(stage, 23, H=136, R=33, tiers=8, trunk=5.0, mat="needle_dark")


def fir_small(stage):
    return conifer(stage, 31, H=44, R=14, tiers=4, trunk=2.2, snow=True)


def fir(stage):
    return conifer(stage, 32, H=86, R=24, tiers=6, trunk=3.4, snow=True)


def fir_giant(stage):
    return conifer(stage, 33, H=130, R=33, tiers=8, trunk=5.0, mat="needle_dark", snow=True)


def frond(k, start, direction, length, rise, droop, width, mat, group, segs=7):
    d = norm([direction[0], 0, direction[2]])
    pts = []
    for i in range(segs + 1):
        t = i / segs
        pts.append(np.asarray(start, float) + d * length * t + np.array([0, rise * t - droop * t * t, 0]))
    for i in range(segs):
        a, b = pts[i], pts[i + 1]
        w = width * (1.0 - 0.75 * (i / segs) ** 1.5)
        F = frame_along(b - a)
        ln = np.linalg.norm(b - a) * 0.75
        k.ell((a + b) / 2, [ln, 0.7, w], mat, R=F, tex=t_speckle(1.5, 0.12, k.seed + i, 0.04 - 0.05 * d[0]), group=group)
    return pts[-1]


def palm(stage):
    k = Plant(stage, 51)
    H = 72
    ts = np.linspace(0, 1, 8)
    pts = [np.array([9 * t * t, H * t, -2 * t]) for t in ts]
    for i in range(len(pts) - 1):
        r = 3.6 - 1.3 * i / (len(pts) - 1)
        k.trunk(pts[i], pts[i + 1], r, r - 0.18, "bark_palm", tex=t_rings(3.2, 51), notch=k.notch(5, 3.6) if i == 0 else None)
    top = pts[-1] + [0, 1.5, 0]
    n = 8
    ranks = (k.rng.permutation(n) + 0.5) / n
    jit = k.rng.uniform(-0.25, 0.25, (n, 3))
    for j in range(n):
        if k.gone(ranks[j]):
            continue
        a = 2 * np.pi * j / n + jit[j, 0]
        frond(k, top, [np.cos(a), 0, np.sin(a) * 0.8], 30 + 6 * jit[j, 1], 10, 24 + 6 * jit[j, 2], 3.6, "leaf_palm", "frond%d" % j)
    k.blob(top + [0, 1.0, 0], 2.4, "leaf_palm", squash=(1, 1.2, 1), tex=t_speckle(1.5, 0.1, 5, -0.12), group="tuft")
    if stage == 0:
        for a in (0.5, 2.6, 4.4):
            k.sphere(top + [np.cos(a) * 2.6, -3.5, np.sin(a) * 2.6], 2.1, "coconut", group="nut")
    k.chips(13)
    k.log(16, 8, 26, 2.8, mat="bark_palm")
    return k


def jungle(stage):
    k = Plant(stage, 61)
    H = 88
    trunk = 5.2
    k.trunk([0, -2, 0], [0, H * 0.85, 0], trunk, trunk * 0.62, "bark", tex=t_bark(2.4, 5.0, seed=61), notch=k.notch(trunk * 2.2, trunk, side=(-0.2, 0, 1)))
    for a in (0.3, 1.9, 3.3, 4.8):
        F = frame_along([np.cos(a), 0, np.sin(a)])
        k.ell([np.cos(a) * 6, 5, np.sin(a) * 6], [8, 7, 1.6], "bark", R=F, tex=t_bark(2.4, 5.0, seed=61), group="buttress%d" % int(a * 10))
    crown(k, [0, H, 0], [52, 18, 28], 110, "leaf_jungle", cr=0.2, bottom=-0.55, branch_from=[0, H * 0.6, 0], branch_r=2.4,
          hang=(30, 11, "vine", 1.6))
    k.chips(20)
    k.log(22, 9, 34, 3.6)
    return k


def banana(stage):
    k = Plant(stage, 121)
    k.trunk([0, -1, 0], [0, 16, 0], 3.0, 2.2, "bark_palm", tex=t_rings(2.4, 121))
    n = 7
    ranks = (k.rng.permutation(n) + 0.5) / n
    jit = k.rng.uniform(-0.3, 0.3, (n, 3))
    for j in range(n):
        if k.gone(ranks[j]):
            continue
        a = 2 * np.pi * j / n + jit[j, 0]
        frond(k, [0, 15, 0], [np.cos(a), 0, np.sin(a) * 0.8], 18 + 4 * jit[j, 1], 12, 14 + 4 * jit[j, 2], 5.2, "leaf_palm", "leaf%d" % j, segs=5)
    k.blob([0, 17, 0], 2.0, "leaf_palm", squash=(1, 1.4, 1), tex=t_speckle(1.5, 0.1, 9, -0.12), group="tuft")
    k.chips(9)
    return k


def willow(stage):
    k = Plant(stage, 71)
    H = 44
    trunk = 5.4
    k.trunk([0, -2, 0], [2, H * 0.9, 0], trunk, trunk * 0.6, "bark", tex=t_bark(seed=71), notch=k.notch(trunk * 1.7, trunk))
    for a in (0.4, 2.3, 4.2):
        k.cap([0, 1, 0], [np.cos(a) * trunk * 1.9, 0.2, np.sin(a) * trunk * 1.9], trunk * 0.5, "bark", r2=trunk * 0.25, group="root")
    crown(k, [2, H + 10, 0], [30, 13, 22], 56, "leaf_willow", cr=0.27, bottom=-0.2, branch_from=[2, H * 0.7, 0], branch_r=2.0,
          hang=(48, 46, "leaf_willow", 1.8))
    k.chips(18)
    k.log(20, 9, 26, 3.4)
    return k


def deadtree(stage):
    k = Plant(stage, 81)
    H = 60
    trunk = 4.0
    k.trunk([0, -2, 0], [2, H * 0.75, 0], trunk, trunk * 0.55, "bark_grey", tex=t_bark(2.2, 4.0, seed=81), notch=k.notch(trunk * 1.7, trunk))
    for a in (0.6, 2.7, 4.5):
        k.cap([0, 1, 0], [np.cos(a) * trunk * 2.0, 0.2, np.sin(a) * trunk * 2.0], trunk * 0.5, "bark_grey", r2=trunk * 0.22, group="root")
    n = 5
    ranks = (k.rng.permutation(n) + 0.5) / n
    rnd = k.rng.uniform(-1, 1, (n, 6))
    for j in range(n):
        y = H * (0.45 + 0.08 * j)
        a = 2 * np.pi * j / n + rnd[j, 0] * 0.3
        start = np.array([2 * y / (H * 0.75), y, 0])
        d = norm([np.cos(a), 0.9 + 0.3 * rnd[j, 1], np.sin(a) * 0.8])
        ln = 24 + 6 * rnd[j, 2]
        if k.gone(ranks[j]):
            k.cap(start, start + d * 5, trunk * 0.4, "bark_grey", r2=trunk * 0.3, group="stub%d" % j)
            continue
        end = start + d * ln
        k.cap(start, end, trunk * 0.42, "bark_grey", r2=trunk * 0.16, group="b%d" % j)
        for s in (-1, 1):
            d2 = norm(d + np.array([np.sin(a) * 0.7 * s, 0.25 + 0.2 * rnd[j, 3], -np.cos(a) * 0.5 * s]))
            mid = start + d * ln * (0.55 + 0.1 * s)
            k.cap(mid, mid + d2 * (11 + 3 * rnd[j, 4 + (s > 0)]), trunk * 0.22, "bark_grey", r2=trunk * 0.1, group="b%d" % j)
    k.cap([2, H * 0.72, 0], [3, H + 6, -1], trunk * 0.45, "bark_grey", r2=trunk * 0.15, group="top")
    k.chips(14)
    k.log(15, 8, 24, 2.8, mat="bark_grey")
    return k


def acacia(stage):
    k = Plant(stage, 91)
    trunk = 3.2
    fork = np.array([2.0, 34, 0])
    k.trunk([0, -2, 0], fork, trunk, trunk * 0.8, "bark", tex=t_bark(seed=91), notch=k.notch(trunk * 1.8, trunk))
    for a, l in ((0.2, 26), (2.9, 24), (1.6, 18), (4.6, 16)):
        k.cap(fork, fork + [np.cos(a) * l, 20, np.sin(a) * l * 0.6], trunk * 0.6, "bark", r2=trunk * 0.35, group="limb")
    crown(k, [2, 60, 0], [54, 7, 20], 120, "leaf_acacia", cr=0.17, bottom=-0.9, keep_top=0.6, squash=(1, 0.55, 1))
    crown(k, [4, 66, -2], [32, 5, 12], 44, "leaf_acacia", cr=0.22, bottom=-0.9, keep_top=0.95, squash=(1, 0.55, 1))
    k.chips(13)
    k.log(16, 9, 26, 2.6)
    return k


def baobab(stage):
    k = Plant(stage, 101)
    H = 54
    body = Prim("capsule", "bark_baobab", a=np.array([0.0, -2, 0]), b=np.array([0.0, H, 0]), r=15.0, r2=10.5)
    belly = Prim("ellipsoid", "bark_baobab", c=np.array([0.0, 18, 0]), r=np.array([17.0, 22, 16]))
    k.solid(lambda p: np.minimum(body.dist(p), belly.dist(p)), "bark_baobab", t_bark(3.4, 6.0, 0.18, 101), "trunk",
            notch=k.notch(14, 16.5, side=(-0.35, 0, 0.95)))
    n = 7
    ranks = (k.rng.permutation(n) + 0.5) / n
    jit = k.rng.uniform(-1, 1, (n, 3))
    for j in range(n):
        a = 2 * np.pi * j / n + jit[j, 0] * 0.3
        start = np.array([np.cos(a) * 6, H, np.sin(a) * 5])
        d = norm([np.cos(a), 0.8 + 0.3 * jit[j, 1], np.sin(a) * 0.8])
        ln = 17 + 4 * jit[j, 2]
        if k.gone(ranks[j]):
            k.cap(start, start + d * 2.5, 3.0, "bark_baobab", r2=2.8, group="stub%d" % j)
            continue
        end = start + d * ln
        k.cap(start, end, 3.4, "bark_baobab", r2=1.8, group="limb%d" % j)
        sub = Plant(stage, 101 + j)
        crown(sub, end + [0, 4, 0], [10, 6, 9], 16, "leaf_baobab", cr=0.36, bottom=-0.6, sectors=1)
        k.prims += sub.prims
    k.chips(26)
    k.log(26, 10, 22, 4.0, mat="bark_baobab")
    return k


def cactus(stage):
    k = Plant(stage, 111)
    top = 42 if stage < 3 else 25
    column = Prim("capsule", "cactus", a=np.array([0.0, -1, 0]), b=np.array([0.0, 40, 0]), r=5.4, r2=5.0)
    cut = (lambda p: np.full(len(p), 1e3)) if stage < 3 else (lambda p: top - p[:, 1])
    k.solid(lambda p: np.maximum(column.dist(p), -cut(p)), "cactus", t_ribs(12), "column", notch=cut if stage == 3 else None, cut_mat="cactus_cut")
    arms = [((-1, 15, 0), (-10, 16, 1), (-11, 30, 1)), ((1, 21, 0), (9, 22, 1), (10, 34, 1))]
    for i, (a, b, c) in enumerate(arms):
        if stage >= 2 - i:
            k.solid(Prim("capsule", "cactus", a=np.array(a, float), b=np.array(a, float) + norm(np.subtract(b, a)) * 4, r=3.4, r2=3.4).dist,
                    "cactus", t_ribs(10), "arm%d" % i)
            k.cyl(np.array(a, float) + norm(np.subtract(b, a)) * 4.2, 3.0, 0.4, "cactus_cut", R=frame_along(np.subtract(b, a))[:, [1, 0, 2]], group="armcut%d" % i)
            continue
        k.cap(a, b, 3.4, "cactus", tex=t_ribs(10), group="arm%d" % i)
        k.cap(b, c, 3.4, "cactus", r2=3.1, tex=t_ribs(10), group="arm%d" % i)
        if stage == 0:
            k.sphere(np.array(c, float) + [0, 3.2, 0], 1.6, "flower", group="flower%d" % i)
    if stage == 0:
        k.sphere([0, 45.5, 0], 1.9, "flower", group="flower")
    return k


def shrub(stage):
    k = Plant(stage, 131)
    for a in (0.3, 1.5, 2.8, 4.1, 5.3):
        k.cap([0, 0, 0], [np.cos(a) * 7, 8, np.sin(a) * 5], 0.9, "bark", r2=0.6, group="twig")
    crown(k, [0, 10, 1], [15, 10, 12], 30, "leaf_shrub", cr=0.32, bottom=-0.8, sectors=4)
    return k


ROCK_LOOKS = {
    "granite": ("granite", None),
    "snow": ("granite", "snow"),
    "sand": ("sandstone", None),
    "moss": ("basalt", "moss"),
}

ORES = {"stone": None, "iron": "ore_iron", "gold": "ore_gold"}


def rock(stage, look, ore, seed=41):
    k = Plant(stage, seed)
    mat, cover = ROCK_LOOKS[look]
    base = t_strata(seed) if look == "sand" else ROCK_TEX
    tex = t_cover(base, cover, top=0.8, var=0.45, cell=3.2, seed=seed, lift=0.15) if cover else base
    keep = [(0, 1, 2, 3, 4), (0, 1, 2, 3), (0, 1, 2), (0, 1)][stage]
    shrink = (1.0, 1.0, 0.8, 0.6)[stage]
    for i in keep:
        c, h, a, rnd = STONES[i]
        R = rot_axis([0, 1, 0], a) @ rot_axis([0, 0, 1], 0.12 * a) @ rot_axis([1, 0, 0], -0.1)
        s = shrink if i == 0 else 1.0
        cc = np.array(c, float) * [1, s, 1] * ROCK_SCALE
        hh = np.array(h, float) * [1, s, 1] * ROCK_SCALE
        k.box(cc, hh, mat, R=R, rnd=rnd * s * ROCK_SCALE, tex=tex, group="s%d" % i)
        k.box(cc + [hh[0] * 0.15, hh[1] * 0.7, -hh[2] * 0.1], hh * [0.6, 0.45, 0.6], mat, R=R @ rot_axis([0, 1, 0], 0.6), rnd=rnd * 0.7 * s * ROCK_SCALE, tex=tex, group="s%d" % i)
        if ORES[ore] and i < 4:
            rng = np.random.RandomState(seed + i)
            for m in range((10, 6, 5, 3)[i]):
                ax = (1, 2, 0, 2, 1, 0, 1, 2, 0, 1)[m]
                q = rng.uniform(-0.65, 0.65, 3) * hh
                q[ax] = hh[ax] * (1 if ax != 0 or m % 2 else -1) * 0.93
                rr = np.array([2.4, 1.8, 2.4]) * rng.uniform(0.75, 1.2) * (1.0 if i == 0 else 0.8) * ROCK_SCALE
                k.ell(cc + R @ q, rr, ORES[ore], R=R @ rot_axis([0, 1, 0], rng.uniform(0, 3)), group="ore%d" % i)
    rng = np.random.RandomState(seed + 9)
    for j in range(stage * 3):
        x, z = rng.uniform(-18, 18) * ROCK_SCALE, rng.uniform(8, 17) * ROCK_SCALE
        k.box([x, 0.8, z], np.array([1.6, 1.0, 1.4]) * rng.uniform(0.7, 1.3) * ROCK_SCALE, mat, R=rot_axis([0, 1, 0], rng.uniform(0, 3)), rnd=0.4, tex=base, group="rubble%d" % j)
    return k


TREES = {
    "oak_small": (oak_small, 1),
    "oak": (oak, 2),
    "oak_giant": (oak_giant, 3),
    "birch": (birch, 2),
    "pine_small": (pine_small, 1),
    "pine": (pine, 2),
    "pine_giant": (pine_giant, 3),
    "fir_small": (fir_small, 1),
    "fir": (fir, 2),
    "fir_giant": (fir_giant, 3),
    "palm": (palm, 2),
    "jungle": (jungle, 3),
    "banana": (banana, 1),
    "willow": (willow, 2),
    "deadtree": (deadtree, 2),
    "acacia": (acacia, 2),
    "baobab": (baobab, 3),
    "cactus": (cactus, 1),
    "shrub": (shrub, 1),
}

COLLISION = {1: (6, 4), 2: (10, 6), 3: (16, 9)}


def looks():
    out = {name: (fn, size) for name, (fn, size) in TREES.items()}
    for ore in ORES:
        for look in ROCK_LOOKS:
            out[f"{ore}_{look}"] = ((lambda s, lk=look, o=ore: rock(s, lk, o)), 0)
    return out


def shoot(kit, w, h, anchor):
    cam = Camera("oblique", k=0.9, w=w, h=h, anchor=anchor)
    return render(kit.prims, NMATS, cam, decals=kit.decals, line_depth=1.5)[0]


def drop_shadow(img, cx, cy, rx, ry, alpha=56):
    h, w = img.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    m = ((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2 <= 1.0
    m &= img[:, :, 3] == 0
    out = img.copy()
    out[m] = [16, 20, 12, alpha]
    return out


def render_look(fn, size):
    tree = size > 0
    w, h = (280, 260) if tree else (96, 96)
    anchor = (w // 2, h - 30) if tree else (w // 2, 60)
    sdf.set_light(sdf.BUILD_LIGHT)
    frames = [shoot(fn(s), w, h, anchor) for s in range(STAGES)]
    sdf.set_light(sdf.CHAR_LIGHT)
    alpha = np.any([f[:, :, 3] > 0 for f in frames], axis=0)
    if tree:
        cols = np.where(alpha.any(0))[0]
        rx = max(18.0, (cols.max() - cols.min()) * 0.32)
        frames = [drop_shadow(f, anchor[0] + rx * 0.25, anchor[1] + 1, rx, rx * 0.32) for f in frames]
        alpha = np.any([f[:, :, 3] > 0 for f in frames], axis=0)
    rows, cols = np.where(alpha.any(1))[0], np.where(alpha.any(0))[0]
    if rows[0] == 0 or cols[0] == 0 or cols[-1] == w - 1 or rows[-1] == h - 1:
        print("clipped:", fn)
    half = max(anchor[0] - cols[0], cols[-1] + 1 - anchor[0]) + 1
    top, bottom = rows[0] - 1, rows[-1] + 2
    if (bottom - top) % 2:
        bottom += 1
    crop = [f[top:bottom, anchor[0] - half:anchor[0] + half] for f in frames]
    return np.concatenate(crop, 0), (int(2 * half), int(bottom - top)), int(anchor[1] - top)


def tile_lines(name, tex_id, region, anchor_y, size):
    fw, fh = region
    base = TREE_BASE if size else ROCK_BASE
    lines = [f'[sub_resource type="TileSetAtlasSource" id="TileSetAtlasSource_{name}"]', f'resource_name = "{name}"',
             f'texture = ExtResource("{tex_id}")', f"texture_region_size = Vector2i({fw}, {fh})"]
    for s in range(STAGES):
        t = f"0:{s}/0"
        lines += [f"{t} = 0", f"{t}/texture_origin = Vector2i(0, {anchor_y - fh // 2 - base})", f"{t}/y_sort_origin = {base + 2 if size else 12}"]
        if size:
            lines.append(f'{t}/material = ExtResource("sway")')
            cx, cy = COLLISION[size]
            lines.append(f"{t}/physics_layer_0/polygon_0/points = PackedVector2Array({-cx}, {base}, 0, {base - cy}, {cx}, {base}, 0, {base + cy})")
    return lines + [""]


def write_tileset(entries):
    head = [f'[gd_resource type="TileSet" format=3 uid="{TILESET_UID}"]', ""]
    ext = [f'[ext_resource type="Material" uid="{gr.uid_for(SWAY_MATERIAL)}" path="{SWAY_MATERIAL}" id="sway"]']
    subs = []
    names = []
    for i, (name, uid, region, anchor_y, size) in enumerate(entries):
        ext.append(f'[ext_resource type="Texture2D" uid="{uid}" path="res://{OUT}/{name}.png" id="{i + 1}_{name}"]')
        subs += tile_lines(name, f"{i + 1}_{name}", region, anchor_y, size)
        names.append(name)
    res = ["[resource]", "tile_size = Vector2i(64, 64)", "physics_layer_0/collision_layer = 8", "physics_layer_0/collision_mask = 7"]
    res += [f'sources/{i} = SubResource("TileSetAtlasSource_{n}")' for i, n in enumerate(names)]
    open(os.path.join(gr.ROOT, TILESET), "w").write("\n".join(head + ext + [""] + subs + res) + "\n")


def build(names=None):
    meta_path = os.path.join(os.path.dirname(__file__), "nature_tiles.json")
    meta = json.load(open(meta_path)) if os.path.exists(meta_path) else {}
    entries = []
    for name, (fn, size) in looks().items():
        if not names or name in names or name not in meta:
            sheet, region, anchor_y = render_look(fn, size)
            gr.save_png(sheet, f"{OUT}/{name}.png")
            meta[name] = [region[0], region[1], anchor_y]
            print(name, region)
        w, h, anchor_y = meta[name]
        entries.append((name, gr.uid_for(gr.res_path(f"{OUT}/{name}.png")), (w, h), anchor_y, size))
    open(meta_path, "w").write("{\n" + ",\n".join(f' "{k}": {json.dumps(v)}' for k, v in sorted(meta.items())) + "\n}\n")
    write_tileset(entries)
    return TILESET_UID
