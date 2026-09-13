using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Slow spin and/or vertical bob for a scenery prop. Cheap enough to run on
    /// a dozen objects per segment; Rebase() is called after every placement.
    /// </summary>
    public class SceneryDrift : MonoBehaviour
    {
        public Vector3 spin;
        public float bobAmplitude;
        public float bobSpeed = 1f;

        private Vector3 basePosition;
        private float phase;

        public void Rebase()
        {
            basePosition = transform.localPosition;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (spin != Vector3.zero)
            {
                transform.Rotate(spin * Time.deltaTime, Space.Self);
            }
            if (bobAmplitude > 0f)
            {
                transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude);
            }
        }
    }
}
