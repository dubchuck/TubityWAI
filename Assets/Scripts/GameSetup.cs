using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TubityWAI
{
    public class GameSetup : MonoBehaviour
    {
        [Header("Global Track Settings")]
        public float tubeRadius = 5f;
        public int radialSegments = 32;

        [Header("Player Settings")]
        public float angularSpeed = 4f;
        public float forwardSpeed = 15f;

        [Tooltip("Multiplier applied to forward speed when the Down Arrow or S key is held.")]
        public float speedBoostMultiplier = 2f;

        [Range(1, 5)]
        [Tooltip("Number of active spheres (1 to 5).")]
        public int sphereCount = 3;

        [Tooltip("Pleasant neon colors for each sphere (supports up to 5).")]
        public Color[] sphereColors = new Color[5]
        {
            new Color(1f, 0.4f, 0f),      // Neon Orange
            new Color(0f, 1f, 0f),        // Neon Green
            new Color(1f, 0f, 0.5f),      // Neon Pink
            new Color(1f, 0.9f, 0f),      // Neon Yellow
            new Color(0.5f, 0f, 1f)       // Neon Purple
        };

        [Header("Jump & Squash/Stretch Settings")]
        [Tooltip("Duration of the jump in seconds.")]
        public float jumpDuration = 0.6f;

        [Tooltip("Duration of the landing recovery bounce in seconds.")]
        public float landingRecoveryDuration = 0.2f;

        [Tooltip("Radial squash amount on takeoff.")]
        public float takeoffSquash = 0.15f;

        [Tooltip("Radial stretch amount during flight.")]
        public float flightStretch = 0.12f;

        [Tooltip("Radial squash amount on landing.")]
        public float landingSquash = 0.2f;

        [Header("Marker Pulse Animation")]
        [Tooltip("Duration of the sphere animation when crossing a marker (in seconds).")]
        public float markerAnimationDuration = 0.333f;

        [Tooltip("Peak size multiplier of the sphere when it crosses a marker.")]
        public float markerPulseScaleMultiplier = 1.15f;

        [Tooltip("Peak brightness (emission multiplier) of the sphere when it crosses a marker.")]
        public float markerPulseBrightnessMultiplier = 1.6f;

        [Header("Tunnel Settings")]
        public float segmentLength = 50f;
        public float markerInterval = 5f;
        public Color tunnelBaseColor = new Color(0.04f, 0.02f, 0.08f); // Glossy Dark Purple
        public Color markerColor = new Color(0f, 1f, 1f); // Neon Cyan

        [Header("Tunnel Texture Settings")]
        [Tooltip("Optional custom texture for the tunnel. If null, a glowing neon grid texture will be generated programmatically!")]
        public Texture2D tunnelTexture;

        [Tooltip("Color of the procedural grid lines (only used if tunnelTexture is null).")]
        public Color gridLineColor = new Color(0.12f, 0.25f, 0.75f); // Cyber Blue

        [Tooltip("Tiling multiplier around the tube circumference.")]
        public float gridTilingU = 8f;

        [Tooltip("Tiling multiplier along the length of each segment.")]
        public float gridTilingV = 8f;

        [Header("Volumetric Light Settings")]
        [Tooltip("Custom color for the volumetric light portal. If left black, it will default to the marker color.")]
        public Color volumetricLightColor = new Color(0f, 0.45f, 0.45f, 1f);

        [Tooltip("Distance ahead of the player to place the volumetric light portal.")]
        public float volumetricLightDistance = 175f;

        [Tooltip("Scale multiplier of the portal relative to the tube radius.")]
        public float volumetricLightSizeMultiplier = 3.2f;

        [Range(0f, 1f)]
        [Tooltip("Maximum opacity of the volumetric light center (default: 0.65).")]
        public float volumetricLightOpacity = 0.65f;

        [Tooltip("Falloff exponent of the glow. Higher values make it tighter/smaller, lower values make it wider/softer.")]
        public float volumetricLightFalloff = 1.8f;

        [Tooltip("Scale pulse multiplier when crossing a marker.")]
        public float volumetricLightPulseScale = 1.25f;

        [Tooltip("Color/Brightness pulse multiplier when crossing a marker.")]
        public float volumetricLightPulseBrightness = 1.8f;

        [Header("Collectible Settings")]
        [Tooltip("Neon color of the collectible coins.")]
        public Color coinColor = new Color(1f, 0.75f, 0f); // Neon Gold/Yellow

        private void Start()
        {
            // 1. Create Materials
            Material tunnelMaterial = CreateSpecularMaterial("TunnelMaterial", tunnelBaseColor, 0.85f);
            Material markerMaterial = CreateEmissiveMaterial("MarkerMaterial", markerColor, 4.5f, 0.1f);

            // Generate detailed dashed/antialiased texture for the marker rings
            Texture2D markerTex = GenerateMarkerTexture();
            if (markerTex != null)
            {
                string texProperty = markerMaterial.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                markerMaterial.SetTexture(texProperty, markerTex);
                if (markerMaterial.HasProperty("_EmissionMap"))
                {
                    markerMaterial.SetTexture("_EmissionMap", markerTex);
                }
                markerMaterial.SetTextureScale(texProperty, new Vector2(16f, 1f)); // Repeat 16 times around the circle
            }

            // Apply texture to tunnel material
            Texture2D activeTexture = tunnelTexture;
            if (activeTexture == null)
            {
                activeTexture = GenerateGridTexture(gridLineColor, tunnelBaseColor);
            }

            if (activeTexture != null)
            {
                string texProperty = tunnelMaterial.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                tunnelMaterial.SetTexture(texProperty, activeTexture);
                tunnelMaterial.SetTextureScale(texProperty, new Vector2(gridTilingU, gridTilingV));
            }
 
            // 2. Setup Player Group Parent
            GameObject playerGroup = new GameObject("PlayerGroup");
            PlayerController playerController = playerGroup.AddComponent<PlayerController>();
            playerController.radius = tubeRadius;
            playerController.angularSpeed = angularSpeed;
            playerController.forwardSpeed = forwardSpeed;
            playerController.speedBoostMultiplier = speedBoostMultiplier;
            playerController.markerInterval = markerInterval;
            playerController.animationDuration = markerAnimationDuration;
            playerController.pulseScaleMultiplier = markerPulseScaleMultiplier;
            playerController.pulseBrightnessMultiplier = markerPulseBrightnessMultiplier;

            // Pass jump parameters
            playerController.jumpDuration = jumpDuration;
            playerController.landingRecoveryDuration = landingRecoveryDuration;
            playerController.takeoffSquash = takeoffSquash;
            playerController.flightStretch = flightStretch;
            playerController.landingSquash = landingSquash;

            // Spawn the spheres as child objects of PlayerGroup
            int actualCount = Mathf.Clamp(sphereCount, 1, 5);

            // Create matching emissive coin materials for each player sphere color
            Material[] coinMaterials = new Material[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                Color color = sphereColors[i % sphereColors.Length];
                coinMaterials[i] = CreateEmissiveMaterial("CoinMaterial_" + i, color, 4.5f, 0.1f);
            }
            for (int i = 0; i < actualCount; i++)
            {
                Color color = sphereColors[i % sphereColors.Length];
                
                // Generate a retro-cyber grid texture specifically matching this sphere's color
                Texture2D sphereGridTex = GenerateSphereGridTexture(color * 2.0f, color * 0.15f);
                Material playerMaterial = CreateSphereMaterial("PlayerMaterial_" + i, color, 2.5f, 0.8f, sphereGridTex);

                GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphereObj.name = "PlayerSphere_" + i;
                sphereObj.transform.SetParent(playerGroup.transform, false);
                sphereObj.GetComponent<MeshRenderer>().sharedMaterial = playerMaterial;

                // Add PlayerSphere component to track color matching
                PlayerSphere sphereComponent = sphereObj.AddComponent<PlayerSphere>();
                sphereComponent.colorIndex = i;

                // Add a kinematic Rigidbody to enable trigger collision events with coins
                Rigidbody sphereRb = sphereObj.AddComponent<Rigidbody>();
                sphereRb.isKinematic = true;
                sphereRb.useGravity = false;

                // Add a point light to each sphere with its matching color
                GameObject lightObj = new GameObject("PointLight_" + i);
                lightObj.transform.SetParent(sphereObj.transform, false);
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.intensity = 15f / actualCount; // Split intensity to prevent over-exposure
                light.range = tubeRadius * 1.5f;
            }

            // Setup Volumetric Light Portal
            Texture2D glowTex = GenerateRadialGlowTexture(volumetricLightOpacity, volumetricLightFalloff);
            
            // If the user specified a custom color, use it. Otherwise, fallback to the marker color.
            Color glowColor = volumetricLightColor;
            if (glowColor.r == 0f && glowColor.g == 0f && glowColor.b == 0f)
            {
                glowColor = markerColor;
            }
            glowColor.a = 1f; // governed by texture opacity
            
            Material glowMat = CreateAdditiveGlowMaterial(glowColor, glowTex);

            GameObject portalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            portalObj.name = "VolumetricLightPortal";
            
            // Remove collider since we only need rendering
            Collider portalCollider = portalObj.GetComponent<Collider>();
            if (portalCollider != null) Destroy(portalCollider);
            
            portalObj.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

            // Position it at the end of the visible tunnel
            float portalDist = volumetricLightDistance;
            portalObj.transform.position = new Vector3(0f, 0f, portalDist);
            
            // Scaled slightly larger than tube diameter to cover the hole
            Vector3 portalScale = new Vector3(tubeRadius * volumetricLightSizeMultiplier, tubeRadius * volumetricLightSizeMultiplier, 1f);
            portalObj.transform.localScale = portalScale;
            portalObj.transform.rotation = Quaternion.LookRotation(Vector3.forward);

            // Link to player controller for animation
            playerController.volumetricLightTransform = portalObj.transform;
            playerController.volumetricLightMaterial = glowMat;
            playerController.volumetricLightBaseColor = glowColor;
            playerController.volumetricLightDistance = portalDist;
            playerController.baseVolumetricLightScale = portalScale;
            playerController.volumetricLightPulseScale = volumetricLightPulseScale;
            playerController.volumetricLightPulseBrightness = volumetricLightPulseBrightness;

            // Setup Game HUD
            GameObject hudObj = new GameObject("GameHUD");
            GameHUD hud = hudObj.AddComponent<GameHUD>();
            hud.player = playerController;

            // 3. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("MainCamera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            // Set camera rendering preferences for high contrast neon look
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
            mainCam.fieldOfView = 65f;

            // Enable high-quality anti-aliasing (SMAA) on the camera data for clean neon edges
            var cameraData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            if (cameraData != null)
            {
                cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
            }

            CameraController camController = mainCam.gameObject.GetComponent<CameraController>();
            if (camController == null)
            {
                camController = mainCam.gameObject.AddComponent<CameraController>();
            }
            camController.target = playerController;
            camController.followDistance = 7f;
            camController.followSmoothing = 12f;

            // 4. Setup Tunnel Generator
            GameObject tunnelGenObj = new GameObject("TunnelGenerator");
            TunnelGenerator tunnelGen = tunnelGenObj.AddComponent<TunnelGenerator>();
            tunnelGen.target = playerController;
            tunnelGen.radius = tubeRadius;
            tunnelGen.segmentLength = segmentLength;
            tunnelGen.radialSegments = radialSegments;
            tunnelGen.markerInterval = markerInterval;
            tunnelGen.tunnelMaterial = tunnelMaterial;
            tunnelGen.markerMaterial = markerMaterial;
            tunnelGen.coinMaterials = coinMaterials;

            // 5. Setup Ambient Lighting & Dim Existing Lights
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);

            Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in allLights)
            {
                // Dim down existing directional lights so they don't wash out the dark scene
                if (light.type == LightType.Directional)
                {
                    light.intensity = 0.1f;
                    light.color = new Color(0.1f, 0.1f, 0.2f);
                }
            }

            // 6. Setup Bloom Post-Processing
            try
            {
                SetupPostProcessing();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Bloom post-processing initialization skipped or failed: " + e.Message);
            }
        }

        private void SetupPostProcessing()
        {
            // Check if there is already a volume
            Volume existingVolume = FindFirstObjectByType<Volume>();
            if (existingVolume != null) return;

            GameObject volumeObj = new GameObject("NeonPostProcessing");
            Volume volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "NeonBloomProfile";

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.intensity.Override(1.8f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.7f);

            volume.sharedProfile = profile;
        }

        private Material CreateEmissiveMaterial(string name, Color color, float emissionIntensity, float smoothness)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard");
            }
            
            Material mat = new Material(urpShader);
            mat.name = name;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color * 0.5f);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color * 0.5f);

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionIntensity);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);

            return mat;
        }

        private Material CreateSpecularMaterial(string name, Color color, float smoothness)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard");
            }

            Material mat = new Material(urpShader);
            mat.name = name;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.1f);

            return mat;
        }

        private Texture2D GenerateGridTexture(Color lineColor, Color bgColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            int thickness = 4; // grid line thickness

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isGridLine = (x < thickness || x >= size - thickness || y < thickness || y >= size - thickness);
                    pixels[y * size + x] = isGridLine ? lineColor : bgColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Material CreateSphereMaterial(string name, Color color, float emissionIntensity, float smoothness, Texture2D tex)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard");
            }

            Material mat = new Material(urpShader);
            mat.name = name;

            // Set main texture and color
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white); // Texture contains the colors
            }
            else
            {
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
                mat.color = Color.white;
            }

            // Set emission map and color
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionMap"))
            {
                mat.SetTexture("_EmissionMap", tex);
            }
            mat.SetColor("_EmissionColor", Color.white * emissionIntensity);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.25f);

            return mat;
        }

        private Texture2D GenerateSphereGridTexture(Color lineColor, Color bgColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            int thickness = 1; // thin grid line thickness (1 pixel)
            int gridDivisions = 8;
            int spacing = size / gridDivisions;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isGridX = (x % spacing < thickness) || (x % spacing >= spacing - thickness);
                    bool isGridY = (y % spacing < thickness) || (y % spacing >= spacing - thickness);
                    
                    // Pole lines for sphere mapping
                    bool isNearPole = (y < thickness || y >= size - thickness);
                    
                    bool isGridLine = isGridX || isGridY || isNearPole;
                    pixels[y * size + x] = isGridLine ? lineColor : bgColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Material CreateAdditiveGlowMaterial(Color color, Texture2D tex)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Unlit/Texture");
            }
            
            Material mat = new Material(urpShader);
            mat.name = "AdditiveGlowMaterial";
            
            // Set shader keywords/properties for additive transparency
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", color);
            }
            else
            {
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
                mat.color = color;
            }
            
            return mat;
        }

        private Texture2D GenerateRadialGlowTexture(float maxOpacity, float falloff)
        {
            int size = 128; // Larger texture size for smoother blending
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            
            Color[] pixels = new Color[size * size];
            float halfSize = size * 0.5f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - halfSize;
                    float dy = y - halfSize;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / halfSize;
                    
                    // Physical exponential decay for natural light scattering/blur
                    float alpha = Mathf.Exp(-dist * falloff) * maxOpacity;
                    
                    // Smoothly fade out the outer edges to prevent any hard quad boundaries
                    if (dist >= 1.0f)
                    {
                        alpha = 0f;
                    }
                    else
                    {
                        float edgeFade = Mathf.Clamp01((1f - dist) / 0.2f); // Fade over the last 20%
                        alpha *= edgeFade;
                    }
                    
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateMarkerTexture()
        {
            int width = 256;
            int height = 16;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1);
                // Soft radial-sine falloff across the width of the line to act as natural antialiasing
                float edgeFade = Mathf.Sin(v * Mathf.PI);
                edgeFade = edgeFade * edgeFade; // Quadratic curve for a sharper core and softer edges
                
                for (int x = 0; x < width; x++)
                {
                    // Dashed digital ring pattern: repeat every 32 pixels
                    int localX = x % 32;
                    bool isSolid = localX < 24; // 24 pixels solid, 8 pixels gap
                    bool isAccentDot = (localX >= 27 && localX <= 28 && y >= 5 && y <= 10);
                    
                    float alpha = isSolid ? 1f : (isAccentDot ? 0.6f : 0f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha * edgeFade);
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
