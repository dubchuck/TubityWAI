using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TubityWAI.Progression;

namespace TubityWAI
{
    /// <summary>
    /// The two menus for Progression Test 1: a world map into 128 levels, and the endless mode
    /// picker. Kept out of MainMenu so that file stays the size it is - this one owns its own
    /// layers on the shared canvas and talks back through callbacks.
    /// </summary>
    public class ProgressionMenu : MonoBehaviour
    {
        // ---- Wiring supplied by MainMenu -----------------------------------------------------
        public System.Action<LevelConfig> onLaunch;
        public System.Action onExit;
        public System.Action onForwardSound;
        public System.Action onBackSound;
        public System.Action onSelectSound;

        private Color rimColor;
        private Color accentColor;
        private Color textColor;

        private GameObject worldLayer, levelLayer, endlessLayer;
        private TubityXLabel worldTitle, levelTitle, endlessTitle;

        private readonly List<GameObject> worldCards = new List<GameObject>();
        private readonly List<GameObject> levelCards = new List<GameObject>();
        private readonly List<GameObject> endlessCards = new List<GameObject>();

        private int selectedWorld = 1;

        private const int WorldColumns = 4;
        private static readonly Vector2 LevelCardSize = new Vector2(264f, 200f);
        private const int LevelColumns = 4;

        public bool AnyVisible
        {
            get
            {
                return (worldLayer != null && worldLayer.activeSelf)
                    || (levelLayer != null && levelLayer.activeSelf)
                    || (endlessLayer != null && endlessLayer.activeSelf);
            }
        }

        public static ProgressionMenu Create(Transform canvas, Color rim, Color accent, Color text)
        {
            GameObject host = new GameObject("ProgressionMenu", typeof(RectTransform));
            host.transform.SetParent(canvas, false);
            RectTransform hostRect = host.GetComponent<RectTransform>();
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.sizeDelta = Vector2.zero;

            ProgressionMenu menu = host.AddComponent<ProgressionMenu>();
            menu.rimColor = rim;
            menu.accentColor = accent;
            menu.textColor = text;
            menu.Build();
            return menu;
        }

        // =========================================================================================
        // Construction
        // =========================================================================================

        private void Build()
        {
            BuildWorldLayer();
            BuildLevelLayer();
            BuildEndlessLayer();
            HideAll();
        }

        private GameObject NewLayer(string name)
        {
            GameObject layer = new GameObject(name, typeof(RectTransform));
            layer.transform.SetParent(transform, false);
            RectTransform rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return layer;
        }

        private GameObject NewPanel(GameObject layer, out TubityXLabel title, string titleText)
        {
            GameObject panel = GlassUIFactory.CreateGlassmorphicPanel(
                layer.transform, new Vector2(1400f, 780f), rimColor, new Vector2(0f, 20f));
            panel.name = "Panel";

            GameObject titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.88f);
            titleRect.anchorMax = new Vector2(1f, 0.97f);
            titleRect.sizeDelta = Vector2.zero;
            title = TubityXUIFactory.AddLabel(titleObj, titleText, 19f, textColor, textColor);

            return panel;
        }

        private void AddBackButton(GameObject panel, System.Action onClick)
        {
            GameObject back = GlassUIFactory.CreateGlassmorphicIconButton(
                panel.transform, new Vector2(140f, 60f), accentColor, "\u25C0", Color.white, 32);
            back.name = "BackButton";
            RectTransform rect = back.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.91f);
            rect.anchorMax = new Vector2(0.05f, 0.91f);
            rect.pivot = new Vector2(0f, 1f);
            back.GetComponent<Button>().onClick.AddListener(() => { Sound(onBackSound); onClick(); });
        }

        /// <summary>A grid cell with a bold line, a small line and an optional lock badge.</summary>
        private GameObject MakeCard(Transform parent, Vector2 size, Color border, string name)
        {
            GameObject card = GlassUIFactory.CreateGlassmorphicIconButton(parent, size, border, "", Color.clear);
            card.name = name;

            GameObject top = new GameObject("CardTitle", typeof(RectTransform));
            top.transform.SetParent(card.transform, false);
            RectTransform topRect = top.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 0.44f);
            topRect.anchorMax = new Vector2(1f, 0.92f);
            topRect.sizeDelta = Vector2.zero;
            TubityXUIFactory.AddLabel(top, "", 20f, textColor, textColor);

            GameObject bottom = new GameObject("CardSub", typeof(RectTransform));
            bottom.transform.SetParent(card.transform, false);
            RectTransform bottomRect = bottom.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0.08f);
            bottomRect.anchorMax = new Vector2(1f, 0.44f);
            bottomRect.sizeDelta = Vector2.zero;
            TubityXUIFactory.AddLabel(bottom, "", 9.5f, rimColor, rimColor);

            GameObject lockIcon = new GameObject("LockIcon", typeof(RectTransform));
            lockIcon.transform.SetParent(card.transform, false);
            RectTransform lockRect = lockIcon.GetComponent<RectTransform>();
            lockRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.pivot = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(42f, 42f);
            Image lockImg = lockIcon.AddComponent<Image>();
            lockImg.sprite = GlassUIFactory.GetLockSprite(false);
            lockImg.color = TubityXUIFactory.Blue;
            lockImg.raycastTarget = false;
            lockIcon.SetActive(false);

            return card;
        }

        /// <summary>
        /// A level card: the level's opening screenshot (LevelThumbnails) in a well across the top,
        /// with the number and stars, then the level's hook, underneath it.
        /// </summary>
        private GameObject MakeLevelCard(Transform parent, Vector2 size, Color border, string name)
        {
            GameObject card = MakeCard(parent, size, border, name);

            GameObject thumb = new GameObject("Thumb", typeof(RectTransform));
            thumb.transform.SetParent(card.transform, false);
            // Above the card's glass, below the lock badge.
            thumb.transform.SetSiblingIndex(LockOf(card).transform.GetSiblingIndex());
            RectTransform thumbRect = thumb.GetComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(0f, 0.40f);
            thumbRect.anchorMax = new Vector2(1f, 1f);
            thumbRect.offsetMin = new Vector2(12f, 0f);
            thumbRect.offsetMax = new Vector2(-12f, -12f);
            RawImage img = thumb.AddComponent<RawImage>();
            img.raycastTarget = false;

            RectTransform titleRect = card.transform.Find("CardTitle").GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.19f);
            titleRect.anchorMax = new Vector2(1f, 0.39f);
            TitleOf(card).CapHeight = 16f;

            RectTransform subRect = card.transform.Find("CardSub").GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.04f);
            subRect.anchorMax = new Vector2(1f, 0.20f);

            // The lock sits on the picture.
            RectTransform lockRect = LockOf(card).GetComponent<RectTransform>();
            lockRect.anchorMin = new Vector2(0.5f, 0.70f);
            lockRect.anchorMax = new Vector2(0.5f, 0.70f);

            return card;
        }

        private static RawImage ThumbOf(GameObject card)
        {
            Transform t = card.transform.Find("Thumb");
            return t != null ? t.GetComponent<RawImage>() : null;
        }

        private static TubityXLabel TitleOf(GameObject card) { return card.transform.Find("CardTitle").GetComponentInChildren<TubityXLabel>(); }
        private static TubityXLabel SubOf(GameObject card) { return card.transform.Find("CardSub").GetComponentInChildren<TubityXLabel>(); }
        private static GameObject LockOf(GameObject card) { return card.transform.Find("LockIcon").gameObject; }

        // ---- Layer 5a: worlds --------------------------------------------------------------

        private void BuildWorldLayer()
        {
            worldLayer = NewLayer("Layer5_ProgressionWorlds");
            GameObject panel = NewPanel(worldLayer, out worldTitle, "PROGRESSION TEST 1");

            GameObject grid = new GameObject("WorldGrid", typeof(RectTransform));
            grid.transform.SetParent(panel.transform, false);
            RectTransform gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.08f, 0.08f);
            gridRect.anchorMax = new Vector2(0.92f, 0.84f);
            gridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(270f, 108f);
            layout.spacing = new Vector2(30f, 24f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = WorldColumns;

            for (int i = 0; i < ProgressionV2.WorldCount; i++)
            {
                Color border = (i % 2 == 0) ? rimColor : accentColor;
                GameObject card = MakeCard(grid.transform, new Vector2(270f, 108f), border, "WorldCard_" + i);
                worldCards.Add(card);
            }

            AddBackButton(panel, () => { HideAll(); if (onExit != null) onExit(); });
        }

        // ---- Layer 5b: levels inside a world -----------------------------------------------

        private void BuildLevelLayer()
        {
            levelLayer = NewLayer("Layer5_ProgressionLevels");
            GameObject panel = NewPanel(levelLayer, out levelTitle, "WORLD");

            GameObject grid = new GameObject("LevelGrid", typeof(RectTransform));
            grid.transform.SetParent(panel.transform, false);
            RectTransform gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.08f, 0.10f);
            gridRect.anchorMax = new Vector2(0.92f, 0.84f);
            gridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = LevelCardSize;
            layout.spacing = new Vector2(36f, 32f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = LevelColumns;

            for (int i = 0; i < ProgressionV2.LevelsPerWorld; i++)
            {
                Color border = (i % 2 == 0) ? rimColor : accentColor;
                GameObject card = MakeLevelCard(grid.transform, LevelCardSize, border, "LevelCard_" + i);
                levelCards.Add(card);
            }

            AddBackButton(panel, ShowWorlds);
        }

        // ---- Layer 6: endless ---------------------------------------------------------------

        private void BuildEndlessLayer()
        {
            endlessLayer = NewLayer("Layer6_Endless");
            GameObject panel = NewPanel(endlessLayer, out endlessTitle, "ENDLESS LEVELS");

            GameObject grid = new GameObject("EndlessGrid", typeof(RectTransform));
            grid.transform.SetParent(panel.transform, false);
            RectTransform gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.08f, 0.08f);
            gridRect.anchorMax = new Vector2(0.92f, 0.84f);
            gridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(360f, 140f);
            layout.spacing = new Vector2(36f, 30f);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            List<EndlessMode> all = EndlessMode.All;
            for (int i = 0; i < all.Count; i++)
            {
                Color border = (i % 2 == 0) ? rimColor : accentColor;
                GameObject card = MakeCard(grid.transform, new Vector2(360f, 140f), border, "EndlessCard_" + i);

                // The blurb gets its own, wider line under the title.
                TubityXLabel sub = SubOf(card);
                if (sub != null) sub.color = rimColor;

                EndlessMode mode = all[i];
                card.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Sound(onForwardSound);
                    if (onLaunch != null) onLaunch(mode.CreateConfig());
                });
                endlessCards.Add(card);
            }

            AddBackButton(panel, () => { HideAll(); if (onExit != null) onExit(); });
        }

        // =========================================================================================
        // Presentation
        // =========================================================================================

        public void HideAll()
        {
            if (worldLayer != null) worldLayer.SetActive(false);
            if (levelLayer != null) levelLayer.SetActive(false);
            if (endlessLayer != null) endlessLayer.SetActive(false);
        }

        public void ShowWorlds()
        {
            HideAll();
            worldLayer.SetActive(true);

            int stars = ProgressionSave.TotalStars();
            int maxStars = ProgressionV2.LevelCount * 3;
            worldTitle.Text = $"PROGRESSION TEST 1   |   {stars} / {maxStars} STARS";

            for (int i = 0; i < worldCards.Count; i++)
            {
                World world = ProgressionV2.Worlds[i];
                GameObject card = worldCards[i];
                bool unlocked = ProgressionSave.IsWorldUnlocked(world.index);
                int worldStars = ProgressionSave.StarsInWorld(world.index);
                int beaten = ProgressionSave.LevelsBeatenInWorld(world.index);

                TubityXLabel title = TitleOf(card);
                TubityXLabel sub = SubOf(card);
                Color locked = new Color(0.45f, 0.45f, 0.55f, 0.85f);

                if (title != null)
                {
                    title.Text = $"{world.index}. {world.name}";
                    title.color = unlocked ? textColor : locked;
                }
                if (sub != null)
                {
                    sub.Text = unlocked
                        ? $"{world.FirstLevel}-{world.LastLevel}   {beaten}/{ProgressionV2.LevelsPerWorld} CLEAR   {worldStars}*"
                        : "LOCKED";
                    sub.color = unlocked ? rimColor : locked;
                }

                LockOf(card).SetActive(!unlocked);

                Button button = card.GetComponent<Button>();
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                int index = world.index;
                button.onClick.AddListener(() => { Sound(onSelectSound); ShowLevels(index); });
            }
        }

        public void ShowLevels(int worldIndex)
        {
            selectedWorld = Mathf.Clamp(worldIndex, 1, ProgressionV2.WorldCount);
            World world = ProgressionV2.Worlds[selectedWorld - 1];

            HideAll();
            levelLayer.SetActive(true);
            levelTitle.Text = $"WORLD {world.index}  -  {world.name}   |   {world.environment}";

            for (int i = 0; i < levelCards.Count; i++)
            {
                int levelNumber = world.FirstLevel + i;
                GameObject card = levelCards[i];

                bool unlocked = ProgressionSave.IsUnlocked(levelNumber);
                int stars = ProgressionSave.GetStars(levelNumber);
                ProgressionDials dials = ProgressionV2.CreateDials(levelNumber);

                TubityXLabel title = TitleOf(card);
                TubityXLabel sub = SubOf(card);
                Color locked = new Color(0.45f, 0.45f, 0.55f, 0.85f);

                if (title != null)
                {
                    title.Text = stars > 0 ? $"{levelNumber} {new string('*', stars)}" : levelNumber.ToString();
                    title.color = unlocked ? textColor : locked;
                }
                if (sub != null)
                {
                    sub.Text = unlocked ? dials.levelName : "LOCKED";
                    sub.color = unlocked ? rimColor : locked;
                }

                LockOf(card).SetActive(!unlocked);

                // The level's opening, dimmed while it is locked. Until thumbnails have been
                // captured the well is just a dark frame.
                RawImage thumb = ThumbOf(card);
                if (thumb != null)
                {
                    Texture2D shot = LevelThumbnails.Get(levelNumber);
                    thumb.texture = shot;
                    thumb.color = shot == null ? new Color(0.05f, 0.06f, 0.11f, 0.9f)
                                : unlocked ? Color.white
                                : new Color(0.30f, 0.30f, 0.36f, 1f);
                }

                Button button = card.GetComponent<Button>();
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                int captured = levelNumber;
                button.onClick.AddListener(() =>
                {
                    Sound(onForwardSound);
                    if (onLaunch != null) onLaunch(ProgressionV2.CreateLevel(captured));
                });
            }
        }

        public void ShowEndless()
        {
            HideAll();
            endlessLayer.SetActive(true);
            endlessTitle.Text = "ENDLESS LEVELS";

            List<EndlessMode> all = EndlessMode.All;
            for (int i = 0; i < endlessCards.Count && i < all.Count; i++)
            {
                EndlessMode mode = all[i];
                GameObject card = endlessCards[i];

                TubityXLabel title = TitleOf(card);
                TubityXLabel sub = SubOf(card);

                int best = ProgressionSave.GetEndlessBest(mode.id);
                float dist = ProgressionSave.GetEndlessBestDistance(mode.id);

                if (title != null) title.Text = mode.displayName;
                if (sub != null)
                {
                    sub.Text = best > 0
                        ? $"BEST {best}   -   {Mathf.RoundToInt(dist)} UNITS"
                        : mode.blurb.ToUpper();
                }
            }
        }

        private void Sound(System.Action s) { if (s != null) s(); }
    }
}
