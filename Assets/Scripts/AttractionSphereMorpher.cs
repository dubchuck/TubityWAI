using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TubityWAI
{
    public class AttractionSphereMorpher : MonoBehaviour
    {
        public int currentCount = 1;
        private List<GameObject> activeSpheres = new List<GameObject>();
        
        private float GetRadiusForCount(int count) => count == 1 ? 0f : 1.2f + (count * 0.05f);
        private float GetScaleForCount(int count) => 1.4f - ((count - 1) * 0.15f);
        
        private Coroutine morphCoroutine;
        
        // This is called initially by GameSetup to pass the template sphere (which has the equipped skin)
        public void Initialize(GameObject templateSphere, int initialCount)
        {
            // Destroy existing children
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            activeSpheres.Clear();

            currentCount = initialCount;
            
            for (int i = 0; i < currentCount; i++)
            {
                GameObject newSphere = Instantiate(templateSphere, transform);
                newSphere.name = "MorphSphere_" + i;
                newSphere.SetActive(true);
                activeSpheres.Add(newSphere);
            }
            
            // Destroy the template as it's no longer needed
            Destroy(templateSphere);
            
            UpdateSpherePositions(1f); // 1f = fully settled
        }

        public void SetSphereCount(int newCount)
        {
            if (newCount == currentCount || newCount < 1 || newCount > 5) return;
            
            if (morphCoroutine != null)
            {
                StopCoroutine(morphCoroutine);
            }
            
            morphCoroutine = StartCoroutine(MorphRoutine(newCount));
        }

        private IEnumerator MorphRoutine(int targetCount)
        {
            float duration = 0.5f; // half second morph
            float elapsed = 0f;
            
            int startCount = currentCount;
            currentCount = targetCount;
            
            // If we need more spheres, instantiate them now but keep them hidden/scaled to 0
            while (activeSpheres.Count < targetCount)
            {
                GameObject newSphere = Instantiate(activeSpheres[0], transform);
                newSphere.name = "MorphSphere_" + activeSpheres.Count;
                newSphere.transform.localScale = Vector3.zero;
                activeSpheres.Add(newSphere);
            }

            // Target angles for the new setup
            float targetAngleStep = 360f / targetCount;
            float startAngleStep = 360f / startCount;

            float startRadius = GetRadiusForCount(startCount);
            float targetRadius = GetRadiusForCount(targetCount);
            float startScale = GetScaleForCount(startCount);
            float targetScale = GetScaleForCount(targetCount);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                bool isCollapsing = t < 0.5f;
                float phaseT = isCollapsing ? (t * 2f) : ((t - 0.5f) * 2f);
                
                // Elastic/Bouncy easing
                float easeT = phaseT * phaseT * (3f - 2f * phaseT); 
                // A "Buckyball" / cell division squash and stretch effect
                float stretchFactor = 1f + Mathf.Sin(phaseT * Mathf.PI) * 0.8f; 
                float smoothT = t * t * (3f - 2f * t);

                for (int i = 0; i < activeSpheres.Count; i++)
                {
                    GameObject sphere = activeSpheres[i];
                    
                    if (i < targetCount)
                    {
                        float angle = i * targetAngleStep * Mathf.Deg2Rad;
                        Vector3 targetPos = new Vector3(Mathf.Sin(angle) * targetRadius, Mathf.Cos(angle) * targetRadius, 0f);
                        if (targetCount == 1) targetPos = Vector3.zero;
                        
                        Vector3 startPos = Vector3.zero; 
                        if (i < startCount)
                        {
                            float sAngle = i * startAngleStep * Mathf.Deg2Rad;
                            startPos = new Vector3(Mathf.Sin(sAngle) * startRadius, Mathf.Cos(sAngle) * startRadius, 0f);
                            if (startCount == 1) startPos = Vector3.zero;
                        }
                        
                        float currentScale = Mathf.Lerp((i < startCount) ? startScale : 0f, targetScale, smoothT);

                        if (isCollapsing)
                        {
                            sphere.transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, easeT);
                            Vector3 dir = startPos.normalized;
                            if (dir == Vector3.zero) dir = Vector3.up;
                            Quaternion look = Quaternion.LookRotation(Vector3.forward, dir);
                            sphere.transform.localRotation = look;
                            sphere.transform.localScale = new Vector3(currentScale / stretchFactor, currentScale * stretchFactor, currentScale / stretchFactor);
                        }
                        else
                        {
                            sphere.transform.localPosition = Vector3.Lerp(Vector3.zero, targetPos, easeT);
                            Vector3 dir = targetPos.normalized;
                            if (dir == Vector3.zero) dir = Vector3.up;
                            Quaternion look = Quaternion.LookRotation(Vector3.forward, dir);
                            sphere.transform.localRotation = look;
                            sphere.transform.localScale = new Vector3(currentScale / stretchFactor, currentScale * stretchFactor, currentScale / stretchFactor);
                        }
                    }
                    else
                    {
                        // Spheres being removed (merging inward)
                        if (isCollapsing)
                        {
                            float sAngle = i * startAngleStep * Mathf.Deg2Rad;
                            Vector3 startPos = new Vector3(Mathf.Sin(sAngle) * startRadius, Mathf.Cos(sAngle) * startRadius, 0f);
                            sphere.transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, easeT);
                            
                            float currentScale = Mathf.Lerp(startScale, 0f, phaseT); // Scale down during collapse
                            sphere.transform.localScale = new Vector3(currentScale / stretchFactor, currentScale * stretchFactor, currentScale / stretchFactor);
                        }
                        else
                        {
                            sphere.transform.localPosition = Vector3.zero;
                            sphere.transform.localScale = Vector3.zero;
                        }
                    }
                }
                
                yield return null;
            }

            // Cleanup removed spheres
            for (int i = activeSpheres.Count - 1; i >= targetCount; i--)
            {
                Destroy(activeSpheres[i]);
                activeSpheres.RemoveAt(i);
            }

            // Ensure final positions are exact
            UpdateSpherePositions(1f);
        }

        private void UpdateSpherePositions(float settleAmount)
        {
            float targetRadius = GetRadiusForCount(currentCount);
            float targetScale = GetScaleForCount(currentCount);

            float angleStep = 360f / currentCount;
            for (int i = 0; i < currentCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Sin(angle) * targetRadius, Mathf.Cos(angle) * targetRadius, 0f);
                if (currentCount == 1) pos = Vector3.zero; // Center single sphere
                activeSpheres[i].transform.localPosition = pos;
                activeSpheres[i].transform.localRotation = Quaternion.identity;
                activeSpheres[i].transform.localScale = new Vector3(targetScale, targetScale, targetScale);
            }
        }
    }
}
