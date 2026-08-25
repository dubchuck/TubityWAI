using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace TubityWAI
{
    /// <summary>
    /// tvOS & Gamepad UI Navigation system providing a 5-layer Glassmorphic reticle indicator,
    /// D-Pad navigation, and Center/Select button execution across all UI menus.
    /// </summary>
    public class TVOSMenuNavigator : MonoBehaviour
    {
        public static TVOSMenuNavigator Instance { get; private set; }

        [Header("Reticle Style Tokens")]
        public Color reticleNeonColor = new Color(0f, 1f, 1f, 0.95f); // Neon Cyan
        public Color reticleGlowColor = new Color(1f, 0.85f, 0f, 0.8f); // Synthwave Gold

        private GameObject reticleObj;
        private RectTransform reticleRect;
        private Outline reticleRim;
        private Shadow reticleGlow;
        private Sprite roundedRectSprite;

        private GameObject currentlyFocusedObj;
        private List<Selectable> activeSelectables = new List<Selectable>();

        private float inputCooldown = 0f;
        private const float COOLDOWN_DURATION = 0.18f;
        private float pulseTimer = 0f;

        private void Awake()
        {
#if UNITY_EDITOR
            Destroy(gameObject);
            return;
#endif
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            roundedRectSprite = CreateRoundedRectSprite(128, 128, 24);
        }

        private void Start()
        {
            EnsureReticleObject();
        }

        private void EnsureReticleObject()
        {
            if (reticleObj != null) return;

            Canvas rootCanvas = FindFirstObjectByType<Canvas>();
            if (rootCanvas == null) return;

            // Create 5-layer Reticle Frame Overlay
            reticleObj = new GameObject("TVOS_Reticle");
            reticleObj.transform.SetParent(rootCanvas.transform, false);
            reticleObj.transform.SetAsLastSibling();

            reticleRect = reticleObj.AddComponent<RectTransform>();
            reticleRect.pivot = new Vector2(0.5f, 0.5f);

            // Layer 1: Reticle Translucent Background
            Image bgImg = reticleObj.AddComponent<Image>();
            bgImg.sprite = roundedRectSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.08f);

            // Layer 2: Neon Rim
            reticleRim = reticleObj.AddComponent<Outline>();
            reticleRim.effectColor = reticleNeonColor;
            reticleRim.effectDistance = new Vector2(3.5f, -3.5f);

            // Layer 3: Ambient Glow
            reticleGlow = reticleObj.AddComponent<Shadow>();
            reticleGlow.effectColor = new Color(reticleNeonColor.r, reticleNeonColor.g, reticleNeonColor.b, 0.65f);
            reticleGlow.effectDistance = new Vector2(-3f, 3f);

            // Layer 4: Specular Reflection Overlay
            GameObject hlObj = new GameObject("ReticleSpecular");
            hlObj.transform.SetParent(reticleObj.transform, false);
            RectTransform hlRect = hlObj.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.02f, 0.55f);
            hlRect.anchorMax = new Vector2(0.98f, 0.96f);
            hlRect.sizeDelta = Vector2.zero;

            Image hlImg = hlObj.AddComponent<Image>();
            hlImg.sprite = roundedRectSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.22f);

            reticleObj.SetActive(false);
        }

        private void Update()
        {
            EnsureReticleObject();

            RefreshSelectables();

            if (activeSelectables.Count == 0)
            {
                if (reticleObj != null && reticleObj.activeSelf)
                {
                    reticleObj.SetActive(false);
                }
                return;
            }

            // Sync currentlyFocusedObj with EventSystem's active selected object if set
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.activeInHierarchy)
            {
                currentlyFocusedObj = EventSystem.current.currentSelectedGameObject;
            }

            // Fallback: If nothing is selected, select first available active UI button
            if (currentlyFocusedObj == null || !currentlyFocusedObj.activeInHierarchy)
            {
                SelectFirstAvailable();
            }

            if (currentlyFocusedObj != null)
            {
                UpdateReticlePositionAndAnimation();
                HandleDirectionalInput();
                HandleSelectInput();
            }
        }

        private void RefreshSelectables()
        {
            activeSelectables.Clear();
            Selectable[] allSelectables = FindObjectsByType<Selectable>(FindObjectsSortMode.None);
            foreach (Selectable sel in allSelectables)
            {
                if (sel != null && sel.gameObject.activeInHierarchy && sel.interactable)
                {
                    // Ensure navigation mode is Automatic so UGUI focus system functions natively
                    Navigation nav = sel.navigation;
                    if (nav.mode == Navigation.Mode.None)
                    {
                        nav.mode = Navigation.Mode.Automatic;
                        sel.navigation = nav;
                    }
                    activeSelectables.Add(sel);
                }
            }
        }

        private void SelectFirstAvailable()
        {
            if (activeSelectables.Count > 0)
            {
                SetFocus(activeSelectables[0].gameObject);
            }
            else
            {
                currentlyFocusedObj = null;
                if (reticleObj != null) reticleObj.SetActive(false);
            }
        }

        public void SetFocus(GameObject target)
        {
            if (target == null) return;
            currentlyFocusedObj = target;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }

            if (reticleObj != null && !reticleObj.activeSelf)
            {
                reticleObj.SetActive(true);
            }
        }

        private void UpdateReticlePositionAndAnimation()
        {
            if (currentlyFocusedObj == null || reticleObj == null) return;

            RectTransform targetRect = currentlyFocusedObj.GetComponent<RectTransform>();
            if (targetRect == null) return;

            // Ensure reticle is child of the active Canvas
            Canvas parentCanvas = currentlyFocusedObj.GetComponentInParent<Canvas>();
            if (parentCanvas != null && reticleObj.transform.parent != parentCanvas.transform)
            {
                reticleObj.transform.SetParent(parentCanvas.transform, false);
            }
            reticleObj.transform.SetAsLastSibling();

            // Smooth position & size tracking
            Vector3 targetWorldPos = targetRect.position;
            Vector2 targetSize = targetRect.rect.size;
            Vector2 padding = new Vector2(16f, 16f);

            reticleRect.position = Vector3.Lerp(reticleRect.position, targetWorldPos, Time.unscaledDeltaTime * 25f);
            reticleRect.sizeDelta = Vector2.Lerp(reticleRect.sizeDelta, targetSize + padding, Time.unscaledDeltaTime * 25f);

            // Reticle pulse animation
            pulseTimer += Time.unscaledDeltaTime * 4f;
            float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
            Color currentNeon = Color.Lerp(reticleNeonColor, reticleGlowColor, pulse);

            if (reticleRim != null) reticleRim.effectColor = currentNeon;
            if (reticleGlow != null) reticleGlow.effectColor = new Color(currentNeon.r, currentNeon.g, currentNeon.b, 0.55f + pulse * 0.25f);
        }

        private void HandleDirectionalInput()
        {
            if (inputCooldown > 0f)
            {
                inputCooldown -= Time.unscaledDeltaTime;
                return;
            }

            Vector2 navInput = Vector2.zero;

            // 1. Gamepad / tvOS D-Pad & Left Stick
            if (Gamepad.current != null)
            {
                if (Gamepad.current.dpad.up.isPressed) navInput.y = 1f;
                else if (Gamepad.current.dpad.down.isPressed) navInput.y = -1f;
                else if (Gamepad.current.dpad.left.isPressed) navInput.x = -1f;
                else if (Gamepad.current.dpad.right.isPressed) navInput.x = 1f;

                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.35f) navInput.x = Mathf.Sign(stick.x);
                if (Mathf.Abs(stick.y) > 0.35f) navInput.y = Mathf.Sign(stick.y);
            }

            // 2. New Input System Keyboard inputs
            if (Keyboard.current != null && navInput == Vector2.zero)
            {
                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) navInput.y = 1f;
                else if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) navInput.y = -1f;
                else if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) navInput.x = -1f;
                else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) navInput.x = 1f;
            }

            if (navInput != Vector2.zero)
            {
                NavigateDirection(navInput);
                inputCooldown = COOLDOWN_DURATION;
            }
        }

        private void NavigateDirection(Vector2 dir)
        {
            if (currentlyFocusedObj == null || activeSelectables.Count <= 1) return;

            Vector3 currentPos = currentlyFocusedObj.transform.position;
            Selectable bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Selectable candidate in activeSelectables)
            {
                if (candidate.gameObject == currentlyFocusedObj) continue;

                Vector3 candidatePos = candidate.transform.position;
                Vector3 delta = candidatePos - currentPos;

                float dist = delta.magnitude;
                if (dist < 0.01f) continue;

                float dot = Vector3.Dot(delta.normalized, new Vector3(dir.x, dir.y, 0f).normalized);
                if (dot > 0.2f) // Directional threshold
                {
                    float score = dot / (dist + 0.001f);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = candidate;
                    }
                }
            }

            if (bestTarget != null)
            {
                SetFocus(bestTarget.gameObject);
            }
        }

        private void HandleSelectInput()
        {
            bool isSelectPressed = false;

            // 1. Gamepad / tvOS Siri Remote Select
            if (Gamepad.current != null)
            {
                isSelectPressed = Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame;
            }

            // 2. New Input System Keyboard Enter / Space
            if (Keyboard.current != null && !isSelectPressed)
            {
                isSelectPressed = Keyboard.current.enterKey.wasPressedThisFrame ||
                                  Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                  Keyboard.current.spaceKey.wasPressedThisFrame;
            }

            if (isSelectPressed && currentlyFocusedObj != null)
            {
                // Execute UI button click handler & Submit event
                Button btn = currentlyFocusedObj.GetComponent<Button>();
                if (btn != null && btn.interactable)
                {
                    btn.onClick.Invoke();
                }
                ExecuteEvents.Execute(currentlyFocusedObj, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }

        private Sprite CreateRoundedRectSprite(int width = 128, int height = 128, int cornerRadius = 24)
        {
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
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
