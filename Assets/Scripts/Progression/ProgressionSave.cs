using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// Progress for Progression Test 1 and the endless modes, kept in its own PlayerPrefs
    /// namespace ("P2_") so nothing here can touch the original campaign's records.
    /// </summary>
    public static class ProgressionSave
    {
        private const string BeatenPrefix = "P2_Beaten_";
        private const string StarsPrefix  = "P2_Stars_";
        private const string EndlessBest  = "P2_EndlessBest_";
        private const string EndlessDist  = "P2_EndlessDist_";

        // ---- Campaign -----------------------------------------------------------------------

        public static bool IsBeaten(int level)
        {
            return PlayerPrefs.GetInt(BeatenPrefix + level, 0) == 1;
        }

        public static int GetStars(int level)
        {
            return PlayerPrefs.GetInt(StarsPrefix + level, 0);
        }

        /// <summary>
        /// Level 1 is always open. Otherwise a level opens when the one before it is beaten - or,
        /// so a single wall can never end a run of 128 levels, when the previous world is at least
        /// half cleared and this is that next world's opening level.
        /// </summary>
        public static bool IsUnlocked(int level)
        {
#if UNITY_EDITOR
            // In the editor every level is open, so the ladder can be jumped into at any point
            // for tuning. Stars and beaten flags are untouched, so the real gating is intact in
            // a build and the recorded progress stays honest.
            return true;
#else
            if (level <= 1) return true;
            if (IsBeaten(level - 1)) return true;

            bool opensAWorld = (level - 1) % ProgressionV2.LevelsPerWorld == 0;
            if (opensAWorld)
            {
                int prevWorld = (level - 1) / ProgressionV2.LevelsPerWorld;   // 1-based, the world before
                if (StarsInWorld(prevWorld) > 0 && LevelsBeatenInWorld(prevWorld) >= ProgressionV2.LevelsPerWorld / 2)
                    return true;
            }
            return false;
#endif
        }

        public static bool IsWorldUnlocked(int worldIndex)
        {
            return IsUnlocked((worldIndex - 1) * ProgressionV2.LevelsPerWorld + 1);
        }

        public static int LevelsBeatenInWorld(int worldIndex)
        {
            int first = (worldIndex - 1) * ProgressionV2.LevelsPerWorld + 1;
            int count = 0;
            for (int n = first; n < first + ProgressionV2.LevelsPerWorld; n++)
                if (IsBeaten(n)) count++;
            return count;
        }

        public static int StarsInWorld(int worldIndex)
        {
            int first = (worldIndex - 1) * ProgressionV2.LevelsPerWorld + 1;
            int stars = 0;
            for (int n = first; n < first + ProgressionV2.LevelsPerWorld; n++)
                stars += GetStars(n);
            return stars;
        }

        public static int TotalStars()
        {
            int stars = 0;
            for (int n = 1; n <= ProgressionV2.LevelCount; n++) stars += GetStars(n);
            return stars;
        }

        public static void RecordResult(int level, int stars)
        {
            PlayerPrefs.SetInt(BeatenPrefix + level, 1);
            PlayerPrefs.SetInt(StarsPrefix + level, Mathf.Max(stars, GetStars(level)));
            PlayerPrefs.DeleteKey(DeathsPrefix + level);   // cleared: the wall is behind them
            PlayerPrefs.Save();
        }

        // ---- Failure-adaptive assist ----------------------------------------------------------

        private const string DeathsPrefix = "P2_Deaths_";

        /// <summary>Deaths on this level since it was last beaten.</summary>
        public static int GetDeaths(int level)
        {
            return PlayerPrefs.GetInt(DeathsPrefix + level, 0);
        }

        public static void RecordDeath(int level)
        {
            PlayerPrefs.SetInt(DeathsPrefix + level, GetDeaths(level) + 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// How much to ease a level the player keeps failing, as a multiplier on its flow band.
        /// The first few deaths change nothing - losing is part of learning, and softening too
        /// early robs the win. Past that it eases off gradually, and never below 0.88, so a level
        /// the player eventually beats is still recognisably the level they were fighting.
        /// </summary>
        public static float AssistFactor(int level)
        {
            int deaths = GetDeaths(level);
            if (deaths < 3) return 1f;
            return Mathf.Lerp(1f, 0.88f, Mathf.Clamp01((deaths - 3) / 5f));
        }

        // ---- Endless ------------------------------------------------------------------------

        public static int GetEndlessBest(string modeId)
        {
            return PlayerPrefs.GetInt(EndlessBest + modeId, 0);
        }

        public static float GetEndlessBestDistance(string modeId)
        {
            return PlayerPrefs.GetFloat(EndlessDist + modeId, 0f);
        }

        public static bool RecordEndlessRun(string modeId, int score, float distance)
        {
            bool isBest = score > GetEndlessBest(modeId);
            if (isBest) PlayerPrefs.SetInt(EndlessBest + modeId, score);
            if (distance > GetEndlessBestDistance(modeId)) PlayerPrefs.SetFloat(EndlessDist + modeId, distance);
            PlayerPrefs.Save();
            return isBest;
        }

        // ---- Dev helpers --------------------------------------------------------------------

        public static void UnlockAll()
        {
            for (int n = 1; n <= ProgressionV2.LevelCount; n++) PlayerPrefs.SetInt(BeatenPrefix + n, 1);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            for (int n = 1; n <= ProgressionV2.LevelCount; n++)
            {
                PlayerPrefs.DeleteKey(BeatenPrefix + n);
                PlayerPrefs.DeleteKey(StarsPrefix + n);
            }
            PlayerPrefs.Save();
        }
    }
}
