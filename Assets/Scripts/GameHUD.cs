using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TubityWAI
{
    /// <summary>
    /// In-game HUD, pause and game-over popups, built in the same TubityX
    /// glass-and-neon language as the main menu (see TubityXUIFactory).
    /// Everything that hugs a screen edge lives under a SafeAreaFitter so it
    /// clears notches, rounded corners and the home indicator.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Tooltip("The player controller component to track scoring and time.")]
        public PlayerController player;

        // Palette - shared with the menu so both screens read as one product.
        private static readonly Color FaceWhite = Color.white;
        private static readonly Color CoolWhite = new Color(0.80f, 0.90f, 1f);
        private static readonly Color DangerRed = new Color(1f, 0.30f, 0.45f);
        private static readonly Color PowerCyan = new Color(0f, 1f, 1f);
        private static readonly Color PowerPurple = new Color(0.8f, 0.2f, 1f);

        // Layout, in reference pixels (1920 x 1080)
        private const float EdgeMargin = 28f;        // gap between a HUD panel and the safe area

        private TubityXLabel scoreText;
        private TubityXLabel coinText;
        private TubityXLabel timeText;

        // Wraps the stats panel, powerup dial and pause button so they can fade/shrink
        // away together the instant the level ends, instead of just popping off with the pause.
        private CanvasGroup gameplayHudGroup;

        // Powerup UI
        private GameObject powerupPanel;
        private TubityXPanel powerupChrome;
        private Image powerupArc;
        private TubityXLabel powerupText;

        // Game Over & Pause Screen Elements
        private GameObject gameOverPanel;
        private GameObject pausePanel;
        private GameObject pauseButton;
        private GameObject replayButton;
        private GameObject resumeButton;
        public bool IsPaused { get; private set; } = false;
        private TubityXLabel finalScoreText;
        private TubityXLabel finalCoinsText;

        // Level complete review card
        private GameObject levelCompletePanel;
        private TubityXLabel lcTitleText;
        private TubityXLabel lcCoinsText;
        private TubityXLabel lcShieldsText;
        private TubityXLabel lcTimeText;
        private GameObject lcNextButton;
        private GameObject lcReplayButton;
        private RectTransform lcCardRect;
        private CanvasGroup lcOverlayGroup;
        private Image[] lcStars = new Image[3];
        private static readonly Color StarLit = new Color(1f, 0.85f, 0.25f);
        private static readonly Color StarDim = new Color(0.30f, 0.30f, 0.42f, 0.9f);
        private static Sprite starSprite;

        // Sphere-count block unlock popup: stacks on top of the level-complete card
        // when a level beat crosses a sphere-count threshold (see GameManager.RecordLevelResult).
        private GameObject sphereUnlockPanel;
        private TubityXLabel suSubtitleText;
        private RectTransform suCardRect;
        private CanvasGroup suOverlayGroup;

        // Pre-run countdown overlay
        private GameObject countdownHost;
        private TubityXLabel countdownText;
        private TubityXLabel countdownHint;
        private bool wasCountingDown = false;
        private float goTimer = 0f;
        private const float GoHoldDuration = 0.8f;

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
            // 1. Canvas
            GameObject canvasObj = new GameObject("HUDCanvas");
            canvasObj.transform.SetParent(this.transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // TubityXPanel / TubityXLabel carry geometry and accent colour in
            // TEXCOORD1 / TEXCOORD2, which canvases do not send by default.
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                             | AdditionalCanvasShaderChannels.TexCoord2;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Every edge-hugging element goes under the safe-area rect.
            RectTransform safe = SafeAreaFitter.Create(canvasObj.transform, "SafeArea");

            // The three gameplay-only elements live under one full-stretch wrapper so
            // PlayGameplayHudExitAnimation can fade/shrink them out as a single unit.
            GameObject gameplayHudObj = new GameObject("GameplayHud", typeof(RectTransform));
            gameplayHudObj.transform.SetParent(safe, false);
            RectTransform gameplayHudRect = gameplayHudObj.GetComponent<RectTransform>();
            gameplayHudRect.anchorMin = Vector2.zero;
            gameplayHudRect.anchorMax = Vector2.one;
            gameplayHudRect.offsetMin = Vector2.zero;
            gameplayHudRect.offsetMax = Vector2.zero;
            gameplayHudGroup = gameplayHudObj.AddComponent<CanvasGroup>();

            CreateStatsPanel(gameplayHudRect);
            CreatePowerupPanel(gameplayHudRect);
            CreatePauseButton(gameplayHudRect);
            CreateGameOverPopup(canvasObj.transform);
            CreatePausePopup(canvasObj.transform);
            CreateLevelCompletePopup(canvasObj.transform);
            CreateSphereUnlockPopup(canvasObj.transform);
            CreateCountdownOverlay(canvasObj.transform);
        }

        // ------------------------------------------------------------------
        // Level complete: title, three stars, the numbers behind them, next / replay / menu
        // ------------------------------------------------------------------
        private void CreateLevelCompletePopup(Transform canvas)
        {
            RectTransform safe;
            levelCompletePanel = CreateOverlay(canvas, "LevelCompletePanel", out safe);
            lcOverlayGroup = levelCompletePanel.AddComponent<CanvasGroup>();

            GameObject card = TubityXUIFactory.CreatePanel(
                safe, new Vector2(560f, 500f), TubityXUIFactory.Gold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 26f);
            card.name = "LevelCompleteCard";
            lcCardRect = card.GetComponent<RectTransform>();

            lcTitleText = AddCardLabel(card, "TitleText", 0.83f, 0.97f, "LEVEL COMPLETE", 28f, FaceWhite, TubityXUIFactory.Gold);

            for (int i = 0; i < 3; i++)
            {
                GameObject starObj = new GameObject("Star_" + i, typeof(RectTransform));
                starObj.transform.SetParent(card.transform, false);
                RectTransform sr = starObj.GetComponent<RectTransform>();
                sr.anchorMin = new Vector2(0.5f, 0.71f);
                sr.anchorMax = new Vector2(0.5f, 0.71f);
                sr.pivot = new Vector2(0.5f, 0.5f);
                sr.anchoredPosition = new Vector2((i - 1) * 92f, 0f);
                sr.sizeDelta = new Vector2(76f, 76f);
                Image img = starObj.AddComponent<Image>();
                img.sprite = StarSprite();
                img.color = StarDim;
                img.raycastTarget = false;
                lcStars[i] = img;
            }

            lcCoinsText = AddCardLabel(card, "CoinsText", 0.50f, 0.60f, "COINS 0 / 0", 15f,
                                       TubityXUIFactory.Gold, TubityXUIFactory.Gold);
            lcShieldsText = AddCardLabel(card, "ShieldsText", 0.40f, 0.50f, "SHIELDS 0 / 0", 15f,
                                         FaceWhite, TubityXUIFactory.Cyan);
            lcTimeText = AddCardLabel(card, "TimeText", 0.30f, 0.40f, "TIME 00:00   SCORE 000", 13f,
                                      CoolWhite, TubityXUIFactory.Blue);

            lcNextButton = AddCardButton(card, "NextLevelButton", "NEXT LEVEL", new Vector2(300f, 56f),
                                         TubityXUIFactory.Blue, 0.19f, 0f, true, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TriggerNextLevel();
            });

            Vector2 btnSize = new Vector2(220f, 52f);
            lcReplayButton = AddCardButton(card, "ReplayButton", "REPLAY", btnSize,
                                           TubityXUIFactory.Cyan, 0.07f, -118f, false, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TriggerReplay();
            });
            AddCardButton(card, "MenuButton", "MAIN MENU", btnSize,
                          TubityXUIFactory.Purple, 0.07f, 118f, false, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
            });

            levelCompletePanel.SetActive(false);
        }

        public void ShowLevelCompleteScreen(GameManager.LevelResult r)
        {
            if (levelCompletePanel == null || r == null) return;

            if (lcTitleText != null)
                lcTitleText.Text = r.isTestLevel || r.levelNumber <= 0 ? "LEVEL COMPLETE" : $"LEVEL {r.levelNumber} COMPLETE";

            for (int i = 0; i < lcStars.Length; i++)
            {
                if (lcStars[i] != null) lcStars[i].color = (i < r.stars) ? StarLit : StarDim;
            }

            if (lcCoinsText != null) lcCoinsText.Text = $"COINS {r.coinsCollected} / {r.coinsSpawned}";
            if (lcShieldsText != null) lcShieldsText.Text = $"SHIELDS {r.shieldsPassed} / {r.shieldsSpawned}";
            if (lcTimeText != null)
            {
                int minutes = Mathf.FloorToInt(r.time / 60f);
                int seconds = Mathf.FloorToInt(r.time % 60f);
                lcTimeText.Text = string.Format("TIME {0:00}:{1:00}   SCORE {2:D3}", minutes, seconds, r.score);
            }

            if (lcNextButton != null) lcNextButton.SetActive(r.hasNextLevel);
            if (countdownHost != null) countdownHost.SetActive(false);
            levelCompletePanel.SetActive(true);
            StartCoroutine(AnimateLevelCompleteIn());

            if (r.unlockedSphereCount > 0)
            {
                StartCoroutine(ShowSphereUnlockPopupAfterDelay(r.unlockedSphereCount, 0.7f));
            }

            if (TVOSMenuNavigator.Instance != null)
            {
                GameObject focus = (r.hasNextLevel && lcNextButton != null) ? lcNextButton : lcReplayButton;
                if (focus != null) TVOSMenuNavigator.Instance.SetFocus(focus);
            }
        }

        /// <summary>Fades the dim backdrop in while the card springs open from a flat squash,
        /// overshooting past full size before settling - the squeeze-and-expand pop-in.</summary>
        private System.Collections.IEnumerator AnimateLevelCompleteIn()
        {
            if (lcOverlayGroup == null || lcCardRect == null) yield break;

            const float duration = 0.5f;
            const float c1 = 1.70158f; // standard "back" easing overshoot constant
            const float c3 = c1 + 1f;

            Vector3 startScale = new Vector3(1.25f, 0.05f, 1f); // flat and a little wide, like a card slammed shut
            lcCardRect.localScale = startScale;
            lcOverlayGroup.alpha = 0f;
            lcOverlayGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float tm1 = t - 1f;
                float overshoot = 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;

                lcCardRect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, overshoot);
                lcOverlayGroup.alpha = Mathf.Clamp01(t / 0.6f);
                yield return null;
            }

            lcCardRect.localScale = Vector3.one;
            lcOverlayGroup.alpha = 1f;
            lcOverlayGroup.interactable = true;
        }

        // ------------------------------------------------------------------
        // Sphere-count block unlock: a small celebration card that stacks on top
        // of the level-complete card the moment a beat crosses a sphere-count
        // threshold (see GameManager.GetSphereUnlockLevel / RecordLevelResult).
        // ------------------------------------------------------------------
        private void CreateSphereUnlockPopup(Transform canvas)
        {
            RectTransform safe;
            sphereUnlockPanel = CreateOverlay(canvas, "SphereUnlockPanel", out safe);
            suOverlayGroup = sphereUnlockPanel.AddComponent<CanvasGroup>();

            GameObject card = TubityXUIFactory.CreatePanel(
                safe, new Vector2(480f, 380f), TubityXUIFactory.Cyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 26f);
            card.name = "SphereUnlockCard";
            suCardRect = card.GetComponent<RectTransform>();

            GameObject lockObj = new GameObject("LockIcon", typeof(RectTransform));
            lockObj.transform.SetParent(card.transform, false);
            RectTransform lockRect = lockObj.GetComponent<RectTransform>();
            lockRect.anchorMin = new Vector2(0.5f, 0.70f);
            lockRect.anchorMax = new Vector2(0.5f, 0.70f);
            lockRect.pivot = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(100f, 100f);
            Image lockImg = lockObj.AddComponent<Image>();
            lockImg.sprite = GlassUIFactory.GetLockSprite(true);
            lockImg.color = TubityXUIFactory.Cyan;
            lockImg.raycastTarget = false;

            AddCardLabel(card, "TitleText", 0.40f, 0.53f, "NEW SPHERES UNLOCKED", 21f, FaceWhite, TubityXUIFactory.Cyan);
            suSubtitleText = AddCardLabel(card, "SubText", 0.28f, 0.40f, "", 15f, CoolWhite, TubityXUIFactory.Blue);

            AddCardButton(card, "ContinueButton", "CONTINUE", new Vector2(220f, 56f),
                          TubityXUIFactory.Blue, 0.10f, 0f, true, () =>
            {
                if (sphereUnlockPanel != null) sphereUnlockPanel.SetActive(false);
            });

            sphereUnlockPanel.SetActive(false);
        }

        private System.Collections.IEnumerator ShowSphereUnlockPopupAfterDelay(int sphereCount, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            ShowSphereUnlockPopup(sphereCount);
        }

        public void ShowSphereUnlockPopup(int sphereCount)
        {
            if (sphereUnlockPanel == null) return;

            if (suSubtitleText != null)
                suSubtitleText.Text = $"YOU CAN NOW RUN WITH {sphereCount} SPHERES";

            sphereUnlockPanel.SetActive(true);
            StartCoroutine(AnimateSphereUnlockIn());
        }

        /// <summary>Same squeeze-and-expand spring as the level-complete card, so the two
        /// popups read as one visual language when they stack.</summary>
        private System.Collections.IEnumerator AnimateSphereUnlockIn()
        {
            if (suOverlayGroup == null || suCardRect == null) yield break;

            const float duration = 0.5f;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            Vector3 startScale = new Vector3(1.25f, 0.05f, 1f);
            suCardRect.localScale = startScale;
            suOverlayGroup.alpha = 0f;
            suOverlayGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float tm1 = t - 1f;
                float overshoot = 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;

                suCardRect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, overshoot);
                suOverlayGroup.alpha = Mathf.Clamp01(t / 0.6f);
                yield return null;
            }

            suCardRect.localScale = Vector3.one;
            suOverlayGroup.alpha = 1f;
            suOverlayGroup.interactable = true;
        }

        /// <summary>A five-point star drawn into a small texture with a one-pixel soft edge.</summary>
        private static Sprite StarSprite()
        {
            if (starSprite != null) return starSprite;

            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float outerR = size * 0.47f;
            float innerR = outerR * 0.45f;
            Vector2 tip = new Vector2(outerR, 0f);
            Vector2 inner = new Vector2(innerR * Mathf.Cos(36f * Mathf.Deg2Rad), innerR * Mathf.Sin(36f * Mathf.Deg2Rad));
            Vector2 edge = inner - tip;

            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f);
                    float rho = p.magnitude;
                    // Fold the angle into one half-point (0 = a tip, pointing up) so one edge line serves all five points.
                    float ang = Mathf.Repeat(Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg - 90f, 72f);
                    if (ang > 36f) ang = 72f - ang;
                    Vector2 u = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                    float denom = edge.x * u.y - edge.y * u.x;
                    float edgeR = Mathf.Abs(denom) > 1e-5f ? (edge.x * tip.y - edge.y * tip.x) / denom : 0f;
                    float a = Mathf.Clamp01(edgeR - rho + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();

            starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return starSprite;
        }

        // ------------------------------------------------------------------
        // Pre-run countdown: a big centred digit with a hint underneath
        // ------------------------------------------------------------------
        private void CreateCountdownOverlay(Transform canvas)
        {
            countdownHost = new GameObject("CountdownOverlay", typeof(RectTransform));
            countdownHost.transform.SetParent(canvas, false);
            RectTransform r = countdownHost.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, 70f);
            r.sizeDelta = new Vector2(700f, 260f);

            GameObject digitHost = new GameObject("Digit", typeof(RectTransform));
            digitHost.transform.SetParent(countdownHost.transform, false);
            RectTransform dr = digitHost.GetComponent<RectTransform>();
            dr.anchorMin = new Vector2(0f, 0.34f);
            dr.anchorMax = new Vector2(1f, 1f);
            dr.sizeDelta = Vector2.zero;
            countdownText = TubityXUIFactory.AddLabel(digitHost, "3", 100f, FaceWhite, TubityXUIFactory.Cyan, 10f);

            GameObject hintHost = new GameObject("Hint", typeof(RectTransform));
            hintHost.transform.SetParent(countdownHost.transform, false);
            RectTransform hr = hintHost.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0f, 0f);
            hr.anchorMax = new Vector2(1f, 0.28f);
            hr.sizeDelta = Vector2.zero;
            countdownHint = TubityXUIFactory.AddLabel(hintHost, "GET READY  -  STEER TO LINE UP", 15f, CoolWhite, TubityXUIFactory.Blue, 3f);

            countdownHost.SetActive(false);
        }

        private void UpdateCountdown()
        {
            if (countdownHost == null || countdownText == null) return;

            if (player.IsCountingDown)
            {
                wasCountingDown = true;
                if (!countdownHost.activeSelf) countdownHost.SetActive(true);

                float remaining = Mathf.Max(0f, player.CountdownRemaining);
                int digit = Mathf.Max(1, Mathf.CeilToInt(remaining));
                countdownText.Text = digit.ToString();

                // Each digit lands large and settles over its second so the beat reads at a glance.
                float frac = remaining - Mathf.Floor(remaining);
                countdownText.transform.localScale = Vector3.one * (1f + 0.35f * frac);
                return;
            }

            if (wasCountingDown)
            {
                wasCountingDown = false;
                goTimer = GoHoldDuration;
                countdownText.Text = "GO!";
                countdownText.transform.localScale = Vector3.one;
                if (countdownHint != null) countdownHint.Text = "";
            }

            if (goTimer > 0f)
            {
                goTimer -= Time.deltaTime;
                float grow = 1f - Mathf.Clamp01(goTimer / GoHoldDuration);
                countdownText.transform.localScale = Vector3.one * (1f + 0.4f * grow);
                if (goTimer <= 0f) countdownHost.SetActive(false);
            }
            else if (countdownHost.activeSelf)
            {
                countdownHost.SetActive(false);
            }
        }

        // ------------------------------------------------------------------
        // Top-centre stats: SCORE / COINS / TIME
        // ------------------------------------------------------------------
        private void CreateStatsPanel(RectTransform safe)
        {
            Vector2 size = new Vector2(340f, 132f);

            // Pivot is the panel centre, so offset by half the height plus the
            // margin - anchoring the centre a few pixels below the edge (the
            // old layout) leaves half the panel off screen.
            GameObject panelObj = TubityXUIFactory.CreatePanel(
                safe, size, TubityXUIFactory.Cyan,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -(size.y * 0.5f + EdgeMargin)), 20f);
            panelObj.name = "HUDPanel";
            panelObj.GetComponent<TubityXPanel>().raycastTarget = false;   // never eat gameplay taps

            scoreText = AddRow(panelObj, "ScoreText", 0.64f, 1.00f, -6f, "SCORE: 000", 17f,
                               FaceWhite, TubityXUIFactory.Cyan);
            coinText = AddRow(panelObj, "CoinText", 0.32f, 0.64f, 0f, "COINS: 000", 14f,
                              TubityXUIFactory.Gold, TubityXUIFactory.Gold);
            timeText = AddRow(panelObj, "TimeText", 0.00f, 0.32f, 6f, "TIME: 00:00", 13f,
                              CoolWhite, TubityXUIFactory.Blue);
        }

        private static TubityXLabel AddRow(GameObject panel, string name, float yMin, float yMax,
                                           float yOffset, string text, float cap,
                                           Color face, Color accent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(panel.transform, false);
            RectTransform r = host.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, yMin);
            r.anchorMax = new Vector2(1f, yMax);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, yOffset);
            r.sizeDelta = Vector2.zero;
            return TubityXUIFactory.AddLabel(host, text, cap, face, accent, cap * 0.16f);
        }

        // ------------------------------------------------------------------
        // Top-right powerup timer: a round glass dial with a radial arc
        // ------------------------------------------------------------------
        private void CreatePowerupPanel(RectTransform safe)
        {
            float d = 150f;
            float inset = d * 0.5f + EdgeMargin;

            powerupPanel = TubityXUIFactory.CreatePanel(
                safe, new Vector2(d, d), PowerCyan,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-inset, -inset), d * 0.5f);      // radius = half size -> a circle
            powerupPanel.name = "PowerupPanel";
            powerupChrome = powerupPanel.GetComponent<TubityXPanel>();
            powerupChrome.raycastTarget = false;

            GameObject arcObj = new GameObject("PowerupArc", typeof(RectTransform));
            arcObj.transform.SetParent(powerupPanel.transform, false);
            RectTransform arcRect = arcObj.GetComponent<RectTransform>();
            arcRect.anchorMin = Vector2.zero;
            arcRect.anchorMax = Vector2.one;
            arcRect.sizeDelta = new Vector2(-22f, -22f);

            powerupArc = arcObj.AddComponent<Image>();
            powerupArc.sprite = GlassUIFactory.GetRingSprite();
            powerupArc.type = Image.Type.Filled;
            powerupArc.fillMethod = Image.FillMethod.Radial360;
            powerupArc.fillOrigin = (int)Image.Origin360.Top;
            powerupArc.fillClockwise = false;
            powerupArc.color = PowerCyan;
            powerupArc.raycastTarget = false;

            GameObject puTextObj = new GameObject("PowerupText", typeof(RectTransform));
            puTextObj.transform.SetParent(powerupPanel.transform, false);
            RectTransform puTextRect = puTextObj.GetComponent<RectTransform>();
            puTextRect.anchorMin = new Vector2(0.12f, 0.30f);
            puTextRect.anchorMax = new Vector2(0.88f, 0.70f);
            puTextRect.sizeDelta = Vector2.zero;

            powerupText = TubityXUIFactory.AddLabel(puTextObj, "INVINCIBLE", 12f, PowerCyan, PowerCyan, 1.5f);

            powerupPanel.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Top-left pause button - the only way to reach the pause popup
        // (and Main Menu) without a keyboard/gamepad.
        // ------------------------------------------------------------------
        private void CreatePauseButton(RectTransform safe)
        {
            float d = 84f;
            float inset = d * 0.5f + EdgeMargin;

            pauseButton = TubityXUIFactory.CreateButton(
                safe, new Vector2(d, d), "II", TubityXUIFactory.Cyan, 34f, 18f, d * 0.5f, false);
            pauseButton.name = "PauseButton";

            RectTransform r = pauseButton.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(inset, -inset);

            pauseButton.GetComponent<Button>().onClick.AddListener(TogglePauseMenu);
        }

        // ------------------------------------------------------------------
        // Popups
        // ------------------------------------------------------------------

        /// <summary>Full-screen dim plus a safe-area rect for the card.</summary>
        private static GameObject CreateOverlay(Transform canvas, string name, out RectTransform safe)
        {
            GameObject overlay = new GameObject(name, typeof(RectTransform));
            overlay.transform.SetParent(canvas, false);
            RectTransform r = overlay.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero;

            // Deep obsidian fade, same as the menu's popup backdrop
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.01f, 0.04f, 0.88f);

            safe = SafeAreaFitter.Create(overlay.transform, "SafeArea");
            return overlay;
        }

        private static TubityXLabel AddCardLabel(GameObject card, string name, float yMin, float yMax,
                                                 string text, float cap, Color face, Color accent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(card.transform, false);
            RectTransform r = host.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.05f, yMin);
            r.anchorMax = new Vector2(0.95f, yMax);
            r.sizeDelta = Vector2.zero;
            return TubityXUIFactory.AddLabel(host, text, cap, face, accent, cap * 0.16f);
        }

        private static GameObject AddCardButton(GameObject card, string name, string label,
                                                Vector2 size, Color accent, float y, float x,
                                                bool pulse, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btn = TubityXUIFactory.CreateButton(card.transform, size, label, accent,
                                                           20f, 3f, 16f, pulse);
            btn.name = name;
            RectTransform r = btn.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, y);
            r.anchorMax = new Vector2(0.5f, y);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, 0f);
            btn.GetComponent<Button>().onClick.AddListener(onClick);
            return btn;
        }

        private void CreateGameOverPopup(Transform canvas)
        {
            RectTransform safe;
            gameOverPanel = CreateOverlay(canvas, "GameOverPanel", out safe);

            GameObject card = TubityXUIFactory.CreatePanel(
                safe, new Vector2(520f, 360f), DangerRed,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 26f);
            card.name = "CardPanel";

            AddCardLabel(card, "TitleText", 0.74f, 0.96f, "GAME OVER", 30f, FaceWhite, DangerRed);
            finalScoreText = AddCardLabel(card, "ScoreText", 0.54f, 0.68f, "FINAL SCORE: 000", 15f,
                                          FaceWhite, TubityXUIFactory.Cyan);
            finalCoinsText = AddCardLabel(card, "CoinsText", 0.40f, 0.54f, "COINS COLLECTED: 000", 15f,
                                          TubityXUIFactory.Gold, TubityXUIFactory.Gold);

            Vector2 btnSize = new Vector2(220f, 58f);
            replayButton = AddCardButton(card, "ReplayButton", "REPLAY", btnSize,
                                         TubityXUIFactory.Blue, 0.18f, -118f, true, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TriggerReplay();
            });

            AddCardButton(card, "MenuButton", "MAIN MENU", btnSize,
                          TubityXUIFactory.Purple, 0.18f, 118f, false, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
            });

            gameOverPanel.SetActive(false);
        }

        private void CreatePausePopup(Transform canvas)
        {
            RectTransform safe;
            pausePanel = CreateOverlay(canvas, "PausePanel", out safe);

            GameObject card = TubityXUIFactory.CreatePanel(
                safe, new Vector2(460f, 400f), TubityXUIFactory.Cyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 26f);
            card.name = "PauseCardPanel";

            AddCardLabel(card, "PauseTitleText", 0.76f, 0.97f, "PAUSED", 30f, FaceWhite, TubityXUIFactory.Cyan);

            Vector2 btnSize = new Vector2(300f, 58f);
            resumeButton = AddCardButton(card, "ResumeButton", "RESUME", btnSize,
                                         TubityXUIFactory.Blue, 0.56f, 0f, true, TogglePauseMenu);

            AddCardButton(card, "PauseReplayButton", "REPLAY", btnSize,
                          TubityXUIFactory.Cyan, 0.37f, 0f, false, () =>
            {
                Time.timeScale = 1f;
                IsPaused = false;
                if (GameManager.Instance != null) GameManager.Instance.TriggerReplay();
            });

            AddCardButton(card, "PauseMenuButton", "MAIN MENU", btnSize,
                          TubityXUIFactory.Purple, 0.18f, 0f, false, () =>
            {
                Time.timeScale = 1f;
                IsPaused = false;
                if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
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
                if (IsPaused && TVOSMenuNavigator.Instance != null && resumeButton != null)
                {
                    TVOSMenuNavigator.Instance.SetFocus(resumeButton);
                }
            }
        }

        /// <summary>Fades and shrinks the gameplay-only HUD (stats, powerup dial, pause button)
        /// away the instant the level ends, so it doesn't just vanish under the review card.</summary>
        public void PlayGameplayHudExitAnimation()
        {
            if (gameplayHudGroup == null) return;
            StartCoroutine(AnimateGameplayHudOut());
        }

        private System.Collections.IEnumerator AnimateGameplayHudOut()
        {
            RectTransform rt = gameplayHudGroup.GetComponent<RectTransform>();
            const float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t; // accelerates away

                gameplayHudGroup.alpha = 1f - ease;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, ease);
                yield return null;
            }

            gameplayHudGroup.alpha = 0f;
            gameplayHudGroup.interactable = false;
            gameplayHudGroup.blocksRaycasts = false;
        }

        public void ShowGameOverScreen()
        {
            if (gameOverPanel == null) return;

            if (player != null)
            {
                if (finalScoreText != null) finalScoreText.Text = "FINAL SCORE: " + player.Score.ToString("D3");
                if (finalCoinsText != null) finalCoinsText.Text = "COINS COLLECTED: " + player.Coins.ToString("D3");
            }

            gameOverPanel.SetActive(true);

            if (TVOSMenuNavigator.Instance != null && replayButton != null)
            {
                TVOSMenuNavigator.Instance.SetFocus(replayButton);
            }
        }

        private void SetPowerup(string label, Color tint, float fill)
        {
            if (!powerupPanel.activeSelf) powerupPanel.SetActive(true);
            powerupText.Text = label;
            powerupText.color = tint;
            powerupText.RimColor = tint;
            powerupArc.color = tint;
            if (powerupChrome != null && powerupChrome.RimColor != tint) powerupChrome.RimColor = tint;
            powerupArc.fillAmount = fill;
        }

        private void Update()
        {
            if (player == null || scoreText == null || coinText == null || timeText == null) return;

            // Only update active UI stats if game is not over (or the finish gate has been crossed)
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsLevelComplete)) return;

            UpdateCountdown();

            scoreText.Text = "SCORE: " + player.Score.ToString("D3");
            coinText.Text = "COINS: " + player.Coins.ToString("D3");

            int minutes = Mathf.FloorToInt(player.TimeElapsed / 60f);
            int seconds = Mathf.FloorToInt(player.TimeElapsed % 60f);
            timeText.Text = string.Format("TIME: {0:00}:{1:00}", minutes, seconds);

            if (player.IsInvincible)
            {
                SetPowerup("INVINCIBLE", PowerCyan,
                           player.InvincibilityTimeRemaining / player.InvincibilityTotalTime);
            }
            else if (player.IsMagnetActive)
            {
                SetPowerup("MAGNET", PowerPurple,
                           player.MagnetTimeRemaining / player.MagnetTotalTime);
            }
            else if (powerupPanel.activeSelf)
            {
                powerupPanel.SetActive(false);
            }
        }
    }
}
