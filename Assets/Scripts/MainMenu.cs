using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace TubityWAI
{
    [ExecuteAlways]
    public class MainMenu : MonoBehaviour
    {
        [Header("Menu Styling")]
        public Color panelBackgroundColor = new Color(0.04f, 0.02f, 0.08f, 0.95f);
        public Color borderNeonColor = new Color(0f, 1f, 1f, 0.85f); // Neon Cyan
        public Color neonMagentaColor = new Color(1f, 0f, 0.6f, 0.85f); // Neon Pink/Magenta
        public Color textGoldColor = new Color(1f, 0.85f, 0f);      // Neon Gold/Yellow

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
        private int selectedSphereCount = 3; // Default selection
        private int currentLevelPage = 0;   // 0 = Levels 1-6, 1 = Levels 7-12
        private Font defaultFont;

        // UI Layer Containers
        private GameObject canvasObj;
        private GameObject layer1Obj; // Sphere Selection
        private GameObject layer2Obj; // Level Selection

        // Text indicator in Layer 2
        private Text sphereIndicatorText;
        private GameObject settingsPopupObj;
        private Button removeAdsBtn;
        private Text removeAdsText;
        private List<GameObject> levelButtons = new List<GameObject>();

        // Test Levels & Top Menu Bar Fields
        private List<LevelConfig> testLevelConfigs = new List<LevelConfig>();
        private GameObject layer3Obj;
        private Text testLevelIndicatorText;
        private List<GameObject> testLevelButtons = new List<GameObject>();

        // Shop Fields
        private GameObject layer4Obj;
        private Text shopTotalCoinsText;
        private List<GameObject> shopButtons = new List<GameObject>();
        private int currentShopPage = 0;

        private struct SkinItem
        {
            public string Name;
            public int Price;
            public string Icon;
        }

        private SkinItem[] shopSkins = new SkinItem[]
        {
            new SkinItem { Name = "DEFAULT GRID", Price = 0, Icon = "▦" },
            new SkinItem { Name = "SOLID CORE", Price = 100, Icon = "⭕" },
            new SkinItem { Name = "STRIPES", Price = 200, Icon = "▤" },
            new SkinItem { Name = "CHECKER", Price = 350, Icon = "▨" },
            new SkinItem { Name = "CIRCUIT", Price = 500, Icon = "⎉" },
            new SkinItem { Name = "HEXAGON", Price = 750, Icon = "⬢" },
            new SkinItem { Name = "PLASMA FX", Price = 1000, Icon = "✦" },
            new SkinItem { Name = "WARP FX", Price = 1500, Icon = "✧" },
            new SkinItem { Name = "GAUNTLET FX", Price = 2000, Icon = "✶" },
            new SkinItem { Name = "CITY FX", Price = 3000, Icon = "🏢" }
        };

        private GameObject logoObj;
        private GameObject bottomNavObj;
        private GameObject topMenuObj;
        private Image campaignTabImg;
        private Outline campaignTabBorder;
        private Image testTabImg;
        private Outline testTabBorder;

        private bool isMenuHidden = false;

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

        private void Start()
        {
            // Clean up old canvas/event system to prevent duplication in edit mode
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
                IAPManager.Instance.OnPurchaseComplete += UpdateSettingsUI;
                IAPManager.Instance.OnRestoreComplete += UpdateSettingsUI;
            }
        }

        private void InitializeLevelConfigurations()
        {
            levelConfigs.Add(new LevelConfig(1, 10f, 0.20f, 1.8f));
            levelConfigs.Add(new LevelConfig(2, 12f, 0.25f, 1.8f));
            levelConfigs.Add(new LevelConfig(3, 14f, 0.30f, 1.9f));
            levelConfigs.Add(new LevelConfig(4, 16f, 0.35f, 1.9f));
            levelConfigs.Add(new LevelConfig(5, 18f, 0.40f, 2.0f));
            levelConfigs.Add(new LevelConfig(6, 20f, 0.45f, 2.0f));
            levelConfigs.Add(new LevelConfig(7, 22f, 0.50f, 2.1f));
            levelConfigs.Add(new LevelConfig(8, 24f, 0.55f, 2.1f));
            levelConfigs.Add(new LevelConfig(9, 26f, 0.60f, 2.2f));
            levelConfigs.Add(new LevelConfig(10, 28f, 0.65f, 2.2f));
            levelConfigs.Add(new LevelConfig(11, 30f, 0.70f, 2.3f));
            levelConfigs.Add(new LevelConfig(12, 35f, 0.80f, 2.5f));
        }

        private void InitializeTestLevelConfigurations()
        {
            testLevelConfigs.Add(new LevelConfig(101, 15f, 0.30f, 2.0f, true, "PLASMA SPHERE", "Plasma Core"));
            testLevelConfigs.Add(new LevelConfig(102, 35f, 0.10f, 2.5f, true, "HYPER WARP", "Speed Test"));
            testLevelConfigs.Add(new LevelConfig(103, 8f, 0.75f, 1.5f, true, "HAZARD GAUNTLET", "Density Test"));
            testLevelConfigs.Add(new LevelConfig(104, 12f, 0.45f, 1.8f, true, "ISO VIEW TEST", "Isometric View", true, 0.5f, 1.2f, 8.0f, -3.5f));
            testLevelConfigs.Add(new LevelConfig(105, 12f, 0.35f, 1.8f, true, "ISO CURVES TEST", "Curved & Angled", true, 0.5f, 1.2f, 8.0f, -3.5f, true, 0.04f, 3.0f));
            testLevelConfigs.Add(new LevelConfig(106, 14f, 0.35f, 1.8f, true, "CITY FLYBY", "Abstract City", true, 0.5f, 1.2f, 8.0f, -3.5f, true, 0.035f, 4.0f, true, true));
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

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // Global Title Logo (Top Center)
            logoObj = new GameObject("MenuLogo");
            logoObj.transform.SetParent(canvasObj.transform, false);
            RectTransform logoRect = logoObj.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.86f);
            logoRect.anchorMax = new Vector2(0.5f, 0.86f);
            logoRect.sizeDelta = new Vector2(750f, 180f);
            logoRect.anchoredPosition = Vector2.zero;

            Image logoImg = logoObj.AddComponent<Image>();
            Sprite logoSprite = Resources.Load<Sprite>("tubityx_title");
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
                logoImg.color = Color.white;
                logoImg.preserveAspect = true;
            }
            else
            {
                Text logoText = logoObj.AddComponent<Text>();
                logoText.font = defaultFont;
                logoText.fontSize = 64;
                logoText.fontStyle = FontStyle.Bold;
                logoText.alignment = TextAnchor.MiddleCenter;
                logoText.color = textGoldColor;
                logoText.text = "TUBITYX";

                Shadow logoShadow = logoObj.AddComponent<Shadow>();
                logoShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                logoShadow.effectDistance = new Vector2(3f, -3f);

                Outline logoGlow = logoObj.AddComponent<Outline>();
                logoGlow.effectColor = borderNeonColor;
                logoGlow.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // Create Settings Popup
            CreateSettingsPopup();

            // =========================================================================
            // BOTTOM NAVIGATION BAR (PLAY, SETTINGS)
            // =========================================================================
            bottomNavObj = new GameObject("BottomNavigationBar");
            bottomNavObj.transform.SetParent(canvasObj.transform, false);
            RectTransform navRect = bottomNavObj.AddComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0.5f, 0.08f);
            navRect.anchorMax = new Vector2(0.5f, 0.08f);
            navRect.pivot = new Vector2(0.5f, 0f);
            navRect.anchoredPosition = Vector2.zero;
            navRect.sizeDelta = new Vector2(1100f, 130f);

            HorizontalLayoutGroup navHlg = bottomNavObj.AddComponent<HorizontalLayoutGroup>();
            navHlg.spacing = 32f;
            navHlg.childAlignment = TextAnchor.MiddleCenter;
            navHlg.childControlWidth = false;
            navHlg.childControlHeight = false;

            // 1. PLAY BUTTON (Double Right Arrow Icon in Neon Cyan Rim)
            GameObject playBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(bottomNavObj.transform, new Vector2(250f, 120f), borderNeonColor, "\u25B6\u25B6", borderNeonColor, 60, true);
            playBtnObj.name = "NavBtn_Play";
            playBtnObj.GetComponent<Button>().onClick.AddListener(() => {
                LevelConfig firstLvl = (levelConfigs.Count > 0) ? levelConfigs[0] : null;
                if (firstLvl != null) LaunchGame(firstLvl);
            });

            // 2. SHOP BUTTON (Cart/Shop Icon in Neon Gold Rim)
            GameObject shopBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(bottomNavObj.transform, new Vector2(180f, 120f), textGoldColor, "\uD83D\uDED2", textGoldColor, 50); // Shopping cart emoji
            shopBtnObj.name = "NavBtn_Shop";
            shopBtnObj.GetComponent<Button>().onClick.AddListener(ShowShopMenu);

            // 3. SETTINGS BUTTON (Gear Icon in Neon Cyan Rim)
            GameObject settingsBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(bottomNavObj.transform, new Vector2(180f, 120f), borderNeonColor, "\u2699", borderNeonColor, 56);
            settingsBtnObj.name = "NavBtn_Settings";
            settingsBtnObj.GetComponent<Button>().onClick.AddListener(OpenSettingsPopup);

            // ==========================================
            // LAYER 1: SPHERE SELECTION LAYER
            // ==========================================
            layer1Obj = new GameObject("Layer1_SphereSelection");
            layer1Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l1Rect = layer1Obj.AddComponent<RectTransform>();
            l1Rect.anchorMin = Vector2.zero;
            l1Rect.anchorMax = Vector2.one;
            l1Rect.sizeDelta = Vector2.zero;

            GameObject l1Panel = GlassUIFactory.CreateGlassmorphicPanel(layer1Obj.transform, new Vector2(1440f, 220f), borderNeonColor, new Vector2(0f, -30f));
            l1Panel.name = "L1_Panel";

            // Title Label for Sphere Selection
            GameObject l1TitleObj = new GameObject("L1_Title");
            l1TitleObj.transform.SetParent(l1Panel.transform, false);
            RectTransform l1TitleRect = l1TitleObj.AddComponent<RectTransform>();
            l1TitleRect.anchorMin = new Vector2(0f, 0.72f);
            l1TitleRect.anchorMax = new Vector2(1f, 0.98f);
            l1TitleRect.sizeDelta = Vector2.zero;

            Text l1TitleText = l1TitleObj.AddComponent<Text>();
            l1TitleText.font = defaultFont;
            l1TitleText.fontSize = 24;
            l1TitleText.fontStyle = FontStyle.Bold;
            l1TitleText.alignment = TextAnchor.MiddleCenter;
            l1TitleText.color = textGoldColor;
            l1TitleText.text = "SELECT PLAYER SPHERES";

            Shadow l1TitleShadow = l1TitleObj.AddComponent<Shadow>();
            l1TitleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            l1TitleShadow.effectDistance = new Vector2(2f, -2f);

            // Button Container inside L1 Panel
            GameObject l1Container = new GameObject("L1_Container");
            l1Container.transform.SetParent(l1Panel.transform, false);
            RectTransform l1cRect = l1Container.AddComponent<RectTransform>();
            l1cRect.anchorMin = new Vector2(0.02f, 0.05f);
            l1cRect.anchorMax = new Vector2(0.98f, 0.72f);
            l1cRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup l1Hlg = l1Container.AddComponent<HorizontalLayoutGroup>();
            l1Hlg.spacing = 18f;
            l1Hlg.childAlignment = TextAnchor.MiddleCenter;
            l1Hlg.childControlWidth = false;
            l1Hlg.childControlHeight = false;

            // 0. Spawn FTUE (Tutorial) Launch Button to the left of the 1-sphere button
            GameObject ftueBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l1Container.transform, new Vector2(200f, 130f), textGoldColor, "\u2753", textGoldColor, 36);
            ftueBtnObj.name = "FTUE_Button";
            ftueBtnObj.GetComponent<Button>().onClick.AddListener(LaunchFTUELevel);

            // Subtitle Label for FTUE Button
            GameObject ftueLabelObj = new GameObject("FTUELabel");
            ftueLabelObj.transform.SetParent(ftueBtnObj.transform, false);
            RectTransform ftueLabelRect = ftueLabelObj.AddComponent<RectTransform>();
            ftueLabelRect.anchorMin = new Vector2(0f, 0.05f);
            ftueLabelRect.anchorMax = new Vector2(1f, 0.35f);
            ftueLabelRect.sizeDelta = Vector2.zero;

            Text ftueLabelText = ftueLabelObj.AddComponent<Text>();
            ftueLabelText.font = defaultFont;
            ftueLabelText.fontSize = 14;
            ftueLabelText.fontStyle = FontStyle.Bold;
            ftueLabelText.alignment = TextAnchor.MiddleCenter;
            ftueLabelText.color = textGoldColor;
            ftueLabelText.text = "HOW TO PLAY";

            Shadow ftueLabelShadow = ftueLabelObj.AddComponent<Shadow>();
            ftueLabelShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            ftueLabelShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Spawn sphere selection glass buttons (1 to 5)
            for (int count = 1; count <= 5; count++)
            {
                int localCount = count;
                Color borderCol = (count % 2 == 1) ? borderNeonColor : neonMagentaColor;
                GameObject btnObj = GlassUIFactory.CreateGlassmorphicIconButton(l1Container.transform, new Vector2(200f, 130f), borderCol, "", Color.clear);
                btnObj.name = "SphereButton_" + count;

                btnObj.GetComponent<Button>().onClick.AddListener(() => OnSphereCountSelected(localCount));

                // Container holding glowing ball icons
                GameObject ballsContainer = new GameObject("BallsContainer");
                ballsContainer.transform.SetParent(btnObj.transform, false);
                RectTransform bcRect = ballsContainer.AddComponent<RectTransform>();
                bcRect.anchorMin = new Vector2(0f, 0.35f);
                bcRect.anchorMax = new Vector2(1f, 0.95f);
                bcRect.sizeDelta = Vector2.zero;

                HorizontalLayoutGroup hlg = ballsContainer.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 6f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                for (int i = 0; i < localCount; i++)
                {
                    GameObject ballObj = new GameObject("BallIcon");
                    ballObj.transform.SetParent(ballsContainer.transform, false);
                    RectTransform bRect = ballObj.AddComponent<RectTransform>();
                    bRect.sizeDelta = new Vector2(26f, 26f);

                    Image ballImg = ballObj.AddComponent<Image>();
                    ballImg.sprite = GlassUIFactory.GetCircleSprite();
                    ballImg.color = sphereColors[i % sphereColors.Length];

                    Outline ballGlow = ballObj.AddComponent<Outline>();
                    ballGlow.effectColor = new Color(1f, 1f, 1f, 0.7f);
                    ballGlow.effectDistance = new Vector2(1f, -1f);
                }

                // Count Subtitle Label
                GameObject labelObj = new GameObject("SphereLabel");
                labelObj.transform.SetParent(btnObj.transform, false);
                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0.05f);
                labelRect.anchorMax = new Vector2(1f, 0.35f);
                labelRect.sizeDelta = Vector2.zero;

                Text labelText = labelObj.AddComponent<Text>();
                labelText.font = defaultFont;
                labelText.fontSize = 15;
                labelText.fontStyle = FontStyle.Bold;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.color = textGoldColor;
                labelText.text = (localCount == 1) ? "1 SPHERE" : $"{localCount} SPHERES";

                Shadow labelShadow = labelObj.AddComponent<Shadow>();
                labelShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                labelShadow.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // ==========================================
            // LAYER 2: LEVEL SELECTION LAYER
            // ==========================================
            layer2Obj = new GameObject("Layer2_LevelSelection");
            layer2Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l2Rect = layer2Obj.AddComponent<RectTransform>();
            l2Rect.anchorMin = Vector2.zero;
            l2Rect.anchorMax = Vector2.one;
            l2Rect.sizeDelta = Vector2.zero;

            GameObject l2Panel = GlassUIFactory.CreateGlassmorphicPanel(layer2Obj.transform, new Vector2(1240f, 640f), borderNeonColor, new Vector2(0f, 40f));
            l2Panel.name = "L2_Panel";

            // Title indicator text
            GameObject indObj = new GameObject("L2_Indicator");
            indObj.transform.SetParent(l2Panel.transform, false);
            RectTransform indRect = indObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0.85f);
            indRect.anchorMax = new Vector2(1f, 0.98f);
            indRect.sizeDelta = Vector2.zero;

            sphereIndicatorText = indObj.AddComponent<Text>();
            sphereIndicatorText.font = defaultFont;
            sphereIndicatorText.fontSize = 28;
            sphereIndicatorText.fontStyle = FontStyle.Bold;
            sphereIndicatorText.alignment = TextAnchor.MiddleCenter;
            sphereIndicatorText.color = textGoldColor;
            sphereIndicatorText.text = "SELECT LEVEL";

            Shadow indShadow = indObj.AddComponent<Shadow>();
            indShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            indShadow.effectDistance = new Vector2(2f, -2f);

            // Level Buttons Grid
            GameObject l2GridObj = new GameObject("L2_Grid");
            l2GridObj.transform.SetParent(l2Panel.transform, false);
            RectTransform l2GridRect = l2GridObj.AddComponent<RectTransform>();
            l2GridRect.anchorMin = new Vector2(0.08f, 0.22f);
            l2GridRect.anchorMax = new Vector2(0.92f, 0.83f);
            l2GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l2Grid = l2GridObj.AddComponent<GridLayoutGroup>();
            l2Grid.cellSize = new Vector2(290f, 135f);
            l2Grid.spacing = new Vector2(40f, 24f);
            l2Grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 6; i++)
            {
                Color bCol = (i % 2 == 0) ? borderNeonColor : neonMagentaColor;
                GameObject lvlBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2GridObj.transform, new Vector2(290f, 135f), bCol, "", Color.clear);
                lvlBtnObj.name = "LevelButton_" + i;

                // Level Number
                GameObject numObj = new GameObject("LevelNumText");
                numObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform numRect = numObj.AddComponent<RectTransform>();
                numRect.anchorMin = new Vector2(0f, 0.40f);
                numRect.anchorMax = new Vector2(1f, 0.95f);
                numRect.sizeDelta = Vector2.zero;

                Text numText = numObj.AddComponent<Text>();
                numText.font = defaultFont;
                numText.fontSize = 42;
                numText.fontStyle = FontStyle.Bold;
                numText.alignment = TextAnchor.MiddleCenter;
                numText.color = textGoldColor;

                Shadow numShadow = numObj.AddComponent<Shadow>();
                numShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                numShadow.effectDistance = new Vector2(2f, -2f);

                // Subtitle
                GameObject subObj = new GameObject("LevelSubText");
                subObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0f, 0.08f);
                subRect.anchorMax = new Vector2(1f, 0.40f);
                subRect.sizeDelta = Vector2.zero;

                Text subText = subObj.AddComponent<Text>();
                subText.font = defaultFont;
                subText.fontSize = 16;
                subText.fontStyle = FontStyle.Bold;
                subText.alignment = TextAnchor.MiddleCenter;
                subText.color = borderNeonColor;

                Shadow subShadow = subObj.AddComponent<Shadow>();
                subShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                subShadow.effectDistance = new Vector2(1.5f, -1.5f);

                levelButtons.Add(lvlBtnObj);
            }

            // Back Button (Bottom Left)
            GameObject backBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            backBtnObj.name = "BackButton";
            RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.08f, 0.08f);
            backRect.anchorMax = new Vector2(0.08f, 0.08f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backBtnObj.GetComponent<Button>().onClick.AddListener(ShowLayer1);

            // Previous Page Button (\u25C0)
            GameObject prevBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25C0", Color.white, 32);
            prevBtnObj.name = "PrevButton";
            RectTransform prevRect = prevBtnObj.GetComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0.82f, 0.08f);
            prevRect.anchorMax = new Vector2(0.82f, 0.08f);
            prevBtnObj.GetComponent<Button>().onClick.AddListener(() => ChangePage(-1));

            // Next Page Button (\u25B6)
            GameObject nextBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25B6", Color.white, 32);
            nextBtnObj.name = "NextButton";
            RectTransform nextRect = nextBtnObj.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.94f, 0.08f);
            nextRect.anchorMax = new Vector2(0.94f, 0.08f);
            nextBtnObj.GetComponent<Button>().onClick.AddListener(() => ChangePage(1));

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
            campaignTabImg = campBtnObj.GetComponent<Image>();
            campaignTabBorder = campBtnObj.GetComponent<Outline>();
            campBtnObj.GetComponent<Button>().onClick.AddListener(ShowCampaign);

            // Test Levels Tab Button
            GameObject testBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(topMenuObj.transform, new Vector2(350f, 55f), neonMagentaColor, "TEST LEVELS", Color.white, 20);
            testTabImg = testBtnObj.GetComponent<Image>();
            testTabBorder = testBtnObj.GetComponent<Outline>();
            testBtnObj.GetComponent<Button>().onClick.AddListener(ShowTestLevels);

            // ==========================================
            // LAYER 3: TEST LEVELS LAYER
            // ==========================================
            layer3Obj = new GameObject("Layer3_TestLevels");
            layer3Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l3Rect = layer3Obj.AddComponent<RectTransform>();
            l3Rect.anchorMin = Vector2.zero;
            l3Rect.anchorMax = Vector2.one;
            l3Rect.sizeDelta = Vector2.zero;

            GameObject l3Panel = GlassUIFactory.CreateGlassmorphicPanel(layer3Obj.transform, new Vector2(1240f, 640f), borderNeonColor, new Vector2(0f, 40f));
            l3Panel.name = "L3_Panel";

            // Indicator
            GameObject l3IndObj = new GameObject("L3_Indicator");
            l3IndObj.transform.SetParent(l3Panel.transform, false);
            RectTransform l3IndRect = l3IndObj.AddComponent<RectTransform>();
            l3IndRect.anchorMin = new Vector2(0f, 0.85f);
            l3IndRect.anchorMax = new Vector2(1f, 0.98f);
            l3IndRect.sizeDelta = Vector2.zero;

            testLevelIndicatorText = l3IndObj.AddComponent<Text>();
            testLevelIndicatorText.font = defaultFont;
            testLevelIndicatorText.fontSize = 28;
            testLevelIndicatorText.fontStyle = FontStyle.Bold;
            testLevelIndicatorText.alignment = TextAnchor.MiddleCenter;
            testLevelIndicatorText.color = textGoldColor;
            testLevelIndicatorText.text = "SELECT TEST LEVEL";

            Shadow l3IndShadow = l3IndObj.AddComponent<Shadow>();
            l3IndShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            l3IndShadow.effectDistance = new Vector2(2f, -2f);

            // Test Level Grid
            GameObject l3GridObj = new GameObject("L3_Grid");
            l3GridObj.transform.SetParent(l3Panel.transform, false);
            RectTransform l3GridRect = l3GridObj.AddComponent<RectTransform>();
            l3GridRect.anchorMin = new Vector2(0.08f, 0.22f);
            l3GridRect.anchorMax = new Vector2(0.92f, 0.83f);
            l3GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l3Grid = l3GridObj.AddComponent<GridLayoutGroup>();
            l3Grid.cellSize = new Vector2(290f, 135f);
            l3Grid.spacing = new Vector2(40f, 24f);
            l3Grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 6; i++)
            {
                Color bCol = (i % 2 == 0) ? borderNeonColor : neonMagentaColor;
                GameObject lvlBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l3GridObj.transform, new Vector2(290f, 135f), bCol, "", Color.clear);
                lvlBtnObj.name = "TestLevelButton_" + i;

                // Level Number
                GameObject numObj = new GameObject("TestLevelNumText");
                numObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform numRect = numObj.AddComponent<RectTransform>();
                numRect.anchorMin = new Vector2(0f, 0.42f);
                numRect.anchorMax = new Vector2(1f, 0.95f);
                numRect.sizeDelta = Vector2.zero;

                Text numText = numObj.AddComponent<Text>();
                numText.font = defaultFont;
                numText.fontSize = 34;
                numText.fontStyle = FontStyle.Bold;
                numText.alignment = TextAnchor.MiddleCenter;
                numText.color = textGoldColor;

                Shadow numShadow = numObj.AddComponent<Shadow>();
                numShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                numShadow.effectDistance = new Vector2(2f, -2f);

                // Subtitle / Title Description
                GameObject subObj = new GameObject("TestLevelSubText");
                subObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0f, 0.08f);
                subRect.anchorMax = new Vector2(1f, 0.42f);
                subRect.sizeDelta = Vector2.zero;

                Text subText = subObj.AddComponent<Text>();
                subText.font = defaultFont;
                subText.fontSize = 14;
                subText.fontStyle = FontStyle.Bold;
                subText.alignment = TextAnchor.MiddleCenter;
                subText.color = borderNeonColor;

                Shadow subShadow = subObj.AddComponent<Shadow>();
                subShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                subShadow.effectDistance = new Vector2(1.5f, -1.5f);

                testLevelButtons.Add(lvlBtnObj);
            }

            // Back Button (\u25C0)
            GameObject l3BackBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l3Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            l3BackBtnObj.name = "L3BackButton";
            RectTransform l3BackRect = l3BackBtnObj.GetComponent<RectTransform>();
            l3BackRect.anchorMin = new Vector2(0.08f, 0.08f);
            l3BackRect.anchorMax = new Vector2(0.08f, 0.08f);
            l3BackRect.pivot = new Vector2(0f, 0.5f);
            l3BackBtnObj.GetComponent<Button>().onClick.AddListener(ShowCampaign);

            layer3Obj.SetActive(false);

            CreateShopUI();
        }


        private void SetTopLevelMenuVisible(bool visible)
        {
            if (logoObj != null) logoObj.SetActive(visible);
            if (topMenuObj != null) topMenuObj.SetActive(visible);
            if (bottomNavObj != null) bottomNavObj.SetActive(visible);
            if (layer1Obj != null) layer1Obj.SetActive(visible);
        }

        private void ShowLayer1()
        {
            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);

            SetTopLevelMenuVisible(true);
        }

        private void OnSphereCountSelected(int spheres)
        {
            selectedSphereCount = spheres;
            ShowLayer2();
        }

        private void ShowLayer2()
        {
            SetTopLevelMenuVisible(false);

            if (layer2Obj != null) layer2Obj.SetActive(true);
            if (layer3Obj != null) layer3Obj.SetActive(false);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);

            if (sphereIndicatorText != null)
            {
                sphereIndicatorText.text = $"SPHERES: {selectedSphereCount}   |   CAMPAIGN LEVELS";
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

                Text[] texts = btnObj.GetComponentsInChildren<Text>();
                Text numText = (texts.Length > 0) ? texts[0] : null;
                Text subText = (texts.Length > 1) ? texts[1] : null;

                if (configIndex < levelConfigs.Count)
                {
                    LevelConfig config = levelConfigs[configIndex];
                    btnObj.SetActive(true);

                    if (numText != null) numText.text = config.levelNumber.ToString();
                    if (subText != null) subText.text = $"LEVEL {config.levelNumber}";

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LaunchGame(config));
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

        private void ShowTestLevels()
        {
            SetTopLevelMenuVisible(false);

            if (testTabImg != null) testTabImg.color = new Color(0.12f, 0.08f, 0.22f, 0.95f);
            if (testTabBorder != null) testTabBorder.effectColor = borderNeonColor;

            if (campaignTabImg != null) campaignTabImg.color = new Color(0.04f, 0.02f, 0.08f, 0.8f);
            if (campaignTabBorder != null) campaignTabBorder.effectColor = borderNeonColor;

            if (layer2Obj != null) layer2Obj.SetActive(false);
            if (layer3Obj != null) layer3Obj.SetActive(true);
            if (layer4Obj != null) layer4Obj.SetActive(false);
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);

            RefreshTestLevelGrid();
        }

        private void RefreshTestLevelGrid()
        {
            if (testLevelIndicatorText != null)
            {
                testLevelIndicatorText.text = $"TEST LEVELS ({selectedSphereCount} " + (selectedSphereCount == 1 ? "SPHERE)" : "SPHERES)");
            }

            for (int i = 0; i < 6; i++)
            {
                GameObject btnObj = testLevelButtons[i];
                Button btn = btnObj.GetComponent<Button>();

                Text[] texts = btnObj.GetComponentsInChildren<Text>();
                Text numText = (texts.Length > 0) ? texts[0] : null;
                Text subText = (texts.Length > 1) ? texts[1] : null;

                if (i < testLevelConfigs.Count)
                {
                    LevelConfig config = testLevelConfigs[i];
                    btnObj.SetActive(true);

                    if (numText != null) numText.text = config.levelNumber.ToString();
                    if (subText != null) subText.text = !string.IsNullOrEmpty(config.levelName) ? config.levelName.ToUpper() : $"TEST {config.levelNumber}";

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LaunchGame(config));
                }
                else
                {
                    btnObj.SetActive(false);
                }
            }
        }

        private void LaunchGame(LevelConfig config)
        {
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

        private void CreateSettingsPopup()
        {
            settingsPopupObj = GlassUIFactory.CreateGlassmorphicPanel(canvasObj.transform, new Vector2(520f, 480f), borderNeonColor, Vector2.zero);
            settingsPopupObj.name = "SettingsPopup";

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(settingsPopupObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.86f);
            titleRect.anchorMax = new Vector2(1f, 0.98f);
            titleRect.sizeDelta = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = textGoldColor;
            titleText.text = "SETTINGS / STORE";

            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // Status Info
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(settingsPopupObj.transform, false);
            RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.05f, 0.74f);
            bodyRect.anchorMax = new Vector2(0.95f, 0.86f);
            bodyRect.sizeDelta = Vector2.zero;

            Text bodyText = bodyObj.AddComponent<Text>();
            bodyText.font = defaultFont;
            bodyText.fontSize = 16;
            bodyText.fontStyle = FontStyle.Bold;
            bodyText.alignment = TextAnchor.MiddleCenter;
            bodyText.color = Color.white;
            bodyText.text = "SFX: ON    |    MUSIC: ON    |    GRAPHICS: ULTRA";

            Shadow bodyShadow = bodyObj.AddComponent<Shadow>();
            bodyShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            bodyShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // 1. Remove Ads Button (Glassmorphic)
            GameObject removeAdsBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), textGoldColor, "", Color.clear);
            removeAdsBtnObj.name = "RemoveAdsButton";
            RectTransform removeAdsRect = removeAdsBtnObj.GetComponent<RectTransform>();
            removeAdsRect.anchorMin = new Vector2(0.5f, 0.58f);
            removeAdsRect.anchorMax = new Vector2(0.5f, 0.58f);

            removeAdsBtn = removeAdsBtnObj.GetComponent<Button>();
            removeAdsBtn.onClick.AddListener(() => {
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

            removeAdsText = removeAdsTextObj.AddComponent<Text>();
            removeAdsText.font = defaultFont;
            removeAdsText.fontSize = 18;
            removeAdsText.fontStyle = FontStyle.Bold;
            removeAdsText.alignment = TextAnchor.MiddleCenter;
            removeAdsText.color = Color.white;
            removeAdsText.text = "REMOVE ADS - $0.99";

            Shadow ratShadow = removeAdsTextObj.AddComponent<Shadow>();
            ratShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            ratShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // 2. Restore Purchases Button (Glassmorphic)
            GameObject restoreBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), borderNeonColor, "", Color.clear);
            restoreBtnObj.name = "RestoreButton";
            RectTransform restoreRect = restoreBtnObj.GetComponent<RectTransform>();
            restoreRect.anchorMin = new Vector2(0.5f, 0.42f);
            restoreRect.anchorMax = new Vector2(0.5f, 0.42f);

            Button restoreBtn = restoreBtnObj.GetComponent<Button>();
            restoreBtn.onClick.AddListener(() => {
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

            Text restoreTextVal = restoreTextObj.AddComponent<Text>();
            restoreTextVal.font = defaultFont;
            restoreTextVal.fontSize = 18;
            restoreTextVal.fontStyle = FontStyle.Bold;
            restoreTextVal.alignment = TextAnchor.MiddleCenter;
            restoreTextVal.color = Color.white;
            restoreTextVal.text = "RESTORE PURCHASES";

            Shadow restShadow = restoreTextObj.AddComponent<Shadow>();
            restShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            restShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // 3. Restore Default Settings Button (Glassmorphic)
            GameObject resetDefaultsBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(420f, 55f), neonMagentaColor, "", Color.clear);
            resetDefaultsBtnObj.name = "RestoreDefaultsButton";
            RectTransform resetDefaultsRect = resetDefaultsBtnObj.GetComponent<RectTransform>();
            resetDefaultsRect.anchorMin = new Vector2(0.5f, 0.26f);
            resetDefaultsRect.anchorMax = new Vector2(0.5f, 0.26f);

            Button resetDefaultsBtn = resetDefaultsBtnObj.GetComponent<Button>();
            resetDefaultsBtn.onClick.AddListener(RestoreDefaultSettings);

            GameObject resetTextObj = new GameObject("Text");
            resetTextObj.transform.SetParent(resetDefaultsBtnObj.transform, false);
            RectTransform resetTRect = resetTextObj.AddComponent<RectTransform>();
            resetTRect.anchorMin = Vector2.zero;
            resetTRect.anchorMax = Vector2.one;
            resetTRect.sizeDelta = Vector2.zero;

            Text resetTextVal = resetTextObj.AddComponent<Text>();
            resetTextVal.font = defaultFont;
            resetTextVal.fontSize = 18;
            resetTextVal.fontStyle = FontStyle.Bold;
            resetTextVal.alignment = TextAnchor.MiddleCenter;
            resetTextVal.color = new Color(1f, 0.85f, 0.85f);
            resetTextVal.text = "RESTORE DEFAULT SETTINGS";

            Shadow resetShadow = resetTextObj.AddComponent<Shadow>();
            resetShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            resetShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // 4. Close Button (Glassmorphic Icon-Only: \u2715)
            GameObject closeBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(140f, 48f), borderNeonColor, "\u2715", Color.white, 26);
            closeBtnObj.name = "CloseButton";
            RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0.08f);
            closeRect.anchorMax = new Vector2(0.5f, 0.08f);

            closeBtnObj.GetComponent<Button>().onClick.AddListener(CloseSettingsPopup);

            settingsPopupObj.SetActive(false); // Hidden by default
        }

        private void OpenSettingsPopup()
        {
            Debug.Log("[MainMenu] Opening Settings popup...");
            SetTopLevelMenuVisible(false);
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
            PlayerPrefs.Save();

            selectedSphereCount = 3;
            currentLevelPage = 0;

            UpdateSettingsUI();
            ShowLayer1();
            Debug.Log("[MainMenu] Default settings successfully restored.");
        }

        private void UpdateSettingsUI()
        {
            if (IAPManager.Instance != null && removeAdsText != null && removeAdsBtn != null)
            {
                bool adsRemoved = IAPManager.Instance.IsAdsRemoved();
                removeAdsText.text = adsRemoved ? "ADS REMOVED" : "REMOVE ADS - $0.99";
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

            GameObject l4Panel = GlassUIFactory.CreateGlassmorphicPanel(layer4Obj.transform, new Vector2(1240f, 680f), textGoldColor, new Vector2(0f, 20f));
            l4Panel.name = "L4_Panel";

            // Title indicator text
            GameObject indObj = new GameObject("L4_Indicator");
            indObj.transform.SetParent(l4Panel.transform, false);
            RectTransform indRect = indObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0.86f);
            indRect.anchorMax = new Vector2(1f, 0.98f);
            indRect.sizeDelta = Vector2.zero;

            shopTotalCoinsText = indObj.AddComponent<Text>();
            shopTotalCoinsText.font = defaultFont;
            shopTotalCoinsText.fontSize = 28;
            shopTotalCoinsText.fontStyle = FontStyle.Bold;
            shopTotalCoinsText.alignment = TextAnchor.MiddleCenter;
            shopTotalCoinsText.color = textGoldColor;
            shopTotalCoinsText.text = "SKIN SHOP   |   COINS: 0";

            Shadow indShadow = indObj.AddComponent<Shadow>();
            indShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            indShadow.effectDistance = new Vector2(2f, -2f);

            // Skin Buttons Grid
            GameObject l4GridObj = new GameObject("L4_Grid");
            l4GridObj.transform.SetParent(l4Panel.transform, false);
            RectTransform l4GridRect = l4GridObj.AddComponent<RectTransform>();
            l4GridRect.anchorMin = new Vector2(0.08f, 0.22f);
            l4GridRect.anchorMax = new Vector2(0.92f, 0.83f);
            l4GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l4Grid = l4GridObj.AddComponent<GridLayoutGroup>();
            l4Grid.cellSize = new Vector2(290f, 160f);
            l4Grid.spacing = new Vector2(40f, 24f);
            l4Grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 6; i++)
            {
                Color bCol = (i % 2 == 0) ? textGoldColor : borderNeonColor;
                GameObject skinBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4GridObj.transform, new Vector2(290f, 160f), bCol, "", Color.clear);
                skinBtnObj.name = "SkinButton_" + i;

                // Icon
                GameObject iconObj = new GameObject("SkinIconText");
                iconObj.transform.SetParent(skinBtnObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.45f);
                iconRect.anchorMax = new Vector2(1f, 0.90f);
                iconRect.sizeDelta = Vector2.zero;

                Text iconText = iconObj.AddComponent<Text>();
                iconText.font = defaultFont;
                iconText.fontSize = 50;
                iconText.fontStyle = FontStyle.Bold;
                iconText.alignment = TextAnchor.MiddleCenter;
                iconText.color = bCol;

                Shadow iconShadow = iconObj.AddComponent<Shadow>();
                iconShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                iconShadow.effectDistance = new Vector2(2f, -2f);

                // Skin Name
                GameObject numObj = new GameObject("SkinNameText");
                numObj.transform.SetParent(skinBtnObj.transform, false);
                RectTransform numRect = numObj.AddComponent<RectTransform>();
                numRect.anchorMin = new Vector2(0f, 0.25f);
                numRect.anchorMax = new Vector2(1f, 0.45f);
                numRect.sizeDelta = Vector2.zero;

                Text numText = numObj.AddComponent<Text>();
                numText.font = defaultFont;
                numText.fontSize = 20;
                numText.fontStyle = FontStyle.Bold;
                numText.alignment = TextAnchor.MiddleCenter;
                numText.color = Color.white;

                Shadow numShadow = numObj.AddComponent<Shadow>();
                numShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                numShadow.effectDistance = new Vector2(2f, -2f);

                // Subtitle (Price or Status)
                GameObject subObj = new GameObject("SkinSubText");
                subObj.transform.SetParent(skinBtnObj.transform, false);
                RectTransform subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0f, 0.05f);
                subRect.anchorMax = new Vector2(1f, 0.25f);
                subRect.sizeDelta = Vector2.zero;

                Text subText = subObj.AddComponent<Text>();
                subText.font = defaultFont;
                subText.fontSize = 16;
                subText.fontStyle = FontStyle.Bold;
                subText.alignment = TextAnchor.MiddleCenter;
                subText.color = textGoldColor;

                Shadow subShadow = subObj.AddComponent<Shadow>();
                subShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                subShadow.effectDistance = new Vector2(1.5f, -1.5f);

                shopButtons.Add(skinBtnObj);
            }

            // Back Button
            GameObject backBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            backBtnObj.name = "BackButton";
            RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.08f, 0.08f);
            backRect.anchorMax = new Vector2(0.08f, 0.08f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backBtnObj.GetComponent<Button>().onClick.AddListener(ShowLayer1);

            // Prev Button
            GameObject prevBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4Panel.transform, new Vector2(120f, 60f), textGoldColor, "\u25C0", Color.white, 32);
            prevBtnObj.name = "PrevButton";
            RectTransform prevRect = prevBtnObj.GetComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0.82f, 0.08f);
            prevRect.anchorMax = new Vector2(0.82f, 0.08f);
            prevBtnObj.GetComponent<Button>().onClick.AddListener(() => ChangeShopPage(-1));

            // Next Button
            GameObject nextBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(l4Panel.transform, new Vector2(120f, 60f), textGoldColor, "\u25B6", Color.white, 32);
            nextBtnObj.name = "NextButton";
            RectTransform nextRect = nextBtnObj.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.94f, 0.08f);
            nextRect.anchorMax = new Vector2(0.94f, 0.08f);
            nextBtnObj.GetComponent<Button>().onClick.AddListener(() => ChangeShopPage(1));

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
            RefreshShopGrid();
        }

        private void ChangeShopPage(int delta)
        {
            int targetPage = currentShopPage + delta;
            int totalPages = Mathf.CeilToInt(shopSkins.Length / 6f);
            
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
                shopTotalCoinsText.text = $"SKIN SHOP   |   COINS: {GameManager.Instance.TotalCoins}";
            }

            int startIndex = currentShopPage * 6;
            
            for (int i = 0; i < 6; i++)
            {
                int skinIndex = startIndex + i;
                GameObject btnObj = shopButtons[i];
                Button btn = btnObj.GetComponent<Button>();

                Text[] texts = btnObj.GetComponentsInChildren<Text>();
                Text iconText = (texts.Length > 0) ? texts[0] : null;
                Text nameText = (texts.Length > 1) ? texts[1] : null;
                Text subText = (texts.Length > 2) ? texts[2] : null;

                if (skinIndex < shopSkins.Length)
                {
                    SkinItem skin = shopSkins[skinIndex];
                    btnObj.SetActive(true);

                    if (iconText != null) iconText.text = skin.Icon;
                    if (nameText != null) nameText.text = skin.Name;

                    bool isUnlocked = GameManager.Instance.IsSkinUnlocked(skinIndex);
                    bool isEquipped = GameManager.Instance.EquippedSkin == skinIndex;

                    if (isEquipped)
                    {
                        if (subText != null)
                        {
                            subText.text = "EQUIPPED";
                            subText.color = borderNeonColor;
                        }
                        btn.interactable = false; // Disable if already equipped
                    }
                    else if (isUnlocked)
                    {
                        if (subText != null)
                        {
                            subText.text = "EQUIP";
                            subText.color = Color.white;
                        }
                        btn.interactable = true;
                    }
                    else
                    {
                        if (subText != null)
                        {
                            subText.text = $"BUY: {skin.Price}";
                            subText.color = (GameManager.Instance.TotalCoins >= skin.Price) ? textGoldColor : new Color(0.6f, 0.6f, 0.6f);
                        }
                        // Disable if they don't have enough coins, to prevent confusion when clicking does nothing
                        btn.interactable = (GameManager.Instance.TotalCoins >= skin.Price);
                    }

                    btn.onClick.RemoveAllListeners();
                    int capturedIndex = skinIndex; // ensure capture
                    btn.onClick.AddListener(() => OnSkinButtonClicked(capturedIndex));
                }
                else
                {
                    btnObj.SetActive(false);
                }
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
                int price = shopSkins[skinIndex].Price;
                if (GameManager.Instance.DeductCoins(price))
                {
                    GameManager.Instance.UnlockSkin(skinIndex);
                    GameManager.Instance.EquippedSkin = skinIndex; // Auto-equip on purchase
                }
            }

            RefreshShopGrid();
        }

        private void OnDestroy()
        {
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnPurchaseComplete -= UpdateSettingsUI;
                IAPManager.Instance.OnRestoreComplete -= UpdateSettingsUI;
            }
        }
    }
}
