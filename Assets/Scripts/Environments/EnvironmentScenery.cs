using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Trackside props for the themed environments. Mirrors CityGenerator's
    /// per-segment hook, but each segment builds its prop set once and only
    /// re-places it when the tunnel pool recycles the segment, so there is no
    /// churn of GameObjects or materials during play.
    ///
    /// Props are assembled from ProceduralMeshes (rocks, trunks, crystals,
    /// leaves, domes, discs) dressed in ProceduralTextures (albedo + normal +
    /// emission maps). Meshes, textures and URP Lit materials are all cached
    /// and shared, and colliders are never added. Every prop receives the main
    /// light's shadow; only the big grounded silhouettes cast one (see CastShadows).
    /// </summary>
    public static partial class EnvironmentScenery
    {
        private const string ContainerName = "EnvironmentScenery";
        private const float TubeGap = 2.5f;        // clearance between tube wall and the nearest prop
        private const float MaxOrbitSpeed = 5f;    // world units/sec a prop may travel along its orbit

        /// <summary>
        /// Grounded props stand on the ground outside the tube, Floating ones hang in the space
        /// around it. Encircle props are centred on the tube's own axis and wrap all the way round
        /// it - cave walls, gates, ring gears - one per segment, lined up with the curve.
        /// </summary>
        public enum Placement { Grounded, Floating, Encircle }

        public class Prop
        {
            public GameObject root;
            public EnvironmentTheme theme;     // which theme built it, so a blend can fade it in and out
            public float dissolveKey;          // 0..1; the prop shows once its theme's weight passes this
            public Placement placement = Placement.Grounded;
            public float minScale = 1f;
            public float maxScale = 1f;
            public float bodyRadius = 1f;      // rough bounding radius at scale 1 (floating props)
            public float extraDistance = 0f;   // push floating props further out (planets)
            public float spread = 14f;         // random extra distance range
            public bool lowerHalfBias = false; // favour the lower half of the tube (grounded things)
            public bool upright = false;       // floating prop keeps its local up (jellyfish)
            public float orbitSpeed = 0f;      // degrees/sec around the tube axis, before the distance clamp
            public SceneryDrift drift;

            // --- Encircle props ---
            /// <summary>Stretch along the tube so a shape this long exactly spans one segment
            /// (walls that must tile); 0 keeps the prop's own proportions (gates, gears).</summary>
            public float encircleLength = 0f;
            /// <summary>Keep this roll instead of a random one (gates stay upright).</summary>
            public bool fixedRoll = false;
            public float roll = 0f;

            /// <summary>Fraction of segments that show this prop at all - for rare set pieces.</summary>
            public float chance = 1f;
        }

        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        public static void ClearCache()
        {
            materialCache.Clear();
        }

        // ------------------------------------------------------------------
        // Entry point (called from TunnelSegment on Start and ResetSegment)
        // ------------------------------------------------------------------
        private static readonly List<EnvironmentTheme> activeThemes = new List<EnvironmentTheme>();

        public static void Decorate(GameObject segment, float segmentLength, float tubeRadius, LevelConfig config)
        {
            if (config == null || !config.HasThemedEnvironment) return;

            float segmentZ = segment.transform.position.z;
            // Sample the blend at the middle of the segment so its props match the stretch of tube they line.
            float sampleZ = segmentZ + segmentLength * 0.5f;

            activeThemes.Clear();
            if (config.HasEnvironmentBlend) config.environmentBlend.ActiveThemes(sampleZ, activeThemes);
            else activeThemes.Add(config.environment);
            if (activeThemes.Count == 0) return;

            Transform existing = segment.transform.Find(ContainerName);
            EnvironmentSceneryContainer container = existing != null ? existing.GetComponent<EnvironmentSceneryContainer>() : null;
            if (container == null)
            {
                if (existing != null) Object.Destroy(existing.gameObject);
                GameObject containerObj = new GameObject(ContainerName);
                containerObj.transform.SetParent(segment.transform, false);
                container = containerObj.AddComponent<EnvironmentSceneryContainer>();
            }

            Random.State prevState = Random.state;

            // A recycled segment may now sit under a different part of the blend, so build any theme's
            // props the first time this segment needs them and keep them for the next time round.
            for (int i = 0; i < activeThemes.Count; i++)
            {
                EnvironmentTheme theme = activeThemes[i];
                if (EnvironmentPalettes.Get(theme) == null) continue;   // None has no trackside props
                if (container.builtThemes.Contains(theme)) continue;

                Random.InitState((int)(segmentZ * 31f) + (int)theme * 977);
                container.builtThemes.Add(theme);

                int first = container.props.Count;
                BuildProps(theme, container.transform, container.props);
                for (int k = first; k < container.props.Count; k++)
                {
                    container.props[k].theme = theme;
                    container.props[k].dissolveKey = Random.value;
                }
            }

            Random.InitState((int)(segmentZ * 31f) + (int)activeThemes[0] * 977);

            for (int i = 0; i < container.props.Count; i++)
            {
                Prop prop = container.props[i];
                float weight = config.HasEnvironmentBlend
                    ? config.environmentBlend.Weight(prop.theme, sampleZ)
                    : (prop.theme == config.environment ? 1f : 0f);

                // Dissolve rather than swap: each prop has its own threshold, so the old theme's props
                // thin out while the new theme's fill in instead of a whole segment changing at once.
                bool visible = weight >= 1f || prop.dissolveKey < weight;
                // A rare set piece only turns up in some segments, decided by the segment's own
                // position so the same stretch of tube always gets the same answer.
                if (visible && prop.chance < 1f) visible = SegmentHash(segmentZ, i) < prop.chance;
                if (prop.root.activeSelf != visible) prop.root.SetActive(visible);
                if (!visible) continue;

                Place(prop, segmentLength, tubeRadius, config, segmentZ);
            }

            Random.state = prevState;
        }

        private static float SegmentHash(float segmentZ, int index)
        {
            float h = Mathf.Sin(segmentZ * 12.9898f + index * 78.233f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        /// <summary>
        /// An Encircle prop spans its segment on the tube's axis. It is aimed along the chord of the
        /// curve from one end of the segment to the other, so a chain of wall shells meets the tube's
        /// own bends instead of stepping sideways at every joint.
        /// </summary>
        private static void PlaceEncircle(Prop prop, float segmentLength, LevelConfig config, float segmentZ)
        {
            Vector3 c0 = config.GetCurveOffset(segmentZ);
            Vector3 c1 = config.GetCurveOffset(segmentZ + segmentLength);
            Vector3 cm = config.GetCurveOffset(segmentZ + segmentLength * 0.5f);
            Vector3 along = new Vector3(c1.x - c0.x, c1.y - c0.y, segmentLength);
            Quaternion follow = Quaternion.LookRotation(along.normalized, Vector3.up);

            float roll = prop.fixedRoll ? prop.roll : Random.Range(0f, 360f);
            float scale = Random.Range(prop.minScale, prop.maxScale);
            float zScale = prop.encircleLength > 0f ? segmentLength / prop.encircleLength : scale;

            Quaternion local = Quaternion.Euler(0f, 0f, roll);
            Transform t = prop.root.transform;
            t.localPosition = new Vector3(cm.x, cm.y, segmentLength * 0.5f);
            t.localRotation = follow * local;
            t.localScale = new Vector3(scale, scale, zScale);

            if (prop.drift != null)
            {
                // No orbit and no wander: the drift only spins it in place (ring gears).
                prop.drift.orbitSpeed = 0f;
                prop.drift.Configure(new Vector2(cm.x, cm.y), 0f, 0f, segmentLength * 0.5f, 0f, follow * local);
            }
        }

        private static void Place(Prop prop, float segmentLength, float tubeRadius, LevelConfig config, float segmentZ)
        {
            if (prop.placement == Placement.Encircle)
            {
                PlaceEncircle(prop, segmentLength, config, segmentZ);
                return;
            }

            float localZ = Random.Range(2f, segmentLength - 2f);
            Vector3 curve = config.GetCurveOffset(segmentZ + localZ);

            // Angle 0 points straight down, matching the tube's own vertex layout.
            float angle = prop.lowerHalfBias ? Random.Range(-0.8f, 0.8f) * Mathf.PI : Random.Range(-Mathf.PI, Mathf.PI);
            Vector3 radial = new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f);

            float scale = Random.Range(prop.minScale, prop.maxScale);
            float dist;
            float safeRadius;
            Quaternion local;   // the part of the rotation that survives an orbit
            Quaternion rot;

            if (prop.placement == Placement.Grounded)
            {
                // Base sits outside the tube, the prop grows radially outward (local +Y).
                dist = tubeRadius + TubeGap + Random.Range(0f, prop.spread);
                safeRadius = tubeRadius + TubeGap;
                local = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                rot = Quaternion.LookRotation(Vector3.forward, radial) * local;
            }
            else
            {
                dist = tubeRadius + TubeGap + prop.bodyRadius * scale + prop.extraDistance + Random.Range(0f, prop.spread);
                safeRadius = tubeRadius + TubeGap * 0.6f + prop.bodyRadius * scale;
                local = prop.upright ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Random.rotation;
                rot = local;
            }

            Transform t = prop.root.transform;
            t.localPosition = new Vector3(curve.x, curve.y, localZ) + radial * dist;
            t.localRotation = rot;
            t.localScale = Vector3.one * scale;

            if (prop.drift != null)
            {
                // Far-out props would streak sideways at a fixed angular rate, so cap
                // how fast any prop may travel along its orbit.
                float maxDegrees = MaxOrbitSpeed * Mathf.Rad2Deg / Mathf.Max(dist, 1f);
                prop.drift.orbitSpeed = Mathf.Clamp(prop.orbitSpeed, -maxDegrees, maxDegrees);
                prop.drift.Configure(new Vector2(curve.x, curve.y), angle, dist, localZ, safeRadius, local);
            }
        }

        // ------------------------------------------------------------------
        // Theme dispatch
        // ------------------------------------------------------------------
        private static void BuildProps(EnvironmentTheme theme, Transform parent, List<Prop> props)
        {
            switch (theme)
            {
                case EnvironmentTheme.Space:      BuildSpace(parent, props); break;
                case EnvironmentTheme.Jungle:     BuildJungle(parent, props); break;
                case EnvironmentTheme.Underwater: BuildUnderwater(parent, props); break;
                case EnvironmentTheme.Volcano:    BuildVolcano(parent, props); break;
                case EnvironmentTheme.Crystal:    BuildCrystal(parent, props); break;
                case EnvironmentTheme.SolarSystem:  BuildSolarSystem(parent, props); break;
                case EnvironmentTheme.AsteroidBelt: BuildAsteroidBelt(parent, props); break;
                default: BuildWorld(theme, parent, props); break;   // the second set (EnvironmentScenery.Worlds.cs)
            }
        }

        // ------------------------------------------------------------------
        // SPACE: cratered asteroids, banded ringed planets, stations, debris
        // ------------------------------------------------------------------
        private static readonly Color[][] PlanetPalettes =
        {
            new[] { new Color(0.95f, 0.55f, 0.25f), new Color(0.85f, 0.75f, 0.55f), new Color(0.70f, 0.35f, 0.15f), new Color(0.98f, 0.85f, 0.65f) },
            new[] { new Color(0.20f, 0.40f, 0.95f), new Color(0.55f, 0.75f, 1.00f), new Color(0.10f, 0.25f, 0.70f), new Color(0.85f, 0.92f, 1.00f) },
            new[] { new Color(0.60f, 0.25f, 0.90f), new Color(0.95f, 0.45f, 0.85f), new Color(0.35f, 0.10f, 0.60f), new Color(0.80f, 0.60f, 1.00f) },
            new[] { new Color(0.15f, 0.80f, 0.65f), new Color(0.55f, 0.95f, 0.75f), new Color(0.05f, 0.50f, 0.45f), new Color(0.90f, 1.00f, 0.90f) }
        };

        private static void BuildSpace(Transform parent, List<Prop> props)
        {
            ProceduralTextures.TexSet rockTex = ProceduralTextures.Rock(1, new Color(0.10f, 0.09f, 0.09f), new Color(0.36f, 0.31f, 0.28f), 3f);
            Material rock = Mat("space_rock", Color.white, 0f, 0.2f, 0f, false, 1f, rockTex, 1.5f, 1.9f);
            Material hull = Mat("space_hull", new Color(0.62f, 0.66f, 0.72f), 0f, 0.65f, 0.8f, false, 1f,
                                ProceduralTextures.Rock(2, new Color(0.55f, 0.58f, 0.62f), new Color(0.75f, 0.78f, 0.82f), 1f), 2f, 1.4f);
            Material panel = Mat("space_panel", Color.white, 1.4f, 0.85f, 0.4f, false, 1f,
                                 ProceduralTextures.SolarGrid(1, new Color(0.08f, 0.18f, 0.55f), new Color(0.35f, 0.75f, 1f)), 2f);
            Material dish = Mat("space_dish", new Color(0.85f, 0.87f, 0.9f), 0f, 0.5f, 0.6f);

            for (int i = 0; i < 5; i++)
            {
                Prop p = NewProp(parent, "Asteroid", Placement.Floating, 1.2f, 3.5f, 1.4f);
                int chunks = Random.Range(1, 4);
                for (int c = 0; c < chunks; c++)
                {
                    Mesh m = ProceduralMeshes.Rock(Random.Range(0, 4), 2, 0.42f, 1.7f);
                    Vector3 s = new Vector3(Random.Range(0.7f, 1.6f), Random.Range(0.6f, 1.2f), Random.Range(0.7f, 1.5f));
                    MeshObj(m, p.root.transform, Random.insideUnitSphere * 0.6f, s, rock, Random.rotation);
                }
                p.drift = Drift(p.root, new Vector3(Random.Range(-25f, 25f), Random.Range(-25f, 25f), 0f), 0f, 0f);
                p.spread = 26f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Planet", Placement.Floating, 8f, 20f, 0.5f);
                int pal = Random.Range(0, PlanetPalettes.Length);
                ProceduralTextures.TexSet bands = ProceduralTextures.Bands(pal + 1, PlanetPalettes[pal]);
                Material planetMat = Mat("space_planet_" + pal, Color.white, 0f, 0.35f, 0f, false, 1f, bands, 1f);
                planetMat.EnableKeyword("_EMISSION");
                planetMat.SetTexture("_EmissionMap", bands.albedo);
                planetMat.SetColor("_EmissionColor", Color.white * 0.35f);
                GameObject body = Prim(PrimitiveType.Sphere, p.root.transform, Vector3.zero, Vector3.one, planetMat, Quaternion.Euler(Random.Range(-25f, 25f), 0f, Random.Range(-20f, 20f)));
                if (Random.value > 0.35f)
                {
                    ProceduralTextures.TexSet ringTex = ProceduralTextures.RingBands(pal + 3, PlanetPalettes[pal][1], PlanetPalettes[pal][3]);
                    Material ringMat = Mat("space_ring_" + pal, Color.white, 0.5f, 0.4f, 0f, true, 0.9f, ringTex, 1f);
                    MeshObj(ProceduralMeshes.Disc(40, 0.65f, 1.25f), body.transform, Vector3.zero, Vector3.one, ringMat, Quaternion.identity);
                }
                p.extraDistance = 30f;
                p.spread = 40f;
                p.drift = Drift(p.root, new Vector3(0f, Random.Range(2f, 5f), 0f), 0f, 0f);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Station", Placement.Floating, 1.5f, 3f, 2.8f);
                Mesh hub = ProceduralMeshes.Cone(i, 0.55f, 0.55f, 2.2f, 12, 2, 0f, 0f, true);
                MeshObj(hub, p.root.transform, new Vector3(0f, 0f, -1.1f), Vector3.one, hull, Quaternion.Euler(90f, 0f, 0f));
                MeshObj(ProceduralMeshes.Cone(i + 5, 0.85f, 0.85f, 0.5f, 12, 1, 0f, 0f, true), p.root.transform, new Vector3(0f, 0f, -0.25f), Vector3.one, hull, Quaternion.Euler(90f, 0f, 0f));
                MeshObj(ProceduralMeshes.Cone(9, 0.08f, 0.08f, 5.2f, 6, 1, 0f, 0f, false), p.root.transform, new Vector3(-2.6f, 0f, 0f), Vector3.one, hull, Quaternion.Euler(0f, 0f, -90f));
                Prim(PrimitiveType.Cube, p.root.transform, new Vector3(2.0f, 0f, 0f), new Vector3(2.4f, 0.06f, 1.0f), panel, Quaternion.identity);
                Prim(PrimitiveType.Cube, p.root.transform, new Vector3(-2.0f, 0f, 0f), new Vector3(2.4f, 0.06f, 1.0f), panel, Quaternion.identity);
                MeshObj(ProceduralMeshes.Dome(12, 5, 0.35f, 0f), p.root.transform, new Vector3(0f, 0.75f, 0.3f), Vector3.one * 0.6f, dish, Quaternion.Euler(-40f, 0f, 0f));
                p.drift = Drift(p.root, new Vector3(0f, 14f, 5f), 0f, 0f);
                p.spread = 22f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Debris", Placement.Floating, 1f, 2f, 2f);
                for (int c = 0; c < 4; c++)
                {
                    MeshObj(ProceduralMeshes.Rock(c, 1, 0.5f, 2f), p.root.transform, Random.insideUnitSphere * 1.6f, Vector3.one * Random.Range(0.15f, 0.4f), rock, Random.rotation);
                }
                p.drift = Drift(p.root, new Vector3(11f, -18f, 14f), 0f, 0f);
                p.spread = 20f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // SOLAR SYSTEM: almost nothing. The named planets on a solar route are
        // world-space landmarks placed by CelestialRoute at fixed distances, not
        // per-segment props, so all this theme contributes is the occasional bit
        // of rock drifting past to give the empty stretches a sense of motion.
        // ------------------------------------------------------------------
        private static Material SunlitRock(string key, int seed, Color dark, Color light, float relief, float tiling)
        {
            // Low smoothness, strong relief, no emission: out here the only thing shaping a rock is
            // the sun's terminator, so the normal map has to do all of the work.
            return Mat(key, Color.white, 0f, 0.14f, 0f, false, 1f,
                       ProceduralTextures.Rock(seed, dark, light, relief), tiling, 2.1f);
        }

        private static void BuildSolarSystem(Transform parent, List<Prop> props)
        {
            Material rock = SunlitRock("sol_rock", 11, new Color(0.055f, 0.050f, 0.048f), new Color(0.30f, 0.27f, 0.25f), 3f, 1.5f);

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "Meteoroid", Placement.Floating, 0.5f, 1.6f, 1.2f);
                int chunks = Random.Range(1, 3);
                for (int c = 0; c < chunks; c++)
                {
                    Mesh m = ProceduralMeshes.Rock(Random.Range(0, 4), 2, 0.45f, 1.8f);
                    Vector3 sc = new Vector3(Random.Range(0.7f, 1.4f), Random.Range(0.6f, 1.1f), Random.Range(0.7f, 1.4f));
                    MeshObj(m, p.root.transform, Random.insideUnitSphere * 0.5f, sc, rock, Random.rotation);
                }
                p.drift = Drift(p.root, new Vector3(Random.Range(-18f, 18f), Random.Range(-18f, 18f), 0f), 0f, 0f);
                p.spread = 30f;
                CastShadows(p);
                props.Add(p);
            }

            {
                Prop p = NewProp(parent, "Dust", Placement.Floating, 0.6f, 1.2f, 1.6f);
                for (int c = 0; c < 4; c++)
                {
                    MeshObj(ProceduralMeshes.Rock(c + 5, 1, 0.5f, 2f), p.root.transform,
                            Random.insideUnitSphere * 1.8f, Vector3.one * Random.Range(0.08f, 0.2f), rock, Random.rotation);
                }
                p.drift = Drift(p.root, new Vector3(9f, -14f, 11f), 0f, 0f);
                p.spread = 26f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // ASTEROID BELT: the same rock, but everywhere. Sizes run from gravel to
        // small moons so the field reads as depth rather than as one repeated
        // object, and the biggest ones cast so the tube flies through real shade.
        // ------------------------------------------------------------------
        private static void BuildAsteroidBelt(Transform parent, List<Prop> props)
        {
            Material rock = SunlitRock("belt_rock", 12, new Color(0.075f, 0.065f, 0.058f), new Color(0.38f, 0.33f, 0.28f), 3.2f, 1.5f);
            Material ice = SunlitRock("belt_ice", 13, new Color(0.16f, 0.20f, 0.26f), new Color(0.68f, 0.78f, 0.88f), 2.2f, 1.2f);
            Material metal = Mat("belt_metal", new Color(0.48f, 0.46f, 0.44f), 0f, 0.62f, 0.75f, false, 1f,
                                 ProceduralTextures.Rock(14, new Color(0.22f, 0.21f, 0.20f), new Color(0.62f, 0.60f, 0.58f), 1.6f), 2f, 1.5f);

            // Four big ones: these are the silhouettes the player actually flies past.
            for (int i = 0; i < 4; i++)
            {
                Prop p = NewProp(parent, "Asteroid", Placement.Floating, 1.8f, 4.5f, 1.5f);
                Material m = i == 3 ? ice : rock;
                int chunks = Random.Range(2, 4);
                for (int c = 0; c < chunks; c++)
                {
                    Vector3 sc = new Vector3(Random.Range(0.7f, 1.6f), Random.Range(0.6f, 1.2f), Random.Range(0.7f, 1.5f));
                    MeshObj(ProceduralMeshes.Rock(Random.Range(0, 4), 2, 0.42f, 1.7f), p.root.transform,
                            Random.insideUnitSphere * 0.7f, sc, m, Random.rotation);
                }
                p.drift = Drift(p.root, new Vector3(Random.Range(-22f, 22f), Random.Range(-22f, 22f), 0f), 0f, 0f);
                p.spread = 24f;
                CastShadows(p);
                props.Add(p);
            }

            // Mid-field rubble, kept off the shadow pass - at this size the shadow is noise.
            for (int i = 0; i < 4; i++)
            {
                Prop p = NewProp(parent, "Rubble", Placement.Floating, 0.7f, 2f, 1.2f);
                MeshObj(ProceduralMeshes.Rock(i + 15, 2, 0.5f, 2f), p.root.transform, Vector3.zero,
                        new Vector3(Random.Range(0.8f, 1.5f), Random.Range(0.7f, 1.2f), Random.Range(0.8f, 1.4f)),
                        i % 4 == 0 ? metal : rock, Random.rotation);
                p.drift = Drift(p.root, new Vector3(Random.Range(-34f, 34f), Random.Range(-34f, 34f), Random.Range(-20f, 20f)), 0f, 0f);
                p.spread = 30f;
                props.Add(p);
            }

            // Gravel clouds: cheap, and they are what sells the field as dense rather than sparse.
            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Gravel", Placement.Floating, 0.8f, 1.6f, 2.2f);
                for (int c = 0; c < 5; c++)
                {
                    MeshObj(ProceduralMeshes.Rock(c, 1, 0.55f, 2.2f), p.root.transform,
                            Random.insideUnitSphere * 2.4f, Vector3.one * Random.Range(0.12f, 0.35f), rock, Random.rotation);
                }
                p.drift = Drift(p.root, new Vector3(13f, -17f, 9f), 0f, 0f);
                p.spread = 28f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // JUNGLE: barked trees with branches and leaf-clump canopies, curled
        // ferns, glowing cup flowers, mossy boulders, hanging vines
        // ------------------------------------------------------------------
        private static void BuildJungle(Transform parent, List<Prop> props)
        {
            Material trunk = Mat("jungle_trunk", Color.white, 0f, 0.15f, 0f, false, 1f,
                                 ProceduralTextures.Bark(1, new Color(0.16f, 0.09f, 0.04f), new Color(0.42f, 0.26f, 0.12f)), 1.5f, 1.9f);
            Material leaf = Mat("jungle_leaf", Color.white, 0.05f, 0.4f, 0f, false, 1f,
                                ProceduralTextures.Leaf(1, new Color(0.10f, 0.42f, 0.12f), new Color(0.38f, 0.85f, 0.25f), new Color(0.75f, 0.95f, 0.45f)), 1f);
            Material moss = Mat("jungle_moss", Color.white, 0f, 0.2f, 0f, false, 1f,
                                ProceduralTextures.Foliage(4, new Color(0.08f, 0.16f, 0.09f), new Color(0.22f, 0.40f, 0.16f)), 1.5f, 2.0f);
            Material vine = Mat("jungle_vine", Color.white, 0f, 0.3f, 0f, false, 1f,
                                ProceduralTextures.Bark(3, new Color(0.10f, 0.28f, 0.08f), new Color(0.28f, 0.55f, 0.18f)), 1f);
            Color[] canopyDark = { new Color(0.06f, 0.30f, 0.10f), new Color(0.10f, 0.38f, 0.08f), new Color(0.04f, 0.24f, 0.10f) };
            Color[] canopyLight = { new Color(0.28f, 0.72f, 0.22f), new Color(0.45f, 0.85f, 0.20f), new Color(0.18f, 0.60f, 0.26f) };
            Color[] flowers = { new Color(1f, 0.30f, 0.60f), new Color(1f, 0.70f, 0.10f), new Color(0.60f, 0.25f, 1f), new Color(0.2f, 0.9f, 1f) };

            for (int i = 0; i < 4; i++)
            {
                Prop p = NewProp(parent, "Tree", Placement.Grounded, 1f, 2.2f, 1f);
                float h = Random.Range(8f, 13f);
                Mesh trunkMesh = ProceduralMeshes.Cone(i, 0.6f, 0.28f, h, 10, 6, 0.08f, Random.Range(-0.04f, 0.04f), true);
                MeshObj(trunkMesh, p.root.transform, Vector3.zero, Vector3.one, trunk, Quaternion.identity);
                int branches = Random.Range(2, 4);
                for (int b = 0; b < branches; b++)
                {
                    float by = h * Random.Range(0.55f, 0.8f);
                    Quaternion br = Quaternion.Euler(Random.Range(30f, 55f), b * (360f / branches) + Random.Range(-30f, 30f), 0f);
                    MeshObj(ProceduralMeshes.Cone(b + 10, 0.2f, 0.06f, h * 0.35f, 7, 4, 0.1f, 0.15f, false), p.root.transform, new Vector3(0f, by, 0f), Vector3.one, trunk, br);
                }
                int blobs = Random.Range(3, 5);
                for (int b = 0; b < blobs; b++)
                {
                    int shade = Random.Range(0, canopyDark.Length);
                    Material cm = Mat("jungle_canopy_" + shade, Color.white, 0f, 0.25f, 0f, false, 1f,
                                      ProceduralTextures.Foliage(shade + 10, canopyDark[shade], canopyLight[shade]), 2f, 1.5f);
                    Vector3 pos = new Vector3(Random.Range(-1.8f, 1.8f), h - Random.Range(0f, 2.5f), Random.Range(-1.8f, 1.8f));
                    Vector3 s = new Vector3(Random.Range(1.6f, 2.4f), Random.Range(1.1f, 1.6f), Random.Range(1.6f, 2.4f));
                    MeshObj(ProceduralMeshes.Rock(b + 20, 2, 0.45f, 1.4f), p.root.transform, pos, s, cm, Random.rotation);
                }
                p.lowerHalfBias = true;
                p.spread = 12f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "Fern", Placement.Grounded, 0.8f, 1.6f, 1f);
                int fronds = Random.Range(5, 8);
                for (int f = 0; f < fronds; f++)
                {
                    Quaternion r = Quaternion.Euler(-Random.Range(35f, 55f), f * (360f / fronds) + Random.Range(-15f, 15f), 0f);
                    MeshObj(ProceduralMeshes.Leaf(f % 3, 2.4f, 0.75f, 0.45f, 0.2f), p.root.transform, Vector3.up * 0.15f, Vector3.one, leaf, r);
                }
                p.lowerHalfBias = true;
                p.spread = 6f;
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "GlowFlower", Placement.Grounded, 0.8f, 1.5f, 1f);
                Color fc = flowers[Random.Range(0, flowers.Length)];
                Material petal = Mat("jungle_petal_" + ColorKey(fc), fc, 1.2f, 0.6f);
                Material bulb = Mat("jungle_bulb_" + ColorKey(fc), Color.Lerp(fc, Color.white, 0.5f), 3.0f, 0.7f);
                float h = Random.Range(1.6f, 3f);
                MeshObj(ProceduralMeshes.Cone(i + 30, 0.09f, 0.05f, h, 6, 5, 0.05f, 0.08f, false), p.root.transform, Vector3.zero, Vector3.one, vine, Quaternion.identity);
                // cup: a dome flipped to open upward
                MeshObj(ProceduralMeshes.Dome(12, 5, 0.55f, 0.35f), p.root.transform, new Vector3(h * 0.08f, h + 0.35f, 0f), Vector3.one * 0.7f, petal, Quaternion.Euler(180f, 0f, 0f));
                MeshObj(ProceduralMeshes.Sphere(2), p.root.transform, new Vector3(h * 0.08f, h + 0.2f, 0f), Vector3.one * 0.28f, bulb, Quaternion.identity);
                for (int l = 0; l < 3; l++)
                {
                    MeshObj(ProceduralMeshes.Leaf(l, 1.4f, 0.5f, 0.35f, 0.15f), p.root.transform, Vector3.zero, Vector3.one, leaf, Quaternion.Euler(-40f, l * 120f, 0f));
                }
                p.lowerHalfBias = true;
                p.spread = 5f;
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Boulder", Placement.Grounded, 1f, 2f, 1f);
                MeshObj(ProceduralMeshes.Rock(i + 40, 2, 0.3f, 1.5f), p.root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 1.1f, 1.5f), moss, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                p.lowerHalfBias = true;
                p.spread = 8f;
                CastShadows(p);
                props.Add(p);
            }

            {
                Prop p = NewProp(parent, "Vines", Placement.Grounded, 1f, 1.6f, 1f);
                for (int v = 0; v < 4; v++)
                {
                    float h = Random.Range(6f, 12f);
                    Vector3 pos = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                    MeshObj(ProceduralMeshes.Cone(v + 50, 0.09f, 0.06f, h, 6, 8, 0.12f, Random.Range(-0.06f, 0.06f), false), p.root.transform, pos, Vector3.one, vine, Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), 0f));
                }
                p.spread = 10f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // UNDERWATER: kelp with fronds, pitted coral fans, wrinkled brain
        // coral, jellyfish with trailing tendrils, rocks
        // ------------------------------------------------------------------
        private static void BuildUnderwater(Transform parent, List<Prop> props)
        {
            Material kelp = Mat("sea_kelp", Color.white, 0.05f, 0.45f, 0f, false, 1f,
                                ProceduralTextures.Bark(5, new Color(0.05f, 0.30f, 0.20f), new Color(0.18f, 0.62f, 0.40f)), 1f);
            Material frond = Mat("sea_frond", Color.white, 0.05f, 0.5f, 0f, false, 1f,
                                 ProceduralTextures.Leaf(5, new Color(0.06f, 0.35f, 0.24f), new Color(0.20f, 0.70f, 0.45f), new Color(0.45f, 0.90f, 0.60f)), 1f);
            Material rock = Mat("sea_rock", Color.white, 0f, 0.3f, 0f, false, 1f,
                                ProceduralTextures.Rock(6, new Color(0.04f, 0.08f, 0.15f), new Color(0.16f, 0.24f, 0.36f), 2.5f), 1.5f, 1.8f);
            Material jelly = Mat("sea_jelly", new Color(0.55f, 0.90f, 1f), 1.3f, 0.95f, 0f, true, 0.42f);
            Material tendril = Mat("sea_tendril", new Color(0.8f, 0.5f, 1f), 1.0f, 0.5f, 0f, true, 0.3f);
            Material jellyCore = Mat("sea_jellycore", new Color(1f, 0.6f, 0.9f), 2.5f, 0.8f);
            Color[] corals = { new Color(1f, 0.35f, 0.50f), new Color(1f, 0.60f, 0.20f), new Color(0.70f, 0.30f, 1f), new Color(0.2f, 1f, 0.8f) };

            for (int i = 0; i < 4; i++)
            {
                Prop p = NewProp(parent, "Kelp", Placement.Grounded, 1f, 2f, 1f);
                int strands = Random.Range(2, 4);
                for (int s = 0; s < strands; s++)
                {
                    float h = Random.Range(7f, 14f);
                    Vector3 pos = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                    Quaternion lean = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), 0f);
                    MeshObj(ProceduralMeshes.Cone(s + i * 3, 0.18f, 0.05f, h, 8, 8, 0.15f, 0.08f, false), p.root.transform, pos, Vector3.one, kelp, lean);
                    for (int f = 0; f < 3; f++)
                    {
                        float fy = h * (0.3f + f * 0.22f);
                        Quaternion fr = lean * Quaternion.Euler(-Random.Range(20f, 50f), f * 137f, 0f);
                        MeshObj(ProceduralMeshes.Leaf(f, 1.8f, 0.45f, 0.3f, 0.1f), p.root.transform, pos + lean * new Vector3(0.08f * fy * fy / h, fy, 0f), Vector3.one, frond, fr);
                    }
                }
                p.lowerHalfBias = true;
                p.spread = 10f;
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "CoralFan", Placement.Grounded, 0.9f, 1.8f, 1f);
                Color cc = corals[Random.Range(0, corals.Length)];
                Material cm = Mat("sea_coral_" + ColorKey(cc), Color.white, 0.5f, 0.45f, 0f, false, 1f,
                                  ProceduralTextures.Coral(i + 1, cc, cc * 0.35f), 2f, 1.5f);
                cm.SetColor("_EmissionColor", cc * 0.45f);
                int fans = Random.Range(4, 7);
                for (int f = 0; f < fans; f++)
                {
                    Quaternion r = Quaternion.Euler(-Random.Range(55f, 80f), f * (360f / fans) + Random.Range(-15f, 15f), Random.Range(-15f, 15f));
                    MeshObj(ProceduralMeshes.Leaf(f % 2 + 5, 2.4f, 1.5f, 0.12f, 0.08f, 6), p.root.transform, Vector3.up * 0.1f, Vector3.one, cm, r);
                }
                p.lowerHalfBias = true;
                p.spread = 6f;
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "BrainCoral", Placement.Grounded, 1f, 2f, 1f);
                Color cc = corals[Random.Range(0, corals.Length)];
                Material cm = Mat("sea_brain_" + ColorKey(cc), Color.white, 0.4f, 0.5f, 0f, false, 1f,
                                  ProceduralTextures.Coral(i + 7, cc, cc * 0.3f), 3f, 2.2f);
                cm.SetColor("_EmissionColor", cc * 0.35f);
                MeshObj(ProceduralMeshes.Rock(i + 60, 3, 0.10f, 4.5f), p.root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.9f, 1.3f), cm, Quaternion.identity);
                p.lowerHalfBias = true;
                p.spread = 6f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "Jellyfish", Placement.Floating, 0.8f, 1.8f, 1.9f);
                MeshObj(ProceduralMeshes.Dome(16, 8, 0.8f, 0.3f), p.root.transform, Vector3.zero, Vector3.one * 0.9f, jelly, Quaternion.identity);
                MeshObj(ProceduralMeshes.Sphere(2), p.root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.45f, 0.3f, 0.45f), jellyCore, Quaternion.identity);
                for (int t = 0; t < 5; t++)
                {
                    float a = t * Mathf.PI * 0.4f;
                    Vector3 pos = new Vector3(Mathf.Cos(a) * 0.4f, 0.05f, Mathf.Sin(a) * 0.4f);
                    Mesh tm = ProceduralMeshes.Cone(t + 70, 0.05f, 0.015f, Random.Range(1.6f, 2.6f), 6, 8, 0.25f, 0.2f, false);
                    MeshObj(tm, p.root.transform, pos, Vector3.one, tendril, Quaternion.Euler(180f + Random.Range(-10f, 10f), a * Mathf.Rad2Deg, 0f));
                }
                p.upright = true;
                p.spread = 18f;
                p.drift = Drift(p.root, new Vector3(0f, 22f, 0f), 0.7f, Random.Range(1.8f, 2.8f));
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "SeaRock", Placement.Grounded, 1f, 2.4f, 1f);
                MeshObj(ProceduralMeshes.Rock(i + 80, 2, 0.35f, 1.6f), p.root.transform, new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.9f, 1.3f), rock, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                p.lowerHalfBias = true;
                p.spread = 8f;
                CastShadows(p);
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // VOLCANO: faceted obsidian spires, crazed lava pools with glowing
        // cracks, drifting hot rocks, lava columns
        // ------------------------------------------------------------------
        private static void BuildVolcano(Transform parent, List<Prop> props)
        {
            Material obsidian = Mat("lava_obsidian", Color.white, 0f, 0.92f, 0.35f, false, 1f,
                                    ProceduralTextures.Rock(9, new Color(0.03f, 0.02f, 0.04f), new Color(0.10f, 0.07f, 0.12f), 1.2f), 1.5f, 1.1f);
            ProceduralTextures.TexSet lavaTex = ProceduralTextures.LavaCracks(1, new Color(0.10f, 0.05f, 0.04f), new Color(1f, 0.45f, 0.05f), 0.07f);
            Material lava = Mat("lava_cracked", Color.white, 2.8f, 0.25f, 0f, false, 1f, lavaTex, 1f, 2.1f);
            ProceduralTextures.TexSet hotTex = ProceduralTextures.LavaCracks(2, new Color(0.14f, 0.08f, 0.06f), new Color(1f, 0.30f, 0.02f), 0.05f);
            Material hotRock = Mat("lava_hotrock", Color.white, 1.8f, 0.3f, 0f, false, 1f, hotTex, 1.5f, 2.0f);
            Material pool = Mat("lava_pool", Color.white, 3.5f, 0.15f, 0f, false, 1f,
                                ProceduralTextures.LavaCracks(3, new Color(0.35f, 0.08f, 0.02f), new Color(1f, 0.55f, 0.08f), 0.16f), 1f, 1f);

            for (int i = 0; i < 4; i++)
            {
                Prop p = NewProp(parent, "Spire", Placement.Grounded, 1f, 2.6f, 1f);
                float h = Random.Range(6f, 11f);
                MeshObj(ProceduralMeshes.Crystal(i, Random.Range(5, 8), 1.0f, h, 0.3f), p.root.transform, Vector3.zero, Vector3.one, obsidian, Quaternion.identity);
                MeshObj(ProceduralMeshes.Crystal(i + 4, 5, 0.6f, h * 0.55f, 0.35f), p.root.transform, new Vector3(0.7f, 0f, -0.4f), Vector3.one, obsidian, Quaternion.Euler(Random.Range(5f, 18f), Random.Range(0f, 360f), 0f));
                p.lowerHalfBias = true;
                p.spread = 12f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "LavaPool", Placement.Grounded, 1f, 2.2f, 1f);
                MeshObj(ProceduralMeshes.Disc(20, 0f, 3.0f, false), p.root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1f, 1f, 0.8f), pool, Quaternion.identity);
                int rim = Random.Range(2, 4);
                for (int r = 0; r < rim; r++)
                {
                    float a = r * (360f / rim) + Random.Range(-30f, 30f);
                    Vector3 pos = Quaternion.Euler(0f, a, 0f) * new Vector3(2.2f, 0.25f, 0f);
                    MeshObj(ProceduralMeshes.Rock(r + 90, 2, 0.4f, 1.8f), p.root.transform, pos, new Vector3(1.1f, 0.6f, 1.0f), hotRock, Random.rotation);
                }
                p.lowerHalfBias = true;
                p.spread = 6f;
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "HotRock", Placement.Floating, 0.8f, 2f, 1.3f);
                MeshObj(ProceduralMeshes.Rock(i + 100, 2, 0.4f, 1.6f), p.root.transform, Vector3.zero, new Vector3(1.3f, 1.0f, 1.2f), hotRock, Quaternion.identity);
                p.spread = 20f;
                p.drift = Drift(p.root, new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0f), 0f, 0f);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "LavaColumn", Placement.Grounded, 1f, 1.8f, 1f);
                float h = Random.Range(5f, 9f);
                MeshObj(ProceduralMeshes.Cone(i + 110, 0.7f, 0.45f, h, 10, 6, 0.12f, 0f, true), p.root.transform, Vector3.zero, Vector3.one, lava, Quaternion.identity);
                MeshObj(ProceduralMeshes.Cone(i + 115, 1.3f, 0.9f, h * 0.35f, 8, 3, 0.2f, 0f, true), p.root.transform, Vector3.zero, Vector3.one, obsidian, Quaternion.identity);
                p.lowerHalfBias = true;
                p.spread = 10f;
                CastShadows(p);
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // CRYSTAL: faceted gem clusters, veined ice boulders and pillars,
        // spinning glow shards
        // ------------------------------------------------------------------
        private static void BuildCrystal(Transform parent, List<Prop> props)
        {
            ProceduralTextures.TexSet iceTex = ProceduralTextures.Ice(1, new Color(0.62f, 0.78f, 0.92f), new Color(0.95f, 1f, 1f));
            Material ice = Mat("ice_boulder", Color.white, 0.08f, 0.88f, 0.05f, false, 1f, iceTex, 1.5f, 1.8f);
            Material pillar = Mat("ice_pillar", new Color(0.85f, 0.92f, 1f), 0.08f, 0.8f, 0.05f, false, 1f,
                                  ProceduralTextures.Ice(2, new Color(0.50f, 0.66f, 0.86f), new Color(0.85f, 0.95f, 1f)), 1.5f, 1.6f);
            Color[] gems = { new Color(0.30f, 0.90f, 1f), new Color(0.70f, 0.40f, 1f), new Color(1f, 0.40f, 0.90f), new Color(0.3f, 1f, 0.7f) };

            for (int i = 0; i < 5; i++)
            {
                Prop p = NewProp(parent, "CrystalCluster", Placement.Grounded, 0.8f, 2f, 1f);
                int shards = Random.Range(3, 6);
                for (int s = 0; s < shards; s++)
                {
                    Color gc = gems[Random.Range(0, gems.Length)];
                    Material gm = Mat("ice_gem_" + ColorKey(gc), gc, 1.5f, 0.96f, 0f, true, 0.6f, iceTex, 1f);
                    float h = Random.Range(2f, 4.5f);
                    Quaternion tilt = Quaternion.Euler(Random.Range(-28f, 28f), Random.Range(0f, 360f), Random.Range(-28f, 28f));
                    Vector3 pos = new Vector3(Random.Range(-0.9f, 0.9f), -0.2f, Random.Range(-0.9f, 0.9f));
                    MeshObj(ProceduralMeshes.Crystal(s + i * 7, 6, Random.Range(0.28f, 0.45f), h, 0.35f), p.root.transform, pos, Vector3.one, gm, tilt);
                }
                MeshObj(ProceduralMeshes.Rock(i + 120, 2, 0.3f, 1.5f), p.root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.4f, 0.5f, 1.3f), ice, Quaternion.identity);
                p.lowerHalfBias = true;
                p.spread = 8f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "IceBoulder", Placement.Grounded, 1f, 2.2f, 1f);
                MeshObj(ProceduralMeshes.Rock(i + 130, 3, 0.25f, 1.4f), p.root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.4f, 1.1f, 1.3f), ice, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                p.lowerHalfBias = true;
                p.spread = 8f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "IcePillar", Placement.Grounded, 1f, 2f, 1f);
                float h = Random.Range(6f, 10f);
                MeshObj(ProceduralMeshes.Cone(i + 140, 1.2f, 0.75f, h, 12, 8, 0.1f, 0f, true), p.root.transform, Vector3.zero, Vector3.one, pillar, Quaternion.Euler(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f)));
                for (int s = 0; s < 3; s++)
                {
                    Color gc = gems[Random.Range(0, gems.Length)];
                    Material gm = Mat("ice_gem_" + ColorKey(gc), gc, 1.5f, 0.96f, 0f, true, 0.6f, iceTex, 1f);
                    Quaternion tilt = Quaternion.Euler(Random.Range(20f, 45f), s * 120f + Random.Range(-30f, 30f), 0f);
                    MeshObj(ProceduralMeshes.Crystal(s + 50, 6, 0.3f, 1.8f, 0.4f), p.root.transform, tilt * new Vector3(0f, 0f, 0f) + new Vector3(0f, 0f, 0f), Vector3.one, gm, tilt);
                }
                p.spread = 12f;
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "GlowShard", Placement.Floating, 0.5f, 1.1f, 1.3f);
                Color gc = gems[Random.Range(0, gems.Length)];
                Material gm = Mat("ice_shard_" + ColorKey(gc), gc, 3.0f, 0.9f);
                MeshObj(ProceduralMeshes.Crystal(i + 60, 5, 0.28f, 2.4f, 0.45f), p.root.transform, new Vector3(0f, -1.2f, 0f), Vector3.one, gm, Quaternion.identity);
                p.spread = 14f;
                p.drift = Drift(p.root, new Vector3(0f, 50f, 70f), 0.4f, 2.0f);
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private static Prop NewProp(Transform parent, string name, Placement placement, float minScale, float maxScale, float bodyRadius)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Prop prop = new Prop
            {
                root = root,
                placement = placement,
                minScale = minScale,
                maxScale = maxScale,
                bodyRadius = bodyRadius
            };

            SceneryDrift d = root.AddComponent<SceneryDrift>();
            float dir = Random.value < 0.5f ? -1f : 1f;
            if (placement == Placement.Encircle)
            {
                // Wraps the tube: never orbits or wanders; a builder may give it a spin.
                prop.orbitSpeed = 0f;
            }
            else if (placement == Placement.Grounded)
            {
                // Anchored to the tube wall: it creeps around the tube and leans,
                // but never slides off its footing.
                prop.orbitSpeed = dir * Random.Range(4f, 9f);
                d.alignToRadial = true;
                d.tiltAmplitude = Random.Range(3f, 8f);
                d.tiltSpeed = Random.Range(1.1f, 2.2f);
            }
            else
            {
                // Free-floating: orbits faster and drifts along and across the tube.
                prop.orbitSpeed = dir * Random.Range(10f, 28f);
                d.surgeAmplitude = Random.Range(1.2f, 2.2f);
                d.surgeSpeed = Random.Range(1.0f, 2.0f);
                d.swayAmplitude = Random.Range(1.8f, 3.5f);
                d.swaySpeed = Random.Range(0.9f, 1.8f);
                d.radialAmplitude = Random.Range(1.0f, 2.2f);
                d.radialSpeed = Random.Range(0.8f, 1.6f);
            }
            prop.drift = d;
            return prop;
        }

        private static SceneryDrift Drift(GameObject root, Vector3 spin, float bobAmplitude, float bobSpeed)
        {
            SceneryDrift d = root.GetComponent<SceneryDrift>();
            if (d == null) d = root.AddComponent<SceneryDrift>();
            d.spin = spin;
            d.bobAmplitude = bobAmplitude;
            d.bobSpeed = bobSpeed;
            return d;
        }

        private static void SetupRenderer(MeshRenderer r, Material mat)
        {
            r.sharedMaterial = mat;
            // Casting is opt-in per prop (see CastShadows); receiving is on for everything, because
            // it is the sampling that gives an unlit-looking prop its footing and it costs nothing
            // extra once the main light's shadow pass is already running.
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        /// <summary>
        /// Opts a prop into the main light's shadow pass. Reserved for the big grounded silhouettes -
        /// trunks, spires, pillars, boulders - because they are the only things whose shadow lands
        /// somewhere the player can see it. Scattering it over ferns, debris and floating props would
        /// multiply the shadow-map draw calls for detail that reads as noise at flying speed.
        /// </summary>
        private static void CastShadows(Prop prop)
        {
            MeshRenderer[] renderers = prop.root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        /// <summary>A generated mesh as a child renderer (no collider).</summary>
        private static GameObject MeshObj(Mesh mesh, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Quaternion localRot)
        {
            GameObject go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            SetupRenderer(go.AddComponent<MeshRenderer>(), mat);
            return go;
        }

        /// <summary>A Unity primitive as a child renderer, collider stripped.</summary>
        private static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Quaternion localRot)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            SetupRenderer(go.GetComponent<MeshRenderer>(), mat);
            return go;
        }

        private static string ColorKey(Color c)
        {
            return ((int)(c.r * 255)).ToString("x2") + ((int)(c.g * 255)).ToString("x2") + ((int)(c.b * 255)).ToString("x2");
        }

        private static Shader FindLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Unlit/Color");
            return s;
        }

        /// <summary>
        /// Cached URP Lit material. With a TexSet the albedo is the base map
        /// (colour acts as tint), the normal map is bound with _NORMALMAP, and
        /// an emission map is multiplied by colour * emission. Without one,
        /// emission &gt; 0 makes a flat emissive colour.
        /// </summary>
        private static Material Mat(string key, Color color, float emission = 0f, float smoothness = 0.4f, float metallic = 0f,
                                    bool transparent = false, float alpha = 0.35f,
                                    ProceduralTextures.TexSet tex = null, float tiling = 1f, float bumpScale = 1f)
        {
            Material mat;
            if (materialCache.TryGetValue(key, out mat) && mat != null) return mat;

            mat = new Material(FindLitShader());
            mat.name = "Env_" + key;

            Color baseColor = color;
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                baseColor = tex != null ? color : color * 0.5f;
                baseColor.a = alpha;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

            if (tex != null)
            {
                string texProp = mat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                mat.SetTexture(texProp, tex.albedo);
                mat.SetTextureScale(texProp, Vector2.one * tiling);
                if (tex.normal != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", tex.normal);
                    mat.SetFloat("_BumpScale", bumpScale);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                if (tex != null && tex.emission != null && mat.HasProperty("_EmissionMap"))
                {
                    mat.SetTexture("_EmissionMap", tex.emission);
                    mat.SetColor("_EmissionColor", Color.white * emission);
                }
                else
                {
                    mat.SetColor("_EmissionColor", color * emission);
                }
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            materialCache[key] = mat;
            return mat;
        }
    }
}
