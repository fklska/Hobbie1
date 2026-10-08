import sys
import numpy as np
from PIL import Image


def sheet(frames, scale=4, bg=(70, 128, 62, 255), cols=None):
    h, w = frames[0].shape[:2]
    cols = cols or len(frames)
    rows = (len(frames) + cols - 1) // cols
    out = Image.new("RGBA", (w * cols, h * rows), bg)
    for k, f in enumerate(frames):
        im = Image.fromarray(np.clip(f, 0, 255).astype(np.uint8), "RGBA")
        out.alpha_composite(im, ((k % cols) * w, (k // cols) * h))
    return out.resize((out.width * scale, out.height * scale), Image.NEAREST)
