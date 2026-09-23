using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// Which gameplay mechanics a level is allowed to use. The 128-level ladder turns these
    /// on one world at a time; the phrase library tags every phrase with what it needs, so a
    /// phrase can only be dealt into a level whose flags cover it. The world comments follow
    /// Documentation/Level-Progression-Spec.md; the values are never renumbered.
    /// </summary>
    [System.Flags]
    public enum Mechanic
    {
        None        = 0,
        Steer       = 1 << 0,   // world 1  - single arcs, lanes
        Jump        = 1 << 1,   // world 2  - hurdles
        Colour      = 1 << 2,   // world 10 - colour shields (needs two spheres to mean anything)
        Spin        = 1 << 3,   // world 4
        Pinch       = 1 << 4,   // world 8  - narrow doors (the tube itself never narrows)
        Snap        = 1 << 5,   // world 7
        GateRing    = 1 << 6,   // world 8  - full ring, single gap
        MovingGap   = 1 << 7,   // world 5  - pendulum + travelling gap
        Compound    = 1 << 8,   // world 6  - deep walls, stacked rings
        ColourShift = 1 << 9,   // world 12
        Drift       = 1 << 10,  // world 11 - true curvature
        Corridor    = 1 << 11,  // world 8  - sustained holds
        Spiral      = 1 << 12,  // world 13 - rolling frame
        Reactive    = 1 << 13,  // world 14 - arcs that chase the player
        LowPreview  = 1 << 14,  // world 15 - darkness / short sight line
        Shapes      = 1 << 15,  // world 3  - towers, wide arcs, doors, blades
        Crossover   = 1 << 16,  // world 6  - double-tap jump to the far side (phase 5)
        Rails       = 1 << 17,  // world 8  - long arcs ridden beside (phase 5)
        Twins       = 1 << 18,  // world 9  - two spheres (phase 4)
        All         = ~0
    }

    /// <summary>
    /// Everything one level needs, in the units the composer thinks in. Built by ProgressionV2
    /// for campaign levels and by EndlessDirector for endless runs, then handed to LevelComposer.
    /// </summary>
    public class ProgressionDials
    {
        // ---- Kinematics -------------------------------------------------------------------
        public int level = 1;
        public float speed = 12f;
        /// <summary>The grid obstacles are placed on. Scales with speed so the ring cadence stays
        /// roughly constant in seconds - this is what stops the reaction window collapsing, and it
        /// is the unit the whole pressure model is denominated in, so it is not a look-and-feel dial.</summary>
        public float markerInterval = 5f;

        /// <summary>
        /// The furthest apart two drawn marker rings may sit, as a multiple of the obstacle grid.
        /// A ring is always drawn under every arc; this only controls how often a filler ring is
        /// added across an empty stretch to keep a sense of speed. Raising it thins the rings out.
        /// Purely visual - obstacle spacing, and so difficulty, is untouched by it.
        /// </summary>
        public float markerRingScale = 2.5f;

        /// <summary>Widest gap allowed between drawn marker rings.</summary>
        public float MarkerRingSpacing { get { return markerInterval * Mathf.Max(1f, markerRingScale); } }
        public float angularSpeed = 4f;
        public float tubeRadius = 5f;
        /// <summary>Target play time; the composer lays down phrases until it is filled.</summary>
        public float levelSeconds = 50f;

        // ---- Flow band --------------------------------------------------------------------
        /// <summary>Pressure the level idles at between features.</summary>
        public float restPressure = 0.25f;
        /// <summary>Bottom and top of the band the level spends most of its time in.</summary>
        public float bandFloor = 0.45f;
        public float bandCeiling = 0.70f;
        /// <summary>Pressure at an intensity peak. Never exceeds HardCeiling.</summary>
        public float spikePressure = 0.88f;
        /// <summary>Absolute limit. The composer clamps to this and the validator enforces it.</summary>
        public const float HardCeiling = 0.95f;

        // ---- Content ----------------------------------------------------------------------
        public Mechanic mechanics = Mechanic.Steer;
        /// <summary>Fraction of arcs that are colour shields rather than solid hazards.</summary>
        public float colourShare = 0f;
        /// <summary>Fraction of rings that carry a motion behaviour.</summary>
        public float motionShare = 0f;
        /// <summary>0..1 scales spin rate, snap frequency and pendulum speed together.</summary>
        public float motionIntensity = 0f;
        /// <summary>0..1 scales arc depth along the tube (deep walls need a timed jump).</summary>
        public float depthIntensity = 0f;
        /// <summary>Multiplies every telegraph window. Above 1 is a kindness, below 1 a threat.</summary>
        public float telegraphScale = 1f;

        /// <summary>Degrees the obstacle field rolls per tube unit (world 13's corkscrew).</summary>
        public float spiralDegPerUnit = 0f;

        /// <summary>Radians per second the tube's curvature drags the player outward (world 11).</summary>
        public float driftStrength = 0f;

        /// <summary>Scales the level's fog distance; below 1 shortens the sight line (world 15).</summary>
        public float previewScale = 1f;

        /// <summary>Spheres the level is designed for. Zero means "whatever the player brought"
        /// (endless modes); the ladder sets it, and the composer clears every arc for all of them.</summary>
        public int sphereCount = 0;

        /// <summary>Amplitude of the tube's cosmetic sway, in world units. Zero is a straight tube;
        /// drift levels curve regardless.</summary>
        public float curveAmplitude = 0f;

        // ---- Deck -------------------------------------------------------------------------
        /// <summary>A hand-authored level names its figures here, with weights. Null deals from
        /// every phrase the mechanic flags allow, as the formula worlds do.</summary>
        public string[] phraseIds;
        public float[] phraseWeights;

        /// <summary>Phrases that use this mechanic are dealt more often, so a world's new thing
        /// is actually the thing its levels are about rather than one card in thirty.</summary>
        public Mechanic featured = Mechanic.None;
        public float featuredWeight = 4f;

        // ---- Presentation -----------------------------------------------------------------
        public EnvironmentTheme environment = EnvironmentTheme.None;
        public string worldName = "";
        public string levelName = "";
        public int seed = 0;

        public bool Has(Mechanic m) { return (mechanics & m) != 0; }

        /// <summary>Distance this level covers, derived from its target duration.</summary>
        public float LengthUnits { get { return Mathf.Round(speed * levelSeconds); } }

        public ProgressionDials Clone()
        {
            return (ProgressionDials)MemberwiseClone();
        }
    }
}
