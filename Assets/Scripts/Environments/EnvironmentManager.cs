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
    /// Mobile budget: one skybox pass, one particle system (&lt;= ~250 particles,
    /// additive unlit), no extra lights, no shadows.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Current { get; private set; }

        public EnvironmentPalette palette;

        private Material skyMaterial;
        private Texture2D detailTexture;
        private ParticleSystem ambientParticles;
        private Transform cameraTransform;

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
            if (palette == null) return null;
            Clear();

            GameObject root = new GameObject("Environment_" + palette.theme);
            EnvironmentManager mgr = root.AddComponent<EnvironmentManager>();
            mgr.palette = palette;
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
            BuildAtmosphere();
            BuildSunLight();
            BuildAmbientParticles();
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

            detailTexture = GenerateNoiseTexture(256, palette.theme == EnvironmentTheme.Underwater ? 5f : 3f, (int)palette.theme * 101);

            skyMaterial = new Material(skyShader);
            skyMaterial.name = "EnvironmentSky_" + palette.theme;
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

            RenderSettings.skybox = skyMaterial;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.backgroundColor = palette.skyHorizon;
            }
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
        private void BuildAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = palette.fogColor;
            RenderSettings.fogStartDistance = palette.fogStart;
            RenderSettings.fogEndDistance = palette.fogEnd;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = palette.ambientLight;
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
                if (l.type == LightType.Directional) { sun = l; break; }
            }

            if (sun == null)
            {
                GameObject sunObj = new GameObject("EnvironmentSun");
                sunObj.transform.SetParent(transform, false);
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.None;
            }

            sun.color = palette.sunLightColor;
            sun.intensity = palette.sunLightIntensity;
            // Light travels from the sun towards the scene.
            sun.transform.rotation = Quaternion.LookRotation(-palette.sunDirection.normalized, Vector3.up);
        }

        // ------------------------------------------------------------------
        // Ambient particles
        // ------------------------------------------------------------------
        private void BuildAmbientParticles()
        {
            if (palette.particleStyle == AmbientParticleStyle.None) return;

            GameObject psObj = new GameObject("AmbientParticles");
            psObj.transform.SetParent(transform, false);
            ambientParticles = psObj.AddComponent<ParticleSystem>();
            ambientParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ambientParticles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 260;
            main.startColor = palette.particleColor;
            main.startSize = new ParticleSystem.MinMaxCurve(palette.particleSize.x, palette.particleSize.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.prewarm = true;
            main.loop = true;
            main.playOnAwake = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ambientParticles.emission;
            emission.rateOverTime = palette.particleRate;

            // Emit in a long box ahead of the camera so the field is already populated as we fly in.
            var shape = ambientParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 34f, 160f);
            shape.position = new Vector3(0f, 0f, 70f);

            var col = ambientParticles.colorOverLifetime;
            col.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;

            var vel = ambientParticles.velocityOverLifetime;
            var size = ambientParticles.sizeOverLifetime;

            switch (palette.particleStyle)
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
                    vel.x = new ParticleSystem.MinMaxCurve(0.5f, Wobble());
                    vel.y = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
                    vel.z = 0f;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.1f));
                    break;

                case AmbientParticleStyle.Embers:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(0.8f, Wobble());
                    vel.y = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
                    break;

                case AmbientParticleStyle.Snow:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
                    main.startSpeed = 0f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(0.4f, Wobble());
                    vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.8f);
                    vel.z = 0f;
                    break;
            }

            ParticleSystemRenderer renderer = psObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = CreateParticleMaterial();

            ambientParticles.Play();
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

        private void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (cameraTransform == null) return;

            // Keep the emitter on the camera; world-space simulation means already-emitted
            // particles stay where they are and drift past as the player flies through them.
            transform.position = cameraTransform.position;
        }
    }
}
