using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TubityWAI
{
    /// <summary>
    /// Runtime owner of a themed environment: applies the procedural skybox,
    /// linear fog, ambient + directional lighting and a single camera-following
    /// ambient particle field. Trackside props live in EnvironmentScenery and are
    /// spawned per tunnel segment so they recycle with the tunnel pool.
    ///
    /// Mobile budget: one skybox pass, up to two billboard particle systems per live theme
    /// (&lt;= ~250 particles each, additive unlit), one extra shadowless directional light for
    /// the accent rim, and the main light's single-cascade hard shadow pass - which themes
    /// without trackside props switch off entirely.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Current { get; private set; }

        public EnvironmentPalette palette;

        /// <summary>Set for a multi-environment level; drives the palette from the player's distance every frame.</summary>
        public EnvironmentBlend blend;

        private Material skyMaterial;
        private Texture2D detailTexture;
        private Transform cameraTransform;
        private Light sunLight;

        /// <summary>
        /// A second, shadowless directional light in the theme's accent colour, aimed across the
        /// sun. One extra per-pixel light is the cheapest way to get a coloured edge on every prop,
        /// and it is what carries the logo's magenta into worlds whose own hue is nowhere near it.
        /// </summary>
        private Light rimLight;

        // Shadow and rim state are pipeline-level switches, so they are only written on a change:
        // a blended level would otherwise toggle them every frame while crossing a transition.
        private bool shadowsOn;
        private bool rimOn;
        private bool lightStateKnown;

        private const string RimLightName = "EnvironmentRim";

        /// <summary>One ambient particle system per distinct style in the blend, faded by that theme's weight.</summary>
        private class ParticleLayer
        {
            public EnvironmentTheme theme;
            public ParticleSystem system;
            public ParticleSystem.EmissionModule emission;
            public ParticleSystem.MainModule main;
            public float baseRate;
            public Color baseColor = Color.white;
            public float appliedWeight = -1f;
        }

        private readonly List<ParticleLayer> particleLayers = new List<ParticleLayer>();
        private readonly List<EnvironmentTheme> themeScratch = new List<EnvironmentTheme>();

        // Set pieces: a theme's backdrop (the star, the black hole) and its storm, one each per
        // theme the level visits, faded with that theme's weight.
        private readonly List<BackdropInstance> backdrops = new List<BackdropInstance>();
        private readonly List<EnvironmentStorm> storms = new List<EnvironmentStorm>();

        // Lightning briefly lifts the sky and fog toward this colour (see FlashSky).
        private static readonly Color FlashTint = new Color(0.75f, 0.82f, 1f);
        private float skyFlash;
        private float skyFlashApplied;

        // Set while the blend is parked on a single stop, so ApplyBlend can bail out early.
        private bool settled;
        private int settledStop = -1;

        /// <summary>Removes any environment created by a previous StartGame and clears caches.</summary>
        public static void Clear()
        {
            if (Current != null)
            {
                Destroy(Current.gameObject);
                Current = null;
            }
            EnvironmentScenery.ClearCache();
        }

        /// <summary>Builds the environment for a palette. Returns null for a null palette.</summary>
        public static EnvironmentManager Create(EnvironmentPalette palette, Camera cam)
        {
            return Create(palette, cam, null);
        }

        /// <summary>
        /// Builds the environment. With a blend the palette is only the starting point: the sky, fog,
        /// lighting and particle mix are re-evaluated from the player's distance every frame.
        /// </summary>
        public static EnvironmentManager Create(EnvironmentPalette palette, Camera cam, EnvironmentBlend blend)
        {
            if (palette == null) return null;
            Clear();

            GameObject root = new GameObject("Environment_" + palette.theme);
            EnvironmentManager mgr = root.AddComponent<EnvironmentManager>();
            mgr.palette = palette.Clone();   // a blend rewrites this in place, so never mutate the cached theme palette
            mgr.blend = (blend != null && blend.IsValid) ? blend : null;
            mgr.cameraTransform = cam != null ? cam.transform : null;
            mgr.Build(cam);
            Current = mgr;
            return mgr;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Build(Camera cam)
        {
            BuildSky(cam);
            ApplyAtmosphere();
            BuildSunLight();
            BuildRimLight();

            if (blend != null)
            {
                // Every theme the level ever visits gets its own particle field up front; the mix is
                // then a matter of fading emission rates, which costs nothing per frame.
                blend.AllThemes(themeScratch);
                List<EnvironmentTheme> all = new List<EnvironmentTheme>(themeScratch);
                for (int i = 0; i < all.Count; i++)
                {
                    BuildParticleFields(EnvironmentPalettes.GetOrClassic(all[i]), all[i]);
                }
                ApplyBlend(CurrentDistance());   // land on the right stop instead of showing stop 0 for a frame
            }
            else
            {
                BuildParticleFields(palette, palette.theme);
            }

            PlayParticleLayers();

            if (blend != null)
            {
                blend.AllThemes(themeScratch);
                List<EnvironmentTheme> all = new List<EnvironmentTheme>(themeScratch);
                for (int i = 0; i < all.Count; i++) AddSetPieces(all[i], cam);
            }
            else
            {
                AddSetPieces(palette.theme, cam);
            }
        }

        /// <summary>
        /// A theme's backdrop and storm, built the first time the level needs that theme (at start,
        /// or when the sandbox appends it mid-run).
        /// </summary>
        private void AddSetPieces(EnvironmentTheme theme, Camera cam)
        {
            EnvironmentPalette source = EnvironmentPalettes.Get(theme);
            if (source == null) return;

            if (source.backdrop != EnvironmentBackdrop.None && backdrops.Find(b => b.theme == theme) == null)
            {
                BackdropInstance b = EnvironmentBackdrops.Build(source, transform);
                if (b != null)
                {
                    backdrops.Add(b);
                    b.SetWeight(ThemeWeight(theme));
                    // The backdrop hangs well past the tunnel; make sure the camera still draws it.
                    Camera c = cam != null ? cam : Camera.main;
                    if (c != null) c.farClipPlane = Mathf.Max(c.farClipPlane, EnvironmentBackdrops.Distance + 400f);
                }
            }

            if (source.lightning > 0f && storms.Find(s => s.theme == theme) == null)
            {
                EnvironmentStorm storm = gameObject.AddComponent<EnvironmentStorm>();
                storm.manager = this;
                storm.theme = theme;
                storm.strikesPerSecond = source.lightning;
                storms.Add(storm);
            }
        }

        /// <summary>How much of the scene belongs to a theme right now: its blend weight at the
        /// player, or 1 for a level's only theme.</summary>
        public float ThemeWeight(EnvironmentTheme theme)
        {
            if (blend != null) return blend.Weight(theme, CurrentDistance());
            return theme == palette.theme ? 1f : 0f;
        }

        /// <summary>Lightning: lifts the sky and fog toward a cold white by `amount` (0 = normal).
        /// Applied in LateUpdate, after any blend has written the palette.</summary>
        public void FlashSky(float amount)
        {
            skyFlash = Mathf.Clamp01(amount);
        }

        private void ApplySkyFlash()
        {
            if (Mathf.Abs(skyFlash - skyFlashApplied) < 0.002f) return;
            skyFlashApplied = skyFlash;

            if (skyMaterial != null)
            {
                skyMaterial.SetColor("_HorizonColor", palette.skyHorizon + FlashTint * skyFlash * 0.8f);
                skyMaterial.SetColor("_TopColor", palette.skyTop + FlashTint * skyFlash * 0.45f);
                skyMaterial.SetColor("_DetailColor", palette.detailColor + FlashTint * skyFlash * 0.6f);
            }
            RenderSettings.fogColor = palette.fogColor + FlashTint * skyFlash * 0.45f;
        }

        /// <summary>
        /// The theme whose noise pattern dresses the sky. The shader has a single detail texture, so a
        /// blend borrows the pattern of its most detailed stop and fades it in with _DetailStrength.
        /// </summary>
        private EnvironmentTheme DetailTheme()
        {
            if (blend == null) return palette.theme;

            blend.AllThemes(themeScratch);
            EnvironmentTheme best = palette.theme;
            float bestStrength = -1f;
            for (int i = 0; i < themeScratch.Count; i++)
            {
                EnvironmentPalette p = EnvironmentPalettes.GetOrClassic(themeScratch[i]);
                if (p.detailStrength > bestStrength)
                {
                    bestStrength = p.detailStrength;
                    best = themeScratch[i];
                }
            }
            return best;
        }

        /// <summary>Distance along the tube the environment is evaluated at: the player, or the camera if there is none.</summary>
        private float CurrentDistance()
        {
            if (PlayerController.Instance != null) return PlayerController.Instance.transform.position.z;
            if (cameraTransform != null) return cameraTransform.position.z;
            return 0f;
        }

        // ------------------------------------------------------------------
        // Sky
        // ------------------------------------------------------------------
        private void BuildSky(Camera cam)
        {
            Shader skyShader = Shader.Find("TubityX/EnvironmentSky");
            if (skyShader == null)
            {
                Debug.LogWarning("[EnvironmentManager] TubityX/EnvironmentSky shader missing - falling back to a solid sky colour.");
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = palette.skyHorizon;
                }
                return;
            }

            EnvironmentTheme detailTheme = DetailTheme();
            detailTexture = GenerateNoiseTexture(256, detailTheme == EnvironmentTheme.Underwater ? 5f : 3f, (int)detailTheme * 101);

            skyMaterial = new Material(skyShader);
            skyMaterial.name = "EnvironmentSky_" + palette.theme;
            skyMaterial.SetTexture("_DetailTex", detailTexture);
            ApplySky();

            RenderSettings.skybox = skyMaterial;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.backgroundColor = palette.skyHorizon;
            }
        }

        /// <summary>Pushes the current palette into the sky material. Cheap enough to run every frame.</summary>
        private void ApplySky()
        {
            if (skyMaterial == null) return;

            skyMaterial.SetColor("_TopColor", palette.skyTop);
            skyMaterial.SetColor("_HorizonColor", palette.skyHorizon);
            skyMaterial.SetColor("_BottomColor", palette.skyBottom);
            skyMaterial.SetFloat("_HorizonPower", palette.horizonPower);
            skyMaterial.SetTexture("_DetailTex", detailTexture);
            skyMaterial.SetColor("_DetailColor", palette.detailColor);
            skyMaterial.SetFloat("_DetailScale", palette.detailScale);
            skyMaterial.SetFloat("_DetailStrength", palette.detailStrength);
            skyMaterial.SetVector("_DetailScroll", new Vector4(palette.detailScroll.x, palette.detailScroll.y, 0f, 0f));
            skyMaterial.SetVector("_DetailStretch", new Vector4(palette.detailStretch.x, palette.detailStretch.y, 0f, 0f));
            skyMaterial.SetFloat("_StarDensity", palette.starDensity);
            skyMaterial.SetColor("_StarColor", palette.starColor);
            skyMaterial.SetVector("_SunDir", new Vector4(palette.sunDirection.x, palette.sunDirection.y, palette.sunDirection.z, 0f));
            skyMaterial.SetColor("_SunColor", palette.sunColor);
            skyMaterial.SetFloat("_SunSize", palette.sunSize);
            skyMaterial.SetFloat("_SunHalo", palette.sunHalo);
        }

        /// <summary>Tileable multi-octave value noise in the red channel. Built once per level.</summary>
        private static Texture2D GenerateNoiseTexture(int size, float baseFrequency, int seed)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = "EnvironmentNoise";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            float ox = (seed * 0.137f) % 100f;
            float oy = (seed * 0.311f) % 100f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Sample on a torus so the texture tiles seamlessly in both axes.
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float n = 0f;
                    float amp = 0.55f;
                    float freq = baseFrequency;
                    for (int o = 0; o < 3; o++)
                    {
                        float sx = ox + Mathf.Sin(u * Mathf.PI * 2f) * freq;
                        float sy = oy + Mathf.Cos(u * Mathf.PI * 2f) * freq;
                        float sz = ox + Mathf.Sin(v * Mathf.PI * 2f) * freq;
                        float sw = oy + Mathf.Cos(v * Mathf.PI * 2f) * freq;
                        n += Mathf.PerlinNoise(sx + sz, sy + sw) * amp;
                        amp *= 0.5f;
                        freq *= 2.1f;
                    }
                    byte b = (byte)(Mathf.Clamp01(n) * 255f);
                    pixels[y * size + x] = new Color32(b, b, b, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, true);
            return tex;
        }

        // ------------------------------------------------------------------
        // Fog & ambient
        // ------------------------------------------------------------------
        private void ApplyAtmosphere()
        {
            // A level may pull the sight line in (Progression Test 1's blackout world); anything
            // that leaves previewScale at 1 keeps the theme's own fog exactly as authored.
            LevelConfig fogConfig = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            float preview = (fogConfig != null) ? Mathf.Clamp(fogConfig.previewScale, 0.25f, 1f) : 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = palette.fogColor;
            RenderSettings.fogStartDistance = palette.fogStart * preview;
            RenderSettings.fogEndDistance = palette.fogEnd * preview;

            // Trilight instead of Flat: a sky, equator and ground colour costs exactly the same to
            // render but gives every prop a top-to-bottom colour shift, which is most of what reads
            // as "solid" on untextured geometry. Themes that leave the two extra bands black fall
            // back to the old flat look rather than going dark.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientLight = palette.ambientLight;
            RenderSettings.ambientEquatorColor = palette.ambientLight;
            RenderSettings.ambientSkyColor = IsBlack(palette.ambientSky) ? palette.ambientLight : palette.ambientSky;
            RenderSettings.ambientGroundColor = IsBlack(palette.ambientGround) ? palette.ambientLight * 0.5f : palette.ambientGround;
        }

        private static bool IsBlack(Color c)
        {
            return c.r <= 0.0005f && c.g <= 0.0005f && c.b <= 0.0005f;
        }

        // ------------------------------------------------------------------
        // Directional light
        // ------------------------------------------------------------------
        private void BuildSunLight()
        {
            Light sun = null;
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type != LightType.Directional) continue;
                // Never adopt an accent rim as the sun. Clear() only marks the previous environment
                // for destruction, so if one was torn down earlier this frame its rim light is still
                // findable here - and adopting it would hand the shadow pass to a light with no
                // shadows and leave the real sun as a stray additional light.
                if (l.gameObject.name == RimLightName) continue;
                // Nor a storm's lightning flash, for the same reason.
                if (l.gameObject.name == EnvironmentStorm.FlashLightName) continue;
                sun = l;
                break;
            }

            if (sun == null)
            {
                GameObject sunObj = new GameObject("EnvironmentSun");
                sunObj.transform.SetParent(transform, false);
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.None;
            }

            sunLight = sun;
            // Name the main light explicitly. URP otherwise picks the brightest directional, and the
            // accent rim added next is a second one - if it ever won, the shadow pass would move to a
            // light that casts nothing, because the mobile RP asset has additional-light shadows off.
            RenderSettings.sun = sunLight;
            ApplySunLight();
        }

        /// <summary>
        /// The accent light. It is created after the sun so BuildSunLight's scan for an existing
        /// directional can never pick it up, and it is disabled outright when a theme asks for no
        /// rim, because a URP additional light costs a per-pixel pass even at zero intensity.
        /// </summary>
        private void BuildRimLight()
        {
            GameObject rimObj = new GameObject(RimLightName);
            rimObj.transform.SetParent(transform, false);
            rimLight = rimObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.shadows = LightShadows.None;
            rimLight.enabled = false;
            ApplyRimLight();
        }

        private void ApplySunLight()
        {
            if (sunLight == null) return;
            sunLight.color = palette.sunLightColor;
            sunLight.intensity = palette.sunLightIntensity;
            // Light travels from the sun towards the scene.
            sunLight.transform.rotation = Quaternion.LookRotation(-palette.sunDirection.normalized, Vector3.up);

            // The mobile URP asset is configured for exactly this: one 1024 shadowmap, one cascade,
            // 50m, hard filtering. Themes with no trackside props leave shadowStrength at 0 so the
            // pass is skipped entirely rather than rendering an empty map.
            bool wantShadows = palette.shadowStrength > 0.01f;
            if (!lightStateKnown || wantShadows != shadowsOn)
            {
                sunLight.shadows = wantShadows ? LightShadows.Hard : LightShadows.None;
                shadowsOn = wantShadows;
            }
            if (wantShadows)
            {
                sunLight.shadowStrength = Mathf.Clamp01(palette.shadowStrength);
                // Props are thin and often steeply lit; the normal bias does the anti-acne work so the
                // depth bias can stay low enough that contact shadows still touch their caster.
                sunLight.shadowBias = 0.06f;
                sunLight.shadowNormalBias = 0.45f;
                sunLight.shadowNearPlane = 0.3f;
            }
        }

        private void ApplyRimLight()
        {
            if (rimLight == null) return;

            // Never let the rim outshine the sun: URP promotes the brightest directional to main
            // light, which would move the shadow pass onto a light that is not casting any.
            float intensity = Mathf.Min(palette.rimLightIntensity, palette.sunLightIntensity * 0.85f);
            bool wantRim = intensity > 0.01f;
            if (!lightStateKnown || wantRim != rimOn)
            {
                rimLight.enabled = wantRim;
                rimOn = wantRim;
            }
            if (!wantRim) { lightStateKnown = true; return; }

            rimLight.color = palette.accentColor;
            rimLight.intensity = intensity;
            Vector3 dir = palette.rimDirection.sqrMagnitude > 0.0001f ? palette.rimDirection.normalized : Vector3.back;
            rimLight.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            lightStateKnown = true;
        }

        // ------------------------------------------------------------------
        // Ambient particles
        // ------------------------------------------------------------------
        /// <summary>
        /// Builds a theme's ambient air: its main field plus the sparser accent field layered under
        /// it. Two systems per theme is still well inside the mobile budget - both are billboard,
        /// unlit and capped - and the second layer is what puts the brand's magenta in the air of
        /// worlds whose own hue is nowhere near it.
        /// </summary>
        private void BuildParticleFields(EnvironmentPalette source, EnvironmentTheme theme)
        {
            if (source == null) return;
            BuildAmbientParticles(source, theme, source.particleStyle, source.particleColor, source.particleRate, source.particleSize, "A");
            BuildAmbientParticles(source, theme, source.particleStyleB, source.particleColorB, source.particleRateB, source.particleSizeB, "B");
        }

        private void BuildAmbientParticles(EnvironmentPalette source, EnvironmentTheme theme,
                                           AmbientParticleStyle style, Color color, float rate, Vector2 size2, string suffix)
        {
            if (source == null || style == AmbientParticleStyle.None) return;

            GameObject psObj = new GameObject("AmbientParticles_" + theme + suffix);
            psObj.transform.SetParent(transform, false);
            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 260;
            main.startColor = color;
            main.startSize = new ParticleSystem.MinMaxCurve(size2.x, size2.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.prewarm = true;
            main.loop = true;
            main.playOnAwake = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            // Emit in a long box ahead of the camera so the field is already populated as we fly in.
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 34f, 160f);
            shape.position = new Vector3(0f, 0f, 70f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;

            var vel = ps.velocityOverLifetime;
            var size = ps.sizeOverLifetime;

            switch (style)
            {
                case AmbientParticleStyle.Stardust:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
                    main.startSpeed = 0f;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, Blink(3, 0.3f));
                    break;

                case AmbientParticleStyle.Fireflies:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.4f, 0.5f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, Blink(4, 0.15f));
                    break;

                case AmbientParticleStyle.Bubbles:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(0.5f);
                    vel.y = Ranged(0.8f, 1.6f);
                    vel.z = Ranged(0f, 0f);
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.1f));
                    break;

                case AmbientParticleStyle.Embers:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(0.8f);
                    vel.y = Ranged(1.5f, 3.5f);
                    vel.z = Ranged(-0.5f, 0.5f);
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
                    break;

                case AmbientParticleStyle.Snow:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(0.4f);
                    vel.y = Ranged(-1.6f, -0.8f);
                    vel.z = Ranged(0f, 0f);
                    break;

                // Near-still dust that creeps back down the tube, so flying through it reads as
                // motion rather than as a static field pinned to the camera.
                case AmbientParticleStyle.Motes:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 13f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(0.25f);
                    vel.y = Ranged(-0.15f, 0.2f);
                    vel.z = Ranged(-2.2f, -0.9f);
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, Blink(2, 0.45f));
                    break;

                // Slanting rain, fast enough to streak (see the stretched renderer below).
                case AmbientParticleStyle.Rain:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Ranged(-3f, -1.5f);
                    vel.y = Ranged(-24f, -17f);
                    vel.z = Ranged(-5f, -2.5f);
                    break;

                // Petals: a slow, wide, tumbling fall.
                case AmbientParticleStyle.Petals:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(1.1f);
                    vel.y = Ranged(-1.3f, -0.6f);
                    vel.z = Ranged(-0.8f, 0.4f);
                    var tumble = ps.rotationOverLifetime;
                    tumble.enabled = true;
                    tumble.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
                    break;

                // Plasma or infalling gas rushing back past the camera.
                case AmbientParticleStyle.Streaks:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(1.2f);
                    vel.y = Ranged(-0.8f, 0.8f);
                    vel.z = Ranged(-34f, -20f);
                    break;

                // Hot flecks spat off machinery: a quick drop, burning out as they go.
                case AmbientParticleStyle.Sparks:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(1.6f);
                    vel.y = Ranged(-7f, -2.5f);
                    vel.z = Ranged(-1f, 1f);
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
                    break;

                // Heavier than snow and swaying wider, to sit under the ember layer.
                case AmbientParticleStyle.Ash:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = Wobbling(0.9f);
                    vel.y = Ranged(-2.4f, -1.2f);
                    vel.z = Ranged(0f, 0f);
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.75f));
                    break;
            }

            ParticleSystemRenderer renderer = psObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (style == AmbientParticleStyle.Rain || style == AmbientParticleStyle.Streaks || style == AmbientParticleStyle.Sparks)
            {
                // Fast things read as streaks, not dots.
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = style == AmbientParticleStyle.Sparks ? 0.05f : 0.04f;
                renderer.lengthScale = 1f;
                renderer.cameraVelocityScale = 0f;
            }
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = CreateParticleMaterial();

            particleLayers.Add(new ParticleLayer
            {
                theme = theme,
                system = ps,
                emission = emission,
                main = main,
                baseRate = rate,
                baseColor = color
            });
        }

        /// <summary>
        /// Starts the particle fields. Deliberately separate from building them: the systems prewarm on
        /// Play, so a blend has to weight their emission down first or every theme's motes would appear
        /// at once on the first frame - fireflies inside the solid tube, for instance.
        /// </summary>
        private void PlayParticleLayers()
        {
            for (int i = 0; i < particleLayers.Count; i++)
            {
                particleLayers[i].system.Play();
            }
        }

        /// <summary>A size curve with N pulses so motes twinkle / fireflies blink.</summary>
        private static AnimationCurve Blink(int pulses, float floor)
        {
            AnimationCurve c = new AnimationCurve();
            int keys = pulses * 2 + 1;
            for (int i = 0; i < keys; i++)
            {
                float t = (float)i / (keys - 1);
                float v = (i % 2 == 0) ? floor : 1f;
                c.AddKey(new Keyframe(t, v, 0f, 0f));
            }
            return c;
        }

        /// <summary>
        /// A velocity axis carrying the Wobble curve, in TwoCurves mode.
        ///
        /// Unity requires velocityOverLifetime's x, y and z to share one curve mode, so an axis that
        /// only needs a constant range still has to be expressed as curves (see Ranged). Getting this
        /// wrong throws "Particle Velocity curves must all be in the same mode" the next time anything
        /// touches the system - which, for a blended level, is every frame.
        /// </summary>
        private static ParticleSystem.MinMaxCurve Wobbling(float amplitude)
        {
            return new ParticleSystem.MinMaxCurve(amplitude, Wobble(), Wobble());
        }

        /// <summary>A constant [min, max] velocity range expressed as flat curves, to match Wobbling's mode.</summary>
        private static ParticleSystem.MinMaxCurve Ranged(float min, float max)
        {
            float scale = Mathf.Max(Mathf.Abs(min), Mathf.Abs(max));
            if (scale < 0.0001f) return new ParticleSystem.MinMaxCurve(1f, Flat(0f), Flat(0f));
            return new ParticleSystem.MinMaxCurve(scale, Flat(min / scale), Flat(max / scale));
        }

        private static AnimationCurve Flat(float value)
        {
            return AnimationCurve.Constant(0f, 1f, value);
        }

        /// <summary>Two full sine cycles in [-1, 1]; the MinMaxCurve multiplier sets the amplitude.</summary>
        private static AnimationCurve Wobble()
        {
            AnimationCurve c = new AnimationCurve();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                c.AddKey(new Keyframe(t, Mathf.Sin(t * Mathf.PI * 4f)));
            }
            return c;
        }

        private static Material CreateParticleMaterial()
        {
            Shader s = Shader.Find("TubityX/NeonParticle");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s == null) s = Shader.Find("Sprites/Default");

            Material mat = new Material(s);
            mat.name = "EnvironmentParticleMat";
            if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", 1.6f);
            if (mat.HasProperty("_Softness")) mat.SetFloat("_Softness", 1.8f);
            return mat;
        }

        // ------------------------------------------------------------------
        // Blending
        // ------------------------------------------------------------------

        /// <summary>
        /// Re-evaluates the blend at a distance and pushes the result through the sky material, fog,
        /// ambient light, sun and particle mix. All of it is property writes on objects that already
        /// exist, so a blended level costs the same to render as a fixed one.
        /// </summary>
        private void ApplyBlend(float distance)
        {
            if (blend == null) return;

            // Once the level has settled on a stop the palette stops changing, so skip the whole pass
            // until the next cross-fade starts. Only the transition stretches cost anything per frame.
            // Stop identity, not theme: a route like the solar system holds one theme across every
            // stop and would otherwise settle on the first frame and never move the sun again.
            int fromStop, toStop;
            float t;
            blend.SampleStops(distance, out fromStop, out toStop, out t);
            if (fromStop == toStop)
            {
                if (settled && settledStop == fromStop) return;
                settled = true;
                settledStop = fromStop;
            }
            else
            {
                settled = false;
            }

            EnvironmentPalette blended = blend.Evaluate(distance);
            // palette is this manager's own copy, so overwriting it never touches the cached theme palettes.
            EnvironmentPalette.CopyInto(blended, palette);

            ApplySky();
            ApplyAtmosphere();
            ApplySunLight();
            ApplyRimLight();

            for (int i = 0; i < particleLayers.Count; i++)
            {
                ParticleLayer layer = particleLayers[i];
                float weight = blend.Weight(layer.theme, distance);
                // Writing a particle module re-validates the whole system, so only do it on a real change.
                if (Mathf.Abs(weight - layer.appliedWeight) < 0.002f) continue;
                layer.appliedWeight = weight;

                layer.emission.rateOverTime = layer.baseRate * weight;

                // Fade the motes out as their theme recedes instead of leaving them hanging at full
                // brightness. The colour comes off the layer, not the palette, because a theme now
                // owns two layers with two different colours.
                Color c = layer.baseColor;
                c.a *= weight;
                layer.main.startColor = c;
            }
        }

        /// <summary>
        /// Extends a blended level while it runs: the world starts cross-fading to <paramref name="theme"/>
        /// <paramref name="distanceAhead"/> units past the player and has settled <paramref name="blendLength"/>
        /// units after that. The tube blender and trackside scenery read the same blend object, so they
        /// follow on their own; only the particle field needs building here, because Build() made
        /// layers for the themes the blend visited at level start and nothing else.
        /// Returns false when the level has no blend to extend.
        /// </summary>
        public bool TransitionTo(EnvironmentTheme theme, float distanceAhead = 40f, float blendLength = 80f)
        {
            if (blend == null) return false;

            float z = CurrentDistance();
            float last = blend.StopCount > 0 ? blend.stops[blend.StopCount - 1].distance : z;
            // Stops must increase, so a change requested mid-fade queues behind the one in progress.
            float target = Mathf.Max(z + distanceAhead + blendLength, last + 1f);
            blend.To(theme, target, blendLength);

            bool hasLayer = false;
            for (int i = 0; i < particleLayers.Count; i++)
            {
                if (particleLayers[i].theme == theme) { hasLayer = true; break; }
            }
            if (!hasLayer)
            {
                int first = particleLayers.Count;
                BuildParticleFields(EnvironmentPalettes.GetOrClassic(theme), theme);
                AddSetPieces(theme, null);
                // Prewarm fills the field at full rate on Play, so weight the new layers to nothing
                // first and let ApplyBlend fade them in as the theme gains weight.
                for (int i = first; i < particleLayers.Count; i++)
                {
                    ParticleLayer layer = particleLayers[i];
                    layer.emission.rateOverTime = 0f;
                    Color c = layer.baseColor;
                    c.a = 0f;
                    layer.main.startColor = c;
                    layer.appliedWeight = 0f;
                    layer.system.Play();
                }
            }

            settled = false;
            return true;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            if (blend != null) ApplyBlend(CurrentDistance());

            for (int i = 0; i < backdrops.Count; i++) backdrops[i].SetWeight(ThemeWeight(backdrops[i].theme));
            ApplySkyFlash();

            if (cameraTransform == null) return;

            // Keep the emitter on the camera; world-space simulation means already-emitted
            // particles stay where they are and drift past as the player flies through them.
            transform.position = cameraTransform.position;
        }
    }
}
