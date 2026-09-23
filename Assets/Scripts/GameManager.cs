using System.Collections.Generic;
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
                if (StemLoopPlayer.Instance != null) StemLoopPlayer.Instance.ApplyMuteState();
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
            public bool usedCheckpoint;     // the run resumed from a checkpoint rather than the start
            public int spheresLost;         // spheres knocked off by hits the player survived

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

        /// <summary>Hits survived by losing a sphere (twin levels without colour).</summary>
        public int SpheresLost { get; private set; }
        public void RegisterSphereLost() { SpheresLost++; }

        public void RegisterCoinSpawned() { CoinsSpawned++; }
        /// <summary>A coin laid down and then taken back (it landed on an arc), so it never counts
        /// against the player's coin share.</summary>
        public void UnregisterCoinSpawned() { if (CoinsSpawned > 0) CoinsSpawned--; }
        public void RegisterShieldSpawned() { ShieldsSpawned++; }
        public void RegisterShieldPassed() { ShieldsPassed++; }

        /// <summary>
        /// Stars for a composed progression level. Shields there are optional alternative routes
        /// rather than the way through, so counting them would punish players for taking the open
        /// gap - coins carry the skill signal instead, and the third star asks for a clean run:
        /// no checkpoint, and no sphere lost along the way (a hit a twin level let you survive).
        /// </summary>
        public static int ComputeProgressionStars(LevelResult r)
        {
            float coins = r.coinsSpawned > 0 ? r.coinsCollected / (float)r.coinsSpawned : 1f;
            if (coins >= 0.85f && !r.usedCheckpoint && r.spheresLost == 0) return 3;
            if (coins >= 0.55f) return 2;
            return 1;
        }

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
            r.spheresLost = SpheresLost;
            r.stars = ComputeStars(r);

            // Progression Test 1 keeps its own records, in its own PlayerPrefs namespace, so
            // nothing it does can disturb the original campaign's stars or unlocks.
            bool isProgression = Progression.ProgressionV2.IsProgressionLevel(currentLevelConfig);
            if (isProgression)
            {
                r.usedCheckpoint = currentLevelConfig.startZ > 0f;
                r.stars = ComputeProgressionStars(r);
                int displayLevel = Progression.ProgressionV2.DisplayNumber(currentLevelConfig);
                r.hasNextLevel = displayLevel < Progression.ProgressionV2.LevelCount;
                r.levelNumber = displayLevel;          // the HUD should say 37, not 1037
                LastResult = r;
                Progression.ProgressionSave.RecordResult(displayLevel, r.stars);
            }
            else
            {
                r.hasNextLevel = !r.isTestLevel && r.levelNumber >= 1 && r.levelNumber < LevelProgression.CampaignLevelCount;
                LastResult = r;

                if (!r.isTestLevel && r.levelNumber >= 1)
                {
                    r.unlockedSphereCount = RecordLevelResult(r.levelNumber, r.stars);
                }
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

            // Nothing stops at the gate: the sphere holds its line and the empty tube keeps
            // streaming past (PlayerController ignores input once the level is complete), and
            // the review card arrives over it after a beat of clear air.
            yield return new WaitForSecondsRealtime(0.9f);

            if (hud != null)
            {
                hud.ShowLevelCompleteScreen(LastResult);
            }
        }

        /// <summary>Loads the next campaign level with the same sphere count, via the replay path.</summary>
        public void TriggerNextLevel()
        {
            if (currentLevelConfig == null) return;

            if (Progression.ProgressionV2.IsProgressionLevel(currentLevelConfig))
            {
                int nextProgression = Progression.ProgressionV2.DisplayNumber(currentLevelConfig) + 1;
                if (nextProgression > Progression.ProgressionV2.LevelCount) return;

                SaveSessionCoins();
                lastSphereCount = currentSphereCount;
                lastLevelConfig = Progression.ProgressionV2.CreateLevel(nextProgression);
                ContinueInPlace(lastLevelConfig);
                return;
            }

            int next = currentLevelConfig.levelNumber + 1;
            if (currentLevelConfig.isTestLevel || next > LevelProgression.CampaignLevelCount) return;

            SaveSessionCoins();
            lastSphereCount = currentSphereCount;
            lastLevelConfig = LevelProgression.CreateCampaignLevel(next);
            ContinueInPlace(lastLevelConfig);
        }

        // ---- Level-to-level transition ---------------------------------------------------------
        // From the review card the next run is built in this scene rather than by reloading it,
        // and the sphere never stops: the card leaves and the controls come straight back, the
        // old tube swells away while the next level's tube flies in out of the distance, and the
        // sphere coasts onto the new start line, where the countdown begins.
        //
        // The new level is laid out from z = 0 like any other, so the hand-off moves the old run
        // instead. Player, camera and old tube shift together - invisible, since nothing changes
        // relative to the camera - so the sphere ends up just behind the new start line.

        private bool isTransitioning;

        /// <summary>The review card has been dismissed and the next run is on its way in.</summary>
        public bool IsTransitioning => isTransitioning;

        private const float ApproachSeconds = 1.6f;      // coasting onto the new start line
        private const float MinApproachDistance = 8f;
        private const float ArriveFromDistance = 240f;   // out past the fog
        private const float ArriveSeconds = 0.85f;
        private const float ArriveStagger = 0.06f;       // per segment, nearest to the sphere first
        private const float DepartSeconds = 0.75f;
        private const float DepartScale = 5f;

        /// <summary>Starts the next run in place. Falls back to a scene reload if there is no
        /// GameSetup to build it (an editor test scene, say).</summary>
        private void ContinueInPlace(LevelConfig next)
        {
            if (isTransitioning || next == null) return;

            if (GameSetup.Instance == null)
            {
                lastLevelConfig = next;
                shouldReplayOnLoad = true;
                ExecuteRestart();
                return;
            }

            isTransitioning = true;
            StartCoroutine(TransitionToRun(next, currentSphereCount, fromRest: IsGameOver));
        }

        private System.Collections.IEnumerator TransitionToRun(LevelConfig next, int sphereCount, bool fromRest)
        {
            // Time stays as it is (paused, after a crash) until the old player is frozen at the
            // hand-off; ResetRunState restarts it. Everything before then runs on unscaled time.

            // Controls are live from here (PlayerController checks IsTransitioning), so the player
            // is already steering while the card clears.
            GameHUD oldHud = FindFirstObjectByType<GameHUD>();
            if (oldHud != null) yield return oldHud.StartCoroutine(oldHud.AnimateLevelCompleteOut());

            PlayerController oldPlayer = PlayerController.Instance;
            TunnelGenerator oldTunnel = FindFirstObjectByType<TunnelGenerator>();
            // Already cleared away after a crash: there is no old tube to hand off from. If its
            // exit is somehow still playing, finish it now rather than shift it mid-flight.
            TunnelGenerator discardedTunnel = null;
            if (oldTunnel != null && oldTunnel == departingTunnel)
            {
                Destroy(oldTunnel.gameObject);
                discardedTunnel = oldTunnel;    // still findable until the frame ends
                departingTunnel = null;
                oldTunnel = null;
            }
            Camera cam = Camera.main;

            if (oldPlayer == null || cam == null)
            {
                // Nothing to hand off from: just build the next run.
                TearDownRun();
                yield return null;
                ResetRunState();
                GameSetup.Instance.StartGame(sphereCount, next);
                isTransitioning = false;
                yield break;
            }

            // ---- Hand-off frame: shift the old run, build the new one around it ------------------
            // The run starts on its line, or on a checkpoint banked before a crash. Coming in from
            // a finished level the approach decelerates evenly, so covering `approach` in
            // ApproachSeconds leaves at the speed the sphere is flying now; after a crash it
            // starts from rest at the new level's pace.
            float startZ = Mathf.Max(0f, next.startZ);
            float pace = fromRest ? next.forwardSpeed : oldPlayer.forwardSpeed;
            float approach = Mathf.Max(MinApproachDistance, Mathf.Max(1f, pace) * ApproachSeconds * 0.5f);
            float angle = oldPlayer.currentAngle;

            Vector3 curve = next.GetCurveOffset(startZ - approach);
            Vector3 handOff = new Vector3(curve.x, curve.y, startZ - approach);
            Vector3 shift = handOff - oldPlayer.transform.position;

            oldPlayer.enabled = false;                  // frozen in pose for its last frame on screen
            oldPlayer.transform.position += shift;
            if (oldTunnel != null)
            {
                oldTunnel.enabled = false;              // stop recycling against the new level's config
                oldTunnel.transform.position += shift;
                // After a crash the arc that did it now sits right where the new sphere appears.
                // The old tube is scenery from here on; nothing in it may be hit.
                foreach (Collider c in oldTunnel.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            }
            Vector3 camPos = cam.transform.position + shift;
            Quaternion camRot = cam.transform.rotation;

            if (oldHud != null) Destroy(oldHud.gameObject);
            foreach (FTUEManager f in FindObjectsByType<FTUEManager>(FindObjectsSortMode.None))
                Destroy(f.gameObject);

            ResetRunState();
            GameSetup.Instance.StartGame(sphereCount, next, inPlace: true);
            cam.transform.SetPositionAndRotation(camPos, camRot);

            PlayerController newPlayer = PlayerController.Instance;
            if (newPlayer == oldPlayer) newPlayer = null;
            if (newPlayer != null)
            {
                newPlayer.transform.position = handOff;
                newPlayer.BeginApproach(startZ - approach, startZ, ApproachSeconds, angle, fromRest,
                                        oldPlayer.SphereCount);
                // Hidden until its first Update has put the spheres on the old ones' line.
                SetRunVisible(newPlayer.gameObject, false);
            }

            TunnelGenerator newTunnel = null;
            foreach (TunnelGenerator g in FindObjectsByType<TunnelGenerator>(FindObjectsSortMode.None))
                if (g != oldTunnel && g != discardedTunnel) newTunnel = g;
            // Tube for the approach and the trailing camera, which both sit behind the start line.
            if (newTunnel != null) newTunnel.extraDistanceBehind = approach + 40f;

            // ---- Next frame: the new run has started, built its tube and posed its sphere -------
            yield return null;

            if (newPlayer != null) SetRunVisible(newPlayer.gameObject, true);
            oldPlayer.gameObject.SetActive(false);
            Destroy(oldPlayer.gameObject);
            foreach (PlayerSphere sphere in FindObjectsByType<PlayerSphere>(FindObjectsSortMode.None))
            {
                // Spheres knocked loose by a partial death belong to no player.
                if (newPlayer == null || !sphere.transform.IsChildOf(newPlayer.transform)) Destroy(sphere.gameObject);
            }

            GameHUD newHud = FindFirstObjectByType<GameHUD>();
            if (newHud != null) newHud.PlayGameplayHudEnterAnimation();

            if (newTunnel != null) FlyInTube(newTunnel, handOff.z);
            if (oldTunnel != null) StartCoroutine(SendTubeAway(oldTunnel, new Vector2(handOff.x, handOff.y)));

            isTransitioning = false;
        }

        /// <summary>Every piece of the new tube starts far down the line and eases into place,
        /// the ones nearest the sphere first, so the level builds outward from the player.</summary>
        private void FlyInTube(TunnelGenerator tunnel, float sphereZ)
        {
            List<TunnelSegment> segments = new List<TunnelSegment>(tunnel.GetComponentsInChildren<TunnelSegment>());
            segments.Sort((a, b) => Mathf.Abs(a.transform.position.z + a.length * 0.5f - sphereZ)
                          .CompareTo(Mathf.Abs(b.transform.position.z + b.length * 0.5f - sphereZ)));

            for (int i = 0; i < segments.Count; i++)
            {
                SegmentFlyIn.Arrive(segments[i], new Vector3(0f, 0f, ArriveFromDistance),
                                    i * ArriveStagger, ArriveSeconds);
            }
        }

        /// <summary>The finished level's tube swells outward past the edges of the screen, then goes.</summary>
        /// <remarks>Runs on real time, so it also plays with the world paused under the crash card.</remarks>
        private System.Collections.IEnumerator SendTubeAway(TunnelGenerator tunnel, Vector2 sphereAxis, float delay = 0f)
        {
            departingTunnel = tunnel;
            tunnel.enabled = false;     // nothing recycles a segment out from under the animation
            // Scenery from here on: nothing in it may be hit, or picked up.
            foreach (Collider c in tunnel.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            foreach (TunnelSegment segment in tunnel.GetComponentsInChildren<TunnelSegment>())
                SegmentFlyIn.Depart(segment.gameObject, sphereAxis, DepartScale, delay, DepartSeconds, unscaledTime: true);

            yield return new WaitForSecondsRealtime(delay + DepartSeconds + 0.1f);
            if (tunnel != null) Destroy(tunnel.gameObject);
            if (departingTunnel == tunnel) departingTunnel = null;
        }

        /// <summary>The pause card's Replay: clear the level away around the paused sphere, then
        /// fly it back in from rest - the crash card's sequence, minus the crash.</summary>
        private System.Collections.IEnumerator ReplayFromPause(LevelConfig again)
        {
            Time.timeScale = 0f;    // stays still while it clears; the hand-off restarts time

            TunnelGenerator tunnel = FindFirstObjectByType<TunnelGenerator>();
            PlayerController pc = PlayerController.Instance;
            if (tunnel != null && pc != null)
            {
                Vector3 axis = pc.transform.position;
                yield return StartCoroutine(SendTubeAway(tunnel, new Vector2(axis.x, axis.y)));
                // Let the destroyed tube actually go before the hand-off looks for tubes.
                yield return null;
            }

            yield return StartCoroutine(TransitionToRun(again, currentSphereCount, fromRest: true));
        }

        /// <summary>A tube already on its way out (cleared after a crash), if any.</summary>
        private TunnelGenerator departingTunnel;

        /// <summary>Matches the crash card's own beat (GameHUD.GameOverBeat), so the level
        /// clears away as the card comes down.</summary>
        private const float CrashClearDelay = 0.35f;

        private static void SetRunVisible(GameObject root, bool visible)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            foreach (Light l in root.GetComponentsInChildren<Light>(true)) l.enabled = visible;
        }

        /// <summary>Everything GameSetup.StartGame creates per run. Tunnel segments, environments
        /// and landmark bodies are cleared by StartGame itself.</summary>
        private void TearDownRun()
        {
            foreach (PlayerController p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                Destroy(p.gameObject);
            // Spheres knocked loose by a partial death are no longer under the player.
            foreach (PlayerSphere sphere in FindObjectsByType<PlayerSphere>(FindObjectsSortMode.None))
                Destroy(sphere.gameObject);
            foreach (GameHUD h in FindObjectsByType<GameHUD>(FindObjectsSortMode.None))
                Destroy(h.gameObject);
            foreach (TunnelGenerator g in FindObjectsByType<TunnelGenerator>(FindObjectsSortMode.None))
                Destroy(g.gameObject);
            foreach (FTUEManager f in FindObjectsByType<FTUEManager>(FindObjectsSortMode.None))
                Destroy(f.gameObject);
        }

        /// <summary>What a scene reload used to reset for free.</summary>
        private void ResetRunState()
        {
            Time.timeScale = 1f;
            IsGameOver = false;
            IsLevelComplete = false;
            LastResult = null;
            CoinsSpawned = 0;
            ShieldsSpawned = 0;
            ShieldsPassed = 0;
            SpheresLost = 0;
            hasSavedCoinsThisSession = false;
            shouldReplayOnLoad = false;
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

        /// <summary>A checkpoint was just banked; let the HUD say so.</summary>
        public void NotifyCheckpoint()
        {
            GameHUD hud = FindFirstObjectByType<GameHUD>();
            if (hud != null) hud.ShowCheckpointToast();
        }

        public void GameOver()
        {
            if (IsGameOver || IsLevelComplete) return;
            IsGameOver = true;

            // A repeated wall in Progression Test 1 eases the level slightly on the next attempt.
            if (Progression.ProgressionV2.IsProgressionLevel(currentLevelConfig))
            {
                Progression.ProgressionSave.RecordDeath(
                    Progression.ProgressionV2.DisplayNumber(currentLevelConfig));
            }

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

            // The crashed level clears away as the card comes down, leaving the sphere on its own,
            // so a Replay only has the new level to bring in.
            TunnelGenerator tunnel = FindFirstObjectByType<TunnelGenerator>();
            PlayerController pc = PlayerController.Instance;
            if (tunnel != null && pc != null)
            {
                Vector3 axis = pc.transform.position;
                StartCoroutine(SendTubeAway(tunnel, new Vector2(axis.x, axis.y), CrashClearDelay));
            }
        }

        public void TriggerReplay()
        {
            SaveSessionCoins();

            // From the review card the replay is a fresh run of the level just beaten, built in
            // place like the next level is. It starts from the top: a checkpoint banked on the
            // way to a win is not where anyone expects "replay" to begin.
            if (IsLevelComplete && currentLevelConfig != null)
            {
                lastSphereCount = currentSphereCount;
                LevelConfig again = currentLevelConfig;
                if (Progression.ProgressionV2.IsProgressionLevel(currentLevelConfig))
                    again = Progression.ProgressionV2.CreateLevel(Progression.ProgressionV2.DisplayNumber(currentLevelConfig));
                again.startZ = 0f;
                lastLevelConfig = again;

                if (AdMobManager.Instance != null)
                    AdMobManager.Instance.ProcessGameOverAd(() => ContinueInPlace(again));
                else
                    ContinueInPlace(again);
                return;
            }

            // Cache active setup settings to static state to trigger direct startup on load
            lastSphereCount = currentSphereCount;
            lastLevelConfig = currentLevelConfig;

            // A progression retry is rebuilt rather than reused, so the death just recorded can
            // take effect through the assist. The banked checkpoint carries across unchanged.
            if (Progression.ProgressionV2.IsProgressionLevel(currentLevelConfig))
            {
                float bankedZ = currentLevelConfig.startZ;
                lastLevelConfig = Progression.ProgressionV2.CreateLevel(
                    Progression.ProgressionV2.DisplayNumber(currentLevelConfig));
                lastLevelConfig.startZ = bankedZ;
            }

            // Paused mid-run (the pause card's Replay): the same as after a crash - the level
            // clears away with the world still paused, then flies back in. The tutorial keeps
            // its reload: it has no countdown or approach to hand over to.
            bool paused = !IsGameOver && !IsLevelComplete && Time.timeScale == 0f;
            bool tutorial = FTUEManager.Instance != null;
            if (paused && !tutorial && GameSetup.Instance != null && !isTransitioning)
            {
                LevelConfig again = lastLevelConfig;
                isTransitioning = true;
                System.Action go = () => StartCoroutine(ReplayFromPause(again));
                if (AdMobManager.Instance != null) AdMobManager.Instance.ProcessGameOverAd(go);
                else go();
                return;
            }

            // After a crash the retry flies in like a next level does, from where the sphere
            // stopped.
            if (IsGameOver)
            {
                LevelConfig again = lastLevelConfig;
                if (AdMobManager.Instance != null)
                    AdMobManager.Instance.ProcessGameOverAd(() => ContinueInPlace(again));
                else
                    ContinueInPlace(again);
                return;
            }

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
