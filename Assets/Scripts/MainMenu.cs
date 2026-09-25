using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace TubityWAI
{
    [ExecuteAlways]
    public class MainMenu : MonoBehaviour
    {
#if UNITY_EDITOR
        [ContextMenu("Bake UI To Scene")]
        public void BakeUIToScene()
        {
            Transform oldCanvas = transform.Find("MainMenuCanvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas.gameObject);
            Transform oldEventSystem = transform.Find("EventSystem");
            if (oldEventSystem != null) DestroyImmediate(oldEventSystem.gameObject);
            
            CreateEventSystem();
            
            if (IAPManager.Instance == null)
            {
                GameObject iapObj = new GameObject("IAPManager");
                iapObj.AddComponent<IAPManager>();
            }

            CreateMenuUI();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[MainMenu] UI Baked into Scene successfully!");
        }
#endif

        [Header("Sphere Count Selector Stage")]
        [Tooltip("How much lower the mirror pad and sphere formation sit while the " +
                 "sphere-count selector is open, vs. their attract-screen hero height.")]
        public float sphereSelectorPadDrop = 0.6f;

        [Header("Menu Styling")]
        public Color panelBackgroundColor = new Color(0.02f, 0.05f, 0.12f, 0.95f); // Darker, less purple background
        public Color borderNeonColor = new Color(0f, 1f, 1f, 0.4f); // Subtle Cyan
        public Color neonMagentaColor = new Color(0f, 0.8f, 1f, 0.4f); // Subtle Teal/Cyan instead of magenta
        public Color textGoldColor = new Color(0.8f, 0.9f, 1f); // Cool white instead of gold
        // TubityX button accents - the label rim, matching each button's neon rim
        public Color PlayAccentColor = new Color(0.30f, 0.80f, 1f);
        public Color SettingsAccentColor = new Color(0.72f, 0.45f, 1f);

        // Visual ball colors for sphere count selection
        private Color[] sphereColors = new Color[5]
        {
            new Color(1f, 0.4f, 0f),      // Neon Orange
            new Color(0f, 1f, 0f),        // Neon Green
            new Color(1f, 0f, 0.5f),      // Neon Pink
            new Color(1f, 0.9f, 0f),      // Neon Yellow
            new Color(0.5f, 0f, 1f)       // Neon Purple
        };

        private List<LevelConfig> levelConfigs = new List<LevelConfig>();
        private int selectedSphereCount = 1; // Default selection
        private int currentLevelPage = 0;   // 0 = Levels 1-6, 1 = Levels 7-12
        private Font defaultFont;

        // UI Layer Containers
        private GameObject canvasObj;
        private GameObject layer1Obj; // Sphere Selection
        private GameObject layer2Obj; // Level Selection

        // Text indicator in Layer 2
        private TubityXLabel sphereIndicatorText;
        private GameObject settingsPopupObj;

        // How To Play: the first PLAY press runs the in-tube tutorial level, and the
        // top-level HOW TO PLAY button replays it any time. The flag lives in PlayerPrefs.
        public const string HowToPlaySeenKey = "HowToPlaySeen";

        // Sphere-count block lock badge, shown in the Layer 1.5 selector when
        // selectedSphereCount is beyond what the player has unlocked so far.
        private GameObject confirmSphereBtnObj;
        private GameObject sphereLockIconObj;
        private TubityXLabel sphereLockHintText;
        private Button removeAdsBtn;
        private TubityXLabel removeAdsText;
        private Button sfxToggleBtn;
        private TubityXLabel sfxToggleText;
        private Button musicToggleBtn;
        private TubityXLabel musicToggleText;
        private List<GameObject> levelButtons = new List<GameObject>();
        private List<GameObject> levelLockIcons = new List<GameObject>();

        // Test Levels & Top Menu Bar Fields
        private List<LevelConfig> testLevelConfigs = new List<LevelConfig>();
        private GameObject layer3Obj;
        private TubityXLabel testLevelIndicatorText;
        private List<GameObject> testLevelButtons = new List<GameObject>();
        private int currentTestLevelPage = 0;
        private const int TestLevelsPerPage = 12;

        // Shop Fields
        private GameObject layer4Obj;

        // Progression Test 1 + endless modes. Owns its own layers on this canvas; see ProgressionMenu.
        private ProgressionMenu progressionMenu;
        private TubityXLabel shopTotalCoinsText;
        private List<GameObject> shopButtons = new List<GameObject>();
        private int currentShopPage = 0;
        private Button buyCoinsBtn;
        private TubityXLabel buyCoinsText;
        
        // 3D Sphere Selector (Layer 1.5)
        private GameObject layer15Obj;

        /// <summary>One shop card: a live preview window plus its three lines of text.</summary>
        private class ShopCard
        {
            public GameObject Root;
            public Button Button;
            public RawImage Preview;
            public TubityXLabel Name;
            public TubityXLabel Blurb;
            public TubityXLabel Sub;
        }

        private readonly List<ShopCard> shopCards = new List<ShopCard>();
        private SkinShopPreview skinPreview;
        private const int ShopCardsPerPage = SkinShopPreview.Slots;

        /// <summary>
        /// The colour every preview sphere wears: the first gameplay slot, so a
        /// card shows exactly what a one-sphere run looks like in that skin.
        /// </summary>
        private Color ShopPreviewColour
        {
            get
            {
                GameSetup setup = GameSetup.Instance != null ? GameSetup.Instance : FindFirstObjectByType<GameSetup>();
                if (setup != null && setup.sphereColors != null && setup.sphereColors.Length > 0) return setup.sphereColors[0];
                return new Color(1f, 0.4f, 0f);
            }
        }

        // TubityX title logo (see Assets/Shaders/TubityXLogo.shader)
        private const float LogoWidth = 1000f;
        private const string LogoSpritePath = "UI/Tex_TubityXLogo";
        private const string LogoMaterialPath = "UI/Mat_TubityXLogo";

        private GameObject logoObj;
        private GameObject topMenuObj;
        private TubityXPanel campaignTab;
        private TubityXPanel testTab;

        // On iOS the Campaign/Test Levels tabs stay hidden until a three-finger
        // double-tap reveals them, so a stray tester tap can't surface them in front
        // of a reviewer. The Editor always shows them so the normal workflow is untouched.
#if UNITY_IOS && !UNITY_EDITOR
        private bool testMenuButtonsUnlocked = false;
#else
        private bool testMenuButtonsUnlocked = true;
#endif

#if UNITY_IOS && !UNITY_EDITOR
        private float threeFingerHoldStartTime = 0f;
        private int threeFingerTapStage = 0; // 0 = waiting for 1st tap, 1 = waiting for 2nd tap
        private float threeFingerTapDeadline = 0f;
        private const float ThreeFingerTapMaxHoldTime = 0.35f; // fingers must lift quickly - a hold/drag cancels the gesture
        private const float ThreeFingerTapGapWindow = 0.6f; // max time between the two taps
#endif

        private bool isMenuHidden = false;
        private AudioSource menuAudioSource;

        /// <summary>Advancing deeper into the menu flow: opening a submenu, confirming, launching a level.</summary>
        private void PlayMenuForward()
        {
            if (menuAudioSource != null) menuAudioSource.PlayOneShot(ProceduralAudio.GetMenuForwardSound());
        }

        /// <summary>Retreating to the previous layer via a screen's own Back button.</summary>
        private void PlayMenuBack()
        {
            if (menuAudioSource != null) menuAudioSource.PlayOneShot(ProceduralAudio.GetMenuBackSound());
        }

        /// <summary>Dismissing an overlay entirely, distinct from stepping back a layer.</summary>
        private void PlayMenuClose()
        {
            if (menuAudioSource != null) menuAudioSource.PlayOneShot(ProceduralAudio.GetMenuCloseSound());
        }

        /// <summary>Choosing or toggling a value in place: tabs, paging, list items.</summary>
        private void PlayMenuSelect()
        {
            if (menuAudioSource != null) menuAudioSource.PlayOneShot(ProceduralAudio.GetMenuSelectSound());
        }

        public void Hide()
        {
            isMenuHidden = true;
            if (canvasObj != null)
            {
                canvasObj.SetActive(false);
            }
        }

        public void Show()
        {
            isMenuHidden = false;
            if (canvasObj != null)
            {
                canvasObj.SetActive(true);
                ShowLayer1();
            }
        }

        private void Awake()
        {
            InitializeLevelConfigurations();
            InitializeTestLevelConfigurations();
        }

#if UNITY_IOS && !UNITY_EDITOR
        private void Update()
        {
            if (!testMenuButtonsUnlocked)
            {
                DetectThreeFingerDoubleTapToUnlockTestMenu();
            }
        }

        /// <summary>
        /// Watches for two quick three-finger taps in a row. A "tap" is all three
        /// fingers landing together and lifting again within ThreeFingerTapMaxHoldTime;
        /// anything slower, or a finger count that drifts off 3 mid-hold, cancels it.
        /// </summary>
        private void DetectThreeFingerDoubleTapToUnlockTestMenu()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return;

            int pressedCount = 0;
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                if (touchscreen.touches[i].press.isPressed) pressedCount++;
            }

            if (pressedCount == 3 && threeFingerHoldStartTime == 0f)
            {
                threeFingerHoldStartTime = Time.unscaledTime;
            }
            else if (pressedCount == 0 && threeFingerHoldStartTime > 0f)
            {
                float heldFor = Time.unscaledTime - threeFingerHoldStartTime;
                threeFingerHoldStartTime = 0f;

                if (heldFor <= ThreeFingerTapMaxHoldTime)
                {
                    RegisterThreeFingerTap();
                }
                else
                {
                    threeFingerTapStage = 0;
                }
            }
            else if (pressedCount != 3 && pressedCount != 0 && threeFingerHoldStartTime > 0f)
            {
                threeFingerHoldStartTime = 0f;
                threeFingerTapStage = 0;
            }

            if (threeFingerTapStage == 1 && Time.unscaledTime > threeFingerTapDeadline)
            {
                threeFingerTapStage = 0;
            }
        }

        private void RegisterThreeFingerTap()
        {
            if (threeFingerTapStage == 0)
            {
                threeFingerTapStage = 1;
                threeFingerTapDeadline = Time.unscaledTime + ThreeFingerTapGapWindow;
            }
            else
            {
                threeFingerTapStage = 0;
                UnlockTestMenuButtons();
            }
        }

        private void UnlockTestMenuButtons()
        {
            testMenuButtonsUnlocked = true;
            if (campaignTab != null) campaignTab.gameObject.SetActive(true);
            if (testTab != null) testTab.gameObject.SetActive(true);
        }
#endif

                private void Start()
        {
            // Clean up old canvas/event system to prevent duplication in edit mode
            Transform existingCanvas = transform.Find("MainMenuCanvas");
            if (existingCanvas != null && Application.isPlaying)
            {
                // UI is baked into the scene! Rebind references instead of generating.
                BindBakedUI(existingCanvas);
            }
            else
            {
                if (existingCanvas != null) DestroyImmediate(existingCanvas.gameObject);
                Transform oldEventSystem = transform.Find("EventSystem");
                if (oldEventSystem != null) DestroyImmediate(oldEventSystem.gameObject);

                CreateEventSystem();

                if (IAPManager.Instance == null)
                {
                    GameObject iapObj = new GameObject("IAPManager");
                    iapObj.AddComponent<IAPManager>();
                }

                CreateMenuUI();
            }

            bool isReplaying = GameManager.shouldReplayOnLoad && GameManager.lastLevelConfig != null;
            if (isReplaying || isMenuHidden)
            {
                Hide();
            }
            else
            {
                ShowLayer1();
            }

            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnPurchaseComplete += OnIAPPurchaseComplete;
                IAPManager.Instance.OnRestoreComplete += UpdateSettingsUI;
                IAPManager.Instance.OnProductsReady += OnIAPProductsReady;
            }
        }

        private void OnIAPPurchaseComplete()
        {
            UpdateSettingsUI();
            // A coin pack changes the balance shown in the shop and which skins are affordable.
            if (layer4Obj != null && layer4Obj.activeSelf) RefreshShopGrid();
        }

        private void OnIAPProductsReady()
        {
            UpdateSettingsUI();
            UpdateBuyCoinsLabel();
        }

        private void UpdateBuyCoinsLabel()
        {
            if (buyCoinsText == null) return;
            string price = IAPManager.Instance != null
                ? IAPManager.Instance.GetLocalizedPrice(IAPManager.ProductCoins100, IAPManager.Coins100FallbackPrice)
                : IAPManager.Coins100FallbackPrice;
            buyCoinsText.Text = $"+{IAPManager.Coins100Amount} COINS - {price}";
        }

        private void BindBakedUI(Transform canvasTrans)
        {
            canvasObj = canvasTrans.gameObject;
            
            // Just call CreateMenuUI for now since we rely on local variables heavily. 
            // In a full refactor we would find all objects by name. 
            // But actually, we can't easily find them all without full script rewrite.
            // Let's just generate the UI on top and destroy the old one for now if they want it baked for viewing only.
            
            // Wait, we'll implement a clean find block here for the critical components:
            layer1Obj = canvasTrans.Find("Layer1_TopMenu")?.gameObject;
            layer15Obj = canvasTrans.Find("Layer15_SphereSelector")?.gameObject;
            layer2Obj = canvasTrans.Find("Layer2_LevelSelection")?.gameObject;
            layer3Obj = canvasTrans.Find("Layer3_TestLevels")?.gameObject;
            layer4Obj = canvasTrans.Find("Layer4_ShopMenu")?.gameObject;
            settingsPopupObj = canvasTrans.Find("SettingsPopup")?.gameObject;
            logoObj = canvasTrans.Find("MenuLogo")?.gameObject;
            topMenuObj = canvasTrans.Find("TopMenuHeader")?.gameObject;
            
            // For now, to ensure logic holds, we just destroy and recreate so the event bindings work!
            DestroyImmediate(canvasTrans.gameObject);
            CreateMenuUI();
        }

        private void InitializeLevelConfigurations()
        {
            // Campaign levels are generated from a difficulty curve; see LevelProgression for the tier table.
            levelConfigs.AddRange(LevelProgression.CreateCampaign());
        }

        private void InitializeTestLevelConfigurations()
        {
            testLevelConfigs.Add(new LevelConfig(101, 15f, 0.30f, 2.0f, true, "PLASMA SPHERE", "Plasma Core"));
            testLevelConfigs.Add(new LevelConfig(102, 35f, 0.10f, 2.5f, true, "HYPER WARP", "Speed Test"));
            testLevelConfigs.Add(new LevelConfig(103, 8f, 0.75f, 1.5f, true, "HAZARD GAUNTLET", "Density Test"));
            testLevelConfigs.Add(new LevelConfig(104, 12f, 0.45f, 1.8f, true, "ISO VIEW TEST", "Isometric View", true, 0.5f, 1.2f, 8.0f, -3.5f));
            testLevelConfigs.Add(new LevelConfig(105, 12f, 0.35f, 1.8f, true, "ISO CURVES TEST", "Curved & Angled", true, 0.5f, 1.2f, 8.0f, -3.5f, true, 0.04f, 3.0f));
            testLevelConfigs.Add(new LevelConfig(106, 14f, 0.35f, 1.8f, true, "CITY FLYBY", "Abstract City", true, 0.5f, 1.2f, 8.0f, -3.5f, true, 0.035f, 4.0f, true, true));
            testLevelConfigs.Add(new LevelConfig(107, 12f, 0.4f, 1.8f, true, "ADD SPHERE TEST", "Partial Death", false, 0f, 0f, 0f, 0f, false, 0.05f, 2.0f, false, false, true, true));

            // Themed environment sets (see EnvironmentTheme). Each set ramps I -> III:
            // I straight and gentle, II winding, III fast, dense and on the angled camera.
            AddThemedSet(201, EnvironmentTheme.Space,      "SPACE DRIFT", "Deep Space");
            AddThemedSet(211, EnvironmentTheme.Jungle,     "CANOPY RUN",  "Jungle Canopy");
            AddThemedSet(221, EnvironmentTheme.Underwater, "REEF DIVE",   "Abyssal Reef");
            AddThemedSet(231, EnvironmentTheme.Volcano,    "MAGMA CORE",  "Volcanic");
            AddThemedSet(241, EnvironmentTheme.Crystal,    "AURORA ICE",  "Crystal Cavern");
            AddThemedSet(261, EnvironmentTheme.Grid,       "GRID RUNNER", "Cyber Grid");

            // Menu-style neon: rings and arcs wrapped in halo ribbons under heavier bloom.
            AddNeonBloomSet(251, "NEON BLOOM", "Hot Neon");

            // Environment blending (see EnvironmentBlend): the world cross-fades as the level runs.
            AddBlendLevels(271);

            // The full solar traverse: Uranus in to Earth, finishing alongside the Moon.
            AddSolarSystemLevel(281);

            // Obstacle-free endless run with live environment / music controls (see SandboxPanel).
            AddSandboxLevel(291);
        }

        /// <summary>
        /// An endless, arc-free run for auditioning environment themes and comparing a straight
        /// music track against the synchronised stem loops. It starts on the classic tube with a
        /// one-stop blend, which is what lets SandboxPanel append further themes while flying.
        /// </summary>
        private void AddSandboxLevel(int number)
        {
            testLevelConfigs.Add(new LevelConfig(number, 14f, 0f, 2.0f, isTest: true,
                                                 name: "AUDIO / ENV LAB", theme: "Sandbox",
                                                 transparentTube: true)
                                 .WithEnvironmentBlend(EnvironmentBlend.From(EnvironmentTheme.None))
                                 .WithSandbox());
        }

        /// <summary>
        /// Levels that cross-fade between environments instead of picking one. The first is the reference
        /// case - the classic solid tunnel opening out into the jungle canopy - and the second chains four
        /// stops to show that a blend takes any number of themes in any order.
        /// </summary>
        private void AddBlendLevels(int firstNumber)
        {
            LevelConfig opening = LevelProgression.CreateCampaignLevel(1);

            // Solid tube for the first 90 units - long enough to read as the classic tunnel - then the
            // canopy opens up between 90 and 260. At 13 units/sec that is about 7 seconds in the tube
            // and 13 seconds of transition.
            testLevelConfigs.Add(new LevelConfig(firstNumber, 13f, 0.25f, 1.9f, isTest: true,
                                                 name: "TUBE TO CANOPY", theme: "Blend Test")
                                 .WithRings(opening.rings)
                                 .WithEnvironmentBlend(EnvironmentBlend.From(EnvironmentTheme.None)
                                                                       .To(EnvironmentTheme.Jungle, 260f, blendLength: 170f)));

            // A continuous morph through four worlds, each cross-fade spanning the whole gap.
            testLevelConfigs.Add(new LevelConfig(firstNumber + 1, 16f, 0.35f, 2.0f, isTest: true,
                                                 name: "GRAND TOUR", theme: "Blend Test",
                                                 curves: true, curveFreq: 0.04f, curveAmp: 3.0f)
                                 .WithEnvironmentBlend(EnvironmentBlend.From(EnvironmentTheme.None)
                                                                       .To(EnvironmentTheme.Space, 240f, blendLength: 150f)
                                                                       .To(EnvironmentTheme.Crystal, 540f)
                                                                       .To(EnvironmentTheme.Volcano, 840f)));
        }

        /// <summary>
        /// The solar system traverse (see SolarSystemRoute): a four-minute run inward from Uranus
        /// past Saturn and Jupiter, through the asteroid belt and Mars, finishing at the Moon with
        /// Earth ahead. The route brings its own environment blend, so all this sets is the pacing.
        ///
        /// Obstacle density is deliberately low and the curves are gentle: the level's content is
        /// what is out of the window, and a tube that swings hard would keep throwing the planets
        /// out of frame.
        /// </summary>
        private void AddSolarSystemLevel(int number)
        {
            LevelConfig refLevel = LevelProgression.CreateCampaignLevel(10);

            testLevelConfigs.Add(new LevelConfig(number, 18f, 0.20f, 2.0f, isTest: true,
                                                 name: "SOLAR TRAVERSE", theme: "Solar System",
                                                 curves: true, curveFreq: 0.012f, curveAmp: 2.5f,
                                                 transparentTube: true)
                                 .WithRings(refLevel.rings)
                                 .WithCelestialRoute(SolarSystemRoute.Build())
                                 .WithLength(SolarSystemRoute.FinishDistance));
        }

        /// <summary>
        /// Three levels that borrow the attract screen's look (halo ribbons + hot bloom) and the
        /// campaign's ring difficulty at levels 6, 12 and 18 so each step adds a new ring mechanic.
        /// </summary>
        private void AddNeonBloomSet(int firstNumber, string name, string themeLabel)
        {
            int[] campaignRef = { 6, 12, 18 };
            float[] speeds = { 14f, 18f, 24f };
            float[] probs = { 0.30f, 0.40f, 0.50f };
            string[] suffix = { " I", " II", " III" };
            for (int i = 0; i < 3; i++)
            {
                LevelConfig refLevel = LevelProgression.CreateCampaignLevel(campaignRef[i]);
                testLevelConfigs.Add(new LevelConfig(firstNumber + i, speeds[i], probs[i], 2.0f, isTest: true,
                                                     name: name + suffix[i], theme: themeLabel,
                                                     curves: refLevel.hasCurves, curveFreq: refLevel.curveFrequency, curveAmp: refLevel.curveAmplitude)
                                     .WithRings(refLevel.rings)
                                     .WithNeonBloom());
            }
        }

        private void AddThemedSet(int firstNumber, EnvironmentTheme theme, string name, string themeLabel)
        {
            testLevelConfigs.Add(new LevelConfig(firstNumber, 13f, 0.25f, 1.9f, isTest: true, name: name + " I", theme: themeLabel,
                                                 transparentTube: true).WithEnvironment(theme));
            testLevelConfigs.Add(new LevelConfig(firstNumber + 1, 16f, 0.35f, 2.0f, isTest: true, name: name + " II", theme: themeLabel,
                                                 curves: true, curveFreq: 0.04f, curveAmp: 3.0f,
                                                 transparentTube: true).WithEnvironment(theme));
            testLevelConfigs.Add(new LevelConfig(firstNumber + 2, 20f, 0.45f, 2.2f, isTest: true, name: name + " III", theme: themeLabel,
                                                 customCam: true, camOffsetX: 0.5f, camOffsetY: 1.2f, camRotX: 8.0f, camRotY: -3.5f,
                                                 curves: true, curveFreq: 0.035f, curveAmp: 4.0f,
                                                 transparentTube: true).WithEnvironment(theme));
        }

        private void CreateEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.transform.SetParent(this.transform);
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private void CreateMenuUI()
        {
            // 1. Create Canvas
            canvasObj = new GameObject("MainMenuCanvas");
            canvasObj.transform.SetParent(this.transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // TubityXPanel / TubityXLabel pass their geometry and accent colour
            // through TEXCOORD1, which canvases do not send by default.
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            menuAudioSource = canvasObj.AddComponent<AudioSource>();
            menuAudioSource.playOnAwake = false;
            menuAudioSource.spatialBlend = 0f;
            menuAudioSource.volume = 0.6f;
            menuAudioSource.mute = !GameManager.SfxEnabled;

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // ---------------------------------------------------------------
            // Global Title Logo (Top Center)
            //
            // One Image, one packed SDF sprite, one material. The neon rim,
            // chromatic ghost rim, inline contour, frosted glass body, framing
            // lines and the animated sheen are all generated inside
            // UI/TubityXLogo.shader, so the whole title is a single draw call
            // with no per-frame CPU work and no Mask/stencil passes.
            // Tune the look on Resources/UI/Mat_TubityXLogo.
            // ---------------------------------------------------------------
            logoObj = new GameObject("MenuLogo");
            logoObj.transform.SetParent(canvasObj.transform, false);
            RectTransform logoRect = logoObj.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.86f);
            logoRect.anchorMax = new Vector2(0.5f, 0.86f);
            // the sprite is 1024 x 256 - keep that ratio or the SDF bands skew
            logoRect.sizeDelta = new Vector2(LogoWidth, LogoWidth * 0.25f);
            logoRect.anchoredPosition = Vector2.zero;

            Image logoImg = logoObj.AddComponent<Image>();
            logoImg.sprite = Resources.Load<Sprite>(LogoSpritePath);
            logoImg.material = Resources.Load<Material>(LogoMaterialPath);
            logoImg.type = Image.Type.Simple;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            if (logoImg.sprite == null || logoImg.material == null)
            {
                Debug.LogWarning("[MainMenu] TubityX logo assets not found. Expected " +
                                 "Assets/Resources/" + LogoSpritePath + ".png and " +
                                 "Assets/Resources/" + LogoMaterialPath + ".mat");
            }

            // Create Settings Popup
            CreateSettingsPopup();

            // ==========================================
            // LAYER 1: TOP LEVEL MENU
            // ==========================================
            layer1Obj = new GameObject("Layer1_TopMenu");
            layer1Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l1Rect = layer1Obj.AddComponent<RectTransform>();
            l1Rect.anchorMin = Vector2.zero;
            l1Rect.anchorMax = Vector2.one;
            l1Rect.sizeDelta = Vector2.zero;

            // 1. PLAY BUTTON (Bottom Left) - blue, with a pulsing border
            GameObject playBtnObj = TubityXUIFactory.CreateButton(
                layer1Obj.transform, new Vector2(260f, 76f), "PLAY",
                TubityXUIFactory.BlueButtonMaterial, PlayAccentColor, 30f, 5f);
            playBtnObj.name = "NavBtn_Play";
            RectTransform playRect = playBtnObj.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.05f, 0.08f);
            playRect.anchorMax = new Vector2(0.05f, 0.08f);
            playRect.pivot = new Vector2(0f, 0f);
            playRect.anchoredPosition = Vector2.zero;
            playBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuForward(); OnPlayPressed(); });

            // 1b. HOW TO PLAY BUTTON (Bottom Centre) - cyan, steady border
            GameObject howToBtnObj = TubityXUIFactory.CreateButton(
                layer1Obj.transform, new Vector2(330f, 76f), "? HOW TO PLAY",
                TubityXUIFactory.Cyan, 24f, 4f, 16f, false);
            howToBtnObj.name = "NavBtn_HowToPlay";
            RectTransform howToRect = howToBtnObj.GetComponent<RectTransform>();
            howToRect.anchorMin = new Vector2(0.5f, 0.08f);
            howToRect.anchorMax = new Vector2(0.5f, 0.08f);
            howToRect.pivot = new Vector2(0.5f, 0f);
            howToRect.anchoredPosition = Vector2.zero;
            howToBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuForward(); LaunchFTUELevel(); });

            // 2. SETTINGS BUTTON (Bottom Right) - purple, steady border
            GameObject settingsBtnObj = TubityXUIFactory.CreateButton(
                layer1Obj.transform, new Vector2(300f, 76f), "SETTINGS",
                TubityXUIFactory.PurpleButtonMaterial, SettingsAccentColor, 24f, 4f);
            settingsBtnObj.name = "NavBtn_Settings";
            RectTransform settingsRect = settingsBtnObj.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.95f, 0.08f);
            settingsRect.anchorMax = new Vector2(0.95f, 0.08f);
            settingsRect.pivot = new Vector2(1f, 0f);
            settingsRect.anchoredPosition = Vector2.zero;
            settingsBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuForward(); OpenSettingsPopup(); });

            // 3. PROGRESSION TEST 1 + ENDLESS LEVELS - a second row above the main three.
            GameObject progressionBtnObj = TubityXUIFactory.CreateButton(
                layer1Obj.transform, new Vector2(380f, 68f), "PROGRESSION TEST 1",
                TubityXUIFactory.Cyan, 21f, 4f, 16f, false);
            progressionBtnObj.name = "NavBtn_ProgressionTest1";
            RectTransform progressionRect = progressionBtnObj.GetComponent<RectTransform>();
            progressionRect.anchorMin = new Vector2(0.05f, 0.20f);
            progressionRect.anchorMax = new Vector2(0.05f, 0.20f);
            progressionRect.pivot = new Vector2(0f, 0f);
            progressionRect.anchoredPosition = Vector2.zero;
            progressionBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuForward(); ShowProgressionMenu(); });

            GameObject endlessBtnObj = TubityXUIFactory.CreateButton(
                layer1Obj.transform, new Vector2(330f, 68f), "ENDLESS LEVELS",
                TubityXUIFactory.PurpleButtonMaterial, SettingsAccentColor, 21f, 4f);
            endlessBtnObj.name = "NavBtn_EndlessLevels";
            RectTransform endlessRect = endlessBtnObj.GetComponent<RectTransform>();
            endlessRect.anchorMin = new Vector2(0.95f, 0.20f);
            endlessRect.anchorMax = new Vector2(0.95f, 0.20f);
            endlessRect.pivot = new Vector2(1f, 0f);
            endlessRect.anchoredPosition = Vector2.zero;
            endlessBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuForward(); ShowEndlessMenu(); });

            // ==========================================
            // LAYER 1.5: 3D SPHERE SELECTOR OVERLAY
            // ==========================================
            layer15Obj = new GameObject("Layer15_SphereSelector");
            layer15Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l15Rect = layer15Obj.AddComponent<RectTransform>();
            l15Rect.anchorMin = Vector2.zero;
            l15Rect.anchorMax = Vector2.one;
            l15Rect.sizeDelta = Vector2.zero;

            // Left Arrow
            GameObject prevSphereBtn = GlassUIFactory.CreateGlassmorphicIconButton(layer15Obj.transform, new Vector2(80f, 80f), borderNeonColor, "\u25C0", Color.white, 24);
            prevSphereBtn.name = "PrevSphereButton";
            RectTransform pSphereRect = prevSphereBtn.GetComponent<RectTransform>();
            pSphereRect.anchorMin = new Vector2(0.3f, 0.5f);
            pSphereRect.anchorMax = new Vector2(0.3f, 0.5f);
            pSphereRect.pivot = new Vector2(0.5f, 0.5f);
            prevSphereBtn.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangeSphereCount(-1); });

            // Right Arrow
            GameObject nextSphereBtn = GlassUIFactory.CreateGlassmorphicIconButton(layer15Obj.transform, new Vector2(80f, 80f), borderNeonColor, "\u25B6", Color.white, 24);
            nextSphereBtn.name = "NextSphereButton";
            RectTransform nSphereRect = nextSphereBtn.GetComponent<RectTransform>();
            nSphereRect.anchorMin = new Vector2(0.7f, 0.5f);
            nSphereRect.anchorMax = new Vector2(0.7f, 0.5f);
            nSphereRect.pivot = new Vector2(0.5f, 0.5f);
            nextSphereBtn.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangeSphereCount(1); });

            // Lock badge - centred over the sphere formation (same height as the
            // arrows) when the browsed sphere count is beyond what the player has
            // unlocked yet.
            sphereLockIconObj = new GameObject("SphereLockIcon", typeof(RectTransform));
            sphereLockIconObj.transform.SetParent(layer15Obj.transform, false);
            RectTransform lockIconRect = sphereLockIconObj.GetComponent<RectTransform>();
            lockIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockIconRect.pivot = new Vector2(0.5f, 0.5f);
            lockIconRect.sizeDelta = new Vector2(220f, 220f);

            // White outline: the same silhouette, a little larger and behind the
            // tinted icon, so it reads as a border rather than a second glyph.
            GameObject lockBorderObj = new GameObject("LockBorder", typeof(RectTransform));
            lockBorderObj.transform.SetParent(sphereLockIconObj.transform, false);
            RectTransform lockBorderRect = lockBorderObj.GetComponent<RectTransform>();
            lockBorderRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockBorderRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockBorderRect.pivot = new Vector2(0.5f, 0.5f);
            lockBorderRect.sizeDelta = lockIconRect.sizeDelta + new Vector2(20f, 20f);
            Image lockBorderIcon = lockBorderObj.AddComponent<Image>();
            lockBorderIcon.sprite = GlassUIFactory.GetLockSprite(false);
            lockBorderIcon.color = Color.white;
            lockBorderIcon.raycastTarget = false;

            GameObject lockFillObj = new GameObject("LockFill", typeof(RectTransform));
            lockFillObj.transform.SetParent(sphereLockIconObj.transform, false);
            RectTransform lockFillRect = lockFillObj.GetComponent<RectTransform>();
            lockFillRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockFillRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockFillRect.pivot = new Vector2(0.5f, 0.5f);
            lockFillRect.sizeDelta = lockIconRect.sizeDelta;
            Image sphereLockIcon = lockFillObj.AddComponent<Image>();
            sphereLockIcon.sprite = GlassUIFactory.GetLockSprite(false);
            sphereLockIcon.color = TubityXUIFactory.Blue;
            sphereLockIcon.raycastTarget = false;

            GameObject sphereLockHintObj = new GameObject("SphereLockHintText", typeof(RectTransform));
            sphereLockHintObj.transform.SetParent(layer15Obj.transform, false);
            RectTransform lockHintRect = sphereLockHintObj.GetComponent<RectTransform>();
            lockHintRect.anchorMin = new Vector2(0.5f, 0.27f);
            lockHintRect.anchorMax = new Vector2(0.5f, 0.27f);
            lockHintRect.pivot = new Vector2(0.5f, 0.5f);
            lockHintRect.sizeDelta = new Vector2(560f, 40f);
            sphereLockHintText = TubityXUIFactory.AddLabel(sphereLockHintObj, "", 15f, Color.white, TubityXUIFactory.Blue, 2f);
            sphereLockIconObj.SetActive(false);
            sphereLockHintObj.SetActive(false);

            // Confirm Button (Bottom Center)
            confirmSphereBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(layer15Obj.transform, new Vector2(280f, 70f), neonMagentaColor, "C O N F I R M", Color.white, 28, true);
            confirmSphereBtnObj.name = "ConfirmSphereButton";
            RectTransform confirmRect = confirmSphereBtnObj.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0.15f);
            confirmRect.anchorMax = new Vector2(0.5f, 0.15f);
            confirmRect.pivot = new Vector2(0.5f, 0.5f);
            confirmSphereBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuForward(); ConfirmSphereCount(); });

            // Back Button (Top Left - standardized location across all menu screens)
            GameObject backSelBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(layer15Obj.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            backSelBtnObj.name = "BackSelectionButton";
            RectTransform backSelRect = backSelBtnObj.GetComponent<RectTransform>();
            backSelRect.anchorMin = new Vector2(0.06f, 0.90f);
            backSelRect.anchorMax = new Vector2(0.06f, 0.90f);
            backSelRect.pivot = new Vector2(0f, 1f);
            backSelBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuBack(); ShowLayer1(); });

            layer15Obj.SetActive(false);

            // ==========================================
            // LAYER 2: LEVEL SELECTION LAYER
            // ==========================================
            layer2Obj = new GameObject("Layer2_LevelSelection");
            layer2Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l2Rect = layer2Obj.AddComponent<RectTransform>();
            l2Rect.anchorMin = Vector2.zero;
            l2Rect.anchorMax = Vector2.one;
            l2Rect.sizeDelta = Vector2.zero;

            GameObject l2Panel = GlassUIFactory.CreateGlassmorphicPanel(layer2Obj.transform, new Vector2(1340f, 720f), borderNeonColor, new Vector2(0f, 30f));
            l2Panel.name = "L2_Panel";

            // Title indicator text
            GameObject indObj = new GameObject("L2_Indicator");
            indObj.transform.SetParent(l2Panel.transform, false);
            RectTransform indRect = indObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0.85f);
            indRect.anchorMax = new Vector2(1f, 0.98f);
            indRect.sizeDelta = Vector2.zero;

            sphereIndicatorText = TubityXUIFactory.AddLabel(indObj, "SELECT LEVEL", 19f, textGoldColor, textGoldColor);

            // Level Buttons Grid
            GameObject l2GridObj = new GameObject("L2_Grid");
            l2GridObj.transform.SetParent(l2Panel.transform, false);
            RectTransform l2GridRect = l2GridObj.AddComponent<RectTransform>();
            l2GridRect.anchorMin = new Vector2(0.11f, 0.10f);
            l2GridRect.anchorMax = new Vector2(0.89f, 0.80f);
            l2GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l2Grid = l2GridObj.AddComponent<GridLayoutGroup>();
            l2Grid.cellSize = new Vector2(280f, 200f);
            l2Grid.spacing = new Vector2(60f, 48f);
            l2Grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 6; i++)
            {
                Color bCol = (i % 2 == 0) ? borderNeonColor : neonMagentaColor;
                GameObject lvlBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2GridObj.transform, new Vector2(280f, 200f), bCol, "", Color.clear);
                lvlBtnObj.name = "LevelButton_" + i;

                // Screenshot of the level's opening (LevelThumbnails), in a well across the top.
                GameObject thumbObj = new GameObject("Thumb", typeof(RectTransform));
                thumbObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform thumbRect = thumbObj.GetComponent<RectTransform>();
                thumbRect.anchorMin = new Vector2(0f, 0.40f);
                thumbRect.anchorMax = new Vector2(1f, 1f);
                thumbRect.offsetMin = new Vector2(12f, 0f);
                thumbRect.offsetMax = new Vector2(-12f, -12f);
                thumbObj.AddComponent<RawImage>().raycastTarget = false;

                // Level Number
                GameObject numObj = new GameObject("LevelNumText");
                numObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform numRect = numObj.AddComponent<RectTransform>();
                numRect.anchorMin = new Vector2(0f, 0.19f);
                numRect.anchorMax = new Vector2(1f, 0.39f);
                numRect.sizeDelta = Vector2.zero;

                TubityXLabel numText = TubityXUIFactory.AddLabel(numObj, "", 18f, textGoldColor, textGoldColor);

                // Subtitle
                GameObject subObj = new GameObject("LevelSubText");
                subObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0f, 0.04f);
                subRect.anchorMax = new Vector2(1f, 0.20f);
                subRect.sizeDelta = Vector2.zero;

                TubityXLabel subText = TubityXUIFactory.AddLabel(subObj, "", 10.9f, borderNeonColor, borderNeonColor);

                // Lock icon overlay - shown centered over the button when this level is locked.
                GameObject lvlLockIconObj = new GameObject("LevelLockIcon", typeof(RectTransform));
                lvlLockIconObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform lvlLockIconRect = lvlLockIconObj.GetComponent<RectTransform>();
                // On the picture.
                lvlLockIconRect.anchorMin = new Vector2(0.5f, 0.70f);
                lvlLockIconRect.anchorMax = new Vector2(0.5f, 0.70f);
                lvlLockIconRect.pivot = new Vector2(0.5f, 0.5f);
                lvlLockIconRect.sizeDelta = new Vector2(46f, 46f);

                // White outline behind the tinted fill, so it reads as a border.
                GameObject lvlLockBorderObj = new GameObject("LockBorder", typeof(RectTransform));
                lvlLockBorderObj.transform.SetParent(lvlLockIconObj.transform, false);
                RectTransform lvlLockBorderRect = lvlLockBorderObj.GetComponent<RectTransform>();
                lvlLockBorderRect.anchorMin = new Vector2(0.5f, 0.5f);
                lvlLockBorderRect.anchorMax = new Vector2(0.5f, 0.5f);
                lvlLockBorderRect.pivot = new Vector2(0.5f, 0.5f);
                lvlLockBorderRect.sizeDelta = lvlLockIconRect.sizeDelta + new Vector2(10f, 10f);
                Image lvlLockBorderImg = lvlLockBorderObj.AddComponent<Image>();
                lvlLockBorderImg.sprite = GlassUIFactory.GetLockSprite(false);
                lvlLockBorderImg.color = Color.white;
                lvlLockBorderImg.raycastTarget = false;

                GameObject lvlLockFillObj = new GameObject("LockFill", typeof(RectTransform));
                lvlLockFillObj.transform.SetParent(lvlLockIconObj.transform, false);
                RectTransform lvlLockFillRect = lvlLockFillObj.GetComponent<RectTransform>();
                lvlLockFillRect.anchorMin = new Vector2(0.5f, 0.5f);
                lvlLockFillRect.anchorMax = new Vector2(0.5f, 0.5f);
                lvlLockFillRect.pivot = new Vector2(0.5f, 0.5f);
                lvlLockFillRect.sizeDelta = lvlLockIconRect.sizeDelta;
                Image lvlLockFillImg = lvlLockFillObj.AddComponent<Image>();
                lvlLockFillImg.sprite = GlassUIFactory.GetLockSprite(false);
                lvlLockFillImg.color = TubityXUIFactory.Blue;
                lvlLockFillImg.raycastTarget = false;

                lvlLockIconObj.SetActive(false);
                levelLockIcons.Add(lvlLockIconObj);

                levelButtons.Add(lvlBtnObj);
            }

            // Back Button (Top Left - standardized location across all menu screens)
            GameObject backBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            backBtnObj.name = "BackButton";
            RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.06f, 0.90f);
            backRect.anchorMax = new Vector2(0.06f, 0.90f);
            backRect.pivot = new Vector2(0f, 1f);
            backBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuBack(); ShowLayer1(); });

            // Previous Page Button (\u25C0) - to the left of the level button grid, vertically centered on it
            GameObject prevBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25C0", Color.white, 32);
            prevBtnObj.name = "PrevButton";
            RectTransform prevRect = prevBtnObj.GetComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0.05f, 0.455f);
            prevRect.anchorMax = new Vector2(0.05f, 0.455f);
            prevBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangePage(-1); });

            // Next Page Button (\u25B6) - to the right of the level button grid, vertically centered on it
            GameObject nextBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25B6", Color.white, 32);
            nextBtnObj.name = "NextButton";
            RectTransform nextRect = nextBtnObj.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.95f, 0.455f);
            nextRect.anchorMax = new Vector2(0.95f, 0.455f);
            nextBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangePage(1); });

            // ==========================================
            // TOP MENU HEADER BAR (Tab Switcher)
            // ==========================================
            topMenuObj = new GameObject("TopMenuHeader");
            topMenuObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tmRect = topMenuObj.AddComponent<RectTransform>();
            tmRect.anchorMin = new Vector2(0.5f, 0.76f);
            tmRect.anchorMax = new Vector2(0.5f, 0.76f);
            tmRect.pivot = new Vector2(0.5f, 0.5f);
            tmRect.sizeDelta = new Vector2(800f, 65f);

            HorizontalLayoutGroup tmHlg = topMenuObj.AddComponent<HorizontalLayoutGroup>();
            tmHlg.spacing = 30f;
            tmHlg.childAlignment = TextAnchor.MiddleCenter;

            // Campaign Tab Button
            GameObject campBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(topMenuObj.transform, new Vector2(350f, 55f), borderNeonColor, "CAMPAIGN", Color.white, 20);
            campaignTab = campBtnObj.GetComponent<TubityXPanel>();
            campBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ShowCampaign(); });
            campBtnObj.SetActive(testMenuButtonsUnlocked);

            // Test Levels Tab Button
            GameObject testBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(topMenuObj.transform, new Vector2(350f, 55f), neonMagentaColor, "TEST LEVELS", Color.white, 20);
            testTab = testBtnObj.GetComponent<TubityXPanel>();
            testBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ShowTestLevels(); });
            // Test Levels shows on iOS for now too, without the three-finger unlock, so TestFlight
            // testers can reach the sandbox. Restore SetActive(testMenuButtonsUnlocked) before review.
            testBtnObj.SetActive(true);

            // ==========================================
            // LAYER 3: TEST LEVELS LAYER
            // ==========================================
            layer3Obj = new GameObject("Layer3_TestLevels");
            layer3Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l3Rect = layer3Obj.AddComponent<RectTransform>();
            l3Rect.anchorMin = Vector2.zero;
            l3Rect.anchorMax = Vector2.one;
            l3Rect.sizeDelta = Vector2.zero;

            GameObject l3Panel = GlassUIFactory.CreateGlassmorphicPanel(layer3Obj.transform, new Vector2(1340f, 720f), borderNeonColor, new Vector2(0f, 30f));
            l3Panel.name = "L3_Panel";

            // Indicator
            GameObject l3IndObj = new GameObject("L3_Indicator");
            l3IndObj.transform.SetParent(l3Panel.transform, false);
            RectTransform l3IndRect = l3IndObj.AddComponent<RectTransform>();
            l3IndRect.anchorMin = new Vector2(0f, 0.85f);
            l3IndRect.anchorMax = new Vector2(1f, 0.98f);
            l3IndRect.sizeDelta = Vector2.zero;

            testLevelIndicatorText = TubityXUIFactory.AddLabel(l3IndObj, "SELECT TEST LEVEL", 19f, textGoldColor, textGoldColor);

            // Test Level Grid
            GameObject l3GridObj = new GameObject("L3_Grid");
            l3GridObj.transform.SetParent(l3Panel.transform, false);
            RectTransform l3GridRect = l3GridObj.AddComponent<RectTransform>();
            l3GridRect.anchorMin = new Vector2(0.10f, 0.22f);
            l3GridRect.anchorMax = new Vector2(0.90f, 0.80f);
            l3GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l3Grid = l3GridObj.AddComponent<GridLayoutGroup>();
            l3Grid.cellSize = new Vector2(230f, 88f);
            l3Grid.spacing = new Vector2(40f, 30f);
            l3Grid.childAlignment = TextAnchor.MiddleCenter;
            l3Grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            l3Grid.constraintCount = 4;

            for (int i = 0; i < TestLevelsPerPage; i++)
            {
                Color bCol = (i % 2 == 0) ? borderNeonColor : neonMagentaColor;
                GameObject lvlBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l3GridObj.transform, new Vector2(230f, 88f), bCol, "", Color.clear);
                lvlBtnObj.name = "TestLevelButton_" + i;

                // Level Number
                GameObject numObj = new GameObject("TestLevelNumText");
                numObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform numRect = numObj.AddComponent<RectTransform>();
                numRect.anchorMin = new Vector2(0f, 0.42f);
                numRect.anchorMax = new Vector2(1f, 0.95f);
                numRect.sizeDelta = Vector2.zero;

                TubityXLabel numText = TubityXUIFactory.AddLabel(numObj, "", 20f, textGoldColor, textGoldColor);

                // Subtitle / Title Description
                GameObject subObj = new GameObject("TestLevelSubText");
                subObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0f, 0.08f);
                subRect.anchorMax = new Vector2(1f, 0.42f);
                subRect.sizeDelta = Vector2.zero;

                TubityXLabel subText = TubityXUIFactory.AddLabel(subObj, "", 8.5f, borderNeonColor, borderNeonColor);

                testLevelButtons.Add(lvlBtnObj);
            }

            // Back Button (Top Left - standardized location across all menu screens)
            GameObject l3BackBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l3Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            l3BackBtnObj.name = "L3BackButton";
            RectTransform l3BackRect = l3BackBtnObj.GetComponent<RectTransform>();
            l3BackRect.anchorMin = new Vector2(0.06f, 0.90f);
            l3BackRect.anchorMax = new Vector2(0.06f, 0.90f);
            l3BackRect.pivot = new Vector2(0f, 1f);
            l3BackBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuBack(); ShowCampaign(); });

            // Previous Page Button (\u25C0)
            GameObject l3PrevBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l3Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25C0", Color.white, 32);
            l3PrevBtnObj.name = "L3PrevButton";
            RectTransform l3PrevRect = l3PrevBtnObj.GetComponent<RectTransform>();
            l3PrevRect.anchorMin = new Vector2(0.82f, 0.08f);
            l3PrevRect.anchorMax = new Vector2(0.82f, 0.08f);
            l3PrevBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangeTestLevelPage(-1); });

            // Next Page Button (\u25B6)
            GameObject l3NextBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l3Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25B6", Color.white, 32);
            l3NextBtnObj.name = "L3NextButton";
            RectTransform l3NextRect = l3NextBtnObj.GetComponent<RectTransform>();
            l3NextRect.anchorMin = new Vector2(0.94f, 0.08f);
            l3NextRect.anchorMax = new Vector2(0.94f, 0.08f);
            l3NextBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangeTestLevelPage(1); });

            layer3Obj.SetActive(false);

            CreateShopUI();

            progressionMenu = ProgressionMenu.Create(canvasObj.transform, borderNeonColor,
                                                     neonMagentaColor, textGoldColor);
            progressionMenu.onLaunch = LaunchGame;
            progressionMenu.onExit = ShowLayer1;
            progressionMenu.onForwardSound = PlayMenuForward;
            progressionMenu.onBackSound = PlayMenuBack;
            progressionMenu.onSelectSound = PlayMenuSelect;
        }


        /// <summary>
        /// The attract-screen hero and the skin-preview cluster occupy the same
        /// spot, so exactly one of them is on at a time: the hero on the top
        /// level, the cluster once PLAY opens the sphere-count selector.
        /// </summary>
        private void SetAttractHeroVisible(bool heroVisible)
        {
            // The platform stays up either way - only what stands on it changes.
            // The hero sphere hovers there on the top level; the count selector
            // puts its own formation on the same pad, and the pad's mirror picks
            // up whichever band spheres are live.
            AttractHeroStage hero = FindFirstObjectByType<AttractHeroStage>(FindObjectsInactive.Include);
            if (hero != null)
            {
                hero.SetVisible(true);
                hero.SetHeroSphereVisible(heroVisible);
            }

            AttractionSphereMorpher cluster = FindFirstObjectByType<AttractionSphereMorpher>(FindObjectsInactive.Include);
            if (cluster != null) cluster.gameObject.SetActive(!heroVisible);

            // The selector's own formation reads a little large against the zoomed-in
            // camera, so drop the pad (and the mirror plane, which tracks the pad's
            // own transform) a bit while it is open, and put it back for the hero.
            float baseY = GameSetup.Instance != null ? GameSetup.Instance.heroPadY
                                                       : (hero != null ? hero.transform.position.y : 0f);
            float padY = heroVisible ? baseY : baseY - sphereSelectorPadDrop;

            if (hero != null)
            {
                Vector3 p = hero.transform.position;
                p.y = padY;
                hero.transform.position = p;
            }
            if (cluster != null) cluster.groundPlaneY = padY;
        }

        /// <summary>
        /// Which tab is live now reads off the rim: the active one lights up and
        /// pulses, the other sits dim. Both share the panel material - the colour
        /// is vertex data, so this costs a mesh rebuild, not a material.
        /// </summary>
        private void SetTabActive(TubityXPanel tab, bool active)
        {
            if (tab == null) return;
            tab.RimColor = active ? borderNeonColor : new Color(0.30f, 0.34f, 0.52f, 1f);
            tab.PulseAmount = active ? 0.35f : 0f;
            tab.Highlight = active ? 0.5f : 0f;
        }

        private void SetTopLevelMenuVisible(bool visible)
        {
            if (topMenuObj != null) topMenuObj.SetActive(visible);
            if (logoObj != null) logoObj.SetActive(visible);
        }

        private void ShowLayer1()
        {
            SetTopLevelMenuVisible(true);
            SetAttractHeroVisible(true);
            if (layer1Obj != null) layer1Obj.SetActive(true);
            if (layer15Obj != null) layer15Obj.SetActive(false);
            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (progressionMenu != null) progressionMenu.HideAll();
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);

            // Revert Camera Zoom if active
            StartCoroutine(LerpCameraToDefault());
        }
        
        private void ShowSphereSelection3D()
        {
            SetTopLevelMenuVisible(false);
            if (layer1Obj != null) layer1Obj.SetActive(false);
            if (layer15Obj != null) layer15Obj.SetActive(true);
            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);
            
            // hand the stage back to the skin cluster before touching the morpher,
            // otherwise the lookup below misses a disabled object
            SetAttractHeroVisible(false);

            // Trigger Zoom Camera Animation
            StartCoroutine(LerpCameraToTarget(new Vector3(0f, -0.5f, 8f), 45f));
            
            // Apply sphere count to the morpher
            AttractionSphereMorpher morpher = FindFirstObjectByType<AttractionSphereMorpher>();
            if (morpher != null) morpher.SetSphereCount(selectedSphereCount);
            RefreshSphereSelectorLock();
        }

        private void ChangeSphereCount(int delta)
        {
            selectedSphereCount += delta;
            if (selectedSphereCount > 5) selectedSphereCount = 1;
            if (selectedSphereCount < 1) selectedSphereCount = 5;

            AttractionSphereMorpher morpher = FindFirstObjectByType<AttractionSphereMorpher>();
            if (morpher != null) morpher.SetSphereCount(selectedSphereCount);
            RefreshSphereSelectorLock();
        }

        /// <summary>Shows the lock badge/hint, dims the sphere formation and disables CONFIRM
        /// while the browsed sphere count is beyond the block the player has unlocked so far
        /// (see GameManager.IsSphereCountUnlocked).</summary>
        private void RefreshSphereSelectorLock()
        {
            bool unlocked = GameManager.IsSphereCountUnlocked(selectedSphereCount);

            if (sphereLockIconObj != null) sphereLockIconObj.SetActive(!unlocked);
            if (sphereLockHintText != null)
            {
                sphereLockHintText.gameObject.SetActive(!unlocked);
                if (!unlocked)
                {
                    int requiredLevel = GameManager.GetSphereUnlockLevel(selectedSphereCount);
                    sphereLockHintText.Text = $"BEAT LEVEL {requiredLevel} TO UNLOCK";
                }
            }
            if (confirmSphereBtnObj != null)
            {
                Button confirmBtn = confirmSphereBtnObj.GetComponent<Button>();
                if (confirmBtn != null) confirmBtn.interactable = unlocked;
            }

            AttractionSphereMorpher morpher = FindFirstObjectByType<AttractionSphereMorpher>();
            if (morpher != null) morpher.SetLocked(!unlocked);
        }

        private void ConfirmSphereCount()
        {
            if (!GameManager.IsSphereCountUnlocked(selectedSphereCount)) return;

            GameManager.lastSphereCount = selectedSphereCount;
            ShowLayer2();
        }

        // Camera Lerp Functions
        private System.Collections.IEnumerator LerpCameraToDefault()
        {
            if (Camera.main == null) yield break;

            // CameraController drives this same camera every LateUpdate in attraction
            // mode (target == null); left enabled, it fights this coroutine's manual
            // position writes every frame, producing a small forward/back jitter for
            // the duration of the tween. Hand it full control back once we're done.
            CameraController camController = Camera.main.GetComponent<CameraController>();
            if (camController != null) camController.enabled = false;

            Vector3 startPos = Camera.main.transform.position;
            Vector3 targetPos = new Vector3(0f, 1f, -10f); // default main menu camera pos
            float startFov = Camera.main.fieldOfView;
            float targetFov = 60f;

            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
                Camera.main.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
                yield return null;
            }

            if (camController != null) camController.enabled = true;
        }

        private System.Collections.IEnumerator LerpCameraToTarget(Vector3 targetPos, float targetFov)
        {
            if (Camera.main == null) yield break;

            // See LerpCameraToDefault: suspend CameraController for the tween so it
            // doesn't add its own per-frame movement on top of this manual Lerp.
            CameraController camController = Camera.main.GetComponent<CameraController>();
            if (camController != null) camController.enabled = false;

            Vector3 startPos = Camera.main.transform.position;
            float startFov = Camera.main.fieldOfView;

            float duration = 0.6f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
                Camera.main.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
                yield return null;
            }

            if (camController != null) camController.enabled = true;
        }

        private void ShowLayer2()
        {
            SetTabActive(campaignTab, true);
            SetTabActive(testTab, false);
            SetTopLevelMenuVisible(false);
            if (layer1Obj != null) layer1Obj.SetActive(false);
            if (layer15Obj != null) layer15Obj.SetActive(false);
            if (layer2Obj != null) layer2Obj.SetActive(true);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);

            if (sphereIndicatorText != null)
            {
                sphereIndicatorText.Text = $"SPHERES: {selectedSphereCount}   |   CAMPAIGN LEVELS";
            }

            currentLevelPage = 0;
            RefreshLevelGrid();
        }

        private void ChangePage(int delta)
        {
            int targetPage = currentLevelPage + delta;
            int totalPages = Mathf.CeilToInt(levelConfigs.Count / 6f);
            
            if (targetPage >= 0 && targetPage < totalPages)
            {
                currentLevelPage = targetPage;
                RefreshLevelGrid();
            }
        }

        private void RefreshLevelGrid()
        {
            int startIndex = currentLevelPage * 6;
            
            for (int i = 0; i < 6; i++)
            {
                int configIndex = startIndex + i;
                GameObject btnObj = levelButtons[i];
                Button btn = btnObj.GetComponent<Button>();

                TubityXLabel[] texts = btnObj.GetComponentsInChildren<TubityXLabel>();
                TubityXLabel numText = (texts.Length > 0) ? texts[0] : null;
                TubityXLabel subText = (texts.Length > 1) ? texts[1] : null;

                if (configIndex < levelConfigs.Count)
                {
                    LevelConfig config = levelConfigs[configIndex];
                    btnObj.SetActive(true);

                    bool unlocked = GameManager.IsLevelUnlocked(config.levelNumber);
                    int stars = GameManager.GetLevelStars(config.levelNumber);
                    Color lockedFace = new Color(0.45f, 0.45f, 0.55f, 0.85f);

                    if (numText != null)
                    {
                        // Beaten levels wear their best star count next to the number.
                        numText.Text = stars > 0 ? config.levelNumber + " " + new string('*', stars) : config.levelNumber.ToString();
                        numText.color = unlocked ? textGoldColor : lockedFace;
                    }
                    if (subText != null)
                    {
                        string tier = string.IsNullOrEmpty(config.levelName) ? $"LEVEL {config.levelNumber}" : config.levelName.ToUpper();
                        subText.Text = unlocked ? tier : "LOCKED";
                        subText.color = unlocked ? borderNeonColor : lockedFace;
                    }

                    btn.interactable = unlocked;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { PlayMenuForward(); LaunchGame(config); });

                    if (i < levelLockIcons.Count) levelLockIcons[i].SetActive(!unlocked);

                    // The level's opening, dimmed while locked; a dark frame until captured.
                    Transform thumbT = btnObj.transform.Find("Thumb");
                    RawImage thumb = thumbT != null ? thumbT.GetComponent<RawImage>() : null;
                    if (thumb != null)
                    {
                        Texture2D shot = TubityWAI.Progression.LevelThumbnails.GetCampaign(config.levelNumber);
                        thumb.texture = shot;
                        thumb.color = shot == null ? new Color(0.05f, 0.06f, 0.11f, 0.9f)
                                    : unlocked ? Color.white
                                    : new Color(0.30f, 0.30f, 0.36f, 1f);
                    }
                }
                else
                {
                    btnObj.SetActive(false);
                }
            }
        }

        private void ShowCampaign()
        {
            ShowLayer2();
        }

        /// <summary>Opens Progression Test 1 - the 128-level ladder, on its own save data.</summary>
        private void ShowProgressionMenu()
        {
            if (progressionMenu == null) return;
            SetTopLevelMenuVisible(false);
            SetAttractHeroVisible(false);
            if (layer1Obj != null) layer1Obj.SetActive(false);
            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);
            progressionMenu.ShowWorlds();
        }

        private void ShowEndlessMenu()
        {
            if (progressionMenu == null) return;
            SetTopLevelMenuVisible(false);
            SetAttractHeroVisible(false);
            if (layer1Obj != null) layer1Obj.SetActive(false);
            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);
            progressionMenu.ShowEndless();
        }

        private void ShowTestLevels()
        {
            SetTopLevelMenuVisible(false);

            SetTabActive(testTab, true);
            SetTabActive(campaignTab, false);

            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(true);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);

            currentTestLevelPage = 0;
            RefreshTestLevelGrid();
        }

        private void ChangeTestLevelPage(int delta)
        {
            int targetPage = currentTestLevelPage + delta;
            int totalPages = Mathf.CeilToInt(testLevelConfigs.Count / (float)TestLevelsPerPage);

            if (targetPage >= 0 && targetPage < totalPages)
            {
                currentTestLevelPage = targetPage;
                RefreshTestLevelGrid();
            }
        }

        private void RefreshTestLevelGrid()
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(testLevelConfigs.Count / (float)TestLevelsPerPage));
            if (testLevelIndicatorText != null)
            {
                string pageSuffix = totalPages > 1 ? $"  {currentTestLevelPage + 1}/{totalPages}" : "";
                testLevelIndicatorText.Text = $"TEST LEVELS ({selectedSphereCount} " + (selectedSphereCount == 1 ? "SPHERE)" : "SPHERES)") + pageSuffix;
            }

            int startIndex = currentTestLevelPage * TestLevelsPerPage;
            for (int i = 0; i < TestLevelsPerPage; i++)
            {
                int configIndex = startIndex + i;
                GameObject btnObj = testLevelButtons[i];
                Button btn = btnObj.GetComponent<Button>();

                TubityXLabel[] texts = btnObj.GetComponentsInChildren<TubityXLabel>();
                TubityXLabel numText = (texts.Length > 0) ? texts[0] : null;
                TubityXLabel subText = (texts.Length > 1) ? texts[1] : null;

                if (configIndex < testLevelConfigs.Count)
                {
                    LevelConfig config = testLevelConfigs[configIndex];
                    btnObj.SetActive(true);

                    if (numText != null) numText.Text = config.levelNumber.ToString();
                    if (subText != null) subText.Text = !string.IsNullOrEmpty(config.levelName) ? config.levelName.ToUpper() : $"TEST {config.levelNumber}";

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { PlayMenuForward(); LaunchGame(config); });
                }
                else
                {
                    btnObj.SetActive(false);
                }
            }
        }

        private void LaunchGame(LevelConfig config)
        {
            // Progression Test 1 and the endless modes keep their own unlock rules and their own
            // save data; their menus have already checked them, so the campaign gate is skipped.
            bool ownsItsGate = config.isTestLevel || Progression.ProgressionV2.IsProgressionLevel(config);
            if (!ownsItsGate && !GameManager.IsLevelUnlocked(config.levelNumber))
            {
                Debug.Log($"[MainMenu] Level {config.levelNumber} is locked; beat level {config.levelNumber - 1} first.");
                return;
            }

            Debug.Log($"[MainMenu] Launching level {config.levelNumber} with {selectedSphereCount} spheres...");
            
            Hide();

            if (GameSetup.Instance != null)
            {
                GameSetup.Instance.StartGame(selectedSphereCount, config);
            }
        }

        private void LaunchFTUELevel()
        {
            LevelConfig ftueConfig = new LevelConfig(99, 10f, 0.0f, 1.8f, isTest: true, name: "HOW TO PLAY", theme: "Tutorial");
            Debug.Log("[MainMenu] Launching First Time User Experience (FTUE) Tutorial level...");
            Hide();
            if (GameSetup.Instance != null)
            {
                GameSetup.Instance.StartGame(1, ftueConfig);
            }
        }

        // ==========================================
        // HOW TO PLAY (first-time user experience)
        // ==========================================

        private static bool HasSeenHowToPlay
        {
            get { return PlayerPrefs.GetInt(HowToPlaySeenKey, 0) == 1; }
        }

        private static void MarkHowToPlaySeen()
        {
            PlayerPrefs.SetInt(HowToPlaySeenKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// PLAY: the very first press runs the in-tube tutorial (FTUEManager)
        /// instead of the sphere picker. The flag is set on launch, so quitting
        /// the tutorial part-way never traps the player in it; the tutorial
        /// sets it again on completion for good measure.
        /// </summary>
        private void OnPlayPressed()
        {
            if (!HasSeenHowToPlay)
            {
                Debug.Log($"[MainMenu] First PLAY: running the tutorial (scheme={TubityXInput.Current})...");
                MarkHowToPlaySeen();
                LaunchFTUELevel();
            }
            else
            {
                ShowSphereSelection3D();
            }
        }

        private void CreateSettingsPopup()
        {
            settingsPopupObj = GlassUIFactory.CreateGlassmorphicPanel(canvasObj.transform, new Vector2(680f, 780f), borderNeonColor, Vector2.zero);
            settingsPopupObj.name = "SettingsPopup";

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(settingsPopupObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.86f);
            titleRect.anchorMax = new Vector2(1f, 0.98f);
            titleRect.sizeDelta = Vector2.zero;

            TubityXLabel titleText = TubityXUIFactory.AddLabel(titleObj, "SETTINGS / STORE", 19f, textGoldColor, textGoldColor);

            // Back Button (Glassmorphic Icon-Only: upper-left corner of the panel)
            GameObject backBtnObj2 = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(110f, 48f), borderNeonColor, "\u25C0", Color.white, 26);
            backBtnObj2.name = "SettingsBackButton";
            RectTransform backRect2 = backBtnObj2.GetComponent<RectTransform>();
            backRect2.anchorMin = new Vector2(0.06f, 0.90f);
            backRect2.anchorMax = new Vector2(0.06f, 0.90f);
            backRect2.pivot = new Vector2(0f, 1f);

            backBtnObj2.GetComponent<Button>().onClick.AddListener(() => { PlayMenuClose(); CloseSettingsPopup(); });

            // SFX Toggle (Glassmorphic)
            GameObject sfxToggleBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), borderNeonColor, "", Color.clear);
            sfxToggleBtnObj.name = "Settings_SfxToggle";
            RectTransform sfxToggleRect = sfxToggleBtnObj.GetComponent<RectTransform>();
            sfxToggleRect.anchorMin = new Vector2(0.5f, 0.76f);
            sfxToggleRect.anchorMax = new Vector2(0.5f, 0.76f);

            sfxToggleBtn = sfxToggleBtnObj.GetComponent<Button>();
            sfxToggleBtn.onClick.AddListener(() => {
                GameManager.SfxEnabled = !GameManager.SfxEnabled;
                if (menuAudioSource != null) menuAudioSource.mute = !GameManager.SfxEnabled;
                PlayMenuSelect();
                UpdateSettingsUI();
            });

            GameObject sfxToggleTextObj = new GameObject("Text");
            sfxToggleTextObj.transform.SetParent(sfxToggleBtnObj.transform, false);
            RectTransform sfxtRect = sfxToggleTextObj.AddComponent<RectTransform>();
            sfxtRect.anchorMin = Vector2.zero;
            sfxtRect.anchorMax = Vector2.one;
            sfxtRect.sizeDelta = Vector2.zero;

            sfxToggleText = TubityXUIFactory.AddLabel(sfxToggleTextObj, "SFX: ON", 14f, Color.white, borderNeonColor);

            // Music Toggle (Glassmorphic)
            GameObject musicToggleBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), borderNeonColor, "", Color.clear);
            musicToggleBtnObj.name = "Settings_MusicToggle";
            RectTransform musicToggleRect = musicToggleBtnObj.GetComponent<RectTransform>();
            musicToggleRect.anchorMin = new Vector2(0.5f, 0.64f);
            musicToggleRect.anchorMax = new Vector2(0.5f, 0.64f);

            musicToggleBtn = musicToggleBtnObj.GetComponent<Button>();
            musicToggleBtn.onClick.AddListener(() => {
                GameManager.MusicEnabled = !GameManager.MusicEnabled;
                PlayMenuSelect();
                UpdateSettingsUI();
            });

            GameObject musicToggleTextObj = new GameObject("Text");
            musicToggleTextObj.transform.SetParent(musicToggleBtnObj.transform, false);
            RectTransform musictRect = musicToggleTextObj.AddComponent<RectTransform>();
            musictRect.anchorMin = Vector2.zero;
            musictRect.anchorMax = Vector2.one;
            musictRect.sizeDelta = Vector2.zero;

            musicToggleText = TubityXUIFactory.AddLabel(musicToggleTextObj, "MUSIC: ON", 14f, Color.white, borderNeonColor);

            // 0. SHOP BUTTON (Glassmorphic)
            GameObject shopBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), textGoldColor, "", Color.clear);
            shopBtnObj.name = "Settings_ShopButton";
            RectTransform shopRect = shopBtnObj.GetComponent<RectTransform>();
            shopRect.anchorMin = new Vector2(0.5f, 0.52f);
            shopRect.anchorMax = new Vector2(0.5f, 0.52f);

            Button shopBtn = shopBtnObj.GetComponent<Button>();
            shopBtn.onClick.AddListener(() => {
                PlayMenuForward();
                CloseSettingsPopup();
                ShowShopMenu();
            });

            GameObject shopTextObj = new GameObject("Text");
            shopTextObj.transform.SetParent(shopBtnObj.transform, false);
            RectTransform shopTRect = shopTextObj.AddComponent<RectTransform>();
            shopTRect.anchorMin = Vector2.zero;
            shopTRect.anchorMax = Vector2.one;
            shopTRect.sizeDelta = Vector2.zero;

            // the cart emoji is outside the atlas; the shop mark stands in for it
            TubityXLabel shopTextVal = TubityXUIFactory.AddLabel(
                shopTextObj, "\u25A6 ENTER COSMETICS SHOP", 15f, textGoldColor, textGoldColor);

            // 1. Remove Ads Button (Glassmorphic)
            GameObject removeAdsBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), borderNeonColor, "", Color.clear);
            removeAdsBtnObj.name = "RemoveAdsButton";
            RectTransform removeAdsRect = removeAdsBtnObj.GetComponent<RectTransform>();
            removeAdsRect.anchorMin = new Vector2(0.5f, 0.40f);
            removeAdsRect.anchorMax = new Vector2(0.5f, 0.40f);

            removeAdsBtn = removeAdsBtnObj.GetComponent<Button>();
            removeAdsBtn.onClick.AddListener(() => {
                PlayMenuSelect();
                Debug.Log("[MainMenu] Remove Ads button clicked!");
                if (IAPManager.Instance != null)
                {
                    IAPManager.Instance.BuyRemoveAds();
                }
                else
                {
                    Debug.LogError("[MainMenu] IAPManager instance is missing.");
                }
            });

            GameObject removeAdsTextObj = new GameObject("Text");
            removeAdsTextObj.transform.SetParent(removeAdsBtnObj.transform, false);
            RectTransform ratRect = removeAdsTextObj.AddComponent<RectTransform>();
            ratRect.anchorMin = Vector2.zero;
            ratRect.anchorMax = Vector2.one;
            ratRect.sizeDelta = Vector2.zero;

            removeAdsText = TubityXUIFactory.AddLabel(removeAdsTextObj, "REMOVE ADS - $0.99", 12.2f, Color.white, borderNeonColor);

            // 2. Restore Purchases Button (Glassmorphic)
            GameObject restoreBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), borderNeonColor, "", Color.clear);
            restoreBtnObj.name = "RestoreButton";
            RectTransform restoreRect = restoreBtnObj.GetComponent<RectTransform>();
            restoreRect.anchorMin = new Vector2(0.5f, 0.28f);
            restoreRect.anchorMax = new Vector2(0.5f, 0.28f);

            Button restoreBtn = restoreBtnObj.GetComponent<Button>();
            restoreBtn.onClick.AddListener(() => {
                PlayMenuSelect();
                Debug.Log("[MainMenu] Restore Purchases button clicked!");
                if (IAPManager.Instance != null)
                {
                    IAPManager.Instance.RestorePurchases();
                }
            });

            GameObject restoreTextObj = new GameObject("Text");
            restoreTextObj.transform.SetParent(restoreBtnObj.transform, false);
            RectTransform resttRect = restoreTextObj.AddComponent<RectTransform>();
            resttRect.anchorMin = Vector2.zero;
            resttRect.anchorMax = Vector2.one;
            resttRect.sizeDelta = Vector2.zero;

            TubityXLabel restoreTextVal = TubityXUIFactory.AddLabel(restoreTextObj, "RESTORE PURCHASES", 12.2f, Color.white, borderNeonColor);

            // 3. Restore Default Settings Button (Glassmorphic)
            GameObject resetDefaultsBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), neonMagentaColor, "", Color.clear);
            resetDefaultsBtnObj.name = "RestoreDefaultsButton";
            RectTransform resetDefaultsRect = resetDefaultsBtnObj.GetComponent<RectTransform>();
            resetDefaultsRect.anchorMin = new Vector2(0.5f, 0.16f);
            resetDefaultsRect.anchorMax = new Vector2(0.5f, 0.16f);

            Button resetDefaultsBtn = resetDefaultsBtnObj.GetComponent<Button>();
            resetDefaultsBtn.onClick.AddListener(() => { PlayMenuSelect(); RestoreDefaultSettings(); });

            GameObject resetTextObj = new GameObject("Text");
            resetTextObj.transform.SetParent(resetDefaultsBtnObj.transform, false);
            RectTransform resetTRect = resetTextObj.AddComponent<RectTransform>();
            resetTRect.anchorMin = Vector2.zero;
            resetTRect.anchorMax = Vector2.one;
            resetTRect.sizeDelta = Vector2.zero;

            TubityXLabel resetTextVal = TubityXUIFactory.AddLabel(resetTextObj, "RESTORE DEFAULT SETTINGS", 12.2f, new Color(1f, 0.85f, 0.85f), borderNeonColor);

            settingsPopupObj.SetActive(false); // Hidden by default
        }

        private void OpenSettingsPopup()
        {
            Debug.Log("[MainMenu] Opening Settings popup...");
            SetTopLevelMenuVisible(false);
            if (layer1Obj != null) layer1Obj.SetActive(false);
            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);

            UpdateSettingsUI();
            if (settingsPopupObj != null) settingsPopupObj.SetActive(true);
        }

        private void CloseSettingsPopup()
        {
            Debug.Log("[MainMenu] Settings popup closed.");
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);
            ShowLayer1();
        }

        private void RestoreDefaultSettings()
        {
            Debug.Log("[MainMenu] Restoring game settings to default...");
            PlayerPrefs.DeleteKey("AdsRemoved");
            PlayerPrefs.DeleteKey(HowToPlaySeenKey);   // PLAY teaches again on the next press
            PlayerPrefs.Save();

            selectedSphereCount = 1;
            currentLevelPage = 0;

            UpdateSettingsUI();
            ShowLayer1();
            Debug.Log("[MainMenu] Default settings successfully restored.");
        }

        private void UpdateSettingsUI()
        {
            if (sfxToggleText != null)
            {
                bool sfxOn = GameManager.SfxEnabled;
                sfxToggleText.Text = sfxOn ? "SFX: ON" : "SFX: OFF";
                sfxToggleText.color = sfxOn ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            if (musicToggleText != null)
            {
                bool musicOn = GameManager.MusicEnabled;
                musicToggleText.Text = musicOn ? "MUSIC: ON" : "MUSIC: OFF";
                musicToggleText.color = musicOn ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            if (IAPManager.Instance != null && removeAdsText != null && removeAdsBtn != null)
            {
                bool adsRemoved = IAPManager.Instance.IsAdsRemoved();
                string adsPrice = IAPManager.Instance.GetLocalizedPrice(IAPManager.ProductRemoveAds, IAPManager.RemoveAdsFallbackPrice);
                removeAdsText.Text = adsRemoved ? "ADS REMOVED" : $"REMOVE ADS - {adsPrice}";
                removeAdsBtn.interactable = !adsRemoved;
                if (adsRemoved)
                {
                    removeAdsText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                }
                else
                {
                    removeAdsText.color = Color.white;
                }
            }
        }

        // ==========================================
        // LAYER 4: SHOP MENU
        // ==========================================
        private void CreateShopUI()
        {
            layer4Obj = new GameObject("Layer4_ShopMenu");
            layer4Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l4Rect = layer4Obj.AddComponent<RectTransform>();
            l4Rect.anchorMin = Vector2.zero;
            l4Rect.anchorMax = Vector2.one;
            l4Rect.sizeDelta = Vector2.zero;

            GameObject l4Panel = GlassUIFactory.CreateGlassmorphicPanel(layer4Obj.transform, new Vector2(1340f, 760f), textGoldColor, new Vector2(0f, 20f));
            l4Panel.name = "L4_Panel";

            // Title indicator text
            GameObject indObj = new GameObject("L4_Indicator");
            indObj.transform.SetParent(l4Panel.transform, false);
            RectTransform indRect = indObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0.86f);
            indRect.anchorMax = new Vector2(1f, 0.98f);
            indRect.sizeDelta = Vector2.zero;

            shopTotalCoinsText = TubityXUIFactory.AddLabel(indObj, "SKIN SHOP   |   COINS: 0", 19f, textGoldColor, textGoldColor);

            // Skin Buttons Grid
            GameObject l4GridObj = new GameObject("L4_Grid");
            l4GridObj.transform.SetParent(l4Panel.transform, false);
            RectTransform l4GridRect = l4GridObj.AddComponent<RectTransform>();
            l4GridRect.anchorMin = new Vector2(0.10f, 0.20f);
            l4GridRect.anchorMax = new Vector2(0.90f, 0.82f);
            l4GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l4Grid = l4GridObj.AddComponent<GridLayoutGroup>();
            l4Grid.cellSize = new Vector2(290f, 160f);
            l4Grid.spacing = new Vector2(48f, 32f);
            l4Grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < ShopCardsPerPage; i++)
            {
                Color bCol = (i % 2 == 0) ? textGoldColor : borderNeonColor;
                GameObject skinBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4GridObj.transform, new Vector2(290f, 160f), bCol, "", Color.clear);
                skinBtnObj.name = "SkinButton_" + i;

                ShopCard card = new ShopCard();
                card.Root = skinBtnObj;
                card.Button = skinBtnObj.GetComponent<Button>();

                // Live preview window on the left: this card's slice of the
                // shared render texture the preview rig draws every frame.
                GameObject previewObj = new GameObject("SkinPreview", typeof(RectTransform), typeof(CanvasRenderer));
                previewObj.transform.SetParent(skinBtnObj.transform, false);
                RectTransform previewRect = previewObj.GetComponent<RectTransform>();
                previewRect.anchorMin = new Vector2(0f, 0.5f);
                previewRect.anchorMax = new Vector2(0f, 0.5f);
                previewRect.pivot = new Vector2(0f, 0.5f);
                previewRect.anchoredPosition = new Vector2(14f, 0f);
                previewRect.sizeDelta = new Vector2(130f, 130f);
                card.Preview = previewObj.AddComponent<RawImage>();
                card.Preview.uvRect = SkinShopPreview.SlotRect(i);
                card.Preview.raycastTarget = false;
                card.Preview.color = Color.white;

                // Skin Name
                GameObject nameObj = new GameObject("SkinNameText");
                nameObj.transform.SetParent(skinBtnObj.transform, false);
                RectTransform nameRect = nameObj.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.52f, 0.60f);
                nameRect.anchorMax = new Vector2(0.98f, 0.88f);
                nameRect.sizeDelta = Vector2.zero;
                card.Name = TubityXUIFactory.AddLabel(nameObj, "", 13.6f, Color.white, borderNeonColor);

                // One-line description
                GameObject blurbObj = new GameObject("SkinBlurbText");
                blurbObj.transform.SetParent(skinBtnObj.transform, false);
                RectTransform blurbRect = blurbObj.AddComponent<RectTransform>();
                blurbRect.anchorMin = new Vector2(0.52f, 0.38f);
                blurbRect.anchorMax = new Vector2(0.98f, 0.60f);
                blurbRect.sizeDelta = Vector2.zero;
                card.Blurb = TubityXUIFactory.AddLabel(blurbObj, "", 8.5f, new Color(0.65f, 0.75f, 0.9f), borderNeonColor);

                // Price or status
                GameObject subObj = new GameObject("SkinSubText");
                subObj.transform.SetParent(skinBtnObj.transform, false);
                RectTransform subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0.52f, 0.12f);
                subRect.anchorMax = new Vector2(0.98f, 0.36f);
                subRect.sizeDelta = Vector2.zero;
                card.Sub = TubityXUIFactory.AddLabel(subObj, "", 10.9f, textGoldColor, textGoldColor);

                shopCards.Add(card);
                shopButtons.Add(skinBtnObj);
            }

            // The rig that renders the previews. It only runs while this layer
            // is up, and goes away with the menu.
            skinPreview = SkinShopPreview.Create(transform, layer4Obj.transform, ShopPreviewColour);
            for (int i = 0; i < shopCards.Count; i++) shopCards[i].Preview.texture = skinPreview.Texture;

            // Back Button (Top Left - standardized location across all menu screens)
            GameObject backBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            backBtnObj.name = "BackButton";
            RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.06f, 0.90f);
            backRect.anchorMax = new Vector2(0.06f, 0.90f);
            backRect.pivot = new Vector2(0f, 1f);
            backBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuBack(); ShowLayer1(); });

            // Buy Coins Button (IAP consumable: 100 coins)
            GameObject buyCoinsBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4Panel.transform, new Vector2(360f, 60f), textGoldColor, "", Color.clear);
            buyCoinsBtnObj.name = "BuyCoinsButton";
            RectTransform buyCoinsRect = buyCoinsBtnObj.GetComponent<RectTransform>();
            buyCoinsRect.anchorMin = new Vector2(0.5f, 0.08f);
            buyCoinsRect.anchorMax = new Vector2(0.5f, 0.08f);

            buyCoinsBtn = buyCoinsBtnObj.GetComponent<Button>();
            buyCoinsBtn.onClick.AddListener(() => {
                PlayMenuSelect();
                Debug.Log("[MainMenu] Buy 100 Coins button clicked!");
                if (IAPManager.Instance != null)
                {
                    IAPManager.Instance.BuyCoins100();
                }
                else
                {
                    Debug.LogError("[MainMenu] IAPManager instance is missing.");
                }
            });

            GameObject buyCoinsTextObj = new GameObject("Text");
            buyCoinsTextObj.transform.SetParent(buyCoinsBtnObj.transform, false);
            RectTransform buyCoinsTRect = buyCoinsTextObj.AddComponent<RectTransform>();
            buyCoinsTRect.anchorMin = Vector2.zero;
            buyCoinsTRect.anchorMax = Vector2.one;
            buyCoinsTRect.sizeDelta = Vector2.zero;

            buyCoinsText = TubityXUIFactory.AddLabel(buyCoinsTextObj, "", 12.2f, textGoldColor, textGoldColor);
            UpdateBuyCoinsLabel();

            // Prev Button
            GameObject prevBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4Panel.transform, new Vector2(120f, 60f), textGoldColor, "\u25C0", Color.white, 32);
            prevBtnObj.name = "PrevButton";
            RectTransform prevRect = prevBtnObj.GetComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0.82f, 0.08f);
            prevRect.anchorMax = new Vector2(0.82f, 0.08f);
            prevBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangeShopPage(-1); });

            // Next Button
            GameObject nextBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4Panel.transform, new Vector2(120f, 60f), textGoldColor, "\u25B6", Color.white, 32);
            nextBtnObj.name = "NextButton";
            RectTransform nextRect = nextBtnObj.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.94f, 0.08f);
            nextRect.anchorMax = new Vector2(0.94f, 0.08f);
            nextBtnObj.GetComponent<Button>().onClick.AddListener(() => { PlayMenuSelect(); ChangeShopPage(1); });

            layer4Obj.SetActive(false);
        }

        private void ShowShopMenu()
        {
            SetTopLevelMenuVisible(false);

            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(true);

            currentShopPage = 0;
            UpdateBuyCoinsLabel();
            RefreshShopGrid();
        }

        private void ChangeShopPage(int delta)
        {
            int targetPage = currentShopPage + delta;
            int totalPages = Mathf.CeilToInt(SphereSkinCatalog.Count / (float)ShopCardsPerPage);
            
            if (targetPage >= 0 && targetPage < totalPages)
            {
                currentShopPage = targetPage;
                RefreshShopGrid();
            }
        }

        private void RefreshShopGrid()
        {
            if (GameManager.Instance == null) return;
            
            if (shopTotalCoinsText != null)
            {
                shopTotalCoinsText.Text = $"SKIN SHOP   |   COINS: {GameManager.Instance.TotalCoins}";
            }

            int startIndex = currentShopPage * ShopCardsPerPage;
            if (skinPreview != null) skinPreview.ShowPage(startIndex, ShopPreviewColour);
            
            for (int i = 0; i < shopCards.Count; i++)
            {
                int skinIndex = startIndex + i;
                ShopCard card = shopCards[i];

                if (skinIndex >= SphereSkinCatalog.Count)
                {
                    card.Root.SetActive(false);
                    continue;
                }

                SphereSkinCatalog.Skin skin = SphereSkinCatalog.Get(skinIndex);
                card.Root.SetActive(true);
                card.Name.Text = skin.Name;
                card.Blurb.Text = skin.Blurb;

                bool isUnlocked = GameManager.Instance.IsSkinUnlocked(skinIndex);
                bool isEquipped = GameManager.Instance.EquippedSkin == skinIndex;

                if (isEquipped)
                {
                    card.Sub.Text = "EQUIPPED";
                    card.Sub.color = borderNeonColor;
                    card.Button.interactable = false; // Disable if already equipped
                }
                else if (isUnlocked)
                {
                    card.Sub.Text = "EQUIP";
                    card.Sub.color = Color.white;
                    card.Button.interactable = true;
                }
                else
                {
                    bool canAfford = GameManager.Instance.TotalCoins >= skin.Price;
                    card.Sub.Text = $"BUY: {skin.Price}";
                    card.Sub.color = canAfford ? textGoldColor : new Color(0.6f, 0.6f, 0.6f);
                    // Disable if they don't have enough coins, to prevent confusion when clicking does nothing
                    card.Button.interactable = canAfford;
                }

                card.Button.onClick.RemoveAllListeners();
                int capturedIndex = skinIndex; // ensure capture
                card.Button.onClick.AddListener(() => { PlayMenuSelect(); OnSkinButtonClicked(capturedIndex); });
            }
        }

        private void OnSkinButtonClicked(int skinIndex)
        {
            if (GameManager.Instance == null) return;

            bool isUnlocked = GameManager.Instance.IsSkinUnlocked(skinIndex);
            
            if (isUnlocked)
            {
                GameManager.Instance.EquippedSkin = skinIndex;
            }
            else
            {
                int price = SphereSkinCatalog.Get(skinIndex).Price;
                if (GameManager.Instance.DeductCoins(price))
                {
                    GameManager.Instance.UnlockSkin(skinIndex);
                    GameManager.Instance.EquippedSkin = skinIndex; // Auto-equip on purchase
                }
            }

            // the count selector's formation is the fitting room - re-dress it now
            // so PLAY shows the new skin without a restart
            GameSetup setup = GameSetup.Instance != null ? GameSetup.Instance : FindFirstObjectByType<GameSetup>();
            if (setup != null) setup.RefreshMenuSkinPreview();

            RefreshShopGrid();
        }

        private void OnDestroy()
        {
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnPurchaseComplete -= OnIAPPurchaseComplete;
                IAPManager.Instance.OnRestoreComplete -= UpdateSettingsUI;
                IAPManager.Instance.OnProductsReady -= OnIAPProductsReady;
            }
        }
    }
}
