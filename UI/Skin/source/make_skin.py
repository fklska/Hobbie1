import os
import random
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICONS = os.path.join(os.path.dirname(ROOT), "Icons")
SCALE = 2

OUTLINE = (28, 19, 15, 255)
TRIM = (201, 154, 69, 255)
TRIM_HI = (246, 210, 120, 255)
TRIM_LO = (128, 90, 38, 255)
PANEL = (44, 34, 28, 255)
PANEL_LO = (34, 26, 21, 255)
PANEL_HI = (58, 45, 36, 255)
INSET = (24, 18, 15, 255)
INSET_LO = (14, 10, 8, 255)
INSET_HI = (66, 51, 40, 255)
PARCH = (233, 216, 176, 255)
PARCH_LO = (205, 182, 136, 255)
PARCH_EDGE = (120, 86, 52, 255)
CLEAR = (0, 0, 0, 0)

BUTTONS = {
    "wood": dict(body=(112, 78, 52), hi=(150, 108, 72), lo=(66, 45, 31), hover=(132, 93, 62), hover_hi=(176, 130, 86), press=(88, 61, 41)),
    "green": dict(body=(70, 124, 62), hi=(110, 170, 90), lo=(36, 70, 36), hover=(84, 146, 72), hover_hi=(134, 196, 108), press=(56, 100, 50)),
    "red": dict(body=(150, 58, 46), hi=(198, 96, 74), lo=(86, 30, 26), hover=(172, 70, 54), hover_hi=(222, 120, 92), press=(122, 46, 38)),
}
DISABLED = dict(body=(78, 70, 64), hi=(98, 90, 82), lo=(48, 42, 38))
FILLS = {
    "gold": ((240, 196, 92), (255, 232, 150), (168, 120, 40)),
    "green": ((92, 168, 74), (150, 210, 120), (48, 96, 44)),
    "red": ((206, 64, 54), (244, 128, 108), (122, 30, 28)),
    "blue": ((70, 128, 196), (128, 182, 236), (36, 68, 120)),
}


def rgba(c, a=255):
    return (c[0], c[1], c[2], a)


def save(img, name, folder=ROOT, scale=SCALE):
    img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    img.save(os.path.join(folder, name + ".png"))


def ring(img, d, color, sides="tlbr", round_corner=False):
    w, h = img.size
    px = img.load()
    for x in range(d, w - d):
        for y in range(d, h - d):
            on_t, on_b = y == d, y == h - 1 - d
            on_l, on_r = x == d, x == w - 1 - d
            if not (on_t or on_b or on_l or on_r):
                continue
            if round_corner and (on_t or on_b) and (on_l or on_r):
                continue
            if (on_t and "t" in sides) or (on_b and "b" in sides) or (on_l and "l" in sides) or (on_r and "r" in sides):
                px[x, y] = color


def fill(img, d, color, noise=0, seed=1):
    w, h = img.size
    px = img.load()
    rnd = random.Random(seed)
    for x in range(d, w - d):
        for y in range(d, h - d):
            n = rnd.randint(-noise, noise) if noise else 0
            px[x, y] = (max(0, color[0] + n), max(0, color[1] + n), max(0, color[2] + n), color[3])


def panel(name, size=24, fill_color=PANEL, alpha=255):
    img = Image.new("RGBA", (size, size), CLEAR)
    fill(img, 3, rgba(fill_color, alpha), noise=2 if alpha == 255 else 0)
    ring(img, 0, OUTLINE, round_corner=True)
    ring(img, 1, TRIM_LO)
    ring(img, 1, TRIM_HI, sides="tl")
    ring(img, 2, OUTLINE)
    ring(img, 3, rgba(PANEL_LO, alpha), sides="t")
    px = img.load()
    for cx, cy in ((4, 4), (size - 5, 4), (4, size - 5), (size - 5, size - 5)):
        px[cx, cy] = TRIM
    save(img, name)


def parchment(name, size=16):
    img = Image.new("RGBA", (size, size), CLEAR)
    fill(img, 1, PARCH, noise=3, seed=7)
    ring(img, 1, PARCH_LO)
    ring(img, 0, PARCH_EDGE, round_corner=True)
    save(img, name)


def button(name, pal, state):
    img = Image.new("RGBA", (12, 13), CLEAR)
    px = img.load()
    w, h = img.size
    if state == "disabled":
        pal = dict(body=DISABLED["body"], hi=DISABLED["hi"], lo=DISABLED["lo"])
        body, hi, lip = pal["body"], pal["hi"], 2
    elif state == "hover":
        body, hi, lip = pal["hover"], pal["hover_hi"], 2
    elif state == "pressed":
        body, hi, lip = pal["press"], pal["body"], 0
    else:
        body, hi, lip = pal["body"], pal["hi"], 2
    top = 1 if state == "pressed" else 0
    for x in range(w):
        for y in range(top, h - (0 if lip else 1)):
            px[x, y] = rgba(body)
    for x in range(w):
        px[x, top + 1] = rgba(hi)
        for k in range(lip):
            px[x, h - 2 - k] = rgba(pal["lo"])
    for x in range(w):
        px[x, top] = OUTLINE
        px[x, h - 1 - (0 if lip else 1)] = OUTLINE
    for y in range(top, h - (0 if lip else 1)):
        px[0, y] = OUTLINE
        px[w - 1, y] = OUTLINE
    bottom = h - 1 - (0 if lip else 1)
    for cx, cy in ((0, top), (w - 1, top), (0, bottom), (w - 1, bottom)):
        px[cx, cy] = CLEAR
    if state == "hover":
        for x in range(1, w - 1):
            px[x, top] = TRIM
        for y in range(top + 1, bottom):
            px[0, y] = TRIM
            px[w - 1, y] = TRIM
        for x in range(1, w - 1):
            px[x, bottom] = TRIM_LO
    if state == "pressed":
        for x in range(1, w - 1):
            px[x, top + 1] = rgba(pal["lo"])
    save(img, name)


def focus():
    img = Image.new("RGBA", (12, 13), CLEAR)
    ring(img, 0, TRIM_HI, round_corner=True)
    save(img, "focus")


def inset(name, size=12, border=OUTLINE, inner=None, fill_color=INSET, alpha=255):
    img = Image.new("RGBA", (size, size), CLEAR)
    fill(img, 1, rgba(fill_color, alpha))
    ring(img, 1, rgba(INSET_LO, alpha), sides="tl")
    ring(img, 1, rgba(INSET_HI, alpha), sides="br")
    ring(img, 0, border, round_corner=True)
    if inner:
        ring(img, 1, inner)
    save(img, name)


def slot_selected():
    img = Image.new("RGBA", (14, 14), CLEAR)
    fill(img, 2, INSET)
    ring(img, 2, INSET_LO, sides="tl")
    ring(img, 0, OUTLINE, round_corner=True)
    ring(img, 1, TRIM_HI)
    ring(img, 2, TRIM_LO, sides="br")
    save(img, "slot_selected")


def bar_fill(name, colors):
    body, hi, lo = colors
    img = Image.new("RGBA", (6, 6), rgba(body))
    px = img.load()
    for x in range(6):
        px[x, 0] = rgba(hi)
        px[x, 5] = rgba(lo)
        px[x, 4] = rgba(tuple((a + b) // 2 for a, b in zip(body, lo)))
    save(img, name)


def grabber(name, body, hi, lo):
    img = Image.new("RGBA", (7, 10), CLEAR)
    px = img.load()
    for x in range(7):
        for y in range(10):
            px[x, y] = OUTLINE
    for x in range(1, 6):
        for y in range(1, 9):
            px[x, y] = rgba(body)
    for y in range(1, 8):
        px[1, y] = rgba(hi)
    for x in range(1, 6):
        px[x, 1] = rgba(hi)
        px[x, 8] = rgba(lo)
    for y in range(2, 9):
        px[5, y] = rgba(lo)
    px[3, 4] = rgba(lo)
    px[3, 5] = rgba(lo)
    for cx, cy in ((0, 0), (6, 0), (0, 9), (6, 9)):
        px[cx, cy] = CLEAR
    save(img, name)


def checkbox(name, checked, disabled=False):
    img = Image.new("RGBA", (10, 10), CLEAR)
    fill(img, 1, INSET)
    ring(img, 1, INSET_LO, sides="tl")
    ring(img, 0, TRIM_LO if not disabled else (70, 62, 56, 255), round_corner=True)
    if checked:
        px = img.load()
        mark = [(2, 5), (3, 6), (4, 7), (5, 6), (6, 5), (7, 4), (7, 3), (3, 5), (4, 6), (5, 5), (6, 4)]
        for x, y in mark:
            px[x, y] = TRIM_HI if not disabled else (140, 130, 120, 255)
    save(img, name)


def toggle(name, on, disabled=False):
    img = Image.new("RGBA", (18, 10), CLEAR)
    track = (70, 124, 62) if on else (60, 48, 40)
    if disabled:
        track = (70, 64, 58)
    fill(img, 1, rgba(track))
    ring(img, 1, rgba(tuple(max(0, c - 30) for c in track)), sides="t")
    ring(img, 0, OUTLINE, round_corner=True)
    px = img.load()
    kx = 10 if on else 2
    for x in range(kx, kx + 6):
        for y in range(2, 8):
            px[x, y] = TRIM if not disabled else (120, 112, 104, 255)
    for x in range(kx, kx + 6):
        px[x, 2] = TRIM_HI if not disabled else (150, 140, 130, 255)
        px[x, 7] = TRIM_LO if not disabled else (90, 84, 78, 255)
    save(img, name)


def arrow(name, direction, color=TRIM_HI):
    img = Image.new("RGBA", (7, 7), CLEAR)
    px = img.load()
    rows = [(1, 5), (2, 4), (3, 3)]
    for i, (a, b) in enumerate(rows):
        for x in range(a, b + 1):
            y = 2 + i
            if direction == "down":
                px[x, y] = color
            elif direction == "up":
                px[x, 6 - y] = color
            elif direction == "right":
                px[y, x] = color
            elif direction == "left":
                px[6 - y, x] = color
    save(img, name)


def tab(name, body, hi, selected):
    img = Image.new("RGBA", (12, 10), CLEAR)
    px = img.load()
    w, h = img.size
    for x in range(w):
        for y in range(h):
            px[x, y] = rgba(body)
    for x in range(w):
        px[x, 0] = OUTLINE
        px[x, 1] = TRIM_HI if selected else rgba(hi)
    for y in range(h):
        px[0, y] = OUTLINE
        px[w - 1, y] = OUTLINE
    px[0, 0] = CLEAR
    px[w - 1, 0] = CLEAR
    if not selected:
        for x in range(w):
            px[x, h - 1] = OUTLINE
    save(img, name)


def scroll(name, vertical, kind):
    img = Image.new("RGBA", (6, 6), CLEAR)
    if kind == "track":
        fill(img, 1, INSET)
        ring(img, 0, rgba(INSET_LO, 255), round_corner=True)
    else:
        col = {"grab": (128, 92, 60), "hover": (160, 118, 78), "press": (190, 146, 96)}[kind]
        fill(img, 1, rgba(col))
        ring(img, 1, rgba(tuple(min(255, c + 30) for c in col)), sides="tl")
        ring(img, 0, OUTLINE, round_corner=True)
    save(img, name)


def ribbon():
    img = Image.new("RGBA", (32, 12), CLEAR)
    px = img.load()
    body, hi, lo, end = (150, 52, 44), (196, 86, 70), (96, 30, 26), (118, 38, 32)
    for x in range(4, 28):
        for y in range(1, 11):
            px[x, y] = rgba(body)
        px[x, 1] = OUTLINE
        px[x, 2] = rgba(hi)
        px[x, 9] = rgba(lo)
        px[x, 10] = OUTLINE
    for side in (0, 1):
        for i in range(5):
            x = i if side == 0 else 31 - i
            for y in range(3, 12):
                notch = abs(y - 7) < (2 - abs(i - 2) // 2) and i < 2
                if not notch:
                    px[x, y] = rgba(end)
            px[x, 3] = OUTLINE
            px[x, 11] = OUTLINE
        x0 = 0 if side == 0 else 31
        for y in range(3, 12):
            if px[x0, y] != CLEAR:
                px[x0, y] = OUTLINE
        xe = 4 if side == 0 else 27
        for y in range(1, 11):
            px[xe, y] = OUTLINE
    save(img, "ribbon")


def hud_panel():
    img = Image.new("RGBA", (10, 10), CLEAR)
    fill(img, 1, (22, 16, 13, 205))
    ring(img, 1, (70, 54, 40, 220), sides="t")
    ring(img, 0, (12, 8, 6, 235), round_corner=True)
    save(img, "hud_panel")


def separator():
    img = Image.new("RGBA", (8, 3), CLEAR)
    px = img.load()
    for x in range(8):
        px[x, 0] = OUTLINE
        px[x, 1] = TRIM_LO
        px[x, 2] = OUTLINE
    save(img, "separator")


def build_skin():
    panel("panel")
    panel("panel_hud", fill_color=(22, 16, 13), alpha=215)
    parchment("parchment")
    for kind, pal in BUTTONS.items():
        prefix = "button" if kind == "wood" else f"button_{kind}"
        for state in ("normal", "hover", "pressed", "disabled"):
            button(f"{prefix}_{state}", pal, state)
    focus()
    inset("field")
    inset("field_focus", border=TRIM)
    inset("slot", size=14)
    inset("slot_hover", size=14, border=TRIM)
    slot_selected()
    for kind, colors in FILLS.items():
        bar_fill(f"fill_{kind}", colors)
    grabber("grabber", TRIM[:3], TRIM_HI[:3], TRIM_LO[:3])
    grabber("grabber_hover", TRIM_HI[:3], (255, 240, 180), TRIM[:3])
    grabber("grabber_disabled", (110, 100, 92), (140, 130, 120), (80, 72, 66))
    checkbox("check_off", False)
    checkbox("check_on", True)
    checkbox("check_off_disabled", False, True)
    checkbox("check_on_disabled", True, True)
    toggle("toggle_off", False)
    toggle("toggle_on", True)
    toggle("toggle_off_disabled", False, True)
    toggle("toggle_on_disabled", True, True)
    for d in ("down", "up", "left", "right"):
        arrow(f"arrow_{d}", d)
    tab("tab_selected", PANEL[:3], PANEL_HI[:3], True)
    tab("tab_unselected", (30, 23, 19), (48, 37, 30), False)
    tab("tab_hover", (52, 40, 32), (90, 70, 50), False)
    for kind in ("track", "grab", "hover", "press"):
        scroll(f"scroll_{kind}", True, kind)
    ribbon()
    hud_panel()
    separator()


ICON_OUTLINE = (30, 20, 16, 255)


def outlined(img):
    w, h = img.size
    src = img.load()
    out = img.copy()
    px = out.load()
    for x in range(w):
        for y in range(h):
            if src[x, y][3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3] != 0:
                    px[x, y] = ICON_OUTLINE
                    break
    return out


def canvas():
    img = Image.new("RGBA", (16, 16), CLEAR)
    return img, ImageDraw.Draw(img), img.load()


def icon(name, img):
    save(outlined(img), name, ICONS)


def icon_wood():
    img, d, px = canvas()
    d.rectangle((1, 6, 11, 11), fill=(124, 82, 48))
    d.line((1, 6, 11, 6), fill=(160, 112, 66))
    d.line((1, 7, 11, 7), fill=(146, 100, 58))
    d.line((1, 11, 11, 11), fill=(88, 56, 32))
    for x, y in ((3, 8), (4, 8), (7, 9), (8, 9), (9, 9), (2, 10), (5, 10)):
        px[x, y] = (96, 62, 36, 255)
    d.ellipse((9, 5, 14, 12), fill=(206, 160, 100))
    d.ellipse((10, 6, 13, 11), fill=(232, 192, 132))
    d.point((11, 8), fill=(170, 120, 66))
    d.point((12, 9), fill=(170, 120, 66))
    for x, y in ((9, 6), (9, 11)):
        px[x, y] = (186, 138, 82, 255)
    px[5, 4] = (88, 140, 60, 255)
    px[6, 5] = (88, 140, 60, 255)
    px[4, 5] = (118, 176, 76, 255)
    icon("wood", img)


def icon_stone():
    img, d, px = canvas()
    d.polygon([(2, 11), (2, 8), (4, 5), (7, 3), (11, 4), (13, 7), (14, 11), (11, 13), (5, 13)], fill=(124, 126, 136))
    d.polygon([(3, 8), (5, 5), (8, 4), (8, 7), (5, 9)], fill=(168, 170, 180))
    d.polygon([(9, 8), (13, 8), (13, 11), (10, 12)], fill=(96, 98, 110))
    d.line((5, 12, 10, 12), fill=(92, 94, 106))
    px[6, 5] = (204, 206, 214, 255)
    px[11, 6] = (150, 152, 162, 255)
    icon("stone", img)


def ingot(top, front, side, shine):
    img, d, px = canvas()
    d.polygon([(4, 5), (12, 5), (14, 9), (2, 9)], fill=top)
    d.rectangle((2, 9, 14, 12), fill=front)
    d.polygon([(12, 5), (14, 9), (14, 12)], fill=side)
    d.line((5, 6, 10, 6), fill=shine)
    d.point((3, 10), fill=shine)
    d.line((2, 12, 14, 12), fill=side)
    return img


def icon_metals():
    icon("iron", ingot((176, 188, 202), (118, 130, 148), (84, 94, 112), (226, 234, 242)))
    icon("gold", ingot((252, 214, 98), (214, 158, 46), (158, 106, 26), (255, 244, 182)))


def icon_coins():
    img, d, px = canvas()
    for i, (x, y) in enumerate(((1, 9), (6, 7), (3, 3))):
        d.ellipse((x, y, x + 8, y + 6), fill=(196, 140, 36))
        d.ellipse((x, y, x + 8, y + 5), fill=(244, 194, 72))
        d.ellipse((x + 2, y + 1, x + 6, y + 4), fill=(222, 166, 52))
        d.point((x + 2, y + 1), fill=(255, 240, 170))
        d.point((x + 3, y + 1), fill=(255, 240, 170))
    icon("coins", img)


def icon_food():
    img, d, px = canvas()
    d.ellipse((1, 5, 14, 13), fill=(178, 112, 52))
    d.ellipse((1, 4, 14, 11), fill=(214, 150, 74))
    d.ellipse((3, 5, 9, 8), fill=(236, 184, 108))
    for x0 in (4, 7, 10):
        d.line((x0, 9, x0 + 2, 6), fill=(160, 98, 44))
    icon("food", img)


def icon_heart():
    img, d, px = canvas()
    d.ellipse((1, 2, 8, 9), fill=(214, 52, 52))
    d.ellipse((7, 2, 14, 9), fill=(214, 52, 52))
    d.polygon([(1, 6), (14, 6), (8, 14), (7, 14)], fill=(214, 52, 52))
    d.polygon([(8, 13), (14, 6), (14, 8), (9, 13)], fill=(158, 28, 38))
    d.point((3, 4), fill=(255, 150, 150))
    d.point((4, 4), fill=(255, 150, 150))
    d.point((3, 5), fill=(255, 150, 150))
    icon("heart", img)


def blade(px, x0, y0, x1, color, edge):
    for i, x in enumerate(range(x0, x1 + 1)):
        y = y0 - i
        px[x, y] = color
        if y + 1 < 16:
            px[x, y + 1] = edge


def icon_sword():
    img, d, px = canvas()
    blade(px, 6, 9, 13, (214, 222, 232, 255), (140, 150, 166, 255))
    px[14, 1] = (214, 222, 232, 255)
    for x, y in ((3, 8), (4, 9), (5, 10), (6, 11), (7, 12)):
        px[x, y] = (226, 172, 60, 255)
    for x, y in ((3, 12), (4, 11)):
        px[x, y] = (110, 70, 40, 255)
    px[2, 13] = (226, 172, 60, 255)
    icon("sword", img)


def icon_worker():
    img, d, px = canvas()
    d.rectangle((3, 10, 12, 15), fill=(94, 128, 70))
    d.rectangle((3, 10, 12, 10), fill=(120, 158, 88))
    d.line((7, 11, 8, 11), fill=(70, 98, 52))
    d.ellipse((4, 2, 11, 9), fill=(236, 190, 146))
    d.rectangle((4, 2, 11, 4), fill=(122, 78, 44))
    d.point((4, 5), fill=(122, 78, 44))
    d.point((11, 5), fill=(122, 78, 44))
    d.point((6, 6), fill=(60, 40, 30))
    d.point((9, 6), fill=(60, 40, 30))
    icon("worker", img)


def icon_skull():
    img, d, px = canvas()
    d.ellipse((2, 1, 13, 11), fill=(232, 226, 208))
    d.rectangle((5, 10, 10, 13), fill=(232, 226, 208))
    d.rectangle((4, 5, 6, 7), fill=(40, 28, 24))
    d.rectangle((9, 5, 11, 7), fill=(40, 28, 24))
    d.point((7, 9), fill=(40, 28, 24))
    d.point((8, 9), fill=(40, 28, 24))
    for x in (6, 8):
        d.line((x, 12, x, 13), fill=(160, 150, 130))
    d.line((3, 9, 3, 6), fill=(196, 188, 168))
    icon("skull", img)


def icon_sun():
    img, d, px = canvas()
    for x, y in ((7, 0), (8, 0), (7, 15), (8, 15), (0, 7), (0, 8), (15, 7), (15, 8), (2, 2), (13, 2), (2, 13), (13, 13)):
        px[x, y] = (250, 200, 70, 255)
    d.ellipse((3, 3, 12, 12), fill=(250, 200, 70))
    d.ellipse((4, 4, 10, 10), fill=(255, 228, 120))
    icon("sun", img)


def icon_moon():
    img, d, px = canvas()
    d.ellipse((2, 2, 13, 13), fill=(214, 220, 240))
    d.ellipse((6, 0, 16, 10), fill=CLEAR)
    px[4, 9] = (170, 178, 206, 255)
    px[5, 11] = (170, 178, 206, 255)
    icon("moon", img)


def icon_hammer():
    img, d, px = canvas()
    for i in range(9):
        px[3 + i, 13 - i] = (132, 90, 52, 255)
        px[4 + i, 13 - i] = (100, 66, 38, 255)
    d.polygon([(7, 3), (10, 0), (15, 5), (12, 8)], fill=(150, 160, 176))
    d.line((8, 3, 10, 1), fill=(206, 214, 226))
    icon("hammer", img)


def icon_house():
    img, d, px = canvas()
    d.rectangle((3, 8, 12, 14), fill=(196, 160, 112))
    d.polygon([(1, 8), (8, 2), (14, 8)], fill=(170, 64, 50))
    d.line((2, 8, 13, 8), fill=(124, 44, 36))
    d.rectangle((7, 10, 9, 14), fill=(110, 72, 40))
    d.rectangle((4, 9, 5, 10), fill=(250, 214, 120))
    icon("house", img)


def icon_shield():
    img, d, px = canvas()
    d.polygon([(2, 2), (13, 2), (13, 8), (8, 14), (7, 14), (2, 8)], fill=(150, 58, 46))
    d.polygon([(7, 2), (8, 2), (8, 13), (7, 13)], fill=(226, 172, 60))
    d.line((2, 6, 13, 6), fill=(226, 172, 60))
    d.line((3, 3, 6, 3), fill=(196, 96, 76))
    icon("shield", img)


def icon_artifact():
    img, d, px = canvas()
    d.polygon([(8, 1), (13, 6), (8, 14), (3, 6)], fill=(116, 84, 196))
    d.polygon([(8, 1), (8, 14), (3, 6)], fill=(150, 120, 230))
    d.line((3, 6, 13, 6), fill=(196, 176, 255))
    d.point((6, 4), fill=(240, 232, 255))
    icon("artifact", img)


def build_icons():
    os.makedirs(ICONS, exist_ok=True)
    icon_wood()
    icon_stone()
    icon_metals()
    icon_coins()
    icon_food()
    icon_heart()
    icon_sword()
    icon_worker()
    icon_skull()
    icon_sun()
    icon_moon()
    icon_hammer()
    icon_house()
    icon_shield()
    icon_artifact()


WOOD_HANDLE = ((140, 96, 56, 255), (100, 66, 38, 255))
STEEL = ((206, 214, 226, 255), (130, 140, 158, 255))


def diag(px, x0, y0, length, colors):
    for i in range(length):
        px[x0 + i, y0 - i] = colors[0]
        if y0 - i + 1 < 16:
            px[x0 + i, y0 - i + 1] = colors[1]









def icon_axe():
    img, d, px = canvas()
    diag(px, 2, 14, 10, WOOD_HANDLE)
    d.polygon([(8, 2), (12, 1), (14, 5), (11, 8), (9, 6)], fill=STEEL[0])
    d.line((12, 2, 14, 5), fill=(240, 244, 250))
    d.line((9, 6, 11, 8), fill=STEEL[1])
    icon("tool_axe", img)


def icon_pickaxe():
    img, d, px = canvas()
    diag(px, 2, 14, 10, WOOD_HANDLE)
    d.arc((3, 0, 15, 10), 200, 340, fill=STEEL[0])
    d.arc((3, 1, 15, 11), 200, 340, fill=STEEL[1])
    px[4, 4] = STEEL[0]
    px[14, 4] = STEEL[0]
    icon("tool_pickaxe", img)


def build_tool_icons():
    icon_axe()
    icon_pickaxe()


if __name__ == "__main__":
    build_skin()
    build_icons()
    build_tool_icons()
