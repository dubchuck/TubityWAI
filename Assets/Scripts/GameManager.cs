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

        // Current session parameters
        public int currentSphereCount;
        public LevelConfig currentLevelConfig;

        public bool IsGameOver { get; private set; } = false;

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
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void GameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;

            // Pause the gameplay physics/time scale
            Time.timeScale = 0f;

            // Notify the GameHUD to present the Game Over overlay
            GameHUD hud = FindFirstObjectByType<GameHUD>();
            if (hud != null)
            {
                hud.ShowGameOverScreen();
            }
        }

        public void TriggerReplay()
        {
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
