using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    public class TunnelGenerator : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("The player controller to track.")]
        public PlayerController target;

        [Header("Tunnel Dimensions")]
        public float radius = 5f;
        public float segmentLength = 20f;
        public int radialSegments = 32;
        public float markerInterval = 5f;

        [Header("Pool Settings")]
        [Tooltip("Number of segments to pre-warm and keep active.")]
        public int activeSegmentsCount = 6;

        [Header("Materials")]
        public Material tunnelMaterial;
        public Material markerMaterial;
        public Material[] coinMaterials;
        public Material obstacleMaterial;
        public Material[] transparentObstacleMaterials;
        public float obstacleSpawnProbability = 0.45f;

        private Queue<GameObject> activeSegments = new Queue<GameObject>();
        private float nextSpawnZ = 0f;

        private void Start()
        {
            if (target == null)
            {
                target = FindFirstObjectByType<PlayerController>();
            }

            // Generate initial set of tunnel segments
            for (int i = 0; i < activeSegmentsCount; i++)
            {
                SpawnSegment();
            }
        }

        private void Update()
        {
            if (target == null) return;

            // Query the main camera's actual Z position to ensure segments only recycle
            // when they are completely behind the camera's field of view.
            float cameraZ = target.zPos - 10f; // Safe fallback
            if (Camera.main != null)
            {
                cameraZ = Camera.main.transform.position.z;
            }

            // Recycle the oldest segment once its end (Z start + segmentLength) is behind the camera
            if (activeSegments.Count > 0)
            {
                GameObject oldestSegment = activeSegments.Peek();
                if (cameraZ > oldestSegment.transform.position.z + segmentLength)
                {
                    activeSegments.Dequeue();
                    
                    // Name update for easy hierarchy viewing
                    oldestSegment.name = "TunnelSegment_" + (nextSpawnZ / segmentLength);
                    
                    // Reposition and regenerate coins for the new location
                    TunnelSegment segmentScript = oldestSegment.GetComponent<TunnelSegment>();
                    if (segmentScript != null)
                    {
                        segmentScript.ResetSegment(nextSpawnZ);
                    }
                    else
                    {
                        oldestSegment.transform.position = new Vector3(0f, 0f, nextSpawnZ);
                    }
                    
                    activeSegments.Enqueue(oldestSegment);
                    nextSpawnZ += segmentLength;
                }
            }
        }

        private void SpawnSegment()
        {
            GameObject segObj = new GameObject("TunnelSegment_" + (nextSpawnZ / segmentLength));
            segObj.transform.SetParent(this.transform);
            segObj.transform.position = new Vector3(0f, 0f, nextSpawnZ);

            TunnelSegment segment = segObj.AddComponent<TunnelSegment>();
            segment.radius = radius;
            segment.length = segmentLength;
            segment.radialSegments = radialSegments;
            segment.markerInterval = markerInterval;
            segment.tunnelMaterial = tunnelMaterial;
            segment.markerMaterial = markerMaterial;
            segment.coinMaterials = coinMaterials;
            segment.obstacleMaterial = obstacleMaterial;
            segment.transparentObstacleMaterials = transparentObstacleMaterials;
            segment.obstacleSpawnProbability = obstacleSpawnProbability;

            activeSegments.Enqueue(segObj);
            nextSpawnZ += segmentLength;
        }
    }
}
