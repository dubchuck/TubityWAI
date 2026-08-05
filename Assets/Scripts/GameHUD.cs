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
        
        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
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
            
            // 2. Create Panel for the Glassmorphic HUD Card
            GameObject panelObj = new GameObject("HUDPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f); // Anchor to Top-center
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector3(0f, -30f, 0f);
            panelRect.sizeDelta = new Vector2(300f, 120f);
            
            // Background panel color (highly transparent dark purple/black)
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.01f, 0.06f, 0.75f);
            
            // Neon Cyan border outline
            Outline border = panelObj.AddComponent<Outline>();
            border.effectColor = new Color(0f, 1f, 1f, 0.6f);
            border.effectDistance = new Vector2(2f, -2f);
            
            // Discover a valid built-in system font
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            
            // 3. Create SCORE text (Top Row)
            GameObject scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform scoreRect = scoreTextObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 0.66f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 0.5f);
            scoreRect.anchoredPosition = new Vector3(0f, -3f, 0f);
            scoreRect.sizeDelta = new Vector2(0f, 0f);
            
            scoreText = scoreTextObj.AddComponent<Text>();
            scoreText.font = defaultFont;
            scoreText.fontSize = 20;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = new Color(0f, 1f, 1f); // Neon Cyan
            
            Shadow scoreShadow = scoreTextObj.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            scoreShadow.effectDistance = new Vector2(1.5f, -1.5f);
            
            // 4. Create COINS text (Middle Row)
            GameObject coinTextObj = new GameObject("CoinText");
            coinTextObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform coinRect = coinTextObj.AddComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0f, 0.33f);
            coinRect.anchorMax = new Vector2(1f, 0.66f);
            coinRect.pivot = new Vector2(0.5f, 0.5f);
            coinRect.anchoredPosition = new Vector3(0f, 0f, 0f);
            coinRect.sizeDelta = new Vector2(0f, 0f);
            
            coinText = coinTextObj.AddComponent<Text>();
            coinText.font = defaultFont;
            coinText.fontSize = 20;
            coinText.fontStyle = FontStyle.Bold;
            coinText.alignment = TextAnchor.MiddleCenter;
            coinText.color = new Color(1f, 0.85f, 0f); // Neon Gold/Yellow
            
            Shadow coinShadow = coinTextObj.AddComponent<Shadow>();
            coinShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            coinShadow.effectDistance = new Vector2(1.5f, -1.5f);
            
            // 5. Create TIME text (Bottom Row)
            GameObject timeTextObj = new GameObject("TimeText");
            timeTextObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform timeRect = timeTextObj.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0f, 0f);
            timeRect.anchorMax = new Vector2(1f, 0.33f);
            timeRect.pivot = new Vector2(0.5f, 0.5f);
            timeRect.anchoredPosition = new Vector3(0f, 3f, 0f);
            timeRect.sizeDelta = new Vector2(0f, 0f);
            
            timeText = timeTextObj.AddComponent<Text>();
            timeText.font = defaultFont;
            timeText.fontSize = 17;
            timeText.fontStyle = FontStyle.Bold;
            timeText.alignment = TextAnchor.MiddleCenter;
            timeText.color = new Color(1f, 0.4f, 0f); // Neon Orange
            
            Shadow timeShadow = timeTextObj.AddComponent<Shadow>();
            timeShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            timeShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // 6. Create GAME OVER Panel Overlay (disabled by default)
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasObj.transform, false);
            
            RectTransform goPanelRect = gameOverPanel.AddComponent<RectTransform>();
            goPanelRect.anchorMin = new Vector2(0f, 0f); // Cover entire screen
            goPanelRect.anchorMax = new Vector2(1f, 1f);
            goPanelRect.pivot = new Vector2(0.5f, 0.5f);
            goPanelRect.anchoredPosition = Vector3.zero;
            goPanelRect.sizeDelta = Vector2.zero;

            // Full-screen backdrop fade (deep dark translucent red)
            Image goPanelImage = gameOverPanel.AddComponent<Image>();
            goPanelImage.color = new Color(0.12f, 0.02f, 0.02f, 0.85f);

            // Centered popup card
            GameObject cardObj = new GameObject("CardPanel");
            cardObj.transform.SetParent(gameOverPanel.transform, false);
            
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector3.zero;
            cardRect.sizeDelta = new Vector2(450f, 300f);

            Image cardImage = cardObj.AddComponent<Image>();
            cardImage.color = new Color(0.04f, 0.01f, 0.02f, 0.95f); // watertight dark backdrop

            // Glowing Neon Red border for Game Over Card
            Outline goCardBorder = cardObj.AddComponent<Outline>();
            goCardBorder.effectColor = new Color(1f, 0f, 0.15f, 0.75f);
            goCardBorder.effectDistance = new Vector2(3f, -3f);

            // Title "GAME OVER"
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.7f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector3(0f, -10f, 0f);
            titleRect.sizeDelta = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 38;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0f, 0.15f); // Neon Hot Pink/Red

            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            titleShadow.effectDistance = new Vector2(2f, -2f);
            titleText.text = "GAME OVER";

            // Stats breakdown
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(cardObj.transform, false);

            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0.35f);
            statsRect.anchorMax = new Vector2(1f, 0.7f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            statsRect.anchoredPosition = Vector3.zero;
            statsRect.sizeDelta = Vector2.zero;

            finalStatsText = statsObj.AddComponent<Text>();
            finalStatsText.font = defaultFont;
            finalStatsText.fontSize = 20;
            finalStatsText.alignment = TextAnchor.MiddleCenter;
            finalStatsText.color = new Color(0.85f, 0.85f, 0.95f);
            
            Shadow statsShadow = statsObj.AddComponent<Shadow>();
            statsShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            statsShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // --- 1. REPLAY BUTTON (Left) ---
            GameObject replayBtnObj = new GameObject("ReplayButton");
            replayBtnObj.transform.SetParent(cardObj.transform, false);

            RectTransform replayBtnRect = replayBtnObj.AddComponent<RectTransform>();
            replayBtnRect.anchorMin = new Vector2(0.5f, 0.2f);
            replayBtnRect.anchorMax = new Vector2(0.5f, 0.2f);
            replayBtnRect.pivot = new Vector2(0.5f, 0.5f);
            replayBtnRect.anchoredPosition = new Vector3(-95f, -10f, 0f);
            replayBtnRect.sizeDelta = new Vector2(160f, 50f);

            Image replayBtnImage = replayBtnObj.AddComponent<Image>();
            replayBtnImage.color = new Color(0.02f, 0.08f, 0.04f, 0.9f); // Dark greenish

            Outline replayBtnBorder = replayBtnObj.AddComponent<Outline>();
            replayBtnBorder.effectColor = new Color(0f, 1f, 0.5f, 0.6f); // Neon green/cyan
            replayBtnBorder.effectDistance = new Vector2(1.5f, -1.5f);

            Button btnReplay = replayBtnObj.AddComponent<Button>();
            btnReplay.targetGraphic = replayBtnImage;
            btnReplay.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerReplay();
                }
            });

            GameObject replayTextObj = new GameObject("ReplayText");
            replayTextObj.transform.SetParent(replayBtnObj.transform, false);
            RectTransform replayTextRect = replayTextObj.AddComponent<RectTransform>();
            replayTextRect.anchorMin = Vector2.zero;
            replayTextRect.anchorMax = Vector2.one;
            replayTextRect.sizeDelta = Vector2.zero;

            Text replayText = replayTextObj.AddComponent<Text>();
            replayText.font = defaultFont;
            replayText.fontSize = 18;
            replayText.fontStyle = FontStyle.Bold;
            replayText.alignment = TextAnchor.MiddleCenter;
            replayText.color = new Color(0.9f, 1f, 0.9f);
            replayText.text = "REPLAY";

            Shadow replayTextShadow = replayTextObj.AddComponent<Shadow>();
            replayTextShadow.effectColor = Color.black;
            replayTextShadow.effectDistance = new Vector2(1f, -1f);

            // --- 2. MENU BUTTON (Right) ---
            GameObject menuBtnObj = new GameObject("MenuButton");
            menuBtnObj.transform.SetParent(cardObj.transform, false);

            RectTransform menuBtnRect = menuBtnObj.AddComponent<RectTransform>();
            menuBtnRect.anchorMin = new Vector2(0.5f, 0.2f);
            menuBtnRect.anchorMax = new Vector2(0.5f, 0.2f);
            menuBtnRect.pivot = new Vector2(0.5f, 0.5f);
            menuBtnRect.anchoredPosition = new Vector3(95f, -10f, 0f);
            menuBtnRect.sizeDelta = new Vector2(160f, 50f);

            Image menuBtnImage = menuBtnObj.AddComponent<Image>();
            menuBtnImage.color = new Color(0.1f, 0.01f, 0.02f, 0.9f); // Dark reddish

            Outline menuBtnBorder = menuBtnObj.AddComponent<Outline>();
            menuBtnBorder.effectColor = new Color(1f, 0f, 0.15f, 0.6f); // Neon red
            menuBtnBorder.effectDistance = new Vector2(1.5f, -1.5f);

            Button btnMenu = menuBtnObj.AddComponent<Button>();
            btnMenu.targetGraphic = menuBtnImage;
            btnMenu.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMainMenu();
                }
            });

            GameObject menuTextObj = new GameObject("MenuText");
            menuTextObj.transform.SetParent(menuBtnObj.transform, false);
            RectTransform menuTextRect = menuTextObj.AddComponent<RectTransform>();
            menuTextRect.anchorMin = Vector2.zero;
            menuTextRect.anchorMax = Vector2.one;
            menuTextRect.sizeDelta = Vector2.zero;

            Text menuText = menuTextObj.AddComponent<Text>();
            menuText.font = defaultFont;
            menuText.fontSize = 18;
            menuText.fontStyle = FontStyle.Bold;
            menuText.alignment = TextAnchor.MiddleCenter;
            menuText.color = new Color(1f, 0.9f, 0.9f);
            menuText.text = "MENU";

            Shadow menuTextShadow = menuTextObj.AddComponent<Shadow>();
            menuTextShadow.effectColor = Color.black;
            menuTextShadow.effectDistance = new Vector2(1f, -1f);

            // Keep the Game Over Screen hidden initially
            gameOverPanel.SetActive(false);
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
