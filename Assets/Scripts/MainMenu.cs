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
        private Sprite circleSprite;
        private Sprite roundedRectSprite;
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
            circleSprite = CreateCircleSprite();
            roundedRectSprite = CreateRoundedRectSprite(128, 128, 24);
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
            }

            // Create Settings Popup
            CreateSettingsPopup();

            // =========================================================================
            // BOTTOM NAVIGATION BAR (PLAY, STORE, SETTINGS, QUIT - Icon-Only from iPadPro129_1.png)
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
            GameObject playBtnObj = CreateGlassmorphicIconButton(bottomNavObj.transform, new Vector2(250f, 120f), borderNeonColor, "\u25B6\u25B6", new Color(0f, 1f, 1f), 60);
            playBtnObj.name = "NavBtn_Play";
            playBtnObj.GetComponent<Button>().onClick.AddListener(() => {
                LevelConfig firstLvl = (levelConfigs.Count > 0) ? levelConfigs[0] : null;
                if (firstLvl != null) LaunchGame(firstLvl);
            });

            // 3. SETTINGS BUTTON (Gear Icon in Neon Cyan Rim)
            GameObject settingsBtnObj = CreateGlassmorphicIconButton(bottomNavObj.transform, new Vector2(180f, 120f), borderNeonColor, "\u2699", new Color(0f, 1f, 1f), 56);
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

            GameObject l1Panel = new GameObject("L1_Panel");
            l1Panel.transform.SetParent(layer1Obj.transform, false);
            RectTransform l1PanelRect = l1Panel.AddComponent<RectTransform>();
            l1PanelRect.anchorMin = new Vector2(0.5f, 0.42f);
            l1PanelRect.anchorMax = new Vector2(0.5f, 0.42f);
            l1PanelRect.pivot = new Vector2(0.5f, 0.5f);
            l1PanelRect.sizeDelta = new Vector2(1200f, 180f);

            HorizontalLayoutGroup l1Hlg = l1Panel.AddComponent<HorizontalLayoutGroup>();
            l1Hlg.spacing = 24f;
            l1Hlg.childAlignment = TextAnchor.MiddleCenter;
            l1Hlg.childControlWidth = false;
            l1Hlg.childControlHeight = false;

            // Spawn sphere selection glass buttons (1 to 5) - NO TEXT
            for (int count = 1; count <= 5; count++)
            {
                int localCount = count;
                Color borderCol = (count % 2 == 1) ? borderNeonColor : neonMagentaColor;
                GameObject btnObj = CreateGlassmorphicIconButton(l1Panel.transform, new Vector2(210f, 140f), borderCol, "", Color.clear);
                btnObj.name = "SphereButton_" + count;

                btnObj.GetComponent<Button>().onClick.AddListener(() => OnSphereCountSelected(localCount));

                // Container holding glowing ball icons
                GameObject ballsContainer = new GameObject("BallsContainer");
                ballsContainer.transform.SetParent(btnObj.transform, false);
                RectTransform bcRect = ballsContainer.AddComponent<RectTransform>();
                bcRect.anchorMin = Vector2.zero;
                bcRect.anchorMax = Vector2.one;
                bcRect.sizeDelta = Vector2.zero;

                HorizontalLayoutGroup hlg = ballsContainer.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 8f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                for (int i = 0; i < localCount; i++)
                {
                    GameObject ballObj = new GameObject("BallIcon");
                    ballObj.transform.SetParent(ballsContainer.transform, false);
                    RectTransform bRect = ballObj.AddComponent<RectTransform>();
                    bRect.sizeDelta = new Vector2(28f, 28f);

                    Image ballImg = ballObj.AddComponent<Image>();
                    ballImg.sprite = circleSprite;
                    ballImg.color = sphereColors[i % sphereColors.Length];

                    Outline ballGlow = ballObj.AddComponent<Outline>();
                    ballGlow.effectColor = new Color(1f, 1f, 1f, 0.7f);
                    ballGlow.effectDistance = new Vector2(1f, -1f);
                }
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

            GameObject l2Panel = new GameObject("L2_Panel");
            l2Panel.transform.SetParent(layer2Obj.transform, false);
            RectTransform l2PanelRect = l2Panel.AddComponent<RectTransform>();
            l2PanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            l2PanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            l2PanelRect.sizeDelta = new Vector2(1200f, 620f);
            l2PanelRect.anchoredPosition = new Vector3(0f, 40f, 0f);

            Image l2Img = l2Panel.AddComponent<Image>();
            l2Img.sprite = roundedRectSprite;
            l2Img.type = Image.Type.Sliced;
            l2Img.color = panelBackgroundColor;

            Outline l2Border = l2Panel.AddComponent<Outline>();
            l2Border.effectColor = borderNeonColor;
            l2Border.effectDistance = new Vector2(3f, -3f);

            // Title indicator text
            GameObject indObj = new GameObject("L2_Indicator");
            indObj.transform.SetParent(l2Panel.transform, false);
            RectTransform indRect = indObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0.84f);
            indRect.anchorMax = new Vector2(1f, 0.98f);
            indRect.sizeDelta = Vector2.zero;

            sphereIndicatorText = indObj.AddComponent<Text>();
            sphereIndicatorText.font = defaultFont;
            sphereIndicatorText.fontSize = 28;
            sphereIndicatorText.fontStyle = FontStyle.Bold;
            sphereIndicatorText.alignment = TextAnchor.MiddleCenter;
            sphereIndicatorText.color = textGoldColor;
            sphereIndicatorText.text = "SELECT LEVEL";

            // Level Buttons Grid
            GameObject l2GridObj = new GameObject("L2_Grid");
            l2GridObj.transform.SetParent(l2Panel.transform, false);
            RectTransform l2GridRect = l2GridObj.AddComponent<RectTransform>();
            l2GridRect.anchorMin = new Vector2(0.1f, 0.22f);
            l2GridRect.anchorMax = new Vector2(0.9f, 0.82f);
            l2GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l2Grid = l2GridObj.AddComponent<GridLayoutGroup>();
            l2Grid.cellSize = new Vector2(280f, 130f);
            l2Grid.spacing = new Vector2(40f, 24f);
            l2Grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 6; i++)
            {
                Color bCol = (i % 2 == 0) ? borderNeonColor : neonMagentaColor;
                GameObject lvlBtnObj = CreateGlassmorphicIconButton(l2GridObj.transform, new Vector2(280f, 130f), bCol, (i + 1).ToString(), Color.white, 48);
                lvlBtnObj.name = "LevelButton_" + i;
                levelButtons.Add(lvlBtnObj);
            }

            // Back Button (Bottom Left, Icon-Only: \u25C0)
            GameObject backBtnObj = CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            backBtnObj.name = "BackButton";
            RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.1f, 0.08f);
            backRect.anchorMax = new Vector2(0.1f, 0.08f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backBtnObj.GetComponent<Button>().onClick.AddListener(ShowLayer1);

            // Previous Page Button (\u25C0)
            GameObject prevBtnObj = CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25C0", Color.white, 32);
            prevBtnObj.name = "PrevButton";
            RectTransform prevRect = prevBtnObj.GetComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0.82f, 0.08f);
            prevRect.anchorMax = new Vector2(0.82f, 0.08f);
            prevBtnObj.GetComponent<Button>().onClick.AddListener(() => ChangePage(-1));

            // Next Page Button (\u25B6)
            GameObject nextBtnObj = CreateGlassmorphicIconButton(l2Panel.transform, new Vector2(120f, 60f), borderNeonColor, "\u25B6", Color.white, 32);
            nextBtnObj.name = "NextButton";
            RectTransform nextRect = nextBtnObj.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.95f, 0.08f);
            nextRect.anchorMax = new Vector2(0.95f, 0.08f);
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
            GameObject campBtnObj = CreateGlassmorphicIconButton(topMenuObj.transform, new Vector2(350f, 55f), borderNeonColor, "CAMPAIGN", Color.white, 20);
            campaignTabImg = campBtnObj.GetComponent<Image>();
            campaignTabBorder = campBtnObj.GetComponent<Outline>();
            campBtnObj.GetComponent<Button>().onClick.AddListener(ShowCampaign);

            // Test Levels Tab Button
            GameObject testBtnObj = CreateGlassmorphicIconButton(topMenuObj.transform, new Vector2(350f, 55f), neonMagentaColor, "TEST LEVELS", Color.white, 20);
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

            GameObject l3Panel = new GameObject("L3_Panel");
            l3Panel.transform.SetParent(layer3Obj.transform, false);
            RectTransform l3PanelRect = l3Panel.AddComponent<RectTransform>();
            l3PanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            l3PanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            l3PanelRect.sizeDelta = new Vector2(1200f, 620f);
            l3PanelRect.anchoredPosition = new Vector3(0f, 40f, 0f);

            Image l3Img = l3Panel.AddComponent<Image>();
            l3Img.sprite = roundedRectSprite;
            l3Img.type = Image.Type.Sliced;
            l3Img.color = panelBackgroundColor;

            Outline l3Border = l3Panel.AddComponent<Outline>();
            l3Border.effectColor = borderNeonColor;
            l3Border.effectDistance = new Vector2(3f, -3f);

            // Indicator
            GameObject l3IndObj = new GameObject("L3_Indicator");
            l3IndObj.transform.SetParent(l3Panel.transform, false);
            RectTransform l3IndRect = l3IndObj.AddComponent<RectTransform>();
            l3IndRect.anchorMin = new Vector2(0f, 0.84f);
            l3IndRect.anchorMax = new Vector2(1f, 0.98f);
            l3IndRect.sizeDelta = Vector2.zero;

            testLevelIndicatorText = l3IndObj.AddComponent<Text>();
            testLevelIndicatorText.font = defaultFont;
            testLevelIndicatorText.fontSize = 28;
            testLevelIndicatorText.fontStyle = FontStyle.Bold;
            testLevelIndicatorText.alignment = TextAnchor.MiddleCenter;
            testLevelIndicatorText.color = textGoldColor;
            testLevelIndicatorText.text = "SELECT TEST LEVEL";

            // Test Level Grid
            GameObject l3GridObj = new GameObject("L3_Grid");
            l3GridObj.transform.SetParent(l3Panel.transform, false);
            RectTransform l3GridRect = l3GridObj.AddComponent<RectTransform>();
            l3GridRect.anchorMin = new Vector2(0.1f, 0.22f);
            l3GridRect.anchorMax = new Vector2(0.9f, 0.82f);
            l3GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l3Grid = l3GridObj.AddComponent<GridLayoutGroup>();
            l3Grid.cellSize = new Vector2(280f, 130f);
            l3Grid.spacing = new Vector2(40f, 24f);
            l3Grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 6; i++)
            {
                Color bCol = (i % 2 == 0) ? borderNeonColor : neonMagentaColor;
                GameObject lvlBtnObj = CreateGlassmorphicIconButton(l3GridObj.transform, new Vector2(280f, 130f), bCol, (101 + i).ToString(), Color.white, 44);
                lvlBtnObj.name = "TestLevelButton_" + i;
                testLevelButtons.Add(lvlBtnObj);
            }

            // Back Button (\u25C0)
            GameObject l3BackBtnObj = CreateGlassmorphicIconButton(l3Panel.transform, new Vector2(140f, 60f), neonMagentaColor, "\u25C0", Color.white, 32);
            l3BackBtnObj.name = "L3BackButton";
            RectTransform l3BackRect = l3BackBtnObj.GetComponent<RectTransform>();
            l3BackRect.anchorMin = new Vector2(0.1f, 0.08f);
            l3BackRect.anchorMax = new Vector2(0.1f, 0.08f);
            l3BackRect.pivot = new Vector2(0f, 0.5f);
            l3BackBtnObj.GetComponent<Button>().onClick.AddListener(ShowCampaign);

            layer3Obj.SetActive(false);
        }

        private GameObject CreateGlassmorphicIconButton(Transform parent, Vector2 size, Color neonBorderColor, string iconUnicode, Color iconColor, int fontSize = 56)
        {
            GameObject btnObj = new GameObject("GlassIconButton");
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image bgImg = btnObj.AddComponent<Image>();
            bgImg.sprite = roundedRectSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.04f, 0.08f, 0.20f, 0.85f); // Deep translucent glass

            Outline rim = btnObj.AddComponent<Outline>();
            rim.effectColor = neonBorderColor;
            rim.effectDistance = new Vector2(2.5f, -2.5f);

            Shadow glowShadow = btnObj.AddComponent<Shadow>();
            glowShadow.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.45f);
            glowShadow.effectDistance = new Vector2(-2f, 2f);

            // Specular Highlight Overlay (Top gradient)
            GameObject highlight = new GameObject("GlassHighlight");
            highlight.transform.SetParent(btnObj.transform, false);
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.05f, 0.52f);
            hlRect.anchorMax = new Vector2(0.95f, 0.94f);
            hlRect.sizeDelta = Vector2.zero;

            Image hlImg = highlight.AddComponent<Image>();
            hlImg.sprite = roundedRectSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.18f); // Specular highlight reflection

            if (!string.IsNullOrEmpty(iconUnicode))
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = Vector2.zero;

                Text iconText = iconObj.AddComponent<Text>();
                iconText.font = defaultFont;
                iconText.fontSize = fontSize;
                iconText.alignment = TextAnchor.MiddleCenter;
                iconText.color = iconColor;
                iconText.text = iconUnicode;

                Shadow iconShadow = iconObj.AddComponent<Shadow>();
                iconShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                iconShadow.effectDistance = new Vector2(2f, -2f);
            }

            btnObj.AddComponent<Button>().targetGraphic = bgImg;
            return btnObj;
        }

        private Sprite CreateCircleSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRoundedRectSprite(int width = 128, int height = 128, int cornerRadius = 24)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            float r = cornerRadius;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = (x < r) ? r - x : (x > width - 1 - r) ? x - (width - 1 - r) : 0f;
                    float cy = (y < r) ? r - y : (y > height - 1 - r) ? y - (height - 1 - r) : 0f;
                    float dist = Mathf.Sqrt(cx * cx + cy * cy);

                    float alpha = Mathf.Clamp01(r - dist + 0.5f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            Vector4 border = new Vector4(r, r, r, r);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
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
            if (settingsPopupObj != null) settingsPopupObj.SetActive(false);

            if (sphereIndicatorText != null)
            {
                sphereIndicatorText.text = $"SPHERES: {selectedSphereCount}";
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
                Text label = btnObj.GetComponentInChildren<Text>();

                if (configIndex < levelConfigs.Count)
                {
                    LevelConfig config = levelConfigs[configIndex];
                    btnObj.SetActive(true);

                    if (label != null)
                    {
                        label.text = config.levelNumber.ToString();
                    }

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
                Text label = btnObj.GetComponentInChildren<Text>();

                if (i < testLevelConfigs.Count)
                {
                    LevelConfig config = testLevelConfigs[i];
                    btnObj.SetActive(true);

                    if (label != null)
                    {
                        label.text = config.levelNumber.ToString();
                    }

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

        private void CreateSettingsPopup()
        {
            settingsPopupObj = new GameObject("SettingsPopup");
            settingsPopupObj.transform.SetParent(canvasObj.transform, false);
            RectTransform popupRect = settingsPopupObj.AddComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(480f, 440f);
            popupRect.anchoredPosition = Vector2.zero;

            Image bg = settingsPopupObj.AddComponent<Image>();
            bg.sprite = roundedRectSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.04f, 0.02f, 0.08f, 0.98f);

            Outline border = settingsPopupObj.AddComponent<Outline>();
            border.effectColor = borderNeonColor;
            border.effectDistance = new Vector2(3f, -3f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(settingsPopupObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.85f);
            titleRect.anchorMax = new Vector2(1f, 0.96f);
            titleRect.sizeDelta = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = textGoldColor;
            titleText.text = "SETTINGS / STORE";

            // Status Info
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(settingsPopupObj.transform, false);
            RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.05f, 0.72f);
            bodyRect.anchorMax = new Vector2(0.95f, 0.84f);
            bodyRect.sizeDelta = Vector2.zero;

            Text bodyText = bodyObj.AddComponent<Text>();
            bodyText.font = defaultFont;
            bodyText.fontSize = 16;
            bodyText.alignment = TextAnchor.MiddleCenter;
            bodyText.color = Color.white;
            bodyText.text = "SFX: ON    |    MUSIC: ON    |    GRAPHICS: ULTRA";

            // 1. Remove Ads Button (Glassmorphic)
            GameObject removeAdsBtnObj = CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(380f, 50f), textGoldColor, "", Color.clear);
            removeAdsBtnObj.name = "RemoveAdsButton";
            RectTransform removeAdsRect = removeAdsBtnObj.GetComponent<RectTransform>();
            removeAdsRect.anchorMin = new Vector2(0.5f, 0.56f);
            removeAdsRect.anchorMax = new Vector2(0.5f, 0.56f);

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

            // 2. Restore Purchases Button (Glassmorphic)
            GameObject restoreBtnObj = CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(380f, 50f), borderNeonColor, "", Color.clear);
            restoreBtnObj.name = "RestoreButton";
            RectTransform restoreRect = restoreBtnObj.GetComponent<RectTransform>();
            restoreRect.anchorMin = new Vector2(0.5f, 0.40f);
            restoreRect.anchorMax = new Vector2(0.5f, 0.40f);

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

            // 3. Restore Default Settings Button (Glassmorphic)
            GameObject resetDefaultsBtnObj = CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(380f, 50f), neonMagentaColor, "", Color.clear);
            resetDefaultsBtnObj.name = "RestoreDefaultsButton";
            RectTransform resetDefaultsRect = resetDefaultsBtnObj.GetComponent<RectTransform>();
            resetDefaultsRect.anchorMin = new Vector2(0.5f, 0.24f);
            resetDefaultsRect.anchorMax = new Vector2(0.5f, 0.24f);

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

            // 4. Close Button (Glassmorphic Icon-Only: \u2715)
            GameObject closeBtnObj = CreateGlassmorphicIconButton(settingsPopupObj.transform, new Vector2(120f, 45f), borderNeonColor, "\u2715", Color.white, 26);
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
