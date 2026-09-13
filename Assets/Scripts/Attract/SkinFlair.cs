using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The extra pieces a skin hangs on a NeonBandSphere - rings, hoops, jets,
    /// shards, orbiting motes, glow shells, particles, arcs - and the per-frame
    /// animation of them and of the ribbons (flicker, pulse, hue cycling).
    ///
    /// NeonBandSphere.ApplySkin attaches one of these to itself; everything it
    /// builds lives under a "Flair" child so a re-skin wipes and rebuilds it.
    /// Sizes are all relative to the host's radius, so the same code dresses the
    /// shop preview, the count selector and the in-game spheres.
    /// </summary>
    [AddComponentMenu("TubityX/Skin Flair")]
    public class SkinFlair : MonoBehaviour
    {
        private const string RootName = "Flair";

        private NeonBandSphere host;
        private SphereSkinCatalog.Skin skin;
        private Color baseColour;
        private Color tint;
        private float R;
        private float seed;

        private Transform root;
        private readonly List<Material> owned = new List<Material>();

        // animated parts
        private readonly List<Transform> hoops = new List<Transform>();
        private readonly List<Vector3> hoopAxes = new List<Vector3>();
        private readonly List<float> hoopSpeeds = new List<float>();
        private readonly List<Transform> motes = new List<Transform>();
        private readonly List<Vector3> moteAxes = new List<Vector3>();
        private readonly List<float> moteAngles = new List<float>();
        private readonly List<float> moteSpeeds = new List<float>();
        private readonly List<LineRenderer> arcs = new List<LineRenderer>();
        private readonly List<float> arcTimers = new List<float>();
        private readonly List<bool> arcLive = new List<bool>();
        private Transform saturn;

        private float baseIntensity, baseHaloIntensity;

        public static void Attach(NeonBandSphere host)
        {
            SkinFlair f = host.GetComponent<SkinFlair>();
            if (f == null) f = host.gameObject.AddComponent<SkinFlair>();
            f.Rebuild(host);
        }

        // ------------------------------------------------------------ build

        public void Rebuild(NeonBandSphere newHost)
        {
            host = newHost;
            Clear();

            skin = host.Skin;
            if (skin == null || skin.Flair == SphereSkinCatalog.Flair.None) return;

            baseColour = host.BaseColour;
            tint = SphereSkinCatalog.Resolve(skin.FlairTint, baseColour);
            R = host.radius;
            seed = (float)(GetInstanceID() & 0xffff);

            root = new GameObject(RootName).transform;
            root.gameObject.layer = gameObject.layer;
            root.SetParent(transform, false);

            if (host.SkinBandMaterial != null && host.SkinBandMaterial.HasProperty("_Intensity"))
                baseIntensity = host.SkinBandMaterial.GetFloat("_Intensity");
            if (host.SkinHaloMaterial != null && host.SkinHaloMaterial.HasProperty("_Intensity"))
                baseHaloIntensity = host.SkinHaloMaterial.GetFloat("_Intensity");

            SphereSkinCatalog.Flair f = skin.Flair;
            if (Has(f, SphereSkinCatalog.Flair.Corona))      BuildShell("Corona", 1.45f, 1f, 0.5f, 1.4f, tint);
            if (Has(f, SphereSkinCatalog.Flair.Glass))       BuildShell("Glass", 1.045f, 0f, 1.6f, 2.2f, tint);
            if (Has(f, SphereSkinCatalog.Flair.SaturnRing))  BuildSaturnRing();
            if (Has(f, SphereSkinCatalog.Flair.GyroHoops))   BuildGyroHoops();
            if (Has(f, SphereSkinCatalog.Flair.Motes))       BuildMotes(3);
            if (Has(f, SphereSkinCatalog.Flair.PolarJets))   BuildPolarJets();
            if (Has(f, SphereSkinCatalog.Flair.Shards))      BuildShards(10);
            if (Has(f, SphereSkinCatalog.Flair.CounterSpin)) BuildCounterSpin();
            if (Has(f, SphereSkinCatalog.Flair.Embers))      BuildParticles("Embers", 0);
            if (Has(f, SphereSkinCatalog.Flair.Wisps))       BuildParticles("Wisps", 1);
            if (Has(f, SphereSkinCatalog.Flair.Sparkle))     BuildParticles("Sparkle", 2);
            if (Has(f, SphereSkinCatalog.Flair.Arcs))        BuildArcs(3);
        }

        private static bool Has(SphereSkinCatalog.Flair f, SphereSkinCatalog.Flair bit) { return (f & bit) != 0; }

        private void Clear()
        {
            Transform old = transform.Find(RootName);
            if (old != null) DestroyImmediate(old.gameObject);
            root = null;
            for (int i = 0; i < owned.Count; i++) if (owned[i] != null) Destroy(owned[i]);
            owned.Clear();
            hoops.Clear(); hoopAxes.Clear(); hoopSpeeds.Clear();
            motes.Clear(); moteAxes.Clear(); moteAngles.Clear(); moteSpeeds.Clear();
            arcs.Clear(); arcTimers.Clear(); arcLive.Clear();
            saturn = null;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < owned.Count; i++) if (owned[i] != null) Destroy(owned[i]);
        }

        // ---- materials -------------------------------------------------------

        private Material ShellMaterial(Color colour, float intensity, float centreWeight, float rimPower, float centrePower)
        {
            Material src = Resources.Load<Material>("Attract/Mat_NeonShell");
            Material m = src != null ? new Material(src) : null;
            if (m == null)
            {
                Shader sh = Shader.Find("TubityX/NeonShell");
                if (sh == null) return null;
                m = new Material(sh);
            }
            m.SetColor("_Colour", colour);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_CentreWeight", centreWeight);
            m.SetFloat("_RimPower", rimPower);
            m.SetFloat("_CentrePower", centrePower);
            owned.Add(m);
            return m;
        }

        private Material ParticleMaterial(float intensity)
        {
            Material src = Resources.Load<Material>("Attract/Mat_NeonParticle");
            Material m = src != null ? new Material(src) : null;
            if (m == null)
            {
                Shader sh = Shader.Find("TubityX/NeonParticle");
                if (sh == null) return null;
                m = new Material(sh);
            }
            m.SetFloat("_Intensity", intensity);
            owned.Add(m);
            return m;
        }

        /// <summary>Ring-shaped pieces share the host's ribbon material so they pulse and hue-cycle with it.</summary>
        private Material RibbonMaterial()
        {
            return host.SkinBandMaterial != null ? host.SkinBandMaterial : host.bandMaterial;
        }

        private static void Dress(MeshRenderer r)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private GameObject Child(string name)
        {
            GameObject go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(root, false);
            return go;
        }

        private GameObject MeshChild(string name, Mesh mesh, Material mat)
        {
            GameObject go = Child(name);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Dress(mr);
            if (mat != null) mr.sharedMaterial = mat;
            return go;
        }

        // ---- pieces ----------------------------------------------------------

        private void BuildShell(string name, float scale, float centreWeight, float intensity, float power, Color colour)
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = name;
            shell.layer = gameObject.layer;
            shell.transform.SetParent(root, false);
            shell.transform.localScale = Vector3.one * R * 2f * scale;
            DestroyImmediate(shell.GetComponent<Collider>());
            MeshRenderer mr = shell.GetComponent<MeshRenderer>();
            Dress(mr);
            Material m = ShellMaterial(colour, intensity, centreWeight, power, power);
            if (m != null) mr.sharedMaterial = m;
        }

        private void BuildSaturnRing()
        {
            Mesh ring = RingMesh(R * 1.38f, R * 1.9f, 96, tint, baseColour);
            GameObject go = MeshChild("SaturnRing", ring, RibbonMaterial());
            go.transform.localRotation = Quaternion.Euler(62f, 0f, 18f);
            saturn = go.transform;
        }

        private void BuildGyroHoops()
        {
            Color gold = SphereSkinCatalog.Resolve(SphereSkinCatalog.Tint.Gold, baseColour);
            AddHoop("Hoop_A", R * 1.22f, R * 1.31f, tint, new Vector3(1f, 0.2f, 0f).normalized, 55f, Quaternion.Euler(80f, 0f, 0f));
            AddHoop("Hoop_B", R * 1.44f, R * 1.51f, baseColour, new Vector3(0f, 0.3f, 1f).normalized, -38f, Quaternion.Euler(10f, 0f, 70f));
            AddHoop("Hoop_C", R * 1.64f, R * 1.69f, gold, new Vector3(0.6f, 1f, 0.2f).normalized, 24f, Quaternion.Euler(45f, 45f, 0f));
        }

        private void AddHoop(string name, float inner, float outer, Color colour, Vector3 axis, float speed, Quaternion start)
        {
            Mesh ring = RingMesh(inner, outer, 96, colour, colour);
            GameObject go = MeshChild(name, ring, RibbonMaterial());
            go.transform.localRotation = start;
            hoops.Add(go.transform);
            hoopAxes.Add(axis);
            hoopSpeeds.Add(speed);
        }

        private void BuildMotes(int count)
        {
            Material m = ShellMaterial(tint, 4f, 1f, 1f, 0.8f);
            for (int i = 0; i < count; i++)
            {
                GameObject mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mote.name = "Mote_" + i;
                mote.layer = gameObject.layer;
                mote.transform.SetParent(root, false);
                mote.transform.localScale = Vector3.one * R * 0.19f;
                DestroyImmediate(mote.GetComponent<Collider>());
                MeshRenderer mr = mote.GetComponent<MeshRenderer>();
                Dress(mr);
                if (m != null) mr.sharedMaterial = m;

                float a = (i / (float)count) * Mathf.PI * 2f + seed * 0.01f;
                Vector3 axis = Quaternion.Euler(0f, i * 120f, 35f + i * 20f) * Vector3.up;
                motes.Add(mote.transform);
                moteAxes.Add(axis.normalized);
                moteAngles.Add(a);
                moteSpeeds.Add((1.4f + 0.35f * i) * (i % 2 == 0 ? 1f : -1f));
            }
            PlaceMotes();
        }

        private void PlaceMotes()
        {
            for (int i = 0; i < motes.Count; i++)
            {
                Vector3 axis = moteAxes[i];
                Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.z) < 0.9f ? Vector3.forward : Vector3.up).normalized;
                Vector3 v = Vector3.Cross(axis, u);
                float orbit = R * (1.55f + 0.18f * i);
                motes[i].localPosition = (Mathf.Cos(moteAngles[i]) * u + Mathf.Sin(moteAngles[i]) * v) * orbit;
            }
        }

        private void BuildPolarJets()
        {
            Mesh jets = JetMesh(R, tint);
            MeshChild("PolarJets", jets, RibbonMaterial());
        }

        private void BuildShards(int count)
        {
            Mesh shards = ShardMesh(count, R, baseColour);
            GameObject go = MeshChild("Shards", shards, RibbonMaterial());
            go.transform.localRotation = Quaternion.Euler(0f, seed, 0f);
        }

        private void BuildCounterSpin()
        {
            GameObject go = Child("CounterBands");
            go.SetActive(false);
            NeonBandSphere counter = go.AddComponent<NeonBandSphere>();
            counter.buildOnAwake = false;
            counter.buildCore = false;
            counter.registerForMirror = false;
            counter.radius = R * 1.035f;
            counter.widthScale = host.widthScale;
            counter.spin = -host.spin * 1.6f;
            counter.bandMaterial = host.bandMaterial;
            counter.haloMaterial = host.haloMaterial;
            counter.haloWidthScale = host.haloWidthScale;
            counter.haloWidthAdd = host.haloWidthAdd;

            Color c = tint;
            SphereSkinCatalog.Skin s = skin;
            SphereSkinCatalog.Skin derived = new SphereSkinCatalog.Skin
            {
                Name = s.Name + " (counter)",
                Include = (b, i) => b.Family == 1,
                Colour = ctx => c,
                WidthScale = s.WidthScale * 1.35f,
                Intensity = s.Intensity,
                CoreWhite = s.CoreWhite,
            };
            counter.ApplySkin(derived, baseColour);
            go.SetActive(true);
        }

        /// <summary>kind: 0 embers, 1 wisps, 2 sparkle.</summary>
        private void BuildParticles(string name, int kind)
        {
            GameObject go = Child(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            ParticleSystem.EmissionModule em = ps.emission;
            ParticleSystem.ShapeModule shape = ps.shape;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;

            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 120;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radiusThickness = 0.15f;

            Gradient g = new Gradient();
            Color c = tint;
            float intensity = 1.5f;

            if (kind == 0)          // embers: hot, drifting up and out, world space so they trail
            {
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(R * 0.6f, R * 1.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(R * 0.07f, R * 0.16f);
                main.gravityModifier = -0.25f;
                em.rateOverTime = 28f;
                shape.radius = R * 1.02f;
                intensity = 2.6f;
                g.SetKeys(
                    new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.5f), 0f), new GradientColorKey(c, 0.4f), new GradientColorKey(c * 0.4f, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            }
            else if (kind == 1)     // wisps: slow, large, faint
            {
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(R * 0.08f, R * 0.25f);
                main.startSize = new ParticleSystem.MinMaxCurve(R * 0.5f, R * 0.9f);
                em.rateOverTime = 9f;
                shape.radius = R * 1.1f;
                intensity = 0.35f;
                g.SetKeys(
                    new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.35f), new GradientAlphaKey(0f, 1f) });
            }
            else                    // sparkle: tiny, brief twinkles held on the surface
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(R * 0.08f, R * 0.2f);
                em.rateOverTime = 22f;
                shape.radius = R * 1.08f;
                shape.radiusThickness = 0.05f;
                intensity = 4f;
                g.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(c, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            }

            col.enabled = true;
            col.color = g;
            size.enabled = true;
            AnimationCurve sc = new AnimationCurve(new Keyframe(0f, kind == 2 ? 0.2f : 1f), new Keyframe(0.3f, 1f), new Keyframe(1f, kind == 1 ? 1.4f : 0.1f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sc);

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            Material m = ParticleMaterial(intensity);
            if (m != null) r.sharedMaterial = m;

            ps.Play(true);
        }

        private void BuildArcs(int count)
        {
            Material m = ParticleMaterial(3.5f);
            for (int i = 0; i < count; i++)
            {
                GameObject go = Child("Arc_" + i);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 10;
                lr.widthMultiplier = R * 0.045f;
                lr.numCapVertices = 2;
                lr.alignment = LineAlignment.View;
                lr.textureMode = LineTextureMode.Stretch;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                Gradient g = new Gradient();
                Color c = Color.Lerp(tint, Color.white, 0.55f);
                g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                          new[] { new GradientAlphaKey(0.2f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0.2f, 1f) });
                lr.colorGradient = g;
                if (m != null) lr.sharedMaterial = m;
                lr.enabled = false;
                arcs.Add(lr);
                arcTimers.Add(Random.Range(0.05f, 0.4f));
                arcLive.Add(false);
            }
        }

        // ---- meshes ----------------------------------------------------------

        /// <summary>A flat annulus in the XZ plane, three vertices across so the ribbon shader profiles it.</summary>
        private static Mesh RingMesh(float inner, float outer, int segs, Color colourA, Color colourB)
        {
            List<Vector3> v = new List<Vector3>((segs + 1) * 3);
            List<Color> c = new List<Color>((segs + 1) * 3);
            List<Vector2> uv = new List<Vector2>((segs + 1) * 3);
            List<int> t = new List<int>(segs * 12);
            float mid = (inner + outer) * 0.5f;
            for (int i = 0; i <= segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Color col = Color.Lerp(colourA, colourB, 0.5f + 0.5f * Mathf.Sin(a * 2f));
                v.Add(d * inner); c.Add(col); uv.Add(new Vector2(0f, 0f));
                v.Add(d * mid);   c.Add(col); uv.Add(new Vector2(0f, 0.5f));
                v.Add(d * outer); c.Add(col); uv.Add(new Vector2(0f, 1f));
            }
            for (int i = 0; i < segs; i++)
            {
                int a = i * 3, b = a + 3;
                t.Add(a); t.Add(a + 1); t.Add(b + 1);
                t.Add(a); t.Add(b + 1); t.Add(b);
                t.Add(a + 1); t.Add(a + 2); t.Add(b + 2);
                t.Add(a + 1); t.Add(b + 2); t.Add(b + 1);
            }
            return Finish("FlairRing", v, c, uv, t);
        }

        /// <summary>Two crossed tapered ribbons out of each pole, fading to nothing along their length.</summary>
        private static Mesh JetMesh(float R, Color colour)
        {
            List<Vector3> v = new List<Vector3>();
            List<Color> c = new List<Color>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            const int steps = 10;
            float len = R * 1.35f;
            for (int pole = -1; pole <= 1; pole += 2)
            {
                for (int cross = 0; cross < 2; cross++)
                {
                    Vector3 across = cross == 0 ? Vector3.right : Vector3.forward;
                    int baseIndex = v.Count;
                    for (int i = 0; i <= steps; i++)
                    {
                        float f = i / (float)steps;
                        float y = pole * (R * 0.98f + len * f);
                        float w = Mathf.Lerp(R * 0.30f, R * 0.02f, f);
                        Color col = colour * Mathf.Pow(1f - f, 1.6f);
                        Vector3 p = new Vector3(0f, y, 0f);
                        v.Add(p - across * w); c.Add(col); uv.Add(new Vector2(f, 0f));
                        v.Add(p);              c.Add(col); uv.Add(new Vector2(f, 0.5f));
                        v.Add(p + across * w); c.Add(col); uv.Add(new Vector2(f, 1f));
                    }
                    for (int i = 0; i < steps; i++)
                    {
                        int a = baseIndex + i * 3, b = a + 3;
                        t.Add(a); t.Add(a + 1); t.Add(b + 1);
                        t.Add(a); t.Add(b + 1); t.Add(b);
                        t.Add(a + 1); t.Add(a + 2); t.Add(b + 2);
                        t.Add(a + 1); t.Add(b + 2); t.Add(b + 1);
                    }
                }
            }
            return Finish("FlairJets", v, c, uv, t);
        }

        /// <summary>Crystal spikes standing off the surface, hues a third of the wheel apart.</summary>
        private static Mesh ShardMesh(int count, float R, Color baseColour)
        {
            List<Vector3> v = new List<Vector3>();
            List<Color> c = new List<Color>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            System.Random rng = new System.Random(20260910);
            for (int i = 0; i < count; i++)
            {
                // roughly even spread: golden-angle spiral over the sphere
                float y = 1f - (i + 0.5f) / count * 2f;
                float r = Mathf.Sqrt(1f - y * y);
                float phi = i * 2.399963f;
                Vector3 n = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
                Vector3 u = Vector3.Cross(n, Mathf.Abs(n.z) < 0.9f ? Vector3.forward : Vector3.up).normalized;
                Vector3 w = Vector3.Cross(n, u);
                float len = R * (0.32f + 0.30f * (float)rng.NextDouble());
                float half = R * (0.045f + 0.03f * (float)rng.NextDouble());
                Color col = SphereSkinCatalog.Shift(baseColour, 120f * (i % 3));
                Color dim = col * 0.55f;

                Vector3 basePos = n * R * 0.96f;
                Vector3 apex = n * (R * 0.96f + len);
                int b = v.Count;
                v.Add(basePos + u * half); c.Add(dim); uv.Add(new Vector2(0f, 0f));
                v.Add(basePos + w * half); c.Add(dim); uv.Add(new Vector2(0f, 1f));
                v.Add(basePos - u * half); c.Add(dim); uv.Add(new Vector2(0f, 0f));
                v.Add(basePos - w * half); c.Add(dim); uv.Add(new Vector2(0f, 1f));
                v.Add(apex);               c.Add(col); uv.Add(new Vector2(1f, 0.5f));
                for (int k = 0; k < 4; k++)
                {
                    t.Add(b + k); t.Add(b + 4); t.Add(b + (k + 1) % 4);
                    t.Add(b + (k + 1) % 4); t.Add(b + 4); t.Add(b + k);   // both faces, ribbons cull off anyway
                }
            }
            return Finish("FlairShards", v, c, uv, t);
        }

        private static Mesh Finish(string name, List<Vector3> v, List<Color> c, List<Vector2> uv, List<int> t)
        {
            Mesh m = new Mesh();
            m.name = name;
            m.SetVertices(v);
            m.SetColors(c);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------ animate

        private void Update()
        {
            if (skin == null || root == null) return;
            float dt = Time.deltaTime;
            float time = Time.time + seed;
            SphereSkinCatalog.Flair f = skin.Flair;

            // ribbon intensity: flicker, pulse
            if (Has(f, SphereSkinCatalog.Flair.Flicker) || Has(f, SphereSkinCatalog.Flair.Pulse))
            {
                float k = 1f;
                if (Has(f, SphereSkinCatalog.Flair.Pulse))
                    k *= 1f + 0.22f * Mathf.Sin(time * 2.4f);
                if (Has(f, SphereSkinCatalog.Flair.Flicker))
                    k *= 1f + 0.14f * (Mathf.PerlinNoise(time * 9f, seed) - 0.5f) * 2f
                            + 0.06f * Mathf.Sin(time * 37f);
                SetIntensity(k);
            }

            if (Has(f, SphereSkinCatalog.Flair.HueCycle))
            {
                float shift = time * 0.7f;
                if (host.SkinBandMaterial != null) host.SkinBandMaterial.SetFloat("_HueShift", shift);
                if (host.SkinHaloMaterial != null) host.SkinHaloMaterial.SetFloat("_HueShift", shift);
            }

            if (saturn != null) saturn.Rotate(Vector3.up, 9f * dt, Space.Self);

            for (int i = 0; i < hoops.Count; i++)
                hoops[i].Rotate(hoopAxes[i], hoopSpeeds[i] * dt, Space.World);

            if (motes.Count > 0)
            {
                for (int i = 0; i < motes.Count; i++) moteAngles[i] += moteSpeeds[i] * dt;
                PlaceMotes();
            }

            for (int i = 0; i < arcs.Count; i++) TickArc(i, dt);
        }

        private void SetIntensity(float k)
        {
            if (host.SkinBandMaterial != null) host.SkinBandMaterial.SetFloat("_Intensity", baseIntensity * k);
            if (host.SkinHaloMaterial != null) host.SkinHaloMaterial.SetFloat("_Intensity", baseHaloIntensity * k);
        }

        private void TickArc(int i, float dt)
        {
            arcTimers[i] -= dt;
            LineRenderer lr = arcs[i];
            if (arcTimers[i] > 0f)
            {
                if (arcLive[i]) JitterArc(lr);
                return;
            }
            arcLive[i] = !arcLive[i];
            lr.enabled = arcLive[i];
            if (arcLive[i])
            {
                StrikeArc(lr);
                arcTimers[i] = Random.Range(0.08f, 0.22f);
            }
            else
            {
                arcTimers[i] = Random.Range(0.12f, 0.6f);
            }
        }

        private Vector3[] arcAnchor = new Vector3[10];

        private void StrikeArc(LineRenderer lr)
        {
            Vector3 a = Random.onUnitSphere;
            Vector3 b = (a + Random.onUnitSphere * 0.9f).normalized;
            int n = lr.positionCount;
            for (int k = 0; k < n; k++)
            {
                float f = k / (float)(n - 1);
                Vector3 p = Vector3.Slerp(a, b, f).normalized * R * 1.06f;
                arcAnchor[k] = p;
            }
            JitterArc(lr);
        }

        private void JitterArc(LineRenderer lr)
        {
            int n = lr.positionCount;
            for (int k = 0; k < n; k++)
            {
                float end = (k == 0 || k == n - 1) ? 0f : 1f;
                Vector3 p = arcAnchor[k] + Random.insideUnitSphere * R * 0.06f * end;
                lr.SetPosition(k, p);
            }
        }
    }
}
