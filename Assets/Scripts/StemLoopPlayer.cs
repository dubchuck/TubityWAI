using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Plays a set of bar-exact stem loops in sample lock-step, the way Soundraw's editor does:
    /// every variant of every stem runs at once from one scheduled DSP start, and the mix is
    /// nothing but which variants are audible. Because all clips in a set share one length,
    /// switching a stem's strength never has to re-sync anything.
    ///
    /// Clips come from Resources/MusicStems and are named
    ///   &lt;Set&gt;_&lt;stem&gt;_h&lt;n&gt;   e.g. TechHouseDark120_dr_h2
    /// where stem is one of KnownStems (me / bc / bc1 / bc2 / bs / dr) and h1, h2 ... are the
    /// variants by rising strength. Strength -1 mutes the stem. Which stems a set has is read
    /// from its clip names: most Soundraw tracks are me + bc + bs + dr, some swap the melody
    /// for two backing parts (bc1 + bc2). Each clip carries its own mix level, so every source
    /// runs at the same gain.
    ///
    /// Changes are quantised to the next bar so a switch lands on the beat. A loop is taken to
    /// be four bars, as every Soundraw block is.
    /// </summary>
    public class StemLoopPlayer : MonoBehaviour
    {
        public static StemLoopPlayer Instance { get; private set; }

        /// <summary>Every stem key Soundraw uses, in display order. A set has a subset of these.</summary>
        public static readonly string[] KnownStems = { "me", "bc", "bc1", "bc2", "bs", "dr" };
        public static readonly string[] KnownStemLabels = { "MELODY", "BACKING", "BACKING 1", "BACKING 2", "BASS", "DRUMS" };
        public const int MaxStems = 6;

        public static readonly string[] EnergyNames = { "QUIET", "MID", "INTENSE" };

        /// <summary>
        /// Soundraw-style energy presets by stem role: the strength each stem takes at QUIET, MID
        /// and INTENSE. Backing carries the quiet floor, bass joins at MID, melody only at INTENSE.
        /// </summary>
        public static int PresetStrength(string stemKey, int energy)
        {
            energy = Mathf.Clamp(energy, 0, EnergyNames.Length - 1);
            switch (stemKey)
            {
                case "me":  return new[] { -1, -1, 1 }[energy];
                case "bs":  return new[] { -1, 0, 1 }[energy];
                case "dr":  return new[] { 0, 0, 1 }[energy];
                default:    return new[] { 0, 1, 1 }[energy];   // bc, bc1, bc2
            }
        }

        private const float MasterVolume = 0.35f;   // matches MusicPlayer's gameplay level
        private const int BarsPerLoop = 4;

        private class Variant
        {
            public AudioClip clip;
            public AudioSource source;
        }

        private class Stem
        {
            public string key;
            public readonly List<Variant> variants = new List<Variant>();   // index = strength
            public int strength = -1;
            public int pendingStrength = -1;
        }

        private readonly List<Stem> stems = new List<Stem>();
        private readonly List<string> sets = new List<string>();
        private AudioClip[] library;
        private string activeSet;

        private double startDsp;
        private double loopSeconds;
        private double barSeconds;
        private int lastBar = -1;
        private bool pending;
        private bool muted;

        public bool IsPlaying { get; private set; }
        public string ActiveSet { get { return activeSet; } }
        public int SetCount { get { return sets.Count; } }

        /// <summary>Stems in the active set, in KnownStems order. Zero until Play has been called.</summary>
        public int StemCount { get { return stems.Count; } }
        public string StemKey(int stemIndex) { return (stemIndex >= 0 && stemIndex < stems.Count) ? stems[stemIndex].key : ""; }
        public string StemLabel(int stemIndex)
        {
            int k = Array.IndexOf(KnownStems, StemKey(stemIndex));
            return k >= 0 ? KnownStemLabels[k] : StemKey(stemIndex).ToUpperInvariant();
        }

        /// <summary>Seconds until the next bar boundary, for a "switching in..." readout.</summary>
        public float SecondsToNextBar
        {
            get
            {
                if (!IsPlaying || barSeconds <= 0.0) return 0f;
                double phase = (AudioSettings.dspTime - startDsp) % barSeconds;
                if (phase < 0.0) phase += barSeconds;
                return (float)(barSeconds - phase);
            }
        }

        public static StemLoopPlayer GetOrCreate()
        {
            if (Instance != null) return Instance;
            GameObject obj = new GameObject("StemLoopPlayer");
            return obj.AddComponent<StemLoopPlayer>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            library = Resources.LoadAll<AudioClip>("MusicStems");
            foreach (AudioClip clip in library)
            {
                string set;
                if (ParseName(clip.name, out set, out _, out _) && !sets.Contains(set)) sets.Add(set);
            }
            sets.Sort(StringComparer.OrdinalIgnoreCase);
            if (sets.Count == 0) Debug.LogWarning("[StemLoopPlayer] No stem loops found in Resources/MusicStems/");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public string SetName(int index)
        {
            return (index >= 0 && index < sets.Count) ? sets[index] : "";
        }

        /// <summary>
        /// Starts (or restarts) the named set with every stem muted, then applies <paramref name="energy"/>.
        /// The set's stems are known as soon as this returns; audio starts a few frames later once
        /// every clip is resident.
        /// </summary>
        public void Play(string set, int energy = 1)
        {
            Stop();
            if (string.IsNullOrEmpty(set) && sets.Count > 0) set = sets[0];
            if (string.IsNullOrEmpty(set)) return;
            activeSet = set;

            // Discover this set's stems up front, in KnownStems order, so the UI can lay out immediately.
            Dictionary<string, Stem> byKey = new Dictionary<string, Stem>();
            foreach (AudioClip clip in library)
            {
                string clipSet, stemKey;
                int variantIndex;
                if (!ParseName(clip.name, out clipSet, out stemKey, out variantIndex) || clipSet != set) continue;
                Stem s;
                if (!byKey.TryGetValue(stemKey, out s))
                {
                    s = new Stem { key = stemKey };
                    byKey[stemKey] = s;
                }
                while (s.variants.Count <= variantIndex) s.variants.Add(null);
                s.variants[variantIndex] = new Variant { clip = clip };
            }
            foreach (string key in KnownStems)
            {
                Stem s;
                if (byKey.TryGetValue(key, out s)) stems.Add(s);
            }
            if (stems.Count == 0)
            {
                Debug.LogWarning("[StemLoopPlayer] Set '" + set + "' has no clips.");
                return;
            }

            // Queue the preset now so GetPendingStrength reads right before the audio is scheduled.
            foreach (Stem s in stems) s.pendingStrength = ClampStrength(s, PresetStrength(s.key, energy));
            pending = true;

            StartCoroutine(StartSet());
        }

        public void Stop()
        {
            StopAllCoroutines();
            IsPlaying = false;
            pending = false;
            foreach (Stem s in stems)
            {
                foreach (Variant v in s.variants)
                {
                    if (v != null && v.source != null) Destroy(v.source);
                }
            }
            stems.Clear();
            lastBar = -1;
        }

        private IEnumerator StartSet()
        {
            List<AudioClip> used = new List<AudioClip>();
            foreach (Stem s in stems)
            {
                foreach (Variant v in s.variants)
                {
                    if (v != null) used.Add(v.clip);
                }
            }

            // Every source must be scheduled off the same clock tick, so make sure no clip is still
            // streaming in from disk when that tick comes.
            foreach (AudioClip clip in used)
            {
                if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            }
            float deadline = Time.realtimeSinceStartup + 5f;
            bool allLoaded = false;
            while (!allLoaded && Time.realtimeSinceStartup < deadline)
            {
                allLoaded = true;
                foreach (AudioClip clip in used)
                {
                    if (clip.loadState != AudioDataLoadState.Loaded) { allLoaded = false; break; }
                }
                if (!allLoaded) yield return null;
            }

            AudioClip reference = used[0];
            loopSeconds = reference.samples / (double)reference.frequency;
            barSeconds = loopSeconds / BarsPerLoop;

            muted = !GameManager.MusicEnabled;
            foreach (Stem s in stems)
            {
                foreach (Variant v in s.variants)
                {
                    if (v == null) continue;
                    AudioSource src = gameObject.AddComponent<AudioSource>();
                    src.clip = v.clip;
                    src.loop = true;
                    src.playOnAwake = false;
                    src.spatialBlend = 0f;
                    src.volume = MasterVolume;
                    src.mute = true;
                    v.source = src;
                    if (v.clip.samples != reference.samples)
                    {
                        Debug.LogWarning("[StemLoopPlayer] " + v.clip.name + " is " + v.clip.samples +
                                         " samples, expected " + reference.samples + " - it will drift.");
                    }
                }
            }

            startDsp = AudioSettings.dspTime + 0.15;
            foreach (Stem s in stems)
            {
                foreach (Variant v in s.variants)
                {
                    if (v != null && v.source != null) v.source.PlayScheduled(startDsp);
                }
            }

            IsPlaying = true;
            ApplyPending();   // the preset queued in Play lands on the first beat
        }

        /// <summary>Queues an energy preset (see PresetStrength) for the next bar.</summary>
        public void ApplyEnergy(int energy, bool immediate = false)
        {
            foreach (Stem s in stems)
            {
                s.pendingStrength = ClampStrength(s, PresetStrength(s.key, energy));
            }
            pending = true;
            if (immediate) ApplyPending();
        }

        /// <summary>Strength of a stem by index into the active set; -1 when muted or not loaded.</summary>
        public int GetStrength(int stemIndex)
        {
            return (stemIndex >= 0 && stemIndex < stems.Count) ? stems[stemIndex].strength : -1;
        }

        /// <summary>The strength that will apply at the next bar (equals GetStrength when nothing is queued).</summary>
        public int GetPendingStrength(int stemIndex)
        {
            return (stemIndex >= 0 && stemIndex < stems.Count) ? stems[stemIndex].pendingStrength : -1;
        }

        public int VariantCount(int stemIndex)
        {
            return (stemIndex >= 0 && stemIndex < stems.Count) ? stems[stemIndex].variants.Count : 0;
        }

        /// <summary>Queues a strength for one stem for the next bar.</summary>
        public void SetStrength(int stemIndex, int strength)
        {
            if (stemIndex < 0 || stemIndex >= stems.Count) return;
            stems[stemIndex].pendingStrength = ClampStrength(stems[stemIndex], strength);
            pending = true;
        }

        /// <summary>Muted -> h1 -> h2 -> ... -> muted, the way Soundraw's grid cells cycle on tap.</summary>
        public void CycleStrength(int stemIndex)
        {
            if (stemIndex < 0 || stemIndex >= stems.Count) return;
            Stem s = stems[stemIndex];
            int next = s.pendingStrength + 1;
            if (next >= s.variants.Count) next = -1;
            SetStrength(stemIndex, next);
        }

        /// <summary>Re-reads GameManager.MusicEnabled; call after that setting changes.</summary>
        public void ApplyMuteState()
        {
            muted = !GameManager.MusicEnabled;
            foreach (Stem s in stems)
            {
                for (int i = 0; i < s.variants.Count; i++)
                {
                    Variant v = s.variants[i];
                    if (v != null && v.source != null) v.source.mute = muted || i != s.strength;
                }
            }
        }

        private static int ClampStrength(Stem s, int strength)
        {
            if (strength < 0) return -1;
            int max = s.variants.Count - 1;
            if (max < 0) return -1;
            int clamped = Mathf.Min(strength, max);
            // Skip a missing variant file rather than falling silent.
            while (clamped >= 0 && s.variants[clamped] == null) clamped--;
            return clamped;
        }

        private void ApplyPending()
        {
            pending = false;
            foreach (Stem s in stems)
            {
                s.strength = s.pendingStrength;
                for (int i = 0; i < s.variants.Count; i++)
                {
                    Variant v = s.variants[i];
                    if (v != null && v.source != null) v.source.mute = muted || i != s.strength;
                }
            }
        }

        private void Update()
        {
            if (!IsPlaying || barSeconds <= 0.0) return;

            double elapsed = AudioSettings.dspTime - startDsp;
            if (elapsed < 0.0) return;
            int bar = (int)Math.Floor(elapsed / barSeconds);
            if (bar != lastBar)
            {
                lastBar = bar;
                if (pending) ApplyPending();
            }
        }

        /// <summary>Splits "Set_stem_hN" into its parts. Variant index is zero-based (h1 -> 0).</summary>
        public static bool ParseName(string clipName, out string set, out string stem, out int variantIndex)
        {
            set = stem = null;
            variantIndex = -1;
            if (string.IsNullOrEmpty(clipName)) return false;

            int lastUnderscore = clipName.LastIndexOf('_');
            if (lastUnderscore <= 0) return false;
            string variant = clipName.Substring(lastUnderscore + 1);
            if (variant.Length < 2 || variant[0] != 'h') return false;
            int n;
            if (!int.TryParse(variant.Substring(1), out n) || n < 1) return false;

            string rest = clipName.Substring(0, lastUnderscore);
            int stemUnderscore = rest.LastIndexOf('_');
            if (stemUnderscore <= 0) return false;

            stem = rest.Substring(stemUnderscore + 1);
            set = rest.Substring(0, stemUnderscore);
            variantIndex = n - 1;
            return Array.IndexOf(KnownStems, stem) >= 0;
        }
    }
}
