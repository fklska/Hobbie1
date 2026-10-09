import numpy as np
from sdf import Prim, norm, rot_axis, frame_from, UP

DIRS = ["e", "se", "s", "sw", "w", "nw", "n", "ne"]


def facing(i):
    a = np.radians(45.0 * i)
    return np.array([np.cos(a), 0.0, np.sin(a)])


def ik2(a, t, l1, l2, pole):
    a = np.asarray(a, float)
    t = np.asarray(t, float)
    v = t - a
    d = np.linalg.norm(v)
    u = v / max(d, 1e-6)
    d = np.clip(d, abs(l1 - l2) + 1e-3, l1 + l2 - 1e-3)
    x = (l1 * l1 - l2 * l2 + d * d) / (2 * d)
    h = np.sqrt(max(l1 * l1 - x * x, 0.0))
    pole = np.asarray(pole, float)
    pp = pole - (pole @ u) * u
    pp = norm(pp) if np.linalg.norm(pp) > 1e-6 else norm(np.cross(u, [1, 0, 0]))
    return a + u * x + pp * h, a + u * d


class Body:
    """Builds world-space prims from a local-space description.

    Local axes: x = character right, y = up, z = forward."""

    def __init__(self, dir_index, scale=1.0, origin=(0, 0, 0)):
        self.M = frame_from(facing(dir_index))
        self.s = scale
        self.o = np.asarray(origin, float)
        self.prims = []
        self.decals = []

    def w(self, p):
        return self.o + self.M @ (np.asarray(p, float) * self.s)

    def wd(self, v):
        return self.M @ np.asarray(v, float)

    def wR(self, Rl=None):
        return self.M if Rl is None else self.M @ Rl

    def sphere(self, c, r, mat, **kw):
        self.prims.append(Prim("sphere", mat, c=self.w(c), r=r * self.s, **kw))

    def ell(self, c, r, mat, Rl=None, clips=(), **kw):
        cl = [(self.wd(n), self.w(o)) for n, o in clips]
        self.prims.append(Prim("ellipsoid", mat, c=self.w(c), r=np.asarray(r, float) * self.s, R=self.wR(Rl), clips=cl, **kw))

    def cap(self, a, b, r, mat, r2=None, **kw):
        self.prims.append(Prim("capsule", mat, a=self.w(a), b=self.w(b), r=r * self.s, r2=(r2 if r2 is not None else r) * self.s, **kw))

    def box(self, c, h, mat, Rl=None, rnd=0.0, clips=(), **kw):
        cl = [(self.wd(n), self.w(o)) for n, o in clips]
        self.prims.append(Prim("box", mat, c=self.w(c), h=np.asarray(h, float) * self.s, R=self.wR(Rl), round=rnd * self.s, clips=cl, **kw))

    def cyl(self, c, r, h, mat, Rl=None, rnd=0.0, **kw):
        self.prims.append(Prim("cyl", mat, c=self.w(c), r=r * self.s, h=h * self.s, R=self.wR(Rl), round=rnd * self.s, **kw))

    def cone(self, c, h, r1, r2, mat, Rl=None, clips=(), **kw):
        cl = [(self.wd(n), self.w(o)) for n, o in clips]
        if "shell" in kw:
            kw["shell"] *= self.s
        self.prims.append(Prim("cone", mat, c=self.w(c), h=h * self.s, r1=r1 * self.s, r2=r2 * self.s, R=self.wR(Rl), clips=cl, **kw))

    def decal(self, p, color, tol=2.0):
        self.decals.append((self.w(p), color, tol * self.s))


def axis_frame(y_axis, x_hint=(1, 0, 0)):
    y = norm(y_axis)
    x = np.asarray(x_hint, float)
    x = x - (x @ y) * y
    if np.linalg.norm(x) < 1e-6:
        x = np.cross(y, [0, 0, 1])
    x = norm(x)
    z = np.cross(x, y)
    return np.stack([x, y, z], axis=1)


class Pose:
    def __init__(self, **kw):
        self.root = np.zeros(3)
        self.lean = 0.0
        self.twist = 0.0
        self.side = 0.0
        self.head_tilt = 0.0
        self.feet = None
        self.hands = None
        self.tool_dir = None
        self.cape = 0.25
        self.fall = 0.0
        self.fall_axis = (1, 0, 0)
        self.extra = {}
        for k, v in kw.items():
            setattr(self, k, v)


def upper_xf(pose, pivot):
    pivot = np.asarray(pivot, float)
    Rl = rot_axis([0, 1, 0], pose.twist) @ rot_axis([1, 0, 0], pose.lean) @ rot_axis([0, 0, 1], pose.side)

    def f(p):
        return pivot + Rl @ (np.asarray(p, float) - pivot) + pose.root
    return f, Rl


def fall_xf(pose):
    if pose.fall == 0:
        return lambda p: np.asarray(p, float), np.eye(3)
    Rf = rot_axis(pose.fall_axis, pose.fall)

    def f(p):
        return Rf @ np.asarray(p, float)
    return f, Rf
