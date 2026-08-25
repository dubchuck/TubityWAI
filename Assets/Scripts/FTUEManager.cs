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
        
        // Target overlay elements
        private GameObject targetOverlayObj;
        private RectTransform overlayRect;
        private Outline overlayBorder;
        private Text overlayText;

        // Progress loading graphic elements (center of screen)
        private GameObject progressContainerObj;
        private Image borderRingImage;
        private Image radialFillImage;
        private Text percentText;

        private Font defaultFont;

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

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
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

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // 2. Pulsing Target Overlay Container
            targetOverlayObj = new GameObject("PulsingTargetOverlay");
            targetOverlayObj.transform.SetParent(canvasObj.transform, false);

            overlayRect = targetOverlayObj.AddComponent<RectTransform>();

            Image overlayBg = targetOverlayObj.AddComponent<Image>();
            overlayBg.sprite = GlassUIFactory.GetRoundedRectSprite();
            overlayBg.type = Image.Type.Sliced;
            overlayBg.color = new Color(0f, 0.85f, 1f, 0.15f);

            overlayBorder = targetOverlayObj.AddComponent<Outline>();
            overlayBorder.effectColor = new Color(0f, 1f, 1f, 0.85f);
            overlayBorder.effectDistance = new Vector2(3f, -3f);

            Shadow overlayShadow = targetOverlayObj.AddComponent<Shadow>();
            overlayShadow.effectColor = new Color(0f, 1f, 1f, 0.45f);
            overlayShadow.effectDistance = new Vector2(-2f, 2f);

            // Text inside Pulsing Target Overlay
            GameObject textObj = new GameObject("OverlayText");
            textObj.transform.SetParent(targetOverlayObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            overlayText = textObj.AddComponent<Text>();
            overlayText.font = defaultFont;
            overlayText.fontSize = 28;
            overlayText.fontStyle = FontStyle.Bold;
            overlayText.alignment = TextAnchor.MiddleCenter;
            overlayText.color = new Color(1f, 0.85f, 0f);

            Shadow textShadow = textObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            textShadow.effectDistance = new Vector2(2f, -2f);

            // 3. Center Radial Loading Graphic (Filled Circle + Border in Center of Screen)
            progressContainerObj = new GameObject("CenterProgressGraphic");
            progressContainerObj.transform.SetParent(canvasObj.transform, false);

            RectTransform progressRect = progressContainerObj.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressRect.pivot = new Vector2(0.5f, 0.5f);
            progressRect.sizeDelta = new Vector2(220f, 220f);
            progressRect.anchoredPosition = new Vector2(0f, -40f);

            // Background Glass Panel for Progress Graphic
            Image progressBg = progressContainerObj.AddComponent<Image>();
            progressBg.sprite = GlassUIFactory.GetCircleSprite();
            progressBg.color = new Color(0.04f, 0.02f, 0.08f, 0.85f);

            // Radial Filled Circle Image
            GameObject fillObj = new GameObject("RadialFill");
            fillObj.transform.SetParent(progressContainerObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            radialFillImage = fillObj.AddComponent<Image>();
            radialFillImage.sprite = GlassUIFactory.GetCircleSprite();
            radialFillImage.type = Image.Type.Filled;
            radialFillImage.fillMethod = Image.FillMethod.Radial360;
            radialFillImage.fillOrigin = (int)Image.Origin360.Top;
            radialFillImage.fillClockwise = true;
            radialFillImage.fillAmount = 0f;
            radialFillImage.color = new Color(0f, 1f, 1f, 0.85f);

            // Outer Border Ring
            GameObject borderObj = new GameObject("BorderRing");
            borderObj.transform.SetParent(progressContainerObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;

            borderRingImage = borderObj.AddComponent<Image>();
            borderRingImage.sprite = GlassUIFactory.GetRingSprite();
            borderRingImage.color = new Color(1f, 0.85f, 0f, 0.95f);

            Outline ringGlow = borderObj.AddComponent<Outline>();
            ringGlow.effectColor = new Color(1f, 0.85f, 0f, 0.6f);
            ringGlow.effectDistance = new Vector2(1.5f, -1.5f);

            // Percentage Text in Center
            GameObject percentObj = new GameObject("PercentText");
            percentObj.transform.SetParent(progressContainerObj.transform, false);
            RectTransform percentRect = percentObj.AddComponent<RectTransform>();
            percentRect.anchorMin = Vector2.zero;
            percentRect.anchorMax = Vector2.one;
            percentRect.sizeDelta = Vector2.zero;

            percentText = percentObj.AddComponent<Text>();
            percentText.font = defaultFont;
            percentText.fontSize = 32;
            percentText.fontStyle = FontStyle.Bold;
            percentText.alignment = TextAnchor.MiddleCenter;
            percentText.color = Color.white;
            percentText.text = "0%";

            Shadow percentShadow = percentObj.AddComponent<Shadow>();
            percentShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            percentShadow.effectDistance = new Vector2(2f, -2f);

            progressContainerObj.SetActive(false);
        }

        private void SetStep(FTUEStep step)
        {
            CurrentStep = step;
            isTransitioningStep = false;

            if (radialFillImage != null)
            {
                radialFillImage.color = new Color(0f, 1f, 1f, 0.85f);
                radialFillImage.fillAmount = 0f;
            }
            if (borderRingImage != null)
            {
                borderRingImage.color = new Color(1f, 0.85f, 0f, 0.95f);
            }

            switch (step)
            {
                case FTUEStep.RotateClockwise:
                    overlayRect.anchorMin = new Vector2(0.52f, 0.33f);
                    overlayRect.anchorMax = new Vector2(0.98f, 0.95f);
                    overlayRect.sizeDelta = Vector2.zero;
                    if (overlayBorder != null) overlayBorder.effectColor = new Color(0f, 1f, 1f, 0.85f);
                    if (overlayText != null)
                    {
                        overlayText.text = "STEP 1: ROTATE CLOCKWISE\nTAP / HOLD HERE \u25B6\u25B6";
                        overlayText.color = new Color(1f, 0.85f, 0f);
                    }
                    if (progressContainerObj != null) progressContainerObj.SetActive(false);
                    break;

                case FTUEStep.RotateCounterClockwise:
                    overlayRect.anchorMin = new Vector2(0.02f, 0.33f);
                    overlayRect.anchorMax = new Vector2(0.48f, 0.95f);
                    overlayRect.sizeDelta = Vector2.zero;
                    if (overlayBorder != null) overlayBorder.effectColor = new Color(1f, 0f, 0.6f, 0.85f);
                    if (overlayText != null)
                    {
                        overlayText.text = "STEP 2: ROTATE COUNTER-CLOCKWISE\n\u25C0\u25C0 TAP / HOLD HERE";
                        overlayText.color = new Color(1f, 0.85f, 0f);
                    }
                    if (progressContainerObj != null) progressContainerObj.SetActive(false);
                    break;

                case FTUEStep.SingleJump:
                    overlayRect.anchorMin = new Vector2(0.05f, 0.05f);
                    overlayRect.anchorMax = new Vector2(0.95f, 0.28f);
                    overlayRect.sizeDelta = Vector2.zero;
                    if (overlayBorder != null) overlayBorder.effectColor = new Color(1f, 0.85f, 0f, 0.85f);
                    if (overlayText != null)
                    {
                        overlayText.text = "STEP 3: SINGLE JUMP\nTAP BOTTOM AREA OR PRESS SPACE";
                        overlayText.color = new Color(1f, 0.85f, 0f);
                    }
                    if (progressContainerObj != null) progressContainerObj.SetActive(false); // No loading graphic for jump
                    wasJumping = false;
                    break;

                case FTUEStep.DoubleJump:
                    overlayRect.anchorMin = new Vector2(0.05f, 0.05f);
                    overlayRect.anchorMax = new Vector2(0.95f, 0.28f);
                    overlayRect.sizeDelta = Vector2.zero;
                    if (overlayBorder != null) overlayBorder.effectColor = new Color(0f, 1f, 0.85f, 0.85f);
                    if (overlayText != null)
                    {
                        overlayText.text = "STEP 4: DOUBLE JUMP CROSSOVER\nDOUBLE TAP BOTTOM OR PRESS SPACE TWICE";
                        overlayText.color = new Color(1f, 0.85f, 0f);
                    }
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

            // Pulse target overlay visual border alpha
            if (targetOverlayObj != null && overlayBorder != null)
            {
                float pulse = 0.65f + Mathf.Sin(Time.time * 4.5f) * 0.25f;
                Color borderCol = overlayBorder.effectColor;
                borderCol.a = pulse;
                overlayBorder.effectColor = borderCol;
            }

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
                    if (percentText != null) percentText.text = $"{Mathf.RoundToInt(progClockwise * 100f)}%";

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
                    if (percentText != null) percentText.text = $"{Mathf.RoundToInt(progCounter * 100f)}%";

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
                        if (overlayText != null) overlayText.text = "JUMP IN FLIGHT...";
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
                            if (overlayText != null) overlayText.text = "CROSSOVER JUMP IN FLIGHT!";
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
            if (overlayText != null)
            {
                overlayText.text = successMessage;
                overlayText.color = new Color(0f, 1f, 0.5f);
            }

            if (radialFillImage != null)
            {
                radialFillImage.color = new Color(0f, 1f, 0.5f, 0.95f);
            }
            if (borderRingImage != null)
            {
                borderRingImage.color = new Color(0f, 1f, 0.5f, 0.95f);
            }

            yield return new WaitForSeconds(0.9f);

            SetStep(nextStep);
        }

        private IEnumerator CompleteTutorialAndExit()
        {
            CurrentStep = FTUEStep.Complete;

            if (overlayText != null)
            {
                overlayText.text = "TUTORIAL COMPLETE!\nYOU ARE READY TO FLY!";
                overlayText.color = new Color(0f, 1f, 0.5f);
            }

            yield return new WaitForSeconds(1.2f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
