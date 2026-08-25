using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TubityWAI
{
    [ExecuteAlways]
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

        [Tooltip("Neon color of the collectible coins.")]
        public Color coinColor = new Color(1f, 0.75f, 0f); // Neon Gold/Yellow

        [Tooltip("Neon color of the powerups.")]
        public Color powerupColor = new Color(0.8f, 1f, 1f); // Glowing Cyan/White

        [Tooltip("Neon color of the magnet powerup.")]
        public Color magnetColor = new Color(0.8f, 0.2f, 1f); // Neon Purple

        [Header("Obstacle Settings")]
        [Tooltip("Neon color of the obstacles.")]
        public Color obstacleColor = new Color(1f, 0f, 0.2f); // Neon Hot Pink/Red

        [Range(0f, 1f)]
        [Tooltip("Spawn probability of obstacles at each marker ring.")]
        public float obstacleSpawnProbability = 0.45f;

        [Header("Menu Settings")]
        [Tooltip("Enable to show the two-layer paginated Main Menu at startup.")]
        public bool showMainMenu = true;

        public static GameSetup Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;

#if !UNITY_EDITOR
                if (FindFirstObjectByType<TVOSMenuNavigator>() == null)
                {
                    GameObject navObj = new GameObject("TVOSMenuNavigatorController");
                    navObj.AddComponent<TVOSMenuNavigator>();
                }
#endif

                // Spawn MainMenu programmatically at startup if toggled and not replaying
                bool isReplaying = GameManager.shouldReplayOnLoad && GameManager.lastLevelConfig != null;
                if (showMainMenu && !isReplaying && FindFirstObjectByType<MainMenu>() == null)
                {
                    GameObject menuObj = new GameObject("MainMenuController");
                    menuObj.AddComponent<MainMenu>();
                }
            }
            else
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (!Application.isPlaying) return; // Skip game initialization when in Editor edit mode

            // Check if a replay was triggered from the Game Over screen
            if (GameManager.shouldReplayOnLoad && GameManager.lastLevelConfig != null)
            {
                int replayCount = GameManager.lastSphereCount;
                LevelConfig replayConfig = GameManager.lastLevelConfig;
                StartGame(replayCount, replayConfig);
                GameManager.shouldReplayOnLoad = false;
                return;
            }

            // If MainMenu is in the scene, wait for user selection and start attraction mode. Otherwise run immediately with defaults for editor testing.
            if (FindFirstObjectByType<MainMenu>() != null)
            {
                StartAttractionMode();
                return;
            }

            StartGameWithDefaultSettings();
        }

        private void StartGameWithDefaultSettings()
        {
            LevelConfig defaultConfig = new LevelConfig(1, forwardSpeed, obstacleSpawnProbability, speedBoostMultiplier);
            StartGame(sphereCount, defaultConfig);
        }

        public void StartGame(int chosenCount, LevelConfig config)
        {
            // Hide MainMenu if present in scene
            MainMenu mainMenu = FindFirstObjectByType<MainMenu>();
            if (mainMenu != null)
            {
                mainMenu.Hide();
            }

            // 0. Clean up Attraction Mode if active
            GameObject attractionGen = GameObject.Find("AttractionTunnelGenerator");
            if (attractionGen != null) Destroy(attractionGen);

            GameObject attractionWorld = GameObject.Find("AttractionModeWorldContainer");
            if (attractionWorld != null) Destroy(attractionWorld);

            GameObject attractionCam = GameObject.Find("AttractionModeCameraContainer");
            if (attractionCam != null) Destroy(attractionCam);
            
            // Clean up any generated tunnel segments from attraction mode
            TunnelSegment[] existingSegments = FindObjectsByType<TunnelSegment>(FindObjectsSortMode.None);
            foreach (var segment in existingSegments)
            {
                Destroy(segment.gameObject);
            }

            // Reset camera position and target
            Camera mainCam = Camera.main;
            CameraController camController = null;
            if (mainCam != null)
            {
                mainCam.transform.position = Vector3.zero;
                mainCam.transform.rotation = Quaternion.identity;
                if (config.isTransparentTube)
                {
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = new Color(0.015f, 0.015f, 0.04f); // Deep synthwave night sky
                }
                else
                {
                    mainCam.clearFlags = CameraClearFlags.Skybox; // Restore default skybox for gameplay
                }
                
                camController = mainCam.gameObject.GetComponent<CameraController>();
                if (camController != null)
                {
                    camController.target = null;
                }
            }

            // Override setup settings with chosen level parameters (force 1 sphere for test levels)
            this.sphereCount = config.isTestLevel ? 1 : chosenCount;
            this.forwardSpeed = config.forwardSpeed;
            this.obstacleSpawnProbability = config.obstacleSpawnProbability;
            this.speedBoostMultiplier = config.speedBoostMultiplier;

            // Instantiate GameManager singleton if not already present
            if (GameManager.Instance == null)
            {
                GameObject managerObj = new GameObject("GameManager");
                managerObj.AddComponent<GameManager>();
            }

            // Instantiate AdMobManager singleton if not already present
            if (AdMobManager.Instance == null)
            {
                GameObject adManagerObj = new GameObject("AdMobManager");
                adManagerObj.AddComponent<AdMobManager>();
            }

            // Cache current parameters on the manager instance for potential replay selection
            GameManager.Instance.currentSphereCount = chosenCount;
            GameManager.Instance.currentLevelConfig = config;

            // 1. Create Materials
            Material tunnelMaterial;
            if (config.isTransparentTube)
            {
                tunnelMaterial = CreateTransparentMaterial("TransparentTunnelMaterial", new Color(0f, 0.85f, 1f, 0.3f), 0.5f, 0.95f);
            }
            else
            {
                tunnelMaterial = CreateSpecularMaterial("TunnelMaterial", tunnelBaseColor, 0.85f);
            }

            Material markerMaterial = CreateEmissiveMaterial("MarkerMaterial", markerColor, 4.5f, 0.1f);
            Material obstacleMaterial = CreateEmissiveMaterial("ObstacleMaterial", obstacleColor, 4.0f, 0.1f);
            Material powerupMaterial = CreateEmissiveMaterial("PowerupMaterial", powerupColor, 5.5f, 0.5f);
            Material magnetMaterial = CreateEmissiveMaterial("MagnetMaterial", magnetColor, 5.5f, 0.5f);

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
                Color gLineColor = config.isTransparentTube ? new Color(0f, 1f, 0.85f, 1f) : gridLineColor;
                Color tBaseColor = config.isTransparentTube ? new Color(0.01f, 0.02f, 0.06f, 0.3f) : tunnelBaseColor;
                activeTexture = GenerateGridTexture(gLineColor, tBaseColor);
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
            // Create matching transparent materials for color-coded passable obstacles
            Material[] transparentObstacleMaterials = new Material[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                Color color = sphereColors[i % sphereColors.Length];
                coinMaterials[i] = CreateEmissiveMaterial("CoinMaterial_" + i, color, 4.5f, 0.1f);
                transparentObstacleMaterials[i] = CreateTransparentMaterial("TransparentObstacleMaterial_" + i, color, 4.0f, 0.1f);
            }
            for (int i = 0; i < actualCount; i++)
            {
                Color color = sphereColors[i % sphereColors.Length];
                
                // Override color if it's a test level
                if (config.isTestLevel)
                {
                    if (config.levelNumber == 101)
                        color = new Color(0.78f, 0.05f, 1f); // Plasma Purple
                    else if (config.levelNumber == 102)
                        color = new Color(0f, 0.85f, 1f); // Warp Blue/Cyan
                    else if (config.levelNumber == 103)
                        color = new Color(1f, 0.55f, 0f); // Gauntlet Amber/Orange
                    else if (config.levelNumber == 106)
                        color = new Color(0f, 1f, 0.75f); // Cyber Cyan/Teal
                }
                
                Material playerMaterial;
                int equippedSkin = GameManager.Instance != null ? GameManager.Instance.EquippedSkin : 0;
                bool isFXSkin = equippedSkin >= 6;

                if (config.isTestLevel || (equippedSkin == 1) || isFXSkin)
                {
                    // For Solid Core (1) or FX skins or test levels, we just use a base emissive material (no grid)
                    playerMaterial = CreateEmissiveMaterial("PlayerMaterial_" + i, color, 3.0f, 0.9f);
                }
                else
                {
                    // Generate texture based on equipped skin
                    Texture2D skinTex = null;
                    if (equippedSkin == 0) skinTex = GenerateSphereGridTexture(color * 2.0f, color * 0.15f);
                    else if (equippedSkin == 2) skinTex = GenerateStripesTexture(color * 2.0f, color * 0.15f);
                    else if (equippedSkin == 3) skinTex = GenerateCheckerboardTexture(color * 2.0f, color * 0.15f);
                    else if (equippedSkin == 4) skinTex = GenerateCircuitTexture(color * 2.0f, color * 0.15f);
                    else if (equippedSkin == 5) skinTex = GenerateDiamondTexture(color * 2.0f, color * 0.15f);
                    else skinTex = GenerateSphereGridTexture(color * 2.0f, color * 0.15f); // fallback

                    playerMaterial = CreateSphereMaterial("PlayerMaterial_" + i, color, 2.5f, 0.8f, skinTex);
                }

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
                light.range = 7f;
                light.intensity = 1.5f;

                // Apply FX if it's a test level OR an FX skin is equipped
                if (config.isTestLevel || isFXSkin)
                {
                    EnergySphereEffects effects = sphereObj.AddComponent<EnergySphereEffects>();
                    
                    if (config.isTestLevel)
                    {
                        if (config.levelNumber == 101) effects.preset = EnergySphereEffects.EffectPreset.Plasma;
                        else if (config.levelNumber == 102) effects.preset = EnergySphereEffects.EffectPreset.Warp;
                        else if (config.levelNumber == 103) effects.preset = EnergySphereEffects.EffectPreset.Gauntlet;
                        else if (config.levelNumber == 106) effects.preset = EnergySphereEffects.EffectPreset.City;
                    }
                    else
                    {
                        // Map shop skins to presets
                        if (equippedSkin == 6) effects.preset = EnergySphereEffects.EffectPreset.Plasma;
                        else if (equippedSkin == 7) effects.preset = EnergySphereEffects.EffectPreset.Warp;
                        else if (equippedSkin == 8) effects.preset = EnergySphereEffects.EffectPreset.Gauntlet;
                        else if (equippedSkin == 9) effects.preset = EnergySphereEffects.EffectPreset.City;
                    }
                }

                // Position spheres equidistant around the entire 360-degree circle
                float startAngle = (i * 2f * Mathf.PI) / actualCount;

                float x = Mathf.Sin(startAngle) * tubeRadius;
                float y = -Mathf.Cos(startAngle) * tubeRadius;
                sphereObj.transform.localPosition = new Vector3(x, y, 0f);
            }


            // 3. Setup Volumetric Light Card
            GameObject volLightObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            volLightObj.name = "VolumetricLightPortal";
            Destroy(volLightObj.GetComponent<Collider>()); // No collision needed for card

            Texture2D glowTex = GenerateRadialGlowTexture(volumetricLightOpacity, volumetricLightFalloff);
            Color glowColor = volumetricLightColor;
            if (glowColor.r == 0f && glowColor.g == 0f && glowColor.b == 0f)
            {
                glowColor = markerColor;
            }
            glowColor.a = 1f;

            Material volLightMaterial = CreateAdditiveGlowMaterial(glowColor, glowTex);
            volLightObj.GetComponent<MeshRenderer>().sharedMaterial = volLightMaterial;

            // Anchor it to player controller group so it remains visible at depth
            volLightObj.transform.SetParent(playerGroup.transform, false);
            volLightObj.transform.localPosition = new Vector3(0f, 0f, 175f); // 175 units ahead
            volLightObj.transform.localScale = new Vector3(tubeRadius * volumetricLightSizeMultiplier, tubeRadius * volumetricLightSizeMultiplier, 1f); // Cover the tube cross-section

            // Bind portal references in the PlayerController for rhythmic pulses
            playerController.volumetricLightTransform = volLightObj.transform;
            playerController.volumetricLightMaterial = volLightMaterial;
            playerController.volumetricLightBaseColor = glowColor;
            playerController.baseVolumetricLightScale = volLightObj.transform.localScale;
            playerController.volumetricLightPulseScale = volumetricLightPulseScale;
            playerController.volumetricLightPulseBrightness = volumetricLightPulseBrightness;

            // Setup Camera follow target and settings
            if (mainCam != null)
            {
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

                camController = mainCam.gameObject.GetComponent<CameraController>();
                if (camController == null)
                {
                    camController = mainCam.gameObject.AddComponent<CameraController>();
                }
                camController.target = playerController;
                camController.followDistance = 7f;
                camController.followSmoothing = 12f;
            }

            // Setup Game HUD
            GameObject hudObj = new GameObject("GameHUD");
            GameHUD hud = hudObj.AddComponent<GameHUD>();
            hud.player = playerController;

            // Setup FTUE Manager if this is the FTUE Tutorial level
            if (config.levelNumber == 99 || (config.levelName != null && config.levelName.ToUpper().Contains("HOW TO PLAY")))
            {
                GameObject ftueObj = new GameObject("FTUEManager");
                ftueObj.AddComponent<FTUEManager>();
            }

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
            tunnelGen.powerupMaterial = powerupMaterial;
            tunnelGen.magnetMaterial = magnetMaterial;
            tunnelGen.obstacleMaterial = obstacleMaterial;
            tunnelGen.transparentObstacleMaterials = transparentObstacleMaterials;
            tunnelGen.obstacleSpawnProbability = obstacleSpawnProbability;

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

        private void SetupPostProcessing(float bloomIntensity = 1.8f, float bloomThreshold = 0.85f, float bloomScatter = 0.7f)
        {
            // Check if there is already a volume
            Volume existingVolume = FindFirstObjectByType<Volume>();
            Volume volume = existingVolume;
            if (volume == null)
            {
                GameObject volumeObj = new GameObject("NeonPostProcessing");
                volume = volumeObj.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1f;

                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "NeonBloomProfile";
                volume.sharedProfile = profile;
            }

            // Find or add Bloom override
            Bloom bloom;
            if (!volume.sharedProfile.TryGet<Bloom>(out bloom))
            {
                bloom = volume.sharedProfile.Add<Bloom>(true);
            }
            
            bloom.active = true;
            bloom.intensity.Override(bloomIntensity);
            bloom.threshold.Override(bloomThreshold);
            bloom.scatter.Override(bloomScatter);
        }

        private Shader GetSafeShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Unlit/Texture");

            if (s == null && GraphicsSettings.defaultRenderPipeline != null && GraphicsSettings.defaultRenderPipeline.defaultMaterial != null)
            {
                s = GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader;
            }
            if (s == null && Canvas.GetDefaultCanvasMaterial() != null)
            {
                s = Canvas.GetDefaultCanvasMaterial().shader;
            }
            return s;
        }

        private Material CreateSafeMaterial(string name)
        {
            Shader s = GetSafeShader();
            Material mat = (s != null) ? new Material(s) : new Material(Canvas.GetDefaultCanvasMaterial());
            mat.name = name;
            return mat;
        }

        private Material CreateEmissiveMaterial(string name, Color color, float emissionIntensity, float smoothness)
        {
            Material mat = CreateSafeMaterial(name);

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
            Material mat = CreateSafeMaterial(name);

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
            Material mat = CreateSafeMaterial(name);

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

        private Texture2D GenerateStripesTexture(Color lineColor, Color bgColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];

            int stripeWidth = 16;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Vertical stripes wrapping the sphere
                    bool isStripe = (x / stripeWidth) % 2 == 0;
                    pixels[y * size + x] = isStripe ? lineColor : bgColor;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateCheckerboardTexture(Color lineColor, Color bgColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];

            int checkSize = 16;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isChecker = ((x / checkSize) % 2 == 0) ^ ((y / checkSize) % 2 == 0);
                    pixels[y * size + x] = isChecker ? lineColor : bgColor;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateCircuitTexture(Color lineColor, Color bgColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            
            // Base background
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

            int lineThickness = 2;
            int numLines = 8;
            
            // Draw some "circuit" lines
            for (int l = 0; l < numLines; l++)
            {
                int x0 = Random.Range(10, size - 10);
                int y0 = Random.Range(10, size - 10);
                int length = Random.Range(30, 80);
                
                // Draw line and dot
                for (int d = 0; d < length; d++)
                {
                    // simple diagonal/straight routing
                    int dx = (l % 2 == 0) ? d : (d / 2);
                    int dy = (l % 2 == 0) ? (d / 2) : d;
                    
                    int px = Mathf.Clamp(x0 + dx, 0, size - 1);
                    int py = Mathf.Clamp(y0 + dy, 0, size - 1);
                    
                    for (int tx = -lineThickness; tx <= lineThickness; tx++)
                    {
                        for (int ty = -lineThickness; ty <= lineThickness; ty++)
                        {
                            int px2 = Mathf.Clamp(px + tx, 0, size - 1);
                            int py2 = Mathf.Clamp(py + ty, 0, size - 1);
                            pixels[py2 * size + px2] = lineColor;
                        }
                    }
                    
                    // Draw node at end
                    if (d == length - 1)
                    {
                        for (int nx = -4; nx <= 4; nx++)
                        {
                            for (int ny = -4; ny <= 4; ny++)
                            {
                                if (nx*nx + ny*ny <= 16)
                                {
                                    int px2 = Mathf.Clamp(px + nx, 0, size - 1);
                                    int py2 = Mathf.Clamp(py + ny, 0, size - 1);
                                    pixels[py2 * size + px2] = lineColor;
                                }
                            }
                        }
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateDiamondTexture(Color lineColor, Color bgColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];

            int diamondScale = 16;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x % diamondScale;
                    int dy = y % diamondScale;
                    bool isLine = Mathf.Abs(dx - dy) < 2 || Mathf.Abs((diamondScale - dx) - dy) < 2;
                    pixels[y * size + x] = isLine ? lineColor : bgColor;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Material CreateAdditiveGlowMaterial(Color color, Texture2D tex)
        {
            Material mat = CreateSafeMaterial("AdditiveGlowMaterial");
            
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

        private Material CreateTransparentMaterial(string name, Color color, float emissionIntensity = 1f, float smoothness = 0.5f)
        {
            Material mat = CreateSafeMaterial(name);

            // Configure for standard alpha transparency blending in URP
            mat.SetFloat("_Surface", 1f); // 1 = Transparent
            mat.SetFloat("_Blend", 0f);   // 0 = Alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            Color baseColor = color * 0.4f;
            baseColor.a = 0.35f; // Nice holographic transparency alpha

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", baseColor);

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionIntensity);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);

            return mat;
        }

        public void StartAttractionMode()
        {
            // Clean up any existing attraction containers first (safety check)
            GameObject oldWorld = GameObject.Find("AttractionModeWorldContainer");
            if (oldWorld != null) DestroyImmediate(oldWorld);
            GameObject oldCamContainer = GameObject.Find("AttractionModeCameraContainer");
            if (oldCamContainer != null) DestroyImmediate(oldCamContainer);

            // 1. Create Materials (transparent black tunnel base for attraction mode)
            Material tunnelMaterial = CreateSpecularMaterial("TunnelMaterial", new Color(0f, 0f, 0f, 0.35f), 0.98f);
            tunnelMaterial.SetFloat("_Surface", 1.0f); // 0 = Opaque, 1 = Transparent
            tunnelMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            tunnelMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            tunnelMaterial.SetInt("_ZWrite", 0);
            tunnelMaterial.DisableKeyword("_ALPHATEST_ON");
            tunnelMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            tunnelMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

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
                markerMaterial.SetTextureScale(texProperty, new Vector2(16f, 1f));
            }

            // 2. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = Vector3.zero;
                mainCam.transform.rotation = Quaternion.identity;

                // Clear to solid dark blue-black color matching the black tunnel
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.015f, 0.015f, 0.025f);

                CameraController camController = mainCam.gameObject.GetComponent<CameraController>();
                if (camController == null)
                {
                    camController = mainCam.gameObject.AddComponent<CameraController>();
                }
                camController.target = null;
                camController.attractionSpeed = forwardSpeed * 0.3f; // relaxed speed
            }

            // 3. Create Attraction Containers
            GameObject worldContainer = new GameObject("AttractionModeWorldContainer");
            GameObject cameraContainer = new GameObject("AttractionModeCameraContainer");
            if (mainCam != null)
            {
                cameraContainer.transform.SetParent(mainCam.transform, false);
            }

            // 4. Setup Reflective Long Road (World Space)
            GameObject roadObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadObj.name = "AttractionRoad";
            roadObj.transform.SetParent(worldContainer.transform, false);
            // Adjust position and X-scale as requested
            roadObj.transform.position = new Vector3(0f, -3.0f, 500f);
            roadObj.transform.localScale = new Vector3(20.0f, 0.1f, 1000f);
            DestroyImmediate(roadObj.GetComponent<Collider>()); // performance

            MeshRenderer roadRenderer = roadObj.GetComponent<MeshRenderer>();
            roadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            roadRenderer.receiveShadows = false;

            Material roadMat = CreateSpecularMaterial("AttractionRoadMaterial", new Color(0.12f, 0.12f, 0.15f), 0.98f); // metallic dark grey
            roadRenderer.sharedMaterial = roadMat;

            // Make road follow camera Z so it appears infinitely long
            AttractionModeRotator roadFollow = roadObj.AddComponent<AttractionModeRotator>();
            roadFollow.followCameraZ = true;
            roadFollow.followOffsetZ = 500f;

            // Headlight attached to camera container to illuminate road and platform reflections
            GameObject headlightObj = new GameObject("AttractionHeadlight");
            headlightObj.transform.SetParent(cameraContainer.transform, false);
            headlightObj.transform.localPosition = new Vector3(0f, 2f, 2f); // slightly raised headlight
            Light headlight = headlightObj.AddComponent<Light>();
            headlight.type = LightType.Point;
            headlight.color = new Color(0.85f, 0.95f, 1f); // cool white light
            headlight.intensity = 2.5f;
            headlight.range = 25f;

            // 5. Setup Rotating Neon Arcs (World Space)
            // Spawn arcs every 3 units along the track, from Z = 10 to Z = 600 (approx. 200 arcs)
            for (float z = 10f; z < 600f; z += 3f)
            {
                GameObject arcObj = new GameObject("NeonArc_" + z);
                arcObj.transform.SetParent(worldContainer.transform, false);
                arcObj.transform.position = new Vector3(0f, 0f, z);

                LineRenderer lineRenderer = arcObj.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false;
                lineRenderer.loop = false;
                lineRenderer.startWidth = 0.18f;
                lineRenderer.endWidth = 0.18f;

                int pointsCount = 40; // Double the segments to make the arcs twice as smooth/round
                lineRenderer.positionCount = pointsCount;
                Vector3[] points = new Vector3[pointsCount];
                float startAngle = Random.Range(0f, 2f * Mathf.PI);
                float arcLength = Random.Range(Mathf.PI * 0.5f, Mathf.PI * 1.5f); // 90 to 270 degrees

                for (int i = 0; i < pointsCount; i++)
                {
                    float progress = (float)i / (pointsCount - 1);
                    float angle = startAngle + progress * arcLength;
                    float x = Mathf.Sin(angle) * (tubeRadius - 0.04f);
                    float y = -Mathf.Cos(angle) * (tubeRadius - 0.04f);
                    points[i] = new Vector3(x, y, 0f);
                }
                lineRenderer.SetPositions(points);

                // Alternating Cyan and Neon Magenta/Pink colors
                Color neonColor = (Random.value < 0.5f) ? new Color(0f, 1f, 1f) : new Color(1f, 0f, 0.5f);
                lineRenderer.sharedMaterial = CreateEmissiveMaterial("ArcMat_" + z, neonColor, 5.5f, 0f);

                // Add rotator component to spin the arc
                AttractionModeRotator rotator = arcObj.AddComponent<AttractionModeRotator>();
                rotator.rotationSpeed = new Vector3(0f, 0f, Random.Range(-35f, 35f));
            }

            // 6. Setup Glowing Swirling Core Sphere Cluster (Managed by AttractionSphereMorpher)
            GameObject sphereCluster = new GameObject("AttractionCoreSphereCluster");
            sphereCluster.transform.SetParent(worldContainer.transform, false);
            // Sits directly on the road at Y = -2.0
            sphereCluster.transform.position = new Vector3(0f, -0.5f, 14f);

            // Create a template sphere to pass to the morpher
            GameObject templateSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(templateSphere.GetComponent<Collider>());
            
            MeshRenderer sphereRenderer = templateSphere.GetComponent<MeshRenderer>();
            sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sphereRenderer.receiveShadows = false;

            // Apply equipped skin to the template sphere
            int equippedSkin = GameManager.Instance != null ? GameManager.Instance.EquippedSkin : 0;
            bool isFXSkin = equippedSkin >= 6;
            Material templateMat;
            Color templateColor = new Color(1f, 0.4f, 0f); // Default Neon Orange for template

            if ((equippedSkin == 1) || isFXSkin)
            {
                templateMat = CreateEmissiveMaterial("TemplateMaterial", templateColor, 3.0f, 0.9f);
            }
            else
            {
                Texture2D skinTex = null;
                if (equippedSkin == 0) skinTex = GenerateSphereGridTexture(templateColor * 2.0f, templateColor * 0.15f);
                else if (equippedSkin == 2) skinTex = GenerateStripesTexture(templateColor * 2.0f, templateColor * 0.15f);
                else if (equippedSkin == 3) skinTex = GenerateCheckerboardTexture(templateColor * 2.0f, templateColor * 0.15f);
                else if (equippedSkin == 4) skinTex = GenerateCircuitTexture(templateColor * 2.0f, templateColor * 0.15f);
                else if (equippedSkin == 5) skinTex = GenerateDiamondTexture(templateColor * 2.0f, templateColor * 0.15f);
                else skinTex = GenerateSphereGridTexture(templateColor * 2.0f, templateColor * 0.15f); // fallback

                templateMat = CreateSphereMaterial("TemplateMaterial", templateColor, 2.5f, 0.8f, skinTex);
            }
            
            sphereRenderer.sharedMaterial = templateMat;

            // Attach FX if equipped
            if (isFXSkin)
            {
                EnergySphereEffects effects = templateSphere.AddComponent<EnergySphereEffects>();
                if (equippedSkin == 6) effects.preset = EnergySphereEffects.EffectPreset.Plasma;
                else if (equippedSkin == 7) effects.preset = EnergySphereEffects.EffectPreset.Warp;
                else if (equippedSkin == 8) effects.preset = EnergySphereEffects.EffectPreset.Gauntlet;
                else if (equippedSkin == 9) effects.preset = EnergySphereEffects.EffectPreset.City;
            }

            // Initialize Morpher
            AttractionSphereMorpher morpher = sphereCluster.AddComponent<AttractionSphereMorpher>();
            int lastSpheres = GameManager.lastSphereCount > 0 ? GameManager.lastSphereCount : 3;
            morpher.Initialize(templateSphere, lastSpheres);

            // Rotate core cluster around multiple axes and follow camera Z
            AttractionModeRotator clusterRotator = sphereCluster.AddComponent<AttractionModeRotator>();
            clusterRotator.rotationSpeed = new Vector3(15f, 30f, 10f);
            clusterRotator.followCameraZ = true;
            clusterRotator.followOffsetZ = 14f;

            // 8. Setup Subtle Dust Starfield Particles (Camera Space, scrolling and wrapping)
            for (int i = 0; i < 200; i++) // Increased to 200 particles
            {
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "StarFieldParticle";
                star.transform.SetParent(cameraContainer.transform, false);

                float angle = Random.Range(0f, 2f * Mathf.PI);
                float dist = Random.Range(6.5f, 14f); // Spawn outside the cylinder walls
                float x = Mathf.Sin(angle) * dist;
                float y = -Mathf.Cos(angle) * dist;
                float z = Random.Range(5f, 50f); // Spawn ahead of camera

                star.transform.localPosition = new Vector3(x, y, z);
                star.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                DestroyImmediate(star.GetComponent<Collider>());

                // Match star color randomly to the neon arc colors (Cyan or Neon Magenta/Pink)
                Color starColor = (Random.value < 0.5f) ? new Color(0f, 1f, 1f) : new Color(1f, 0f, 0.5f);
                Material starMat = CreateEmissiveMaterial("StarMat_" + i, starColor, 2.5f, 0f);
                star.GetComponent<MeshRenderer>().sharedMaterial = starMat;

                // Scrolling Z speed to move stars past the camera
                AttractionModeRotator starScroll = star.AddComponent<AttractionModeRotator>();
                starScroll.localZSpeed = -forwardSpeed * 0.4f; // scroll backwards
                starScroll.wrapMinZ = 3f;
                starScroll.wrapMaxZ = 50f;
            }

            // 9. Setup Tunnel Generator (World Space)
            GameObject tunnelGenObj = new GameObject("AttractionTunnelGenerator");
            TunnelGenerator tunnelGen = tunnelGenObj.AddComponent<TunnelGenerator>();
            tunnelGen.target = null;
            tunnelGen.radius = tubeRadius;
            tunnelGen.segmentLength = segmentLength;
            tunnelGen.radialSegments = radialSegments;
            tunnelGen.markerInterval = markerInterval;
            tunnelGen.tunnelMaterial = tunnelMaterial;
            tunnelGen.markerMaterial = markerMaterial;
            tunnelGen.spawnCoins = false;
            tunnelGen.spawnObstacles = false;

            // 10. Setup Ambient Lighting & Dim Directional Lights
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.06f);

            Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in allLights)
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = 0.08f;
                    light.color = new Color(0.1f, 0.1f, 0.22f);
                }
            }

            try
            {
                SetupPostProcessing(2.2f, 0.7f, 0.7f); // Subtler bloom to prevent scene wash-out
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Bloom post-processing initialization skipped or failed: " + e.Message);
            }
        }
    }
}
