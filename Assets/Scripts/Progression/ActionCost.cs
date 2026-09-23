using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// Converts "what this ring asks the player to do" into seconds, and seconds into
    /// <b>pressure</b> - the one number the whole 128-level ladder is tuned against.
    ///
    ///     pressure = seconds the action needs / seconds the player actually has
    ///
    /// Pressure at or above 1.0 is unsurvivable by construction: the move cannot be
    /// finished before the ring arrives. The composer never emits such a ring, and
    /// ProgressionValidator fails the build if one ever appears.
    ///
    /// Reference costs at the shipping tuning (angularSpeed 4 rad/s, jumpDuration 0.6 s):
    ///     hold          0.00 s      quarter lap   0.39 s
    ///     45 degrees    0.20 s      half lap      0.79 s
    ///     jump          0.60 s      colour match  move + 0.25 s
    /// </summary>
    public static class ActionCost
    {
        /// <summary>Read the ring, pick a gap, commit. Paid once per ring, on top of the move.</summary>
        public const float DecisionLatency = 0.18f;

        /// <summary>A jump occupies the player for its full arc; it cannot be cut short.</summary>
        public const float JumpSeconds = 0.6f;

        /// <summary>Extra reading time when the gap is a colour shield rather than an open gap.</summary>
        public const float ColourRecognition = 0.25f;

        /// <summary>Extra reading time when the ring is moving and the gap has to be led.</summary>
        public const float MotionRecognition = 0.2f;

        /// <summary>Seconds to steer through an angle at the player's angular speed.</summary>
        public static float SteerSeconds(float deltaDeg, float angularSpeed)
        {
            return (Mathf.Abs(deltaDeg) * Mathf.Deg2Rad) / Mathf.Max(0.1f, angularSpeed);
        }

        /// <summary>Signed shortest way round from a to b, in degrees, in [-180, 180].</summary>
        public static float ShortestDelta(float fromDeg, float toDeg)
        {
            float d = Mathf.Repeat(toDeg - fromDeg + 180f, 360f) - 180f;
            return d;
        }

        /// <summary>Seconds available to cross `markers` marker rings at this speed.</summary>
        public static float AvailableSeconds(int markers, float markerInterval, float speed)
        {
            return (markers * markerInterval) / Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// The fewest whole marker rings that keep this action at or under `targetPressure`.
        /// This is the composer's core move: rather than hoping a random layout is fair, it
        /// spaces every ring by exactly what the required action costs.
        /// </summary>
        public static int MarkersNeeded(float requiredSeconds, float targetPressure,
                                        float markerInterval, float speed)
        {
            float target = Mathf.Clamp(targetPressure, 0.05f, 0.95f);
            float secondsAllowed = requiredSeconds / target;
            float markers = (secondsAllowed * Mathf.Max(0.1f, speed)) / Mathf.Max(0.1f, markerInterval);
            return Mathf.Max(1, Mathf.CeilToInt(markers - 0.0001f));
        }

        public static float Pressure(float requiredSeconds, int markers, float markerInterval, float speed)
        {
            float available = AvailableSeconds(markers, markerInterval, speed);
            return available <= 0f ? 99f : requiredSeconds / available;
        }

        // ---- Clearing arcs -------------------------------------------------------------------

        /// <summary>Half the angle a sphere covers on the wall, with its trigger's slack.
        /// A sphere of radius 0.5 riding at 4.5 subtends about 6.4 degrees either side.</summary>
        public const float SphereHalfDeg = 8f;

        /// <summary>Extra room the optimal line keeps from an arc edge. Players do not graze
        /// arcs on purpose, so costing the move to the bare edge would under-price it.</summary>
        public const float ComfortDeg = 14f;

        /// <summary>
        /// The smallest signed rotation from `lineDeg` that puts every sphere clear of every
        /// arc, counting an arc in a sphere's own colour as open for that sphere. Spheres sit evenly round the tube (line + k * 360 / spheres), as
        /// PlayerController lays them out. Tries a comfortable clearance first and falls back
        /// to the bare minimum, so a tight but fair ring is still found. False means no line
        /// clears the ring at all - it has to be jumped.
        /// </summary>
        public static bool NearestClearDelta(IList<ArcSpec> arcs, float lineDeg, int spheres, out float delta)
        {
            if (NearestClearDelta(arcs, lineDeg, spheres, SphereHalfDeg + ComfortDeg, out delta)) return true;
            return NearestClearDelta(arcs, lineDeg, spheres, SphereHalfDeg, out delta);
        }

        public static bool NearestClearDelta(IList<ArcSpec> arcs, float lineDeg, int spheres,
                                             float clearanceDeg, out float delta)
        {
            int n = Mathf.Max(1, spheres);
            float step = 360f / n;
            // Search the whole way round. Identical spheres would repeat every `step`, but
            // colours tell them apart: a half turn that swaps two twins into their own colours
            // is a different answer from holding still.
            const int reach = 180;

            for (int k = 0; k <= reach; k++)
            {
                if (IsClear(arcs, lineDeg + k, n, step, clearanceDeg)) { delta = k; return true; }
                if (k > 0 && IsClear(arcs, lineDeg - k, n, step, clearanceDeg)) { delta = -k; return true; }
            }
            delta = 0f;
            return false;
        }

        private static bool IsClear(IList<ArcSpec> arcs, float lineDeg, int spheres, float step, float clearanceDeg)
        {
            for (int s = 0; s < spheres; s++)
            {
                float a = lineDeg + s * step;
                for (int i = 0; i < arcs.Count; i++)
                {
                    ArcSpec arc = arcs[i];
                    // A shield in this sphere's own colour is open air to it (Obstacle lets the
                    // matching sphere pass through), and a wall to every other sphere.
                    if (arc.isColourCoded && arc.colourIndex == s) continue;
                    float from = arc.startAngleDeg - clearanceDeg;
                    if (Mathf.Repeat(a - from, 360f) <= arc.arcAngleDeg + 2f * clearanceDeg) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Seconds a jump over this ring costs. Taller arcs shrink the window in which the sphere
        /// is inward of them, and deeper ones take longer to pass under, so both are a timing tax
        /// on top of the jump itself.
        /// </summary>
        public static float JumpOverSeconds(float heightFrac, float depth, float speed)
        {
            float heightTax = Mathf.Max(0f, heightFrac - 0.15f) * 0.5f;
            float depthTax = Mathf.Max(0f, depth) / Mathf.Max(1f, speed);
            return JumpSeconds + DecisionLatency + heightTax + depthTax;
        }
    }
}
