using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>One arc of a composed ring, in absolute tube degrees.</summary>
    public class ArcSpec
    {
        /// <summary>Where the arc begins, in the same angular space as PlayerController.currentAngle
        /// (0 at the bottom of the tube, increasing toward +X).</summary>
        public float startAngleDeg;
        public float arcAngleDeg;
        /// <summary>Radial reach inward from the wall, in world units.</summary>
        public float thickness;
        /// <summary>Extent along the tube axis, in world units.</summary>
        public float depth;

        public bool isColourCoded;
        public int colourIndex;
        public bool colourShift;
        public float colourShiftInterval;
        public int colourShiftSeed;
    }

    /// <summary>
    /// One fully specified obstacle ring: where it sits, what blocks it, how it moves. The
    /// composer produces these ahead of time so difficulty is known before a frame is drawn;
    /// TunnelSegment just builds what it is told.
    /// </summary>
    public class RingSpec
    {
        public float z;
        public ArcSpec[] arcs = new ArcSpec[0];

        /// <summary>Body colour for this ring's solid arcs. Consecutive rings alternate through
        /// LevelComposer.SolidTints so that a row of them reads as separate objects at depth
        /// rather than one continuous smear. Colour-coded shields ignore it and keep their own hue.</summary>
        public Color solidTint = Color.white;

        public RingArcGroup.Motion motion = RingArcGroup.Motion.Static;
        public float spinDegPerSec;
        public float snapInterval = 2f;
        public float snapAngle = 90f;
        public float snapTurnDuration = 0.2f;
        public float telegraphWindow = 0.7f;
        public float oscAmplitudeDeg = 45f;
        public float oscPeriod = 2f;
        public float oscPhase;
        public float chaseDegPerSec = 55f;
        public float chaseOffsetDeg;
        public float chaseEngageDistance = 90f;

        // ---- Diagnostics, for the validator and the HUD readout -------------------------
        /// <summary>The phrase this ring came from.</summary>
        public string phraseId = "";
        /// <summary>Seconds the required move costs.</summary>
        public float requiredSeconds;
        /// <summary>requiredSeconds divided by the seconds actually available. Never >= 1.</summary>
        public float pressure;
        /// <summary>Safe angle the optimal line takes through this ring.</summary>
        public float safeAngleDeg;
        /// <summary>No line clears the ring: it can only be jumped.</summary>
        public bool requiresJump;
        /// <summary>The optimal line jumps it (because it must, or because hopping is quicker).</summary>
        public bool jumped;
    }
}
