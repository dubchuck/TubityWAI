namespace TubityWAI
{
    [System.Serializable]
    public class LevelConfig
    {
        public int levelNumber;
        public float forwardSpeed;
        public float obstacleSpawnProbability;
        public float speedBoostMultiplier;

        public LevelConfig(int number, float speed, float obsProb, float boost)
        {
            levelNumber = number;
            forwardSpeed = speed;
            obstacleSpawnProbability = obsProb;
            speedBoostMultiplier = boost;
        }
    }
}
