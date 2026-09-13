using UnityEngine;

namespace TubityWAI
{
    [System.Serializable]
    public class LevelConfig
    {
        public int levelNumber;
        public float forwardSpeed;
        public float obstacleSpawnProbability;
        public float speedBoostMultiplier;
        
        // Optional custom properties for test levels
        public bool isTestLevel;
        public string levelName;
        public string themeName;

        // Custom camera settings
        public bool useCustomCamera;
        public float cameraOffsetX;
        public float cameraOffsetY;
        public float cameraRotationX;
        public float cameraRotationY;

        // Curve settings
        public bool hasCurves;
        public float curveFrequency;
        public float curveAmplitude;

        // Visual / Scenery settings
        public bool isTransparentTube;
        public bool hasCityFlyby;

        // Custom Mechanics settings
        public bool allowPartialDeath;
        public bool spawnAddSpherePowerup;

        // Themed environment (space, jungle, underwater, ...). None keeps the classic neon tunnel.
        public EnvironmentTheme environment = EnvironmentTheme.None;

        // Obstacle ring tuning (arc counts, spans, motion). Classic() reproduces the original single static arc.
        public RingDifficulty rings = RingDifficulty.Classic();

        // Obstacle-free run-in at the start of the level, in tube units. Negative means auto:
        // three seconds of travel at this level's speed, clamped to [60, 150].
        public float startClearDistance = -1f;

        // Menu-style hot neon: heavier bloom plus additive halo ribbons on marker rings and arcs.
        public bool neonBloom = false;

        // Where the finish gate sits, in tube units from the start. Zero or less means endless.
        public float levelLength = 0f;

        // Seed for every spawn decision (arcs, ring motion, coins, powerups). Zero derives one from the level number.
        public int seed = 0;

        public LevelConfig(int number, float speed, float obsProb, float boost, bool isTest = false, string name = "", string theme = "",
                           bool customCam = false, float camOffsetX = 0f, float camOffsetY = 0f, float camRotX = 0f, float camRotY = 0f,
                           bool curves = false, float curveFreq = 0.05f, float curveAmp = 2.0f,
                           bool transparentTube = false, bool cityFlyby = false, bool partialDeath = false, bool spawnAddSphere = false)
        {
            levelNumber = number;
            forwardSpeed = speed;
            obstacleSpawnProbability = obsProb;
            speedBoostMultiplier = boost;
            isTestLevel = isTest;
            levelName = name;
            themeName = theme;

            useCustomCamera = customCam;
            cameraOffsetX = camOffsetX;
            cameraOffsetY = camOffsetY;
            cameraRotationX = camRotX;
            cameraRotationY = camRotY;

            hasCurves = curves;
            curveFrequency = curveFreq;
            curveAmplitude = curveAmp;

            isTransparentTube = transparentTube;
            hasCityFlyby = cityFlyby;
            allowPartialDeath = partialDeath;
            spawnAddSpherePowerup = spawnAddSphere;
        }

        /// <summary>Fluent setter so the long positional constructor stays untouched.</summary>
        public LevelConfig WithEnvironment(EnvironmentTheme theme)
        {
            environment = theme;
            return this;
        }

        /// <summary>Fluent setter for the obstacle ring difficulty (see RingDifficulty / LevelProgression).</summary>
        public LevelConfig WithRings(RingDifficulty ringDifficulty)
        {
            rings = ringDifficulty ?? RingDifficulty.Classic();
            return this;
        }

        public LevelConfig WithLength(float length)
        {
            levelLength = length;
            return this;
        }

        public LevelConfig WithSeed(int levelSeed)
        {
            seed = levelSeed;
            return this;
        }

        public bool HasFinish { get { return levelLength > 0f; } }

        /// <summary>The seed all tunnel spawning runs from, so a level lays out identically every run.</summary>
        public int GetSeed()
        {
            if (seed != 0) return seed;
            return unchecked(levelNumber * 7919 + 12345);
        }

        /// <summary>Fluent setter: rings and arcs bloom like the attract-mode hero sphere.</summary>
        public LevelConfig WithNeonBloom(bool enabled = true)
        {
            neonBloom = enabled;
            return this;
        }

        /// <summary>Distance from z = 0 that stays free of obstacles so the player can read the scene.</summary>
        public float GetStartClearDistance()
        {
            if (startClearDistance >= 0f) return startClearDistance;
            return Mathf.Clamp(forwardSpeed * 3f, 60f, 150f);
        }

        public Vector3 GetCurveOffset(float z)
        {
            if (!hasCurves) return Vector3.zero;
            
            // Generate a winding S-curve using sine/cosine
            float x = Mathf.Sin(z * curveFrequency) * curveAmplitude;
            float y = Mathf.Cos(z * curveFrequency * 0.5f) * (curveAmplitude * 0.5f);
            return new Vector3(x, y, 0f);
        }
    }
}
