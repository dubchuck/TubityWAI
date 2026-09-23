using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>One of the sixteen eight-level worlds that make up Progression Test 1.</summary>
    public class World
    {
        public int index;              // 1..16
        public string name;
        public Mechanic introduces;
        public EnvironmentTheme environment;

        public World(int index, string name, Mechanic introduces, EnvironmentTheme environment)
        {
            this.index = index; this.name = name;
            this.introduces = introduces; this.environment = environment;
        }

        public int FirstLevel { get { return (index - 1) * ProgressionV2.LevelsPerWorld + 1; } }
        public int LastLevel { get { return index * ProgressionV2.LevelsPerWorld; } }
    }

    /// <summary>
    /// The 128-level ladder: sixteen worlds of eight, each introducing one mechanic and wearing
    /// one environment. Documentation/Level-Progression-Spec.md is the design this follows.
    ///
    /// Worlds 1-3 are authored level by level (LevelRecipes): each of those levels adds one
    /// specific thing, starting from 20 seconds of single arcs in open tube. Worlds 4-16 are
    /// generated from the world table below.
    ///
    /// Inside a world the eight levels run introduce - practice - combine - finale:
    ///
    ///     1-2   the new mechanic nearly alone, with the flow band pulled down
    ///     3-5   the new mechanic folded in with everything learned so far
    ///     6-7   the same at the full band
    ///     8     a finale level: wider band, higher spike, longer
    ///
    /// Everything difficulty-related routes through the pressure model in ActionCost, so the
    /// whole ladder is one readable set of curves rather than 128 hand-tuned guesses.
    /// </summary>
    public static class ProgressionV2
    {
        public const int LevelsPerWorld = 8;
        public const int WorldCount = 16;
        public const int LevelCount = LevelsPerWorld * WorldCount;   // 128

        /// <summary>Level numbers for this mode live in their own band so they can never collide
        /// with the original campaign (1-24), the test levels (101+) or the themed sets (201+).</summary>
        public const int LevelNumberBase = 1000;

        private static List<World> worlds;

        public static List<World> Worlds
        {
            get
            {
                if (worlds == null) BuildWorlds();
                return worlds;
            }
        }

        private static void BuildWorlds()
        {
            worlds = new List<World>(WorldCount)
            {
                // Crossover, Rails and Twins have no figures yet (spec phases 4-5); their worlds
                // lean on the mechanics listed alongside them until they do.
                new World( 1, "FIRST LIGHT",   Mechanic.Steer,       EnvironmentTheme.Grid),
                new World( 2, "LIFT OFF",      Mechanic.Jump,        EnvironmentTheme.Grid),
                new World( 3, "SHAPES",        Mechanic.Shapes,      EnvironmentTheme.Space),
                new World( 4, "CAROUSEL",      Mechanic.Spin,        EnvironmentTheme.Space),
                new World( 5, "PENDULUM",      Mechanic.MovingGap,   EnvironmentTheme.Crystal),
                new World( 6, "CROSSOVER",     Mechanic.Crossover | Mechanic.Compound, EnvironmentTheme.Crystal),
                new World( 7, "CLOCKWORK",     Mechanic.Snap,        EnvironmentTheme.Jungle),
                new World( 8, "RAILS",         Mechanic.Rails | Mechanic.Pinch | Mechanic.GateRing | Mechanic.Corridor,
                                                                     EnvironmentTheme.Jungle),
                new World( 9, "TWINS",         Mechanic.Twins,       EnvironmentTheme.Underwater),
                new World(10, "PRISM",         Mechanic.Colour,      EnvironmentTheme.Underwater),
                new World(11, "LONG CURVE",    Mechanic.Drift,       EnvironmentTheme.Volcano),
                new World(12, "SPECTRUM",      Mechanic.ColourShift, EnvironmentTheme.Volcano),
                new World(13, "HELIX",         Mechanic.Spiral,      EnvironmentTheme.AsteroidBelt),
                new World(14, "PURSUIT",       Mechanic.Reactive,    EnvironmentTheme.AsteroidBelt),
                new World(15, "BLACKOUT",      Mechanic.LowPreview,  EnvironmentTheme.SolarSystem),
                new World(16, "EVERYTHING",    Mechanic.None,        EnvironmentTheme.SolarSystem),
            };
        }

        public static World WorldOf(int level)
        {
            int idx = Mathf.Clamp((level - 1) / LevelsPerWorld, 0, WorldCount - 1);
            return Worlds[idx];
        }

        /// <summary>Every mechanic unlocked by the end of this world, cumulative.</summary>
        public static Mechanic MechanicsThrough(int worldIndex)
        {
            Mechanic m = Mechanic.Steer;
            for (int i = 0; i < worldIndex && i < Worlds.Count; i++) m |= Worlds[i].introduces;
            return m;
        }

        public static List<LevelConfig> CreateAll()
        {
            List<LevelConfig> list = new List<LevelConfig>(LevelCount);
            for (int n = 1; n <= LevelCount; n++) list.Add(CreateLevel(n));
            return list;
        }

        /// <summary>
        /// The dials for a level. `applyAssist` is on for real play, so a level the player keeps
        /// failing quietly eases; the validator passes false so it reports the authored curve.
        /// </summary>
        public static ProgressionDials CreateDials(int level, bool applyAssist = true)
        {
            int n = Mathf.Clamp(level, 1, LevelCount);
            float t = (n - 1) / (float)(LevelCount - 1);

            World world = WorldOf(n);
            int phase = (n - 1) % LevelsPerWorld;        // 0..7 within the world
            bool isFinale = phase == LevelsPerWorld - 1;

            ProgressionDials d = new ProgressionDials();
            d.level = n;
            d.seed = unchecked(n * 92821 + 7717);

            LevelRecipe recipe = LevelRecipes.For(n);

            // ---- Kinematics ---------------------------------------------------------------
            // A sawtooth: each world opens a little slower than the last one finished, so a new
            // mechanic is met with room to spare, then climbs through the world.
            int w = world.index - 1;
            d.speed = (recipe != null && recipe.HasKinematics) ? recipe.speed
                    : Mathf.Lerp(WorldSpeedStart[w], WorldSpeedEnd[w], phase / (float)(LevelsPerWorld - 1));
            // Ring spacing grows with speed, so the cadence falls from 0.50 s to about 0.31 s a
            // marker instead of the 0.14 s the old flat 5-unit interval collapsed to at speed 36.
            d.markerInterval = 5f * Mathf.Pow(d.speed / 11f, 0.6f);
            d.angularSpeed = 4f;
            d.tubeRadius = 5f;
            // Short early levels for a fast first win; a finale is the world's longest level.
            d.levelSeconds = (recipe != null && recipe.HasKinematics) ? recipe.seconds
                           : isFinale ? WorldSecondsFinale[w]
                           : Mathf.Lerp(WorldSecondsStart[w], WorldSecondsFinale[w] / 1.2f, phase / 6f);

            // ---- Flow band ----------------------------------------------------------------
            // The teaching dip: the first two levels of every world sit low so the new mechanic
            // is met with room to spare, then the band climbs back through the world.
            float phaseScale;
            switch (phase)
            {
                case 0:  phaseScale = 0.80f; break;
                case 1:  phaseScale = 0.88f; break;
                case 2:
                case 3:
                case 4:  phaseScale = 1.00f; break;
                case 5:
                case 6:  phaseScale = 1.06f; break;
                default: phaseScale = 1.12f; break;      // finale
            }

            if (recipe != null && recipe.HasBand)
            {
                d.bandFloor     = recipe.bandFloor;
                d.bandCeiling   = recipe.bandCeiling;
                d.spikePressure = recipe.spikePressure;
                d.restPressure  = Mathf.Max(0.12f, recipe.bandFloor - 0.05f);
            }
            else
            {
                d.restPressure  = Mathf.Lerp(0.22f, 0.34f, t);
                d.bandFloor     = Mathf.Lerp(0.40f, 0.68f, t) * phaseScale;
                d.bandCeiling   = Mathf.Lerp(0.58f, 0.86f, t) * phaseScale;
                d.spikePressure = Mathf.Lerp(0.72f, 0.94f, t) * Mathf.Min(1f, phaseScale);
            }

            if (applyAssist)
            {
                float assist = ProgressionSave.AssistFactor(n);
                if (assist < 1f)
                {
                    d.bandFloor *= assist;
                    d.bandCeiling *= assist;
                    d.spikePressure *= assist;
                    d.telegraphScale /= assist;      // longer warnings, too
                }
            }

            d.bandFloor     = Mathf.Clamp(d.bandFloor, 0.15f, 0.80f);
            d.bandCeiling   = Mathf.Clamp(d.bandCeiling, d.bandFloor + 0.05f, 0.90f);
            d.spikePressure = Mathf.Clamp(d.spikePressure, d.bandCeiling, ProgressionDials.HardCeiling);

            // ---- Mechanics ----------------------------------------------------------------
            // Phase 0 meets the new mechanic nearly alone; from phase 1 the whole toolbox is back.
            // "Alone" means steering plus jumping - the two universal verbs - and nothing else,
            // and not even jumping in world 1, which is where jumping has yet to be taught.
            if (phase == 0 && world.introduces != Mechanic.None && recipe == null)
            {
                Mechanic basics = Mechanic.Steer;
                if (world.index >= 2) basics |= Mechanic.Jump;
                d.mechanics = basics | world.introduces;
            }
            else
            {
                d.mechanics = MechanicsThrough(world.index);
            }

            // An authored level deals exactly its own figures. A generated one deals everything
            // its flags allow, weighted toward the world's own mechanic - heavily while it is
            // being learned (slots 1-4), less once the world starts combining.
            if (recipe != null)
            {
                d.phraseIds = recipe.phraseIds;
                d.phraseWeights = recipe.phraseWeights;
            }
            else
            {
                d.featured = world.introduces;
                d.featuredWeight = phase < 4 ? 4f : 2f;
            }

            // One sphere until world 9, two from there on. The composer clears every ring for
            // both, and colour (world 10) is what makes the pair more than a harder single.
            d.sphereCount = world.index >= 9 ? 2 : 1;

            // ---- Content scalars ----------------------------------------------------------
            float afterColour = RampFrom(n, WorldFirstLevel(10));
            float afterMotion = RampFrom(n, WorldFirstLevel(4));
            float afterDepth  = RampFrom(n, WorldFirstLevel(6));

            d.colourShare     = d.Has(Mechanic.Colour) ? Mathf.Lerp(0.25f, 0.55f, afterColour) : 0f;
            d.motionShare     = d.Has(Mechanic.Spin)   ? Mathf.Lerp(0.20f, 0.50f, afterMotion) : 0f;
            d.motionIntensity = d.Has(Mechanic.Spin)   ? afterMotion : 0f;
            d.depthIntensity  = d.Has(Mechanic.Compound) ? Mathf.Lerp(0.3f, 1f, afterDepth) : 0f;
            d.telegraphScale  = Mathf.Lerp(1.35f, 0.85f, t);

            // World 11 banks the tube, world 13 corkscrews the whole obstacle field, world 15
            // shortens the sight line. Each stays on once introduced, ramping to the last level.
            float afterDrift   = RampFrom(n, WorldFirstLevel(11));
            float afterSpiral  = RampFrom(n, WorldFirstLevel(13));
            float afterDark    = RampFrom(n, WorldFirstLevel(15));

            d.driftStrength    = d.Has(Mechanic.Drift)      ? Mathf.Lerp(0.5f, 1.4f, afterDrift)   : 0f;
            d.spiralDegPerUnit = d.Has(Mechanic.Spiral)     ? Mathf.Lerp(0.15f, 0.55f, afterSpiral) : 0f;
            d.previewScale     = d.Has(Mechanic.LowPreview) ? Mathf.Lerp(0.8f, 0.5f, afterDark)     : 1f;

            // A level met on its own terms: nothing moving, no random shields.
            if (recipe != null && recipe.calm)
            {
                d.motionShare = 0f;
                d.colourShare = 0f;
            }

            // Cosmetic sway: straight through world 2, authored in world 3, on from world 4,
            // stronger once it drifts.
            float generatedSway = world.index <= 3 ? 0f : d.driftStrength > 0f ? 3.0f : 2.5f;
            d.curveAmplitude = (recipe != null && recipe.curveAmplitude >= 0f) ? recipe.curveAmplitude : generatedSway;

            // ---- Presentation -------------------------------------------------------------
            d.environment = world.environment;
            d.worldName = world.name;
            d.levelName = recipe != null ? recipe.hook : LevelName(world, phase);

            return d;
        }

        public static LevelConfig CreateLevel(int level)
        {
            int n = Mathf.Clamp(level, 1, LevelCount);
            ProgressionDials d = CreateDials(n);

            // The curve is the cosmetic S-bend it has always been; only driftStrength makes it
            // cost anything. Drift needs a curving tube to drift against.
            bool curves = d.curveAmplitude > 0f || d.driftStrength > 0f;

            // Scenery is placed outside the tube wall (EnvironmentScenery spawns props at
            // tubeRadius + gap), so an opaque wall hides the entire environment and leaves only a
            // sliver of sky at the far end. A themed level needs the see-through tube, the same
            // one the standalone themed sets use. The camera's clear flags are safe to leave
            // alone: EnvironmentManager.BuildSky runs after GameSetup's camera pass and puts
            // them back to Skybox.
            bool showEnvironment = d.environment != EnvironmentTheme.None;

            LevelConfig config = new LevelConfig(
                LevelNumberBase + n, d.speed, 0f, 2.2f,
                isTest: false, name: d.levelName, theme: d.worldName,
                curves: curves, curveFreq: 0.035f, curveAmp: Mathf.Max(d.curveAmplitude, curves ? 2.5f : 0f),
                transparentTube: showEnvironment);

            config.driftStrength = d.driftStrength;
            config.spiralDegPerUnit = d.spiralDegPerUnit;
            config.previewScale = d.previewScale;
            config.WithEnvironment(d.environment);
            config.WithLength(d.LengthUnits);
            config.WithSeed(d.seed);
            config.WithComposer(new LevelComposer(d));

            config.forcedSphereCount = d.sphereCount;

            // The Shield: with twins, a hit costs a sphere rather than the run, and Add-Sphere
            // pickups turn up to win it back. Off wherever colour is in play - the survivor slides
            // onto the main line, and colour rings ahead were laid out for the twin who rode there.
            bool shield = d.sphereCount > 1 && !d.Has(Mechanic.Colour);
            config.allowPartialDeath = shield;
            config.spawnAddSpherePowerup = shield;

            // Long levels bank progress every 28 seconds of travel. Losing a minute or more to
            // one mistake is the fastest way out of flow and into quitting; the early, short
            // levels stay all-or-nothing because there is little to lose there.
            if (d.levelSeconds >= 60f) config.WithCheckpoints(d.speed * 28f);

            return config;
        }

        /// <summary>The level number shown to the player, 1..128.</summary>
        public static int DisplayNumber(LevelConfig config)
        {
            return config != null ? config.levelNumber - LevelNumberBase : 0;
        }

        public static bool IsProgressionLevel(LevelConfig config)
        {
            if (config == null) return false;
            int n = config.levelNumber - LevelNumberBase;
            return n >= 1 && n <= LevelCount;
        }

        // Per-world kinematics for the generated worlds (index 0 = world 1). Worlds 1-3 are
        // authored in LevelRecipes; their entries here only keep the tables complete.
        private static readonly float[] WorldSpeedStart =
            { 10f, 11f, 12f, 13f, 14f, 15f, 16f, 17.5f, 18f, 19f, 20f, 21f, 22f, 23f, 24f, 26f };
        private static readonly float[] WorldSpeedEnd =
            { 12f, 13f, 14f, 15f, 16.5f, 18f, 19f, 20.5f, 21f, 22f, 23.5f, 25f, 26f, 27.5f, 29f, 32f };
        private static readonly float[] WorldSecondsStart =
            { 20f, 25f, 28f, 30f, 34f, 36f, 38f, 40f, 40f, 44f, 46f, 48f, 50f, 52f, 54f, 60f };
        private static readonly float[] WorldSecondsFinale =
            { 38f, 44f, 48f, 52f, 56f, 60f, 62f, 66f, 66f, 70f, 74f, 78f, 80f, 84f, 86f, 95f };

        private static int WorldFirstLevel(int worldIndex)
        {
            return (worldIndex - 1) * LevelsPerWorld + 1;
        }

        /// <summary>0 at level `from`, 1 at the last level, clamped.</summary>
        private static float RampFrom(int n, int from)
        {
            if (n <= from) return 0f;
            return Mathf.Clamp01((n - from) / (float)(LevelCount - from));
        }

        private static string LevelName(World world, int phase)
        {
            switch (phase)
            {
                case 0: return world.name;
                case 1: return world.name + " II";
                case 2: return world.name + " III";
                case 3: return world.name + " IV";
                case 4: return world.name + " V";
                case 5: return world.name + " VI";
                case 6: return world.name + " VII";
                default: return world.name + " FINALE";
            }
        }
    }
}
