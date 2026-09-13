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
        Grid
    }

    /// <summary>How the ambient particle field behaves for a theme.</summary>
    public enum AmbientParticleStyle
    {
        None,
        Stardust,   // still, twinkling motes
        Fireflies,  // slow wandering, blinking
        Bubbles,    // rise steadily, wobble
        Embers,     // rise fast, flicker out
        Snow        // drift down slowly
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
        public Color ambientLight;
        public Color sunLightColor;
        public float sunLightIntensity = 0.5f;

        // --- Tunnel ---
        public Color tubeTint;             // transparent tube material colour
        public Color tubeGridColor;        // procedural grid lines
        public Color tubeBaseColor;        // grid background (alpha = tube opacity)

        // --- Player ---
        public Color sphereColor;
        public EnergySphereEffects.EffectPreset effectPreset = EnergySphereEffects.EffectPreset.Plasma;

        // --- Ambient particles ---
        public AmbientParticleStyle particleStyle = AmbientParticleStyle.None;
        public Color particleColor = Color.white;
        public float particleRate = 20f;
        public Vector2 particleSize = new Vector2(0.15f, 0.4f);
    }

    public static class EnvironmentPalettes
    {
        private static EnvironmentPalette space, jungle, underwater, volcano, crystal, grid;

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
                default: return null;
            }
        }

        private static EnvironmentPalette BuildSpace()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Space,
                displayName = "Deep Space",

                skyTop = new Color(0.03f, 0.01f, 0.10f),
                skyHorizon = new Color(0.22f, 0.06f, 0.36f),
                skyBottom = new Color(0.01f, 0.01f, 0.05f),
                horizonPower = 4f,
                detailColor = new Color(0.45f, 0.12f, 0.65f),
                detailScale = 1.6f,
                detailStrength = 1.0f,
                detailScroll = new Vector2(0.004f, 0.001f),
                detailStretch = new Vector2(1f, 0.7f),
                starDensity = 1.0f,
                starColor = new Color(0.9f, 0.95f, 1.1f),
                sunDirection = new Vector3(-0.45f, 0.35f, 0.8f),
                sunColor = new Color(0.8f, 0.9f, 1.3f),
                sunSize = 0.012f,
                sunHalo = 1.2f,

                fogColor = new Color(0.22f, 0.06f, 0.36f),
                fogStart = 90f,
                fogEnd = 460f,
                ambientLight = new Color(0.10f, 0.08f, 0.18f),
                sunLightColor = new Color(0.75f, 0.8f, 1f),
                sunLightIntensity = 0.5f,

                tubeTint = new Color(0.35f, 0.7f, 1f, 0.3f),
                tubeGridColor = new Color(0.6f, 0.9f, 1f, 1f),
                tubeBaseColor = new Color(0.02f, 0.02f, 0.08f, 0.22f),

                sphereColor = new Color(0.2f, 0.9f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                particleStyle = AmbientParticleStyle.Stardust,
                particleColor = new Color(0.8f, 0.9f, 1f),
                particleRate = 18f,
                particleSize = new Vector2(0.08f, 0.25f)
            };
        }

        private static EnvironmentPalette BuildJungle()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Jungle,
                displayName = "Jungle Canopy",

                skyTop = new Color(0.05f, 0.38f, 0.32f),
                skyHorizon = new Color(0.62f, 0.82f, 0.28f),
                skyBottom = new Color(0.02f, 0.14f, 0.06f),
                horizonPower = 2.5f,
                detailColor = new Color(0.12f, 0.45f, 0.18f),
                detailScale = 2.2f,
                detailStrength = 0.7f,
                detailScroll = new Vector2(0.012f, 0.002f),
                detailStretch = new Vector2(1f, 1f),
                starDensity = 0f,
                sunDirection = new Vector3(0.45f, 0.6f, 0.65f),
                sunColor = new Color(1.2f, 1.0f, 0.5f),
                sunSize = 0.05f,
                sunHalo = 1.6f,

                fogColor = new Color(0.62f, 0.82f, 0.28f),
                fogStart = 35f,
                fogEnd = 330f,
                ambientLight = new Color(0.12f, 0.22f, 0.10f),
                sunLightColor = new Color(1f, 0.92f, 0.6f),
                sunLightIntensity = 0.75f,

                tubeTint = new Color(0.3f, 0.9f, 0.35f, 0.3f),
                tubeGridColor = new Color(0.55f, 1f, 0.25f, 1f),
                tubeBaseColor = new Color(0.02f, 0.06f, 0.02f, 0.22f),

                sphereColor = new Color(1f, 0.85f, 0.1f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                particleStyle = AmbientParticleStyle.Fireflies,
                particleColor = new Color(1f, 0.95f, 0.45f),
                particleRate = 14f,
                particleSize = new Vector2(0.12f, 0.3f)
            };
        }

        private static EnvironmentPalette BuildUnderwater()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Underwater,
                displayName = "Abyssal Reef",

                skyTop = new Color(0.0f, 0.5f, 0.65f),
                skyHorizon = new Color(0.0f, 0.22f, 0.42f),
                skyBottom = new Color(0.0f, 0.02f, 0.10f),
                horizonPower = 2f,
                detailColor = new Color(0.25f, 0.8f, 0.9f),
                detailScale = 4f,
                detailStrength = 0.55f,
                detailScroll = new Vector2(0.03f, 0.02f),
                detailStretch = new Vector2(1f, 1f),
                starDensity = 0f,
                sunDirection = new Vector3(0.1f, 0.9f, 0.4f),
                sunColor = new Color(0.6f, 1.0f, 1.1f),
                sunSize = 0.12f,
                sunHalo = 2.2f,

                fogColor = new Color(0.0f, 0.22f, 0.42f),
                fogStart = 20f,
                fogEnd = 270f,
                ambientLight = new Color(0.05f, 0.16f, 0.24f),
                sunLightColor = new Color(0.5f, 0.85f, 1f),
                sunLightIntensity = 0.55f,

                tubeTint = new Color(0.1f, 0.85f, 1f, 0.3f),
                tubeGridColor = new Color(0.25f, 1f, 0.9f, 1f),
                tubeBaseColor = new Color(0.0f, 0.04f, 0.08f, 0.2f),

                sphereColor = new Color(1f, 0.45f, 0.75f),
                effectPreset = EnergySphereEffects.EffectPreset.Plasma,

                particleStyle = AmbientParticleStyle.Bubbles,
                particleColor = new Color(0.7f, 0.95f, 1f),
                particleRate = 22f,
                particleSize = new Vector2(0.1f, 0.35f)
            };
        }

        private static EnvironmentPalette BuildVolcano()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Volcano,
                displayName = "Magma Core",

                skyTop = new Color(0.03f, 0.0f, 0.01f),
                skyHorizon = new Color(0.70f, 0.14f, 0.02f),
                skyBottom = new Color(0.30f, 0.04f, 0.0f),
                horizonPower = 3.5f,
                detailColor = new Color(0.55f, 0.18f, 0.08f),
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
                sunLightColor = new Color(1f, 0.42f, 0.15f),
                sunLightIntensity = 0.6f,

                tubeTint = new Color(1f, 0.35f, 0.05f, 0.3f),
                tubeGridColor = new Color(1f, 0.55f, 0.1f, 1f),
                tubeBaseColor = new Color(0.06f, 0.02f, 0.0f, 0.22f),

                sphereColor = new Color(0.35f, 0.95f, 1f),
                effectPreset = EnergySphereEffects.EffectPreset.Gauntlet,

                particleStyle = AmbientParticleStyle.Embers,
                particleColor = new Color(1f, 0.5f, 0.1f),
                particleRate = 26f,
                particleSize = new Vector2(0.06f, 0.2f)
            };
        }

        private static EnvironmentPalette BuildCrystal()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Crystal,
                displayName = "Aurora Cavern",

                skyTop = new Color(0.02f, 0.03f, 0.14f),
                skyHorizon = new Color(0.10f, 0.36f, 0.58f),
                skyBottom = new Color(0.02f, 0.05f, 0.12f),
                horizonPower = 3f,
                detailColor = new Color(0.15f, 0.95f, 0.55f),
                detailScale = 1.4f,
                detailStrength = 1.1f,
                detailScroll = new Vector2(0.015f, 0.004f),
                detailStretch = new Vector2(1f, 0.35f),   // wide horizontal bands = aurora curtains
                starDensity = 0.6f,
                starColor = new Color(0.85f, 0.95f, 1f),
                sunDirection = new Vector3(0.5f, 0.55f, 0.65f),
                sunColor = new Color(0.9f, 1.0f, 1.2f),
                sunSize = 0.02f,
                sunHalo = 0.8f,

                fogColor = new Color(0.10f, 0.36f, 0.58f),
                fogStart = 35f,
                fogEnd = 350f,
                ambientLight = new Color(0.10f, 0.15f, 0.24f),
                sunLightColor = new Color(0.7f, 0.85f, 1f),
                sunLightIntensity = 0.55f,

                tubeTint = new Color(0.55f, 0.75f, 1f, 0.3f),
                tubeGridColor = new Color(0.75f, 0.9f, 1f, 1f),
                tubeBaseColor = new Color(0.02f, 0.04f, 0.10f, 0.2f),

                sphereColor = new Color(1f, 0.3f, 0.9f),
                effectPreset = EnergySphereEffects.EffectPreset.City,

                particleStyle = AmbientParticleStyle.Snow,
                particleColor = new Color(0.9f, 0.97f, 1f),
                particleRate = 24f,
                particleSize = new Vector2(0.08f, 0.22f)
            };
        }

        /// <summary>
        /// A stark void wrapped in a bright wireframe grid: near-black sky and fog with no
        /// stars or nebula detail, and no trackside props (see EnvironmentScenery's dispatch
        /// switch), so the tube's own glowing grid lines are the entire scene.
        /// </summary>
        private static EnvironmentPalette BuildGrid()
        {
            return new EnvironmentPalette
            {
                theme = EnvironmentTheme.Grid,
                displayName = "Cyber Grid",

                skyTop = new Color(0.01f, 0.02f, 0.05f),
                skyHorizon = new Color(0.02f, 0.05f, 0.10f),
                skyBottom = new Color(0.0f, 0.0f, 0.01f),
                horizonPower = 5f,
                detailColor = new Color(0.03f, 0.08f, 0.14f),
                detailScale = 3f,
                detailStrength = 0.15f,
                detailScroll = new Vector2(0.006f, 0.002f),
                detailStretch = new Vector2(1f, 1f),
                starDensity = 0f,
                sunDirection = new Vector3(0.2f, 0.6f, 0.75f),
                sunColor = new Color(0.5f, 0.9f, 1.1f),
                sunSize = 0.02f,
                sunHalo = 0.6f,

                fogColor = new Color(0.02f, 0.05f, 0.10f),
                fogStart = 25f,
                fogEnd = 220f,
                ambientLight = new Color(0.05f, 0.09f, 0.14f),
                sunLightColor = new Color(0.6f, 0.9f, 1f),
                sunLightIntensity = 0.45f,

                tubeTint = new Color(0.1f, 0.85f, 1f, 0.3f),
                tubeGridColor = new Color(0.15f, 0.85f, 1f, 1f),
                tubeBaseColor = new Color(0.0f, 0.0f, 0.0f, 0.28f),

                sphereColor = new Color(0.2f, 1f, 0.9f),
                effectPreset = EnergySphereEffects.EffectPreset.Warp,

                particleStyle = AmbientParticleStyle.None
            };
        }
    }
}
