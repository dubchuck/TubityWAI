using UnityEngine;
using UnityEditor;

public class SetupLogoMaterial
{
    [MenuItem("Tubity/Setup Neon Logo Material")]
    public static void SetupMaterial()
    {
        string matPath = "Assets/Materials/UI/Mat_NeonLogoSheen.mat";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/UI"))
            AssetDatabase.CreateFolder("Assets/Materials", "UI");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("UI/NeonGradientSheen");
            if (shader == null)
            {
                Debug.LogError("Shader UI/NeonGradientSheen not found!");
                return;
            }
            mat = new Material(shader);
            
            // Set properties
            mat.SetColor("_LeftColor", new Color(0f, 1f, 1f, 1f)); // Cyan
            mat.SetColor("_RightColor", new Color(1f, 0f, 0.8f, 1f)); // Magenta
            mat.SetColor("_SheenColor", Color.white);
            mat.SetFloat("_SheenWidth", 0.1f);
            mat.SetFloat("_SheenSpeed", 0.7f);
            mat.SetFloat("_SheenAngle", -0.5f);
            
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created Material: " + matPath);
        }
        else
        {
            Debug.Log("Material already exists: " + matPath);
        }
        
        // Find logo in scene and apply
        GameObject logo = GameObject.Find("MenuLogo");
        if (logo != null)
        {
            UnityEngine.UI.Image img = logo.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.material = mat;
                Debug.Log("Applied material to MenuLogo Image.");
            }
            UnityEngine.UI.Text txt = logo.GetComponent<UnityEngine.UI.Text>();
            if (txt != null)
            {
                txt.material = mat;
                Debug.Log("Applied material to MenuLogo Text.");
            }
        }
        else
        {
            Debug.LogWarning("MenuLogo object not found in scene. Generate UI first.");
        }
    }
}
