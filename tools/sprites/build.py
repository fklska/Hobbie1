import sys
import time
import warnings
import numpy as np

warnings.filterwarnings("ignore")

import sdf
from sdf import Camera, render
from palette import MATS
import godot_res as gr

CHAR_PITCH = 30.0


def render_character(module, out_dir, name, size, anchor, mats=MATS, dirs=range(8), pitch=CHAR_PITCH, big=None):
    anims = {}
    for an, spec in module.ANIMS.items():
        fn, fps, loop = spec[:3]
        extra = spec[3] if len(spec) > 3 else None
        sz = size
        anc = anchor
        if big and an in big:
            pad = big[an]
            sz = (size[0] + 2 * pad, size[1] + 2 * pad)
            anc = (anchor[0] + pad, anchor[1] + pad)
        cam = Camera("ortho", pitch=pitch, w=sz[0], h=sz[1], anchor=anc)
        poses = fn()
        rows = []
        for d in dirs:
            row = []
            for pose in poses:
                b = module.build(pose, d, extra) if extra is not None else module.build(pose, d)
                img, _, _ = render(b.prims, mats, cam, decals=b.decals)
                if hasattr(module, "shadow"):
                    lp, rx, ry = module.shadow(pose)
                    sx, sy, _ = cam.project(b.w(lp))
                    img = under_shadow(img, sx, sy, rx, ry)
                if hasattr(module, "trail"):
                    pts = module.trail(an, len(row), d)
                    if pts:
                        img = draw_trail(img, cam, pts)
                check_clip(img, f"{name}/{an}/{d}")
                row.append(img)
            rows.append(row)
        anims[an] = (rows, fps, loop)
    return gr.directional_character(out_dir, name, anims)


def under_shadow(img, cx, cy, rx, ry, alpha=80):
    h, w = img.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    m = ((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2 <= 1.0
    m &= img[:, :, 3] == 0
    out = img.copy()
    out[m] = [18, 14, 28, alpha]
    return out


def draw_trail(img, cam, pts, color=(236, 244, 255)):
    out = img.copy()
    h, w = img.shape[:2]
    scr = [cam.project(p)[:2] for p in pts]
    n = len(scr)
    for k in range(n - 1):
        (x0, y0), (x1, y1) = scr[k], scr[k + 1]
        steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        thick = 1 if k < n // 3 else 2
        alpha = 110 + int(130 * k / n)
        for t in np.linspace(0, 1, steps):
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            for dx in range(thick):
                for dy in range(thick):
                    i, j = int(y) + dy, int(x) + dx
                    if 0 <= i < h and 0 <= j < w:
                        a = out[i, j, 3]
                        if a == 0 or out[i, j, :3].sum() < 600:
                            out[i, j] = [*color, max(alpha, a)] if a == 0 else [*color, 255]
    return out


def check_clip(img, label):
    a = img[:, :, 3] > 0
    if a[0].any() or a[-1].any() or a[:, 0].any() or a[:, -1].any():
        print("clipped:", label)


def hero():
    import hero as h
    return render_character(h, "Art/hero", "hero", (64, 64), (32, 52), big={"death": 8})


MOBS = {
    "goblin": ("mobs", (64, 64), (32, 52), {"death": 8}),
    "orc": ("mobs", (96, 96), (48, 78), {"death": 12}),
    "skeleton_archer": ("mobs", (64, 64), (32, 52), {"death": 8}),
    "wolf": ("beasts", (64, 64), (32, 46), {"death": 6}),
}


def mobs(names=None):
    import importlib
    out = {}
    for name, (mod, size, anchor, big) in MOBS.items():
        if names and name not in names:
            continue
        m = getattr(importlib.import_module(mod), name)
        out[name] = render_character(m, f"Art/mobs/{name}", name, size, anchor, big=big)
    return out


def giant():
    import giant as g
    return render_character(g, "Art/mobs/stone_giant", "stone_giant", (128, 128), (64, 110), big={"death": 12, "melee": 8})


def fit(img, w, h, label):
    ys, xs = np.where(img[:, :, 3] > 0)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    if y1 - y0 > h or x1 - x0 > w:
        print("too big:", label, x1 - x0, y1 - y0)
    out = np.zeros((h, w, 4))
    oy, ox = max(0, (h - (y1 - y0)) // 2), max(0, (w - (x1 - x0)) // 2)
    crop = img[y0:min(y1, y0 + h), x0:min(x1, x0 + w)]
    out[oy:oy + crop.shape[0], ox:ox + crop.shape[1]] = crop
    return out


def weapons(names=None):
    import items
    for name, fn in items.WEAPONS.items():
        if names and name not in names:
            continue
        k = fn()
        img, _, _ = render(k.prims, MATS, Camera("ortho", pitch=12, w=48, h=48, anchor=(24, 24)), decals=k.decals)
        gr.save_png(fit(img, 32, 32, name), f"Art/items/weapons/{name}.png")


def pickups(names=None):
    import items
    for name, (fn, size, anchor, pitch) in items.PICKUPS.items():
        if names and name not in names:
            continue
        k = fn()
        img, _, _ = render(k.prims, MATS, Camera("ortho", pitch=pitch, w=size[0], h=size[1], anchor=anchor), decals=k.decals)
        check_clip(img, name)
        gr.save_png(img, f"Art/items/{name}.png")


def beam():
    import effects
    return effects.giant_beam()


def render_building(fn, fp, pad_top=220):
    W, D = fp[0] * 64, fp[1] * 64
    cam = Camera("oblique", k=BK, w=W, h=D + pad_top, anchor=(0, pad_top))
    kit = fn()
    sdf.set_light(sdf.BUILD_LIGHT)
    img, _, _ = render(kit.prims, MATS, cam, decals=kit.decals, line_depth=3.0)
    sdf.set_light(sdf.CHAR_LIGHT)
    a = img[:, :, 3] > 0
    rows = np.where(a.any(1))[0]
    top = max(0, rows[0] - 1) if len(rows) else 0
    if top == 0 and len(rows):
        print("building clipped at top:", fn.__name__)
    if a[:, 0].any() or a[:, -1].any():
        print("touches side:", fn.__name__)
    return img[top:]


def buildings(names=None):
    import buildings as bd
    for name, (fp, levels) in bd.BUILDINGS.items():
        if names and name not in names:
            continue
        for i, fn in enumerate(levels):
            img = render_building(fn, fp)
            gr.save_png(img, f"Art/buildings/{name}_{i + 1}.png")


BK = 0.9

TARGETS = {"hero": hero, "buildings": buildings, "mobs": mobs, "giant": giant,
           "beam": beam, "weapons": weapons, "pickups": pickups}

if __name__ == "__main__":
    names = sys.argv[1:] or list(TARGETS)
    for n in names:
        t = time.time()
        if ":" in n:
            n, sub = n.split(":")
            uid = TARGETS[n](sub.split(","))
        else:
            uid = TARGETS[n]()
        print(n, uid, f"{time.time() - t:.1f}s")
