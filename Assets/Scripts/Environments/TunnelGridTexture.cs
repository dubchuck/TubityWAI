using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The tube wall's grid pattern, in one place. GameSetup bakes it once for a fixed level and
    /// EnvironmentTubeBlender repaints it as a blended level crosses between themes, so the two
    /// have to agree pixel for pixel or the wall would jump the first time the blender takes over.
    ///
    /// A cell is not a flat border any more: the line has a glow falloff spilling inward, the
    /// background carries a faint scanline and a diagonal weave, and a dim accent hairline crosses
    /// the middle. All of it is baked into a 128x128 texture once, so the extra detail is free at
    /// run time - it just means the surface the player stares at for the whole level has something
    /// to catch the light instead of reading as flat colour.
    /// </summary>
    public static class TunnelGridTexture
    {
        public const int Size = 128;
        private const int LineThickness = 4;
        private const float GlowReach = 13f;    // pixels the line bleeds inward from a cell edge
        private const int HairlineHalf = 1;     // half-width of the accent cross through the cell

        /// <summary>A fresh grid texture, ready to bind to a tunnel material.</summary>
        public static Texture2D Create(Color line, Color background, Color accent)
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            tex.name = "TunnelGrid";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[Size * Size];
            Paint(pixels, line, background, accent);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Fills a caller-owned Size*Size buffer. The blender reuses one array across repaints so a
        /// cross-fade never allocates.
        /// </summary>
        public static void Paint(Color[] pixels, Color line, Color background, Color accent)
        {
            if (pixels == null || pixels.Length < Size * Size) return;

            // The accent hairline is the brand's second colour showing through the wall. It stays
            // close to the background so it reads as a machined detail, not a second grid.
            Color hairline = Color.Lerp(background, accent, 0.30f);
            hairline.a = background.a;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Distance to the nearest cell edge, in both axes at once.
                    int edge = Mathf.Min(Mathf.Min(x, Size - 1 - x), Mathf.Min(y, Size - 1 - y));

                    Color c;
                    if (edge < LineThickness)
                    {
                        c = line;
                    }
                    else
                    {
                        c = background;

                        // A faint weave so the flat middle of a cell is not literally one colour:
                        // one scanline along the tube plus a slow diagonal, both within a few
                        // percent of the background so they never read as a pattern on their own.
                        float scan = (y % 8 < 4) ? 0.028f : -0.014f;
                        float weave = Mathf.Sin((x + y) * 0.19634954f) * 0.016f;   // 2*pi/32
                        c = Lift(c, scan + weave);

                        int midX = Mathf.Abs(x - Size / 2);
                        int midY = Mathf.Abs(y - Size / 2);
                        if (midX <= HairlineHalf || midY <= HairlineHalf) c = hairline;

                        // Glow spilling in off the four edges, strongest right against the line.
                        float glow = Mathf.Clamp01(1f - (edge - LineThickness) / GlowReach);
                        glow *= glow;                       // quadratic falloff reads as light, not a gradient
                        c = Color.Lerp(c, line, glow * 0.55f);
                        c.a = Mathf.Lerp(background.a, line.a, glow * 0.55f);
                    }

                    pixels[y * Size + x] = c;
                }
            }
        }

        /// <summary>Nudges a colour's brightness without touching its hue or alpha.</summary>
        private static Color Lift(Color c, float amount)
        {
            return new Color(
                Mathf.Clamp01(c.r + amount * 0.5f + c.r * amount),
                Mathf.Clamp01(c.g + amount * 0.5f + c.g * amount),
                Mathf.Clamp01(c.b + amount * 0.5f + c.b * amount),
                c.a);
        }
    }
}
