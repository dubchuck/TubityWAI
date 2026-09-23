using UnityEngine;

namespace TubityWAI
{
    public enum CollectibleType
    {
        Coin,
        Powerup,
        MagnetPowerup,
        AddSpherePowerup
    }

    public class Collectible : MonoBehaviour
    {
        [Header("Collectible Settings")]
        public CollectibleType type = CollectibleType.Coin;

        [Tooltip("The color index required to collect this item.")]
        public int colorIndex = 0;

        [Header("Animation Settings")]
        [Tooltip("Speed of self-rotation around the local Y-axis.")]
        public float rotationSpeed = 120f;

        [Tooltip("Amplitude of the radial hovering motion.")]
        public float hoverAmplitude = 0.12f;

        [Tooltip("Speed of the hovering oscillation.")]
        public float hoverSpeed = 4f;

        private Vector3 baseLocalPos;
        private float hoverTimeOffset;
        private bool isBeingMagnetized = false;
        private float magnetSpeed = 0f;

        private void Start()
        {
            baseLocalPos = transform.localPosition;
            // Introduce a random offset so coins hover out of phase with each other
            hoverTimeOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (type == CollectibleType.Coin && PlayerController.Instance != null && PlayerController.Instance.IsMagnetActive)
            {
                Transform targetTransform = PlayerController.Instance.GetMagnetTarget(this.colorIndex);
                if (targetTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, targetTransform.position);
                    if (dist < 25f)
                    {
                        isBeingMagnetized = true;
                    }
                }
            }

            if (isBeingMagnetized && PlayerController.Instance != null)
            {
                Transform targetTransform = PlayerController.Instance.GetMagnetTarget(this.colorIndex);
                if (targetTransform != null)
                {
                    magnetSpeed += Time.deltaTime * 60f;
                    transform.position = Vector3.MoveTowards(transform.position, targetTransform.position, magnetSpeed * Time.deltaTime);

                    if (Vector3.Distance(transform.position, targetTransform.position) < 1.5f)
                    {
                        OnCollected(PlayerController.Instance);
                    }
                }
                return; // Skip normal animation
            }

            // 1. Rotate coin around its own axis
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);

            // 2. Perform soft radial hover (pushing in/out relative to the tube center)
            float hoverOffset = Mathf.Sin(Time.time * hoverSpeed + hoverTimeOffset) * hoverAmplitude;
            Vector3 radialDirection = baseLocalPos.normalized;
            transform.localPosition = baseLocalPos + radialDirection * hoverOffset;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 1. Check if the overlapping object is a player sphere and verify color index match
            PlayerSphere sphere = other.GetComponent<PlayerSphere>();
            if (sphere != null)
            {
                if (this.type == CollectibleType.Powerup || this.type == CollectibleType.MagnetPowerup || this.type == CollectibleType.AddSpherePowerup || isBeingMagnetized || sphere.colorIndex == this.colorIndex)
                {
                    Debug.Log($"[Collectible] Collected! Type: {type}.");
                    
                    // Resolve PlayerController from parent hierarchy to update score
                    PlayerController player = null;
                    Transform current = other.transform;
                    while (current != null)
                    {
                        player = current.GetComponent<PlayerController>();
                        if (player != null) break;
                        current = current.parent;
                    }

                    if (player != null)
                    {
                        OnCollected(player);
                    }
                }
                else
                {
                    Debug.Log($"[Collectible] Coin color MISMATCH: Coin index = {colorIndex}, Sphere index = {sphere.colorIndex}. Ignoring.");
                }
            }
        }

        private void OnCollected(PlayerController player)
        {
            if (type == CollectibleType.Coin)
            {
                Debug.Log($"[Collectible] Coin collected! Incrementing player coin count.");
                player.AddCoin();
            }
            else if (type == CollectibleType.Powerup)
            {
                Debug.Log($"[Collectible] Powerup collected! Activating Invincibility.");
                player.ActivateInvincibility();
            }
            else if (type == CollectibleType.MagnetPowerup)
            {
                Debug.Log($"[Collectible] Magnet Powerup collected! Activating Magnet.");
                player.ActivateMagnet();
            }
            else if (type == CollectibleType.AddSpherePowerup)
            {
                Debug.Log($"[Collectible] Add Sphere Powerup collected! Adding sphere.");
                player.RestoreSphere();
            }

            // Clean up the collectible object
            Destroy(gameObject);
        }
    }
}
