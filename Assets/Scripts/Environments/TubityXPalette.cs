using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The one place the game's brand colours live: the cyan -> magenta neon axis of the
    /// TubityX title logo (Resources/UI/Mat_TubityXLogo's _ColorA / _ColorB), the near-black
    /// violet ink its body is cut from, and the blue / violet button rims of the main menu.
    ///
    /// The environments reach their colours through the helpers here instead of hard-coding
    /// them, which is what keeps six very different worlds reading as one product:
    ///
    ///   matter - sky bands, fog, prop albedo - keeps the theme's own hue, so Jungle still
    ///            looks green and Volcano still looks hot. Nothing is hue-shifted to cyan.
    ///   energy - grid lines, emissive trim, glow props - goes through Neon(), which forces
    ///            the theme hue into the logo's register: high chroma, full value.
    ///   accent - rim light, neon halos, secondary motes - goes through Accent(), which is
    ///            brand magenta carrying a little of the theme hue. Every world therefore
    ///            wears the same two-tone neon rim the logo does.
    ///   ink    - every deep shadow, sky floor and tube backdrop is Deep(): the logo's body
    ///            colour with a breath of theme hue, instead of each theme inventing its
    ///            own black.
    ///
    /// Keep these in sync with Mat_TubityXLogo and TubityXUIFactory's rim colours.
    /// </summary>
    public static class TubityXPalette
    {
        /// <summary>Logo gradient start (_ColorA). The "powered on" end of the brand axis.</summary>
        public static readonly Color Cyan = new Color(0.42f, 0.93f, 1.00f);

        /// <summary>Logo gradient end (_ColorB). The accent end of the brand axis.</summary>
        public static readonly Color Magenta = new Color(1.00f, 0.30f, 0.86f);

        /// <summary>The menu's Settings rim (TubityXUIFactory.Purple).</summary>
        public static readonly Color Violet = new Color(0.62f, 0.32f, 1.00f);

        /// <summary>The menu's Play rim (TubityXUIFactory.Blue).</summary>
        public static readonly Color Blue = new Color(0.20f, 0.62f, 1.00f);

        /// <summary>Logo body colour (_BodyColor): the near-black violet everything dark sits on.</summary>
        public static readonly Color Ink = new Color(0.030f, 0.035f, 0.075f);

        /// <summary>Logo glass highlight (_GlassColor): the cool white specular of the brand.</summary>
        public static readonly Color Glass = new Color(0.62f, 0.76f, 0.95f);

        /// <summary>
        /// Forces a colour into the brand's neon register - the theme keeps its hue, but
        /// gains the chroma and value the logo's strokes have. This is what stops a theme
        /// colour reading as "painted plastic" next to the menu.
        /// </summary>
        public static Color Neon(Color hue, float minSaturation = 0.80f, float minValue = 1f)
        {
            float h, s, v;
            Color.RGBToHSV(hue, out h, out s, out v);
            Color c = Color.HSVToRGB(h, Mathf.Max(s, minSaturation), Mathf.Max(v, minValue));
            c.a = hue.a;
            return c;
        }

        /// <summary>
        /// Brand magenta carrying <paramref name="lean"/> of the theme's own hue. Used for the rim
        /// light, prop halos and accent motes, so the magenta half of the logo's gradient turns up
        /// in every environment without any two of them looking alike.
        /// </summary>
        public static Color Accent(Color hue, float lean = 0.28f)
        {
            Color c = Color.Lerp(Magenta, Neon(hue), Mathf.Clamp01(lean));
            c.a = 1f;
            return c;
        }

        /// <summary>
        /// The counterweight to Accent: brand cyan carrying a little theme hue, for the cool
        /// side of a two-tone lighting setup and for grid lines that want to sit closer to
        /// the logo than to their own world.
        /// </summary>
        public static Color Cool(Color hue, float lean = 0.28f)
        {
            Color c = Color.Lerp(Cyan, Neon(hue), Mathf.Clamp01(lean));
            c.a = 1f;
            return c;
        }

        /// <summary>
        /// The logo's ink with a breath of theme hue mixed in. Every sky floor, fog-dark and
        /// tube backdrop in the game is built from this, so the blacks all match.
        /// </summary>
        public static Color Deep(Color hue, float t = 0.18f)
        {
            Color tinted = new Color(hue.r * 0.22f, hue.g * 0.22f, hue.b * 0.22f, 1f);
            Color c = Color.Lerp(Ink, tinted, Mathf.Clamp01(t));
            c.a = hue.a;
            return c;
        }

        /// <summary>Ink with an alpha, for the transparent tube's grid backdrop.</summary>
        public static Color DeepAlpha(Color hue, float t, float alpha)
        {
            Color c = Deep(hue, t);
            c.a = alpha;
            return c;
        }
    }
}
