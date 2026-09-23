using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using TubityWAI.Progression;

namespace TubityWAI.EditorTools
{
    /// <summary>
    /// Composes all 128 levels offline and reports what they actually ask of the player.
    /// A level fails if any ring exceeds ProgressionDials.HardCeiling pressure (it cannot be
    /// cleared however well the player plays), or if it breaks the spec's teaching rules:
    /// world 1 may never need a jump, and level 1 stays a quick first win.
    /// </summary>
    public static class ProgressionValidator
    {
        [MenuItem("TubityX/Progression/Validate All 128 Levels")]
        public static void ValidateAll()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("lvl  hook                 speed  rings  dur_s  arcs  open  jumps  p_med  p_max  gap_med  gap_min");

            int failures = 0;
            float worstPressure = 0f;
            int worstLevel = 0;

            for (int n = 1; n <= ProgressionV2.LevelCount; n++)
            {
                ProgressionDials d = ProgressionV2.CreateDials(n, applyAssist: false);
                LevelComposer composer = new LevelComposer(d);
                List<RingSpec> rings = composer.Rings;

                if (rings.Count == 0)
                {
                    sb.AppendLine($"{n,3}  {d.worldName,-13}  EMPTY LEVEL");
                    failures++;
                    continue;
                }

                List<float> pressures = new List<float>(rings.Count);
                foreach (RingSpec r in rings) pressures.Add(r.pressure);
                pressures.Sort();

                List<float> gaps = new List<float>();
                for (int i = 1; i < rings.Count; i++) gaps.Add((rings[i].z - rings[i - 1].z) / d.speed);
                gaps.Sort();

                float pMed = pressures[pressures.Count / 2];
                float pMax = pressures[pressures.Count - 1];
                float gMed = gaps.Count > 0 ? gaps[gaps.Count / 2] : 0f;
                float gMin = gaps.Count > 0 ? gaps[0] : 0f;

                // Arcs per ring, how much of the tube is open, and how many rings need a jump.
                int arcTotal = 0, jumps = 0;
                float openTotal = 0f;
                foreach (RingSpec r in rings)
                {
                    arcTotal += r.arcs.Length;
                    float covered = 0f;
                    foreach (ArcSpec a in r.arcs) covered += a.arcAngleDeg;
                    openTotal += Mathf.Clamp01(1f - covered / 360f);
                    if (r.requiresJump) jumps++;
                }
                float arcsPerRing = arcTotal / (float)rings.Count;
                float openFrac = openTotal / rings.Count;
                float seconds = d.LengthUnits / d.speed;

                if (pMax > worstPressure) { worstPressure = pMax; worstLevel = n; }

                string flag = "";
                if (pMax > ProgressionDials.HardCeiling) flag += "  <-- OVER CEILING";
                if (ProgressionV2.WorldOf(n).index == 1 && jumps > 0) flag += "  <-- JUMP IN WORLD 1";
                if (n == 1 && seconds > 22f) flag += "  <-- LEVEL 1 TOO LONG";
                if (flag.Length > 0) failures++;

                sb.AppendLine($"{n,3}  {d.levelName,-19}  {d.speed,5:F1}  {rings.Count,5}  {seconds,5:F0}  " +
                              $"{arcsPerRing,4:F1}  {openFrac,4:F2}  {jumps,5}  " +
                              $"{pMed,5:F2}  {pMax,5:F2}  {gMed,7:F2}  {gMin,7:F2}{flag}");
            }

            Debug.Log(sb.ToString());

            if (failures > 0)
                Debug.LogError($"[ProgressionValidator] {failures} level(s) fail (see flags above). " +
                               $"Highest pressure: level {worstLevel} at {worstPressure:F2}.");
            else
                Debug.Log($"[ProgressionValidator] All {ProgressionV2.LevelCount} levels pass. " +
                          $"Highest pressure anywhere: level {worstLevel} at {worstPressure:F2} " +
                          $"(ceiling {ProgressionDials.HardCeiling:F2}).");
        }

        [MenuItem("TubityX/Progression/Dump Level Layout...")]
        public static void DumpOne()
        {
            int level = Mathf.Clamp(EditorPrefs.GetInt("TubityX_DumpLevel", 1), 1, ProgressionV2.LevelCount);
            ProgressionDials d = ProgressionV2.CreateDials(level, applyAssist: false);
            LevelComposer composer = new LevelComposer(d);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Level {level} - {d.levelName} ({d.worldName}), {d.environment}");
            sb.AppendLine($"speed {d.speed:F1}  interval {d.markerInterval:F2}  length {d.LengthUnits:F0}  " +
                          $"band {d.bandFloor:F2}-{d.bandCeiling:F2}  spike {d.spikePressure:F2}");
            sb.AppendLine($"mechanics: {d.mechanics}");
            sb.AppendLine("   z      phrase        arcs  safe    req_s  pressure  answer  motion");

            foreach (RingSpec r in composer.Rings)
            {
                string answer = r.requiresJump ? "JUMP" : r.jumped ? "hop" : "steer";
                sb.AppendLine($"{r.z,7:F1}  {r.phraseId,-12}  {r.arcs.Length,4}  {r.safeAngleDeg,5:F0}  " +
                              $"{r.requiredSeconds,6:F2}  {r.pressure,8:F2}  {answer,-6}  {r.motion}");
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("TubityX/Progression/Unlock All Progression Levels")]
        public static void UnlockAll()
        {
            ProgressionSave.UnlockAll();
            Debug.Log("[ProgressionValidator] All 128 progression levels unlocked.");
        }

        [MenuItem("TubityX/Progression/Reset Progression Progress")]
        public static void ResetAll()
        {
            ProgressionSave.ResetAll();
            Debug.Log("[ProgressionValidator] Progression Test 1 progress reset.");
        }
    }
}
