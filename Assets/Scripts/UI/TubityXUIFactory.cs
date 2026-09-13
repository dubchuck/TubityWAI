using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    /// <summary>
    /// Builds menu chrome in the TubityX logo's visual language: analytic
    /// rounded-glass panels with neon rims, and labels in the display face.
    ///
    /// Rim colour and pulse now travel in the vertex stream (TEXCOORD2), so every
    /// panel and button in the menu shares ONE material regardless of colour -
    /// the whole menu batches into a panel pass and a label pass.
    /// </summary>
    public static class TubityXUIFactory
    {
        public const string PanelMaterial = "UI/Mat_TubityXPanel";
        public const string LabelMaterial = "UI/Mat_TubityXLabel";

        // kept so older call sites still compile; both resolve to the one material
        public const string BlueButtonMaterial = PanelMaterial;
        public const string PurpleButtonMaterial = PanelMaterial;

        public static readonly Color Blue = new Color(0.20f, 0.62f, 1.00f);
        public static readonly Color Purple = new Color(0.62f, 0.32f, 1.00f);
        public static readonly Color Cyan = new Color(0.20f, 0.85f, 1.00f);
        public static readonly Color Gold = new Color(1.00f, 0.72f, 0.20f);

        private static Material panelMat, labelMat;

        private static Material Panel()
        {
            if (panelMat == null) panelMat = Load(PanelMaterial);
            return panelMat;
        }

        private static Material Label()
        {
            if (labelMat == null) labelMat = Load(LabelMaterial);
            return labelMat;
        }

        private static Material Load(string path)
        {
            Material m = Resources.Load<Material>(path);
            if (m == null)
                Debug.LogWarning("[TubityXUIFactory] Missing material Assets/Resources/" + path + ".mat");
            return m;
        }

        /// <summary>
        /// A glass panel: the popup and screen background. No Button, no input.
        /// </summary>
        public static GameObject CreatePanel(Transform parent, Vector2 size, Color accent,
                                             Vector2 anchorMin, Vector2 anchorMax,
                                             Vector2 anchoredPosition, float cornerRadius = 22f)
        {
            GameObject root = new GameObject("TubityXPanel",
                                             typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            TubityXPanel panel = root.AddComponent<TubityXPanel>();
            panel.CornerRadius = cornerRadius;
            panel.RimColor = Opaque(accent);
            panel.PulseAmount = 0f;
            panel.material = Panel();
            panel.raycastTarget = true;      // a popup should swallow clicks behind it
            return root;
        }

        /// <summary>
        /// A TubityX button. The returned object carries the Button component, so
        /// existing call sites keep wiring onClick the same way.
        /// </summary>
        public static GameObject CreateButton(Transform parent, Vector2 size, string label,
                                              Color accent, float capHeight = 27f,
                                              float tracking = 3f, float cornerRadius = 16f,
                                              bool pulse = false)
        {
            GameObject root = new GameObject("TubityXButton",
                                             typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            TubityXPanel panel = root.AddComponent<TubityXPanel>();
            panel.CornerRadius = cornerRadius;
            panel.RimColor = Opaque(accent);
            panel.PulseAmount = pulse ? 1f : 0f;
            panel.material = Panel();
            panel.raycastTarget = true;

            if (!string.IsNullOrEmpty(label))
                AddLabel(root, label, capHeight, Color.white, accent, tracking);

            Button button = root.AddComponent<Button>();
            button.targetGraphic = panel;

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

        /// <summary>
        /// Overload matching the older signature, so the material-path argument
        /// from earlier call sites still compiles.
        /// </summary>
        public static GameObject CreateButton(Transform parent, Vector2 size, string label,
                                              string panelMaterialPath, Color accent,
                                              float capHeight = 27f, float tracking = 3f,
                                              float cornerRadius = 16f)
        {
            return CreateButton(parent, size, label, accent, capHeight, tracking, cornerRadius,
                                panelMaterialPath == BlueButtonMaterial);
        }

        /// <summary>Text in the display face, stretched over an existing object.</summary>
        public static TubityXLabel AddLabel(GameObject host, string text, float capHeight,
                                            Color face, Color accent, float tracking = 2f,
                                            TubityXLabel.Align align = TubityXLabel.Align.Center)
        {
            GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            obj.transform.SetParent(host.transform, false);
            RectTransform r = obj.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero;
            r.anchoredPosition = Vector2.zero;

            TubityXLabel lbl = obj.AddComponent<TubityXLabel>();
            lbl.material = Label();
            lbl.Text = text;
            lbl.CapHeight = capHeight;
            lbl.Tracking = tracking;
            lbl.RimColor = Opaque(accent);
            lbl.Alignment = align;
            lbl.color = face;
            lbl.raycastTarget = false;
            return lbl;
        }

        /// <summary>
        /// The old palette carries alpha (0.4) on its accent colours, which would
        /// wash the rim out. Rim alpha is not meaningful here, so drop it.
        /// </summary>
        private static Color Opaque(Color c)
        {
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (m < 0.35f && m > 0.001f) c *= 0.85f / m;    // lift very dark accents
            return new Color(c.r, c.g, c.b, 1f);
        }
    }
}
