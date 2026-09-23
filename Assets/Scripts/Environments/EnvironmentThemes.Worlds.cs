using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The second set of worlds - ten themes built for the sandbox lab first. Same rules as the
    /// originals in EnvironmentTheme.cs: fog colour matches the horizon so far segments arrive
    /// unseen, every theme carries a magenta-family accent, and each palette leans on one
    /// identity hue. The shapes that make these worlds read (walls, gates, gears, the sun, the
    /// black hole) live in EnvironmentScenery.Worlds.cs and EnvironmentBackdrops.cs.
    /// </summary>
    public static partial class EnvironmentPalettes
    {
        private static EnvironmentPalette prismHall, cavern, lavaTube, solarFlare, blackHole;
        private static EnvironmentPalette thunderstorm, sunsetCanyon, clockwork, sakuraGates, candyClouds;

        private static EnvironmentPalette GetWorld(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.PrismHall:    return prismHall    ?? (prismHall    = BuildPrismHall());
                case EnvironmentTheme.Cavern:       return cavern       ?? (cavern       = BuildCavern());
                case EnvironmentTheme.LavaTube:     return lavaTube     ?? (lavaTube     = BuildLavaTube());
                case EnvironmentTheme.SolarFlare:   return solarFlare   ?? (solarFlare   = BuildSolarFlare());
                case EnvironmentTheme.BlackHole:    return blackHole    ?? (blackHole    = BuildBlackHole());
                case EnvironmentTheme.Thunderstorm: return thunderstorm ?? (thunderstorm = BuildThunderstorm());
                case EnvironmentTheme.SunsetCanyon: return sunsetCanyon ?? (sunsetCanyon = BuildSunsetCanyon());
                case EnvironmentTheme.Clockwork:    return clockwork    ?? (clockwork    = BuildClockwork());
                case EnvironmentTheme.SakuraGates:  return sakuraGates  ?? (sakuraGates  = BuildSakuraGates());
                case EnvironmentTheme.CandyClouds:  return candyClouds  ?? (candyClouds  = BuildCandyClouds());
                default: return null;
            }
        }

        /// <summary>
        /// Falling through a hall of mirrors. A near-black void so the mirror panels' own
        /// iridescence is the light in the scene; stars stand in for glints off distant glass.
        /// </summary>
        private static EnvironmentPalette BuildPrismHall()
        {
            Color hue = new Color(0.75f, 0.70f, 1f);
            Color accent = TubityXPalette.Accent(hue, 0.35f);
            Color ink = TubityXPalette.Deep(TubityXPalette.Violet, 0.10f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.PrismHall,
                displayName = "Hall of Mirrors",

                skyTop = ink,
                skyHorizon = TubityXPalette.Deep(hue, 0.20f),
                skyBottom = ink * 0.6f,
                horizonPower = 4f,
                detailColor = new Color(0.55f, 0.50f, 0.80f),
                detailScale = 2.6f,
                detailStrength = 0.35f,
                detailScroll = new Vector2(0.01f, 0.002f),
                detailStretch = new Vector2(1f, 0.25f),
                starDensity = 0.45f,
                starColor = new Color(1.3f, 1.3f, 1.5f),
                sunDirection = new Vector3(0.1f, 0.4f, 0.9f),
                sunColor = new Color(0.9f, 0.9f, 1.2f),
                sunSize = 0.01f,
                sunHalo = 0.6f,

                fogColor = TubityXPalette.Deep(hue, 0.20f),
                fogStart = 30f,
                fogEnd = 230f,
                ambientLight = new Color(0.14f, 0.13f, 0.20f),
                ambientSky = new Color(0.20f, 0.18f, 0.30f),
                ambientGround = Color.Lerp(ink, accent, 0.25f),
                sunLightColor = new Color(0.95f, 0.95f, 1f),
                sunLightIntensity = 0.6f,
                shadowStrength = 0f,

                accentColor = accent,
                rimLightIntensity = 0.45f,
                rimDirection = new Vector3(-0.6f, 0.2f, -0.75f),

                tubeTint = new Color(0.85f, 0.85f, 1f, 0.22f),
                tubeGridColor = TubityXPalette.Neon(new Color(0.8f, 0.9f, 1f), 0.3f),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.08f, 0.14f),

                sphereColor = new Color(1f, 1f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                particleStyle = AmbientParticleStyle.Stardust,
                particleColor = new Color(1f, 1f, 1.1f),
                particleRate = 18f,
                particleSize = new Vector2(0.04f, 0.14f),

                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = accent,
                particleRateB = 8f,
                particleSizeB = new Vector2(0.04f, 0.12f)
            };
        }

        /// <summary>
        /// A lantern-lit mine cave: warm light from above, a violet bounce off wet rock below,
        /// and teal glow from the crystals and glow-worms. The fog is close and dark - the cave
        /// walls are the horizon.
        /// </summary>
        private static EnvironmentPalette BuildCavern()
        {
            Color hue = new Color(0.95f, 0.62f, 0.28f);   // lantern amber
            Color accent = TubityXPalette.Accent(new Color(0.3f, 0.9f, 0.85f), 0.25f);
            Color gloom = new Color(0.07f, 0.045f, 0.06f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Cavern,
                displayName = "Lantern Cave",

                skyTop = gloom * 0.6f,
                skyHorizon = gloom,
                skyBottom = gloom * 0.5f,
                horizonPower = 2f,
                detailColor = new Color(0.12f, 0.08f, 0.10f),
                detailScale = 2f,
                detailStrength = 0.2f,
                starDensity = 0f,
                sunDirection = new Vector3(0.2f, 0.9f, 0.4f),
                sunColor = Color.black,
                sunSize = 0.001f,
                sunHalo = 0f,

                fogColor = gloom,
                fogStart = 18f,
                fogEnd = 140f,
                ambientLight = new Color(0.16f, 0.10f, 0.08f),
                ambientSky = new Color(0.22f, 0.14f, 0.08f),
                ambientGround = new Color(0.10f, 0.06f, 0.14f),
                sunLightColor = hue,
                sunLightIntensity = 0.75f,
                shadowStrength = 0.6f,

                accentColor = accent,
                rimLightIntensity = 0.4f,
                rimDirection = new Vector3(-0.5f, -0.5f, -0.7f),

                tubeTint = new Color(1f, 0.7f, 0.35f, 0.18f),
                tubeGridColor = TubityXPalette.Neon(hue, 0.6f),
                tubeBaseColor = new Color(0.05f, 0.03f, 0.02f, 0.14f),

                sphereColor = new Color(0.35f, 1f, 0.9f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                // Dust in the lantern light, and glow-worms hanging in the dark.
                particleStyle = AmbientParticleStyle.Motes,
                particleColor = new Color(1f, 0.78f, 0.45f),
                particleRate = 14f,
                particleSize = new Vector2(0.03f, 0.10f),

                particleStyleB = AmbientParticleStyle.Fireflies,
                particleColorB = new Color(0.3f, 1f, 0.9f),
                particleRateB = 9f,
                particleSizeB = new Vector2(0.06f, 0.16f)
            };
        }

        /// <summary>
        /// Inside a lava tube: basalt walls cracked with glowing seams and a molten river along the
        /// floor. The light comes from below, so the sun points up at the scene.
        /// </summary>
        private static EnvironmentPalette BuildLavaTube()
        {
            Color hue = new Color(1f, 0.38f, 0.05f);
            Color accent = TubityXPalette.Accent(hue);
            Color smoke = new Color(0.20f, 0.04f, 0.02f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.LavaTube,
                displayName = "Lava Tube",

                skyTop = smoke * 0.5f,
                skyHorizon = smoke,
                skyBottom = new Color(0.45f, 0.10f, 0.02f),
                horizonPower = 2.5f,
                detailColor = new Color(0.30f, 0.08f, 0.03f),
                detailScale = 2f,
                detailStrength = 0.5f,
                detailScroll = new Vector2(0.03f, 0.01f),
                starDensity = 0f,
                sunDirection = new Vector3(0f, -0.85f, 0.5f),
                sunColor = Color.black,
                sunSize = 0.001f,
                sunHalo = 0f,

                fogColor = smoke,
                fogStart = 20f,
                fogEnd = 150f,
                ambientLight = new Color(0.22f, 0.07f, 0.03f),
                ambientSky = new Color(0.10f, 0.03f, 0.03f),
                ambientGround = new Color(0.55f, 0.16f, 0.04f),
                sunLightColor = new Color(1f, 0.45f, 0.12f),
                sunLightIntensity = 0.9f,
                shadowStrength = 0.5f,

                accentColor = accent,
                rimLightIntensity = 0.35f,
                rimDirection = new Vector3(0.6f, 0.3f, -0.7f),

                tubeTint = new Color(1f, 0.4f, 0.1f, 0.2f),
                tubeGridColor = TubityXPalette.Neon(hue),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.10f, 0.16f),

                sphereColor = new Color(0.35f, 0.95f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Gauntlet,

                particleStyle = AmbientParticleStyle.Embers,
                particleColor = new Color(1f, 0.55f, 0.15f),
                particleRate = 30f,
                particleSize = new Vector2(0.05f, 0.18f),

                particleStyleB = AmbientParticleStyle.Ash,
                particleColorB = new Color(0.14f, 0.10f, 0.10f),
                particleRateB = 10f,
                particleSizeB = new Vector2(0.05f, 0.16f)
            };
        }

        /// <summary>
        /// Skimming a star's corona. The star itself is a backdrop disc (EnvironmentBackdrops) far
        /// ahead; the sky only carries its glow. Near prominence loops and plasma streaking past are
        /// what give the distance its scale. The fog runs long so the loops fade with range.
        /// </summary>
        private static EnvironmentPalette BuildSolarFlare()
        {
            Color hue = new Color(1f, 0.55f, 0.12f);
            Color accent = TubityXPalette.Accent(hue, 0.3f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.SolarFlare,
                displayName = "Corona",

                skyTop = new Color(0.02f, 0.01f, 0.02f),
                skyHorizon = new Color(0.20f, 0.06f, 0.02f),
                skyBottom = new Color(0.03f, 0.01f, 0.01f),
                horizonPower = 2f,
                detailColor = new Color(0.35f, 0.12f, 0.04f),
                detailScale = 1.4f,
                detailStrength = 0.45f,
                detailScroll = new Vector2(0.004f, 0.001f),
                detailStretch = new Vector2(1f, 0.5f),
                starDensity = 0.25f,
                starColor = new Color(1.2f, 1f, 0.8f),
                // The backdrop sun sits along this; the sky's own disc stays hidden behind it and
                // only its broad halo is used, as the glow the corona throws across the sky.
                sunDirection = new Vector3(0.30f, 0.16f, 0.94f),
                sunColor = new Color(1.6f, 0.6f, 0.15f),
                sunSize = 0.002f,
                sunHalo = 3.2f,

                fogColor = new Color(0.20f, 0.06f, 0.02f),
                fogStart = 120f,
                fogEnd = 1400f,
                ambientLight = new Color(0.14f, 0.07f, 0.04f),
                ambientSky = new Color(0.20f, 0.10f, 0.04f),
                ambientGround = new Color(0.06f, 0.02f, 0.02f),
                sunLightColor = new Color(1f, 0.85f, 0.6f),
                sunLightIntensity = 1.2f,
                shadowStrength = 0.8f,

                accentColor = accent,
                rimLightIntensity = 0.3f,
                rimDirection = new Vector3(-0.7f, -0.2f, -0.6f),

                tubeTint = new Color(1f, 0.75f, 0.3f, 0.2f),
                tubeGridColor = TubityXPalette.Neon(hue, 0.7f),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.08f, 0.12f),

                sphereColor = new Color(0.4f, 0.8f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                particleStyle = AmbientParticleStyle.Streaks,
                particleColor = new Color(1f, 0.7f, 0.3f),
                particleRate = 22f,
                particleSize = new Vector2(0.05f, 0.14f),

                particleStyleB = AmbientParticleStyle.Embers,
                particleColorB = new Color(1f, 0.85f, 0.5f),
                particleRateB = 12f,
                particleSizeB = new Vector2(0.04f, 0.12f),

                backdrop = EnvironmentBackdrop.Sun
            };
        }

        /// <summary>
        /// Close enough to a black hole to watch its accretion disc flow, far enough to see all of
        /// it. The disc (EnvironmentBackdrops) is the only real light source, so the sun is set
        /// along it and the scene is lit warm from ahead.
        /// </summary>
        private static EnvironmentPalette BuildBlackHole()
        {
            Color hue = new Color(1f, 0.72f, 0.40f);
            Color accent = TubityXPalette.Accent(new Color(0.5f, 0.6f, 1f), 0.3f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.BlackHole,
                displayName = "Event Horizon",

                skyTop = TubityXPalette.Ink * 0.4f,
                skyHorizon = TubityXPalette.Ink * 0.6f,
                skyBottom = TubityXPalette.Ink * 0.3f,
                horizonPower = 1.5f,
                detailColor = new Color(0.22f, 0.20f, 0.30f),
                detailScale = 1.1f,
                detailStrength = 0.30f,
                detailScroll = new Vector2(0.001f, 0.0003f),
                detailStretch = new Vector2(1f, 0.18f),
                starDensity = 1f,
                starColor = TubityXPalette.Glass * 1.5f,
                sunDirection = new Vector3(0.20f, 0.08f, 0.98f),
                sunColor = Color.black,
                sunSize = 0.001f,
                sunHalo = 0f,

                fogColor = TubityXPalette.Ink * 0.6f,
                fogStart = 140f,
                fogEnd = 1600f,
                ambientLight = new Color(0.05f, 0.045f, 0.06f),
                ambientSky = new Color(0.06f, 0.06f, 0.09f),
                ambientGround = new Color(0.03f, 0.03f, 0.05f),
                sunLightColor = hue,
                sunLightIntensity = 0.7f,
                shadowStrength = 0.7f,

                accentColor = accent,
                rimLightIntensity = 0.25f,
                rimDirection = new Vector3(0.6f, -0.3f, -0.7f),

                tubeTint = new Color(0.7f, 0.8f, 1f, 0.18f),
                tubeGridColor = TubityXPalette.Neon(new Color(0.8f, 0.85f, 1f), 0.4f),
                tubeBaseColor = TubityXPalette.DeepAlpha(new Color(0.5f, 0.6f, 1f), 0.06f, 0.1f),

                sphereColor = new Color(0.3f, 0.85f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                particleStyle = AmbientParticleStyle.Stardust,
                particleColor = TubityXPalette.Glass,
                particleRate = 10f,
                particleSize = new Vector2(0.04f, 0.12f),

                // Gas falling inward, streaking past toward the disc.
                particleStyleB = AmbientParticleStyle.Streaks,
                particleColorB = new Color(1f, 0.75f, 0.45f),
                particleRateB = 6f,
                particleSizeB = new Vector2(0.03f, 0.08f),

                backdrop = EnvironmentBackdrop.BlackHole
            };
        }

        /// <summary>
        /// Inside a storm cell: slate cloud all round, rain slanting past, and lightning
        /// (EnvironmentStorm) that flashes the whole sky.
        /// </summary>
        private static EnvironmentPalette BuildThunderstorm()
        {
            Color hue = new Color(0.55f, 0.65f, 0.85f);
            Color accent = TubityXPalette.Accent(hue, 0.3f);
            Color slate = new Color(0.12f, 0.14f, 0.19f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Thunderstorm,
                displayName = "Thunderhead",

                skyTop = slate * 0.5f,
                skyHorizon = slate,
                skyBottom = slate * 0.35f,
                horizonPower = 1.8f,
                detailColor = new Color(0.20f, 0.22f, 0.28f),
                detailScale = 1.6f,
                detailStrength = 1.2f,
                detailScroll = new Vector2(0.03f, 0.01f),
                detailStretch = new Vector2(1f, 0.6f),
                starDensity = 0f,
                sunDirection = new Vector3(0.3f, 0.8f, 0.5f),
                sunColor = Color.black,
                sunSize = 0.001f,
                sunHalo = 0f,

                fogColor = slate,
                fogStart = 25f,
                fogEnd = 180f,
                ambientLight = new Color(0.12f, 0.13f, 0.16f),
                ambientSky = new Color(0.16f, 0.18f, 0.22f),
                ambientGround = new Color(0.06f, 0.07f, 0.09f),
                sunLightColor = new Color(0.7f, 0.78f, 0.9f),
                sunLightIntensity = 0.35f,
                shadowStrength = 0f,

                accentColor = accent,
                rimLightIntensity = 0.25f,
                rimDirection = new Vector3(-0.6f, 0.3f, -0.7f),

                tubeTint = new Color(0.7f, 0.85f, 1f, 0.2f),
                tubeGridColor = TubityXPalette.Neon(hue, 0.5f),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.08f, 0.14f),

                sphereColor = new Color(1f, 0.9f, 0.3f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                particleStyle = AmbientParticleStyle.Rain,
                particleColor = new Color(0.75f, 0.85f, 1f, 0.7f),
                particleRate = 90f,
                particleSize = new Vector2(0.03f, 0.06f),

                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = new Color(0.5f, 0.55f, 0.65f),
                particleRateB = 10f,
                particleSizeB = new Vector2(0.2f, 0.5f),

                lightning = 0.35f
            };
        }

        /// <summary>
        /// A desert canyon at golden hour: sandstone walls either side, the sun low at the far end
        /// of the canyon, long raking shadows.
        /// </summary>
        private static EnvironmentPalette BuildSunsetCanyon()
        {
            Color hue = new Color(1f, 0.55f, 0.25f);
            Color accent = TubityXPalette.Accent(hue, 0.3f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.SunsetCanyon,
                displayName = "Sunset Canyon",

                skyTop = new Color(0.16f, 0.12f, 0.34f),
                skyHorizon = new Color(1f, 0.50f, 0.26f),
                skyBottom = new Color(0.40f, 0.16f, 0.14f),
                horizonPower = 3.2f,
                detailColor = new Color(0.60f, 0.30f, 0.30f),
                detailScale = 1.5f,
                detailStrength = 0.35f,
                detailScroll = new Vector2(0.004f, 0.001f),
                detailStretch = new Vector2(1f, 0.3f),
                starDensity = 0.05f,
                sunDirection = new Vector3(-0.1f, 0.07f, 1f),
                sunColor = new Color(2.2f, 1.2f, 0.5f),
                sunSize = 0.05f,
                sunHalo = 2.2f,

                fogColor = new Color(0.85f, 0.42f, 0.25f),
                fogStart = 60f,
                fogEnd = 420f,
                ambientLight = new Color(0.30f, 0.18f, 0.16f),
                ambientSky = new Color(0.30f, 0.24f, 0.40f),
                ambientGround = new Color(0.34f, 0.16f, 0.08f),
                sunLightColor = new Color(1f, 0.62f, 0.35f),
                sunLightIntensity = 1.0f,
                shadowStrength = 0.8f,

                accentColor = accent,
                rimLightIntensity = 0.35f,
                rimDirection = new Vector3(0.6f, 0.4f, -0.7f),

                tubeTint = new Color(1f, 0.75f, 0.45f, 0.2f),
                tubeGridColor = TubityXPalette.Neon(hue, 0.6f),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.08f, 0.12f),

                sphereColor = new Color(0.3f, 0.8f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                particleStyle = AmbientParticleStyle.Motes,
                particleColor = new Color(1f, 0.8f, 0.55f),
                particleRate = 12f,
                particleSize = new Vector2(0.03f, 0.10f),

                particleStyleB = AmbientParticleStyle.Stardust,
                particleColorB = accent,
                particleRateB = 4f,
                particleSizeB = new Vector2(0.03f, 0.08f)
            };
        }

        /// <summary>The works of a giant clock: brass and copper under warm furnace light, smoke and sparks.</summary>
        private static EnvironmentPalette BuildClockwork()
        {
            Color hue = new Color(0.95f, 0.66f, 0.30f);
            Color accent = TubityXPalette.Accent(new Color(0.3f, 0.9f, 0.8f), 0.3f);
            Color smoke = new Color(0.16f, 0.10f, 0.06f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Clockwork,
                displayName = "Clockwork",

                skyTop = smoke * 0.5f,
                skyHorizon = smoke,
                skyBottom = smoke * 0.4f,
                horizonPower = 2.2f,
                detailColor = new Color(0.24f, 0.16f, 0.08f),
                detailScale = 2f,
                detailStrength = 0.5f,
                detailScroll = new Vector2(0.01f, 0.004f),
                starDensity = 0f,
                sunDirection = new Vector3(0.3f, 0.7f, 0.6f),
                sunColor = Color.black,
                sunSize = 0.001f,
                sunHalo = 0f,

                fogColor = smoke,
                fogStart = 25f,
                fogEnd = 200f,
                ambientLight = new Color(0.20f, 0.14f, 0.08f),
                ambientSky = new Color(0.26f, 0.18f, 0.10f),
                ambientGround = new Color(0.12f, 0.08f, 0.06f),
                sunLightColor = hue,
                sunLightIntensity = 0.9f,
                shadowStrength = 0.6f,

                accentColor = accent,
                rimLightIntensity = 0.4f,
                rimDirection = new Vector3(-0.7f, 0.1f, -0.7f),

                tubeTint = new Color(1f, 0.75f, 0.4f, 0.18f),
                tubeGridColor = TubityXPalette.Neon(hue, 0.6f),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.08f, 0.14f),

                sphereColor = new Color(0.3f, 1f, 0.9f),
                effectPreset = EnergySphereEffects.EffectPreset.Gauntlet,

                particleStyle = AmbientParticleStyle.Sparks,
                particleColor = new Color(1f, 0.65f, 0.25f),
                particleRate = 18f,
                particleSize = new Vector2(0.03f, 0.09f),

                particleStyleB = AmbientParticleStyle.Motes,
                particleColorB = new Color(0.35f, 0.30f, 0.26f),
                particleRateB = 10f,
                particleSizeB = new Vector2(0.2f, 0.5f)
            };
        }

        /// <summary>A tunnel of torii gates under cherry blossom at dusk; petals everywhere.</summary>
        private static EnvironmentPalette BuildSakuraGates()
        {
            Color hue = new Color(1f, 0.62f, 0.78f);
            Color accent = TubityXPalette.Accent(hue, 0.2f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.SakuraGates,
                displayName = "Sakura Gates",

                skyTop = new Color(0.30f, 0.26f, 0.52f),
                skyHorizon = new Color(0.98f, 0.66f, 0.72f),
                skyBottom = new Color(0.50f, 0.32f, 0.42f),
                horizonPower = 2.6f,
                detailColor = new Color(0.55f, 0.40f, 0.55f),
                detailScale = 1.6f,
                detailStrength = 0.4f,
                detailScroll = new Vector2(0.005f, 0.001f),
                detailStretch = new Vector2(1f, 0.35f),
                starDensity = 0.1f,
                sunDirection = new Vector3(0.25f, 0.12f, 0.96f),
                sunColor = new Color(1.6f, 1.2f, 1.1f),
                sunSize = 0.03f,
                sunHalo = 1.6f,

                fogColor = new Color(0.95f, 0.66f, 0.74f),
                fogStart = 40f,
                fogEnd = 280f,
                ambientLight = new Color(0.34f, 0.26f, 0.30f),
                ambientSky = new Color(0.36f, 0.32f, 0.46f),
                ambientGround = new Color(0.30f, 0.20f, 0.22f),
                sunLightColor = new Color(1f, 0.82f, 0.80f),
                sunLightIntensity = 0.8f,
                shadowStrength = 0.6f,

                accentColor = accent,
                rimLightIntensity = 0.3f,
                rimDirection = new Vector3(-0.6f, 0.3f, -0.7f),

                tubeTint = new Color(1f, 0.8f, 0.9f, 0.18f),
                tubeGridColor = TubityXPalette.Neon(hue, 0.5f),
                tubeBaseColor = TubityXPalette.DeepAlpha(hue, 0.06f, 0.1f),

                sphereColor = new Color(0.4f, 1f, 0.8f),
                effectPreset = EnergySphereEffects.EffectPreset.City,

                particleStyle = AmbientParticleStyle.Petals,
                particleColor = new Color(1f, 0.75f, 0.85f),
                particleRate = 26f,
                particleSize = new Vector2(0.12f, 0.26f),

                particleStyleB = AmbientParticleStyle.Fireflies,
                particleColorB = new Color(1f, 0.85f, 0.55f),
                particleRateB = 6f,
                particleSizeB = new Vector2(0.06f, 0.14f)
            };
        }

        /// <summary>A pastel sky of sweets: cotton-candy clouds, lollipops, candy canes and gumdrops.</summary>
        private static EnvironmentPalette BuildCandyClouds()
        {
            Color hue = new Color(1f, 0.7f, 0.9f);
            Color accent = TubityXPalette.Accent(hue, 0.2f);

            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.CandyClouds,
                displayName = "Sugar Sky",

                skyTop = new Color(0.55f, 0.75f, 1f),
                skyHorizon = new Color(1f, 0.78f, 0.90f),
                skyBottom = new Color(0.78f, 0.70f, 1f),
                horizonPower = 2f,
                detailColor = new Color(1f, 0.95f, 1f),
                detailScale = 1.8f,
                detailStrength = 0.35f,
                detailScroll = new Vector2(0.008f, 0.002f),
                detailStretch = new Vector2(1f, 0.5f),
                starDensity = 0f,
                sunDirection = new Vector3(0.3f, 0.6f, 0.75f),
                sunColor = new Color(1.4f, 1.3f, 1.1f),
                sunSize = 0.03f,
                sunHalo = 1.2f,

                fogColor = new Color(1f, 0.78f, 0.90f),
                fogStart = 50f,
                fogEnd = 300f,
                ambientLight = new Color(0.55f, 0.48f, 0.58f),
                ambientSky = new Color(0.55f, 0.62f, 0.80f),
                ambientGround = new Color(0.60f, 0.46f, 0.56f),
                sunLightColor = new Color(1f, 0.95f, 0.92f),
                sunLightIntensity = 0.7f,
                shadowStrength = 0.35f,

                accentColor = accent,
                rimLightIntensity = 0.25f,
                rimDirection = new Vector3(-0.6f, 0.3f, -0.7f),

                tubeTint = new Color(0.7f, 1f, 0.9f, 0.2f),
                tubeGridColor = TubityXPalette.Neon(new Color(1f, 0.6f, 0.85f), 0.5f),
                tubeBaseColor = new Color(1f, 0.9f, 0.95f, 0.1f),

                sphereColor = new Color(1f, 0.4f, 0.7f),
                effectPreset = EnergySphereEffects.EffectPreset.City,

                // Sprinkles hanging in the air, and soap-bubble sheen floating up.
                particleStyle = AmbientParticleStyle.Stardust,
                particleColor = new Color(1f, 1f, 1f),
                particleRate = 16f,
                particleSize = new Vector2(0.05f, 0.12f),

                particleStyleB = AmbientParticleStyle.Bubbles,
                particleColorB = new Color(0.8f, 0.9f, 1f),
                particleRateB = 8f,
                particleSizeB = new Vector2(0.15f, 0.35f)
            };
        }
    }
}
