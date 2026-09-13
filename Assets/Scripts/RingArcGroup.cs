using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Drives the motion of one ring of obstacle arcs. The arcs are children of this
    /// object, so rotating it about the tube axis moves them (and their trigger
    /// colliders) together as a group.
    /// </summary>
    public class RingArcGroup : MonoBehaviour
    {
        public enum Motion { Static, Spin, Snap, Oscillate }

        public Motion motion = Motion.Static;

        [Header("Spin")]
        [Tooltip("Signed rotation rate in degrees per second.")]
        public float spinDegPerSec = 45f;

        [Header("Snap")]
        [Tooltip("Seconds between snaps.")]
        public float snapInterval = 2f;
        [Tooltip("Signed angle turned per snap (90 or 180).")]
        public float snapAngle = 90f;
        [Tooltip("How long the snap turn itself takes.")]
        public float snapTweenDuration = 0.2f;

        [Header("Snap Telegraph")]
        [Tooltip("Seconds before a snap during which the arcs flash and swell.")]
        public float telegraphWindow = 0.7f;
        [Tooltip("Uniform radial scale of the group at the moment of the snap (1 = no swell).")]
        public float telegraphSwell = 1.05f;

        [Header("Oscillate")]
        public float oscillateAmplitudeDeg = 45f;
        public float oscillatePeriod = 2f;
        [Tooltip("Starting phase in radians. Set by the spawner from the level seed so runs repeat exactly.")]
        public float oscillatePhase = 0f;

        private float baseAngle;
        private float angle;
        private float timer;
        private float phase;
        private bool snapping;
        private float snapFrom;
        private float snapTo;
        private float snapT;
        private Obstacle[] arcs;

        private void Start()
        {
            baseAngle = transform.localEulerAngles.z;
            angle = baseAngle;
            arcs = GetComponentsInChildren<Obstacle>();
            // Phase comes from the seeded spawner so neighbouring pendulums differ but every run matches.
            phase = oscillatePhase;
        }

        private void Update()
        {
            if (motion == Motion.Static) return;

            float dt = Time.deltaTime;
            switch (motion)
            {
                case Motion.Spin:
                    angle += spinDegPerSec * dt;
                    break;

                case Motion.Snap:
                    if (!snapping)
                    {
                        timer += dt;

                        // Wind-up: flash and swell over the last telegraphWindow seconds before the turn.
                        float timeToSnap = snapInterval - timer;
                        float warn = (telegraphWindow > 0f) ? Mathf.Clamp01(1f - timeToSnap / telegraphWindow) : 0f;
                        ApplyTelegraph(warn, true);

                        if (timer >= snapInterval)
                        {
                            timer = 0f;
                            snapping = true;
                            snapFrom = angle;
                            snapTo = angle + snapAngle;
                            snapT = 0f;
                        }
                    }
                    if (snapping)
                    {
                        snapT += dt / Mathf.Max(0.01f, snapTweenDuration);
                        if (snapT >= 1f)
                        {
                            snapping = false;
                            angle = snapTo;
                            ApplyTelegraph(0f, false);
                        }
                        else
                        {
                            // Ease-out so the turn reads as a decisive click rather than a drift.
                            float e = 1f - (1f - snapT) * (1f - snapT);
                            angle = Mathf.Lerp(snapFrom, snapTo, e);
                            // Hold the warning look through the turn and release it as the arcs settle.
                            ApplyTelegraph(1f - e, false);
                        }
                    }
                    break;

                case Motion.Oscillate:
                    timer += dt;
                    float w = 2f * Mathf.PI / Mathf.Max(0.05f, oscillatePeriod);
                    angle = baseAngle + Mathf.Sin(timer * w + phase) * oscillateAmplitudeDeg;
                    break;
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// Drives the pre-snap warning on every arc of this ring: emission flashes towards white
        /// and the whole ring swells radially. warn is 0 (calm) to 1 (about to turn). With blink
        /// on, the flash strobes faster as the turn approaches so the rhythm itself is the cue.
        /// </summary>
        private void ApplyTelegraph(float warn, bool blink)
        {
            warn = Mathf.Clamp01(warn);
            float strength = warn;
            if (blink && warn > 0f)
            {
                float blinkHz = Mathf.Lerp(3f, 9f, warn);
                float wave = 0.6f + 0.4f * Mathf.Sin(Time.time * blinkHz * 2f * Mathf.PI);
                strength = warn * wave;
            }

            float swell = 1f + (telegraphSwell - 1f) * warn;
            transform.localScale = new Vector3(swell, swell, 1f);

            if (arcs == null || arcs.Length == 0) arcs = GetComponentsInChildren<Obstacle>();
            for (int i = 0; i < arcs.Length; i++)
            {
                if (arcs[i] != null) arcs[i].SetTelegraph(strength);
            }
        }
    }
}
