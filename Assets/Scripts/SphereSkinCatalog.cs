using System;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The twelve sphere skins sold in the cosmetics shop.
    ///
    /// Every skin is a restyling of the attract-screen hero: the same baked band
    /// winding (NeonBandData), re-coloured and thinned out per skin. A skin never
    /// carries its own fixed colours - it is a recipe that takes the sphere's
    /// gameplay colour and returns the ribbon colours, so five spheres in five
    /// colours stay tellable apart with any skin on, which the coin matching
    /// depends on.
    ///
    /// Indices are what PlayerPrefs stores, so append new skins; never reorder.
    /// </summary>
    public static class SphereSkinCatalog
    {
        /// <summary>What a skin knows about one ribbon while colouring it.</summary>
        public struct BandCtx
        {
            /// <summary>0 at the first included band of this family, 1 at the last.</summary>
            public float T;
            /// <summary>Ordinal among included bands of the same family.</summary>
            public int Ordinal;
            public bool Cross;
            /// <summary>The sphere's gameplay colour.</summary>
            public Color Base;
        }

        /// <summary>
        /// The extra pieces a skin hangs on the sphere, over and above its
        /// ribbons. SkinFlair builds and animates them.
        /// </summary>
        [Flags]
        public enum Flair
        {
            None        = 0,
            Corona      = 1 << 0,   // soft ball of light over the whole sphere
            Flicker     = 1 << 1,   // ribbon intensity jitters like a real tube
            Pulse       = 1 << 2,   // ribbon intensity breathes slowly
            CounterSpin = 1 << 3,   // the crossing family on its own, spun the other way
            SaturnRing  = 1 << 4,   // one wide flat ring, tilted
            Motes       = 1 << 5,   // small lights orbiting the sphere
            PolarJets   = 1 << 6,   // beams out of both poles
            GyroHoops   = 1 << 7,   // nested hoops tumbling on different axes
            Sparkle     = 1 << 8,   // brief twinkles on the surface
            HueCycle    = 1 << 9,   // ribbon hues rotate over time
            Embers      = 1 << 10,  // hot particles drifting up and away
            Wisps       = 1 << 11,  // slow faint mist
            Arcs        = 1 << 12,  // electric arcs jumping across the surface
            Shards      = 1 << 13,  // crystal spikes standing off the surface
            Glass       = 1 << 14,  // fresnel glass rim hugging the ball
        }

        /// <summary>Which colour a skin's flair takes, relative to the sphere's own.</summary>
        public enum Tint { Base, Complement, Gold, White, Warm, Cool }

        public sealed class Skin
        {
            public string Name;
            public string Blurb;
            public int Price;

            public Flair Flair = Flair.None;
            public Tint FlairTint = Tint.Base;
            /// <summary>Multiplies the sphere's own spin; negative reverses it.</summary>
            public float SpinScale = 1f;

            /// <summary>Which of the baked bands survive. Index is into NeonBandData.Bands.</summary>
            public Func<NeonBandData.Band, int, bool> Include = (b, i) => true;
            /// <summary>Ribbon colour for one band.</summary>
            public Func<BandCtx, Color> Colour = c => c.Base;

            /// <summary>Multiplies every ribbon's width.</summary>
            public float WidthScale = 1f;
            /// <summary>NeonBand material _Intensity.</summary>
            public float Intensity = 1.6f;
            /// <summary>NeonBand material _CoreWhite: how white the ribbon centre burns.</summary>
            public float CoreWhite = 0.45f;

            /// <summary>How much of the base colour the dark core body carries (0 = the hero's near-black).</summary>
            public float CoreBody = 0f;
            /// <summary>Fresnel rim on the core, as a multiple of the base colour.</summary>
            public float CoreRim = 0.45f;
            public float RimStrength = 0.85f;
            /// <summary>In-game only: emission the URP core material glows with, so marker pulses still read.</summary>
            public float CoreGlow = 0.22f;
        }

        // ------------------------------------------------------------ helpers

        private static Color HSV(float h, float s, float v)
        {
            h -= Mathf.Floor(h);
            return Color.HSVToRGB(h, Mathf.Clamp01(s), Mathf.Clamp01(v));
        }

        private static void ToHSV(Color c, out float h, out float s, out float v)
        {
            Color.RGBToHSV(c, out h, out s, out v);
            // the gameplay palette is fully saturated neon; keep some floor so a
            // desaturated custom colour still ramps somewhere
            s = Mathf.Max(s, 0.6f);
            v = Mathf.Max(v, 0.85f);
        }

        /// <summary>Resolve a flair tint against the sphere's colour.</summary>
        public static Color Resolve(Tint tint, Color baseColour)
        {
            switch (tint)
            {
                case Tint.Complement: return Shift(baseColour, 180f);
                case Tint.Gold:       return Gold;
                case Tint.White:      return White;
                case Tint.Warm:       return Color.Lerp(baseColour, new Color(1f, 0.45f, 0.1f), 0.55f);
                case Tint.Cool:       return Color.Lerp(baseColour, new Color(0.3f, 0.7f, 1f), 0.55f);
                default:              return baseColour;
            }
        }

        /// <summary>Hue-shift the base by `degrees`, keeping its saturation and value.</summary>
        public static Color Shift(Color c, float degrees)
        {
            float h, s, v;
            ToHSV(c, out h, out s, out v);
            return HSV(h + degrees / 360f, s, v);
        }

        /// <summary>
        /// The hero ramp, rebuilt around the sphere's own hue: a light tint at one
        /// pole sweeping into the full colour, with the hue drifting by `hueSpan`
        /// degrees on the way. Every "wound" skin is some setting of this.
        /// </summary>
        private static Color Ramp(Color c, float t, float hueSpan, float satFrom, float satTo, float valFrom, float valTo)
        {
            float h, s, v;
            ToHSV(c, out h, out s, out v);
            return HSV(h + (hueSpan * t) / 360f,
                       Mathf.Lerp(satFrom, satTo, t) * s,
                       Mathf.Lerp(valFrom, valTo, t) * v);
        }

        private static readonly Color Gold  = new Color(1.00f, 0.70f, 0.18f);
        private static readonly Color White = new Color(0.92f, 0.96f, 1.00f);

        private static bool Polar(NeonBandData.Band b)   { return b.Family == 1 || b.Theta < 0.95f || b.Theta > Mathf.PI - 0.95f; }
        private static bool Belt(NeonBandData.Band b)    { return b.Family == 1 || (b.Theta > 1.05f && b.Theta < Mathf.PI - 1.05f); }

        // ------------------------------------------------------------ the catalogue

        public static readonly Skin[] Skins =
        {
            // 0 - the hero look, tinted. Free and always unlocked.
            new Skin
            {
                Name = "NEON WIND", Blurb = "THE SIGNATURE WINDING", Price = 0,
                Colour = c => c.Cross ? (c.Ordinal % 2 == 0 ? Shift(c.Base, 40f) : Gold)
                                      : (c.Ordinal % 5 == 4 ? Gold
                                                            : Ramp(c.Base, c.T, 55f, 0.55f, 1f, 1f, 0.95f)),
            },

            // 1 - main winding only, burning toward white
            new Skin
            {
                Name = "SOLAR FLARE", Blurb = "WHITE-HOT RIBBONS", Price = 150,
                Include = (b, i) => b.Family == 0,
                Colour = c => Color.Lerp(White, c.Base, Mathf.Pow(c.T, 0.7f)),
                WidthScale = 1.3f, Intensity = 2.2f, CoreWhite = 0.7f,
                CoreBody = 0.05f, CoreRim = 0.6f, CoreGlow = 0.3f,
                Flair = Flair.Corona | Flair.Flicker, FlairTint = Tint.Warm, SpinScale = 1.3f,
            },

            // 2 - alternate the colour with its complement
            new Skin
            {
                Name = "TWIN TONE", Blurb = "COLOUR AND ITS OPPOSITE", Price = 250,
                // the crossing family is the complement, on its own and counter-spun
                Include = (b, i) => b.Family == 0,
                Colour = c => c.Ordinal % 2 == 0 ? c.Base : Shift(c.Base, 180f),
                WidthScale = 1.1f, Intensity = 1.7f,
                CoreRim = 0.35f,
                Flair = Flair.CounterSpin, FlairTint = Tint.Complement, SpinScale = 0.9f,
            },

            // 3 - every third ring, fat and bright
            new Skin
            {
                Name = "ORBITALS", Blurb = "A FEW WIDE RINGS", Price = 350,
                Include = (b, i) => b.Family == 1 || i % 3 == 0,
                Colour = c => c.Cross ? White : Ramp(c.Base, c.T, 0f, 0.7f, 1f, 1f, 1f),
                WidthScale = 1.9f, Intensity = 1.9f, CoreWhite = 0.55f,
                CoreBody = 0.04f, CoreRim = 0.5f,
                Flair = Flair.SaturnRing | Flair.Motes, FlairTint = Tint.White, SpinScale = 0.7f,
            },

            // 4 - only the "eyes" at the poles survive
            new Skin
            {
                Name = "POLAR CAPS", Blurb = "RINGS AT THE POLES ONLY", Price = 500,
                Include = (b, i) => Polar(b),
                Colour = c => c.Cross ? Gold : Ramp(c.Base, c.T, -70f, 0.6f, 1f, 1f, 0.9f),
                WidthScale = 1.35f, Intensity = 1.8f,
                CoreBody = 0.08f, CoreRim = 0.7f, RimStrength = 1.2f, CoreGlow = 0.3f,
                Flair = Flair.PolarJets, FlairTint = Tint.Cool, SpinScale = 1.1f,
            },

            // 5 - the band around the middle
            new Skin
            {
                Name = "EQUATOR BELT", Blurb = "WOUND AROUND THE MIDDLE", Price = 500,
                Include = (b, i) => Belt(b),
                Colour = c => c.Cross ? Shift(c.Base, 30f)
                                      : (c.Ordinal % 2 == 1 ? Gold : c.Base),
                WidthScale = 1.5f, Intensity = 1.8f,
                CoreBody = 0.05f, CoreRim = 0.45f,
                Flair = Flair.GyroHoops, FlairTint = Tint.Gold, SpinScale = 0.8f,
            },

            // 6 - the colour warmed toward gold, white accents
            new Skin
            {
                Name = "HONEY WIRE", Blurb = "WARM GOLD THREADS", Price = 650,
                Colour = c => c.Cross ? White
                                      : (c.Ordinal % 4 == 3 ? White : Color.Lerp(c.Base, Gold, 0.35f + 0.45f * c.T)),
                WidthScale = 0.85f, Intensity = 1.9f, CoreWhite = 0.3f,
                CoreBody = 0.06f, CoreRim = 0.5f,
                Flair = Flair.Sparkle, FlairTint = Tint.Gold, SpinScale = 1.2f,
            },

            // 7 - the hero's rainbow sweep, started from the sphere's own hue
            new Skin
            {
                Name = "SPECTRUM", Blurb = "A FULL HUE SWEEP", Price = 800,
                Colour = c => c.Cross ? Shift(c.Base, 90f + 60f * c.Ordinal)
                                      : (c.Ordinal % 5 == 4 ? Gold : Ramp(c.Base, c.T, 150f, 1f, 1f, 1f, 1f)),
                Intensity = 1.7f, CoreWhite = 0.4f,
                CoreRim = 0.3f,
                Flair = Flair.HueCycle, SpinScale = 1f,
            },

            // 8 - pale threads over a tinted glass body
            new Skin
            {
                Name = "GHOST", Blurb = "PALE THREADS, GLASS CORE", Price = 1000,
                Colour = c => Color.Lerp(White, c.Base, 0.35f + 0.25f * c.T),
                WidthScale = 0.6f, Intensity = 1.1f, CoreWhite = 0.85f,
                CoreBody = 0.45f, CoreRim = 1.2f, RimStrength = 1.6f, CoreGlow = 0.9f,
                Flair = Flair.Glass | Flair.Wisps | Flair.Pulse, FlairTint = Tint.White, SpinScale = 0.5f,
            },

            // 9 - thick, hot, and drifting toward the red end
            new Skin
            {
                Name = "INFERNO", Blurb = "OVERDRIVEN AND WIDE", Price = 1200,
                Colour = c => c.Cross ? Shift(c.Base, -35f) : Ramp(c.Base, c.T, -45f, 0.85f, 1f, 1f, 1f),
                WidthScale = 1.6f, Intensity = 2.8f, CoreWhite = 0.6f,
                CoreBody = 0.12f, CoreRim = 0.9f, RimStrength = 1.1f, CoreGlow = 0.45f,
                Flair = Flair.Embers | Flair.Pulse, FlairTint = Tint.Warm, SpinScale = 1.7f,
            },

            // 10 - the crossing family leads, the winding is halved and inverted
            new Skin
            {
                Name = "VOID LATTICE", Blurb = "SPARSE, INVERTED CAGE", Price = 1500,
                // half the winding, inverted; the crossing family counter-spins in the base colour
                Include = (b, i) => b.Family == 0 && i % 2 == 1,
                Colour = c => c.Ordinal % 3 == 0 ? c.Base : Shift(c.Base, 180f),
                WidthScale = 1.2f, Intensity = 2.0f, CoreWhite = 0.25f,
                CoreBody = 0f, CoreRim = 0.15f, RimStrength = 0.4f, CoreGlow = 0.15f,
                Flair = Flair.CounterSpin | Flair.Arcs, FlairTint = Tint.Base, SpinScale = -1.2f,
            },

            // 11 - three hues a third of the wheel apart
            new Skin
            {
                Name = "PRISM", Blurb = "THREE-WAY COLOUR SPLIT", Price = 2000,
                Colour = c => c.Cross ? White : Shift(c.Base, 120f * (c.Ordinal % 3)),
                WidthScale = 1.05f, Intensity = 2.1f, CoreWhite = 0.5f,
                CoreBody = 0.06f, CoreRim = 0.55f, CoreGlow = 0.3f,
                Flair = Flair.Shards | Flair.Sparkle, FlairTint = Tint.White, SpinScale = 0.6f,
            },
        };

        public static int Count { get { return Skins.Length; } }

        /// <summary>Never throws: an index outside the catalogue falls back to the free skin.</summary>
        public static Skin Get(int index)
        {
            if (index < 0 || index >= Skins.Length) return Skins[0];
            return Skins[index];
        }
    }
}
