using UnityEngine;
using UnityEditor;

public class MaterialGenerator : EditorWindow
{
    [MenuItem("Jx3/Materials/Generate All Materials")]
    static void GenerateAllMaterials()
    {
        string outputDir = "Assets/Art/Textures";
        if (!AssetDatabase.IsValidFolder(outputDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            AssetDatabase.CreateFolder("Assets/Art", "Textures");
        }

        CreateHeroMaterials(outputDir);
        CreateBossMaterial(outputDir);
        CreateWeaponMaterial(outputDir);
        CreateUIButtonMaterial(outputDir);
        CreateUIPanelMaterial(outputDir);

        AssetDatabase.Refresh();
        Debug.Log("[MaterialGenerator] All materials generated to " + outputDir);
    }

    static Material CreateMaterialAsset(string name, string shaderName, Color color, string dir)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        mat.name = name;
        AssetDatabase.CreateAsset(mat, dir + "/" + name + ".mat");
        return mat;
    }

    static void CreateHeroMaterials(string dir)
    {
        string[] heroQualities = new string[] { "Common", "Rare", "Epic", "Legendary" };
        Color[] heroColors = new Color[]
        {
            new Color(0.7f, 0.7f, 0.7f),
            new Color(0.3f, 0.6f, 0.3f),
            new Color(0.3f, 0.4f, 0.9f),
            new Color(1f, 0.7f, 0.1f)
        };

        for (int i = 0; i < heroQualities.Length; i++)
        {
            Material mat = CreateMaterialAsset(
                "Mat_Hero_" + heroQualities[i],
                "Unlit/Color",
                heroColors[i],
                dir
            );
            mat.SetFloat("_Glossiness", i > 1 ? 0.5f : 0f);
            EditorUtility.SetDirty(mat);
        }
        Debug.Log("[MaterialGenerator] 4 hero materials created (Toon style via Unlit/Color)");
    }

    static void CreateBossMaterial(string dir)
    {
        Material mat = CreateMaterialAsset("Mat_Boss", "Standard", new Color(0.3f, 0.05f, 0.05f), dir);
        mat.SetFloat("_Metallic", 0.3f);
        mat.SetFloat("_Glossiness", 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.8f, 0.1f, 0.05f) * 0.8f);
        EditorUtility.SetDirty(mat);
        Debug.Log("[MaterialGenerator] Boss material created (dark red + emission)");
    }

    static void CreateWeaponMaterial(string dir)
    {
        Material mat = CreateMaterialAsset("Mat_Weapon", "Standard", new Color(0.6f, 0.6f, 0.7f), dir);
        mat.SetFloat("_Metallic", 0.9f);
        mat.SetFloat("_Glossiness", 0.8f);
        EditorUtility.SetDirty(mat);
        Debug.Log("[MaterialGenerator] Weapon material created (metallic)");
    }

    static void CreateUIButtonMaterial(string dir)
    {
        Material mat = CreateMaterialAsset("Mat_UI_Button", "Unlit/Color", new Color(0.5f, 0.2f, 0.8f), dir);
        mat.SetFloat("_Glossiness", 0.3f);
        EditorUtility.SetDirty(mat);
        Debug.Log("[MaterialGenerator] UI_Button material created (gradient purple)");
    }

    static void CreateUIPanelMaterial(string dir)
    {
        Material mat = CreateMaterialAsset("Mat_UI_Panel", "Unlit/Color", new Color(0.08f, 0.06f, 0.12f, 0.85f), dir);
        mat.SetFloat("_Glossiness", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        EditorUtility.SetDirty(mat);
        Debug.Log("[MaterialGenerator] UI_Panel material created (semi-transparent dark)");
    }
}
