"""The rest of the TubityX typeface - A-Z, 0-9 and a few marks.

Same geometric system as the six logo letters in glyphs.py:
    vertical stem  0.290 x cap height
    horizontal bar 0.190 x cap height
Everything is drawn in a local box (0..W, 0..H), y down, y=0 at the cap line.
"""
from PIL import Image, ImageDraw
from glyphs import SV, SH, WIDTHS as LOGO_WIDTHS, draw_glyph as draw_logo_glyph

# width of each glyph as a fraction of cap height
W_EXT = {
    "A": 1.14, "C": 1.10, "D": 1.10, "E": 1.02, "F": 1.00, "G": 1.14, "H": 1.12,
    "J": 0.98, "K": 1.10, "L": 0.98, "M": 1.34, "N": 1.14, "O": 1.16, "P": 1.06,
    "Q": 1.16, "R": 1.08, "S": 1.06, "V": 1.14, "W": 1.46, "Z": 1.06,
    "0": 1.14, "1": 0.74, "2": 1.06, "3": 1.06, "4": 1.14, "5": 1.06,
    "6": 1.10, "7": 1.02, "8": 1.08, "9": 1.10,
    " ": 0.46, ".": 0.34, ",": 0.34, "-": 0.72, ":": 0.34, "!": 0.30,
    "?": 0.96, "'": 0.30, "/": 0.86, "(": 0.52, ")": 0.52, "+": 0.92, "&": 1.16,
}
WIDTHS = dict(LOGO_WIDTHS)
WIDTHS.update(W_EXT)


class Pen:
    """Additive/subtractive painter over one glyph box."""

    def __init__(self, W, H):
        self.W, self.H = W, H
        self.size = (int(W) + 2, int(H) + 2)
        self.add_im = Image.new("L", self.size, 0)
        self.a = ImageDraw.Draw(self.add_im)
        self._scratch = None

    def _d(self, cut):
        """Cuts are applied the moment they are drawn, so later strokes survive."""
        if not cut:
            return self.a
        self._scratch = Image.new("L", self.size, 0)
        return ImageDraw.Draw(self._scratch)

    def _flush(self, cut):
        if cut and self._scratch is not None:
            self.add_im.paste(0, (0, 0), self._scratch)
            self._scratch = None

    def rect(self, x0, y0, x1, y1, cut=False):
        self._d(cut).rectangle([x0, y0, x1, y1], fill=255)
        self._flush(cut)

    def rrect(self, x0, y0, x1, y1, r, corners=(True,) * 4, cut=False):
        r = max(0.0, min(r, (x1 - x0) / 2 - 0.5, (y1 - y0) / 2 - 0.5))
        if r <= 0.5:
            self.rect(x0, y0, x1, y1, cut)
        else:
            self._d(cut).rounded_rectangle([x0, y0, x1, y1], radius=r,
                                           fill=255, corners=corners)
            self._flush(cut)

    def poly(self, pts, cut=False):
        self._d(cut).polygon([tuple(p) for p in pts], fill=255)
        self._flush(cut)

    def diag(self, p0, p1, w, cut=False):
        """Bar between two points with horizontal thickness w."""
        (x0, y0), (x1, y1) = p0, p1
        self.poly([(x0, y0), (x0 + w, y0), (x1 + w, y1), (x1, y1)], cut)

    def ring(self, x0, y0, x1, y1, r, tv, th, corners=(True,) * 4):
        """Rounded-rect outline: tv = side thickness, th = top/bottom thickness."""
        self.rrect(x0, y0, x1, y1, r, corners)
        self.rrect(x0 + tv, y0 + th, x1 - tv, y1 - th, max(r - tv, 0.0), corners, cut=True)

    def result(self):
        return self.add_im


def draw_glyph(ch, H):
    """Bitmap of one glyph at cap height H. Falls back to the logo set."""
    if ch in LOGO_WIDTHS and ch not in W_EXT:
        return draw_logo_glyph(ch, H)

    W = WIDTHS[ch] * H
    sv, sh = SV * H, SH * H
    p = Pen(W, H)
    mid_t, mid_b = H / 2 - sh / 2, H / 2 + sh / 2
    r = 0.30 * H                       # generic outer corner radius

    if ch == " ":
        pass

    elif ch == "A":
        aw = 0.34 * H
        p.poly([(0, H), (aw, H), (W / 2 + aw / 2, 0), (W / 2 - aw / 2, 0)])
        p.poly([(W, H), (W - aw, H), (W / 2 - aw / 2, 0), (W / 2 + aw / 2, 0)])
        p.rect(sv * 0.55, H * 0.60, W - sv * 0.55, H * 0.60 + sh)

    elif ch == "C":
        p.ring(0, 0, W, H, r, sv, sh)
        p.rect(W - sv - 1, H * 0.20, W + 2, H * 0.80, cut=True)

    elif ch == "D":
        p.ring(0, 0, W, H, r, sv, sh, corners=(False, True, True, False))

    elif ch == "E":
        p.rect(0, 0, sv, H)
        p.rect(0, 0, W, sh)
        p.rect(0, mid_t, W * 0.93, mid_b)
        p.rect(0, H - sh, W, H)

    elif ch == "F":
        p.rect(0, 0, sv, H)
        p.rect(0, 0, W, sh)
        p.rect(0, mid_t, W * 0.93, mid_b)

    elif ch == "G":
        p.ring(0, 0, W, H, r, sv, sh)
        p.rect(W - sv - 1, H * 0.26, W + 2, H * 0.46, cut=True)
        p.rect(W * 0.52, H * 0.46, W, H * 0.46 + sh)
        p.rect(W - sv, H * 0.46, W, H * 0.74)

    elif ch == "H":
        p.rect(0, 0, sv, H)
        p.rect(W - sv, 0, W, H)
        p.rect(0, mid_t, W, mid_b)

    elif ch == "J":
        p.ring(0, H - 2 * r, W, H, r, sv, sh, corners=(False, False, True, True))
        p.rect(-2, H - 2 * r - 2, W + 2, H - 2 * r + sh, cut=True)   # drop the hook's top bar
        p.rect(W - sv, 0, W, H - r)

    elif ch == "K":
        aw = 0.32 * H
        p.rect(0, 0, sv, H)
        p.diag((W - aw, 0), (sv * 0.55, H * 0.54), aw)
        p.diag((sv * 0.55, H * 0.46), (W - aw, H), aw)

    elif ch == "L":
        p.rect(0, 0, sv, H)
        p.rect(0, H - sh, W, H)

    elif ch == "M":
        aw = 0.30 * H
        p.rect(0, 0, sv, H)
        p.rect(W - sv, 0, W, H)
        p.poly([(0, 0), (sv, 0), (W / 2 + aw / 2, H * 0.72), (W / 2 - aw / 2, H * 0.72)])
        p.poly([(W, 0), (W - sv, 0), (W / 2 - aw / 2, H * 0.72), (W / 2 + aw / 2, H * 0.72)])

    elif ch == "N":
        p.rect(0, 0, sv, H)
        p.rect(W - sv, 0, W, H)
        p.poly([(0, 0), (sv, 0), (W, H), (W - sv, H)])

    elif ch == "O":
        p.ring(0, 0, W, H, r, sv, sh)

    elif ch == "P":
        p.rect(0, 0, sv, H)
        p.ring(0, 0, W, mid_b, 0.26 * H, sv, sh, corners=(False, True, True, False))

    elif ch == "Q":
        p.ring(0, 0, W, H, r, sv, sh)
        p.poly([(W * 0.56, H * 0.68), (W * 0.56 + sv, H * 0.68), (W, H), (W - sv, H)])

    elif ch == "R":
        p.rect(0, 0, sv, H)
        p.ring(0, 0, W, mid_b, 0.26 * H, sv, sh, corners=(False, True, True, False))
        p.poly([(W * 0.46, mid_b - sh * 0.2), (W * 0.46 + sv, mid_b - sh * 0.2), (W, H), (W - sv, H)])

    elif ch == "S":
        rt, rb = 0.26 * H, 0.28 * H
        p.ring(0, 0, W, mid_b, rt, sv, sh, corners=(True, True, False, False))
        p.ring(0, mid_t, W, H, rb, sv, sh, corners=(False, False, True, True))
        # open the upper bowl on the right and the lower bowl on the left,
        # stopping short of the waist so the middle bar stays continuous
        p.rect(W - sv - 1, sh * 0.9, W + 2, mid_t, cut=True)
        p.rect(-2, mid_b, sv + 1, H - sh * 0.9, cut=True)

    elif ch == "V":
        aw = 0.34 * H
        p.poly([(0, 0), (aw, 0), (W / 2 + aw / 2, H), (W / 2 - aw / 2, H)])
        p.poly([(W, 0), (W - aw, 0), (W / 2 - aw / 2, H), (W / 2 + aw / 2, H)])

    elif ch == "W":
        aw = 0.28 * H
        q1, q2 = W * 0.30, W * 0.70
        p.poly([(0, 0), (aw, 0), (q1 + aw / 2, H), (q1 - aw / 2, H)])
        p.poly([(W / 2, 0), (W / 2 - aw, 0), (q1 - aw / 2, H), (q1 + aw / 2, H)])
        p.poly([(W / 2, 0), (W / 2 + aw, 0), (q2 + aw / 2, H), (q2 - aw / 2, H)])
        p.poly([(W, 0), (W - aw, 0), (q2 - aw / 2, H), (q2 + aw / 2, H)])

    elif ch == "Z":
        aw = 0.36 * H
        p.rect(0, 0, W, sh)
        p.rect(0, H - sh, W, H)
        p.poly([(W - aw, sh * 0.2), (W, sh * 0.2), (aw, H - sh * 0.2), (0, H - sh * 0.2)])

    # ---- digits ----------------------------------------------------------
    elif ch == "0":
        p.ring(0, 0, W, H, r, sv, sh)
        p.poly([(W * 0.62, H * 0.22), (W * 0.72, H * 0.22), (W * 0.38, H * 0.78), (W * 0.28, H * 0.78)])

    elif ch == "1":
        p.rect(W - sv, 0, W, H)
        p.poly([(0, sh * 1.5), (0, sh * 2.6), (W - sv, 0), (W - sv, sh * 1.1)])

    elif ch == "2":
        p.ring(0, 0, W, mid_b, 0.28 * H, sv, sh, corners=(True, True, False, False))
        p.rect(-2, sh * 0.9, sv + 1, mid_b, cut=True)
        p.poly([(W - sv, mid_t), (W, mid_t), (sv, H - sh), (0, H - sh)])
        p.rect(0, H - sh, W, H)

    elif ch == "3":
        p.ring(0, 0, W, mid_b, 0.26 * H, sv, sh, corners=(True, True, False, False))
        p.rect(-2, sh * 0.9, sv + 1, mid_b, cut=True)
        p.ring(0, mid_t, W, H, 0.28 * H, sv, sh, corners=(False, False, True, True))
        p.rect(-2, mid_t, sv + 1, H - sh * 0.9, cut=True)
        p.rect(W * 0.28, mid_t, W - sv, mid_b)

    elif ch == "4":
        p.rect(W * 0.62, 0, W * 0.62 + sv, H)
        p.poly([(W * 0.62, 0), (W * 0.62 + sv, 0), (sv, H * 0.70), (0, H * 0.70)])
        p.rect(0, H * 0.70, W, H * 0.70 + sh)

    elif ch == "5":
        p.rect(0, 0, W, sh)
        p.rect(0, 0, sv, mid_b)
        p.rect(0, mid_t, W * 0.72, mid_b)
        p.ring(0, mid_t, W, H, 0.28 * H, sv, sh, corners=(False, False, True, True))
        p.rect(-2, mid_b, sv + 1, H - sh * 0.9, cut=True)

    elif ch == "6":
        p.ring(0, 0, W, H, r, sv, sh)
        p.rect(W - sv - 1, sh * 0.9, W + 2, mid_t, cut=True)
        p.rect(0, mid_t, W, mid_b)

    elif ch == "7":
        aw = 0.34 * H
        p.rect(0, 0, W, sh)
        p.poly([(W - aw, sh * 0.2), (W, sh * 0.2), (W * 0.34, H), (W * 0.34 - aw, H)])

    elif ch == "8":
        p.ring(0, 0, W, mid_b, 0.26 * H, sv, sh, corners=(True, True, False, False))
        p.ring(0, mid_t, W, H, 0.28 * H, sv, sh, corners=(False, False, True, True))

    elif ch == "9":
        p.ring(0, 0, W, H, r, sv, sh)
        p.rect(-2, mid_b, sv + 1, H - sh * 0.9, cut=True)
        p.rect(0, mid_t, W, mid_b)

    # ---- marks -----------------------------------------------------------
    elif ch == ".":
        p.rrect(0, H - sh, W, H, sh * 0.35)
    elif ch == ",":
        p.rrect(0, H - sh, W, H, sh * 0.35)
        p.poly([(W * 0.15, H), (W, H), (W * 0.1, H + sh * 0.9)])
    elif ch == "-":
        p.rect(0, mid_t, W, mid_b)
    elif ch == "+":
        p.rect(0, mid_t, W, mid_b)
        p.rect(W / 2 - sh / 2, H * 0.5 - W / 2, W / 2 + sh / 2, H * 0.5 + W / 2)
    elif ch == ":":
        p.rrect(0, H * 0.24, W, H * 0.24 + sh, sh * 0.35)
        p.rrect(0, H - sh, W, H, sh * 0.35)
    elif ch == "!":
        p.rect(0, 0, W, H * 0.68)
        p.rrect(0, H - sh, W, H, sh * 0.35)
    elif ch == "'":
        p.rect(0, 0, W, H * 0.30)
    elif ch == "/":
        aw = 0.30 * H
        p.poly([(W - aw, 0), (W, 0), (aw, H), (0, H)])
    elif ch == "(":
        # a thin, strongly curved stroke - sv would read as a solid slab here
        p.ring(0, 0, W * 2.8, H, H * 0.48, sh, sh)
        p.rect(W, -2, W * 2.8 + 2, H + 2, cut=True)
    elif ch == ")":
        p.ring(-W * 1.8, 0, W, H, H * 0.48, sh, sh)
        p.rect(-W * 1.8 - 2, -2, 0, H + 2, cut=True)
    elif ch == "?":
        p.ring(0, 0, W, mid_b + sh * 0.4, 0.30 * H, sv, sh, corners=(True, True, False, False))
        p.rect(-2, sh * 0.9, sv + 1, mid_b + sh, cut=True)
        p.rect(W * 0.5 - sv / 2, mid_t, W * 0.5 + sv / 2, H * 0.72)
        p.rrect(W * 0.5 - sv / 2, H - sh, W * 0.5 + sv / 2, H, sh * 0.35)
    elif ch == "&":
        p.ring(W * 0.10, 0, W * 0.68, H * 0.44, 0.20 * H, sv * 0.85, sh)
        p.ring(0, H * 0.40, W * 0.80, H, 0.26 * H, sv * 0.85, sh)
        p.rect(W * 0.62, H * 0.40, W * 0.80, H * 0.62, cut=True)
        p.diag((W * 0.34, H * 0.34), (W - sv, H), sv * 0.85)
    else:
        raise ValueError("no glyph for %r" % ch)

    return p.result(), W
