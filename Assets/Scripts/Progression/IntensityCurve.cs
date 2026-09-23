using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// The shape of a level's demand over its length, expressed as a target pressure.
    ///
    ///     warm-up   0.00 - 0.12   rest rising to the band floor - read the room
    ///     build     0.12 - 0.45   floor to ceiling, breathing every few phrases
    ///     peak      0.45 - 0.75   ceiling with spikes, each paid back by a rest
    ///     release   0.75 - 0.88   back to the floor
    ///     finale    0.88 - 1.00   one authored spike, then clear air to the gate
    ///
    /// A flat difficulty reads as monotonous no matter how high it is set; the breathing is
    /// what keeps a player in flow rather than grinding them down or boring them.
    /// </summary>
    public static class IntensityCurve
    {
        /// <summary>Below this many seconds a level is too short for the five-part shape: a 12%
        /// warm-up of a 20-second level is gone before it registers.</summary>
        public const float ShortLevelSeconds = 35f;

        /// <summary>Target pressure at normalised progress t through the level.</summary>
        public static float Target(float t, ProgressionDials d)
        {
            t = Mathf.Clamp01(t);
            if (d.levelSeconds > 0f && d.levelSeconds < ShortLevelSeconds) return ShortTarget(t, d);

            float floor = d.bandFloor;
            float ceil = d.bandCeiling;
            float rest = d.restPressure;
            float spike = Mathf.Min(d.spikePressure, ProgressionDials.HardCeiling);

            float macro;
            if (t < 0.12f)
            {
                macro = Mathf.Lerp(rest, floor, Smooth(t / 0.12f));
            }
            else if (t < 0.45f)
            {
                macro = Mathf.Lerp(floor, ceil, Smooth((t - 0.12f) / 0.33f));
            }
            else if (t < 0.75f)
            {
                macro = ceil;
            }
            else if (t < 0.88f)
            {
                macro = Mathf.Lerp(ceil, floor, Smooth((t - 0.75f) / 0.13f));
            }
            else if (t < 0.97f)
            {
                // Finale: a single authored surge, the level's last word.
                macro = Mathf.Lerp(floor, spike, Smooth((t - 0.88f) / 0.09f));
            }
            else
            {
                // Clean air into the finish gate so the run ends on a win, not a scramble.
                macro = Mathf.Lerp(spike, rest, Smooth((t - 0.97f) / 0.03f));
            }

            // Micro breathing: roughly six cycles per level, so the player feels waves rather
            // than a wall. Amplitude shrinks near the ends where the macro shape already moves.
            float envelope = Mathf.Sin(t * Mathf.PI);
            float wave = Mathf.Sin(t * Mathf.PI * 12f) * 0.06f * envelope;

            return Mathf.Clamp(macro + wave, 0.12f, ProgressionDials.HardCeiling);
        }

        /// <summary>
        /// A short level's shape, in seconds rather than fractions:
        ///
        ///     intro      first 3 s of rings   rest rising to the floor - the hook figure, gently
        ///     body       until 4 s from end   floor easing to the ceiling, with light breathing
        ///     flourish   last 4 s of rings    one burst at the spike, then the run-out to the gate
        ///
        /// The run-in before the first ring and the run-out after the last are the composer's.
        /// </summary>
        private static float ShortTarget(float t, ProgressionDials d)
        {
            float seconds = d.levelSeconds;
            float speed = Mathf.Max(1f, d.speed);
            float s = t * seconds;

            // Where rings start and stop, matching LevelComposer.Begin and RunOutUnits.
            float first = Mathf.Clamp(speed * 1.5f, 30f, 75f) / speed;
            float last = seconds - LevelComposer.RunOutUnits / speed;
            float introEnd = first + 3f;
            float flourishStart = Mathf.Max(introEnd + 2f, last - 4f);

            float rest = d.restPressure, floor = d.bandFloor, ceil = d.bandCeiling;
            float spike = Mathf.Min(d.spikePressure, ProgressionDials.HardCeiling);

            float target;
            if (s < introEnd)
            {
                target = Mathf.Lerp(rest, floor, Smooth((s - first) / 3f));
            }
            else if (s < flourishStart)
            {
                float k = (s - introEnd) / Mathf.Max(0.5f, flourishStart - introEnd);
                target = Mathf.Lerp(floor, ceil, Smooth(k)) + Mathf.Sin(k * Mathf.PI * 4f) * 0.03f;
            }
            else
            {
                target = spike;
            }
            return Mathf.Clamp(target, 0.12f, ProgressionDials.HardCeiling);
        }

        /// <summary>True where the level should be handing back a rest regardless of the target.</summary>
        public static bool IsQuietZone(float t)
        {
            return t < 0.06f || (t > 0.97f);
        }

        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
