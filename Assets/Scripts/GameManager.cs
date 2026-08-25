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

        // Current session parameters
        public int currentSphereCount;
        public LevelConfig currentLevelConfig;

        public bool IsGameOver { get; private set; } = false;
        private bool hasSavedCoinsThisSession = false;

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
            if (IsGameOver) return;
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
            shouldReplayOnLoad = false;
            lastLevelConfig = null;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ExecuteRestart()
        {
            // Reset the time scale to normal before reloading
            Time.timeScale = 1f;
            IsGameOver = false;

            // Reload the active scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
