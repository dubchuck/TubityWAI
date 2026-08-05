# AdMob Unity Integration Guide

This guide details the Google Mobile Ads (AdMob) integration for `TubityWAI`.

---

## 1. Credentials & Configuration

### App IDs
- **Android App ID**: `ca-app-pub-1871515768118195~9628463601`
- **iOS App ID**: `ca-app-pub-1871515768118195~1439696409`

These are configured in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` and injected automatically into Android Manifest and iOS `Info.plist` during builds.

### Interstitial Ad Unit IDs
- **Android Ad Unit (`TubityXInterstitialAndroid`)**: `ca-app-pub-1871515768118195/8315381931`
- **iOS Ad Unit (`InterstitialTubityX`)**: `ca-app-pub-1871515768118195/2517240285`
- **Test Android Ad Unit**: `ca-app-pub-3940256099942544/1033173712`
- **Test iOS Ad Unit**: `ca-app-pub-3940256099942544/4411468910`

---

## 2. Reusable Component (`AdMobManager`)

The project uses `AdMobManager.cs` ([Assets/Scripts/AdMobManager.cs](file:///Users/jeremy/Documents/dubchuck/TubityWAI/Assets/Scripts/AdMobManager.cs)), a persistent singleton (`DontDestroyOnLoad`) that manages interstitial ad preloading, retry logic, and game-over frequency capping.

### Inspector Controls & Persistence
- **Use Test Ad Units**: Checkbox (`useTestAdUnits`). Set to `true` during development to protect your AdMob account from policy violations. Uncheck `useTestAdUnits` before releasing to production.
- **Min / Max Game Over Threshold**: Set to `3` and `5` by default.
- **PlayerPrefs Persistence**:
  - `AdMob_GameOverCounter`: Persists the game over count between sessions.
  - `AdMob_TargetThreshold`: Persists the target threshold (3 to 5) so progress is maintained seamlessly when re-launching the app.

---

## 3. Runtime Flow

1. On scene startup or initialization via [GameSetup.cs](file:///Users/jeremy/Documents/dubchuck/TubityWAI/Assets/Scripts/GameSetup.cs), `AdMobManager` initializes the SDK and preloads an interstitial ad.
2. Every time a player loses and hits Replay/Restart, `GameManager.cs` calls `AdMobManager.Instance.ProcessGameOverAd(ExecuteRestart)`.
3. `AdMobManager` increments the game-over counter.
4. A target threshold between **3 and 5** game overs is randomly selected.
5. When the counter reaches the target threshold:
   - The preloaded interstitial ad is shown.
   - Upon closing or dismissing the ad, the level resets smoothly via the callback.
   - A new random threshold between 3 and 5 is generated for the next cycle.
6. If the threshold is not reached yet or the ad is not ready, the level resets immediately without delay.
