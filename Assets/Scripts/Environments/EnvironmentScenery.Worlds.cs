using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Trackside scenery for the second set of worlds (palettes in EnvironmentThemes.Worlds.cs).
    /// Several of these are enclosed - a cave, a lava tube, a hall of mirrors - so they lean on
    /// Placement.Encircle: one wall or frame per segment, centred on the tube and tiling into a
    /// continuous space around it. The rest follow the originals' rules: a handful of props per
    /// segment, shared cached meshes and materials, shadows only from the big grounded shapes.
    /// </summary>
    public static partial class EnvironmentScenery
    {
        private static void BuildWorld(EnvironmentTheme theme, Transform parent, List<Prop> props)
        {
            switch (theme)
            {
                case EnvironmentTheme.PrismHall:    BuildPrismHall(parent, props); break;
                case EnvironmentTheme.Cavern:       BuildCavern(parent, props); break;
                case EnvironmentTheme.LavaTube:     BuildLavaTube(parent, props); break;
                case EnvironmentTheme.SolarFlare:   BuildSolarFlare(parent, props); break;
                case EnvironmentTheme.BlackHole:    BuildBlackHole(parent, props); break;
                case EnvironmentTheme.Thunderstorm: BuildThunderstorm(parent, props); break;
                case EnvironmentTheme.SunsetCanyon: BuildSunsetCanyon(parent, props); break;
                case EnvironmentTheme.Clockwork:    BuildClockwork(parent, props); break;
                case EnvironmentTheme.SakuraGates:  BuildSakuraGates(parent, props); break;
                case EnvironmentTheme.CandyClouds:  BuildCandyClouds(parent, props); break;
            }
        }

        /// <summary>Segment length the encircling walls are built to span (TunnelSegment.length).</summary>
        private const float WallLength = 20f;

        /// <summary>The tube's angle convention: 0 straight down, increasing toward +X.</summary>
        private static Vector3 WorldRadial(float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), -Mathf.Cos(a), 0f);
        }

        /// <summary>A piece standing on a wall and pointing in toward the tube's axis.</summary>
        private static Quaternion PointInward(Vector3 radial)
        {
            return Quaternion.LookRotation(Vector3.forward, -radial);
        }

        /// <summary>Additive glow (TubityX/PlasmaGlow), or a flat emissive Lit material if it is missing.</summary>
        private static Material GlowMat(string key, Color color, float intensity, float flow = 2f, float rimPower = 1.5f)
        {
            Material mat;
            if (materialCache.TryGetValue(key, out mat) && mat != null) return mat;
            Shader shader = Shader.Find("TubityX/PlasmaGlow");
            if (shader == null) return Mat(key, color, intensity, 0.2f);

            mat = new Material(shader) { name = "Env_" + key };
            mat.SetColor("_Color", color);
            mat.SetFloat("_Intensity", intensity);
            mat.SetFloat("_Flow", flow);
            mat.SetFloat("_RimPower", rimPower);
            materialCache[key] = mat;
            return mat;
        }

        /// <summary>Faked mirror (TubityX/Iridescent), or a polished metal Lit material if it is missing.</summary>
        private static Material MirrorMat(string key, Color baseColor, float intensity)
        {
            Material mat;
            if (materialCache.TryGetValue(key, out mat) && mat != null) return mat;
            Shader shader = Shader.Find("TubityX/Iridescent");
            if (shader == null) return Mat(key, new Color(0.7f, 0.72f, 0.8f), 0.1f, 0.95f, 0.9f);

            mat = new Material(shader) { name = "Env_" + key };
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Intensity", intensity);
            materialCache[key] = mat;
            return mat;
        }

        /// <summary>A Lit material wearing a stripe texture - candy canes and lollipops.</summary>
        private static Material StripeMat(string key, Color a, Color b, int stripes, Vector2 tiling)
        {
            Material mat;
            if (materialCache.TryGetValue(key, out mat) && mat != null) return mat;
            mat = Mat(key, Color.white, 0f, 0.65f);
            string texProp = mat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            mat.SetTexture(texProp, WorldTextures.Stripes(a, b, stripes));
            mat.SetTextureScale(texProp, tiling);
            return mat;
        }

        private static Prop Encircle(Transform parent, string name, float wallLength = 0f, bool upright = false)
        {
            Prop p = NewProp(parent, name, Placement.Encircle, 1f, 1f, 1f);
            p.encircleLength = wallLength;
            p.fixedRoll = upright;
            p.roll = 0f;
            return p;
        }

        // ------------------------------------------------------------------
        // HALL OF MIRRORS: an octagon of mirror panels round the tube, edged in
        // light, a wider one turned half a side behind it, prisms splitting beams
        // ------------------------------------------------------------------
        private static void BuildPrismHall(Transform parent, List<Prop> props)
        {
            Material mirror = MirrorMat("prism_mirror", new Color(0.05f, 0.05f, 0.08f), 1.1f);
            Material edge = Mat("prism_edge", new Color(0.85f, 0.95f, 1f), 3f, 0.9f);

            {
                // Random roll per segment, so the octagons don't line up and the hall reads as a
                // kaleidoscope rather than a corridor.
                Prop p = Encircle(parent, "MirrorHall");
                AddMirrorOctagon(p.root.transform, 10.5f, 7f, 0f, mirror, edge);
                AddMirrorOctagon(p.root.transform, 16f, 5f, 22.5f, mirror, edge);
                props.Add(p);
            }

            Color[] spectrum =
            {
                new Color(1f, 0.2f, 0.2f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.95f, 0.2f),
                new Color(0.3f, 1f, 0.35f), new Color(0.25f, 0.6f, 1f), new Color(0.65f, 0.3f, 1f)
            };

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "PrismShard", Placement.Floating, 1.1f, 2.2f, 1.6f);
                MeshObj(ProceduralMeshes.Crystal(i + 300, 3, 0.9f, 3.4f, 0.6f), p.root.transform, new Vector3(0f, -1.7f, 0f), Vector3.one, mirror, Quaternion.identity);
                p.spread = 14f;
                p.drift = Drift(p.root, new Vector3(Random.Range(-25f, 25f), Random.Range(20f, 45f), 0f), 0.3f, 1.2f);
                props.Add(p);
            }

            {
                // White light into a prism, the spectrum fanning out of it.
                Prop p = NewProp(parent, "SpectrumFan", Placement.Floating, 0.9f, 1.4f, 4f);
                Transform t = p.root.transform;
                Prim(PrimitiveType.Cube, t, new Vector3(-7f, 0f, 0f), new Vector3(14f, 0.22f, 0.22f), GlowMat("prism_beam_white", Color.white, 2.5f, 3f), Quaternion.identity);
                MeshObj(ProceduralMeshes.Crystal(310, 3, 1.3f, 2.8f, 0.6f), t, new Vector3(0f, -1.4f, 0f), Vector3.one, mirror, Quaternion.identity);
                for (int c = 0; c < spectrum.Length; c++)
                {
                    float angle = -14f + c * 5.6f;
                    Quaternion rot = Quaternion.Euler(0f, 0f, angle);
                    Prim(PrimitiveType.Cube, t, rot * new Vector3(9f, 0f, 0f), new Vector3(18f, 0.18f, 0.18f),
                         GlowMat("prism_beam_" + c, spectrum[c], 2.4f, 3f), rot);
                }
                p.spread = 22f;
                p.extraDistance = 6f;
                props.Add(p);
            }
        }

        private static void AddMirrorOctagon(Transform parent, float radius, float depth, float turn, Material mirror, Material edge)
        {
            const int sides = 8;
            float width = 2f * radius * Mathf.Tan(Mathf.PI / sides);
            float corner = radius / Mathf.Cos(Mathf.PI / sides);
            for (int k = 0; k < sides; k++)
            {
                float a = turn + k * 360f / sides;
                Vector3 r = WorldRadial(a);
                Prim(PrimitiveType.Cube, parent, r * radius, new Vector3(width * 0.97f, 0.25f, depth), mirror,
                     Quaternion.LookRotation(Vector3.forward, r));

                Vector3 rc = WorldRadial(a + 180f / sides);
                Prim(PrimitiveType.Cube, parent, rc * corner, new Vector3(0.22f, 0.22f, depth + 0.2f), edge,
                     Quaternion.LookRotation(Vector3.forward, rc));
            }
        }

        // ------------------------------------------------------------------
        // LANTERN CAVE: rock walls all round with stalactites and glowing
        // crystals, timber mine supports hung with lanterns
        // ------------------------------------------------------------------
        private static void BuildCavern(Transform parent, List<Prop> props)
        {
            Material rock = Mat("cave_rock", Color.white, 0f, 0.25f, 0f, false, 1f,
                                ProceduralTextures.Rock(21, new Color(0.09f, 0.06f, 0.08f), new Color(0.36f, 0.25f, 0.20f), 2.2f), 3f, 1.4f);
            Material wood = Mat("cave_wood", Color.white, 0f, 0.2f, 0f, false, 1f,
                                ProceduralTextures.Bark(22, new Color(0.18f, 0.10f, 0.05f), new Color(0.42f, 0.26f, 0.12f)), 1f, 1f);
            Material lantern = Mat("cave_lantern", new Color(1f, 0.72f, 0.35f), 4f, 0.5f);
            Material iron = Mat("cave_iron", new Color(0.12f, 0.11f, 0.12f), 0f, 0.5f, 0.6f);
            Color[] gems = { new Color(0.3f, 1f, 0.9f), new Color(0.7f, 0.4f, 1f), new Color(1f, 0.45f, 0.85f) };

            {
                Prop p = Encircle(parent, "CaveWall", WallLength);
                Transform t = p.root.transform;
                MeshObj(WorldMeshes.Shell(31 + Random.Range(0, 3), 15f, WallLength, 0.30f, 1.6f), t, Vector3.zero, Vector3.one, rock, Quaternion.identity);

                // Stalactites off the roof, shorter stalagmites off the floor, all pointing in.
                for (int k = 0; k < 10; k++)
                {
                    float a = Random.Range(0f, 360f);
                    Vector3 r = WorldRadial(a);
                    bool roof = r.y > 0f;
                    float h = roof ? Random.Range(3f, 6.5f) : Random.Range(1.8f, 3.6f);
                    Mesh cone = ProceduralMeshes.Cone(k + 400, Random.Range(0.7f, 1.4f), 0.05f, h, 8, 4, 0.25f, 0f, true);
                    MeshObj(cone, t, r * 14.2f + Vector3.forward * Random.Range(-8f, 8f), Vector3.one, rock, PointInward(r));
                }

                // Two crystal clusters glowing out of the rock.
                for (int c = 0; c < 2; c++)
                {
                    Vector3 r = WorldRadial(Random.Range(0f, 360f));
                    Vector3 basePos = r * 13.6f + Vector3.forward * Random.Range(-7f, 7f);
                    Color gc = gems[Random.Range(0, gems.Length)];
                    Material gm = Mat("cave_gem_" + ColorKey(gc), gc, 2.2f, 0.9f);
                    for (int s = 0; s < 4; s++)
                    {
                        Quaternion tilt = PointInward(r) * Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-30f, 30f));
                        MeshObj(ProceduralMeshes.Crystal(s + c * 5 + 420, 6, Random.Range(0.25f, 0.4f), Random.Range(1.6f, 3.2f), 0.35f),
                                t, basePos, Vector3.one, gm, tilt);
                    }
                }
                props.Add(p);
            }

            {
                // A timber frame straddling the tube, lanterns hanging off the beam.
                Prop p = Encircle(parent, "MineSupport", 0f, upright: true);
                Transform t = p.root.transform;
                for (int side = -1; side <= 1; side += 2)
                {
                    Prim(PrimitiveType.Cube, t, new Vector3(side * 8.2f, -1.5f, 0f), new Vector3(0.9f, 18f, 0.9f), wood, Quaternion.identity);
                    Prim(PrimitiveType.Cube, t, new Vector3(side * 6.9f, 6.4f, 0f), new Vector3(0.6f, 3.2f, 0.6f), wood, Quaternion.Euler(0f, 0f, side * 45f));
                    Prim(PrimitiveType.Cube, t, new Vector3(side * 4.2f, 7.1f, 0f), new Vector3(0.08f, 1.2f, 0.08f), iron, Quaternion.identity);
                    Prim(PrimitiveType.Cube, t, new Vector3(side * 4.2f, 6.25f, 0f), new Vector3(0.5f, 0.6f, 0.5f), lantern, Quaternion.identity);
                }
                Prim(PrimitiveType.Cube, t, new Vector3(0f, 7.9f, 0f), new Vector3(17.5f, 1f, 1f), wood, Quaternion.identity);
                p.chance = 0.6f;
                CastShadows(p);
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // LAVA TUBE: basalt walls with glowing seams, a molten river along the
        // floor, drips of lava hanging from the roof
        // ------------------------------------------------------------------
        private static void BuildLavaTube(Transform parent, List<Prop> props)
        {
            Material wall = Mat("lavatube_wall", Color.white, 1.4f, 0.35f, 0f, false, 1f,
                                ProceduralTextures.LavaCracks(41, new Color(0.07f, 0.045f, 0.045f), new Color(1f, 0.35f, 0.05f), 0.045f), 3f, 1.6f);
            Material river = Mat("lavatube_river", Color.white, 3.2f, 0.2f, 0f, false, 1f,
                                 ProceduralTextures.LavaCracks(42, new Color(0.9f, 0.25f, 0.03f), new Color(1f, 0.8f, 0.3f), 0.22f), 2f, 0.5f);
            Material obsidian = Mat("lavatube_obsidian", Color.white, 0f, 0.9f, 0.3f, false, 1f,
                                    ProceduralTextures.Rock(43, new Color(0.03f, 0.02f, 0.03f), new Color(0.12f, 0.08f, 0.10f), 1.2f), 1.5f, 1f);
            Material drip = GlowMat("lavatube_drip", new Color(1f, 0.45f, 0.08f), 3f, 1f);

            {
                Prop p = Encircle(parent, "LavaTubeWall", WallLength);
                Transform t = p.root.transform;
                MeshObj(WorldMeshes.Shell(51 + Random.Range(0, 3), 11.5f, WallLength, 0.2f, 1.8f, 36, 10), t, Vector3.zero, Vector3.one, wall, Quaternion.identity);

                for (int k = 0; k < 7; k++)
                {
                    // Roof only: lava drips down, it doesn't grow up.
                    Vector3 r = WorldRadial(Random.Range(120f, 240f));
                    float h = Random.Range(1.6f, 3.4f);
                    Vector3 root = r * 10.6f + Vector3.forward * Random.Range(-8f, 8f);
                    MeshObj(ProceduralMeshes.Cone(k + 500, Random.Range(0.4f, 0.8f), 0.08f, h, 8, 3, 0.2f, 0f, true), t, root, Vector3.one, obsidian, PointInward(r));
                    Prim(PrimitiveType.Sphere, t, root - r * (h + 0.15f), Vector3.one * 0.3f, drip, Quaternion.identity);
                }
                props.Add(p);
            }

            {
                // The river: a bright strip of floor, scrolling back past the player.
                Prop p = Encircle(parent, "LavaRiver", WallLength, upright: true);
                MeshObj(WorldMeshes.Shell(60, 9.4f, WallLength, 0f, 1f, 16, 4, -38f, 76f, 1f, 3f), p.root.transform, Vector3.zero, Vector3.one, river, Quaternion.identity);
                MaterialScroll scroll = p.root.AddComponent<MaterialScroll>();
                scroll.target = river;
                scroll.speed = new Vector2(0f, -0.35f);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Boulder", Placement.Grounded, 0.6f, 1.2f, 1f);
                MeshObj(ProceduralMeshes.Rock(i + 510, 2, 0.35f, 1.6f), p.root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 0.9f, 1.2f), obsidian, Random.rotation);
                p.lowerHalfBias = true;
                p.spread = 1.2f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // CORONA: prominence loops at every distance round the track - the star
        // itself is the backdrop (EnvironmentBackdrops) - and long plasma filaments
        // ------------------------------------------------------------------
        private static void BuildSolarFlare(Transform parent, List<Prop> props)
        {
            Material hot = GlowMat("sun_loop", new Color(1f, 0.55f, 0.15f), 2.8f, 2.5f, 1.2f);
            Material core = GlowMat("sun_loop_core", new Color(1f, 0.85f, 0.5f), 2f, 3f, 2.5f);
            Material filament = GlowMat("sun_filament", new Color(1f, 0.45f, 0.1f), 1.6f, 1.5f, 1f);

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "Prominence", Placement.Floating, 1.2f, 2.6f, 9f);
                float r = Random.Range(7f, 12f);
                MeshObj(WorldMeshes.TorusArc(r, r * 0.09f, 180f), p.root.transform, Vector3.zero, Vector3.one, hot, Quaternion.identity);
                MeshObj(WorldMeshes.TorusArc(r, r * 0.045f, 180f), p.root.transform, Vector3.zero, Vector3.one, core, Quaternion.identity);
                p.extraDistance = 25f;
                p.spread = 70f;
                p.drift = Drift(p.root, new Vector3(0f, Random.Range(3f, 8f), 0f), 0f, 0f);
                PulseScale pulse = p.root.AddComponent<PulseScale>();
                pulse.amount = 0.07f;
                pulse.period = Random.Range(4f, 7f);
                props.Add(p);
            }

            {
                Prop p = NewProp(parent, "Filament", Placement.Floating, 1f, 1.8f, 6f);
                MeshObj(WorldMeshes.TorusArc(40f, 0.45f, 55f), p.root.transform, new Vector3(0f, -40f, 0f), Vector3.one, filament, Quaternion.identity);
                p.extraDistance = 30f;
                p.spread = 60f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // EVENT HORIZON: sparse dark debris; the black hole is the backdrop
        // ------------------------------------------------------------------
        private static void BuildBlackHole(Transform parent, List<Prop> props)
        {
            Material rock = SunlitRock("bh_rock", 601, new Color(0.05f, 0.05f, 0.06f), new Color(0.22f, 0.2f, 0.2f), 2f, 1.5f);
            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "Debris", Placement.Floating, 0.6f, 2.2f, 1.2f);
                MeshObj(ProceduralMeshes.Rock(i + 610, 2, 0.4f, 1.8f), p.root.transform, Vector3.zero, new Vector3(1.3f, 0.9f, 1.1f), rock, Quaternion.identity);
                p.spread = 40f;
                p.extraDistance = 8f;
                p.drift = Drift(p.root, new Vector3(Random.Range(-15f, 15f), Random.Range(-15f, 15f), 0f), 0f, 0f);
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // THUNDERHEAD: banks of dark cloud all round and cloud towers below;
        // the lightning is EnvironmentStorm's
        // ------------------------------------------------------------------
        private static void BuildThunderstorm(Transform parent, List<Prop> props)
        {
            Material dark = Mat("storm_cloud", new Color(0.22f, 0.24f, 0.29f), 0f, 0.05f);
            Material lit = Mat("storm_cloud_lit", new Color(0.36f, 0.38f, 0.44f), 0f, 0.05f);

            for (int i = 0; i < 4; i++)
            {
                Prop p = NewProp(parent, "StormCloud", Placement.Floating, 1.6f, 3.2f, 3.5f);
                int puffs = Random.Range(5, 9);
                for (int k = 0; k < puffs; k++)
                {
                    Vector3 pos = Random.insideUnitSphere * 2.6f;
                    pos.y *= 0.55f;
                    MeshObj(ProceduralMeshes.Rock(i * 10 + k + 700, 2, 0.12f, 1.1f), p.root.transform, pos, Vector3.one * Random.Range(1.2f, 2.2f),
                            k % 2 == 0 ? dark : lit, Random.rotation);
                }
                p.spread = 26f;
                p.extraDistance = 4f;
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "CloudTower", Placement.Grounded, 1.2f, 2.2f, 1f);
                for (int k = 0; k < 6; k++)
                {
                    Vector3 pos = new Vector3(Random.Range(-1.5f, 1.5f), k * 2.4f, Random.Range(-1.5f, 1.5f));
                    MeshObj(ProceduralMeshes.Rock(i * 10 + k + 750, 2, 0.12f, 1.1f), p.root.transform, pos, Vector3.one * Random.Range(1.8f, 2.8f) * (1f - k * 0.08f),
                            k % 2 == 0 ? dark : lit, Random.rotation);
                }
                p.lowerHalfBias = true;
                p.spread = 16f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // SUNSET CANYON: sandstone walls either side, a canyon floor far below,
        // hoodoos, and now and then a natural arch across the sky
        // ------------------------------------------------------------------
        private static void BuildSunsetCanyon(Transform parent, List<Prop> props)
        {
            Color[] strata =
            {
                new Color(0.55f, 0.22f, 0.12f), new Color(0.78f, 0.44f, 0.22f), new Color(0.62f, 0.30f, 0.16f),
                new Color(0.88f, 0.60f, 0.36f), new Color(0.50f, 0.20f, 0.14f)
            };
            Material canyon = Mat("canyon_rock", Color.white, 0f, 0.12f, 0f, false, 1f, ProceduralTextures.Bands(61, strata, 9), 1f, 1f);
            Material sand = Mat("canyon_sand", Color.white, 0f, 0.1f, 0f, false, 1f,
                                ProceduralTextures.Rock(62, new Color(0.55f, 0.30f, 0.18f), new Color(0.85f, 0.58f, 0.38f), 1f), 3f, 0.6f);

            {
                // Walls rise either side (right: 35-150 degrees round from straight down, left: the
                // mirror), open to the sky overhead. Strata run up the wall.
                Prop p = Encircle(parent, "CanyonWalls", WallLength, upright: true);
                Transform t = p.root.transform;
                MeshObj(WorldMeshes.Shell(63, 26f, WallLength, 0.22f, 1.4f, 24, 8, 35f, 115f, 1f, 1f, true), t, Vector3.zero, Vector3.one, canyon, Quaternion.identity);
                MeshObj(WorldMeshes.Shell(64, 26f, WallLength, 0.22f, 1.4f, 24, 8, -150f, 115f, 1f, 1f, true), t, Vector3.zero, Vector3.one, canyon, Quaternion.identity);
                MeshObj(WorldMeshes.Shell(65, 30f, WallLength, 0.08f, 1f, 12, 4, -35f, 70f, 2f, 1f), t, Vector3.zero, Vector3.one, sand, Quaternion.identity);

                // Hoodoos stand on the real floor below and rise toward the track. (Grounded props
                // grow outward from the tube, which here would hang them upside down.)
                int hoodoos = Random.Range(1, 3);
                for (int i = 0; i < hoodoos; i++)
                {
                    float h = Random.Range(8f, 15f);
                    float x = Random.Range(-13f, 13f);
                    float floor = -Mathf.Sqrt(30f * 30f - x * x) + 0.5f;
                    Vector3 at = new Vector3(x, floor, Random.Range(-7f, 7f));
                    MeshObj(ProceduralMeshes.Cone(i + 800, 1.8f, 1.1f, h, 10, 6, 0.25f, 0f, true), t, at, Vector3.one, canyon, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    MeshObj(ProceduralMeshes.Rock(i + 810, 2, 0.3f, 1.4f), t, at + new Vector3(0f, h, 0f), new Vector3(1.9f, 0.7f, 1.8f), canyon, Quaternion.identity);
                }
                CastShadows(p);
                props.Add(p);
            }

            {
                // A natural arch spanning the canyon, legs down to the floor.
                Prop p = Encircle(parent, "NaturalArch", 0f, upright: true);
                Transform t = p.root.transform;
                MeshObj(WorldMeshes.TorusArc(15f, 2.6f, 200f, 28, 10, 0.35f, 64), t, Vector3.zero, Vector3.one, canyon, Quaternion.identity);
                for (int side = -1; side <= 1; side += 2)
                {
                    // From the canyon floor up to where the arch meets it.
                    MeshObj(ProceduralMeshes.Cone(820 + side, 3.4f, 2.5f, 23.5f, 10, 5, 0.2f, 0f, true), t, new Vector3(side * 14.8f, -26f, 0f), Vector3.one, canyon, Quaternion.identity);
                }
                p.chance = 0.3f;
                CastShadows(p);
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // CLOCKWORK: ring gears turning round the tube, cogs spinning in the air,
        // copper pipes with valve wheels
        // ------------------------------------------------------------------
        private static void BuildClockwork(Transform parent, List<Prop> props)
        {
            Material brass = Mat("clock_brass", new Color(0.86f, 0.64f, 0.30f), 0.05f, 0.75f, 0.9f);
            Material copper = Mat("clock_copper", new Color(0.74f, 0.40f, 0.24f), 0f, 0.7f, 0.85f);
            Material iron = Mat("clock_iron", new Color(0.20f, 0.19f, 0.21f), 0f, 0.5f, 0.7f);

            {
                Prop p = Encircle(parent, "RingGear");
                MeshObj(WorldMeshes.Gear(9.2f, 10.6f, 40, 0.9f, 1.4f), p.root.transform, Vector3.zero, Vector3.one, brass, Quaternion.identity);
                p.drift = Drift(p.root, new Vector3(0f, 0f, (Random.value < 0.5f ? -1f : 1f) * Random.Range(8f, 16f)), 0f, 0f);
                props.Add(p);
            }

            {
                Prop p = Encircle(parent, "OuterGear");
                MeshObj(WorldMeshes.Gear(12.6f, 13.6f, 60, 0.7f, 0.8f), p.root.transform, Vector3.zero, Vector3.one, iron, Quaternion.identity);
                p.drift = Drift(p.root, new Vector3(0f, 0f, (Random.value < 0.5f ? -1f : 1f) * Random.Range(5f, 10f)), 0f, 0f);
                p.chance = 0.5f;
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "Cog", Placement.Floating, 1f, 2.2f, 2.6f);
                Material m = i % 2 == 0 ? brass : copper;
                MeshObj(WorldMeshes.Gear(0.6f, 2.4f, 14, 0.5f, 0.7f), p.root.transform, Vector3.zero, Vector3.one, m, Quaternion.identity);
                MeshObj(ProceduralMeshes.Cone(900 + i, 0.35f, 0.35f, 3f, 8, 1, 0f, 0f, true), p.root.transform, new Vector3(0f, 0f, -1.5f), Vector3.one, iron, Quaternion.Euler(90f, 0f, 0f));
                p.spread = 18f;
                p.drift = Drift(p.root, new Vector3(0f, 0f, (Random.value < 0.5f ? -1f : 1f) * Random.Range(40f, 70f)), 0f, 0f);
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Pipe", Placement.Grounded, 1f, 1.6f, 1f);
                float h = Random.Range(8f, 14f);
                MeshObj(ProceduralMeshes.Cone(910 + i, 0.45f, 0.45f, h, 10, 4, 0f, 0f, true), p.root.transform, Vector3.zero, Vector3.one, copper, Quaternion.identity);
                MeshObj(WorldMeshes.Gear(0.3f, 0.9f, 8, 0.15f, 0.2f), p.root.transform, new Vector3(0f, h * 0.6f, 0.55f), Vector3.one, iron, Quaternion.identity);
                p.spread = 10f;
                CastShadows(p);
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // SAKURA GATES: two torii per segment over the tube, cherry trees and
        // stone lanterns below
        // ------------------------------------------------------------------
        private static void BuildSakuraGates(Transform parent, List<Prop> props)
        {
            Material red = Mat("torii_red", new Color(0.86f, 0.18f, 0.10f), 0.12f, 0.35f);
            Material black = Mat("torii_black", new Color(0.06f, 0.05f, 0.05f), 0f, 0.4f);
            Material bark = Mat("sakura_bark", Color.white, 0f, 0.2f, 0f, false, 1f,
                                ProceduralTextures.Bark(71, new Color(0.12f, 0.07f, 0.06f), new Color(0.30f, 0.20f, 0.18f)), 1f, 1f);
            Material blossomA = Mat("sakura_blossom", new Color(1f, 0.72f, 0.84f), 0.12f, 0.25f);
            Material blossomB = Mat("sakura_blossom_b", new Color(1f, 0.86f, 0.93f), 0.12f, 0.25f);
            Material stone = Mat("sakura_stone", new Color(0.55f, 0.54f, 0.52f), 0f, 0.2f);
            Material lamp = Mat("sakura_lamp", new Color(1f, 0.8f, 0.5f), 3f, 0.4f);

            {
                Prop p = Encircle(parent, "Torii", 0f, upright: true);
                Transform t = p.root.transform;
                for (int g = -1; g <= 1; g += 2)
                {
                    float z = g * 5f;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        MeshObj(ProceduralMeshes.Cone(81, 0.55f, 0.46f, 18f, 12, 4, 0f, 0f, true), t, new Vector3(side * 7.4f, -10f, z), Vector3.one, red, Quaternion.identity);
                        // The kasagi's upswept ends.
                        Prim(PrimitiveType.Cube, t, new Vector3(side * 10.3f, 8.75f, z), new Vector3(2.4f, 0.7f, 1.3f), black, Quaternion.Euler(0f, 0f, side * 12f));
                    }
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 8.4f, z), new Vector3(19.2f, 0.8f, 1.3f), black, Quaternion.identity);   // kasagi
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 7.7f, z), new Vector3(18f, 0.5f, 1.1f), red, Quaternion.identity);       // shimaki
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 6.3f, z), new Vector3(17f, 0.5f, 0.75f), red, Quaternion.identity);      // nuki
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 7.0f, z), new Vector3(0.6f, 1.4f, 0.5f), red, Quaternion.identity);      // gakuzuka
                }
                CastShadows(p);
                props.Add(p);
            }

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "CherryTree", Placement.Grounded, 0.9f, 1.5f, 1f);
                float h = Random.Range(5f, 7f);
                MeshObj(ProceduralMeshes.Cone(i + 1000, 0.5f, 0.25f, h, 8, 5, 0.15f, 0.6f, true), p.root.transform, Vector3.zero, Vector3.one, bark, Quaternion.identity);
                for (int k = 0; k < 6; k++)
                {
                    Vector3 pos = new Vector3(Random.Range(-2.5f, 2.5f), h + Random.Range(-0.8f, 1.2f), Random.Range(-2.5f, 2.5f));
                    MeshObj(ProceduralMeshes.Rock(i * 10 + k + 1010, 2, 0.2f, 1.3f), p.root.transform, pos, Vector3.one * Random.Range(1.4f, 2.4f),
                            k % 2 == 0 ? blossomA : blossomB, Random.rotation);
                }
                p.lowerHalfBias = true;
                p.spread = 10f;
                CastShadows(p);
                props.Add(p);
            }

            {
                Prop p = NewProp(parent, "StoneLantern", Placement.Grounded, 0.9f, 1.3f, 1f);
                Transform t = p.root.transform;
                MeshObj(ProceduralMeshes.Cone(1100, 0.45f, 0.3f, 1f, 8, 1, 0f, 0f, true), t, Vector3.zero, Vector3.one, stone, Quaternion.identity);
                Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.4f, 0f), new Vector3(0.85f, 0.8f, 0.85f), stone, Quaternion.identity);
                Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.4f, 0f), new Vector3(0.55f, 0.45f, 0.9f), lamp, Quaternion.identity);
                MeshObj(ProceduralMeshes.Cone(1101, 0.85f, 0.05f, 0.7f, 8, 1, 0f, 0f, true), t, new Vector3(0f, 1.8f, 0f), Vector3.one, stone, Quaternion.identity);
                p.lowerHalfBias = true;
                p.spread = 3f;
                props.Add(p);
            }
        }

        // ------------------------------------------------------------------
        // SUGAR SKY: pastel cotton-candy clouds, lollipops, candy canes, gumdrops
        // ------------------------------------------------------------------
        private static void BuildCandyClouds(Transform parent, List<Prop> props)
        {
            Material[] clouds =
            {
                Mat("candy_cloud_pink", new Color(1f, 0.78f, 0.88f), 0.12f, 0.3f),
                Mat("candy_cloud_mint", new Color(0.72f, 1f, 0.88f), 0.12f, 0.3f),
                Mat("candy_cloud_lilac", new Color(0.84f, 0.78f, 1f), 0.12f, 0.3f)
            };
            Material cane = StripeMat("candy_cane", Color.white, new Color(0.95f, 0.15f, 0.25f), 4, new Vector2(1f, 4f));
            Material swirl = StripeMat("candy_lolly", new Color(1f, 0.4f, 0.7f), Color.white, 6, Vector2.one);
            Material stick = Mat("candy_stick", new Color(1f, 1f, 0.95f), 0.05f, 0.6f);
            Color[] gumColors = { new Color(1f, 0.3f, 0.4f), new Color(0.4f, 0.9f, 0.4f), new Color(1f, 0.8f, 0.2f), new Color(0.5f, 0.5f, 1f) };

            for (int i = 0; i < 3; i++)
            {
                Prop p = NewProp(parent, "CandyCloud", Placement.Floating, 1.4f, 2.6f, 3f);
                Material m = clouds[i % clouds.Length];
                int puffs = Random.Range(5, 8);
                for (int k = 0; k < puffs; k++)
                {
                    Vector3 pos = Random.insideUnitSphere * 2.2f;
                    pos.y *= 0.5f;
                    MeshObj(ProceduralMeshes.Rock(i * 10 + k + 1200, 2, 0.1f, 1f), p.root.transform, pos, Vector3.one * Random.Range(1.2f, 2f), m, Random.rotation);
                }
                p.spread = 24f;
                p.extraDistance = 3f;
                props.Add(p);
            }

            for (int i = 0; i < 2; i++)
            {
                Prop p = NewProp(parent, "Lollipop", Placement.Grounded, 0.9f, 1.5f, 1f);
                MeshObj(ProceduralMeshes.Cone(1300 + i, 0.14f, 0.14f, 5f, 8, 1, 0f, 0f, true), p.root.transform, Vector3.zero, Vector3.one, stick, Quaternion.identity);
                MeshObj(ProceduralMeshes.Disc(28, 0f, 2.3f, true), p.root.transform, new Vector3(0f, 6.2f, 0f), Vector3.one, swirl, Quaternion.Euler(90f, 0f, 0f));
                p.lowerHalfBias = true;
                p.spread = 8f;
                props.Add(p);
            }

            {
                Prop p = NewProp(parent, "CandyCane", Placement.Grounded, 0.9f, 1.4f, 1f);
                MeshObj(ProceduralMeshes.Cone(1310, 0.4f, 0.4f, 6f, 12, 10, 0f, 1.2f, true), p.root.transform, Vector3.zero, Vector3.one, cane, Quaternion.identity);
                p.spread = 8f;
                CastShadows(p);
                props.Add(p);
            }

            {
                Prop p = NewProp(parent, "Gumdrops", Placement.Grounded, 1f, 1.6f, 1f);
                for (int k = 0; k < 3; k++)
                {
                    Color gc = gumColors[Random.Range(0, gumColors.Length)];
                    Material gm = Mat("candy_gum_" + ColorKey(gc), gc, 0.15f, 0.85f);
                    MeshObj(ProceduralMeshes.Dome(16, 8, 0.8f, 0.15f), p.root.transform, new Vector3(Random.Range(-1.6f, 1.6f), 0f, Random.Range(-1.6f, 1.6f)),
                            Vector3.one * Random.Range(0.8f, 1.3f), gm, Quaternion.identity);
                }
                p.lowerHalfBias = true;
                p.spread = 4f;
                props.Add(p);
            }
        }
    }
}
