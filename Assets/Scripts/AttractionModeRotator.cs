using UnityEngine;

namespace TubityWAI
{
    public class AttractionModeRotator : MonoBehaviour
    {
        public Vector3 rotationSpeed = Vector3.zero;
        public Space rotationSpace = Space.Self;

        [Header("Z-Axis Translation & Wrapping")]
        public float localZSpeed = 0f;
        public float wrapMinZ = 2f;
        public float wrapMaxZ = 60f;

        [Header("Camera Z Follow (World Space)")]
        public bool followCameraZ = false;
        public float followOffsetZ = 0f;

        private void Update()
        {
            if (rotationSpeed != Vector3.zero)
            {
                transform.Rotate(rotationSpeed * Time.deltaTime, rotationSpace);
            }

            if (localZSpeed != 0f)
            {
                Vector3 pos = transform.localPosition;
                pos.z += localZSpeed * Time.deltaTime;
                if (localZSpeed < 0f && pos.z < wrapMinZ)
                {
                    pos.z = wrapMaxZ;
                }
                else if (localZSpeed > 0f && pos.z > wrapMaxZ)
                {
                    pos.z = wrapMinZ;
                }
                transform.localPosition = pos;
            }

            if (followCameraZ && Camera.main != null)
            {
                Vector3 pos = transform.position;
                pos.z = Camera.main.transform.position.z + followOffsetZ;
                transform.position = pos;
            }
        }
    }
}
