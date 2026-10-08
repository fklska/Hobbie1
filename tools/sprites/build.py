import sys
import time
import warnings
import numpy as np

warnings.filterwarnings("ignore")

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


TARGETS = {"hero": hero}

if __name__ == "__main__":
    names = sys.argv[1:] or list(TARGETS)
    for n in names:
        t = time.time()
        uid = TARGETS[n]()
        print(n, uid, f"{time.time() - t:.1f}s")
