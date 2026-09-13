using UnityEngine;
using UnityEngine.SceneManagement;

namespace TubityWAI
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // Static parameters preserved across scene loads
        public static int lastSphereCount = 3;
        public static LevelConfig lastLevelConfig = null;
        public static bool shouldReplayOnLoad = false;
        
        // Persistent Shop Data
        private const string PREF_TOTAL_COINS = "TotalCoins";
        private const string PREF_EQUIPPED_SKIN = "EquippedSkin";
        private const string PREF_SKIN_UNLOCKED_PREFIX = "SkinUnlocked_";

        // Persistent Audio Settings
        private const string PREF_SFX_ENABLED = "SfxEnabled";
        private const string PREF_MUSIC_ENABLED = "MusicEnabled";

        public static bool SfxEnabled
        {
            get => PlayerPrefs.GetInt(PREF_SFX_ENABLED, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_SFX_ENABLED, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool MusicEnabled
        {
            get => PlayerPrefs.GetInt(PREF_MUSIC_ENABLED, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_MUSIC_ENABLED, value ? 1 : 0);
                PlayerPrefs.Save();
                if (MusicPlayer.Instance != null) MusicPlayer.Instance.ApplyMuteState();
            }
        }

        // Current session parameters
        public int currentSphereCount;
        public LevelConfig currentLevelConfig;

        public bool IsGameOver { get; private set; } = false;
        private bool hasSavedCoinsThisSession = false;

        // ---- Campaign progress (persistent) ------------------------------------------------
        private const string PREF_LEVEL_BEATEN_PREFIX = "LevelBeaten_";
        private const string PREF_LEVEL_STARS_PREFIX = "LevelStars_";

        /// <summary>Level 1 is always open; every other level opens once the one before it is beaten.</summary>
        public static bool IsLevelUnlocked(int level)
        {
            return level <= 1 || PlayerPrefs.GetInt(PREF_LEVEL_BEATEN_PREFIX + (level - 1), 0) == 1;
        }

        public static bool IsLevelBeaten(int level)
        {
            return PlayerPrefs.GetInt(PREF_LEVEL_BEATEN_PREFIX + level, 0) == 1;
        }

        public static int GetLevelStars(int level)
        {
            return PlayerPrefs.GetInt(PREF_LEVEL_STARS_PREFIX + level, 0);
        }

        /// <summary>Records the result and returns the sphere count this beat just unlocked (0 if none).</summary>
        public static int RecordLevelResult(int level, int stars)
        {
            bool wasAlreadyBeaten = IsLevelBeaten(level);
            PlayerPrefs.SetInt(PREF_LEVEL_BEATEN_PREFIX + level, 1);
            PlayerPrefs.SetInt(PREF_LEVEL_STARS_PREFIX + level, Mathf.Max(stars, GetLevelStars(level)));
            PlayerPrefs.Save();

            if (wasAlreadyBeaten) return 0;
            for (int n = 2; n <= 5; n++)
            {
                if (GetSphereUnlockLevel(n) == level) return n;
            }
            return 0;
        }

        // ---- Sphere-count blocks (persistent) -----------------------------------------------
        // The campaign's 24 levels split into five-level blocks: sphere count 1 is always
        // available, and sphere count N (2-5) opens once level (N-1)*SphereBlockSize is beaten.
        private const int SphereBlockSize = 5;

        /// <summary>Campaign level that must be beaten to open this sphere count. 0 means always open.</summary>
        public static int GetSphereUnlockLevel(int sphereCount)
        {
            return (Mathf.Clamp(sphereCount, 1, 5) - 1) * SphereBlockSize;
        }

        public static bool IsSphereCountUnlocked(int sphereCount)
        {
            int required = GetSphereUnlockLevel(sphereCount);
            return required <= 0 || IsLevelBeaten(required);
        }

        /// <summary>Highest sphere count currently unlocked, for clamping a stale selection.</summary>
        public static int GetMaxUnlockedSphereCount()
        {
            for (int n = 5; n > 1; n--)
            {
                if (IsSphereCountUnlocked(n)) return n;
            }
            return 1;
        }

        /// <summary>Dev helper: opens every campaign level without touching star records.</summary>
        public static void UnlockAllLevels()
        {
            for (int n = 1; n <= LevelProgression.CampaignLevelCount; n++)
                PlayerPrefs.SetInt(PREF_LEVEL_BEATEN_PREFIX + n, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Dev helper: wipes beaten flags and stars so the campaign locks back to level 1.</summary>
        public static void ResetLevelProgress()
        {
            for (int n = 1; n <= LevelProgression.CampaignLevelCount; n++)
            {
                PlayerPrefs.DeleteKey(PREF_LEVEL_BEATEN_PREFIX + n);
                PlayerPrefs.DeleteKey(PREF_LEVEL_STARS_PREFIX + n);
            }
            PlayerPrefs.Save();
        }

        // ---- Level result ---------------------------------------------------------------------
        public class LevelResult
        {
            public int levelNumber;
            public bool isTestLevel;
            public int coinsCollected;
            public int coinsSpawned;
            public int shieldsPassed;
            public int shieldsSpawned;
            public int score;
            public float time;
            public int stars;
            public bool hasNextLevel;
            public int unlockedSphereCount; // sphere count just unlocked by this result, 0 if none

            /// <summary>0..1: coins collected weighted 60%, shields passed 40%. Missing categories drop out.</summary>
            public float Performance()
            {
                float weight = 0f, total = 0f;
                if (coinsSpawned > 0) { total += 0.6f * (coinsCollected / (float)coinsSpawned); weight += 0.6f; }
                if (shieldsSpawned > 0) { total += 0.4f * (shieldsPassed / (float)shieldsSpawned); weight += 0.4f; }
                return weight > 0f ? Mathf.Clamp01(total / weight) : 1f;
            }
        }

        public bool IsLevelComplete { get; private set; } = false;
        public LevelResult LastResult { get; private set; }

        // Per-run tallies fed by the tunnel spawner and the obstacles (only things before the gate count)
        public int CoinsSpawned { get; private set; }
        public int ShieldsSpawned { get; private set; }
        public int ShieldsPassed { get; private set; }

        public void RegisterCoinSpawned() { CoinsSpawned++; }
        public void RegisterShieldSpawned() { ShieldsSpawned++; }
        public void RegisterShieldPassed() { ShieldsPassed++; }

        /// <summary>1 star for finishing, 2 at 45% performance, 3 at 80%.</summary>
        public static int ComputeStars(LevelResult r)
        {
            float p = r.Performance();
            if (p >= 0.80f) return 3;
            if (p >= 0.45f) return 2;
            return 1;
        }

        public void LevelComplete()
        {
            if (IsGameOver || IsLevelComplete) return;
            IsLevelComplete = true;

            PlayerController pc = FindFirstObjectByType<PlayerController>();
            LevelResult r = new LevelResult();
            r.levelNumber = currentLevelConfig != null ? currentLevelConfig.levelNumber : 0;
            r.isTestLevel = currentLevelConfig != null && currentLevelConfig.isTestLevel;
            r.coinsCollected = pc != null ? pc.Coins : 0;
            r.coinsSpawned = CoinsSpawned;
            r.shieldsPassed = ShieldsPassed;
            r.shieldsSpawned = ShieldsSpawned;
            r.score = pc != null ? pc.Score : 0;
            r.time = pc != null ? pc.TimeElapsed : 0f;
            r.stars = ComputeStars(r);
            r.hasNextLevel = !r.isTestLevel && r.levelNumber >= 1 && r.levelNumber < LevelProgression.CampaignLevelCount;
            LastResult = r;

            if (!r.isTestLevel && r.levelNumber >= 1)
            {
                r.unlockedSphereCount = RecordLevelResult(r.levelNumber, r.stars);
            }

            SaveSessionCoins();
            StartCoroutine(PresentLevelComplete());
        }

        private System.Collections.IEnumerator PresentLevelComplete()
        {
            GameHUD hud = FindFirstObjectByType<GameHUD>();
            if (hud != null)
            {
                hud.PlayGameplayHudExitAnimation();
            }

            // Let the player coast through the gate for a beat before the finish burst.
            yield return new WaitForSecondsRealtime(0.5f);
            Time.timeScale = 0f;

            // Freeze the world, then let the player sphere(s) explode into a huge,
            // receding hero shot (unscaled time, so it still plays while paused).
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                yield return pc.StartCoroutine(pc.PlayFinishBurst());
            }

            if (hud != null)
            {
                hud.ShowLevelCompleteScreen(LastResult);
            }
        }

        /// <summary>Loads the next campaign level with the same sphere count, via the replay path.</summary>
        public void TriggerNextLevel()
        {
            if (currentLevelConfig == null) return;
            int next = currentLevelConfig.levelNumber + 1;
            if (currentLevelConfig.isTestLevel || next > LevelProgression.CampaignLevelCount) return;

            SaveSessionCoins();
            lastSphereCount = currentSphereCount;
            lastLevelConfig = LevelProgression.CreateCampaignLevel(next);
            shouldReplayOnLoad = true;
            ExecuteRestart();
        }

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            if (FindFirstObjectByType<MusicPlayer>() == null)
            {
                GameObject musicObj = new GameObject("MusicPlayer");
                musicObj.AddComponent<MusicPlayer>();
            }

            // Ensure default skin (index 0) is always unlocked
            UnlockSkin(0);
        }

        public int TotalCoins
        {
            get => PlayerPrefs.GetInt(PREF_TOTAL_COINS, 0);
            private set
            {
                PlayerPrefs.SetInt(PREF_TOTAL_COINS, value);
                PlayerPrefs.Save();
            }
        }

        public void AddCoinsToTotal(int amount)
        {
            if (amount > 0)
            {
                TotalCoins += amount;
            }
        }

        /// <summary>
        /// Adds coins to the persistent total without needing a live instance,
        /// so an IAP grant works in any scene (menu or gameplay).
        /// </summary>
        public static void GrantCoins(int amount)
        {
            if (amount <= 0) return;
            int total = PlayerPrefs.GetInt(PREF_TOTAL_COINS, 0) + amount;
            PlayerPrefs.SetInt(PREF_TOTAL_COINS, total);
            PlayerPrefs.Save();
        }

        public bool DeductCoins(int amount)
        {
            if (TotalCoins >= amount)
            {
                TotalCoins -= amount;
                return true;
            }
            return false;
        }

        public bool IsSkinUnlocked(int skinIndex)
        {
            return PlayerPrefs.GetInt(PREF_SKIN_UNLOCKED_PREFIX + skinIndex, 0) == 1;
        }

        public void UnlockSkin(int skinIndex)
        {
            PlayerPrefs.SetInt(PREF_SKIN_UNLOCKED_PREFIX + skinIndex, 1);
            PlayerPrefs.Save();
        }

        public int EquippedSkin
        {
            get => PlayerPrefs.GetInt(PREF_EQUIPPED_SKIN, 0);
            set
            {
                if (IsSkinUnlocked(value))
                {
                    PlayerPrefs.SetInt(PREF_EQUIPPED_SKIN, value);
                    PlayerPrefs.Save();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SaveSessionCoins()
        {
            if (hasSavedCoinsThisSession) return;
            hasSavedCoinsThisSession = true;

            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                AddCoinsToTotal(pc.Coins);
            }
        }

        public void GameOver()
        {
            if (IsGameOver || IsLevelComplete) return;
            IsGameOver = true;

            // Pause the gameplay physics/time scale
            Time.timeScale = 0f;

            // Save collected coins to persistent total
            SaveSessionCoins();

            // Notify the GameHUD to present the Game Over overlay
            GameHUD hud = FindFirstObjectByType<GameHUD>();
            if (hud != null)
            {
                hud.ShowGameOverScreen();
            }
        }

        public void TriggerReplay()
        {
            SaveSessionCoins();

            // Cache active setup settings to static state to trigger direct startup on load
            lastSphereCount = currentSphereCount;
            lastLevelConfig = currentLevelConfig;
            shouldReplayOnLoad = true;

            // Check if AdMob interstitial ad should be displayed before restarting
            if (AdMobManager.Instance != null)
            {
                AdMobManager.Instance.ProcessGameOverAd(ExecuteRestart);
            }
            else
            {
                ExecuteRestart();
            }
        }

        public void RestartGame()
        {
            TriggerReplay();
        }

        public void ReturnToMainMenu()
        {
            SaveSessionCoins();

            Time.timeScale = 1f;
            IsGameOver = false;
            IsLevelComplete = false;
            shouldReplayOnLoad = false;
            lastLevelConfig = null;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ExecuteRestart()
        {
            // Reset the time scale to normal before reloading
            Time.timeScale = 1f;
            IsGameOver = false;
            IsLevelComplete = false;

            // Reload the active scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
