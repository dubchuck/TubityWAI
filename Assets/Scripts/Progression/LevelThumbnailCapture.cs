#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace TubityWAI.Progression
{
    /// <summary>
    /// Editor-only: renders a thumbnail of every ladder level's and campaign level's opening into
    /// Assets/Resources/LevelThumbs. Started in Play mode by the TubityX > Progression >
    /// Capture Level Thumbnails menu (Editor/LevelThumbnailMenu.cs), which also re-imports the
    /// images once Play mode ends.
    ///
    /// Each level is built the normal way (GameSetup.StartGame) but begun just short of its
    /// first ring (a composed level) or the end of its clear run-in (a campaign level), so the
    /// picture shows the tube, the environment and the first arcs rather than empty tube. The
    /// countdown holds the sphere still while the camera renders straight into a texture, which
    /// leaves out every HUD and menu overlay.
    /// </summary>
    public class LevelThumbnailCapture : MonoBehaviour
    {
        /// <summary>One picture to take: which level, and where it is saved.</summary>
        private struct Shot
        {
            public string fileName;
            public System.Func<LevelConfig> build;
            public string label;
        }

        /// <summary>How far short of the first ring the sphere is parked.</summary>
        private const float OpeningLead = 26f;
        /// <summary>SessionState key the editor menu watches to re-import on the way out.</summary>
        public const string WrittenKey = "TubityX.CaptureThumbs.Written";

        private IEnumerator Start()
        {
            // Let the scene come up (GameSetup, the menu, attract mode) before taking it over.
            for (int i = 0; i < 10; i++) yield return null;

            if (GameSetup.Instance == null)
            {
                Debug.LogError("[LevelThumbnails] No GameSetup in the scene; open the game scene and try again.");
                Stop();
                yield break;
            }

            float volume = AudioListener.volume;
            AudioListener.volume = 0f;

            string dir = Path.Combine(Application.dataPath, "Resources", LevelThumbnails.ResourceFolder);
            Directory.CreateDirectory(dir);

            RenderTexture rt = new RenderTexture(LevelThumbnails.Width, LevelThumbnails.Height, 24,
                                                 RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 8;
            Texture2D readback = new Texture2D(LevelThumbnails.Width, LevelThumbnails.Height, TextureFormat.RGB24, false);

            System.Collections.Generic.List<Shot> shots = new System.Collections.Generic.List<Shot>();
            for (int n = 1; n <= ProgressionV2.LevelCount; n++)
            {
                int level = n;
                shots.Add(new Shot { fileName = LevelThumbnails.NameFor(level), label = $"Progression level {level}",
                                     build = () => ProgressionV2.CreateLevel(level) });
            }
            for (int n = 1; n <= LevelProgression.CampaignLevelCount; n++)
            {
                int level = n;
                shots.Add(new Shot { fileName = LevelThumbnails.CampaignNameFor(level), label = $"Campaign level {level}",
                                     build = () => LevelProgression.CreateCampaignLevel(level) });
            }

            int written = 0;
            for (int i = 0; i < shots.Count; i++)
            {
                Shot shot = shots[i];
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                        "Capturing level thumbnails", $"{shot.label}  ({i + 1} of {shots.Count})",
                        i / (float)shots.Count))
                    break;

                LevelConfig config = shot.build();
                config.startZ = Mathf.Max(0f, FirstRingZ(config) - OpeningLead);

                TearDownRun();
                yield return null;      // let Destroy land before the next build looks around

                int spheres = config.forcedSphereCount > 0 ? config.forcedSphereCount : 1;
                GameSetup.Instance.StartGame(spheres, config);
                yield return null;

                // Put the camera straight onto its follow pose so it isn't still gliding in from
                // the origin, then give segments, scenery and the sky a few frames to settle.
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 curve = config.GetCurveOffset(config.startZ);
                    cam.transform.position = new Vector3(curve.x, curve.y, config.startZ - 7f);
                }
                for (int f = 0; f < 12; f++) yield return null;

                cam = Camera.main;
                if (cam == null) continue;

                RenderTexture previous = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = previous;

                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                readback.Apply();
                RenderTexture.active = null;

                File.WriteAllBytes(Path.Combine(dir, shot.fileName + ".jpg"), readback.EncodeToJPG(88));
                written++;

                // Every build makes its own materials and textures; don't let 150 of them pile up.
                if (i % 8 == 7) yield return Resources.UnloadUnusedAssets();
            }

            UnityEditor.EditorUtility.ClearProgressBar();
            rt.Release();
            Destroy(rt);
            Destroy(readback);
            AudioListener.volume = volume;

            Debug.Log($"[LevelThumbnails] Wrote {written} thumbnails to Assets/Resources/{LevelThumbnails.ResourceFolder}.");
            UnityEditor.SessionState.SetBool(WrittenKey, written > 0);
            Stop();
        }

        /// <summary>Where the level's first arcs are: a composed level's first ring, or the end of
        /// a campaign level's obstacle-free run-in (its arcs are rolled per marker from there).</summary>
        private static float FirstRingZ(LevelConfig config)
        {
            if (config.HasComposer)
            {
                System.Collections.Generic.List<RingSpec> rings = new System.Collections.Generic.List<RingSpec>();
                config.composer.RingsInRange(0f, Mathf.Max(200f, config.levelLength), rings);
                if (rings.Count > 0) return rings[0].z;
                return 60f;
            }
            return config.GetStartClearDistance() + 5f;
        }

        /// <summary>What GameSetup.StartGame creates per run (as GameManager's own teardown).</summary>
        private static void TearDownRun()
        {
            foreach (PlayerController p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None)) Destroy(p.gameObject);
            foreach (PlayerSphere s in FindObjectsByType<PlayerSphere>(FindObjectsSortMode.None)) Destroy(s.gameObject);
            foreach (GameHUD h in FindObjectsByType<GameHUD>(FindObjectsSortMode.None)) Destroy(h.gameObject);
            foreach (TunnelGenerator g in FindObjectsByType<TunnelGenerator>(FindObjectsSortMode.None)) Destroy(g.gameObject);
            foreach (FTUEManager f in FindObjectsByType<FTUEManager>(FindObjectsSortMode.None)) Destroy(f.gameObject);
        }

        private void Stop()
        {
            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
