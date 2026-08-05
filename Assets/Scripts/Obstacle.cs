using UnityEngine;

namespace TubityWAI
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Obstacle : MonoBehaviour
    {
        [Header("Procedural Toroid Settings")]
        public float radius = 5f;
        public float thickness = 0.5f; // Radial thickness (inward from the wall)
        public float depth = 0.35f;     // Depth along the Z-axis
        public float arcAngle = 90f;   // Angular span in degrees
        public int radialSegments = 12;
        public Material obstacleMaterial;

        [Header("Color Matching Settings")]
        public bool isColorCoded = false;
        public int targetColorIndex = -1; // -1 represents solid hazard (dangerous to all)

        // Pass-through animation states
        private bool isPassingThrough = false;
        private float passTimer = 0f;
        private float passDuration = 0.3f;
        private Material matInstance;
        private Color baseColor;
        private Color baseEmissionColor;

        private void Start()
        {
            GenerateMesh();
            CreateCompoundTriggers();
        }

        private void Update()
        {
            if (isPassingThrough)
            {
                passTimer += Time.deltaTime;
                float t = passTimer / passDuration;

                if (t >= 1f)
                {
                    Destroy(gameObject);
                }
                else
                {
                    // 1. Subtle scale pulse on match
                    float scaleMult = Mathf.Lerp(1f, 1.12f, Mathf.Sin(t * Mathf.PI));
                    transform.localScale = new Vector3(scaleMult, scaleMult, 1f);

                    // 2. Smoothly dissolve opacity and glowing emission
                    if (matInstance != null)
                    {
                        if (matInstance.HasProperty("_BaseColor"))
                        {
                            Color c = baseColor;
                            c.a = Mathf.Lerp(baseColor.a, 0f, t);
                            matInstance.SetColor("_BaseColor", c);
                        }

                        if (matInstance.HasProperty("_EmissionColor"))
                        {
                            Color em = Color.Lerp(baseEmissionColor, Color.black, t);
                            matInstance.SetColor("_EmissionColor", em);
                        }
                    }
                }
            }
        }

        private void GenerateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "ToroidSection";

            float innerRad = radius - thickness;
            float outerRad = radius;
            float halfDepth = depth * 0.5f;
            float angleStep = arcAngle / radialSegments;

            int vertCount = (radialSegments + 1) * 4;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int i = 0; i <= radialSegments; i++)
            {
                float deg = i * angleStep;
                float rad = deg * Mathf.Deg2Rad;
                float sin = Mathf.Sin(rad);
                float cos = -Mathf.Cos(rad);

                int baseIdx = i * 4;
                vertices[baseIdx + 0] = new Vector3(sin * innerRad, cos * innerRad, -halfDepth);
                vertices[baseIdx + 1] = new Vector3(sin * outerRad, cos * outerRad, -halfDepth);
                vertices[baseIdx + 2] = new Vector3(sin * innerRad, cos * innerRad, halfDepth);
                vertices[baseIdx + 3] = new Vector3(sin * outerRad, cos * outerRad, halfDepth);

                float u = (float)i / radialSegments;
                uvs[baseIdx + 0] = new Vector2(u, 0f);
                uvs[baseIdx + 1] = new Vector2(u, 1f);
                uvs[baseIdx + 2] = new Vector2(u, 0f);
                uvs[baseIdx + 3] = new Vector2(u, 1f);
            }

            int totalTris = (radialSegments * 4 + 2) * 6;
            int[] triangles = new int[totalTris];
            int tIdx = 0;

            for (int i = 0; i < radialSegments; i++)
            {
                int baseIdx = i * 4;
                int nextIdx = (i + 1) * 4;

                // Front Face (Z = -halfDepth)
                triangles[tIdx++] = baseIdx + 0;
                triangles[tIdx++] = baseIdx + 1;
                triangles[tIdx++] = nextIdx + 0;

                triangles[tIdx++] = baseIdx + 1;
                triangles[tIdx++] = nextIdx + 1;
                triangles[tIdx++] = nextIdx + 0;

                // Back Face (Z = +halfDepth)
                triangles[tIdx++] = baseIdx + 2;
                triangles[tIdx++] = nextIdx + 2;
                triangles[tIdx++] = baseIdx + 3;

                triangles[tIdx++] = baseIdx + 3;
                triangles[tIdx++] = nextIdx + 2;
                triangles[tIdx++] = nextIdx + 3;

                // Inner Curved Face (facing center)
                triangles[tIdx++] = baseIdx + 0;
                triangles[tIdx++] = nextIdx + 2;
                triangles[tIdx++] = baseIdx + 2;

                triangles[tIdx++] = baseIdx + 0;
                triangles[tIdx++] = nextIdx + 0;
                triangles[tIdx++] = nextIdx + 2;

                // Outer Curved Face (facing wall)
                triangles[tIdx++] = baseIdx + 1;
                triangles[tIdx++] = baseIdx + 3;
                triangles[tIdx++] = nextIdx + 1;

                triangles[tIdx++] = nextIdx + 1;
                triangles[tIdx++] = baseIdx + 3;
                triangles[tIdx++] = nextIdx + 3;
            }

            // Left End Cap (i = 0)
            triangles[tIdx++] = 0;
            triangles[tIdx++] = 2;
            triangles[tIdx++] = 1;

            triangles[tIdx++] = 1;
            triangles[tIdx++] = 2;
            triangles[tIdx++] = 3;

            // Right End Cap (i = radialSegments)
            int endBase = radialSegments * 4;
            triangles[tIdx++] = endBase + 0;
            triangles[tIdx++] = endBase + 1;
            triangles[tIdx++] = endBase + 2;

            triangles[tIdx++] = endBase + 1;
            triangles[tIdx++] = endBase + 3;
            triangles[tIdx++] = endBase + 2;

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = mesh;
            
            // Assign material and instantiate it for runtime animation support
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null && obstacleMaterial != null)
            {
                matInstance = renderer.material = new Material(obstacleMaterial);
                if (matInstance.HasProperty("_BaseColor"))
                {
                    baseColor = matInstance.GetColor("_BaseColor");
                }
                if (matInstance.HasProperty("_EmissionColor"))
                {
                    baseEmissionColor = matInstance.GetColor("_EmissionColor");
                }
            }
        }

        private void CreateCompoundTriggers()
        {
            float innerRad = radius - thickness;
            float outerRad = radius;
            float rMid = (innerRad + outerRad) * 0.5f;
            float angleStep = arcAngle / radialSegments;

            float segmentWidth = 2f * rMid * Mathf.Sin((angleStep * 0.5f) * Mathf.Deg2Rad);
            float colliderWidth = segmentWidth * 1.05f;
            float colliderHeight = thickness;
            float colliderDepth = depth;

            for (int i = 0; i < radialSegments; i++)
            {
                float midAngle = (i + 0.5f) * angleStep;
                float rad = midAngle * Mathf.Deg2Rad;
                float sin = Mathf.Sin(rad);
                float cos = -Mathf.Cos(rad);

                GameObject trigObj = new GameObject("TriggerSegment_" + i);
                trigObj.transform.SetParent(this.transform, false);
                trigObj.transform.localPosition = new Vector3(sin * rMid, cos * rMid, 0f);

                Vector3 radialDir = trigObj.transform.localPosition.normalized;
                trigObj.transform.localRotation = Quaternion.LookRotation(Vector3.forward, radialDir);

                BoxCollider box = trigObj.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(colliderWidth, colliderHeight, colliderDepth);

                ObstacleTrigger triggerScript = trigObj.AddComponent<ObstacleTrigger>();
                triggerScript.parentObstacle = this;
            }
        }

        public void HandleTriggerEnter(Collider other)
        {
            if (isPassingThrough) return; // Ignore secondary triggers during animation

            PlayerSphere sphere = other.GetComponent<PlayerSphere>();
            if (sphere != null)
            {
                if (isColorCoded && sphere.colorIndex == targetColorIndex)
                {
                    // Successful color match! Trigger pass-through dissolve animation
                    StartPassThroughAnimation();
                }
                else
                {
                    // Color mismatch or standard solid obstacle: trigger Game Over!
                    Debug.Log($"[Obstacle] Crash! Color mismatch. Target index = {targetColorIndex}, Sphere index = {sphere.colorIndex}.");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GameOver();
                    }
                }
            }
        }

        private void StartPassThroughAnimation()
        {
            isPassingThrough = true;
            passTimer = 0f;

            // Disable all child triggers so we don't double trigger
            for (int i = 0; i < transform.childCount; i++)
            {
                BoxCollider box = transform.GetChild(i).GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.enabled = false;
                }
            }

            Debug.Log($"[Obstacle] Color match success! Initiating pass-through dissolve animation.");
        }
    }

    public class ObstacleTrigger : MonoBehaviour
    {
        public Obstacle parentObstacle;

        private void OnTriggerEnter(Collider other)
        {
            if (parentObstacle != null)
            {
                parentObstacle.HandleTriggerEnter(other);
            }
        }
    }
}
