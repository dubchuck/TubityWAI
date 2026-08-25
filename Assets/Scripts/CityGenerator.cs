using UnityEngine;
using System.Collections.Generic;

namespace TubityWAI
{
    public class CityGenerator : MonoBehaviour
    {
        private static List<Material> cachedBuildingMaterials;
        private static bool materialsInitialized = false;

        public static void ClearMaterialCache()
        {
            materialsInitialized = false;
            cachedBuildingMaterials = null;
        }

        private static void InitializeMaterials()
        {
            if (materialsInitialized && cachedBuildingMaterials != null) return;

            cachedBuildingMaterials = new List<Material>();

            // Significantly muted, very dark flat colors (no tiled textures)
            Color[] flatDarkColors = new Color[]
            {
                new Color(0.015f, 0.025f, 0.06f),  // Dark Slate Blue
                new Color(0.02f,  0.02f,  0.035f), // Deep Charcoal Grey
                new Color(0.035f, 0.015f, 0.055f), // Muted Midnight Purple
                new Color(0.02f,  0.035f, 0.05f),  // Dark Steel Blue-Grey
                new Color(0.04f,  0.02f,  0.045f)  // Deep Dark Violet
            };

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Sprites/Default");
            if (litShader == null) litShader = Shader.Find("Unlit/Color");

            for (int i = 0; i < flatDarkColors.Length; i++)
            {
                Color c = flatDarkColors[i];

                Material mat = (litShader != null) ? new Material(litShader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
                mat.name = "FlatDarkBuildingMat_" + i;

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", c);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", c);

                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.2f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);

                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", Color.black);
                }

                cachedBuildingMaterials.Add(mat);
            }

            materialsInitialized = true;
        }

        public static void GenerateCityForSegment(GameObject segmentParent, float segmentLength, float tubeRadius, LevelConfig config)
        {
            InitializeMaterials();

            // Destroy existing city container under segment if present
            Transform existingContainer = segmentParent.transform.Find("CityFlybyContainer");
            if (existingContainer != null)
            {
                Destroy(existingContainer.gameObject);
            }

            GameObject cityContainer = new GameObject("CityFlybyContainer");
            cityContainer.transform.SetParent(segmentParent.transform, false);

            float segmentZ = segmentParent.transform.position.z;

            // Deterministic random seed based on segment position
            Random.State prevState = Random.state;
            Random.InitState((int)(segmentZ * 17f));

            int buildingCount = Random.Range(6, 10); // 6 to 9 buildings per segment
            float minSafeRadius = tubeRadius + 4.0f;  // Strict clearance radius from tube axis

            for (int i = 0; i < buildingCount; i++)
            {
                float localZ = Random.Range(1f, segmentLength - 1f);
                float absoluteZ = segmentZ + localZ;

                Vector3 curveOffset = Vector3.zero;
                if (config != null)
                {
                    curveOffset = config.GetCurveOffset(absoluteZ);
                }

                // Pick random radial angle around tube
                float angleRad = Random.Range(-1.2f, 1.2f) * Mathf.PI;
                Vector3 radialDir = new Vector3(Mathf.Sin(angleRad), -Mathf.Cos(angleRad), 0f).normalized;

                // Base placement distance starts safely outside tube radius
                float baseDist = minSafeRadius + Random.Range(1.0f, 25.0f);

                Material facadeMat = cachedBuildingMaterials[Random.Range(0, cachedBuildingMaterials.Count)];
                int buildingType = Random.Range(0, 4);

                GameObject building = null;

                switch (buildingType)
                {
                    case 0:
                        building = BuildSkyscraper(cityContainer.transform, facadeMat);
                        break;
                    case 1:
                        building = BuildCyberMonolith(cityContainer.transform, facadeMat);
                        break;
                    case 2:
                        building = BuildSpireTower(cityContainer.transform, facadeMat);
                        break;
                    case 3:
                        building = BuildMidRiseBlock(cityContainer.transform, facadeMat);
                        break;
                }

                if (building != null)
                {
                    // Orient building so its height points RADIALLY OUTWARDS away from the tube
                    Quaternion outwardRot = Quaternion.LookRotation(Vector3.forward, radialDir);
                    
                    // Apply subtle random Y rotation offset around its outward axis
                    outwardRot *= Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);

                    building.transform.localRotation = outwardRot;

                    // Position building base and push outward until all corners pass safe clearance
                    PositionAndEnforceClearance(building, curveOffset, radialDir, baseDist, localZ, minSafeRadius);
                }
            }

            Random.state = prevState;
        }

        private static void PositionAndEnforceClearance(GameObject building, Vector3 curveOffset, Vector3 radialDir, float baseDist, float localZ, float minSafeRadius)
        {
            MeshFilter mf = building.GetComponent<MeshFilter>();
            Bounds bounds = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);

            float currentDist = baseDist;
            int maxIterations = 20;

            for (int attempt = 0; attempt < maxIterations; attempt++)
            {
                // Place building with bottom of bounding box at currentDist along radialDir
                float pivotOffsetY = bounds.extents.y; // Primitive pivots are centered, so shift up by half height
                Vector3 centerPos = new Vector3(curveOffset.x, curveOffset.y, localZ) + radialDir * (currentDist + pivotOffsetY);
                building.transform.localPosition = centerPos;

                // Check 8 bounding box corners against tube axis curve clearance
                bool intersects = false;
                Vector3 ext = bounds.extents;

                Vector3[] localCorners = new Vector3[8]
                {
                    new Vector3(-ext.x, -ext.y, -ext.z),
                    new Vector3( ext.x, -ext.y, -ext.z),
                    new Vector3(-ext.x,  ext.y, -ext.z),
                    new Vector3( ext.x,  ext.y, -ext.z),
                    new Vector3(-ext.x, -ext.y,  ext.z),
                    new Vector3( ext.x, -ext.y,  ext.z),
                    new Vector3(-ext.x,  ext.y,  ext.z),
                    new Vector3( ext.x,  ext.y,  ext.z)
                };

                for (int c = 0; c < 8; c++)
                {
                    Vector3 worldCorner = building.transform.TransformPoint(localCorners[c]);
                    Vector2 corner2D = new Vector2(worldCorner.x, worldCorner.y);
                    Vector2 curve2D = new Vector2(curveOffset.x, curveOffset.y);

                    if (Vector2.Distance(corner2D, curve2D) < minSafeRadius)
                    {
                        intersects = true;
                        break;
                    }
                }

                if (!intersects)
                {
                    break; // Clearance confirmed!
                }

                // If any corner is inside safe radius, push further outward
                currentDist += 2.0f;
            }
        }

        private static GameObject BuildSkyscraper(Transform parent, Material mat)
        {
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "AbstractSkyscraper";
            building.transform.SetParent(parent, false);

            float width = Random.Range(4f, 7.5f);
            float height = Random.Range(20f, 45f);
            float depth = Random.Range(4f, 7.5f);
            building.transform.localScale = new Vector3(width, height, depth);

            building.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(building.GetComponent<Collider>());
            return building;
        }

        private static GameObject BuildCyberMonolith(Transform parent, Material mat)
        {
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "CyberMonolith";
            building.transform.SetParent(parent, false);

            float width = Random.Range(5f, 9f);
            float height = Random.Range(16f, 32f);
            float depth = Random.Range(5f, 9f);
            building.transform.localScale = new Vector3(width, height, depth);

            building.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(building.GetComponent<Collider>());
            return building;
        }

        private static GameObject BuildSpireTower(Transform parent, Material mat)
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "SpireTower";
            tower.transform.SetParent(parent, false);

            float diam = Random.Range(4f, 7f);
            float height = Random.Range(24f, 50f);
            tower.transform.localScale = new Vector3(diam, height * 0.5f, diam); // Cylinder default length is 2 units

            tower.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(tower.GetComponent<Collider>());
            return tower;
        }

        private static GameObject BuildMidRiseBlock(Transform parent, Material mat)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "MidRiseBlock";
            block.transform.SetParent(parent, false);

            float width = Random.Range(7f, 13f);
            float height = Random.Range(12f, 25f);
            float depth = Random.Range(7f, 13f);
            block.transform.localScale = new Vector3(width, height, depth);

            block.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(block.GetComponent<Collider>());
            return block;
        }
    }
}
