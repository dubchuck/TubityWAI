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

        [Header("Obstacle Settings")]
        public Material obstacleMaterial; // standard solid red
        public Material[] transparentObstacleMaterials; // passable color-coded shields
        public float obstacleSpawnProbability = 0.45f; // 45% chance to spawn at each marker ring

        private void Start()
        {
            GenerateMesh();
            SpawnMarkers();
            SpawnCoins();
            SpawnObstacles();
        }

        public void ResetSegment(float newZ)
        {
            transform.position = new Vector3(0f, 0f, newZ);
            SpawnCoins();
            SpawnObstacles();
        }

        private void GenerateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "InwardCylinder";

            // Radial segments + 1 to close the loop properly with UV mapping
            int verticesPerLoop = radialSegments + 1;
            int totalVertices = verticesPerLoop * 2; // Loop at Z = 0 and Loop at Z = length

            Vector3[] vertices = new Vector3[totalVertices];
            Vector2[] uvs = new Vector2[totalVertices];
            Vector3[] normals = new Vector3[totalVertices];

            // Loop 0 (z = 0) and Loop 1 (z = length)
            for (int zIndex = 0; zIndex <= 1; zIndex++)
            {
                float z = zIndex * length;
                for (int i = 0; i <= radialSegments; i++)
                {
                    float u = (float)i / radialSegments;
                    float angle = u * 2f * Mathf.PI;
                    
                    float x = Mathf.Sin(angle) * radius;
                    float y = -Mathf.Cos(angle) * radius; // Match bottom start position
                    
                    int index = i + zIndex * verticesPerLoop;
                    vertices[index] = new Vector3(x, y, z);
                    
                    // Aspect ratio-corrected UV wrapping
                    float repeatV = length / (2f * Mathf.PI * radius);
                    uvs[index] = new Vector2(u, zIndex * repeatV);

                    // Normal points inward (towards center of the tube)
                    normals[index] = new Vector3(-x, -y, 0f).normalized;
                }
            }

            int triCount = radialSegments * 6;
            int[] triangles = new int[triCount];
            int tIndex = 0;

            for (int i = 0; i < radialSegments; i++)
            {
                int v00 = i;
                int v10 = i + 1;
                int v01 = i + verticesPerLoop;
                int v11 = i + 1 + verticesPerLoop;

                // Triangle 1 (Inward-facing, clockwise when viewed from inside)
                triangles[tIndex++] = v00;
                triangles[tIndex++] = v01;
                triangles[tIndex++] = v10;

                // Triangle 2 (Inward-facing, clockwise when viewed from inside)
                triangles[tIndex++] = v10;
                triangles[tIndex++] = v01;
                triangles[tIndex++] = v11;
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
            // Spawn rings along the inside of the tube at regular Z coordinates
            float currentZ = markerInterval;
            while (currentZ < length)
            {
                GameObject ringObj = new GameObject("MarkerRing");
                ringObj.transform.SetParent(this.transform, false);
                ringObj.transform.localPosition = new Vector3(0f, 0f, currentZ);

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

            // 2. 40% probability to spawn a line of coins in each tunnel segment
            if (Random.value < 0.4f)
            {
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

                    // Create a 3D thin disc coin shape using a squashed cylinder primitive
                    GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    coin.name = "Coin_" + i;
                    coin.transform.SetParent(this.transform, false);

                    // Position inside cylindrical space
                    float x = Mathf.Sin(angle) * spawnRadius;
                    float y = -Mathf.Cos(angle) * spawnRadius;
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

                    // Attach Collectible behavior
                    Collectible collectible = coin.AddComponent<Collectible>();
                    collectible.type = CollectibleType.Coin;
                    collectible.colorIndex = colorIndex; // Only collectable by matching player sphere
                    collectible.rotationSpeed = 160f;
                    collectible.hoverAmplitude = 0.08f;
                    collectible.hoverSpeed = 3.5f;
                }
            }
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

            // 2. Loop along the segment at marker intervals and spawn obstacles centered with marker rings
            float currentZ = markerInterval;
            while (currentZ < length)
            {
                if (Random.value < obstacleSpawnProbability)
                {
                    GameObject obsObj = new GameObject("Obstacle");
                    obsObj.transform.SetParent(this.transform, false);
                    obsObj.transform.localPosition = new Vector3(0f, 0f, currentZ);

                    // Choose a random rotation around the tube Z-axis
                    float angleDeg = Random.Range(0f, 360f);
                    obsObj.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

                    Obstacle obs = obsObj.AddComponent<Obstacle>();
                    obs.radius = radius;
                    
                    // Choose a random thickness from 0.5 up to half the tube height (radius * 0.5)
                    float[] thicknesses = { 0.5f, 1.0f, 1.5f, 2.0f, radius * 0.5f };
                    obs.thickness = thicknesses[Random.Range(0, thicknesses.Length)];
                    obs.depth = 0.4f;     // Z-axis thickness centered at the marker ring
                    
                    // Pick a random arc angle
                    float[] arcAngles = { 60f, 90f, 120f };
                    obs.arcAngle = arcAngles[Random.Range(0, arcAngles.Length)];
                    
                    // 50% chance to spawn a color-coded passable obstacle, 50% chance for solid red
                    bool isColorCoded = (Random.value < 0.5f) && (transparentObstacleMaterials != null && transparentObstacleMaterials.Length > 0);

                    if (isColorCoded)
                    {
                        int colorIdx = Random.Range(0, transparentObstacleMaterials.Length);
                        obs.isColorCoded = true;
                        obs.targetColorIndex = colorIdx;
                        obs.obstacleMaterial = transparentObstacleMaterials[colorIdx];
                    }
                    else
                    {
                        obs.isColorCoded = false;
                        obs.targetColorIndex = -1;
                        obs.obstacleMaterial = obstacleMaterial;
                    }
                }
                currentZ += markerInterval;
            }
        }
    }
}
