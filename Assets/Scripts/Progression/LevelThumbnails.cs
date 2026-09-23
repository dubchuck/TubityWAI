using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// Screenshots of each level's opening - the 128 ladder levels and the 24 campaign levels -
    /// shown on their level buttons. They are rendered in the editor by TubityX > Progression >
    /// Capture Level Thumbnails (LevelThumbnailCapture) and ship as ordinary Resources textures;
    /// a level without one just shows a blank frame.
    /// </summary>
    public static class LevelThumbnails
    {
        /// <summary>Folder under Assets/Resources the captures are written to and loaded from.</summary>
        public const string ResourceFolder = "LevelThumbs";

        /// <summary>2:1, the shape of the picture well on a level card.</summary>
        public const int Width = 448;
        public const int Height = 224;

        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>File name for a ladder level (1..128).</summary>
        public static string NameFor(int level)
        {
            return "P2_" + level.ToString("000");
        }

        /// <summary>File name for a campaign level (1..24).</summary>
        public static string CampaignNameFor(int level)
        {
            return "C_" + level.ToString("00");
        }

        /// <summary>The thumbnail for a ladder level, or null if none has been captured.</summary>
        public static Texture2D Get(int level)
        {
            return Load(NameFor(level));
        }

        /// <summary>The thumbnail for a campaign level, or null if none has been captured.</summary>
        public static Texture2D GetCampaign(int level)
        {
            return Load(CampaignNameFor(level));
        }

        private static Texture2D Load(string name)
        {
            Texture2D tex;
            if (cache.TryGetValue(name, out tex) && tex != null) return tex;
            tex = Resources.Load<Texture2D>(ResourceFolder + "/" + name);
            if (tex != null) cache[name] = tex;
            return tex;
        }
    }
}
