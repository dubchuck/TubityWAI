"""Bake Tex_TubityXLogo.png - the packed data texture for UI/TubityXLogo.shader.

    R = signed distance field of the wordmark  (0.5 = outline, +-SPREAD px)
    G = framing-line / marker mask, antialiased, ends tapered
    B = soft glow of the framing lines
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage
from glyphs import draw_glyph

TEX_W, TEX_H = 1024, 256
SS      = 4
SPREAD  = 28.0          # SDF half-range in final-texture pixels

CAP_H     = 96.0        # cap height in texture px
CAP_TOP   = 80.0
WORD_LEFT = 156.0
TRACK     = 0.057       # of cap height

W, H = TEX_W * SS, TEX_H * SS

# ---------------------------------------------------------------- wordmark
def wordmark_mask():
    h = CAP_H * SS
    track = TRACK * h
    canvas = Image.new("L", (W, H), 0)
    x = WORD_LEFT * SS
    for ch in "TUBITYX":
        g, gw = draw_glyph(ch, h)
        canvas.paste(g, (int(round(x)), int(round(CAP_TOP * SS))), g)
        x += gw + track
    return np.array(canvas) > 127, (x - track) / SS

# ---------------------------------------------------------------- framing lines
def _taper(draw_len, fade_a, fade_b, n):
    t = np.linspace(0, draw_len, n)
    a = np.clip(t / max(fade_a, 1e-3), 0, 1) if fade_a > 0 else np.ones(n)
    b = np.clip((draw_len - t) / max(fade_b, 1e-3), 0, 1) if fade_b > 0 else np.ones(n)
    return a * b

def stroke(img, pts, width, fade_start=0.0, fade_end=0.0):
    """Polyline with alpha tapers at the ends, drawn as a chain of dabs."""
    d = ImageDraw.Draw(img)
    segs = [(np.array(pts[i]), np.array(pts[i + 1])) for i in range(len(pts) - 1)]
    lens = [np.linalg.norm(b - a) for a, b in segs]
    total = sum(lens)
    walked = 0.0
    for (a, b), L in zip(segs, lens):
        n = max(2, int(L * 2))
        for i in range(n + 1):
            t = i / n
            p = a + (b - a) * t
            s = walked + L * t
            al = 1.0
            if fade_start > 0:
                al *= min(1.0, s / fade_start)
            if fade_end > 0:
                al *= min(1.0, (total - s) / fade_end)
            v = int(round(255 * al))
            if v <= 0:
                continue
            r = width / 2
            d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r],
                      fill=max(v, 0))
        walked += L

def tri(img, cx, cy, size, width, up=True, alpha=255):
    d = ImageDraw.Draw(img)
    h = size
    w = size * 1.05
    if up:
        pts = [(cx, cy - h * .6), (cx + w * .55, cy + h * .5), (cx - w * .55, cy + h * .5)]
    else:
        pts = [(cx, cy + h * .6), (cx + w * .55, cy - h * .5), (cx - w * .55, cy - h * .5)]
    d.line(pts + [pts[0]], fill=alpha, width=int(round(width)), joint="curve")

def ring(img, cx, cy, r, width, alpha=255):
    d = ImageDraw.Draw(img)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=alpha, width=int(round(width)))

def framing_mask():
    img = Image.new("L", (W, H), 0)
    s = SS

    # --- top-left cluster ------------------------------------------------
    # bright rail with a 45 deg chamfer dropping away to the left
    stroke(img, [(130.9 * s, 82.2 * s), (153.8 * s, 61.5 * s), (396.0 * s, 61.5 * s)],
           3.0 * s, fade_start=14 * s, fade_end=90 * s)
    # thin upper rail
    stroke(img, [(196.0 * s, 50.5 * s), (363.0 * s, 50.5 * s)],
           2.0 * s, fade_start=34 * s, fade_end=40 * s)
    tri(img, 377 * s, 50.5 * s, 9 * s, 1.6 * s, up=True)
    ring(img, 430 * s, 51.5 * s, 2.6 * s, 1.3 * s, alpha=200)

    # --- bottom-right cluster: the same marks rotated 180 deg ------------
    half = np.array(img)
    img = Image.fromarray(np.maximum(half, half[::-1, ::-1]))
    return img

# ---------------------------------------------------------------- sdf
def sdf(mask):
    d_out = ndimage.distance_transform_edt(~mask)
    d_in = ndimage.distance_transform_edt(mask)
    d = (d_out - d_in) / SS
    d = np.clip(d, -SPREAD, SPREAD)
    d = d.reshape(TEX_H, SS, TEX_W, SS).mean(axis=(1, 3))
    return d / SPREAD * 0.5 + 0.5

def down(a):
    return a.reshape(TEX_H, SS, TEX_W, SS).mean(axis=(1, 3))

wm, right_edge = wordmark_mask()
print("wordmark spans x %.1f .. %.1f  (cap %.0f .. %.0f)" %
      (WORD_LEFT, right_edge, CAP_TOP, CAP_TOP + CAP_H))

R = sdf(wm)
frame = down(np.asarray(framing_mask()).astype(float) / 255.0)
G = np.clip(frame, 0, 1)
B = np.asarray(Image.fromarray((G * 255).astype(np.uint8)).filter(
        ImageFilter.GaussianBlur(3.2))).astype(float) / 255.0
B = np.clip(B * 1.9, 0, 1)

out = np.stack([R, G, B], axis=-1)
Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB") \
     .save("/home/claude/tubityx/Tex_TubityXLogo.png")
np.save("/home/claude/tubityx/_packed.npy", out)
print("wrote Tex_TubityXLogo.png", TEX_W, "x", TEX_H)
