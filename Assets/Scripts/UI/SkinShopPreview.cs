using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TubityWAI
{
    /// <summary>
    /// Live 3D previews for the cosmetics shop: one off-screen camera renders a
    /// row of skinned band spheres into a single wide texture, and each shop
    /// card shows its slice of it through a RawImage uvRect. One camera and one
    /// texture for the whole page, however many cards are on it.
    ///
    /// The rig lives far below the menu world so nothing else ever sees it, and
    /// the camera only runs while the shop is open.
    /// </summary>
    public class SkinShopPreview : MonoBehaviour
    {
        public const int Slots = 6;
        private const int SlotPixels = 256;
        private const float CellSize = 2.8f;              // world units per slot, ortho

        private Camera cam;
        private RenderTexture texture;
        private readonly NeonBandSphere[] spheres = new NeonBandSphere[Slots];
        private Transform watch;

        public Texture Texture { get { return texture; } }

        /// <summary>UV rectangle of one slot in the shared texture.</summary>
        public static Rect SlotRect(int slot)
        {
            return new Rect(slot / (float)Slots, 0f, 1f / Slots, 1f);
        }

        /// <summary>
        /// Build the rig under `parent` (destroyed with the menu). `watch` is the
        /// shop layer: the camera renders only while it is active.
        /// </summary>
        public static SkinShopPreview Create(Transform parent, Transform watch, Color previewColour)
        {
            // Belt-and-braces against leftovers: GameSetup is [ExecuteAlways] and MainMenu
            // builds this rig from that Awake/Start path, so with "Reload Scene" off (or any
            // other edit/play boundary quirk) a rig built by a previous Play session can survive
            // into the saved scene instead of being torn down. Without this guard, each Play
            // press adds another full rig (camera + 6 preview spheres) that never goes away -
            // that is exactly how 9 of them ended up baked into SampleScene.unity.
            SkinShopPreview[] stale = Object.FindObjectsByType<SkinShopPreview>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < stale.Length; i++)
            {
                if (stale[i] != null) Object.DestroyImmediate(stale[i].gameObject);
            }

            GameObject rigObj = new GameObject("SkinShopPreviewRig");
            rigObj.transform.SetParent(parent, false);
            // well clear of the attract world and the tunnel, both of which sit
            // near the origin; the main camera's frustum never reaches here
            rigObj.transform.position = new Vector3(0f, -900f, 0f);

            SkinShopPreview p = rigObj.AddComponent<SkinShopPreview>();
            p.watch = watch;
            p.Build(previewColour);
            return p;
        }

        private void Build(Color previewColour)
        {
            texture = new RenderTexture(SlotPixels * Slots, SlotPixels, 24, RenderTextureFormat.ARGB32);
            texture.name = "SkinShopPreview";
            texture.antiAliasing = 2;
            texture.Create();

            GameObject camObj = new GameObject("PreviewCamera");
            camObj.transform.SetParent(transform, false);
            camObj.transform.localPosition = new Vector3(0f, 0f, -10f);

            cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CellSize * 0.5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.022f, 0.055f, 1f);   // the core's own dark
            cam.targetTexture = texture;
            cam.depth = -50f;
            cam.allowHDR = true;
            cam.useOcclusionCulling = false;

            // the rig sits alone at -900, so culling by distance is enough; the
            // camera just must not see the far-away scene
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = true;      // bloom is what sells the neon
                data.renderShadows = false;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.requiresDepthOption = CameraOverrideOption.Off;
            }

            Material bandMat = Resources.Load<Material>("Attract/Mat_NeonBand");
            Material coreMat = Resources.Load<Material>("Attract/Mat_NeonBandCore");
            Material haloMat = Resources.Load<Material>("Attract/Mat_NeonBandHalo");

            for (int i = 0; i < Slots; i++)
            {
                GameObject ball = new GameObject("Preview_" + i);
                ball.SetActive(false);
                ball.transform.SetParent(transform, false);
                float x = (i - (Slots - 1) * 0.5f) * CellSize;
                ball.transform.localPosition = new Vector3(x, 0f, 0f);
                // lean each one so both "eyes" of the winding show at once
                ball.transform.localRotation = Quaternion.Euler(18f, -30f + i * 7f, 0f);

                NeonBandSphere s = ball.AddComponent<NeonBandSphere>();
                s.buildOnAwake = false;
                s.registerForMirror = false;
                s.radius = 1.0f;
                s.widthScale = 1.1f;
                s.spin = new Vector3(0f, 26f, 5f);
                s.bandMaterial = bandMat;
                s.coreMaterial = coreMat;
                s.haloMaterial = haloMat;
                s.ApplySkin(SphereSkinCatalog.Get(0), previewColour);
                ball.SetActive(true);
                spheres[i] = s;
            }
        }

        /// <summary>Show skins `firstSkin`.. in the slots; slots past the catalogue go dark.</summary>
        public void ShowPage(int firstSkin, Color previewColour)
        {
            for (int i = 0; i < Slots; i++)
            {
                int index = firstSkin + i;
                bool has = index >= 0 && index < SphereSkinCatalog.Count;
                spheres[i].gameObject.SetActive(has);
                if (has) spheres[i].ApplySkin(SphereSkinCatalog.Get(index), previewColour);
            }
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            bool live = watch != null && watch.gameObject.activeInHierarchy;
            if (cam.enabled != live) cam.enabled = live;
        }

        private void OnDestroy()
        {
            if (cam != null) cam.targetTexture = null;
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
