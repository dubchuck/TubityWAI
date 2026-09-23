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
        private GameObject statsPanelObj;

        [Tooltip("The top-right score / coins / time pills. The tutorial hides them so only its own copy is on screen.")]
        public bool showStatsPanel = true;

        // Top-right column: one framed pill per stat, then one per active powerup, stacked with
        // a gap between each. A layout group does the stacking, so a powerup that switches on
        // or off simply takes or gives back its slot beneath the stats.
        private const float PillWidth = 156f;
        private const float PillHeight = 52f;
        private const float PillGap = 10f;
        private const float PillIcon = 30f;
        private RectTransform hudColumn;

        // Wraps the stats panel, powerup dial and pause button so they can fade/shrink
        // away together the instant the level ends, instead of just popping off with the pause.
        private CanvasGroup gameplayHudGroup;

        // Powerup pills: an icon and a bar that drains as the effect runs out.
        private GameObject invinciblePill;
        private RectTransform invincibleFill;
        private GameObject magnetPill;
        private RectTransform magnetFill;

        // Game Over & Pause Screen Elements
        private GameObject gameOverPanel;
        private GameObject pausePanel;
        private CanvasGroup pauseGroup;
        private RectTransform pauseCardRect;
        private Coroutine pausePop;
        private bool pauseClosing;      // the card is slamming out; time restarts once it's gone
        private GameObject exitTutorialPanel;   // "leave the tutorial?" confirm, tutorial runs only
        private GameObject keepGoingButton;
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

        private RectTransform[] lcButtons = new RectTransform[0];

        // Crash review card
        private CanvasGroup goOverlayGroup;
        private RectTransform goCardRect;
        private TubityXLabel goTitleText;
        private RectTransform[] goButtons = new RectTransform[0];

        // Set once a review card's button has been pressed, so a second tap can't start a
        // second exit while the first is still animating.
        private bool reviewLeaving = false;

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
            CreateExitTutorialPopup(canvasObj.transform);
            CreateLevelCompletePopup(canvasObj.transform);
            CreateSphereUnlockPopup(canvasObj.transform);
            CreateCountdownOverlay(canvasObj.transform);
            CreateCheckpointToast(canvasObj.transform);

            LevelConfig levelConfig = GameManager.Instance != null ? GameManager.Instance.currentLevelConfig : null;
            if (levelConfig != null && levelConfig.sandbox) SandboxPanel.Create(safe);
        }

        // ------------------------------------------------------------------
        // Checkpoint toast - a brief confirmation that progress is banked.
        // ------------------------------------------------------------------

        private TubityXLabel checkpointToastText;
        private float checkpointToastTimer;
        private const float CheckpointToastDuration = 1.6f;

        private void CreateCheckpointToast(Transform canvas)
        {
            GameObject host = new GameObject("CheckpointToast", typeof(RectTransform));
            host.transform.SetParent(canvas, false);
            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.70f);
            rect.anchorMax = new Vector2(0.5f, 0.70f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 70f);

            checkpointToastText = TubityXUIFactory.AddLabel(host, "CHECKPOINT", 24f,
                                                            FaceWhite, TubityXUIFactory.Cyan, 5f);
            Color c = checkpointToastText.color;
            c.a = 0f;
            checkpointToastText.color = c;
        }

        public void ShowCheckpointToast()
        {
            ShowToast("CHECKPOINT");
        }

        /// <summary>A brief centred message - a checkpoint banked, a twin lost or won back.</summary>
        public void ShowToast(string text)
        {
            if (checkpointToastText == null) return;
            checkpointToastText.Text = text;
            checkpointToastTimer = CheckpointToastDuration;
        }

        /// <summary>Fades the checkpoint toast. Unscaled, so it still reads if the game pauses.</summary>
        private void UpdateCheckpointToast()
        {
            if (checkpointToastText == null) return;
            if (checkpointToastTimer <= 0f) return;

            checkpointToastTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(checkpointToastTimer / CheckpointToastDuration);
            // Snap in, linger, fade out.
            float alpha = (t > 0.75f) ? Mathf.InverseLerp(1f, 0.75f, t) : Mathf.Clamp01(t / 0.5f);

            Color c = checkpointToastText.color;
            c.a = alpha;
            checkpointToastText.color = c;
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
                safe, new Vector2(620f, 570f), TubityXUIFactory.Gold,
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
                sr.anchoredPosition = new Vector2((i - 1) * 100f, 0f);
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
                                           TubityXUIFactory.Cyan, 0.08f, -132f, false, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TriggerReplay();
            });
            GameObject lcMenuButton = AddCardButton(card, "MenuButton", "MAIN MENU", btnSize,
                          TubityXUIFactory.Purple, 0.08f, 132f, false, () =>
            {
                LeaveReview(AnimateLevelCompleteOut(), () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
                });
            });

            lcButtons = new[] { lcNextButton.GetComponent<RectTransform>(),
                                lcReplayButton.GetComponent<RectTransform>(),
                                lcMenuButton.GetComponent<RectTransform>() };

            levelCompletePanel.SetActive(false);
        }

        public void ShowLevelCompleteScreen(GameManager.LevelResult r)
        {
            if (levelCompletePanel == null || r == null) return;

            if (lcTitleText != null)
                lcTitleText.Text = r.isTestLevel || r.levelNumber <= 0 ? "LEVEL COMPLETE" : $"LEVEL {r.levelNumber} COMPLETE";

            if (lcShieldsText != null) lcShieldsText.Text = $"SHIELDS {r.shieldsPassed} / {r.shieldsSpawned}";
            int minutes = Mathf.FloorToInt(r.time / 60f);
            int seconds = Mathf.FloorToInt(r.time % 60f);

            if (lcNextButton != null) lcNextButton.SetActive(r.hasNextLevel);
            if (countdownHost != null) countdownHost.SetActive(false);
            levelCompletePanel.SetActive(true);
            StartCoroutine(AnimateLevelCompleteIn());

            // The card's contents land in order once it has opened: stars one by one, then the
            // numbers counting up, then the buttons. Each element hides itself on the first call,
            // so nothing shows early.
            const float starsAt = 0.30f, starGap = 0.17f;
            for (int i = 0; i < lcStars.Length; i++)
            {
                if (lcStars[i] != null) StartCoroutine(StarPop(lcStars[i], i < r.stars, starsAt + i * starGap));
            }

            float statsAt = starsAt + lcStars.Length * starGap + 0.05f;
            if (lcCoinsText != null)
            {
                StartCoroutine(RiseIn(lcCoinsText.rectTransform, statsAt));
                StartCoroutine(CountUp(lcCoinsText, r.coinsCollected, statsAt, 0.5f,
                                       n => $"COINS {n} / {r.coinsSpawned}"));
            }
            if (lcShieldsText != null) StartCoroutine(RiseIn(lcShieldsText.rectTransform, statsAt + 0.07f));
            if (lcTimeText != null)
            {
                StartCoroutine(RiseIn(lcTimeText.rectTransform, statsAt + 0.14f));
                StartCoroutine(CountUp(lcTimeText, r.score, statsAt + 0.14f, 0.5f,
                                       n => string.Format("TIME {0:00}:{1:00}   SCORE {2:D3}", minutes, seconds, n)));
            }

            float buttonsAt = statsAt + 0.30f;
            for (int i = 0; i < lcButtons.Length; i++)
            {
                if (lcButtons[i] != null && lcButtons[i].gameObject.activeSelf)
                    StartCoroutine(RiseIn(lcButtons[i], buttonsAt + i * 0.08f));
            }

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

        /// <summary>
        /// The pop-in played backwards: the card anticipates with a small swell, then slams flat
        /// and wide as the backdrop clears, taking a stacked sphere-unlock card with it. Buttons
        /// stop responding on the first frame so a second tap can't start a second transition.
        /// </summary>
        public System.Collections.IEnumerator AnimateLevelCompleteOut()
        {
            if (lcOverlayGroup == null || lcCardRect == null || !levelCompletePanel.activeSelf) yield break;

            lcOverlayGroup.interactable = false;
            lcOverlayGroup.blocksRaycasts = false;
            bool withUnlock = sphereUnlockPanel != null && sphereUnlockPanel.activeSelf && suOverlayGroup != null;
            if (withUnlock) suOverlayGroup.interactable = false;

            const float duration = 0.32f;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            Vector3 endScale = new Vector3(1.25f, 0.05f, 1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float anticipate = c3 * t * t * t - c1 * t * t;   // "back" ease-in: dips below 0 first

                Vector3 scale = Vector3.LerpUnclamped(Vector3.one, endScale, anticipate);
                float alpha = 1f - Mathf.Clamp01((t - 0.35f) / 0.65f);
                lcCardRect.localScale = scale;
                lcOverlayGroup.alpha = alpha;
                if (withUnlock)
                {
                    suCardRect.localScale = scale;
                    suOverlayGroup.alpha = alpha;
                }
                yield return null;
            }

            levelCompletePanel.SetActive(false);
            if (withUnlock) sphereUnlockPanel.SetActive(false);
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
                safe, new Vector2(540f, 430f), TubityXUIFactory.Cyan,
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
        // Top-right stats: score, coins, time - one framed pill each, icon + value
        // ------------------------------------------------------------------
        private void CreateStatsPanel(RectTransform safe)
        {
            GameObject columnObj = new GameObject("HudColumn", typeof(RectTransform));
            columnObj.transform.SetParent(safe, false);
            hudColumn = columnObj.GetComponent<RectTransform>();
            hudColumn.anchorMin = new Vector2(1f, 1f);
            hudColumn.anchorMax = new Vector2(1f, 1f);
            hudColumn.pivot = new Vector2(1f, 1f);
            hudColumn.anchoredPosition = new Vector2(-EdgeMargin, -EdgeMargin);
            StackVertically(columnObj);

            // The stats get a group of their own so the tutorial can hide them without taking
            // the powerups with them.
            GameObject statsObj = new GameObject("Stats", typeof(RectTransform));
            statsObj.transform.SetParent(hudColumn, false);
            StackVertically(statsObj, sizeToFit: false);   // the column sizes it
            statsPanelObj = statsObj;
            statsObj.SetActive(showStatsPanel);

            scoreText = CreatePill(statsObj.transform, "ScorePill", StarSprite(), TubityXUIFactory.Cyan, FaceWhite, "000");
            coinText = CreatePill(statsObj.transform, "CoinPill", HudIcons.Coin(), TubityXUIFactory.Gold, TubityXUIFactory.Gold, "000");
            timeText = CreatePill(statsObj.transform, "TimePill", HudIcons.Clock(), TubityXUIFactory.Blue, CoolWhite, "00:00");
        }

        /// <summary>Show or hide the stats panel; safe to call before or after the HUD is built.</summary>
        public void SetStatsPanelVisible(bool visible)
        {
            showStatsPanel = visible;
            if (statsPanelObj != null) statsPanelObj.SetActive(visible);
        }

        /// <summary>Right-aligned vertical stack. The outermost one sizes itself to its contents;
        /// a stack inside another is sized by its parent's layout.</summary>
        private static void StackVertically(GameObject obj, bool sizeToFit = true)
        {
            VerticalLayoutGroup stack = obj.AddComponent<VerticalLayoutGroup>();
            stack.spacing = PillGap;
            stack.childAlignment = TextAnchor.UpperRight;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;
            if (!sizeToFit) return;

            ContentSizeFitter fit = obj.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>One framed pill: an icon on the left in the accent colour, the value right-aligned.</summary>
        private static GameObject CreatePillFrame(Transform parent, string name, Sprite icon, Color accent)
        {
            GameObject pill = TubityXUIFactory.CreatePanel(
                parent, new Vector2(PillWidth, PillHeight), accent,
                new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, PillHeight * 0.5f);
            pill.name = name;
            pill.GetComponent<TubityXPanel>().raycastTarget = false;   // never eat gameplay taps

            LayoutElement size = pill.AddComponent<LayoutElement>();
            size.preferredWidth = PillWidth;
            size.preferredHeight = PillHeight;

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(pill.transform, false);
            RectTransform ir = iconObj.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0f, 0.5f);
            ir.anchorMax = new Vector2(0f, 0.5f);
            ir.pivot = new Vector2(0f, 0.5f);
            ir.anchoredPosition = new Vector2(PillHeight * 0.32f, 0f);
            ir.sizeDelta = new Vector2(PillIcon, PillIcon);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = icon;
            img.color = accent;
            img.preserveAspect = true;
            img.raycastTarget = false;

            return pill;
        }

        private static TubityXLabel CreatePill(Transform parent, string name, Sprite icon, Color accent,
                                               Color face, string text)
        {
            GameObject pill = CreatePillFrame(parent, name, icon, accent);

            GameObject host = new GameObject("Value", typeof(RectTransform));
            host.transform.SetParent(pill.transform, false);
            RectTransform r = host.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(PillHeight * 0.32f + PillIcon + 8f, 0f);
            r.offsetMax = new Vector2(-PillHeight * 0.42f, 0f);
            return TubityXUIFactory.AddLabel(host, text, 17f, face, accent, 2.5f, TubityXLabel.Align.Right);
        }

        // ------------------------------------------------------------------
        // Powerup pills, stacked under the stats in the same column
        // ------------------------------------------------------------------
        private void CreatePowerupPanel(RectTransform safe)
        {
            invinciblePill = CreatePowerupPill("InvinciblePill", HudIcons.Bolt(), PowerCyan, out invincibleFill);
            magnetPill = CreatePowerupPill("MagnetPill", HudIcons.Magnet(), PowerPurple, out magnetFill);
        }

        private GameObject CreatePowerupPill(string name, Sprite icon, Color accent, out RectTransform fill)
        {
            GameObject pill = CreatePillFrame(hudColumn, name, icon, accent);

            // Track and fill: the fill's right edge is the time left.
            GameObject track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(pill.transform, false);
            RectTransform tr = track.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 0.5f);
            tr.anchorMax = new Vector2(1f, 0.5f);
            tr.offsetMin = new Vector2(PillHeight * 0.32f + PillIcon + 10f, -4f);
            tr.offsetMax = new Vector2(-PillHeight * 0.42f, 4f);
            Image trackImg = track.AddComponent<Image>();
            trackImg.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            trackImg.raycastTarget = false;

            GameObject bar = new GameObject("Fill", typeof(RectTransform));
            bar.transform.SetParent(track.transform, false);
            fill = bar.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImg = bar.AddComponent<Image>();
            fillImg.color = accent;
            fillImg.raycastTarget = false;

            pill.SetActive(false);
            return pill;
        }

        /// <summary>Shows a powerup pill while its effect runs, its bar at the fraction left.</summary>
        private static void SetPowerupPill(GameObject pill, RectTransform fill, bool active, float remaining)
        {
            if (pill == null) return;
            if (pill.activeSelf != active) pill.SetActive(active);
            if (active) fill.anchorMax = new Vector2(Mathf.Clamp01(remaining), 1f);
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
            // Generous side margins: card text never runs close to the rim.
            r.anchorMin = new Vector2(0.08f, yMin);
            r.anchorMax = new Vector2(0.92f, yMax);
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

            goOverlayGroup = gameOverPanel.AddComponent<CanvasGroup>();

            GameObject card = TubityXUIFactory.CreatePanel(
                safe, new Vector2(580f, 420f), DangerRed,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 26f);
            card.name = "CardPanel";
            goCardRect = card.GetComponent<RectTransform>();

            goTitleText = AddCardLabel(card, "TitleText", 0.74f, 0.96f, "GAME OVER", 30f, FaceWhite, DangerRed);
            finalScoreText = AddCardLabel(card, "ScoreText", 0.54f, 0.68f, "FINAL SCORE: 000", 15f,
                                          FaceWhite, TubityXUIFactory.Cyan);
            finalCoinsText = AddCardLabel(card, "CoinsText", 0.40f, 0.54f, "COINS COLLECTED: 000", 15f,
                                          TubityXUIFactory.Gold, TubityXUIFactory.Gold);

            Vector2 btnSize = new Vector2(220f, 58f);
            replayButton = AddCardButton(card, "ReplayButton", "REPLAY", btnSize,
                                         TubityXUIFactory.Blue, 0.18f, -132f, true, () =>
            {
                LeaveReview(SlamCardOut(goOverlayGroup, goCardRect, gameOverPanel), () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.TriggerReplay();
                });
            });

            GameObject goMenuButton = AddCardButton(card, "MenuButton", "MAIN MENU", btnSize,
                          TubityXUIFactory.Purple, 0.18f, 132f, false, () =>
            {
                LeaveReview(SlamCardOut(goOverlayGroup, goCardRect, gameOverPanel), () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
                });
            });

            goButtons = new[] { replayButton.GetComponent<RectTransform>(), goMenuButton.GetComponent<RectTransform>() };

            gameOverPanel.SetActive(false);
        }

        private void CreatePausePopup(Transform canvas)
        {
            RectTransform safe;
            pausePanel = CreateOverlay(canvas, "PausePanel", out safe);
            pauseGroup = pausePanel.AddComponent<CanvasGroup>();

            GameObject card = TubityXUIFactory.CreatePanel(
                safe, new Vector2(520f, 470f), TubityXUIFactory.Cyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 26f);
            card.name = "PauseCardPanel";
            pauseCardRect = card.GetComponent<RectTransform>();

            AddCardLabel(card, "PauseTitleText", 0.76f, 0.97f, "PAUSED", 30f, FaceWhite, TubityXUIFactory.Cyan);

            Vector2 btnSize = new Vector2(300f, 58f);
            resumeButton = AddCardButton(card, "ResumeButton", "RESUME", btnSize,
                                         TubityXUIFactory.Blue, 0.56f, 0f, true, TogglePauseMenu);

            // The world stays paused through the card's exit: GameManager clears the level away
            // and flies it back in, restarting time itself at the hand-off.
            AddCardButton(card, "PauseReplayButton", "REPLAY", btnSize,
                          TubityXUIFactory.Cyan, 0.37f, 0f, false, () =>
            {
                LeaveReview(SlamCardOut(pauseGroup, pauseCardRect, pausePanel), () =>
                {
                    IsPaused = false;
                    if (GameManager.Instance != null) GameManager.Instance.TriggerReplay();
                });
            });

            AddCardButton(card, "PauseMenuButton", "MAIN MENU", btnSize,
                          TubityXUIFactory.Purple, 0.18f, 0f, false, () =>
            {
                // Leaving the tutorial part-way deserves a second look; the game stays paused meanwhile.
                if (FTUEManager.Instance != null && exitTutorialPanel != null)
                {
                    pausePanel.SetActive(false);
                    exitTutorialPanel.SetActive(true);
                    if (TVOSMenuNavigator.Instance != null && keepGoingButton != null)
                        TVOSMenuNavigator.Instance.SetFocus(keepGoingButton);
                    return;
                }

                Time.timeScale = 1f;
                IsPaused = false;
                if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
            });

            pausePanel.SetActive(false);
        }

        /// <summary>
        /// Confirmation shown when MAIN MENU is chosen from the pause popup during the
        /// tutorial. KEEP GOING returns to the pause popup; LEAVE ends the run.
        /// </summary>
        private void CreateExitTutorialPopup(Transform canvas)
        {
            RectTransform safe;
            exitTutorialPanel = CreateOverlay(canvas, "ExitTutorialPanel", out safe);

            GameObject card = TubityXUIFactory.CreatePanel(
                safe, new Vector2(620f, 400f), TubityXUIFactory.Gold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 26f);
            card.name = "ExitTutorialCardPanel";

            AddCardLabel(card, "TitleText", 0.72f, 0.95f, "LEAVE THE TUTORIAL?", 26f, FaceWhite, TubityXUIFactory.Gold);
            AddCardLabel(card, "SubText", 0.56f, 0.70f, "YOU CAN REPLAY IT ANY TIME FROM HOW TO PLAY", 12f,
                         CoolWhite, TubityXUIFactory.Cyan);

            Vector2 btnSize = new Vector2(300f, 58f);
            keepGoingButton = AddCardButton(card, "KeepGoingButton", "KEEP GOING", btnSize,
                                            TubityXUIFactory.Blue, 0.38f, 0f, true, () =>
            {
                exitTutorialPanel.SetActive(false);
                if (pausePanel != null)
                {
                    pausePanel.SetActive(true);
                    if (pausePop != null) StopCoroutine(pausePop);
                    pausePop = StartCoroutine(PopCardIn(pauseGroup, pauseCardRect));
                }
                if (TVOSMenuNavigator.Instance != null && resumeButton != null)
                    TVOSMenuNavigator.Instance.SetFocus(resumeButton);
            });

            AddCardButton(card, "LeaveTutorialButton", "LEAVE", btnSize,
                          TubityXUIFactory.Purple, 0.16f, 0f, false, () =>
            {
                exitTutorialPanel.SetActive(false);
                Time.timeScale = 1f;
                IsPaused = false;
                if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
            });

            exitTutorialPanel.SetActive(false);
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
            // Past the finish gate the review card is the only menu; the tube keeps moving under it.
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsLevelComplete)) return;
            // A card's button has been pressed and its exit (or a replay) is under way.
            if (reviewLeaving || pauseClosing || (GameManager.Instance != null && GameManager.Instance.IsTransitioning)) return;

            // A pop still running is cut short and the card put back to rest, so whatever comes
            // next - the slam-out, or the next pause - starts from the top.
            if (pausePop != null) { StopCoroutine(pausePop); pausePop = null; }
            if (pauseCardRect != null) pauseCardRect.localScale = Vector3.one;
            if (pauseGroup != null) { pauseGroup.alpha = 1f; pauseGroup.interactable = true; }

            if (!IsPaused)
            {
                IsPaused = true;
                Time.timeScale = 0f;
                if (pausePanel != null)
                {
                    pausePanel.SetActive(true);
                    pausePop = StartCoroutine(PopCardIn(pauseGroup, pauseCardRect));
                    if (TVOSMenuNavigator.Instance != null && resumeButton != null)
                        TVOSMenuNavigator.Instance.SetFocus(resumeButton);
                }
                return;
            }

            // Unpausing from the keyboard / remote while the exit confirm is up dismisses it too;
            // the pause card is already hidden behind it, so there is nothing to slam out.
            if (exitTutorialPanel != null && exitTutorialPanel.activeSelf) exitTutorialPanel.SetActive(false);

            if (pausePanel != null && pausePanel.activeSelf)
            {
                pauseClosing = true;
                StartCoroutine(ResumeAfterSlam());
                return;
            }

            IsPaused = false;
            Time.timeScale = 1f;
        }

        /// <summary>Resume: the card slams out with the world still held, then play picks up.</summary>
        private System.Collections.IEnumerator ResumeAfterSlam()
        {
            yield return StartCoroutine(SlamCardOut(pauseGroup, pauseCardRect, pausePanel));
            IsPaused = false;
            Time.timeScale = 1f;
            pauseClosing = false;
        }

        /// <summary>
        /// A card's generic pop-in, the same spring as the level-complete card's: the backdrop
        /// fades in while the card opens from a flat squash, overshoots and settles. Unscaled
        /// time, so it plays with the game paused. Buttons wake once it has landed.
        /// </summary>
        private System.Collections.IEnumerator PopCardIn(CanvasGroup group, RectTransform card)
        {
            if (group == null || card == null) yield break;

            const float duration = 0.4f;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            Vector3 startScale = new Vector3(1.25f, 0.05f, 1f);
            card.localScale = startScale;
            group.alpha = 0f;
            group.interactable = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float tm1 = t - 1f;
                float overshoot = 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;
                card.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, overshoot);
                group.alpha = Mathf.Clamp01(t / 0.6f);
                yield return null;
            }

            card.localScale = Vector3.one;
            group.alpha = 1f;
            group.interactable = true;
            if (card == pauseCardRect) pausePop = null;
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

        /// <summary>The exit animation in reverse, for a run entered in flight (the level-to-level
        /// hand-off) so the stats don't pop on over a level that is still assembling.</summary>
        public void PlayGameplayHudEnterAnimation()
        {
            if (gameplayHudGroup == null) return;
            StartCoroutine(AnimateGameplayHudIn());
        }

        private System.Collections.IEnumerator AnimateGameplayHudIn()
        {
            RectTransform rt = gameplayHudGroup.GetComponent<RectTransform>();
            const float duration = 0.45f;
            float elapsed = 0f;
            gameplayHudGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - (1f - t) * (1f - t); // decelerates in

                gameplayHudGroup.alpha = ease;
                rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, ease);
                yield return null;
            }

            gameplayHudGroup.alpha = 1f;
            rt.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------
        // Review card animation: the crash card, and the pieces both cards share
        // ------------------------------------------------------------------

        /// <summary>A beat after the crash before the card comes, so the hit registers first.</summary>
        private const float GameOverBeat = 0.35f;
        /// <summary>How long the card's pop-in takes (PopCardIn).</summary>
        private const float GameOverPop = 0.40f;

        /// <summary>
        /// The crash card: held back for a beat so the hit registers, then the same pop-in as
        /// every other card, and the title flickers like a failing sign as it lands. Unscaled
        /// time throughout - the world is paused under it.
        /// </summary>
        private System.Collections.IEnumerator AnimateGameOverIn()
        {
            if (goOverlayGroup == null || goCardRect == null) yield break;

            // Hidden through the beat, in the pose the pop starts from.
            goOverlayGroup.alpha = 0f;
            goOverlayGroup.interactable = false;
            goCardRect.anchoredPosition = Vector2.zero;
            goCardRect.localRotation = Quaternion.identity;
            goCardRect.localScale = new Vector3(1.25f, 0.05f, 1f);

            yield return new WaitForSecondsRealtime(GameOverBeat);
            yield return StartCoroutine(PopCardIn(goOverlayGroup, goCardRect));

            if (goTitleText != null) StartCoroutine(Flicker(goTitleText));
        }

        /// <summary>
        /// A card's generic exit, the same motion as the level-complete card's: a small swell,
        /// then it slams flat and wide as its backdrop clears. Unscaled time, for paused cards.
        /// </summary>
        private System.Collections.IEnumerator SlamCardOut(CanvasGroup group, RectTransform card, GameObject panel)
        {
            if (group == null || card == null || panel == null || !panel.activeSelf) yield break;

            group.interactable = false;
            group.blocksRaycasts = false;

            const float duration = 0.32f;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            Vector3 endScale = new Vector3(1.25f, 0.05f, 1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float anticipate = c3 * t * t * t - c1 * t * t;
                card.localScale = Vector3.LerpUnclamped(Vector3.one, endScale, anticipate);
                group.alpha = 1f - Mathf.Clamp01((t - 0.35f) / 0.65f);
                yield return null;
            }

            panel.SetActive(false);
            card.localScale = Vector3.one;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        /// <summary>Plays a card's exit, then does what the button asked. Ignores repeat presses.</summary>
        private void LeaveReview(System.Collections.IEnumerator exit, System.Action then)
        {
            if (reviewLeaving) return;
            reviewLeaving = true;
            StartCoroutine(ExitThen(exit, then));
        }

        private System.Collections.IEnumerator ExitThen(System.Collections.IEnumerator exit, System.Action then)
        {
            yield return StartCoroutine(exit);
            then?.Invoke();
        }

        private static CanvasGroup GroupOf(Component c)
        {
            CanvasGroup g = c.GetComponent<CanvasGroup>();
            return g != null ? g : c.gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Hides an element at once, then after `delay` brings it up into place from a little
        /// below with a slight overshoot. A hidden button takes no taps until it starts to show.
        /// </summary>
        private System.Collections.IEnumerator RiseIn(RectTransform rt, float delay,
                                                      float rise = 26f, float duration = 0.3f)
        {
            CanvasGroup g = GroupOf(rt);
            Vector2 home = rt.anchoredPosition;
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;
            rt.anchoredPosition = home - new Vector2(0f, rise);

            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            g.interactable = true;
            g.blocksRaycasts = true;

            const float c1 = 1.2f, c3 = c1 + 1f;   // a gentler "back" than the card's own spring
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float tm1 = t - 1f;
                float ease = 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;
                rt.anchoredPosition = home - new Vector2(0f, rise * (1f - ease));
                g.alpha = Mathf.Clamp01(t * 1.6f);
                yield return null;
            }

            rt.anchoredPosition = home;
            g.alpha = 1f;
        }

        /// <summary>Counts a number up from zero on an ease-out, so it slows as it lands.</summary>
        private System.Collections.IEnumerator CountUp(TubityXLabel label, int target, float delay,
                                                       float duration, System.Func<int, string> format)
        {
            label.Text = format(0);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            float elapsed = 0f;
            while (elapsed < duration && target > 0)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - (1f - t) * (1f - t);
                label.Text = format(Mathf.RoundToInt(target * ease));
                yield return null;
            }
            label.Text = format(target);
        }

        /// <summary>
        /// A star lands: earned ones punch in from nothing with a spin and a white-hot flash
        /// cooling to gold, with a chime; unearned ones just settle in quietly.
        /// </summary>
        private System.Collections.IEnumerator StarPop(Image star, bool lit, float delay)
        {
            RectTransform rt = star.rectTransform;
            rt.localScale = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            star.color = lit ? StarLit : StarDim;

            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (lit && player != null) player.PlaySound(ProceduralAudio.GetCoinSound());

            const float duration = 0.34f;
            const float c1 = 2.6f, c3 = c1 + 1f;   // a big overshoot: the star punches past full size
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (lit)
                {
                    float tm1 = t - 1f;
                    float ease = 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;
                    rt.localScale = Vector3.one * ease;
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-45f, 0f, 1f - (1f - t) * (1f - t)));
                    star.color = Color.Lerp(Color.white, StarLit, t);
                }
                else
                {
                    rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1f - (1f - t) * (1f - t));
                }
                yield return null;
            }

            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            star.color = lit ? StarLit : StarDim;
        }

        /// <summary>A failing-sign flicker: a few uneven drop-outs, then steady.</summary>
        private System.Collections.IEnumerator Flicker(TubityXLabel label)
        {
            CanvasGroup g = GroupOf(label);
            float[] beats = { 0.05f, 0.04f, 0.07f, 0.03f, 0.09f, 0.05f };
            for (int i = 0; i < beats.Length; i++)
            {
                g.alpha = (i % 2 == 0) ? 0.2f : 1f;
                yield return new WaitForSecondsRealtime(beats[i]);
            }
            g.alpha = 1f;
        }

        public void ShowGameOverScreen()
        {
            if (gameOverPanel == null) return;

            int score = player != null ? player.Score : 0;
            int coins = player != null ? player.Coins : 0;

            gameOverPanel.SetActive(true);
            StartCoroutine(AnimateGameOverIn());

            // Timed from the card's landing (see AnimateGameOverIn): the numbers count up, then
            // the buttons rise in.
            const float landed = GameOverBeat + GameOverPop;
            if (finalScoreText != null)
            {
                StartCoroutine(RiseIn(finalScoreText.rectTransform, landed + 0.12f));
                StartCoroutine(CountUp(finalScoreText, score, landed + 0.12f, 0.55f, n => "FINAL SCORE: " + n.ToString("D3")));
            }
            if (finalCoinsText != null)
            {
                StartCoroutine(RiseIn(finalCoinsText.rectTransform, landed + 0.20f));
                StartCoroutine(CountUp(finalCoinsText, coins, landed + 0.20f, 0.55f, n => "COINS COLLECTED: " + n.ToString("D3")));
            }
            for (int i = 0; i < goButtons.Length; i++)
            {
                if (goButtons[i] != null) StartCoroutine(RiseIn(goButtons[i], landed + 0.45f + i * 0.08f));
            }

            if (TVOSMenuNavigator.Instance != null && replayButton != null)
            {
                TVOSMenuNavigator.Instance.SetFocus(replayButton);
            }
        }

        private void Update()
        {
            // The toast fades on its own clock, before any of the early-outs below.
            UpdateCheckpointToast();

            if (player == null || scoreText == null || coinText == null || timeText == null) return;

            // Only update active UI stats if game is not over (or the finish gate has been crossed)
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsLevelComplete)) return;

            UpdateCountdown();

            scoreText.Text = player.Score.ToString("D3");
            coinText.Text = player.Coins.ToString("D3");

            int minutes = Mathf.FloorToInt(player.TimeElapsed / 60f);
            int seconds = Mathf.FloorToInt(player.TimeElapsed % 60f);
            timeText.Text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Both can run at once; each has its own pill, stacked under the stats.
            SetPowerupPill(invinciblePill, invincibleFill, player.IsInvincible,
                           player.InvincibilityTimeRemaining / player.InvincibilityTotalTime);
            SetPowerupPill(magnetPill, magnetFill, player.IsMagnetActive,
                           player.MagnetTimeRemaining / player.MagnetTotalTime);
        }
    }
}
