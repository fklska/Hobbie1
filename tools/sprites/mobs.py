import numpy as np
from sdf import norm, rot_axis
from rig import axis_frame
import humanoid as hu


def cone_to(b, base, tip, r, mat, group="head"):
    d = tip - base
    L = np.linalg.norm(d)
    b.cone((base + tip) / 2, L / 2, r, 0.15, mat, Rl=axis_frame(d / L), group=group)


class GoblinSpec(hu.Spec):
    S = 0.84
    head = 1.28
    hunch = 0.22
    wide = 0.95
    limb = 0.85
    eyes = "#f5d040"
    mouth = "#1a1424"
    pads = False
    buckle = False
    mats = dict(skin="goblin_skin", shirt="leather", sleeve="goblin_skin", forearm="goblin_skin", hand="goblin_skin",
                pants="leather_dark", shin="goblin_skin", boot="leather_dark", belt="rope", pads="leather", neck="goblin_skin")

    def head_features(self, b, H, Rh, pose):
        for sx in (1, -1):
            base = H([sx * 5.0, 0.8, -0.5])
            tip = H([sx * 11.5, 3.6, -2.5])
            cone_to(b, base, tip, 2.2, "goblin_skin")
        cone_to(b, H([0, -1.2, 5.0]), H([0, -2.4, 8.4]), 1.4, "goblin_skin")
        b.ell(H([0, 2.2, 4.4]), [4.6, 1.0, 1.6], "goblin_skin", Rl=Rh, group="head")
        b.decal(H([1.2, -3.6, 5.2]), "#f2ead2", 1.2)

    def body_features(self, b, U, Lw, RU, Rf, pose):
        b.box(Lw([0, 10.5, 3.8]), [3.2, 3.2, 0.5], "leather_dark", Rl=Rf, rnd=0.4, group="loin")


class OrcSpec(hu.Spec):
    S = 1.42
    head = 1.0
    hunch = 0.12
    wide = 1.3
    limb = 1.25
    eyes = "#e8402a"
    mouth = None
    buckle = False
    mats = dict(skin="orc_skin", shirt="orc_skin", sleeve="orc_skin", forearm="leather_dark", hand="orc_skin",
                pants="leather_dark", shin="orc_skin", boot="leather_dark", belt="leather_dark", pads="iron", neck="orc_skin")

    def head_features(self, b, H, Rh, pose):
        b.ell(H([0, 1.8, 4.2]), [5.4, 1.6, 2.0], "orc_skin", Rl=Rh, group="head")
        b.ell(H([0, -3.4, 3.6]), [4.6, 2.4, 2.8], "orc_skin", Rl=Rh, group="head")
        for sx in (1, -1):
            cone_to(b, H([sx * 2.6, -3.4, 5.6]), H([sx * 3.0, 0.2, 6.4]), 0.9, "bone")
            cone_to(b, H([sx * 5.6, 0.5, -0.5]), H([sx * 8.5, 1.5, -1.5]), 1.3, "orc_skin")
        b.ell(H([0, 6.0, -1.0]), [2.4, 2.2, 2.4], "hair", Rl=Rh, group="head")
        b.cap(H([0, 6.5, -2.5]), H([0, 1.0, -7.5]), 1.3, "hair", r2=0.7, group="head")

    def body_features(self, b, U, Lw, RU, Rf, pose):
        b.box(Lw([0, 10.0, 3.6]), [3.8, 3.6, 0.6], "leather", Rl=Rf, rnd=0.4, group="loin")
        b.ell(Lw([0, 13.4, 0]), [5.8 * 1.3, 3.0, 4.5 * 1.3], "leather_dark", Rl=Rf, group="kilt")
        Rs = RU @ rot_axis([0, 0, 1], 0.6)
        b.box(U([0, 21, 0]), [0.9, 8.0, 4.0], "leather_dark", Rl=Rs, rnd=0.3, group="strap")

    def back_features(self, b, U, RU, pose):
        for sx in (1, -1):
            cone_to(b, U([sx * 6.6, 27.4, 0]), U([sx * 8.0, 31.0, -0.5]), 1.0, "iron", group="spike")


class SkeletonSpec(hu.Spec):
    S = 1.08
    head = 0.92
    hunch = 0.05
    wide = 0.88
    limb = 0.5
    eyes = None
    mouth = None
    pads = False
    buckle = False
    skirt = True
    mats = dict(skin="bone", shirt="rags", sleeve="bone", forearm="bone", hand="bone",
                pants="bone", shin="bone", boot="bone", belt="leather_dark", pads="rags", neck="bone")

    def skull_shape(self, b, H, Rh, hc, hs):
        b.ell(hc, [5.6 * hs, 5.8 * hs, 5.8 * hs], "bone", Rl=Rh, group="head")
        b.box(H([0, -4.6, 2.2]), [3.4, 1.5, 2.8], "bone", Rl=Rh, rnd=0.8, group="head")

    def head_features(self, b, H, Rh, pose):
        for ex in (2.1, -2.1):
            for dx, dy in ((0, 0), (0, -1.0), (0.9 * np.sign(ex), 0), (0.9 * np.sign(ex), -1.0)):
                b.decal(H([ex + dx * 0.6, -0.2 + dy, 5.4]), "#120c12", 2.0)
            b.decal(H([ex, -0.6, 5.5]), "#d63a2a", 2.0)
        for tx in (-1.6, 0.0, 1.6):
            b.decal(H([tx, -4.6, 5.2]), "#2a251e", 1.8)
        clip = (Rh @ norm([0, -0.6, 1.0]), H([0, 1.4, 2.4]))
        b.ell(H([0, 1.6, -0.8]), [7.0, 7.2, 7.2], "rags", Rl=Rh, clips=[clip], group="hood")

    def body_features(self, b, U, Lw, RU, Rf, pose):
        b.cap(U([0, 23.0, -1.8]), U([0, 16.0, -1.8]), 1.2, "bone", group="spine")

    def back_features(self, b, U, RU, pose):
        base = U([2.0, 17.0, -4.6])
        top = U([-2.5, 28.0, -4.2])
        ax = norm(top - base)
        b.cyl((base + top) / 2, 2.0, np.linalg.norm(top - base) / 2, "leather", Rl=axis_frame(ax), group="quiver")
        for i, o in enumerate((-0.8, 0.6, 1.6)):
            q = top + ax * 2.0 + RU @ np.array([o, 0, 0.3 * i])
            b.cap(top - ax * 1.0 + RU @ np.array([o, 0, 0]), q, 0.35, "wood", group="qarrow%d" % i)
            b.cone(q + ax * 0.8, 0.9, 0.9, 0.2, "cloth_red", Rl=axis_frame(ax), group="qfl%d" % i)


class Mob:
    def __init__(self, spec, anims, shadow_r, trail_tool=None):
        self.SPEC = spec
        self.ANIMS = anims
        self.shadow = hu.shadow_for(shadow_r)
        if trail_tool:
            self.trail = lambda an, i, d: hu.slash_trail(an, i, d, lambda pose, dd: self.build(pose, dd, trail_tool))

    def build(self, pose, d, tool=None):
        return hu.build(pose, d, self.SPEC, tool)


goblin = Mob(GoblinSpec(), {
    "idle": (hu.anim_idle, 6.0, True, "dagger"),
    "walk": (lambda: hu.anim_walk(stride=4.4, lift=2.6), 14.0, True, "dagger"),
    "attack": (hu.anim_slash, 14.0, False, "dagger"),
    "death": (hu.anim_death, 9.0, False, "dagger"),
}, 7.5, trail_tool="dagger")

orc = Mob(OrcSpec(), {
    "idle": (lambda: hu.anim_idle(tool_dir=[0.2, -0.3, 1.0]), 4.0, True, "club"),
    "walk": (lambda: hu.anim_walk(stride=3.6, lift=1.8, tool_dir=[0.2, -0.3, 1.0]), 9.0, True, "club"),
    "attack": (hu.anim_smash, 10.0, False, "club"),
    "death": (hu.anim_death, 7.0, False, "club"),
}, 13.0)

skeleton_archer = Mob(SkeletonSpec(), {
    "idle": (hu.anim_bow_idle, 5.0, True, "bow"),
    "walk": (hu.anim_bow_walk, 11.0, True, "bow"),
    "attack": (hu.anim_bow, 9.0, False, "bow"),
    "death": (lambda: hu.anim_death(tool_dir=False), 8.0, False, "bow"),
}, 8.0)
