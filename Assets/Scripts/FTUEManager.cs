using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TubityWAI
{
    public enum FTUEStep
    {
        RotateClockwise,        // Step 1: Full circle clockwise
        RotateCounterClockwise, // Step 2: Full circle counter-clockwise
        SingleJump,             // Step 3: Single jump (dismisses upon landing)
        DoubleJump,             // Step 4: Double jump crossover (dismisses upon landing)
        Complete                // Step 5: Finished
    }

    public class FTUEManager : MonoBehaviour
    {
        public static FTUEManager Instance { get; private set; }

        public FTUEStep CurrentStep { get; private set; } = FTUEStep.RotateClockwise;

        private PlayerController player;
        private GameObject canvasObj;
        
        // Target overlay elements - a TubityX glass panel with a pulsing neon
        // rim, a title line and a hint line in the display face.
        private GameObject targetOverlayObj;
        private RectTransform overlayRect;
        private TubityXPanel overlayChrome;
        private TubityXLabel overlayTitle;
        private TubityXLabel overlayHint;

        // Progress dial (center of screen): round glass panel, radial fill, percent
        private GameObject progressContainerObj;
        private TubityXPanel progressChrome;
        private Image radialFillImage;
        private TubityXLabel percentText;

        private static readonly Color Gold = TubityXUIFactory.Gold;
        private static readonly Color Cyan = new Color(0f, 1f, 1f);
        private static readonly Color Pink = new Color(1f, 0.25f, 0.65f);
        private static readonly Color Mint = new Color(0f, 1f, 0.55f);

        private float lastAngle = 0f;
        private float accumulatedClockwiseAngle = 0f;
        private float accumulatedCounterClockwiseAngle = 0f;
        private const float FULL_CIRCLE_RADIANS = Mathf.PI * 2f;

        private bool wasJumping = false;
        private bool hasInitiatedDoubleJump = false;
        private bool isTransitioningStep = false;

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                lastAngle = player.currentAngle;
            }

            CreateFTUEUI();
            SetStep(FTUEStep.RotateClockwise);
        }

        private void CreateFTUEUI()
        {
            // 1. Create Canvas
            canvasObj = new GameObject("FTUECanvas");
            canvasObj.transform.SetParent(this.transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Above HUD
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                             | AdditionalCanvasShaderChannels.TexCoord2;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // The overlays are anchored to screen edges; keep them off the notch.
            RectTransform safe = SafeAreaFitter.Create(canvasObj.transform, "SafeArea");

            // 2. Pulsing Target Overlay - anchors are set per step in SetStep
            targetOverlayObj = TubityXUIFactory.CreatePanel(
                safe, Vector2.zero, Cyan, new Vector2(0.02f, 0.33f), new Vector2(0.48f, 0.95f),
                Vector2.zero, 28f);
            targetOverlayObj.name = "PulsingTargetOverlay";
            overlayRect = targetOverlayObj.GetComponent<RectTransform>();
            overlayChrome = targetOverlayObj.GetComponent<TubityXPanel>();
            overlayChrome.PulseAmount = 1f;          // the shader pulses the rim for us
            overlayChrome.raycastTarget = false;     // taps must reach the gameplay input

            overlayTitle = AddOverlayLine(targetOverlayObj, "OverlayTitle", 24f, 22f, Gold, Gold);
            overlayHint = AddOverlayLine(targetOverlayObj, "OverlayHint", -22f, 14f, Color.white, Cyan);

            // 3. Center progress dial: round glass panel with a radial fill inside
            float dial = 220f;
            progressContainerObj = TubityXUIFactory.CreatePanel(
                safe, new Vector2(dial, dial), Gold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f), dial * 0.5f);
            progressContainerObj.name = "CenterProgressGraphic";
            progressChrome = progressContainerObj.GetComponent<TubityXPanel>();
            progressChrome.raycastTarget = false;

            GameObject fillObj = new GameObject("RadialFill", typeof(RectTransform));
            fillObj.transform.SetParent(progressContainerObj.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-24f, -24f);

            radialFillImage = fillObj.AddComponent<Image>();
            radialFillImage.sprite = GlassUIFactory.GetCircleSprite();
            radialFillImage.type = Image.Type.Filled;
            radialFillImage.fillMethod = Image.FillMethod.Radial360;
            radialFillImage.fillOrigin = (int)Image.Origin360.Top;
            radialFillImage.fillClockwise = true;
            radialFillImage.fillAmount = 0f;
            radialFillImage.color = new Color(0f, 1f, 1f, 0.35f);
            radialFillImage.raycastTarget = false;

            GameObject percentObj = new GameObject("PercentText", typeof(RectTransform));
            percentObj.transform.SetParent(progressContainerObj.transform, false);
            RectTransform percentRect = percentObj.GetComponent<RectTransform>();
            percentRect.anchorMin = new Vector2(0.1f, 0.3f);
            percentRect.anchorMax = new Vector2(0.9f, 0.7f);
            percentRect.sizeDelta = Vector2.zero;

            percentText = TubityXUIFactory.AddLabel(percentObj, "0%", 30f, Color.white, Cyan, 3f);

            progressContainerObj.SetActive(false);
        }

        /// <summary>One line of overlay copy, vertically offset from the panel centre.</summary>
        private static TubityXLabel AddOverlayLine(GameObject panel, string name, float yOffset,
                                                   float cap, Color face, Color accent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(panel.transform, false);
            RectTransform r = host.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.03f, 0.5f);
            r.anchorMax = new Vector2(0.97f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(0f, cap * 2.4f);
            r.anchoredPosition = new Vector2(0f, yOffset);
            return TubityXUIFactory.AddLabel(host, "", cap, face, accent, cap * 0.16f);
        }

        /// <summary>Title on the first line, hint on the second; either may be empty.</summary>
        private void SetOverlayText(string title, string hint, Color face)
        {
            if (overlayTitle != null)
            {
                overlayTitle.Text = title;
                overlayTitle.color = face;
                overlayTitle.RimColor = face;
                RectTransform tr = overlayTitle.transform.parent as RectTransform;
                if (tr != null) tr.anchoredPosition = new Vector2(0f, string.IsNullOrEmpty(hint) ? 0f : 22f);
            }
            if (overlayHint != null)
            {
                overlayHint.Text = hint ?? "";
            }
        }

        private void SetOverlayText(string title, Color face)
        {
            SetOverlayText(title, "", face);
        }

        private void SetOverlayRim(Color rim)
        {
            if (overlayChrome != null) overlayChrome.RimColor = rim;
        }

        private void SetProgressColors(Color fill, Color rim)
        {
            if (radialFillImage != null)
                radialFillImage.color = new Color(fill.r, fill.g, fill.b, 0.35f);
            if (progressChrome != null) progressChrome.RimColor = rim;
        }

        private void SetStep(FTUEStep step)
        {
            CurrentStep = step;
            isTransitioningStep = false;

            if (radialFillImage != null) radialFillImage.fillAmount = 0f;
            SetProgressColors(Cyan, Gold);

            switch (step)
            {
                case FTUEStep.RotateClockwise:
                    overlayRect.anchorMin = new Vector2(0.52f, 0.33f);
                    overlayRect.anchorMax = new Vector2(0.98f, 0.95f);
                    overlayRect.sizeDelta = Vector2.zero;
                    SetOverlayRim(Cyan);
                    SetOverlayText("STEP 1: ROTATE CLOCKWISE", "TAP / HOLD HERE \u25B6\u25B6", Gold);
                    if (progressContainerObj != null) progressContainerObj.SetActive(false);
                    break;

                case FTUEStep.RotateCounterClockwise:
                    overlayRect.anchorMin = new Vector2(0.02f, 0.33f);
                    overlayRect.anchorMax = new Vector2(0.48f, 0.95f);
                    overlayRect.sizeDelta = Vector2.zero;
                    SetOverlayRim(Pink);
                    SetOverlayText("STEP 2: ROTATE COUNTER-CLOCKWISE", "\u25C0\u25C0 TAP / HOLD HERE", Gold);
                    if (progressContainerObj != null) progressContainerObj.SetActive(false);
                    break;

                case FTUEStep.SingleJump:
                    overlayRect.anchorMin = new Vector2(0.05f, 0.05f);
                    overlayRect.anchorMax = new Vector2(0.95f, 0.28f);
                    overlayRect.sizeDelta = Vector2.zero;
                    SetOverlayRim(Gold);
                    SetOverlayText("STEP 3: SINGLE JUMP", "TAP BOTTOM AREA OR PRESS SPACE", Gold);
                    if (progressContainerObj != null) progressContainerObj.SetActive(false); // No loading graphic for jump
                    wasJumping = false;
                    break;

                case FTUEStep.DoubleJump:
                    overlayRect.anchorMin = new Vector2(0.05f, 0.05f);
                    overlayRect.anchorMax = new Vector2(0.95f, 0.28f);
                    overlayRect.sizeDelta = Vector2.zero;
                    SetOverlayRim(Mint);
                    SetOverlayText("STEP 4: DOUBLE JUMP CROSSOVER", "DOUBLE TAP BOTTOM OR PRESS SPACE TWICE", Gold);
                    if (progressContainerObj != null) progressContainerObj.SetActive(false); // No loading graphic for double jump
                    wasJumping = false;
                    hasInitiatedDoubleJump = false;
                    break;

                case FTUEStep.Complete:
                    if (targetOverlayObj != null) targetOverlayObj.SetActive(false);
                    if (progressContainerObj != null) progressContainerObj.SetActive(false);
                    break;
            }
        }

        private void Update()
        {
            if (CurrentStep == FTUEStep.Complete || isTransitioningStep) return;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                lastAngle = player.currentAngle;
            }

            // Track radial angle delta
            float currentAng = player.currentAngle;
            float delta = currentAng - lastAngle;
            if (delta < -Mathf.PI) delta += Mathf.PI * 2f;
            if (delta > Mathf.PI) delta -= Mathf.PI * 2f;
            lastAngle = currentAng;

            switch (CurrentStep)
            {
                case FTUEStep.RotateClockwise:
                    if (delta > 0f)
                    {
                        accumulatedClockwiseAngle += delta;
                        if (!progressContainerObj.activeSelf) progressContainerObj.SetActive(true);
                    }
                    float progClockwise = Mathf.Clamp01(accumulatedClockwiseAngle / FULL_CIRCLE_RADIANS);
                    if (radialFillImage != null) radialFillImage.fillAmount = progClockwise;
                    if (percentText != null) percentText.Text = Mathf.RoundToInt(progClockwise * 100f) + "%";

                    if (progClockwise >= 1.0f)
                    {
                        isTransitioningStep = true;
                        StartCoroutine(TransitionToNextStep(FTUEStep.RotateCounterClockwise, "STEP 1 COMPLETE!"));
                    }
                    break;

                case FTUEStep.RotateCounterClockwise:
                    if (delta < 0f)
                    {
                        accumulatedCounterClockwiseAngle += -delta;
                        if (!progressContainerObj.activeSelf) progressContainerObj.SetActive(true);
                    }
                    float progCounter = Mathf.Clamp01(accumulatedCounterClockwiseAngle / FULL_CIRCLE_RADIANS);
                    if (radialFillImage != null) radialFillImage.fillAmount = progCounter;
                    if (percentText != null) percentText.Text = Mathf.RoundToInt(progCounter * 100f) + "%";

                    if (progCounter >= 1.0f)
                    {
                        isTransitioningStep = true;
                        StartCoroutine(TransitionToNextStep(FTUEStep.SingleJump, "STEP 2 COMPLETE!"));
                    }
                    break;

                case FTUEStep.SingleJump:
                    if (player.IsJumping)
                    {
                        wasJumping = true;
                        SetOverlayText("JUMP IN FLIGHT...", Gold);
                    }
                    else if (wasJumping)
                    {
                        // Jump has landed!
                        isTransitioningStep = true;
                        StartCoroutine(TransitionToNextStep(FTUEStep.DoubleJump, "JUMP LANDED! PERFECT!"));
                    }
                    break;

                case FTUEStep.DoubleJump:
                    if (player.IsJumping)
                    {
                        wasJumping = true;
                        if (player.HasCrossedOver)
                        {
                            hasInitiatedDoubleJump = true;
                            SetOverlayText("CROSSOVER JUMP IN FLIGHT!", Gold);
                        }
                    }
                    else if (wasJumping && hasInitiatedDoubleJump)
                    {
                        // Crossover double jump has landed!
                        isTransitioningStep = true;
                        StartCoroutine(CompleteTutorialAndExit());
                    }
                    break;
            }
        }

        private IEnumerator TransitionToNextStep(FTUEStep nextStep, string successMessage)
        {
            SetOverlayText(successMessage, Mint);
            SetOverlayRim(Mint);
            SetProgressColors(Mint, Mint);

            yield return new WaitForSeconds(0.9f);

            SetStep(nextStep);
        }

        private IEnumerator CompleteTutorialAndExit()
        {
            CurrentStep = FTUEStep.Complete;

            SetOverlayText("TUTORIAL COMPLETE!", "YOU ARE READY TO FLY!", Mint);
            SetOverlayRim(Mint);

            yield return new WaitForSeconds(1.2f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
