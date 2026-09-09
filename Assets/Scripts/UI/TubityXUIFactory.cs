using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    /// <summary>
    /// Builds menu buttons in the TubityX logo's visual language: an analytic
    /// rounded-glass panel with a neon rim, and a label in the display face.
    /// Two graphics per button instead of the five the older glass stack used.
    /// </summary>
    public static class TubityXUIFactory
    {
        public const string BlueButtonMaterial   = "UI/Mat_TubityXButtonBlue";
        public const string PurpleButtonMaterial = "UI/Mat_TubityXButtonPurple";
        public const string LabelMaterial        = "UI/Mat_TubityXLabel";

        private static Material Load(string path)
        {
            Material m = Resources.Load<Material>(path);
            if (m == null)
                Debug.LogWarning("[TubityXUIFactory] Missing material Assets/Resources/" + path + ".mat");
            return m;
        }

        /// <summary>
        /// A TubityX button. The returned object carries the Button component,
        /// so existing call sites can keep wiring onClick the same way.
        /// </summary>
        /// <param name="accent">Neon colour for the label's rim and glow.</param>
        /// <param name="panelMaterialPath">Blue pulses; purple is steady.</param>
        public static GameObject CreateButton(Transform parent, Vector2 size, string label,
                                              string panelMaterialPath, Color accent,
                                              float capHeight = 27f, float tracking = 3f,
                                              float cornerRadius = 16f)
        {
            // Create the renderer with the object rather than relying on the
            // Graphic base class to require one - it does not.
            GameObject root = new GameObject("TubityXButton",
                                             typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            TubityXPanel panel = root.AddComponent<TubityXPanel>();
            panel.CornerRadius = cornerRadius;
            panel.material = Load(panelMaterialPath);
            panel.raycastTarget = true;

            if (!string.IsNullOrEmpty(label))
            {
                GameObject labelObj = new GameObject("Label",
                                                     typeof(RectTransform), typeof(CanvasRenderer));
                labelObj.transform.SetParent(root.transform, false);
                RectTransform lr = labelObj.GetComponent<RectTransform>();
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.sizeDelta = Vector2.zero;
                lr.anchoredPosition = Vector2.zero;

                TubityXLabel text = labelObj.AddComponent<TubityXLabel>();
                text.material = Load(LabelMaterial);
                text.Text = label;
                text.RimColor = accent;
                text.raycastTarget = false;
                text.CapHeight = capHeight;
                text.Tracking = tracking;
            }

            Button button = root.AddComponent<Button>();
            button.targetGraphic = panel;

            // The panel handles its own hover lighting, so keep uGUI's tint out
            // of it except for the press, where a slight dim reads as feedback.
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            cb.fadeDuration = 0.08f;
            button.colors = cb;

            TubityXButtonFX fx = root.AddComponent<TubityXButtonFX>();
            fx.panel = panel;

            return root;
        }
    }
}
