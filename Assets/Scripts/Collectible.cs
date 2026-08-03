using UnityEngine;

namespace TubityWAI
{
    public enum CollectibleType
    {
        Coin,
        Powerup
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

        private void Start()
        {
            baseLocalPos = transform.localPosition;
            // Introduce a random offset so coins hover out of phase with each other
            hoverTimeOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
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
                if (sphere.colorIndex == this.colorIndex)
                {
                    Debug.Log($"[Collectible] Coin color match ({colorIndex}) with {other.name}! Collecting...");
                    
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
                // Future Powerup logic goes here
            }

            // Clean up the collectible object
            Destroy(gameObject);
        }
    }
}
