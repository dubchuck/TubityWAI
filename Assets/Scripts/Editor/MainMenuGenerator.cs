using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TubityWAI.Editor
{
    public class MainMenuGenerator
    {
        [MenuItem("Tubity/Generate MainMenu UI")]
        public static void GenerateMainMenuUI()
        {
            MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>();
            if (mainMenu == null)
            {
                Debug.LogError("[MainMenuGenerator] MainMenu component not found in the scene.");
                return;
            }

            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GlassPanel.prefab");
            GameObject btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GlassIconButton.prefab");

            if (panelPrefab == null || btnPrefab == null)
            {
                Debug.LogError("[MainMenuGenerator] Missing prefabs. Please run 'Tubity/Generate UI Assets and Prefabs' first.");
                return;
            }

            // Cleanup old canvas
            Transform oldCanvas = mainMenu.transform.Find("MainMenuCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

            // Create Canvas
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            canvasObj.transform.SetParent(mainMenu.transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Global Title Logo
            GameObject logoObj = new GameObject("MenuLogo");
            logoObj.transform.SetParent(canvasObj.transform, false);
            RectTransform logoRect = logoObj.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.86f);
            logoRect.anchorMax = new Vector2(0.5f, 0.86f);
            logoRect.sizeDelta = new Vector2(750f, 180f);
            logoRect.anchoredPosition = Vector2.zero;

            Image logoImg = logoObj.AddComponent<Image>();
            Sprite logoSprite = Resources.Load<Sprite>("tubityx_title");
            
            // Try to load or create Neon Sheen Material
            Material neonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UI/Mat_NeonLogoSheen.mat");
            if (neonMat == null)
            {
                Shader shader = Shader.Find("UI/NeonGradientSheen");
                if (shader != null)
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
                    if (!AssetDatabase.IsValidFolder("Assets/Materials/UI")) AssetDatabase.CreateFolder("Assets/Materials", "UI");
                    
                    neonMat = new Material(shader);
                    neonMat.SetColor("_LeftColor", new Color(0f, 1f, 1f, 1f));
                    neonMat.SetColor("_RightColor", new Color(1f, 0f, 0.8f, 1f));
                    AssetDatabase.CreateAsset(neonMat, "Assets/Materials/UI/Mat_NeonLogoSheen.mat");
                    AssetDatabase.SaveAssets();
                }
            }

            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
                logoImg.color = Color.white;
                logoImg.preserveAspect = true;
                if (neonMat != null) logoImg.material = neonMat;
            }
            else
            {
                Text logoText = logoObj.AddComponent<Text>();
                logoText.font = defaultFont;
                logoText.fontSize = 64;
                logoText.alignment = TextAnchor.MiddleCenter;
                logoText.color = mainMenu.textGoldColor;
                logoText.text = "TUBITYX";
                if (neonMat != null) logoText.material = neonMat;
                
                Shadow logoShadow = logoObj.AddComponent<Shadow>();
                logoShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
                logoShadow.effectDistance = new Vector2(1f, -1f);
                Outline logoGlow = logoObj.AddComponent<Outline>();
                logoGlow.effectColor = mainMenu.borderNeonColor;
                logoGlow.effectDistance = new Vector2(1f, -1f);
            }

            // ================= LAYER 1 =================
            GameObject layer1Obj = new GameObject("Layer1_TopMenu");
            layer1Obj.transform.SetParent(canvasObj.transform, false);
            RectTransform l1Rect = layer1Obj.AddComponent<RectTransform>();
            l1Rect.anchorMin = Vector2.zero; l1Rect.anchorMax = Vector2.one; l1Rect.sizeDelta = Vector2.zero;

            GameObject playBtnObj = InstantiateButton(btnPrefab, layer1Obj.transform, "NavBtn_Play", "P L A Y", mainMenu.borderNeonColor, mainMenu.borderNeonColor, new Vector2(240f, 70f));
            RectTransform playRect = playBtnObj.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.05f, 0.08f); playRect.anchorMax = new Vector2(0.05f, 0.08f); playRect.pivot = new Vector2(0f, 0f); playRect.anchoredPosition = Vector2.zero;
            GlassUIButtonFX playFX = playBtnObj.GetComponent<GlassUIButtonFX>(); if (playFX != null) playFX.enablePulse = true;

            GameObject settingsBtnObj = InstantiateButton(btnPrefab, layer1Obj.transform, "NavBtn_Settings", "\u2699  S E T T I N G S", mainMenu.borderNeonColor, mainMenu.borderNeonColor, new Vector2(260f, 70f));
            RectTransform settingsRect = settingsBtnObj.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.95f, 0.08f); settingsRect.anchorMax = new Vector2(0.95f, 0.08f); settingsRect.pivot = new Vector2(1f, 0f); settingsRect.anchoredPosition = Vector2.zero;

            // Apply fields
            SerializedObject so = new SerializedObject(mainMenu);
            so.FindProperty("canvasObj").objectReferenceValue = canvasObj;
            so.FindProperty("logoObj").objectReferenceValue = logoObj;
            so.FindProperty("layer1Obj").objectReferenceValue = layer1Obj;
            so.FindProperty("playBtn").objectReferenceValue = playBtnObj.GetComponent<Button>();
            so.FindProperty("settingsBtn").objectReferenceValue = settingsBtnObj.GetComponent<Button>();
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mainMenu);

            Debug.Log("[MainMenuGenerator] MainMenu UI partially generated and bound. (Test Script)");
        }

        private static GameObject InstantiateButton(GameObject prefab, Transform parent, string name, string text, Color rimColor, Color textColor, Vector2 size)
        {
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            obj.name = name;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Outline rim = obj.GetComponent<Outline>();
            if (rim != null)
            {
                rim.effectColor = new Color(rimColor.r, rimColor.g, rimColor.b, 0.4f);
            }

            Shadow shadow = obj.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = new Color(rimColor.r, rimColor.g, rimColor.b, 0.2f);
            }

            Transform textObj = obj.transform.Find("IconText");
            if (textObj != null)
            {
                Text t = textObj.GetComponent<Text>();
                if (t != null)
                {
                    t.text = text;
                    t.color = textColor;
                }
            }

            return obj;
        }
    }
}
