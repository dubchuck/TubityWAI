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

        // Optional: cross-fade through several environments as the player flies forward. When set it
        // supersedes `environment`, which then just records where the level starts.
        public EnvironmentBlend environmentBlend;

        // Optional: named world-space bodies (planets, moons) placed at fixed distances, for levels
        // that fly past landmarks rather than through scattered scenery. See CelestialBodies.
        public CelestialRoute celestialRoute;

        // Obstacle ring tuning (arc counts, spans, motion). Classic() reproduces the original single static arc.
        public RingDifficulty rings = RingDifficulty.Classic();

        // Obstacle-free run-in at the start of the level, in tube units. Negative means auto:
        // three seconds of travel at this level's speed, clamped to [60, 150].
        public float startClearDistance = -1f;

        // Menu-style hot neon: heavier bloom plus additive halo ribbons on marker rings and arcs.
        public bool neonBloom = false;

        // Where the finish gate sits, in tube units from the start. Zero or less means endless.
        public float levelLength = 0f;

        // Optional: a composed level (Progression Test 1 / endless modes). When set, the tunnel
        // spawns exactly the rings this composer lays out instead of rolling per marker ring,
        // and obstacleSpawnProbability / rings are ignored. Null keeps the classic spawner.
        public Progression.LevelComposer composer;

        // How hard the tube's curvature pulls the player toward the outside of a turn, in radians
        // per second at full curvature. Zero keeps curves purely cosmetic, as they have always been.
        public float driftStrength = 0f;

        // Degrees the whole obstacle field rolls per tube unit travelled - the world corkscrews,
        // so a line that was safe keeps sliding out from under the player.
        public float spiralDegPerUnit = 0f;

        // Scales the level's fog distance. Below 1 shortens the sight line, which is a difficulty
        // dial in its own right; 1 leaves the environment's own fog alone.
        public float previewScale = 1f;

        // Where this run begins along the tube. Non-zero means the player is resuming from a
        // checkpoint after a crash; the level is otherwise identical, since layout is seeded.
        public float startZ = 0f;

        // Distance between checkpoints. Zero disables them, and the level is all-or-nothing.
        public float checkpointInterval = 0f;

        // Seed for every spawn decision (arcs, ring motion, coins, powerups). Zero derives one from the level number.
        public int seed = 0;

        // Spheres this level is designed for. The progression ladder sets it, because its rings
        // are laid out for that count; zero leaves it to the menu's selection.
        public int forcedSphereCount = 0;

        // Sandbox lab: no obstacle arcs, and the HUD shows live environment and music controls
        // (see SandboxPanel). Pair it with an open-ended environment blend so themes can be appended.
        public bool sandbox = false;

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

        /// <summary>
        /// Fluent setter for a multi-environment level: the world cross-fades through the blend's stops
        /// as the player advances. `environment` follows the blend's first stop so anything that only
        /// looks at the starting theme (music, the one-off sphere setup) still gets a sensible answer.
        /// </summary>
        public LevelConfig WithEnvironmentBlend(EnvironmentBlend blend)
        {
            environmentBlend = (blend != null && blend.IsValid) ? blend : null;
            if (environmentBlend != null) environment = environmentBlend.FirstTheme;
            return this;
        }

        public bool HasEnvironmentBlend { get { return environmentBlend != null && environmentBlend.IsValid; } }

        /// <summary>
        /// Fluent setter for a landmark route. It brings its own environment blend - the sky, fog and
        /// sun have to move in step with the bodies - so this supersedes any blend set separately.
        /// </summary>
        public LevelConfig WithCelestialRoute(CelestialRoute route)
        {
            celestialRoute = route;
            if (route != null && route.blend != null) WithEnvironmentBlend(route.blend);
            return this;
        }

        public bool HasCelestialRoute { get { return celestialRoute != null && celestialRoute.bodies.Count > 0; } }

        /// <summary>The theme in effect at a distance along the tube. Constant for a non-blended level.</summary>
        public EnvironmentTheme EnvironmentAt(float z)
        {
            return HasEnvironmentBlend ? environmentBlend.ThemeAt(z) : environment;
        }

        /// <summary>True if the level shows a themed environment anywhere along its length.</summary>
        public bool HasThemedEnvironment
        {
            get { return HasEnvironmentBlend ? environmentBlend.HasThemedStop : environment != EnvironmentTheme.None; }
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

        /// <summary>Fluent setter: hand level layout to a composer (see Progression/LevelComposer).</summary>
        public LevelConfig WithComposer(Progression.LevelComposer levelComposer)
        {
            composer = levelComposer;
            return this;
        }

        public bool HasComposer { get { return composer != null; } }

        public bool HasCheckpoints { get { return checkpointInterval > 0f && levelLength > 0f; } }

        public LevelConfig WithCheckpoints(float interval)
        {
            checkpointInterval = Mathf.Max(0f, interval);
            return this;
        }

        /// <summary>The checkpoint at or before z, or 0 if none has been reached.</summary>
        public float CheckpointAt(float z)
        {
            if (!HasCheckpoints) return 0f;
            float last = Mathf.Floor(z / checkpointInterval) * checkpointInterval;
            // The final stretch belongs to the finish, not to another checkpoint.
            if (last >= levelLength - checkpointInterval * 0.5f) last -= checkpointInterval;
            return Mathf.Max(0f, last);
        }

        /// <summary>
        /// Spacing of the marker rings drawn on the wall, and of the sphere pulse that follows them.
        /// Composed levels widen it with speed so the cadence stays readable, and widen it again by
        /// markerRingScale to thin the rings out; everything else keeps the classic fixed interval.
        /// This is the drawn spacing only - obstacle placement uses the composer's own finer grid.
        /// </summary>
        public float MarkerIntervalOr(float fallback)
        {
            return (composer != null && composer.dials != null) ? composer.dials.MarkerRingSpacing : fallback;
        }

        public bool HasFinish { get { return levelLength > 0f; } }

        /// <summary>The seed all tunnel spawning runs from, so a level lays out identically every run.</summary>
        public int GetSeed()
        {
            if (seed != 0) return seed;
            return unchecked(levelNumber * 7919 + 12345);
        }

        /// <summary>Fluent setter: obstacle-free run with the SandboxPanel controls on the HUD.</summary>
        public LevelConfig WithSandbox(bool enabled = true)
        {
            sandbox = enabled;
            return this;
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

        /// <summary>
        /// The angle, in the player's angular space, that the outside of the current turn points
        /// toward - the direction a drifting player slides. Magnitude is how hard the turn pulls,
        /// scaled so a typical curve gives roughly 1.
        /// </summary>
        public bool GetDrift(float z, out float outwardAngleRad, out float magnitude)
        {
            outwardAngleRad = 0f;
            magnitude = 0f;
            if (!hasCurves || driftStrength <= 0f) return false;

            // Lateral acceleration by central difference; the outside of a turn is opposite it.
            const float h = 6f;
            Vector3 a = GetCurveOffset(z + h) - 2f * GetCurveOffset(z) + GetCurveOffset(z - h);
            Vector2 outward = new Vector2(-a.x, -a.y);
            float len = outward.magnitude;
            if (len < 1e-5f) return false;

            outward /= len;
            // Match the tube's convention: position = (sin(t), -cos(t)).
            outwardAngleRad = Mathf.Atan2(outward.x, -outward.y);
            magnitude = Mathf.Clamp01(len / (curveAmplitude * 0.02f + 1e-4f));
            return true;
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
