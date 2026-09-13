using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Runtime-baked surface textures for the themed scenery. Each recipe bakes
    /// a tileable height field once (torus-sampled Perlin FBM), colours it, and
    /// optionally derives a tangent-space normal map from it so URP Lit picks
    /// up real surface relief. Small (128-256 px), mip-mapped, cached by key.
    /// </summary>
    public static class ProceduralTextures
    {
        public class TexSet
        {
            public Texture2D albedo;
            public Texture2D normal;
            public Texture2D emission;
        }

        private static readonly Dictionary<string, TexSet> cache = new Dictionary<string, TexSet>();

        private static TexSet Cached(string key, System.Func<TexSet> bake)
        {
            TexSet t;
            if (cache.TryGetValue(key, out t) && t != null && t.albedo != null) return t;
            t = bake();
            cache[key] = t;
            return t;
        }

        // ------------------------------------------------------------------
        // Height fields
        // ------------------------------------------------------------------
        /// <summary>Tileable FBM in [0,1]. stretchX/Y squash the noise for streaks.</summary>
        public static float[,] Fbm(int size, float frequency, int octaves, int seed, float stretchX = 1f, float stretchY = 1f)
        {
            float[,] h = new float[size, size];
            float ox = (seed * 0.137f) % 97f + 3f;
            float oy = (seed * 0.311f) % 89f + 5f;
            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size * Mathf.PI * 2f;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size * Mathf.PI * 2f;
                    float n = 0f, amp = 0.55f, freq = frequency, norm = 0f;
                    for (int o = 0; o < octaves; o++)
                    {
                        float fx = freq * stretchX, fy = freq * stretchY;
                        float sx = Mathf.Sin(u) * fx, cx = Mathf.Cos(u) * fx;
                        float sy = Mathf.Sin(v) * fy, cy = Mathf.Cos(v) * fy;
                        n += Mathf.PerlinNoise(ox + sx + sy * 0.7f, oy + cx * 0.7f + cy) * amp;
                        norm += amp;
                        amp *= 0.5f;
                        freq *= 2.07f;
                    }
                    h[x, y] = n / norm;
                }
            }
            return h;
        }

        private static float Ridge(float n) { return 1f - Mathf.Abs(n * 2f - 1f); }

        private static Texture2D Make(int size, Color32[] px, bool linear = false)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, true, linear);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 1;
            t.SetPixels32(px);
            t.Apply(true, true);
            return t;
        }

        private static Color32 C(Color c) { return (Color32)c; }

        /// <summary>Sobel normal map from a height field. Stored as (x, y, z, 1) which URP unpacks on every platform.</summary>
        public static Texture2D NormalFromHeight(float[,] h, float strength)
        {
            int size = h.GetLength(0);
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                int y0 = (y - 1 + size) % size, y1 = (y + 1) % size;
                for (int x = 0; x < size; x++)
                {
                    int x0 = (x - 1 + size) % size, x1 = (x + 1) % size;
                    float dx = (h[x1, y0] + 2f * h[x1, y] + h[x1, y1]) - (h[x0, y0] + 2f * h[x0, y] + h[x0, y1]);
                    float dy = (h[x0, y1] + 2f * h[x, y1] + h[x1, y1]) - (h[x0, y0] + 2f * h[x, y0] + h[x1, y0]);
                    Vector3 n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    px[y * size + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
                }
            }
            Texture2D t = Make(size, px, true);
            t.name = "ProcNormal";
            return t;
        }

        // ------------------------------------------------------------------
        // Recipes
        // ------------------------------------------------------------------
        /// <summary>Mottled stone with fine speckle. Relief from the same field.</summary>
        public static TexSet Rock(int seed, Color dark, Color light, float relief = 2.5f)
        {
            return Cached($"rock_{seed}_{Key(dark)}_{Key(light)}", () =>
            {
                int size = 128;
                float[,] h = Fbm(size, 3f, 4, seed);
                float[,] fine = Fbm(size, 14f, 2, seed + 7);
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float n = Mathf.Clamp01(h[x, y] * 0.8f + fine[x, y] * 0.2f);
                        Color c = Color.Lerp(dark, light, n);
                        if (fine[x, y] > 0.72f) c *= 1.15f;
                        px[y * size + x] = C(c);
                    }
                float[,] relief2 = h;
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) relief2[x, y] = h[x, y] * 0.7f + fine[x, y] * 0.3f;
                return new TexSet { albedo = Make(size, px), normal = NormalFromHeight(relief2, relief) };
            });
        }

        /// <summary>Vertical bark streaks with ridges.</summary>
        public static TexSet Bark(int seed, Color dark, Color light)
        {
            return Cached($"bark_{seed}_{Key(dark)}_{Key(light)}", () =>
            {
                int size = 128;
                float[,] h = Fbm(size, 4f, 3, seed, 3.5f, 0.5f);
                float[,] r = new float[size, size];
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float ridge = Ridge(h[x, y]);
                        r[x, y] = ridge;
                        Color c = Color.Lerp(dark, light, Mathf.Pow(ridge, 1.5f));
                        px[y * size + x] = C(c);
                    }
                return new TexSet { albedo = Make(size, px), normal = NormalFromHeight(r, 3.5f) };
            });
        }

        /// <summary>Blotchy two-tone foliage with light speckle for leaf clumps.</summary>
        public static TexSet Foliage(int seed, Color dark, Color light)
        {
            return Cached($"foliage_{seed}_{Key(dark)}_{Key(light)}", () =>
            {
                int size = 128;
                float[,] h = Fbm(size, 6f, 3, seed);
                float[,] fine = Fbm(size, 18f, 2, seed + 3);
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float n = Mathf.SmoothStep(0.3f, 0.7f, h[x, y]);
                        Color c = Color.Lerp(dark, light, n);
                        if (fine[x, y] > 0.68f) c = Color.Lerp(c, light * 1.2f, 0.5f);
                        px[y * size + x] = C(c);
                    }
                return new TexSet { albedo = Make(size, px), normal = NormalFromHeight(fine, 1.6f) };
            });
        }

        /// <summary>Leaf blade: midrib and diagonal veins over a soft gradient. UV v runs base→tip.</summary>
        public static TexSet Leaf(int seed, Color dark, Color light, Color vein)
        {
            return Cached($"leaf_{seed}_{Key(dark)}_{Key(light)}", () =>
            {
                int size = 128;
                float[,] h = Fbm(size, 5f, 2, seed);
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = (float)x / size, v = (float)y / size;
                        float midrib = Mathf.Exp(-Mathf.Pow((u - 0.5f) * 28f, 2f));
                        float veins = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((v * 14f + Mathf.Abs(u - 0.5f) * 10f) * Mathf.PI)), 12f) * 0.7f;
                        Color c = Color.Lerp(dark, light, 0.35f + h[x, y] * 0.5f + (1f - v) * 0.15f);
                        c = Color.Lerp(c, vein, Mathf.Clamp01(midrib + veins));
                        px[y * size + x] = C(c);
                    }
                return new TexSet { albedo = Make(size, px) };
            });
        }

        /// <summary>Gas-giant style latitude bands warped by noise. UV v is latitude.</summary>
        public static TexSet Bands(int seed, Color[] palette, int bandCount = 7)
        {
            return Cached($"bands_{seed}_{palette.Length}_{Key(palette[0])}", () =>
            {
                int size = 256;
                float[,] h = Fbm(size, 3f, 3, seed, 1f, 0.35f);
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float v = (float)y / size;
                        float band = v * bandCount + (h[x, y] - 0.5f) * 1.6f;
                        float f = band - Mathf.Floor(band);
                        int i0 = ((int)Mathf.Floor(band) % palette.Length + palette.Length) % palette.Length;
                        int i1 = (i0 + 1) % palette.Length;
                        Color c = Color.Lerp(palette[i0], palette[i1], Mathf.SmoothStep(0f, 1f, f));
                        c *= 0.85f + h[x, y] * 0.3f;
                        px[y * size + x] = C(c);
                    }
                return new TexSet { albedo = Make(size, px) };
            });
        }

        /// <summary>Dark basalt crazed with glowing cracks; the crack mask is the emission map.</summary>
        public static TexSet LavaCracks(int seed, Color rock, Color glow, float crackWidth = 0.06f)
        {
            return Cached($"lava_{seed}_{Key(rock)}_{Key(glow)}", () =>
            {
                int size = 128;
                float[,] h = Fbm(size, 4f, 2, seed);
                float[,] fine = Fbm(size, 12f, 2, seed + 11);
                float[,] relief = new float[size, size];
                Color32[] alb = new Color32[size * size];
                Color32[] emi = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float crack = Mathf.SmoothStep(1f - crackWidth * 2f, 1f, Ridge(h[x, y]));
                        relief[x, y] = fine[x, y] * 0.6f - crack * 0.6f;
                        Color c = rock * (0.7f + fine[x, y] * 0.5f);
                        alb[y * size + x] = C(Color.Lerp(c, glow * 0.5f, crack));
                        emi[y * size + x] = C(glow * crack);
                    }
                return new TexSet { albedo = Make(size, alb), normal = NormalFromHeight(relief, 3f), emission = Make(size, emi) };
            });
        }

        /// <summary>Pale ice with soft cloudy depth and thin bright veins.</summary>
        public static TexSet Ice(int seed, Color baseColor, Color vein)
        {
            return Cached($"ice_{seed}_{Key(baseColor)}_{Key(vein)}", () =>
            {
                int size = 128;
                float[,] h = Fbm(size, 3f, 3, seed);
                float[,] v = Fbm(size, 7f, 2, seed + 5);
                float[,] relief = new float[size, size];
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float veins = Mathf.SmoothStep(0.86f, 1f, Ridge(v[x, y]));
                        relief[x, y] = h[x, y] * 0.5f + veins * 0.5f;
                        Color c = baseColor * (0.8f + h[x, y] * 0.35f);
                        px[y * size + x] = C(Color.Lerp(c, vein, veins * 0.8f));
                    }
                return new TexSet { albedo = Make(size, px), normal = NormalFromHeight(relief, 1.8f) };
            });
        }

        /// <summary>Coral / sponge: pitted with dark pores.</summary>
        public static TexSet Coral(int seed, Color baseColor, Color pore)
        {
            return Cached($"coral_{seed}_{Key(baseColor)}_{Key(pore)}", () =>
            {
                int size = 128;
                float[,] h = Fbm(size, 16f, 2, seed);
                float[,] tone = Fbm(size, 3f, 2, seed + 9);
                float[,] relief = new float[size, size];
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float pores = Mathf.SmoothStep(0.6f, 0.78f, h[x, y]);
                        relief[x, y] = 1f - pores;
                        Color c = baseColor * (0.85f + tone[x, y] * 0.3f);
                        px[y * size + x] = C(Color.Lerp(c, pore, pores));
                    }
                return new TexSet { albedo = Make(size, px), normal = NormalFromHeight(relief, 3f) };
            });
        }

        /// <summary>Concentric bands along v with gaps, for planet rings (alpha carries the gaps).</summary>
        public static TexSet RingBands(int seed, Color a, Color b)
        {
            return Cached($"ring_{seed}_{Key(a)}_{Key(b)}", () =>
            {
                int size = 128;
                Color32[] px = new Color32[size * size];
                float ox = seed * 0.37f;
                for (int y = 0; y < size; y++)
                {
                    float v = (float)y / size;
                    float n = Mathf.PerlinNoise(ox, v * 9f) * 0.6f + Mathf.PerlinNoise(ox + 5f, v * 31f) * 0.4f;
                    float alpha = Mathf.SmoothStep(0.25f, 0.6f, n) * Mathf.Sin(v * Mathf.PI);
                    Color c = Color.Lerp(a, b, n);
                    c.a = alpha;
                    Color32 c32 = C(c);
                    for (int x = 0; x < size; x++) px[y * size + x] = c32;
                }
                return new TexSet { albedo = Make(size, px) };
            });
        }

        /// <summary>Solar-panel cell grid; the grid lines glow via the emission map.</summary>
        public static TexSet SolarGrid(int seed, Color cell, Color line)
        {
            return Cached($"solar_{seed}_{Key(cell)}_{Key(line)}", () =>
            {
                int size = 64;
                Color32[] alb = new Color32[size * size];
                Color32[] emi = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        bool isLine = (x % 16 < 2) || (y % 16 < 2);
                        alb[y * size + x] = C(isLine ? line * 0.6f : cell * (0.9f + ((x / 16 + y / 16) % 2) * 0.15f));
                        emi[y * size + x] = C(isLine ? line : Color.black);
                    }
                return new TexSet { albedo = Make(size, alb), emission = Make(size, emi) };
            });
        }

        private static string Key(Color c)
        {
            return ((int)(c.r * 255)).ToString("x2") + ((int)(c.g * 255)).ToString("x2") + ((int)(c.b * 255)).ToString("x2");
        }
    }
}
