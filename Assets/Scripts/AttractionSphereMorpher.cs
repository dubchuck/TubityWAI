using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TubityWAI
{
    public class AttractionSphereMorpher : MonoBehaviour
    {
        public int currentCount = 1;

        [Header("Ground Plane")]
        [Tooltip("Keep the whole formation clear of the hero platform, so the count " +
                 "selector stands on the same pad the menu sphere hovers over.")]
        public bool groundToPlane;
        public float groundPlaneY = -1.58f;
        public float groundGap = 0.12f;
        [Tooltip("Radius of one sphere at scale 1, used to work out how far the " +
                 "formation reaches.")]
        public float unitSphereRadius = 0.5f;
        private List<GameObject> activeSpheres = new List<GameObject>();

        /// <summary>Set by the sphere-count selector while the browsed count is a
        /// still-locked block, so newly morphed-in spheres pick up the same fade.</summary>
        private bool isLocked = false;

        /// <summary>
        /// Called for every sphere the formation creates, with its index. GameSetup
        /// uses it to dress each one in the equipped skin in that slot's gameplay
        /// colour, so the count selector previews exactly what the run will show.
        /// </summary>
        public System.Action<GameObject, int> decorateSphere;
        
        private float GetRadiusForCount(int count) => count == 1 ? 0f : 1.2f + (count * 0.05f);
        private float GetScaleForCount(int count) => 1.4f - ((count - 1) * 0.15f);
        
        private Coroutine morphCoroutine;

        /// <summary>
        /// How far the formation reaches from its centre: ring radius plus one
        /// sphere. Using the full extent rather than the lowest sphere means the
        /// cluster clears the pad no matter how the rotator has tilted it.
        /// </summary>
        private float FormationRadius(int count)
        {
            float local = GetRadiusForCount(count) + unitSphereRadius * GetScaleForCount(count);
            return local * Mathf.Abs(transform.lossyScale.y);
        }

        private void LateUpdate()
        {
            if (!groundToPlane) return;
            Vector3 p = transform.position;
            p.y = groundPlaneY + groundGap + FormationRadius(currentCount);
            transform.position = p;
        }
        
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
                if (decorateSphere != null) decorateSphere(newSphere, i);
            }
            
            // Destroy the template as it's no longer needed
            Destroy(templateSphere);

            UpdateSpherePositions(1f); // 1f = fully settled
            ApplyFadeToAll();
        }

        /// <summary>Re-dress every live sphere - after the shop equips a different skin.</summary>
        public void Redecorate()
        {
            if (decorateSphere == null) return;
            for (int i = 0; i < activeSpheres.Count; i++)
            {
                if (activeSpheres[i] != null) decorateSphere(activeSpheres[i], i);
            }
            ApplyFadeToAll();
        }

        /// <summary>The sphere-count selector calls this when the browsed count is a
        /// still-locked block, so the whole formation reads as dimmed/inactive.</summary>
        public void SetLocked(bool locked)
        {
            if (isLocked == locked) return;
            isLocked = locked;
            ApplyFadeToAll();
        }

        private void ApplyFadeToAll()
        {
            float amount = isLocked ? 1f : 0f;
            for (int i = 0; i < activeSpheres.Count; i++)
            {
                if (activeSpheres[i] == null) continue;
                NeonBandSphere bands = activeSpheres[i].GetComponent<NeonBandSphere>();
                if (bands != null) bands.SetFade(amount);
            }
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
            float duration = 0.25f; // quarter second morph for a snappier feel
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
                if (decorateSphere != null) decorateSphere(newSphere, activeSpheres.Count - 1);
            }

            // Clones do not inherit a source sphere's MaterialPropertyBlock override,
            // so re-apply the fade to everyone the instant the new count is known.
            ApplyFadeToAll();

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
            ApplyFadeToAll();
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
