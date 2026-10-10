import numpy as np
from sdf import norm, rot_axis
from kit import Kit, t_planks, t_bricks, t_shingles, t_thatch, t_noise, t_rows, t_flame, t_rock, lean_frame

K = 0.9
CELL = 64

RY = lambda a: rot_axis([0, 1, 0], a)
RX = lambda a: rot_axis([1, 0, 0], a)
RZ = lambda a: rot_axis([0, 0, 1], a)


def logs_wall_front(k, x0, x1, z, y0, y1, r=2.3, mat="log", ext=2.0):
    y = y0 + r
    i = 0
    while y + r <= y1 + 0.5:
        k.cap([x0 - ext, y, z], [x1 + ext, y, z], r, mat, group="logs%d" % i)
        y += 2 * r - 0.3
        i += 1
    return y - r


def log_cabin(k, x0, x1, z0, z1, h, r=2.3):
    top = logs_wall_front(k, x0, x1, z1, 0, h, r)
    logs_wall_front(k, x0, x1, z0, 0, h, r, ext=0)
    y = r
    while y + r <= h + 0.5:
        k.cap([x0, y + r * 0.5, z0], [x0, y + r * 0.5, z1], r, "log", group="side")
        k.cap([x1, y + r * 0.5, z0], [x1, y + r * 0.5, z1], r, "log", group="side")
        y += 2 * r - 0.3
    k.box_mm([x0, 0, z0], [x1, top, z1], "log", group="core")
    return top


ROOF_T = 2.2


def gable_x(k, x0, x1, z0, z1, y, rh, mat, tex, over=3.0, wall=None, wall_tex=None):
    cz = (z0 + z1) / 2
    half = (z1 - z0) / 2
    k.add("prism", mat, tex, "roof", c=np.array([(x0 + x1) / 2, y - over * rh / half, cz]),
          w=half + over, h=rh + over * rh / half, l=(x1 - x0) / 2 + over, R=RY(np.pi / 2), thick=ROOF_T)
    k.add("prism", wall or "beam", wall_tex, "gable", c=np.array([(x0 + x1) / 2, y - 0.5, cz]),
          w=half, h=rh - 1.0, l=(x1 - x0) / 2, R=RY(np.pi / 2))


def gable_z(k, x0, x1, z0, z1, y, rh, mat, tex, over=3.0, wall=None, wall_tex=None):
    cx = (x0 + x1) / 2
    half = (x1 - x0) / 2
    k.add("prism", mat, tex, "roof", c=np.array([cx, y - over * rh / half, (z0 + z1) / 2]),
          w=half + over, h=rh + over * rh / half, l=(z1 - z0) / 2 + over, thick=ROOF_T)
    k.add("prism", wall or "beam", wall_tex, "gable", c=np.array([cx, y - 0.5, (z0 + z1) / 2]),
          w=half, h=rh - 1.0, l=(z1 - z0) / 2)


def hip_roof(k, x0, x1, z0, z1, y, rh, mat, tex, over=3.0):
    cx, cz = (x0 + x1) / 2, (z0 + z1) / 2
    hx, hz = (x1 - x0) / 2, (z1 - z0) / 2
    s = rh / min(hx, hz)
    yb = y - over * s
    ex, ez = hx + over, hz + over
    L = np.sqrt(1 + s * s)

    def f(p):
        qx, qy, qz = np.abs(p[:, 0] - cx), p[:, 1] - yb, np.abs(p[:, 2] - cz)
        d = np.maximum((s * qz + qy - s * ez) / L, (s * qx + qy - s * ex) / L)
        return np.maximum(d, -qy)
    k.add("custom", mat, tex, "roof", fn=f)


def door(k, x, z, w=10, h=16, mat="plank", arch=True, dark=False):
    k.box_mm([x - w / 2 - 1.5, 0, z - 1], [x + w / 2 + 1.5, h + 1.5, z + 0.8], "beam", group="doorframe")
    k.box_mm([x - w / 2, 0, z], [x + w / 2, h, z + 1.2], "glass" if dark else mat,
             tex=None if dark else t_planks(3.0, gap=0.25, seed=x), group="door")
    if arch:
        k.cyl([x, h, z + 0.2], w / 2 + 1.5, 1.0, "beam", R=RX(np.pi / 2), group="doorframe")
        k.cyl([x, h, z + 0.6], w / 2, 0.8, "glass" if dark else mat, R=RX(np.pi / 2), group="door")
    if not dark:
        k.decal([x + w / 2 - 2, h * 0.45, z + 1.3], "#f3d36b", 2.0)


def window(k, x, y, z, w=7, h=7, lit=True, shutters=None):
    k.box_mm([x - w / 2 - 1, y - h / 2 - 1, z - 1], [x + w / 2 + 1, y + h / 2 + 1, z + 0.7], "beam", group="winframe")
    k.box_mm([x - w / 2, y - h / 2, z], [x + w / 2, y + h / 2, z + 1.0], "glass_lit" if lit else "glass", group="win")
    k.box_mm([x - 0.45, y - h / 2, z + 0.5], [x + 0.45, y + h / 2, z + 1.4], "beam", group="mull")
    k.box_mm([x - w / 2, y - 0.45, z + 0.5], [x + w / 2, y + 0.45, z + 1.4], "beam", group="mull")
    if shutters:
        for sx in (-1, 1):
            k.box_mm([x + sx * (w / 2 + 1) - (w / 2 if sx < 0 else 0), y - h / 2 - 1, z + 0.5],
                     [x + sx * (w / 2 + 1) + (w / 2 if sx > 0 else 0), y + h / 2 + 1, z + 1.3], shutters,
                     tex=t_planks(2.5, gap=0.2), group="shutter")


def banner(k, x, y, z, mat="cloth_red", w=6, h=12, pole=True, emblem="#f3d36b"):
    if pole:
        k.cap([x - w / 2 - 1, y, z + 1], [x + w / 2 + 1, y, z + 1], 0.6, "beam", group="banner_pole")
    k.box_mm([x - w / 2, y - h, z], [x + w / 2, y, z + 0.8], mat, group="banner")
    k.decal([x, y - h * 0.45, z + 0.9], emblem, 2.0)
    k.decal([x, y - h * 0.45 + 1, z + 0.9], emblem, 2.0)


def flag(k, x, y, z, hpole=18, mat="cloth_red"):
    k.cap([x, y, z], [x, y + hpole, z], 0.7, "beam", group="flagpole")
    k.sphere([x, y + hpole + 0.8, z], 1.1, "gold", group="flagpole")
    k.box_mm([x + 0.6, y + hpole - 7, z - 0.4], [x + 10, y + hpole - 0.5, z + 0.4], mat, group="flag")


def crenels(k, x0, x1, z0, z1, y, mat, tex, h=4.0, w=5.0, depth=3.0, front_only=False):
    n = max(2, int(round((x1 - x0) / (w * 2))))
    step = (x1 - x0) / n
    for i in range(n):
        cx = x0 + step * (i + 0.5)
        k.box([cx, y + h / 2, z1 - depth / 2], [step / 4 + 0.5, h / 2, depth / 2], mat, tex=tex, group="cren")
        if not front_only:
            k.box([cx, y + h / 2, z0 + depth / 2], [step / 4 + 0.5, h / 2, depth / 2], mat, tex=tex, group="cren")
    m = max(2, int(round((z1 - z0) / (w * 2))))
    if not front_only:
        stz = (z1 - z0) / m
        for i in range(m):
            cz = z0 + stz * (i + 0.5)
            for cx in (x0 + depth / 2, x1 - depth / 2):
                k.box([cx, y + h / 2, cz], [depth / 2, h / 2, stz / 4 + 0.5], mat, tex=tex, group="cren")


def barrel(k, x, z, r=3.6, h=4.6, y=0.0):
    k.cyl([x, y + h, z], r, h, "plank", tex=t_planks(2.0, gap=0.2, seed=x * 3), rnd=0.8, group="barrel%d" % int(x))
    for hy in (y + 1.8, y + 2 * h - 1.8):
        k.torus([x, hy, z], r + 0.05, 0.45, "metal", group="barrel%d" % int(x))


def crate(k, x, z, s=4.5, y=0.0, rot=0.0):
    k.box([x, y + s, z], [s, s, s], "plank", R=RY(rot), rnd=0.4, tex=t_planks(3.0, vertical=False, seed=x), group="crate%d" % int(x * 10 + z))


def sack(k, x, z, mat="cloth_white", r=3.2):
    k.ell([x, r * 0.85, z], [r, r * 0.9, r * 0.85], mat, group="sack%d" % int(x * 10 + z))
    k.ell([x, r * 1.75, z], [1.2, 0.9, 1.2], mat, group="sack%d" % int(x * 10 + z))


def log_pile(k, x, z, n=3, length=14, r=2.2):
    i = 0
    for row in range(n):
        cnt = n - row
        for j in range(cnt):
            xx = x + (j - (cnt - 1) / 2) * (2 * r)
            k.cap([xx, r + row * (2 * r - 0.6), z - length / 2], [xx, r + row * (2 * r - 0.6), z + length / 2], r, "log", group="pile%d" % i)
            i += 1


def chimney(k, x, z, y0, y1, mat="stone", w=4.5, smoke=False):
    k.box_mm([x - w, y0, z - w], [x + w, y1, z + w], mat, tex=t_bricks(4.5, 3.0, gap=0.25, seed=x), group="chimney")
    k.box_mm([x - w - 0.8, y1 - 1.5, z - w - 0.8], [x + w + 0.8, y1, z + w + 0.8], mat, group="chimney")


def fence_x(k, x0, x1, z, h=7.0, post=6.0):
    n = int((x1 - x0) / post)
    for i in range(n + 1):
        x = x0 + i * (x1 - x0) / max(n, 1)
        k.cap([x, 0, z], [x, h, z], 0.9, "plank", group="fence")
    for y in (h * 0.45, h * 0.85):
        k.cap([x0, y, z], [x1, y, z], 0.6, "plank", group="fence")


def fence_z(k, z0, z1, x, h=7.0, post=6.0):
    n = int((z1 - z0) / post)
    for i in range(n + 1):
        z = z0 + i * (z1 - z0) / max(n, 1)
        k.cap([x, 0, z], [x, h, z], 0.9, "plank", group="fence")
    for y in (h * 0.45, h * 0.85):
        k.cap([x, y, z0], [x, y, z1], 0.6, "plank", group="fence")


def ground_patch(k, x0, x1, z0, z1, mat="dirt", tex=None, h=0.6, oval=False):
    if oval:
        k.ell([(x0 + x1) / 2, -0.4, (z0 + z1) / 2], [(x1 - x0) / 2, 0.9, (z1 - z0) / 2], mat, tex=tex or t_noise(0.16, 1.5), group="ground")
    else:
        k.box_mm([x0, -h, z0], [x1, 0.3, z1], mat, rnd=0.5, tex=tex or t_noise(0.16, 1.5), group="ground")


def campfire(k, x, z, big=1.0, flame=True):
    for i in range(9):
        a = 2 * np.pi * i / 9
        k.sphere([x + np.cos(a) * 7.5 * big, 1.6, z + np.sin(a) * 6.5 * big], 2.4 * big, "stone", group="ring%d" % i)
    for a in (0.3, 1.9, 3.5, 5.0):
        d = np.array([np.cos(a), 0, np.sin(a)])
        k.cap([x, 5.0 * big, z] , [x + d[0] * 7 * big, 1.2, z + d[2] * 6 * big], 1.5 * big, "log", group="firelog%d" % int(a * 10))
    k.ell([x, 1.2, z], [4.5 * big, 1.2, 4.0 * big], "ember", group="embers")
    if flame:
        k.add("custom", "fire", t_flame(1.0, 14.0 * big), "flame", fn=_flame_sdf(x, z, big))


def _flame_sdf(x, z, big):
    c = np.array([x, 0, z])

    def f(p):
        q = p - c
        yy = q[:, 1]
        r = np.clip(4.6 * big * (1 - (yy - 2) / (13 * big)), 0, None)
        wob = 0.8 * np.sin(yy * 0.9 + q[:, 0] * 0.7)
        d = np.sqrt(q[:, 0] ** 2 + (q[:, 2] * 1.2) ** 2) - (r + wob * (r > 0))
        d = np.maximum(d, -(yy - 1.0))
        d = np.maximum(d, yy - 15 * big)
        return d
    return f


def tent(k, x, z, w=12, l=14, h=14, mat="cloth_white", along_x=False):
    if along_x:
        k.add("prism", mat, t_rows(4.0, 0, 0.18), "tent", c=np.array([x, 0, z]), w=w, h=h, l=l, R=RY(np.pi / 2))
    else:
        k.add("prism", mat, t_rows(4.0, 2, 0.18), "tent", c=np.array([x, 0, z]), w=w, h=h, l=l)
    k.cap([x, 0, z + l + 0.5], [x, h + 2, z + l + 0.5], 0.7, "beam", group="tentpole")
    k.prism([x, 0, z + l + 0.2], w * 0.35, h * 0.6, 0.4, "glass", along_x=False, group="tentdoor")


# ---------------------------------------------------------------- town hall (2x2)

def town_hall_1():
    k = Kit()
    ground_patch(k, 14, 114, 36, 124, "dirt", oval=True)
    campfire(k, 64, 88, 1.3)
    tent(k, 34, 52, w=14, l=13, h=17, mat="cloth_white")
    log_pile(k, 97, 54, 3, 18)
    k.cap([40, 2.4, 110], [58, 2.6, 114], 2.5, "log", group="seat1")
    k.cap([76, 2.4, 114], [94, 2.4, 108], 2.5, "log", group="seat2")
    k.cap([96, 2.4, 80], [101, 2.4, 96], 2.5, "log", group="seat3")
    for x in (52, 76):
        k.cap([x, 0, 88], [x, 17, 88], 0.9, "beam", group="spit%d" % x)
    k.cap([52, 16, 88], [76, 16, 88], 0.7, "beam", group="spitbar")
    k.cyl([64, 11.5, 88], 3.2, 2.6, "metal", rnd=1.0, group="pot")
    k.cap([98, 0, 36], [98, 40, 36], 1.0, "beam", group="bpole")
    banner(k, 98, 38, 36.8, "cloth_red", w=8, h=14)
    crate(k, 22, 74, 4.5)
    sack(k, 28, 82)
    return k


def town_hall_2():
    k = Kit()
    x0, x1, z0, z1 = 12, 116, 52, 108
    ground_patch(k, 6, 122, 100, 126, "dirt")
    top = log_cabin(k, x0, x1, z0, z1, 36)
    gable_x(k, x0, x1, z0, z1, top, 22, "shingle", t_shingles(6, 3.4), over=5, wall="log")
    gable_z(k, 46, 82, 76, z1 + 6, top + 4, 17, "shingle", t_shingles(6, 3.4, seed=2), over=4, wall="plank", wall_tex=t_planks(3.5))
    k.box_mm([46, 0, z1 - 4], [82, top + 4, z1 + 6], "plank", tex=t_planks(3.5), group="porchwall")
    door(k, 64, z1 + 6.4, 14, 22, arch=False)
    window(k, 64, top + 9, z1 + 6.4, 6, 6)
    for x in (24, 104):
        window(k, x, 19, z1 + 2.4, 8, 9, shutters="plank")
    banner(k, 36, 33, z1 + 3.2, "cloth_red", w=7, h=14)
    banner(k, 92, 33, z1 + 3.2, "cloth_red", w=7, h=14)
    flag(k, 64, top + 26, 80, 16)
    barrel(k, 88, z1 + 12)
    return k


def town_hall_3():
    k = Kit()
    x0, x1, z0, z1 = 10, 118, 44, 106
    ground_patch(k, 4, 124, 100, 126, "cobble", t_bricks(6, 4, 0.2, seed=4))
    st = t_bricks(9, 4.5, seed=1, jitter=3)
    k.box_mm([x0, 0, z0], [x1, 26, z1], "stone", tex=st, group="ground_floor")
    k.box_mm([x0 - 1, 25, z0 - 1], [x1 + 1, 28, z1 + 1], "beam", group="sill")
    k.box_mm([x0 + 1, 28, z0 + 1], [x1 - 1, 48, z1 - 1], "plaster", tex=t_noise(0.06, 2), group="upper")
    for x in np.linspace(x0 + 2, x1 - 2, 9):
        k.box_mm([x - 1.2, 28, z1 - 1], [x + 1.2, 48, z1 + 0.2], "beam", group="timber")
    k.box_mm([x0 + 1, 38, z1 - 1], [x1 - 1, 39.6, z1 + 0.2], "beam", group="timber")
    gable_x(k, x0, x1, z0, z1, 48, 24, "tile", t_shingles(5, 3.2), over=5, wall="plaster")
    k.box_mm([46, 0, z1 - 2], [82, 50, z1 + 6], "stone", tex=t_bricks(9, 4.5, seed=2, jitter=3), group="front_bay")
    gable_z(k, 46, 82, 70, z1 + 6, 50, 20, "tile", t_shingles(5, 3.2, seed=3), over=4, wall="plaster", wall_tex=t_noise(0.05, 2))
    door(k, 64, z1 + 6.4, 14, 21)
    k.cyl([64, 38, z1 + 6.0], 5.4, 0.9, "beam", R=RX(np.pi / 2), group="clock")
    k.cyl([64, 38, z1 + 6.8], 4.2, 0.6, "cloth_white", R=RX(np.pi / 2), group="clockface")
    for dy, dx in ((1.5, 0), (0.5, 0), (0, 1.0), (0, 2.0)):
        k.decal([64 + dx, 38 + dy, z1 + 7.6], "#14111a", 2)
    for x in (22, 36, 92, 106):
        window(k, x, 13, z1 + 0.4, 7, 10)
        window(k, x, 40, z1 - 0.6, 7, 8, shutters="cloth_green")
    chimney(k, 100, 62, 48, 78)
    banner(k, 52, 46, z1 + 6.6, "cloth_red", w=6, h=14, pole=False)
    banner(k, 76, 46, z1 + 6.6, "cloth_red", w=6, h=14, pole=False)
    flag(k, 64, 70, 84, 14)
    return k


def town_hall_4():
    k = Kit()
    st = lambda s: t_bricks(9, 5, seed=s, jitter=4)
    ground_patch(k, 6, 122, 102, 126, "cobble", t_bricks(6, 4, 0.2, seed=4))
    k.box_mm([32, 0, 30], [96, 66, 92], "stone", tex=st(1), group="keep")
    crenels(k, 32, 96, 30, 92, 66, "stone", st(2), h=5, w=5)
    hip_roof(k, 36, 92, 34, 88, 66, 22, "slate", t_shingles(4, 3), over=0)
    k.box_mm([12, 0, 70], [116, 36, 114], "stone", tex=st(3), group="curtain")
    crenels(k, 12, 116, 70, 114, 36, "stone", st(4), h=5, w=4.5)
    k.box_mm([13, 36, 71], [115, 36.8, 113], "cobble", tex=t_bricks(5, 4, 0.18, seed=8), group="walkway")
    for x in (18, 110):
        k.cyl([x, 27, 106], 13, 27, "stone", tex=st(5 + x), group="tower%d" % x)
        k.cyl([x, 55, 106], 14.0, 1.5, "stone", group="tower%d" % x)
        k.cone([x, 68, 106], 12, 15.5, 0.5, "slate", tex=t_shingles(4, 3), group="spire%d" % x)
        flag(k, x, 80, 106, 10, "cloth_red")
        window(k, x, 36, 106 + 12.6, 4, 7)
    k.box_mm([46, 0, 110], [82, 32, 118], "stone", tex=st(9), group="gatehouse")
    crenels(k, 46, 82, 110, 118, 32, "stone", st(10), h=5, w=4.5, front_only=True)
    k.box_mm([55, 0, 117.5], [73, 18, 118.6], "glass", group="gate")
    k.cyl([64, 18, 118.0], 9, 0.6, "glass", R=RX(np.pi / 2), group="gate")
    for x in np.arange(57, 72, 3.0):
        k.box_mm([x - 0.5, 4, 118.4], [x + 0.5, 26, 119.2], "metal", group="portcullis")
    for y in (8, 14, 20):
        k.box_mm([55, y - 0.5, 118.4], [73, y + 0.5, 119.2], "metal", group="portcullis")
    for x in (44, 64, 84):
        window(k, x, 54, 92.4, 5, 9)
    banner(k, 51, 30, 118.8, "cloth_red", w=6, h=14, pole=False)
    banner(k, 77, 30, 118.8, "cloth_red", w=6, h=14, pole=False)
    flag(k, 64, 88, 60, 14, "cloth_red")
    return k


# ---------------------------------------------------------------- house (1x1)

def house_1():
    k = Kit()
    ground_patch(k, 6, 58, 24, 62, "dirt", oval=True)
    k.add("prism", "thatch", t_thatch(2.6, seed=5), "hut", c=np.array([32, 0, 40]), w=21, h=28, l=15, thick=3.2)
    k.add("prism", "glass", None, "hutin", c=np.array([32, 0, 40]), w=17.5, h=23.5, l=14.6)
    for sx in (-1, 1):
        k.cap([32 - sx * 6, 34, 55.6], [32 + sx * 12, 12, 55.6], 1.3, "log", group="stick%d" % sx)
    k.ell([26, 23, 44], [5, 3, 7], "leaf", group="tuft")
    k.ell([39, 19, 50], [4, 3, 5], "leaf", group="tuft2")
    k.cap([46, 1.6, 61], [57, 1.6, 57], 1.6, "log", group="wood")
    k.cap([47, 4.4, 60], [56, 4.4, 58], 1.4, "log", group="wood2")
    return k


def house_2():
    k = Kit()
    x0, x1, z0, z1 = 10, 54, 28, 56
    ground_patch(k, 6, 58, 52, 63, "dirt")
    top = log_cabin(k, x0, x1, z0, z1, 26, r=2.1)
    gable_z(k, x0, x1, z0, z1, top, 20, "thatch", t_thatch(), over=4, wall="plank", wall_tex=t_planks(3.2))
    door(k, 32, z1 + 0.6, 9, 16, arch=False)
    window(k, 18, 15, z1 + 0.8, 6, 6, shutters="plank")
    window(k, 46, 15, z1 + 0.8, 6, 6, shutters="plank")
    window(k, 32, top + 7, z1 - 0.4, 4, 4)
    return k


def house_3():
    k = Kit()
    x0, x1, z0, z1 = 8, 56, 26, 56
    k.box_mm([x0, 0, z0], [x1, 26, z1], "stone_warm", tex=t_bricks(8, 4, seed=7, jitter=3), group="walls")
    gable_z(k, x0, x1, z0, z1, 26, 22, "tile", t_shingles(5, 3), over=4, wall="plaster", wall_tex=t_noise(0.05, 2))
    k.box_mm([31, 26, z1 - 0.6], [33, 46, z1 + 0.2], "beam", group="tb")
    k.box_mm([x0 + 3, 26, z1 - 0.6], [x1 - 3, 28, z1 + 0.2], "beam", group="tb")
    door(k, 32, z1 + 0.4, 9, 15)
    window(k, 18, 14, z1 + 0.4, 6, 8, shutters="cloth_green")
    window(k, 46, 14, z1 + 0.4, 6, 8, shutters="cloth_green")
    window(k, 26, 35, z1 + 0.2, 4, 5)
    window(k, 38, 35, z1 + 0.2, 4, 5)
    chimney(k, 46, 36, 34, 54, "brick")
    for x in (14, 50):
        k.ell([x, 2.5, 59.5], [3.5, 2.6, 2.2], "leaf", group="bush%d" % x)
    return k


# ---------------------------------------------------------------- props

def shed_roof(k, x0, x1, z0, z1, y_back, y_front, mat, tex, over=3.0, t=1.8):
    a = np.arctan2(y_back - y_front, z1 - z0)
    length = np.hypot(y_back - y_front, z1 - z0) / 2 + over
    c = np.array([(x0 + x1) / 2, (y_back + y_front) / 2 + t, (z0 + z1) / 2])
    k.box(c, [(x1 - x0) / 2 + over, t, length], mat, R=RX(a), tex=tex, group="shedroof")


def posts(k, pts, y, r=1.6, mat="log"):
    for i, (x, z) in enumerate(pts):
        k.cap([x, 0, z], [x, y, z], r, mat, group="post%d" % i)


def anvil(k, x, z, y=0.0):
    k.box_mm([x - 2.5, y, z - 2.5], [x + 2.5, y + 5, z + 2.5], "log", group="anvil_base")
    k.box_mm([x - 5, y + 5, z - 2], [x + 4, y + 8, z + 2], "metal", rnd=0.4, group="anvil")
    k.cone([x + 6, y + 7, z], 1.0, 1.4, 0.2, "metal", R=RZ(np.pi / 2), group="anvil")


def target(k, x, z, r=7.0):
    for sx in (-1, 1):
        k.cap([x + sx * 4, 0, z - 2], [x + sx * 2, 16, z + 1], 0.9, "beam", group="tstand")
    k.cyl([x, 12, z + 1.5], r, 1.2, "thatch", R=RX(np.pi / 2 - 0.25), group="tface")
    for i, (rr, col) in enumerate(((r * 0.75, "cloth_white"), (r * 0.5, "cloth_red"), (r * 0.25, "cloth_yellow"))):
        k.cyl([x, 12 + 0.3 * (i + 1), z + 2.8 + 0.25 * i], rr, 0.25, col, R=RX(np.pi / 2 - 0.25), group="tface%d" % i)


def weapon_rack(k, x, z, kinds=("spear", "sword", "spear")):
    k.cap([x - 9, 13, z], [x + 9, 13, z], 0.8, "beam", group="rack")
    k.cap([x - 9, 3, z], [x + 9, 3, z], 0.8, "beam", group="rack")
    for sx in (-9, 9):
        k.cap([x + sx, 0, z], [x + sx, 15, z], 0.9, "beam", group="rack")
    for i, kind in enumerate(kinds):
        xx = x - 6 + i * 6
        if kind == "spear":
            k.cap([xx, 1, z + 1], [xx, 22, z + 1], 0.55, "plank", group="w%d" % i)
            k.cone([xx, 23.5, z + 1], 2.0, 1.2, 0.1, "metal", group="w%d" % i)
        elif kind == "sword":
            k.cap([xx, 3, z + 1], [xx, 18, z + 1], 0.8, "steel", r2=0.3, group="w%d" % i)
            k.box_mm([xx - 2, 2.2, z + 0.6], [xx + 2, 3.2, z + 1.4], "gold", group="w%d" % i)
        elif kind == "axe":
            k.cap([xx, 2, z + 1], [xx, 17, z + 1], 0.6, "plank", group="w%d" % i)
            k.box_mm([xx, 13, z + 0.5], [xx + 4, 17, z + 1.5], "metal", group="w%d" % i)
        elif kind == "bow":
            k.add("torus", "plank", None, "w%d" % i, c=np.array([xx - 3, 10, z + 1]), RR=6.0, r=0.5, R=RX(np.pi / 2), clips=[(np.array([-1.0, 0, 0]), np.array([xx - 1.5, 10, z + 1]))])
            k.cap([xx + 1.5, 4.5, z + 1], [xx + 1.5, 15.5, z + 1], 0.2, "cloth_white", group="w%d" % i)
        elif kind == "musket":
            k.cap([xx, 2, z + 1], [xx, 22, z + 1], 0.6, "metal", group="w%d" % i)
            k.cap([xx, 2, z + 1], [xx, 9, z + 1], 1.1, "plank", r2=0.7, group="w%d" % i)


def dummy(k, x, z):
    k.cap([x, 0, z], [x, 18, z], 1.0, "beam", group="dummy")
    k.cap([x - 7, 13, z], [x + 7, 13, z], 0.9, "beam", group="dummy")
    k.ell([x, 12, z], [4.2, 5.5, 3.2], "thatch", group="dummy_b")
    k.sphere([x, 20, z], 3.2, "cloth_white", group="dummy_h")


def stall(k, x, z, w=26, d=14, cloth="cloth_red", stripe="cloth_white", goods=0):
    for sx in (-1, 1):
        for sz in (-1, 1):
            k.cap([x + sx * w / 2, 0, z + sz * d / 2], [x + sx * w / 2, 18 if sz < 0 else 15, z + sz * d / 2], 0.9, "beam", group="stallpost")
    k.box_mm([x - w / 2, 6, z - d / 2 + 1], [x + w / 2, 8, z + d / 2], "plank", tex=t_planks(3, False), group="counter")
    k.box_mm([x - w / 2 + 1, 0, z + d / 2 - 1.5], [x + w / 2 - 1, 7, z + d / 2], "plank", tex=t_planks(3), group="counterfront")
    a = np.arctan2(4.0, d)
    n = 6
    for i in range(n):
        x0 = x - w / 2 - 2 + i * (w + 4) / n
        x1 = x0 + (w + 4) / n
        k.box([(x0 + x1) / 2, 17.5, z], [(x1 - x0) / 2, 0.7, d / 2 + 2.5], cloth if i % 2 == 0 else stripe, R=RX(a), group="awning")
    for i in range(n):
        x0 = x - w / 2 - 2 + i * (w + 4) / n
        x1 = x0 + (w + 4) / n
        k.box_mm([x0, 12.5, z + d / 2 + 2.0], [x1, 15.5, z + d / 2 + 2.8], cloth if i % 2 == 0 else stripe, group="valance")
    goods_cols = [("leaf", "cloth_red", "wheat"), ("cloth_yellow", "leaf", "cloth_red"), ("wheat", "cloth_blue", "leaf")][goods % 3]
    for i, gx in enumerate(np.linspace(x - w / 2 + 5, x + w / 2 - 5, 3)):
        k.cyl([gx, 9.2, z + 1], 3.6, 1.2, "plank", group="basket%d" % i)
        for j in range(4):
            k.sphere([gx - 1.4 + (j % 2) * 2.8, 10.6 + (j // 2) * 0.8, z + 0.2 + (j // 2) * 1.6], 1.3, goods_cols[i], group="fruit%d" % i)


def sprouts(k, x0, x1, z0, z1, step_x=6.0, step_z=10.0, kind="sprout"):
    zz = z0
    i = 0
    while zz <= z1:
        k.box_mm([x0, -0.5, zz - 3.0], [x1, 2.2, zz + 3.0], "soil", rnd=1.2, tex=t_noise(0.12, 1.5, seed=zz), group="ridge%d" % i)
        xx = x0 + 3
        while xx <= x1 - 2:
            if kind == "sprout":
                k.ell([xx, 3.6, zz], [1.8, 1.4, 1.6], "leaf", group="pl%d_%d" % (i, int(xx)))
            elif kind == "cabbage":
                k.sphere([xx, 3.8, zz], 2.4, "grass", group="pl%d_%d" % (i, int(xx)))
            elif kind == "wheat":
                k.cap([xx, 1.5, zz], [xx + 0.6, 9.5, zz - 0.5], 1.2, "wheat", r2=1.6, group="pl%d_%d" % (i, int(xx)))
            xx += step_x
        zz += step_z
        i += 1


def scarecrow(k, x, z):
    k.cap([x, 0, z], [x, 20, z], 0.9, "beam", group="sc")
    k.cap([x - 7, 15, z], [x + 7, 15, z], 0.8, "beam", group="sc")
    k.ell([x, 13, z], [4, 5, 2.6], "cloth_blue", group="scb")
    k.sphere([x, 20, z], 3, "thatch", group="sch")
    k.cone([x, 23.5, z], 1.5, 4.5, 2.0, "wheat", group="schat")


def windmill(k, x, z, h=46):
    k.cone([x, h / 2, z], h / 2, 11, 7, "plaster", tex=t_noise(0.06, 2), group="mill")
    k.cone([x, h + 5, z], 5, 8.5, 0.5, "shingle", tex=t_shingles(4, 3), group="millroof")
    hub = np.array([x, h - 2, z + 9])
    k.sphere(hub, 1.8, "beam", group="hub")
    for i in range(4):
        a = np.pi / 4 + i * np.pi / 2
        d = np.array([np.cos(a), np.sin(a), 0])
        tip = hub + d * 24
        k.cap(hub, tip, 0.8, "beam", group="blade%d" % i)
        side = np.array([-d[1], d[0], 0])
        c = hub + d * 15 + side * 2.6
        R = np.stack([side, d, [0, 0, 1.0]], 1)
        k.box(c, [2.4, 9, 0.35], "cloth_white", R=R, tex=t_rows(3.0, 1, 0.2), group="sail%d" % i)
    door(k, x, z + 9.6, 6, 11, arch=True)
    window(k, x, 28, z + 8.2, 4, 5)


def smoke(k, x, y, z, n=3):
    return
    for i in range(n):
        k.sphere([x + i * 1.6, y + 3 + i * 5.5, z - i * 0.5], 2.2 + i * 0.9, "cloth_white", group="smoke%d" % i)


# ---------------------------------------------------------------- storage (2x2)

def storage_1():
    k = Kit()
    ground_patch(k, 8, 120, 34, 124, "dirt", oval=True)
    posts(k, [(18, 40), (110, 40), (18, 76), (110, 76)], 28, 1.8)
    shed_roof(k, 16, 112, 38, 78, 34, 27, "thatch", t_thatch(2.8), over=3)
    log_pile(k, 34, 98, 3, 20)
    barrel(k, 58, 92)
    barrel(k, 68, 104)
    crate(k, 86, 92, 5)
    crate(k, 98, 98, 4.5, rot=0.4)
    crate(k, 88, 94, 3.5, y=10)
    sack(k, 78, 112)
    sack(k, 86, 116, "cloth_yellow")
    sack(k, 54, 114)
    return k


def storage_2():
    k = Kit()
    x0, x1, z0, z1 = 14, 114, 36, 108
    ground_patch(k, 8, 120, 100, 126, "dirt")
    k.box_mm([x0, 0, z0], [x1, 34, z1], "plank", tex=t_planks(4.5, seed=3), group="walls")
    gable_z(k, x0, x1, z0, z1, 34, 34, "shingle", t_shingles(6, 3.4), over=5, wall="plank", wall_tex=t_planks(4.5, seed=4))
    k.box_mm([x0 - 0.5, 0, z1 - 1], [x0 + 3, 34, z1 + 1], "beam", group="corner")
    k.box_mm([x1 - 3, 0, z1 - 1], [x1 + 0.5, 34, z1 + 1], "beam", group="corner")
    for x in (52, 76):
        k.box_mm([x - 12, 0, z1], [x + 12, 26, z1 + 1.2], "plank", tex=t_planks(3.0, seed=x), group="bdoor%d" % x)
        k.box_mm([x - 12, 0, z1 + 0.8], [x + 12, 2, z1 + 1.8], "beam", group="bdoorf%d" % x)
        k.box_mm([x - 12, 24, z1 + 0.8], [x + 12, 26, z1 + 1.8], "beam", group="bdoorf%d" % x)
        k.box_mm([x - 12, 0, z1 + 0.8], [x - 10, 26, z1 + 1.8], "beam", group="bdoorf%d" % x)
        k.box_mm([x + 10, 0, z1 + 0.8], [x + 12, 26, z1 + 1.8], "beam", group="bdoorf%d" % x)
        ang = np.arctan2(24, 20)
        for s in (-1, 1):
            k.box([x, 13, z1 + 1.6], [15, 1.0, 0.5], "beam", R=RZ(s * ang), group="brace%d" % x)
    k.box_mm([58, 44, z1 - 0.5], [70, 54, z1 + 0.6], "glass", group="loft")
    k.box_mm([56, 42.5, z1 - 0.6], [72, 55.5, z1 + 0.3], "beam", group="loftf")
    k.ell([64, 45.5, z1 + 1.0], [5, 2, 1.2], "wheat", group="hay")
    k.cap([64, 58, z1 + 2], [64, 62, z1 + 8], 0.7, "beam", group="hoist")
    sack(k, 24, 116, "cloth_yellow")
    barrel(k, 104, 116)
    return k


def storage_3():
    k = Kit()
    x0, x1, z0, z1 = 10, 118, 40, 108
    ground_patch(k, 4, 124, 100, 126, "cobble", t_bricks(6, 4, 0.2, seed=6))
    k.box_mm([x0, 0, z0], [x1, 36, z1], "stone", tex=t_bricks(10, 5, seed=11, jitter=4), group="walls")
    for x in (x0 + 2, 64, x1 - 2):
        k.box_mm([x - 3, 0, z1 - 1], [x + 3, 36, z1 + 2], "stone_warm", tex=t_bricks(6, 5, seed=x), group="pil%d" % x)
    gable_x(k, x0, x1, z0, z1, 36, 26, "tile", t_shingles(5, 3.2), over=5, wall="stone")
    for x in (37, 91):
        k.box_mm([x - 13, 0, z1 - 0.5], [x + 13, 24, z1 + 0.8], "plank", tex=t_planks(3.2, seed=x), group="gate%d" % x)
        k.cyl([x, 24, z1 + 0.1], 13, 0.7, "plank", R=RX(np.pi / 2), tex=t_planks(3.2), group="gate%d" % x)
        k.box_mm([x - 13, 9, z1 + 0.6], [x + 13, 11, z1 + 1.6], "metal", group="hinge%d" % x)
        k.box_mm([x - 13, 18, z1 + 0.6], [x + 13, 20, z1 + 1.6], "metal", group="hinge%d" % x)
    crate(k, 64, 118, 4.5)
    crate(k, 72, 120, 3.5)
    barrel(k, 18, 118)
    barrel(k, 110, 118)
    return k


# ---------------------------------------------------------------- farm (2x2)

def farm_1():
    k = Kit()
    ground_patch(k, 10, 118, 20, 122, "grass", t_noise(0.1, 2))
    sprouts(k, 18, 110, 36, 112, 7, 12, "sprout")
    fence_x(k, 10, 118, 124, 7)
    fence_z(k, 22, 124, 10, 7)
    fence_z(k, 22, 124, 118, 7)
    scarecrow(k, 64, 70)
    return k


def farm_2():
    k = Kit()
    ground_patch(k, 6, 122, 16, 124, "grass", t_noise(0.1, 2))
    sprouts(k, 48, 116, 30, 112, 5, 9, "wheat")
    sprouts(k, 10, 42, 82, 114, 7, 11, "cabbage")
    x0, x1, z0, z1 = 10, 42, 26, 68
    k.box_mm([x0, 0, z0], [x1, 22, z1], "plank", tex=t_planks(3.6, seed=8), group="shed")
    gable_z(k, x0, x1, z0, z1, 22, 16, "thatch", t_thatch(), over=3, wall="plank", wall_tex=t_planks(3.6, seed=9))
    door(k, 26, z1 + 0.5, 9, 14, arch=False)
    fence_x(k, 6, 122, 126, 7)
    fence_z(k, 20, 126, 6, 7)
    fence_z(k, 20, 126, 122, 7)
    return k


def farm_3():
    k = Kit()
    ground_patch(k, 4, 124, 12, 124, "grass", t_noise(0.1, 2))
    sprouts(k, 8, 82, 70, 116, 5, 8, "wheat")
    sprouts(k, 8, 60, 28, 60, 5, 8, "wheat")
    windmill(k, 98, 52, 48)
    x0, x1, z0, z1 = 82, 122, 82, 116
    k.box_mm([x0, 0, z0], [x1, 22, z1], "plank", tex=t_planks(3.6, seed=8), group="barn")
    gable_x(k, x0, x1, z0, z1, 22, 14, "tile", t_shingles(5, 3), over=3, wall="plank")
    door(k, 102, z1 + 0.5, 12, 15, arch=False)
    sack(k, 72, 120, "cloth_yellow")
    sack(k, 66, 122)
    fence_x(k, 4, 124, 126, 7)
    return k


# ---------------------------------------------------------------- blacksmith (3x2)

def blacksmith_1():
    k = Kit()
    ground_patch(k, 10, 182, 30, 124, "dirt", oval=True)
    posts(k, [(40, 36), (152, 36), (40, 70), (152, 70)], 28)
    shed_roof(k, 38, 154, 34, 72, 34, 28, "thatch", t_thatch(2.8), over=3)
    log_pile(k, 70, 52, 3, 16)
    k.cap([110, 3, 50], [140, 3, 54], 2.2, "log", group="lone")
    k.box_mm([64, 0, 80], [94, 12, 102], "stone", tex=t_bricks(6, 4, seed=21, jitter=2), group="hearth")
    k.ell([79, 12.5, 91], [11, 1.5, 8], "ember", group="coals")
    k.add("custom", "fire", t_flame(12, 22), "flame", fn=_flame_sdf_y(79, 91, 12, 0.6))
    k.ell([104, 8, 92], [7, 3.5, 5], "leather", group="bellows")
    k.cap([110, 8, 92], [116, 11, 92], 0.8, "beam", group="bellows_h")
    anvil(k, 124, 110)
    barrel(k, 146, 104)
    k.cap([32, 2, 108], [32, 14, 108], 4.5, "stone", group="grind")
    k.cyl([32, 10, 108], 6, 1.5, "stone", R=RZ(np.pi / 2), group="grindwheel")
    return k


def _flame_sdf_y(x, z, y0, big):
    c = np.array([x, y0, z])

    def f(p):
        q = p - c
        yy = q[:, 1]
        r = np.clip(5.5 * big * (1 - yy / (13 * big)), 0, None)
        wob = 0.9 * np.sin(yy * 0.9 + q[:, 0] * 0.7)
        d = np.sqrt(q[:, 0] ** 2 + (q[:, 2] * 1.2) ** 2) - (r + wob * (r > 0))
        d = np.maximum(d, -yy)
        d = np.maximum(d, yy - 13 * big)
        return d
    return f


def blacksmith_2():
    k = Kit()
    ground_patch(k, 6, 186, 96, 126, "dirt")
    x0, x1, z0, z1 = 10, 92, 36, 104
    top = log_cabin(k, x0, x1, z0, z1, 30)
    gable_x(k, x0, x1, z0, z1, top, 22, "shingle", t_shingles(6, 3.4), over=4, wall="log")
    door(k, 32, z1 + 2.4, 11, 18, arch=False)
    window(k, 66, 17, z1 + 2.4, 9, 8, shutters="plank")
    posts(k, [(100, 40), (180, 40), (100, 74), (180, 74)], 30)
    shed_roof(k, 98, 182, 38, 76, 38, 32, "shingle", t_shingles(6, 3.4, seed=4), over=3)
    k.box_mm([130, 0, 80], [170, 14, 100], "stone", tex=t_bricks(6, 4, seed=22, jitter=2), group="hearth")
    chimney(k, 150, 54, 0, 62, "stone", 6)
    k.ell([150, 14.5, 90], [14, 1.5, 7], "ember", group="coals")
    k.add("custom", "fire", t_flame(14, 24), "flame", fn=_flame_sdf_y(150, 90, 14, 0.6))
    anvil(k, 120, 110)
    barrel(k, 176, 112)
    weapon_rack(k, 112, 58, ("axe", "sword", "spear"))
    return k


def blacksmith_3():
    k = Kit()
    st = lambda s: t_bricks(9, 5, seed=s, jitter=4)
    ground_patch(k, 4, 188, 100, 126, "cobble", t_bricks(6, 4, 0.2, seed=6))
    x0, x1, z0, z1 = 8, 184, 36, 104
    k.box_mm([x0, 0, z0], [x1, 36, z1], "stone", tex=st(31), group="walls")
    gable_x(k, x0, x1, z0, z1, 36, 24, "tile", t_shingles(5, 3.2), over=5, wall="stone")
    for x in (64, 128):
        k.box_mm([x - 14, 0, z1 - 1], [x + 14, 22, z1 + 0.6], "glass", group="arch%d" % x)
        k.cyl([x, 22, z1 - 0.2], 14, 0.8, "glass", R=RX(np.pi / 2), group="arch%d" % x)
    k.ell([64, 6, z1 - 2], [10, 5, 3], "ember", group="glow1")
    k.add("custom", "fire", t_flame(4, 16), "flame", fn=_flame_sdf_y(64, z1 - 4, 4, 0.7))
    anvil(k, 128, z1 + 4)
    door(k, 168, z1 + 0.4, 10, 18)
    window(k, 26, 18, z1 + 0.4, 8, 10)
    chimney(k, 40, 58, 36, 84, "brick", 7)
    smoke(k, 40, 84, 58)
    weapon_rack(k, 96, z1 + 8, ("sword", "axe", "spear"))
    barrel(k, 150, z1 + 10)
    banner(k, 96, 33, z1 + 0.8, "cloth_red", w=8, h=12, pole=False)
    return k


# ---------------------------------------------------------------- workshop (2x2)

def workshop_1():
    k = Kit()
    ground_patch(k, 6, 122, 30, 124, "dirt", oval=True)
    x0, x1, z0, z1 = 12, 74, 36, 84
    k.box_mm([x0, 0, z0], [x1, 24, z1], "plank", tex=t_planks(3.8, seed=41), group="shed")
    gable_x(k, x0, x1, z0, z1, 24, 18, "thatch", t_thatch(), over=4, wall="plank")
    door(k, 30, z1 + 0.5, 9, 15, arch=False)
    window(k, 56, 13, z1 + 0.6, 8, 6, shutters="plank")
    target(k, 100, 70)
    weapon_rack(k, 60, 104, ("bow", "bow", "spear"))
    k.cyl([94, 4.5, 106], 4, 4.5, "plank", tex=t_planks(2.0), group="quiver")
    for i in range(4):
        k.cap([92.5 + i, 8, 106], [92 + i * 1.2, 15, 106 - 0.5], 0.4, "plank", group="arrow%d" % i)
    return k


def workshop_2():
    k = Kit()
    ground_patch(k, 6, 122, 92, 126, "dirt")
    x0, x1, z0, z1 = 10, 84, 34, 96
    k.box_mm([x0, 0, z0], [x1, 30, z1], "plaster", tex=t_noise(0.05, 2), group="walls")
    for x in np.linspace(x0 + 1.5, x1 - 1.5, 7):
        k.box_mm([x - 1.2, 0, z1 - 0.8], [x + 1.2, 30, z1 + 0.4], "beam", group="tb")
    for y in (1.0, 15.0, 29.0):
        k.box_mm([x0, y - 1, z1 - 0.8], [x1, y + 1, z1 + 0.4], "beam", group="tb")
    gable_x(k, x0, x1, z0, z1, 30, 20, "tile", t_shingles(5, 3), over=4, wall="plaster")
    door(k, 26, z1 + 0.6, 9, 14, arch=False)
    window(k, 60, 22, z1 + 0.6, 10, 7, shutters="cloth_blue")
    k.box_mm([46, 9, 104], [78, 11, 116], "plank", tex=t_planks(3.0, False), group="bench")
    for x, z in ((48, 106), (76, 106), (48, 114), (76, 114)):
        k.cap([x, 0, z], [x, 9, z], 0.9, "beam", group="benchleg")
    k.cap([52, 12.5, 110], [64, 12.5, 110], 0.8, "plank", group="xbow")
    k.cap([58, 12.5, 105], [58, 12.5, 115], 0.6, "beam", group="xbow")
    target(k, 108, 64)
    weapon_rack(k, 104, 104, ("bow", "spear", "bow"))
    return k


def workshop_3():
    k = Kit()
    ground_patch(k, 4, 124, 96, 126, "cobble", t_bricks(6, 4, 0.2, seed=8))
    x0, x1, z0, z1 = 10, 118, 38, 102
    k.box_mm([x0, 0, z0], [x1, 32, z1], "brick", tex=t_bricks(7, 3.5, seed=51), group="walls")
    gable_x(k, x0, x1, z0, z1, 32, 22, "slate", t_shingles(4.5, 3), over=4, wall="brick")
    door(k, 64, z1 + 0.5, 12, 18)
    for x in (30, 98):
        window(k, x, 18, z1 + 0.5, 9, 11)
    k.box_mm([44, 24, z1 - 0.2], [84, 30, z1 + 0.9], "plank", group="sign")
    k.cap([52, 27, z1 + 1.2], [76, 27, z1 + 1.2], 0.7, "metal", group="signgun")
    k.cap([52, 27, z1 + 1.2], [58, 27, z1 + 1.2], 1.2, "plank", r2=0.8, group="signgun")
    chimney(k, 98, 58, 32, 74, "brick", 5.5)
    smoke(k, 98, 74, 58, 2)
    barrel(k, 18, z1 + 14)
    barrel(k, 28, z1 + 16)
    weapon_rack(k, 104, z1 + 12, ("musket", "musket", "bow"))
    return k


# ---------------------------------------------------------------- market (2x2)

def market_1():
    k = Kit()
    ground_patch(k, 6, 122, 30, 124, "dirt", oval=True)
    stall(k, 40, 64, 30, 14, "cloth_yellow", "cloth_white", 0)
    stall(k, 90, 88, 30, 14, "cloth_green", "cloth_white", 1)
    crate(k, 24, 104, 4.5)
    sack(k, 34, 108, "cloth_yellow")
    barrel(k, 110, 60)
    return k


def market_2():
    k = Kit()
    ground_patch(k, 4, 124, 22, 126, "cobble", t_bricks(6, 4, 0.2, seed=9))
    stall(k, 32, 48, 34, 14, "cloth_red", "cloth_white", 0)
    stall(k, 96, 48, 34, 14, "cloth_blue", "cloth_white", 1)
    stall(k, 32, 98, 34, 14, "cloth_green", "cloth_yellow", 2)
    stall(k, 96, 98, 34, 14, "cloth_yellow", "cloth_red", 0)
    barrel(k, 64, 72)
    crate(k, 64, 118, 4)
    return k


def market_3():
    k = Kit()
    st = lambda s: t_bricks(9, 5, seed=s, jitter=4)
    ground_patch(k, 4, 124, 96, 126, "cobble", t_bricks(6, 4, 0.2, seed=9))
    x0, x1, z0, z1 = 10, 118, 34, 100
    k.box_mm([x0, 0, z0], [x1, 48, z1 - 12], "stone_warm", tex=st(61), group="hall")
    k.box_mm([x0, 24, z1 - 12], [x1, 48, z1], "stone_warm", tex=st(62), group="upper")
    for x in np.linspace(x0 + 4, x1 - 4, 6):
        k.box_mm([x - 3.5, 0, z1 - 4], [x + 3.5, 24, z1], "stone_warm", tex=st(63 + int(x)), group="col%d" % int(x))
    for xa, xb in zip(np.linspace(x0 + 4, x1 - 4, 6)[:-1], np.linspace(x0 + 4, x1 - 4, 6)[1:]):
        c = (xa + xb) / 2
        k.cyl([c, 17, z1 - 0.5], (xb - xa) / 2 - 3.5, 0.7, "glass", R=RX(np.pi / 2), group="archtop%d" % int(c))
    k.box_mm([x0 + 1, 0, z1 - 12], [x1 - 1, 17, z1 - 11], "glass", group="arcade_back")
    k.box_mm([x0 - 1, 23, z1 - 1], [x1 + 1, 25.5, z1 + 1], "stone", group="cornice")
    gable_x(k, x0, x1, z0, z1, 48, 22, "tile", t_shingles(5, 3.2), over=5, wall="stone_warm")
    for x in (24, 44, 84, 104):
        window(k, x, 37, z1 + 0.4, 7, 10)
    k.cyl([64, 38, z1 + 0.4], 6, 0.8, "gold", R=RX(np.pi / 2), group="emblem")
    k.decal([64, 38, z1 + 1.4], "#91591f", 2)
    banner(k, 52, 46, z1 + 1.5, "cloth_blue", w=6, h=14, pole=False)
    banner(k, 76, 46, z1 + 1.5, "cloth_blue", w=6, h=14, pole=False)
    crate(k, 30, 112, 4)
    barrel(k, 100, 112)
    return k


# ---------------------------------------------------------------- barracks (2x2)

def barracks_1():
    k = Kit()
    ground_patch(k, 8, 120, 40, 124, "dirt", oval=True)
    x0, x1, z0, z1 = 30, 86, 42, 90
    top = log_cabin(k, x0, x1, z0, z1, 26)
    gable_z(k, x0, x1, z0, z1, top, 18, "thatch", t_thatch(), over=4, wall="plank", wall_tex=t_planks(3.4))
    door(k, 58, z1 + 2.4, 10, 17, arch=False)
    window(k, 42, 15, z1 + 2.4, 5, 5)
    weapon_rack(k, 100, 104, ("spear", "spear", "spear"))
    k.cap([22, 0, 104], [22, 22, 104], 1.0, "beam", group="torch")
    k.add("custom", "fire", t_flame(22, 28), "torchfire", fn=_flame_sdf_y(22, 104, 22, 0.35))
    banner(k, 58, top + 15, z1 + 3, "cloth_red", w=6, h=11, pole=False)
    return k


def barracks_2():
    k = Kit()
    ground_patch(k, 4, 124, 84, 126, "dirt")
    x0, x1, z0, z1 = 8, 120, 34, 88
    top = log_cabin(k, x0, x1, z0, z1, 30)
    gable_x(k, x0, x1, z0, z1, top, 22, "shingle", t_shingles(6, 3.4), over=4, wall="log")
    door(k, 64, z1 + 2.4, 12, 19, arch=False)
    for x in (22, 42, 86, 106):
        window(k, x, 18, z1 + 2.4, 6, 7, shutters="plank")
    banner(k, 52, 30, z1 + 3.2, "cloth_red", w=6, h=13)
    banner(k, 76, 30, z1 + 3.2, "cloth_red", w=6, h=13)
    dummy(k, 26, 110)
    dummy(k, 46, 112)
    weapon_rack(k, 98, 110, ("sword", "spear", "axe"))
    return k


def barracks_3():
    k = Kit()
    st = lambda s: t_bricks(9, 5, seed=s, jitter=4)
    ground_patch(k, 4, 124, 96, 126, "cobble", t_bricks(6, 4, 0.2, seed=12))
    x0, x1, z0, z1 = 8, 120, 32, 100
    k.box_mm([x0, 0, z0], [x1, 44, z1], "stone", tex=st(71), group="walls")
    crenels(k, x0, x1, z0, z1, 44, "stone", st(72), h=5, w=5)
    k.box_mm([x0 + 1, 44, z0 + 1], [x1 - 1, 44.8, z1 - 1], "cobble", tex=t_bricks(5, 4, 0.18, seed=7), group="roofdeck")
    hip_roof(k, x0 + 8, x1 - 8, z0 + 7, z1 - 7, 44, 18, "slate", t_shingles(4.5, 3), over=0)
    for x in (x0 + 6, x1 - 6):
        k.box_mm([x - 8, 0, z1 - 6], [x + 8, 52, z1 + 2], "stone", tex=st(73 + x), group="but%d" % x)
        crenels(k, x - 8, x + 8, z1 - 6, z1 + 2, 52, "stone", st(74), h=4, w=3.5, front_only=True)
        window(k, x, 32, z1 + 2.4, 3, 8)
    k.box_mm([52, 0, z1 - 0.5], [76, 22, z1 + 0.6], "plank", tex=t_planks(3.2), group="gate")
    k.cyl([64, 22, z1], 12, 0.6, "plank", R=RX(np.pi / 2), tex=t_planks(3.2), group="gate")
    for x in (36, 92):
        window(k, x, 26, z1 + 0.4, 3, 9)
    banner(k, 44, 40, z1 + 0.8, "cloth_red", w=7, h=16, pole=False)
    banner(k, 84, 40, z1 + 0.8, "cloth_red", w=7, h=16, pole=False)
    flag(k, 64, 62, 66, 16, "cloth_red")
    weapon_rack(k, 30, 114, ("sword", "musket", "spear"))
    return k


def wall_1():
    k = Kit()
    n = 10
    for i in range(n):
        x = (i + 0.5) * 64 / n
        h = 30 + (hash_i(i) - 0.5) * 4
        k.cap([x, 0, 36], [x, h, 36], 3.1, "log", group="pal%d" % i)
        k.cone([x, h + 3.0, 36], 3.0, 3.1, 0.3, "log", group="palt%d" % i)
    k.box_mm([0, 18, 38.4], [64, 20.5, 40.5], "beam", group="band")
    k.box_mm([0, 7, 38.4], [64, 9.5, 40.5], "beam", group="band")
    return k


def hash_i(i):
    return (np.sin(i * 12.9898) * 43758.5453) % 1.0


def wall_2():
    k = Kit()
    k.box_mm([0, 0, 32], [64, 30, 44], "stone", tex=t_bricks(9, 5, seed=81, jitter=4), group="wall")
    k.box_mm([0, 30, 31], [64, 33, 45], "stone", tex=t_bricks(8, 3, 0.2, seed=82), group="cap")
    return k


def wall_3():
    k = Kit()
    st = t_bricks(10, 5, seed=91, jitter=4)
    k.box_mm([0, 0, 22], [64, 40, 50], "stone", tex=st, group="wall")
    k.box_mm([0, 40, 23], [64, 40.8, 49], "cobble", tex=t_bricks(5, 4, 0.18), group="walk")
    for i in range(4):
        x = 8 + i * 16
        k.box([x, 43.5, 47.5], [5, 3.5, 2.5], "stone", tex=st, group="cren%d" % i)
        k.box([x, 43.5, 24.5], [5, 3.5, 2.5], "stone", tex=st, group="crenb%d" % i)
    for x in (16, 48):
        k.box_mm([x - 1, 18, 49.6], [x + 1, 28, 50.6], "glass", group="slit%d" % x)
    return k


# ---------------------------------------------------------------- tower (1x1)

def tower_1():
    k = Kit()
    ground_patch(k, 6, 58, 30, 62, "dirt", oval=True)
    pts = [(14, 32), (50, 32), (14, 56), (50, 56)]
    for i, (x, z) in enumerate(pts):
        k.cap([x, 0, z], [32 + (x - 32) * 0.82, 40, 44 + (z - 44) * 0.82], 1.9, "log", group="leg%d" % i)
    k.cap([16, 12, 56], [48, 22, 56], 1.1, "log", group="x1")
    k.cap([48, 12, 56], [16, 22, 56], 1.1, "log", group="x2")
    k.box_mm([12, 40, 28], [52, 43, 60], "plank", tex=t_planks(3.2, False), group="deck")
    for x in np.linspace(14, 50, 7):
        k.cap([x, 43, 59], [x, 50, 59], 0.8, "log", group="rail%d" % int(x))
    k.cap([12, 50, 59], [52, 50, 59], 0.9, "log", group="railtop")
    for x, z in ((14, 30), (50, 30), (14, 58), (50, 58)):
        k.cap([x, 43, z], [x, 60, z], 1.0, "log", group="rp%d%d" % (x, z))
    gable_z(k, 12, 52, 28, 60, 60, 12, "thatch", t_thatch(), over=3)
    for y in range(4, 40, 6):
        k.cap([28, y, 62 - y * 0.1], [36, y, 62 - y * 0.1], 0.6, "plank", group="lad%d" % y)
    k.cap([28, 0, 62], [28, 42, 58], 0.7, "plank", group="ladr")
    k.cap([36, 0, 62], [36, 42, 58], 0.7, "plank", group="ladr2")
    return k


def tower_2():
    k = Kit()
    st = t_bricks(8, 4.5, seed=101, jitter=3)
    k.box_mm([12, 0, 26], [52, 30, 58], "stone", tex=st, group="base")
    k.box_mm([10, 30, 24], [54, 33, 60], "beam", group="floor")
    k.box_mm([13, 33, 27], [51, 52, 57], "plank", tex=t_planks(3.4), group="upper")
    for x in (13, 51):
        k.box_mm([x - 1.5, 33, 56], [x + 1.5, 52, 58.5], "beam", group="cornerpost%d" % x)
    hip_roof(k, 13, 51, 27, 57, 52, 14, "shingle", t_shingles(5, 3.2), over=4)
    door(k, 32, 58.4, 9, 15)
    window(k, 32, 43, 57.4, 10, 5)
    for x in (20, 44):
        k.box_mm([x - 1, 12, 57.8], [x + 1, 22, 58.6], "glass", group="slit%d" % x)
    flag(k, 32, 66, 42, 10)
    return k


def tower_3():
    k = Kit()
    st = t_bricks(8, 5, seed=111, jitter=4)
    k.cyl([32, 30, 42], 22, 30, "stone", tex=st, group="tower")
    k.cyl([32, 61.5, 42], 24, 1.6, "stone", group="ring")
    for i in range(8):
        a = np.pi / 2 + i * 2 * np.pi / 8
        k.box([32 + np.cos(a) * 21.5, 66, 42 + np.sin(a) * 21.5], [4.2, 3.2, 2.4], "stone", R=RY(-a + np.pi / 2), tex=st, group="cren%d" % i)
    k.cyl([32, 62.4, 42], 21, 0.4, "cobble", tex=t_bricks(5, 4, 0.18), group="deck")
    door(k, 32, 64.2, 10, 16)
    for y in (30, 46):
        k.box_mm([31, y, 63.4], [33, y + 8, 64.4], "glass", group="slit%d" % y)
    flag(k, 32, 62, 40, 16, "cloth_red")
    return k


# ---------------------------------------------------------------- gathering props

ROCK = t_rock(cell=3.5, var=0.25)


def stump(k, x, z, r=4.2, h=3.4, axe=False):
    g = "stump%d" % int(x * 10 + z)
    k.cyl([x, h / 2, z], r, h / 2, "log", rnd=0.5, group=g)
    k.cyl([x, h + 0.1, z], r - 0.7, 0.2, "plank", group=g)
    if axe:
        tool_axe(k, [x + 0.6, h + 1.2, z + 0.4], norm([0.55, 1.0, 0.35]), group=g)


def tool_axe(k, at, d, group, length=12.0):
    at = np.asarray(at, float)
    side = norm(np.cross(d, [0, 0, 1.0]))
    k.cap(at, at + d * length, 0.6, "plank", group=group)
    head = at + d * 1.2
    k.box(head - side * 1.6, [2.2, 1.4, 0.5], "metal", R=lean_frame(d) @ RZ(np.pi / 2), rnd=0.2, group=group)


def tool_pick(k, at, d, group, length=11.0):
    at = np.asarray(at, float)
    top = at + d * length
    side = norm(np.cross(d, [0, 0, 1.0]))
    k.cap(at, top, 0.6, "plank", group=group)
    k.cap(top, top + side * 4.5 - d * 1.2, 0.9, "metal", r2=0.35, group=group)
    k.cap(top, top - side * 4.5 - d * 1.2, 0.9, "metal", r2=0.35, group=group)


def sawhorse(k, x, z, length=24, log=True, along_x=True, r=3.0):
    g = "horse%d" % int(x * 10 + z)
    a = np.array([1.0, 0, 0]) if along_x else np.array([0, 0, 1.0])
    s = np.array([0, 0, 1.0]) if along_x else np.array([1.0, 0, 0])
    c = np.array([x, 0, z], float)
    for e in (-1, 1):
        base = c + a * e * (length / 2 - 3)
        for sx in (-1, 1):
            k.cap(base + s * sx * 4 + [0, 0, 0], base - s * sx * 1.0 + [0, 9, 0], 0.8, "beam", group=g)
    if log:
        k.cap(c + [0, 9 + r, 0] - a * length / 2, c + [0, 9 + r, 0] + a * length / 2, r, "log", group=g)
        for e in (-1, 1):
            k.cyl(c + [0, 9 + r, 0] + a * e * (length / 2 + r * 0.7), r - 0.8, 0.25, "plank",
                  R=RZ(np.pi / 2) if along_x else RX(np.pi / 2), group=g)


def plank_stack(k, x, z, w=12, l=24, layers=4, along_x=False):
    g = "planks%d" % int(x * 10 + z)
    for i in range(layers):
        off = (i % 2) * 1.2 - 0.6
        if along_x:
            k.box_mm([x - l / 2 + off, i * 1.7, z - w / 2], [x + l / 2 + off, i * 1.7 + 1.5, z + w / 2], "plank",
                     tex=t_planks(3.0, vertical=True, seed=i + x), group=g)
        else:
            k.box_mm([x - w / 2, i * 1.7, z - l / 2 + off], [x + w / 2, i * 1.7 + 1.5, z + l / 2 + off], "plank",
                     tex=t_planks(3.0, vertical=True, seed=i + x), group=g)


def sawdust(k, x, z, rx=9, rz=6):
    k.ell([x, -0.2, z], [rx, 0.9, rz], "wheat", tex=t_noise(0.25, 0.8, seed=x), group="ground")


def saw_blade(k, c, r=9.0, along_x=True):
    R = RX(np.pi / 2) if along_x else RZ(np.pi / 2)
    k.cyl(c, r, 0.35, "steel", R=R, group="blade")
    k.cyl(c, 1.6, 0.9, "metal", R=R, group="blade")


def stone_block(k, x, z, s=(4.5, 3.5, 4.0), y=0.0, rot=0.0, mat="stone_warm"):
    k.box([x, y + s[1], z], list(s), mat, R=RY(rot), rnd=0.5, tex=t_bricks(30, 30, gap=0.0, var=0.08, seed=x + z),
          group="block%d" % int(x * 10 + z + y))


def block_stack(k, x, z, mat="stone_warm"):
    for i, (dx, dz, y) in enumerate(((-5, 0, 0), (5, 0, 0), (0, 0, 7.2))):
        k.box([x + dx, y + 3.5, z + dz], [4.6, 3.5, 4.2], mat, rnd=0.5, tex=t_bricks(30, 30, gap=0.0, var=0.08, seed=i + x),
              group="bstack%d" % int(x * 10 + z))


def ore_pile(k, x, z, ore="rust", base="stone", r=7.0, seed=0):
    rng = np.random.RandomState(seed + int(x * 7 + z))
    g = "ore%d" % int(x * 10 + z)
    k.ell([x, 0.5, z], [r, r * 0.55, r * 0.85], base, tex=ROCK, group=g)
    for n in range(7):
        a = rng.uniform(0, 2 * np.pi)
        d = rng.uniform(0.2, 0.8)
        k.ell([x + np.cos(a) * r * d, r * 0.45 * (1 - d * 0.6), z + np.sin(a) * r * 0.85 * d],
              np.array([1.7, 1.3, 1.6]) * rng.uniform(0.8, 1.2), ore, R=RY(rng.uniform(0, 3)), group=g)


def rails(k, x, z0, z1, gauge=3.2):
    for sx in (-1, 1):
        k.cap([x + sx * gauge, 0.9, z0], [x + sx * gauge, 0.9, z1], 0.45, "metal", group="rails")
    zz = z0 + 1.5
    while zz < z1:
        k.box_mm([x - gauge - 2, -0.2, zz - 1.0], [x + gauge + 2, 0.7, zz + 1.0], "beam", group="rails")
        zz += 5.0


def mine_cart(k, x, z, ore="rust", base="stone", along_z=True):
    g = "cart%d" % int(x * 10 + z)
    hw, hl = (3.8, 5.4) if along_z else (5.4, 3.8)
    k.box([x, 5.6, z], [hw, 2.8, hl], "plank", tex=t_planks(2.4, vertical=False, seed=x), rnd=0.3, group=g)
    k.box([x, 8.2, z], [hw + 0.3, 0.45, hl + 0.3], "metal", group=g)
    for sx in (-1, 1):
        for sz in (-1, 1):
            wc = [x + sx * (hw + 0.3), 2.2, z + sz * (hl - 1.8)] if along_z else [x + sx * (hw - 1.8), 2.2, z + sz * (hl + 0.3)]
            k.cyl(wc, 2.2, 0.5, "metal", R=RZ(np.pi / 2) if along_z else RX(np.pi / 2), group=g)
    k.ell([x, 8.6, z], [hw * 0.85, 2.2, hl * 0.85], base, tex=ROCK, group=g)
    for i, (dx, dz) in enumerate(((-1.5, -1.8), (1.4, 0.6), (-0.3, 2.2), (1.8, -2.4))):
        k.ell([x + dx, 10.2, z + dz], [1.4, 1.1, 1.3], ore, group=g)


def lantern(k, x, z, h=16):
    g = "lantern%d" % int(x * 10 + z)
    k.cap([x, 0, z], [x, h, z], 0.8, "beam", group=g)
    k.cap([x, h, z], [x + 4, h, z], 0.6, "beam", group=g)
    k.box([x + 4, h - 3.2, z], [1.4, 1.8, 1.4], "glass_lit", group=g)
    k.box([x + 4, h - 1.0, z], [1.8, 0.4, 1.8], "metal", group=g)


def crane(k, x, z, h=36, reach=20, ang=0.6, load="stone_warm"):
    g = "crane%d" % int(x * 10 + z)
    d = np.array([np.cos(ang), 0, np.sin(ang)])
    k.cap([x, 0, z], [x, h, z], 1.7, "log", group=g)
    for a in (0.0, 2.1, 4.2):
        f = np.array([np.cos(a), 0, np.sin(a)])
        k.cap([x, 12, z], [x + f[0] * 9, 0, z + f[2] * 9], 1.0, "beam", group=g)
    tip = np.array([x, h + 6, z]) + d * reach
    k.cap([x, h - 12, z], tip, 1.1, "beam", group=g)
    k.cap([x, h + 2, z], tip, 0.5, "rope", group=g)
    k.sphere(tip, 1.4, "metal", group=g)
    hook = tip - [0, h - 4, 0]
    k.cap(tip, hook, 0.3, "rope", group=g)
    if load:
        k.box([hook[0], hook[1] - 3.5, hook[2]], [4.0, 3.2, 3.6], load, rnd=0.5, group=g)


def headframe(k, x, z, h=40, w=14):
    g = "headframe"
    for sx in (-1, 1):
        k.cap([x + sx * w, 0, z + 6], [x + sx * 3, h, z], 1.4, "beam", group=g)
        k.cap([x + sx * w, 0, z - 6], [x + sx * 3, h, z], 1.4, "beam", group=g)
    for y in (12, 24):
        t = y / h
        hw = w + (3 - w) * t
        k.cap([x - hw, y, z + 6 * (1 - t)], [x + hw, y, z + 6 * (1 - t)], 0.9, "beam", group=g)
        k.cap([x - hw, y, z - 6 * (1 - t)], [x + hw, y, z - 6 * (1 - t)], 0.9, "beam", group=g)
    k.box_mm([x - 4.5, h - 1, z - 2], [x + 4.5, h + 1.4, z + 2], "beam", group=g)
    k.add("torus", "metal", None, g, c=np.array([x, h + 6.5, z]), RR=6.0, r=0.8, R=RX(np.pi / 2))
    for a in range(4):
        q = a * np.pi / 4
        k.cap([x - 6 * np.cos(q), h + 6.5 - 6 * np.sin(q), z], [x + 6 * np.cos(q), h + 6.5 + 6 * np.sin(q), z], 0.35, "metal", group=g)
    k.cap([x + 6, h + 6.5, z], [x + 6, 4, z], 0.3, "rope", group=g)


def shaft(k, x, z, w=12):
    k.box_mm([x - w / 2 - 2, 0, z - w / 2 - 2], [x + w / 2 + 2, 3, z + w / 2 + 2], "beam", tex=t_planks(3.0, vertical=False), group="shaft")
    k.box_mm([x - w / 2, 2, z - w / 2], [x + w / 2, 3.2, z + w / 2], "dark", group="shaft")


def log_heap(k, x, z, rows=(3, 2, 1), length=13.0, r=2.8, ang=0.45):
    a = np.array([np.cos(ang), 0, -np.sin(ang)])
    b = np.array([np.sin(ang), 0, np.cos(ang)])
    Re = lean_frame(a)
    g = "heap%d" % int(x * 10 + z)
    for row, cnt in enumerate(rows):
        for j in range(cnt):
            c = np.array([x, 0, z]) + b * (j - (cnt - 1) / 2) * 2 * r + [0, r + row * (2 * r - 0.9), 0]
            L = length / 2 - row * 0.6 + (j % 2) * 0.7
            k.cap(c - a * L, c + a * L, r, "log", group=g)
            for sx in (1, -1):
                k.cyl(c + sx * a * (L + r * 0.72), r - 0.7, 0.3, "plank", R=Re, group=g)


def cliff(k, x0, x1, z0, z1, h, seed=0, steps=1, cut="stone"):
    rng = np.random.RandomState(seed)
    depth = (z1 - z0) / steps
    for i in range(steps):
        za, zb = z0 + depth * i, z0 + depth * (i + 1)
        hh = h * (1 - i / steps)
        n = max(2, int((x1 - x0) / 22))
        xs = np.linspace(x0, x1, n + 1)
        for j in range(n):
            jit = rng.uniform(-0.12, 0.12) * hh
            mat = cut if i > 0 or steps == 1 else "rock"
            tex = ROCK if mat == "rock" else t_bricks(13, 7, gap=0.18, var=0.12, seed=seed + i * 7 + j, jitter=5)
            k.box_mm([xs[j] - 1.5, 0, za], [xs[j + 1] + 1.5, hh + jit, zb], mat, rnd=1.4, tex=tex, group="cliff%d" % i)
    for n in range(int((x1 - x0) / 16)):
        cx = rng.uniform(x0 + 6, x1 - 6)
        k.ell([cx, h + rng.uniform(-3, 1), z0 + rng.uniform(4, depth - 4)], [rng.uniform(6, 10), rng.uniform(3, 5), rng.uniform(5, 8)],
              "rock", R=RY(rng.uniform(0, 3)), tex=ROCK, group="cliff0")
    for n in range(int((x1 - x0) / 20)):
        cx = rng.uniform(x0 + 6, x1 - 6)
        k.ell([cx, h + rng.uniform(1, 3), z0 + rng.uniform(3, depth - 3)], [rng.uniform(3, 6), 1.6, rng.uniform(3, 5)],
              "moss", tex=t_noise(0.2, 1.0, seed=n), group="cliff0")


def mine_mouth(k, x, z, w=18, h=18, stone_frame=False):
    k.box_mm([x - w / 2, 0, z - 1.0], [x + w / 2, h, z + 0.6], "dark", group="mouth")
    k.cyl([x, h, z - 0.2], w / 2, 0.8, "dark", R=RX(np.pi / 2), group="mouth")
    if stone_frame:
        st = t_bricks(6, 4, seed=7, jitter=2)
        for sx in (-1, 1):
            k.box_mm([x + sx * (w / 2 + 3) - 3, 0, z - 1], [x + sx * (w / 2 + 3) + 3, h + 2, z + 3], "stone", tex=st, group="frame")
        k.cyl([x, h + 1, z + 1], w / 2 + 6, 2.0, "stone", R=RX(np.pi / 2), tex=st, clips=[(np.array([0, -1.0, 0]), np.array([x, h, z]))], group="frame")
        k.cyl([x, h, z + 1.4], w / 2 + 0.6, 2.2, "dark", R=RX(np.pi / 2), group="frame")
        k.box_mm([x - 2.5, h + w / 2 + 2, z + 2.6], [x + 2.5, h + w / 2 + 6, z + 3.6], "stone_warm", group="frame")
    else:
        for sx in (-1, 1):
            k.cap([x + sx * (w / 2 + 1.4), 0, z + 1.4], [x + sx * (w / 2 + 0.8), h + w / 2 + 1, z + 1.4], 1.8, "log", group="frame")
        k.cap([x - w / 2 - 5, h + w / 2 + 2.4, z + 1.6], [x + w / 2 + 5, h + w / 2 + 2.4, z + 1.6], 2.1, "log", group="frame")
        for sx in (-1, 1):
            k.cap([x + sx * (w / 2 + 0.8), h + 1, z + 1.6], [x + sx * (w / 2 - 4.5), h + w / 2 + 1.5, z + 1.6], 1.0, "beam", group="frame")


# ---------------------------------------------------------------- lumber mill (2x2)

def lumber_mill_1():
    k = Kit()
    ground_patch(k, 8, 120, 28, 124, "dirt", oval=True)
    sawdust(k, 70, 96, 18, 9)
    posts(k, [(20, 38), (58, 38), (20, 60), (58, 60)], 24, 1.5)
    shed_roof(k, 18, 60, 36, 62, 29, 24, "thatch", t_thatch(2.6, seed=3), over=2.5)
    log_heap(k, 39, 52, (3, 2), 26, 2.6, 0.0)
    log_heap(k, 96, 50, (3, 2, 1), 22, 3.0, 0.35)
    sawhorse(k, 70, 92, 26)
    k.box([70, 13.5, 92], [9.0, 2.6, 0.3], "steel", R=RZ(0.15), group="bucksaw")
    k.cap([60, 12, 92], [61, 20, 92], 0.6, "plank", group="bucksaw")
    stump(k, 30, 96, 4.8, 4.0, axe=True)
    stump(k, 104, 86, 4.4, 3.4)
    stump(k, 96, 110, 3.8, 3.0)
    plank_stack(k, 42, 114, 12, 24, 3, along_x=True)
    return k


def lumber_mill_2():
    k = Kit()
    ground_patch(k, 6, 122, 24, 126, "dirt")
    sawdust(k, 86, 100, 22, 10)
    x0, x1, z0, z1 = 10, 60, 30, 70
    top = log_cabin(k, x0, x1, z0, z1, 24)
    gable_z(k, x0, x1, z0, z1, top, 16, "shingle", t_shingles(6, 3.4, seed=5), over=4, wall="plank", wall_tex=t_planks(3.4))
    door(k, 35, z1 + 2.4, 10, 15, arch=False)
    window(k, 50, 15, z1 + 2.4, 5, 5, shutters="plank")
    log_heap(k, 96, 50, (4, 3, 2, 1), 28, 3.0, 0.3)
    sawhorse(k, 86, 92, 36, r=3.4)
    k.box([86, 16.5, 92], [0.35, 6.0, 11.5], "steel", group="pitsaw")
    k.cap([86, 23, 80], [86, 26, 77], 0.7, "plank", group="pitsaw")
    k.cap([86, 10, 104], [86, 7, 107], 0.7, "plank", group="pitsaw")
    plank_stack(k, 28, 104, 12, 28, 5, along_x=True)
    plank_stack(k, 54, 116, 12, 22, 3, along_x=True)
    stump(k, 112, 112, 4.4, 3.6, axe=True)
    return k


def lumber_mill_3():
    k = Kit()
    ground_patch(k, 4, 124, 20, 126, "cobble", t_bricks(6, 4, 0.2, seed=13))
    sawdust(k, 72, 102, 20, 8)
    x0, x1, z0, z1 = 8, 84, 26, 66
    k.box_mm([x0, 0, z0], [x1, 6, z1], "stone", tex=t_bricks(8, 4, seed=14, jitter=3), group="mill")
    k.box_mm([x0, 6, z0], [x1, 32, z1], "plank", tex=t_planks(4.0, seed=15), group="mill")
    for x in np.linspace(x0 + 1.5, x1 - 1.5, 5):
        k.box_mm([x - 1.5, 6, z1 - 0.6], [x + 1.5, 32, z1 + 0.8], "beam", group="mill")
    gable_x(k, x0, x1, z0, z1, 32, 18, "tile", t_shingles(5, 3, seed=17), over=4, wall="plank")
    door(k, 26, z1 + 0.6, 14, 18, arch=False)
    window(k, 62, 20, z1 + 0.6, 8, 7, shutters="plank")
    chimney(k, 70, 40, 32, 62, "brick", 4.5)
    k.box_mm([54, 0, 84], [110, 9, 100], "plank", tex=t_planks(3.0, vertical=False, seed=18), group="table")
    for xx in (58, 106):
        for zz in (86, 98):
            k.cap([xx, 0, zz], [xx, 9, zz], 1.0, "beam", group="table")
    saw_blade(k, [82, 10, 92], 10.0)
    k.cap([56, 12.6, 92], [76, 12.6, 92], 3.4, "log", group="sawlog")
    k.box_mm([88, 9, 88], [108, 10.4, 96], "plank", tex=t_planks(3.0, vertical=False, seed=19), group="sawlog2")
    crane(k, 112, 44, 30, 14, 2.6, load=None)
    log_heap(k, 102, 66, (3, 2, 1), 22, 3.0, 0.25)
    plank_stack(k, 24, 104, 12, 30, 6, along_x=True)
    plank_stack(k, 34, 120, 10, 24, 3, along_x=True)
    barrel(k, 118, 116)
    return k


# ---------------------------------------------------------------- quarry (2x2)

def quarry_1():
    k = Kit()
    ground_patch(k, 6, 122, 50, 124, "dirt", oval=True)
    cliff(k, 10, 118, 20, 64, 30, seed=3, steps=2)
    stone_block(k, 36, 84)
    stone_block(k, 50, 96, (4.0, 3.0, 3.6), rot=0.4)
    stone_block(k, 96, 86, (5.0, 3.8, 4.2), rot=-0.3)
    block_stack(k, 74, 110)
    for sx in (-1, 1):
        k.cap([24 + sx * 4.5, 1.0, 102], [24 + sx * 4.5, 1.0, 120], 1.0, "beam", group="sledge")
    k.box_mm([17, 2, 104], [31, 3, 118], "plank", tex=t_planks(2.5), group="sledge")
    k.box([24, 6.8, 111], [4.4, 3.8, 4.2], "stone_warm", rnd=0.5, group="sledge")
    tool_pick(k, [106, 0.5, 108], norm([-0.3, 1.0, 0.5]), "pick")
    return k


def quarry_2():
    k = Kit()
    ground_patch(k, 4, 124, 44, 126, "dirt")
    cliff(k, 6, 122, 14, 66, 38, seed=4, steps=3)
    crane(k, 98, 84, 34, 20, -2.2)
    posts(k, [(12, 82), (40, 82), (12, 104), (40, 104)], 20, 1.4)
    shed_roof(k, 10, 42, 80, 106, 24, 19, "shingle", t_shingles(6, 3.4, seed=8), over=3)
    block_stack(k, 60, 112)
    block_stack(k, 100, 114)
    stone_block(k, 64, 88, (5.0, 3.8, 4.2), rot=0.3)
    mine_cart(k, 80, 100, ore="stone_warm", base="stone_warm", along_z=False)
    tool_pick(k, [118, 0.5, 98], norm([-0.4, 1.0, 0.4]), "pick")
    return k


def quarry_3():
    k = Kit()
    ground_patch(k, 4, 124, 40, 126, "cobble", t_bricks(6, 4, 0.2, seed=19))
    cliff(k, 4, 124, 10, 64, 44, seed=5, steps=3)
    x0, x1, z0, z1 = 8, 50, 74, 104
    k.box_mm([x0, 0, z0], [x1, 22, z1], "stone_warm", tex=t_bricks(8, 4, seed=20, jitter=3), group="workshop")
    gable_x(k, x0, x1, z0, z1, 22, 13, "tile", t_shingles(5, 3, seed=21), over=3, wall="stone_warm")
    door(k, 29, z1 + 0.4, 10, 15)
    wheel_c = np.array([94, 20, 86])
    k.add("torus", "plank", None, "treadwheel", c=wheel_c, RR=15, r=1.5, R=RX(np.pi / 2))
    k.add("torus", "plank", None, "treadwheel", c=wheel_c + [0, 0, 5], RR=15, r=1.5, R=RX(np.pi / 2))
    for a in range(6):
        q = a * np.pi / 6
        dd = np.array([np.cos(q), np.sin(q), 0]) * 15
        k.cap(wheel_c - dd + [0, 0, 2.5], wheel_c + dd + [0, 0, 2.5], 0.7, "beam", group="treadwheel")
    k.cyl(wheel_c + [0, 0, 2.5], 1.8, 4.0, "beam", R=RX(np.pi / 2), group="treadwheel")
    for sx in (-1, 1):
        k.cap([94 + sx * 11, 0, 90], [94, 20, 90], 1.4, "beam", group="treadwheel")
    crane(k, 66, 76, 38, 20, 2.3)
    block_stack(k, 66, 114, "stone")
    block_stack(k, 104, 114)
    stone_block(k, 84, 106, (5.0, 3.8, 4.2), rot=0.5, mat="stone")
    return k


# ---------------------------------------------------------------- mine (2x2)

def mine_1():
    k = Kit()
    ground_patch(k, 6, 122, 50, 124, "dirt", oval=True)
    cliff(k, 12, 116, 22, 62, 34, seed=11, cut="rock")
    mine_mouth(k, 54, 62)
    rails(k, 54, 63, 114)
    mine_cart(k, 54, 100)
    ore_pile(k, 94, 90, seed=1)
    ore_pile(k, 104, 108, "gold", "rock", 5.5, seed=2)
    lantern(k, 26, 72)
    tool_pick(k, [22, 0.5, 108], norm([0.3, 1.0, 0.5]), "pick")
    crate(k, 82, 74, 4.0)
    return k


def mine_2():
    k = Kit()
    ground_patch(k, 4, 124, 46, 126, "dirt")
    cliff(k, 4, 74, 16, 60, 38, seed=12, cut="rock")
    mine_mouth(k, 38, 60)
    rails(k, 38, 61, 118)
    mine_cart(k, 38, 94)
    shaft(k, 100, 66)
    headframe(k, 100, 66, 38, 13)
    ore_pile(k, 76, 106, seed=3)
    ore_pile(k, 104, 110, "gold", "rock", 6.0, seed=4)
    lantern(k, 14, 70)
    barrel(k, 118, 92)
    crate(k, 16, 106, 4.5)
    return k


def mine_3():
    k = Kit()
    ground_patch(k, 4, 124, 44, 126, "cobble", t_bricks(6, 4, 0.2, seed=22))
    cliff(k, 4, 72, 12, 58, 44, seed=13, cut="rock")
    mine_mouth(k, 36, 58, w=18, h=18, stone_frame=True)
    rails(k, 36, 61, 120)
    mine_cart(k, 36, 82)
    mine_cart(k, 36, 108)
    shaft(k, 100, 56)
    headframe(k, 100, 56, 44, 14)
    k.box_mm([80, 0, 82], [108, 16, 104], "stone", tex=t_bricks(6, 4, seed=23, jitter=2), group="smelter")
    k.cone([94, 22, 93], 6, 11, 6.5, "stone", tex=t_bricks(6, 4, seed=24), group="smelter")
    chimney(k, 94, 93, 26, 50, "brick", 4.0)
    k.box_mm([89, 3, 103.6], [99, 11, 104.6], "ember", group="smelter")
    k.add("custom", "fire", t_flame(4, 14), "flame", fn=_flame_sdf_y(94, 104, 3, 0.45))
    ore_pile(k, 114, 114, seed=5)
    ore_pile(k, 66, 112, "gold", "rock", 6.0, seed=6)
    lantern(k, 14, 66)
    return k

FIXED = {"town_hall"}

BUILDINGS = {
    "town_hall": ((2, 2), [town_hall_1, town_hall_2, town_hall_3, town_hall_4]),
    "house": ((1, 1), [house_1, house_2, house_3]),
    "storage": ((2, 2), [storage_1, storage_2, storage_3]),
    "farm": ((2, 2), [farm_1, farm_2, farm_3]),
    "blacksmith": ((3, 2), [blacksmith_1, blacksmith_2, blacksmith_3]),
    "workshop": ((2, 2), [workshop_1, workshop_2, workshop_3]),
    "market": ((2, 2), [market_1, market_2, market_3]),
    "barracks": ((2, 2), [barracks_1, barracks_2, barracks_3]),
    "wall": ((1, 1), [wall_1, wall_2, wall_3]),
    "tower": ((1, 1), [tower_1, tower_2, tower_3]),
    "lumber_mill": ((2, 2), [lumber_mill_1, lumber_mill_2, lumber_mill_3]),
    "quarry": ((2, 2), [quarry_1, quarry_2, quarry_3]),
    "mine": ((2, 2), [mine_1, mine_2, mine_3]),
}
