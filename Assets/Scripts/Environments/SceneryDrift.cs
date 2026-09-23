using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Ambient motion for a scenery prop: a slow orbit around the tube axis plus
    /// optional surge (along the tube), sway (tangential) and radial breathing,
    /// all evaluated in the tube's own cylindrical frame so a prop can never
    /// wander into the tube wall. Cheap enough to run on a dozen objects per
    /// segment; Configure() is called after every placement.
    /// </summary>
    public class SceneryDrift : MonoBehaviour
    {
        // --- self rotation -------------------------------------------------
        public Vector3 spin;               // degrees/sec about the prop's own axes
        public float bobAmplitude;         // extra radial bob, folded in with radialAmplitude
        public float bobSpeed = 1f;

        // --- motion around / along the tube --------------------------------
        public float orbitSpeed;           // degrees/sec around the tube axis (signed)
        public float swayAmplitude;        // tangential travel, world units
        public float swaySpeed = 0.35f;
        public float surgeAmplitude;       // forward/back along the tube, world units
        public float surgeSpeed = 0.3f;
        public float radialAmplitude;      // in/out from the tube, world units
        public float radialSpeed = 0.25f;
        public float tiltAmplitude;        // degrees of lean, for anchored props
        public float tiltSpeed = 0.5f;

        /// <summary>Anchored props re-point their local up along the radial direction as they orbit.</summary>
        public bool alignToRadial;

        private Vector2 tubeCenter;        // tube axis in the segment's local space
        private float baseAngle;           // 0 points straight down, matching the tube's vertex layout
        private float baseRadius;
        private float baseZ;
        private float minRadius;           // never closer to the axis than this
        private Quaternion localRotation;  // yaw about the prop's own up (alignToRadial) or its full rotation
        private float orbitAngle;
        private float phaseSway, phaseSurge, phaseRadial, phaseBob, phaseTilt;
        private bool configured;

        /// <summary>
        /// Hand the prop its slot in the tube's cylindrical frame. <paramref name="safeRadius"/>
        /// is the closest the prop may come to the axis without touching the tube.
        /// </summary>
        public void Configure(Vector2 center, float angle, float radius, float z, float safeRadius, Quaternion rotation)
        {
            tubeCenter = center;
            baseAngle = angle;
            baseRadius = radius;
            baseZ = z;
            minRadius = safeRadius;
            localRotation = rotation;

            orbitAngle = 0f;
            phaseSway = Random.Range(0f, Mathf.PI * 2f);
            phaseSurge = Random.Range(0f, Mathf.PI * 2f);
            phaseRadial = Random.Range(0f, Mathf.PI * 2f);
            phaseBob = Random.Range(0f, Mathf.PI * 2f);
            phaseTilt = Random.Range(0f, Mathf.PI * 2f);
            configured = true;
        }

        private void Update()
        {
            if (!configured) return;

            float t = Time.time;
            orbitAngle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

            float radius = baseRadius;
            if (radialAmplitude > 0f) radius += Mathf.Sin(t * radialSpeed + phaseRadial) * radialAmplitude;
            if (bobAmplitude > 0f) radius += Mathf.Sin(t * bobSpeed + phaseBob) * bobAmplitude;
            if (radius < minRadius) radius = minRadius;

            float angle = baseAngle + orbitAngle;
            if (swayAmplitude > 0f) angle += Mathf.Sin(t * swaySpeed + phaseSway) * (swayAmplitude / radius);

            float z = baseZ;
            if (surgeAmplitude > 0f) z += Mathf.Sin(t * surgeSpeed + phaseSurge) * surgeAmplitude;

            Vector3 radial = new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f);
            Vector3 pos = new Vector3(tubeCenter.x, tubeCenter.y, z) + radial * radius;

            if (alignToRadial)
            {
                Quaternion rot = Quaternion.LookRotation(Vector3.forward, radial);
                if (tiltAmplitude > 0f)
                {
                    // Lean about the tube's tangent so the prop rocks over its own base.
                    float lean = Mathf.Sin(t * tiltSpeed + phaseTilt) * tiltAmplitude;
                    rot *= Quaternion.Euler(0f, 0f, lean);
                }
                transform.localRotation = rot * localRotation;
            }
            else if (spin != Vector3.zero)
            {
                transform.Rotate(spin * Time.deltaTime, Space.Self);
            }

            transform.localPosition = pos;
        }
    }
}
