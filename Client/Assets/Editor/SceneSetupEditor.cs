using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class SceneSetupEditor : EditorWindow
{
    [MenuItem("指尖江湖2/场景搭建/初始化所有场景")]
    static void SetupAllScenes()
    {
        var scenes = new[] { "Boot", "Login", "MainCity", "DungeonSelect", "Battle", "PVP" };
        foreach (var scene in scenes)
        {
            var path = "Assets/Scenes/" + scene + ".unity";
            var loaded = EditorSceneManager.GetSceneByPath(path);
            if (!loaded.IsValid())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
        }
        Debug.Log("[SceneSetup] All scenes loaded. Please set lighting and save.");
    }

    [MenuItem("指尖江湖2/场景搭建/应用默认光照设置")]
    static void ApplyDefaultLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.12f, 0.2f);
        RenderSettings.ambientIntensity = 0.6f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.06f, 0.12f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.015f;
        
        var light = FindObjectOfType<Light>();
        if (light == null)
        {
            var lightGo = new GameObject("Directional Light", typeof(Light));
            light = lightGo.GetComponent<Light>();
        }
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        light.color = new Color(0.8f, 0.75f, 0.9f);
        light.intensity = 0.8f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.6f;
        
        Debug.Log("[SceneSetup] Default lighting applied");
    }

    [MenuItem("指尖江湖2/场景搭建/打开所有场景")]
    static void OpenAllScenes()
    {
        var scenes = new[] { "Boot", "Login", "MainCity", "DungeonSelect", "Battle", "PVP" };
        foreach (var s in scenes)
        {
            var path = "Assets/Scenes/" + s + ".unity";
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        Debug.Log("[SceneSetup] All scenes opened in additive mode");
    }
}