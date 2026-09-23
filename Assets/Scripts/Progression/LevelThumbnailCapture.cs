#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

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
    /// countdown holds the sphere still while the camera renders straight into a texture through
    /// a render request, which leaves out every HUD and menu overlay. Should that come back black
    /// (it can, depending on the pipeline's setup), the frame is grabbed from the Game view with
    /// the overlays hidden instead - so keep the Game view visible while it runs. A frame that is
    /// still black is skipped rather than saved.
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

            // No MSAA: a multisampled target can read back as black.
            RenderTexture rt = new RenderTexture(LevelThumbnails.Width, LevelThumbnails.Height, 24,
                                                 RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 1;
            rt.Create();
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

            int written = 0, skipped = 0;
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

                RenderThroughPipeline(cam, rt);
                ReadBack(rt, readback);

                if (IsBlack(readback))
                {
                    yield return new WaitForEndOfFrame();
                    GrabGameView(rt);
                    ReadBack(rt, readback);
                }

                if (IsBlack(readback))
                {
                    Debug.LogWarning($"[LevelThumbnails] {shot.label} rendered black; skipped. Is the Game view visible?");
                    // Don't leave an older, possibly just as black, picture standing in for it.
                    string stale = Path.Combine(dir, shot.fileName + ".jpg");
                    if (File.Exists(stale)) File.Delete(stale);
                    skipped++;
                    continue;
                }

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

            Debug.Log($"[LevelThumbnails] Wrote {written} thumbnails to Assets/Resources/{LevelThumbnails.ResourceFolder}" +
                      (skipped > 0 ? $"; {skipped} came out black and were skipped." : "."));
            UnityEditor.SessionState.SetBool(WrittenKey, written > 0);
            Stop();
        }

        /// <summary>
        /// Renders the camera into `target` the way the render pipeline supports: a standard render
        /// request (URP renders its own post-processing into it). Falls back to Camera.Render on a
        /// pipeline that doesn't take requests.
        /// </summary>
        private static void RenderThroughPipeline(Camera cam, RenderTexture target)
        {
            RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                request.destination = target;
                RenderPipeline.SubmitRenderRequest(cam, request);
                return;
            }

            RenderTexture previous = cam.targetTexture;
            cam.targetTexture = target;
            cam.Render();
            cam.targetTexture = previous;
        }

        /// <summary>
        /// The fallback: this frame as the Game view shows it, with every screen-space overlay
        /// (HUD, menus, countdown) switched off for it, cropped to 2:1 from the centre and scaled
        /// into `target`. Must run after WaitForEndOfFrame.
        /// </summary>
        private static void GrabGameView(RenderTexture target)
        {
            System.Collections.Generic.List<Canvas> hidden = new System.Collections.Generic.List<Canvas>();
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.enabled && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.enabled = false;
                    hidden.Add(c);
                }
            }

            Texture2D screen = ScreenCapture.CaptureScreenshotAsTexture();
            foreach (Canvas c in hidden) c.enabled = true;
            if (screen == null) return;

            float aspect = LevelThumbnails.Width / (float)LevelThumbnails.Height;
            float w = screen.width, h = screen.height;
            Vector2 scale = w / h > aspect ? new Vector2(h * aspect / w, 1f) : new Vector2(1f, w / aspect / h);
            Vector2 offset = new Vector2((1f - scale.x) * 0.5f, (1f - scale.y) * 0.5f);
            Graphics.Blit(screen, target, scale, offset);
            Destroy(screen);
        }

        private static void ReadBack(RenderTexture source, Texture2D into)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            into.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            into.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Nothing brighter than near-black anywhere: the render didn't land.</summary>
        private static bool IsBlack(Texture2D tex)
        {
            Color32[] px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i += 7)
            {
                if (px[i].r > 12 || px[i].g > 12 || px[i].b > 12) return false;
            }
            return true;
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
