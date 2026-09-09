"""Vector letterforms for the TUBITYX wordmark, measured from the mockup.

All shapes are drawn in a local box (0..W, 0..H) where H is the cap height and
y=0 is the cap line.  Proportions taken off the reference art:
    stem (vertical)     0.290 H
    bar  (horizontal)   0.190 H
    tracking            0.057 H
"""
from PIL import Image, ImageDraw

SV = 0.290          # vertical stem, as a fraction of cap height
SH = 0.190          # horizontal bar
WIDTHS = {"T": 1.110, "U": 1.125, "B": 1.090, "I": 0.284, "Y": 1.200, "X": 1.140}


def _canvas(w, h):
    im = Image.new("L", (int(round(w)) + 2, int(round(h)) + 2), 0)
    return im, ImageDraw.Draw(im)


def draw_glyph(ch, H):
    """Return an L-mode bitmap of one glyph at cap height H (already supersampled)."""
    W = WIDTHS[ch] * H
    sv, sh = SV * H, SH * H
    im, d = _canvas(W, H)
    cut = Image.new("L", im.size, 0)
    dc = ImageDraw.Draw(cut)

    if ch == "T":
        d.rectangle([0, 0, W, sh], fill=255)
        d.rectangle([(W - sv) / 2, 0, (W + sv) / 2, H], fill=255)

    elif ch == "U":
        r = min(0.42 * W, W / 2 - 1, H / 2 - 1)
        d.rounded_rectangle([0, 0, W, H], radius=r, fill=255,
                            corners=(False, False, True, True))
        ri = max(1.0, r - sv)
        dc.rounded_rectangle([sv, -H, W - sv, H - sh], radius=ri, fill=255,
                             corners=(False, False, True, True))

    elif ch == "B":
        mid_t, mid_b = H / 2 - sh / 2, H / 2 + sh / 2
        rt = min(0.26 * H, mid_b / 2 - 1, W / 2 - 1)
        rb = min(0.30 * H, (H - mid_t) / 2 - 1, W / 2 - 1)
        d.rounded_rectangle([0, 0, W, mid_b], radius=rt, fill=255,
                            corners=(False, True, True, False))
        d.rounded_rectangle([0, mid_t, W, H], radius=rb, fill=255,
                            corners=(False, True, True, False))
        rti = min(0.13 * H, (mid_t - sh) / 2 - 1)
        rbi = min(0.16 * H, (H - sh - mid_b) / 2 - 1)
        dc.rounded_rectangle([sv, sh, W - sh, mid_t], radius=rti, fill=255,
                             corners=(False, True, True, False))
        dc.rounded_rectangle([sv, mid_b, W - sh, H - sh], radius=rbi, fill=255,
                             corners=(False, True, True, False))

    elif ch == "I":
        d.rectangle([0, 0, W, H], fill=255)

    elif ch == "Y":
        cx, ymid, aw = W / 2, 0.530 * H, 0.360 * H
        d.polygon([(0, 0), (aw, 0), (cx + sv / 2, ymid), (cx - sv / 2, ymid)], fill=255)
        d.polygon([(W, 0), (W - aw, 0), (cx - sv / 2, ymid), (cx + sv / 2, ymid)], fill=255)
        d.rectangle([cx - sv / 2, ymid - 1, cx + sv / 2, H], fill=255)

    elif ch == "X":
        bw = 0.380 * H
        d.polygon([(0, 0), (bw, 0), (W, H), (W - bw, H)], fill=255)
        d.polygon([(W, 0), (W - bw, 0), (0, H), (bw, H)], fill=255)

    else:
        raise ValueError(ch)

    im.paste(0, (0, 0), cut)
    return im, W
