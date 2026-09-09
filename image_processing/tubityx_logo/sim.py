"""Offline preview of UI/TubityXLogo.shader - the maths here mirrors the HLSL
fragment stage one-for-one so the look can be checked without opening Unity."""
import numpy as np
from PIL import Image

TEX = np.load("/home/claude/tubityx/_packed.npy")     # H,W,3  (R sdf, G frame, B frameGlow)
TH, TW, _ = TEX.shape
SPREAD = 28.0

P = dict(
    StrokeOut=1.05, StrokeIn=2.15,      # neon rim band, in texture px about the outline
    InlinePos=3.25, InlineW=1.00,       # the inner "inline" contour
    CoreWhite=0.62, InlineAlpha=0.85, InlineWhite=0.12, FrameWhite=0.35,                     # how white the middle of the rim burns
    GlowRange=26.0, GlowPower=2.6, GlowStrength=0.70, HotFalloff=2.6, HotStrength=0.50,
    RimOffset=(-1.9, 1.9),              # chromatic ghost rim, texture px (x, y-down)
    RimStrength=0.85,
    ColorA=(0.42, 0.93, 1.00),          # cyan   (left end of the wordmark)
    ColorB=(1.00, 0.30, 0.86),          # magenta(right end / the X)
    GradStart=0.66, GradEnd=0.75,
    BodyColor=(0.030, 0.035, 0.075), BodyAlpha=0.92, BodyTint=0.012,
    GlassColor=(0.62, 0.76, 0.95), GlassAlpha=0.09, GlassTopBoost=0.55,
    FrameAlpha=1.0, FrameGlow=0.80,
    SheenColor=(1.0, 1.0, 1.0), SheenWidth=0.055, SheenSoft=0.075,
    SheenSkew=-0.45, SheenStrength=0.45, SheenGlassBoost=0.7,
)


LINEAR = True          # the project renders in Linear colour space (m_ActiveColorSpace: 1)

def s2l(c):
    """sRGB -> linear, exactly what Unity does to a Color property in Linear space."""
    c = np.asarray(c, float)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4) if LINEAR else c

def l2s(c):
    c = np.clip(np.asarray(c, float), 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055) if LINEAR else c

def band(d, lo, hi, aa):
    """1 inside [lo,hi], antialiased."""
    return np.clip((d - lo) / aa, 0, 1) * np.clip((hi - d) / aa, 0, 1)

def sample(ch, u, v):
    x = np.clip(u * TW - 0.5, 0, TW - 1); y = np.clip(v * TH - 0.5, 0, TH - 1)
    x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int)
    x1 = np.minimum(x0 + 1, TW - 1); y1 = np.minimum(y0 + 1, TH - 1)
    fx = (x - x0)[..., None] if ch is None else (x - x0); fy = (y - y0)
    a = TEX[y0, x0, ch]; b = TEX[y0, x1, ch]; c = TEX[y1, x0, ch]; d = TEX[y1, x1, ch]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy

def render(W, Hpx, time=0.0, bg=(0.016, 0.014, 0.045)):
    u = (np.arange(W) + 0.5) / W
    v = (np.arange(Hpx) + 0.5) / Hpx
    U, V = np.meshgrid(u, v)
    aa = max(0.75, TW / W)

    d = (sample(0, U, V) - 0.5) * 2.0 * SPREAD
    ro = P["RimOffset"]
    d2 = (sample(0, U + ro[0] / TW, V + ro[1] / TH) - 0.5) * 2.0 * SPREAD
    frame = sample(1, U, V)
    fglow = sample(2, U, V)

    g = np.clip((U - P["GradStart"]) / (P["GradEnd"] - P["GradStart"]), 0, 1)
    g = g * g * (3 - 2 * g)
    A = s2l(P["ColorA"]); B = s2l(P["ColorB"])
    main = A[None, None, :] * (1 - g)[..., None] + B[None, None, :] * g[..., None]
    comp = B[None, None, :] * (1 - g)[..., None] + A[None, None, :] * g[..., None]

    strokeM = band(d, -P["StrokeIn"], P["StrokeOut"], aa)
    inlineM = band(d, -(P["InlinePos"] + P["InlineW"]), -P["InlinePos"], aa)
    fillM   = np.clip((-(P["InlinePos"] + P["InlineW"]) - d) / aa, 0, 1)
    bodyM   = np.clip((P["StrokeOut"] - d) / aa, 0, 1)
    rimM    = band(d2, -P["StrokeIn"], P["StrokeOut"], aa) * (1 - strokeM) * P["RimStrength"]

    # windowed falloff: reaches exactly zero at _GlowRange, so the halo cannot
    # leave a faint rectangle over the whole sprite (an exp() never quite does)
    w = np.clip(1.0 - np.maximum(d, 0) / P["GlowRange"], 0, 1)
    outGlow = (w ** P["GlowPower"]) * P["GlowStrength"]
    neonBleed = np.exp(-np.abs(d) / P["HotFalloff"]) * P["HotStrength"]

    half = 0.5 * (P["StrokeIn"] + P["StrokeOut"])
    core = np.clip(1 - np.abs(d + (P["StrokeIn"] - P["StrokeOut"]) * 0.5) / half, 0, 1)
    core = core * core * P["CoreWhite"]
    strokeCol = main + (1 - main) * core[..., None]

    rgb = np.zeros((Hpx, W, 3)); a = np.zeros((Hpx, W))
    def layer(c, al):
        nonlocal rgb, a
        al = np.clip(al, 0, 1)[..., None]
        c = np.broadcast_to(np.asarray(c, float), rgb.shape)
        rgb = rgb * (1 - al) + c * al
        a = a * (1 - al[..., 0]) + al[..., 0]

    layer(main, outGlow)                                            # 1 outer bloom
    body = s2l(P["BodyColor"])[None, None, :] + main * P["BodyTint"]
    layer(body, bodyM * P["BodyAlpha"])                             # 2 dark glass body
    top = (1 - V)[..., None]
    glass = s2l(P["GlassColor"])[None, None, :] * (0.55 + P["GlassTopBoost"] * top)
    layer(glass, fillM * P["GlassAlpha"] * (0.6 + 0.8 * (1 - V)))   # 3 frosted panel
    layer(main, neonBleed)                                          # 4 neon bleed
    layer(comp, rimM)                                               # 5 chromatic ghost rim
    layer(main + (1 - main) * P["InlineWhite"], inlineM * P["InlineAlpha"])  # 6 inline contour
    layer(strokeCol, strokeM)                                       # 7 main neon rim
    layer(main, fglow * P["FrameGlow"])                             # 8 framing-line glow
    layer(main + (1 - main) * P["FrameWhite"], frame * P["FrameAlpha"])      # 9 framing lines

    # ---- sheen sweep -------------------------------------------------------
    p = U + (V - 0.5) * P["SheenSkew"]
    pos = -0.35 + np.clip(time, 0, 1) * 1.7
    dist = np.abs(p - pos)
    s = 1 - np.clip((dist - P["SheenWidth"]) / P["SheenSoft"], 0, 1)
    s = s * s * (3 - 2 * s) * P["SheenStrength"]
    boost = 1 + P["SheenGlassBoost"] * fillM
    rgb = rgb + s2l(P["SheenColor"])[None, None, :] * (s * a * boost)[..., None]

    out = s2l(bg)[None, None, :] * (1 - a[..., None]) + rgb * a[..., None]
    return l2s(out), a

if __name__ == "__main__":
    img, _ = render(1024, 256, time=0.62)
    Image.fromarray((img * 255).astype(np.uint8)).save("/home/claude/tubityx/preview.png")
    print("ok")
