import hashlib
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DIRS = ["e", "se", "s", "sw", "w", "nw", "n", "ne"]

IMPORT_PARAMS = """compress/mode=0
compress/high_quality=false
compress/lossy_quality=0.7
compress/hdr_compression=1
compress/normal_map=0
compress/channel_pack=0
mipmaps/generate=false
mipmaps/limit=-1
roughness/mode=0
roughness/src_normal=""
process/fix_alpha_border=true
process/premult_alpha=false
process/normal_map_invert_y=false
process/hdr_as_srgb=false
process/hdr_clamp_exposure=false
process/size_limit=0
detect_3d/compress_to=1
"""


def uid_for(key):
    n = int(hashlib.sha256(key.encode()).hexdigest()[:16], 16) & 0x7FFFFFFFFFFFFFFF
    chars = "abcdefghijklmnopqrstuvwxyz0123456789"
    s = ""
    while True:
        s = chars[n % 36] + s
        n //= 36
        if n == 0:
            break
    return "uid://" + s


def res_path(rel):
    return "res://" + rel.replace(os.sep, "/")


def save_png(img, rel):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if isinstance(img, np.ndarray):
        img = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGBA")
    img.save(path, optimize=True)
    rp = res_path(rel)
    uid = uid_for(rp)
    base = os.path.basename(rel)
    md5 = hashlib.md5(rp.encode()).hexdigest()
    imp = f"""[remap]

importer="texture"
type="CompressedTexture2D"
uid="{uid}"
path="res://.godot/imported/{base}-{md5}.ctex"
metadata={{
"vram_texture": false
}}

[deps]

source_file="{rp}"
dest_files=["res://.godot/imported/{base}-{md5}.ctex"]

[params]

{IMPORT_PARAMS}"""
    imp_path = path + ".import"
    if os.path.exists(imp_path):
        old = open(imp_path).read()
        if f'uid="{uid}"' in old:
            return uid
    open(imp_path, "w").write(imp)
    return uid


def sheet_from(frames_by_row):
    h, w = frames_by_row[0][0].shape[:2]
    cols = max(len(r) for r in frames_by_row)
    out = np.zeros((h * len(frames_by_row), w * cols, 4))
    for i, row in enumerate(frames_by_row):
        for j, f in enumerate(row):
            out[i * h:(i + 1) * h, j * w:(j + 1) * w] = f
    return out


def write_sprite_frames(rel, anims):
    """anims: list of dicts {name, sheet_rel, uid, rects: [(x,y,w,h)], fps, loop}"""
    rp = res_path(rel)
    uid = uid_for(rp)
    sheets = []
    for a in anims:
        if a["sheet_rel"] not in [s[0] for s in sheets]:
            sheets.append((a["sheet_rel"], a["uid"]))
    lines = [f'[gd_resource type="SpriteFrames" format=3 uid="{uid}"]', ""]
    ids = {}
    for k, (srel, suid) in enumerate(sheets):
        rid = f"{k + 1}_{os.path.splitext(os.path.basename(srel))[0]}"
        ids[srel] = rid
        lines.append(f'[ext_resource type="Texture2D" uid="{suid}" path="{res_path(srel)}" id="{rid}"]')
    lines.append("")
    entries = []
    for a in anims:
        frames = []
        for k, (x, y, w, h) in enumerate(a["rects"]):
            sid = f"AtlasTexture_{a['name']}_{k}"
            lines.append(f'[sub_resource type="AtlasTexture" id="{sid}"]')
            lines.append(f'atlas = ExtResource("{ids[a["sheet_rel"]]}")')
            lines.append(f"region = Rect2({x}, {y}, {w}, {h})")
            lines.append("")
            frames.append(sid)
        fr = ", ".join('{\n"duration": 1.0,\n"texture": SubResource("%s")\n}' % s for s in frames)
        entries.append('{\n"frames": [%s],\n"loop": %s,\n"name": &"%s",\n"speed": %s\n}' % (
            fr, "true" if a["loop"] else "false", a["name"], float(a["fps"])))
    lines.append("[resource]")
    lines.append("animations = [" + ", ".join(entries) + "]")
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "w").write("\n".join(lines) + "\n")
    return uid


def directional_character(out_dir, name, anims):
    """anims: dict anim -> (frames_by_dir [8][n], fps, loop). Writes one sheet per animation and a SpriteFrames."""
    entries = []
    for an, (rows, fps, loop) in anims.items():
        frame_h, frame_w = rows[0][0].shape[:2]
        srel = f"{out_dir}/{name}_{an}.png"
        suid = save_png(sheet_from(rows), srel)
        for di, dn in enumerate(DIRS):
            rects = [(j * frame_w, di * frame_h, frame_w, frame_h) for j in range(len(rows[di]))]
            entries.append(dict(name=f"{an}_{dn}", sheet_rel=srel, uid=suid, rects=rects, fps=fps, loop=loop))
    return write_sprite_frames(f"{out_dir}/{name}_frames.tres", entries)
