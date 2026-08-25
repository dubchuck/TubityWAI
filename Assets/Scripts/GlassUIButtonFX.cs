using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TubityWAI
{
    [RequireComponent(typeof(Button))]
    public class GlassUIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool enablePulse = false;
        public Outline neonRim;
        public Shadow ambientGlow;
        
        private Vector3 originalScale;
        private Vector3 targetScale;
        public float hoverScaleMultiplier = 1.05f;
        public float clickScaleMultiplier = 0.95f;

        public float pulseSpeed = 4f;
        private float baseGlowAlpha = 0.45f;
        private float baseRimAlpha = 1f;
        
        private void Start()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;
            
            if (neonRim != null) baseRimAlpha = neonRim.effectColor.a;
            if (ambientGlow != null) baseGlowAlpha = ambientGlow.effectColor.a;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);

            if (enablePulse && neonRim != null && ambientGlow != null)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0 to 1
                
                Color rColor = neonRim.effectColor;
                rColor.a = baseRimAlpha + (pulse * 0.2f);
                neonRim.effectColor = rColor;
                
                Color gColor = ambientGlow.effectColor;
                gColor.a = baseGlowAlpha + (pulse * 0.3f);
                ambientGlow.effectColor = gColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = originalScale * hoverScaleMultiplier;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = originalScale * clickScaleMultiplier;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Return to hover scale if still hovered, else exit handles it
            targetScale = originalScale * hoverScaleMultiplier;
        }
    }
}
