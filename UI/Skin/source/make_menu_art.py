import math
import os
import random
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "MainMenu", "art")
W, H = 480, 270
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def dither_pick(stops, t, x, y):
    t = max(0.0, min(1.0, t)) * (len(stops) - 1)
    i = min(int(t), len(stops) - 2)
    f = t - i
    return stops[i + 1] if f * 16 > BAYER[y % 4][x % 4] + 0.5 else stops[i]


def noise1d(seed, octaves):
    rnd = random.Random(seed)
    layers = [(rnd.random() * 1000, f, a) for f, a in octaves]

    def f(x):
        return sum(math.sin(x * fr + ph) * am for ph, fr, am in layers)
    return f


def ridge(px, base, amp, seed, color, octaves, sharp=False):
    n = noise1d(seed, octaves)
    tops = []
    for x in range(W):
        v = n(x)
        if sharp:
            v = 1.0 - abs(v) * 1.2
        top = int(base - v * amp)
        tops.append(top)
        for y in range(max(0, top), H):
            px[x, y] = color
    return tops


def pine(px, x0, base, height, color):
    for dy in range(height):
        half = int((dy / height) * height * 0.32) + (1 if dy % 3 == 2 else 0)
        y = base - height + dy
        for x in range(x0 - half, x0 + half + 1):
            if 0 <= x < W and 0 <= y < H:
                px[x, y] = color
    for y in range(base, base + 3):
        if 0 <= y < H:
            px[x0, y] = color


def house(px, x0, base, w, h, wall, roof, light):
    for x in range(x0, x0 + w):
        for y in range(base - h, base):
            px[x, y] = wall
    for i in range(w // 2 + 2):
        for x in range(x0 - 1 + i, x0 + w + 1 - i):
            px[x, base - h - i] = roof
    px[x0 + 2, base - h + 2] = light
    px[x0 + 3, base - h + 2] = light
    px[x0 + 2, base - h + 3] = light
    px[x0 + 3, base - h + 3] = light


def glow(px, cx, cy, radius, color, strength):
    for x in range(cx - radius, cx + radius + 1):
        for y in range(cy - radius, cy + radius + 1):
            if not (0 <= x < W and 0 <= y < H):
                continue
            d = math.hypot(x - cx, y - cy) / radius
            if d >= 1:
                continue
            t = (1 - d) ** 2 * strength + (BAYER[y % 4][x % 4] - 7.5) / 16 * 0.12
            t = round(max(0.0, t) * 5) / 5
            if t > 0:
                px[x, y] = lerp(px[x, y], color, min(1.0, t))


def background():
    img = Image.new("RGB", (W, H))
    px = img.load()
    sky = [(22, 20, 46), (38, 32, 72), (78, 54, 100), (150, 82, 104), (222, 128, 96), (248, 186, 120)]
    for x in range(W):
        for y in range(H):
            px[x, y] = dither_pick(sky, (y / 170) ** 1.3, x, y)
    rnd = random.Random(3)
    for _ in range(70):
        x, y = rnd.randrange(W), rnd.randrange(80)
        px[x, y] = (255, 240, 210) if rnd.random() < 0.3 else (190, 180, 210)
    sun = (255, 214, 140)
    for x in range(W):
        for y in range(H):
            d = math.hypot(x - 330, y - 150)
            if d < 22:
                px[x, y] = sun if d < 20 or BAYER[y % 4][x % 4] > 7 else px[x, y]
    glow(px, 330, 150, 70, (255, 200, 130), 0.55)
    sky_snapshot = img.copy()
    ridge(px, 150, 26, 11, (92, 66, 112), [(0.012, 1.0), (0.031, 0.5), (0.077, 0.25)], sharp=True)
    ridge(px, 168, 14, 12, (70, 52, 94), [(0.009, 1.0), (0.023, 0.6), (0.06, 0.2)])
    tops = ridge(px, 192, 12, 21, (50, 40, 74), [(0.007, 1.0), (0.019, 0.5), (0.05, 0.2)])
    rnd = random.Random(5)
    for x in range(0, W, 5):
        if rnd.random() < 0.7:
            pine(px, x + rnd.randrange(3), tops[x] + 2, rnd.randrange(8, 15), (42, 34, 64))
    tops = ridge(px, 222, 10, 31, (30, 28, 50), [(0.006, 1.0), (0.017, 0.5), (0.045, 0.25)])
    for x in range(0, W, 7):
        if rnd.random() < 0.8 and not (250 < x < 360):
            pine(px, x + rnd.randrange(4), tops[x] + 3, rnd.randrange(14, 26), (24, 22, 40))
    hill = noise1d(41, [(0.005, 1.0), (0.013, 0.4)])
    for x in range(W):
        top = int(246 - hill(x) * 8 - max(0, 40 - abs(x - 300) * 0.35))
        for y in range(top, H):
            px[x, y] = (18, 16, 28)
    wall, roof, light = (34, 28, 40), (26, 20, 30), (255, 196, 102)
    for x0, w, h in ((262, 10, 7), (280, 14, 9), (322, 11, 8), (340, 9, 6)):
        base = 240 - int(max(0, 30 - abs(x0 + w // 2 - 300) * 0.35))
        house(px, x0, base, w, h, wall, roof, light)
    glow(px, 304, 214, 30, (255, 150, 70), 0.9)
    for y, x in ((214, 304), (213, 303), (213, 305), (212, 304), (215, 303), (215, 305)):
        px[x, y] = (255, 214, 120) if y < 214 else (255, 140, 60)
    for i in range(40):
        y = 208 - i
        x = 304 + int(math.sin(i * 0.25) * 3 + i * 0.15)
        if BAYER[y % 4][x % 4] > i * 0.3:
            px[x, y] = lerp(px[x, y], (120, 100, 120), 0.5)
    sky = sky_snapshot.load()
    out = img.convert("RGBA")
    opx = out.load()
    for x in range(W):
        for y in range(H):
            if sky[x, y] == px[x, y]:
                opx[x, y] = px[x, y] + (250,)
    out.save(os.path.join(OUT, "menu_bg.png"))


def clouds():
    cw, ch = 480, 110
    img = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    px = img.load()
    rnd = random.Random(9)
    blobs = []
    for _ in range(9):
        cx, cy = rnd.randrange(cw), rnd.randrange(20, 90)
        for _ in range(rnd.randrange(4, 8)):
            blobs.append((cx + rnd.randrange(-30, 30), cy + rnd.randrange(-6, 6), rnd.randrange(8, 18)))
    for x in range(cw):
        for y in range(ch):
            best = 0.0
            for bx, by, r in blobs:
                for off in (-cw, 0, cw):
                    d = math.hypot((x - bx - off) * 0.55, y - by) / r
                    if d < 1:
                        best = max(best, 1 - d)
            if best > 0.05:
                shade = (y % 110) / 110
                col = lerp((236, 170, 150), (120, 86, 120), min(1, shade * 1.4))
                a = 150 if best > 0.35 else (90 if BAYER[y % 4][x % 4] > 6 else 0)
                if a:
                    px[x, y] = col + (a,)
    img.save(os.path.join(OUT, "menu_clouds.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    background()
    clouds()
