using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Keeps a full-stretch RectTransform inside Screen.safeArea, so HUD
    /// corners clear the notch, rounded display corners and the home
    /// indicator. Parent everything that hugs a screen edge under this rect.
    ///
    /// Anchors are set as fractions of the screen, which is what makes this
    /// independent of the CanvasScaler's reference resolution.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Safe Area Fitter")]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Extra inset in reference pixels, on top of the safe area. " +
                 "Gives neon halos room so they are not cut by the screen edge.")]
        public float padding = 0f;

        private RectTransform rect;
        private Rect lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2Int lastScreen = Vector2Int.zero;

        /// <summary>Create a stretched safe-area container under a canvas.</summary>
        public static RectTransform Create(Transform canvas, string name = "SafeArea",
                                           float padding = 0f)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(canvas, false);
            RectTransform r = obj.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            SafeAreaFitter fitter = obj.AddComponent<SafeAreaFitter>();
            fitter.padding = padding;
            fitter.Apply();
            return r;
        }

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            // Rotation, split view and the editor's device simulator all change
            // the safe area at runtime; poll rather than trust a one-off.
            if (Screen.safeArea != lastSafeArea ||
                Screen.width != lastScreen.x || Screen.height != lastScreen.y)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;  min.y /= Screen.height;
            max.x /= Screen.width;  max.y /= Screen.height;

            // A bad safe area (some editor / platform combinations report zero)
            // must never collapse the whole HUD.
            if (max.x - min.x < 0.2f || max.y - min.y < 0.2f)
            {
                min = Vector2.zero;
                max = Vector2.one;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
