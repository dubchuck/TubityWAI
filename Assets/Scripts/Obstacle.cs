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

        [Header("Colour Shift Settings")]
        [Tooltip("When set (2+ entries) the arc cycles which sphere colour it accepts every colorShiftInterval seconds.")]
        public Material[] colorShiftMaterials;
        public float colorShiftInterval = 0f;
        [Tooltip("Seed for the colour sequence, set by the spawner so the shifts repeat run to run.")]
        public int colorShiftSeed = 0;
        private float colorShiftTimer = 0f;
        private System.Random shiftRng;

        [Header("Neon Halo (optional)")]
        [Tooltip("Additive ribbon material (TubityX/NeonBandHalo) drawn as a soft cloud around the arc's face. Null = no halo.")]
        public Material haloMaterial;
        [Tooltip("How far the halo cloud reaches beyond the arc, in tube units.")]
        public float haloPadding = 0.6f;
        private GameObject haloObj;
        private Mesh haloMesh;
        private float[] haloFade;

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
            if (haloMaterial != null)
            {
                BuildHalo();
            }
        }

        private void OnDestroy()
        {
            if (haloMesh != null) Destroy(haloMesh);
        }

        /// <summary>
        /// A flat annular-sector ribbon just in front of the arc's face, tinted by vertex colour
        /// and shaded by TubityX/NeonBandHalo: a gaussian across the ribbon (uv.y) that peaks on
        /// the arc and fades to nothing haloPadding beyond it, with the angular ends feathered
        /// through the vertex colour. This is the same neon "cloud" the attract sphere uses.
        /// </summary>
        private void BuildHalo()
        {
            float halfDepth = depth * 0.5f;
            float rMid = radius - thickness * 0.5f;
            float halfWidth = thickness * 0.5f + haloPadding;
            float inner = Mathf.Max(0.05f, rMid - halfWidth);
            float outer = rMid + halfWidth;
            float padDeg = Mathf.Min(45f, haloPadding / Mathf.Max(0.1f, rMid) * Mathf.Rad2Deg);
            const int padSteps = 3;
            int steps = radialSegments + padSteps * 2;

            int vertCount = (steps + 1) * 3;
            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            haloFade = new float[vertCount];
            float z = -halfDepth - 0.02f;

            for (int i = 0; i <= steps; i++)
            {
                float deg;
                if (i < padSteps) deg = -padDeg + padDeg * (i / (float)padSteps);
                else if (i <= padSteps + radialSegments) deg = arcAngle * ((i - padSteps) / (float)radialSegments);
                else deg = arcAngle + padDeg * ((i - padSteps - radialSegments) / (float)padSteps);

                float fade = 1f;
                if (deg < 0f || deg > arcAngle)
                {
                    float d = (deg < 0f ? -deg : deg - arcAngle) / Mathf.Max(0.01f, padDeg);
                    fade = Mathf.Exp(-(d * 2.1f) * (d * 2.1f));
                }

                float rad = deg * Mathf.Deg2Rad;
                float sin = Mathf.Sin(rad);
                float cos = -Mathf.Cos(rad);
                int b = i * 3;
                verts[b + 0] = new Vector3(sin * inner, cos * inner, z);
                verts[b + 1] = new Vector3(sin * rMid, cos * rMid, z);
                verts[b + 2] = new Vector3(sin * outer, cos * outer, z);
                uvs[b + 0] = new Vector2(0f, 0f);
                uvs[b + 1] = new Vector2(0f, 0.5f);
                uvs[b + 2] = new Vector2(0f, 1f);
                haloFade[b + 0] = fade; haloFade[b + 1] = fade; haloFade[b + 2] = fade;
            }

            int[] tris = new int[steps * 12];
            int t = 0;
            for (int i = 0; i < steps; i++)
            {
                int a = i * 3;
                int c = a + 3;
                tris[t++] = a; tris[t++] = a + 1; tris[t++] = c + 1;
                tris[t++] = a; tris[t++] = c + 1; tris[t++] = c;
                tris[t++] = a + 1; tris[t++] = a + 2; tris[t++] = c + 2;
                tris[t++] = a + 1; tris[t++] = c + 2; tris[t++] = c + 1;
            }

            haloMesh = new Mesh();
            haloMesh.name = "ArcHalo";
            haloMesh.vertices = verts;
            haloMesh.uv = uvs;
            haloMesh.triangles = tris;
            haloMesh.RecalculateBounds();

            haloObj = new GameObject("Halo", typeof(MeshFilter), typeof(MeshRenderer));
            haloObj.transform.SetParent(this.transform, false);
            haloObj.GetComponent<MeshFilter>().sharedMesh = haloMesh;
            MeshRenderer hr = haloObj.GetComponent<MeshRenderer>();
            hr.sharedMaterial = haloMaterial;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hr.receiveShadows = false;
            hr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            hr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            RefreshHaloColours();
        }

        /// <summary>The arc's own hue at unit brightness, read from its material (emission first).</summary>
        private Color HaloColour()
        {
            Color c = Color.white;
            if (obstacleMaterial != null)
            {
                if (obstacleMaterial.HasProperty("_EmissionColor")) c = obstacleMaterial.GetColor("_EmissionColor");
                else if (obstacleMaterial.HasProperty("_BaseColor")) c = obstacleMaterial.GetColor("_BaseColor");
                else if (obstacleMaterial.HasProperty("_Color")) c = obstacleMaterial.GetColor("_Color");
            }
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (m > 0.0001f) { c.r /= m; c.g /= m; c.b /= m; }
            c.a = 1f;
            return c;
        }

        private void RefreshHaloColours()
        {
            if (haloMesh == null || haloFade == null) return;
            Color col = HaloColour();
            Color[] cols = new Color[haloFade.Length];
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i] = new Color(col.r * haloFade[i], col.g * haloFade[i], col.b * haloFade[i], 1f);
            }
            haloMesh.colors = cols;
        }

        private void Update()
        {
            if (!isPassingThrough && isColorCoded && colorShiftInterval > 0f
                && colorShiftMaterials != null && colorShiftMaterials.Length > 1)
            {
                colorShiftTimer += Time.deltaTime;
                if (colorShiftTimer >= colorShiftInterval)
                {
                    colorShiftTimer = 0f;
                    ShiftColor();
                }
            }

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
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
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
                    if (GameManager.Instance != null) GameManager.Instance.RegisterShieldPassed();

                    // Trigger pass-through shrink & dissolve animation into player sphere center
                    StartPassThroughAnimation(sphere.transform);
                }
                else
                {
                    // Color mismatch or standard solid obstacle: trigger Game Over or partial death!
                    Debug.Log($"[Obstacle] Crash! Color mismatch. Target index = {targetColorIndex}, Sphere index = {sphere.colorIndex}.");
                    if (player != null)
                    {
                        player.HandleCrash(sphere.transform);
                        Shatter(sphere.transform);
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GameOver();
                    }
                }
            }
        }

        /// <summary>
        /// Warning look driven by RingArcGroup ahead of a snap turn: 0 restores the normal
        /// colours, 1 pushes the emission to a hot white flash. Ignored once the arc is being
        /// collected or shattered.
        /// </summary>
        public void SetTelegraph(float strength)
        {
            if (isPassingThrough || matInstance == null) return;
            strength = Mathf.Clamp01(strength);

            if (matInstance.HasProperty("_EmissionColor"))
            {
                float peak = Mathf.Max(baseEmissionColor.r, Mathf.Max(baseEmissionColor.g, baseEmissionColor.b));
                float hotIntensity = Mathf.Max(1f, peak * 1.6f);
                Color hot = new Color(hotIntensity, hotIntensity, hotIntensity, 1f);
                matInstance.SetColor("_EmissionColor", Color.Lerp(baseEmissionColor, hot, strength));
            }
            if (matInstance.HasProperty("_BaseColor"))
            {
                Color c = Color.Lerp(baseColor, Color.white, strength * 0.6f);
                c.a = baseColor.a;
                matInstance.SetColor("_BaseColor", c);
            }
        }

        /// <summary>Switch this colour-coded arc to a different accepted colour (never the current one).</summary>
        private void ShiftColor()
        {
            int count = colorShiftMaterials.Length;
            int current = Mathf.Clamp(targetColorIndex, 0, count - 1);
            if (shiftRng == null) shiftRng = new System.Random(colorShiftSeed);
            int next = (current + 1 + shiftRng.Next(0, count - 1)) % count;

            targetColorIndex = next;
            obstacleMaterial = colorShiftMaterials[next];

            if (matInstance != null && obstacleMaterial != null)
            {
                matInstance.CopyPropertiesFromMaterial(obstacleMaterial);
                if (matInstance.HasProperty("_BaseColor"))
                {
                    baseColor = matInstance.GetColor("_BaseColor");
                }
                if (matInstance.HasProperty("_EmissionColor"))
                {
                    baseEmissionColor = matInstance.GetColor("_EmissionColor");
                }
            }
            RefreshHaloColours();
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
            if (haloObj != null) haloObj.SetActive(false);   // the cloud has nothing left to wrap

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
