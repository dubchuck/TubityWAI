using UnityEditor;
using UnityEngine;
using TubityWAI.Progression;

namespace TubityWAI.EditorTools
{
    /// <summary>
    /// TubityX > Progression > Capture Level Thumbnails: enters Play mode, lets
    /// LevelThumbnailCapture render every ladder level's opening, and re-imports the images
    /// when Play mode ends. Run it with the game scene open; it takes a couple of minutes.
    /// </summary>
    [InitializeOnLoad]
    public static class LevelThumbnailMenu
    {
        private const string PendingKey = "TubityX.CaptureThumbs.Pending";

        static LevelThumbnailMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("TubityX/Progression/Capture Level Thumbnails")]
        private static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[LevelThumbnails] Leave Play mode first; the capture starts its own session.");
                return;
            }
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                new GameObject("LevelThumbnailCapture").AddComponent<LevelThumbnailCapture>();
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(LevelThumbnailCapture.WrittenKey, false))
            {
                SessionState.SetBool(LevelThumbnailCapture.WrittenKey, false);
                AssetDatabase.Refresh();
            }
        }
    }

    /// <summary>Thumbnails are UI pictures: no mipmaps, no wrapping, capped at their own size.</summary>
    public class LevelThumbnailImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/" + LevelThumbnails.ResourceFolder + "/")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 512;
        }
    }
}
