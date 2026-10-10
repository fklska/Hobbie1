import numpy as np
from sdf import norm, rot_axis
from rig import Body, Pose, ik2, axis_frame, upper_xf, fall_xf

HIP_Y = 13.2
WAIST = np.array([0.0, 15.5, 0.0])
SH_Y = 24.8
SH_X = 5.4
UA, FA = 5.0, 4.8
TH, SN = 6.4, 6.2

REST_HANDS = {"R": np.array([6.6, 15.6, 1.4]), "L": np.array([-6.8, 15.8, 0.4])}
REST_FEET = {"R": np.array([2.9, 1.9, 0.3]), "L": np.array([-2.9, 1.9, -0.3])}


class Spec:
    S = 1.12
    wide = 1.0
    limb = 1.0
    head = 1.0
    hunch = 0.0
    eyes = "#1a1424"
    mats = dict(skin="skin", shirt="tunic", sleeve="tunic", forearm="leather", hand="skin", pants="pants",
                shin="leather_dark", boot="leather_dark", belt="leather", pads="leather", neck="skin")
    pads = True
    belt = True
    skirt = True
    buckle = True
    mouth = "#9c5a43"

    def head_features(self, b, H, Rh, pose):
        pass

    def body_features(self, b, U, Lw, RU, Rf, pose):
        pass

    def back_features(self, b, U, RU, pose):
        pass


def build(pose, d, spec, tool=None):
    sp = spec
    m = sp.mats
    w = sp.wide
    lr = sp.limb
    b = Body(d, sp.S)
    pose.lean += sp.hunch
    up, Rup = upper_xf(pose, WAIST)
    pose.lean -= sp.hunch
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

    def wx(p):
        p = np.array(p, float)
        p[0] *= w
        return p

    for side, sx in (("R", 1), ("L", -1)):
        hip = Lw([sx * 2.9 * w, HIP_Y, 0])
        ank = F(wx(feet[side]))
        knee, ank2 = ik2(hip, ank, TH, SN, Rf @ np.array([0, 0.2, 1.0]))
        g = "leg" + side
        b.cap(hip, knee, 2.25 * lr * w, m["pants"], r2=1.95 * lr * w, group=g)
        b.cap(knee, ank2, 1.95 * lr * w, m["shin"], r2=1.85 * lr * w, group=g)
        fwd = Rf @ np.array([0, 0, 1.0])
        b.ell(ank2 + fwd * 1.0 - Rf @ np.array([0, 0.5, 0]), [2.0 * w * min(lr, 1), 1.55, 2.9 * min(lr * 1.2, 1)], m["boot"], Rl=Rf, group=g)

    if sp.skirt:
        b.ell(Lw([0, 13.6, 0]), [5.6 * w, 3.6, 4.3 * w], m["shirt"], Rl=Rf, group="torso")
    b.ell(U([0, 20.2, 0]), [5.1 * w, 6.0, 3.7 * w], m["shirt"], Rl=RU, group="torso")
    if sp.belt:
        b.ell(U([0, 16.0, 0]), [5.45 * w, 1.05, 4.05 * w], m["belt"], Rl=RU, group="torso")
        if sp.buckle:
            b.decal(U([0, 16.0, 4.1 * w]), "#f3d36b", 1.5)
            b.decal(U([0.9, 16.0, 4.0 * w]), "#d29a33", 1.5)
    sp.body_features(b, U, Lw, RU, Rf, pose)
    sp.back_features(b, U, RU, pose)

    hs = sp.head
    b.cap(U([0, 25, 0]), U([0, 27.5, 0.2]), 1.8 * w * lr, m["neck"], group="head")
    hc = U([0, 32.2 + (hs - 1) * 5.0, 0.3])
    Rh = RU @ rot_axis([1, 0, 0], pose.head_tilt)

    def H(p):
        return hc + Rh @ (np.asarray(p, float) * hs)

    if sp.skull_shape:
        sp.skull_shape(b, H, Rh, hc, hs)
    else:
        b.ell(hc, [6.3 * hs, 5.9 * hs, 5.9 * hs], m["skin"], Rl=Rh, group="head")
    sp.head_features(b, H, Rh, pose)
    if sp.eyes:
        for ex in (2.3, -2.3):
            b.decal(H([ex, -0.2, 5.85]), sp.eyes, 1.8 * hs)
            b.decal(H([ex, -1.2, 5.7]), sp.eyes, 1.8 * hs)
    if sp.mouth:
        b.decal(H([0.0, -2.9, 5.5]), sp.mouth, 1.2 * hs)

    for side, sx in (("R", 1), ("L", -1)):
        g = "arm" + side
        sh = U([sx * SH_X * w, SH_Y, 0])
        if sp.pads:
            b.ell(U([sx * (SH_X - 0.2) * w, SH_Y + 0.2, 0]), [2.5 * w, 2.0 * w, 2.5 * w], m["pads"], Rl=RU, group=g)
        hand = wx(hands[side])
        hand = U(hand)
        pole = RU @ np.array([sx * 0.6, -0.2, -1.0])
        elbow, wrist = ik2(sh, hand, UA, FA, pole)
        b.cap(sh, elbow, 1.75 * lr * w, m["sleeve"], r2=1.6 * lr * w, group=g)
        b.cap(elbow, wrist, 1.6 * lr * w, m["forearm"], r2=1.45 * lr * w, group=g)
        b.sphere(wrist, 1.65 * min(w, 1.4) * max(lr, 0.75), m["hand"], group=g)
        if side == "R":
            b.hand_r = wrist
        else:
            b.hand_l = wrist

    if tool and pose.tool_dir is not None:
        tdir = norm(Rf @ (Rup @ np.asarray(pose.tool_dir, float)))
        cross = norm(np.cross(tdir, RU @ np.array([0, 0, 1.0]) + np.array([0, 0.01, 0])))
        TOOLS[tool](b, b.hand_r, tdir, cross, RU, pose)
    if pose.extra.get("bow") is not None:
        bow_pose(b, RU, pose)
    return b


Spec.skull_shape = None


def sword(b, hand, tdir, cross, RU, pose, length=15.5, mat="steel", guard="gold", grip="leather"):
    hand = np.asarray(hand, float)
    b.sphere(hand - tdir * 2.3, 0.85, guard, layer="weapon", group="weapon")
    b.cap(hand - tdir * 1.9, hand + tdir * 1.3, 0.6, grip, layer="weapon", group="weapon")
    Rg = axis_frame(cross, tdir)
    b.box(hand + tdir * 1.75, [0.55, 2.5, 0.6], guard, Rl=Rg, rnd=0.3, layer="weapon", group="weapon")
    b.cap(hand + tdir * 2.1, hand + tdir * length, 0.95, mat, r2=0.3, layer="weapon", group="weapon")
    b.tip = b.w(hand + tdir * (length - 1))


def dagger(b, hand, tdir, cross, RU, pose):
    sword(b, hand, tdir, cross, RU, pose, length=9.0, mat="iron", guard="iron", grip="leather_dark")


def pickaxe(b, hand, tdir, cross, RU, pose):
    hand = np.asarray(hand, float)
    head_up = RU @ norm(pose.extra.get("pick_up", [0, 1, 0]))
    top = hand + tdir * 9.0
    b.cap(hand - tdir * 2.5, top, 0.7, "wood", layer="weapon", group="weapon")
    side = norm(head_up - (head_up @ tdir) * tdir)
    a = top + side * 4.5 - tdir * 0.8
    c = top - side * 4.5 - tdir * 0.8
    b.cap(top, a, 1.05, "iron", r2=0.45, layer="weapon", group="weapon")
    b.cap(top, c, 1.05, "iron", r2=0.45, layer="weapon", group="weapon")
    b.sphere(top, 1.25, "iron", layer="weapon", group="weapon")


def club(b, hand, tdir, cross, RU, pose):
    hand = np.asarray(hand, float)
    b.cap(hand - tdir * 2.0, hand + tdir * 13.0, 1.0, "wood", r2=2.6, layer="weapon", group="weapon")
    side = norm(np.cross(tdir, cross))
    for i, (o, s) in enumerate(((9.0, cross), (11.5, -cross), (10.5, side), (12.5, -side), (8.0, -side))):
        base = hand + tdir * o
        b.cap(base + s * 1.6, base + s * 3.6, 0.55, "iron", r2=0.15, layer="weapon", group="weapon")
    b.tip = b.w(hand + tdir * 13.0)


def bow_held(b, hand, tdir, cross, RU, pose):
    pass


def staff(b, hand, tdir, cross, RU, pose, top_mat="orb_fire"):
    hand = np.asarray(hand, float)
    top = hand + tdir * 19.0
    b.cap(hand - tdir * 12.0, top, 0.75, "wood", r2=0.9, layer="weapon", group="weapon")
    side = norm(np.cross(tdir, cross))
    for s in (cross, -cross, side):
        b.cap(top - tdir * 0.6, top + tdir * 2.6 + s * 1.6, 0.45, "robe_trim", r2=0.25, layer="weapon", group="claw")
    b.sphere(top + tdir * 2.0, 1.9, top_mat, layer="weapon", group="orb")
    b.tip = b.w(top + tdir * 2.0)


def druid_staff(b, hand, tdir, cross, RU, pose):
    hand = np.asarray(hand, float)
    top = hand + tdir * 18.0
    b.cap(hand - tdir * 12.0, top, 0.8, "wood", r2=1.0, layer="weapon", group="weapon")
    side = norm(np.cross(tdir, cross))
    hook = top + tdir * 2.4 + cross * 2.2
    b.cap(top, top + tdir * 2.6 + cross * 0.4, 0.9, "wood", r2=0.8, layer="weapon", group="weapon")
    b.cap(top + tdir * 2.6 + cross * 0.4, hook, 0.8, "wood", r2=0.6, layer="weapon", group="weapon")
    b.cap(hook, hook - tdir * 1.6 + cross * 0.6, 0.6, "wood", r2=0.4, layer="weapon", group="weapon")
    b.sphere(top + tdir * 1.4 + cross * 1.0, 1.25, "orb_leaf", layer="weapon", group="orb")
    for k, (a, l) in enumerate(((0.6, 2.4), (-0.7, 2.2), (2.2, 2.0))):
        d = norm(cross * np.cos(a) + side * np.sin(a) + tdir * 0.4)
        b.ell(top + d * l * 0.7, [1.3, 0.45, 0.8], "leaf", Rl=axis_frame(d, side), layer="weapon", group="leaf%d" % k)
    b.tip = b.w(top + tdir * 2.0)


TOOLS = {"sword": sword, "dagger": dagger, "pickaxe": pickaxe, "club": club, "bow": bow_held, "staff": staff, "druid_staff": druid_staff}


def bow_pose(b, RU, pose):
    draw = pose.extra.get("bow")
    hl = np.asarray(b.hand_l, float)
    hr = np.asarray(b.hand_r, float)
    fwd = RU @ np.array([0, 0, 1.0])
    up = RU @ np.array([0, 1.0, 0])
    bend = 4.0 + 1.0 * draw
    top = hl + up * 8.5 - fwd * 1.5
    bot = hl - up * 8.5 - fwd * 1.5
    mid = hl + fwd * 0.6
    for a, c in ((top, mid), (mid, bot)):
        b.cap(a, c, 0.6, "wood", layer="weapon", group="bow")
    b.cap(top, top - up * 1.2 + fwd * 1.3, 0.5, "wood", layer="weapon", group="bow")
    b.cap(bot, bot + up * 1.2 + fwd * 1.3, 0.5, "wood", layer="weapon", group="bow")
    nock = hr if draw > 0.05 else (top + bot) / 2 - fwd * 0.4
    b.cap(top, nock, 0.18, "cloth_white", layer="weapon", group="string")
    b.cap(nock, bot, 0.18, "cloth_white", layer="weapon", group="string")
    if pose.extra.get("arrow", False):
        tipp = nock + norm(mid - nock) * 14.0
        b.cap(nock, tipp, 0.35, "wood", layer="weapon", group="arrow")
        b.cone(tipp + norm(mid - nock) * 0.9, 0.9, 0.9, 0.1, "iron", Rl=axis_frame(norm(mid - nock)), layer="weapon", group="arrow")


def p(**kw):
    return Pose(**kw)


IDLE_DIR = [0.28, -0.45, 1.0]


def shadow_for(scale_r=9.0):
    def shadow(pose):
        z = 21.0 * abs(pose.fall) / 1.5
        if pose.fall:
            z -= 20.5 * np.sin(abs(pose.fall))
        return np.array([0, 0, z]), scale_r + 6.0 * abs(pose.fall) / 1.5, scale_r * 0.4
    return shadow


def anim_idle(cape=True, tool_dir=IDLE_DIR):
    out = []
    for i in range(4):
        bob = [0, -0.35, -0.7, -0.35][i]
        hands = {"R": REST_HANDS["R"] + [0, bob * 0.5, 0.4], "L": REST_HANDS["L"] + [0, bob * 0.6, 0]}
        out.append(p(root=np.array([0, bob, 0]), hands=hands, tool_dir=tool_dir, cape=0.18 + 0.04 * i % 2))
    return out


def anim_walk(stride=4.0, lift=2.2, tool_dir=IDLE_DIR, n=8):
    out = []
    for i in range(n):
        ph = 2 * np.pi * i / n
        feet = {}
        for side, off in (("R", 0.0), ("L", np.pi)):
            a = ph + off
            z = stride * np.cos(a)
            ly = lift * max(0.0, np.sin(a))
            feet[side] = np.array([REST_FEET[side][0], 1.9 + ly, z])
        bob = -0.55 + 0.55 * np.cos(2 * ph)
        hands = {
            "R": REST_HANDS["R"] + [0, 0.2, 1.4 - 1.3 * np.cos(ph)],
            "L": REST_HANDS["L"] + [0, 0.3 + 0.4 * abs(np.cos(ph)), 2.6 * np.cos(ph)],
        }
        out.append(p(root=np.array([0, bob, 0.3]), feet=feet, hands=hands, tool_dir=tool_dir,
                     twist=0.1 * np.cos(ph), lean=0.08, cape=0.42 + 0.12 * np.sin(2 * ph)))
    return out


SLASH = [
    ([6.8, 25.5, -1.8], [0.45, 1.0, -0.55], 0.35, [-6.0, 18.5, 2.5]),
    ([5.4, 30.0, 0.4], [0.15, 1.0, 0.15], 0.45, [-6.5, 19.5, 3.0]),
    ([1.5, 24.0, 9.0], [-0.15, 0.25, 1.0], 0.05, [-6.6, 17.0, 1.0]),
    ([-4.5, 17.0, 7.5], [-0.85, -0.45, 0.5], -0.45, [-7.0, 15.5, -1.5]),
    ([-5.5, 15.5, 5.5], [-0.75, -0.65, 0.3], -0.42, [-7.0, 15.5, -1.5]),
    ([3.5, 17.0, 4.0], [0.3, -0.4, 1.0], -0.1, [-6.8, 15.8, 0.4]),
]


def anim_slash():
    out = []
    for i, (h, dr, tw, lh) in enumerate(SLASH):
        feet = {"R": np.array([2.9, 1.9, -1.6]), "L": np.array([-2.9, 1.9, 2.6])}
        lean = [0.0, -0.05, 0.18, 0.22, 0.2, 0.08][i]
        out.append(p(root=np.array([0, -0.5 if i in (2, 3, 4) else 0, 0.6]), feet=feet,
                     hands={"R": np.array(h), "L": np.array(lh)}, tool_dir=dr, twist=tw, lean=lean, cape=0.35))
    return out


def slash_trail(an, i, d, build_fn):
    if an != "attack" or i not in (2, 3):
        return None
    a, c = SLASH[i - 1], SLASH[i]
    pts = []
    for t in np.linspace(0, 1, 9):
        hh = np.array(a[0]) * (1 - t) + np.array(c[0]) * t
        dr = norm(np.array(a[1]) * (1 - t) + np.array(c[1]) * t)
        tw = a[2] * (1 - t) + c[2] * t
        b = build_fn(p(root=np.array([0, -0.5, 0.6]), hands={"R": hh, "L": np.array(c[3])}, tool_dir=dr, twist=tw), d)
        pts.append(b.tip)
    return pts


SMASH = [
    ([3.0, 27.0, -1.0], [0.1, 1.0, -0.6], 0.15, -0.05),
    ([2.0, 32.0, -2.0], [0.05, 0.6, -1.0], 0.1, -0.15),
    ([1.5, 26.0, 6.0], [0.0, 0.7, 1.0], 0.0, 0.15),
    ([1.0, 16.0, 9.0], [0.0, -0.5, 1.0], -0.05, 0.38),
    ([1.0, 15.0, 9.0], [0.0, -0.6, 1.0], -0.05, 0.4),
    ([2.5, 19.0, 5.0], [0.1, 0.3, 1.0], 0.0, 0.15),
]


def anim_smash():
    out = []
    for i, (h, dr, tw, lean) in enumerate(SMASH):
        hr = np.array(h)
        hl = hr - norm(dr) * 2.6 + np.array([-1.6, 0, 0])
        feet = {"R": np.array([3.1, 1.9, -1.2]), "L": np.array([-3.1, 1.9, 2.0])}
        out.append(p(root=np.array([0, -0.7 if i in (3, 4) else 0, 0.4]), feet=feet,
                     hands={"R": hr, "L": hl}, tool_dir=dr, twist=tw, lean=lean))
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


def anim_bow():
    out = []
    keys = [(0.0, False, 0.0), (0.3, True, 0.0), (0.7, True, 0.0), (1.0, True, 0.0), (1.0, True, 0.0), (0.0, False, 0.0)]
    for i, (draw, arrow, _) in enumerate(keys):
        hl = np.array([-3.5, 24.5, 9.5])
        hr = np.array([-1.5 + 2.5 * draw, 25.0, 9.0 - 8.5 * draw])
        if i == 5:
            hr = np.array([3.5, 24.0, -1.5])
        feet = {"R": np.array([2.9, 1.9, -1.8]), "L": np.array([-2.9, 1.9, 2.2])}
        out.append(p(root=np.array([0, 0, 0.2]), feet=feet, hands={"R": hr, "L": hl}, twist=0.35, lean=-0.03,
                     extra={"bow": draw, "arrow": arrow}))
    return out


def anim_bow_idle():
    out = []
    for i in range(4):
        bob = [0, -0.35, -0.7, -0.35][i]
        hands = {"R": REST_HANDS["R"] + [0, bob * 0.5, 0.4], "L": np.array([-6.0, 17.5 + bob * 0.5, 3.5])}
        out.append(p(root=np.array([0, bob, 0]), hands=hands, extra={"bow": 0.0, "arrow": False}))
    return out


def anim_bow_walk():
    out = anim_walk(tool_dir=None)
    for i, q in enumerate(out):
        q.hands["L"] = np.array([-6.0, 17.8, 3.5 + 0.8 * np.cos(2 * np.pi * i / 8)])
        q.extra = {"bow": 0.0, "arrow": False}
    return out


def anim_death(tool_dir=True):
    out = []
    falls = [0.0, 0.12, 0.42, 0.9, 1.35, 1.5]
    for i, f in enumerate(falls):
        k = min(1.0, i / 3)
        feet = {"R": np.array([2.9, 1.9, 0.8 * k]), "L": np.array([-2.9, 1.9, 1.6 * k])}
        hands = {"R": REST_HANDS["R"] + [1.5 * k, 4.0 * k, 0.5], "L": REST_HANDS["L"] + [-1.5 * k, 4.0 * k, 0.2]}
        out.append(p(root=np.array([0, -1.2 * k, 0]), feet=feet, hands=hands,
                     tool_dir=[0.4, -0.6 - 0.4 * k, 1.0] if tool_dir else None,
                     fall=-f, fall_axis=(1, 0, 0), head_tilt=-0.2 * k, cape=0.1, lean=-0.1 * k))
    return out


STAFF_DIR = [0.12, 1.0, 0.3]

CAST = [
    ([6.6, 17.0, 2.0], [0.1, 1.0, 0.3], [-6.8, 15.8, 0.4], 0.0),
    ([5.6, 22.0, 4.0], [0.05, 1.0, 0.5], [-6.2, 19.0, 3.0], -0.04),
    ([4.6, 28.5, 3.5], [0.0, 1.0, 0.2], [-5.6, 26.0, 4.0], -0.1),
    ([4.4, 29.5, 3.0], [0.0, 1.0, 0.15], [-5.4, 27.0, 4.2], -0.12),
    ([3.4, 25.0, 9.0], [0.0, 0.5, 1.0], [-3.8, 23.0, 8.5], 0.12),
    ([5.4, 20.0, 6.0], [0.08, 0.85, 0.6], [-6.4, 17.5, 2.0], 0.05),
]


def anim_cast():
    out = []
    for i, (h, dr, lh, lean) in enumerate(CAST):
        feet = {"R": np.array([2.9, 1.9, -1.2]), "L": np.array([-2.9, 1.9, 1.8])}
        out.append(p(root=np.array([0, 0.3 if i in (2, 3) else 0, 0.2]), feet=feet,
                     hands={"R": np.array(h), "L": np.array(lh)}, tool_dir=dr, lean=lean, cape=0.3 + 0.1 * (i in (2, 3))))
    return out


def anim_heavy_windup():
    keys = [SMASH[0], ([2.2, 31.5, -1.6], [0.05, 1.0, -0.45], 0.1, -0.12), ([1.8, 33.0, -2.2], [0.05, 1.0, -0.75], 0.1, -0.2)]
    out = []
    for i, (h, dr, tw, lean) in enumerate(keys):
        hr = np.array(h)
        hl = hr - norm(dr) * 2.6 + np.array([-1.6, 0, 0])
        feet = {"R": np.array([3.1, 1.9, -1.2]), "L": np.array([-3.1, 1.9, 2.0])}
        out.append(p(root=np.array([0, 0, 0.4]), feet=feet, hands={"R": hr, "L": hl}, tool_dir=dr, twist=tw, lean=lean, cape=0.2))
    return out


def anim_heavy():
    return anim_smash()[2:]
