using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 指尖江湖2 - Art文件夹结构自动生成工具
/// 菜单: Jx3/Art/Create Art Folder Structure
/// </summary>
public class CreateArtFolders : EditorWindow
{
    [MenuItem("Jx3/Art/Create Art Folder Structure")]
    static void CreateFolders()
    {
        string artRoot = "Assets/Art";

        string[] folders = new string[]
        {
            // 环境相关
            "Environments/Materials",
            "Environments/Meshes",
            "Environments/Textures",

            // 模型分类
            "Models/Heroes",
            "Models/Enemies",
            "Models/NPCs",
            "Models/Props",
            "Models/Weapons",
            "Models/Vehicles",

            // 纹理
            "Textures/Environments",
            "Textures/Characters",
            "Textures/UI",

            // 动画
            "Animations/Heroes",
            "Animations/Enemies",

            // 预制体
            "Prefabs/Scenes",
            "Prefabs/Props",
            "Prefabs/UI",

            // 其他
            "Fonts",
            "Shaders",
            "VFX",
            "Audio",
        };

        int created = 0;
        foreach (var folder in folders)
        {
            string fullPath = Path.Combine(artRoot, folder);
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                string parent = Path.GetDirectoryName(fullPath).Replace("\\", "/");
                string newDir = Path.GetFileName(fullPath);
                string result = AssetDatabase.CreateFolder(parent, newDir);
                if (!string.IsNullOrEmpty(result))
                {
                    created++;
                    Debug.Log($"[CreateArtFolders] Created: {fullPath}");
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[CreateArtFolders] Done! Created {created} folders.");
        EditorUtility.DisplayDialog("Art文件夹结构", $"创建完成！\n共创建 {created} 个文件夹。", "确定");
    }

    [MenuItem("Jx3/Art/Create Art Folder Structure", true)]
    static bool ValidateCreateFolders()
    {
        return true;
    }
}
