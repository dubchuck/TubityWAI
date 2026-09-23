using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TubityWAI
{
    /// <summary>One named body on a celestial route: a planet, a moon's host, or a moon.</summary>
    public class CelestialBody
    {
        public string name;

        /// <summary>Distance along the tube, in tube units, where the body sits abeam the player.</summary>
        public float distance;

        /// <summary>Lateral offset from the tube axis. Its magnitude is what sets how big the body gets
        /// before it swings out of frame: a body is on screen roughly while it is further ahead than
        /// it is off to the side, so peak apparent size is about atan(radius / 1.41 * |offset|).</summary>
        public Vector2 offset;

        public float radius = 50f;

        /// <summary>Band colours for ProceduralTextures.Bands - dark, mid, shadow, highlight.</summary>
        public Color[] bands;
        public int bandCount = 7;

        /// <summary>
        /// Emission taken off the body's own albedo. Flying sunward means every body ahead is backlit,
        /// so a little self-glow is what keeps the unlit limb from reading as a hole in the star field.
        /// </summary>
        public float selfGlow = 0.16f;

        public float smoothness = 0.25f;
        public float spinSpeed = 2f;        // degrees/sec about the body's own axis
        public float axialTilt = 15f;

        /// <summary>A translucent shell a few percent larger than the body. Clear alpha skips it.</summary>
        public Color atmosphere = Color.clear;

        public CelestialRing ring;
        public List<CelestialMoon> moons;

        public CelestialBody WithRing(CelestialRing r) { ring = r; return this; }

        public CelestialBody WithMoon(CelestialMoon m)
        {
            if (moons == null) moons = new List<CelestialMoon>();
            moons.Add(m);
            return this;
        }
    }

    /// <summary>A ring system, as a flat annulus in the body's tilted equatorial plane.</summary>
    public class CelestialRing
    {
        public float innerScale = 1.4f;     // multiples of the body radius
        public float outerScale = 2.3f;
        public Color inner = Color.white;
        public Color outer = Color.white;
        public float opacity = 0.75f;
        public int segments = 64;
    }

    /// <summary>A small body orbiting its host in the host's tilted equatorial plane.</summary>
    public class CelestialMoon
    {
        public string name;
        public float radius = 6f;
        public float orbitRadius = 90f;
        public float orbitPhase;            // degrees
        public float orbitSpeed = 3f;       // degrees/sec
        public Color tint = Color.white;
        public float selfGlow = 0.12f;
    }

    /// <summary>
    /// Spins a body about its own (already tilted) up axis and, for a moon, walks it around its host.
    /// Deliberately not SceneryDrift: that works in the tube's cylindrical frame so props can never
    /// clip the wall, which is exactly wrong for something hundreds of units outside it.
    /// </summary>
    public class CelestialSpin : MonoBehaviour
    {
        public float spinSpeed;
        public float orbitRadius;
        public float orbitSpeed;
        public float orbitAngle;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (spinSpeed != 0f) transform.Rotate(Vector3.up, spinSpeed * dt, Space.Self);

            if (orbitRadius > 0f)
            {
                orbitAngle += orbitSpeed * dt;
                float a = orbitAngle * Mathf.Deg2Rad;
                // Orbit in the parent's XZ plane; the parent carries the axial tilt, so a ring system
                // and its shepherd moons share one plane without either needing to know about it.
                transform.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * orbitRadius;
            }
        }
    }

    /// <summary>
    /// Builds the named bodies of a celestial route as plain world-space objects, once, at level start.
    ///
    /// They deliberately sit outside the tunnel's segment pool. Segments only ever extend ~120 units
    /// ahead of the camera, so anything parented to one cannot be seen until it is almost on top of
    /// the player - the opposite of what a planet needs. These are placed at absolute Z, live for the
    /// whole level, and are revealed by fog alone: the route's long fogEnd turns distance into haze,
    /// which is what makes the gaps between planets read as gaps rather than as dead air.
    ///
    /// Cost is a handful of objects for the entire level - roughly a dozen renderers sharing a few
    /// materials - so it is cheaper than one segment's worth of trackside props.
    /// </summary>
    public class CelestialBodies : MonoBehaviour
    {
        public static CelestialBodies Current { get; private set; }

        private readonly List<Material> owned = new List<Material>();

        /// <summary>Removes the bodies built for a previous level.</summary>
        public static void Clear()
        {
            if (Current != null)
            {
                Destroy(Current.gameObject);
                Current = null;
            }
        }

        /// <summary>
        /// Spawns every body in <paramref name="bodies"/>. The camera's far clip is pushed out to cover
        /// the furthest one, because the default 1000 would cull a planet the fog is still fading in.
        /// </summary>
        public static CelestialBodies Create(IList<CelestialBody> bodies, Camera cam)
        {
            Clear();
            if (bodies == null || bodies.Count == 0) return null;

            GameObject root = new GameObject("CelestialBodies");
            CelestialBodies host = root.AddComponent<CelestialBodies>();

            float furthest = 0f;
            for (int i = 0; i < bodies.Count; i++)
            {
                CelestialBody body = bodies[i];
                if (body == null) continue;
                host.Build(body, root.transform);
                furthest = Mathf.Max(furthest, body.distance + body.radius * 4f);
            }

            // Only ever widened, never narrowed, so this cannot undercut whatever else set it. The
            // scene is reloaded between levels (GameManager), which is what puts it back afterwards.
            if (cam != null) cam.farClipPlane = Mathf.Max(cam.farClipPlane, furthest + 500f);

            Current = host;
            return host;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null) Destroy(owned[i]);
            }
            owned.Clear();
        }

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------
        private void Build(CelestialBody body, Transform parent)
        {
            // The pivot carries the axial tilt, so the body's spin, its rings and its moons all share
            // one tilted frame. Uranus rolls on its side purely by setting axialTilt to 98.
            GameObject pivot = new GameObject(body.name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = new Vector3(body.offset.x, body.offset.y, body.distance);
            pivot.transform.localRotation = Quaternion.Euler(body.axialTilt, 0f, 0f);

            ProceduralTextures.TexSet tex = ProceduralTextures.Bands(
                Mathf.Abs(body.name.GetHashCode()) % 97 + 1, body.bands, body.bandCount);

            Material surface = BodyMaterial(body.name + "_surface", tex, body.selfGlow, body.smoothness);

            GameObject globe = Sphere(body.name + "_globe", pivot.transform, Vector3.zero, body.radius * 2f, surface);
            CelestialSpin spin = globe.AddComponent<CelestialSpin>();
            spin.spinSpeed = body.spinSpeed;

            if (body.atmosphere.a > 0.001f) BuildAtmosphere(body, pivot.transform);
            if (body.ring != null) BuildRing(body, pivot.transform);

            if (body.moons != null)
            {
                for (int i = 0; i < body.moons.Count; i++) BuildMoon(body.moons[i], pivot.transform);
            }
        }

        /// <summary>
        /// A translucent shell a few percent proud of the surface. It is a plain sphere rather than a
        /// fresnel shader because at these distances the limb is only a few pixels wide - the shell
        /// reads as a haze against the stars, which is all it needs to do, for one extra draw.
        /// </summary>
        private void BuildAtmosphere(CelestialBody body, Transform pivot)
        {
            Material mat = NewMaterial(body.name + "_atmosphere");
            Color tint = body.atmosphere;
            MakeTransparent(mat);
            SetColor(mat, new Color(tint.r, tint.g, tint.b, tint.a));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(tint.r, tint.g, tint.b) * 0.8f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);

            Sphere(body.name + "_atmosphere", pivot, Vector3.zero, body.radius * 2.06f, mat);
        }

        private void BuildRing(CelestialBody body, Transform pivot)
        {
            CelestialRing ring = body.ring;
            ProceduralTextures.TexSet tex = ProceduralTextures.RingBands(
                Mathf.Abs(body.name.GetHashCode()) % 89 + 3, ring.inner, ring.outer);

            Material mat = NewMaterial(body.name + "_ring");
            MakeTransparent(mat);
            SetColor(mat, new Color(1f, 1f, 1f, ring.opacity));
            BindAlbedo(mat, tex.albedo, 1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", tex.albedo);
            // Rings are lit edge-on from a long way off; without a little emission the shadowed half
            // of the annulus disappears and the ring reads as a broken arc.
            mat.SetColor("_EmissionColor", Color.white * 0.30f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);

            // Disc() lies in XZ, which is already the pivot's tilted equatorial plane.
            Mesh mesh = ProceduralMeshes.Disc(ring.segments, ring.innerScale, ring.outerScale);
            GameObject go = new GameObject(body.name + "_ring");
            go.transform.SetParent(pivot, false);
            go.transform.localScale = Vector3.one * body.radius;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Configure(go.AddComponent<MeshRenderer>(), mat);
        }

        private void BuildMoon(CelestialMoon moon, Transform pivot)
        {
            Material mat = NewMaterial(moon.name + "_surface");
            SetColor(mat, moon.tint);
            if (moon.selfGlow > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", moon.tint * moon.selfGlow);
            }
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

            // An orbit carrier at the origin of the tilted frame; CelestialSpin walks the moon around it.
            GameObject carrier = new GameObject(moon.name);
            carrier.transform.SetParent(pivot, false);
            CelestialSpin spin = carrier.AddComponent<CelestialSpin>();
            spin.orbitRadius = moon.orbitRadius;
            spin.orbitSpeed = moon.orbitSpeed;
            spin.orbitAngle = moon.orbitPhase;
            spin.spinSpeed = 0f;

            Sphere(moon.name + "_globe", carrier.transform, Vector3.zero, moon.radius * 2f, mat);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private GameObject Sphere(string name, Transform parent, Vector3 localPos, float diameter, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * diameter;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Configure(go.GetComponent<MeshRenderer>(), mat);
            return go;
        }

        /// <summary>
        /// Landmarks neither cast nor receive shadows. The main light's shadow map only covers the
        /// first 50 units of the camera frustum, so a body hundreds of units out could not appear in
        /// it anyway - the terminator that actually matters is the lit/unlit split from N.L, which
        /// costs nothing and works at any distance.
        /// </summary>
        private static void Configure(MeshRenderer r, Material mat)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private Material BodyMaterial(string name, ProceduralTextures.TexSet tex, float glow, float smoothness)
        {
            Material mat = NewMaterial(name);
            SetColor(mat, Color.white);
            BindAlbedo(mat, tex.albedo, 1f);
            if (tex.normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", tex.normal);
                mat.SetFloat("_BumpScale", 1.2f);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (glow > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", tex.albedo);
                mat.SetColor("_EmissionColor", Color.white * glow);
            }
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            return mat;
        }

        private Material NewMaterial(string name)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            Material mat = new Material(s) { name = "Celestial_" + name };
            owned.Add(mat);
            return mat;
        }

        private static void SetColor(Material mat, Color c)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        }

        private static void BindAlbedo(Material mat, Texture tex, float tiling)
        {
            string prop = mat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            mat.SetTexture(prop, tex);
            mat.SetTextureScale(prop, Vector2.one * tiling);
        }

        private static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
