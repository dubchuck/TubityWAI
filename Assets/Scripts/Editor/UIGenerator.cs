using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using TubityWAI; // To get the colors

namespace TubityWAI.Editor
{
    public class UIGenerator
    {
        [MenuItem("Tubity/Generate UI Assets and Prefabs")]
        public static void GenerateUIAssetsAndPrefabs()
        {
            string dir = "Assets/Art/UI";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Generate Sprites
            Sprite roundedRect = GenerateAndSaveSprite(dir, "RoundedRect", 128, 128, 12, new Vector4(12, 12, 12, 12));
            Sprite circle = GenerateAndSaveSprite(dir, "Circle", 32, 32, 16, Vector4.zero);
            Sprite ring = GenerateAndSaveRingSprite(dir, "Ring", 128, 50f, 62f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UIGenerator] Sprites generated successfully.");

            // Now Generate Prefabs
            string prefabDir = "Assets/Prefabs/UI";
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
            }

            // We need standard colors from MainMenu for default prefab appearance
            Color neonBorderColor = new Color(0f, 1f, 1f, 0.4f);
            Color textGoldColor = new Color(0.8f, 0.9f, 1f);

            GameObject panelObj = CreateGlassmorphicPanelPrefab(roundedRect, neonBorderColor);
            PrefabUtility.SaveAsPrefabAsset(panelObj, $"{prefabDir}/GlassPanel.prefab");
            Object.DestroyImmediate(panelObj);

            GameObject buttonObj = CreateGlassmorphicIconButtonPrefab(roundedRect, neonBorderColor, textGoldColor);
            PrefabUtility.SaveAsPrefabAsset(buttonObj, $"{prefabDir}/GlassIconButton.prefab");
            Object.DestroyImmediate(buttonObj);

            Debug.Log("[UIGenerator] Prefabs generated successfully.");
        }

        private static GameObject CreateGlassmorphicPanelPrefab(Sprite bgSprite, Color neonBorderColor)
        {
            GameObject panelObj = new GameObject("GlassPanel");
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500, 500);

            // Layer 1: Base Glass Fill
            Image bgImg = panelObj.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.02f, 0.05f, 0.12f, 0.70f);

            // Layer 2: Subtle Accent Line
            Outline rim = panelObj.AddComponent<Outline>();
            rim.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.4f);
            rim.effectDistance = new Vector2(1f, -1f);

            // Layer 3: Minimal Ambient Glow
            Shadow glowShadow = panelObj.AddComponent<Shadow>();
            glowShadow.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.2f);
            glowShadow.effectDistance = new Vector2(-1f, 1f);

            // Layer 4: Faint Specular Highlight
            GameObject highlight = new GameObject("GlassHighlight");
            highlight.transform.SetParent(panelObj.transform, false);
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.01f, 0.55f);
            hlRect.anchorMax = new Vector2(0.99f, 0.98f);
            hlRect.sizeDelta = Vector2.zero;

            Image hlImg = highlight.AddComponent<Image>();
            hlImg.sprite = bgSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.05f);
            hlImg.raycastTarget = false;

            return panelObj;
        }

        private static GameObject CreateGlassmorphicIconButtonPrefab(Sprite bgSprite, Color neonBorderColor, Color textColor)
        {
            GameObject btnObj = new GameObject("GlassIconButton");
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 70);

            // Layer 1: Base Glass Fill
            Image bgImg = btnObj.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // Layer 2: Subtle Accent Line
            Outline rim = btnObj.AddComponent<Outline>();
            rim.effectColor = new Color(neonBorderColor.r, neonBorderColor.g, neonBorderColor.b, 0.4f);
            rim.effectDistance = new Vector2(1f, -1f);

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
            hlImg.sprite = bgSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.05f);
            hlImg.raycastTarget = false;

            // Layer 5: Icon / Text Content
            GameObject iconObj = new GameObject("IconText");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;

            Text iconText = iconObj.AddComponent<Text>();
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (iconText.font == null) iconText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            iconText.fontSize = 24;
            iconText.fontStyle = FontStyle.Normal;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = textColor;
            iconText.text = "BUTTON";

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.1f);
            cb.pressedColor = new Color(1f, 1f, 1f, 0.2f);
            cb.selectedColor = new Color(1f, 1f, 1f, 0f);
            cb.fadeDuration = 0.1f;
            btn.colors = cb;
            
            GlassUIButtonFX fx = btnObj.AddComponent<GlassUIButtonFX>();
            fx.neonRim = rim;
            fx.ambientGlow = glowShadow;
            fx.enablePulse = false;

            return btnObj;
        }

        private static Sprite GenerateAndSaveSprite(string dir, string name, int width, int height, float cornerRadius, Vector4 border)
        {
            string path = $"{dir}/{name}.png";
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            float r = cornerRadius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (cornerRadius > 0 && width == height && cornerRadius >= width / 2f)
                    {
                        // Circle
                        float dx = x - r + 0.5f;
                        float dy = y - r + 0.5f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(r - dist);
                        pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        // Rounded Rect
                        float cx = (x < r) ? r - x : (x > width - 1 - r) ? x - (width - 1 - r) : 0f;
                        float cy = (y < r) ? r - y : (y > height - 1 - r) ? y - (height - 1 - r) : 0f;
                        float dist = Mathf.Sqrt(cx * cx + cy * cy);
                        float alpha = Mathf.Clamp01(r - dist + 0.5f);
                        pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteBorder = border;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GenerateAndSaveRingSprite(string dir, string name, int size, float innerR, float outerR)
        {
            string path = $"{dir}/{name}.png";
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
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
