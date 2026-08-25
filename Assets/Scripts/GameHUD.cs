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

        // Powerup UI
        private GameObject powerupPanel;
        private Image powerupArc;
        private Text powerupText;
        private Outline powerupRim;
        private Shadow powerupGlow;

        // Game Over & Pause Screen Elements
        private GameObject gameOverPanel;
        private GameObject pausePanel;
        public bool IsPaused { get; private set; } = false;
        private Text finalStatsText;
        private Font defaultFont;

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
            GameObject panelObj = GlassUIFactory.CreateGlassmorphicPanel(canvasObj.transform, new Vector2(320f, 130f), new Color(0f, 1f, 1f, 0.85f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector3(0f, -30f, 0f));
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
            scoreText.fontStyle = FontStyle.Normal;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = new Color(0f, 1f, 1f); // Neon Cyan
            
            Shadow scoreShadow = scoreTextObj.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            scoreShadow.effectDistance = new Vector2(1f, -1f);
            
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
            coinText.fontStyle = FontStyle.Normal;
            coinText.alignment = TextAnchor.MiddleCenter;
            coinText.color = new Color(1f, 1f, 1f); // White instead of Gold for minimalist
            
            Shadow coinShadow = coinTextObj.AddComponent<Shadow>();
            coinShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            coinShadow.effectDistance = new Vector2(1f, -1f);
            
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
            timeText.fontStyle = FontStyle.Normal;
            timeText.alignment = TextAnchor.MiddleCenter;
            timeText.color = new Color(0.8f, 0.9f, 1f); // Cool white instead of orange
            
            Shadow timeShadow = timeTextObj.AddComponent<Shadow>();
            timeShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            timeShadow.effectDistance = new Vector2(1f, -1f);

            // 6. Create POWERUP Panel (Top Right)
            powerupPanel = GlassUIFactory.CreateGlassmorphicPanel(canvasObj.transform, new Vector2(160f, 160f), new Color(0f, 1f, 1f, 0.85f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector3(-20f, -20f, 0f));
            powerupPanel.name = "PowerupPanel";
            powerupRim = powerupPanel.GetComponent<Outline>();
            powerupGlow = powerupPanel.GetComponent<Shadow>();

            GameObject arcObj = new GameObject("PowerupArc");
            arcObj.transform.SetParent(powerupPanel.transform, false);
            RectTransform arcRect = arcObj.AddComponent<RectTransform>();
            arcRect.anchorMin = Vector2.zero;
            arcRect.anchorMax = Vector2.one;
            arcRect.sizeDelta = new Vector2(-20f, -20f);

            powerupArc = arcObj.AddComponent<Image>();
            powerupArc.sprite = GlassUIFactory.GetRingSprite();
            powerupArc.type = Image.Type.Filled;
            powerupArc.fillMethod = Image.FillMethod.Radial360;
            powerupArc.fillOrigin = (int)Image.Origin360.Top;
            powerupArc.fillClockwise = false;
            powerupArc.color = new Color(0f, 1f, 1f);

            GameObject puTextObj = new GameObject("PowerupText");
            puTextObj.transform.SetParent(powerupPanel.transform, false);
            RectTransform puTextRect = puTextObj.AddComponent<RectTransform>();
            puTextRect.anchorMin = Vector2.zero;
            puTextRect.anchorMax = Vector2.one;
            puTextRect.sizeDelta = Vector2.zero;

            powerupText = puTextObj.AddComponent<Text>();
            powerupText.font = defaultFont;
            powerupText.fontSize = 20;
            powerupText.fontStyle = FontStyle.Normal;
            powerupText.alignment = TextAnchor.MiddleCenter;
            powerupText.color = new Color(0f, 1f, 1f);
            
            Shadow puShadow = puTextObj.AddComponent<Shadow>();
            puShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            puShadow.effectDistance = new Vector2(1f, -1f);

            powerupPanel.SetActive(false);

            // 7. Create GAME OVER Panel Overlay (disabled by default)
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
            GameObject cardObj = GlassUIFactory.CreateGlassmorphicPanel(gameOverPanel.transform, new Vector2(480f, 340f), new Color(1f, 0f, 0.2f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector3.zero);
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
            titleText.fontStyle = FontStyle.Normal;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.4f, 0.4f); // Softer red
            
            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            titleShadow.effectDistance = new Vector2(1f, -1f);
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
            finalStatsText.fontStyle = FontStyle.Normal;
            finalStatsText.alignment = TextAnchor.MiddleCenter;
            finalStatsText.color = new Color(0.9f, 0.9f, 1f);
            
            Shadow statsShadow = statsObj.AddComponent<Shadow>();
            statsShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            statsShadow.effectDistance = new Vector2(1f, -1f);

            // --- 1. REPLAY BUTTON (Left) ---
            GameObject replayBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(cardObj.transform, new Vector2(170f, 55f), new Color(0f, 1f, 0.6f, 0.85f), "REPLAY", Color.white, 18);
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
            GameObject menuBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(cardObj.transform, new Vector2(170f, 55f), new Color(1f, 0f, 0.3f, 0.85f), "MAIN MENU", Color.white, 18);
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

            // 7. Create PAUSE Panel Overlay (disabled by default)
            pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(canvasObj.transform, false);

            RectTransform pPanelRect = pausePanel.AddComponent<RectTransform>();
            pPanelRect.anchorMin = Vector2.zero;
            pPanelRect.anchorMax = Vector2.one;
            pPanelRect.pivot = new Vector2(0.5f, 0.5f);
            pPanelRect.anchoredPosition = Vector3.zero;
            pPanelRect.sizeDelta = Vector2.zero;

            Image pPanelImage = pausePanel.AddComponent<Image>();
            pPanelImage.color = new Color(0.02f, 0.01f, 0.04f, 0.88f);

            GameObject pCardObj = GlassUIFactory.CreateGlassmorphicPanel(pausePanel.transform, new Vector2(440f, 380f), new Color(0f, 1f, 1f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector3.zero);
            pCardObj.name = "PauseCardPanel";

            // Title "PAUSED"
            GameObject pTitleObj = new GameObject("PauseTitleText");
            pTitleObj.transform.SetParent(pCardObj.transform, false);

            RectTransform pTitleRect = pTitleObj.AddComponent<RectTransform>();
            pTitleRect.anchorMin = new Vector2(0f, 0.74f);
            pTitleRect.anchorMax = new Vector2(1f, 0.98f);
            pTitleRect.pivot = new Vector2(0.5f, 0.5f);
            pTitleRect.anchoredPosition = new Vector3(0f, -5f, 0f);
            pTitleRect.sizeDelta = Vector2.zero;

            Text pTitleText = pTitleObj.AddComponent<Text>();
            pTitleText.font = defaultFont;
            pTitleText.fontSize = 38;
            pTitleText.fontStyle = FontStyle.Normal;
            pTitleText.alignment = TextAnchor.MiddleCenter;
            pTitleText.color = new Color(0.8f, 0.9f, 1f); // Cool white
            pTitleText.text = "PAUSED";
            
            Shadow pTitleShadow = pTitleObj.AddComponent<Shadow>();
            pTitleShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            pTitleShadow.effectDistance = new Vector2(1f, -1f);

            // 1. RESUME BUTTON
            GameObject resumeBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(pCardObj.transform, new Vector2(260f, 55f), new Color(0f, 1f, 0.6f, 0.85f), "RESUME", Color.white, 20);
            resumeBtnObj.name = "ResumeButton";
            RectTransform resumeBtnRect = resumeBtnObj.GetComponent<RectTransform>();
            resumeBtnRect.anchorMin = new Vector2(0.5f, 0.56f);
            resumeBtnRect.anchorMax = new Vector2(0.5f, 0.56f);
            resumeBtnRect.pivot = new Vector2(0.5f, 0.5f);
            resumeBtnRect.anchoredPosition = Vector3.zero;
            resumeBtnObj.GetComponent<Button>().onClick.AddListener(TogglePauseMenu);

            // 2. REPLAY BUTTON
            GameObject pReplayBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(pCardObj.transform, new Vector2(260f, 55f), new Color(1f, 0f, 0.6f, 0.85f), "REPLAY", Color.white, 20);
            pReplayBtnObj.name = "PauseReplayButton";
            RectTransform pReplayBtnRect = pReplayBtnObj.GetComponent<RectTransform>();
            pReplayBtnRect.anchorMin = new Vector2(0.5f, 0.36f);
            pReplayBtnRect.anchorMax = new Vector2(0.5f, 0.36f);
            pReplayBtnRect.pivot = new Vector2(0.5f, 0.5f);
            pReplayBtnRect.anchoredPosition = Vector3.zero;
            pReplayBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                IsPaused = false;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerReplay();
                }
            });

            // 3. MAIN MENU BUTTON
            GameObject pMenuBtnObj = GlassUIFactory.CreateGlassmorphicIconButton(pCardObj.transform, new Vector2(260f, 55f), new Color(1f, 0f, 0.3f, 0.85f), "MAIN MENU", Color.white, 20);
            pMenuBtnObj.name = "PauseMenuButton";
            RectTransform pMenuBtnRect = pMenuBtnObj.GetComponent<RectTransform>();
            pMenuBtnRect.anchorMin = new Vector2(0.5f, 0.16f);
            pMenuBtnRect.anchorMax = new Vector2(0.5f, 0.16f);
            pMenuBtnRect.pivot = new Vector2(0.5f, 0.5f);
            pMenuBtnRect.anchoredPosition = Vector3.zero;
            pMenuBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                IsPaused = false;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMainMenu();
                }
            });

            pausePanel.SetActive(false);
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

        public void TogglePauseMenu()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(IsPaused);
                if (IsPaused && TVOSMenuNavigator.Instance != null)
                {
                    Transform resumeBtn = pausePanel.transform.Find("PauseCardPanel/ResumeButton");
                    if (resumeBtn != null)
                    {
                        TVOSMenuNavigator.Instance.SetFocus(resumeBtn.gameObject);
                    }
                }
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

            if (TVOSMenuNavigator.Instance != null)
            {
                Transform replayBtn = gameOverPanel.transform.Find("CardPanel/ReplayButton");
                if (replayBtn != null)
                {
                    TVOSMenuNavigator.Instance.SetFocus(replayBtn.gameObject);
                }
            }
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

            // Update Powerup Arc UI
            if (player.IsInvincible)
            {
                if (!powerupPanel.activeSelf) powerupPanel.SetActive(true);
                
                Color cyan = new Color(0f, 1f, 1f);
                powerupText.text = "INVINCIBLE";
                powerupText.color = cyan;
                powerupArc.color = cyan;
                if (powerupRim != null) powerupRim.effectColor = cyan;
                if (powerupGlow != null) powerupGlow.effectColor = new Color(cyan.r, cyan.g, cyan.b, 0.45f);

                powerupArc.fillAmount = player.InvincibilityTimeRemaining / player.InvincibilityTotalTime;
            }
            else if (player.IsMagnetActive)
            {
                if (!powerupPanel.activeSelf) powerupPanel.SetActive(true);
                
                Color purple = new Color(0.8f, 0.2f, 1f);
                powerupText.text = "MAGNET";
                powerupText.color = purple;
                powerupArc.color = purple;
                if (powerupRim != null) powerupRim.effectColor = purple;
                if (powerupGlow != null) powerupGlow.effectColor = new Color(purple.r, purple.g, purple.b, 0.45f);

                powerupArc.fillAmount = player.MagnetTimeRemaining / player.MagnetTotalTime;
            }
            else
            {
                if (powerupPanel.activeSelf) powerupPanel.SetActive(false);
            }
        }
    }
}
