using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    /// <summary>
    /// Text in the TubityX display face, drawn from the SDF glyph atlas that
    /// bake_font.py produces. One mesh, one draw call, crisp at any size.
    /// The accent colour rides in TEXCOORD1 so every label in the menu can
    /// share one material while keeping its own neon rim colour.
    /// </summary>
    // Graphic and MaskableGraphic do NOT require a CanvasRenderer - only the
    // concrete graphics do (see Image). Without this the object reaches
    // GraphicRaycaster with no renderer and CanvasRenderer.cull throws.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/TubityX Label")]
    public class TubityXLabel : MaskableGraphic
    {
        public const string AtlasResourcePath = "UI/Tex_TubityXFont";

        [SerializeField] [TextArea(1, 3)] private string text = "PLAY";
        [SerializeField] private float capHeight = 27f;
        [SerializeField] private float tracking = 3f;
        [SerializeField] private Color rimColor = new Color(0.30f, 0.80f, 1f, 1f);
        [Range(0f, 2f)] [SerializeField] private float glowStrength = 0.4f;

        private static Texture2D atlas;

        public string Text
        {
            get { return text; }
            set { if (text != value) { text = value; SetVerticesDirty(); } }
        }

        public Color RimColor
        {
            get { return rimColor; }
            set { rimColor = value; SetVerticesDirty(); }
        }

        public float CapHeight
        {
            get { return capHeight; }
            set { capHeight = value; SetVerticesDirty(); }
        }

        public float Tracking
        {
            get { return tracking; }
            set { tracking = value; SetVerticesDirty(); }
        }

        public override Texture mainTexture
        {
            get
            {
                if (atlas == null) atlas = Resources.Load<Texture2D>(AtlasResourcePath);
                return atlas != null ? (Texture)atlas : Texture2D.whiteTexture;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TubityXPanel.EnableTexCoord1(canvas);
        }

        protected override void Start()
        {
            base.Start();
            TubityXPanel.EnableTexCoord1(canvas);
        }

        /// <summary>Rendered width in local units, for sizing a button to its label.</summary>
        public float MeasureWidth()
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            float s = capHeight / TubityXFontMetrics.Cap;
            float w = 0f;
            for (int i = 0; i < text.Length; i++) w += TubityXFontMetrics.Get(text[i]).Advance * s;
            return w + tracking * Mathf.Max(0, text.Length - 1);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (string.IsNullOrEmpty(text)) return;

            float s = capHeight / TubityXFontMetrics.Cap;
            float pad = TubityXFontMetrics.Pad * s;
            float aw = TubityXFontMetrics.AtlasWidth;
            float ah = TubityXFontMetrics.AtlasHeight;

            Rect r = GetPixelAdjustedRect();
            float pen = r.x + (r.width - MeasureWidth()) * 0.5f;
            float capTop = r.y + (r.height + capHeight) * 0.5f;    // y is up in UI space

            Color32 c = color;
            Vector4 accent = new Vector4(rimColor.r, rimColor.g, rimColor.b, glowStrength);
            int quad = 0;

            for (int i = 0; i < text.Length; i++)
            {
                TubityXFontMetrics.G g = TubityXFontMetrics.Get(text[i]);
                if (text[i] != ' ')
                {
                    float x0 = pen - pad;
                    float x1 = x0 + g.W * s;
                    float y1 = capTop + pad;              // tile top
                    float y0 = y1 - g.H * s;              // tile bottom

                    // atlas rows run top-down; UV v is bottom-up
                    float u0 = g.X / aw;
                    float u1 = (g.X + g.W) / aw;
                    float v1 = 1f - g.Y / ah;
                    float v0 = 1f - (g.Y + g.H) / ah;

                    vh.AddVert(new Vector3(x0, y0), c, new Vector4(u0, v0, 0f, 0f), accent,
                               Vector4.zero, Vector4.zero, Vector3.back, new Vector4(1f, 0f, 0f, -1f));
                    vh.AddVert(new Vector3(x0, y1), c, new Vector4(u0, v1, 0f, 0f), accent,
                               Vector4.zero, Vector4.zero, Vector3.back, new Vector4(1f, 0f, 0f, -1f));
                    vh.AddVert(new Vector3(x1, y1), c, new Vector4(u1, v1, 0f, 0f), accent,
                               Vector4.zero, Vector4.zero, Vector3.back, new Vector4(1f, 0f, 0f, -1f));
                    vh.AddVert(new Vector3(x1, y0), c, new Vector4(u1, v0, 0f, 0f), accent,
                               Vector4.zero, Vector4.zero, Vector3.back, new Vector4(1f, 0f, 0f, -1f));

                    int b = quad * 4;
                    vh.AddTriangle(b, b + 1, b + 2);
                    vh.AddTriangle(b + 2, b + 3, b);
                    quad++;
                }
                pen += g.Advance * s + tracking;
            }
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
