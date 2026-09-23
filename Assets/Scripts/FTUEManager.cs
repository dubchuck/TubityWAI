using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TubityWAI
{
    public enum FTUEStep
    {
        RotateClockwise,        // Step 1: full circle clockwise from where you start
        RotateCounterClockwise, // Step 2: full circle counter-clockwise from where you start
        Jump,                   // Step 3: jump, steering locked
        DodgeArc,               // Step 4: steer around a solid arc
        DoubleJump,             // Step 5: jump, then jump again in the air to cross to the far wall
        JumpArc,                // Step 6: jump over a solid arc, steering locked
        CollectCoin,            // Step 7: roll through a coin trail
        Complete
    }

    /// <summary>
    /// The first-time user experience: a real run down the tube where each
    /// step is taught by doing. Rotation steps fill a dial from 0% as the
    /// sphere turns away from where it started the step; the obstacle and
    /// coin steps place a single arc or coin trail ahead of the player on cue,
    /// centred on wherever the sphere is right then. Crashing into a tutorial
    /// arc never ends the run - the arc shatters, the hint turns hazard pink,
    /// and a fresh arc is laid down for another go.
    ///
    /// Hint copy comes from TubityXInput so it names the controls the player
    /// actually has: touch zones, keyboard keys or the Siri Remote. The text
    /// floats straight over the tube - no card behind it.
    /// </summary>
    public class FTUEManager : MonoBehaviour
    {
        public static FTUEManager Instance { get; private set; }

        public FTUEStep CurrentStep { get; private set; } = FTUEStep.RotateClockwise;

        private const int StepCount = 7;

        private PlayerController player;
        private TunnelGenerator tunnel;
        private GameObject canvasObj;

        // Floating overlay copy: a title line and a hint line in the display
        // face, positioned per step. No panel behind them.
        private GameObject targetOverlayObj;
        private RectTransform overlayRect;
        private TubityXLabel overlayTitle;
        private TubityXLabel overlayHint;

        // "STEERING LOCKED" line, top centre, for the jump-only steps.
        private GameObject lockBadgeObj;

        // Progress dial (center of screen): round glass panel, radial fill, percent
        private GameObject progressContainerObj;
        private TubityXPanel progressChrome;
        private Image radialFillImage;
        private TubityXLabel percentText;

        private static readonly Color Gold = TubityXUIFactory.Gold;
        private static readonly Color Cyan = new Color(0f, 1f, 1f);
        private static readonly Color Pink = new Color(1f, 0.25f, 0.65f);
        private static readonly Color Mint = new Color(0f, 1f, 0.55f);
        private static readonly Color Hazard = new Color(1f, 0.15f, 0.35f);

        // Signed rotation since the current step began, in radians. A full
        // turn in the step's direction completes it; turning the wrong way
        // just winds the dial back toward 0%.
        private float lastAngle = 0f;
        private float netRotation = 0f;
        private const float FULL_CIRCLE_RADIANS = Mathf.PI * 2f;

        private bool wasJumping = false;
        private bool sawCrossover = false;
        private bool isTransitioningStep = false;

        // The arc or coin trail the current step placed in the tube.
        private GameObject scripted;
        private float scriptedZ;          // z of the arc, or of the last coin in the trail
        private bool retrying = false;
        private int coinsAtStepStart = 0;
        private int lastCoinsShown = 0;

        [Tooltip("How far ahead, in seconds of travel, a tutorial arc or coin trail appears.")]
        public float leadSeconds = 4.5f;
        [Tooltip("Angular span of every tutorial arc, in degrees. Wide enough to read, narrow enough to dodge.")]
        public float arcSpanDegrees = 110f;
        [Tooltip("How far every tutorial arc reaches in from the wall, as a fraction of the tube radius. " +
                 "A jump peaks at the tube centre, so anything under 1 clears with good timing; 0.45 " +
                 "matches the tallest campaign arcs while leaving a fair window.")]
        public float arcHeightFraction = 0.45f;

        [Tooltip("How far each tutorial coin sits to the side of the player's line, in radians. " +
                 "Big enough that the sphere must steer for it, small enough to reach in a ring's travel.")]
        public float coinOffsetRadians = 0.42f;

        // The coin step ends only once every one of these has been picked up.
        private const int CoinsToCollect = 3;

        private InputScheme Scheme { get { return TubityXInput.Current; } }

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
                if (player != null) player.SteeringLocked = false;
            }
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            tunnel = FindFirstObjectByType<TunnelGenerator>();
            if (player != null)
            {
                lastAngle = player.currentAngle;
            }

            CreateFTUEUI();
            SetStep(FTUEStep.RotateClockwise);
        }

        // ==================================================================
        // UI
        // ==================================================================

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

            // 2. Floating step copy - anchors are set per step in SetStep. The
            //    labels carry their own neon glow, so nothing sits behind them.
            targetOverlayObj = new GameObject("StepOverlay", typeof(RectTransform));
            targetOverlayObj.transform.SetParent(safe, false);
            overlayRect = targetOverlayObj.GetComponent<RectTransform>();
            overlayRect.pivot = new Vector2(0.5f, 0.5f);

            overlayTitle = AddOverlayLine(targetOverlayObj, "OverlayTitle", 26f, 26f, Gold, Gold);
            overlayHint = AddOverlayLine(targetOverlayObj, "OverlayHint", -26f, 16f, Color.white, Cyan);

            // 3. Steering-locked line, top centre, off until a jump-only step
            lockBadgeObj = new GameObject("SteeringLockedText", typeof(RectTransform));
            lockBadgeObj.transform.SetParent(safe, false);
            RectTransform lockRect = lockBadgeObj.GetComponent<RectTransform>();
            lockRect.anchorMin = new Vector2(0.2f, 1f);
            lockRect.anchorMax = new Vector2(0.8f, 1f);
            lockRect.pivot = new Vector2(0.5f, 1f);
            lockRect.sizeDelta = new Vector2(0f, 46f);
            lockRect.anchoredPosition = new Vector2(0f, -24f);
            TubityXUIFactory.AddLabel(lockBadgeObj, "STEERING LOCKED - JUMP ONLY", 14f, Gold, Gold, 3f);
            lockBadgeObj.SetActive(false);

            // 4. Center progress dial: round glass panel with a radial fill inside
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

        /// <summary>One line of overlay copy, vertically offset from the block centre.</summary>
        private static TubityXLabel AddOverlayLine(GameObject host, string name, float yOffset,
                                                   float cap, Color face, Color accent)
        {
            GameObject line = new GameObject(name, typeof(RectTransform));
            line.transform.SetParent(host.transform, false);
            RectTransform r = line.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.03f, 0.5f);
            r.anchorMax = new Vector2(0.97f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(0f, cap * 2.4f);
            r.anchoredPosition = new Vector2(0f, yOffset);
            return TubityXUIFactory.AddLabel(line, "", cap, face, accent, cap * 0.16f);
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
                if (tr != null) tr.anchoredPosition = new Vector2(0f, string.IsNullOrEmpty(hint) ? 0f : 26f);
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

        /// <summary>The step's accent: with no card behind the copy it lives in the hint's glow.</summary>
        private void SetOverlayRim(Color rim)
        {
            if (overlayHint != null) overlayHint.RimColor = rim;
        }

        private void SetProgressColors(Color fill, Color rim)
        {
            if (radialFillImage != null)
                radialFillImage.color = new Color(fill.r, fill.g, fill.b, 0.35f);
            if (progressChrome != null) progressChrome.RimColor = rim;
        }

        private void PlaceOverlay(Vector2 anchorMin, Vector2 anchorMax)
        {
            overlayRect.anchorMin = anchorMin;
            overlayRect.anchorMax = anchorMax;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Bottom band, clear of the tube ahead, for the jump, obstacle and coin steps.</summary>
        private void PlaceOverlayBottom()
        {
            PlaceOverlay(new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f));
        }

        private static string StepTitle(int step, string name)
        {
            return "STEP " + step + " / " + StepCount + ": " + name;
        }

        private void ShowDial(bool show)
        {
            if (progressContainerObj == null) return;
            progressContainerObj.SetActive(show);
            if (show)
            {
                if (radialFillImage != null) radialFillImage.fillAmount = 0f;
                if (percentText != null) percentText.Text = "0%";
            }
        }

        // ==================================================================
        // Steps
        // ==================================================================

        private void SetStep(FTUEStep step)
        {
            CurrentStep = step;
            isTransitioningStep = false;
            retrying = false;
            wasJumping = false;
            sawCrossover = false;
            lastCoinsShown = 0;

            // Every rotation step measures from where the sphere is right now.
            netRotation = 0f;
            if (player != null) lastAngle = player.currentAngle;

            SetProgressColors(Cyan, Gold);
            ShowDial(false);

            bool lockSteering = step == FTUEStep.Jump || step == FTUEStep.JumpArc;
            if (player != null) player.SteeringLocked = lockSteering;
            if (lockBadgeObj != null) lockBadgeObj.SetActive(lockSteering);

            switch (step)
            {
                case FTUEStep.RotateClockwise:
                    PlaceOverlay(new Vector2(0.52f, 0.33f), new Vector2(0.98f, 0.95f));
                    SetOverlayRim(Cyan);
                    SetOverlayText(StepTitle(1, "ROLL CLOCKWISE"), TubityXInput.SteerRightHint(Scheme), Gold);
                    ShowDial(true);
                    break;

                case FTUEStep.RotateCounterClockwise:
                    PlaceOverlay(new Vector2(0.02f, 0.33f), new Vector2(0.48f, 0.95f));
                    SetOverlayRim(Pink);
                    SetOverlayText(StepTitle(2, "ROLL COUNTER-CLOCKWISE"), TubityXInput.SteerLeftHint(Scheme), Gold);
                    ShowDial(true);
                    break;

                case FTUEStep.DoubleJump:
                    PlaceOverlayBottom();
                    SetOverlayRim(Gold);
                    SetOverlayText(StepTitle(5, "DOUBLE JUMP"), TubityXInput.DoubleJumpHint(Scheme), Gold);
                    break;

                case FTUEStep.DodgeArc:
                    PlaceOverlayBottom();
                    SetOverlayRim(Hazard);
                    SetOverlayText(StepTitle(4, "DODGE THE RED ARC"), TubityXInput.SteerAnyHint(Scheme), Gold);
                    SpawnStepObjects();
                    break;

                case FTUEStep.Jump:
                    PlaceOverlayBottom();
                    SetOverlayRim(Gold);
                    SetOverlayText(StepTitle(3, "JUMP"), TubityXInput.JumpHint(Scheme), Gold);
                    break;

                case FTUEStep.JumpArc:
                    PlaceOverlayBottom();
                    SetOverlayRim(Hazard);
                    SetOverlayText(StepTitle(6, "JUMP OVER THE RED ARC"), "JUST BEFORE IT: " + TubityXInput.JumpHint(Scheme), Gold);
                    SpawnStepObjects();
                    break;

                case FTUEStep.CollectCoin:
                    PlaceOverlayBottom();
                    SetOverlayRim(Gold);
                    SetOverlayText(StepTitle(7, "GRAB ALL " + CoinsToCollect + " COINS (0 / " + CoinsToCollect + ")"),
                                   "ONE PER RING - STEER ONTO EACH OF THEM", Gold);
                    coinsAtStepStart = player != null ? player.Coins : 0;
                    SpawnStepObjects();
                    break;

                case FTUEStep.Complete:
                    if (targetOverlayObj != null) targetOverlayObj.SetActive(false);
                    ShowDial(false);
                    break;
            }
        }

        /// <summary>Lays down whatever the current step needs, ahead of the player, on their current line.</summary>
        private void SpawnStepObjects()
        {
            if (scripted != null) Destroy(scripted);
            scripted = null;
            if (player == null) return;

            float speed = Mathf.Max(4f, player.forwardSpeed);
            float lead = Mathf.Max(30f, speed * leadSeconds);
            float z = player.zPos + lead;
            float angle = player.currentAngle;

            switch (CurrentStep)
            {
                case FTUEStep.DodgeArc:
                case FTUEStep.JumpArc:
                    // One standard tutorial arc, the same size whether it is dodged or jumped.
                    scripted = SpawnArc(z, angle, arcSpanDegrees, TubeRadius() * Mathf.Clamp(arcHeightFraction, 0.1f, 0.8f));
                    scriptedZ = z;
                    break;

                case FTUEStep.CollectCoin:
                    scripted = SpawnCoinSet(z, angle, out scriptedZ);
                    break;
            }
        }

        private float TubeRadius()
        {
            if (tunnel != null) return tunnel.radius;
            if (player != null) return player.radius;
            return 5f;
        }

        /// <summary>
        /// One solid hazard arc centred on centerAngle. Same construction as
        /// TunnelSegment.SpawnRingArcGroup: the arc mesh spans 0..arcAngle from
        /// its own local rotation, so rotate the group by (centre - half span).
        /// </summary>
        private GameObject SpawnArc(float z, float centerAngle, float arcDeg, float thickness)
        {
            GameObject group = new GameObject("TutorialArc");
            group.transform.position = new Vector3(0f, 0f, z);
            group.transform.rotation = Quaternion.Euler(0f, 0f, centerAngle * Mathf.Rad2Deg - arcDeg * 0.5f);

            GameObject obsObj = new GameObject("Obstacle");
            obsObj.transform.SetParent(group.transform, false);

            Obstacle obs = obsObj.AddComponent<Obstacle>();
            obs.radius = TubeRadius();
            obs.thickness = Mathf.Max(0.3f, thickness);
            obs.depth = 0.4f;
            obs.arcAngle = arcDeg;
            obs.isColorCoded = false;
            obs.targetColorIndex = -1;
            obs.obstacleMaterial = tunnel != null ? tunnel.obstacleMaterial : null;
            return group;
        }

        /// <summary>
        /// CoinsToCollect coins in the player's own colour, one on every other
        /// marker ring, each nudged a little to one side of the player's line
        /// (alternating) so the sphere has to steer for every one. Built the
        /// way TunnelSegment.SpawnCoins builds them.
        /// </summary>
        private GameObject SpawnCoinSet(float startZ, float playerAngle, out float lastZ)
        {
            GameObject trail = new GameObject("TutorialCoins");
            trail.transform.position = Vector3.zero;
            lastZ = startZ;

            int colorIndex = 0;
            PlayerSphere firstSphere = player != null ? player.GetComponentInChildren<PlayerSphere>() : null;
            if (firstSphere != null) colorIndex = firstSphere.colorIndex;

            Material[] mats = tunnel != null ? tunnel.coinMaterials : null;
            float spawnRadius = TubeRadius() - 0.35f;

            // Sit each coin on a marker ring, every other ring, so they read as
            // "one per arc" and there is room to steer between them.
            float ring = tunnel != null ? Mathf.Max(1f, tunnel.markerInterval) : 5f;
            float firstZ = Mathf.Ceil(startZ / ring) * ring;
            float spacing = ring * 2f;

            for (int i = 0; i < CoinsToCollect; i++)
            {
                float z = firstZ + spacing * i;
                lastZ = z;

                // Alternate sides: right, left, right ...
                float side = (i % 2 == 0) ? 1f : -1f;
                float angle = playerAngle + side * coinOffsetRadians;

                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin_" + i;
                coin.transform.SetParent(trail.transform, false);

                float x = Mathf.Sin(angle) * spawnRadius;
                float y = -Mathf.Cos(angle) * spawnRadius;
                coin.transform.localPosition = new Vector3(x, y, z);
                coin.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);

                Vector3 radialDir = new Vector3(x, y, 0f).normalized;
                coin.transform.localRotation = Quaternion.LookRotation(Vector3.forward, radialDir);

                Collider oldCol = coin.GetComponent<Collider>();
                if (oldCol != null) Destroy(oldCol);

                SphereCollider sphereCol = coin.AddComponent<SphereCollider>();
                sphereCol.isTrigger = true;
                sphereCol.radius = 1.3f;

                MeshRenderer mr = coin.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    if (mats != null && mats.Length > 0) mr.sharedMaterial = mats[Mathf.Clamp(colorIndex, 0, mats.Length - 1)];
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }

                Collectible collectible = coin.AddComponent<Collectible>();
                collectible.type = CollectibleType.Coin;
                collectible.colorIndex = colorIndex;
                collectible.rotationSpeed = 160f;
                collectible.hoverAmplitude = 0.08f;
                collectible.hoverSpeed = 3.5f;
            }

            return trail;
        }

        // ==================================================================
        // Crash handling: tutorial arcs never end the run
        // ==================================================================

        /// <summary>
        /// Called by Obstacle before it would crash the player. Returns true to
        /// swallow the crash (the arc still shatters for feedback) and set the
        /// step up again.
        /// </summary>
        public bool InterceptCrash(Obstacle obstacle)
        {
            if (CurrentStep != FTUEStep.DodgeArc && CurrentStep != FTUEStep.JumpArc) return false;
            if (retrying || isTransitioningStep) return true;
            StartCoroutine(RetryAfterCrash());
            return true;
        }

        private IEnumerator RetryAfterCrash()
        {
            retrying = true;

            if (Camera.main != null)
            {
                CameraController cam = Camera.main.GetComponent<CameraController>();
                if (cam != null) cam.TriggerJitter(0.3f, 0.4f);
            }

            bool dodge = CurrentStep == FTUEStep.DodgeArc;
            SetOverlayRim(Hazard);
            SetOverlayText(dodge ? "OUCH! STEER AROUND IT" : "OUCH! JUMP JUST BEFORE IT",
                           dodge ? TubityXInput.SteerAnyHint(Scheme) : TubityXInput.JumpHint(Scheme), Hazard);

            // The shattered arc takes 1.25 s to clear itself; lay a fresh one after that.
            yield return new WaitForSeconds(1.4f);

            SetStep(CurrentStep);
        }

        // ==================================================================
        // Per-frame step logic
        // ==================================================================

        private void Update()
        {
            if (CurrentStep == FTUEStep.Complete || isTransitioningStep) return;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                lastAngle = player.currentAngle;
            }

            // Signed turn this frame, then the running total since the step began
            float currentAng = player.currentAngle;
            float delta = currentAng - lastAngle;
            if (delta < -Mathf.PI) delta += Mathf.PI * 2f;
            if (delta > Mathf.PI) delta -= Mathf.PI * 2f;
            lastAngle = currentAng;
            netRotation += delta;

            switch (CurrentStep)
            {
                case FTUEStep.RotateClockwise:
                    UpdateDial(netRotation / FULL_CIRCLE_RADIANS, FTUEStep.RotateCounterClockwise, "STEP 1 COMPLETE!");
                    break;

                case FTUEStep.RotateCounterClockwise:
                    UpdateDial(-netRotation / FULL_CIRCLE_RADIANS, FTUEStep.Jump, "STEP 2 COMPLETE!");
                    break;

                case FTUEStep.DoubleJump:
                    if (player.IsJumping)
                    {
                        wasJumping = true;
                        if (player.HasCrossedOver && !sawCrossover)
                        {
                            sawCrossover = true;
                            SetOverlayText("CROSSING OVER!", Gold);
                        }
                        else if (!sawCrossover)
                        {
                            SetOverlayText("NOW! PRESS AGAIN IN THE AIR", TubityXInput.DoubleJumpHint(Scheme), Gold);
                        }
                    }
                    else if (wasJumping)
                    {
                        if (sawCrossover)
                        {
                            isTransitioningStep = true;
                            StartCoroutine(TransitionToNextStep(FTUEStep.JumpArc, "CROSSOVER LANDED!"));
                        }
                        else
                        {
                            // A single hop: reset and ask for the second press next time.
                            wasJumping = false;
                            SetOverlayText("JUMP, THEN JUMP AGAIN MID-AIR", TubityXInput.DoubleJumpHint(Scheme), Gold);
                        }
                    }
                    break;

                case FTUEStep.DodgeArc:
                    if (retrying || scripted == null) break;
                    if (player.zPos > scriptedZ + 2f)
                    {
                        isTransitioningStep = true;
                        Destroy(scripted, 3f);
                        scripted = null;
                        StartCoroutine(TransitionToNextStep(FTUEStep.DoubleJump, "CLEAN DODGE!"));
                    }
                    break;

                case FTUEStep.Jump:
                    if (player.IsJumping)
                    {
                        wasJumping = true;
                        SetOverlayText("JUMP IN FLIGHT...", Gold);
                    }
                    else if (wasJumping)
                    {
                        isTransitioningStep = true;
                        StartCoroutine(TransitionToNextStep(FTUEStep.DodgeArc, "JUMP LANDED! PERFECT!"));
                    }
                    break;

                case FTUEStep.JumpArc:
                    if (retrying || scripted == null) break;
                    if (player.zPos > scriptedZ + 2f)
                    {
                        isTransitioningStep = true;
                        Destroy(scripted, 3f);
                        scripted = null;
                        StartCoroutine(TransitionToNextStep(FTUEStep.CollectCoin, "CLEARED IT!"));
                    }
                    break;

                case FTUEStep.CollectCoin:
                    if (retrying) break;
                    int collected = player.Coins - coinsAtStepStart;
                    if (collected >= CoinsToCollect)
                    {
                        isTransitioningStep = true;
                        if (scripted != null) Destroy(scripted, 3f);
                        scripted = null;
                        StartCoroutine(CompleteTutorialAndExit());
                    }
                    else if (scripted != null && player.zPos > scriptedZ + 3f)
                    {
                        // The last ring went by with coins still out there: all of them, again.
                        StartCoroutine(RetryCoins(collected));
                    }
                    else if (collected != lastCoinsShown)
                    {
                        lastCoinsShown = collected;
                        SetOverlayText(StepTitle(7, "GRAB ALL " + CoinsToCollect + " COINS (" + collected + " / " + CoinsToCollect + ")"),
                                       collected > 0 ? "KEEP GOING - GET THE REST" : "ONE PER RING - STEER ONTO EACH OF THEM", Gold);
                    }
                    break;
            }
        }

        /// <summary>Dial for the rotation steps: 0% at the step's start angle, 100% one full turn later.</summary>
        private void UpdateDial(float progress, FTUEStep nextStep, string successMessage)
        {
            progress = Mathf.Clamp01(progress);
            if (radialFillImage != null) radialFillImage.fillAmount = progress;
            if (percentText != null) percentText.Text = Mathf.RoundToInt(progress * 100f) + "%";

            if (progress >= 1f)
            {
                isTransitioningStep = true;
                StartCoroutine(TransitionToNextStep(nextStep, successMessage));
            }
        }

        private IEnumerator RetryCoins(int collected)
        {
            retrying = true;
            SetOverlayRim(Hazard);
            int missed = CoinsToCollect - collected;
            SetOverlayText("MISSED " + missed + "! YOU NEED ALL " + CoinsToCollect, TubityXInput.SteerAnyHint(Scheme), Hazard);
            yield return new WaitForSeconds(1.2f);
            SetStep(FTUEStep.CollectCoin);   // resets the count and lays down a fresh set
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
            if (player != null) player.SteeringLocked = false;
            if (lockBadgeObj != null) lockBadgeObj.SetActive(false);

            // The coin's payoff, then the send-off.
            SetOverlayRim(Gold);
            SetOverlayText("COIN COLLECTED!", "COINS BUY NEW SPHERE SKINS IN THE SHOP", Gold);
            yield return new WaitForSeconds(2.2f);

            CurrentStep = FTUEStep.Complete;
            SetOverlayRim(Mint);
            SetOverlayText("TUTORIAL COMPLETE!", "YOU ARE READY TO FLY", Mint);

            // Whatever brought us here, PLAY goes straight into the game from now on.
            PlayerPrefs.SetInt(MainMenu.HowToPlaySeenKey, 1);
            PlayerPrefs.Save();

            yield return new WaitForSeconds(1.5f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
