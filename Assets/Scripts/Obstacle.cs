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
        private float passDuration = 0.35f;
        private Material matInstance;
        private Color baseColor;
        private Color baseEmissionColor;
        private Transform collectingSphere;
        private Vector3 initialWorldPos;
        private Vector3 initialLocalScale;

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
                    float easeT = t * t;

                    // 1. Smoothly shrink scale down to zero into the sphere center
                    transform.localScale = Vector3.Lerp(initialLocalScale, Vector3.zero, easeT);

                    // 2. Smoothly translate position towards the center of the collecting player sphere
                    if (collectingSphere != null)
                    {
                        transform.position = Vector3.Lerp(initialWorldPos, collectingSphere.position, easeT);
                    }

                    // 3. Smoothly dissolve opacity and glowing emission
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
                PlayerController player = null;
                Transform current = other.transform;
                while (current != null)
                {
                    player = current.GetComponent<PlayerController>();
                    if (player != null) break;
                    current = current.parent;
                }

                if (player != null && player.IsInvincible)
                {
                    // Invincibility Powerup active: smash through!
                    player.AddScore(10); // Bonus points
                    
#if UNITY_ANDROID || UNITY_IOS
                    // Simple fallback vibration, assuming new Input System haptics plugin isn't present
                    Handheld.Vibrate();
#endif
                    if (UnityEngine.InputSystem.Gamepad.current != null)
                    {
                        // Gamepad rumble fallback
                        UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(0.5f, 0.5f);
                        // Ideally we'd reset this after a delay, but we'll let it ride for a frame or use a quick pulse if we had a manager
                    }

                    player.PlaySound(ProceduralAudio.GetBreakSound());
                    Shatter(sphere.transform);
                    return;
                }

                if (isColorCoded && sphere.colorIndex == targetColorIndex)
                {
                    // Successful color match! Resolve player controller to add +3 points to score
                    if (player != null)
                    {
                        player.AddScore(3);
                        player.PlaySound(ProceduralAudio.GetAcceptSound());
                    }

                    // Trigger pass-through shrink & dissolve animation into player sphere center
                    StartPassThroughAnimation(sphere.transform);
                }
                else
                {
                    // Color mismatch or standard solid obstacle: trigger Game Over!
                    Debug.Log($"[Obstacle] Crash! Color mismatch. Target index = {targetColorIndex}, Sphere index = {sphere.colorIndex}.");
                    if (player != null)
                    {
                        player.PlaySound(ProceduralAudio.GetCrashSound());
                    }
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GameOver();
                    }
                }
            }
        }

        private void StartPassThroughAnimation(Transform playerSphereTransform)
        {
            isPassingThrough = true;
            passTimer = 0f;
            collectingSphere = playerSphereTransform;
            initialWorldPos = transform.position;
            initialLocalScale = transform.localScale;

            // Disable all child triggers so we don't double trigger
            for (int i = 0; i < transform.childCount; i++)
            {
                BoxCollider box = transform.GetChild(i).GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.enabled = false;
                }
            }

            Debug.Log($"[Obstacle] Color match success! +3 Score. Initiating shrink into player sphere center.");
        }

        private void Shatter(Transform playerSphereTransform)
        {
            isPassingThrough = true;

            // Disable all child triggers
            for (int i = 0; i < transform.childCount; i++)
            {
                BoxCollider box = transform.GetChild(i).GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.enabled = false;
                }
            }

            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                ExplodingObstacle exploder = gameObject.AddComponent<ExplodingObstacle>();
                exploder.Initialize(filter.sharedMesh, playerSphereTransform.position);
            }
        }
    }

    public class ExplodingObstacle : MonoBehaviour
    {
        private Mesh mesh;
        private Vector3[] currentVerts;
        private Vector3[] vertexVelocities;
        private float timer = 0f;
        public Vector3 explosionCenter;
        
        public void Initialize(Mesh originalMesh, Vector3 expCenter)
        {
            explosionCenter = expCenter;
            mesh = Instantiate(originalMesh);
            GetComponent<MeshFilter>().sharedMesh = mesh;
            
            // Subdivide mesh logic: duplicate shared vertices so each triangle is independent,
            // subdivide into 4, then push each triangle outward.
            int[] tris = originalMesh.triangles;
            Vector3[] verts = originalMesh.vertices;
            Vector2[] uvs = originalMesh.uv;
            
            int newTriCount = tris.Length * 4;
            Vector3[] newVerts = new Vector3[newTriCount];
            Vector2[] newUvs = new Vector2[newTriCount];
            int[] newTris = new int[newTriCount];
            vertexVelocities = new Vector3[newTriCount];

            int vIdx = 0;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v1 = verts[tris[i]];
                Vector3 v2 = verts[tris[i+1]];
                Vector3 v3 = verts[tris[i+2]];
                
                Vector2 uv1 = uvs[tris[i]];
                Vector2 uv2 = uvs[tris[i+1]];
                Vector2 uv3 = uvs[tris[i+2]];
                
                // Midpoints for subdivision
                Vector3 m12 = (v1 + v2) * 0.5f;
                Vector3 m23 = (v2 + v3) * 0.5f;
                Vector3 m31 = (v3 + v1) * 0.5f;
                
                Vector2 uv12 = (uv1 + uv2) * 0.5f;
                Vector2 uv23 = (uv2 + uv3) * 0.5f;
                Vector2 uv31 = (uv3 + uv1) * 0.5f;

                AddIndependentTriangle(ref vIdx, newVerts, newUvs, newTris, v1, m12, m31, uv1, uv12, uv31);
                AddIndependentTriangle(ref vIdx, newVerts, newUvs, newTris, v2, m23, m12, uv2, uv23, uv12);
                AddIndependentTriangle(ref vIdx, newVerts, newUvs, newTris, v3, m31, m23, uv3, uv31, uv23);
                AddIndependentTriangle(ref vIdx, newVerts, newUvs, newTris, m12, m23, m31, uv12, uv23, uv31);
            }
            
            mesh.vertices = newVerts;
            mesh.uv = newUvs;
            mesh.triangles = newTris;
            mesh.RecalculateNormals();
            
            currentVerts = newVerts;
            
            for (int i = 0; i < newVerts.Length; i += 3)
            {
                Vector3 center = transform.TransformPoint((newVerts[i] + newVerts[i+1] + newVerts[i+2]) / 3f);
                Vector3 dir = (center - explosionCenter).normalized;
                dir = (dir + Random.insideUnitSphere * 0.25f).normalized; // Add random scatter
                float speed = Random.Range(20f, 40f);
                
                Vector3 localVel = transform.InverseTransformDirection(dir * speed);
                vertexVelocities[i] = localVel;
                vertexVelocities[i+1] = localVel;
                vertexVelocities[i+2] = localVel;
            }
        }
        
        private void AddIndependentTriangle(ref int vIdx, Vector3[] verts, Vector2[] uvs, int[] tris, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            verts[vIdx] = v1; uvs[vIdx] = uv1; tris[vIdx] = vIdx; vIdx++;
            verts[vIdx] = v2; uvs[vIdx] = uv2; tris[vIdx] = vIdx; vIdx++;
            verts[vIdx] = v3; uvs[vIdx] = uv3; tris[vIdx] = vIdx; vIdx++;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > 1.25f)
            {
                if (UnityEngine.InputSystem.Gamepad.current != null && timer < 1.35f)
                {
                    // Stop gamepad rumble just before destroying
                    UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(0f, 0f);
                }
                Destroy(gameObject);
                return;
            }
            
            for (int i = 0; i < currentVerts.Length; i++)
            {
                currentVerts[i] += vertexVelocities[i] * Time.deltaTime;
                // Basic gravity
                vertexVelocities[i] += transform.InverseTransformDirection(Vector3.down) * 15f * Time.deltaTime;
            }
            
            mesh.vertices = currentVerts;
            mesh.RecalculateBounds();
            
            // Fade out the material
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                Material mat = renderer.material;
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = Mathf.Lerp(1f, 0f, timer / 1.25f);
                    mat.SetColor("_BaseColor", c);
                }
            }
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
