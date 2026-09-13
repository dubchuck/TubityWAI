using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    public static class GlassUIFactory
    {
        private static Sprite cachedCircleSprite;
        private static Sprite cachedRoundedRectSprite;
        private static Sprite cachedRingSprite;
        private static Sprite cachedLockSprite;
        private static Sprite cachedLockOpenSprite;

        public static Sprite GetCircleSprite()
        {
            if (cachedCircleSprite == null)
            {
#if UNITY_EDITOR
                cachedCircleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Circle.png");
                if (cachedCircleSprite != null) return cachedCircleSprite;
#endif
                int size = 32;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size * size];
                float radius = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - radius + 0.5f;
                        float dy = y - radius + 0.5f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();
                cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            return cachedCircleSprite;
        }

        public static Sprite GetRoundedRectSprite()
        {
            if (cachedRoundedRectSprite == null)
            {
#if UNITY_EDITOR
                cachedRoundedRectSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/RoundedRect.png");
                if (cachedRoundedRectSprite != null) return cachedRoundedRectSprite;
#endif
                int width = 128;
                int height = 128;
                int cornerRadius = 12; // Minimalist sharper corners

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[width * height];
                float r = cornerRadius;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float cx = (x < r) ? r - x : (x > width - 1 - r) ? x - (width - 1 - r) : 0f;
                        float cy = (y < r) ? r - y : (y > height - 1 - r) ? y - (height - 1 - r) : 0f;
                        float dist = Mathf.Sqrt(cx * cx + cy * cy);
                        float alpha = Mathf.Clamp01(r - dist + 0.5f);
                        pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();
                Vector4 border = new Vector4(r, r, r, r);
                cachedRoundedRectSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            }
            return cachedRoundedRectSprite;
        }

        public static Sprite GetRingSprite()
        {
            if (cachedRingSprite == null)
            {
#if UNITY_EDITOR
                cachedRingSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Ring.png");
                if (cachedRingSprite != null) return cachedRingSprite;
#endif
                int size = 128;
                float innerR = 50f;
                float outerR = 62f;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size * size];
                float center = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center + 0.5f;
                        float dy = y - center + 0.5f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        float alphaInner = Mathf.Clamp01(dist - innerR);
                        float alphaOuter = Mathf.Clamp01(outerR - dist);
                        float alpha = Mathf.Min(alphaInner, alphaOuter);

                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();
                cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            return cachedRingSprite;
        }

        /// <summary>
        /// A padlock silhouette, drawn with the same per-pixel analytic distance
        /// + antialiasing technique as the sprites above. `open` springs the
        /// shackle free of the body's right leg, for the "just unlocked"
        /// celebration; the closed version marks a still-locked sphere-count block.
        /// </summary>
        public static Sprite GetLockSprite(bool open = false)
        {
            if (open)
            {
                if (cachedLockOpenSprite == null) cachedLockOpenSprite = BuildLockSprite(true);
                return cachedLockOpenSprite;
            }
            if (cachedLockSprite == null) cachedLockSprite = BuildLockSprite(false);
            return cachedLockSprite;
        }

        private static Sprite BuildLockSprite(bool open)
        {
            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float cx = size * 0.5f;
            float bodyHalfW = size * 0.26f;
            float bodyLeft = cx - bodyHalfW;
            float bodyBottom = size * 0.08f;
            float bodyTop = size * 0.52f;
            float bodyCornerR = size * 0.09f;
            float bodyW = bodyHalfW * 2f;
            float bodyH = bodyTop - bodyBottom;

            float legOffset = size * 0.14f;
            float legThickness = size * 0.10f;
            float archOuterR = legOffset + legThickness * 0.5f;
            float archInnerR = legOffset - legThickness * 0.5f;
            float legHeight = size * 0.16f;
            float archCenterX = open ? cx - legOffset * 0.25f : cx;
            float archCenterY = bodyTop + legHeight + (open ? size * 0.05f : 0f);

            float keyholeR = size * 0.045f;
            float keyholeCenterY = bodyBottom + bodyH * 0.42f;
            float slotHalfW = size * 0.022f;
            float slotTop = keyholeCenterY;
            float slotBottom = bodyBottom + bodyH * 0.18f;

            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f;
                    float fy = y + 0.5f;

                    // Body: rounded rect, same corner-distance technique as GetRoundedRectSprite.
                    float lx = fx - bodyLeft;
                    float ly = fy - bodyBottom;
                    float bcx = (lx < bodyCornerR) ? bodyCornerR - lx : (lx > bodyW - bodyCornerR ? lx - (bodyW - bodyCornerR) : 0f);
                    float bcy = (ly < bodyCornerR) ? bodyCornerR - ly : (ly > bodyH - bodyCornerR ? ly - (bodyH - bodyCornerR) : 0f);
                    float bodyDist = Mathf.Sqrt(bcx * bcx + bcy * bcy);
                    float alphaBody = Mathf.Clamp01(bodyCornerR - bodyDist + 0.5f);

                    // Keyhole cutout: a circle over a short slot.
                    float kdx = fx - cx;
                    float kdy = fy - keyholeCenterY;
                    float alphaKeyholeCircle = Mathf.Clamp01(keyholeR - Mathf.Sqrt(kdx * kdx + kdy * kdy) + 0.5f);
                    float slotDx = Mathf.Abs(fx - cx) - slotHalfW;
                    float alphaSlot = (fy <= slotTop && fy >= slotBottom) ? Mathf.Clamp01(0.5f - slotDx) : 0f;
                    alphaBody *= 1f - Mathf.Max(alphaKeyholeCircle, alphaSlot);

                    // Shackle: left leg always present; right leg only when closed/locked.
                    float alphaLeg1 = LockLegAlpha(fx, fy, archCenterX - legOffset, legThickness, bodyTop, archCenterY);
                    float alphaLeg2 = open ? 0f : LockLegAlpha(fx, fy, archCenterX + legOffset, legThickness, bodyTop, archCenterY);

                    float adx = fx - archCenterX;
                    float ady = fy - archCenterY;
                    float archDist = Mathf.Sqrt(adx * adx + ady * ady);
                    float alphaArch = ady >= 0f
                        ? Mathf.Min(Mathf.Clamp01(archDist - archInnerR + 0.5f), Mathf.Clamp01(archOuterR - archDist + 0.5f))
                        : 0f;

                    float alpha = Mathf.Max(alphaBody, Mathf.Max(alphaLeg1, Mathf.Max(alphaLeg2, alphaArch)));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float LockLegAlpha(float x, float y, float legX, float thickness, float yFrom, float yTo)
        {
            if (y < yFrom - 0.5f || y > yTo + 0.5f) return 0f;
            float dx = Mathf.Abs(x - legX) - thickness * 0.5f;
            return Mathf.Clamp01(0.5f - dx);
        }

        // ------------------------------------------------------------------
        // Panels and buttons now render through TubityXPanel: an analytic
        // rounded rectangle with a neon rim, evaluated in the shader. These
        // wrappers keep the old signatures so every call site in the menu moved
        // over at once, but the five-graphic Image/Outline/Shadow stack is gone -
        // each control is one graphic plus its label, and rim colour rides in the
        // vertex stream so the whole menu shares a single material.
        //
        // The sprite helpers above are untouched; the HUD and FTUE still use them.
        // ------------------------------------------------------------------

        public static GameObject CreateGlassmorphicPanel(Transform parent, Vector2 size,
                                                         Color neonBorderColor,
                                                         Vector2 anchorMin, Vector2 anchorMax,
                                                         Vector2 anchoredPosition)
        {
            GameObject panel = TubityXUIFactory.CreatePanel(parent, size, neonBorderColor,
                                                            anchorMin, anchorMax, anchoredPosition);
            panel.name = "GlassPanel";
            return panel;
        }

        public static GameObject CreateGlassmorphicPanel(Transform parent, Vector2 size,
                                                         Color neonBorderColor,
                                                         Vector2 anchoredPosition)
        {
            return CreateGlassmorphicPanel(parent, size, neonBorderColor,
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           anchoredPosition);
        }

        public static GameObject CreateGlassmorphicIconButton(Transform parent, Vector2 size,
                                                              Color neonBorderColor,
                                                              string labelText, Color textColor,
                                                              int fontSize = 56,
                                                              bool enablePulse = false)
        {
            // Old call sites pass Unity Text point sizes and pre-spaced strings
            // like "C O N F I R M"; the display face brings its own tracking.
            string label = labelText;
            if (!string.IsNullOrEmpty(label) && LooksLetterSpaced(label))
                label = label.Replace(" ", "");

            float cap = Mathf.Clamp(fontSize * 0.68f, 12f, 40f);
            float radius = Mathf.Min(16f, Mathf.Min(size.x, size.y) * 0.28f);

            GameObject btn = TubityXUIFactory.CreateButton(parent, size, label, neonBorderColor,
                                                           cap, cap * 0.14f, radius, enablePulse);
            btn.name = "GlassIconButton";
            return btn;
        }

        /// <summary>"C O N F I R M" - a space between every letter.</summary>
        private static bool LooksLetterSpaced(string s)
        {
            int spaces = 0, letters = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ') spaces++;
                else letters++;
            }
            return letters > 2 && spaces >= letters - 1;
        }
    }
}
