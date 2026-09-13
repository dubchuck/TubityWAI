"""Compose a whole menu screen from the panel and label shader maths, so the
popup and grid layouts can be judged before Unity ever opens."""
import numpy as np
from PIL import Image
import sim_button as SB

W, H = 1280, 720
BG = (0.015, 0.015, 0.040)


def _round_box(px, py, hw, hh, r):
    return SB.round_box(px, py, hw, hh, r)


def panel(buf, cx, cy, w, h, rim, radius=22.0, pulse=0.0, highlight=0.0, phase=0.5):
    ys, xs = np.mgrid[0:H, 0:W]
    px, py = xs - cx, ys - cy
    P = SB.PANEL
    d = _round_box(px, py, w / 2, h / 2, radius)
    ox, oy = P["RimGhost"]
    d2 = _round_box(px - ox, py - oy, w / 2, h / 2, radius)

    pu = (0.5 - 0.5 * np.cos(phase * 2 * np.pi)) * pulse
    rimc = SB.s2l(rim) * (1 - pu) + SB.s2l((0.45, 0.98, 1.0)) * pu
    boost = 1.0 + 0.55 * pu + 0.7 * highlight

    fillM = np.clip((-P["RimW"] - d) / 1.0, 0, 1)
    rimM = SB.band(d, -P["RimW"], 0.0)
    ghostM = SB.band(d2, -P["RimW"], 0.0) * (1 - rimM) * P["GhostStrength"]
    halo = (np.clip(1 - np.maximum(d, 0) / P["HaloRange"], 0, 1) ** P["HaloPower"]
            * P["HaloStrength"] * (d > 0))
    bleed = np.exp(-np.abs(d) / P["BleedFalloff"]) * P["BleedStrength"]
    top = np.clip(0.5 - py / max(h, 1), 0, 1)

    def lay(c, al):
        al = np.clip(al, 0, 1)[..., None]
        c = np.broadcast_to(np.asarray(c, float), buf.shape)
        buf[:] = buf * (1 - al) + c * al

    lay(rimc * boost, halo * boost)
    lay(SB.s2l(P["GlassColor"]), fillM * P["GlassAlpha"])
    lay(SB.s2l(P["SpecColor"]), fillM * P["SpecAlpha"] * np.clip(top * 1.6 - 0.5, 0, 1))
    lay(rimc * boost, bleed * boost)
    lay(SB.s2l((0.9, 0.9, 0.9)), ghostM * 0.35)
    lay(np.clip(rimc * boost, 0, 1), rimM)


def label(buf, cx, cy, w, text, cap, tracking, face, accent, align="c"):
    L = SB.LABEL
    fit = max(0.0, w - 14)
    width = SB.text_width(text, cap, tracking)
    if width > fit > 1:
        cap *= fit / width
        width = SB.text_width(text, cap, tracking)
    if align == "l":
        cx = cx - w / 2 + 7 + width / 2
    elif align == "r":
        cx = cx + w / 2 - 7 - width / 2
    dl = SB.label_sdf(W, H, text, cap, cx, cy, tracking)
    lfill = np.clip((-L["RimW"] - dl) / 1.0, 0, 1)
    lrim = SB.band(dl, -L["RimW"], 0.0)
    lglow = np.clip(1 - np.maximum(dl, 0) / L["GlowRange"], 0, 1) ** L["GlowPower"] * 0.4

    def lay(c, al):
        al = np.clip(al, 0, 1)[..., None]
        c = np.broadcast_to(np.asarray(c, float), buf.shape)
        buf[:] = buf * (1 - al) + c * al

    lay(SB.s2l(accent), lglow)
    lay(SB.s2l(accent), lrim * L["RimAlpha"])
    lay(SB.s2l(face), lfill)


def finish(buf):
    out = SB.s2l(BG)[None, None, :] * (1 - 0) + buf
    return SB.l2s(np.clip(buf + SB.s2l(BG)[None, None, :] * (buf.sum(axis=2, keepdims=True) == 0), 0, 4))
