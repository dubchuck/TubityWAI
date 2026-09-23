using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>How a slot's ring behaves. A phrase may demand one; otherwise the composer
    /// picks from the level's motion budget.</summary>
    public enum MotionHint { Inherit, Static, Spin, Snap, Oscillate, Chase }

    /// <summary>
    /// Where a blocker slot's arcs sit around the tube, relative to the player's line as the
    /// composer has it when the ring is laid down. This is what turns "an arc somewhere" into
    /// "an arc that asks for a move", and so what lets the opening levels teach steering
    /// without ever needing a jump.
    /// </summary>
    public enum Placement
    {
        /// <summary>Offset 0 on the line (a little jitter): an arc there asks for a move either
        /// way, one at 180 sits on the far side where a twin rides.</summary>
        OnLine,
        /// <summary>Arc covers the line but hangs toward -angle, so the short way out is +angle.</summary>
        PushPos,
        /// <summary>Mirror of PushPos: the short way out is -angle.</summary>
        PushNeg,
        /// <summary>Anywhere; may not ask for a move at all.</summary>
        Free
    }

    /// <summary>One hazard arc of a blocker slot. Angles are relative to the slot's anchor.</summary>
    public class BlockerArc
    {
        /// <summary>Centre of the arc, in degrees from the anchor.</summary>
        public float offsetDeg = 0f;
        public float spanDeg = 90f;
        /// <summary>Radial reach inward from the wall, as a fraction of tube radius. Low reads as
        /// "hop over it", tall as "steer round it" - keep that consistent across figures.</summary>
        public float heightFrac = 0.3f;
        /// <summary>Extent along the tube, in world units.</summary>
        public float depth = 1f;

        /// <summary>
        /// -1 is a solid arc. 0 and up colours the arc like that sphere (0 rides the player's
        /// line, 1 is the next one round, as PlayerController spaces them): that sphere flies
        /// straight through it, every other sphere hits it. The composer counts on this, so a
        /// ring can be passable only by lining the colours up. A colour the level has no sphere
        /// for is laid down solid.
        /// </summary>
        public int colour = -1;

        public BlockerArc(float offsetDeg, float spanDeg, float heightFrac = 0.3f, float depth = 1f, int colour = -1)
        {
            this.offsetDeg = offsetDeg; this.spanDeg = spanDeg;
            this.heightFrac = heightFrac; this.depth = depth;
            this.colour = colour;
        }

        /// <summary>A ring closed all the way round: nothing to steer to, so it must be jumped.</summary>
        public bool IsFullRing { get { return spanDeg >= 340f; } }
    }

    /// <summary>
    /// One ring inside a phrase. Usually described by <b>where it is safe</b> - gaps the
    /// composer walls around - but a slot can instead name its <b>hazards</b> directly (see
    /// <see cref="blockers"/>): a single small arc in open tube, which a gap cannot express.
    /// </summary>
    public class PhraseSlot
    {
        /// <summary>When set, the slot is these hazard arcs and gapAngles/gapDeg are ignored.
        /// The composer finds the nearest clear line itself, for however many spheres the
        /// player is carrying.</summary>
        public BlockerArc[] blockers;

        /// <summary>How a blocker slot is anchored to the player's line.</summary>
        public Placement placement = Placement.OnLine;

        /// <summary>Multiplies the level's target pressure for this slot only, so a figure can
        /// author its own rhythm (two hurdles in quick succession) without the level getting
        /// denser everywhere. Clamped under the hard ceiling.</summary>
        public float pressureScale = 1f;

        public bool IsBlocker { get { return blockers != null && blockers.Length > 0; } }

        /// <summary>Gap centres in degrees, relative to the phrase's base angle. Empty means the
        /// ring is fully blocked and must be jumped.</summary>
        public float[] gapAngles = new float[] { 0f };

        /// <summary>Angular width of every gap, in degrees. Narrower is a precision tax.</summary>
        public float gapDeg = 90f;

        /// <summary>Gaps are colour shields the matching sphere passes through, not open air.</summary>
        public bool colourGap = false;

        public MotionHint motion = MotionHint.Inherit;

        /// <summary>Radial reach of the arcs as a fraction of tube radius.</summary>
        public float thicknessFrac = 0.25f;

        /// <summary>Extra depth along the tube, 0..1, scaled by the level's depthIntensity.</summary>
        public float depthBias = 0f;

        /// <summary>A gap slot with no gaps: a closed wall. Blocker slots work out whether they
        /// need a jump from their arcs (see LevelComposer), so this is false for them.</summary>
        public bool RequiresJump { get { return !IsBlocker && (gapAngles == null || gapAngles.Length == 0); } }
    }

    /// <summary>
    /// A named, reusable multi-ring figure. Phrases carry no absolute spacing: the composer
    /// spaces their slots at run time from the level's speed and the current pressure target,
    /// so one vocabulary covers level 1 and level 128 without rewriting.
    /// </summary>
    public class LevelPhrase
    {
        public string id = "";
        public Mechanic requires = Mechanic.Steer;
        public PhraseSlot[] slots = new PhraseSlot[0];

        /// <summary>Breathing markers appended after the last slot, before the next phrase.</summary>
        public int trailMarkers = 1;

        /// <summary>A rest phrase: empty tube, used to pay back an intensity spike.</summary>
        public bool isRest = false;

        /// <summary>Flavour weight, 0..1. Used to keep variety interesting, never to set difficulty
        /// (difficulty is entirely the spacing the composer derives).</summary>
        public float spice = 0.5f;

        public int SlotCount { get { return slots != null ? slots.Length : 0; } }

        public LevelPhrase(string id, Mechanic requires, params PhraseSlot[] slots)
        {
            this.id = id;
            this.requires = requires;
            this.slots = slots ?? new PhraseSlot[0];
        }

        public LevelPhrase WithTrail(int markers) { trailMarkers = Mathf.Max(0, markers); return this; }
        public LevelPhrase WithSpice(float s) { spice = Mathf.Clamp01(s); return this; }
        public LevelPhrase AsRest() { isRest = true; return this; }
    }
}
