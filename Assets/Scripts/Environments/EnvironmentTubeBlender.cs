using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Drives the shared tunnel material from an EnvironmentBlend, so the tube itself takes part in the
    /// transition: a solid neon wall can dissolve into the translucent shell the themed levels fly through.
    ///
    /// Tint and emission are plain material properties and update every frame. The grid texture has to be
    /// repainted to change the line and background colours, so it is only rebuilt once the blended colours
    /// have moved far enough to be worth it - a handful of 128x128 repaints across a whole level.
    ///
    /// Every segment shares one material, so the whole tube shifts together rather than segment by segment.
    /// The palette is sampled at the player because that is the only stretch of wall not buried in fog.
    /// </summary>
    public class EnvironmentTubeBlender : MonoBehaviour
    {
        /// <summary>Colour distance that triggers a repaint of the grid texture.</summary>
        private const float RepaintThreshold = 0.015f;

        private const int TextureSize = TunnelGridTexture.Size;

        public Material tunnelMaterial;
        public EnvironmentBlend blend;
        public Vector2 tiling = new Vector2(8f, 1f);

        private Texture2D gridTexture;
        private Color[] pixels;
        private Color paintedLine, paintedBase, paintedAccent;
        private bool painted;
        private bool settled;
        private int settledStop = -1;

        public static EnvironmentTubeBlender Attach(GameObject host, Material tunnelMaterial, EnvironmentBlend blend, Vector2 tiling)
        {
            if (host == null || tunnelMaterial == null || blend == null || !blend.IsValid) return null;

            EnvironmentTubeBlender blender = host.AddComponent<EnvironmentTubeBlender>();
            blender.tunnelMaterial = tunnelMaterial;
            blender.blend = blend;
            blender.tiling = tiling;
            blender.Apply(blender.CurrentDistance());
            return blender;
        }

        private void OnDestroy()
        {
            if (gridTexture != null) Destroy(gridTexture);
        }

        private float CurrentDistance()
        {
            if (PlayerController.Instance != null) return PlayerController.Instance.transform.position.z;
            if (Camera.main != null) return Camera.main.transform.position.z;
            return 0f;
        }

        private void LateUpdate()
        {
            Apply(CurrentDistance());
        }

        private void Apply(float distance)
        {
            if (tunnelMaterial == null || blend == null) return;

            // Nothing to do while the blend is parked on one stop; the wall is already the right colour.
            // Keyed on the stop, not the theme, so a route whose stops share a theme still repaints.
            int fromStop, toStop;
            float t;
            blend.SampleStops(distance, out fromStop, out toStop, out t);
            if (fromStop == toStop)
            {
                if (settled && settledStop == fromStop) return;
                settled = true;
                settledStop = fromStop;
            }
            else
            {
                settled = false;
            }

            EnvironmentPalette p = blend.Evaluate(distance);

            // Matches GameSetup's transparent tube: a dimmed tint for the body, the tint itself for glow.
            Color baseColor = p.tubeTint * 0.4f;
            baseColor.a = p.tubeTint.a;

            if (tunnelMaterial.HasProperty("_BaseColor")) tunnelMaterial.SetColor("_BaseColor", baseColor);
            else if (tunnelMaterial.HasProperty("_Color")) tunnelMaterial.SetColor("_Color", baseColor);

            Color emission = p.tubeTint * p.tubeEmission;
            emission.a = 1f;
            if (tunnelMaterial.HasProperty("_EmissionColor")) tunnelMaterial.SetColor("_EmissionColor", emission);

            RepaintGrid(p.tubeGridColor, p.tubeBaseColor, p.accentColor);
        }

        private void RepaintGrid(Color lineColor, Color bgColor, Color accentColor)
        {
            if (painted && Delta(lineColor, paintedLine) < RepaintThreshold &&
                Delta(bgColor, paintedBase) < RepaintThreshold &&
                Delta(accentColor, paintedAccent) < RepaintThreshold)
            {
                return;
            }

            if (gridTexture == null)
            {
                gridTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true);
                gridTexture.name = "BlendedTunnelGrid";
                gridTexture.wrapMode = TextureWrapMode.Repeat;
                gridTexture.filterMode = FilterMode.Bilinear;
                pixels = new Color[TextureSize * TextureSize];

                string texProperty = tunnelMaterial.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                tunnelMaterial.SetTexture(texProperty, gridTexture);
                tunnelMaterial.SetTextureScale(texProperty, tiling);
            }

            TunnelGridTexture.Paint(pixels, lineColor, bgColor, accentColor);

            gridTexture.SetPixels(pixels);
            gridTexture.Apply();

            paintedLine = lineColor;
            paintedBase = bgColor;
            paintedAccent = accentColor;
            painted = true;
        }

        private static float Delta(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
        }
    }
}
