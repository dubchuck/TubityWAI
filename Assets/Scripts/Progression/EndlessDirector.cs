using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// Runs an endless mode: ramps the player's forward speed along the mode's curve, and in
    /// Pure Flow steers difficulty from how the player is actually coping.
    ///
    /// The coping signal is deliberately not deaths - a run only has one of those, far too late
    /// to be useful. Instead the director measures <b>how close to the safe line the player was
    /// when each ring went past</b>. Consistently centred means there is room for more; scraping
    /// through means back off. That is a live read on the flow band rather than a post-mortem.
    ///
    /// It installs itself at load and stays dormant until an endless config is running, so no
    /// other system needs to know it exists.
    /// </summary>
    public class EndlessDirector : MonoBehaviour
    {
        public static EndlessDirector Instance { get; private set; }

        private EndlessMode mode;
        private LevelComposer composer;
        private int ringCursor;
        private float marginSum;
        private int marginCount;
        private float nextEvalZ;
        private bool recorded;

        /// <summary>Rings sampled before the bias is allowed to move again.</summary>
        private const int EvalWindow = 14;
        private const float BiasMin = -0.18f;
        private const float BiasMax = 0.12f;

        /// <summary>Margin above which the player is coasting, and below which they are scraping.</summary>
        private const float ComfortableMargin = 0.72f;
        private const float StrainedMargin = 0.48f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;
            GameObject host = new GameObject("EndlessDirector");
            Instance = host.AddComponent<EndlessDirector>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            EndlessMode running = ModeFor(config);

            if (running == null)
            {
                if (mode != null) Clear();
                return;
            }

            if (running != mode) Bind(running, config);

            PlayerController pc = PlayerController.Instance;
            if (pc == null) return;

            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                RecordRun(pc);
                return;
            }

            float z = pc.zPos;

            // 1. Follow the mode's speed ramp.
            pc.forwardSpeed = mode.SpeedAt(z);

            // 2. Pure Flow only: watch how the player is clearing rings and nudge the band.
            if (mode.adaptive) Sample(pc, z);
        }

        private static EndlessMode ModeFor(LevelConfig config)
        {
            if (config == null || !config.HasComposer) return null;
            foreach (EndlessMode m in EndlessMode.All)
                if (m.LevelNumber == config.levelNumber) return m;
            return null;
        }

        private void Bind(EndlessMode m, LevelConfig config)
        {
            mode = m;
            composer = config.composer;
            ringCursor = 0;
            marginSum = 0f;
            marginCount = 0;
            nextEvalZ = 0f;
            recorded = false;

            // Carry over what previous runs learned about this player, damped so one good run
            // does not make the next one unplayable.
            mode.adaptiveBias = mode.adaptive ? CarriedBias(m.id) : 0f;
        }

        private void Clear()
        {
            mode = null;
            composer = null;
            recorded = false;
        }

        /// <summary>
        /// For every ring the player has just passed, score how centred they were on the safe
        /// line, then move the bias once per window.
        /// </summary>
        private void Sample(PlayerController pc, float z)
        {
            if (composer == null) return;

            var composed = composer.Composed;
            float playerAngle = pc.currentAngle * Mathf.Rad2Deg;

            while (ringCursor < composed.Count && composed[ringCursor].z < z)
            {
                RingSpec ring = composed[ringCursor];
                ringCursor++;

                // Jump walls have no safe angle to be centred on.
                if (ring.arcs.Length == 1 && ring.arcs[0].arcAngleDeg > 300f) continue;
                // A turning ring's safe angle moves; the recorded one is no longer where it was.
                if (ring.motion != RingArcGroup.Motion.Static) continue;

                float error = Mathf.Abs(ActionCost.ShortestDelta(playerAngle, ring.safeAngleDeg));
                marginSum += 1f - Mathf.Clamp01(error / 90f);
                marginCount++;
            }

            if (marginCount < EvalWindow || z < nextEvalZ) return;

            float avg = marginSum / marginCount;
            if (avg > ComfortableMargin) mode.adaptiveBias += 0.025f;
            else if (avg < StrainedMargin) mode.adaptiveBias -= 0.04f;

            mode.adaptiveBias = Mathf.Clamp(mode.adaptiveBias, BiasMin, BiasMax);

            marginSum = 0f;
            marginCount = 0;
            nextEvalZ = z + 400f;
        }

        private void RecordRun(PlayerController pc)
        {
            if (recorded || mode == null) return;
            recorded = true;

            float distance = pc.zPos;
            int score = Mathf.RoundToInt(distance) + pc.Coins * 10;
            ProgressionSave.RecordEndlessRun(mode.id, score, distance);

            if (mode.adaptive) StoreBias(mode.id, mode.adaptiveBias);
        }

        // ---- Between-run adaptation -----------------------------------------------------------

        private const string BiasKey = "P2_EndlessBias_";

        private static float CarriedBias(string modeId)
        {
            // Half of what the last run settled on, so the mode always re-reads the player.
            return Mathf.Clamp(PlayerPrefs.GetFloat(BiasKey + modeId, 0f) * 0.5f, BiasMin, BiasMax);
        }

        private static void StoreBias(string modeId, float bias)
        {
            PlayerPrefs.SetFloat(BiasKey + modeId, Mathf.Clamp(bias, BiasMin, BiasMax));
            PlayerPrefs.Save();
        }
    }
}
