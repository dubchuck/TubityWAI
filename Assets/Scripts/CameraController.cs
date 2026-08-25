using UnityEngine;

namespace TubityWAI
{
    public class CameraController : MonoBehaviour
    {
        [Header("Follow Settings")]
        [Tooltip("The player to follow.")]
        public PlayerController target;

        [Tooltip("Distance behind the player (along Z axis).")]
        public float followDistance = 10f;

        [Tooltip("How smoothly the camera catches up to the player's movement.")]
        public float followSmoothing = 15f;

        [Tooltip("Speed when in attraction mode (no target).")]
        public float attractionSpeed = 7.5f;

        [Header("Powerup Settings")]
        public float defaultFOV = 60f;
        public float invincibilityFOV = 90f;

        private Camera cam;

        private float jitterTimer = 0f;
        private float jitterMagnitude = 0f;

        public void TriggerJitter(float duration, float magnitude)
        {
            jitterTimer = duration;
            jitterMagnitude = magnitude;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                // In attraction mode, move straight forward along Z and look slightly down (11 degrees) to see the road
                transform.position += new Vector3(0f, 0f, attractionSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(11f, 0f, 0f);
                return;
            }

            // Check if there is a custom camera configuration for this level
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            
            float offsetX = 0f;
            float offsetY = 0f;
            float rotX = 0f;
            float rotY = 0f;
            
            if (config != null && config.useCustomCamera)
            {
                offsetX = config.cameraOffsetX;
                offsetY = config.cameraOffsetY;
                rotX = config.cameraRotationX;
                rotY = config.cameraRotationY;
            }

            // We want the camera to remain at the center of the cylinder (with optional offsets)
            // and follow the player's forward movement.
            Vector3 targetCurveOffset = Vector3.zero;
            if (config != null)
            {
                targetCurveOffset = config.GetCurveOffset(target.zPos);
            }

            float dynamicOffsetX = 0f;
            float dynamicRotY = 0f;
            if (config != null && config.hasCurves)
            {
                // Look ahead 25 units to detect upcoming bend direction
                Vector3 aheadCurve = config.GetCurveOffset(target.zPos + 25f);
                float bendDirectionX = aheadCurve.x - targetCurveOffset.x;
                
                // Shift camera to the outside of the curve (opposite to bend direction) to maximize sightline
                dynamicOffsetX = -bendDirectionX * 0.6f; 
                
                // Pan camera towards the bend (yaw rotation) to follow the curve
                dynamicRotY = bendDirectionX * 4.0f; 
            }

            Vector3 targetPosition = new Vector3(
                targetCurveOffset.x + offsetX + dynamicOffsetX, 
                targetCurveOffset.y + offsetY, 
                target.zPos - followDistance
            );

            // Smoothly lerp towards target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothing * Time.deltaTime);

            // Apply camera rotation (default to identity if rotX and rotY are 0)
            transform.rotation = Quaternion.Euler(rotX, rotY + dynamicRotY, 0f);

            // Apply Powerup FOV Zoom Effect
            if (cam != null)
            {
                float targetFOV = Mathf.Lerp(defaultFOV, invincibilityFOV, target.InvincibilityEffectStrength);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 8f);
            }

            // Apply Jitter Effect
            if (jitterTimer > 0)
            {
                transform.position += Random.insideUnitSphere * jitterMagnitude;
                jitterTimer -= Time.deltaTime;
            }
        }
    }
}
