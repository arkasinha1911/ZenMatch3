using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class GlowMaterialCreator
{
    static GlowMaterialCreator()
    {
        EditorApplication.delayCall += CreateMaterial;
    }

    private static void CreateMaterial()
    {
        string matPath = "Assets/Materials/GlowMaterial2D.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            Shader shader = Shader.Find("Custom/GlowPiece2D");
            if (shader != null)
            {
                Material mat = new Material(shader);
                // Set default glow properties
                mat.SetColor("_GlowColor", new Color(0.2f, 0.8f, 1.0f, 1.0f)); // Nice neon blue default
                mat.SetFloat("_GlowIntensity", 3.0f);
                mat.SetFloat("_GlowPulseSpeed", 3.0f);
                
                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created GlowMaterial2D successfully!");
            }
        }
    }
}
