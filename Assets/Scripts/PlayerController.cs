using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace TubityWAI
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Radius of the cylinder track.")]
        public float radius = 5f;

        [Tooltip("Speed of radial movement around the tube (in radians per second).")]
        public float angularSpeed = 4f;

        [Tooltip("Forward movement (falling) speed down the tube (units per second).")]
        public float forwardSpeed = 15f;

        [Tooltip("Multiplier applied to forward speed when the Down Arrow or S key is held.")]
        public float speedBoostMultiplier = 2f;

        [Header("Rotation Settings")]
        [Tooltip("Smoothness of player rotation alignment.")]
        public float rotationSmoothing = 10f;

        [Header("Jump Settings")]
        [HideInInspector]
        public float jumpDuration = 0.6f;
        [HideInInspector]
        public float landingRecoveryDuration = 0.2f;
        [HideInInspector]
        public float takeoffSquash = 0.15f;
        [HideInInspector]
        public float flightStretch = 0.12f;
        [HideInInspector]
        public float landingSquash = 0.2f;

        [Header("Marker Animation Settings")]
        [HideInInspector]
        public float markerInterval = 5f;
        [HideInInspector]
        public float animationDuration = 0.333f;
        [HideInInspector]
        public float pulseScaleMultiplier = 1.15f;
        [HideInInspector]
        public float pulseBrightnessMultiplier = 1.6f;

        [Header("Volumetric Light Settings")]
        [HideInInspector]
        public Transform volumetricLightTransform;
        [HideInInspector]
        public Material volumetricLightMaterial;
        [HideInInspector]
        public Color volumetricLightBaseColor;
        [HideInInspector]
        public float volumetricLightDistance = 175f;
        [HideInInspector]
        public Vector3 baseVolumetricLightScale;
        [HideInInspector]
        public float volumetricLightPulseScale = 1.25f;
        [HideInInspector]
        public float volumetricLightPulseBrightness = 1.8f;

        // Scoring and Game HUD State
        public int Score { get; private set; }
        public int Coins { get; private set; }
        public float TimeElapsed { get; private set; }

        public static PlayerController Instance { get; private set; }

        [Header("Powerup Settings")]
        [HideInInspector]
        public float invincibilitySpeedMultiplier = 3.5f;
        public bool IsInvincible { get; private set; } = false;
        
        // Expose a normalized value (0.0 to 1.0) for the camera and UI to use for effects
        public float InvincibilityEffectStrength { get; private set; } = 0f;

        public bool IsMagnetActive { get; private set; } = false;
        private float magnetTimer = 0f;
        private const float MAGNET_DURATION = 10f;
        
        public float MagnetTimeRemaining => magnetTimer;
        public float MagnetTotalTime => MAGNET_DURATION;

        private float invincibilityTimer = 0f;
        private float reacclimationTimer = 0f;
        private bool isReacclimating = false;
        private const float INVINCIBILITY_DURATION = 5f;
        private const float REACCLIMATION_DURATION = 0.5f;

        public float InvincibilityTimeRemaining => invincibilityTimer + reacclimationTimer;
        public float InvincibilityTotalTime => INVINCIBILITY_DURATION + REACCLIMATION_DURATION;


        // The current angle (theta) around the cylinder axis in radians.
        [HideInInspector]
        public float currentAngle = 0f;

        // Jump state properties for FTUE tutorial tracking
        public bool IsJumping => isJumping;
        public bool HasCrossedOver => hasCrossedOver;

        // The current Z position along the tube.
        [HideInInspector]
        public float zPos = 0f;

        private float lastZ = 0f;
        private float animationTimer = -1f;

        // Jump state variables
        private bool isJumping = false;
        private float jumpTimer = 0f;
        private float jumpStartAngle = 0f;
        private float jumpTargetAngle = 0f;
        private bool hasCrossedOver = false;

        // Landing bounce variables
        private bool isRecovering = false;
        private float recoveryTimer = 0f;

        private struct SphereInfo
        {
            public Transform transform;
            public Material material;
            public Color baseEmissionColor;
            public Vector3 baseScale;
        }
        private List<SphereInfo> childSpheres = new List<SphereInfo>();
        private ParticleSystem speedLinesPS;
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
            zPos = transform.position.z;
            lastZ = zPos;
            Score = 0;
            Coins = 0;
            TimeElapsed = 0f;
            
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.volume = 0.8f;

            // Discover and register all child spheres dynamically
            childSpheres.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                PlayerSphere sphereComp = child.GetComponent<PlayerSphere>();
                if (sphereComp != null)
                {
                    Renderer r = child.GetComponent<Renderer>();
                    if (r != null)
                    {
                        SphereInfo info = new SphereInfo();
                        info.transform = child;
                        info.material = r.material; // Instance copy to modify at runtime
                        info.baseScale = child.localScale;
                        if (info.material.HasProperty("_EmissionColor"))
                        {
                            info.baseEmissionColor = info.material.GetColor("_EmissionColor");
                        }
                        else if (info.material.HasProperty("_Color"))
                        {
                            info.baseEmissionColor = info.material.GetColor("_Color");
                        }
                        childSpheres.Add(info);
                    }
                }
            }
            
            // Setup Speed Lines Particle System
            GameObject speedLinesObj = new GameObject("SpeedLines");
            speedLinesObj.transform.SetParent(this.transform, false);
            speedLinesObj.transform.localPosition = new Vector3(0, 0, 80f);
            speedLinesObj.transform.localRotation = Quaternion.Euler(0, 180, 0); // Emit towards the camera
            
            speedLinesPS = speedLinesObj.AddComponent<ParticleSystem>();
            var main = speedLinesPS.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 1.5f;
            main.startSpeed = 120f; // Very fast particles
            main.startSize = 0.4f;
            main.startColor = new Color(1f, 1f, 1f, 0.7f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = speedLinesPS.emission;
            emission.rateOverTime = 0f; // Controlled dynamically

            var shape = speedLinesPS.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = radius * 1.8f;
            
            var renderer = speedLinesPS.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale = 0.05f;
            renderer.lengthScale = 6.0f;
            
            // Basic unlit line material
            Shader s = Shader.Find("Sprites/Default");
            if (s != null)
            {
                Material lineMat = new Material(s);
                lineMat.color = new Color(0.2f, 1f, 1f, 0.6f);
                renderer.material = lineMat;
            }
        }

        public void AddCoin()
        {
            Coins++;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(ProceduralAudio.GetCoinSound());
            }
        }

        public void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void AddScore(int points)
        {
            Score += points;
        }

        public void ActivateInvincibility()
        {
            IsInvincible = true;
            invincibilityTimer = INVINCIBILITY_DURATION;
            reacclimationTimer = REACCLIMATION_DURATION;
            isReacclimating = false;
            PlaySound(ProceduralAudio.GetSpeedUpSound());
        }

        public void ActivateMagnet()
        {
            if (!IsMagnetActive)
            {
                PlaySound(ProceduralAudio.GetMagnetOnSound());
            }
            IsMagnetActive = true;
            magnetTimer = MAGNET_DURATION;
        }

        public Transform GetMagnetTarget(int targetColorIndex)
        {
            if (childSpheres == null || childSpheres.Count == 0) return transform;
            
            if (targetColorIndex == -1) return childSpheres[0].transform;

            foreach (var info in childSpheres)
            {
                PlayerSphere ps = info.transform.GetComponent<PlayerSphere>();
                if (ps != null && ps.colorIndex == targetColorIndex)
                {
                    return info.transform;
                }
            }
            
            return childSpheres[0].transform;
        }

        private void Update()
        {
            // 1. Handle steering & speed boost inputs (Keyboard + Gamepad/tvOS D-Pad)
            float steerInput = 0f;
            bool isDownPressed = false;
            bool spacePressed = false;
            bool menuPressed = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
                {
                    steerInput = -1f;
                }
                else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
                {
                    steerInput = 1f;
                }

                isDownPressed = Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed;
                spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
                menuPressed = Keyboard.current.escapeKey.wasPressedThisFrame;

                if (Keyboard.current.pKey.wasPressedThisFrame)
                {
                    ActivateInvincibility();
                }

                if (Keyboard.current.mKey.wasPressedThisFrame)
                {
                    ActivateMagnet();
                }
            }

            if (Gamepad.current != null)
            {
                // Left / Right steer
                if (Gamepad.current.dpad.left.isPressed || Gamepad.current.leftStick.x.ReadValue() < -0.3f)
                {
                    steerInput = -1f;
                }
                else if (Gamepad.current.dpad.right.isPressed || Gamepad.current.leftStick.x.ReadValue() > 0.3f)
                {
                    steerInput = 1f;
                }

                // Down speed boost
                if (Gamepad.current.dpad.down.isPressed || Gamepad.current.leftStick.y.ReadValue() < -0.5f)
                {
                    isDownPressed = true;
                }

                // Up / ButtonSouth Jump
                if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.leftStick.y.ReadValue() > 0.5f)
                {
                    spacePressed = true;
                }

                // Menu / Pause toggle
                if (Gamepad.current.selectButton.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    menuPressed = true;
                }
            }

            // 2. Process mobile touch & editor mouse inputs
            ProcessTouchAndMouseInputs(ref steerInput, ref spacePressed);

            // 3. Handle Menu / Pause Toggle
            if (menuPressed && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            {
                GameHUD hud = FindFirstObjectByType<GameHUD>();
                if (hud != null)
                {
                    hud.TogglePauseMenu();
                }
            }


            // 3. Apply steering (independent in-air and on-ground steering)
            float steerAmount = steerInput * angularSpeed * Time.deltaTime;
            if (isJumping)
            {
                jumpStartAngle += steerAmount;
                jumpTargetAngle += steerAmount;
            }
            else
            {
                currentAngle += steerAmount;
            }

            // Keep the base angle wrapped in [0, 2*PI]
            if (currentAngle < 0f) currentAngle += Mathf.PI * 2f;
            if (currentAngle > Mathf.PI * 2f) currentAngle -= Mathf.PI * 2f;

            // Update forward movement and time elapsed
            TimeElapsed += Time.deltaTime;
            
            // Powerup state machine
            float currentInvincibilityBoost = 1f;
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
                currentInvincibilityBoost = invincibilitySpeedMultiplier;
                // Soft lerp in the strength so the camera smoothly tracks it over ~0.25s
                InvincibilityEffectStrength = Mathf.Min(1f, InvincibilityEffectStrength + Time.deltaTime * 4f);
            }
            else if (reacclimationTimer > 0f)
            {
                if (!isReacclimating)
                {
                    isReacclimating = true;
                    PlaySound(ProceduralAudio.GetSpeedDownSound());
                }
                reacclimationTimer -= Time.deltaTime;
                float t = reacclimationTimer / REACCLIMATION_DURATION; // Goes from 1 to 0
                currentInvincibilityBoost = Mathf.Lerp(1f, invincibilitySpeedMultiplier, t);
                InvincibilityEffectStrength = t;
            }
            else
            {
                IsInvincible = false;
                InvincibilityEffectStrength = 0f;
            }

            if (magnetTimer > 0f)
            {
                magnetTimer -= Time.deltaTime;
            }
            else
            {
                if (IsMagnetActive)
                {
                    PlaySound(ProceduralAudio.GetMagnetOffSound());
                    IsMagnetActive = false;
                }
            }

            if (speedLinesPS != null)
            {
                var em = speedLinesPS.emission;
                // Fade speed lines emission based on strength
                em.rateOverTime = 200f * InvincibilityEffectStrength;
            }

            float activeSpeed = forwardSpeed * (isDownPressed ? speedBoostMultiplier : 1f) * currentInvincibilityBoost;
            zPos += activeSpeed * Time.deltaTime;

            // Get the curve offset at the current zPos
            Vector3 curveOffset = Vector3.zero;
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            if (config != null)
            {
                curveOffset = config.GetCurveOffset(zPos);
            }
            transform.position = new Vector3(curveOffset.x, curveOffset.y, zPos);

            // Update Volumetric Light Position ahead of the player relative to curve
            if (volumetricLightTransform != null)
            {
                Vector3 lightCurveOffset = Vector3.zero;
                if (config != null)
                {
                    lightCurveOffset = config.GetCurveOffset(zPos + volumetricLightDistance);
                }
                volumetricLightTransform.position = new Vector3(lightCurveOffset.x, lightCurveOffset.y, zPos + volumetricLightDistance);
            }

            // 4. Update Jump State Machine
            if (spacePressed)
            {
                if (!isJumping)
                {
                    // Start a new Jump from current position
                    isJumping = true;
                    isRecovering = false;
                    jumpTimer = 0f;
                    jumpStartAngle = currentAngle;
                    jumpTargetAngle = currentAngle;
                    hasCrossedOver = false;
                    PlaySound(ProceduralAudio.GetJumpSound());
                }
                else if (!hasCrossedOver)
                {
                    // Double press space to initiate crossover to the opposite side
                    jumpTargetAngle = jumpStartAngle + Mathf.PI;
                    hasCrossedOver = true;
                    PlaySound(ProceduralAudio.GetDoubleJumpSound());
                }
            }

            // Calculate current height (inward offset) and radial squash/stretch scale multiplier
            float heightFactor = 0f;
            float jumpRadialScale = 1f;

            if (isJumping)
            {
                jumpTimer += Time.deltaTime;
                float t = jumpTimer / jumpDuration;

                if (t >= 1f)
                {
                    // Landing triggers recovery bounce
                    isJumping = false;
                    currentAngle = jumpTargetAngle;
                    isRecovering = true;
                    recoveryTimer = 0f;

                    heightFactor = 0f;
                    jumpRadialScale = 1f - landingSquash;
                }
                else
                {
                    // Parabolic arc for height: peaks at center (0,0) when t = 0.5
                    heightFactor = 4f * t * (1f - t);

                    // Procedural squash & stretch based on jump stage
                    if (t < 0.2f)
                    {
                        // Takeoff squash loading
                        jumpRadialScale = Mathf.Lerp(1f - takeoffSquash, 1f + flightStretch, t / 0.2f);
                    }
                    else if (t < 0.5f)
                    {
                        // Flight stretch returning to normal at apex
                        jumpRadialScale = Mathf.Lerp(1f + flightStretch, 1f, (t - 0.2f) / 0.3f);
                    }
                    else if (t < 0.8f)
                    {
                        // Re-stretching as we fall
                        jumpRadialScale = Mathf.Lerp(1f, 1f + flightStretch, (t - 0.5f) / 0.3f);
                    }
                    else
                    {
                        // Squashing just before impact
                        jumpRadialScale = Mathf.Lerp(1f + flightStretch, 1f - landingSquash, (t - 0.8f) / 0.2f);
                    }
                }
            }
            else if (isRecovering)
            {
                recoveryTimer += Time.deltaTime;
                float t = recoveryTimer / landingRecoveryDuration;

                if (t >= 1f)
                {
                    isRecovering = false;
                    jumpRadialScale = 1f;
                }
                else
                {
                    // Elastic decaying bounce back to normal
                    float bounce = Mathf.Cos(t * Mathf.PI * 2.5f) * (1f - t) * landingSquash;
                    jumpRadialScale = 1f - bounce;
                }
            }

            // 5. Detect marker crossings (triggers secondary pulse animation & increments score)
            if (Mathf.Floor(lastZ / markerInterval) != Mathf.Floor(zPos / markerInterval))
            {
                animationTimer = 0f; // Trigger pulse
                Score++; // Increment player score
            }
            lastZ = zPos;

            float pulseScaleMult = 1f;
            if (animationTimer >= 0f)
            {
                animationTimer += Time.deltaTime;
                float t = animationTimer / animationDuration;
                if (t >= 1f)
                {
                    animationTimer = -1f;

                    // Reset Volumetric Light
                    if (volumetricLightTransform != null)
                    {
                        volumetricLightTransform.localScale = baseVolumetricLightScale;
                    }
                    if (volumetricLightMaterial != null && volumetricLightMaterial.HasProperty("_BaseColor"))
                    {
                        volumetricLightMaterial.SetColor("_BaseColor", volumetricLightBaseColor);
                    }
                }
                else
                {
                    float progress = Mathf.Sin(t * Mathf.PI);
                    pulseScaleMult = Mathf.Lerp(1f, pulseScaleMultiplier, progress);

                    // Pulse emission colors on spheres
                    for (int i = 0; i < childSpheres.Count; i++)
                    {
                        SphereInfo sphere = childSpheres[i];
                        if (sphere.material != null && sphere.material.HasProperty("_EmissionColor"))
                        {
                            Color currentEmission = Color.Lerp(sphere.baseEmissionColor, sphere.baseEmissionColor * pulseBrightnessMultiplier, progress);
                            sphere.material.SetColor("_EmissionColor", currentEmission);
                        }
                    }

                    // Pulse Volumetric Light scale & brightness
                    if (volumetricLightTransform != null)
                    {
                        float portalScaleMult = Mathf.Lerp(1f, volumetricLightPulseScale, progress);
                        volumetricLightTransform.localScale = baseVolumetricLightScale * portalScaleMult;
                    }
                    if (volumetricLightMaterial != null && volumetricLightMaterial.HasProperty("_BaseColor"))
                    {
                        Color currentGlow = Color.Lerp(volumetricLightBaseColor, volumetricLightBaseColor * volumetricLightPulseBrightness, progress);
                        volumetricLightMaterial.SetColor("_BaseColor", currentGlow);
                    }
                }
            }

            // Reset materials if not animating
            if (animationTimer < 0f)
            {
                for (int i = 0; i < childSpheres.Count; i++)
                {
                    SphereInfo sphere = childSpheres[i];
                    if (sphere.material != null && sphere.material.HasProperty("_EmissionColor"))
                    {
                        sphere.material.SetColor("_EmissionColor", sphere.baseEmissionColor);
                    }
                }

                if (volumetricLightMaterial != null && volumetricLightMaterial.HasProperty("_BaseColor"))
                {
                    volumetricLightMaterial.SetColor("_BaseColor", volumetricLightBaseColor);
                }
            }

            // 6. Apply positioning, rotation, and squash/stretch scale
            int count = childSpheres.Count;
            float baseAngle = isJumping ? Mathf.LerpAngle(jumpStartAngle * Mathf.Rad2Deg, jumpTargetAngle * Mathf.Rad2Deg, jumpTimer / jumpDuration) * Mathf.Deg2Rad : currentAngle;

            for (int i = 0; i < count; i++)
            {
                SphereInfo sphere = childSpheres[i];
                float angleOffset = (i * 2f * Mathf.PI) / count;
                float totalAngle = baseAngle + angleOffset;

                // Radius calculation accounting for inward jump height
                float sphereRadius = sphere.baseScale.y * 0.5f;
                float effectiveRadius = radius - sphereRadius;
                float currentRadius = effectiveRadius * (1f - heightFactor);

                float x = Mathf.Sin(totalAngle) * currentRadius;
                float y = -Mathf.Cos(totalAngle) * currentRadius;
                sphere.transform.localPosition = new Vector3(x, y, 0f);

                // Compute combined scale factor (Jump Squash & Stretch * Marker Pulse)
                float radialScale = jumpRadialScale;
                float tangentScale = (1f / Mathf.Sqrt(radialScale)) * pulseScaleMult;
                radialScale *= pulseScaleMult; // Pulse scales all dimensions

                sphere.transform.localScale = new Vector3(
                    sphere.baseScale.x * tangentScale,
                    sphere.baseScale.y * radialScale,
                    sphere.baseScale.z * tangentScale
                );

                // Stand perpendicular to the track wall (even at center)
                Vector3 wallDir = new Vector3(Mathf.Sin(totalAngle), -Mathf.Cos(totalAngle), 0f);
                Vector3 localUp = -wallDir;
                Quaternion targetLocalRot = Quaternion.LookRotation(Vector3.forward, localUp);
                sphere.transform.localRotation = Quaternion.Slerp(sphere.transform.localRotation, targetLocalRot, rotationSmoothing * Time.deltaTime);
            }
        }

        private void ProcessTouchAndMouseInputs(ref float steerInput, ref bool spacePressed)
        {
            // Process Mobile Touches using new Input System
            var touchscreen = Touchscreen.current;
            bool touchProcessed = false;

            if (touchscreen != null)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    var touch = touchscreen.touches[i];
                    if (touch.press.isPressed)
                    {
                        touchProcessed = true;
                        Vector2 pos = touch.position.ReadValue();

                        // Bottom third of screen: Jump (trigger on Began)
                        if (pos.y < Screen.height / 3f)
                        {
                            if (touch.press.wasPressedThisFrame)
                            {
                                spacePressed = true;
                            }
                        }
                        else
                        {
                            // Left/Right division for top two-thirds
                            if (pos.x < Screen.width / 2f)
                            {
                                steerInput = -1f;
                            }
                            else
                            {
                                steerInput = 1f;
                            }
                        }
                    }
                }
            }

            // Process Editor Mouse Clicks using new Input System (only if touch didn't handle it)
            if (!touchProcessed)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    bool isPressed = mouse.leftButton.isPressed;
                    bool wasPressedThisFrame = mouse.leftButton.wasPressedThisFrame;

                    if (isPressed || wasPressedThisFrame)
                    {
                        Vector2 mousePos = mouse.position.ReadValue();

                        // Bottom third of screen: Jump (trigger on Down)
                        if (mousePos.y < Screen.height / 3f)
                        {
                            if (wasPressedThisFrame)
                            {
                                spacePressed = true;
                            }
                        }
                        else if (isPressed)
                        {
                            // Left/Right division for top two-thirds
                            if (mousePos.x < Screen.width / 2f)
                            {
                                steerInput = -1f;
                            }
                            else
                            {
                                steerInput = 1f;
                            }
                        }
                    }
                }
            }
        }
    }
}
