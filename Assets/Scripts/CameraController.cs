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

        private void LateUpdate()
        {
            if (target == null) return;

            // We want the camera to remain at the center of the cylinder (0, 0, Z)
            // and follow the player's forward movement.
            Vector3 targetPosition = new Vector3(0f, 0f, target.zPos - followDistance);

            // Smoothly lerp towards target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothing * Time.deltaTime);

            // Keep the camera aligned with the tube (pointing straight forward)
            transform.rotation = Quaternion.identity;
        }
    }
}
