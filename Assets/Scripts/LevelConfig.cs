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

        public LevelConfig(int number, float speed, float obsProb, float boost, bool isTest = false, string name = "", string theme = "",
                           bool customCam = false, float camOffsetX = 0f, float camOffsetY = 0f, float camRotX = 0f, float camRotY = 0f,
                           bool curves = false, float curveFreq = 0.05f, float curveAmp = 2.0f,
                           bool transparentTube = false, bool cityFlyby = false)
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
