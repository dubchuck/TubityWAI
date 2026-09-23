using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// The HUD's icons, drawn in code like the review card's star so the HUD needs no art
    /// assets. Each is a white shape on transparent (tint it with Image.color), rasterised once
    /// with 4x4 supersampling for clean edges and cached for the session.
    ///
    /// Shapes are described as "is this point inside", in a unit square centred on the origin
    /// with y up, which keeps each one a few readable lines.
    /// </summary>
    public static class HudIcons
    {
        private const int Size = 96;
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>A coin: a disc with an engraved rim.</summary>
        public static Sprite Coin()
        {
            return Get("coin", p =>
            {
                float r = p.magnitude;
                return r < 0.46f && !(r > 0.31f && r < 0.36f);
            });
        }

        /// <summary>A clock face: rim, two hands, a hub.</summary>
        public static Sprite Clock()
        {
            return Get("clock", p =>
            {
                float r = p.magnitude;
                if (r < 0.47f && r > 0.385f) return true;
                if (r < 0.07f) return true;
                return Capsule(p, Vector2.zero, new Vector2(0f, 0.26f), 0.045f)
                    || Capsule(p, Vector2.zero, new Vector2(0.19f, 0f), 0.045f);
            });
        }

        /// <summary>A lightning bolt, for invincibility.</summary>
        public static Sprite Bolt()
        {
            Vector2[] bolt =
            {
                new Vector2( 0.10f,  0.48f), new Vector2(-0.26f, -0.03f), new Vector2(-0.02f, -0.03f),
                new Vector2(-0.12f, -0.48f), new Vector2( 0.27f,  0.07f), new Vector2( 0.03f,  0.07f),
            };
            return Get("bolt", p => InPolygon(p, bolt));
        }

        /// <summary>A horseshoe magnet with its pole caps split off.</summary>
        public static Sprite Magnet()
        {
            return Get("magnet", p =>
            {
                Vector2 c = new Vector2(0f, -0.02f);
                const float outer = 0.40f, inner = 0.17f, top = 0.44f, capLine = 0.25f, capGap = 0.035f;

                // The bend: the lower half of a thick ring.
                float r = (p - c).magnitude;
                if (p.y <= c.y) return r < outer && r > inner;

                // The legs, cut once near the top so the poles read as caps.
                bool leg = Mathf.Abs(p.x) < outer && Mathf.Abs(p.x) > inner && p.y < top;
                return leg && Mathf.Abs(p.y - capLine) > capGap;
            });
        }

        // ---------------------------------------------------------------------------------------

        private static Sprite Get(string key, System.Func<Vector2, bool> inside)
        {
            Sprite sprite;
            if (cache.TryGetValue(key, out sprite) && sprite != null) return sprite;

            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            const int ss = 4;
            Color32[] px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            Vector2 p = new Vector2((x + (sx + 0.5f) / ss) / Size - 0.5f,
                                                    (y + (sy + 0.5f) / ss) / Size - 0.5f);
                            if (inside(p)) hits++;
                        }
                    }
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / (ss * ss)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();

            sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>Within `radius` of the segment a-b.</summary>
        private static bool Capsule(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return (p - (a + ab * t)).sqrMagnitude < radius * radius;
        }

        /// <summary>Even-odd point-in-polygon.</summary>
        private static bool InPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
