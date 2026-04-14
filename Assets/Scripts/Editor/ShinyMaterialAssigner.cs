using UnityEngine;
using UnityEditor;
using System.IO;

public class ShinyMaterialAssigner : EditorWindow
{
    [MenuItem("ZenMatch3/Apply Shiny Material to Game Pieces")]
    public static void ApplyShinyMaterial()
    {
        string shaderPath = "Custom/ShinyPiece2D";
        Shader shinyShader = Shader.Find(shaderPath);
        
        if (shinyShader == null)
        {
            Debug.LogError("Could not find the ShinyPiece2D shader. Please ensure it compiled correctly.");
            return;
        }

        string matPath = "Assets/Materials/ShinyPieceMaterial.mat";
        
        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        Material shinyMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (shinyMat == null)
        {
            shinyMat = new Material(shinyShader);
            AssetDatabase.CreateAsset(shinyMat, matPath);
            Debug.Log("Created ShinyPieceMaterial at " + matPath);
        }
        else
        {
            shinyMat.shader = shinyShader; // Update shader just in case
        }
        
        // Apply to specific shape prefabs
        string[] prefabsToUpdate = new string[] 
        {
            "Circle", "Square", "Triangle", "Hexagon Point Top", "Isometric Diamond", "Blank",
            "HBomb", "VBomb", "SBomb" // Apply to powerups if they exist
        };

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        int updateCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            bool shouldUpdate = false;
            foreach (string name in prefabsToUpdate)
            {
                if (prefab.name == name)
                {
                    shouldUpdate = true;
                    break;
                }
            }

            if (shouldUpdate)
            {
                SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sharedMaterial != shinyMat)
                {
                    sr.sharedMaterial = shinyMat;
                    EditorUtility.SetDirty(prefab);
                    updateCount++;
                    Debug.Log("Applied Shiny Material to " + prefab.name);
                }
            }
        }

        if (updateCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully updated {updateCount} prefabs with the Shiny Material!");
        }
        else
        {
            Debug.Log("All prefabs already have the Shiny Material, or none were found.");
        }
    }
}
