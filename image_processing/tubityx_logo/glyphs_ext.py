"""The rest of the TubityX typeface - A-Z, 0-9 and a few marks.

Same geometric system as the six logo letters in glyphs.py:
    vertical stem  0.290 x cap height
    horizontal bar 0.190 x cap height
Everything is drawn in a local box (0..W, 0..H), y down, y=0 at the cap line.
"""
from PIL import Image, ImageDraw
from glyphs import SV, SH, WIDTHS as LOGO_WIDTHS, draw_glyph as draw_logo_glyph

# width of each glyph as a fraction of cap height
XH   = 0.72     # x-height, as a fraction of cap height
DESC = 0.26     # how far descenders drop below the baseline

W_EXT = {
    "A": 1.14, "C": 1.10, "D": 1.10, "E": 1.02, "F": 1.00, "G": 1.14, "H": 1.12,
    "J": 0.98, "K": 1.10, "L": 0.98, "M": 1.34, "N": 1.14, "O": 1.16, "P": 1.06,
    "Q": 1.16, "R": 1.08, "S": 1.06, "V": 1.14, "W": 1.46, "Z": 1.06,
    "0": 1.14, "1": 0.74, "2": 1.06, "3": 1.06, "4": 1.14, "5": 1.06,
    "6": 1.10, "7": 1.02, "8": 1.08, "9": 1.10,
    " ": 0.46, ".": 0.34, ",": 0.34, "-": 0.72, ":": 0.34, "!": 0.30,
    "?": 0.96, "'": 0.30, "/": 0.86, "(": 0.52, ")": 0.52, "+": 0.92, "&": 1.16,
    "$": 1.02, "%": 1.20, "*": 0.80, "=": 0.92, "#": 1.10,
}
WIDTHS = dict(LOGO_WIDTHS)
WIDTHS.update(W_EXT)


class Pen:
    """Additive/subtractive painter over one glyph box.

    The box runs from the cap line (y=0) to the descender depth, so every glyph
    shares one vertical frame and the label component can place them all by cap
    line alone."""

    def __init__(self, W, H):
        self.W, self.H = W, H
        self.size = (int(W) + 2, int(H * (1 + DESC)) + 2)
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
        """Rounded-rect outline: tv = side thickness, th = top/bottom thickness.
        If the walls would meet, the shape stays solid rather than throwing."""
        self.rrect(x0, y0, x1, y1, r, corners)
        ix0, iy0, ix1, iy1 = x0 + tv, y0 + th, x1 - tv, y1 - th
        if ix1 > ix0 + 0.5 and iy1 > iy0 + 0.5:
            self.rrect(ix0, iy0, ix1, iy1, max(r - tv, 0.0), corners, cut=True)

    def result(self):
        return self.add_im


def draw_glyph(ch, H):
    """Bitmap of one glyph at cap height H. Falls back to the logo set."""
    if ch in LOGO_WIDTHS and ch not in W_EXT:
        # the six logo letters come from glyphs.py untouched; re-frame them into
        # the taller box so every glyph shares one vertical origin
        g, gw = draw_logo_glyph(ch, H)
        tall = Image.new("L", (g.width, int(H * (1 + DESC)) + 2), 0)
        tall.paste(g, (0, 0), g)
        return tall, gw

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
    elif ch == "$":
        # an S with the stroke running through it
        rt = 0.24 * H
        p.ring(0, 0.10 * H, W, mid_b, rt, sv, sh, corners=(True, True, False, False))
        p.ring(0, mid_t, W, H * 0.92, rt, sv, sh, corners=(False, False, True, True))
        p.rect(W - sv - 1, 0.10 * H + sh * 0.9, W + 2, mid_t, cut=True)
        p.rect(-2, mid_b, sv + 1, H * 0.92 - sh * 0.9, cut=True)
        p.rect(W / 2 - sh * 0.42, 0, W / 2 + sh * 0.42, H)
    elif ch == "%":
        r = 0.17 * H
        p.ring(0, 0, 2 * r, 2 * r, r, 0.10 * H, 0.10 * H)
        p.ring(W - 2 * r, H - 2 * r, W, H, r, 0.10 * H, 0.10 * H)
        aw = 0.20 * H
        p.poly([(W - aw, 0), (W, 0), (aw, H), (0, H)])
    elif ch == "*":
        import math as _m
        cy, R = 0.34 * H, 0.30 * H
        for k in range(3):
            a = k * _m.pi / 3
            dx, dy = _m.cos(a) * R, _m.sin(a) * R
            p.poly([(W / 2 - dx - sh * 0.30, cy - dy), (W / 2 - dx + sh * 0.30, cy - dy),
                    (W / 2 + dx + sh * 0.30, cy + dy), (W / 2 + dx - sh * 0.30, cy + dy)])
    elif ch == "=":
        p.rect(0, H * 0.34, W, H * 0.34 + sh * 0.75)
        p.rect(0, H * 0.62, W, H * 0.62 + sh * 0.75)
    elif ch == "#":
        t = sh * 0.55
        for k in (0.34, 0.62):
            p.rect(0, H * k, W, H * k + t)
        for k in (0.30, 0.62):
            p.poly([(W * k + 0.06 * H, 0.10 * H), (W * k + 0.06 * H + t, 0.10 * H),
                    (W * k - 0.06 * H + t, H), (W * k - 0.06 * H, H)])
    elif ch == "&":
        p.ring(W * 0.10, 0, W * 0.68, H * 0.44, 0.20 * H, sv * 0.85, sh)
        p.ring(0, H * 0.40, W * 0.80, H, 0.26 * H, sv * 0.85, sh)
        p.rect(W * 0.62, H * 0.40, W * 0.80, H * 0.62, cut=True)
        p.diag((W * 0.34, H * 0.34), (W - sv, H), sv * 0.85)
    else:
        raise ValueError("no glyph for %r" % ch)

    return p.result(), W


# ---------------------------------------------------------------- lowercase
W_LOWER = {
    "a": 0.92, "b": 0.95, "c": 0.88, "d": 0.95, "e": 0.92, "f": 0.62, "g": 0.95,
    "h": 0.92, "i": 0.284, "j": 0.54, "k": 0.88, "l": 0.284, "m": 1.36, "n": 0.92,
    "o": 0.96, "p": 0.95, "q": 0.95, "r": 0.62, "s": 0.86, "t": 0.64, "u": 0.92,
    "v": 0.90, "w": 1.30, "x": 0.90, "y": 0.90, "z": 0.84,
}
# icon glyphs, so arrows, closes and the shop's skin marks ride in the same
# atlas as the letters - one draw call, and they inherit the neon rim
W_ICON = {
    "◀": 0.80, "▶": 0.80, "▲": 0.86, "▼": 0.86,
    "✕": 0.86, "✓": 0.92, "⚙": 1.00,
    "▦": 0.96, "▤": 0.96, "▨": 0.96, "▩": 0.96, "⭕": 0.98,
    "⬢": 0.94, "✦": 0.94, "✧": 0.94, "✶": 0.94, "⎉": 0.96,
}
WIDTHS.update(W_LOWER)
WIDTHS.update(W_ICON)


def draw_lower(ch, H):
    """Lowercase and icons. Baseline is at y=H; the x-height band starts at
    y = H - XH*H; descenders run to y = H + DESC*H."""
    W = WIDTHS[ch] * H
    sv, sh = SV * H, SH * H
    xt = H - XH * H            # top of the x-height band
    db = H + DESC * H          # descender depth
    p = Pen(W, H)
    xr = 0.30 * H              # generic bowl radius
    mid_t, mid_b = (xt + H) / 2 - sh / 2, (xt + H) / 2 + sh / 2

    if ch == "a":
        p.ring(0, xt, W, H, xr, sv, sh)
        p.rect(W - sv, xt, W, H)
    elif ch == "b":
        p.rect(0, 0, sv, H)
        p.ring(0, xt, W, H, xr, sv, sh)
    elif ch == "c":
        p.ring(0, xt, W, H, xr, sv, sh)
        p.rect(W - sv - 1, xt + sh * 1.1, W + 2, H - sh * 1.1, cut=True)
    elif ch == "d":
        p.ring(0, xt, W, H, xr, sv, sh)
        p.rect(W - sv, 0, W, H)
    elif ch == "e":
        p.ring(0, xt, W, H, xr, sv, sh)
        p.rect(0, mid_t, W, mid_b)
        p.rect(W - sv - 1, mid_b, W + 2, H - sh * 1.1, cut=True)
    elif ch == "f":
        # flat-topped f, which suits a geometric techno face better than a hook
        sx = 0.02 * H
        p.rect(sx, 0.10 * H, sx + sv, H)
        p.rect(sx, 0.10 * H, W, 0.10 * H + sh)
        p.rect(-0.06 * H, xt, W * 0.92, xt + sh)
    elif ch == "g":
        # bowl, then a shallow hook: a thin-walled ring with its top bar removed
        hookTop, hookW, hookTh = db - 0.38 * H, 0.62 * W, 0.13 * H
        p.ring(0, xt, W, H, xr, sv, sh)
        p.ring(W - hookW, hookTop, W, db, 0.15 * H, sv, hookTh,
               corners=(False, False, True, True))
        p.rect(-2, hookTop - 2, W + 2, hookTop + hookTh, cut=True)
        p.rect(W - sv, xt + sh, W, db - 0.15 * H)
    elif ch == "h":
        p.rect(0, 0, sv, H)
        p.ring(0, xt, W, xt + 2 * xr, xr, sv, sh, corners=(True, True, False, False))
        p.rect(-2, xt + xr, W + 2, db + 2, cut=True)
        p.rect(0, 0, sv, H)
        p.rect(W - sv, xt + xr - 1, W, H)
    elif ch == "i":
        p.rect(0, xt, W, H)
        p.rect(0, xt - 0.36 * H, W, xt - 0.36 * H + 0.22 * H)
    elif ch == "j":
        hookTop, hookW, hookTh = db - 0.38 * H, 0.72 * W, 0.13 * H
        p.ring(W - hookW, hookTop, W, db, 0.15 * H, sv, hookTh,
               corners=(False, False, True, True))
        p.rect(-2, hookTop - 2, W + 2, hookTop + hookTh, cut=True)
        p.rect(W - sv, xt, W, db - 0.15 * H)
        p.rect(W - sv, xt - 0.36 * H, W, xt - 0.36 * H + 0.22 * H)
    elif ch == "k":
        aw = 0.30 * H
        p.rect(0, 0, sv, H)
        p.diag((W - aw, xt), (sv * 0.5, (xt + H) / 2 + aw * 0.15), aw)
        p.diag((sv * 0.5, (xt + H) / 2 - aw * 0.15), (W - aw, H), aw)
    elif ch == "l":
        p.rect(0, 0, sv, H)
    elif ch == "m":
        third = (W - sv) / 2
        p.rect(0, xt, sv, H)
        for k in (1, 2):
            x0 = third * (k - 1)
            p.ring(x0, xt, x0 + third + sv, xt + 2 * xr, xr, sv, sh,
                   corners=(True, True, False, False))
        p.rect(-2, xt + xr, W + 2, db + 2, cut=True)
        p.rect(0, xt, sv, H)
        p.rect(third - sv / 2, xt + xr - 1, third + sv / 2, H)
        p.rect(W - sv, xt + xr - 1, W, H)
    elif ch == "n":
        p.rect(0, xt, sv, H)
        p.ring(0, xt, W, xt + 2 * xr, xr, sv, sh, corners=(True, True, False, False))
        p.rect(-2, xt + xr, W + 2, db + 2, cut=True)
        p.rect(0, xt, sv, H)
        p.rect(W - sv, xt + xr - 1, W, H)
    elif ch == "o":
        p.ring(0, xt, W, H, xr, sv, sh)
    elif ch == "p":
        p.rect(0, xt, sv, db)
        p.ring(0, xt, W, H, xr, sv, sh)
    elif ch == "q":
        p.rect(W - sv, xt, W, db)
        p.ring(0, xt, W, H, xr, sv, sh)
    elif ch == "r":
        p.rect(0, xt, sv, H)
        p.ring(0, xt, W, xt + 2 * xr, xr, sv, sh,
               corners=(True, True, False, False))
        p.rect(-2, xt + xr, W + 2, db + 2, cut=True)
        p.rect(0, xt, sv, H)
    elif ch == "s":
        rt = min(0.24 * H, ((H - xt) / 2) / 2 - 1)
        p.ring(0, xt, W, mid_b, rt, sv, sh, corners=(True, True, False, False))
        p.ring(0, mid_t, W, H, rt, sv, sh, corners=(False, False, True, True))
        p.rect(W - sv - 1, xt + sh * 0.9, W + 2, mid_t, cut=True)
        p.rect(-2, mid_b, sv + 1, H - sh * 0.9, cut=True)
    elif ch == "t":
        p.rect(0.02 * H, 0.22 * H, 0.02 * H + sv, H)
        p.ring(0.02 * H, H - 2 * xr, 0.02 * H + sv + 2 * xr, H, xr, sv, sh,
               corners=(False, False, True, False))
        p.rect(-2, -2, W + 2, H - xr, cut=True)
        p.rect(0.02 * H, 0.22 * H, 0.02 * H + sv, H - xr + 1)
        p.rect(-0.10 * H, xt, W, xt + sh)
    elif ch == "u":
        p.ring(0, xt, W, H, xr, sv, sh, corners=(False, False, True, True))
        p.rect(sv, xt - 2, W - sv, xt + sh, cut=True)
        p.rect(W - sv, xt, W, H)
    elif ch == "v":
        aw = 0.32 * H
        p.poly([(0, xt), (aw, xt), (W / 2 + aw / 2, H), (W / 2 - aw / 2, H)])
        p.poly([(W, xt), (W - aw, xt), (W / 2 - aw / 2, H), (W / 2 + aw / 2, H)])
    elif ch == "w":
        aw = 0.26 * H
        q1, q2 = W * 0.30, W * 0.70
        p.poly([(0, xt), (aw, xt), (q1 + aw / 2, H), (q1 - aw / 2, H)])
        p.poly([(W / 2, xt), (W / 2 - aw, xt), (q1 - aw / 2, H), (q1 + aw / 2, H)])
        p.poly([(W / 2, xt), (W / 2 + aw, xt), (q2 + aw / 2, H), (q2 - aw / 2, H)])
        p.poly([(W, xt), (W - aw, xt), (q2 - aw / 2, H), (q2 + aw / 2, H)])
    elif ch == "x":
        bw = 0.32 * H
        p.poly([(0, xt), (bw, xt), (W, H), (W - bw, H)])
        p.poly([(W, xt), (W - bw, xt), (0, H), (bw, H)])
    elif ch == "y":
        aw = 0.30 * H
        p.poly([(0, xt), (aw, xt), (W * 0.62 + aw / 2, H), (W * 0.62 - aw / 2, H)])
        p.poly([(W, xt), (W - aw, xt), (W * 0.18, db), (W * 0.18 - aw, db)])
    elif ch == "z":
        aw = 0.32 * H
        p.rect(0, xt, W, xt + sh)
        p.rect(0, H - sh, W, H)
        p.poly([(W - aw, xt + sh * 0.2), (W, xt + sh * 0.2),
                (aw, H - sh * 0.2), (0, H - sh * 0.2)])

    # ---- icons -----------------------------------------------------------
    elif ch in ("◀", "▶", "▲", "▼"):
        cy = (xt + H) / 2
        s = 0.34 * H
        if ch == "◀":
            p.poly([(0, cy), (W, cy - s), (W, cy + s)])
        elif ch == "▶":
            p.poly([(W, cy), (0, cy - s), (0, cy + s)])
        elif ch == "▲":
            p.poly([(W / 2, cy - s), (W, cy + s * 0.85), (0, cy + s * 0.85)])
        else:
            p.poly([(W / 2, cy + s), (W, cy - s * 0.85), (0, cy - s * 0.85)])
    elif ch == "✕":
        bw = 0.22 * H
        p.diag((0, 0.10 * H), (W - bw, H), bw)
        p.diag((W - bw, 0.10 * H), (0, H), bw)
    elif ch == "✓":
        bw = 0.20 * H
        p.diag((0, (xt + H) / 2), (W * 0.34 - bw, H), bw)
        p.diag((W - bw, xt - 0.08 * H), (W * 0.34 - bw, H), bw)
    elif ch in ("▦", "▤", "▨", "▩"):
        # the shop's pattern marks: a square with the skin's motif inside
        y0, y1 = 0.10 * H, H
        t = 0.10 * H
        p.ring(0, y0, W, y1, 0.06 * H, t, t)
        inner0, inner1 = y0 + t * 1.9, y1 - t * 1.9
        gap = (inner1 - inner0) / 4.0
        if ch == "▤":                                   # stripes
            for k in range(3):
                yy = inner0 + gap * (k + 0.4)
                p.rect(t * 1.9, yy, W - t * 1.9, yy + gap * 0.55)
        elif ch == "▦":                                 # grid
            for k in range(3):
                yy = inner0 + gap * (k + 0.4)
                p.rect(t * 1.9, yy, W - t * 1.9, yy + gap * 0.35)
            xg = (W - t * 3.8) / 3.0
            for k in range(2):
                xx = t * 1.9 + xg * (k + 1)
                p.rect(xx - gap * 0.18, inner0, xx + gap * 0.18, inner1)
        elif ch == "▩":                                 # dense block
            p.rect(t * 2.2, inner0, W - t * 2.2, inner1)
        else:                                           # checker
            cw, chh = (W - t * 3.8) / 2.0, (inner1 - inner0) / 2.0
            p.rect(t * 1.9, inner0, t * 1.9 + cw, inner0 + chh)
            p.rect(t * 1.9 + cw, inner0 + chh, t * 1.9 + 2 * cw, inner0 + 2 * chh)
    elif ch == "⭕":
        cy, R = (0.10 * H + H) / 2, 0.44 * H
        p.ring(W / 2 - R, cy - R, W / 2 + R, cy + R, R, 0.13 * H, 0.13 * H)
    elif ch == "⬢":
        import math as _m
        cy, R = (0.10 * H + H) / 2, 0.46 * H
        pts = [(W / 2 + R * _m.cos(_m.pi / 6 + k * _m.pi / 3),
                cy + R * _m.sin(_m.pi / 6 + k * _m.pi / 3)) for k in range(6)]
        p.poly(pts)
        inner = [(W / 2 + (R - 0.13 * H) * _m.cos(_m.pi / 6 + k * _m.pi / 3),
                  cy + (R - 0.13 * H) * _m.sin(_m.pi / 6 + k * _m.pi / 3)) for k in range(6)]
        p.poly(inner, cut=True)
    elif ch in ("✦", "✧", "✶"):
        import math as _m
        cy = (0.10 * H + H) / 2
        R, r = 0.46 * H, (0.13 * H if ch == "✶" else 0.16 * H)
        spikes = 6 if ch == "✶" else 4
        pts = []
        for k in range(spikes * 2):
            a = -_m.pi / 2 + k * _m.pi / spikes
            rad = R if k % 2 == 0 else r
            pts.append((W / 2 + rad * _m.cos(a), cy + rad * _m.sin(a)))
        p.poly(pts)
        if ch == "✧":                                   # hollow variant
            inner = [(W / 2 + (x - W / 2) * 0.55, cy + (y - cy) * 0.55) for x, y in pts]
            p.poly(inner, cut=True)
    elif ch == "⎉":
        # circuit: a ring with four stubs running to the edges
        cy, R = (0.10 * H + H) / 2, 0.26 * H
        p.ring(W / 2 - R, cy - R, W / 2 + R, cy + R, R, 0.11 * H, 0.11 * H)
        t = 0.055 * H
        p.rect(W / 2 - t, 0.10 * H, W / 2 + t, cy - R)
        p.rect(W / 2 - t, cy + R, W / 2 + t, H)
        p.rect(0, cy - t, W / 2 - R, cy + t)
        p.rect(W / 2 + R, cy - t, W, cy + t)
    elif ch == "⚙":
        cx, cy, R = W / 2, (0.10 * H + H) / 2, 0.40 * H
        p.ring(cx - R, cy - R, cx + R, cy + R, R, 0.16 * H, 0.16 * H)
        import math
        for k in range(8):
            a = k * math.pi / 4
            tx, ty = cx + math.cos(a) * R, cy + math.sin(a) * R
            t = 0.10 * H
            p.rect(tx - t, ty - t, tx + t, ty + t)
    else:
        raise ValueError("no glyph for %r" % ch)

    return p.result(), W


_base_draw = draw_glyph


def draw_glyph(ch, H):                                    # noqa: F811
    if ch in W_LOWER or ch in W_ICON:
        return draw_lower(ch, H)
    return _base_draw(ch, H)
