using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// The vocabulary the composer writes levels out of. Every phrase is a figure the player can
    /// learn and recognise; difficulty comes from how tightly the composer spaces it, never from
    /// the phrase itself. That separation is what lets one vocabulary cover all 128 levels.
    /// </summary>
    public static class PhraseLibrary
    {
        private static List<LevelPhrase> all;

        /// <summary>Slot helper: gaps at the given angles, all the same width.</summary>
        private static PhraseSlot S(float gapDeg, params float[] gapAngles)
        {
            return new PhraseSlot { gapAngles = gapAngles, gapDeg = gapDeg };
        }

        /// <summary>Slot helper: a fully blocked ring - the player has to jump it.</summary>
        private static PhraseSlot Wall(float thickness = 0.28f, float depthBias = 0f)
        {
            return new PhraseSlot { gapAngles = new float[0], gapDeg = 0f,
                                    thicknessFrac = thickness, depthBias = depthBias };
        }

        private static PhraseSlot Colour(float gapDeg, params float[] gapAngles)
        {
            return new PhraseSlot { gapAngles = gapAngles, gapDeg = gapDeg, colourGap = true };
        }

        private static PhraseSlot Moving(MotionHint hint, float gapDeg, params float[] gapAngles)
        {
            return new PhraseSlot { gapAngles = gapAngles, gapDeg = gapDeg, motion = hint };
        }

        /// <summary>One hazard arc: centre offset from the slot's anchor, span, height, depth.</summary>
        private static BlockerArc A(float offsetDeg, float spanDeg, float height = 0.3f, float depth = 1f)
        {
            return new BlockerArc(offsetDeg, spanDeg, height, depth);
        }

        /// <summary>A hazard arc in a sphere's colour: that sphere flies through it, the rest hit it.
        /// Tall, so it reads as something to meet rather than something to hop.</summary>
        private static BlockerArc Col(float offsetDeg, float spanDeg, int sphere, float height = 0.45f)
        {
            return new BlockerArc(offsetDeg, spanDeg, height, 1f, sphere);
        }

        /// <summary>Slot helper: hazard arcs anchored to the player's line.</summary>
        private static PhraseSlot B(Placement placement, params BlockerArc[] arcs)
        {
            return new PhraseSlot { blockers = arcs, placement = placement };
        }

        /// <summary>A full ring low enough to hop - the jump's first lesson.</summary>
        private static PhraseSlot Hurdle(float height = 0.15f, float pressureScale = 1f)
        {
            PhraseSlot slot = B(Placement.OnLine, A(0f, 356f, height, 0.6f));
            slot.pressureScale = pressureScale;
            return slot;
        }

        public static List<LevelPhrase> All
        {
            get
            {
                if (all == null) Build();
                return all;
            }
        }

        private static void Build()
        {
            all = new List<LevelPhrase>();

            // ---- Rests ------------------------------------------------------------------
            all.Add(new LevelPhrase("Breath", Mechanic.None).WithTrail(4).AsRest().WithSpice(0f));
            all.Add(new LevelPhrase("LongBreath", Mechanic.None).WithTrail(8).AsRest().WithSpice(0f));

            // ---- World 1: single arcs in open tube -----------------------------------------
            // Hazards, not walls: one arc on the player's line, three quarters of the tube open.
            // Every figure here asks for a steer and none can need a jump. Height 30% and depth 1
            // are deliberately chunky - the first thing the game shows should be easy to read.
            all.Add(new LevelPhrase("Nudge", Mechanic.Steer,
                B(Placement.OnLine, A(0f, 90f))).WithTrail(0).WithSpice(0.1f));

            // Short way out flips each ring: left, right, left, right.
            all.Add(new LevelPhrase("Slalom", Mechanic.Steer,
                B(Placement.PushPos, A(0f, 90f)), B(Placement.PushNeg, A(0f, 90f)),
                B(Placement.PushPos, A(0f, 90f)), B(Placement.PushNeg, A(0f, 90f))).WithTrail(1).WithSpice(0.25f));

            all.Add(new LevelPhrase("BigNudge", Mechanic.Steer,
                B(Placement.OnLine, A(0f, 125f))).WithTrail(0).WithSpice(0.2f));

            all.Add(new LevelPhrase("BigSlalom", Mechanic.Steer,
                B(Placement.PushPos, A(0f, 135f)), B(Placement.PushNeg, A(0f, 110f)),
                B(Placement.PushPos, A(0f, 135f))).WithTrail(1).WithSpice(0.35f));

            // Two arcs opposite each other: two open lanes, the line is always blocked.
            all.Add(new LevelPhrase("Lanes", Mechanic.Steer,
                B(Placement.OnLine, A(0f, 90f), A(180f, 90f)),
                B(Placement.OnLine, A(0f, 90f), A(180f, 90f))).WithTrail(1).WithSpice(0.3f));

            // Always out the same way: continuous, committed steering.
            all.Add(new LevelPhrase("Follow", Mechanic.Steer,
                B(Placement.PushPos, A(0f, 90f)), B(Placement.PushPos, A(0f, 90f)),
                B(Placement.PushPos, A(0f, 90f)), B(Placement.PushPos, A(0f, 90f))).WithTrail(1).WithSpice(0.4f));

            all.Add(new LevelPhrase("FollowBack", Mechanic.Steer,
                B(Placement.PushPos, A(0f, 90f)), B(Placement.PushPos, A(0f, 90f)),
                B(Placement.PushNeg, A(0f, 90f)), B(Placement.PushNeg, A(0f, 90f))).WithTrail(1).WithSpice(0.5f));

            // Two arcs, uneven gaps: one small lane, one big one. Reading which is which.
            all.Add(new LevelPhrase("OffsetLanes", Mechanic.Steer,
                B(Placement.OnLine, A(0f, 100f), A(130f, 70f)),
                B(Placement.OnLine, A(0f, 100f), A(-130f, 70f))).WithTrail(1).WithSpice(0.45f));

            all.Add(new LevelPhrase("Trident", Mechanic.Steer,
                B(Placement.OnLine, A(0f, 70f), A(120f, 70f), A(240f, 70f)),
                B(Placement.OnLine, A(0f, 70f), A(120f, 70f), A(240f, 70f))).WithTrail(1).WithSpice(0.55f));

            // A quick slalom for finales and flourishes; the band, not the figure, makes it tight.
            all.Add(new LevelPhrase("Burst", Mechanic.Steer,
                B(Placement.PushPos, A(0f, 80f)), B(Placement.PushNeg, A(0f, 80f)),
                B(Placement.PushPos, A(0f, 80f)), B(Placement.PushNeg, A(0f, 80f))).WithTrail(2).WithSpice(0.9f));

            // ---- World 2: hurdles ------------------------------------------------------------
            // Low full rings: nothing to steer to, but low enough that the jump is forgiving.
            all.Add(new LevelPhrase("Hurdle", Mechanic.Jump, Hurdle()).WithTrail(1).WithSpice(0.3f));

            all.Add(new LevelPhrase("HurdleNudge", Mechanic.Jump | Mechanic.Steer,
                Hurdle(), B(Placement.OnLine, A(0f, 90f))).WithTrail(1).WithSpice(0.4f));

            // Low and wide: hop it or go round it, both work.
            all.Add(new LevelPhrase("LowWide", Mechanic.Jump,
                B(Placement.OnLine, A(0f, 180f, 0.15f, 0.8f))).WithTrail(1).WithSpice(0.45f));

            // Two hurdles in quick succession - the second is spaced tighter than the level's band.
            all.Add(new LevelPhrase("DoubleHop", Mechanic.Jump,
                Hurdle(), Hurdle(0.15f, 2.2f)).WithTrail(2).WithSpice(0.55f));

            all.Add(new LevelPhrase("HighHurdle", Mechanic.Jump, Hurdle(0.25f)).WithTrail(1).WithSpice(0.5f));

            // Land, then read: a hurdle straight into lanes.
            all.Add(new LevelPhrase("LandLook", Mechanic.Jump | Mechanic.Steer,
                Hurdle(),
                B(Placement.OnLine, A(0f, 90f), A(180f, 90f)),
                B(Placement.OnLine, A(0f, 70f), A(120f, 70f), A(240f, 70f))).WithTrail(2).WithSpice(0.65f));

            // ---- World 3: shapes -------------------------------------------------------------
            // Height is the verb: towers (55%) say steer, hurdles (15%) say hop.
            all.Add(new LevelPhrase("Tower", Mechanic.Shapes,
                B(Placement.OnLine, A(0f, 60f, 0.55f, 1.2f))).WithTrail(0).WithSpice(0.3f));

            all.Add(new LevelPhrase("TowerRow", Mechanic.Shapes,
                B(Placement.PushPos, A(0f, 60f, 0.55f, 1.2f)),
                B(Placement.PushNeg, A(0f, 60f, 0.55f, 1.2f)),
                B(Placement.PushPos, A(0f, 60f, 0.55f, 1.2f))).WithTrail(1).WithSpice(0.45f));

            all.Add(new LevelPhrase("TowerHop", Mechanic.Shapes | Mechanic.Jump,
                B(Placement.OnLine, A(0f, 60f, 0.55f, 1.2f)),
                Hurdle(),
                B(Placement.PushNeg, A(0f, 60f, 0.55f, 1.2f))).WithTrail(1).WithSpice(0.55f));

            // Spans from a sliver to half the tube in one figure.
            all.Add(new LevelPhrase("Wide", Mechanic.Shapes,
                B(Placement.PushPos, A(0f, 45f)),
                B(Placement.OnLine, A(0f, 180f)),
                B(Placement.PushNeg, A(0f, 90f)),
                B(Placement.OnLine, A(0f, 150f))).WithTrail(1).WithSpice(0.5f));

            // The first walls: a wide door in a ring, then another somewhere else.
            all.Add(new LevelPhrase("Door", Mechanic.Shapes,
                S(110f, 0f), S(110f, 70f), S(110f, -40f)).WithTrail(2).WithSpice(0.45f));

            // Thin blades against thick slabs, and a deep low arc worth hopping early.
            all.Add(new LevelPhrase("Blades", Mechanic.Shapes | Mechanic.Jump,
                B(Placement.OnLine, A(0f, 90f, 0.3f, 0.4f)),
                B(Placement.PushPos, A(0f, 90f, 0.3f, 2.5f)),
                B(Placement.OnLine, A(0f, 200f, 0.15f, 2.5f))).WithTrail(1).WithSpice(0.6f));

            all.Add(new LevelPhrase("Four", Mechanic.Shapes,
                B(Placement.OnLine, A(0f, 45f), A(90f, 45f), A(180f, 45f), A(270f, 45f)),
                B(Placement.OnLine, A(45f, 45f), A(135f, 45f), A(225f, 45f), A(315f, 45f))).WithTrail(1).WithSpice(0.6f));

            all.Add(new LevelPhrase("Squeeze", Mechanic.Shapes,
                S(80f, 0f), S(78f, 60f), S(75f, 120f)).WithTrail(2).WithSpice(0.7f));

            // ---- Your own colour (world 3, one sphere) ----------------------------------------
            // An arc in the sphere's own colour is open air to it. The first lesson is simply to
            // trust that: hold the line and fly through, instead of dodging on reflex.
            all.Add(new LevelPhrase("OwnColour", Mechanic.Shapes,
                B(Placement.OnLine, Col(0f, 120f, 0))).WithTrail(0).WithSpice(0.3f));

            // Keep or dodge: your colour, then a solid arc, then your colour again.
            all.Add(new LevelPhrase("KeepOrDodge", Mechanic.Shapes,
                B(Placement.OnLine, Col(0f, 110f, 0)),
                B(Placement.OnLine, A(0f, 90f)),
                B(Placement.OnLine, Col(0f, 110f, 0)),
                B(Placement.PushNeg, A(0f, 90f))).WithTrail(1).WithSpice(0.5f));

            // A wide wall in your colour with a gap in it: both routes work, the straight one is yours.
            all.Add(new LevelPhrase("OwnWall", Mechanic.Shapes,
                B(Placement.OnLine, Col(0f, 250f, 0))).WithTrail(1).WithSpice(0.4f));

            // Yours on the line, solid across the tube.
            all.Add(new LevelPhrase("OwnSplit", Mechanic.Shapes,
                B(Placement.OnLine, Col(0f, 110f, 0), A(180f, 110f)),
                B(Placement.OnLine, A(180f, 110f), Col(0f, 110f, 0))).WithTrail(1).WithSpice(0.45f));

            // ---- Twins (world 9, two spheres opposite each other) --------------------------
            // Sphere 0 rides the line, sphere 1 rides the far side. Every figure here is checked
            // for both, so the pair always has a way through without jumping.

            // An arc on your twin's side: the danger is the sphere you are not looking at.
            all.Add(new LevelPhrase("TwinWatch", Mechanic.Twins,
                B(Placement.OnLine, A(180f, 90f)),
                B(Placement.OnLine, A(0f, 90f)),
                B(Placement.OnLine, A(180f, 90f))).WithTrail(1).WithSpice(0.35f));

            // Doors either side for both at once: narrow enough that both have to be lined up.
            all.Add(new LevelPhrase("TwinDoors", Mechanic.Twins,
                B(Placement.OnLine, A(0f, 120f), A(180f, 120f)),
                B(Placement.OnLine, A(0f, 120f), A(180f, 120f))).WithTrail(1).WithSpice(0.45f));

            // Uneven: each sphere has a different arc in its way.
            all.Add(new LevelPhrase("TwinSplit", Mechanic.Twins,
                B(Placement.OnLine, A(0f, 90f), A(150f, 70f)),
                B(Placement.OnLine, A(0f, 90f), A(-150f, 70f))).WithTrail(1).WithSpice(0.55f));

            // Four small arcs: the twins thread opposite gaps together.
            all.Add(new LevelPhrase("TwinQuad", Mechanic.Twins,
                B(Placement.OnLine, A(0f, 50f), A(90f, 50f), A(180f, 50f), A(270f, 50f))).WithTrail(1).WithSpice(0.65f));

            // Slalom where the far arc is the one that sets the direction.
            all.Add(new LevelPhrase("TwinSlalom", Mechanic.Twins,
                B(Placement.PushPos, A(0f, 90f)), B(Placement.OnLine, A(200f, 90f)),
                B(Placement.PushNeg, A(0f, 90f)), B(Placement.OnLine, A(160f, 90f))).WithTrail(1).WithSpice(0.6f));

            // ---- Colour with twins (world 10) -----------------------------------------------
            // Each twin passes its own colour and hits the other's. Some rings can only be read
            // one way: line both spheres up with their own colours.

            // Your colour on your line; your twin's colour on theirs. Hold still: both fly through.
            all.Add(new LevelPhrase("ColourHold", Mechanic.Colour | Mechanic.Twins,
                B(Placement.OnLine, Col(0f, 120f, 0)),
                B(Placement.OnLine, Col(180f, 120f, 1)),
                B(Placement.OnLine, Col(0f, 120f, 0), Col(180f, 120f, 1))).WithTrail(1).WithSpice(0.35f));

            // The other twin's colour on your line: coloured, but not yours - dodge it.
            all.Add(new LevelPhrase("WrongColour", Mechanic.Colour | Mechanic.Twins,
                B(Placement.OnLine, Col(0f, 110f, 1)),
                B(Placement.OnLine, Col(0f, 110f, 0)),
                B(Placement.OnLine, Col(180f, 110f, 0))).WithTrail(1).WithSpice(0.55f));

            // The whole tube in two colours, each half matching the twin already in it: trust it.
            all.Add(new LevelPhrase("ColourMatch", Mechanic.Colour | Mechanic.Twins,
                B(Placement.OnLine, Col(0f, 176f, 0), Col(180f, 176f, 1))).WithTrail(1).WithSpice(0.5f));

            // The whole tube in two colours, each half matching the *other* twin: the only way
            // through is a big turn (about 100 degrees) that carries both across the seams into
            // their own colours.
            all.Add(new LevelPhrase("HalfTurn", Mechanic.Colour | Mechanic.Twins,
                B(Placement.OnLine, Col(0f, 176f, 1), Col(180f, 176f, 0))).WithTrail(2).WithSpice(0.8f));

            // Colours and solids mixed: only the colour-matched rotation clears it.
            all.Add(new LevelPhrase("ColourLock", Mechanic.Colour | Mechanic.Twins,
                B(Placement.OnLine, Col(0f, 100f, 1), A(90f, 70f), Col(180f, 100f, 0), A(270f, 70f)),
                B(Placement.OnLine, Col(0f, 100f, 0), A(90f, 70f), Col(180f, 100f, 1), A(270f, 70f))).WithTrail(2).WithSpice(0.85f));

            // ---- Steering with walls (world 3 onward) ----------------------------------------
            // Two doors: either gap works, so the move is always short. The gentlest figure.
            all.Add(new LevelPhrase("TwoDoor", Mechanic.Steer,
                S(85f, 0f, 180f), S(85f, 90f, 270f), S(85f, 0f, 180f)).WithTrail(2).WithSpice(0.2f));

            // Corridor: the gap never moves. Teaches that holding still is an answer.
            all.Add(new LevelPhrase("Corridor", Mechanic.Steer,
                S(100f, 0f), S(100f, 0f), S(100f, 0f), S(100f, 0f)).WithTrail(2).WithSpice(0.15f));

            // Ladder: the gap walks one way. Teaches committed, continuous steering.
            all.Add(new LevelPhrase("Ladder", Mechanic.Steer,
                S(90f, 0f), S(90f, 60f), S(90f, 120f), S(90f, 180f)).WithTrail(2).WithSpice(0.4f));

            all.Add(new LevelPhrase("LadderBack", Mechanic.Steer,
                S(90f, 0f), S(90f, -60f), S(90f, -120f), S(90f, -180f)).WithTrail(2).WithSpice(0.4f));

            // Zigzag: reverse direction every ring. The classic rhythm figure.
            all.Add(new LevelPhrase("Zigzag", Mechanic.Steer,
                S(90f, -70f), S(90f, 70f), S(90f, -70f), S(90f, 70f)).WithTrail(2).WithSpice(0.5f));

            // Sweep: narrow gaps marching steadily. Precision under continuous motion.
            all.Add(new LevelPhrase("Sweep", Mechanic.Steer,
                S(70f, 0f), S(70f, 45f), S(70f, 90f), S(70f, 135f), S(70f, 180f)).WithTrail(2).WithSpice(0.55f));

            // Threading: opposed gaps. The expensive half-lap figure.
            all.Add(new LevelPhrase("Threading", Mechanic.Steer,
                S(95f, 0f), S(95f, 180f), S(95f, 0f)).WithTrail(3).WithSpice(0.65f));

            // Pinhole: one narrow gap, nothing else. Pure precision.
            all.Add(new LevelPhrase("Pinhole", Mechanic.Steer,
                S(55f, 0f), S(55f, 40f)).WithTrail(3).WithSpice(0.7f));

            // FakeOut: two comfortable doors, then the pattern breaks.
            all.Add(new LevelPhrase("FakeOut", Mechanic.Steer,
                S(85f, 0f, 180f), S(85f, 0f, 180f), S(70f, 90f)).WithTrail(3).WithSpice(0.8f));

            // ---- Jump -------------------------------------------------------------------
            all.Add(new LevelPhrase("JumpWall", Mechanic.Jump, Wall()).WithTrail(3).WithSpice(0.35f));

            all.Add(new LevelPhrase("JumpRun", Mechanic.Jump,
                S(95f, 0f), Wall(), S(95f, 0f)).WithTrail(3).WithSpice(0.5f));

            all.Add(new LevelPhrase("HopStep", Mechanic.Jump | Mechanic.Steer,
                Wall(), S(85f, 90f), Wall(), S(85f, -90f)).WithTrail(3).WithSpice(0.7f));

            all.Add(new LevelPhrase("DoubleWall", Mechanic.Jump,
                Wall(0.32f), Wall(0.32f)).WithTrail(4).WithSpice(0.6f));

            // ---- Colour -----------------------------------------------------------------
            all.Add(new LevelPhrase("ColourRun", Mechanic.Colour,
                Colour(95f, 0f), Colour(95f, 0f), Colour(95f, 0f)).WithTrail(2).WithSpice(0.4f));

            all.Add(new LevelPhrase("ColourSwap", Mechanic.Colour,
                Colour(90f, 0f), Colour(90f, 100f), Colour(90f, -100f)).WithTrail(3).WithSpice(0.6f));

            all.Add(new LevelPhrase("MixedGate", Mechanic.Colour | Mechanic.Steer,
                S(85f, 0f), Colour(85f, 90f), S(85f, 180f)).WithTrail(2).WithSpice(0.55f));

            // ---- Spin -------------------------------------------------------------------
            all.Add(new LevelPhrase("Spinner", Mechanic.Spin,
                Moving(MotionHint.Spin, 95f, 0f, 180f),
                Moving(MotionHint.Spin, 95f, 0f, 180f)).WithTrail(3).WithSpice(0.5f));

            all.Add(new LevelPhrase("SpinGauntlet", Mechanic.Spin,
                Moving(MotionHint.Spin, 80f, 0f),
                Moving(MotionHint.Spin, 80f, 0f),
                Moving(MotionHint.Spin, 80f, 0f)).WithTrail(4).WithSpice(0.75f));

            // ---- Snap -------------------------------------------------------------------
            all.Add(new LevelPhrase("Metronome", Mechanic.Snap,
                Moving(MotionHint.Snap, 90f, 0f, 180f),
                Moving(MotionHint.Snap, 90f, 0f, 180f),
                Moving(MotionHint.Snap, 90f, 0f, 180f)).WithTrail(3).WithSpice(0.65f));

            all.Add(new LevelPhrase("SnapPair", Mechanic.Snap | Mechanic.Steer,
                S(90f, 0f), Moving(MotionHint.Snap, 85f, 90f)).WithTrail(3).WithSpice(0.6f));

            // ---- Pendulum / moving gap --------------------------------------------------
            all.Add(new LevelPhrase("Pendulum", Mechanic.MovingGap,
                Moving(MotionHint.Oscillate, 95f, 0f),
                Moving(MotionHint.Oscillate, 95f, 0f)).WithTrail(3).WithSpice(0.6f));

            all.Add(new LevelPhrase("PendulumRun", Mechanic.MovingGap,
                Moving(MotionHint.Oscillate, 85f, 0f),
                Moving(MotionHint.Oscillate, 85f, 120f),
                Moving(MotionHint.Oscillate, 85f, -120f)).WithTrail(4).WithSpice(0.8f));

            // ---- Compound / deep walls --------------------------------------------------
            all.Add(new LevelPhrase("DeepWall", Mechanic.Compound,
                Wall(0.3f, 1f)).WithTrail(4).WithSpice(0.6f));

            all.Add(new LevelPhrase("Stacked", Mechanic.Compound,
                S(80f, 0f), S(80f, 120f), S(80f, 240f)).WithTrail(3).WithSpice(0.85f));

            all.Add(new LevelPhrase("WallThread", Mechanic.Compound | Mechanic.Jump,
                Wall(0.3f, 0.6f), S(80f, 180f), Wall(0.3f, 0.6f)).WithTrail(4).WithSpice(0.9f));

            // ---- Narrows (world 5): sustained precision, the gap never widens -----------
            all.Add(new LevelPhrase("Narrows", Mechanic.Pinch,
                S(60f, 0f), S(55f, 0f), S(50f, 0f), S(50f, 0f)).WithTrail(3).WithSpice(0.7f));

            all.Add(new LevelPhrase("NarrowWalk", Mechanic.Pinch,
                S(58f, 0f), S(54f, 35f), S(50f, 70f), S(48f, 105f)).WithTrail(4).WithSpice(0.85f));

            // ---- Gate rings (world 7): one way through, no second door ------------------
            all.Add(new LevelPhrase("GateRun", Mechanic.GateRing,
                S(65f, 0f), S(65f, 0f), S(65f, 0f)).WithTrail(3).WithSpice(0.6f));

            all.Add(new LevelPhrase("GateStep", Mechanic.GateRing,
                S(62f, 0f), S(62f, 90f), S(62f, 180f)).WithTrail(3).WithSpice(0.8f));

            all.Add(new LevelPhrase("GateJump", Mechanic.GateRing | Mechanic.Jump,
                S(62f, 0f), Wall(), S(62f, 120f)).WithTrail(4).WithSpice(0.85f));

            // ---- The hold (world 12): long stretches where standing still is the skill ---
            all.Add(new LevelPhrase("LongHold", Mechanic.Corridor,
                S(85f, 0f), S(80f, 0f), S(75f, 0f), S(70f, 0f), S(70f, 0f), S(70f, 0f))
                .WithTrail(3).WithSpice(0.5f));

            all.Add(new LevelPhrase("HoldBreak", Mechanic.Corridor | Mechanic.Steer,
                S(80f, 0f), S(80f, 0f), S(80f, 0f), S(70f, 150f), S(70f, 150f))
                .WithTrail(3).WithSpice(0.8f));

            // ---- Pursuit (world 14): rings that close on where you are standing ----------
            all.Add(new LevelPhrase("Pursuit", Mechanic.Reactive,
                Moving(MotionHint.Chase, 95f, 0f, 180f),
                Moving(MotionHint.Chase, 95f, 0f, 180f)).WithTrail(4).WithSpice(0.75f));

            all.Add(new LevelPhrase("PursuitRun", Mechanic.Reactive | Mechanic.Steer,
                Moving(MotionHint.Chase, 90f, 0f),
                S(90f, 120f),
                Moving(MotionHint.Chase, 90f, 240f)).WithTrail(4).WithSpice(0.9f));

            // ---- Colour shifting --------------------------------------------------------
            all.Add(new LevelPhrase("Shifter", Mechanic.ColourShift,
                Colour(95f, 0f), Colour(95f, 0f), Colour(95f, 0f)).WithTrail(3).WithSpice(0.75f));

            all.Add(new LevelPhrase("ShiftSwap", Mechanic.ColourShift | Mechanic.Steer,
                Colour(90f, 0f), Colour(90f, 140f), Colour(90f, -140f)).WithTrail(4).WithSpice(0.9f));
        }

        /// <summary>Every phrase this level's mechanic set can express, rests excluded.</summary>
        public static List<LevelPhrase> Deck(Mechanic allowed)
        {
            List<LevelPhrase> deck = new List<LevelPhrase>();
            foreach (LevelPhrase p in All)
            {
                if (p.isRest) continue;
                if ((p.requires & ~allowed) == 0) deck.Add(p);
            }
            return deck;
        }

        public static LevelPhrase Rest(bool longRest)
        {
            string want = longRest ? "LongBreath" : "Breath";
            foreach (LevelPhrase p in All) if (p.id == want) return p;
            return All[0];
        }

        public static LevelPhrase ById(string id)
        {
            foreach (LevelPhrase p in All) if (p.id == id) return p;
            return null;
        }
    }
}
