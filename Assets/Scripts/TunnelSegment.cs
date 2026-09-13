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
            SpawnCityFlyby();

            if (seeded) Random.state = previous;
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
            else if (config != null && config.environment != EnvironmentTheme.None)
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
                GetComponent<MeshRenderer>().sharedMaterial = tunnelMaterial;
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

            // 2. Spawn rings along the inside of the tube at regular Z coordinates
            float currentZ = markerInterval;
            while (currentZ < length)
            {
                float absoluteZ = transform.position.z + currentZ;
                Vector3 curveOffset = Vector3.zero;
                LevelConfig config = (GameManager.Instance != null) ? GameManager.Instance.currentLevelConfig : null;
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

                currentZ += markerInterval;
            }
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

                for (int i = 0; i < count; i++)
                {
                    float localZ = spacing * (i + 1);
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
            float localZ = length * 0.5f; // Spawn in the middle of the segment
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
            float localZ = length * 0.5f; // Spawn in the middle of the segment
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
            float localZ = length * 0.5f; // Spawn in the middle of the segment
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
