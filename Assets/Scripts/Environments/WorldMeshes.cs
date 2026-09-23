using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Meshes for the second set of worlds, alongside ProceduralMeshes: shells that wrap the tube
    /// (cave and lava-tube walls, canyon sides, the lava river), toothed gears, torus arcs (solar
    /// prominences, rock arches) and lightning bolts. Cached by their parameters like the originals,
    /// so a shape is built once however many segments use it.
    ///
    /// Angles follow the tube's own convention: 0 is straight down, increasing toward +X.
    /// </summary>
    public static class WorldMeshes
    {
        private static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        private static Mesh Cached(string key, System.Func<Mesh> build)
        {
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = build();
            m.name = key;
            cache[key] = m;
            return m;
        }

        private static Vector3 Radial(float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), -Mathf.Cos(a), 0f);
        }

        /// <summary>
        /// A rough wall around the z axis, facing inward, <paramref name="length"/> long and centred on
        /// z = 0. The roughness pulls the wall in toward the axis and eases to nothing at both ends,
        /// so consecutive shells always meet on the same clean circle whatever their roll - which is
        /// what lets one shape per segment tile into a continuous cave. A partial arc
        /// (<paramref name="arcDeg"/> under 360) makes a single wall, canyon side or river bed.
        /// <paramref name="uvAroundIsV"/> swaps the UVs so a banded texture runs up the wall (strata).
        /// </summary>
        public static Mesh Shell(int seed, float radius, float length, float roughness, float frequency,
                                 int radial = 40, int rings = 10, float arcStartDeg = 0f, float arcDeg = 360f,
                                 float uTiles = 4f, float vTiles = 1f, bool uvAroundIsV = false)
        {
            string key = $"shell_{seed}_{radius:F1}_{length:F1}_{roughness:F2}_{frequency:F2}_{radial}_{rings}_{arcStartDeg:F0}_{arcDeg:F0}_{uTiles:F1}_{vTiles:F1}_{uvAroundIsV}";
            return Cached(key, () =>
            {
                bool closed = arcDeg >= 359.9f;
                int cols = radial + 1;
                List<Vector3> v = new List<Vector3>(cols * (rings + 1));
                List<Vector2> uv = new List<Vector2>(v.Capacity);
                List<int> tris = new List<int>(radial * rings * 6);

                for (int j = 0; j <= rings; j++)
                {
                    float tz = j / (float)rings;
                    float z = (tz - 0.5f) * length;
                    // Full roughness through the middle, none at the joints.
                    float ends = Mathf.Sin(tz * Mathf.PI);
                    float taper = Mathf.Sqrt(Mathf.Clamp01(ends));

                    for (int i = 0; i <= radial; i++)
                    {
                        float ti = i / (float)radial;
                        // A closed shell reuses the first column's shape at the seam.
                        float angle = arcStartDeg + arcDeg * (closed && i == radial ? 0f : ti);
                        Vector3 dir = Radial(angle);
                        float n = ProceduralMeshes.Fbm3(new Vector3(dir.x * frequency, dir.y * frequency, z / radius * frequency * 1.5f), seed);
                        float r = radius * (1f - roughness * taper * n);
                        v.Add(dir * r + new Vector3(0f, 0f, z));

                        Vector2 t = new Vector2(ti * uTiles, tz * vTiles);
                        uv.Add(uvAroundIsV ? new Vector2(t.y, t.x) : t);
                    }
                }

                for (int j = 0; j < rings; j++)
                {
                    for (int i = 0; i < radial; i++)
                    {
                        int a = j * cols + i, b = a + 1, c = a + cols, d = c + 1;
                        // Wound so the faces point in, toward the axis.
                        tris.Add(a); tris.Add(c); tris.Add(b);
                        tris.Add(b); tris.Add(c); tris.Add(d);
                    }
                }

                Mesh m = new Mesh();
                m.SetVertices(v);
                m.SetUVs(0, uv);
                m.SetTriangles(tris, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            });
        }

        /// <summary>
        /// A toothed gear in the XY plane, turning about z, <paramref name="thickness"/> deep. Flat
        /// shaded (every face has its own vertices) so the brass catches the light on each tooth.
        /// With a large inner radius it is a ring gear the tube passes straight through.
        /// </summary>
        public static Mesh Gear(float innerRadius, float outerRadius, int teeth, float toothDepth, float thickness)
        {
            string key = $"gear_{innerRadius:F2}_{outerRadius:F2}_{teeth}_{toothDepth:F2}_{thickness:F2}";
            return Cached(key, () =>
            {
                // Outline: four points per tooth - root, rise, tip, fall.
                int n = teeth * 4;
                Vector2[] outer = new Vector2[n];
                for (int k = 0; k < n; k++)
                {
                    int phase = k % 4;
                    float a = (k / (float)n) * Mathf.PI * 2f;
                    float r = (phase == 1 || phase == 2) ? outerRadius + toothDepth : outerRadius;
                    // Taper the tooth: tips a little narrower than their roots.
                    float nudge = (phase == 1 ? 0.12f : phase == 2 ? -0.12f : 0f) * (Mathf.PI * 2f / n);
                    outer[k] = new Vector2(Mathf.Cos(a + nudge), Mathf.Sin(a + nudge)) * r;
                }

                List<Vector3> v = new List<Vector3>();
                List<Vector3> nrm = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();
                float hz = thickness * 0.5f;

                void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
                {
                    int s = v.Count;
                    v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                    for (int q = 0; q < 4; q++) nrm.Add(normal);
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                    tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                    tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
                }

                for (int k = 0; k < n; k++)
                {
                    Vector2 o0 = outer[k], o1 = outer[(k + 1) % n];
                    float a0 = (k / (float)n) * Mathf.PI * 2f, a1 = ((k + 1) / (float)n) * Mathf.PI * 2f;
                    Vector2 i0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * innerRadius;
                    Vector2 i1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * innerRadius;

                    // Front (-z) and back (+z) faces.
                    Quad(new Vector3(i0.x, i0.y, -hz), new Vector3(i1.x, i1.y, -hz), new Vector3(o1.x, o1.y, -hz), new Vector3(o0.x, o0.y, -hz), Vector3.back);
                    Quad(new Vector3(o0.x, o0.y, hz), new Vector3(o1.x, o1.y, hz), new Vector3(i1.x, i1.y, hz), new Vector3(i0.x, i0.y, hz), Vector3.forward);

                    // Outer rim (teeth).
                    Vector2 edge = o1 - o0;
                    Vector3 outN = new Vector3(edge.y, -edge.x, 0f).normalized;
                    Quad(new Vector3(o0.x, o0.y, -hz), new Vector3(o1.x, o1.y, -hz), new Vector3(o1.x, o1.y, hz), new Vector3(o0.x, o0.y, hz), outN);

                    // Inner bore.
                    Vector2 mid = (i0 + i1) * 0.5f;
                    Vector3 inN = new Vector3(-mid.x, -mid.y, 0f).normalized;
                    Quad(new Vector3(i1.x, i1.y, -hz), new Vector3(i0.x, i0.y, -hz), new Vector3(i0.x, i0.y, hz), new Vector3(i1.x, i1.y, hz), inN);
                }

                Mesh m = new Mesh();
                m.SetVertices(v);
                m.SetNormals(nrm);
                m.SetUVs(0, uv);
                m.SetTriangles(tris, 0);
                FixWinding(m);
                m.RecalculateBounds();
                return m;
            });
        }

        /// <summary>
        /// Part of a torus in the XY plane: an arch of <paramref name="arcDeg"/> centred on +Y, its
        /// centreline <paramref name="majorRadius"/> from the origin, its tube
        /// <paramref name="minorRadius"/> thick. uv.x runs along the arch. A 180-degree arc stands on
        /// its two feet at y = 0 - a solar prominence loop, or a rock arch over the track.
        /// <paramref name="wobble"/> roughens the tube for rock.
        /// </summary>
        public static Mesh TorusArc(float majorRadius, float minorRadius, float arcDeg, int segments = 28, int sides = 10,
                                    float wobble = 0f, int seed = 0)
        {
            string key = $"torus_{majorRadius:F1}_{minorRadius:F2}_{arcDeg:F0}_{segments}_{sides}_{wobble:F2}_{seed}";
            return Cached(key, () =>
            {
                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();

                for (int s = 0; s <= segments; s++)
                {
                    float ts = s / (float)segments;
                    float th = (ts - 0.5f) * arcDeg * Mathf.Deg2Rad;          // 0 at the top
                    Vector3 centre = new Vector3(Mathf.Sin(th), Mathf.Cos(th), 0f) * majorRadius;
                    Vector3 outward = centre.normalized;

                    // Plasma loops swell in the middle and pinch at the feet.
                    float swell = wobble > 0f ? 1f : Mathf.Lerp(0.55f, 1f, Mathf.Sin(ts * Mathf.PI));

                    for (int k = 0; k <= sides; k++)
                    {
                        float tk = k / (float)sides;
                        float ph = tk * Mathf.PI * 2f;
                        Vector3 ring = outward * Mathf.Cos(ph) + Vector3.forward * Mathf.Sin(ph);
                        float rr = minorRadius * swell;
                        if (wobble > 0f)
                            rr *= 1f + wobble * (ProceduralMeshes.Fbm3(centre * 0.4f + ring * 1.3f, seed) - 0.5f) * 2f;
                        v.Add(centre + ring * rr);
                        uv.Add(new Vector2(ts, tk));
                    }
                }

                int cols = sides + 1;
                for (int s = 0; s < segments; s++)
                {
                    for (int k = 0; k < sides; k++)
                    {
                        int a = s * cols + k, b = a + 1, c = a + cols, d = c + 1;
                        tris.Add(a); tris.Add(b); tris.Add(c);
                        tris.Add(b); tris.Add(d); tris.Add(c);
                    }
                }

                Mesh m = new Mesh();
                m.SetVertices(v);
                m.SetUVs(0, uv);
                m.SetTriangles(tris, 0);
                m.RecalculateNormals();
                FixWinding(m);
                m.RecalculateBounds();
                return m;
            });
        }

        /// <summary>
        /// A lightning bolt: a jagged ribbon in the XY plane falling from the origin to
        /// -<paramref name="length"/>, with a couple of forks. Drawn double-sided by PlasmaGlow.
        /// </summary>
        public static Mesh Bolt(int seed, float length, float width, int steps = 14)
        {
            string key = $"bolt_{seed}_{length:F0}_{width:F2}_{steps}";
            return Cached(key, () =>
            {
                System.Random rnd = new System.Random(seed);
                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();

                void Branch(Vector3 start, Vector3 dir, float len, float w, int n)
                {
                    Vector3 p = start;
                    Vector3 side = Vector3.Cross(dir, Vector3.forward).normalized;
                    for (int s = 0; s < n; s++)
                    {
                        Vector3 next = p + dir * (len / n) + side * (float)(rnd.NextDouble() * 2.0 - 1.0) * len / n * 0.9f;
                        float taper = w * (1f - s / (float)n * 0.7f);
                        Vector3 along = (next - p).normalized;
                        Vector3 across = Vector3.Cross(along, Vector3.forward).normalized * taper;

                        int b = v.Count;
                        v.Add(p - across); v.Add(p + across); v.Add(next - across); v.Add(next + across);
                        uv.Add(new Vector2(s / (float)n, 0)); uv.Add(new Vector2(s / (float)n, 1));
                        uv.Add(new Vector2((s + 1) / (float)n, 0)); uv.Add(new Vector2((s + 1) / (float)n, 1));
                        tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                        tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
                        p = next;
                    }
                }

                Branch(Vector3.zero, Vector3.down, length, width, steps);
                for (int f = 0; f < 2; f++)
                {
                    Vector3 from = Vector3.down * length * (0.25f + 0.25f * f);
                    Vector3 dir = new Vector3(f == 0 ? 0.7f : -0.6f, -1f, 0f).normalized;
                    Branch(from, dir, length * 0.35f, width * 0.6f, steps / 2);
                }

                Mesh m = new Mesh();
                m.SetVertices(v);
                m.SetUVs(0, uv);
                m.SetTriangles(tris, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            });
        }

        /// <summary>
        /// Flips any triangle whose winding disagrees with its vertex normals, so shapes built
        /// quickly by hand still face the way their normals say.
        /// </summary>
        private static void FixWinding(Mesh m)
        {
            Vector3[] v = m.vertices;
            Vector3[] n = m.normals;
            int[] t = m.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                Vector3 avg = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                if (Vector3.Dot(face, avg) < 0f)
                {
                    int tmp = t[i + 1];
                    t[i + 1] = t[i + 2];
                    t[i + 2] = tmp;
                }
            }
            m.triangles = t;
        }
    }

    /// <summary>Small generated textures the second set of worlds needs.</summary>
    public static class WorldTextures
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Diagonal two-colour stripes. On a cylinder's UVs they wind into a spiral (candy canes);
        /// on a disc's they swirl (lollipops).
        /// </summary>
        public static Texture2D Stripes(Color a, Color b, int stripes = 4)
        {
            string key = $"stripes_{(Color32)a}_{(Color32)b}_{stripes}";
            Texture2D tex;
            if (cache.TryGetValue(key, out tex) && tex != null) return tex;

            const int size = 64;
            tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float s = ((x + y) / (float)size) * stripes;
                    float f = s - Mathf.Floor(s);
                    // Soft edge between the two colours so the stripes don't alias.
                    float m = Mathf.SmoothStep(0.44f, 0.56f, f) - Mathf.SmoothStep(0.94f, 1f, f);
                    px[y * size + x] = Color.Lerp(a, b, Mathf.Clamp01(m));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            cache[key] = tex;
            return tex;
        }
    }
}
