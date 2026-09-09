using UnityEngine;
using UnityEngine.EventSystems;

namespace TubityWAI
{
    /// <summary>
    /// Hover / press feedback for a TubityXPanel button. The highlight is a
    /// vertex value rather than a material property, so lighting up a button
    /// costs a mesh rebuild instead of a material instance - and buttons that
    /// share a material still batch.
    /// Also handles tvOS / gamepad focus via ISelectHandler.
    /// </summary>
    [AddComponentMenu("UI/TubityX Button FX")]
    public class TubityXButtonFX : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        public TubityXPanel panel;
        public float hoverScale = 1.05f;
        public float pressScale = 0.96f;
        public float responsiveness = 15f;

        private Vector3 baseScale;
        private Vector3 targetScale;
        private float targetHighlight;
        private bool hovered, selected;

        private void Awake()
        {
            baseScale = transform.localScale;
            targetScale = baseScale;
            if (panel == null) panel = GetComponent<TubityXPanel>();
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale,
                                                Time.unscaledDeltaTime * responsiveness);
            if (panel != null)
            {
                float h = Mathf.Lerp(panel.Highlight, targetHighlight,
                                     Time.unscaledDeltaTime * responsiveness);
                if (Mathf.Abs(h - panel.Highlight) > 0.002f) panel.Highlight = h;
                else if (!Mathf.Approximately(panel.Highlight, targetHighlight))
                    panel.Highlight = targetHighlight;
            }
        }

        private void Refresh(bool pressed)
        {
            bool lit = hovered || selected;
            targetHighlight = lit ? 1f : 0f;
            targetScale = baseScale * (pressed ? pressScale : (lit ? hoverScale : 1f));
        }

        public void OnPointerEnter(PointerEventData e) { hovered = true; Refresh(false); }
        public void OnPointerExit(PointerEventData e) { hovered = false; Refresh(false); }
        public void OnPointerDown(PointerEventData e) { Refresh(true); }
        public void OnPointerUp(PointerEventData e) { Refresh(false); }
        public void OnSelect(BaseEventData e) { selected = true; Refresh(false); }
        public void OnDeselect(BaseEventData e) { selected = false; Refresh(false); }
    }
}
