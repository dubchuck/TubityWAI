using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// One endless run type. Each mode isolates a different facet of the game so the set feels
    /// like several games rather than one game at seven speeds: Velocity taxes only reaction,
    /// Density only planning, Kaleidoscope only timing, Spectrum only colour memory, and Pure
    /// Flow lets the adaptive director hold the player in the band indefinitely.
    ///
    /// A mode is a function from distance travelled to dials. LevelComposer calls it as it
    /// streams, so the run ramps without any level boundaries.
    /// </summary>
    public class EndlessMode
    {
        public string id = "";
        public string displayName = "";
        public string blurb = "";
        public EnvironmentTheme environment = EnvironmentTheme.None;
        public EnvironmentBlend blend;

        // ---- Ramps, all keyed on distance travelled in tube units ------------------------
        public float startSpeed = 16f;
        public float endSpeed = 28f;
        public float speedRamp = 12000f;

        public float startPressure = 0.50f;
        public float endPressure = 0.88f;
        public float pressureRamp = 12000f;

        /// <summary>Mechanics come online in this order, one every `unlockEvery` units.</summary>
        public Mechanic baseMechanics = Mechanic.Steer;
        public Mechanic[] unlockOrder = new Mechanic[0];
        public float unlockEvery = 1500f;

        public float startColourShare = 0f, endColourShare = 0f;
        public float startMotionShare = 0f, endMotionShare = 0f;
        public float startMotionIntensity = 0f, endMotionIntensity = 1f;
        public float depthIntensity = 0f;

        /// <summary>Pure Flow only: the director steers pressure from measured performance
        /// instead of following the distance ramp.</summary>
        public bool adaptive = false;

        /// <summary>Set at run time by EndlessDirector; shifts the pressure target up or down.</summary>
        [System.NonSerialized] public float adaptiveBias = 0f;

        public float SpeedAt(float z)
        {
            return Mathf.Lerp(startSpeed, endSpeed, Mathf.Clamp01(z / Mathf.Max(1f, speedRamp)));
        }

        public Mechanic MechanicsAt(float z)
        {
            Mechanic m = baseMechanics;
            int unlocked = Mathf.FloorToInt(z / Mathf.Max(1f, unlockEvery));
            for (int i = 0; i < unlockOrder.Length && i < unlocked; i++) m |= unlockOrder[i];
            return m;
        }

        /// <summary>The dials in force at this distance. This is what the composer streams from.</summary>
        public ProgressionDials DialsAt(float z)
        {
            float t = Mathf.Clamp01(z / Mathf.Max(1f, pressureRamp));

            ProgressionDials d = new ProgressionDials();
            d.level = 0;
            d.seed = 0;
            d.speed = SpeedAt(z);
            d.markerInterval = 5f * Mathf.Pow(Mathf.Max(1f, d.speed) / 11f, 0.6f);
            d.angularSpeed = 4f;
            d.tubeRadius = 5f;
            d.levelSeconds = 0f;                       // endless: no length

            float pressure = Mathf.Lerp(startPressure, endPressure, t) + adaptiveBias;
            pressure = Mathf.Clamp(pressure, 0.25f, ProgressionDials.HardCeiling);

            d.restPressure = pressure * 0.45f;
            d.bandFloor = pressure * 0.85f;
            d.bandCeiling = pressure;
            d.spikePressure = Mathf.Min(ProgressionDials.HardCeiling, pressure * 1.05f);

            d.mechanics = MechanicsAt(z);
            d.colourShare = Mathf.Lerp(startColourShare, endColourShare, t);
            d.motionShare = Mathf.Lerp(startMotionShare, endMotionShare, t);
            d.motionIntensity = Mathf.Lerp(startMotionIntensity, endMotionIntensity, t);
            d.depthIntensity = depthIntensity;
            d.telegraphScale = Mathf.Lerp(1.2f, 0.85f, t);
            d.environment = environment;
            d.worldName = displayName;
            d.levelName = displayName;
            return d;
        }

        /// <summary>A LevelConfig that runs this mode forever.</summary>
        public LevelConfig CreateConfig()
        {
            adaptiveBias = 0f;

            ProgressionDials seedDials = DialsAt(0f);
            seedDials.seed = unchecked(id.GetHashCode() ^ (int)(Random.value * 1000000));

            LevelConfig config = new LevelConfig(
                LevelNumber, startSpeed, 0f, 2.2f,
                isTest: true, name: displayName, theme: "ENDLESS");

            LevelComposer composer = new LevelComposer(seedDials);
            composer.rampAt = DialsAt;
            composer.endZ = float.PositiveInfinity;

            config.WithComposer(composer);
            config.WithEnvironment(environment);
            if (blend != null) config.WithEnvironmentBlend(blend);
            config.levelLength = 0f;                   // endless: no finish gate
            return config;
        }

        /// <summary>Endless configs live in their own number band, clear of every other mode.</summary>
        public int LevelNumber { get { return 900 + IndexOf(id); } }

        // =====================================================================================

        private static List<EndlessMode> modes;

        public static List<EndlessMode> All
        {
            get
            {
                if (modes == null) Build();
                return modes;
            }
        }

        public static EndlessMode ById(string id)
        {
            foreach (EndlessMode m in All) if (m.id == id) return m;
            return null;
        }

        private static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].id == id) return i;
            return 0;
        }

        private static void Build()
        {
            modes = new List<EndlessMode>();

            // 1. The showcase: the director holds you in the flow band for as long as you last.
            modes.Add(new EndlessMode
            {
                id = "flow",
                displayName = "PURE FLOW",
                blurb = "Adapts to you. Stays hard enough to hold attention, never hard enough to break it.",
                environment = EnvironmentTheme.Grid,
                adaptive = true,
                startSpeed = 14f, endSpeed = 30f, speedRamp = 18000f,
                startPressure = 0.55f, endPressure = 0.78f, pressureRamp = 9000f,
                baseMechanics = Mechanic.Steer | Mechanic.Jump,
                unlockOrder = new[] { Mechanic.Colour, Mechanic.Spin, Mechanic.Snap,
                                      Mechanic.MovingGap, Mechanic.Compound, Mechanic.ColourShift },
                unlockEvery = 2000f,
                startColourShare = 0.15f, endColourShare = 0.45f,
                startMotionShare = 0.10f, endMotionShare = 0.45f,
                depthIntensity = 0.5f,
            });

            // 2. Reaction only: the tube empties out as it accelerates.
            modes.Add(new EndlessMode
            {
                id = "velocity",
                displayName = "VELOCITY",
                blurb = "Speed climbs without end. Few obstacles, no time to think about them.",
                environment = EnvironmentTheme.Space,
                startSpeed = 13f, endSpeed = 52f, speedRamp = 24000f,
                startPressure = 0.55f, endPressure = 0.80f, pressureRamp = 16000f,
                baseMechanics = Mechanic.Steer | Mechanic.Jump,
                unlockOrder = new Mechanic[0],
            });

            // 3. Planning only: speed barely moves, the tube fills up.
            modes.Add(new EndlessMode
            {
                id = "density",
                displayName = "DENSITY",
                blurb = "Speed holds steady. Everything else closes in.",
                environment = EnvironmentTheme.Volcano,
                startSpeed = 17f, endSpeed = 21f, speedRamp = 20000f,
                startPressure = 0.45f, endPressure = 0.93f, pressureRamp = 13000f,
                baseMechanics = Mechanic.Steer | Mechanic.Jump,
                unlockOrder = new[] { Mechanic.Colour, Mechanic.Compound, Mechanic.Spin, Mechanic.Snap },
                unlockEvery = 1200f,
                startColourShare = 0.2f, endColourShare = 0.5f,
                startMotionShare = 0.1f, endMotionShare = 0.35f,
                depthIntensity = 1f,
            });

            // 4. Timing only: everything turns.
            modes.Add(new EndlessMode
            {
                id = "kaleidoscope",
                displayName = "KALEIDOSCOPE",
                blurb = "Every ring is turning. Read the rotation, not the gap.",
                environment = EnvironmentTheme.Crystal,
                startSpeed = 16f, endSpeed = 26f, speedRamp = 15000f,
                startPressure = 0.50f, endPressure = 0.85f, pressureRamp = 12000f,
                baseMechanics = Mechanic.Steer | Mechanic.Jump | Mechanic.Spin,
                unlockOrder = new[] { Mechanic.Snap, Mechanic.MovingGap },
                unlockEvery = 1800f,
                startMotionShare = 0.85f, endMotionShare = 1f,
                startMotionIntensity = 0.3f, endMotionIntensity = 1f,
            });

            // 5. Colour memory only.
            modes.Add(new EndlessMode
            {
                id = "spectrum",
                displayName = "SPECTRUM",
                blurb = "Colour shields all the way down, and they do not stay the colour you saw.",
                environment = EnvironmentTheme.Underwater,
                startSpeed = 15f, endSpeed = 25f, speedRamp = 15000f,
                startPressure = 0.48f, endPressure = 0.84f, pressureRamp = 12000f,
                baseMechanics = Mechanic.Steer | Mechanic.Jump | Mechanic.Colour,
                unlockOrder = new[] { Mechanic.ColourShift },
                unlockEvery = 2500f,
                startColourShare = 0.7f, endColourShare = 1f,
            });

            // 6. The scenic run: fixed, comfortable pressure, the whole environment set on tour.
            modes.Add(new EndlessMode
            {
                id = "odyssey",
                displayName = "ODYSSEY",
                blurb = "A long, calm run through every world. Go as far as you like.",
                environment = EnvironmentTheme.None,
                blend = EnvironmentBlend.From(EnvironmentTheme.None)
                                        .To(EnvironmentTheme.Grid, 1200f, blendLength: 400f)
                                        .To(EnvironmentTheme.Space, 3000f, blendLength: 500f)
                                        .To(EnvironmentTheme.Crystal, 5200f, blendLength: 500f)
                                        .To(EnvironmentTheme.Jungle, 7400f, blendLength: 500f)
                                        .To(EnvironmentTheme.Underwater, 9600f, blendLength: 500f)
                                        .To(EnvironmentTheme.Volcano, 11800f, blendLength: 500f)
                                        .To(EnvironmentTheme.AsteroidBelt, 14000f, blendLength: 500f)
                                        .To(EnvironmentTheme.SolarSystem, 16200f, blendLength: 600f),
                startSpeed = 15f, endSpeed = 22f, speedRamp = 20000f,
                startPressure = 0.42f, endPressure = 0.62f, pressureRamp = 18000f,
                baseMechanics = Mechanic.Steer | Mechanic.Jump,
                unlockOrder = new[] { Mechanic.Colour, Mechanic.Spin, Mechanic.Snap },
                unlockEvery = 4000f,
                startColourShare = 0.2f, endColourShare = 0.35f,
                startMotionShare = 0.1f, endMotionShare = 0.3f,
            });

            // 7. Everything, fast.
            modes.Add(new EndlessMode
            {
                id = "gauntlet",
                displayName = "GAUNTLET",
                blurb = "Every mechanic from the first ring, and it does not let up.",
                environment = EnvironmentTheme.AsteroidBelt,
                startSpeed = 18f, endSpeed = 44f, speedRamp = 10000f,
                startPressure = 0.62f, endPressure = 0.95f, pressureRamp = 7000f,
                baseMechanics = Mechanic.Steer | Mechanic.Jump | Mechanic.Colour | Mechanic.Spin,
                unlockOrder = new[] { Mechanic.Snap, Mechanic.MovingGap, Mechanic.Compound, Mechanic.ColourShift },
                unlockEvery = 700f,
                startColourShare = 0.3f, endColourShare = 0.55f,
                startMotionShare = 0.3f, endMotionShare = 0.6f,
                startMotionIntensity = 0.4f, endMotionIntensity = 1f,
                depthIntensity = 1f,
            });
        }
    }
}
