import numpy as np
from sdf import norm, rot_axis
from rig import Body, Pose, ik2, axis_frame, upper_xf, fall_xf

S = 1.12
HIP_Y = 13.2
WAIST = np.array([0.0, 15.5, 0.0])
SH_Y = 24.8
SH_X = 5.4
UA, FA = 5.0, 4.8
TH, SN = 6.4, 6.2

REST_HANDS = {"R": np.array([6.6, 15.6, 1.4]), "L": np.array([-6.8, 15.8, 0.4])}
REST_FEET = {"R": np.array([2.9, 1.9, 0.3]), "L": np.array([-2.9, 1.9, -0.3])}


def build(pose, d, tool="sword"):
    b = Body(d, S)
    up, Rup = upper_xf(pose, WAIST)
    fl, Rf = fall_xf(pose)

    def U(p):
        return fl(up(p))

    def Lw(p):
        return fl(np.asarray(p, float) + np.array([0, pose.root[1] * 0.6, pose.root[2]]))

    def F(p):
        return fl(p)

    if pose.fall:
        shift = np.array([0, 0, 21.0 * abs(pose.fall) / 1.5])
        fl0 = fl
        fl = lambda p: fl0(p) + shift

    RU = Rf @ Rup

    feet = pose.feet or REST_FEET
    hands = pose.hands or REST_HANDS

    for side, sx in (("R", 1), ("L", -1)):
        hip = Lw([sx * 2.9, HIP_Y, 0])
        ank = F(feet[side])
        knee, ank2 = ik2(hip, ank, TH, SN, Rf @ np.array([0, 0.2, 1.0]))
        g = "leg" + side
        b.cap(hip, knee, 2.25, "pants", r2=1.95, group=g)
        b.cap(knee, ank2, 1.95, "leather_dark", r2=1.85, group=g)
        fwd = Rf @ np.array([0, 0, 1.0])
        b.ell(ank2 + fwd * 1.0 - Rf @ np.array([0, 0.5, 0]), [2.0, 1.55, 2.9], "leather_dark", Rl=Rf, group=g)

    b.ell(Lw([0, 13.6, 0]), [5.6, 3.6, 4.3], "tunic", Rl=Rf, group="torso")
    b.ell(U([0, 20.2, 0]), [5.1, 6.0, 3.7], "tunic", Rl=RU, group="torso")
    b.ell(U([0, 16.0, 0]), [5.45, 1.05, 4.05], "leather", Rl=RU, group="torso")
    b.decal(U([0, 16.0, 4.1]), "#f3d36b", 1.5)
    b.decal(U([0.9, 16.0, 4.0]), "#d29a33", 1.5)

    b.ell(U([0, 25.4, -0.3]), [4.7, 1.7, 3.9], "cape", Rl=RU, group="torso")
    cape_top = U([0, 24.6, -2.9])
    swing = pose.cape
    cdir = RU @ norm([0, -1, -swing])
    cape_len = 12.5
    cape_c = cape_top + cdir * cape_len / 2 + RU @ np.array([0, 0, 2.2])
    Rc = axis_frame(-cdir, RU @ np.array([1.0, 0, 0]))
    back = (RU @ np.array([0, 0, 1.0]), U([0, 20, -1.4]))
    b.cone(cape_c, cape_len / 2, 6.6, 4.3, "cape", Rl=Rc, shell=0.45, clips=[back], group="cape")

    b.cap(U([0, 25, 0]), U([0, 27.5, 0.2]), 1.8, "skin", group="head")
    hc = U([0, 32.2, 0.3])
    Rh = RU @ rot_axis([1, 0, 0], pose.head_tilt)
    b.ell(hc, [6.3, 5.9, 5.9], "skin", Rl=Rh, group="head")

    def H(p):
        return hc + Rh @ np.asarray(p, float)

    hair_clip = (Rh @ norm([0, -1.0, 1.15]), H([0, 0.3, 1.6]))
    b.ell(H([0, 1.1, -0.7]), [6.8, 6.5, 6.6], "hair", Rl=Rh, clips=[hair_clip], group="head")
    b.ell(H([0.6, 4.4, 3.6]), [5.0, 2.0, 2.0], "hair", Rl=Rh, group="head")
    b.ell(H([-3.6, 3.2, 3.2]), [2.0, 2.4, 2.0], "hair", Rl=Rh, group="head")
    for ex in (2.3, -2.3):
        b.decal(H([ex, -0.2, 5.85]), "#1a1424", 1.8)
        b.decal(H([ex, -1.2, 5.7]), "#1a1424", 1.8)
    b.decal(H([0.0, -2.9, 5.5]), "#9c5a43", 1.2)

    for side, sx in (("R", 1), ("L", -1)):
        g = "arm" + side
        sh = U([sx * SH_X, SH_Y, 0])
        b.ell(U([sx * (SH_X - 0.2), SH_Y + 0.2, 0]), [2.5, 2.0, 2.5], "leather", Rl=RU, group=g)
        hand = hands[side]
        hand = U(hand) if pose.extra.get("hands_upper", True) else F(hand)
        pole = RU @ np.array([sx * 0.6, -0.2, -1.0])
        elbow, wrist = ik2(sh, hand, UA, FA, pole)
        b.cap(sh, elbow, 1.75, "tunic", r2=1.6, group=g)
        b.cap(elbow, wrist, 1.6, "leather", r2=1.45, group=g)
        b.sphere(wrist, 1.65, "skin", group=g)

    if tool and pose.tool_dir is not None:
        hand = U(hands["R"]) if pose.extra.get("hands_upper", True) else F(hands["R"])
        tdir = norm(Rf @ (Rup @ np.asarray(pose.tool_dir, float)))
        cross = norm(np.cross(tdir, RU @ np.array([0, 0, 1.0]) + np.array([0, 0.01, 0])))
        if tool == "sword":
            sword(b, hand, tdir, cross)
        elif tool == "pickaxe":
            pickaxe(b, hand, tdir, RU @ norm(pose.extra.get("pick_up", [0, 1, 0])))
    return b


def sword(b, hand, tdir, cross):
    hand = np.asarray(hand, float)
    s = 1.0
    b.sphere(hand - tdir * 2.3 * s, 0.85, "gold", layer="weapon", group="weapon")
    b.cap(hand - tdir * 1.9, hand + tdir * 1.3, 0.6, "leather", layer="weapon", group="weapon")
    Rg = axis_frame(cross, tdir)
    b.box(hand + tdir * 1.75, [0.55, 2.5, 0.6], "gold", Rl=Rg, rnd=0.3, layer="weapon", group="weapon")
    b.cap(hand + tdir * 2.1, hand + tdir * 15.5, 0.95, "steel", r2=0.3, layer="weapon", group="weapon")
    b.tip = b.w(hand + tdir * 14.5)


def pickaxe(b, hand, tdir, head_up):
    hand = np.asarray(hand, float)
    top = hand + tdir * 9.0
    b.cap(hand - tdir * 2.5, top, 0.7, "wood", layer="weapon", group="weapon")
    side = norm(head_up - (head_up @ tdir) * tdir)
    a = top + side * 4.5 - tdir * 0.8
    c = top - side * 4.5 - tdir * 0.8
    b.cap(top, a, 1.05, "iron", r2=0.45, layer="weapon", group="weapon")
    b.cap(top, c, 1.05, "iron", r2=0.45, layer="weapon", group="weapon")
    b.sphere(top, 1.25, "iron", layer="weapon", group="weapon")


def shadow(pose):
    z = 21.0 * abs(pose.fall) / 1.5
    if pose.fall:
        z -= 20.5 * np.sin(abs(pose.fall))
    return np.array([0, 0, z]), 9.0 + 6.0 * abs(pose.fall) / 1.5, 3.6


def p(**kw):
    return Pose(**kw)


IDLE_DIR = [0.28, -0.45, 1.0]


def anim_idle():
    out = []
    for i in range(4):
        bob = [0, -0.35, -0.7, -0.35][i]
        hands = {
            "R": REST_HANDS["R"] + [0, bob * 0.5, 0.4],
            "L": REST_HANDS["L"] + [0, bob * 0.6, 0],
        }
        out.append(p(root=np.array([0, bob, 0]), hands=hands, tool_dir=IDLE_DIR, cape=0.18 + 0.04 * i % 2))
    return out


def anim_walk():
    out = []
    n = 8
    for i in range(n):
        ph = 2 * np.pi * i / n
        feet = {}
        for side, off in (("R", 0.0), ("L", np.pi)):
            a = ph + off
            z = 4.0 * np.cos(a)
            lift = 2.2 * max(0.0, np.sin(a))
            base = REST_FEET[side]
            feet[side] = np.array([base[0], 1.9 + lift, z])
        bob = -0.55 + 0.55 * np.cos(2 * ph)
        hands = {
            "R": REST_HANDS["R"] + [0, 0.2, 1.4 - 1.3 * np.cos(ph)],
            "L": REST_HANDS["L"] + [0, 0.3 + 0.4 * abs(np.cos(ph)), 2.6 * np.cos(ph)],
        }
        out.append(p(root=np.array([0, bob, 0.3]), feet=feet, hands=hands, tool_dir=IDLE_DIR,
                     twist=0.1 * np.cos(ph), lean=0.08, cape=0.42 + 0.12 * np.sin(2 * ph)))
    return out


ATTACK = [
    ([6.8, 25.5, -1.8], [0.45, 1.0, -0.55], 0.35, [-6.0, 18.5, 2.5]),
    ([5.4, 30.0, 0.4], [0.15, 1.0, 0.15], 0.45, [-6.5, 19.5, 3.0]),
    ([1.5, 24.0, 9.0], [-0.15, 0.25, 1.0], 0.05, [-6.6, 17.0, 1.0]),
    ([-4.5, 17.0, 7.5], [-0.85, -0.45, 0.5], -0.45, [-7.0, 15.5, -1.5]),
    ([-5.5, 15.5, 5.5], [-0.75, -0.65, 0.3], -0.42, [-7.0, 15.5, -1.5]),
    ([3.5, 17.0, 4.0], [0.3, -0.4, 1.0], -0.1, [-6.8, 15.8, 0.4]),
]


def trail(an, i, d):
    if an != "attack" or i not in (2, 3):
        return None
    a, c = ATTACK[i - 1], ATTACK[i]
    pts = []
    for t in np.linspace(0, 1, 9):
        hh = np.array(a[0]) * (1 - t) + np.array(c[0]) * t
        dr = norm(np.array(a[1]) * (1 - t) + np.array(c[1]) * t)
        tw = a[2] * (1 - t) + c[2] * t
        b = build(p(root=np.array([0, -0.5, 0.6]), hands={"R": hh, "L": np.array(c[3])}, tool_dir=dr, twist=tw), d)
        pts.append(b.tip)
    return pts


def anim_attack():
    out = []
    for i, (h, dr, tw, lh) in enumerate(ATTACK):
        feet = {"R": np.array([2.9, 1.9, -1.6]), "L": np.array([-2.9, 1.9, 2.6])}
        lean = [0.0, -0.05, 0.18, 0.22, 0.2, 0.08][i]
        out.append(p(root=np.array([0, -0.5 if i in (2, 3, 4) else 0, 0.6]), feet=feet,
                     hands={"R": np.array(h), "L": np.array(lh)}, tool_dir=dr, twist=tw, lean=lean, cape=0.35))
    return out


WORK = [
    ([3.2, 29.0, 1.0], [0.3, 1.0, -0.7], -0.05),
    ([3.0, 31.5, -0.6], [0.3, 0.55, -1.0], -0.12),
    ([2.6, 27.0, 5.0], [0.2, 0.8, 1.0], 0.1),
    ([2.2, 19.0, 8.6], [0.15, -0.35, 1.0], 0.3),
    ([2.2, 18.0, 8.6], [0.15, -0.5, 1.0], 0.32),
    ([2.8, 22.5, 5.0], [0.2, 0.35, 1.0], 0.15),
]


def anim_work():
    out = []
    for i, (h, dr, lean) in enumerate(WORK):
        hr = np.array(h)
        tdir = norm(dr)
        hl = hr - tdir * 2.4 + np.array([-1.0, 0, 0])
        feet = {"R": np.array([3.1, 1.9, -0.8]), "L": np.array([-3.1, 1.9, 1.8])}
        out.append(p(root=np.array([0, -0.6 if i in (3, 4) else 0, 0.3]), feet=feet,
                     hands={"R": hr, "L": hl}, tool_dir=dr, lean=lean, cape=0.3,
                     extra={"pick_up": [0, 0.2, -1.0] if dr[1] > 0 else [0, 1.0, 0.3]}))
    return out


def anim_death():
    out = []
    falls = [0.0, 0.12, 0.42, 0.9, 1.35, 1.5]
    for i, f in enumerate(falls):
        k = min(1.0, i / 3)
        feet = {"R": np.array([2.9, 1.9, 0.8 * k]), "L": np.array([-2.9, 1.9, 1.6 * k])}
        hands = {"R": REST_HANDS["R"] + [1.5 * k, 4.0 * k, 0.5], "L": REST_HANDS["L"] + [-1.5 * k, 4.0 * k, 0.2]}
        out.append(p(root=np.array([0, -1.2 * k, 0]), feet=feet, hands=hands, tool_dir=[0.4, -0.6 - 0.4 * k, 1.0],
                     fall=-f, fall_axis=(1, 0, 0), head_tilt=-0.2 * k, cape=0.1, lean=-0.1 * k))
    return out


ANIMS = {
    "idle": (anim_idle, 5.0, True, "sword"),
    "walk": (anim_walk, 13.0, True, "sword"),
    "attack": (anim_attack, 14.0, False, "sword"),
    "work": (anim_work, 11.0, True, "pickaxe"),
    "death": (anim_death, 8.0, False, "sword"),
}
