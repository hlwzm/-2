using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class SceneBuilderEditor : EditorWindow
{
    private static readonly string MatsDir = "Assets/Art/Environments/Materials";
    private static readonly string[] SceneNames = { "Boot", "Login", "MainCity", "DungeonSelect", "Battle", "PVP" };
    private static readonly Color AmbientColor = new Color(0.102f, 0.102f, 0.180f);
    private static readonly Color WarmLightColor = new Color(1f, 0.85f, 0.6f);
    private static readonly Color FogColor = new Color(0.45f, 0.35f, 0.55f);

    [MenuItem("Jx3/Scene/Setup All Scenes")]
    static void SetupAllScenes()
    {
        if (!EditorUtility.DisplayDialog("搭建所有场景", "将为6个场景创建3D环境。继续？", "开始", "取消"))
            return;

        EditorUtility.DisplayProgressBar("场景搭建", "生成材质库...", 0.05f);
        CreateMaterialLibrary();

        for (int i = 0; i < SceneNames.Length; i++)
        {
            float p = (float)(i + 1) / SceneNames.Length;
            EditorUtility.DisplayProgressBar("场景搭建", "处理: " + SceneNames[i], p);
            SetupScene(SceneNames[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        Debug.Log("[SceneBuilder] All scenes done!");
        EditorUtility.DisplayDialog("完成", "全部6个场景已搭建完毕！", "确定");
    }

    // ===== 材质库 =====
    static void CreateMaterialLibrary()
    {
        EnsureFolder(MatsDir);
        string[][] mats = new string[][] {
            new[]{"Ground_Grass","0.15,0.20,0.12,1","0"},
            new[]{"Wall_Stone","0.30,0.28,0.32,1","0.3"},
            new[]{"Wall_Wood","0.35,0.25,0.15,1","0"},
            new[]{"Platform_Stone","0.35,0.32,0.38,1","0.2"},
            new[]{"Arena_Floor","0.20,0.18,0.22,1","0.1"},
            new[]{"Building_Wall","0.25,0.22,0.28,1","0.1"},
            new[]{"Boundary_Wall","0.15,0.12,0.20,0.3","0"},
            new[]{"Marble_White","0.70,0.70,0.75,1","0.3"},
            new[]{"Floor_Dark","0.12,0.10,0.16,1","0"},
            new[]{"Portal_Glow","0.40,0.20,0.60,0.5","0"},
        };
        float step = 1f / (mats.Length + 1);
        for (int i = 0; i < mats.Length; i++)
        {
            EditorUtility.DisplayProgressBar("场景搭建", "材质: " + mats[i][0], step * (i + 1));
            string path = MatsDir + "/" + mats[i][0] + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;
            var parts = mats[i][1].Split(',');
            var c = new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
            var mat = new Material(Shader.Find("Standard"));
            if (mat.shader == null) { Debug.LogWarning("Shader not found"); continue; }
            mat.name = mats[i][0];
            mat.color = c;
            mat.SetFloat("_Metallic", float.Parse(mats[i][2]));
            mat.SetFloat("_Glossiness", 0.1f);
            if (c.a < 1f)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.renderQueue = 3000;
            }
            AssetDatabase.CreateAsset(mat, path);
        }
        CreateSkyboxMat();
        AssetDatabase.Refresh();
        Debug.Log("[SceneBuilder] " + (mats.Length + 1) + " materials created.");
    }

    static void CreateSkyboxMat()
    {
        string path = MatsDir + "/Skybox_Gradient.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var shader = Shader.Find("Skybox/Procedural");
        if (shader == null) { Debug.LogWarning("Skybox shader not found"); return; }
        var mat = new Material(shader);
        mat.name = "Skybox_Gradient";
        mat.SetColor("_SkyTint", new Color(0.35f, 0.20f, 0.55f));
        mat.SetColor("_GroundColor", new Color(0.08f, 0.06f, 0.12f));
        AssetDatabase.CreateAsset(mat, path);
    }

    static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            current += "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(current))
            {
                string parent = Path.GetDirectoryName(current).Replace("\\", "/");
                string name = Path.GetFileName(current);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    // ===== 场景搭建 =====
    static void SetupScene(string sceneName)
    {
        string scenePath = "Assets/Scenes/" + sceneName + ".unity";
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        ClearGenerated();
        CreateEnvironment(sceneName);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] Setup: " + sceneName);
    }

    static void ClearGenerated()
    {
        var objs = new List<GameObject>();
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name.StartsWith("_ENV_")) objs.Add(go);
        foreach (var go in objs) Object.DestroyImmediate(go);
    }

    static void CreateEnvironment(string name)
    {
        var root = new GameObject("_ENV_SceneRoot");
        // var env removed
        // env removed

        // Skybox
        var skyMat = AssetDatabase.LoadAssetAtPath<Material>(MatsDir + "/Skybox_Gradient.mat");
        if (skyMat != null) RenderSettings.skybox = skyMat;
        RenderSettings.ambientLight = AmbientColor;
        RenderSettings.fog = true;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.02f;

        // Directional light
        var lightGo = new GameObject("_ENV_DirectionalLight");
        lightGo.transform.SetParent(root.transform);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = WarmLightColor;
        light.intensity = 0.8f;
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Ground
        var groundMat = LoadMat("Ground_Grass");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "_ENV_Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.position = new Vector3(0, -0.01f, 0);
        ground.transform.localScale = new Vector3(20, 1, 20);
        ground.GetComponent<MeshRenderer>().material = groundMat ?? new Material(Shader.Find("Standard"));

        // Boundary walls
        var wallMat = LoadMat("Boundary_Wall");
        float s = 40;
        AddWall(root, new Vector3(s / 2, 5, 0), new Vector3(1, 10, s), wallMat);
        AddWall(root, new Vector3(-s / 2, 5, 0), new Vector3(1, 10, s), wallMat);
        AddWall(root, new Vector3(0, 5, s / 2), new Vector3(s, 10, 1), wallMat);
        AddWall(root, new Vector3(0, 5, -s / 2), new Vector3(s, 10, 1), wallMat);

        // Scene-specific features
        switch (name)
        {
            case "Login": BuildLogin(root); break;
            case "MainCity": BuildMainCity(root); break;
            case "DungeonSelect": BuildDungeonSelect(root); break;
            case "Battle": BuildBattle(root); break;
            case "PVP": BuildPVP(root); break;
        }
    }

    static Material LoadMat(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(MatsDir + "/" + name + ".mat");
    }

    static void AddWall(GameObject parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "_ENV_Wall";
        wall.transform.SetParent(parent.transform);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        if (mat != null) wall.GetComponent<MeshRenderer>().material = mat;
    }

    static void AddCube(GameObject parent, Vector3 pos, Vector3 scale, Material mat, string name)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent.transform);
        cube.transform.position = pos;
        cube.transform.localScale = scale;
        if (mat != null) cube.GetComponent<MeshRenderer>().material = mat;
    }

    // ===== 各场景特有元素 =====
    static void BuildLogin(GameObject root)
    {
        var stoneMat = LoadMat("Platform_Stone");
        var marbleMat = LoadMat("Marble_White");
        // Main platform
        AddCube(root, new Vector3(0, 0.5f, 0), new Vector3(15, 1, 10), stoneMat, "_ENV_Login_Platform");
        AddCube(root, new Vector3(0, 1.5f, -3), new Vector3(10, 1, 4), marbleMat, "_ENV_Login_Upper");
        // Mountains (simple cones from cubes)
        for (int i = 0; i < 3; i++)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m.name = "_ENV_Mountain";
            m.transform.SetParent(root.transform);
            m.transform.position = new Vector3(-15 + i * 15, 5, -20);
            m.transform.localScale = new Vector3(10 - i * 2, 10 - i * 2, 8 - i);
            m.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.10f, 0.18f) };
        }
    }

    static void BuildMainCity(GameObject root)
    {
        var stoneMat = LoadMat("Platform_Stone");
        var buildMat = LoadMat("Building_Wall");
        var marbleMat = LoadMat("Marble_White");
        // Central plaza
        AddCube(root, new Vector3(0, 0.3f, 0), new Vector3(30, 0.6f, 30), stoneMat, "_ENV_City_Plaza");
        // Center statue base
        AddCube(root, new Vector3(0, 1, 0), new Vector3(3, 2, 3), marbleMat, "_ENV_City_Statue");
        // 4 streets with buildings
        for (int dir = 0; dir < 4; dir++)
        {
            float angle = dir * 90 * Mathf.Deg2Rad;
            float cx = Mathf.Sin(angle) * 20;
            float cz = Mathf.Cos(angle) * 20;
            for (int b = 0; b < 4; b++)
            {
                float bx = cx + Mathf.Sin(angle) * (b * 5 + 3);
                float bz = cz + Mathf.Cos(angle) * (b * 5 + 3);
                AddCube(root, new Vector3(bx, 3, bz), new Vector3(5, 6, 5), buildMat, "_ENV_Building");
            }
        }
    }

    static void BuildDungeonSelect(GameObject root)
    {
        var portalMat = LoadMat("Portal_Glow");
        var stoneMat = LoadMat("Platform_Stone");
        // Center platform
        AddCube(root, new Vector3(0, 0.5f, 0), new Vector3(20, 1, 12), stoneMat, "_ENV_DS_Platform");
        // Portal arches
        for (int i = 0; i < 5; i++)
        {
            float angle = (i - 2) * 25 * Mathf.Deg2Rad;
            float px = Mathf.Sin(angle) * 8;
            float pz = Mathf.Cos(angle) * 8;
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "_ENV_Portal_" + i;
            frame.transform.SetParent(root.transform);
            frame.transform.position = new Vector3(px, 3, pz);
            frame.transform.localScale = new Vector3(3, 6, 1);
            frame.GetComponent<MeshRenderer>().material = portalMat ?? stoneMat;
        }
    }

    static void BuildBattle(GameObject root)
    {
        var arenaMat = LoadMat("Arena_Floor");
        var stoneMat = LoadMat("Wall_Stone");
        // Arena ring
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "_ENV_Arena";
        ring.transform.SetParent(root.transform);
        ring.transform.position = new Vector3(0, 0.2f, 0);
        ring.transform.localScale = new Vector3(8, 0.4f, 8);
        ring.GetComponent<MeshRenderer>().material = arenaMat ?? new Material(Shader.Find("Standard"));
        // Pillars
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30 * Mathf.Deg2Rad;
            float px = Mathf.Sin(a) * 7;
            float pz = Mathf.Cos(a) * 7;
            AddCube(root, new Vector3(px, 2.5f, pz), new Vector3(0.8f, 5, 0.8f), stoneMat, "_ENV_Pillar");
        }
    }

    static void BuildPVP(GameObject root)
    {
        var floorMat = LoadMat("Floor_Dark");
        var stoneMat = LoadMat("Wall_Stone");
        // Arena floor
        AddCube(root, new Vector3(0, 0.2f, 0), new Vector3(10, 0.4f, 10), floorMat, "_ENV_PVP_Floor");
        // Fence
        var fenceMat = LoadMat("Wall_Wood");
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90 * Mathf.Deg2Rad;
            float fx = Mathf.Sin(a) * 5.5f;
            float fz = Mathf.Cos(a) * 5.5f;
            AddCube(root, new Vector3(fx, 1.5f, fz), new Vector3(10, 3, 0.5f), fenceMat, "_ENV_Fence");
        }
        // Fire pillars at corners
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90 * Mathf.Deg2Rad + 45 * Mathf.Deg2Rad;
            float fx = Mathf.Sin(a) * 7;
            float fz = Mathf.Cos(a) * 7;
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "_ENV_FirePillar";
            pillar.transform.SetParent(root.transform);
            pillar.transform.position = new Vector3(fx, 2, fz);
            pillar.transform.localScale = new Vector3(0.5f, 4, 0.5f);
            pillar.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard")) { color = new Color(0.8f, 0.2f, 0.1f) };
        }
    }
}