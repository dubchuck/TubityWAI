using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The attract-screen centrepiece: the neon band sphere hovering over a
    /// polished pad that reflects it.
    ///
    /// The reflection is mirrored geometry, not a reflection camera. The band
    /// mesh is drawn a second time through a matrix that flips it about the pad
    /// surface, with TubityX/NeonBandMirror stencil-testing against the ref the
    /// platform stamped. Cost: one extra draw of a mesh already in memory - no
    /// second camera, no render texture, no extra scene pass.
    /// </summary>
    [AddComponentMenu("TubityX/Attract Hero Stage")]
    public class AttractHeroStage : MonoBehaviour
    {
        [Header("Layout")]
        public float sphereRadius = 1f;
        [Tooltip("Gap between the bottom of the sphere and the pad, as a fraction of radius.")]
        public float hoverGap = 0.18f;
        [Tooltip("Half-width of the square pad, in sphere radii.")]
        public float padHalf = 1.78f;
        public float padThickness = 0.11f;
        [Tooltip("Pad rotation about Y. 45 puts a corner toward the camera, as in the concept art.")]
        public float padYaw = 45f;

        [Header("Motion")]
        public float bobAmplitude = 0.06f;   // world units
        public float bobPeriod = 4.5f;

        [Header("Materials")]
        public Material bandMaterial;
        public Material bandMirrorMaterial;
        public Material bandHaloMaterial;
        public Material coreMaterial;
        public Material platformMaterial;
        public Material skirtMaterial;

        private NeonBandSphere sphere;
        private Transform platform;
        private Material mirrorInstance;
        private float planeY;
        private float sphereRestY;

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            // --- the pad ------------------------------------------------------
            GameObject pad = new GameObject("Platform");
            platform = pad.transform;
            platform.SetParent(transform, false);
            platform.localPosition = Vector3.zero;
            platform.localRotation = Quaternion.Euler(0f, padYaw, 0f);

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Quad);
            top.name = "PadSurface";
            top.transform.SetParent(platform, false);
            top.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // face up
            top.transform.localScale = Vector3.one * padHalf * 2f * sphereRadius;
            DestroyImmediate(top.GetComponent<Collider>());
            Dress(top.GetComponent<MeshRenderer>(), platformMaterial);

            GameObject skirt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skirt.name = "PadSkirt";
            skirt.transform.SetParent(platform, false);
            float w = padHalf * 2f * sphereRadius * 0.97f;
            skirt.transform.localScale = new Vector3(w, padThickness, w);
            skirt.transform.localPosition = new Vector3(0f, -padThickness * 0.5f - 0.002f, 0f);
            DestroyImmediate(skirt.GetComponent<Collider>());
            Dress(skirt.GetComponent<MeshRenderer>(), skirtMaterial);

            planeY = transform.position.y;

            // --- the sphere ---------------------------------------------------
            // Created inactive so AddComponent's Awake does not build a default
            // sphere before these fields are set - otherwise the mesh is generated
            // twice, once at the wrong radius.
            GameObject ball = new GameObject("HeroSphere");
            ball.SetActive(false);
            ball.transform.SetParent(transform, false);
            sphereRestY = sphereRadius * (1f + hoverGap);
            ball.transform.localPosition = new Vector3(0f, sphereRestY, 0f);

            sphere = ball.AddComponent<NeonBandSphere>();
            sphere.buildOnAwake = false;
            sphere.radius = sphereRadius;
            sphere.bandMaterial = bandMaterial;
            sphere.haloMaterial = bandHaloMaterial;
            sphere.coreMaterial = coreMaterial;
            sphere.Build();
            ball.SetActive(true);

            if (bandMirrorMaterial != null)
            {
                mirrorInstance = new Material(bandMirrorMaterial);
                mirrorInstance.name = bandMirrorMaterial.name + " (mirror)";
            }
        }

        private static void Dress(MeshRenderer r, Material m)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            if (m != null) r.sharedMaterial = m;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }

        /// <summary>
        /// Show or hide only the hero sphere. The pad stays put - the sphere-count
        /// selector stands its own spheres on it, and the mirror reflects whichever
        /// band spheres are live.
        /// </summary>
        public void SetHeroSphereVisible(bool visible)
        {
            if (sphere == null) return;
            if (sphere.gameObject.activeSelf != visible) sphere.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            DrawReflection();
            if (sphere == null || !sphere.gameObject.activeInHierarchy) return;

            if (bobAmplitude > 0f && bobPeriod > 0.01f)
            {
                float bob = Mathf.Sin(Time.time * Mathf.PI * 2f / bobPeriod) * bobAmplitude;
                Vector3 p = sphere.transform.localPosition;
                p.y = sphereRestY + bob;
                sphere.transform.localPosition = p;
            }
        }

        /// <summary>
        /// Re-draw every live band mesh flipped about the pad surface. Reflecting y
        /// about the plane is y -> 2*planeY - y, which is exactly what these two
        /// matrix entries do; everything else about each transform is preserved.
        ///
        /// Iterating NeonBandSphere.Active rather than just the hero is what lets
        /// the pad keep reflecting during sphere-count selection, when the hero is
        /// hidden and the morpher's clones are standing on it instead.
        /// </summary>
        private void DrawReflection()
        {
            if (mirrorInstance == null) return;

            float y = transform.position.y;
            mirrorInstance.SetFloat("_MirrorPlaneY", y);

            Matrix4x4 flip = Matrix4x4.identity;
            flip.m11 = -1f;
            flip.m13 = 2f * y;

            for (int i = 0; i < NeonBandSphere.Active.Count; i++)
            {
                NeonBandSphere s = NeonBandSphere.Active[i];
                if (s == null || s.BandMesh == null || s.BandRenderer == null) continue;
                if (!s.BandRenderer.gameObject.activeInHierarchy) continue;

                Graphics.DrawMesh(s.BandMesh,
                                  flip * s.BandRenderer.transform.localToWorldMatrix,
                                  mirrorInstance, gameObject.layer, null, 0, null,
                                  UnityEngine.Rendering.ShadowCastingMode.Off, false, null,
                                  UnityEngine.Rendering.LightProbeUsage.Off);
            }
        }

        private void OnDestroy()
        {
            if (mirrorInstance != null) DestroyImmediate(mirrorInstance);
        }
    }
}
