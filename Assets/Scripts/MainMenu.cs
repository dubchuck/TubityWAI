using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace TubityWAI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Menu Styling")]
        public Color panelBackgroundColor = new Color(0.04f, 0.02f, 0.08f, 0.95f);
        public Color borderNeonColor = new Color(0f, 1f, 1f, 0.7f); // Neon Cyan
        public Color textGoldColor = new Color(1f, 0.85f, 0f);     // Neon Gold/Yellow

        // The default colors used for the visual ball icons inside the buttons
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

        // UI Layer Containers
        private GameObject canvasObj;
        private GameObject layer1Obj; // Sphere Selection
        private GameObject layer2Obj; // Level Selection

        // Text indicator in Layer 2
        private Text sphereIndicatorText;
        private List<GameObject> levelButtons = new List<GameObject>();

        private void Awake()
        {
            circleSprite = CreateCircleSprite();
            InitializeLevelConfigurations();
        }

        private void Start()
        {
            CreateEventSystem();
            CreateMenuUI();
            ShowLayer1();
        }

        private void InitializeLevelConfigurations()
        {
            // Progressive difficulty setup
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

        private void CreateEventSystem()
        {
            // Verify and instantiate EventSystem for click events using the New Input System Module
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

            // Find a valid font
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // ==========================================
            // LAYER 1: SPHERE SELECTION LAYER
            // ==========================================
            layer1Obj = new GameObject("Layer1_SphereSelection");
            layer1Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l1Rect = layer1Obj.AddComponent<RectTransform>();
            l1Rect.anchorMin = Vector2.zero;
            l1Rect.anchorMax = Vector2.one;
            l1Rect.sizeDelta = Vector2.zero;

            // Background panel
            GameObject l1Panel = new GameObject("L1_Panel");
            l1Panel.transform.SetParent(layer1Obj.transform, false);
            RectTransform l1PanelRect = l1Panel.AddComponent<RectTransform>();
            l1PanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            l1PanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            l1PanelRect.sizeDelta = new Vector2(1200f, 600f);

            Image l1Img = l1Panel.AddComponent<Image>();
            l1Img.color = panelBackgroundColor;

            Outline l1Border = l1Panel.AddComponent<Outline>();
            l1Border.effectColor = borderNeonColor;
            l1Border.effectDistance = new Vector2(3f, -3f);

            // Title Text
            GameObject titleObj = new GameObject("L1_Title");
            titleObj.transform.SetParent(l1Panel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.8f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 42;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = textGoldColor;
            titleText.text = "SELECT SPHERES COUNT";

            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = Color.black;
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // Grid Container for buttons
            GameObject l1GridObj = new GameObject("L1_Grid");
            l1GridObj.transform.SetParent(l1Panel.transform, false);
            RectTransform l1GridRect = l1GridObj.AddComponent<RectTransform>();
            l1GridRect.anchorMin = new Vector2(0.05f, 0.15f);
            l1GridRect.anchorMax = new Vector2(0.95f, 0.75f);
            l1GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l1Grid = l1GridObj.AddComponent<GridLayoutGroup>();
            l1Grid.cellSize = new Vector2(200f, 180f);
            l1Grid.spacing = new Vector2(18f, 0f);
            l1Grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            l1Grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            l1Grid.childAlignment = TextAnchor.MiddleCenter;

            // Spawn sphere selection buttons (1 to 5)
            for (int count = 1; count <= 5; count++)
            {
                int localCount = count;
                GameObject btnObj = new GameObject("SphereButton_" + count);
                btnObj.transform.SetParent(l1GridObj.transform, false);

                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(0.12f, 0.08f, 0.22f, 0.9f);

                Outline btnBorder = btnObj.AddComponent<Outline>();
                btnBorder.effectColor = new Color(1f, 0f, 0.5f, 0.6f); // Hot Pink border
                btnBorder.effectDistance = new Vector2(1.5f, -1.5f);

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = btnImg; // Required for interaction raycasts
                btn.onClick.AddListener(() => OnSphereCountSelected(localCount));

                // Button Text
                GameObject btnTextObj = new GameObject("Text");
                btnTextObj.transform.SetParent(btnObj.transform, false);
                RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
                btnTextRect.anchorMin = new Vector2(0f, 0.5f);
                btnTextRect.anchorMax = new Vector2(1f, 0.95f);
                btnTextRect.sizeDelta = Vector2.zero;

                Text btnText = btnTextObj.AddComponent<Text>();
                btnText.font = defaultFont;
                btnText.fontSize = 20;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;
                btnText.text = localCount + (localCount == 1 ? " SPHERE" : " SPHERES");

                // Button Ball Visuals Container
                GameObject ballsContainer = new GameObject("BallsContainer");
                ballsContainer.transform.SetParent(btnObj.transform, false);
                RectTransform bcRect = ballsContainer.AddComponent<RectTransform>();
                bcRect.anchorMin = new Vector2(0.05f, 0.05f);
                bcRect.anchorMax = new Vector2(0.95f, 0.45f);
                bcRect.sizeDelta = Vector2.zero;

                HorizontalLayoutGroup hlg = ballsContainer.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 6f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                // Add visual balls
                for (int i = 0; i < localCount; i++)
                {
                    GameObject ballObj = new GameObject("BallIcon");
                    ballObj.transform.SetParent(ballsContainer.transform, false);
                    RectTransform bRect = ballObj.AddComponent<RectTransform>();
                    bRect.sizeDelta = new Vector2(20f, 20f);

                    Image ballImg = ballObj.AddComponent<Image>();
                    ballImg.sprite = circleSprite;
                    ballImg.color = sphereColors[i % sphereColors.Length];
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

            // Background panel
            GameObject l2Panel = new GameObject("L2_Panel");
            l2Panel.transform.SetParent(layer2Obj.transform, false);
            RectTransform l2PanelRect = l2Panel.AddComponent<RectTransform>();
            l2PanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            l2PanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            l2PanelRect.sizeDelta = new Vector2(1200f, 700f);

            Image l2Img = l2Panel.AddComponent<Image>();
            l2Img.color = panelBackgroundColor;

            Outline l2Border = l2Panel.AddComponent<Outline>();
            l2Border.effectColor = borderNeonColor;
            l2Border.effectDistance = new Vector2(3f, -3f);

            // Title indicator text (Center Top)
            GameObject indObj = new GameObject("L2_Indicator");
            indObj.transform.SetParent(l2Panel.transform, false);
            RectTransform indRect = indObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0.82f);
            indRect.anchorMax = new Vector2(1f, 0.98f);
            indRect.sizeDelta = Vector2.zero;

            sphereIndicatorText = indObj.AddComponent<Text>();
            sphereIndicatorText.font = defaultFont;
            sphereIndicatorText.fontSize = 28;
            sphereIndicatorText.fontStyle = FontStyle.Bold;
            sphereIndicatorText.alignment = TextAnchor.MiddleCenter;
            sphereIndicatorText.color = textGoldColor;
            sphereIndicatorText.text = "SPHERES: 3";

            Shadow indShadow = indObj.AddComponent<Shadow>();
            indShadow.effectColor = Color.black;
            indShadow.effectDistance = new Vector2(2f, -2f);

            // Level Buttons Grid
            GameObject l2GridObj = new GameObject("L2_Grid");
            l2GridObj.transform.SetParent(l2Panel.transform, false);
            RectTransform l2GridRect = l2GridObj.AddComponent<RectTransform>();
            l2GridRect.anchorMin = new Vector2(0.1f, 0.22f);
            l2GridRect.anchorMax = new Vector2(0.9f, 0.8f);
            l2GridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup l2Grid = l2GridObj.AddComponent<GridLayoutGroup>();
            l2Grid.cellSize = new Vector2(280f, 150f);
            l2Grid.spacing = new Vector2(40f, 30f);
            l2Grid.childAlignment = TextAnchor.MiddleCenter;

            // Instantiate 6 placeholder buttons for the active page
            for (int i = 0; i < 6; i++)
            {
                GameObject lvlBtnObj = new GameObject("LevelButton_" + i);
                lvlBtnObj.transform.SetParent(l2GridObj.transform, false);

                Image lvlBtnImg = lvlBtnObj.AddComponent<Image>();
                lvlBtnImg.color = new Color(0.08f, 0.05f, 0.16f, 0.95f);

                Outline lvlBtnBorder = lvlBtnObj.AddComponent<Outline>();
                lvlBtnBorder.effectColor = new Color(0f, 0.8f, 1f, 0.6f);
                lvlBtnBorder.effectDistance = new Vector2(1.5f, -1.5f);

                Button lvlBtn = lvlBtnObj.AddComponent<Button>();
                lvlBtn.targetGraphic = lvlBtnImg; // Required for interaction raycasts
                
                // Text child
                GameObject lvlTextObj = new GameObject("Label");
                lvlTextObj.transform.SetParent(lvlBtnObj.transform, false);
                RectTransform lvlTextRect = lvlTextObj.AddComponent<RectTransform>();
                lvlTextRect.anchorMin = Vector2.zero;
                lvlTextRect.anchorMax = Vector2.one;
                lvlTextRect.sizeDelta = Vector2.zero;

                Text lvlText = lvlTextObj.AddComponent<Text>();
                lvlText.font = defaultFont;
                lvlText.fontSize = 22;
                lvlText.fontStyle = FontStyle.Bold;
                lvlText.alignment = TextAnchor.MiddleCenter;
                lvlText.color = Color.white;

                levelButtons.Add(lvlBtnObj);
            }

            // Back Button (Bottom Left)
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(l2Panel.transform, false);
            RectTransform backRect = backBtnObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.1f, 0.06f);
            backRect.anchorMax = new Vector2(0.1f, 0.06f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.anchoredPosition = Vector3.zero;
            backRect.sizeDelta = new Vector2(180f, 50f);

            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(0.15f, 0.03f, 0.03f, 0.9f); // Dark red backing

            Outline backBorder = backBtnObj.AddComponent<Outline>();
            backBorder.effectColor = new Color(1f, 0.2f, 0.2f, 0.6f);
            backBorder.effectDistance = new Vector2(1.5f, -1.5f);

            Button backBtn = backBtnObj.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.onClick.AddListener(ShowLayer1);

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform backTextRect = backTextObj.AddComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.sizeDelta = Vector2.zero;

            Text backText = backTextObj.AddComponent<Text>();
            backText.font = defaultFont;
            backText.fontSize = 18;
            backText.fontStyle = FontStyle.Bold;
            backText.alignment = TextAnchor.MiddleCenter;
            backText.color = new Color(1f, 0.8f, 0.8f);
            backText.text = "BACK";

            // Paginated Navigation Buttons (Bottom Right)
            // Previous Page Button
            GameObject prevBtnObj = new GameObject("PrevButton");
            prevBtnObj.transform.SetParent(l2Panel.transform, false);
            RectTransform prevRect = prevBtnObj.AddComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0.9f, 0.06f);
            prevRect.anchorMax = new Vector2(0.9f, 0.06f);
            prevRect.pivot = new Vector2(1f, 0.5f);
            prevRect.anchoredPosition = new Vector3(-200f, 0f, 0f);
            prevRect.sizeDelta = new Vector2(160f, 50f);

            Image prevImg = prevBtnObj.AddComponent<Image>();
            prevImg.color = new Color(0.04f, 0.12f, 0.08f, 0.9f);

            Outline prevBorder = prevBtnObj.AddComponent<Outline>();
            prevBorder.effectColor = new Color(0f, 1f, 0.5f, 0.5f);
            prevBorder.effectDistance = new Vector2(1.5f, -1.5f);

            Button prevBtn = prevBtnObj.AddComponent<Button>();
            prevBtn.targetGraphic = prevImg;
            prevBtn.onClick.AddListener(() => ChangePage(-1));

            GameObject prevTextObj = new GameObject("Text");
            prevTextObj.transform.SetParent(prevBtnObj.transform, false);
            RectTransform prevTextRect = prevTextObj.AddComponent<RectTransform>();
            prevTextRect.anchorMin = Vector2.zero;
            prevTextRect.anchorMax = Vector2.one;
            prevTextRect.sizeDelta = Vector2.zero;

            Text prevText = prevTextObj.AddComponent<Text>();
            prevText.font = defaultFont;
            prevText.fontSize = 16;
            prevText.fontStyle = FontStyle.Bold;
            prevText.alignment = TextAnchor.MiddleCenter;
            prevText.color = Color.white;
            prevText.text = "< PREV";

            // Next Page Button
            GameObject nextBtnObj = new GameObject("NextButton");
            nextBtnObj.transform.SetParent(l2Panel.transform, false);
            RectTransform nextRect = nextBtnObj.AddComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.9f, 0.06f);
            nextRect.anchorMax = new Vector2(0.9f, 0.06f);
            nextRect.pivot = new Vector2(1f, 0.5f);
            nextRect.anchoredPosition = Vector3.zero;
            nextRect.sizeDelta = new Vector2(160f, 50f);

            Image nextImg = nextBtnObj.AddComponent<Image>();
            nextImg.color = new Color(0.04f, 0.12f, 0.08f, 0.9f);

            Outline nextBorder = nextBtnObj.AddComponent<Outline>();
            nextBorder.effectColor = new Color(0f, 1f, 0.5f, 0.5f);
            nextBorder.effectDistance = new Vector2(1.5f, -1.5f);

            Button nextBtn = nextBtnObj.AddComponent<Button>();
            nextBtn.targetGraphic = nextImg;
            nextBtn.onClick.AddListener(() => ChangePage(1));

            GameObject nextTextObj = new GameObject("Text");
            nextTextObj.transform.SetParent(nextBtnObj.transform, false);
            RectTransform nextTextRect = nextTextObj.AddComponent<RectTransform>();
            nextTextRect.anchorMin = Vector2.zero;
            nextTextRect.anchorMax = Vector2.one;
            nextTextRect.sizeDelta = Vector2.zero;

            Text nextText = nextTextObj.AddComponent<Text>();
            nextText.font = defaultFont;
            nextText.fontSize = 16;
            nextText.fontStyle = FontStyle.Bold;
            nextText.alignment = TextAnchor.MiddleCenter;
            nextText.color = Color.white;
            nextText.text = "NEXT >";
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
                    // Antialiased circle edge
                    float alpha = Mathf.Clamp01(radius - dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void ShowLayer1()
        {
            layer1Obj.SetActive(true);
            layer2Obj.SetActive(false);
        }

        private void OnSphereCountSelected(int spheres)
        {
            selectedSphereCount = spheres;
            ShowLayer2();
        }

        private void ShowLayer2()
        {
            layer1Obj.SetActive(false);
            layer2Obj.SetActive(true);

            // Update center top indicator
            if (sphereIndicatorText != null)
            {
                sphereIndicatorText.text = $"PLAYING WITH: {selectedSphereCount} " + (selectedSphereCount == 1 ? "SPHERE" : "SPHERES");
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

                    // Configure text description (Progressive speed & obstacles info!)
                    label.text = $"LEVEL {config.levelNumber}\n" +
                                 $"<color=#00ffff>Speed: {config.forwardSpeed}</color>\n" +
                                 $"<color=#ff00ff>Hazards: {Mathf.RoundToInt(config.obstacleSpawnProbability * 100)}%</color>";

                    // Rebuild onClick listener
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LaunchGame(config));
                }
                else
                {
                    // Disable unused cells in grid
                    btnObj.SetActive(false);
                }
            }
        }

        private void LaunchGame(LevelConfig config)
        {
            Debug.Log($"[MainMenu] Launching level {config.levelNumber} with {selectedSphereCount} spheres...");
            
            // Disable menu Canvas
            canvasObj.SetActive(false);

            // Trigger GameSetup to run
            if (GameSetup.Instance != null)
            {
                GameSetup.Instance.StartGame(selectedSphereCount, config);
            }
        }
    }
}
