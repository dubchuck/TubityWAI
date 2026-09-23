using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>One theme pinned to a distance along the tube. See EnvironmentBlend.</summary>
    public class EnvironmentStop
    {
        /// <summary>The theme in full effect at <see cref="distance"/>. None is the classic neon tunnel.</summary>
        public EnvironmentTheme theme;

        /// <summary>Distance from the level start, in tube units, where this theme is fully applied.</summary>
        public float distance;

        /// <summary>
        /// How many tube units before <see cref="distance"/> the cross-fade from the previous stop runs.
        /// Zero or less means "use the whole gap", so the level morphs continuously between the two stops.
        /// A positive value holds the previous theme steady and then swaps over this final stretch.
        /// </summary>
        public float blendLength;

        /// <summary>
        /// Optional palette for this stop, instead of the theme's shared one. This is what lets a
        /// single theme appear at several stops with different sky, fog and sun settings - a solar
        /// system level is one SolarSystem theme whose sun grows across half a dozen stops.
        /// The theme still drives trackside scenery; only the atmosphere comes from here.
        /// </summary>
        public EnvironmentPalette palette;

        public EnvironmentStop(EnvironmentTheme theme, float distance, float blendLength = 0f, EnvironmentPalette palette = null)
        {
            this.theme = theme;
            this.distance = distance;
            this.blendLength = blendLength;
            this.palette = palette;
        }
    }

    /// <summary>
    /// An ordered list of environment stops the level cross-fades through as the player flies forward.
    /// Any number of stops and any combination of themes works, including EnvironmentTheme.None for the
    /// classic neon tunnel, so "solid tube that opens into the jungle canopy" is just
    /// <c>EnvironmentBlend.From(None).To(Jungle, 700f)</c>.
    ///
    /// Everything downstream reads the blend through three calls:
    ///   - <see cref="Evaluate"/> for the interpolated palette (sky, fog, light, tube colours),
    ///   - <see cref="Sample"/> when both endpoints are needed at once (ambient particles),
    ///   - <see cref="Weight"/> for how much of one theme's trackside scenery to show.
    /// </summary>
    public class EnvironmentBlend
    {
        public readonly List<EnvironmentStop> stops = new List<EnvironmentStop>();

        /// <summary>Smoothstep the cross-fade so the transition eases in and out instead of starting abruptly.</summary>
        public bool smooth = true;

        // Reused so evaluating every frame allocates nothing.
        private readonly EnvironmentPalette scratch = new EnvironmentPalette();

        public static EnvironmentBlend From(EnvironmentTheme theme, float distance = 0f)
        {
            EnvironmentBlend blend = new EnvironmentBlend();
            blend.stops.Add(new EnvironmentStop(theme, distance));
            return blend;
        }

        /// <summary>Starts the blend on an explicit palette rather than a theme's shared one.</summary>
        public static EnvironmentBlend From(EnvironmentPalette palette, float distance = 0f)
        {
            EnvironmentBlend blend = new EnvironmentBlend();
            if (palette != null) blend.stops.Add(new EnvironmentStop(palette.theme, distance, 0f, palette));
            return blend;
        }

        /// <summary>Adds the next stop. Distances must increase; a stop at or before the previous one is ignored.</summary>
        public EnvironmentBlend To(EnvironmentTheme theme, float distance, float blendLength = 0f)
        {
            if (stops.Count > 0 && distance <= stops[stops.Count - 1].distance)
            {
                Debug.LogWarning("[EnvironmentBlend] Ignoring stop " + theme + " at " + distance +
                                 " - stops must be ordered by increasing distance.");
                return this;
            }
            stops.Add(new EnvironmentStop(theme, distance, blendLength));
            return this;
        }

        /// <summary>
        /// Adds a stop carrying its own palette. Consecutive stops may share a theme - scenery then
        /// stays put at full weight while the sky, fog, sun and lighting keep moving between them.
        /// </summary>
        public EnvironmentBlend To(EnvironmentPalette palette, float distance, float blendLength = 0f)
        {
            if (palette == null) return this;
            if (stops.Count > 0 && distance <= stops[stops.Count - 1].distance)
            {
                Debug.LogWarning("[EnvironmentBlend] Ignoring palette stop " + palette.displayName + " at " + distance +
                                 " - stops must be ordered by increasing distance.");
                return this;
            }
            stops.Add(new EnvironmentStop(palette.theme, distance, blendLength, palette));
            return this;
        }

        public EnvironmentBlend WithSmoothing(bool enabled)
        {
            smooth = enabled;
            return this;
        }

        public bool IsValid { get { return stops.Count > 0; } }

        /// <summary>The theme the level starts in, which is what the one-off setup at level start keys off.</summary>
        public EnvironmentTheme FirstTheme { get { return stops.Count > 0 ? stops[0].theme : EnvironmentTheme.None; } }

        public EnvironmentTheme LastTheme { get { return stops.Count > 0 ? stops[stops.Count - 1].theme : EnvironmentTheme.None; } }

        /// <summary>True if any stop uses a themed environment, i.e. the level needs a sky, fog and scenery at all.</summary>
        public bool HasThemedStop
        {
            get
            {
                for (int i = 0; i < stops.Count; i++)
                {
                    if (stops[i].theme != EnvironmentTheme.None) return true;
                }
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Sampling
        // ------------------------------------------------------------------

        public int StopCount { get { return stops.Count; } }

        /// <summary>
        /// The palette a stop contributes: its own override, or its theme's shared one. Never mutate
        /// the result - a theme's palette is a cached singleton shared by every level using it.
        /// </summary>
        public EnvironmentPalette PaletteOf(int index)
        {
            if (index < 0 || index >= stops.Count) return null;
            EnvironmentStop stop = stops[index];
            return stop.palette ?? EnvironmentPalettes.GetOrClassic(stop.theme);
        }

        /// <summary>
        /// The two stops straddling <paramref name="z"/> and how far between them we are. Equal indices
        /// mean the level has settled on one stop and nothing is cross-fading.
        ///
        /// Callers that need to know whether anything is still moving must compare these indices rather
        /// than the themes from <see cref="Sample"/>: with per-stop palettes two adjacent stops can share
        /// a theme and still be mid-transition.
        /// </summary>
        public void SampleStops(float z, out int from, out int to, out float t)
        {
            from = to = 0;
            t = 0f;
            if (stops.Count == 0) { from = to = -1; return; }

            if (stops.Count == 1 || z <= stops[0].distance) return;

            if (z >= stops[stops.Count - 1].distance)
            {
                from = to = stops.Count - 1;
                return;
            }

            for (int i = 0; i < stops.Count - 1; i++)
            {
                EnvironmentStop a = stops[i];
                EnvironmentStop b = stops[i + 1];
                if (z < a.distance || z >= b.distance) continue;

                from = i;
                to = i + 1;

                // A positive blendLength parks the cross-fade at the end of the gap; otherwise it spans it.
                float fadeStart = b.blendLength > 0f
                    ? Mathf.Max(a.distance, b.distance - b.blendLength)
                    : a.distance;

                float raw = b.distance > fadeStart ? Mathf.InverseLerp(fadeStart, b.distance, z) : 1f;
                t = smooth ? Mathf.SmoothStep(0f, 1f, raw) : raw;
                return;
            }
        }

        /// <summary>
        /// The two themes straddling <paramref name="z"/> and how far between them we are.
        /// Outside the stop range both themes are the same and t is 0.
        /// </summary>
        public void Sample(float z, out EnvironmentTheme from, out EnvironmentTheme to, out float t)
        {
            from = to = EnvironmentTheme.None;
            int ia, ib;
            SampleStops(z, out ia, out ib, out t);
            if (ia < 0) { t = 0f; return; }

            from = stops[ia].theme;
            to = stops[ib].theme;

            // Two stops that only differ in palette are one theme as far as scenery is concerned.
            if (from == to) t = 0f;
        }

        /// <summary>
        /// The palette at <paramref name="z"/>. The returned instance is reused between calls, so copy
        /// it if it needs to outlive the next Evaluate.
        /// </summary>
        public EnvironmentPalette Evaluate(float z)
        {
            int ia, ib;
            float t;
            SampleStops(z, out ia, out ib, out t);
            if (ia < 0) return null;

            EnvironmentPalette a = PaletteOf(ia);
            if (ia == ib) return a;

            EnvironmentPalette.LerpInto(a, PaletteOf(ib), t, scratch);
            return scratch;
        }

        /// <summary>The theme that dominates at <paramref name="z"/>, for anything that cannot cross-fade.</summary>
        public EnvironmentTheme ThemeAt(float z)
        {
            EnvironmentTheme from, to;
            float t;
            Sample(z, out from, out to, out t);
            return t < 0.5f ? from : to;
        }

        /// <summary>
        /// How present <paramref name="theme"/> is at <paramref name="z"/>, in 0..1. Used to dissolve
        /// trackside scenery in and out prop by prop rather than swapping whole segments at once.
        /// </summary>
        public float Weight(EnvironmentTheme theme, float z)
        {
            EnvironmentTheme from, to;
            float t;
            Sample(z, out from, out to, out t);

            if (from == to) return from == theme ? 1f : 0f;

            float weight = 0f;
            if (from == theme) weight += 1f - t;
            if (to == theme) weight += t;
            return weight;
        }

        /// <summary>Appends every theme with a non-zero weight at <paramref name="z"/> to <paramref name="into"/>.</summary>
        public void ActiveThemes(float z, List<EnvironmentTheme> into)
        {
            into.Clear();
            EnvironmentTheme from, to;
            float t;
            Sample(z, out from, out to, out t);

            if (t < 1f) into.Add(from);
            if (to != from && t > 0f) into.Add(to);
        }

        /// <summary>Every distinct theme the blend ever visits, for one-off setup at level start.</summary>
        public void AllThemes(List<EnvironmentTheme> into)
        {
            into.Clear();
            for (int i = 0; i < stops.Count; i++)
            {
                if (!into.Contains(stops[i].theme)) into.Add(stops[i].theme);
            }
        }
    }
}
