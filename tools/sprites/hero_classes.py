import numpy as np
from sdf import norm, rot_axis
from rig import axis_frame
import humanoid as hu
from hero import HeroSpec


def cone_to(b, base, tip, r, mat, group, r2=0.15):
    d = np.asarray(tip, float) - np.asarray(base, float)
    L = np.linalg.norm(d)
    b.cone((np.asarray(base) + np.asarray(tip)) / 2, L / 2, r, r2, mat, Rl=axis_frame(d / L), group=group)


class MageSpec(hu.Spec):
    pads = False
    buckle = False
    mats = dict(skin="skin", shirt="robe", sleeve="robe", forearm="robe", hand="skin", pants="robe",
                shin="robe", boot="leather_dark", belt="robe_trim", pads="robe", neck="skin")

    def head_features(self, b, H, Rh, pose):
        hair_clip = (Rh @ norm([0, -1.0, 1.15]), H([0, 0.3, 1.6]))
        b.ell(H([0, 0.6, -0.9]), [6.7, 6.2, 6.5], "feather", Rl=Rh, clips=[hair_clip], group="head")
        b.ell(H([0, -3.9, 4.0]), [3.6, 3.4, 2.4], "feather", Rl=Rh, group="beard")
        cone_to(b, H([0, -5.5, 4.6]), H([0, -9.2, 4.2]), 2.2, "feather", "beard")
        b.ell(H([0, 4.4, -0.4]), [8.8, 0.75, 8.6], "robe", Rl=Rh, group="hat")
        b.ell(H([0, 5.4, -0.6]), [5.9, 1.2, 5.8], "robe_trim", Rl=Rh, group="hatband")
        Rt = Rh @ rot_axis([1, 0, 0], -0.32)
        top = H([0, 5.6, -0.8]) + Rt @ np.array([0, 9.6, 0])
        b.cone((H([0, 5.6, -0.8]) + top) / 2, 4.8, 5.7, 0.6, "robe", Rl=Rt, group="hat")
        b.sphere(top - Rt @ np.array([0, 0.3, 0]), 0.9, "robe_trim", group="hattip")

    def body_features(self, b, U, Lw, RU, Rf, pose):
        b.cone(Lw([0, 8.4, 0]), 6.2, 6.4, 5.1, "robe", Rl=Rf, group="robe")
        b.ell(Lw([0, 2.6, 0]), [6.5, 0.55, 6.5], "robe_trim", Rl=Rf, group="robe")
        b.ell(U([0, 24.6, 0.4]), [5.6, 1.6, 4.2], "robe_trim", Rl=RU, group="collar")


class DruidSpec(HeroSpec):
    buckle = False
    mats = dict(skin="skin", shirt="leather", sleeve="cloak_green", forearm="leather_dark", hand="skin", pants="leather_dark",
                shin="leather_dark", boot="leather_dark", belt="rope", pads="leaf", neck="skin")

    def head_features(self, b, H, Rh, pose):
        clip = (Rh @ norm([0, -0.55, 1.0]), H([0, 1.6, 2.6]))
        b.ell(H([0, 1.4, -0.9]), [7.1, 7.2, 7.2], "cloak_green", Rl=Rh, clips=[clip], group="hood")
        b.ell(H([0, 3.6, 3.2]), [4.6, 1.6, 2.2], "hair", Rl=Rh, group="fringe")
        for sx in (1, -1):
            base = H([sx * 3.4, 5.6, -1.4])
            mid = H([sx * 6.0, 9.6, -2.4])
            tip = H([sx * 7.6, 13.4, -3.2])
            b.cap(base, mid, 0.85, "antler", r2=0.7, group="antler%d" % sx)
            b.cap(mid, tip, 0.7, "antler", r2=0.35, group="antler%d" % sx)
            b.cap(mid, H([sx * 4.8, 13.0, -0.6]), 0.55, "antler", r2=0.3, group="antler%d" % sx)
            b.cap(H([sx * 6.9, 11.6, -2.8]), H([sx * 9.8, 12.4, -2.0]), 0.5, "antler", r2=0.28, group="antler%d" % sx)

    def body_features(self, b, U, Lw, RU, Rf, pose):
        for k, (x, y, z, a) in enumerate(((-2.6, 22.0, 3.7, 0.4), (0.8, 21.0, 3.9, -0.3), (2.9, 23.0, 3.4, 0.9))):
            b.ell(U([x, y, z]), [1.4, 0.5, 0.9], "leaf", Rl=RU @ rot_axis([0, 0, 1], a), group="leafchest%d" % k)

    def back_features(self, b, U, RU, pose):
        b.ell(U([0, 25.4, -0.3]), [5.0, 1.8, 4.1], "cloak_green", Rl=RU, group="torso")
        cape_top = U([0, 24.6, -2.9])
        cdir = RU @ norm([0, -1, -pose.cape])
        cape_len = 15.0
        cape_c = cape_top + cdir * cape_len / 2 + RU @ np.array([0, 0, 2.2])
        Rc = axis_frame(-cdir, RU @ np.array([1.0, 0, 0]))
        back = (RU @ np.array([0, 0, 1.0]), U([0, 20, -1.4]))
        b.cone(cape_c, cape_len / 2, 7.0, 4.4, "cloak_green", Rl=Rc, shell=0.45, clips=[back], group="cape")


class HeroClass:
    def __init__(self, spec, weapon):
        self.SPEC = spec
        self.shadow = hu.shadow_for(9.0)
        self.ANIMS = {
            "idle": (lambda: hu.anim_idle(tool_dir=hu.STAFF_DIR), 5.0, True, weapon),
            "walk": (lambda: hu.anim_walk(tool_dir=hu.STAFF_DIR), 13.0, True, weapon),
            "attack": (hu.anim_slash, 14.0, False, weapon),
            "cast": (hu.anim_cast, 10.0, False, weapon),
            "work": (hu.anim_work, 11.0, True, "pickaxe"),
            "death": (hu.anim_death, 8.0, False, weapon),
        }

    def build(self, pose, d, tool=None):
        return hu.build(pose, d, self.SPEC, tool)

    def trail(self, an, i, d):
        return hu.slash_trail(an, i, d, lambda pose, dd: self.build(pose, dd, "staff"))


mage = HeroClass(MageSpec(), "staff")
druid = HeroClass(DruidSpec(), "druid_staff")
