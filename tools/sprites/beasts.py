import numpy as np
from sdf import norm, rot_axis
from rig import Body, Pose, ik2, axis_frame


def cone_to(b, base, tip, r, mat, group):
    d = np.asarray(tip, float) - np.asarray(base, float)
    L = np.linalg.norm(d)
    b.cone((np.asarray(base) + np.asarray(tip)) / 2, L / 2, r, 0.15, mat, Rl=axis_frame(d / L), group=group)


class Wolf:
    S = 1.3

    def __init__(self, fur="fur", light="fur_light", eye="#f5d040", collar=False):
        self.fur = fur
        self.light = light
        self.eye = eye
        self.collar = collar

    def build(self, pose, d, tool=None):
        b = Body(d, self.S)
        ex = pose.extra
        roll = ex.get("roll", 0.0)
        Rr = rot_axis([0, 0, 1], roll)
        piv = np.array([0, 0, 0.0])
        root = pose.root

        def X(p):
            return piv + Rr @ (np.asarray(p, float) + root - piv)

        pitch = ex.get("pitch", 0.0)
        Rp = rot_axis([1, 0, 0], pitch)

        def B(p):
            q = np.asarray(p, float)
            return X(Rp @ q)

        RB = Rr @ Rp
        b.ell(B([0, 10.5, -1.0]), [3.4, 3.6, 7.6], self.fur, Rl=RB, group="body")
        b.ell(B([0, 11.2, 4.2]), [4.0, 4.5, 4.6], self.fur, Rl=RB, group="body")
        b.ell(B([0, 9.0, 3.6]), [2.6, 2.6, 3.6], self.light, Rl=RB, group="body")
        b.ell(B([0, 12.6, 1.0]), [3.0, 1.6, 6.0], self.fur, Rl=RB, group="body")
        hd = ex.get("head", np.zeros(3))
        Rh = RB @ rot_axis([1, 0, 0], ex.get("head_pitch", 0.0))
        hc = B(np.array([0, 14.4, 9.6]) + hd)

        def Hh(p):
            return hc + Rh @ np.asarray(p, float)

        b.cap(B([0, 12.5, 6.0]), hc, 2.6, self.fur, group="neck")
        if self.collar:
            nc = (B([0, 12.5, 6.0]) + hc) / 2
            for k in range(6):
                a = 2 * np.pi * k / 6
                off = RB @ np.array([np.cos(a) * 2.7, np.sin(a) * 2.5 + 0.3, -0.4])
                b.ell(nc + off, [1.3, 0.5, 0.9], "leaf", Rl=RB @ rot_axis([0, 0, 1], a), group="collar%d" % k)
        b.ell(hc, [3.0, 3.0, 3.4], self.fur, Rl=Rh, group="head")
        jaw = ex.get("jaw", 0.0)
        b.ell(Hh([0, -0.4, 3.6]), [1.6, 1.3, 2.8], self.fur, Rl=Rh, group="snout")
        b.ell(Hh([0, -1.5 - jaw * 0.6, 3.0]), [1.3, 0.7, 2.5], self.light, Rl=Rh @ rot_axis([1, 0, 0], jaw * 0.6), group="jaw")
        if jaw > 0.2:
            b.ell(Hh([0, -1.0 - jaw * 0.3, 3.4]), [1.1, 0.6, 2.0], "cloth_red", Rl=Rh, group="mouth")
            b.decal(Hh([0.6, -0.6, 5.6]), "#f2ead2", 1.5)
            b.decal(Hh([-0.6, -0.6, 5.6]), "#f2ead2", 1.5)
        b.decal(Hh([0, 0.4, 6.4]), "#14111a", 1.6)
        for sx in (1, -1):
            cone_to(b, Hh([sx * 1.7, 2.0, -0.6]), Hh([sx * 2.3, 5.4, -1.2]), 1.25, self.fur, "ear")
            b.decal(Hh([sx * 1.4, 0.9, 2.6]), self.eye, 1.4)
        tw = ex.get("tail", 0.0)
        t0 = B([0, 12.0, -8.0])
        t1 = B([np.sin(tw) * 2.0, 10.5, -11.5])
        t2 = B([np.sin(tw) * 4.0, 8.0, -14.0])
        b.cap(t0, t1, 1.5, self.fur, r2=1.6, group="tail")
        b.cap(t1, t2, 1.6, self.light, r2=0.8, group="tail")
        feet = pose.feet
        for name, hip, pole in (("FL", [-2.0, 9.0, 5.0], [0, 0, -1]), ("FR", [2.0, 9.0, 5.0], [0, 0, -1]),
                                ("HL", [-2.3, 9.5, -5.6], [0, 0, 1]), ("HR", [2.3, 9.5, -5.6], [0, 0, 1])):
            hp = B(hip)
            ft = X(feet[name])
            knee, ft2 = ik2(hp, ft, 5.0, 5.2, RB @ np.array(pole, float) + np.array([0, -0.2, 0]))
            b.cap(hp, knee, 1.7, self.fur, r2=1.2, group="leg" + name)
            b.cap(knee, ft2, 1.15, self.fur, r2=0.95, group="leg" + name)
            b.ell(ft2 + RB @ np.array([0, 0.1, 0.6]), [1.2, 0.8, 1.5], self.light, Rl=RB, group="leg" + name)
        return b


REST = {"FL": np.array([-2.0, 1.0, 5.6]), "FR": np.array([2.0, 1.0, 5.6]),
        "HL": np.array([-2.3, 1.0, -6.0]), "HR": np.array([2.3, 1.0, -6.0])}


def wolf_idle():
    out = []
    for i in range(4):
        bob = [0, -0.3, -0.5, -0.3][i]
        out.append(Pose(root=np.array([0, bob, 0]), feet={k: v - np.array([0, bob, 0]) for k, v in REST.items()},
                        extra={"tail": [0.4, 0.1, -0.3, 0.1][i], "head_pitch": 0.05 * i % 2}))
    return out


def wolf_walk():
    out = []
    n = 8
    for i in range(n):
        ph = 2 * np.pi * i / n
        feet = {}
        for name, off in (("FL", 0.0), ("HR", 0.0), ("FR", np.pi), ("HL", np.pi)):
            a = ph + off
            v = REST[name].copy()
            v[2] += 3.6 * np.cos(a)
            v[1] += 2.0 * max(0.0, np.sin(a))
            feet[name] = v
        bob = 0.5 * np.cos(2 * ph)
        out.append(Pose(root=np.array([0, bob, 0]), feet={k: v - np.array([0, bob, 0]) for k, v in feet.items()},
                        extra={"tail": 0.3 * np.sin(ph), "pitch": 0.04 * np.sin(ph), "head": np.array([0, -0.3, 0.3])}))
    return out


def wolf_attack():
    out = []
    keys = [(-1.5, 0.0, 0.10, 0.0), (-2.5, -0.6, 0.18, 0.4), (3.5, 0.6, -0.12, 1.0), (5.0, 0.2, -0.05, 1.0), (3.0, 0.0, 0.0, 0.3), (0.5, 0.0, 0.0, 0.0)]
    for z, y, pitch, jaw in keys:
        feet = {k: v.copy() for k, v in REST.items()}
        feet["FL"][2] += max(0, z) * 0.9
        feet["FR"][2] += max(0, z) * 0.7
        out.append(Pose(root=np.array([0, y, z]), feet={k: v - np.array([0, y, z]) for k, v in feet.items()},
                        extra={"pitch": pitch, "jaw": jaw, "head": np.array([0, -1.0 * jaw, 1.2 * jaw]), "head_pitch": 0.2 * jaw}))
    return out


def wolf_death():
    out = []
    for i, r in enumerate([0.0, 0.25, 0.7, 1.2, 1.45, 1.5]):
        feet = {k: v + np.array([0, 0.6 * i, 0]) for k, v in REST.items()}
        out.append(Pose(root=np.array([0, -1.2 * min(i, 3) / 3, 0]), feet=feet,
                        extra={"roll": r, "jaw": 0.4 if i > 2 else 0.0, "head_pitch": 0.2 * min(i, 3) / 3, "tail": -0.3}))
    return out


class WolfMod:
    ANIMS = {
        "idle": (wolf_idle, 5.0, True),
        "walk": (wolf_walk, 14.0, True),
        "attack": (wolf_attack, 14.0, False),
        "death": (wolf_death, 9.0, False),
    }
    _w = Wolf()

    def build(self, pose, d):
        return self._w.build(pose, d)

    def shadow(self, pose):
        return np.array([0, 0, 0.0]), 12.0, 5.0


wolf = WolfMod()


class AllyWolfMod(WolfMod):
    _w = Wolf(fur="fur_ally", light="fur_ally_light", eye="#9cf07a", collar=True)


ally_wolf = AllyWolfMod()


class Crow:
    S = 1.3

    def build(self, pose, d, tool=None):
        b = Body(d, self.S)
        ex = pose.extra
        R = rot_axis([0, 0, 1], ex.get("roll", 0.0)) @ rot_axis([1, 0, 0], ex.get("pitch", 0.0))
        c = np.array([0, ex.get("h", 16.0), 0]) + pose.root

        def P(p):
            return c + R @ np.asarray(p, float)

        b.ell(P([0, 0, 0]), [2.5, 2.3, 4.4], "crow", Rl=R, group="body")
        b.ell(P([0, -0.7, 0.9]), [2.2, 1.8, 3.0], "crow", Rl=R, group="body")
        hc = P([0, 1.5, 4.5])
        b.sphere(hc, 2.2, "crow", group="head")
        beak = ex.get("beak", 0.0)
        cone_to(b, P([0, 1.3, 6.1]), P([0, 0.9 - beak * 0.4, 9.2]), 0.95, "beak", "beak")
        if beak > 0:
            cone_to(b, P([0, 0.6, 6.0]), P([0, -0.6 - beak * 0.6, 8.3]), 0.7, "beak", "jaw")
        for sx in (1, -1):
            b.decal(P([sx * 1.55, 2.0, 5.7]), "#e8dcb0", 1.3)
        b.ell(P([0, 0.5, -5.6]), [2.5, 0.45, 3.1], "crow", Rl=R @ rot_axis([1, 0, 0], -0.22), group="tail")
        flap = ex.get("flap", 0.0)
        fold = ex.get("fold", 0.0)
        for sx in (1, -1):
            sh = P([sx * 1.9, 1.0, 1.2])
            Rw = R @ rot_axis([0, 0, 1], sx * flap)
            b.ell(sh + Rw @ np.array([sx * 3.4, 0, -0.6]), [3.7, 0.55, 2.5], "crow", Rl=Rw, group="wing%d" % sx)
            Rw2 = Rw @ rot_axis([0, 0, 1], sx * (flap * 0.5 - fold))
            base = sh + Rw @ np.array([sx * 6.6, 0, -0.8])
            b.ell(base + Rw2 @ np.array([sx * 3.0, 0, -1.3]), [3.4, 0.42, 1.9], "crow",
                  Rl=Rw2 @ rot_axis([0, 1, 0], sx * 0.35), group="tip%d" % sx)
        for sx in (1, -1):
            b.cap(P([sx * 0.8, -2.0, 0.2]), P([sx * 0.8, -3.0, -1.4]), 0.35, "beak", group="leg%d" % sx)
        return b


def crow_idle():
    return [Pose(extra={"flap": f, "h": h, "pitch": 0.05}) for f, h in ((0.3, 16.0), (0.12, 15.6), (-0.08, 15.2), (0.12, 15.6))]


def crow_walk():
    keys = ((1.0, 16.0), (0.55, 16.3), (0.0, 16.6), (-0.55, 16.2), (-0.25, 15.8), (0.45, 15.7))
    return [Pose(extra={"flap": f, "h": h, "pitch": 0.12, "fold": 0.2 if f < 0 else 0.0}) for f, h in keys]


def crow_attack():
    keys = ((0.9, 15.0, 0.1, 0.0, 0.0), (1.15, 12.0, 0.35, 1.5, 0.0), (0.4, 8.5, 0.6, 4.0, 1.0),
            (-0.3, 7.0, 0.7, 5.0, 1.0), (0.6, 10.0, 0.3, 3.0, 0.3), (0.9, 14.0, 0.1, 1.0, 0.0))
    return [Pose(root=np.array([0, 0, z]), extra={"flap": f, "h": h, "pitch": pt, "beak": bk}) for f, h, pt, z, bk in keys]


def crow_death():
    keys = ((0.6, 14.0, 0.2), (-0.2, 11.0, 0.6), (0.8, 7.0, 1.1), (0.2, 3.6, 1.5), (0.0, 2.2, 1.6), (0.0, 2.2, 1.6))
    return [Pose(extra={"flap": f, "h": h, "roll": r, "pitch": 0.3, "fold": 0.4}) for f, h, r in keys]


class CrowMod:
    ANIMS = {
        "idle": (crow_idle, 7.0, True),
        "walk": (crow_walk, 14.0, True),
        "attack": (crow_attack, 14.0, False),
        "death": (crow_death, 10.0, False),
    }
    _c = Crow()

    def build(self, pose, d):
        return self._c.build(pose, d)

    def shadow(self, pose):
        r = pose.root
        return np.array([r[0], 0, r[2]]), 7.0, 3.0


crow = CrowMod()


BEAR_REST = {"FL": np.array([-3.0, 1.2, 6.8]), "FR": np.array([3.0, 1.2, 6.8]),
             "HL": np.array([-3.2, 1.2, -6.8]), "HR": np.array([3.2, 1.2, -6.8])}


class Bear:
    S = 1.75

    def build(self, pose, d, tool=None):
        b = Body(d, self.S)
        ex = pose.extra
        Rr = rot_axis([0, 0, 1], ex.get("roll", 0.0))
        root = pose.root

        def X(p):
            return Rr @ (np.asarray(p, float) + root)

        Rp = rot_axis([1, 0, 0], ex.get("pitch", 0.0))
        piv = np.array([0, 9.0, -6.5])

        def B(p):
            return X(piv + Rp @ (np.asarray(p, float) - piv))

        RB = Rr @ Rp
        b.ell(B([0, 11.0, -1.0]), [5.2, 5.0, 8.6], "bear", Rl=RB, group="body")
        b.ell(B([0, 12.8, 4.2]), [5.5, 5.6, 5.2], "bear", Rl=RB, group="body")
        b.ell(B([0, 15.4, 3.0]), [3.8, 2.2, 4.0], "bear", Rl=RB, group="body")
        b.ell(B([0, 11.4, -6.2]), [5.0, 4.8, 4.4], "bear", Rl=RB, group="body")
        b.ell(B([0, 8.2, 1.0]), [4.0, 3.0, 6.0], "bear", Rl=RB, group="body")
        b.sphere(B([0, 12.6, -10.2]), 1.3, "bear_light", group="tail")
        hd = ex.get("head", np.zeros(3))
        Rh = RB @ rot_axis([1, 0, 0], ex.get("head_pitch", 0.0))
        hc = B(np.array([0, 13.4, 11.0]) + hd)

        def Hh(p):
            return hc + Rh @ np.asarray(p, float)

        b.cap(B([0, 13.4, 6.0]), hc, 3.8, "bear", group="neck")
        b.ell(hc, [4.1, 3.8, 4.0], "bear", Rl=Rh, group="head")
        jaw = ex.get("jaw", 0.0)
        b.ell(Hh([0, -1.0, 3.6]), [2.3, 1.8, 2.5], "bear_light", Rl=Rh, group="snout")
        b.ell(Hh([0, -2.4 - jaw * 0.9, 3.0]), [1.9, 0.8, 2.2], "bear_light", Rl=Rh @ rot_axis([1, 0, 0], jaw * 0.5), group="jaw")
        if jaw > 0.2:
            b.ell(Hh([0, -1.8 - jaw * 0.4, 3.4]), [1.6, 0.7, 1.8], "cloth_red", Rl=Rh, group="mouth")
            for sx in (0.9, -0.9):
                b.decal(Hh([sx, -1.3, 5.6]), "#f2ead2", 1.5)
        b.decal(Hh([0, -0.2, 6.0]), "#14111a", 1.8)
        b.decal(Hh([0.6, -0.2, 6.0]), "#14111a", 1.8)
        b.decal(Hh([-0.6, -0.2, 6.0]), "#14111a", 1.8)
        for sx in (1, -1):
            b.sphere(Hh([sx * 2.9, 3.2, -0.8]), 1.4, "bear", group="ear%d" % sx)
            b.decal(Hh([sx * 1.7, 1.0, 3.4]), "#14111a", 1.4)
        feet = pose.feet
        for name, hip, pole in (("FL", [-2.9, 9.8, 6.0], [0, 0, -1]), ("FR", [2.9, 9.8, 6.0], [0, 0, -1]),
                                ("HL", [-3.1, 10.0, -6.5], [0, 0, 1]), ("HR", [3.1, 10.0, -6.5], [0, 0, 1])):
            hp = B(hip)
            ft = X(feet[name])
            knee, ft2 = ik2(hp, ft, 5.0, 5.0, RB @ np.array(pole, float) + np.array([0, -0.2, 0]))
            b.cap(hp, knee, 2.9, "bear", r2=2.4, group="leg" + name)
            b.cap(knee, ft2, 2.4, "bear", r2=2.1, group="leg" + name)
            paw = ft2 + Rr @ np.array([0, 0.0, 0.8])
            b.ell(paw, [2.1, 1.1, 2.5], "bear", Rl=Rr, group="leg" + name)
            for cx in (-0.9, 0.0, 0.9):
                b.decal(paw + Rr @ np.array([cx, -0.2, 2.5]), "#e6dcc0", 1.2)
        return b


def bear_idle():
    out = []
    for i in range(4):
        bob = [0, -0.3, -0.5, -0.3][i]
        out.append(Pose(root=np.array([0, bob, 0]), feet={k: v - np.array([0, bob, 0]) for k, v in BEAR_REST.items()},
                        extra={"head_pitch": [0.0, 0.05, 0.1, 0.05][i], "head": np.array([0.4 * np.sin(i * np.pi / 2), 0, 0])}))
    return out


def bear_walk():
    out = []
    n = 8
    for i in range(n):
        ph = 2 * np.pi * i / n
        feet = {}
        for name, off in (("FL", 0.0), ("HR", 0.0), ("FR", np.pi), ("HL", np.pi)):
            a = ph + off
            v = BEAR_REST[name].copy()
            v[2] += 3.0 * np.cos(a)
            v[1] += 1.6 * max(0.0, np.sin(a))
            feet[name] = v
        bob = 0.45 * np.cos(2 * ph)
        out.append(Pose(root=np.array([0, bob, 0]), feet={k: v - np.array([0, bob, 0]) for k, v in feet.items()},
                        extra={"roll": 0.04 * np.sin(ph), "head": np.array([0.5 * np.sin(ph), -0.4, 0.2]), "head_pitch": 0.08}))
    return out


def bear_attack():
    keys = [(0.0, -0.5, 0.0, None, 0.0), (-0.35, 0.3, 0.3, ([-3.0, 7.0, 9.0], [3.0, 7.0, 9.0]), 0.5),
            (-0.6, 0.6, 0.0, ([-3.4, 13.0, 9.5], [3.4, 13.0, 9.5]), 1.0), (-0.12, 0.0, 1.6, ([-3.0, 2.4, 12.0], [3.0, 2.4, 12.0]), 0.7),
            (0.06, -0.3, 1.6, ([-3.0, 1.2, 10.4], [3.0, 1.2, 10.4]), 0.2), (0.0, 0.0, 0.5, None, 0.0)]
    out = []
    for pitch, y, z, front, jaw in keys:
        feet = {k: v.copy() for k, v in BEAR_REST.items()}
        if front:
            feet["FL"], feet["FR"] = np.array(front[0]), np.array(front[1])
        out.append(Pose(root=np.array([0, y, z]), feet={k: v - np.array([0, y, z]) for k, v in feet.items()},
                        extra={"pitch": pitch, "jaw": jaw, "head_pitch": -0.25 * jaw}))
    return out


def bear_death():
    out = []
    for i, r in enumerate([0.0, 0.3, 0.8, 1.25, 1.5, 1.55]):
        feet = {k: v + np.array([0, 0.6 * i, 0]) for k, v in BEAR_REST.items()}
        out.append(Pose(root=np.array([0, -1.6 * min(i, 3) / 3, 0]), feet=feet,
                        extra={"roll": r, "jaw": 0.4 if i > 2 else 0.0, "head_pitch": 0.2 * min(i, 3) / 3}))
    return out


class BearMod:
    ANIMS = {
        "idle": (bear_idle, 4.0, True),
        "walk": (bear_walk, 9.0, True),
        "attack": (bear_attack, 10.0, False),
        "death": (bear_death, 7.0, False),
    }
    _b = Bear()

    def build(self, pose, d):
        return self._b.build(pose, d)

    def shadow(self, pose):
        return np.array([0, 0, 0.0]), 22.0, 9.0


bear = BearMod()
