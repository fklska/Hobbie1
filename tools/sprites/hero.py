import numpy as np
from sdf import norm
from rig import axis_frame
import humanoid as hu


class HeroSpec(hu.Spec):
    def head_features(self, b, H, Rh, pose):
        hair_clip = (Rh @ norm([0, -1.0, 1.15]), H([0, 0.3, 1.6]))
        b.ell(H([0, 1.1, -0.7]), [6.8, 6.5, 6.6], "hair", Rl=Rh, clips=[hair_clip], group="head")
        b.ell(H([0.6, 4.4, 3.6]), [5.0, 2.0, 2.0], "hair", Rl=Rh, group="head")
        b.ell(H([-3.6, 3.2, 3.2]), [2.0, 2.4, 2.0], "hair", Rl=Rh, group="head")

    def back_features(self, b, U, RU, pose):
        b.ell(U([0, 25.4, -0.3]), [4.7, 1.7, 3.9], "cape", Rl=RU, group="torso")
        cape_top = U([0, 24.6, -2.9])
        cdir = RU @ norm([0, -1, -pose.cape])
        cape_len = 12.5
        cape_c = cape_top + cdir * cape_len / 2 + RU @ np.array([0, 0, 2.2])
        Rc = axis_frame(-cdir, RU @ np.array([1.0, 0, 0]))
        back = (RU @ np.array([0, 0, 1.0]), U([0, 20, -1.4]))
        b.cone(cape_c, cape_len / 2, 6.6, 4.3, "cape", Rl=Rc, shell=0.45, clips=[back], group="cape")


SPEC = HeroSpec()
shadow = hu.shadow_for(9.0)


def build(pose, d, tool="sword"):
    return hu.build(pose, d, SPEC, tool)


def trail(an, i, d):
    return hu.slash_trail(an, i, d, build)


ANIMS = {
    "idle": (hu.anim_idle, 5.0, True, "sword"),
    "walk": (hu.anim_walk, 13.0, True, "sword"),
    "attack": (hu.anim_slash, 14.0, False, "sword"),
    "work": (hu.anim_work, 11.0, True, "pickaxe"),
    "death": (hu.anim_death, 8.0, False, "sword"),
}
