using System;
using UnityEngine;
using GoogleMobileAds.Api;

namespace TubityWAI
{
    /// <summary>
    /// Reusable singleton component for Google Mobile Ads (AdMob) Interstitial Ad initialization,
    /// preloading, exponential retry logic, and randomized game-over frequency capping (3 to 5 game overs).
    /// </summary>
    public class AdMobManager : MonoBehaviour
    {
        public static AdMobManager Instance { get; private set; }

        [Header("AdMob Configuration")]
        [Tooltip("If true, Google Test Ad Unit IDs will be used to protect your account during development.")]
        [SerializeField] private bool useTestAdUnits = true;

        [Header("Production Ad Unit IDs")]
        [Tooltip("Android Interstitial Ad Unit ID (TubityXInterstitialAndroid)")]
        [SerializeField] private string adUnitIdAndroidReal = "ca-app-pub-1871515768118195/8315381931";

        [Tooltip("iOS Interstitial Ad Unit ID (InterstitialTubityX)")]
        [SerializeField] private string adUnitIdIOSReal = "ca-app-pub-1871515768118195/2517240285";

        [Header("Test Ad Unit IDs")]
        [SerializeField] private string adUnitIdAndroidTest = "ca-app-pub-3940256099942544/1033173712";
        [SerializeField] private string adUnitIdIOSTest = "ca-app-pub-3940256099942544/4411468910";

        [Header("Frequency Capping Settings")]
        [Tooltip("Minimum game overs required before showing an ad.")]
        [SerializeField] private int minGameOverThreshold = 3;

        [Tooltip("Maximum game overs required before showing an ad.")]
        [SerializeField] private int maxGameOverThreshold = 5;

        private InterstitialAd _interstitialAd;
        private int _gameOverCounter = 0;
        private int _targetGameOverThreshold;
        private int _retryAttempt = 0;
        private bool _isSdkInitialized = false;
        private Action _pendingAdDismissedCallback;

        public string ActiveAdUnitId
        {
            get
            {
                if (useTestAdUnits)
                {
#if UNITY_IOS
                    return adUnitIdIOSTest;
#else
                    return adUnitIdAndroidTest;
#endif
                }
                else
                {
#if UNITY_IOS
                    return adUnitIdIOSReal;
#else
                    return adUnitIdAndroidReal;
#endif
                }
            }
        }

        private const string PrefsGameOverCounterKey = "AdMob_GameOverCounter";
        private const string PrefsTargetThresholdKey = "AdMob_TargetThreshold";

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                // If attached to a multi-purpose GameObject (e.g. GameSetup), detach onto a dedicated GameObject so the host object is not marked DontDestroyOnLoad
                if (transform.parent != null || GetComponents<Component>().Length > 2)
                {
                    GameObject standaloneObj = new GameObject("AdMobManager");
                    AdMobManager standaloneManager = standaloneObj.AddComponent<AdMobManager>();
                    standaloneManager.useTestAdUnits = this.useTestAdUnits;
                    standaloneManager.adUnitIdAndroidReal = this.adUnitIdAndroidReal;
                    standaloneManager.adUnitIdIOSReal = this.adUnitIdIOSReal;
                    standaloneManager.adUnitIdAndroidTest = this.adUnitIdAndroidTest;
                    standaloneManager.adUnitIdIOSTest = this.adUnitIdIOSTest;
                    standaloneManager.minGameOverThreshold = this.minGameOverThreshold;
                    standaloneManager.maxGameOverThreshold = this.maxGameOverThreshold;
                    Destroy(this);
                    return;
                }

                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadStateFromPrefs();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeSdk();
        }

        private void LoadStateFromPrefs()
        {
            _gameOverCounter = PlayerPrefs.GetInt(PrefsGameOverCounterKey, 0);
            _targetGameOverThreshold = PlayerPrefs.GetInt(PrefsTargetThresholdKey, 0);

            if (_targetGameOverThreshold < minGameOverThreshold || _targetGameOverThreshold > maxGameOverThreshold)
            {
                InitializeTargetThreshold();
            }
            else
            {
                Debug.Log($"[AdMobManager] Restored play count from PlayerPrefs - Counter: {_gameOverCounter}/{_targetGameOverThreshold}");
            }
        }

        private void SaveStateToPrefs()
        {
            PlayerPrefs.SetInt(PrefsGameOverCounterKey, _gameOverCounter);
            PlayerPrefs.SetInt(PrefsTargetThresholdKey, _targetGameOverThreshold);
            PlayerPrefs.Save();
        }

        private void InitializeTargetThreshold()
        {
            _targetGameOverThreshold = UnityEngine.Random.Range(minGameOverThreshold, maxGameOverThreshold + 1);
            SaveStateToPrefs();
            Debug.Log($"[AdMobManager] Target game-overs set for next ad display: {_targetGameOverThreshold}");
        }

        /// <summary>
        /// Initializes the Google Mobile Ads SDK.
        /// </summary>
        public void InitializeSdk()
        {
            Debug.Log("[AdMobManager] Initializing Google Mobile Ads SDK...");
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdMobManager] Google Mobile Ads SDK Initialized successfully.");
                _isSdkInitialized = true;
                LoadInterstitialAd();
            });
        }

        /// <summary>
        /// Preloads the interstitial ad with automatic retry on failure.
        /// </summary>
        public void LoadInterstitialAd()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            string adUnitId = ActiveAdUnitId;
            Debug.Log($"[AdMobManager] Loading Interstitial Ad for Ad Unit ID: {adUnitId} (TestMode: {useTestAdUnits})");

            var adRequest = new AdRequest();
            InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    _retryAttempt++;
                    double retryDelay = Math.Pow(2, Math.Min(6, _retryAttempt));
                    Debug.LogWarning($"[AdMobManager] Interstitial ad failed to load: {error?.GetMessage() ?? "Unknown error"}. Retrying in {retryDelay} seconds...");
                    Invoke(nameof(LoadInterstitialAd), (float)retryDelay);
                    return;
                }

                Debug.Log("[AdMobManager] Interstitial Ad loaded successfully.");
                _interstitialAd = ad;
                _retryAttempt = 0;

                RegisterAdEvents(ad);
            });
        }

        private void RegisterAdEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMobManager] Interstitial ad full screen content closed.");
                ExecuteDismissCallback();
                LoadInterstitialAd();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[AdMobManager] Interstitial ad failed to show: {error.GetMessage()}");
                ExecuteDismissCallback();
                LoadInterstitialAd();
            };
        }

        private void ExecuteDismissCallback()
        {
            _pendingAdDismissedCallback?.Invoke();
            _pendingAdDismissedCallback = null;
        }

        /// <summary>
        /// Increments game over counter and presents interstitial ad when random threshold (3-5) is reached.
        /// Executes <paramref name="onCompleted"/> when ad is closed, failed, or skipped.
        /// </summary>
        public void ProcessGameOverAd(Action onCompleted)
        {
            if (PlayerPrefs.GetInt("AdsRemoved", 0) == 1)
            {
                Debug.Log("[AdMobManager] Ads have been removed. Skipping ad presentation.");
                onCompleted?.Invoke();
                return;
            }

            _gameOverCounter++;
            SaveStateToPrefs();
            Debug.Log($"[AdMobManager] Game Over recorded. Progress: {_gameOverCounter}/{_targetGameOverThreshold}");

            if (_gameOverCounter >= _targetGameOverThreshold)
            {
                bool adShown = TryShowInterstitial(onCompleted);
                if (adShown)
                {
                    _gameOverCounter = 0;
                    InitializeTargetThreshold();
                }
                else
                {
                    Debug.LogWarning("[AdMobManager] Ad was not ready when threshold reached. Proceeding with game restart.");
                    onCompleted?.Invoke();
                }
            }
            else
            {
                onCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Attempts to display the preloaded Interstitial Ad. Returns true if display request was initiated.
        /// </summary>
        public bool TryShowInterstitial(Action onDismissedCallback = null)
        {
            if (PlayerPrefs.GetInt("AdsRemoved", 0) == 1)
            {
                Debug.Log("[AdMobManager] Ads have been removed. Bypassing TryShowInterstitial.");
                onDismissedCallback?.Invoke();
                return false;
            }

            if (_isSdkInitialized && _interstitialAd != null && _interstitialAd.CanShowAd())
            {
                Debug.Log("[AdMobManager] Presenting Interstitial Ad...");
                _pendingAdDismissedCallback = onDismissedCallback;
                _interstitialAd.Show();
                return true;
            }
            else
            {
                Debug.Log("[AdMobManager] Interstitial ad not ready yet. Triggering preload...");
                LoadInterstitialAd();
                return false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
        }
    }
}
