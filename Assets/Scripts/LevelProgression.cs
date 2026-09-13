using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Builds the campaign level list. Difficulty ramps on two axes:
    ///
    ///  * Continuous knobs (forward speed, obstacle density, solid-vs-shield ratio, arc
    ///    height) interpolate smoothly from level 1 to the last level.
    ///  * Mechanics unlock in three-level tiers so each tier teaches one new skill:
    ///
    ///      1-3   BASICS            single narrow arcs, mostly colour shields, slow
    ///      4-6   SOLID WALLS       more solid hazards, wider arcs, occasional 2-arc rings
    ///      7-9   SPINNERS          ring groups that rotate at a fixed rate
    ///     10-12  SNAP TURNS        rings that click 90 degrees on a timer; the tube starts to wind
    ///     13-15  TRIPLE RINGS      up to 3 arcs per ring, faster spins, 180 degree snaps
    ///     16-18  DEEP WALLS        arcs get thick along the tube (timed jumps) and pendulum rings appear
    ///     19-21  SHIFTING SHIELDS  colour shields change the colour they accept
    ///     22-24  GAUNTLET          everything at once, top speed, tight curves
    /// </summary>
    public static class LevelProgression
    {
        public const int CampaignLevelCount = 24;

        public static List<LevelConfig> CreateCampaign()
        {
            var list = new List<LevelConfig>(CampaignLevelCount);
            for (int n = 1; n <= CampaignLevelCount; n++)
            {
                list.Add(CreateCampaignLevel(n));
            }
            return list;
        }

        public static LevelConfig CreateCampaignLevel(int level)
        {
            int n = Mathf.Clamp(level, 1, CampaignLevelCount);
            float t = (n - 1) / (float)(CampaignLevelCount - 1);

            // ---- Continuous ramps -------------------------------------------------
            float speed = Mathf.Lerp(10f, 36f, t);
            float obstacleProbability = Mathf.Lerp(0.20f, 0.70f, t);
            float boost = Mathf.Lerp(1.8f, 2.5f, t);

            RingDifficulty rings = new RingDifficulty();
            rings.solidChance = Mathf.Lerp(0.25f, 0.65f, t);
            rings.minHeightFraction = Mathf.Lerp(0.10f, 0.20f, t);
            rings.maxHeightFraction = Mathf.Lerp(0.35f, 0.60f, t);
            rings.minGapDegrees = 40f;

            // ---- Arc layout tiers -------------------------------------------------
            if (n <= 5)       { rings.minArcsPerRing = 1; rings.maxArcsPerRing = 1; }
            else if (n <= 12) { rings.minArcsPerRing = 1; rings.maxArcsPerRing = 2; }
            else if (n <= 19) { rings.minArcsPerRing = 1; rings.maxArcsPerRing = 3; }
            else              { rings.minArcsPerRing = 2; rings.maxArcsPerRing = 3; }

            if (n <= 3)       rings.arcAngleChoices = new[] { 45f, 60f, 90f };
            else if (n <= 9)  rings.arcAngleChoices = new[] { 60f, 90f, 120f };
            else if (n <= 17) rings.arcAngleChoices = new[] { 60f, 90f, 120f, 150f };
            else              rings.arcAngleChoices = new[] { 90f, 120f, 150f };

            // Deep walls (thick along the tube axis) from level 16. Capped at 2.0 so two
            // consecutive marker rings (5 apart) never merge into one continuous wall.
            rings.minDepth = 0.4f;
            rings.maxDepth = n >= 16 ? Mathf.Lerp(0.4f, 2.0f, Ramp(n, 16, CampaignLevelCount)) : 0.4f;

            // ---- Motion unlocks ---------------------------------------------------
            // Every motion type gets both more common and faster with level: spin rate roughly
            // triples, snaps come twice as often and turn quicker, pendulums swing wider and faster.
            if (n >= 7)
            {
                float r = Ramp(n, 7, CampaignLevelCount);
                rings.spinChance = Mathf.Lerp(0.20f, 0.35f, r);
                rings.minSpinDegPerSec = Mathf.Lerp(25f, 80f, r);
                rings.maxSpinDegPerSec = Mathf.Lerp(50f, 150f, r);
            }
            if (n >= 10)
            {
                float r = Ramp(n, 10, CampaignLevelCount);
                rings.snapChance = Mathf.Lerp(0.15f, 0.25f, r);
                rings.snapInterval = Mathf.Lerp(2.2f, 1.0f, r);
                rings.snapTurnDuration = Mathf.Lerp(0.25f, 0.12f, r);
                rings.snapTelegraphWindow = Mathf.Lerp(0.9f, 0.6f, r);
                rings.snap180Chance = n >= 13 ? 0.5f : 0f;
            }
            if (n >= 16)
            {
                float r = Ramp(n, 16, CampaignLevelCount);
                rings.oscillateChance = Mathf.Lerp(0.15f, 0.20f, r);
                rings.oscillateAmplitudeDeg = Mathf.Lerp(45f, 90f, r);
                rings.oscillatePeriod = Mathf.Lerp(2.4f, 1.2f, r);
            }
            if (n >= 19)
            {
                float r = Ramp(n, 19, CampaignLevelCount);
                rings.colorShiftChance = Mathf.Lerp(0.30f, 0.50f, r);
                rings.colorShiftInterval = Mathf.Lerp(1.6f, 1.1f, r);
            }

            // ---- Tube curvature ---------------------------------------------------
            bool curves = n >= 10;
            float curveFreq = 0.03f, curveAmp = 1.5f;
            if (n >= 22)      { curveFreq = 0.045f; curveAmp = 3.5f; }
            else if (n >= 16) { curveFreq = 0.04f;  curveAmp = 2.5f; }

            // Level length: 45 s of travel at level 1 rising to 75 s at the last level.
            float lengthUnits = Mathf.Round(speed * Mathf.Lerp(45f, 75f, t));

            LevelConfig config = new LevelConfig(n, speed, obstacleProbability, boost,
                                                 isTest: false, name: TierName(n), theme: "",
                                                 curves: curves, curveFreq: curveFreq, curveAmp: curveAmp);
            return config.WithRings(rings).WithLength(lengthUnits);
        }

        /// <summary>0 at level 'from', 1 at level 'to', clamped.</summary>
        private static float Ramp(int n, int from, int to)
        {
            return Mathf.Clamp01((n - from) / (float)(to - from));
        }

        public static string TierName(int n)
        {
            if (n <= 3) return "BASICS";
            if (n <= 6) return "SOLID WALLS";
            if (n <= 9) return "SPINNERS";
            if (n <= 12) return "SNAP TURNS";
            if (n <= 15) return "TRIPLE RINGS";
            if (n <= 18) return "DEEP WALLS";
            if (n <= 21) return "SHIFTING SHIELDS";
            return "GAUNTLET";
        }
    }
}
