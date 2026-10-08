import numpy as np
from sdf import norm, rot_axis
from rig import Body, Pose, ik2, axis_frame


def cone_to(b, base, tip, r, mat, group):
    d = np.asarray(tip, float) - np.asarray(base, float)
    L = np.linalg.norm(d)
    b.cone((np.asarray(base) + np.asarray(tip)) / 2, L / 2, r, 0.15, mat, Rl=axis_frame(d / L), group=group)


class Wolf:
    S = 1.3

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
        b.ell(B([0, 10.5, -1.0]), [3.4, 3.6, 7.6], "fur", Rl=RB, group="body")
        b.ell(B([0, 11.2, 4.2]), [4.0, 4.5, 4.6], "fur", Rl=RB, group="body")
        b.ell(B([0, 9.0, 3.6]), [2.6, 2.6, 3.6], "fur_light", Rl=RB, group="body")
        b.ell(B([0, 12.6, 1.0]), [3.0, 1.6, 6.0], "fur", Rl=RB, group="body")
        hd = ex.get("head", np.zeros(3))
        Rh = RB @ rot_axis([1, 0, 0], ex.get("head_pitch", 0.0))
        hc = B(np.array([0, 14.4, 9.6]) + hd)

        def Hh(p):
            return hc + Rh @ np.asarray(p, float)

        b.cap(B([0, 12.5, 6.0]), hc, 2.6, "fur", group="neck")
        b.ell(hc, [3.0, 3.0, 3.4], "fur", Rl=Rh, group="head")
        jaw = ex.get("jaw", 0.0)
        b.ell(Hh([0, -0.4, 3.6]), [1.6, 1.3, 2.8], "fur", Rl=Rh, group="snout")
        b.ell(Hh([0, -1.5 - jaw * 0.6, 3.0]), [1.3, 0.7, 2.5], "fur_light", Rl=Rh @ rot_axis([1, 0, 0], jaw * 0.6), group="jaw")
        if jaw > 0.2:
            b.ell(Hh([0, -1.0 - jaw * 0.3, 3.4]), [1.1, 0.6, 2.0], "cloth_red", Rl=Rh, group="mouth")
            b.decal(Hh([0.6, -0.6, 5.6]), "#f2ead2", 1.5)
            b.decal(Hh([-0.6, -0.6, 5.6]), "#f2ead2", 1.5)
        b.decal(Hh([0, 0.4, 6.4]), "#14111a", 1.6)
        for sx in (1, -1):
            cone_to(b, Hh([sx * 1.7, 2.0, -0.6]), Hh([sx * 2.3, 5.4, -1.2]), 1.25, "fur", "ear")
            b.decal(Hh([sx * 1.4, 0.9, 2.6]), "#f5d040", 1.4)
        tw = ex.get("tail", 0.0)
        t0 = B([0, 12.0, -8.0])
        t1 = B([np.sin(tw) * 2.0, 10.5, -11.5])
        t2 = B([np.sin(tw) * 4.0, 8.0, -14.0])
        b.cap(t0, t1, 1.5, "fur", r2=1.6, group="tail")
        b.cap(t1, t2, 1.6, "fur_light", r2=0.8, group="tail")
        feet = pose.feet
        for name, hip, pole in (("FL", [-2.0, 9.0, 5.0], [0, 0, -1]), ("FR", [2.0, 9.0, 5.0], [0, 0, -1]),
                                ("HL", [-2.3, 9.5, -5.6], [0, 0, 1]), ("HR", [2.3, 9.5, -5.6], [0, 0, 1])):
            hp = B(hip)
            ft = X(feet[name])
            knee, ft2 = ik2(hp, ft, 5.0, 5.2, RB @ np.array(pole, float) + np.array([0, -0.2, 0]))
            b.cap(hp, knee, 1.7, "fur", r2=1.2, group="leg" + name)
            b.cap(knee, ft2, 1.15, "fur", r2=0.95, group="leg" + name)
            b.ell(ft2 + RB @ np.array([0, 0.1, 0.6]), [1.2, 0.8, 1.5], "fur_light", Rl=RB, group="leg" + name)
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
