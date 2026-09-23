using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TubityWAI
{
    /// <summary>
    /// A theme's far-off set piece - the star in Corona, the black hole in Event Horizon. It hangs
    /// in the sky along the palette's sunDirection, parented to the EnvironmentManager's root, which
    /// rides with the camera: like the skybox it never gets closer, which is what makes it read as
    /// enormous and distant. The sense of scale comes from the nearer scenery passing in front of it.
    /// </summary>
    public class BackdropInstance
    {
        public EnvironmentTheme theme;
        public GameObject root;
        public readonly List<Material> materials = new List<Material>();
        public float appliedWeight = -1f;

        /// <summary>Fades the whole piece with its theme's share of a blend.</summary>
        public void SetWeight(float weight)
        {
            if (Mathf.Abs(weight - appliedWeight) < 0.002f) return;
            appliedWeight = weight;
            bool show = weight > 0.001f;
            if (root.activeSelf != show) root.SetActive(show);
            for (int i = 0; i < materials.Count; i++)
                if (materials[i] != null && materials[i].HasProperty("_Fade")) materials[i].SetFloat("_Fade", weight);
        }
    }

    public static class EnvironmentBackdrops
    {
        /// <summary>How far out a backdrop hangs. Well past the tunnel and scenery, inside the far clip.</summary>
        public const float Distance = 450f;

        public static BackdropInstance Build(EnvironmentPalette palette, Transform envRoot)
        {
            if (palette == null) return null;
            switch (palette.backdrop)
            {
                case EnvironmentBackdrop.Sun: return BuildSun(palette, envRoot);
                case EnvironmentBackdrop.BlackHole: return BuildBlackHole(palette, envRoot);
                default: return null;
            }
        }

        private static BackdropInstance NewInstance(EnvironmentPalette palette, Transform envRoot, string name)
        {
            BackdropInstance b = new BackdropInstance { theme = palette.theme };
            b.root = new GameObject(name);
            b.root.transform.SetParent(envRoot, false);
            return b;
        }

        /// <summary>A camera-facing quad at the backdrop's distance, covering `halfAngleDeg` either way.</summary>
        private static GameObject Billboard(Transform parent, Vector3 dir, float halfAngleDeg, Material mat)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = dir * Distance;
            quad.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            float size = 2f * Distance * Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad);
            quad.transform.localScale = new Vector3(size, size, 1f);

            MeshRenderer r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return quad;
        }

        private static Material ShaderMaterial(string shaderName, string name)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("[EnvironmentBackdrops] Shader " + shaderName + " missing; skipping the backdrop.");
                return null;
            }
            return new Material(shader) { name = name };
        }

        // ------------------------------------------------------------------
        // The star: a boiling disc in a streaming corona, prominence loops
        // arching off its limb
        // ------------------------------------------------------------------
        private static BackdropInstance BuildSun(EnvironmentPalette palette, Transform envRoot)
        {
            Material disc = ShaderMaterial("TubityX/SunDisc", "Backdrop_SunDisc");
            if (disc == null) return null;

            BackdropInstance b = NewInstance(palette, envRoot, "Backdrop_Sun");
            Vector3 dir = palette.sunDirection.normalized;

            // The shader's disc fills 0.42 of the quad's half-width; the rest is corona. A 38-degree
            // half-angle quad puts the star at about 18 degrees across - close enough to fill the view.
            const float halfAngle = 38f;
            GameObject quad = Billboard(b.root.transform, dir, halfAngle, disc);
            b.materials.Add(disc);

            // Loops rooted on the limb, standing out from the disc and turning slowly with it.
            Shader glowShader = Shader.Find("TubityX/PlasmaGlow");
            if (glowShader != null)
            {
                float discRadius = Distance * Mathf.Tan(halfAngle * Mathf.Deg2Rad) * 0.42f;
                Quaternion face = Quaternion.LookRotation(dir, Vector3.up);
                Random.State prev = Random.state;
                Random.InitState(4242);
                for (int i = 0; i < 5; i++)
                {
                    Material loop = new Material(glowShader) { name = "Backdrop_SunLoop" };
                    loop.SetColor("_Color", new Color(1f, 0.5f, 0.12f));
                    loop.SetFloat("_Intensity", 3.2f);
                    loop.SetFloat("_Flow", 1.5f);
                    b.materials.Add(loop);

                    float around = i * 72f + Random.Range(-20f, 20f);
                    Quaternion spin = Quaternion.AngleAxis(around, dir);
                    Vector3 up = spin * (face * Vector3.up);
                    float size = discRadius * Random.Range(0.12f, 0.22f);

                    GameObject arc = new GameObject("FlareLoop");
                    arc.transform.SetParent(b.root.transform, false);
                    // Feet on the limb, the loop standing out from the disc into the corona.
                    arc.transform.localPosition = dir * (Distance - 2f) + up * discRadius * 0.97f;
                    arc.transform.localRotation = Quaternion.LookRotation(dir, up) * Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f);
                    arc.transform.localScale = Vector3.one * size;
                    MeshFilter mf = arc.AddComponent<MeshFilter>();
                    mf.sharedMesh = WorldMeshes.TorusArc(1f, 0.08f, 180f);
                    MeshRenderer mr = arc.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = loop;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    PulseScale pulse = arc.AddComponent<PulseScale>();
                    pulse.amount = 0.12f;
                    pulse.period = Random.Range(5f, 9f);
                }
                Random.state = prev;
            }
            return b;
        }

        // ------------------------------------------------------------------
        // The black hole: one quad, all procedural (TubityX/BlackHole)
        // ------------------------------------------------------------------
        private static BackdropInstance BuildBlackHole(EnvironmentPalette palette, Transform envRoot)
        {
            Material mat = ShaderMaterial("TubityX/BlackHole", "Backdrop_BlackHole");
            if (mat == null) return null;

            BackdropInstance b = NewInstance(palette, envRoot, "Backdrop_BlackHole");
            // Half-angle 36 degrees: the disc spans most of the view, the shadow a tenth of it.
            Billboard(b.root.transform, palette.sunDirection.normalized, 36f, mat);
            b.materials.Add(mat);
            return b;
        }
    }
}
