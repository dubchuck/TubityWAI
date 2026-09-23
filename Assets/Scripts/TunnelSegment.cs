using UnityEngine;

namespace TubityWAI
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TunnelSegment : MonoBehaviour
    {
        [Header("Cylinder Settings")]
        public float radius = 5f;
        public float length = 20f;
        public int radialSegments = 32;
        public Material tunnelMaterial;

        [Header("Marker Rings Settings")]
        public float markerInterval = 5f;
        public Material markerMaterial;
        public float ringWidth = 0.08f;

        [Header("Collectible Settings")]
        public Material[] coinMaterials;
        public Material powerupMaterial;
        public Material magnetMaterial;

        [Header("Obstacle Settings")]
        public Material obstacleMaterial; // standard solid red
        public Material[] transparentObstacleMaterials; // passable color-coded shields
        public float obstacleSpawnProbability = 0.45f; // 45% chance to spawn at each marker ring

        [Header("Runtime Spawn Control")]
        public bool spawnCoins = true;
        public bool spawnObstacles = true;

        // Neon bloom levels: one shared additive halo material (colour comes from vertex colours)
        // and one shared ring-halo mesh per segment, since every marker ring has the same shape.
        private static Material neonHaloMaterial;
        private Mesh markerHaloMesh;

        private static Material NeonHaloMaterial()
        {
            if (neonHaloMaterial == null)
            {
                Material src = Resources.Load<Material>("Attract/Mat_NeonBandHalo");
                if (src == null) return null;
                neonHaloMaterial = new Material(src);
                neonHaloMaterial.name = "NeonHalo_Tunnel";
                // The ribbons lie flat on the wall or the arc face, so skip the limb boost the sphere needs.
                if (neonHaloMaterial.HasProperty("_Fresnel")) neonHaloMaterial.SetFloat("_Fresnel", 0f);
                if (neonHaloMaterial.HasProperty("_Intensity")) neonHaloMaterial.SetFloat("_Intensity", 0.9f);
            }
            return neonHaloMaterial;
        }

        private static Material arcBorderMaterial;

        /// <summary>
        /// Unlit material for arc outlines. Unlit on purpose: the outline has to hold the same
        /// brightness however the level is lit, since it is the one part of an arc that still
        /// reads once distance and bloom have flattened the body. Culling is off so the strip
        /// shows regardless of which way its triangles wound.
        /// </summary>
        private static Material ArcBorderMaterial()
        {
            if (arcBorderMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null) s = Shader.Find("Unlit/Color");
                if (s == null) s = Shader.Find("Sprites/Default");
                if (s == null) return null;

                arcBorderMaterial = new Material(s);
                arcBorderMaterial.name = "ArcBorder";
                if (arcBorderMaterial.HasProperty("_Cull")) arcBorderMaterial.SetFloat("_Cull", 0f);
            }
            return arcBorderMaterial;
        }

        private static bool IsNeonBloom(LevelConfig config)
        {
            return config != null && config.neonBloom;
        }

        private void OnDestroy()
        {
            if (markerHaloMesh != null) Destroy(markerHaloMesh);
        }

        /// <summary>Marker colour at unit brightness, from the marker material's emission.</summary>
        private Color MarkerHaloColour()
        {
            Color c = new Color(0.2f, 0.85f, 1f);
            if (markerMaterial != null)
            {
                if (markerMaterial.HasProperty("_EmissionColor")) c = markerMaterial.GetColor("_EmissionColor");
                else if (markerMaterial.HasProperty("_BaseColor")) c = markerMaterial.GetColor("_BaseColor");
            }
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (m > 0.0001f) { c.r /= m; c.g /= m; c.b /= m; }
            c.a = 1f;
            return c;
        }

        /// <summary>
        /// A wide ribbon lying on the wall under the thin marker line, shaded by the menu's
        /// NeonBandHalo gaussian so the ring reads as a neon tube in a soft cloud of its colour.
        /// </summary>
        private void AddMarkerHalo(GameObject ringObj)
        {
            Material mat = NeonHaloMaterial();
            if (mat == null) return;

            if (markerHaloMesh == null)
            {
                const float half = 0.45f;
                float r = radius - 0.03f;
                int n = radialSegments;
                Color col = MarkerHaloColour();

                Vector3[] verts = new Vector3[(n + 1) * 3];
                Vector2[] uvs = new Vector2[verts.Length];
                Color[] cols = new Color[verts.Length];
                for (int i = 0; i <= n; i++)
                {
                    float angle = (i * 2f * Mathf.PI) / n;
                    float x = Mathf.Sin(angle) * r;
                    float y = -Mathf.Cos(angle) * r;
                    int b = i * 3;
                    verts[b + 0] = new Vector3(x, y, -half);
                    verts[b + 1] = new Vector3(x, y, 0f);
                    verts[b + 2] = new Vector3(x, y, half);
                    uvs[b + 0] = new Vector2(0f, 0f);
                    uvs[b + 1] = new Vector2(0f, 0.5f);
                    uvs[b + 2] = new Vector2(0f, 1f);
                    cols[b + 0] = col; cols[b + 1] = col; cols[b + 2] = col;
                }

                int[] tris = new int[n * 12];
                int t = 0;
                for (int i = 0; i < n; i++)
                {
                    int a = i * 3;
                    int c = a + 3;
                    tris[t++] = a; tris[t++] = a + 1; tris[t++] = c + 1;
                    tris[t++] = a; tris[t++] = c + 1; tris[t++] = c;
                    tris[t++] = a + 1; tris[t++] = a + 2; tris[t++] = c + 2;
                    tris[t++] = a + 1; tris[t++] = c + 2; tris[t++] = c + 1;
                }

                markerHaloMesh = new Mesh();
                markerHaloMesh.name = "MarkerRingHalo";
                markerHaloMesh.vertices = verts;
                markerHaloMesh.uv = uvs;
                markerHaloMesh.colors = cols;
                markerHaloMesh.triangles = tris;
                markerHaloMesh.RecalculateBounds();
            }

            GameObject halo = new GameObject("MarkerRingHalo", typeof(MeshFilter), typeof(MeshRenderer));
            halo.transform.SetParent(ringObj.transform, false);
            halo.GetComponent<MeshFilter>().sharedMesh = markerHaloMesh;
            MeshRenderer mr = halo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        /// <summary>Small collectibles never need to cast or receive shadows.</summary>
        private static void SetNoShadow(MeshRenderer mr)
        {
            if (mr == null) return;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        /// <summary>True once the segment has built its mesh and contents for where it stands.
        /// Anything that moves the segment for show (SegmentFlyIn) waits for this, because
        /// Populate lays everything out from the segment's position at the time.</summary>
        public bool IsPopulated { get; private set; }

        private void Start()
        {
            Populate();
        }

        public void ResetSegment(float newZ)
        {
            transform.position = new Vector3(0f, 0f, newZ);
            Populate();
        }

        /// <summary>
        /// Builds everything in this segment under a seed derived from the level and the segment's
        /// position, so a level lays out identically every run. The global random state is put
        /// back afterwards so effects elsewhere keep their variety.
        /// </summary>
        private void Populate()
        {
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            bool seeded = config != null;
            Random.State previous = Random.state;
            if (seeded) Random.InitState(SegmentSeed(config.GetSeed(), transform.position.z));

            GenerateMesh();
            SpawnMarkers();
            SpawnFinishGate(config);
            SpawnCoins();
            SpawnObstacles();
            KeepPickupsOffArcs(config);
            SpawnCityFlyby();

            if (seeded) Random.state = previous;
            IsPopulated = true;
        }

        private int SegmentSeed(int levelSeed, float z)
        {
            int index = Mathf.RoundToInt(z / Mathf.Max(1f, length));
            unchecked
            {
                int h = levelSeed;
                h ^= index * 0x27d4eb2f;
                h *= 0x165667b1;
                h ^= (h >> 15);
                h += 0x7f4a7c15;
                return h;
            }
        }

        private static float FinishZ(LevelConfig config)
        {
            return (config != null && config.levelLength > 0f) ? config.levelLength : float.MaxValue;
        }

        private static Material finishMaterial;

        /// <summary>A stack of wide gold rings marking the end of the level, if it falls in this segment.</summary>
        private void SpawnFinishGate(LevelConfig config)
        {
            if (config == null || config.levelLength <= 0f || markerMaterial == null) return;
            float localZ = config.levelLength - transform.position.z;
            if (localZ < 0f || localZ >= length) return;

            if (finishMaterial == null)
            {
                finishMaterial = new Material(markerMaterial);
                finishMaterial.name = "FinishGateMaterial";
                Color gold = new Color(1f, 0.85f, 0.25f);
                if (finishMaterial.HasProperty("_BaseColor")) finishMaterial.SetColor("_BaseColor", gold);
                if (finishMaterial.HasProperty("_EmissionColor")) finishMaterial.SetColor("_EmissionColor", gold * 6f);
            }

            Vector3 curveOffset = config.GetCurveOffset(config.levelLength);
            float[] offsets = { -1.2f, -0.4f, 0.4f, 1.2f };
            for (int k = 0; k < offsets.Length; k++)
            {
                // Named MarkerRing_* so the marker cleanup on recycle removes it.
                GameObject ringObj = new GameObject("MarkerRing_Finish");
                ringObj.transform.SetParent(this.transform, false);
                ringObj.transform.localPosition = new Vector3(curveOffset.x, curveOffset.y, localZ + offsets[k]);

                LineRenderer lr = ringObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = radialSegments;
                lr.startWidth = 0.3f;
                lr.endWidth = 0.3f;
                lr.textureMode = LineTextureMode.Tile;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.sharedMaterial = finishMaterial;

                Vector3[] points = new Vector3[radialSegments];
                for (int i = 0; i < radialSegments; i++)
                {
                    float angle = (i * 2f * Mathf.PI) / radialSegments;
                    points[i] = new Vector3(Mathf.Sin(angle) * (radius - 0.05f), -Mathf.Cos(angle) * (radius - 0.05f), 0f);
                }
                lr.SetPositions(points);

                if (IsNeonBloom(config)) AddMarkerHalo(ringObj);
            }
        }

        private void SpawnCityFlyby()
        {
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            if (config != null && config.hasCityFlyby)
            {
                CityGenerator.GenerateCityForSegment(this.gameObject, length, radius, config);
            }
            else if (config != null && config.HasThemedEnvironment)
            {
                EnvironmentScenery.Decorate(this.gameObject, length, radius, config);
            }
        }

        private void GenerateMesh()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name == "InwardCylinder")
            {
                Destroy(filter.sharedMesh);
            }

            Mesh mesh = new Mesh();
            mesh.name = "InwardCylinder";

            int zSegments = 10; // Number of subdivisions along the length for curves
            int verticesPerLoop = radialSegments + 1;
            int totalVertices = verticesPerLoop * (zSegments + 1);

            Vector3[] vertices = new Vector3[totalVertices];
            Vector2[] uvs = new Vector2[totalVertices];
            Vector3[] normals = new Vector3[totalVertices];

            for (int zIndex = 0; zIndex <= zSegments; zIndex++)
            {
                float progress = (float)zIndex / zSegments;
                float z = progress * length;
                float absoluteZ = transform.position.z + z;
                
                Vector3 curveOffset = Vector3.zero;
                LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
                if (config != null)
                {
                    curveOffset = config.GetCurveOffset(absoluteZ);
                }

                for (int i = 0; i <= radialSegments; i++)
                {
                    float u = (float)i / radialSegments;
                    float angle = u * 2f * Mathf.PI;
                    
                    float x = Mathf.Sin(angle) * radius + curveOffset.x;
                    float y = -Mathf.Cos(angle) * radius + curveOffset.y;
                    
                    int index = i + zIndex * verticesPerLoop;
                    vertices[index] = new Vector3(x, y, z);
                    
                    float repeatV = length / (2f * Mathf.PI * radius);
                    uvs[index] = new Vector2(u, progress * repeatV);

                    // Normal points inward (towards the center of the cylinder at this loop)
                    normals[index] = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f);
                }
            }

            int triCount = radialSegments * zSegments * 6;
            int[] triangles = new int[triCount];
            int tIndex = 0;

            for (int zIndex = 0; zIndex < zSegments; zIndex++)
            {
                for (int i = 0; i < radialSegments; i++)
                {
                    int v00 = i + zIndex * verticesPerLoop;
                    int v10 = i + 1 + zIndex * verticesPerLoop;
                    int v01 = i + (zIndex + 1) * verticesPerLoop;
                    int v11 = i + 1 + (zIndex + 1) * verticesPerLoop;

                    triangles[tIndex++] = v00;
                    triangles[tIndex++] = v01;
                    triangles[tIndex++] = v10;

                    triangles[tIndex++] = v10;
                    triangles[tIndex++] = v01;
                    triangles[tIndex++] = v11;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;

            // Recalculate bounds so culling works correctly
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = mesh;
            if (tunnelMaterial != null)
            {
                MeshRenderer tubeRenderer = GetComponent<MeshRenderer>();
                tubeRenderer.sharedMaterial = tunnelMaterial;
                // The tube wraps the camera, so anything it cast would land on the player rather
                // than on the world, and it is transparent in themed levels anyway.
                tubeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tubeRenderer.receiveShadows = false;
            }
        }

        private void SpawnMarkers()
        {
            // 1. Destroy any existing marker ring children (crucial for recycling)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("MarkerRing"))
                {
                    Destroy(child.gameObject);
                }
            }

            // 2. Spawn rings along the inside of the tube at regular Z coordinates.
            // Composed levels widen the interval with speed, so the first ring is found from the
            // absolute Z grid rather than assumed to sit one interval into the segment - an
            // interval that does not divide segmentLength would otherwise leave uneven seams.
            LevelConfig markerConfig = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            float interval = MarkerSpacing(markerConfig);
            float segmentZ = transform.position.z;

            // A composed level places its rings itself - one under every arc, plus fillers across
            // empty stretches - so the positions are read from it rather than stepped off a grid.
            // The list is held for the rest of Populate, which snaps pickups onto these same rings.
            if (composedMarkers == null) composedMarkers = new System.Collections.Generic.List<float>();
            composedMarkers.Clear();
            bool composedRings = markerConfig != null && markerConfig.HasComposer;
            if (composedRings) markerConfig.composer.MarkersInRange(segmentZ, segmentZ + length, composedMarkers);

            int markerIndex = 0;
            float currentZ;
            if (composedRings)
            {
                if (composedMarkers.Count == 0) return;
                currentZ = composedMarkers[0] - segmentZ;
            }
            else
            {
                currentZ = Mathf.Ceil((segmentZ + 0.001f) / interval) * interval - segmentZ;
                if (currentZ <= 0.001f) currentZ = interval;
            }

            while (currentZ < length)
            {
                float absoluteZ = segmentZ + currentZ;
                Vector3 curveOffset = Vector3.zero;
                LevelConfig config = markerConfig;
                if (config != null)
                {
                    curveOffset = config.GetCurveOffset(absoluteZ);
                }

                GameObject ringObj = new GameObject("MarkerRing");
                ringObj.transform.SetParent(this.transform, false);
                ringObj.transform.localPosition = new Vector3(curveOffset.x, curveOffset.y, currentZ);

                LineRenderer lineRenderer = ringObj.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false;
                lineRenderer.loop = true;
                lineRenderer.positionCount = radialSegments;
                lineRenderer.startWidth = ringWidth;
                lineRenderer.endWidth = ringWidth;

                // Configure standard properties to match modern look
                lineRenderer.textureMode = LineTextureMode.Tile;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;

                if (markerMaterial != null)
                {
                    lineRenderer.sharedMaterial = markerMaterial;
                }

                Vector3[] points = new Vector3[radialSegments];
                for (int i = 0; i < radialSegments; i++)
                {
                    float angle = (i * 2f * Mathf.PI) / radialSegments;
                    // Position rings slightly inside the cylinder so they don't clip with the wall mesh
                    float x = Mathf.Sin(angle) * (radius - 0.02f);
                    float y = -Mathf.Cos(angle) * (radius - 0.02f);
                    points[i] = new Vector3(x, y, 0f);
                }

                lineRenderer.SetPositions(points);

                if (IsNeonBloom(config))
                {
                    lineRenderer.startWidth = ringWidth * 1.4f;
                    lineRenderer.endWidth = ringWidth * 1.4f;
                    AddMarkerHalo(ringObj);
                }

                if (composedRings)
                {
                    markerIndex++;
                    if (markerIndex >= composedMarkers.Count) break;
                    currentZ = composedMarkers[markerIndex] - segmentZ;
                }
                else
                {
                    currentZ += interval;
                }
            }
        }

        /// <summary>Marker-ring positions inside this segment, absolute Z. Empty on a classic level.</summary>
        private System.Collections.Generic.List<float> composedMarkers;

        /// <summary>
        /// Snaps a pickup onto the nearest marker ring, so coins and powerups sit on the same beat
        /// as the arcs instead of floating between rings. Only this segment's own rings are
        /// considered - snapping to one in the next segment would place the pickup outside the
        /// object it is parented to. Returns the input unchanged if there is nothing to snap to.
        /// </summary>
        private float SnapToMarker(LevelConfig config, float localZ)
        {
            if (config == null || !config.HasComposer) return localZ;
            if (composedMarkers == null || composedMarkers.Count == 0) return localZ;

            float segmentZ = transform.position.z;
            float best = localZ;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < composedMarkers.Count; i++)
            {
                float candidate = composedMarkers[i] - segmentZ;
                if (candidate < 0.5f || candidate > length - 0.5f) continue;

                float distance = Mathf.Abs(candidate - localZ);
                if (distance < bestDistance) { bestDistance = distance; best = candidate; }
            }
            return best;
        }

        // ---- Keeping pickups off the arcs ---------------------------------------------------------
        // Pickups snap to marker rings to sit on the beat, and a composed level draws a marker ring
        // under every arc - so a pickup at a random angle regularly landed inside one. Once a
        // segment has built both, every pickup is checked against the arcs near it: moved to the
        // nearest clear angle, or (a powerup) to another ring in the segment, or dropped.

        /// <summary>Half a pickup's footprint around the wall, with a little air, in degrees.</summary>
        private const float PickupHalfAngleDeg = 8f;
        /// <summary>Half a pickup's footprint along the tube, with a little air.</summary>
        private const float PickupHalfDepth = 0.9f;

        private struct ArcBand
        {
            public float z, halfDepth, startDeg, spanDeg;
            public bool moving;     // spins, snaps, swings or chases: sweeps its whole band
        }

        private readonly System.Collections.Generic.List<ArcBand> arcBands = new System.Collections.Generic.List<ArcBand>();
        private System.Collections.Generic.List<Progression.RingSpec> bandScratch;

        private void KeepPickupsOffArcs(LevelConfig config)
        {
            Collectible[] pickups = GetComponentsInChildren<Collectible>();
            if (pickups.Length == 0) return;

            GatherArcBands(config);
            if (arcBands.Count == 0) return;

            float segmentZ = transform.position.z;
            foreach (Collectible pickup in pickups)
            {
                Vector3 p = pickup.transform.localPosition;
                float absoluteZ = segmentZ + p.z;
                Vector3 curve = config != null ? config.GetCurveOffset(absoluteZ) : Vector3.zero;
                Vector2 radial = new Vector2(p.x - curve.x, p.y - curve.y);
                // Positions are laid out as (sin a, -cos a) * r, the same angle space as the arcs.
                float angle = Mathf.Atan2(radial.x, -radial.y) * Mathf.Rad2Deg;
                float r = radial.magnitude;

                if (!ArcBlocks(absoluteZ, angle)) continue;

                float clear;
                if (NearestClearAngle(absoluteZ, angle, out clear))
                {
                    PlacePickup(pickup, p.z, clear, r, config);
                    continue;
                }

                // Nothing clear at this depth (a moving ring, a full ring). A powerup is worth
                // relocating to another ring in the segment; a coin is just one of a trail.
                if (pickup.type != CollectibleType.Coin)
                {
                    float z2, a2;
                    if (FindClearRing(config, p.z, angle, out z2, out a2))
                    {
                        PlacePickup(pickup, z2, a2, r, config);
                        continue;
                    }
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.UnregisterCoinSpawned();
                }
                Destroy(pickup.gameObject);
            }
        }

        /// <summary>Every arc that could reach a pickup in this segment - from the composer for a
        /// composed level (so a neighbouring segment's rings count too), else this segment's own.</summary>
        private void GatherArcBands(LevelConfig config)
        {
            arcBands.Clear();
            float segmentZ = transform.position.z;

            if (config != null && config.HasComposer)
            {
                if (bandScratch == null) bandScratch = new System.Collections.Generic.List<Progression.RingSpec>();
                config.composer.RingsInRange(segmentZ - 6f, segmentZ + length + 6f, bandScratch);
                foreach (Progression.RingSpec ring in bandScratch)
                {
                    bool moving = ring.motion != RingArcGroup.Motion.Static;
                    foreach (Progression.ArcSpec arc in ring.arcs)
                    {
                        arcBands.Add(new ArcBand { z = ring.z, halfDepth = arc.depth * 0.5f,
                                                   startDeg = arc.startAngleDeg, spanDeg = arc.arcAngleDeg,
                                                   moving = moving });
                    }
                }
                return;
            }

            // Classic spawner: arcs never sit within a few units of a segment edge, so this
            // segment's own are the only ones in reach.
            foreach (Obstacle obs in GetComponentsInChildren<Obstacle>())
            {
                RingArcGroup group = obs.GetComponentInParent<RingArcGroup>();
                float start = obs.transform.localEulerAngles.z + (group != null ? group.transform.localEulerAngles.z : 0f);
                arcBands.Add(new ArcBand { z = obs.transform.position.z, halfDepth = obs.depth * 0.5f,
                                           startDeg = start, spanDeg = obs.arcAngle,
                                           moving = group != null && group.motion != RingArcGroup.Motion.Static });
            }
        }

        private bool ArcBlocks(float z, float angleDeg)
        {
            for (int i = 0; i < arcBands.Count; i++)
            {
                ArcBand band = arcBands[i];
                if (Mathf.Abs(z - band.z) >= band.halfDepth + PickupHalfDepth) continue;
                if (band.moving) return true;
                float from = band.startDeg - PickupHalfAngleDeg;
                if (Mathf.Repeat(angleDeg - from, 360f) <= band.spanDeg + 2f * PickupHalfAngleDeg) return true;
            }
            return false;
        }

        private bool NearestClearAngle(float z, float angleDeg, out float clear)
        {
            for (int k = 2; k <= 180; k += 2)
            {
                if (!ArcBlocks(z, angleDeg + k)) { clear = angleDeg + k; return true; }
                if (!ArcBlocks(z, angleDeg - k)) { clear = angleDeg - k; return true; }
            }
            clear = angleDeg;
            return false;
        }

        /// <summary>The nearest marker ring in this segment with a clear angle, for a powerup whose
        /// own ring had none.</summary>
        private bool FindClearRing(LevelConfig config, float localZ, float angleDeg, out float bestZ, out float bestAngle)
        {
            bestZ = localZ;
            bestAngle = angleDeg;
            float bestDistance = float.MaxValue;
            float segmentZ = transform.position.z;

            System.Collections.Generic.List<float> candidates = new System.Collections.Generic.List<float>();
            if (config != null && config.HasComposer && composedMarkers != null)
            {
                foreach (float m in composedMarkers) candidates.Add(m - segmentZ);
            }
            else
            {
                float spacing = MarkerSpacing(config);
                for (float z = spacing; z < length; z += spacing) candidates.Add(z);
            }

            foreach (float candidate in candidates)
            {
                if (candidate < 0.5f || candidate > length - 0.5f) continue;
                float distance = Mathf.Abs(candidate - localZ);
                if (distance >= bestDistance) continue;

                float clear = angleDeg;
                if (ArcBlocks(segmentZ + candidate, angleDeg) && !NearestClearAngle(segmentZ + candidate, angleDeg, out clear))
                    continue;
                bestDistance = distance;
                bestZ = candidate;
                bestAngle = clear;
            }
            return bestDistance < float.MaxValue;
        }

        private void PlacePickup(Collectible pickup, float localZ, float angleDeg, float r, LevelConfig config)
        {
            Vector3 curve = config != null ? config.GetCurveOffset(transform.position.z + localZ) : Vector3.zero;
            float a = angleDeg * Mathf.Deg2Rad;
            pickup.transform.localPosition = new Vector3(Mathf.Sin(a) * r + curve.x, -Mathf.Cos(a) * r + curve.y, localZ);

            // Coins face down the tube, standing on the wall where they now are.
            if (pickup.type == CollectibleType.Coin)
                pickup.transform.localRotation = Quaternion.LookRotation(Vector3.forward, pickup.transform.localPosition.normalized);
        }

        /// <summary>Marker spacing for this level: a composer's own interval, else the classic one.</summary>
        private float MarkerSpacing(LevelConfig config)
        {
            float spacing = (config != null) ? config.MarkerIntervalOr(markerInterval) : markerInterval;
            return Mathf.Max(1f, spacing);
        }

        private void SpawnCoins()
        {
            // 1. Destroy any existing uncollected coin children (crucial for recycling)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Coin") || child.GetComponent<Collectible>() != null)
                {
                    Destroy(child.gameObject);
                }
            }

            // Nothing to collect in the segment that sits behind the start line.
            if (transform.position.z < 0f) return;

            // 2. 40% probability to spawn a line of coins in each tunnel segment
            if (spawnCoins && Random.value < 0.4f)
            {
                LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
                float finishZ = FinishZ(config);
                bool powerupsAllowed = transform.position.z + length * 0.5f < finishZ - 5f;

                // Powerup chances (the rolls always happen so the seeded sequence stays aligned)
                float powerupRoll = Random.value;
                if (powerupsAllowed && config != null && config.spawnAddSpherePowerup && powerupRoll < 0.15f)
                {
                    SpawnAddSpherePowerup();
                    return;
                }
                else if (powerupsAllowed && powerupMaterial != null && powerupRoll > 0.15f && powerupRoll < 0.25f)
                {
                    SpawnSinglePowerup();
                    return;
                }
                else if (powerupsAllowed && magnetMaterial != null && powerupRoll > 0.25f && powerupRoll < 0.35f)
                {
                    SpawnSingleMagnet();
                    return;
                }

                int count = Random.Range(3, 6); // 3 to 5 coins in a row
                float angle = Random.Range(0f, 2f * Mathf.PI); // Random angle around the cylinder track
                float spacing = length / (count + 1);

                // Choose a random color index from the available sphere colors
                int colorIndex = 0;
                if (coinMaterials != null && coinMaterials.Length > 0)
                {
                    colorIndex = Random.Range(0, coinMaterials.Length);
                }

                // Place coins slightly inward from the tube radius so they hover right above the track surface
                float spawnRadius = radius - 0.35f;

                // The trail starts on a ring so it reads as being on the same beat as the arcs;
                // the coins keep their own close spacing rather than being spread ring to ring,
                // which would turn a pickup trail into a scattering.
                float trailStart = SnapToMarker(config, spacing);

                for (int i = 0; i < count; i++)
                {
                    float localZ = trailStart + spacing * i;
                    if (localZ >= length) break;
                    float absoluteZ = transform.position.z + localZ;
                    if (absoluteZ > finishZ - 5f) continue;   // nothing past the gate
                    Vector3 curveOffset = Vector3.zero;
                    if (config != null)
                    {
                        curveOffset = config.GetCurveOffset(absoluteZ);
                    }

                    // Create a 3D thin disc coin shape using a squashed cylinder primitive
                    GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    coin.name = "Coin_" + i;
                    coin.transform.SetParent(this.transform, false);

                    // Position inside cylindrical space relative to the curved center
                    float x = Mathf.Sin(angle) * spawnRadius + curveOffset.x;
                    float y = -Mathf.Cos(angle) * spawnRadius + curveOffset.y;
                    coin.transform.localPosition = new Vector3(x, y, localZ);

                    // Flatten cylinder into a circular coin disc
                    coin.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);

                    // Rotate so flat faces point along the tube (looking at the player as they fall)
                    Vector3 radialDir = coin.transform.localPosition.normalized;
                    coin.transform.localRotation = Quaternion.LookRotation(Vector3.forward, radialDir);

                    // Replace thin CapsuleCollider with a larger SphereCollider to prevent physics tunneling at high speeds
                    Collider oldCol = coin.GetComponent<Collider>();
                    if (oldCol != null)
                    {
                        DestroyImmediate(oldCol);
                    }

                    SphereCollider sphereCol = coin.AddComponent<SphereCollider>();
                    sphereCol.isTrigger = true;
                    sphereCol.radius = 1.3f; // Generous trigger envelope

                    // Assign color-matching emissive material
                    MeshRenderer mr = coin.GetComponent<MeshRenderer>();
                    if (mr != null && coinMaterials != null && coinMaterials.Length > 0)
                    {
                        mr.sharedMaterial = coinMaterials[colorIndex];
                    }
                    SetNoShadow(mr);

                    // Attach Collectible behavior
                    Collectible collectible = coin.AddComponent<Collectible>();
                    collectible.type = CollectibleType.Coin;
                    collectible.colorIndex = colorIndex; // Only collectable by matching player sphere
                    collectible.rotationSpeed = 160f;
                    collectible.hoverAmplitude = 0.08f;
                    collectible.hoverSpeed = 3.5f;

                    if (GameManager.Instance != null) GameManager.Instance.RegisterCoinSpawned();
                }
            }
        }

        private void SpawnSinglePowerup()
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float localZ = SnapToMarker(
                (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null,
                length * 0.5f);
            float absoluteZ = transform.position.z + localZ;
            Vector3 curveOffset = Vector3.zero;
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            if (config != null)
            {
                curveOffset = config.GetCurveOffset(absoluteZ);
            }

            float spawnRadius = radius - 0.35f;

            GameObject powerup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            powerup.name = "Powerup";
            powerup.transform.SetParent(this.transform, false);

            float x = Mathf.Sin(angle) * spawnRadius + curveOffset.x;
            float y = -Mathf.Cos(angle) * spawnRadius + curveOffset.y;
            powerup.transform.localPosition = new Vector3(x, y, localZ);
            powerup.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            Collider oldCol = powerup.GetComponent<Collider>();
            if (oldCol != null)
            {
                DestroyImmediate(oldCol);
            }

            SphereCollider sphereCol = powerup.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 1.3f;

            MeshRenderer mr = powerup.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = powerupMaterial;
            }
            SetNoShadow(mr);

            Collectible collectible = powerup.AddComponent<Collectible>();
            collectible.type = CollectibleType.Powerup;
            collectible.colorIndex = -1; // Any color can collect
            collectible.rotationSpeed = 250f;
            collectible.hoverAmplitude = 0.15f;
            collectible.hoverSpeed = 5f;
        }

        private void SpawnSingleMagnet()
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float localZ = SnapToMarker(
                (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null,
                length * 0.5f);
            float absoluteZ = transform.position.z + localZ;
            Vector3 curveOffset = Vector3.zero;
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            if (config != null)
            {
                curveOffset = config.GetCurveOffset(absoluteZ);
            }

            float spawnRadius = radius - 0.35f;

            GameObject magnet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            magnet.name = "MagnetPowerup";
            magnet.transform.SetParent(this.transform, false);

            float x = Mathf.Sin(angle) * spawnRadius + curveOffset.x;
            float y = -Mathf.Cos(angle) * spawnRadius + curveOffset.y;
            magnet.transform.localPosition = new Vector3(x, y, localZ);
            magnet.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            Collider oldCol = magnet.GetComponent<Collider>();
            if (oldCol != null)
            {
                DestroyImmediate(oldCol);
            }

            SphereCollider sphereCol = magnet.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 1.3f;

            MeshRenderer mr = magnet.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = magnetMaterial;
            }
            SetNoShadow(mr);

            Collectible collectible = magnet.AddComponent<Collectible>();
            collectible.type = CollectibleType.MagnetPowerup;
            collectible.colorIndex = -1; // Any color can collect
            collectible.rotationSpeed = 250f;
            collectible.hoverAmplitude = 0.15f;
            collectible.hoverSpeed = 5f;
        }

        private void SpawnAddSpherePowerup()
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float localZ = SnapToMarker(
                (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null,
                length * 0.5f);
            float absoluteZ = transform.position.z + localZ;
            Vector3 curveOffset = Vector3.zero;
            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            if (config != null)
            {
                curveOffset = config.GetCurveOffset(absoluteZ);
            }

            float spawnRadius = radius - 0.35f;

            GameObject powerup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            powerup.name = "AddSpherePowerup";
            powerup.transform.SetParent(this.transform, false);

            float x = Mathf.Sin(angle) * spawnRadius + curveOffset.x;
            float y = -Mathf.Cos(angle) * spawnRadius + curveOffset.y;
            powerup.transform.localPosition = new Vector3(x, y, localZ);
            powerup.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            Collider oldCol = powerup.GetComponent<Collider>();
            if (oldCol != null)
            {
                DestroyImmediate(oldCol);
            }

            BoxCollider boxCol = powerup.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(1.5f, 1.5f, 1.5f);

            MeshRenderer mr = powerup.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = powerupMaterial; // Reusing powerup material for now
            }
            SetNoShadow(mr);

            Collectible collectible = powerup.AddComponent<Collectible>();
            collectible.type = CollectibleType.AddSpherePowerup;
            collectible.colorIndex = -1; // Any color can collect
            collectible.rotationSpeed = 250f;
            collectible.hoverAmplitude = 0.15f;
            collectible.hoverSpeed = 5f;
        }

        private void SpawnObstacles()
        {
            // 1. Destroy any existing obstacle child objects (for pool recycling cleanup)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Obstacle") || child.GetComponent<Obstacle>() != null)
                {
                    Destroy(child.gameObject);
                }
            }

            if (!spawnObstacles) return;

            LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            RingDifficulty rings = (config != null && config.rings != null) ? config.rings : RingDifficulty.Classic();

            // Composed levels (Progression Test 1, endless modes) already know exactly which rings
            // belong where - the composer solved their spacing from what each move costs - so this
            // segment just builds the ones that fall inside it.
            if (config != null && config.HasComposer)
            {
                SpawnComposedRings(config);
                return;
            }

            // Keep the run-in clear so the player has time to read the first rings (attraction mode has no config).
            float clearDistance = (config != null) ? config.GetStartClearDistance() : 0f;

            // 2. Loop along the segment at marker intervals and spawn a ring arc group centered on each marker ring
            float currentZ = markerInterval;
            while (currentZ < length)
            {
                float absoluteZ = transform.position.z + currentZ;
                bool inRunIn = absoluteZ < clearDistance;
                bool inRunOut = absoluteZ > FinishZ(config) - 25f;   // a clean run up to the gate
                if (!inRunIn && !inRunOut && Random.value < obstacleSpawnProbability)
                {
                    Vector3 curveOffset = Vector3.zero;
                    if (config != null)
                    {
                        curveOffset = config.GetCurveOffset(absoluteZ);
                    }

                    SpawnRingArcGroup(rings, new Vector3(curveOffset.x, curveOffset.y, currentZ), absoluteZ);
                }
                currentZ += markerInterval;
            }
        }

        /// <summary>
        /// Builds every composed ring whose Z falls inside this segment. Unlike the classic
        /// spawner nothing is rolled here: arc angles, colours and motion all come straight
        /// from the RingSpec the composer produced before the level started.
        /// </summary>
        private void SpawnComposedRings(LevelConfig config)
        {
            if (composedScratch == null) composedScratch = new System.Collections.Generic.List<Progression.RingSpec>();

            float segmentZ = transform.position.z;
            config.composer.RingsInRange(segmentZ, segmentZ + length, composedScratch);
            if (composedScratch.Count == 0) return;

            bool canColorCode = transparentObstacleMaterials != null && transparentObstacleMaterials.Length > 0;
            Material arcHalo = IsNeonBloom(config) ? NeonHaloMaterial() : null;
            Material arcBorder = ArcBorderMaterial();

            for (int r = 0; r < composedScratch.Count; r++)
            {
                Progression.RingSpec spec = composedScratch[r];
                float localZ = spec.z - segmentZ;
                Vector3 curveOffset = config.GetCurveOffset(spec.z);

                GameObject groupObj = new GameObject("ObstacleRing");
                groupObj.transform.SetParent(this.transform, false);
                groupObj.transform.localPosition = new Vector3(curveOffset.x, curveOffset.y, localZ);
                groupObj.transform.localRotation = Quaternion.identity;

                for (int a = 0; a < spec.arcs.Length; a++)
                {
                    Progression.ArcSpec arc = spec.arcs[a];

                    GameObject obsObj = new GameObject("Obstacle");
                    obsObj.transform.SetParent(groupObj.transform, false);
                    obsObj.transform.localRotation = Quaternion.Euler(0f, 0f, arc.startAngleDeg);

                    Obstacle obs = obsObj.AddComponent<Obstacle>();
                    obs.radius = radius;
                    obs.thickness = arc.thickness;
                    obs.depth = arc.depth;
                    obs.arcAngle = arc.arcAngleDeg;
                    obs.haloMaterial = arcHalo;
                    // Outline every arc, and alternate the body colour of the solid ones, so a run
                    // of rings reads as separate objects receding rather than one flat wash.
                    obs.borderMaterial = arcBorder;
                    // Wide arcs need proportionally more segments or they read as polygons and
                    // their compound triggers get coarse enough to let a sphere slip through.
                    obs.radialSegments = Mathf.Clamp(Mathf.CeilToInt(arc.arcAngleDeg / 10f), 6, 48);

                    bool colour = arc.isColourCoded && canColorCode;
                    if (colour)
                    {
                        int idx = Mathf.Clamp(arc.colourIndex, 0, transparentObstacleMaterials.Length - 1);
                        obs.isColorCoded = true;
                        obs.targetColorIndex = idx;
                        obs.obstacleMaterial = transparentObstacleMaterials[idx];
                        if (GameManager.Instance != null) GameManager.Instance.RegisterShieldSpawned();

                        if (arc.colourShift && transparentObstacleMaterials.Length > 1)
                        {
                            obs.colorShiftMaterials = transparentObstacleMaterials;
                            obs.colorShiftInterval = Mathf.Max(0.3f, arc.colourShiftInterval);
                            obs.colorShiftSeed = arc.colourShiftSeed;
                        }
                    }
                    else
                    {
                        obs.isColorCoded = false;
                        obs.targetColorIndex = -1;
                        obs.obstacleMaterial = obstacleMaterial;
                        obs.tintColor = spec.solidTint;
                    }
                }

                RingArcGroup group = groupObj.AddComponent<RingArcGroup>();
                group.motion = spec.motion;
                group.spinDegPerSec = spec.spinDegPerSec;
                group.snapInterval = Mathf.Max(0.4f, spec.snapInterval);
                group.snapAngle = spec.snapAngle;
                group.snapTweenDuration = Mathf.Clamp(spec.snapTurnDuration, 0.05f, 1f);
                group.telegraphWindow = Mathf.Clamp(spec.telegraphWindow, 0f, group.snapInterval * 0.8f);
                group.oscillateAmplitudeDeg = spec.oscAmplitudeDeg;
                group.oscillatePeriod = Mathf.Max(0.3f, spec.oscPeriod);
                group.oscillatePhase = spec.oscPhase;
                group.chaseDegPerSec = spec.chaseDegPerSec;
                group.chaseOffsetDeg = spec.chaseOffsetDeg;
                group.chaseEngageDistance = spec.chaseEngageDistance;
            }
        }

        private System.Collections.Generic.List<Progression.RingSpec> composedScratch;

        /// <summary>
        /// Spawns one "ring arc group": 1..N toroid arcs sharing a marker ring, parented to a
        /// RingArcGroup that may spin, snap or swing them as a unit. Arcs are laid out in equal
        /// angular slots with jitter, always keeping at least rings.minGapDegrees between them.
        /// </summary>
        private void SpawnRingArcGroup(RingDifficulty rings, Vector3 localPosition, float absoluteZ)
        {
            GameObject groupObj = new GameObject("ObstacleRing");
            groupObj.transform.SetParent(this.transform, false);
            groupObj.transform.localPosition = localPosition;
            groupObj.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            int minArcs = Mathf.Clamp(rings.minArcsPerRing, 1, 4);
            int maxArcs = Mathf.Clamp(rings.maxArcsPerRing, minArcs, 4);
            int arcCount = Random.Range(minArcs, maxArcs + 1);

            float slot = 360f / arcCount;
            float minGap = Mathf.Clamp(rings.minGapDegrees, 0f, slot - 20f);
            float maxArc = slot - minGap;

            float minHeight = Mathf.Max(0.4f, radius * Mathf.Clamp01(rings.minHeightFraction));
            float maxHeight = Mathf.Max(minHeight, radius * Mathf.Clamp(rings.maxHeightFraction, 0f, 0.8f));
            float depth = Random.Range(Mathf.Max(0.1f, rings.minDepth), Mathf.Max(rings.minDepth, rings.maxDepth));

            bool canColorCode = transparentObstacleMaterials != null && transparentObstacleMaterials.Length > 0;
            LevelConfig levelConfig = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
            Material arcHalo = IsNeonBloom(levelConfig) ? NeonHaloMaterial() : null;
            float[] arcChoices = (rings.arcAngleChoices != null && rings.arcAngleChoices.Length > 0)
                ? rings.arcAngleChoices : new float[] { 60f, 90f, 120f };

            for (int i = 0; i < arcCount; i++)
            {
                float arcAngle = Mathf.Clamp(arcChoices[Random.Range(0, arcChoices.Length)], 20f, maxArc);
                float jitter = Random.Range(0f, Mathf.Max(0f, slot - arcAngle - minGap));
                float startAngle = i * slot + jitter;

                GameObject obsObj = new GameObject("Obstacle");
                obsObj.transform.SetParent(groupObj.transform, false);
                obsObj.transform.localRotation = Quaternion.Euler(0f, 0f, startAngle);

                Obstacle obs = obsObj.AddComponent<Obstacle>();
                obs.radius = radius;
                obs.thickness = Random.Range(minHeight, maxHeight);
                obs.depth = depth;
                obs.arcAngle = arcAngle;
                obs.haloMaterial = arcHalo;

                bool isColorCoded = canColorCode && Random.value >= rings.solidChance;
                if (isColorCoded)
                {
                    int colorIdx = Random.Range(0, transparentObstacleMaterials.Length);
                    obs.isColorCoded = true;
                    obs.targetColorIndex = colorIdx;
                    obs.obstacleMaterial = transparentObstacleMaterials[colorIdx];
                    if (GameManager.Instance != null) GameManager.Instance.RegisterShieldSpawned();

                    if (transparentObstacleMaterials.Length > 1 && Random.value < rings.colorShiftChance)
                    {
                        obs.colorShiftMaterials = transparentObstacleMaterials;
                        obs.colorShiftInterval = Mathf.Max(0.3f, rings.colorShiftInterval);
                        obs.colorShiftSeed = Random.Range(int.MinValue, int.MaxValue);
                    }
                }
                else
                {
                    obs.isColorCoded = false;
                    obs.targetColorIndex = -1;
                    obs.obstacleMaterial = obstacleMaterial;
                }
            }

            // Ring motion: at most one behaviour per ring, chosen by cumulative chance.
            RingArcGroup group = groupObj.AddComponent<RingArcGroup>();
            float roll = Random.value;
            float spinEdge = rings.spinChance;
            float snapEdge = spinEdge + rings.snapChance;
            float oscEdge = snapEdge + rings.oscillateChance;

            if (roll < spinEdge)
            {
                group.motion = RingArcGroup.Motion.Spin;
                float rate = Random.Range(rings.minSpinDegPerSec, Mathf.Max(rings.minSpinDegPerSec, rings.maxSpinDegPerSec));
                group.spinDegPerSec = (Random.value < 0.5f) ? -rate : rate;
            }
            else if (roll < snapEdge)
            {
                group.motion = RingArcGroup.Motion.Snap;
                group.snapInterval = Mathf.Max(0.4f, rings.snapInterval);
                group.snapTweenDuration = Mathf.Clamp(rings.snapTurnDuration, 0.05f, 1f);
                group.telegraphWindow = Mathf.Clamp(rings.snapTelegraphWindow, 0f, group.snapInterval * 0.8f);
                float snap = (Random.value < rings.snap180Chance) ? 180f : 90f;
                group.snapAngle = (Random.value < 0.5f) ? -snap : snap;
            }
            else if (roll < oscEdge)
            {
                group.motion = RingArcGroup.Motion.Oscillate;
                group.oscillateAmplitudeDeg = rings.oscillateAmplitudeDeg;
                group.oscillatePeriod = Mathf.Max(0.3f, rings.oscillatePeriod);
                group.oscillatePhase = Random.Range(0f, 2f * Mathf.PI);
            }
            else
            {
                group.motion = RingArcGroup.Motion.Static;
            }
        }
    }
}
