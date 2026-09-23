using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// Turns a level's dials into a concrete, ordered list of rings.
    ///
    /// The method, in one line: <b>lay phrases end to end, and space every ring by exactly what
    /// the move it demands costs at this level's speed.</b> Spacing is derived, never rolled, so
    /// no level can ask for a move the player has no time to make - the failure mode the old
    /// per-marker coin flip fell into above level 13.
    ///
    /// Composition is deterministic from the level seed and happens incrementally, a phrase at a
    /// time, so a finite level can be fully composed for validation while an endless run streams
    /// forever from the same code path.
    /// </summary>
    public class LevelComposer
    {
        public readonly ProgressionDials dials;

        /// <summary>Endless runs supply this to re-evaluate the dials as the run advances.
        /// Null means the level's dials are constant, which is the campaign case.</summary>
        public System.Func<float, ProgressionDials> rampAt;

        /// <summary>Where composition stops. Campaign levels set this from the level length;
        /// endless runs leave it at infinity.</summary>
        public float endZ = float.PositiveInfinity;

        /// <summary>Clear air after the last ring before the finish gate.</summary>
        public const float RunOutUnits = 30f;
        /// <summary>Intensity debt at which the composer forces a rest, whatever the envelope says.</summary>
        private const float DebtThreshold = 2.4f;

        /// <summary>How many distinct sphere colours the run has to work with.</summary>
        public static int PlayerColourCount = 3;

        /// <summary>
        /// Body colours for solid arcs, applied in turn so neighbouring rings never share one.
        /// Two entries of strongly different luminance separate depth better than a longer, subtler
        /// list would. Neither hue appears in the sphere palette (orange, green, pink, yellow,
        /// purple), so a solid arc is never mistakable for a colour shield. Change them here.
        /// </summary>
        public static Color[] SolidTints =
        {
            new Color(1.00f, 0.12f, 0.18f),   // hot red
            new Color(1.00f, 0.88f, 0.74f),   // warm white
        };

        private readonly List<RingSpec> rings = new List<RingSpec>();
        private List<LevelPhrase> deck;
        private List<float> deckWeights;
        private System.Random rand;

        private bool started;
        private bool finished;
        private float z;
        private float currentAngle;
        private float debt;
        private LevelPhrase lastPhrase;
        private int repeatsLeft;
        private bool mirror;

        public LevelComposer(ProgressionDials dials)
        {
            this.dials = dials;
            if (dials != null && dials.levelSeconds > 0f)
                endZ = dials.LengthUnits - RunOutUnits;
        }

        /// <summary>Every ring in the level. Only meaningful for a finite (campaign) level.</summary>
        public List<RingSpec> Rings
        {
            get
            {
                EnsureComposedTo(float.IsInfinity(endZ) ? 5000f : endZ);
                return rings;
            }
        }

        public int ComposedCount { get { return rings.Count; } }

        /// <summary>Read-only view of everything composed so far. Grows as an endless run streams.</summary>
        public IReadOnlyList<RingSpec> Composed { get { return rings; } }

        // ---- Marker rings -------------------------------------------------------------------
        // Drawn rings are derived from the composition rather than laid on a grid of their own:
        // one under every arc, so a ring always means "something is here", plus filler rings
        // across empty stretches so the tube never loses its sense of speed. That is what keeps
        // arcs and powerups aligned to rings while leaving the obstacle grid - and therefore the
        // pressure model - completely alone.

        private readonly List<float> markers = new List<float>();
        private int markerRingCursor;
        private float lastMarkerZ;
        private bool markersStarted;

        /// <summary>Marker-ring positions in [zStart, zEnd).</summary>
        public void MarkersInRange(float zStart, float zEnd, List<float> results)
        {
            results.Clear();
            EnsureMarkersTo(zEnd);

            int lo = 0, hi = markers.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (markers[mid] < zStart) lo = mid + 1; else hi = mid;
            }
            for (int i = lo; i < markers.Count && markers[i] < zEnd; i++) results.Add(markers[i]);
        }

        /// <summary>The marker ring nearest to z, for snapping pickups onto the rhythm.</summary>
        public float NearestMarker(float z)
        {
            EnsureMarkersTo(z + Mathf.Max(1f, dials.MarkerRingSpacing) * 2f);
            if (markers.Count == 0) return z;

            int lo = 0, hi = markers.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (markers[mid] < z) lo = mid + 1; else hi = mid;
            }
            if (lo == 0) return markers[0];
            if (lo >= markers.Count) return markers[markers.Count - 1];
            return (z - markers[lo - 1] <= markers[lo] - z) ? markers[lo - 1] : markers[lo];
        }

        /// <summary>True if a marker ring sits in (fromZ, toZ] - the player just passed one.</summary>
        public bool CrossedMarker(float fromZ, float toZ)
        {
            if (toZ <= fromZ) return false;
            EnsureMarkersTo(toZ);

            int lo = 0, hi = markers.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (markers[mid] <= fromZ) lo = mid + 1; else hi = mid;
            }
            return lo < markers.Count && markers[lo] <= toZ;
        }

        private void EnsureMarkersTo(float targetZ)
        {
            EnsureComposedTo(targetZ + 50f);

            if (!markersStarted) { lastMarkerZ = 0f; markersStarted = true; }
            float maxGap = Mathf.Max(1f, dials.MarkerRingSpacing);

            // One marker per composed ring. Several arcs share a ring's z, so skip repeats.
            while (markerRingCursor < rings.Count)
            {
                float ringZ = rings[markerRingCursor].z;
                markerRingCursor++;
                if (ringZ <= lastMarkerZ + 0.01f) continue;

                FillMarkersTo(ringZ, maxGap);
                markers.Add(ringZ);
                lastMarkerZ = ringZ;
            }

            // Past the last ring there is nothing left to align to, so plain fillers carry the
            // stretch - a level's run-out, or a long rest in an endless run. Filling only as far
            // as the write head keeps this safe: no ring can ever be placed behind it, so no
            // filler can end up sitting where an arc's ring later needs to go.
            FillMarkersTo(finished ? targetZ : Mathf.Min(targetZ, this.z), maxGap);
        }

        private void FillMarkersTo(float z, float maxGap)
        {
            while (lastMarkerZ + maxGap < z - 0.01f)
            {
                lastMarkerZ += maxGap;
                markers.Add(lastMarkerZ);
            }
        }

        /// <summary>Rings whose z falls in [zStart, zEnd). Composes further ahead as needed.</summary>
        public void RingsInRange(float zStart, float zEnd, List<RingSpec> results)
        {
            results.Clear();
            EnsureComposedTo(zEnd + 200f);

            int lo = 0, hi = rings.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (rings[mid].z < zStart) lo = mid + 1; else hi = mid;
            }
            for (int i = lo; i < rings.Count && rings[i].z < zEnd; i++) results.Add(rings[i]);
        }

        /// <summary>Composes forward until the write head passes targetZ, or the level ends.</summary>
        public void EnsureComposedTo(float targetZ)
        {
            if (!started) Begin();

            int guard = 0;
            while (!finished && z < targetZ && guard++ < 20000)
            {
                EmitNextPhrase();
            }
        }

        // =====================================================================================

        private void Begin()
        {
            started = true;

            int seed = dials.seed != 0 ? dials.seed : unchecked(dials.level * 7919 + 12345);
            rand = new System.Random(seed);

            // Only colour a shield in a colour the player is actually carrying, so a shield is
            // always a real alternative route rather than a wall wearing a friendly colour.
            if (GameManager.Instance != null && GameManager.Instance.currentSphereCount > 0)
                PlayerColourCount = Mathf.Clamp(GameManager.Instance.currentSphereCount, 1, 5);

            // Every ring is cleared for every sphere. A level designed for a count uses it;
            // otherwise (endless) it is whatever the player brought. The deck depends on it.
            sphereCount = dials.sphereCount > 0 ? dials.sphereCount : PlayerColourCount;

            RebuildDeck(dials);

            // Obstacle-free run-in: about a second and a half of travel, on top of the three-second
            // countdown the player already gets before moving at all.
            z = Mathf.Clamp(dials.speed * 1.5f, 30f, 75f);
            currentAngle = 0f;
            debt = 0f;
        }

        /// <summary>The dials in force at the current write head. Constant unless a ramp is set.</summary>
        private ProgressionDials DialsHere()
        {
            if (rampAt == null) return dials;
            ProgressionDials d = rampAt(z);
            return d ?? dials;
        }

        private void EmitNextPhrase()
        {
            if (z >= endZ) { finished = true; return; }

            ProgressionDials d = DialsHere();
            float target = TargetHere(d);

            // ---- Choose the next phrase -------------------------------------------------
            LevelPhrase phrase;
            if (QuietHere() || debt >= DebtThreshold)
            {
                phrase = PhraseLibrary.Rest(debt >= DebtThreshold);
                debt = 0f;
                repeatsLeft = 0;
            }
            else if (repeatsLeft > 0 && lastPhrase != null)
            {
                // Repeat-then-vary: the same figure comes back mirrored. This is what turns a
                // level into something learnable rather than something merely survived.
                phrase = lastPhrase;
                repeatsLeft--;
                mirror = !mirror;
            }
            else
            {
                phrase = PickPhrase(rand, lastPhrase, d);
                repeatsLeft = (rand.NextDouble() < 0.55) ? 1 : 0;
                mirror = false;
            }

            // ---- Lay it down -------------------------------------------------------------
            // On a spiralling level the whole field corkscrews with distance, so a phrase laid
            // down here arrives rotated. The cost model is untouched: the composer still measures
            // the move between the actual gap angles it is about to emit.
            float baseAngle = (float)(rand.NextDouble() * 360.0) + z * d.spiralDegPerUnit;
            EmitPhrase(phrase, baseAngle, target, d);

            if (!phrase.isRest) debt += Mathf.Max(0f, target - d.restPressure) * phrase.SlotCount * 0.35f;
            else debt = 0f;

            if (!phrase.isRest) lastPhrase = phrase;
        }

        private int sphereCount = 1;

        private float TargetHere(ProgressionDials d)
        {
            if (rampAt != null) return Mathf.Clamp(d.bandCeiling, 0.12f, ProgressionDials.HardCeiling);
            float t = Mathf.Clamp01(z / Mathf.Max(1f, d.LengthUnits));
            return IntensityCurve.Target(t, d);
        }

        private bool QuietHere()
        {
            if (rampAt != null) return false;
            float t = Mathf.Clamp01(z / Mathf.Max(1f, dials.LengthUnits));
            return IntensityCurve.IsQuietZone(t);
        }

        /// <summary>
        /// Emits one phrase, advancing the write head. Every slot's spacing is solved from the
        /// cost of the move that reaches it, so pressure lands on target by construction.
        /// </summary>
        private void EmitPhrase(LevelPhrase phrase, float baseAngle, float target, ProgressionDials d)
        {
            float interval = d.markerInterval;

            for (int i = 0; i < phrase.SlotCount; i++)
            {
                PhraseSlot slot = phrase.slots[i];

                float required;
                float chosenAngle = currentAngle;
                bool requiresJump = slot.RequiresJump;
                bool jumped = slot.RequiresJump;

                // Blocker slots always, and gap slots once there is more than one sphere, are
                // costed from their actual arcs: the move is whatever rotation clears every arc
                // for every sphere, with each sphere's own colour counting as open to it.
                List<ArcSpec> prebuilt = null;
                if (slot.IsBlocker)
                    prebuilt = BuildBlockerArcs(slot, d);
                else if (!slot.RequiresJump && sphereCount > 1)
                    prebuilt = GapArcsAt(slot, baseAngle, mirror, GapThickness(slot, d), GapDepth(slot, d));

                if (prebuilt != null)
                {
                    // A ring built around the colours is a lesson in matching them: space it for
                    // the steer (or the hold), even where a hop over it would be quicker.
                    bool ownColours = HasOwnColour(slot);
                    float delta;
                    if (!CostArcs(prebuilt, d, !ownColours, out required, out delta, out requiresJump, out jumped))
                        continue;       // nothing clears it at this level: leave the ring out
                    chosenAngle = jumped ? currentAngle : currentAngle + delta;

                    if (ownColours || slot.colourGap) required += ActionCost.ColourRecognition;
                    if (slot.motion != MotionHint.Static && slot.motion != MotionHint.Inherit)
                        required += ActionCost.MotionRecognition;
                }
                else if (slot.RequiresJump)
                {
                    required = ActionCost.JumpSeconds + ActionCost.DecisionLatency;
                }
                else
                {
                    // The optimal line goes through whichever gap is cheapest to reach.
                    float bestDelta = 999f;
                    for (int g = 0; g < slot.gapAngles.Length; g++)
                    {
                        float raw = mirror ? -slot.gapAngles[g] : slot.gapAngles[g];
                        float abs = baseAngle + raw;
                        float delta = ActionCost.ShortestDelta(currentAngle, abs);
                        if (Mathf.Abs(delta) < Mathf.Abs(bestDelta))
                        {
                            bestDelta = delta;
                            chosenAngle = abs;
                        }
                    }

                    required = ActionCost.SteerSeconds(bestDelta, d.angularSpeed)
                             + ActionCost.DecisionLatency;

                    // A narrow gap has to be hit, not merely reached.
                    if (slot.gapDeg < 75f) required += 0.12f;
                    if (slot.colourGap) required += ActionCost.ColourRecognition;
                    if (slot.motion != MotionHint.Static && slot.motion != MotionHint.Inherit)
                        required += ActionCost.MotionRecognition;
                }

                // A figure may set its own rhythm for one slot (a quick second hurdle).
                float slotTarget = target;
                if (!Mathf.Approximately(slot.pressureScale, 1f))
                    slotTarget = Mathf.Clamp(target * slot.pressureScale, 0.05f, ProgressionDials.HardCeiling - 0.1f);

                int markers = ActionCost.MarkersNeeded(required, slotTarget, interval, d.speed);
                z += markers * interval;
                if (z >= endZ) { finished = true; return; }

                RingSpec spec = BuildRing(slot, phrase, baseAngle, chosenAngle, z, d, prebuilt);
                spec.solidTint = SolidTints[rings.Count % SolidTints.Length];
                spec.requiredSeconds = required;
                spec.pressure = ActionCost.Pressure(required, markers, interval, d.speed);
                spec.requiresJump = requiresJump;
                spec.jumped = jumped;
                rings.Add(spec);

                currentAngle = chosenAngle;
            }

            z += phrase.trailMarkers * interval;
        }

        /// <summary>
        /// Lays a blocker slot's arcs down in absolute degrees, anchored to the player's line so
        /// the ring asks for the move the figure intends. Mirroring flips both the offsets and
        /// which way a push sends the player.
        /// </summary>
        private List<ArcSpec> BuildBlockerArcs(PhraseSlot slot, ProgressionDials d)
        {
            BlockerArc first = slot.blockers[0];
            float firstOffset = mirror ? -first.offsetDeg : first.offsetDeg;
            float span = Mathf.Min(first.spanDeg, 356f);

            Placement placement = slot.placement;
            if (mirror && placement == Placement.PushPos) placement = Placement.PushNeg;
            else if (mirror && placement == Placement.PushNeg) placement = Placement.PushPos;

            // Where the slot's anchor (offset 0) lands. OnLine puts it on the player's line, so an
            // arc at offset 180 sits on the far side - where a twin rides. A push places the
            // first arc itself, overhanging the line.
            float anchor;
            switch (placement)
            {
                case Placement.PushPos:
                case Placement.PushNeg:
                {
                    // Cover the line, overhanging it by only `overlap` on the side the player
                    // should leave by, so the short way out is always that way.
                    float overlap = Mathf.Min(span * 0.4f, 20f + (float)rand.NextDouble() * 10f);
                    float side = placement == Placement.PushPos ? 1f : -1f;
                    anchor = currentAngle + side * (overlap - span * 0.5f) - firstOffset;
                    break;
                }
                case Placement.Free:
                    anchor = (float)(rand.NextDouble() * 360.0);
                    break;
                default:
                    anchor = currentAngle + ((float)rand.NextDouble() * 2f - 1f) * 8f;
                    break;
            }
            return BlockerArcsAt(slot, anchor, mirror, d, sphereCount);
        }

        /// <summary>A blocker slot's arcs with its anchor at `anchor`, in absolute degrees.</summary>
        private static List<ArcSpec> BlockerArcsAt(PhraseSlot slot, float anchor, bool mirrored,
                                                   ProgressionDials d, int spheres)
        {
            List<ArcSpec> arcs = new List<ArcSpec>(slot.blockers.Length);
            for (int i = 0; i < slot.blockers.Length; i++)
            {
                BlockerArc b = slot.blockers[i];
                float arcSpan = Mathf.Clamp(b.spanDeg, 10f, 356f);
                float arcCentre = anchor + (mirrored ? -b.offsetDeg : b.offsetDeg);
                ArcSpec arc = new ArcSpec
                {
                    startAngleDeg = Mathf.Repeat(arcCentre - arcSpan * 0.5f, 360f),
                    arcAngleDeg = arcSpan,
                    thickness = d.tubeRadius * Mathf.Clamp(b.heightFrac, 0.08f, 0.6f),
                    // Rails (long arcs) are phase 5; until the segment pool is checked against
                    // them, depth stays within what a single tunnel segment carries comfortably.
                    depth = Mathf.Clamp(b.depth, 0.2f, 3f),
                    colourIndex = -1
                };
                // An authored colour is only meaningful if the level has that sphere.
                if (b.colour >= 0 && b.colour < spheres)
                {
                    arc.isColourCoded = true;
                    arc.colourIndex = b.colour;
                }
                arcs.Add(arc);
            }
            return arcs;
        }

        /// <summary>A gap slot's walls - everything between its gaps - in absolute degrees.</summary>
        private static List<ArcSpec> GapArcsAt(PhraseSlot slot, float baseAngle, bool mirrored,
                                               float thickness, float depth)
        {
            List<ArcSpec> arcs = new List<ArcSpec>();

            // Sort the gaps around the tube, then wall off everything between them.
            int n = slot.gapAngles.Length;
            float[] gaps = new float[n];
            for (int g = 0; g < n; g++)
            {
                float raw = mirrored ? -slot.gapAngles[g] : slot.gapAngles[g];
                gaps[g] = Mathf.Repeat(baseAngle + raw, 360f);
            }
            System.Array.Sort(gaps);

            float half = slot.gapDeg * 0.5f;
            for (int g = 0; g < n; g++)
            {
                float from = gaps[g] + half;
                float span = (n == 1)
                    ? 360f - slot.gapDeg
                    : Mathf.Repeat((gaps[(g + 1) % n] - half) - from, 360f);

                if (span < 10f) continue;      // too thin to read; drop it

                arcs.Add(new ArcSpec
                {
                    startAngleDeg = from,
                    arcAngleDeg = span,
                    thickness = thickness,
                    depth = depth,
                    colourIndex = -1
                });
            }
            return arcs;
        }

        private static float GapThickness(PhraseSlot slot, ProgressionDials d)
        {
            return d.tubeRadius * Mathf.Clamp(slot.thicknessFrac, 0.08f, 0.6f);
        }

        private static float GapDepth(PhraseSlot slot, ProgressionDials d)
        {
            return Mathf.Lerp(0.4f, 2.0f, Mathf.Clamp01(slot.depthBias) * d.depthIntensity);
        }

        /// <summary>The slot colours one of its arcs for a sphere this level actually has.</summary>
        private bool HasOwnColour(PhraseSlot slot)
        {
            if (!slot.IsBlocker) return false;
            foreach (BlockerArc b in slot.blockers)
                if (b.colour >= 0 && b.colour < sphereCount) return true;
            return false;
        }

        /// <summary>
        /// What getting past these arcs costs from the current line: the nearest rotation that
        /// clears them for every sphere, or a hop over them once the level has taught jumping,
        /// whichever is cheaper (`allowHop` false keeps the steer unless nothing else works).
        /// False when neither is possible.
        /// </summary>
        private bool CostArcs(List<ArcSpec> arcs, ProgressionDials d, bool allowHop,
                              out float required, out float delta, out bool requiresJump, out bool jumped)
        {
            bool tight = false;
            bool canSteer = ActionCost.NearestClearDelta(arcs, currentAngle, sphereCount,
                                ActionCost.SphereHalfDeg + ActionCost.ComfortDeg, out delta);
            if (!canSteer)
            {
                canSteer = ActionCost.NearestClearDelta(arcs, currentAngle, sphereCount,
                                ActionCost.SphereHalfDeg, out delta);
                tight = canSteer;
            }

            float steerCost = float.PositiveInfinity;
            if (canSteer)
            {
                steerCost = ActionCost.SteerSeconds(delta, d.angularSpeed) + ActionCost.DecisionLatency;
                if (tight) steerCost += 0.12f;      // a squeeze has to be hit, not merely reached
            }

            // Jumping: only once the level has taught it, so an opening level can never quietly
            // depend on a verb the player has not met.
            float jumpCost = float.PositiveInfinity;
            if (d.Has(Mechanic.Jump) && (allowHop || !canSteer))
            {
                float height = 0f, depth = 0f;
                for (int a = 0; a < arcs.Count; a++)
                {
                    height = Mathf.Max(height, arcs[a].thickness / Mathf.Max(0.1f, d.tubeRadius));
                    depth = Mathf.Max(depth, arcs[a].depth);
                }
                jumpCost = ActionCost.JumpOverSeconds(height, depth, d.speed);
            }

            requiresJump = !canSteer;
            jumped = jumpCost < steerCost;
            required = jumped ? jumpCost : steerCost;
            return canSteer || !float.IsInfinity(jumpCost);
        }

        /// <summary>
        /// Whether `spheres` spheres can steer through every ring of a phrase. Rotation and
        /// mirroring cannot change the answer, so one layout at angle zero decides it. Walls and
        /// full rings are meant to be jumped and always pass. A multi-sphere deck keeps only
        /// phrases that pass, so a one-gap wall built for a single sphere never turns into a
        /// forced jump for two.
        /// </summary>
        public static bool PhrasePassable(LevelPhrase phrase, int spheres, ProgressionDials d)
        {
            if (spheres <= 1) return true;
            foreach (PhraseSlot slot in phrase.slots)
            {
                if (slot.RequiresJump) continue;

                List<ArcSpec> arcs;
                if (slot.IsBlocker)
                {
                    bool fullRing = true;
                    foreach (BlockerArc b in slot.blockers) fullRing &= b.IsFullRing;
                    if (fullRing) continue;
                    arcs = BlockerArcsAt(slot, 0f, false, d, spheres);
                }
                else
                {
                    arcs = GapArcsAt(slot, 0f, false, 1f, 1f);
                }

                float delta;
                if (!ActionCost.NearestClearDelta(arcs, 0f, spheres, ActionCost.SphereHalfDeg, out delta))
                    return false;
            }
            return true;
        }

        /// <summary>Turns "where it is safe" into "where the arcs are".</summary>
        private RingSpec BuildRing(PhraseSlot slot, LevelPhrase phrase, float baseAngle,
                                   float safeAngle, float ringZ, ProgressionDials d,
                                   List<ArcSpec> prebuilt = null)
        {
            RingSpec spec = new RingSpec();
            spec.z = ringZ;
            spec.phraseId = phrase.id;
            spec.safeAngleDeg = safeAngle;

            float thickness = GapThickness(slot, d);
            float depth = GapDepth(slot, d);

            List<ArcSpec> arcs = new List<ArcSpec>();

            if (prebuilt != null)
            {
                arcs.AddRange(prebuilt);
            }
            else if (slot.RequiresJump)
            {
                // A fully closed ring. Left a hair short of 360 so the seam never z-fights.
                arcs.Add(new ArcSpec
                {
                    startAngleDeg = baseAngle,
                    arcAngleDeg = 356f,
                    thickness = thickness,
                    depth = depth
                });
            }
            else
            {
                arcs.AddRange(GapArcsAt(slot, baseAngle, mirror, thickness, depth));
            }

            ApplyColour(slot, arcs, d);
            ApplyMotion(slot, spec, d);

            spec.arcs = arcs.ToArray();

            // A chasing ring aims its widest arc at the player, so what closes on them is a wall
            // rather than a gap edge.
            if (spec.motion == RingArcGroup.Motion.Chase && arcs.Count > 0)
            {
                ArcSpec widest = arcs[0];
                for (int i = 1; i < arcs.Count; i++)
                    if (arcs[i].arcAngleDeg > widest.arcAngleDeg) widest = arcs[i];
                spec.chaseOffsetDeg = widest.startAngleDeg + widest.arcAngleDeg * 0.5f;
            }

            return spec;
        }

        /// <summary>
        /// Colour-codes arcs. On a colour slot every arc becomes a shield, so the ring is a
        /// readable "which of my spheres fits" question rather than a lottery; elsewhere the
        /// level's colourShare decides how many arcs are shields instead of solid hazards.
        /// </summary>
        private void ApplyColour(PhraseSlot slot, List<ArcSpec> arcs, ProgressionDials d)
        {
            int palette = Mathf.Max(1, sphereCount);

            for (int i = 0; i < arcs.Count; i++)
            {
                // A figure's own colours were costed as laid out; leave them alone.
                if (arcs[i].isColourCoded) continue;

                bool shield = slot.colourGap || rand.NextDouble() < d.colourShare;

                if (!shield || palette <= 1)
                {
                    arcs[i].isColourCoded = false;
                    arcs[i].colourIndex = -1;
                    continue;
                }

                arcs[i].isColourCoded = true;
                arcs[i].colourIndex = rand.Next(0, palette);

                if (d.Has(Mechanic.ColourShift) && rand.NextDouble() < 0.5)
                {
                    arcs[i].colourShift = true;
                    arcs[i].colourShiftInterval = Mathf.Lerp(1.8f, 1.0f, d.motionIntensity);
                    arcs[i].colourShiftSeed = rand.Next(int.MinValue, int.MaxValue);
                }
            }
        }

        private void ApplyMotion(PhraseSlot slot, RingSpec spec, ProgressionDials d)
        {
            MotionHint hint = slot.motion;
            if (hint == MotionHint.Inherit)
            {
                if (rand.NextDouble() >= d.motionShare) { spec.motion = RingArcGroup.Motion.Static; return; }

                // Pick a behaviour the level has actually unlocked.
                List<MotionHint> options = new List<MotionHint>();
                if (d.Has(Mechanic.Spin)) options.Add(MotionHint.Spin);
                if (d.Has(Mechanic.Snap)) options.Add(MotionHint.Snap);
                if (d.Has(Mechanic.MovingGap)) options.Add(MotionHint.Oscillate);
                if (d.Has(Mechanic.Reactive)) options.Add(MotionHint.Chase);
                if (options.Count == 0) { spec.motion = RingArcGroup.Motion.Static; return; }
                hint = options[rand.Next(0, options.Count)];
            }

            float k = Mathf.Clamp01(d.motionIntensity);
            float sign = rand.NextDouble() < 0.5 ? -1f : 1f;

            switch (hint)
            {
                case MotionHint.Spin:
                    spec.motion = RingArcGroup.Motion.Spin;
                    spec.spinDegPerSec = sign * Mathf.Lerp(25f, 110f, k);
                    break;

                case MotionHint.Snap:
                    spec.motion = RingArcGroup.Motion.Snap;
                    spec.snapInterval = Mathf.Lerp(2.4f, 1.1f, k);
                    spec.snapTurnDuration = Mathf.Lerp(0.26f, 0.13f, k);
                    spec.telegraphWindow = Mathf.Clamp(Mathf.Lerp(0.95f, 0.6f, k) * d.telegraphScale,
                                                       0.15f, spec.snapInterval * 0.8f);
                    spec.snapAngle = sign * ((k > 0.45f && rand.NextDouble() < 0.5) ? 180f : 90f);
                    break;

                case MotionHint.Oscillate:
                    spec.motion = RingArcGroup.Motion.Oscillate;
                    spec.oscAmplitudeDeg = Mathf.Lerp(40f, 95f, k);
                    spec.oscPeriod = Mathf.Lerp(2.6f, 1.3f, k);
                    spec.oscPhase = (float)(rand.NextDouble() * Mathf.PI * 2f);
                    break;

                case MotionHint.Chase:
                    spec.motion = RingArcGroup.Motion.Chase;
                    // Capped well under the player's own steering speed (4 rad/s is about 230 deg/s),
                    // so committing early always beats it and the ring is pressure, not a wall.
                    spec.chaseDegPerSec = Mathf.Lerp(35f, 85f, k);
                    spec.chaseEngageDistance = Mathf.Lerp(120f, 80f, k);
                    break;

                default:
                    spec.motion = RingArcGroup.Motion.Static;
                    break;
            }
        }

        private LevelPhrase PickPhrase(System.Random rng, LevelPhrase avoid, ProgressionDials d)
        {
            // An endless run's mechanic set widens as it goes, so the deck is rebuilt when it changes.
            if (rampAt != null && d.mechanics != deckMechanics) RebuildDeck(d);

            for (int attempt = 0; attempt < 6; attempt++)
            {
                LevelPhrase p = WeightedPick(rng);
                if (avoid == null || p.id != avoid.id || deck.Count == 1) return p;
            }
            return WeightedPick(rng);
        }

        private LevelPhrase WeightedPick(System.Random rng)
        {
            float total = 0f;
            for (int i = 0; i < deckWeights.Count; i++) total += deckWeights[i];
            double roll = rng.NextDouble() * total;
            for (int i = 0; i < deck.Count; i++)
            {
                roll -= deckWeights[i];
                if (roll < 0.0) return deck[i];
            }
            return deck[deck.Count - 1];
        }

        private Mechanic deckMechanics = Mechanic.None;

        /// <summary>
        /// A hand-authored level deals exactly the figures it names, at the weights it names.
        /// Otherwise the deck is every phrase the mechanic flags allow, with the level's
        /// featured mechanic dealt more often.
        /// </summary>
        private void RebuildDeck(ProgressionDials d)
        {
            deck = new List<LevelPhrase>();
            deckWeights = new List<float>();

            if (d.phraseIds != null && d.phraseIds.Length > 0)
            {
                for (int i = 0; i < d.phraseIds.Length; i++)
                {
                    LevelPhrase p = PhraseLibrary.ById(d.phraseIds[i]);
                    if (p == null)
                    {
                        Debug.LogWarning("[LevelComposer] Unknown phrase '" + d.phraseIds[i] + "' in level " + d.level);
                        continue;
                    }
                    float w = (d.phraseWeights != null && i < d.phraseWeights.Length) ? d.phraseWeights[i] : 1f;
                    if (w <= 0f) continue;
                    deck.Add(p);
                    deckWeights.Add(w);
                }
            }
            else
            {
                foreach (LevelPhrase p in PhraseLibrary.Deck(d.mechanics))
                {
                    bool featured = d.featured != Mechanic.None && (p.requires & d.featured) != 0;
                    deck.Add(p);
                    deckWeights.Add(featured ? Mathf.Max(1f, d.featuredWeight) : 1f);
                }
            }

            // More than one sphere: keep only what the whole set can actually steer through.
            if (sphereCount > 1)
            {
                for (int i = deck.Count - 1; i >= 0; i--)
                {
                    if (PhrasePassable(deck[i], sphereCount, d)) continue;
                    deck.RemoveAt(i);
                    deckWeights.RemoveAt(i);
                }
            }

            if (deck.Count == 0)
            {
                deck.Add(PhraseLibrary.ById(sphereCount > 1 ? "Lanes" : "Corridor"));
                deckWeights.Add(1f);
            }
            deckMechanics = d.mechanics;
        }
    }
}
