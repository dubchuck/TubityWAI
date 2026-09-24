using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The world a level flies through. None keeps the classic neon tunnel;
    /// everything else swaps in a themed skybox, fog, lighting, ambient
    /// particles and trackside scenery (see EnvironmentManager / EnvironmentScenery).
    /// </summary>
    public enum EnvironmentTheme
    {
        None = 0,
        Space,
        Jungle,
        Underwater,
        Volcano,
        Crystal,
        Grid,
        // Appended, never reordered: LevelConfig serialises the theme as an int.
        SolarSystem,
        AsteroidBelt,
        // The second set (EnvironmentThemes.Worlds.cs / EnvironmentScenery.Worlds.cs).
        PrismHall,      // falling through a hall of mirrors
        Cavern,         // a lantern-lit mine cave
        LavaTube,       // inside a basalt lava tube, a molten river below
        SolarFlare,     // skimming a star's corona
        BlackHole,      // a lensed accretion disc filling the sky
        Thunderstorm,   // inside a storm cell, lightning all round
        SunsetCanyon,   // a desert canyon at golden hour
        Clockwork,      // the works of a giant clock
        BlossomArbor,   // a garden walk under flowering trellis arches
        CandyClouds     // a pastel sky of sweets
    }

    /// <summary>A huge far-off object a theme hangs in its sky (see EnvironmentBackdrops).</summary>
    public enum EnvironmentBackdrop
    {
        None,
        Sun,        // a boiling star with a streaming corona and flare loops on its limb
        BlackHole   // a shadow ringed by its lensed, flowing accretion disc
    }

    /// <summary>How the ambient particle field behaves for a theme.</summary>
    public enum AmbientParticleStyle
    {
        None,
        Stardust,   // still, twinkling motes
        Fireflies,  // slow wandering, blinking
        Bubbles,    // rise steadily, wobble
        Embers,     // rise fast, flicker out
        Snow,       // drift down slowly
        Motes,      // near-still neon dust, drifting toward the camera
        Ash,        // heavy flecks falling with a sideways sway
        // Appended with the second set of worlds.
        Rain,       // fast slanting streaks
        Petals,     // slow tumbling fall
        Streaks,    // bright plasma rushing past the camera
        Sparks      // short-lived hot flecks falling off machinery
    }

    /// <summary>
    /// Everything colour- and atmosphere-related for one theme, in one place.
    /// All values were tuned for the mobile URP renderer: one skybox pass,
    /// linear fog, a single directional light and one particle system.
    /// </summary>
    public class EnvironmentPalette
    {
        public EnvironmentTheme theme;
        public string displayName;

        // --- Procedural skybox (TubityX/EnvironmentSky) ---
        public Color skyTop;
        public Color skyHorizon;
        public Color skyBottom;
        public float horizonPower = 3f;
        public Color detailColor;          // nebula / cloud / caustic / aurora tint
        public float detailScale = 2f;
        public float detailStrength = 0.8f;
        public Vector2 detailScroll = new Vector2(0.01f, 0.003f);
        public Vector2 detailStretch = Vector2.one;
        public float starDensity = 0f;
        public Color starColor = Color.white;
        public Vector3 sunDirection = new Vector3(0.3f, 0.5f, 0.8f);
        public Color sunColor;
        public float sunSize = 0.03f;
        public float sunHalo = 1f;

        // --- Atmosphere ---
        public Color fogColor;             // keep equal to skyHorizon so far segments pop in unseen
        public float fogStart = 30f;
        public float fogEnd = 180f;
        public Color ambientLight;         // the equator band of the trilight ambient
        public Color ambientSky;           // light falling from above; black falls back to ambientLight
        public Color ambientGround;        // bounce from below; black falls back to ambientLight * 0.5
        public Color sunLightColor;
        public float sunLightIntensity = 0.5f;

        // --- Shadows (main directional light only; see EnvironmentManager.ApplySunLight) ---
        /// <summary>0 turns the shadow pass off entirely. Above 0 enables hard shadows at this opacity.</summary>
        public float shadowStrength = 0f;

        // --- Brand accent (see TubityXPalette) ---
        /// <summary>The magenta-family neon this theme wears: rim light, prop halos, accent motes.</summary>
        public Color accentColor = TubityXPalette.Magenta;

        /// <summary>
        /// A second, shadowless directional light in the accent colour, aimed across the sun so
        /// props get a coloured edge instead of falling flat into the fog. Kept below
        /// sunLightIntensity so URP never promotes it over the sun and moves the shadows.
        /// </summary>
        public float rimLightIntensity = 0f;
        public Vector3 rimDirection = new Vector3(-0.65f, -0.2f, -0.7f);

        // --- Tunnel ---
        public Color tubeTint;             // transparent tube material colour
        public Color tubeGridColor;        // procedural grid lines
        public Color tubeBaseColor;        // grid background (alpha = tube opacity)
        public float tubeEmission = 0.5f;  // emission multiplier on tubeTint; ~0 gives a matte, unlit wall

        // --- Player ---
        public Color sphereColor;
        public EnergySphereEffects.EffectPreset effectPreset = EnergySphereEffects.EffectPreset.Plasma;

        // --- Ambient particles ---
        public AmbientParticleStyle particleStyle = AmbientParticleStyle.None;
        public Color particleColor = Color.white;
        public float particleRate = 20f;
        public Vector2 particleSize = new Vector2(0.15f, 0.4f);

        // A second, sparser field layered under the first - pollen under fireflies, ash under
        // embers, accent motes under everything. Usually tinted accentColor, which is what puts
        // a little of the logo's magenta in the air of every world.
        public AmbientParticleStyle particleStyleB = AmbientParticleStyle.None;
        public Color particleColorB = Color.white;
        public float particleRateB = 8f;
        public Vector2 particleSizeB = new Vector2(0.1f, 0.3f);

        // --- Set pieces (discrete, read per theme rather than blended) ---
        /// <summary>A far-off object hung in the sky along sunDirection.</summary>
        public EnvironmentBackdrop backdrop = EnvironmentBackdrop.None;
        /// <summary>Lightning strikes per second at full weight; 0 for none.</summary>
        public float lightning = 0f;

        public EnvironmentPalette Clone()
        {
            return (EnvironmentPalette)MemberwiseClone();
        }

        /// <summary>
        /// Blends every continuous field of two palettes into <paramref name="dest"/>, which lets a level
        /// cross-fade between any two themes (see EnvironmentBlend). Writing into a caller-owned palette
        /// keeps this allocation-free so it can run every frame.
        ///
        /// Discrete fields - theme, display name, particle style, sphere effect preset - cannot be
        /// interpolated, so they snap to whichever side is nearer. Callers that need both particle
        /// styles alive at once (EnvironmentManager) read the two source palettes directly instead.
        /// </summary>
        public static void LerpInto(EnvironmentPalette a, EnvironmentPalette b, float t, EnvironmentPalette dest)
        {
            if (a == null || b == null || dest == null) return;
            t = Mathf.Clamp01(t);
            EnvironmentPalette near = t < 0.5f ? a : b;

            dest.theme = near.theme;
            dest.displayName = near.displayName;

            dest.skyTop = Color.Lerp(a.skyTop, b.skyTop, t);
            dest.skyHorizon = Color.Lerp(a.skyHorizon, b.skyHorizon, t);
            dest.skyBottom = Color.Lerp(a.skyBottom, b.skyBottom, t);
            dest.horizonPower = Mathf.Lerp(a.horizonPower, b.horizonPower, t);
            dest.detailColor = Color.Lerp(a.detailColor, b.detailColor, t);
            dest.detailScale = Mathf.Lerp(a.detailScale, b.detailScale, t);
            dest.detailStrength = Mathf.Lerp(a.detailStrength, b.detailStrength, t);
            dest.detailScroll = Vector2.Lerp(a.detailScroll, b.detailScroll, t);
            dest.detailStretch = Vector2.Lerp(a.detailStretch, b.detailStretch, t);
            dest.starDensity = Mathf.Lerp(a.starDensity, b.starDensity, t);
            dest.starColor = Color.Lerp(a.starColor, b.starColor, t);
            // Slerp keeps the sun swinging around the sky instead of sliding through the origin.
            dest.sunDirection = Vector3.Slerp(a.sunDirection.normalized, b.sunDirection.normalized, t);
            dest.sunColor = Color.Lerp(a.sunColor, b.sunColor, t);
            dest.sunSize = Mathf.Lerp(a.sunSize, b.sunSize, t);
            dest.sunHalo = Mathf.Lerp(a.sunHalo, b.sunHalo, t);

            dest.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
            dest.fogStart = Mathf.Lerp(a.fogStart, b.fogStart, t);
            dest.fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, t);
            dest.ambientLight = Color.Lerp(a.ambientLight, b.ambientLight, t);
            dest.ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t);
            dest.ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t);
            dest.sunLightColor = Color.Lerp(a.sunLightColor, b.sunLightColor, t);
            dest.sunLightIntensity = Mathf.Lerp(a.sunLightIntensity, b.sunLightIntensity, t);
            dest.shadowStrength = Mathf.Lerp(a.shadowStrength, b.shadowStrength, t);

            dest.accentColor = Color.Lerp(a.accentColor, b.accentColor, t);
            dest.rimLightIntensity = Mathf.Lerp(a.rimLightIntensity, b.rimLightIntensity, t);
            dest.rimDirection = Vector3.Slerp(a.rimDirection.normalized, b.rimDirection.normalized, t);

            dest.tubeTint = Color.Lerp(a.tubeTint, b.tubeTint, t);
            dest.tubeGridColor = Color.Lerp(a.tubeGridColor, b.tubeGridColor, t);
            dest.tubeBaseColor = Color.Lerp(a.tubeBaseColor, b.tubeBaseColor, t);
            dest.tubeEmission = Mathf.Lerp(a.tubeEmission, b.tubeEmission, t);

            dest.sphereColor = Color.Lerp(a.sphereColor, b.sphereColor, t);
            dest.effectPreset = near.effectPreset;

            dest.particleStyle = near.particleStyle;
            dest.particleColor = Color.Lerp(a.particleColor, b.particleColor, t);
            dest.particleRate = Mathf.Lerp(a.particleRate, b.particleRate, t);
            dest.particleSize = Vector2.Lerp(a.particleSize, b.particleSize, t);

            dest.particleStyleB = near.particleStyleB;
            dest.particleColorB = Color.Lerp(a.particleColorB, b.particleColorB, t);
            dest.particleRateB = Mathf.Lerp(a.particleRateB, b.particleRateB, t);
            dest.particleSizeB = Vector2.Lerp(a.particleSizeB, b.particleSizeB, t);
        }

        /// <summary>Overwrites <paramref name="dest"/> with every field of <paramref name="source"/>.</summary>
        public static void CopyInto(EnvironmentPalette source, EnvironmentPalette dest)
        {
            LerpInto(source, source, 0f, dest);
        }
    }

    public static partial class EnvironmentPalettes
    {
        /// <summary>
        /// Where the sun sits for the solar-system themes: ahead of the player, up and to the left.
        /// Flying sunward means everything ahead is backlit, so landmark bodies show a lit limb on
        /// their sun side. CelestialRoute places Earth on the opposite side of the tube from this so
        /// the two never crowd the same corner of the screen.
        /// </summary>
        public static readonly Vector3 SunDirection = new Vector3(-0.42f, 0.30f, 0.86f);

        private static EnvironmentPalette space, jungle, underwater, volcano, crystal, grid, classic;
        private static EnvironmentPalette solarSystem, asteroidBelt;

        /// <summary>
        /// Like Get, but EnvironmentTheme.None resolves to the classic neon tunnel's own palette
        /// instead of null, so None can act as a stop in an EnvironmentBlend. Only blending should
        /// use this; the rest of the game still treats a null palette as "leave the classic look alone".
        /// </summary>
        public static EnvironmentPalette GetOrClassic(EnvironmentTheme theme)
        {
            return Get(theme) ?? (classic ?? (classic = BuildClassic()));
        }

        /// <summary>Returns null for EnvironmentTheme.None.</summary>
        public static EnvironmentPalette Get(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Space:      return space      ?? (space      = BuildSpace());
                case EnvironmentTheme.Jungle:     return jungle     ?? (jungle     = BuildJungle());
                case EnvironmentTheme.Underwater: return underwater ?? (underwater = BuildUnderwater());
                case EnvironmentTheme.Volcano:    return volcano    ?? (volcano    = BuildVolcano());
                case EnvironmentTheme.Crystal:    return crystal    ?? (crystal    = BuildCrystal());
                case EnvironmentTheme.Grid:       return grid       ?? (grid       = BuildGrid());
                case EnvironmentTheme.SolarSystem:  return solarSystem  ?? (solarSystem  = BuildSolarSystem());
                case EnvironmentTheme.AsteroidBelt: return asteroidBelt ?? (asteroidBelt = BuildAsteroidBelt());
                default: return GetWorld(theme);   // the second set; null for None
            }
        }

        /// <summary>
        /// The classic neon tunnel expressed as a palette so it can be one end of a blend:
        /// an opaque grid wall in a near-black void, with the dim ambient and directional
        /// light GameSetup applies to untextured levels. The values here mirror GameSetup's
        /// tunnelBaseColor / gridLineColor defaults and its lighting pass - keep them in sync.
        /// </summary>
        private static EnvironmentPalette BuildClassic()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.None,
                displayName = "Neon Tunnel",

                skyTop = TubityXPalette.Deep(TubityXPalette.Violet, 0.10f),
                skyHorizon = TubityXPalette.Deep(TubityXPalette.Blue, 0.10f),
                skyBottom = TubityXPalette.Ink * 0.5f,
                horizonPower = 4f,
                detailColor = TubityXPalette.Deep(TubityXPalette.Blue, 0.30f),
                detailScale = 2f,
                detailStrength = 0f,
                detailScroll = new Vector2(0.004f, 0.001f),
                detailStretch = Vector2.one,
                starDensity = 0f,
                starColor = Color.white,
                sunDirection = new Vector3(0.3f, 0.5f, 0.8f),
                sunColor = new Color(0.05f, 0.05f, 0.1f),
                sunSize = 0.01f,
                sunHalo = 0f,

                fogColor = TubityXPalette.Deep(TubityXPalette.Blue, 0.10f),
                fogStart = 30f,
                fogEnd = 200f,
                ambientLight = new Color(0.05f, 0.05f, 0.08f),
                ambientSky = TubityXPalette.Deep(TubityXPalette.Blue, 0.28f),
                ambientGround = TubityXPalette.Ink,
                sunLightColor = new Color(0.1f, 0.1f, 0.2f),
                sunLightIntensity = 0.1f,
                // No trackside props in the classic tunnel, so a shadow pass would render an empty map.
                shadowStrength = 0f,

                accentColor = TubityXPalette.Magenta,
                rimLightIntensity = 0f,

                // tubeTint * 0.4 reproduces GameSetup's tunnelBaseColor; alpha 1 keeps the wall solid.
                tubeTint = new Color(0.1f, 0.05f, 0.2f, 1f),
                tubeGridColor = new Color(TubityXPalette.Blue.r, TubityXPalette.Blue.g, TubityXPalette.Blue.b, 1f),
                tubeBaseColor = TubityXPalette.DeepAlpha(TubityXPalette.Violet, 0.25f, 1f),
                tubeEmission = 0f,

                sphereColor = new Color(0f, 1f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                particleStyle = AmbientParticleStyle.None
            };
        }

        private static EnvironmentPalette BuildSpace()
        {
            // Identity hue: ion blue. Everything below is that hue run through the brand helpers,
            // so Space reads as the logo's cyan half without becoming a flat cyan level.
            Color hue = new Color(0.30f, 0.72f, 1.00f);
            Color energy = TubityXPalette.Neon(hue);
            Color accent = TubityXPalette.Accent(hue);
            Color ink = TubityXPalette.Deep(hue);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Space,
                displayName = "Deep Space",

                skyTop = TubityXPalette.Deep(TubityXPalette.Violet, 0.34f),
                skyHorizon = new Color(0.21f, 0.06f, 0.35f),
                skyBottom = ink * 0.6f,
                horizonPower = 4f,
                // The nebula is the brand's magenta-violet, which is what ties the sky to the title screen.
                detailColor = Color.Lerp(TubityXPalette.Magenta, TubityXPalette.Violet, 0.55f) * 0.62f,
                detailScale = 1.6f,
                detailStrength = 1.0f,
                detailScroll = new Vector2(0.004f, 0.001f),
                detailStretch = new Vector2(1f, 0.7f),
                starDensity = 1.0f,
                starColor = TubityXPalette.Glass * 1.5f,
                sunDirection = new Vector3(-0.45f, 0.35f, 0.8f),
                sunColor = TubityXPalette.Cyan * 1.3f,
                sunSize = 0.012f,
                sunHalo = 1.2f,

                fogColor = new Color(0.21f, 0.06f, 0.35f),
                fogStart = 90f,
                fogEnd = 460f,
                ambientLight = new Color(0.10f, 0.08f, 0.18f),
                ambientSky = Color.Lerp(ink, TubityXPalette.Violet, 0.18f),
                ambientGround = Color.Lerp(ink, accent, 0.10f),
                sunLightColor = Color.Lerp(TubityXPalette.Glass, TubityXPalette.Cyan, 0.45f),
                sunLightIntensity = 0.5f,
                // Hard sun, no atmosphere: asteroids and hulls carry the strongest terminator in the game.
                shadowStrength = 0.75f,

                accentColor = accent,
                rimLightIntensity = 0.30f,
                rimDirection = new Vector3(0.7f, -0.15f, -0.7f),

                tubeTint = new Color(0.35f, 0.7f, 1f, 0.3f),
                tubeGridColor = energy,
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.12f, 0.22f),

                sphereColor = new Color(0.2f, 0.9f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                particleStyle = AmbientParticleStyle.Stardust,
                particleColor = TubityXPalette.Glass,
                particleRate = 18f,
                particleSize = new Vector2(0.08f, 0.25f),

                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = accent,
                particleRateB = 7f,
                particleSizeB = new Vector2(0.05f, 0.16f)
            };
        }

        private static EnvironmentPalette BuildJungle()
        {
            // Identity hue: leaf green. The accent that comes back is an orchid magenta, which is
            // both the logo's second colour and exactly what a jungle would grow anyway.
            Color hue = new Color(0.45f, 0.95f, 0.25f);
            Color energy = TubityXPalette.Neon(hue);
            // Green sits opposite magenta on the wheel, so the default lean would mix to a dusty
            // pink. A shorter lean keeps the rim in the same hot band as every other theme's.
            Color accent = TubityXPalette.Accent(hue, 0.16f);
            Color ink = TubityXPalette.Deep(hue);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Jungle,
                displayName = "Jungle Canopy",

                skyTop = new Color(0.04f, 0.30f, 0.28f),
                skyHorizon = new Color(0.58f, 0.85f, 0.32f),
                skyBottom = ink,
                horizonPower = 2.5f,
                detailColor = new Color(0.10f, 0.44f, 0.20f),
                detailScale = 2.2f,
                detailStrength = 0.7f,
                detailScroll = new Vector2(0.012f, 0.002f),
                detailStretch = new Vector2(1f, 1f),
                starDensity = 0f,
                sunDirection = new Vector3(0.45f, 0.6f, 0.65f),
                sunColor = new Color(1.2f, 1.0f, 0.5f),
                sunSize = 0.05f,
                sunHalo = 1.6f,

                fogColor = new Color(0.58f, 0.85f, 0.32f),
                fogStart = 35f,
                fogEnd = 330f,
                ambientLight = new Color(0.12f, 0.22f, 0.10f),
                // Light down through the canopy, bounce up off the leaf litter.
                ambientSky = new Color(0.26f, 0.40f, 0.18f),
                ambientGround = Color.Lerp(ink, accent, 0.14f),
                sunLightColor = new Color(1f, 0.92f, 0.6f),
                sunLightIntensity = 0.75f,
                // The densest grounded geometry in the game: trunks and canopy blobs need the shadow.
                shadowStrength = 0.8f,

                accentColor = accent,
                rimLightIntensity = 0.38f,
                rimDirection = new Vector3(-0.7f, -0.25f, -0.65f),

                tubeTint = new Color(0.3f, 0.9f, 0.35f, 0.3f),
                tubeGridColor = energy,
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.14f, 0.22f),

                sphereColor = new Color(1f, 0.85f, 0.1f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                particleStyle = AmbientParticleStyle.Fireflies,
                particleColor = new Color(1f, 0.95f, 0.45f),
                particleRate = 14f,
                particleSize = new Vector2(0.12f, 0.3f),

                // Orchid pollen sifting down through the firefly layer.
                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = Color.Lerp(accent, Color.white, 0.35f),
                particleRateB = 10f,
                particleSizeB = new Vector2(0.05f, 0.14f)
            };
        }

        private static EnvironmentPalette BuildUnderwater()
        {
            // Identity hue: reef aqua - the theme nearest the brand cyan, so its magenta accent
            // does the most work here. Bioluminescence is the excuse; brand cohesion is the point.
            Color hue = new Color(0.15f, 0.95f, 0.92f);
            Color energy = TubityXPalette.Neon(hue);
            Color accent = TubityXPalette.Accent(hue, 0.18f);
            Color ink = TubityXPalette.Deep(hue);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Underwater,
                displayName = "Abyssal Reef",

                skyTop = new Color(0.0f, 0.48f, 0.64f),
                skyHorizon = new Color(0.0f, 0.21f, 0.41f),
                skyBottom = ink,
                horizonPower = 2f,
                detailColor = TubityXPalette.Cool(hue, 0.55f) * 0.85f,
                detailScale = 4f,
                detailStrength = 0.55f,
                detailScroll = new Vector2(0.03f, 0.02f),
                detailStretch = new Vector2(1f, 1f),
                starDensity = 0f,
                sunDirection = new Vector3(0.1f, 0.9f, 0.4f),
                sunColor = TubityXPalette.Cyan * 1.12f,
                sunSize = 0.12f,
                sunHalo = 2.2f,

                fogColor = new Color(0.0f, 0.21f, 0.41f),
                fogStart = 20f,
                fogEnd = 270f,
                ambientLight = new Color(0.05f, 0.16f, 0.24f),
                // Surface light above, a magenta glow off the reef below.
                ambientSky = new Color(0.10f, 0.30f, 0.38f),
                ambientGround = Color.Lerp(ink, accent, 0.16f),
                sunLightColor = Color.Lerp(TubityXPalette.Glass, TubityXPalette.Cyan, 0.6f),
                sunLightIntensity = 0.55f,
                // Light knifing down through the water: shadows read as shafts between the kelp.
                shadowStrength = 0.6f,

                accentColor = accent,
                rimLightIntensity = 0.42f,
                rimDirection = new Vector3(0.6f, 0.35f, -0.7f),

                tubeTint = new Color(0.1f, 0.85f, 1f, 0.3f),
                tubeGridColor = energy,
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.12f, 0.2f),

                sphereColor = new Color(1f, 0.45f, 0.75f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                particleStyle = AmbientParticleStyle.Bubbles,
                particleColor = new Color(0.7f, 0.95f, 1f),
                particleRate = 22f,
                particleSize = new Vector2(0.1f, 0.35f),

                // Glowing plankton hanging between the bubble trails.
                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = accent,
                particleRateB = 12f,
                particleSizeB = new Vector2(0.04f, 0.13f)
            };
        }

        private static EnvironmentPalette BuildVolcano()
        {
            // Identity hue: magma orange. Accent() turns that into a hot pink that sits a hue-step
            // off the lava instead of blending into it, which is what keeps the silhouettes readable.
            Color hue = new Color(1f, 0.45f, 0.08f);
            Color energy = TubityXPalette.Neon(hue);
            Color accent = TubityXPalette.Accent(hue);
            Color ink = TubityXPalette.Deep(hue);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Volcano,
                displayName = "Magma Core",

                // The only sky in the game whose ceiling is pure ink - the heat is all below the horizon.
                skyTop = TubityXPalette.Ink,
                skyHorizon = new Color(0.70f, 0.14f, 0.02f),
                skyBottom = new Color(0.28f, 0.04f, 0.02f),
                horizonPower = 3.5f,
                detailColor = new Color(0.55f, 0.18f, 0.10f),
                detailScale = 1.8f,
                detailStrength = 0.8f,
                detailScroll = new Vector2(0.02f, 0.006f),
                detailStretch = new Vector2(1f, 0.8f),
                starDensity = 0f,
                sunDirection = new Vector3(0.25f, 0.18f, 0.95f),
                sunColor = new Color(1.4f, 0.4f, 0.05f),
                sunSize = 0.04f,
                sunHalo = 1.8f,

                fogColor = new Color(0.70f, 0.14f, 0.02f),
                fogStart = 30f,
                fogEnd = 300f,
                ambientLight = new Color(0.26f, 0.08f, 0.04f),
                // Ash overhead, glowing rock underfoot: the inverse of every other theme.
                ambientSky = Color.Lerp(ink, accent, 0.12f),
                ambientGround = new Color(0.34f, 0.10f, 0.04f),
                sunLightColor = new Color(1f, 0.42f, 0.15f),
                sunLightIntensity = 0.6f,
                // Low raking sun across obsidian spires - the cheapest shadow in the game to read.
                shadowStrength = 0.7f,

                accentColor = accent,
                rimLightIntensity = 0.45f,
                rimDirection = new Vector3(-0.75f, 0.1f, -0.65f),

                tubeTint = new Color(1f, 0.35f, 0.05f, 0.3f),
                tubeGridColor = energy,
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.14f, 0.22f),

                sphereColor = new Color(0.35f, 0.95f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Gauntlet,

                particleStyle = AmbientParticleStyle.Embers,
                particleColor = new Color(1f, 0.5f, 0.1f),
                particleRate = 26f,
                particleSize = new Vector2(0.06f, 0.2f),

                // Ash falling back through the embers - dark, so it silhouettes against the sky.
                particleStyleB = AmbientParticleStyle.Ash,
                particleColorB = new Color(0.16f, 0.12f, 0.14f),
                particleRateB = 14f,
                particleSizeB = new Vector2(0.06f, 0.2f)
            };
        }

        private static EnvironmentPalette BuildCrystal()
        {
            // Identity hue: glacier blue. The aurora curtains run the full brand gradient - a cyan
            // detail band over a magenta rim light - which makes this the most logo-like sky.
            Color hue = new Color(0.40f, 0.82f, 1f);
            Color energy = TubityXPalette.Neon(hue, 0.55f);
            Color accent = TubityXPalette.Accent(hue, 0.22f);
            Color ink = TubityXPalette.Deep(hue);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Crystal,
                displayName = "Aurora Cavern",

                skyTop = TubityXPalette.Deep(TubityXPalette.Violet, 0.22f),
                skyHorizon = new Color(0.10f, 0.36f, 0.58f),
                skyBottom = ink,
                horizonPower = 3f,
                detailColor = TubityXPalette.Cool(new Color(0.2f, 1f, 0.65f), 0.45f),
                detailScale = 1.4f,
                detailStrength = 1.1f,
                detailScroll = new Vector2(0.015f, 0.004f),
                detailStretch = new Vector2(1f, 0.35f),   // wide horizontal bands = aurora curtains
                starDensity = 0.6f,
                starColor = TubityXPalette.Glass * 1.35f,
                sunDirection = new Vector3(0.5f, 0.55f, 0.65f),
                sunColor = new Color(0.9f, 1.0f, 1.2f),
                sunSize = 0.02f,
                sunHalo = 0.8f,

                fogColor = new Color(0.10f, 0.36f, 0.58f),
                fogStart = 35f,
                fogEnd = 350f,
                ambientLight = new Color(0.10f, 0.15f, 0.24f),
                // Aurora light from above, a violet bounce off the ice below.
                ambientSky = new Color(0.14f, 0.26f, 0.34f),
                ambientGround = Color.Lerp(ink, TubityXPalette.Violet, 0.18f),
                sunLightColor = Color.Lerp(TubityXPalette.Glass, TubityXPalette.Cyan, 0.35f),
                sunLightIntensity = 0.55f,
                // Faceted gems need a terminator or they read as flat coloured glass.
                shadowStrength = 0.7f,

                accentColor = accent,
                rimLightIntensity = 0.5f,
                rimDirection = new Vector3(-0.6f, 0.3f, -0.75f),

                tubeTint = new Color(0.55f, 0.75f, 1f, 0.3f),
                tubeGridColor = energy,
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.12f, 0.2f),

                sphereColor = new Color(1f, 0.3f, 0.9f),
                effectPreset = EnergySphereEffects.EffectPreset.City,

                particleStyle = AmbientParticleStyle.Snow,
                particleColor = new Color(0.9f, 0.97f, 1f),
                particleRate = 24f,
                particleSize = new Vector2(0.08f, 0.22f),

                // Aurora sparks hanging in the air under the snow.
                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = accent,
                particleRateB = 11f,
                particleSizeB = new Vector2(0.04f, 0.14f)
            };
        }

        /// <summary>
        /// Real vacuum, as opposed to Deep Space's stylised nebula: a black sky carrying nothing but
        /// a dense star field and a faint Milky Way band, and no trackside planets - a solar system
        /// level's planets are named landmarks placed at fixed distances by CelestialRoute, not props
        /// scattered per segment.
        ///
        /// The fog here is doing an unusual job. The tunnel only ever extends ~120 units ahead, so a
        /// fogStart past that leaves the tube completely unfogged, and the long fogEnd is purely
        /// atmospheric perspective on the landmarks: a planet 1500 units out is a smudge, the same
        /// planet at 300 is crisp. That gradient is what makes the gaps between planets read as
        /// distance rather than as empty flying.
        ///
        /// Every field a solar route animates - sun size, halo, sun colour, light intensity, fog -
        /// is overridden per stop by CelestialRoute. What is set here is the resting state and,
        /// more importantly, the particle and scenery character the whole route inherits.
        /// </summary>
        private static EnvironmentPalette BuildSolarSystem()
        {
            Color hue = new Color(0.45f, 0.70f, 1f);          // cold sunlight off airless rock
            Color accent = TubityXPalette.Accent(hue, 0.20f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.SolarSystem,
                displayName = "Solar System",

                // No air, so no horizon: the sky is the same ink in every direction and the stars
                // carry it. Leaving a hint of violet up top keeps it from reading as a dead grey.
                skyTop = TubityXPalette.Deep(TubityXPalette.Violet, 0.12f),
                skyHorizon = TubityXPalette.Ink * 0.55f,
                skyBottom = TubityXPalette.Ink * 0.35f,
                horizonPower = 1.5f,
                // The Milky Way: the detail layer squashed into a wide horizontal band, dim and cool.
                detailColor = new Color(0.34f, 0.40f, 0.62f),
                detailScale = 1.1f,
                detailStrength = 0.40f,
                detailScroll = new Vector2(0.0015f, 0.0004f),
                detailStretch = new Vector2(1f, 0.16f),
                starDensity = 1f,
                starColor = TubityXPalette.Glass * 1.6f,
                sunDirection = SunDirection,
                sunColor = new Color(2.2f, 2.1f, 1.95f),
                sunSize = 0.0030f,
                sunHalo = 0.65f,

                fogColor = TubityXPalette.Ink * 0.55f,
                fogStart = 150f,
                fogEnd = 1800f,
                ambientLight = new Color(0.035f, 0.040f, 0.060f),
                ambientSky = new Color(0.05f, 0.055f, 0.085f),
                ambientGround = Color.Lerp(TubityXPalette.Ink, accent, 0.10f),
                sunLightColor = new Color(1f, 0.97f, 0.92f),
                sunLightIntensity = 0.45f,
                // Airless bodies have the hardest terminator there is; this is the point of the theme.
                shadowStrength = 0.9f,

                accentColor = accent,
                rimLightIntensity = 0.22f,
                rimDirection = new Vector3(0.75f, -0.3f, -0.6f),

                tubeTint = new Color(0.30f, 0.66f, 1f, 0.24f),
                tubeGridColor = TubityXPalette.Neon(hue, 0.65f),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.10f, 0.16f),

                sphereColor = new Color(0.2f, 0.9f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                // Sparse and slow: interplanetary dust, not a weather effect.
                particleStyle = AmbientParticleStyle.Stardust,
                particleColor = TubityXPalette.Glass,
                particleRate = 9f,
                particleSize = new Vector2(0.05f, 0.16f),

                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = accent,
                particleRateB = 4f,
                particleSizeB = new Vector2(0.04f, 0.11f)
            };
        }

        /// <summary>
        /// The stretch between Mars and Jupiter. Same vacuum as SolarSystem, but the air is full of
        /// grit and the rock bounces enough sunlight back to lift the ambient. It is a separate theme
        /// rather than a palette stop because the belt is defined by its scenery density, and the
        /// blend's per-prop dissolve is what thins the rocks in and out at its edges.
        /// </summary>
        private static EnvironmentPalette BuildAsteroidBelt()
        {
            EnvironmentPalette p = BuildSolarSystem().Clone();
            p.theme = EnvironmentTheme.AsteroidBelt;
            p.displayName = "Asteroid Belt";

            // 2.7 AU: the sun is meaningfully bigger here than at Uranus and actually lights the rock.
            p.sunSize = 0.014f;
            p.sunHalo = 1.15f;
            p.sunLightIntensity = 0.85f;

            // Dust catching the light, and enough bounce off nearby rock to fill the shadow side.
            p.detailColor = new Color(0.40f, 0.38f, 0.46f);
            p.detailStrength = 0.50f;
            p.ambientLight = new Color(0.075f, 0.070f, 0.075f);
            p.ambientSky = new Color(0.095f, 0.092f, 0.105f);
            p.ambientGround = new Color(0.075f, 0.062f, 0.058f);
            p.fogEnd = 1400f;

            p.tubeGridColor = TubityXPalette.Neon(new Color(0.75f, 0.72f, 0.95f), 0.55f);

            // Grit rather than dust: denser, heavier, and it drifts past instead of hanging still.
            p.particleStyle = AmbientParticleStyle.Motes;
            p.particleColor = new Color(0.72f, 0.70f, 0.66f);
            p.particleRate = 26f;
            p.particleSize = new Vector2(0.05f, 0.20f);

            p.particleStyleB = AmbientParticleStyle.Stardust;
            p.particleColorB = TubityXPalette.Glass;
            p.particleRateB = 8f;
            p.particleSizeB = new Vector2(0.04f, 0.12f);
            return p;
        }

        /// <summary>
        /// A stark void wrapped in a bright wireframe grid: near-black sky and fog with no
        /// stars or nebula detail, and no trackside props (see EnvironmentScenery's dispatch
        /// switch), so the tube's own glowing grid lines are the entire scene.
        ///
        /// This is the theme closest to the title screen, so it uses the brand colours raw:
        /// the logo's cyan on the grid, its magenta in the air, its ink everywhere else.
        /// Having no props, it is also the one themed level that never pays for a shadow pass.
        /// </summary>
        private static EnvironmentPalette BuildGrid()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Grid,
                displayName = "Cyber Grid",

                skyTop = TubityXPalette.Ink * 0.6f,
                skyHorizon = TubityXPalette.Deep(TubityXPalette.Cyan, 0.16f),
                skyBottom = TubityXPalette.Ink * 0.25f,
                horizonPower = 5f,
                detailColor = TubityXPalette.Deep(TubityXPalette.Cyan, 0.42f),
                detailScale = 3f,
                detailStrength = 0.15f,
                detailScroll = new Vector2(0.006f, 0.002f),
                detailStretch = new Vector2(1f, 1f),
                starDensity = 0f,
                sunDirection = new Vector3(0.2f, 0.6f, 0.75f),
                sunColor = TubityXPalette.Cyan * 1.1f,
                sunSize = 0.02f,
                sunHalo = 0.6f,

                fogColor = TubityXPalette.Deep(TubityXPalette.Cyan, 0.16f),
                fogStart = 25f,
                fogEnd = 220f,
                ambientLight = new Color(0.05f, 0.09f, 0.14f),
                ambientSky = TubityXPalette.Deep(TubityXPalette.Cyan, 0.34f),
                ambientGround = TubityXPalette.Deep(TubityXPalette.Magenta, 0.20f),
                sunLightColor = Color.Lerp(TubityXPalette.Glass, TubityXPalette.Cyan, 0.5f),
                sunLightIntensity = 0.45f,
                shadowStrength = 0f,

                accentColor = TubityXPalette.Magenta,
                rimLightIntensity = 0f,

                tubeTint = new Color(0.1f, 0.85f, 1f, 0.3f),
                tubeGridColor = TubityXPalette.Cyan,
                tubeBaseColor = TubityXPalette.DeepAlpha(TubityXPalette.Violet, 0.10f, 0.28f),

                sphereColor = new Color(0.2f, 1f, 0.9f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                // Nothing trackside to look at, so the void itself gets the atmosphere: a slow
                // cyan dust with a magenta counter-layer, the logo gradient hanging in the air.
                particleStyle = AmbientParticleStyle.Motes,
                particleColor = TubityXPalette.Cyan,
                particleRate = 13f,
                particleSize = new Vector2(0.05f, 0.18f),

                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = TubityXPalette.Magenta,
                particleRateB = 8f,
                particleSizeB = new Vector2(0.04f, 0.14f)
            };
        }
    }
}
