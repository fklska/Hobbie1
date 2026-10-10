import numpy as np
from sdf import hexc
import godot_res as gr

OUT = "Art/effects"
FIRE = [hexc(c) for c in ("#3a0c04", "#9a2410", "#e0521a", "#ff9a2e", "#ffd54a", "#fff6c4")]
SMOKE = [hexc(c) for c in ("#1c1716", "#2f2826", "#4a403c", "#6b605a")]
ROCK = [hexc(c) for c in ("#181210", "#3a2c26", "#5c4840", "#7e685c")]
DUST = [hexc(c) for c in ("#2e2116", "#5a4330", "#8c7052", "#c0a47e", "#e6d4b0")]
STEEL = [hexc(c) for c in ("#26405a", "#5e8ab0", "#a6cbe8", "#eef8ff")]
LEAF = [hexc(c) for c in ("#123a1c", "#2f7244", "#6ebd7a", "#b6f0a0", "#efffe0")]
STAR = [hexc(c) for c in ("#5a3a08", "#d29a33", "#ffe27a", "#fffbe0")]
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 + 1 / 32


def grid(w, h):
    yy, xx = np.mgrid[0:h, 0:w] + 0.5
    return xx, yy


def waves(xx, yy, px, py, seed, n=6, lo=1, hi=4):
    rng = np.random.RandomState(seed)
    out = np.zeros_like(xx)
    tot = 0
    for _ in range(n):
        kx, ky = rng.randint(-hi, hi + 1), rng.randint(lo, hi + 1)
        a = 1.0 / np.hypot(kx, ky)
        out += a * np.sin(2 * np.pi * (kx * xx / px + ky * yy / py) + rng.uniform(0, 2 * np.pi))
        tot += a
    return out / tot


def dither(a, shape):
    h, w = shape
    return a > np.tile(BAYER, (h // 4 + 1, w // 4 + 1))[:h, :w]


def bands(img, heat, ramp, cuts, alpha=None):
    for k, c in enumerate(cuts):
        m = heat > c
        if alpha is not None:
            m &= alpha
        img[m] = [*ramp[min(k, len(ramp) - 1)], 255]


def fire_heat(xx, yy, cx, cy, w, h, t, seed, wob=0.35):
    u = (xx - cx) / w
    v = (cy - yy) / h
    vc = np.clip(v, 0, 1.2)
    r = 0.5 * np.clip(1 - vc, 0, 1) ** 0.75 * np.clip((v + 0.35) / 0.45, 0, 1)
    n = waves(xx, yy + t * h * 1.6, 64, h * 1.6, seed)
    sway = 0.18 * np.sin(2 * np.pi * (t + v * 0.6)) * vc
    body = 1 - np.abs(u - sway) / np.maximum(r, 1e-3)
    return body * (1.15 - 0.6 * vc) + wob * n * (0.3 + vc)


def fire_layers(img, heat):
    bands(img, heat, FIRE, [0.0, 0.18, 0.38, 0.6, 0.82, 1.02])


def flame(i, n=8, w=24, h=36):
    xx, yy = grid(w, h)
    img = np.zeros((h, w, 4))
    t = i / n
    heat = fire_heat(xx, yy, w / 2, h - 5, 7.5, 28, t, 1)
    heat = np.maximum(heat, fire_heat(xx, yy, w / 2 - 4, h - 4, 4.5, 16, (t + 0.4) % 1, 2) * 0.85)
    heat = np.maximum(heat, fire_heat(xx, yy, w / 2 + 4, h - 4, 4.5, 18, (t + 0.7) % 1, 3) * 0.85)
    fire_layers(img, heat)
    rng = np.random.RandomState(i)
    for k in range(3):
        px = int(w / 2 + rng.uniform(-7, 7))
        py = int(h - 8 - ((t * 30 + k * 11) % 26))
        if 0 <= py < h:
            img[py, px] = [*FIRE[4], 255]
    return img


def fire_burst(i, n=9, s=96):
    xx, yy = grid(s, s)
    img = np.zeros((s, s, 4))
    t = i / (n - 1)
    cx, cy = s / 2, s * 0.62
    R = 8 + 30 * (1 - (1 - t) ** 2)
    d = np.hypot((xx - cx) / 1.0, (yy - cy) / 0.55)
    if t < 0.75:
        ring = 1 - np.abs(d - R) / (6 + 4 * (1 - t))
        bands(img, ring * (1 - t) * 2.2, FIRE, [0.0, 0.5, 1.0, 1.5])
    heat = np.full_like(xx, -1.0)
    tongues = 9
    for k in range(tongues):
        a = 2 * np.pi * k / tongues + 0.3
        px, py = cx + np.cos(a) * R * 0.85, cy + np.sin(a) * R * 0.85 * 0.55
        hh = (26 - 6 * (k % 3)) * np.sin(np.pi * min(1, t * 1.3 + 0.08))
        if hh < 2:
            continue
        heat = np.maximum(heat, fire_heat(xx, yy, px, py, 7.0, hh, (t + k * 0.13) % 1, 10 + k) * (1 - 0.55 * t))
    f = max(0.0, 1 - 1.6 * t)
    core = (1 - d / (14 + 10 * t)) * 1.6 + waves(xx, yy, 48, 48, 4) * 0.2 if f > 0 else np.full_like(xx, -1.0)
    core = np.where(core > 0, core * f, core)
    heat = np.maximum(heat, fire_heat(xx, yy, cx, cy + 4, 12 + 6 * t, 40 * (1 - t) + 6, t, 20) * (1 - t) ** 0.6)
    heat = np.maximum(heat, core)
    fire_layers(img, heat)
    if t > 0.45:
        sm = 1 - np.hypot(xx - cx, (yy - (cy - 18 - 22 * t)) / 0.8) / (10 + 22 * t) + waves(xx, yy, 48, 48, 6) * 0.35
        m = (sm > 0) & (img[..., 3] == 0) & dither(np.clip((1.05 - t) * 1.7, 0, 1), sm.shape)
        img[m & (sm > 0.35)] = [*SMOKE[2], 200]
        img[m & (sm <= 0.35)] = [*SMOKE[1], 170]
    return img


def comet(i, n, w, h, hr, rock=True, seed=0):
    xx, yy = grid(w, h)
    img = np.zeros((h, w, 4))
    t = i / n
    hx, hy = w / 2, h - hr - 4
    v = (hy - yy) / (hy - 2)
    u = (xx - hx) / (hr * 1.35)
    width = np.clip(1 - v, 0, 1) ** 1.1 * (1 + 0.25 * np.clip(v * 4, 0, 1))
    nz = waves(xx, yy + t * h, 64, h, seed + 1, n=8, hi=6)
    nz2 = waves(xx * 1.7, yy + t * h * 2, 64, h, seed + 2, n=6, hi=8)
    tail = (1 - np.abs(u + 0.25 * nz2 * v) / np.maximum(width, 1e-3)) * (1.2 - 0.9 * np.clip(v, 0, 1)) + 0.45 * nz * np.clip(v, 0, 1)
    tail = np.where(v > -0.05, tail, -1)
    d = np.hypot(xx - hx, yy - hy) / (hr * 1.5)
    heat = np.maximum(tail, (1 - d) * 1.8)
    fire_layers(img, heat)
    rng = np.random.RandomState(seed + i)
    for k in range(int(w * h / 900)):
        px = int(np.clip(hx + rng.normal(0, hr * 0.9), 0, w - 1))
        py = int(hy - ((t * h * 1.3 + k * 37) % (h * 0.9)))
        if 0 <= py < h:
            img[py, px] = [*FIRE[3 + k % 3], 255]
    if rock:
        rd = np.hypot(xx - hx, (yy - hy) * 1.05) / hr
        rn = waves(xx, yy, 64, 64, seed + 5, hi=3) * 0.12
        body = rd + rn < 1
        lx, ly = (xx - hx) / hr, (yy - hy) / hr
        shade = np.clip(0.5 - 0.4 * lx - 0.35 * ly + rn * 2, 0, 0.999)
        k = (shade * len(ROCK)).astype(int)
        for j in range(len(ROCK)):
            img[body & (k == j)] = [*ROCK[j], 255]
        crack = body & (np.abs(waves(xx, yy, 48, 48, seed + 9, hi=3)) < 0.06) & (rd < 0.85)
        img[crack] = [*FIRE[3], 255]
        img[crack & (ly < 0)] = [*FIRE[4], 255]
        rim = body & (rd + rn > 0.82) & (ly < 0.2)
        img[rim] = [*FIRE[3], 255]
        edge = (rd + rn >= 1) & (rd + rn < 1.12)
        img[edge & (img[..., 3] == 0)] = [*FIRE[0], 255]
    return img


def meteor(i, n=6):
    return comet(i, n, 80, 184, 21, True, 30)


def fireball(i, n=6):
    return comet(i, n, 28, 56, 6, False, 50)


def explosion(i, n=12, s=256):
    xx, yy = grid(s, s)
    img = np.zeros((s, s, 4))
    t = i / (n - 1)
    cx, cy = s / 2, s * 0.66
    e = 1 - (1 - min(1, t * 1.8)) ** 3
    if t < 0.6:
        R = 20 + 110 * e
        d = np.hypot(xx - cx, (yy - cy) / 0.5)
        ring = 1 - np.abs(d - R) / (5 + 10 * (1 - t))
        m = ring > 0
        img[m] = [*DUST[3], 255]
        img[m & (ring > 0.5)] = [*DUST[4], 255]
        if t > 0.3:
            img[m & ~dither(np.full(m.shape, (0.6 - t) / 0.3), m.shape)] = 0
    rise = 60 * t
    R = 18 + 62 * e
    blob = 1 - np.hypot(xx - cx, (yy - (cy - 18 - rise)) / 0.86) / R
    blob += waves(xx, yy + rise, 128, 128, 70, n=10, hi=7) * 0.38
    stem = 1 - np.hypot((xx - cx) / (0.45 + 0.2 * t), (yy - (cy - rise * 0.4)) / 1.2) / (R * 0.7)
    stem += waves(xx, yy, 128, 128, 71, n=8, hi=6) * 0.3
    dens = np.maximum(blob, stem - 2 * (1 - min(1, t * 3)))
    temp = dens * 2.0 * (1 - t) ** 1.4
    if i == 0:
        temp = np.where(dens > -0.3, temp + 0.6, temp)
    smoke = (dens > 0) & (temp < 0.18)
    if t > 0.2:
        fade = np.clip((1.12 - t) * 1.5, 0, 1)
        m = smoke & dither(np.full(dens.shape, fade), dens.shape)
        ly = (yy - (cy - 18 - rise)) / R
        sh = np.clip(0.55 - 0.6 * ly + dens * 0.6, 0, 0.999)
        k = (sh * len(SMOKE)).astype(int)
        for j in range(len(SMOKE)):
            img[m & (k == j)] = [*SMOKE[j], 235]
    heat = np.where(smoke, -1, temp)
    fire_layers(img, heat)
    if i <= 1:
        f = 1 - np.hypot(xx - cx, (yy - cy + 10) / 0.8) / (40 + 30 * i)
        img[f > 0.3] = [*FIRE[5], 255]
        img[(f > 0) & (f <= 0.3)] = [*FIRE[4], 255]
    rng = np.random.RandomState(80)
    for k in range(26):
        a = rng.uniform(-np.pi * 0.95, -np.pi * 0.05)
        sp = rng.uniform(60, 120)
        px = cx + np.cos(a) * sp * e * 1.1
        py = cy - 10 + np.sin(a) * sp * e * 0.9 + 140 * t * t
        if t < 0.85 and 1 <= px < s - 2 and 1 <= py < s - 2:
            c = ROCK[1 + k % 2] if k % 3 else FIRE[3 + k % 2]
            img[int(py) - 1:int(py) + 2, int(px) - 1:int(px) + 2] = [*FIRE[0], 255]
            img[int(py), int(px) - 1:int(px) + 1] = [*c, 255]
    return img


def crater(w=176, h=96):
    xx, yy = grid(w, h)
    img = np.zeros((h, w, 4))
    cx, cy = w / 2, h / 2
    d = np.hypot((xx - cx) / (w / 2 - 4), (yy - cy) / (h / 2 - 4))
    nz = waves(xx, yy, w, h, 90, n=10, hi=8) * 0.12
    ang = np.arctan2((yy - cy) * 2, xx - cx)
    spikes = 0.1 * np.maximum(0, np.sin(ang * 9 + 1.3)) ** 3
    r = d + nz - spikes
    outer = (r < 1) & dither(np.clip((1 - r) * 3.2, 0, 1), r.shape)
    img[outer] = [*SMOKE[0], 150]
    img[r < 0.78] = [*SMOKE[0], 215]
    img[(r < 0.62) & (r > 0.5)] = [*SMOKE[1], 230]
    img[r < 0.5] = [*ROCK[1], 235]
    img[(r < 0.5) & ((yy - cy) / h < -0.02)] = [*ROCK[0], 240]
    img[(r < 0.5) & (r > 0.43) & (yy > cy)] = [*ROCK[2], 240]
    crack = (r < 0.75) & (np.abs(waves(xx, yy, w, h, 91, hi=6)) < 0.05)
    img[crack] = [*FIRE[1], 255]
    img[crack & (r < 0.4)] = [*FIRE[2], 255]
    return img


def shockwave(i, n=8, w=224, h=128):
    xx, yy = grid(w, h)
    img = np.zeros((h, w, 4))
    t = i / (n - 1)
    cx, cy = w / 2, h * 0.55
    R = 24 + 80 * (1 - (1 - t) ** 2)
    d = np.hypot(xx - cx, (yy - cy) / 0.5)
    nz = waves(xx, yy, w, h, 100, n=8, hi=7) * 6
    ring = 1 - np.abs(d + nz - R) / (4 + 10 * (1 - t))
    fade = 1 - t
    m = (ring > 0) & dither(np.full(ring.shape, min(1, fade * 1.6)), ring.shape)
    up = yy < cy
    img[m] = [*DUST[1], 230]
    img[m & (ring > 0.35)] = [*DUST[2], 240]
    img[m & (ring > 0.7) & up] = [*DUST[3], 255]
    img[m & (ring > 0.7) & ~up] = [*DUST[2], 255]
    rng = np.random.RandomState(101)
    for k in range(18):
        a = 2 * np.pi * k / 18 + rng.uniform(-0.15, 0.15)
        rr = R * rng.uniform(0.8, 1.05)
        px = cx + np.cos(a) * rr
        py = cy + np.sin(a) * rr * 0.5 - 22 * np.sin(np.pi * t) * rng.uniform(0.4, 1)
        if t < 0.9 and 1 <= px < w - 2 and 1 <= py < h - 2:
            img[int(py) - 1:int(py) + 1, int(px) - 1:int(px) + 1] = [*DUST[k % 3], 255]
    if i < 2:
        f = 1 - np.hypot(xx - cx, (yy - cy) / 0.5) / (30 + 14 * i)
        mm = (f > 0) & dither(np.clip(f * 2, 0, 1), f.shape)
        img[mm & (img[..., 3] == 0)] = [*DUST[4], 200]
    return img


def whirl(i, n=6, w=192, h=112):
    xx, yy = grid(w, h)
    img = np.zeros((h, w, 4))
    cx, cy = w / 2, h * 0.58
    ang = np.arctan2((yy - cy) / 0.5, xx - cx)
    d = np.hypot(xx - cx, (yy - cy) / 0.5)
    for k, (R, wd) in enumerate(((78, 7), (58, 5))):
        head = -2 * np.pi * i / n + k * np.pi
        lag = (head - ang) % (2 * np.pi)
        arc = (lag < 2.4) & (np.abs(d - R) < wd * (1 - lag / 2.6))
        s = 1 - lag / 2.4
        img[arc & (s <= 0.35)] = [*STEEL[0], 170]
        img[arc & (s > 0.35)] = [*STEEL[1], 220]
        img[arc & (s > 0.65)] = [*STEEL[2], 255]
        img[arc & (s > 0.85) & (np.abs(d - R) < wd * 0.4)] = [*STEEL[3], 255]
    return img


def stun(i, n=8, w=40, h=20):
    xx, yy = grid(w, h)
    img = np.zeros((h, w, 4))
    for k in range(3):
        a = 2 * np.pi * (i / n + k / 3)
        px, py = w / 2 + np.cos(a) * 14, h / 2 + np.sin(a) * 5
        sz = 2.6 + 0.8 * np.sin(a)
        r = np.hypot(xx - px, yy - py)
        th = np.arctan2(yy - py, xx - px)
        star = r < sz * (0.55 + 0.45 * np.abs(np.cos(th * 2.5)))
        img[(r < sz + 1.2) & ~star & (r < sz * (0.55 + 0.45 * np.abs(np.cos(th * 2.5))) + 1.2)] = [*STAR[0], 255]
        img[star] = [*STAR[1], 255]
        img[star & (r < sz * 0.55)] = [*STAR[2], 255]
        img[star & (r < 0.9)] = [*STAR[3], 255]
    return img


def summon(i, n=10, s=112):
    xx, yy = grid(s, s)
    img = np.zeros((s, s, 4))
    t = i / (n - 1)
    cx, cy = s / 2, s * 0.68
    R = 10 + 38 * (1 - (1 - t) ** 2)
    d = np.hypot(xx - cx, (yy - cy) / 0.5)
    if t < 0.8:
        ring = 1 - np.abs(d - R) / (3 + 4 * (1 - t))
        m = (ring > 0) & dither(np.full(ring.shape, 1.2 - t * 1.3), ring.shape)
        img[m] = [*LEAF[2], 220]
        img[m & (ring > 0.6)] = [*LEAF[4], 255]
    if t < 0.5:
        hgt = np.clip((cy - yy) / 70, 0, 1)
        col = 1 - np.abs(xx - cx) / ((14 * (1 - t * 1.4) + 1) * (1 - 0.7 * hgt))
        m = (col > 0) & (yy < cy + 2) & (yy > cy - 70 * min(1, t * 4)) & dither(np.clip(col * 1.5, 0, 1) * (1 - t * 1.8) * (1 - hgt), col.shape)
        img[m] = [*LEAF[3], 200]
        img[m & (col > 0.6)] = [*LEAF[4], 255]
    rng = np.random.RandomState(120)
    for k in range(14):
        a0 = 2 * np.pi * k / 14
        a = a0 + t * 3.2
        rr = 8 + (R + 6) * rng.uniform(0.6, 1.1)
        px = cx + np.cos(a) * rr
        py = cy + np.sin(a) * rr * 0.5 - 50 * t * rng.uniform(0.4, 1.0)
        if t > 0.92 and k % 2:
            continue
        c = LEAF[1 + k % 3]
        L = 3.2 + (k % 3)
        ca, sa = np.cos(a * 1.7 + k), np.sin(a * 1.7 + k)
        u = ((xx - px) * ca + (yy - py) * sa) / L
        v = (-(xx - px) * sa + (yy - py) * ca) / (L * 0.45)
        m = u * u + v * v < 1
        o = (u * u / 1.5 + v * v / 2.6 < 1) & ~m & (img[..., 3] == 0)
        img[o] = [*LEAF[0], 255]
        img[m] = [*c, 255]
        img[m & (v < -0.3)] = [*LEAF[min(4, 2 + k % 3)], 255]
    return img


ANIMS = {
    "flame": (flame, 8, 12.0, True),
    "fire_burst": (fire_burst, 9, 16.0, False),
    "meteor": (meteor, 6, 14.0, True),
    "fireball": (fireball, 6, 14.0, True),
    "explosion": (explosion, 12, 14.0, False),
    "shockwave": (shockwave, 8, 18.0, False),
    "whirl": (whirl, 6, 18.0, True),
    "stun": (stun, 8, 10.0, True),
    "summon": (summon, 10, 14.0, False),
}


def build(names=None):
    entries = []
    for name, (fn, n, fps, loop) in ANIMS.items():
        srel = f"{OUT}/{name}.png"
        if names and name not in names:
            from PIL import Image
            import os
            sheet = Image.open(os.path.join(gr.ROOT, srel))
            frames = None
            fw, fh = sheet.size[0] // n, sheet.size[1]
            suid = gr.uid_for(gr.res_path(srel))
        else:
            frames = [fn(i, n) for i in range(n)]
            fh, fw = frames[0].shape[:2]
            suid = gr.save_png(gr.sheet_from([frames]), srel)
        entries.append(dict(name=name, sheet_rel=srel, uid=suid, rects=[(k * fw, 0, fw, fh) for k in range(n)], fps=fps, loop=loop))
    if not names or "crater" in names:
        gr.save_png(crater(), f"{OUT}/crater.png")
    return gr.write_sprite_frames(f"{OUT}/spell_frames.tres", entries)
