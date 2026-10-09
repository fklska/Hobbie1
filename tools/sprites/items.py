import numpy as np
from sdf import rot_axis
from kit import Kit

DIAG = rot_axis([0, 0, 1], -np.pi / 4)


class Item(Kit):
    def __init__(self, M=None):
        super().__init__()
        self.M = np.eye(3) if M is None else M

    def P(self, p):
        return self.M @ np.asarray(p, float)

    def Rm(self, R=None):
        return self.M if R is None else self.M @ R

    def c(self, a, b, r, mat, r2=None):
        return self.cap(self.P(a), self.P(b), r, mat, r2=r2)

    def s(self, c, r, mat):
        return self.sphere(self.P(c), r, mat)

    def e(self, c, r, mat, R=None):
        return self.ell(self.P(c), r, mat, R=self.Rm(R))

    def b(self, c, h, mat, R=None, rnd=0.0):
        return self.box(self.P(c), h, mat, R=self.Rm(R), rnd=rnd)

    def cy(self, c, r, h, mat, R=None, rnd=0.0):
        return self.cyl(self.P(c), r, h, mat, R=self.Rm(R), rnd=rnd)

    def co(self, c, h, r1, r2, mat, R=None):
        return self.cone(self.P(c), h, r1, r2, mat, R=self.Rm(R))

    def arc(self, pts, r, mat, r_end=None):
        n = len(pts) - 1
        for i in range(n):
            ra = r if r_end is None else r + (r_end - r) * abs(2 * i / n - 1)
            rb = r if r_end is None else r + (r_end - r) * abs(2 * (i + 1) / n - 1)
            self.c(pts[i], pts[i + 1], ra, mat, r2=rb)


ZX = rot_axis([0, 0, 1], np.pi / 2)


def club():
    k = Item(DIAG)
    k.c([0, -18, 0], [0, -11, 0], 1.5, "leather_dark")
    k.c([0, -11, 0], [0, -3, 0], 1.3, "wood", r2=1.6)
    k.c([0, -3, 0], [0, 13, 0], 2.0, "wood", r2=4.6)
    k.s([0.6, 14.5, 0], 3.6, "wood")
    for y, a in ((4, 0.3), (8, 2.4), (11, 4.4), (13.5, 1.2), (7, 5.6)):
        r = 1.6 + (y + 3) * 0.17
        k.s([np.cos(a) * r, y, np.sin(a) * r], 0.95, "iron")
    return k


def spear():
    k = Item(DIAG)
    k.c([0, -20, 0], [0, 9, 0], 0.85, "wood")
    k.c([0, -20, 0], [0, -18.5, 0], 1.1, "iron")
    k.cy([0, 9.5, 0], 1.25, 1.3, "rope", rnd=0.3)
    k.e([0, 15.0, 0], [2.6, 5.2, 0.75], "steel")
    k.co([0, 20.5, 0], 1.6, 1.4, 0.1, "steel")
    return k


def sword():
    k = Item(DIAG)
    k.s([0, -15.5, 0], 1.6, "gold")
    k.c([0, -14.5, 0], [0, -8.5, 0], 0.95, "leather")
    k.b([0, -7.6, 0], [4.8, 0.85, 1.0], "gold", rnd=0.4)
    k.b([0, 5.0, 0], [1.55, 11.5, 0.45], "steel", rnd=0.3)
    k.co([0, 18.0, 0], 1.6, 1.55, 0.05, "steel", R=None)
    return k


def halberd():
    k = Item(DIAG)
    k.c([0, -20, 0], [0, 15, 0], 0.85, "wood")
    k.co([0, 18.5, 0], 3.2, 1.2, 0.05, "steel")
    k.b([2.6, 11.5, 0], [2.6, 2.6, 0.45], "steel", rnd=0.3)
    k.e([5.2, 11.5, 0], [1.6, 4.6, 0.45], "steel")
    k.co([-2.8, 12.0, 0], 2.2, 1.2, 0.1, "steel", R=ZX)
    k.cy([0, 8.3, 0], 1.15, 0.6, "iron")
    k.cy([0, 14.8, 0], 1.15, 0.6, "iron")
    k.c([0, -5, 0], [0, 0, 0], 1.1, "leather_dark")
    return k


def sling():
    k = Item(DIAG)
    k.torus(k.P([0, 15.5, 0]), 2.2, 0.55, "rope", R=k.Rm(rot_axis([1, 0, 0], np.pi / 2)))
    for sx in (1, -1):
        k.arc([[sx * 0.4, 13.3, 0], [sx * 1.4, 4, 0.3], [sx * 3.0, -4, 0.6], [sx * 4.4, -9, 0]], 0.42, "rope")
    k.e([0, -11.5, 0], [5.0, 3.4, 2.6], "leather")
    k.s([0, -10.6, 1.4], 2.6, "stone")
    return k


def bow():
    k = Item(DIAG)
    ys = np.linspace(-16, 16, 13)
    pts = [[8.5 * (1 - (y / 16) ** 2) - 3.5 - 0.8 * (abs(y) > 13), y, 0] for y in ys]
    k.arc(pts, 1.35, "wood", r_end=0.65)
    k.c([pts[6][0], -2.4, 0], [pts[6][0], 2.4, 0], 1.65, "leather")
    k.c([-4.0, -15.2, -0.3], [-4.0, 15.2, -0.3], 0.3, "cloth_white")
    return k


def crossbow():
    k = Item(DIAG)
    k.b([0, -6.5, 0], [1.6, 12.5, 1.2], "wood", rnd=0.6)
    k.b([0, -16.5, -0.2], [2.4, 3.0, 1.5], "wood", rnd=0.8)
    xs = np.linspace(-12, 12, 11)
    prod = [[x, 7.5 + 3.0 * (1 - (x / 12) ** 2), 0.4] for x in xs]
    k.arc(prod, 0.95, "steel", r_end=0.55)
    k.c([-12, 7.5, 0.4], [0, 0.5, 0.6], 0.25, "cloth_white")
    k.c([12, 7.5, 0.4], [0, 0.5, 0.6], 0.25, "cloth_white")
    k.c([0, 0.5, 1.4], [0, 12.5, 1.4], 0.45, "wood")
    k.co([0, 13.8, 1.4], 1.3, 0.95, 0.05, "iron")
    k.b([0, -4.0, -1.6], [0.5, 2.0, 0.8], "iron", rnd=0.3)
    k.torus(k.P([0, 13.0, 0]), 2.2, 0.45, "iron", R=k.Rm(rot_axis([1, 0, 0], np.pi / 2)))
    return k


def musket():
    k = Item(DIAG)
    k.b([0, -15.0, 0], [2.4, 3.8, 1.3], "wood", rnd=0.9)
    k.c([0, -12, 0], [0, -6, 0], 1.3, "wood", r2=1.0)
    k.c([-0.6, -6, 0], [-0.6, 13, 0], 1.15, "wood", r2=0.9)
    k.c([0.5, -9, 0.3], [0.5, 19.8, 0.3], 0.8, "iron")
    k.b([1.2, -7.2, 0.6], [1.0, 2.2, 0.5], "metal", rnd=0.3)
    k.c([1.9, -8.6, 0.8], [2.6, -6.4, 0.8], 0.35, "metal")
    for y in (1.0, 9.0):
        k.cy([0, y, 0.1], 1.45, 0.45, "gold")
    k.b([-1.6, -8.0, 0], [0.4, 0.9, 0.35], "metal", rnd=0.15)
    return k


def arrow():
    k = Item()
    k.c([-11, 0, 0], [6.5, 0, 0], 0.55, "wood")
    k.co([8.3, 0, 0], 2.3, 1.5, 0.05, "iron", R=rot_axis([0, 0, 1], -np.pi / 2))
    for a in (0.3, 2.4, 4.5):
        R = rot_axis([1, 0, 0], a)
        k.box(np.array([-8.4, 0, 0]) + R @ np.array([0, 1.4, 0]), [2.6, 1.3, 0.2], "cloth_red", R=R @ rot_axis([0, 0, 1], 0.25), rnd=0.15)
    return k


def coins():
    k = Item()
    for i in range(3):
        k.cyl([-3.2 + 0.4 * (i % 2), 0.6 + i * 1.3, -0.5], 3.0, 0.55, "gold", rnd=0.25)
    k.cyl([1.2, 0.6, 3.2], 3.0, 0.55, "gold", R=rot_axis([1, 0, 0], 0.15), rnd=0.25)
    face = rot_axis([1, 0, 0], np.pi / 2 - 0.25)
    k.cyl([3.6, 3.2, -0.6], 3.2, 0.6, "gold", R=face, rnd=0.25)
    k.box([3.6, 3.25, -0.05], [0.5, 1.4, 0.3], "gold", R=rot_axis([1, 0, 0], -0.25), rnd=0.2)
    return k


def artifact():
    k = Item()
    up = rot_axis([1, 0, 0], np.pi / 2)
    k.torus([0, 9.0, 0], 4.6, 0.95, "gold", R=up)
    k.e([0, 9.0, 0.4], [3.6, 4.2, 2.0], "amethyst")
    k.e([-1.0, 10.4, 2.1], [1.0, 1.1, 0.5], "amethyst_glow")
    k.torus([0, 15.0, 0], 1.4, 0.5, "gold", R=rot_axis([0, 1, 0], np.pi / 2) @ up)
    for a in (0.6, 2.5, 3.8, 5.6):
        k.sphere([np.cos(a) * 4.6, 9.0 + np.sin(a) * 4.6, 0.9], 0.9, "amethyst")
    return k


WEAPONS = {"club": club, "spear": spear, "sword": sword, "halberd": halberd,
           "sling": sling, "bow": bow, "crossbow": crossbow, "musket": musket}

# name -> (builder, (w, h), anchor, pitch)
PICKUPS = {
    "arrow": (arrow, (28, 12), (14, 6), 30.0),
    "coins": (coins, (24, 20), (12, 14), 40.0),
    "artifact": (artifact, (20, 22), (10, 19), 20.0),
}
