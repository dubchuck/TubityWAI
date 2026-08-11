using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TubityWAI
{
    public class GameHUD : MonoBehaviour
    {
        [Tooltip("The player controller component to track scoring and time.")]
        public PlayerController player;
        
        private Text scoreText;
        private Text coinText;
        private Text timeText;

        // Game Over Screen Elements
        private GameObject gameOverPanel;
        private Text finalStatsText;
        private Sprite roundedRectSprite;
        private Font defaultFont;
        
        private void Awake()
        {
            roundedRectSprite = CreateRoundedRectSprite(128, 128, 24);
        }

        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            
            CreateUI();
            CreateEventSystem();
        }
        
        private void CreateUI()
        {
            // 1. Create Canvas GameObject
            GameObject canvasObj = new GameObject("HUDCanvas");
            canvasObj.transform.SetParent(this.transform, false);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 2. Create Top HUD Glassmorphic Panel (320x130)
            GameObject panelObj = CreateGlassmorphicPanel(canvasObj.transform, new Vector2(320f, 130f), new Color(0f, 1f, 1f, 0.85f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector3(0f, -30f, 0f));
            panelObj.name = "HUDPanel";
            
            // 3. Create SCORE text (Top Row)
            GameObject scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform scoreRect = scoreTextObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 0.66f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 0.5f);
            scoreRect.anchoredPosition = new Vector3(0f, -3f, 0f);
            scoreRect.sizeDelta = Vector2.zero;
            
            scoreText = scoreTextObj.AddComponent<Text>();
            scoreText.font = defaultFont;
            scoreText.fontSize = 20;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = new Color(0f, 1f, 1f); // Neon Cyan
            
            Shadow scoreShadow = scoreTextObj.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            scoreShadow.effectDistance = new Vector2(2f, -2f);
            
            // 4. Create COINS text (Middle Row)
            GameObject coinTextObj = new GameObject("CoinText");
            coinTextObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform coinRect = coinTextObj.AddComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0f, 0.33f);
            coinRect.anchorMax = new Vector2(1f, 0.66f);
            coinRect.pivot = new Vector2(0.5f, 0.5f);
            coinRect.anchoredPosition = Vector3.zero;
            coinRect.sizeDelta = Vector2.zero;
            
            coinText = coinTextObj.AddComponent<Text>();
            coinText.font = defaultFont;
            coinText.fontSize = 20;
            coinText.fontStyle = FontStyle.Bold;
            coinText.alignment = TextAnchor.MiddleCenter;
            coinText.color = new Color(1f, 0.85f, 0f); // Synthwave Gold
            
            Shadow coinShadow = coinTextObj.AddComponent<Shadow>();
            coinShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            coinShadow.effectDistance = new Vector2(2f, -2f);
            
            // 5. Create TIME text (Bottom Row)
            GameObject timeTextObj = new GameObject("TimeText");
            timeTextObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform timeRect = timeTextObj.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0f, 0f);
            timeRect.anchorMax = new Vector2(1f, 0.33f);
            timeRect.pivot = new Vector2(0.5f, 0.5f);
            timeRect.anchoredPosition = new Vector3(0f, 3f, 0f);
            timeRect.sizeDelta = Vector2.zero;
            
            timeText = timeTextObj.AddComponent<Text>();
            timeText.font = defaultFont;
            timeText.fontSize = 18;
            timeText.fontStyle = FontStyle.Bold;
            timeText.alignment = TextAnchor.MiddleCenter;
            timeText.color = new Color(1f, 0.4f, 0f); // Neon Orange
            
            Shadow timeShadow = timeTextObj.AddComponent<Shadow>();
            timeShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            timeShadow.effectDistance = new Vector2(2f, -2f);

            // 6. Create GAME OVER Panel Overlay (disabled by default)
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasObj.transform, false);
            
            RectTransform goPanelRect = gameOverPanel.AddComponent<RectTransform>();
            goPanelRect.anchorMin = Vector2.zero; // Cover entire screen
            goPanelRect.anchorMax = Vector2.one;
            goPanelRect.pivot = new Vector2(0.5f, 0.5f);
            goPanelRect.anchoredPosition = Vector3.zero;
            goPanelRect.sizeDelta = Vector2.zero;

            // Full-screen backdrop fade (deep dark synthwave translucent obsidian)
            Image goPanelImage = gameOverPanel.AddComponent<Image>();
            goPanelImage.color = new Color(0.02f, 0.01f, 0.04f, 0.88f);

            // Centered 5-layer Glassmorphic Game Over Card (480x340)
            GameObject cardObj = CreateGlassmorphicPanel(gameOverPanel.transform, new Vector2(480f, 340f), new Color(1f, 0f, 0.2f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector3.zero);
            cardObj.name = "CardPanel";

            // Title "GAME OVER"
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.72f);
            titleRect.anchorMax = new Vector2(1f, 0.98f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector3(0f, -5f, 0f);
            titleRect.sizeDelta = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 40;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0f, 0.2f); // Hazard Hot Pink/Red

            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            titleShadow.effectDistance = new Vector2(2.5f, -2.5f);
            titleText.text = "GAME OVER";

            // Stats breakdown
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(cardObj.transform, false);

            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.05f, 0.36f);
            statsRect.anchorMax = new Vector2(0.95f, 0.72f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            statsRect.anchoredPosition = Vector3.zero;
            statsRect.sizeDelta = Vector2.zero;

            finalStatsText = statsObj.AddComponent<Text>();
            finalStatsText.font = defaultFont;
            finalStatsText.fontSize = 20;
            finalStatsText.fontStyle = FontStyle.Bold;
            finalStatsText.alignment = TextAnchor.MiddleCenter;
            finalStatsText.color = new Color(0.9f, 0.9f, 1f);
            
            Shadow statsShadow = statsObj.AddComponent<Shadow>();
            statsShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            statsShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // --- 1. REPLAY BUTTON (Left) ---
            GameObject replayBtnObj = CreateGlassmorphicIconButton(cardObj.transform, new Vector2(170f, 55f), new Color(0f, 1f, 0.6f, 0.85f), "REPLAY", Color.white, 18);
            replayBtnObj.name = "ReplayButton";

            RectTransform replayBtnRect = replayBtnObj.GetComponent<RectTransform>();
            replayBtnRect.anchorMin = new Vector2(0.5f, 0.18f);
            replayBtnRect.anchorMax = new Vector2(0.5f, 0.18f);
            replayBtnRect.pivot = new Vector2(0.5f, 0.5f);
            replayBtnRect.anchoredPosition = new Vector3(-105f, 0f, 0f);

            replayBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerReplay();
                }
            });

            // --- 2. MENU BUTTON (Right) ---
            GameObject menuBtnObj = CreateGlassmorphicIconButton(cardObj.transform, new Vector2(170f, 55f), new Color(1f, 0f, 0.3f, 0.85f), "MAIN MENU", Color.white, 18);
            menuBtnObj.name = "MenuButton";

            RectTransform menuBtnRect = menuBtnObj.GetComponent<RectTransform>();
            menuBtnRect.anchorMin = new Vector2(0.5f, 0.18f);
            menuBtnRect.anchorMax = new Vector2(0.5f, 0.18f);
            menuBtnRect.pivot = new Vector2(0.5f, 0.5f);
            menuBtnRect.anchoredPosition = new Vector3(105f, 0f, 0f);

            menuBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMainMenu();
                }
            });

            // Keep the Game Over Screen hidden initially
            gameOverPanel.SetActive(false);
        }

        private GameObject CreateGlassmorphicPanel(Transform parent, Vector2 size, Color neonBorderColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
            GameObject panelObj = new GameObject("GlassPanel");
            panelObj.transform.SetParent(parent, false);
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            // Layer 1: Base Glass Fill
            Image bgImg = panelObj.AddComponent<Image>();
            bgImg.sprite = roundedRectSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.04f, 0.02f, 0.08f, 0.95f);

            // Layer 2: Neon Rim
            Outline rim = panelObj.AddComponent<Outline>();
            rim.effectColor = neonBorderColor;
            rim.effectDistance = new Vector2(2.5f, -2.5f);

            // Layer 3: Ambient Glow
            Shadow glowShadow = panelObj.AddComponent<Shadow>();
            glowShadow.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.45f);
            glowShadow.effectDistance = new Vector2(-2f, 2f);

            // Layer 4: Specular Highlight Overlay (Top gradient reflection)
            GameObject highlight = new GameObject("GlassHighlight");
            highlight.transform.SetParent(panelObj.transform, false);
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.01f, 0.55f);
            hlRect.anchorMax = new Vector2(0.99f, 0.98f);
            hlRect.sizeDelta = Vector2.zero;

            Image hlImg = highlight.AddComponent<Image>();
            hlImg.sprite = roundedRectSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.18f);

            return panelObj;
        }

        private GameObject CreateGlassmorphicIconButton(Transform parent, Vector2 size, Color neonBorderColor, string labelText, Color textColor, int fontSize = 18)
        {
            GameObject btnObj = new GameObject("GlassIconButton");
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            // Layer 1: Base Glass Fill
            Image bgImg = btnObj.AddComponent<Image>();
            bgImg.sprite = roundedRectSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.04f, 0.08f, 0.20f, 0.85f);

            // Layer 2: Neon Rim
            Outline rim = btnObj.AddComponent<Outline>();
            rim.effectColor = neonBorderColor;
            rim.effectDistance = new Vector2(2.5f, -2.5f);

            // Layer 3: Ambient Glow
            Shadow glowShadow = btnObj.AddComponent<Shadow>();
            glowShadow.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.45f);
            glowShadow.effectDistance = new Vector2(-2f, 2f);

            // Layer 4: Specular Highlight Overlay
            GameObject highlight = new GameObject("GlassHighlight");
            highlight.transform.SetParent(btnObj.transform, false);
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.02f, 0.50f);
            hlRect.anchorMax = new Vector2(0.98f, 0.96f);
            hlRect.sizeDelta = Vector2.zero;

            Image hlImg = highlight.AddComponent<Image>();
            hlImg.sprite = roundedRectSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.18f);

            // Layer 5: Text / Icon Content
            if (!string.IsNullOrEmpty(labelText))
            {
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                Text textVal = textObj.AddComponent<Text>();
                textVal.font = defaultFont;
                textVal.fontSize = fontSize;
                textVal.fontStyle = FontStyle.Bold;
                textVal.alignment = TextAnchor.MiddleCenter;
                textVal.color = textColor;
                textVal.text = labelText;

                Shadow textShadow = textObj.AddComponent<Shadow>();
                textShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                textShadow.effectDistance = new Vector2(2f, -2f);
            }

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.35f, 1.35f, 1.45f, 1f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.1f;
            btn.colors = cb;

            return btnObj;
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

        private void CreateEventSystem()
        {
            // Verify and instantiate EventSystem for click events
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.transform.SetParent(this.transform);
                eventSystemObj.AddComponent<EventSystem>();
                
                // Use the New Input System's UI Input Module to avoid StandaloneInputModule legacy input system conflicts
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        public void ShowGameOverScreen()
        {
            if (gameOverPanel == null) return;

            // Update stats message
            if (finalStatsText != null && player != null)
            {
                finalStatsText.text = $"FINAL SCORE: {player.Score.ToString("D3")}\nCOINS COLLECTED: {player.Coins.ToString("D3")}";
            }

            gameOverPanel.SetActive(true);
        }
        
        private void Update()
        {
            if (player == null || scoreText == null || coinText == null || timeText == null) return;
            
            // Only update active UI stats if game is not over
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            // Format score: e.g. SCORE: 007
            scoreText.text = "SCORE: " + player.Score.ToString("D3");
            
            // Format coins: e.g. COINS: 015
            coinText.text = "COINS: " + player.Coins.ToString("D3");
            
            // Format time: e.g. TIME: 01:23
            int minutes = Mathf.FloorToInt(player.TimeElapsed / 60f);
            int seconds = Mathf.FloorToInt(player.TimeElapsed % 60f);
            timeText.text = string.Format("TIME: {0:00}:{1:00}", minutes, seconds);
        }
    }
}
