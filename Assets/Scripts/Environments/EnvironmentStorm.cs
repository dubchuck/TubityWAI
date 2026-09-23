using UnityEngine;
using UnityEngine.Rendering;

namespace TubityWAI
{
    /// <summary>
    /// Lightning for a stormy theme (palette.lightning strikes per second). Each strike is a bolt
    /// flickering in the cloud ahead, a directional flash that lights every prop at once, and the
    /// sky flaring (EnvironmentManager.FlashSky). All of it scales with the theme's weight, so a
    /// storm fades in and out with a blend. Lives on the EnvironmentManager's root, which rides with
    /// the camera, so bolts are placed relative to it.
    /// </summary>
    public class EnvironmentStorm : MonoBehaviour
    {
        public EnvironmentManager manager;
        public EnvironmentTheme theme;
        public float strikesPerSecond = 0.3f;

        /// <summary>The flash light's name, so EnvironmentManager never adopts it as a sun.</summary>
        public const string FlashLightName = "LightningFlash";

        private Light flashLight;
        private GameObject bolt;
        private MeshRenderer boltRenderer;
        private Material boltMaterial;
        private float nextStrike;
        private float strikeTime = -1f;
        private float strikeWeight;

        // A strike's shape over time: flash, dip, second flash, fade.
        private static readonly float[] FlickerKeys = { 0f, 1f, 0.25f, 0.9f, 0.4f, 0f };
        private const float StrikeLength = 0.55f;

        private void Start()
        {
            GameObject lightObj = new GameObject(FlashLightName);
            lightObj.transform.SetParent(transform, false);
            flashLight = lightObj.AddComponent<Light>();
            flashLight.type = LightType.Directional;
            flashLight.shadows = LightShadows.None;
            flashLight.color = new Color(0.8f, 0.87f, 1f);
            flashLight.intensity = 0f;
            flashLight.enabled = false;

            Shader glow = Shader.Find("TubityX/PlasmaGlow");
            if (glow != null)
            {
                boltMaterial = new Material(glow) { name = "LightningBolt" };
                boltMaterial.SetColor("_Color", new Color(0.8f, 0.88f, 1f));
                boltMaterial.SetFloat("_Intensity", 6f);
                boltMaterial.SetFloat("_RimPower", 0.6f);
                boltMaterial.SetFloat("_Flicker", 40f);

                bolt = new GameObject("LightningBolt");
                bolt.transform.SetParent(transform, false);
                bolt.AddComponent<MeshFilter>();
                boltRenderer = bolt.AddComponent<MeshRenderer>();
                boltRenderer.sharedMaterial = boltMaterial;
                boltRenderer.shadowCastingMode = ShadowCastingMode.Off;
                boltRenderer.receiveShadows = false;
                bolt.SetActive(false);
            }

            ScheduleNext(1f);
        }

        private void ScheduleNext(float weight)
        {
            float rate = Mathf.Max(0.01f, strikesPerSecond * Mathf.Max(0.05f, weight));
            // Exponential gaps read as natural; clamp the extremes so it neither machine-guns nor sulks.
            float gap = -Mathf.Log(Mathf.Max(0.001f, Random.value)) / rate;
            nextStrike = Time.time + Mathf.Clamp(gap, 0.6f, 12f);
        }

        private void Update()
        {
            float weight = manager != null ? manager.ThemeWeight(theme) : 1f;

            if (strikeTime < 0f && weight > 0.05f && Time.time >= nextStrike) Strike(weight);

            if (strikeTime >= 0f)
            {
                float u = (Time.time - strikeTime) / StrikeLength;
                if (u >= 1f)
                {
                    strikeTime = -1f;
                    flashLight.enabled = false;
                    if (bolt != null) bolt.SetActive(false);
                    if (manager != null) manager.FlashSky(0f);
                    ScheduleNext(weight);
                    return;
                }

                float f = Sample(u) * strikeWeight;
                flashLight.intensity = f * 1.4f;
                if (boltMaterial != null) boltMaterial.SetFloat("_Fade", f);
                if (manager != null) manager.FlashSky(f);
            }
        }

        private void Strike(float weight)
        {
            strikeTime = Time.time;
            strikeWeight = Mathf.Clamp01(weight);

            // Somewhere in the cloud ahead, off to one side, high up - and near enough that the
            // storm's thick fog (it closes in by 180 units) doesn't swallow it.
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 at = new Vector3(side * Random.Range(20f, 60f), Random.Range(20f, 45f), Random.Range(50f, 110f));

            flashLight.enabled = true;
            flashLight.transform.rotation = Quaternion.LookRotation(-at.normalized + Vector3.down * 0.3f, Vector3.up);

            if (bolt != null)
            {
                bolt.GetComponent<MeshFilter>().sharedMesh = WorldMeshes.Bolt(Random.Range(0, 6), Random.Range(45f, 75f), 0.5f);
                bolt.transform.localPosition = at;
                bolt.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));
                bolt.SetActive(true);
            }
        }

        private static float Sample(float u)
        {
            float x = Mathf.Clamp01(u) * (FlickerKeys.Length - 1);
            int i = Mathf.Min(FlickerKeys.Length - 2, (int)x);
            return Mathf.Lerp(FlickerKeys[i], FlickerKeys[i + 1], x - i);
        }
    }
}
