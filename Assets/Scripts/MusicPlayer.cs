using UnityEngine;

namespace TubityWAI
{
    public class MusicPlayer : MonoBehaviour
    {
        private static MusicPlayer instance;
        private AudioSource audioSource;
        private AudioClip[] tracks;
        private int currentTrackIndex = -1;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
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

            PlayNextTrack();
        }

        private void Update()
        {
            if (audioSource != null && !audioSource.isPlaying && tracks != null && tracks.Length > 0)
            {
                PlayNextTrack();
            }
        }

        private void PlayNextTrack()
        {
            if (tracks.Length == 0) return;

            int nextTrack;
            if (tracks.Length > 1)
            {
                do
                {
                    nextTrack = Random.Range(0, tracks.Length);
                } while (nextTrack == currentTrackIndex);
            }
            else
            {
                nextTrack = 0;
            }

            currentTrackIndex = nextTrack;
            audioSource.clip = tracks[currentTrackIndex];
            audioSource.Play();
            Debug.Log($"[MusicPlayer] Now playing: {audioSource.clip.name}");
        }
    }
}
