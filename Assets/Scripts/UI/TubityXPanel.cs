using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    /// <summary>
    /// Button / panel chrome for the TubityX menu: one quad, no texture.
    /// UI/TubityXPanel.shader evaluates the rounded rectangle analytically, so
    /// the geometry travels in the vertex stream rather than in material
    /// properties - which is what lets differently sized buttons and their
    /// hover state share a single material and a single draw call.
    /// </summary>
    // Graphic and MaskableGraphic do NOT require a CanvasRenderer - only the
    // concrete graphics do (see Image). Without this the object reaches
    // GraphicRaycaster with no renderer and CanvasRenderer.cull throws.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/TubityX Panel")]
    public class TubityXPanel : MaskableGraphic
    {
        [SerializeField] private float cornerRadius = 16f;

        [Tooltip("Extra mesh around the rect so the outer halo is not clipped. " +
                 "Keep this at or above the material's Halo Range.")]
        [SerializeField] private float glowPadding = 26f;

        [Range(0f, 1f)]
        [SerializeField] private float highlight;

        [Header("Colour")]
        [Tooltip("Neon rim colour. Travels in the vertex stream, so every panel in " +
                 "the menu can have its own colour and still share one material.")]
        [SerializeField] private Color rimColor = new Color(0.20f, 0.62f, 1f, 1f);

        [Range(0f, 1f)]
        [Tooltip("0 = steady border, 1 = full colour pulse.")]
        [SerializeField] private float pulseAmount;

        /// <summary>0 = idle, 1 = hovered / focused. Written into the vertex
        /// stream, so changing it never instances the material.</summary>
        public float Highlight
        {
            get { return highlight; }
            set
            {
                float v = Mathf.Clamp01(value);
                if (Mathf.Approximately(v, highlight)) return;
                highlight = v;
                SetVerticesDirty();
            }
        }

        public float CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = value; SetVerticesDirty(); }
        }

        public Color RimColor
        {
            get { return rimColor; }
            set { rimColor = value; SetVerticesDirty(); }
        }

        public float PulseAmount
        {
            get { return pulseAmount; }
            set { pulseAmount = Mathf.Clamp01(value); SetVerticesDirty(); }
        }

        /// <summary>The shaders read geometry from TEXCOORD1 and colour from
        /// TEXCOORD2. Canvases send neither by default.</summary>
        internal static void EnableTexCoord1(Canvas canvas)
        {
            if (canvas == null) return;
            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            root.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                           | AdditionalCanvasShaderChannels.TexCoord2;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnableTexCoord1(canvas);
        }

        protected override void Start()
        {
            base.Start();
            EnableTexCoord1(canvas);   // canvas can still be null during OnEnable
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            float halfW = r.width * 0.5f;
            float halfH = r.height * 0.5f;
            float cx = r.x + halfW;
            float cy = r.y + halfH;
            float pad = Mathf.Max(0f, glowPadding);

            float ex = halfW + pad;
            float ey = halfH + pad;
            Color32 c = color;
            Vector4 shape = new Vector4(halfW, halfH,
                                        Mathf.Min(cornerRadius, Mathf.Min(halfW, halfH)),
                                        highlight);
            Vector4 tint = new Vector4(rimColor.r, rimColor.g, rimColor.b, pulseAmount);

            AddVert(vh, cx - ex, cy - ey, -ex, -ey, c, shape, tint);
            AddVert(vh, cx - ex, cy + ey, -ex, ey, c, shape, tint);
            AddVert(vh, cx + ex, cy + ey, ex, ey, c, shape, tint);
            AddVert(vh, cx + ex, cy - ey, ex, -ey, c, shape, tint);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private static void AddVert(VertexHelper vh, float x, float y, float lx, float ly,
                                    Color32 c, Vector4 shape, Vector4 tint)
        {
            // uv0 = pixels from the quad centre, uv1 = rounded-rect shape,
            // uv2 = rim colour + pulse amount
            vh.AddVert(new Vector3(x, y), c,
                       new Vector4(lx, ly, 0f, 0f), shape, tint, Vector4.zero,
                       Vector3.back, new Vector4(1f, 0f, 0f, -1f));
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}
