import numpy as np
from sdf import rot_axis
from kit import Kit, t_rock
from rig import axis_frame

ROCK_TEX = t_rock(cell=3.5, var=0.25)
STONES = [([-1, 8, -1], [12, 8.5, 10], 0.35, 4.5), ([11, 5, 6], [7, 5, 6], 0.9, 3.0), ([-12, 4, 5], [6.5, 4.5, 5.5], -0.5, 2.8),
          ([2, 2, 12], [3.2, 2, 2.6], 0.4, 1.3), ([-17, 1.6, -5], [2.6, 1.6, 2.2], 1.1, 1.0)]


def boulders(mat, ore=None, seed=0):
    k = Kit()
    for i, (c, h, a, rnd) in enumerate(STONES):
        R = rot_axis([0, 1, 0], a) @ rot_axis([0, 0, 1], 0.12 * a) @ rot_axis([1, 0, 0], -0.1)
        k.box(c, h, mat, R=R, rnd=rnd, tex=ROCK_TEX, group="s%d" % i)
        k.box(np.array(c) + [h[0] * 0.15, h[1] * 0.7, -h[2] * 0.1], np.array(h) * [0.6, 0.45, 0.6], mat, R=R @ rot_axis([0, 1, 0], 0.6), rnd=rnd * 0.7, tex=ROCK_TEX, group="s%d" % i)
    if ore:
        rng = np.random.RandomState(seed)
        for c, h, a, _ in STONES[:3]:
            R = rot_axis([0, 1, 0], a) @ rot_axis([0, 0, 1], 0.12 * a) @ rot_axis([1, 0, 0], -0.1)
            for n in range(6):
                ax = (1, 2, 0, 2, 1, 0)[n]
                q = rng.uniform(-0.6, 0.6, 3) * h
                q[ax] = h[ax] * (1 if ax != 0 or n % 2 else -1) * 0.92
                k.ell(np.array(c) + R @ q, np.array([1.8, 1.4, 1.8]) * rng.uniform(0.8, 1.2), ore, R=R @ rot_axis([0, 1, 0], rng.uniform(0, 3)), group="ore")
    return k


def rock():
    return boulders("stone")


def iron():
    return boulders("stone", ore="rust", seed=2)


def gold():
    return boulders("rock", ore="gold", seed=5)


def wood():
    k = Kit()
    r = 3.4
    a = np.array([np.cos(0.55), 0, -np.sin(0.55)])
    b = np.array([np.sin(0.55), 0, np.cos(0.55)])
    Re = axis_frame(a)
    for row, cnt in enumerate((4, 3, 2)):
        for j in range(cnt):
            c = b * (j - (cnt - 1) / 2) * 2 * r + [0, r + row * (2 * r - 1.0), 0]
            L = 11 - row * 0.8 + (j % 2) * 0.8
            k.cap(c - a * L, c + a * L, r, "log", group="log%d%d" % (row, j))
            for sx in (1, -1):
                k.cyl(c + sx * a * (L + r * 0.75), r - 0.8, 0.3, "plank", R=Re, group="end%d%d" % (row, j))
    return k


RESOURCES = {"rock": rock, "iron": iron, "gold": gold, "wood": wood}
