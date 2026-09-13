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
        [Tooltip("Also build one segment behind z = 0 so the camera, which trails the player, starts inside the tube.")]
        public bool spawnSegmentBehindStart = true;

        [Header("Materials")]
        public Material tunnelMaterial;
        public Material markerMaterial;
        public Material[] coinMaterials;
        public Material powerupMaterial;
        public Material magnetMaterial;
        public Material obstacleMaterial;
        public Material[] transparentObstacleMaterials;
        public float obstacleSpawnProbability = 0.45f;

        [Header("Attraction Settings")]
        public bool spawnCoins = true;
        public bool spawnObstacles = true;

        private Queue<GameObject> activeSegments = new Queue<GameObject>();
        private float nextSpawnZ = 0f;

        private void Start()
        {
            if (target == null)
            {
                target = FindFirstObjectByType<PlayerController>();
            }

            // Start one segment early so the trailing camera never looks at the tube's open end.
            if (spawnSegmentBehindStart)
            {
                nextSpawnZ = -segmentLength;
            }

            // Generate initial set of tunnel segments
            for (int i = 0; i < activeSegmentsCount; i++)
            {
                SpawnSegment();
            }
        }

        private void Update()
        {
            float cameraZ = 0f;
            if (target != null)
            {
                cameraZ = target.zPos - 10f; // Safe fallback
            }
            if (Camera.main != null)
            {
                cameraZ = Camera.main.transform.position.z;
            }
            else if (target == null)
            {
                return;
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
                        segmentScript.spawnCoins = spawnCoins;
                        segmentScript.spawnObstacles = spawnObstacles;
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
            segment.powerupMaterial = powerupMaterial;
            segment.magnetMaterial = magnetMaterial;
            segment.obstacleMaterial = obstacleMaterial;
            segment.transparentObstacleMaterials = transparentObstacleMaterials;
            segment.obstacleSpawnProbability = obstacleSpawnProbability;
            segment.spawnCoins = this.spawnCoins;
            segment.spawnObstacles = this.spawnObstacles;

            activeSegments.Enqueue(segObj);
            nextSpawnZ += segmentLength;
        }
    }
}
