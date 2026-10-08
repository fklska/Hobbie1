import numpy as np
from sdf import norm, rot_axis, Prim
from rig import Body, Pose, ik2, axis_frame
from kit import t_rock

ROCK = t_rock()

S = 1.25
HIP = np.array([10.0, 24.0, 0.0])
SH = np.array([22.0, 55.0, 0.0])
UA, FA = 15.0, 15.0
TH, SN = 10.0, 10.0
REST_HANDS = {"R": np.array([26.0, 24.0, 7.0]), "L": np.array([-26.0, 24.0, 7.0])}
REST_FEET = {"R": np.array([10.0, 5.0, 1.0]), "L": np.array([-10.0, 5.0, -1.0])}


def cone_to(b, base, tip, r, mat, group):
    d = np.asarray(tip, float) - np.asarray(base, float)
    L = np.linalg.norm(d)
    b.cone((np.asarray(base) + np.asarray(tip)) / 2, L / 2, r, 0.2, mat, Rl=axis_frame(d / L), group=group)


def build(pose, d, tool=None):
    b = Body(d, S)
    ex = pose.extra
    R = rot_axis([0, 1, 0], pose.twist) @ rot_axis([1, 0, 0], pose.lean) @ rot_axis([0, 0, 1], pose.side)
    piv = np.array([0, 26.0, 0])

    def U(p):
        return piv + R @ (np.asarray(p, float) - piv) + pose.root

    def L(p):
        return np.asarray(p, float) + np.array([0, pose.root[1] * 0.7, pose.root[2]])

    feet = pose.feet or REST_FEET
    hands = pose.hands or REST_HANDS
    glow = ex.get("glow", 0.0)

    for side, sx in (("R", 1), ("L", -1)):
        hip = L(HIP * [sx, 1, 1])
        ank = np.asarray(feet[side], float)
        knee, ank2 = ik2(hip, ank, TH, SN, np.array([0, 0.3, 1.0]))
        g = "leg" + side
        b.cap(hip, knee, 6.2, "rock", r2=5.4, group=g)
        b.cap(knee, ank2, 5.4, "rock", r2=5.0, group=g)
        b.box(knee + [0, 0.5, 3.5], [3.5, 3.0, 2.0], "rock", Rl=rot_axis([1, 0, 0], 0.4), rnd=1.2, group=g)
        b.ell(ank2 + np.array([0, -1.8, 2.5]), [6.5, 3.8, 8.0], "rock", group=g)

    b.ell(L([0, 26, 0]), [14, 7, 9.5], "rock", group="pelvis")
    b.box(U([0, 44, 0]), [16, 13, 11], "rock", Rl=R, rnd=5.0, group="torso")
    for sx in (1, -1):
        b.box(U([sx * 8.5, 49, 6.5]), [7.5, 7.0, 4.5], "rock", Rl=R @ rot_axis([0, 0, 1], sx * 0.25) @ rot_axis([1, 0, 0], -0.15), rnd=2.0, group="plate%d" % sx)
        b.box(U([sx * 10, 36, 5.5]), [6.0, 5.5, 4.0], "rock", Rl=R @ rot_axis([0, 0, 1], -sx * 0.3), rnd=2.0, group="plate2%d" % sx)
    core = U([0, 42, 12.0])
    fwd = R @ np.array([0, 0, 1.0])
    up = R @ np.array([0, 1.0, 0])
    gm = "crystal_glow" if glow >= 0 else "crystal"
    cone_to(b, core - up * 3 - fwd * 2, core + up * (6 + 2 * glow) + fwd * 1.5, 3.2 + 0.6 * glow, gm, "core")
    cone_to(b, core - up * 2 + R @ np.array([2.5, 0, -1.5]), core + R @ np.array([5.5, 3.5, 1.0]), 1.8, gm, "core2")
    cone_to(b, core - up * 2 + R @ np.array([-2.5, 0, -1.5]), core + R @ np.array([-5.0, 4.0, 0.8]), 1.6, gm, "core3")
    for i, (o, tip) in enumerate((([-6, 56, -8], [-9, 68, -13]), ([4, 57, -9], [6, 72, -14]), ([12, 52, -9], [17, 62, -14]))):
        cone_to(b, U(o), U(tip), 2.4 - 0.3 * i, "crystal", "shard%d" % i)

    hd = ex.get("head", np.zeros(3))
    hc = U(np.array([0, 59, 7.5]) + hd)
    Rh = R @ rot_axis([1, 0, 0], ex.get("head_pitch", 0.0))
    b.box(hc, [6.2, 5.2, 5.5], "rock", Rl=Rh, rnd=2.2, group="head")
    b.box(hc + Rh @ np.array([0, 3.0, 2.5]), [6.8, 1.6, 3.0], "rock", Rl=Rh @ rot_axis([1, 0, 0], 0.2), rnd=0.8, group="brow")
    for sx in (1.0, -1.0):
        for dx, dy in ((0, 0), (0.9, 0)):
            b.decal(hc + Rh @ np.array([sx * (2.4 + dx), 0.6 + dy, 5.6]), "#c4f2ff" if glow >= 0 else "#2f7fc0", 2.5)
    b.ell(hc + Rh @ np.array([0, 5.0, -0.5]), [5.5, 1.8, 5.0], "moss", Rl=Rh, group="hmoss")

    for side, sx in (("R", 1), ("L", -1)):
        g = "arm" + side
        sh = U(SH * [sx, 1, 1])
        b.ell(U(SH * [sx, 1, 1] + [sx * 1.0, 3.0, 0]), [10.5, 9.0, 10.5], "rock", Rl=R, group="sh" + side)
        b.ell(U(SH * [sx, 1, 1] + [sx * 1.0, 10.5, 0]), [7.5, 2.4, 7.5], "moss", Rl=R, group="moss" + side)
        hand = U(hands[side])
        elbow, wrist = ik2(sh, hand, UA, FA, R @ np.array([sx * 0.5, 0.0, -1.0]))
        b.cap(sh, elbow, 6.0, "rock", r2=5.2, group=g)
        b.cap(elbow, wrist, 5.6, "rock", r2=6.4, group=g)
        b.box(wrist, [6.6, 6.4, 6.6], "rock", Rl=axis_frame(norm(wrist - elbow)), rnd=3.0, group="fist" + side)
    for pr in b.prims:
        if pr.mat == "rock":
            pr.tex = ROCK
    if ex.get("crumble"):
        crumble(b, ex["crumble"])
    return b


def crumble(b, t):
    rng = np.random.RandomState(7)
    for pr in b.prims:
        k = pr.kw
        pts = [kk for kk in ("c", "a", "b") if kk in k]
        if not pts:
            continue
        cy = np.mean([k[kk][1] for kk in pts])
        cx = np.mean([k[kk][0] for kk in pts])
        cz = np.mean([k[kk][2] for kk in pts])
        spread = np.array([cx, 0, cz * 0.3]) * 0.35 + rng.uniform(-4, 4, 3) * [1, 0, 0.4]
        rad = 4.0 + rng.uniform(0, 3)
        target_y = rad * 0.6
        dy = (target_y - cy) * t
        delta = spread * t + np.array([0, dy, 0])
        for kk in pts:
            k[kk] = k[kk] + delta
        if pr.mat == "crystal_glow" and t > 0.5:
            pr.mat = "crystal"
    b.decals = [] if t > 0.4 else b.decals


def P(**kw):
    return Pose(**kw)


def idle():
    out = []
    for i in range(4):
        bob = [0, -0.6, -1.2, -0.6][i]
        hands = {s: REST_HANDS[s] + [0, bob * 0.6, 0.5 * (i % 2)] for s in "RL"}
        out.append(P(root=np.array([0, bob, 0]), hands=hands, extra={"glow": 0.0}))
    return out


def walk():
    out = []
    n = 8
    for i in range(n):
        ph = 2 * np.pi * i / n
        feet = {}
        for side, off in (("R", 0.0), ("L", np.pi)):
            a = ph + off
            v = REST_FEET[side].copy()
            v[2] = 7.0 * np.cos(a)
            v[1] += 4.0 * max(0.0, np.sin(a))
            feet[side] = v
        bob = -1.2 + 1.2 * np.cos(2 * ph)
        hands = {"R": REST_HANDS["R"] + [0, 0, -5.0 * np.cos(ph)], "L": REST_HANDS["L"] + [0, 0, 5.0 * np.cos(ph)]}
        out.append(P(root=np.array([0, bob, 0.5]), feet=feet, hands=hands, side=0.05 * np.cos(ph), twist=0.06 * np.cos(ph), lean=0.08))
    return out


def melee():
    keys = [
        ([20, 40, 4], 0.0, 0.0, -0.05),
        ([10, 74, -4], 1.5, 0.0, -0.18),
        ([9, 80, -6], 2.0, 0.0, -0.22),
        ([7, 40, 22], -2.0, 4.0, 0.25),
        ([7, 12, 24], -6.0, 5.0, 0.42),
        ([7, 12, 24], -6.0, 5.0, 0.42),
        ([18, 30, 12], -2.0, 2.0, 0.15),
    ]
    out = []
    for h, dy, dz, lean in keys:
        hands = {"R": np.array(h, float), "L": np.array(h, float) * [-1, 1, 1]}
        feet = {"R": np.array([10, 5, -3.0]), "L": np.array([-10, 5, 5.0])}
        out.append(P(root=np.array([0, dy, dz]), hands=hands, feet=feet, lean=lean,
                     extra={"head_pitch": -lean * 0.5}))
    return out


def cast():
    out = []
    for i in range(6):
        g = [0.2, 0.6, 1.0, 1.0, 0.8, 1.0][i]
        w = [0.0, 0.5, 1.0, 1.0, 1.0, 1.0][i]
        hands = {"R": np.array([28 + 6 * w, 38 + 18 * w, 8 - 2 * w]), "L": np.array([-28 - 6 * w, 38 + 18 * w, 8 - 2 * w])}
        out.append(P(root=np.array([0, 0.5 * w, 0]), hands=hands, lean=-0.12 * w,
                     extra={"glow": g, "head_pitch": -0.25 * w}))
    return out


def armor():
    out = []
    for i in range(6):
        t = min(1.0, i / 4)
        hands = {"R": np.array([26 - 20 * t, 24 + 10 * t, 7 + 10 * t]), "L": np.array([-26 + 20 * t, 24 + 10 * t, 7 + 10 * t])}
        feet = {"R": np.array([10 + 2 * t, 5, 1.0]), "L": np.array([-10 - 2 * t, 5, -1.0])}
        out.append(P(root=np.array([0, -9 * t, 0]), hands=hands, feet=feet, lean=0.45 * t,
                     extra={"head_pitch": 0.4 * t, "head": np.array([0, -4 * t, -1 * t]), "glow": -1 if t > 0.5 else 0.0}))
    return out


def death():
    out = []
    for i in range(8):
        t = [0.0, 0.0, 0.12, 0.3, 0.55, 0.8, 0.95, 1.0][i]
        k = min(1.0, i / 2)
        hands = {s: REST_HANDS[s] + [0, -4 * k, 0] for s in "RL"}
        out.append(P(root=np.array([0, -5 * k, 0]), hands=hands, lean=0.25 * k,
                     extra={"crumble": t, "glow": -1 if i > 3 else 0.0, "head_pitch": 0.3 * k}))
    return out


ANIMS = {
    "idle": (idle, 4.0, True),
    "walk": (walk, 8.0, True),
    "melee": (melee, 10.0, False),
    "cast": (cast, 8.0, True),
    "armor": (armor, 10.0, False),
    "death": (death, 6.0, False),
}


def shadow(pose):
    return np.array([0, 0, 0.0]), 28.0, 11.0
