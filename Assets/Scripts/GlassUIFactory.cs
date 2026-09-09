using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    public static class GlassUIFactory
    {
        private static Sprite cachedCircleSprite;
        private static Sprite cachedRoundedRectSprite;
        private static Sprite cachedRingSprite;

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

        public static GameObject CreateGlassmorphicPanel(Transform parent, Vector2 size, Color neonBorderColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GlassPanel.prefab");
            if (prefab != null)
            {
                GameObject inst = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                RectTransform rt = inst.GetComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                rt.anchoredPosition = anchoredPosition;

                Outline r = inst.GetComponent<Outline>();
                if (r != null) { r.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.4f); }
                Shadow s = inst.GetComponent<Shadow>();
                if (s != null) { s.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.2f); }

                return inst;
            }
#endif

            GameObject panelObj = new GameObject("GlassPanel");
            panelObj.transform.SetParent(parent, false);
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            // Layer 1: Base Glass Fill
            Image bgImg = panelObj.AddComponent<Image>();
            bgImg.sprite = GetRoundedRectSprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.02f, 0.05f, 0.12f, 0.70f); // Darker, cleaner glass

            // Layer 2: Subtle Accent Line
            Outline rim = panelObj.AddComponent<Outline>();
            rim.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.4f);
            rim.effectDistance = new Vector2(1f, -1f); // Thin accent line

            // Layer 3: Minimal Ambient Glow
            Shadow glowShadow = panelObj.AddComponent<Shadow>();
            glowShadow.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.2f);
            glowShadow.effectDistance = new Vector2(-1f, 1f);

            // Layer 4: Very faint Specular Highlight
            GameObject highlight = new GameObject("GlassHighlight");
            highlight.transform.SetParent(panelObj.transform, false);
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.01f, 0.55f);
            hlRect.anchorMax = new Vector2(0.99f, 0.98f);
            hlRect.sizeDelta = Vector2.zero;

            Image hlImg = highlight.AddComponent<Image>();
            hlImg.sprite = GetRoundedRectSprite();
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.05f); // Extremely faint
            hlImg.raycastTarget = false;

            return panelObj;
        }

        public static GameObject CreateGlassmorphicPanel(Transform parent, Vector2 size, Color neonBorderColor, Vector2 anchoredPosition)
        {
            return CreateGlassmorphicPanel(parent, size, neonBorderColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        }

        public static GameObject CreateGlassmorphicIconButton(Transform parent, Vector2 size, Color neonBorderColor, string labelText, Color textColor, int fontSize = 56, bool enablePulse = false)
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GlassIconButton.prefab");
            if (prefab != null)
            {
                GameObject inst = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                RectTransform rt = inst.GetComponent<RectTransform>();
                rt.sizeDelta = size;

                Outline r = inst.GetComponent<Outline>();
                if (r != null) { r.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.4f); }
                Shadow s = inst.GetComponent<Shadow>();
                if (s != null) { s.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.2f); }

                GlassUIButtonFX fxComp = inst.GetComponent<GlassUIButtonFX>();
                if (fxComp != null) fxComp.enablePulse = enablePulse;

                Transform iconTextTransform = inst.transform.Find("IconText");
                if (iconTextTransform != null)
                {
                    Text t = iconTextTransform.GetComponent<Text>();
                    if (t != null)
                    {
                        t.text = labelText;
                        t.color = textColor;
                        t.fontSize = fontSize;
                    }
                }
                return inst;
            }
#endif

            GameObject btnObj = new GameObject("GlassIconButton");
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            // Layer 1: Base Glass Fill
            Image bgImg = btnObj.AddComponent<Image>();
            bgImg.sprite = GetRoundedRectSprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // Layer 2: Subtle Accent Line
            Outline rim = btnObj.AddComponent<Outline>();
            rim.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.4f);
            rim.effectDistance = new Vector2(1f, -1f); // Thin accent line

            // Layer 3: Minimal Ambient Glow
            Shadow glowShadow = btnObj.AddComponent<Shadow>();
            glowShadow.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.2f);
            glowShadow.effectDistance = new Vector2(-1f, 1f);

            // Layer 4: Faint Specular Highlight
            GameObject highlight = new GameObject("GlassHighlight");
            highlight.transform.SetParent(btnObj.transform, false);
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.02f, 0.50f);
            hlRect.anchorMax = new Vector2(0.98f, 0.96f);
            hlRect.sizeDelta = Vector2.zero;

            Image hlImg = highlight.AddComponent<Image>();
            hlImg.sprite = GetRoundedRectSprite();
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.05f);
            hlImg.raycastTarget = false;

            // Layer 5: Icon / Text Content
            if (!string.IsNullOrEmpty(labelText))
            {
                GameObject iconObj = new GameObject("IconText");
                iconObj.transform.SetParent(btnObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = Vector2.zero;

                Text iconText = iconObj.AddComponent<Text>();
                // Try to load a clean sans-serif font for the cyber minimalist look
                Font customFont = Font.CreateDynamicFontFromOSFont(new string[] { "Helvetica Neue", "Helvetica", "Roboto", "Arial" }, fontSize);
                if (customFont != null)
                {
                    iconText.font = customFont;
                }
                else
                {
                    Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    iconText.font = defaultFont;
                }
                
                iconText.fontSize = fontSize;
                iconText.fontStyle = FontStyle.Normal; // Minimalist normal font
                iconText.alignment = TextAnchor.MiddleCenter;
                iconText.color = textColor;
                iconText.text = labelText;
                
                // Note: Purposely omitting text Shadow to keep the font thin and crisp, 
                // which is a staple of the cyber minimalist style.
            }

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0f); // Completely clear background
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.1f); // Subtle glass highlight on hover
            cb.pressedColor = new Color(1f, 1f, 1f, 0.2f);
            cb.selectedColor = new Color(1f, 1f, 1f, 0f);
            cb.fadeDuration = 0.1f;
            btn.colors = cb;
            
            // Add custom FX script
            GlassUIButtonFX fx = btnObj.AddComponent<GlassUIButtonFX>();
            fx.neonRim = rim;
            fx.ambientGlow = glowShadow;
            fx.enablePulse = enablePulse;

            return btnObj;
        }
    }
}
