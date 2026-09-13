"""Offline preview of UI/TubityXPanel.shader + UI/TubityXLabel.shader.
Mirrors the HLSL so the buttons can be judged without opening Unity.
Composites in linear, like the project."""
import numpy as np
from PIL import Image
import re

# ---- font atlas ---------------------------------------------------------
ATLAS = np.asarray(Image.open("Tex_TubityXFont.png")).astype(float) / 255.0
CS = open("TubityXFontMetrics.cs").read()
CAP = float(re.search(r"Cap = ([\d.]+)f", CS).group(1))
PAD = float(re.search(r"Pad = ([\d.]+)f", CS).group(1))
SPREAD = float(re.search(r"Spread = ([\d.]+)f", CS).group(1))
GLYPHS = {}
for m in re.finditer(r"new G\('(\\u[0-9A-Fa-f]{4}|\\'|.)', ([\d.]+), ([\d.]+), ([\d.]+), ([\d.]+), ([\d.]+)f\)", CS):
    raw = m.group(1)
    ch = chr(int(raw[2:], 16)) if raw.startswith("\\u") else raw.replace("\\'", "'")
    GLYPHS[ch] = tuple(float(x) for x in m.groups()[1:])


def s2l(c):
    c = np.asarray(c, float)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def l2s(c):
    c = np.clip(np.asarray(c, float), 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def bilinear(img, x, y):
    h, w = img.shape
    x = np.clip(x, 0, w - 1.001); y = np.clip(y, 0, h - 1.001)
    x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int)
    fx = x - x0; fy = y - y0
    a = img[y0, x0]; b = img[y0, x0 + 1]; c = img[y0 + 1, x0]; d = img[y0 + 1, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def text_width(text, cap_px, tracking):
    s = cap_px / CAP
    return sum(GLYPHS.get(c, GLYPHS.get(c.upper(), GLYPHS[" "]))[4] for c in text) * s + tracking * (len(text) - 1)


def label_sdf(W, H, text, cap_px, cx, cy, tracking):
    """Union SDF of a string, in screen pixels, on a WxH raster."""
    s = cap_px / CAP
    total = text_width(text, cap_px, tracking)
    pen = cx - total / 2
    top = cy - cap_px / 2
    out = np.full((H, W), 1e6)
    ys, xs = np.mgrid[0:H, 0:W]
    for ch in text:
        gx, gy, gw, gh, adv = GLYPHS.get(ch, GLYPHS.get(ch.upper(), GLYPHS[" "]))
        x0 = pen - PAD * s
        y0 = top - PAD * s
        u = (xs - x0) / (gw * s) * gw + gx
        v = (ys - y0) / (gh * s) * gh + gy
        inside = (xs >= x0) & (xs < x0 + gw * s) & (ys >= y0) & (ys < y0 + gh * s)
        d = (bilinear(ATLAS, u, v) - 0.5) * 2 * SPREAD * s      # -> screen px
        out = np.where(inside, np.minimum(out, d), out)
        pen += adv * s + tracking
    return out


def round_box(px, py, hw, hh, r):
    qx = np.abs(px) - (hw - r)
    qy = np.abs(py) - (hh - r)
    return (np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2)
            + np.minimum(np.maximum(qx, qy), 0) - r)


def band(d, lo, hi, aa=1.0):
    return np.clip((d - lo) / aa, 0, 1) * np.clip((hi - d) / aa, 0, 1)


PANEL = dict(
    Radius=16.0, RimW=2.2, RimGhost=(-1.6, 1.6), GhostStrength=0.55,
    GlassColor=(0.022, 0.040, 0.098), GlassAlpha=0.90,
    SpecColor=(0.72, 0.86, 1.00), SpecAlpha=0.10,
    HaloRange=24.0, HaloPower=2.6, HaloStrength=0.55,
    BleedFalloff=2.6, BleedStrength=0.22,
    RimCoreWhite=0.45,
)
LABEL = dict(
    FaceColor=(0.95, 0.98, 1.00), FaceAlpha=1.0,
    RimW=1.5, RimAlpha=0.90, RimCoreWhite=0.0,
    GlowRange=6.0, GlowPower=2.4, GlowStrength=0.40,
)


def draw_button(W, H, text, rim_srgb, pulse_srgb, pulse_amt, phase,
                cap_px=27.0, tracking=3.0, highlight=0.0, bg=(0.015, 0.015, 0.040)):
    ys, xs = np.mgrid[0:H, 0:W]
    px = xs - (W - 1) / 2.0
    py = ys - (H - 1) / 2.0
    bw, bh = W * 0.5 - 30, H * 0.5 - 22            # the button rect inside the raster
    P = PANEL

    d = round_box(px, py, bw, bh, P["Radius"])
    ox, oy = P["RimGhost"]
    d2 = round_box(px - ox, py - oy, bw, bh, P["Radius"])

    pulse = 0.5 - 0.5 * np.cos(phase * 2 * np.pi)
    rim = s2l(rim_srgb) * (1 - pulse * pulse_amt) + s2l(pulse_srgb) * (pulse * pulse_amt)
    boost = 1.0 + 0.55 * pulse * pulse_amt + 0.7 * highlight

    fillM = np.clip((-P["RimW"] - d) / 1.0, 0, 1)
    rimM = band(d, -P["RimW"], 0.0)
    ghostM = band(d2, -P["RimW"], 0.0) * (1 - rimM) * P["GhostStrength"]
    # the halo lives strictly outside the panel, or it floods the glass interior
    halo = (np.clip(1 - np.maximum(d, 0) / P["HaloRange"], 0, 1) ** P["HaloPower"]
            * P["HaloStrength"] * (d > 0))
    bleed = np.exp(-np.abs(d) / P["BleedFalloff"]) * P["BleedStrength"]
    top = 1.0 - ys / float(H)

    rgb = np.zeros((H, W, 3)); a = np.zeros((H, W))

    def layer(c, al):
        nonlocal rgb, a
        al = np.clip(al, 0, 1)[..., None]
        c = np.broadcast_to(np.asarray(c, float), rgb.shape)
        rgb = rgb * (1 - al) + c * al
        a = a * (1 - al[..., 0]) + al[..., 0]

    layer(rim * boost, halo * boost)
    layer(s2l(P["GlassColor"]), fillM * P["GlassAlpha"])
    layer(s2l(P["SpecColor"]), fillM * P["SpecAlpha"] * np.clip(top * 1.6 - 0.5, 0, 1))
    layer(rim * boost, bleed * boost)
    layer(s2l((1, 1, 1)) * 0.9, ghostM * 0.35)
    core = np.clip(1 - np.abs(d + P["RimW"] * 0.5) / (P["RimW"] * 0.6), 0, 1) ** 2 * P["RimCoreWhite"]
    layer(np.clip(rim * boost, 0, 1) + (1 - rim) * core[..., None], rimM)

    # ---- label ----------------------------------------------------------
    L = LABEL
    dl = label_sdf(W, H, text, cap_px, (W - 1) / 2.0, (H - 1) / 2.0, tracking)
    lfill = np.clip((-L["RimW"] - dl) / 1.0, 0, 1)
    lrim = band(dl, -L["RimW"], 0.0)
    lglow = np.clip(1 - np.maximum(dl, 0) / L["GlowRange"], 0, 1) ** L["GlowPower"] * L["GlowStrength"]
    layer(rim * boost, lglow)
    layer(rim * boost, lrim * L["RimAlpha"])
    layer(s2l(L["FaceColor"]), lfill * L["FaceAlpha"])

    out = s2l(bg)[None, None, :] * (1 - a[..., None]) + rgb * a[..., None]
    return l2s(out), a
