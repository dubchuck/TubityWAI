using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Scrolls a material's texture over time - the lava river flowing past. The offset is set
    /// from the clock rather than accumulated, so several props sharing one material all agree
    /// and none of them speeds it up.
    /// </summary>
    public class MaterialScroll : MonoBehaviour
    {
        public Material target;
        public Vector2 speed = new Vector2(0f, -0.25f);

        private void Update()
        {
            if (target == null) return;
            Vector2 offset = speed * Time.time;
            offset.x -= Mathf.Floor(offset.x);
            offset.y -= Mathf.Floor(offset.y);
            if (target.HasProperty("_BaseMap")) target.SetTextureOffset("_BaseMap", offset);
            else target.mainTextureOffset = offset;
        }
    }

    /// <summary>Slow breathing in scale - prominence loops swelling and settling.</summary>
    public class PulseScale : MonoBehaviour
    {
        public float amount = 0.08f;
        public float period = 5f;

        private Vector3 baseScale;
        private float phase;

        private void Start()
        {
            baseScale = transform.localScale;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, period) + phase) * amount;
            transform.localScale = baseScale * s;
        }
    }
}
