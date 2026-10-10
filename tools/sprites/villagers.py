import numpy as np
from sdf import norm, rot_axis
from rig import axis_frame
import humanoid as hu

CARRY_HAND = np.array([5.4, 21.0, 2.6])
CARRY_DIR = [0.6, 0.8, -0.45]


def axe(b, hand, tdir, cross, RU, pose):
    hand = np.asarray(hand, float)
    head_up = RU @ norm(pose.extra.get("pick_up", [0, 1, 0]))
    top = hand + tdir * 9.0
    b.cap(hand - tdir * 2.5, top + tdir * 1.0, 0.7, "wood", layer="weapon", group="weapon")
    side = -norm(head_up - (head_up @ tdir) * tdir)
    Rg = axis_frame(tdir, side)
    b.box(top + side * 1.8 - tdir * 0.2, [2.2, 1.5, 0.5], "iron", Rl=Rg, rnd=0.2, layer="weapon", group="weapon")
    b.box(top + side * 4.0 - tdir * 0.2, [0.55, 2.6, 0.35], "steel", Rl=Rg, rnd=0.15, layer="weapon", group="weapon")
    b.box(top - side * 1.1, [0.9, 1.2, 0.7], "iron", Rl=Rg, rnd=0.2, layer="weapon", group="weapon")


hu.TOOLS["axe"] = axe


def anim_carry_idle():
    out = hu.anim_idle(tool_dir=CARRY_DIR)
    for q in out:
        q.hands["R"] = CARRY_HAND + [0, q.root[1] * 0.4, 0]
    return out


def anim_carry_walk():
    out = hu.anim_walk(stride=3.8, lift=2.0, tool_dir=CARRY_DIR)
    for i, q in enumerate(out):
        q.hands["R"] = CARRY_HAND + [0, 0.25 * np.cos(4 * np.pi * i / len(out)), 0]
    return out


class WoodcutterSpec(hu.Spec):
    S = 1.02
    pads = False
    buckle = False
    mats = dict(skin="skin", shirt="cloth_red", sleeve="cloth_red", forearm="skin", hand="skin", pants="pants",
                shin="leather_dark", boot="leather_dark", belt="leather", pads="leather", neck="skin")

    def head_features(self, b, H, Rh, pose):
        clip = (Rh @ norm([0, -0.9, 1.0]), H([0, 1.8, 2.2]))
        b.ell(H([0, 1.6, -0.6]), [6.8, 6.6, 6.7], "cloth_green", Rl=Rh, clips=[clip], group="cap")
        b.ell(H([0, 3.4, 2.6]), [6.4, 1.4, 3.6], "cloth_green", Rl=Rh, group="cap")
        b.sphere(H([0, 7.4, -1.6]), 1.7, "cloth_white", group="cap")
        b.ell(H([0, -3.6, 3.8]), [4.6, 3.0, 2.6], "hair", Rl=Rh, group="beard")
        b.ell(H([0, -1.6, 5.2]), [3.0, 0.9, 0.9], "hair", Rl=Rh, group="beard")


class StonemasonSpec(hu.Spec):
    S = 1.04
    wide = 1.06
    pads = False
    buckle = False
    mats = dict(skin="skin", shirt="cloth_white", sleeve="cloth_white", forearm="skin", hand="skin", pants="leather_dark",
                shin="leather_dark", boot="leather_dark", belt="leather", pads="leather", neck="skin")

    def head_features(self, b, H, Rh, pose):
        clip = (Rh @ norm([0, -1.0, 0.25]), H([0, 1.4, 0]))
        b.ell(H([0, 1.2, -0.3]), [6.75, 6.2, 6.6], "cloth_yellow", Rl=Rh, clips=[clip], group="cap")
        b.ell(H([0, 1.9, 4.6]), [4.6, 0.7, 3.0], "cloth_yellow", Rl=Rh, group="cap")
        b.sphere(H([0, 7.0, -0.6]), 1.1, "cloth_yellow", group="cap")
        b.ell(H([0, -2.4, 4.6]), [3.4, 1.4, 1.6], "hair", Rl=Rh, group="beard")

    def body_features(self, b, U, Lw, RU, Rf, pose):
        b.box(U([0, 16.0, 3.9 * self.wide]), [4.2, 6.4, 0.5], "leather", Rl=RU, rnd=0.4, group="apron")
        b.box(Lw([0, 10.4, 4.4]), [4.0, 3.2, 0.45], "leather", Rl=Rf, rnd=0.4, group="apron")


class MinerSpec(hu.Spec):
    S = 1.0
    pads = True
    buckle = False
    mats = dict(skin="skin", shirt="rags", sleeve="rags", forearm="leather_dark", hand="skin", pants="pants",
                shin="leather_dark", boot="leather_dark", belt="leather_dark", pads="leather", neck="skin")

    def head_features(self, b, H, Rh, pose):
        b.ell(H([0, 1.8, 0.0]), [7.0, 5.6, 7.0], "iron", Rl=Rh, clips=[(Rh @ np.array([0, -1.0, 0]), H([0, 0.6, 0]))], group="helmet")
        b.ell(H([0, 0.9, 0.2]), [8.0, 0.8, 8.0], "iron", Rl=Rh, group="helmet")
        b.box(H([0, 3.4, 6.2]), [1.4, 1.5, 0.8], "metal", Rl=Rh, rnd=0.3, group="lamp")
        b.box(H([0, 3.4, 7.0]), [0.9, 0.9, 0.4], "glass_lit", Rl=Rh, group="lamp")
        b.ell(H([0, -3.2, 4.2]), [4.2, 2.6, 2.4], "hair", Rl=Rh, group="beard")
        b.decal(H([1.6, -0.6, 5.9]), "#2a2622", 1.4)


class Villager:
    def __init__(self, spec, tool):
        self.SPEC = spec
        self.ANIMS = {
            "idle": (anim_carry_idle, 5.0, True, tool),
            "walk": (anim_carry_walk, 12.0, True, tool),
            "work": (hu.anim_work, 10.0, True, tool),
        }
        self.shadow = hu.shadow_for(8.0)

    def build(self, pose, d, tool=None):
        return hu.build(pose, d, self.SPEC, tool)


woodcutter = Villager(WoodcutterSpec(), "axe")
stonemason = Villager(StonemasonSpec(), "pickaxe")
miner = Villager(MinerSpec(), "pickaxe")
