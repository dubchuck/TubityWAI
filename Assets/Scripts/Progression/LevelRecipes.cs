using System.Collections.Generic;

namespace TubityWAI.Progression
{
    /// <summary>
    /// A hand-written level: what it is called, exactly which figures it deals, and - for the
    /// opening worlds - how fast and long it runs and how hard it presses. Worlds 1-3 are
    /// authored in full because every one of their levels adds one specific thing (see
    /// Documentation/Level-Progression-Spec.md, section 6); worlds 9-10 author only their
    /// figures and hooks, since two spheres and colour need exact teaching order, and take
    /// their pacing from ProgressionV2's world table like the generated worlds.
    /// </summary>
    public class LevelRecipe
    {
        public string hook;
        public float speed;
        public float seconds;
        public float bandFloor;
        public float bandCeiling;
        public float spikePressure;
        /// <summary>Negative: whatever the world table gives. Set by Sway.</summary>
        public float curveAmplitude = -1f;
        /// <summary>No motion and no random shields: the level's new figure is met on its own.</summary>
        public bool calm;
        public string[] phraseIds = new string[0];
        public float[] phraseWeights = new float[0];

        /// <summary>A fully authored level: its own speed, length and flow band.</summary>
        public LevelRecipe(string hook, float speed, float seconds, float floor, float ceiling, float spike)
        {
            this.hook = hook; this.speed = speed; this.seconds = seconds;
            bandFloor = floor; bandCeiling = ceiling; spikePressure = spike;
        }

        /// <summary>A level whose figures and hook are authored but whose speed, length and flow
        /// band come from the world table, as a generated level's would.</summary>
        public LevelRecipe(string hook)
        {
            this.hook = hook;
        }

        public bool HasKinematics { get { return speed > 0f; } }
        public bool HasBand { get { return bandFloor > 0f; } }

        /// <summary>Figures and weights, as alternating id / weight pairs.</summary>
        public LevelRecipe Deal(params object[] idWeightPairs)
        {
            int n = idWeightPairs.Length / 2;
            phraseIds = new string[n];
            phraseWeights = new float[n];
            for (int i = 0; i < n; i++)
            {
                phraseIds[i] = (string)idWeightPairs[i * 2];
                phraseWeights[i] = System.Convert.ToSingle(idWeightPairs[i * 2 + 1]);
            }
            return this;
        }

        /// <summary>Cosmetic sway of the tube. Arrives on its own level, never alongside a new figure.</summary>
        public LevelRecipe Sway(float amplitude) { curveAmplitude = amplitude; return this; }

        public LevelRecipe Calm() { calm = true; return this; }
    }

    public static class LevelRecipes
    {
        private static Dictionary<int, LevelRecipe> byLevel;

        /// <summary>The authored recipe for a ladder level, or null if the level is generated.</summary>
        public static LevelRecipe For(int level)
        {
            if (byLevel == null) Build();
            LevelRecipe r;
            return byLevel.TryGetValue(level, out r) ? r : null;
        }

        private static void Add(int level, LevelRecipe recipe) { byLevel[level] = recipe; }

        private static void Build()
        {
            byLevel = new Dictionary<int, LevelRecipe>();

            // ---- World 1: FIRST LIGHT - steering only, no jump anywhere --------------------------
            //                  hook            speed  secs  floor  ceil  spike
            Add(1,  new LevelRecipe("FIRST LIGHT",  10.0f, 20f, 0.26f, 0.30f, 0.34f)
                .Deal("Nudge", 1f));
            Add(2,  new LevelRecipe("BOTH WAYS",    10.2f, 22f, 0.27f, 0.31f, 0.36f)
                .Deal("Slalom", 1f, "Nudge", 0.3f));
            Add(3,  new LevelRecipe("BIGGER",       10.5f, 24f, 0.28f, 0.33f, 0.38f)
                .Deal("BigNudge", 1f, "BigSlalom", 1f, "Nudge", 0.3f));
            Add(4,  new LevelRecipe("TWO LANES",    10.8f, 26f, 0.29f, 0.35f, 0.41f)
                .Deal("Lanes", 1f, "Nudge", 0.5f, "Slalom", 0.3f));
            Add(5,  new LevelRecipe("FOLLOW",       11.0f, 28f, 0.30f, 0.37f, 0.43f)
                .Deal("Follow", 1f, "FollowBack", 1f, "Slalom", 0.3f));
            Add(6,  new LevelRecipe("PICK ONE",     11.3f, 30f, 0.31f, 0.39f, 0.46f)
                .Deal("OffsetLanes", 2f, "Lanes", 0.4f, "Follow", 0.3f));
            Add(7,  new LevelRecipe("THREE LANES",  11.6f, 32f, 0.33f, 0.41f, 0.48f)
                .Deal("Trident", 2f, "OffsetLanes", 0.4f, "Lanes", 0.3f));
            Add(8,  new LevelRecipe("FIRST LIGHT FINALE", 12.0f, 38f, 0.34f, 0.43f, 0.53f)
                .Deal("Trident", 1f, "OffsetLanes", 1f, "Lanes", 1f, "FollowBack", 1f,
                      "BigSlalom", 1f, "Slalom", 0.6f, "Burst", 0.8f));

            // ---- World 2: LIFT OFF - hurdles, then jump-or-steer ---------------------------------
            Add(9,  new LevelRecipe("HOP",          11.0f, 25f, 0.26f, 0.32f, 0.38f)
                .Deal("Hurdle", 1f, "Nudge", 0.3f));
            Add(10, new LevelRecipe("HOP AND STEER", 11.3f, 27f, 0.28f, 0.34f, 0.40f)
                .Deal("HurdleNudge", 1f, "Hurdle", 0.4f, "Slalom", 0.3f));
            Add(11, new LevelRecipe("EITHER WAY",   11.6f, 29f, 0.30f, 0.37f, 0.44f)
                .Deal("LowWide", 1f, "Hurdle", 0.4f, "Nudge", 0.3f));
            Add(12, new LevelRecipe("DOUBLE HOP",   11.9f, 31f, 0.32f, 0.39f, 0.46f)
                .Deal("DoubleHop", 1f, "Hurdle", 0.4f, "Slalom", 0.3f));
            Add(13, new LevelRecipe("HIGHER",       12.2f, 33f, 0.33f, 0.41f, 0.48f)
                .Deal("HighHurdle", 1f, "HurdleNudge", 0.5f, "Lanes", 0.3f));
            Add(14, new LevelRecipe("LAND AND LOOK", 12.5f, 35f, 0.35f, 0.43f, 0.50f)
                .Deal("LandLook", 1f, "Trident", 0.5f, "HighHurdle", 0.5f));
            Add(15, new LevelRecipe("PACE",         12.8f, 38f, 0.37f, 0.45f, 0.53f)
                .Deal("HurdleNudge", 1f, "LowWide", 1f, "DoubleHop", 0.6f, "Trident", 1f,
                      "OffsetLanes", 1f, "FollowBack", 0.6f, "HighHurdle", 0.6f));
            Add(16, new LevelRecipe("LIFT OFF FINALE", 13.0f, 44f, 0.38f, 0.48f, 0.60f)
                .Deal("LandLook", 1f, "DoubleHop", 1f, "HighHurdle", 1f, "LowWide", 0.6f,
                      "Trident", 1f, "Burst", 0.8f, "BigSlalom", 0.6f));

            // ---- World 3: SHAPES - height, span and depth, then the first walls ------------------
            Add(17, new LevelRecipe("TOWERS",       12.0f, 28f, 0.32f, 0.40f, 0.47f)
                .Deal("Tower", 1f, "TowerRow", 1f, "Hurdle", 0.4f));
            Add(18, new LevelRecipe("WIDE",         12.3f, 30f, 0.34f, 0.42f, 0.49f)
                .Deal("Wide", 1f, "Tower", 0.4f, "Nudge", 0.3f));
            Add(19, new LevelRecipe("DOORS",        12.6f, 32f, 0.34f, 0.43f, 0.50f)
                .Deal("Door", 1f, "Lanes", 0.3f, "Tower", 0.3f));
            Add(20, new LevelRecipe("BLADES",       12.9f, 34f, 0.36f, 0.45f, 0.52f)
                .Deal("Blades", 1f, "Door", 0.4f, "TowerHop", 0.4f));
            // No new figure: this level's novelty is the tube itself starting to sway.
            Add(21, new LevelRecipe("DOOR RUN",     13.2f, 36f, 0.38f, 0.47f, 0.55f)
                .Deal("TwoDoor", 1f, "Ladder", 1f, "LadderBack", 1f, "Zigzag", 1f, "Door", 0.5f)
                .Sway(2.5f));
            // One sphere, so the only colour is the player's own: an arc in it is open air. This is
            // the rule world 10 builds on, taught while it is still simple.
            Add(22, new LevelRecipe("SAME COLOUR",  13.5f, 38f, 0.39f, 0.49f, 0.57f)
                .Deal("OwnColour", 1f, "KeepOrDodge", 1f, "OwnWall", 0.6f, "OwnSplit", 0.6f, "Door", 0.3f)
                .Sway(2.5f));
            Add(23, new LevelRecipe("FOUR",         13.8f, 41f, 0.41f, 0.51f, 0.60f)
                .Deal("Four", 1f, "Squeeze", 0.5f, "Door", 0.5f, "KeepOrDodge", 0.4f, "TowerRow", 0.4f)
                .Sway(2.5f));
            Add(24, new LevelRecipe("SHAPES FINALE", 14.0f, 48f, 0.42f, 0.54f, 0.66f)
                .Deal("Squeeze", 1f, "Four", 1f, "Blades", 1f, "TowerHop", 1f, "Wide", 0.6f,
                      "Door", 0.6f, "Zigzag", 0.6f, "LandLook", 0.6f, "OwnSplit", 0.6f, "Burst", 0.6f)
                .Sway(2.5f));

            // ---- World 9: TWINS - two spheres, opposite each other ------------------------------
            // Figures and hooks are authored; speed, length and band follow the world table. Any
            // figure the pair cannot steer through is dropped by the composer's deck filter.
            Add(65, new LevelRecipe("TWINS").Calm()
                .Deal("TwinWatch", 1f, "Lanes", 1f, "Nudge", 0.6f, "Slalom", 0.5f, "Follow", 0.4f));
            Add(66, new LevelRecipe("SPLIT")
                .Deal("TwinSplit", 1f, "TwinDoors", 1f, "TwinWatch", 0.5f, "Lanes", 0.3f));
            Add(67, new LevelRecipe("TWIN MIX")
                .Deal("TwinDoors", 1f, "TwinSlalom", 1f, "Hurdle", 0.5f, "TwoDoor", 0.5f,
                      "Spinner", 0.5f, "Metronome", 0.4f));
            Add(68, new LevelRecipe("FOUR WAYS")
                .Deal("TwinQuad", 1f, "TwinSplit", 0.6f, "TwinWatch", 0.5f, "Lanes", 0.3f));
            Add(69, new LevelRecipe("TWIN PACE")
                .Deal("TwinSlalom", 1f, "TwinQuad", 0.6f, "TwinDoors", 0.6f, "DoubleHop", 0.4f, "Spinner", 0.5f));
            Add(70, new LevelRecipe("TOGETHER")
                .Deal("TwinSplit", 1f, "TwinSlalom", 1f, "TwinQuad", 1f, "TwinDoors", 0.8f, "TwinWatch", 0.6f,
                      "Metronome", 0.6f, "Spinner", 0.6f, "HurdleNudge", 0.5f, "DeepWall", 0.4f, "TwoDoor", 0.4f));
            Add(71, new LevelRecipe("TWIN TEST")
                .Deal("TwinSplit", 1f, "TwinSlalom", 1f, "TwinQuad", 1f, "TwinDoors", 1f, "TwinWatch", 0.6f,
                      "Metronome", 0.8f, "Spinner", 0.8f, "HurdleNudge", 0.6f, "DeepWall", 0.5f, "DoubleHop", 0.5f));
            Add(72, new LevelRecipe("TWINS FINALE")
                .Deal("TwinSplit", 1f, "TwinSlalom", 1f, "TwinQuad", 1f, "TwinDoors", 1f, "TwinWatch", 0.8f,
                      "Metronome", 0.8f, "Spinner", 0.8f, "DoubleHop", 0.6f, "DeepWall", 0.6f, "Burst", 0.6f));

            // ---- World 10: PRISM - each twin passes its own colour and hits the other's ---------
            Add(73, new LevelRecipe("PRISM").Calm()
                .Deal("ColourHold", 1f, "TwinWatch", 0.4f, "Lanes", 0.3f));
            Add(74, new LevelRecipe("WRONG COLOUR")
                .Deal("WrongColour", 1f, "ColourHold", 0.6f, "TwinDoors", 0.3f));
            Add(75, new LevelRecipe("MATCH")
                .Deal("ColourMatch", 1f, "WrongColour", 0.5f, "ColourHold", 0.4f, "TwinSplit", 0.3f));
            Add(76, new LevelRecipe("HALF TURN")
                .Deal("HalfTurn", 1f, "ColourMatch", 0.6f, "TwinSplit", 0.4f, "WrongColour", 0.4f));
            Add(77, new LevelRecipe("PRISM PACE")
                .Deal("WrongColour", 1f, "ColourMatch", 1f, "HalfTurn", 0.6f, "TwinSlalom", 0.6f, "TwinQuad", 0.5f));
            Add(78, new LevelRecipe("LOCK")
                .Deal("ColourLock", 1f, "HalfTurn", 0.5f, "WrongColour", 0.5f, "Spinner", 0.3f, "TwinDoors", 0.3f));
            Add(79, new LevelRecipe("PRISM TEST")
                .Deal("ColourLock", 1f, "HalfTurn", 1f, "WrongColour", 1f, "ColourMatch", 0.8f, "ColourHold", 0.6f,
                      "TwinSplit", 0.6f, "TwinSlalom", 0.6f, "Metronome", 0.5f, "Spinner", 0.5f));
            Add(80, new LevelRecipe("PRISM FINALE")
                .Deal("ColourLock", 1f, "HalfTurn", 1f, "WrongColour", 1f, "ColourMatch", 1f, "ColourHold", 0.6f,
                      "TwinQuad", 0.6f, "TwinSlalom", 0.6f, "Metronome", 0.6f, "Burst", 0.5f));
        }
    }
}
