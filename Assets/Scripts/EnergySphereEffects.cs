using UnityEngine;

namespace TubityWAI
{
    public class EnergySphereEffects : MonoBehaviour
    {
        public enum EffectPreset { Plasma, Warp, Gauntlet, City }
        public EffectPreset preset = EffectPreset.Plasma;

        [Header("Mesh Displacement Settings")]
        public float noiseScale = 4f;
        public float noiseSpeed = 3f;
        public float displacementStrength = 0.08f;

        private Mesh originalMesh;
        private Mesh deformedMesh;
        private Vector3[] baseVertices;
        private Vector3[] baseNormals;

        private void Start()
        {
            // Set up presets
            ApplyPresetSettings();

            // Setup Mesh Copy for vertex displacement
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null)
            {
                originalMesh = mf.sharedMesh;
                deformedMesh = Instantiate(originalMesh);
                deformedMesh.name = "DeformedSphereMesh";
                mf.mesh = deformedMesh;

                baseVertices = originalMesh.vertices;
                baseNormals = originalMesh.normals;
            }

            // Create Procedural Particle Material and Texture
            Texture2D particleTex = GenerateSoftCircleTexture();
            Material particleMat = CreateParticleMaterial(particleTex);

            // Setup custom visual effects
            CreateAuraGlow(particleMat);
            CreateSparkTrails(particleMat);
            CreateRibbonTrail(particleMat);
        }

        private void ApplyPresetSettings()
        {
            Renderer r = GetComponent<Renderer>();
            Material mat = r != null ? r.material : null;

            if (mat != null)
            {
                mat.SetColor("_BaseColor", new Color(0.015f, 0.015f, 0.015f));
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.015f, 0.015f, 0.015f));
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 1.0f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.9f);
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            Color outlineColor = Color.white;

            switch (preset)
            {
                case EffectPreset.Plasma:
                    noiseScale = 5f;
                    noiseSpeed = 4f;
                    displacementStrength = 0.08f;
                    outlineColor = new Color(0.85f, 0.05f, 1f);
                    break;

                case EffectPreset.Warp:
                    noiseScale = 3f;
                    noiseSpeed = 6f;
                    displacementStrength = 0.04f;
                    outlineColor = new Color(0f, 0.85f, 1f);
                    break;

                case EffectPreset.Gauntlet:
                    noiseScale = 6f;
                    noiseSpeed = 2f;
                    displacementStrength = 0.11f;
                    outlineColor = new Color(1f, 0.55f, 0f);
                    break;

                case EffectPreset.City:
                    noiseScale = 4f;
                    noiseSpeed = 3.5f;
                    displacementStrength = 0.05f;
                    outlineColor = new Color(0f, 1f, 0.75f);
                    break;
            }

            CreateOutlineShell(outlineColor);
        }

        private void CreateOutlineShell(Color outlineColor)
        {
            GameObject outlineObj = new GameObject("OutlineShell");
            outlineObj.transform.SetParent(this.transform, false);
            // Scale up slightly to form the outline border
            outlineObj.transform.localScale = Vector3.one * 1.07f;

            MeshFilter mf = outlineObj.AddComponent<MeshFilter>();
            MeshFilter parentMf = GetComponent<MeshFilter>();
            if (parentMf != null)
            {
                mf.mesh = parentMf.sharedMesh;
            }

            MeshRenderer mr = outlineObj.AddComponent<MeshRenderer>();
            
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");
            
            Material outlineMat = new Material(unlitShader);
            outlineMat.name = "OutlineMaterial";
            
            // Render only backfaces (Cull Front), creating the outer silhouette rim
            outlineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
            
            if (outlineMat.HasProperty("_BaseColor"))
            {
                outlineMat.SetColor("_BaseColor", outlineColor);
            }
            else
            {
                outlineMat.color = outlineColor;
            }

            outlineMat.EnableKeyword("_EMISSION");
            outlineMat.SetColor("_EmissionColor", outlineColor * 3.5f);

            mr.sharedMaterial = outlineMat;
        }

        private void Update()
        {
            if (deformedMesh == null || baseVertices == null) return;

            float t = Time.time * noiseSpeed;
            Vector3[] vertices = new Vector3[baseVertices.Length];

            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector3 v = baseVertices[i];
                Vector3 normal = baseNormals[i];

                // Triplanar noise mapping using the local vertex coordinates
                float nx = Mathf.PerlinNoise(v.y * noiseScale + t, v.z * noiseScale);
                float ny = Mathf.PerlinNoise(v.z * noiseScale + t, v.x * noiseScale);
                float nz = Mathf.PerlinNoise(v.x * noiseScale + t, v.y * noiseScale);

                float bx = Mathf.Abs(normal.x);
                float by = Mathf.Abs(normal.y);
                float bz = Mathf.Abs(normal.z);
                float sum = bx + by + bz;
                float noise = sum > 0.0001f ? (nx * bx + ny * by + nz * bz) / sum : 0f;

                // Overlay second octave for dynamic solar flares
                float nx2 = Mathf.PerlinNoise(v.y * noiseScale * 2.2f - t * 1.5f, v.z * noiseScale * 2.2f);
                float ny2 = Mathf.PerlinNoise(v.z * noiseScale * 2.2f - t * 1.5f, v.x * noiseScale * 2.2f);
                float nz2 = Mathf.PerlinNoise(v.x * noiseScale * 2.2f - t * 1.5f, v.y * noiseScale * 2.2f);
                float noise2 = sum > 0.0001f ? (nx2 * bx + ny2 * by + nz2 * bz) / sum : 0f;

                float finalNoise = (noise * 0.7f + noise2 * 0.3f) * displacementStrength;
                vertices[i] = v + normal * finalNoise;
            }

            deformedMesh.vertices = vertices;
            deformedMesh.RecalculateNormals();
            deformedMesh.RecalculateBounds();
        }

        private void CreateAuraGlow(Material mat)
        {
            GameObject auraObj = new GameObject("AuraGlow");
            auraObj.transform.SetParent(this.transform, false);

            ParticleSystem ps = auraObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = auraObj.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;

            // Configure Main Module
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = 0f; // locked to sphere
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.2f);

            Color baseAuraColor = Color.white;
            if (preset == EffectPreset.Plasma) baseAuraColor = new Color(0.85f, 0.05f, 1f);
            else if (preset == EffectPreset.Warp) baseAuraColor = new Color(0f, 0.75f, 1f);
            else if (preset == EffectPreset.Gauntlet) baseAuraColor = new Color(1f, 0.45f, 0f);

            main.startColor = Color.white; // Handled by gradient color keys

            // Configure Emission
            var emission = ps.emission;
            emission.rateOverTime = 35f;

            // Configure Shape (tight sphere)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            // Color Over Lifetime (Smooth Fade In/Out)
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(baseAuraColor, 0f), new GradientColorKey(baseAuraColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.2f), new GradientAlphaKey(0.45f, 0.8f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;
        }

        private void CreateSparkTrails(Material mat)
        {
            GameObject sparkObj = new GameObject("SparkTrails");
            sparkObj.transform.SetParent(this.transform, false);

            ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = sparkObj.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;

            // Configure Main Module
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // spark trails flow in space
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.26f);

            // Configure Emission
            var emission = ps.emission;
            emission.rateOverTime = 90f; // High frequency sparks

            // Configure Shape (sphere matching player size)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // Velocity over lifetime - pull backwards slightly in Z direction relative to forward movement
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            // Introduce slight drag/deceleration
            main.gravityModifier = 0f;

            // Spark Noise to add energetic turbulence
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 2.5f;
            noise.frequency = 2f;
            noise.scrollSpeed = 1.5f;

            // Color Over Lifetime Gradient
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient sparkGrad = new Gradient();

            if (preset == EffectPreset.Plasma)
            {
                // Purple, Violet, and Magenta sparks matching the sphere color
                sparkGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(0.85f, 0.05f, 1f), 0.2f), // Intense Magenta
                        new GradientColorKey(new Color(0.5f, 0f, 0.75f), 0.6f),  // Deep Purple
                        new GradientColorKey(new Color(0.2f, 0f, 0.4f), 1f)      // Dark Indigo
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0f),
                        new GradientAlphaKey(0.85f, 0.7f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else if (preset == EffectPreset.Warp)
            {
                // Electric Cyan to Royal Blue to Navy
                sparkGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(0f, 0.8f, 1f), 0.2f),
                        new GradientColorKey(new Color(0f, 0.2f, 1f), 0.6f),
                        new GradientColorKey(new Color(0f, 0f, 0.3f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0f),
                        new GradientAlphaKey(0.85f, 0.7f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else if (preset == EffectPreset.Gauntlet)
            {
                // Bright Yellow to Amber Orange to Crimson Red
                sparkGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(1f, 0.9f, 0.1f), 0.2f),
                        new GradientColorKey(new Color(1f, 0.45f, 0f), 0.6f),
                        new GradientColorKey(new Color(0.7f, 0f, 0f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0f),
                        new GradientAlphaKey(0.85f, 0.7f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }

            colorOverLifetime.color = sparkGrad;
        }

        private void CreateRibbonTrail(Material mat)
        {
            GameObject trailObj = new GameObject("RibbonTrail");
            trailObj.transform.SetParent(this.transform, false);

            TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
            trail.sharedMaterial = mat;
            trail.time = 0.5f;
            trail.startWidth = 0.8f;
            trail.endWidth = 0f;
            trail.numCornerVertices = 5;

            Gradient trailGrad = new Gradient();

            if (preset == EffectPreset.Plasma)
            {
                trailGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.85f, 0.05f, 1f), 0f),
                        new GradientColorKey(new Color(0.5f, 0f, 0.75f), 0.5f),
                        new GradientColorKey(Color.black, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.75f, 0f),
                        new GradientAlphaKey(0.35f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else if (preset == EffectPreset.Warp)
            {
                trailGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0f, 0.8f, 1f), 0f),
                        new GradientColorKey(new Color(0f, 0.3f, 0.8f), 0.5f),
                        new GradientColorKey(Color.black, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.75f, 0f),
                        new GradientAlphaKey(0.35f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else if (preset == EffectPreset.Gauntlet)
            {
                trailGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1f, 0.5f, 0f), 0f),
                        new GradientColorKey(new Color(0.8f, 0.15f, 0f), 0.5f),
                        new GradientColorKey(Color.black, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.75f, 0f),
                        new GradientAlphaKey(0.35f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }

            trail.colorGradient = trailGrad;
        }

        private Texture2D GenerateSoftCircleTexture()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float halfSize = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - halfSize + 0.5f;
                    float dy = y - halfSize + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / halfSize;
                    
                    // Quadratic dropoff for organic soft blurring around particle margins
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha;

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Material CreateParticleMaterial(Texture2D tex)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard");
            }

            Material mat = new Material(urpShader);
            mat.name = "SoftParticleAdditiveMat";

            // Configure for additive transparency blending
            mat.SetFloat("_Surface", 1f); // 1 = Transparent surface type
            mat.SetFloat("_Blend", 2f);   // 2 = Additive blend mode in URP
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
            }
            else if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
                mat.color = Color.white;
            }

            return mat;
        }
    }
}
