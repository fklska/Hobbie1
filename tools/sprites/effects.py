import numpy as np
from sdf import hexc
import godot_res as gr

W, H = 520, 64
OX, OY = 16, 32
LENGTH = 480
DEEP, DARK, MID, LIGHT, PALE, WHITE = (hexc(c) for c in ("#0c2a48", "#145080", "#2f7fc0", "#5ec0f0", "#b8f0ff", "#ecfdff"))
ROCK = [hexc(c) for c in ("#181413", "#47403d", "#69605a", "#8c827a")]

YY, XX = np.mgrid[0:H, 0:W] + 0.5


def paint(img, mask, col, a=255):
    img[mask] = [*col, a]


def disc(img, cx, cy, r, col, a=255):
    paint(img, (XX - cx) ** 2 + (YY - cy) ** 2 <= r * r, col, a)


def diamond(img, cx, cy, a, b, fill, hi=WHITE, line=DEEP):
    m = np.abs(XX - cx) / (a + 1) + np.abs(YY - cy) / (b + 1) <= 1
    paint(img, m, line)
    paint(img, np.abs(XX - cx) / a + np.abs(YY - cy) / b <= 1, fill)
    paint(img, (np.abs(XX - cx) < 1) & (YY > cy - b) & (YY < cy - b + 1.5), hi)


def rays(img, cx, cy, n, r0, r1, phase, col, a=255):
    for k in range(n):
        ang = phase + 2 * np.pi * k / n
        for r in np.arange(r0, r1, 0.5):
            x, y = int(cx + np.cos(ang) * r), int(cy + np.sin(ang) * r)
            if 0 <= x < W and 0 <= y < H:
                img[y, x] = [*col, a]


def orb(img, r, ph):
    disc(img, OX, OY, r + 4, DARK, 150)
    disc(img, OX, OY, r + 2.5, LIGHT, 220)
    disc(img, OX, OY, r + 1, PALE)
    disc(img, OX, OY, max(r - 1.5, 1), WHITE)
    rays(img, OX, OY, 4, r + 3, r + 7, ph, WHITE, 230)


def beam(i, n=6):
    img = np.zeros((H, W, 4))
    ph = 2 * np.pi * i / n
    L = LENGTH * [0.55, 1, 1, 1, 1, 1][i]
    dx = XX - OX
    on = (dx >= 0) & (dx <= L)
    taper = np.clip(dx / 18, 0.4, 1) * np.sqrt(np.clip((L - dx) / 10, 0, 1))
    wob = 1.6 * np.sin(dx * 0.11 - ph * 2) + 1.0 * np.sin(dx * 0.23 + ph * 3)
    cy = OY + 1.2 * np.sin(dx * 0.05 - ph)
    r = np.abs(YY - cy)
    pulse = [0.85, 1.0, 1.1, 1.0, 1.05, 0.95][i]
    helix = [OY + s * 10.5 * taper * np.sin(dx * 0.085 - ph * 2 + s * 0.0) for s in (1, -1)]
    front = [np.cos(dx * 0.085 - ph * 2) * s > 0 for s in (1, -1)]
    for h, f in zip(helix, front):
        paint(img, on & (np.abs(YY - h) < 0.8) & ~f, DARK, 200)
    paint(img, on & (r < (14.5 + wob) * taper * pulse), DARK, 150)
    paint(img, on & (r < (13 + wob) * taper * pulse), MID, 130)
    paint(img, on & (r < (9 + 0.7 * wob) * taper * pulse), LIGHT, 225)
    paint(img, on & (r < (5.5 + 0.5 * wob) * taper * pulse), PALE)
    paint(img, on & (r < 2.4 * taper * pulse), WHITE)
    for h, f in zip(helix, front):
        paint(img, on & (np.abs(YY - h) < 0.8) & f, WHITE)
    rng = np.random.RandomState(3)
    for k in range(16):
        u = rng.uniform(0, 1)
        x = OX + 20 + ((u + i * 0.045) % 1.0) * (L - 30)
        side = 1 if k % 2 else -1
        y = OY + side * (11 + rng.uniform(0, 4) + 1.2 * i)
        if x < OX + L - 6:
            diamond(img, x, y, 1.6 + (k % 3) * 0.5, 2.6 + (k % 2), LIGHT)
    ex = OX + L
    disc(img, ex, OY, 11 + (i % 2), DARK, 150)
    disc(img, ex, OY, 9 + (i % 2), LIGHT, 220)
    disc(img, ex, OY, 6, PALE)
    disc(img, ex, OY, 3, WHITE)
    rays(img, ex, OY, 7, 10, 17 + 2 * (i % 2), ph * 0.5, WHITE, 230)
    for k in range(5):
        a = -np.pi / 2 + (k - 2) * 0.55 + rng.uniform(-0.2, 0.2)
        d = 13 + ((i * 3 + k * 5) % 10)
        px, py = ex + 3 + np.cos(a) * d * 0.7, OY + np.sin(a) * d * 0.85 + 4
        disc(img, px, py, 2.2, ROCK[0])
        disc(img, px, py, 1.6, ROCK[1 + k % 3])
    orb(img, 5 + (i % 2), ph)
    return img


def charge(i, n=9):
    img = np.zeros((H, W, 4))
    s = (i + 1) / n
    dx = XX - OX
    dash = ((dx - i * 6) % 14) < 7
    line = (dx > 14) & (dx < LENGTH) & (np.abs(YY - OY) < 0.6) & dash
    if s > 0.55:
        paint(img, (dx > 14) & (dx < LENGTH) & (np.abs(YY - OY) < 1.6), MID, int(50 + 60 * s))
    paint(img, line, PALE, int(70 + 120 * s))
    for k in range(10):
        ang = 2 * np.pi * k / 10 + i * 0.45
        fr = (k / 10 + i / n) % 1.0
        rad = 30 * (1 - fr) + 6
        diamond(img, OX + np.cos(ang) * rad, OY + np.sin(ang) * rad * 0.8, 1.2 + fr, 1.8 + fr, LIGHT)
    orb(img, 1.5 + 5.5 * s + (i % 2) * 0.8, i * 0.4)
    return img


def giant_beam(out_dir="Art/mobs/stone_giant"):
    rows = {"pre_laser": ([charge(i) for i in range(9)], 10.0), "laser": ([beam(i) for i in range(6)], 10.0)}
    srel = f"{out_dir}/stone_giant_beam.png"
    frames = [f for fs, _ in rows.values() for f in fs]
    suid = gr.save_png(gr.sheet_from([[f] for f in frames]), srel)
    entries, y = [], 0
    for name, (fs, fps) in rows.items():
        entries.append(dict(name=name, sheet_rel=srel, uid=suid, rects=[(0, (y + k) * H, W, H) for k in range(len(fs))], fps=fps, loop=True))
        y += len(fs)
    return gr.write_sprite_frames(f"{out_dir}/stone_giant_beam_frames.tres", entries)
