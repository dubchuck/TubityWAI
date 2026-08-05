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

        // The current angle (theta) around the cylinder axis in radians.
        [HideInInspector]
        public float currentAngle = 0f;

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

        private void Start()
        {
            zPos = transform.position.z;
            lastZ = zPos;
            Score = 0;
            Coins = 0;
            TimeElapsed = 0f;

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
                        childSpheres.Add(info);
                    }
                }
            }
        }

        public void AddCoin()
        {
            Coins++;
        }

        private void Update()
        {
            // 1. Handle keyboard inputs (supporting both steering and speed boost)
            float steerInput = 0f;
            bool isDownPressed = false;
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
            }
            else
            {
                steerInput = Input.GetAxisRaw("Horizontal");
                isDownPressed = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            }

            // 2. Handle Space jump inputs
            bool spacePressed = false;
            if (Keyboard.current != null)
            {
                spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
            else
            {
                spacePressed = Input.GetKeyDown(KeyCode.Space);
            }

            // 3. Process mobile touch & editor mouse inputs
            ProcessTouchAndMouseInputs(ref steerInput, ref spacePressed);


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
            float activeSpeed = forwardSpeed * (isDownPressed ? speedBoostMultiplier : 1f);
            zPos += activeSpeed * Time.deltaTime;
            transform.position = new Vector3(0f, 0f, zPos);

            // Update Volumetric Light Position ahead of the player
            if (volumetricLightTransform != null)
            {
                volumetricLightTransform.position = new Vector3(0f, 0f, zPos + volumetricLightDistance);
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
                }
                else if (!hasCrossedOver)
                {
                    // Double press space to initiate crossover to the opposite side
                    jumpTargetAngle = jumpStartAngle + Mathf.PI;
                    hasCrossedOver = true;
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
            // Process Mobile Touches
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    Vector2 pos = touch.position;

                    // Bottom third of screen: Jump (trigger on Began)
                    if (pos.y < Screen.height / 3f)
                    {
                        if (touch.phase == UnityEngine.TouchPhase.Began)
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
            // Process Editor Mouse Clicks (for easy mock testing/play in Editor)
            else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Input.mousePosition;

                // Bottom third of screen: Jump (trigger on Down)
                if (mousePos.y < Screen.height / 3f)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        spacePressed = true;
                    }
                }
                else if (Input.GetMouseButton(0))
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
