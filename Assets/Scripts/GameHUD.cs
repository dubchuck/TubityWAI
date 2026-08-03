using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    public class GameHUD : MonoBehaviour
    {
        [Tooltip("The player controller component to track scoring and time.")]
        public PlayerController player;
        
        private Text scoreText;
        private Text coinText;
        private Text timeText;
        
        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
            
            CreateUI();
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
            
            // 2. Create Panel for the Glassmorphic HUD Card (extended height to support 3 rows)
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
        }
        
        private void Update()
        {
            if (player == null || scoreText == null || coinText == null || timeText == null) return;
            
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
