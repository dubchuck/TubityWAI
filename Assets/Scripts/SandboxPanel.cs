using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace TubityWAI
{
    /// <summary>
    /// Live controls for the sandbox test level (LevelConfig.sandbox). Every option is its own
    /// button, lit when selected, so nothing has to be cycled through:
    ///
    ///   ENV     one button per EnvironmentTheme; pressing one appends it to the running blend
    ///   TRACKS  OFF plus one button per straight track in Resources/Music
    ///   STEMS   one button per stem set in Resources/MusicStems (see StemLoopPlayer)
    ///   ENERGY  Soundraw-style presets, and per-stem OFF / H1 / H2 toggles, in stem mode
    ///
    /// Built by GameHUD into the bottom-left of the safe area.
    /// </summary>
    public class SandboxPanel : MonoBehaviour
    {
        private enum MusicMode { Off, Track, Stems }

        private const float EdgeMargin = 28f;
        private const float RowHeight = 46f;
        private const float HeaderHeight = 22f;
        private static readonly Color Face = Color.white;
        private static readonly Color Dim = new Color(0.72f, 0.78f, 0.95f);
        private static readonly Color Idle = TubityXUIFactory.Cyan;
        private static readonly Color Selected = TubityXUIFactory.Gold;

        private EnvironmentTheme[] themes;
        private int themeIndex;

        private MusicMode musicMode = MusicMode.Track;
        private int trackIndex;
        private int setIndex;
        private int energy = 1;

        private GameObject[] themeButtons;
        private GameObject offButton;
        private GameObject[] trackButtons;
        private GameObject[] setButtons;
        private GameObject[] energyButtons;
        private readonly GameObject[] stemButtons = new GameObject[StemLoopPlayer.MaxStems];
        private GameObject energyRow;
        private GameObject stemRow;
        private TubityXLabel statusLabel;

        public static SandboxPanel Create(RectTransform safe)
        {
            SandboxPanel panel = new GameObject("SandboxPanelLogic", typeof(RectTransform)).AddComponent<SandboxPanel>();
            panel.Build(safe);
            return panel;
        }

        private void Build(RectTransform safe)
        {
            themes = (EnvironmentTheme[])Enum.GetValues(typeof(EnvironmentTheme));
            LevelConfig cfg = GameManager.Instance != null ? GameManager.Instance.currentLevelConfig : null;
            themeIndex = Mathf.Max(0, Array.IndexOf(themes, cfg != null ? cfg.environment : EnvironmentTheme.None));

            MusicPlayer music = MusicPlayer.Instance;
            int trackCount = music != null ? music.TrackCount : 0;
            if (music != null) trackIndex = Mathf.Max(0, music.CurrentTrackIndex);

            StemLoopPlayer stems = StemLoopPlayer.GetOrCreate();
            int setCount = stems.SetCount;

            // Plan the rows first so the panel is exactly as tall as it needs to be.
            const int ThemeColumns = 5, TrackColumns = 4, SetColumns = 6;
            int themeRows = Rows(themes.Length, ThemeColumns);
            int trackRows = Rows(trackCount + 1, TrackColumns);   // +1 for OFF
            int setRows = Rows(setCount, SetColumns);
            int totalRows = themeRows + trackRows + setRows + 2;   // energy row + stem row
            float height = 4 * HeaderHeight + totalRows * RowHeight + HeaderHeight + 24f;   // headers + rows + status + padding
            Vector2 size = new Vector2(780f, height);

            GameObject panelObj = TubityXUIFactory.CreatePanel(
                safe, size, TubityXUIFactory.Purple,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(size.x * 0.5f + EdgeMargin, size.y * 0.5f + EdgeMargin), 20f);
            panelObj.name = "SandboxPanel";
            transform.SetParent(panelObj.transform, false);
            gameObject.name = "SandboxPanelLogic";

            // Lay rows out from the top in pixels; y is the running cursor.
            float y = -12f;

            AddHeader(panelObj, ref y, "ENVIRONMENT");
            string[] themeLabels = new string[themes.Length];
            for (int i = 0; i < themes.Length; i++) themeLabels[i] = ThemeName(themes[i]);
            themeButtons = AddButtonGrid(panelObj, ref y, themeLabels, ThemeColumns, SelectTheme);

            AddHeader(panelObj, ref y, "MUSIC: STRAIGHT TRACKS");
            string[] trackLabels = new string[trackCount + 1];
            trackLabels[0] = "OFF";
            for (int i = 0; i < trackCount; i++) trackLabels[i + 1] = ShortTrackName(music.TrackName(i));
            GameObject[] trackGrid = AddButtonGrid(panelObj, ref y, trackLabels, TrackColumns, SelectTrackButton);
            offButton = trackGrid[0];
            trackButtons = new GameObject[trackCount];
            Array.Copy(trackGrid, 1, trackButtons, 0, trackCount);

            AddHeader(panelObj, ref y, "MUSIC: STEM LOOPS");
            string[] setLabels = new string[setCount];
            for (int i = 0; i < setCount; i++) setLabels[i] = ShortSetName(stems.SetName(i));
            setButtons = setCount > 0
                ? AddButtonGrid(panelObj, ref y, setLabels, SetColumns, SelectSet)
                : new GameObject[0];
            if (setCount == 0) AddHeader(panelObj, ref y, "(NO STEM SETS IN RESOURCES/MUSICSTEMS)");

            AddHeader(panelObj, ref y, "STEM ENERGY / TOGGLES");
            energyRow = new GameObject("EnergyRow", typeof(RectTransform));
            energyRow.transform.SetParent(panelObj.transform, false);
            energyButtons = AddButtonGrid(panelObj, ref y, StemLoopPlayer.EnergyNames, 3, SelectEnergy, energyRow);

            stemRow = new GameObject("StemRow", typeof(RectTransform));
            stemRow.transform.SetParent(panelObj.transform, false);
            // One slot per possible stem; a set fills the first StemCount of them (see UpdateStemButtons).
            string[] stemSlots = new string[StemLoopPlayer.MaxStems];
            for (int i = 0; i < stemSlots.Length; i++) stemSlots[i] = "STEM " + (i + 1);
            GameObject[] stemGrid = AddButtonGrid(panelObj, ref y, stemSlots, StemLoopPlayer.MaxStems, ToggleStem, stemRow);
            Array.Copy(stemGrid, stemButtons, stemButtons.Length);

            statusLabel = AddHeader(panelObj, ref y, "");
            statusLabel.RimColor = Dim;

            Refresh();
        }

        private static int Rows(int count, int columns)
        {
            return Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
        }

        // ------------------------------------------------------------------
        // Actions
        // ------------------------------------------------------------------

        private void SelectTheme(int index)
        {
            themeIndex = index;
            EnvironmentTheme theme = themes[index];
            if (EnvironmentManager.Current != null && EnvironmentManager.Current.TransitionTo(theme))
            {
                // Anything that only reads the starting theme (music pools) should see the new one.
                if (GameManager.Instance != null && GameManager.Instance.currentLevelConfig != null)
                    GameManager.Instance.currentLevelConfig.environment = theme;
            }
            else
            {
                Debug.LogWarning("[SandboxPanel] No blended environment to extend; the level needs WithEnvironmentBlend.");
            }
            Refresh();
        }

        /// <summary>Index 0 is OFF; the rest are straight tracks.</summary>
        private void SelectTrackButton(int gridIndex)
        {
            MusicPlayer music = MusicPlayer.Instance;
            if (gridIndex == 0)
            {
                musicMode = MusicMode.Off;
                if (music != null) music.Suspend();
                if (StemLoopPlayer.Instance != null) StemLoopPlayer.Instance.Stop();
            }
            else
            {
                musicMode = MusicMode.Track;
                trackIndex = gridIndex - 1;
                if (StemLoopPlayer.Instance != null) StemLoopPlayer.Instance.Stop();
                if (music != null) music.PlayTrackIndex(trackIndex);
            }
            Refresh();
        }

        private void SelectSet(int index)
        {
            musicMode = MusicMode.Stems;
            setIndex = index;
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Suspend();
            StemLoopPlayer stems = StemLoopPlayer.GetOrCreate();
            stems.Play(stems.SetName(index), energy);
            Refresh();
        }

        private void SelectEnergy(int index)
        {
            energy = index;
            if (musicMode == MusicMode.Stems && StemLoopPlayer.Instance != null)
                StemLoopPlayer.Instance.ApplyEnergy(energy);
            Refresh();
        }

        private void ToggleStem(int index)
        {
            if (musicMode != MusicMode.Stems || StemLoopPlayer.Instance == null) return;
            StemLoopPlayer.Instance.CycleStrength(index);
            Refresh();
        }

        // ------------------------------------------------------------------
        // Display
        // ------------------------------------------------------------------

        private void Refresh()
        {
            Highlight(themeButtons, themeIndex);
            Highlight(trackButtons, musicMode == MusicMode.Track ? trackIndex : -1);
            SetLit(offButton, musicMode == MusicMode.Off);
            Highlight(setButtons, musicMode == MusicMode.Stems ? setIndex : -1);

            bool stemsMode = musicMode == MusicMode.Stems;
            Highlight(energyButtons, stemsMode ? energy : -1);
            UpdateStemButtons();
        }

        private void UpdateStemButtons()
        {
            StemLoopPlayer stems = StemLoopPlayer.Instance;
            bool live = musicMode == MusicMode.Stems && stems != null;
            int count = live ? stems.StemCount : 0;
            for (int i = 0; i < stemButtons.Length; i++)
            {
                bool present = i < count;
                stemButtons[i].SetActive(present);
                if (!present) continue;
                int strength = stems.GetPendingStrength(i);
                string tag = strength < 0 ? "OFF" : "H" + (strength + 1);
                SetLabel(stemButtons[i], stems.StemLabel(i) + " " + tag);
                SetLit(stemButtons[i], strength >= 0);
            }
        }

        private void Update()
        {
            if (statusLabel == null) return;

            string status = "";
            if (PlayerController.Instance != null)
                status = "Z " + Mathf.RoundToInt(PlayerController.Instance.transform.position.z);

            StemLoopPlayer stems = StemLoopPlayer.Instance;
            if (musicMode == MusicMode.Stems && stems != null && stems.IsPlaying)
            {
                status += "   " + stems.ActiveSet.ToUpperInvariant()
                        + "   NEXT BAR " + stems.SecondsToNextBar.ToString("0.0") + "S";
                // A queued change shows up as the button flipping when the bar lands.
                UpdateStemButtons();
            }
            else if (musicMode == MusicMode.Track && MusicPlayer.Instance != null)
            {
                status += "   " + MusicPlayer.Instance.TrackName(trackIndex).ToUpperInvariant();
            }
            statusLabel.Text = status;
        }

        private static string ThemeName(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.None: return "CLASSIC";
                case EnvironmentTheme.Underwater: return "WATER";
                case EnvironmentTheme.SolarSystem: return "SOLAR";
                case EnvironmentTheme.AsteroidBelt: return "BELT";
                case EnvironmentTheme.PrismHall: return "MIRRORS";
                case EnvironmentTheme.Cavern: return "CAVE";
                case EnvironmentTheme.LavaTube: return "LAVA TUBE";
                case EnvironmentTheme.SolarFlare: return "CORONA";
                case EnvironmentTheme.BlackHole: return "BLACK HOLE";
                case EnvironmentTheme.Thunderstorm: return "STORM";
                case EnvironmentTheme.SunsetCanyon: return "CANYON";
                case EnvironmentTheme.Clockwork: return "CLOCKWORK";
                case EnvironmentTheme.BlossomArbor: return "ARBOR";
                case EnvironmentTheme.CandyClouds: return "CANDY";
                default: return theme.ToString().ToUpperInvariant();
            }
        }

        /// <summary>"TechnoTranceEuphoric128" -> "TRANCE EUPHORIC 128": genre abbreviated, mood and BPM kept.</summary>
        private static string ShortSetName(string set)
        {
            if (string.IsNullOrEmpty(set)) return "?";
            Match m = Regex.Match(set, @"^(BeatsAmbient|Ambient|TechHouse|TechnoTrance|TropicalHouse)([A-Za-z]*?)(\d+)$");
            if (!m.Success) return set.Length > 13 ? set.Substring(0, 13).ToUpperInvariant() : set.ToUpperInvariant();
            string genre;
            switch (m.Groups[1].Value)
            {
                case "BeatsAmbient": genre = "BEATS"; break;
                case "Ambient": genre = "AMB"; break;
                case "TechHouse": genre = "TECH"; break;
                case "TechnoTrance": genre = "TRANCE"; break;
                default: genre = "TROP"; break;
            }
            string mood = m.Groups[2].Value.ToUpperInvariant();
            return (genre + " " + mood + " " + m.Groups[3].Value).Replace("  ", " ");
        }

        /// <summary>"129_full_glass_0156" -> "GLASS"; anything else is trimmed to fit a button.</summary>
        private static string ShortTrackName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            Match m = Regex.Match(name, @"^\d+_full_(.+?)_\d+$");
            string s = m.Success ? m.Groups[1].Value : name;
            s = s.Replace("PremiumBeat.com", "").Replace("-", " ").Replace("_", " ").Trim();
            s = s.ToUpperInvariant();
            return s.Length > 14 ? s.Substring(0, 14) : s;
        }

        // ------------------------------------------------------------------
        // Layout helpers
        // ------------------------------------------------------------------

        private static TubityXLabel AddHeader(GameObject panel, ref float y, string text)
        {
            GameObject host = new GameObject("Header", typeof(RectTransform));
            host.transform.SetParent(panel.transform, false);
            RectTransform r = host.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(24f, y - HeaderHeight);
            r.offsetMax = new Vector2(-24f, y);
            y -= HeaderHeight;
            return TubityXUIFactory.AddLabel(host, text, 11f, Dim, TubityXUIFactory.Purple, 2.5f, TubityXLabel.Align.Left);
        }

        /// <summary>
        /// Buttons in a grid across the panel width, as many rows as needed. Buttons are parented to
        /// <paramref name="group"/> when given, so a whole row can be shown or hidden together.
        /// </summary>
        private static GameObject[] AddButtonGrid(GameObject panel, ref float y, string[] labels, int columns,
                                                  Action<int> onClick, GameObject group = null)
        {
            const float sideInset = 24f, gap = 8f;
            float panelWidth = panel.GetComponent<RectTransform>().sizeDelta.x;
            float cellWidth = (panelWidth - 2f * sideInset - gap * (columns - 1)) / columns;
            Vector2 btnSize = new Vector2(cellWidth, RowHeight - gap);

            Transform parent = panel.transform;
            if (group != null)
            {
                RectTransform g = group.GetComponent<RectTransform>();
                g.anchorMin = Vector2.zero;
                g.anchorMax = Vector2.one;
                g.sizeDelta = Vector2.zero;
                g.anchoredPosition = Vector2.zero;
                parent = group.transform;
            }

            GameObject[] buttons = new GameObject[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                int col = i % columns, row = i / columns;
                GameObject btn = TubityXUIFactory.CreateButton(parent, btnSize, labels[i], Idle, 12f, 1f, 10f, false);
                btn.name = "Btn_" + labels[i];
                RectTransform r = btn.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(sideInset + col * (cellWidth + gap), y - row * RowHeight - gap * 0.5f);
                btn.GetComponent<Button>().onClick.AddListener(() => onClick(index));
                buttons[i] = btn;
            }
            y -= Rows(labels.Length, columns) * RowHeight;
            return buttons;
        }

        private static void Highlight(GameObject[] group, int selected)
        {
            if (group == null) return;
            for (int i = 0; i < group.Length; i++) SetLit(group[i], i == selected);
        }

        private static void SetLit(GameObject btn, bool lit)
        {
            if (btn == null) return;
            Color c = lit ? Selected : Idle;
            TubityXPanel chrome = btn.GetComponent<TubityXPanel>();
            if (chrome != null)
            {
                chrome.RimColor = c;
                chrome.Highlight = lit ? 1f : 0f;
            }
            TubityXLabel label = btn.GetComponentInChildren<TubityXLabel>();
            if (label != null) label.RimColor = c;
        }

        private static void SetLabel(GameObject btn, string text)
        {
            TubityXLabel label = btn != null ? btn.GetComponentInChildren<TubityXLabel>() : null;
            if (label != null) label.Text = text;
        }
    }
}
