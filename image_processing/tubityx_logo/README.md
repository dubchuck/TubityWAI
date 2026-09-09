# TubityX logo and typeface — regenerating the art

The logo is not a picture. `Assets/Resources/UI/Tex_TubityXLogo.png` is a packed
data texture and `Assets/Shaders/TubityXLogo.shader` paints everything from it:

| channel | contents |
| ------- | -------- |
| R | signed distance field of the wordmark, 0.5 = the glyph outline, ±28 px range |
| G | framing-line / triangle / ring mask |
| B | soft glow of the framing lines |

Because R is a distance field, the neon rim, the dark gap, the inline contour,
the frosted body and the bloom halo are all just distance bands in the shader —
so their widths and colours are material sliders, not baked pixels.

## Files

* `glyphs.py` — the seven letterforms as vector outlines (T U B I T Y X),
  measured off the original mockup: cap height 88, vertical stem 0.290 H,
  horizontal bar 0.190 H, tracking 0.057 H. Edit `WIDTHS`, `SV`, `SH` here to
  change the type itself.
* `bake.py` — lays the wordmark out, draws the framing lines, computes the SDF
  at 4× supersample and writes `Tex_TubityXLogo.png` (1024 × 256).
* `sim.py` — a NumPy port of the fragment shader. Renders the finished look
  offline so you can try band widths and colours without opening Unity.

## Usage

```sh
python3 bake.py          # -> Tex_TubityXLogo.png
python3 sim.py           # -> preview.png
```

Then copy `Tex_TubityXLogo.png` over `Assets/Resources/UI/Tex_TubityXLogo.png`.
Keep the 1024 × 256 size and the 28 px spread, or update `_Spread` on the
material to match.

Requires `numpy`, `scipy` and `pillow`.

## Colour space

`sim.py` composites in **linear** (`LINEAR = True`) because the project is set to
Linear colour space and the menu canvas is Screen Space - Overlay, so no URP
bloom reaches the logo — every bit of glow comes from the shader. If the project
is ever switched to Gamma, set `LINEAR = False` and re-tune: the same numbers
look markedly different, mostly in the halo and the glass body.

## If you change the layout

`bake.py` prints the wordmark's bounds in texture pixels. The shader's
`_GradStart` / `_GradEnd` are in texture UV, so if the wordmark moves, re-check
where the cyan→magenta ramp lands (it should finish just before the X).


---

# The TubityX display face

The buttons use the same letterforms as the logo, extended to a full alphabet.
There is no .ttf anywhere in this — the glyphs are vector outlines drawn in
`glyphs_ext.py` and baked into an SDF atlas.

* `glyphs_ext.py` — A–Z, 0–9 and `+ - . , : ! ? ' / ( ) &` in the same system as
  the logo (stem 0.290 H, bar 0.190 H). The six logo letters come from
  `glyphs.py` unchanged, so editing this file can never alter the logo.
* `bake_font.py` — packs every glyph into `Tex_TubityXFont.png` (512 × 624, one
  channel, SDF at cap height 56 with a 10 px spread) and generates
  `TubityXFontMetrics.cs`, a compile-time lookup table so nothing is parsed at
  runtime.
* `sim_button.py` — offline preview of the panel and label shaders together.

```sh
python3 bake_font.py     # -> Tex_TubityXFont.png + TubityXFontMetrics.cs
```

Copy the PNG to `Assets/Resources/UI/` and the .cs to `Assets/Scripts/UI/`.
Both are regenerated wholesale; don't hand-edit either.

## Adding a glyph

Add its width to `W_EXT` and a branch to `draw_glyph`, then re-bake. `Pen` cuts
are applied the moment they are drawn, so you can safely add strokes after
subtracting a notch — the earlier version accumulated all cuts to the end, which
silently erased anything drawn after them.

## Buttons

`TubityXPanel` evaluates its rounded rectangle analytically — no texture — and
carries its size, corner radius and hover state in TEXCOORD1. That is what lets
buttons of different sizes share one material, and why hover costs a vertex
rewrite rather than a material instance. The canvas has to be told to send that
channel (`additionalShaderChannels |= TexCoord1`); the components do it
themselves, and `MainMenu` sets it when it creates the canvas.

Button colour comes from the material, so each colourway is its own asset:
`Mat_TubityXButtonBlue` (border pulses, 2.2 s) and `Mat_TubityXButtonPurple`
(steady). Label accent colour rides in TEXCOORD1 instead, so every label in the
menu shares `Mat_TubityXLabel`.
