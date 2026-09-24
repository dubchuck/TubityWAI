using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Small library of generated meshes for the themed scenery: noise-displaced
    /// rocks, tapered/bent trunks, faceted crystals, curled leaves, domes and
    /// discs. Every mesh is cached by its parameters and shared between all the
    /// props that use it, so a level only ever pays the generation cost once.
    /// Triangle counts are kept in the low hundreds for mobile.
    /// </summary>
    public static class ProceduralMeshes
    {
        private static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        private static Mesh Cached(string key, System.Func<Mesh> build)
        {
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = build();
            m.name = "Proc_" + key;
            cache[key] = m;
            return m;
        }

        // ------------------------------------------------------------------
        // Noise helpers
        // ------------------------------------------------------------------
        /// <summary>Cheap 3D value noise from three orthogonal Perlin planes.</summary>
        public static float Noise3(Vector3 p, float seed)
        {
            float a = Mathf.PerlinNoise(p.x + seed, p.y + seed * 0.37f);
            float b = Mathf.PerlinNoise(p.y + seed * 1.7f, p.z + seed * 0.11f);
            float c = Mathf.PerlinNoise(p.z + seed * 0.53f, p.x + seed * 2.3f);
            return (a + b + c) / 3f;
        }

        public static float Fbm3(Vector3 p, float seed, int octaves = 3)
        {
            float sum = 0f, amp = 0.55f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise3(p * freq, seed + i * 19f) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2.1f;
            }
            return sum / norm;
        }

        // ------------------------------------------------------------------
        // Icosphere & rocks
        // ------------------------------------------------------------------
        private static void Icosphere(int subdivisions, out List<Vector3> verts, out List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;

            tris = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };

            for (int s = 0; s < subdivisions; s++)
            {
                Dictionary<long, int> midpoints = new Dictionary<long, int>();
                List<int> next = new List<int>(tris.Count * 4);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Midpoint(a, b, verts, midpoints);
                    int bc = Midpoint(b, c, verts, midpoints);
                    int ca = Midpoint(c, a, verts, midpoints);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = next;
            }
        }

        private static int Midpoint(int a, int b, List<Vector3> verts, Dictionary<long, int> midpoints)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            int idx;
            if (midpoints.TryGetValue(key, out idx)) return idx;
            Vector3 m = ((verts[a] + verts[b]) * 0.5f).normalized;
            verts.Add(m);
            idx = verts.Count - 1;
            midpoints[key] = idx;
            return idx;
        }

        private static Vector2 SphericalUV(Vector3 n)
        {
            float u = 0.5f + Mathf.Atan2(n.x, n.z) / (2f * Mathf.PI);
            float v = 0.5f + Mathf.Asin(Mathf.Clamp(n.y, -1f, 1f)) / Mathf.PI;
            return new Vector2(u, v);
        }

        /// <summary>
        /// Unit-radius rock: an icosphere pushed in and out along its normals by
        /// low-frequency noise. Smooth normals; squash it with the transform scale.
        /// </summary>
        public static Mesh Rock(int seed, int subdivisions = 2, float roughness = 0.35f, float frequency = 1.6f)
        {
            return Cached($"rock_{seed}_{subdivisions}_{roughness:F2}_{frequency:F2}", () =>
            {
                List<Vector3> verts; List<int> tris;
                Icosphere(subdivisions, out verts, out tris);

                Vector3[] v = new Vector3[verts.Count];
                Vector2[] uv = new Vector2[verts.Count];
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector3 n = verts[i];
                    float d = 1f + (Fbm3(n * frequency, seed * 7.13f) - 0.5f) * 2f * roughness;
                    v[i] = n * d;
                    uv[i] = SphericalUV(n) * 2f;
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

        /// <summary>Smooth unit sphere with spherical UVs (planets, bulbs).</summary>
        public static Mesh Sphere(int subdivisions = 3)
        {
            return Rock(0, subdivisions, 0f, 1f);
        }

        // ------------------------------------------------------------------
        // Cones / trunks / stalks
        // ------------------------------------------------------------------
        /// <summary>
        /// Tapered tube from y=0 to y=height. Wobble roughens the silhouette,
        /// bend curves it along +X (quadratic), caps close the ends.
        /// </summary>
        public static Mesh Cone(int seed, float bottomRadius, float topRadius, float height, int segments = 12, int rings = 6,
                                float wobble = 0f, float bend = 0f, bool caps = true)
        {
            return Cached($"cone_{seed}_{bottomRadius:F2}_{topRadius:F2}_{height:F2}_{segments}_{rings}_{wobble:F2}_{bend:F2}_{caps}", () =>
            {
                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();

                int perRing = segments + 1;
                for (int r = 0; r <= rings; r++)
                {
                    float t = (float)r / rings;
                    float y = t * height;
                    float radius = Mathf.Lerp(bottomRadius, topRadius, t);
                    float bx = bend * t * t * height;
                    for (int i = 0; i <= segments; i++)
                    {
                        float a = (float)i / segments * Mathf.PI * 2f;
                        float rr = radius;
                        if (wobble > 0f)
                        {
                            rr *= 1f + (Noise3(new Vector3(Mathf.Cos(a) * 1.5f, t * 3f, Mathf.Sin(a) * 1.5f), seed * 3.1f) - 0.5f) * 2f * wobble;
                        }
                        v.Add(new Vector3(Mathf.Cos(a) * rr + bx, y, Mathf.Sin(a) * rr));
                        uv.Add(new Vector2((float)i / segments, t));
                    }
                }
                for (int r = 0; r < rings; r++)
                {
                    for (int i = 0; i < segments; i++)
                    {
                        int a = r * perRing + i, b = a + 1, c = a + perRing, d = c + 1;
                        tris.AddRange(new[] { a, c, b, b, c, d });
                    }
                }

                if (caps)
                {
                    AddCap(v, uv, tris, new Vector3(0f, 0f, 0f), bottomRadius, 0, perRing, false);
                    AddCap(v, uv, tris, new Vector3(bend * height, height, 0f), topRadius, rings * perRing, perRing, true);
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

        private static void AddCap(List<Vector3> v, List<Vector2> uv, List<int> tris, Vector3 center, float radius, int ringStart, int perRing, bool up)
        {
            if (radius <= 0.0001f) return;
            int c = v.Count;
            v.Add(center);
            uv.Add(new Vector2(0.5f, 0.5f));
            int first = v.Count;
            for (int i = 0; i < perRing; i++)
            {
                v.Add(v[ringStart + i]);
                uv.Add(new Vector2(0.5f, 0.5f) + new Vector2(v[ringStart + i].x - center.x, v[ringStart + i].z - center.z) * 0.5f / radius);
            }
            for (int i = 0; i < perRing - 1; i++)
            {
                if (up) tris.AddRange(new[] { c, first + i + 1, first + i });
                else tris.AddRange(new[] { c, first + i, first + i + 1 });
            }
        }

        // ------------------------------------------------------------------
        // Crystals (flat shaded)
        // ------------------------------------------------------------------
        /// <summary>
        /// Faceted crystal: an n-sided prism from y=0 that widens slightly, then
        /// closes to a point at y=height. Irregular radii per side so no two
        /// look alike. Flat shading via unshared vertices.
        /// </summary>
        public static Mesh Crystal(int seed, int sides, float radius, float height, float tipRatio = 0.35f)
        {
            return Cached($"crystal_{seed}_{sides}_{radius:F2}_{height:F2}_{tipRatio:F2}", () =>
            {
                Random.State prev = Random.state;
                Random.InitState(seed * 131 + sides);

                Vector3[] baseRing = new Vector3[sides];
                Vector3[] midRing = new Vector3[sides];
                float midY = height * (1f - tipRatio);
                for (int i = 0; i < sides; i++)
                {
                    float a = (float)i / sides * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                    float rb = radius * Random.Range(0.7f, 1.0f);
                    float rm = radius * Random.Range(0.9f, 1.25f);
                    baseRing[i] = new Vector3(Mathf.Cos(a) * rb, 0f, Mathf.Sin(a) * rb);
                    midRing[i] = new Vector3(Mathf.Cos(a) * rm, midY + Random.Range(-0.08f, 0.08f) * height, Mathf.Sin(a) * rm);
                }
                Vector3 tip = new Vector3(Random.Range(-0.1f, 0.1f) * radius, height, Random.Range(-0.1f, 0.1f) * radius);
                Vector3 bottom = Vector3.zero;
                Random.state = prev;

                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();

                for (int i = 0; i < sides; i++)
                {
                    int j = (i + 1) % sides;
                    float u0 = (float)i / sides, u1 = (float)(i + 1) / sides;
                    // side quad
                    Quad(v, uv, tris, baseRing[i], baseRing[j], midRing[j], midRing[i],
                         new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, 1f - tipRatio), new Vector2(u0, 1f - tipRatio));
                    // tip facet
                    Tri(v, uv, tris, midRing[i], midRing[j], tip, new Vector2(u0, 1f - tipRatio), new Vector2(u1, 1f - tipRatio), new Vector2((u0 + u1) * 0.5f, 1f));
                    // bottom
                    Tri(v, uv, tris, baseRing[j], baseRing[i], bottom, new Vector2(u1, 0f), new Vector2(u0, 0f), new Vector2(0.5f, 0f));
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

        private static void Tri(List<Vector3> v, List<Vector2> uv, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            uv.Add(ua); uv.Add(ub); uv.Add(uc);
            // winding chosen so the face normal points away from the axis for our ring order
            tris.AddRange(new[] { i, i + 2, i + 1 });
        }

        private static void Quad(List<Vector3> v, List<Vector2> uv, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            Tri(v, uv, tris, a, b, c, ua, ub, uc);
            Tri(v, uv, tris, a, c, d, ua, uc, ud);
        }

        // ------------------------------------------------------------------
        // Leaves / fronds / fins
        // ------------------------------------------------------------------
        /// <summary>
        /// Double-sided tapered strip from the origin along +Z. Widest around
        /// 40% of its length, pointed at the tip, drooping by 'curl'. A gentle
        /// V-fold along the midrib gives it some volume under lighting.
        /// </summary>
        public static Mesh Leaf(int seed, float length, float width, float curl = 0.35f, float fold = 0.15f, int segments = 8)
        {
            return Cached($"leaf_{seed}_{length:F2}_{width:F2}_{curl:F2}_{fold:F2}_{segments}", () =>
            {
                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();

                for (int side = 0; side < 2; side++)
                {
                    int start = v.Count;
                    for (int i = 0; i <= segments; i++)
                    {
                        float t = (float)i / segments;
                        // At the tip (t = 1) the float sine of PI comes out as a hair below zero, and a
                        // fractional power of a negative is NaN; clamp so the tip is the point it means to be.
                        float w = width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI)), 0.65f) * (0.25f + 0.75f * Mathf.Sin(Mathf.Min(1f, t * 1.4f) * Mathf.PI * 0.5f));
                        float droop = -curl * t * t * length;
                        float z = t * length;
                        float edgeLift = fold * w;
                        v.Add(new Vector3(-w * 0.5f, droop + edgeLift, z));
                        v.Add(new Vector3(0f, droop, z));
                        v.Add(new Vector3(w * 0.5f, droop + edgeLift, z));
                        uv.Add(new Vector2(0f, t)); uv.Add(new Vector2(0.5f, t)); uv.Add(new Vector2(1f, t));
                    }
                    for (int i = 0; i < segments; i++)
                    {
                        int a = start + i * 3;
                        int b = a + 3;
                        if (side == 0)
                            tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1, a + 1, b + 1, a + 2, a + 2, b + 1, b + 2 });
                        else
                            tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b, a + 1, a + 2, b + 1, a + 2, b + 2, b + 1 });
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

        // ------------------------------------------------------------------
        // Domes (jellyfish bells, coral heads, flower caps)
        // ------------------------------------------------------------------
        /// <summary>
        /// Hemisphere of unit radius with its rim at y=0, squashed vertically.
        /// 'skirt' &gt; 0 continues the surface below the rim, curling inward.
        /// </summary>
        public static Mesh Dome(int segments = 16, int rings = 8, float squash = 0.7f, float skirt = 0.25f)
        {
            return Cached($"dome_{segments}_{rings}_{squash:F2}_{skirt:F2}", () =>
            {
                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();

                float latMax = Mathf.PI * 0.5f * (1f + skirt);
                int perRing = segments + 1;
                for (int r = 0; r <= rings; r++)
                {
                    float t = (float)r / rings;
                    float lat = t * latMax;
                    float y = Mathf.Cos(lat) * squash;
                    float rad = Mathf.Sin(lat);
                    for (int i = 0; i <= segments; i++)
                    {
                        float a = (float)i / segments * Mathf.PI * 2f;
                        v.Add(new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad));
                        uv.Add(new Vector2((float)i / segments, 1f - t));
                    }
                }
                for (int r = 0; r < rings; r++)
                {
                    for (int i = 0; i < segments; i++)
                    {
                        int a = r * perRing + i, b = a + 1, c = a + perRing, d = c + 1;
                        tris.AddRange(new[] { a, b, c, b, d, c });
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

        // ------------------------------------------------------------------
        // Discs / annuli (planet rings, lava pools)
        // ------------------------------------------------------------------
        /// <summary>Flat ring in the XZ plane. UV v runs inner→outer so a 1D gradient texture bands it.</summary>
        public static Mesh Disc(int segments, float innerRadius, float outerRadius, bool doubleSided = true)
        {
            return Cached($"disc_{segments}_{innerRadius:F2}_{outerRadius:F2}_{doubleSided}", () =>
            {
                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> tris = new List<int>();

                int sides = doubleSided ? 2 : 1;
                for (int side = 0; side < sides; side++)
                {
                    int start = v.Count;
                    for (int i = 0; i <= segments; i++)
                    {
                        float a = (float)i / segments * Mathf.PI * 2f;
                        Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        v.Add(dir * innerRadius); uv.Add(new Vector2((float)i / segments, 0f));
                        v.Add(dir * outerRadius); uv.Add(new Vector2((float)i / segments, 1f));
                    }
                    for (int i = 0; i < segments; i++)
                    {
                        int a = start + i * 2, b = a + 1, c = a + 2, d = a + 3;
                        if (side == 0) tris.AddRange(new[] { a, c, b, b, c, d });
                        else tris.AddRange(new[] { a, b, c, b, d, c });
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
    }
}
