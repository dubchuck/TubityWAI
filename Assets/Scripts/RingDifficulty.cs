using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Per-level tuning for the obstacle "ring arc groups" that TunnelSegment spawns at
    /// marker rings. Every knob defaults to the classic behaviour (one static arc of
    /// 60/90/120 degrees, half of them solid) so levels that never set it play as before.
    /// </summary>
    [System.Serializable]
    public class RingDifficulty
    {
        [Header("Arc Layout")]
        [Tooltip("Fewest arcs in one ring group.")]
        public int minArcsPerRing = 1;
        [Tooltip("Most arcs in one ring group (capped at 4).")]
        public int maxArcsPerRing = 1;
        [Tooltip("Angular spans an arc may take. Multi-arc rings clamp these so gaps remain.")]
        public float[] arcAngleChoices = { 60f, 90f, 120f };
        [Tooltip("Clearance always left between neighbouring arcs of one ring, in degrees.")]
        public float minGapDegrees = 40f;
        [Tooltip("Radial height of an arc (how far it reaches in from the wall) as a fraction of the tube radius.")]
        public float minHeightFraction = 0.1f;
        public float maxHeightFraction = 0.5f;
        [Tooltip("Thickness of an arc along the tube axis. Deeper arcs need a well-timed jump to clear.")]
        public float minDepth = 0.4f;
        public float maxDepth = 0.4f;
        [Tooltip("Chance an arc is a solid hazard instead of a colour-coded shield.")]
        public float solidChance = 0.5f;

        [Header("Ring Motion (at most one applies per ring; chances are cumulative)")]
        [Tooltip("Chance the whole ring group spins at a fixed rate.")]
        public float spinChance = 0f;
        public float minSpinDegPerSec = 30f;
        public float maxSpinDegPerSec = 60f;
        [Tooltip("Chance the ring group snaps by 90 (or 180) degrees on a timer.")]
        public float snapChance = 0f;
        public float snapInterval = 2f;
        [Tooltip("How long the snap turn itself takes; shorter is harder to react to.")]
        public float snapTurnDuration = 0.2f;
        [Tooltip("Seconds before each snap during which the arcs flash and swell as a warning.")]
        public float snapTelegraphWindow = 0.7f;
        [Tooltip("Chance a snapping ring turns 180 degrees per snap instead of 90.")]
        public float snap180Chance = 0f;
        [Tooltip("Chance the ring group swings back and forth like a pendulum.")]
        public float oscillateChance = 0f;
        public float oscillateAmplitudeDeg = 45f;
        public float oscillatePeriod = 2f;

        [Header("Colour Shifting")]
        [Tooltip("Chance a colour-coded arc cycles the colour it accepts over time (needs 2+ sphere colours).")]
        public float colorShiftChance = 0f;
        public float colorShiftInterval = 1.5f;

        public static RingDifficulty Classic()
        {
            return new RingDifficulty();
        }
    }
}
