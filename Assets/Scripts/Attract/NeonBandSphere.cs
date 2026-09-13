using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The attract-screen hero: neon ribbons wound around a dark sphere.
    ///
    /// The bands are latitude rings about a tilted axis (see NeonBandData), built
    /// as ribbons that hug the surface rather than round tubes - three vertices
    /// across instead of six or eight, and the shader shapes the cross-section
    /// from UV.y. All of them go into ONE mesh, so the whole sphere is two draw
    /// calls: the opaque dark core, then the additive ribbons.
    ///
    /// The core is what makes it read as a solid ball: it is opaque, so the
    /// depth buffer hides the bands on the far side for free.
    ///
    /// With no skin applied this is exactly the signed-off hero. ApplySkin
    /// re-colours and thins the same winding per SphereSkinCatalog, which is how
    /// the shop preview, the count selector and the in-game player spheres all
    /// wear what the player bought.
    /// </summary>
    [AddComponentMenu("TubityX/Neon Band Sphere")]
    public class NeonBandSphere : MonoBehaviour
    {
        [Tooltip("Sphere radius in world units.")]
        public float radius = 1f;

        [Tooltip("Scales every ribbon's width. 1 matches the concept art.")]
        public float widthScale = 1f;

        public Material bandMaterial;
        public Material coreMaterial;

        [Tooltip("The soft glow cloud under each ribbon (TubityX/NeonBandHalo). " +
                 "Optional: with none set the sphere is just core plus ribbons.")]
        public Material haloMaterial;

        [Tooltip("Halo half-width as a multiple of each ribbon's own width.")]
        public float haloWidthScale = 2.5f;

        [Tooltip("Fixed floor added to every halo's half-width (fraction of " +
                 "radius) so the thinnest ribbons still glow.")]
        public float haloWidthAdd = 0.045f;

        [Tooltip("Off for a template that is built once and then cloned - the " +
                 "clones share the mesh instead of each rebuilding it.")]
        public bool buildOnAwake = true;

        [Tooltip("Off when the host already is the ball (the in-game player " +
                 "sphere primitive) and only the ribbons are wanted.")]
        public bool buildCore = true;

        [Tooltip("Off for spheres the attract stage's mirror must not reflect: " +
                 "the shop preview rig and the in-game spheres.")]
        public bool registerForMirror = true;

        [Header("Motion")]
        public Vector3 spin = new Vector3(0f, 14f, 4f);

        private Mesh bandMesh;
        private MeshRenderer bandRenderer;
        private bool ownsBandMesh;

        private SphereSkinCatalog.Skin skin;
        private Color baseColour = Color.white;
        private Material skinBandMaterial;
        private Material skinCoreMaterial;
        private Material skinHaloMaterial;

        /// <summary>The skin this sphere wears, or null for the untouched hero art.</summary>
        public SphereSkinCatalog.Skin Skin { get { return skin; } }
        public Color BaseColour { get { return baseColour; } }

        /// <summary>This sphere's own ribbon and halo materials once skinned (null before). SkinFlair animates them.</summary>
        public Material SkinBandMaterial { get { return skinBandMaterial; } }
        public Material SkinHaloMaterial { get { return skinHaloMaterial; } }

        // The spin before any skin scaled it. Public so Instantiate copies it to
        // clones; otherwise a clone would rescale an already-scaled spin.
        [HideInInspector] public Vector3 baseSpin;
        [HideInInspector] public bool hasBaseSpin;

        /// <summary>
        /// Every enabled band sphere, so AttractHeroStage can mirror all of them
        /// without a per-frame scene lookup. Clones made by the morpher register
        /// here too, which is how the count selector gets reflections.
        /// </summary>
        public static readonly System.Collections.Generic.List<NeonBandSphere> Active =
            new System.Collections.Generic.List<NeonBandSphere>();

        private void OnEnable()
        {
            ResolveBuiltParts();
            if (registerForMirror && !Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void OnDestroy()
        {
            if (ownsBandMesh && bandMesh != null) Destroy(bandMesh);
            if (skinBandMaterial != null) Destroy(skinBandMaterial);
            if (skinCoreMaterial != null) Destroy(skinCoreMaterial);
            if (skinHaloMaterial != null) Destroy(skinHaloMaterial);
        }

        /// <summary>
        /// A clone built by Instantiate skips Build(), so its mesh and renderer
        /// references are null even though the child objects are there. Pick them
        /// back up rather than rebuilding an identical mesh per clone.
        /// </summary>
        private void ResolveBuiltParts()
        {
            if (bandMesh != null && bandRenderer != null) return;
            Transform bands = transform.Find("Bands");
            if (bands == null) return;
            MeshFilter mf = bands.GetComponent<MeshFilter>();
            if (mf != null) bandMesh = mf.sharedMesh;
            bandRenderer = bands.GetComponent<MeshRenderer>();
            ownsBandMesh = false;   // shared with whatever it was cloned from
        }

        /// <summary>The ribbon mesh, for the mirror to draw a second time.</summary>
        public Mesh BandMesh { get { return bandMesh; } }
        public MeshRenderer BandRenderer { get { return bandRenderer; } }

        private void Awake()
        {
            if (buildOnAwake) Build();
        }

        private void Update()
        {
            if (spin != Vector3.zero) transform.Rotate(spin * Time.deltaTime, Space.Self);
        }

        public void Build()
        {
            if (buildCore) BuildCore();
            BuildBands();
        }

        /// <summary>
        /// Dress this sphere in a shop skin, coloured for one gameplay colour.
        /// Rebuilds the ribbons (colour is vertex data) and gives the sphere its
        /// own band and core materials so intensity and body tint are per skin.
        /// Safe to call repeatedly - the shop preview does, on every page turn.
        /// </summary>
        public void ApplySkin(SphereSkinCatalog.Skin newSkin, Color colour)
        {
            skin = newSkin;
            baseColour = colour;

            if (!hasBaseSpin) { baseSpin = spin; hasBaseSpin = true; }
            spin = baseSpin * (skin != null ? skin.SpinScale : 1f);

            if (skin != null)
            {
                Material bandSource = bandMaterial;
                if (skinBandMaterial == null && bandSource != null)
                {
                    skinBandMaterial = new Material(bandSource);
                    skinBandMaterial.name = bandSource.name + " (skin)";
                }
                if (skinBandMaterial != null)
                {
                    if (skinBandMaterial.HasProperty("_Intensity")) skinBandMaterial.SetFloat("_Intensity", skin.Intensity);
                    if (skinBandMaterial.HasProperty("_CoreWhite")) skinBandMaterial.SetFloat("_CoreWhite", skin.CoreWhite);
                }

                // the halo scales with the ribbons: an overdriven skin glows harder too
                if (skinHaloMaterial == null && haloMaterial != null)
                {
                    skinHaloMaterial = new Material(haloMaterial);
                    skinHaloMaterial.name = haloMaterial.name + " (skin)";
                }
                if (skinHaloMaterial != null && haloMaterial != null && skinHaloMaterial.HasProperty("_Intensity"))
                {
                    float baseHalo = haloMaterial.HasProperty("_Intensity") ? haloMaterial.GetFloat("_Intensity") : 0.55f;
                    skinHaloMaterial.SetFloat("_Intensity", baseHalo * (skin.Intensity / 1.6f));
                }

                if (buildCore && coreMaterial != null)
                {
                    if (skinCoreMaterial == null)
                    {
                        skinCoreMaterial = new Material(coreMaterial);
                        skinCoreMaterial.name = coreMaterial.name + " (skin)";
                    }
                    Color body = NeonBandData.CoreColour + colour * skin.CoreBody;
                    if (skinCoreMaterial.HasProperty("_Colour")) skinCoreMaterial.SetColor("_Colour", body);
                    if (skinCoreMaterial.HasProperty("_RimColour")) skinCoreMaterial.SetColor("_RimColour", colour * skin.CoreRim);
                    if (skinCoreMaterial.HasProperty("_RimStrength")) skinCoreMaterial.SetFloat("_RimStrength", skin.RimStrength);
                }
            }

            ResolveBuiltParts();
            if (buildCore)
            {
                Transform core = transform.Find("Core");
                if (core == null) BuildCore();
                else
                {
                    MeshRenderer mr = core.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = ActiveCoreMaterial();
                }
            }
            BuildBands();

            // rings, jets, motes, particles... whatever the skin hangs on top
            SkinFlair.Attach(this);
        }

        /// <summary>Keep the skin, change the gameplay colour - for spheres cloned from another.</summary>
        public void SetBaseColour(Color colour)
        {
            ApplySkin(skin ?? SphereSkinCatalog.Get(0), colour);
        }

        /// <summary>
        /// Dims this sphere via per-renderer MaterialPropertyBlocks - never touches the
        /// shared skin materials, so other spheres wearing the same skin are unaffected.
        /// 0 = normal neon brightness, 1 = fully dimmed. Used by the sphere-count
        /// selector to show a still-locked block without hiding the formation outright.
        /// </summary>
        public void SetFade(float amount)
        {
            amount = Mathf.Clamp01(amount);
            float glow = 1f - amount;

            if (bandRenderer != null)
            {
                Material bandMat = ActiveBandMaterial();
                float baseBandIntensity = (bandMat != null && bandMat.HasProperty("_Intensity")) ? bandMat.GetFloat("_Intensity") : 1.6f;
                MaterialPropertyBlock bandBlock = new MaterialPropertyBlock();
                bandBlock.SetFloat("_Intensity", baseBandIntensity * glow);
                bandRenderer.SetPropertyBlock(bandBlock, 0);

                if (bandRenderer.sharedMaterials.Length > 1)
                {
                    Material haloMat = ActiveHaloMaterial();
                    float baseHaloIntensity = (haloMat != null && haloMat.HasProperty("_Intensity")) ? haloMat.GetFloat("_Intensity") : 0.55f;
                    MaterialPropertyBlock haloBlock = new MaterialPropertyBlock();
                    haloBlock.SetFloat("_Intensity", baseHaloIntensity * glow);
                    bandRenderer.SetPropertyBlock(haloBlock, 1);
                }
            }

            if (buildCore)
            {
                Transform core = transform.Find("Core");
                MeshRenderer coreRenderer = core != null ? core.GetComponent<MeshRenderer>() : null;
                if (coreRenderer != null)
                {
                    Material coreMat = ActiveCoreMaterial();
                    Color rim = (coreMat != null && coreMat.HasProperty("_RimColour")) ? coreMat.GetColor("_RimColour") : new Color(0.10f, 0.35f, 0.75f);
                    float baseRimStrength = (coreMat != null && coreMat.HasProperty("_RimStrength")) ? coreMat.GetFloat("_RimStrength") : 0.85f;

                    MaterialPropertyBlock coreBlock = new MaterialPropertyBlock();
                    coreBlock.SetColor("_RimColour", rim * glow);
                    coreBlock.SetFloat("_RimStrength", baseRimStrength * glow);
                    coreRenderer.SetPropertyBlock(coreBlock);
                }
            }
        }

        private Material ActiveBandMaterial() { return (skin != null && skinBandMaterial != null) ? skinBandMaterial : bandMaterial; }
        private Material ActiveCoreMaterial() { return (skin != null && skinCoreMaterial != null) ? skinCoreMaterial : coreMaterial; }
        private Material ActiveHaloMaterial() { return (skin != null && skinHaloMaterial != null) ? skinHaloMaterial : haloMaterial; }

        private void BuildCore()
        {
            Transform existing = transform.Find("Core");
            if (existing != null) DestroyImmediate(existing.gameObject);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.layer = gameObject.layer;
            core.transform.SetParent(transform, false);
            core.transform.localScale = Vector3.one * radius * 2f;
            DestroyImmediate(core.GetComponent<Collider>());

            MeshRenderer mr = core.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            Material m = ActiveCoreMaterial();
            if (m != null) mr.sharedMaterial = m;
        }

        private void BuildBands()
        {
            Transform existing = transform.Find("Bands");
            if (existing != null) DestroyImmediate(existing.gameObject);
            if (ownsBandMesh && bandMesh != null) Destroy(bandMesh);

            GameObject bands = new GameObject("Bands", typeof(MeshFilter), typeof(MeshRenderer));
            bands.layer = gameObject.layer;
            bands.transform.SetParent(transform, false);

            bandMesh = BuildBandMesh();
            ownsBandMesh = true;
            bands.GetComponent<MeshFilter>().sharedMesh = bandMesh;

            bandRenderer = bands.GetComponent<MeshRenderer>();
            bandRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bandRenderer.receiveShadows = false;
            bandRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            bandRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            // submesh 0 is the ribbons, submesh 1 (when there is a halo material)
            // the glow under them - one mesh, one renderer, two materials
            Material m = ActiveBandMaterial();
            Material h = ActiveHaloMaterial();
            if (bandMesh.subMeshCount > 1 && h != null) bandRenderer.sharedMaterials = new Material[] { m, h };
            else if (m != null) bandRenderer.sharedMaterial = m;
        }

        /// <summary>
        /// Which baked bands this sphere draws, and what colour each gets. Without
        /// a skin that is all of them in their baked colours - the hero.
        /// </summary>
        private void SelectBands(List<int> indices, List<Color> colours, out float widthMul)
        {
            widthMul = widthScale;
            NeonBandData.Band[] all = NeonBandData.Bands;

            if (skin == null)
            {
                for (int i = 0; i < all.Length; i++) { indices.Add(i); colours.Add(all[i].Colour); }
                return;
            }

            widthMul *= skin.WidthScale;

            // ordinals are per family, so a recipe's "every other band" reads the
            // same whether or not the crossing family is switched off
            int mainCount = 0, crossCount = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (!skin.Include(all[i], i)) continue;
                indices.Add(i);
                if (all[i].Family == 0) mainCount++; else crossCount++;
            }

            int mainSeen = 0, crossSeen = 0;
            for (int k = 0; k < indices.Count; k++)
            {
                NeonBandData.Band b = all[indices[k]];
                SphereSkinCatalog.BandCtx ctx = new SphereSkinCatalog.BandCtx();
                ctx.Cross = b.Family != 0;
                ctx.Base = baseColour;
                if (ctx.Cross)
                {
                    ctx.Ordinal = crossSeen++;
                    ctx.T = crossCount > 1 ? ctx.Ordinal / (float)(crossCount - 1) : 0f;
                }
                else
                {
                    ctx.Ordinal = mainSeen++;
                    ctx.T = mainCount > 1 ? ctx.Ordinal / (float)(mainCount - 1) : 0f;
                }
                Color c = skin.Colour(ctx);
                c.a = 1f;
                colours.Add(c);
            }
        }

        private Mesh BuildBandMesh()
        {
            List<Vector3> verts = new List<Vector3>(16384);
            List<Color> cols = new List<Color>(16384);
            List<Vector2> uvs = new List<Vector2>(16384);
            List<int> bandTris = new List<int>(16384);
            List<int> haloTris = new List<int>(16384);

            List<int> chosen = new List<int>(NeonBandData.Bands.Length);
            List<Color> chosenColours = new List<Color>(NeonBandData.Bands.Length);
            float widthMul;
            SelectBands(chosen, chosenColours, out widthMul);

            for (int k = 0; k < chosen.Count; k++)
            {
                NeonBandData.Band band = NeonBandData.Bands[chosen[k]];
                float half = band.Width * widthMul;
                AddRibbon(band, chosenColours[k], half, NeonBandData.Lift, verts, cols, uvs, bandTris);
            }

            if (haloMaterial != null)
            {
                // sits between the core and the ribbons: above the core so the
                // depth test hides the far side, below the ribbons so the hot
                // line always covers its own glow
                float haloLift = Mathf.Lerp(1f, NeonBandData.Lift, 0.6f);
                for (int k = 0; k < chosen.Count; k++)
                {
                    NeonBandData.Band band = NeonBandData.Bands[chosen[k]];
                    float half = band.Width * widthMul * haloWidthScale + haloWidthAdd;
                    AddRibbon(band, chosenColours[k], half, haloLift, verts, cols, uvs, haloTris);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = skin == null ? "NeonBands" : "NeonBands (" + skin.Name + ")";
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = haloTris.Count > 0 ? 2 : 1;
            mesh.SetTriangles(bandTris, 0);
            if (haloTris.Count > 0) mesh.SetTriangles(haloTris, 1);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);   // keep it readable; the mirror re-draws it
            return mesh;
        }

        /// <summary>
        /// One ribbon of half-width `half` along a band's ring, its three edges
        /// placed ON the sphere at `lift` so it curves with the surface instead
        /// of cutting through it.
        /// </summary>
        private void AddRibbon(NeonBandData.Band band, Color colour, float half, float lift,
                               List<Vector3> verts, List<Color> cols, List<Vector2> uvs, List<int> tris)
        {
            Vector3 axis = (band.Family == 0 ? NeonBandData.AxisMain
                                             : NeonBandData.AxisCross).normalized;
            Vector3 u, v;
            Basis(axis, out u, out v);

            float ringR = Mathf.Sin(band.Theta);
            Vector3 centre = axis * Mathf.Cos(band.Theta);
            int seg = Mathf.Max(24, Mathf.RoundToInt(NeonBandData.Segments * Mathf.Max(ringR, 0.15f)));
            int baseIndex = verts.Count;

            for (int i = 0; i <= seg; i++)
            {
                float t = (i / (float)seg) * Mathf.PI * 2f;
                Vector3 p = centre + ringR * (Mathf.Cos(t) * u + Mathf.Sin(t) * v);
                Vector3 n = p.normalized;                       // outward from the sphere
                Vector3 tangent = (-Mathf.Sin(t) * u + Mathf.Cos(t) * v).normalized;
                Vector3 across = Vector3.Cross(tangent, n).normalized;

                AddVert(verts, cols, uvs, (n - across * half).normalized, colour, 0f, lift);
                AddVert(verts, cols, uvs, n, colour, 0.5f, lift);
                AddVert(verts, cols, uvs, (n + across * half).normalized, colour, 1f, lift);
            }

            for (int i = 0; i < seg; i++)
            {
                int a = baseIndex + i * 3;
                int c = a + 3;
                // two quads: edge -> centre, centre -> edge
                tris.Add(a); tris.Add(a + 1); tris.Add(c + 1);
                tris.Add(a); tris.Add(c + 1); tris.Add(c);
                tris.Add(a + 1); tris.Add(a + 2); tris.Add(c + 2);
                tris.Add(a + 1); tris.Add(c + 2); tris.Add(c + 1);
            }
        }

        private void AddVert(List<Vector3> verts, List<Color> cols, List<Vector2> uvs,
                             Vector3 dir, Color colour, float across, float lift)
        {
            verts.Add(dir * radius * lift);
            cols.Add(colour);
            uvs.Add(new Vector2(0f, across));
        }

        private static void Basis(Vector3 n, out Vector3 u, out Vector3 v)
        {
            Vector3 a = Mathf.Abs(n.z) < 0.9f ? Vector3.forward : Vector3.up;
            u = Vector3.Cross(n, a).normalized;
            v = Vector3.Cross(n, u);
        }
    }
}
