using System;
using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    public class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }

        private AudioSource audioSource;
        private AudioClip[] tracks;
        private int currentTrackIndex = -1;
        private bool inGameplay = false;
        private EnvironmentTheme currentEnvironment = EnvironmentTheme.None;

        // Gameplay track pool per environment, matched against clip names (case-insensitive
        // substring). Themes with no entry here draw from every non-loop track instead.
        private static readonly Dictionary<EnvironmentTheme, string[]> environmentTracks = new Dictionary<EnvironmentTheme, string[]>
        {
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            tracks = Resources.LoadAll<AudioClip>("Music");
            if (tracks == null || tracks.Length == 0)
            {
                Debug.LogWarning("[MusicPlayer] No music tracks found in Resources/Music/");
                return;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D background music
            audioSource.volume = 0.35f; // Mix it slightly lower than SFX
            audioSource.loop = false;
            ApplyMuteState();

            PlayMenuAmbient();
        }

        /// <summary>Syncs the music mute state with [[GameManager.MusicEnabled]]; call after that setting changes.</summary>
        public void ApplyMuteState()
        {
            if (audioSource != null) audioSource.mute = !GameManager.MusicEnabled;
        }

        private void Update()
        {
            if (audioSource == null || audioSource.isPlaying || tracks == null || tracks.Length == 0) return;

            if (inGameplay)
            {
                PlayTrack(PickGameplayTrackIndex());
            }
            else
            {
                PlayMenuAmbient();
            }
        }

        /// <summary>Call when the menu/attract screen becomes active: loops the ambient menu track (falls back to shuffling everything if none is tagged as a loop).</summary>
        public void PlayMenuAmbient()
        {
            if (tracks == null || tracks.Length == 0) return;
            inGameplay = false;

            int loopTrack = Array.FindIndex(tracks, IsLoopTrack);
            PlayTrack(loopTrack >= 0 ? loopTrack : PickRandomTrackIndex(tracks.Length));
        }

        /// <summary>Call when a level starts: switches to upbeat music, drawn from that environment's pool when [[environmentTracks]] has one.</summary>
        public void PlayGameplayMusic(EnvironmentTheme environment)
        {
            if (tracks == null || tracks.Length == 0) return;
            inGameplay = true;
            currentEnvironment = environment;
            PlayTrack(PickGameplayTrackIndex());
        }

        private int PickGameplayTrackIndex()
        {
            List<int> pool = new List<int>();

            if (environmentTracks.TryGetValue(currentEnvironment, out string[] names))
            {
                for (int i = 0; i < tracks.Length; i++)
                {
                    foreach (string name in names)
                    {
                        if (tracks[i].name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            pool.Add(i);
                            break;
                        }
                    }
                }
            }

            if (pool.Count == 0)
            {
                for (int i = 0; i < tracks.Length; i++)
                {
                    if (!IsLoopTrack(tracks[i])) pool.Add(i);
                }
            }

            if (pool.Count == 0) return PickRandomTrackIndex(tracks.Length); // every track is a loop track

            if (pool.Count > 1) pool.Remove(currentTrackIndex);

            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        private int PickRandomTrackIndex(int length)
        {
            if (length <= 1) return 0;

            int next;
            do
            {
                next = UnityEngine.Random.Range(0, length);
            } while (next == currentTrackIndex);
            return next;
        }

        private static bool IsLoopTrack(AudioClip clip)
        {
            return clip.name.IndexOf("Loop", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void PlayTrack(int index)
        {
            currentTrackIndex = index;
            audioSource.clip = tracks[currentTrackIndex];
            audioSource.loop = IsLoopTrack(audioSource.clip);
            audioSource.Play();
            Debug.Log($"[MusicPlayer] Now playing: {audioSource.clip.name}");
        }
    }
}
