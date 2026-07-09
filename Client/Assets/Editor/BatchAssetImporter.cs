using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Jx3.Editor
{
    /// <summary>
    /// 批量导入 Asset Store 资源包
    /// 菜单: Tools > 批量导入资源包
    /// </summary>
    public class BatchAssetImporter : EditorWindow
    {
        private string resourceDir = "";
        private Vector2 scrollPos;
        private List<string> packages = new();
        private List<bool> selected = new();
        private bool imported = false;

        [MenuItem("Tools/批量导入资源包")]
        static void ShowWindow()
        {
            var w = GetWindow<BatchAssetImporter>("批量导入资源包");
            w.minSize = new Vector2(600, 500);
        }

        void OnEnable()
        {
            // 默认路径: 项目根目录的 resource 文件夹
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            resourceDir = Path.Combine(projectRoot, "resource");
            ScanPackages();
        }

        void ScanPackages()
        {
            packages.Clear();
            selected.Clear();
            if (!Directory.Exists(resourceDir)) return;

            var files = Directory.GetFiles(resourceDir, "*.unitypackage", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var relPath = Path.GetRelativePath(resourceDir, f);
                packages.Add(relPath);
                selected.Add(true); // 默认全选
            }
        }

        void OnGUI()
        {
            GUILayout.Label("批量导入 Asset Store 资源包", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // 路径选择
            GUILayout.Label("资源目录:", EditorStyles.miniLabel);
            var newDir = GUILayout.TextField(resourceDir);
            if (newDir != resourceDir)
            {
                resourceDir = newDir;
                ScanPackages();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("浏览...", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFolderPanel("选择资源目录", resourceDir, "");
                if (!string.IsNullOrEmpty(path))
                {
                    resourceDir = path;
                    ScanPackages();
                }
            }
            if (GUILayout.Button("重新扫描", GUILayout.Width(80)))
                ScanPackages();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 全选/反选
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60)))
                for (int i = 0; i < selected.Count; i++) selected[i] = true;
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
                for (int i = 0; i < selected.Count; i++) selected[i] = false;
            GUILayout.Label($"共 {packages.Count} 个包", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 列表
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (int i = 0; i < packages.Count; i++)
            {
                GUILayout.BeginHorizontal();
                selected[i] = GUILayout.Toggle(selected[i], "", GUILayout.Width(20));
                var pkgName = Path.GetFileNameWithoutExtension(packages[i]);
                var folder = Path.GetDirectoryName(packages[i]);
                GUILayout.Label(pkgName, GUILayout.Width(250));
                GUILayout.Label(folder, EditorStyles.miniLabel);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // 导入按钮
            int count = 0;
            foreach (var s in selected) if (s) count++;

            GUI.enabled = count > 0 && !imported;
            if (GUILayout.Button($"导入选中的 {count} 个资源包", GUILayout.Height(40)))
            {
                ImportSelected();
            }
            GUI.enabled = true;

            if (imported)
                GUILayout.Label("导入完成！请查看 Console 日志。", EditorStyles.boldLabel);
        }

        void ImportSelected()
        {
            imported = false;
            int total = 0;
            foreach (var s in selected) if (s) total++;

            int current = 0;
            for (int i = 0; i < packages.Count; i++)
            {
                if (!selected[i]) continue;
                current++;

                var fullPath = Path.Combine(resourceDir, packages[i]);
                var pkgName = Path.GetFileNameWithoutExtension(packages[i]);
                var folder = Path.GetDirectoryName(packages[i]);

                EditorUtility.DisplayProgressBar(
                    "导入资源包",
                    $"[{current}/{total}] {pkgName}\n{folder}",
                    (float)current / total);

                Debug.Log($"[BatchImport] [{current}/{total}] Importing: {packages[i]}");

                try
                {
                    AssetDatabase.ImportPackage(fullPath, interactive: false);
                    Debug.Log($"[BatchImport] ✅ Success: {packages[i]}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BatchImport] ❌ Failed: {packages[i]}\n{ex.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            imported = true;
            Debug.Log($"[BatchImport] === 导入完成! 共 {total} 个包 ===");
        }
    }
}