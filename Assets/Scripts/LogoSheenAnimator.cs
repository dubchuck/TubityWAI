using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LogoSheenAnimator : MonoBehaviour
{
    public RectTransform maskRect;
    public RectTransform contentContainer;
    public RectTransform originalLogoRect;
    
    public float animationDuration = 3f;
    public float cooldownDuration = 5f;

    private float startX;
    private float endX;

    void Start()
    {
        if (maskRect == null || contentContainer == null || originalLogoRect == null) return;
        
        float width = originalLogoRect.rect.width;
        // Start to the left of the logo, extra padding for the rotated mask
        startX = -width / 2f - maskRect.rect.width - 200f;
        // End to the right of the logo
        endX = width / 2f + maskRect.rect.width + 200f;
        
        StartCoroutine(SheenRoutine());
    }

    void LateUpdate()
    {
        if (maskRect != null && contentContainer != null && originalLogoRect != null)
        {
            // Counter-act the mask's movement and rotation so the content stays perfectly aligned with the original logo
            contentContainer.position = originalLogoRect.position;
            contentContainer.rotation = originalLogoRect.rotation;
        }
    }

    IEnumerator SheenRoutine()
    {
        while (true)
        {
            float elapsed = 0f;
            Vector2 startPos = new Vector2(startX, 0f);
            Vector2 endPos = new Vector2(endX, 0f);
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float easedT = t * t * (3f - 2f * t);
                maskRect.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);
                yield return null;
            }

            // Move off-screen immediately during cooldown
            maskRect.anchoredPosition = new Vector2(9999f, 9999f);
            
            yield return new WaitForSeconds(cooldownDuration);
        }
    }
}
