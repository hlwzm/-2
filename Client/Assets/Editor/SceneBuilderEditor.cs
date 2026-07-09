using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 指尖江湖2 - 全场景3D环境搭建工具
/// 菜单: Jx3/Scene/Setup All Scenes
/// 程序化生成：材质库 → 地面/天空盒/光照/雾 → 边界墙 → 场景特色元素
/// </summary>
public class SceneBuilderEditor : EditorWindow
{
    private static readonly string ScenesDir = "Assets/Scenes";
    private static readonly string MatsDir = "Assets/Art/Environments/Materials";
    private static readonly string[] SceneNames = { "Boot", "Login", "MainCity", "DungeonSelect", "Battle", "PVP" };

    private static readonly Color AmbientColor = new Color(0.102f, 0.102f, 0.180f);
    private static readonly Color WarmLightColor = new Color(1f, 0.85f, 0.6f);
    private static readonly Color FogColor = new Color(0.45f, 0.35f, 0.55f);

    [MenuItem("Jx3/Scene/Setup All Scenes")]
    static void SetupAllScenes()
    {
        if (!EditorUtility.DisplayDialog("搭建所有场景", "将自动为全部6个场景创建3D环境。\n\n此操作会修改场景文件，确定继续？", "开始", "取消"))
            return;

        EditorUtility.DisplayProgressBar("场景搭建", "正在生成材质库...", 0f);
        CreateMaterialLibrary();

        for (int i = 0; i < SceneNames.Length; i++)
        {
            float progress = (float)(i + 1) / SceneNames.Length;
            EditorUtility.DisplayProgressBar("场景搭建", $"正在处理: {SceneNames[i]}...", progress);
            SetupScene(SceneNames[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        Debug.Log("[SceneBuilder] All scenes setup complete!");
        EditorUtility.DisplayDialog("场景搭建完成", "全部6个场景已搭建完毕！", "确定");
    }

    [MenuItem("Jx3/Scene/Setup Current Scene Only")]
    static void SetupCurrentScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        string sceneName = Path.GetFileNameWithoutExtension(activeScene.path);
        CreateMaterialLibrary();
        SetupScene(sceneName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SceneBuilder] Scene '{sceneName}' setup complete.");
    }

    [MenuItem("Jx3/Scene/Setup Current Scene Only", true)]
    static bool ValidateCurrentScene()
    {
        return !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path);
    }

    // ================================================================
    // 材质库
    // ================================================================

    static void CreateMaterialLibrary()
    {
        EnsureFolder(MatsDir);
        CreateGroundMaterial();
        CreateWallStoneMaterial();
        CreateWallWoodMaterial();
        CreateSkyboxGradientMaterial();
        CreatePlatformMaterial();
        CreateArenaMaterial();
        CreatePortalGlowMaterial();
        CreateBuildingMaterial();
        CreateBoundaryWallMaterial();
        CreateMarbleMaterial();
        CreateFloorDarkMaterial();
        Debug.Log("[SceneBuilder] Material library created.");
    }

    static Material CreateMaterialFile(string name, Color color, float metallic, float smoothness)
    {
        string path = MatsDir + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void MakeTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    static Material CreateGroundMaterial()
    {
        string path = MatsDir + "/Ground_Grass.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Standard"));
        mat.name = "Ground_Grass";
        mat.color = new Color(0.15f, 0.20f, 0.12f);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.1f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material CreateWallStoneMaterial()
    {
        return CreateMaterialFile("Wall_Stone", new Color(0.30f, 0.28f, 0.32f), 0.3f, 0.2f);
    }

    static Material CreateWallWoodMaterial()
    {
        return CreateMaterialFile("Wall_Wood", new Color(0.35f, 0.25f, 0.15f), 0f, 0.1f);
    }

    static Material CreateSkyboxGradientMaterial()
    {
        string path = MatsDir + "/Skybox_Gradient.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Skybox/Procedural"));
        mat.name = "Skybox_Gradient";
        mat.SetFloat("_SunSize", 0.01f);
        mat.SetFloat("_SunSizeConvergence", 0f);
        mat.SetFloat("_AtmosphereThickness", 0.6f);
        mat.SetColor("_SkyTint", new Color(0.35f, 0.20f, 0.55f));
        mat.SetColor("_GroundColor", new Color(0.08f, 0.06f, 0.12f));
        mat.SetFloat("_Exposure", 0.4f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material CreatePlatformMaterial()
    {
        return CreateMaterialFile("Platform_Stone", new Color(0.25f, 0.22f, 0.30f), 0.4f, 0.3f);
    }

    static Material CreateArenaMaterial()
    {
        return CreateMaterialFile("Arena_Floor", new Color(0.22f, 0.18f, 0.28f), 0.5f, 0.3f);
    }

    static Material CreatePortalGlowMaterial()
    {
        string path = MatsDir + "/Portal_Glow.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Standard"));
        mat.name = "Portal_Glow";
        mat.color = new Color(0.40f, 0.20f, 0.70f, 0.6f);
        mat.SetFloat("_Metallic", 0.5f);
        mat.SetFloat("_Glossiness", 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.30f, 0.10f, 0.60f));
        MakeTransparent(mat);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material CreateBuildingMaterial()
    {
        return CreateMaterialFile("Building_Wall", new Color(0.20f, 0.18f, 0.25f), 0.2f, 0.2f);
    }

    static Material CreateBoundaryWallMaterial()
    {
        string path = MatsDir + "/Boundary_Wall.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Standard"));
        mat.name = "Boundary_Wall";
        mat.color = new Color(0.35f, 0.25f, 0.50f, 0.15f);
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Glossiness", 0.3f);
        MakeTransparent(mat);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material CreateMarbleMaterial()
    {
        return CreateMaterialFile("Marble_White", new Color(0.75f, 0.72f, 0.80f), 0.6f, 0.7f);
    }

    static Material CreateFloorDarkMaterial()
    {
        return CreateMaterialFile("Floor_Dark", new Color(0.12f, 0.10f, 0.16f), 0.1f, 0.15f);
    }

    // ================================================================
    // 场景设置核心
    // ================================================================

    static void SetupScene(string sceneName)
    {
        string scenePath = ScenesDir + "/" + sceneName + ".unity";
        if (!File.Exists(scenePath))
        {
            Debug.LogError($"[SceneBuilder] Scene not found: {scenePath}");
            return;
        }

        EditorSceneManager.OpenScene(scenePath);
        CleanGeneratedElements();
        SetupLighting(sceneName);
        SetupSkybox();
        CreateGround(sceneName);
        CreateBoundaryWalls(sceneName);
        CreateSceneElements(sceneName);
        SetupCamera(sceneName);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[SceneBuilder] '{sceneName}' setup OK.");
    }

    static void CleanGeneratedElements()
    {
        var rootObjects = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        var toDelete = new List<GameObject>();
        foreach (var go in rootObjects)
        {
            if (go.name.StartsWith("_SceneElements_") ||
                go.name.StartsWith("_BoundaryWall_") ||
                go.name.StartsWith("_Ground_") ||
                go.name.StartsWith("_DirectionalLight_") ||
                go.name.Contains("BoundaryWall"))
            {
                toDelete.Add(go);
            }
        }
        foreach (var go in toDelete) DestroyImmediate(go);
    }

    // ================================================================
    // 光照系统
    // ================================================================

    static void SetupLighting(string sceneName)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColor;
        RenderSettings.ambientIntensity = 0.7f;

        RenderSettings.fog = true;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = sceneName == "DungeonSelect" ? 0.025f : 0.015f;

        var lightGo = GameObject.Find("_DirectionalLight_");
        if (lightGo == null) lightGo = new GameObject("_DirectionalLight_", typeof(Light));

        var light = lightGo.GetComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        light.color = WarmLightColor;
        light.intensity = 0.9f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.5f;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.4f;
    }

    // ================================================================
    // 天空盒
    // ================================================================

    static void SetupSkybox()
    {
        var skyMat = AssetDatabase.LoadAssetAtPath<Material>(MatsDir + "/Skybox_Gradient.mat");
        if (skyMat != null) RenderSettings.skybox = skyMat;
    }

    // ================================================================
    // 地面
    // ================================================================

    static void CreateGround(string sceneName)
    {
        var groundGo = new GameObject("_Ground_Plane", typeof(MeshFilter), typeof(MeshRenderer));
        groundGo.transform.position = Vector3.zero;
        var mesh = Resources.GetBuiltinResource<Mesh>("Plane.fbx");
        groundGo.GetComponent<MeshFilter>().sharedMesh = mesh;

        float scale = sceneName == "MainCity" ? 60 : (sceneName == "Battle" || sceneName == "PVP" ? 30 : 40);
        groundGo.transform.localScale = new Vector3(scale, 1, scale);

        Material groundMat = (sceneName == "MainCity" || sceneName == "Login")
            ? AssetDatabase.LoadAssetAtPath<Material>(MatsDir + "/Ground_Grass.mat")
            : AssetDatabase.LoadAssetAtPath<Material>(MatsDir + "/Floor_Dark.mat");
        if (groundMat == null) groundMat = new Material(Shader.Find("Standard"));
        groundGo.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
        groundGo.AddComponent<MeshCollider>();
    }

    // ================================================================
    // 边界墙
    // ================================================================

    static void CreateBoundaryWalls(string sceneName)
    {
        float size = sceneName == "MainCity" ? 60 : (sceneName == "Battle" || sceneName == "PVP" ? 30 : 40);
        float half = size / 2f;
        float wallHeight = sceneName == "MainCity" ? 3f : 2f;
        float wallThickness = 0.5f;

        var wallMat = AssetDatabase.LoadAssetAtPath<Material>(MatsDir + "/Boundary_Wall.mat");

        (string name, Vector3 pos, Vector3 scale)[] walls = {
            ("North", new Vector3(0, wallHeight/2, half), new Vector3(size+2, wallHeight, wallThickness)),
            ("South", new Vector3(0, wallHeight/2, -half), new Vector3(size+2, wallHeight, wallThickness)),
            ("East", new Vector3(half, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, size+2)),
            ("West", new Vector3(-half, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, size+2)),
        };

        foreach (var w in walls)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "_BoundaryWall_" + w.name;
            wall.transform.position = w.pos;
            wall.transform.localScale = w.scale;
            wall.GetComponent<BoxCollider>().enabled = true;
            if (wallMat != null) wall.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
        }
    }

    // ================================================================
    // 摄像机
    // ================================================================

    static void SetupCamera(string sceneName)
    {
        var cam = Camera.main;
        if (cam == null) return;
        switch (sceneName)
        {
            case "Boot":      cam.transform.SetPositionAndRotation(new Vector3(0, 2, -5), Quaternion.Euler(10, 0, 0)); break;
            case "Login":     cam.transform.SetPositionAndRotation(new Vector3(0, 4, -12), Quaternion.Euler(15, 0, 0)); break;
            case "MainCity":  cam.transform.SetPositionAndRotation(new Vector3(0, 15, -25), Quaternion.Euler(30, 0, 0)); break;
            case "DungeonSelect": cam.transform.SetPositionAndRotation(new Vector3(0, 5, -12), Quaternion.Euler(18, 0, 0)); break;
            case "Battle":    cam.transform.SetPositionAndRotation(new Vector3(0, 8, -14), Quaternion.Euler(25, 0, 0)); break;
            case "PVP":       cam.transform.SetPositionAndRotation(new Vector3(0, 8, -12), Quaternion.Euler(28, 0, 0)); break;
        }
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 60;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200;
    }

    // ================================================================
    // 场景特色元素
    // ================================================================

    static void CreateSceneElements(string sceneName)
    {
        var container = new GameObject("_SceneElements_" + sceneName);
        container.transform.position = Vector3.zero;
        switch (sceneName)
        {
            case "MainCity": BuildMainCity(container); break;
            case "Battle": BuildBattle(container); break;
            case "DungeonSelect": BuildDungeonSelect(container); break;
            case "Login": BuildLogin(container); break;
            case "PVP": BuildPVP(container); break;
        }
    }

    // ================================================================
    // MainCity - 中央广场 + 4街道 + 建筑轮廓(盒体)
    // ================================================================

    static Material LoadMat(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(MatsDir + "/" + name + ".mat") ?? new Material(Shader.Find("Standard"));
    }

    static void BuildMainCity(GameObject parent)
    {
        var stoneMat = LoadMat("Platform_Stone");
        var buildingMat = LoadMat("Building_Wall");
        var marbleMat = LoadMat("Marble_White");
        var grassMat = LoadMat("Ground_Grass");

        // 中央广场 - 圆形
        var plaza = CreatePrim("Plaza_Center", PrimitiveType.Cylinder, parent, new Vector3(8, 0.15f, 8), new Vector3(0, 0.15f, 0), stoneMat);

        // 雕像底座
        var pedestal = CreatePrim("Pedestal", PrimitiveType.Cylinder, parent, new Vector3(1.5f, 0.5f, 1.5f), new Vector3(0, 0.5f, 0), marbleMat);
        var statuePillar = CreatePrim("Statue_Pillar", PrimitiveType.Cylinder, parent, new Vector3(0.4f, 2f, 0.4f), new Vector3(0, 1.7f, 0), marbleMat);
        var statueOrb = CreatePrim("Statue_Orb", PrimitiveType.Sphere, parent, new Vector3(0.8f, 0.8f, 0.8f), new Vector3(0, 3f, 0), marbleMat);

        // 广场灯柱（8根）
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45 * Mathf.Deg2Rad;
            float x = Mathf.Sin(a) * 7f, z = Mathf.Cos(a) * 7f;
            CreatePrim("LampPillar_"+i, PrimitiveType.Cylinder, parent, new Vector3(0.15f, 1.5f, 0.15f), new Vector3(x, 0.75f, z), stoneMat);
            CreatePrim("LampOrb_"+i, PrimitiveType.Sphere, parent, new Vector3(0.3f, 0.3f, 0.3f), new Vector3(x, 1.8f, z), marbleMat);
        }

        // 4条街道
        string[] dirs = { "North", "South", "East", "West" };
        Vector3[] offsets = {
            new Vector3(0, 0.05f, 14f), new Vector3(0, 0.05f, -14f),
            new Vector3(14f, 0.05f, 0), new Vector3(-14f, 0.05f, 0)
        };
        Vector3[] scales = {
            new Vector3(3, 0.1f, 25), new Vector3(3, 0.1f, 25),
            new Vector3(25, 0.1f, 3), new Vector3(25, 0.1f, 3)
        };
        for (int i = 0; i < 4; i++)
            CreatePrim("Street_"+dirs[i], PrimitiveType.Cube, parent, scales[i], offsets[i], stoneMat);

        // 建筑轮廓（沿街道两侧排列）
        var rand = new System.Random(42);
        Vector3[] axes = { new Vector3(0,0,1), new Vector3(0,0,-1), new Vector3(1,0,0), new Vector3(-1,0,0) };
        for (int d = 0; d < 4; d++)
        {
            for (int b = 0; b < 4; b++)
            {
                float dist = 18f + b * 4f;
                float w = 2f + (float)rand.NextDouble() * 1.5f;
                float h = 2f + (float)rand.NextDouble() * 2.5f;
                float dep = 2f + (float)rand.NextDouble() * 1.5f;
                float sideOff = ((b % 2 == 0 ? 1 : -1) * (3f / 2 + w / 2 + 1f));

                Vector3 pos;
                if (d < 2) pos = new Vector3(sideOff, h/2, (d==0?1:-1)*dist);
                else pos = new Vector3((d==2?1:-1)*dist, h/2, sideOff);

                CreatePrim($"Building_{dirs[d]}_{b}", PrimitiveType.Cube, parent, new Vector3(w, h, dep), pos, buildingMat);
                CreatePrim($"Roof_{dirs[d]}_{b}", PrimitiveType.Cube, parent, new Vector3(w*0.8f, 0.2f, dep*0.8f), pos + new Vector3(0, h/2+0.1f, 0), buildingMat);
            }
        }
    }

    // ================================================================
    // Battle - 圆形竞技场 + 立柱
    // ================================================================

    static void BuildBattle(GameObject parent)
    {
        var arenaMat = LoadMat("Arena_Floor");
        var stoneMat = LoadMat("Wall_Stone");

        // 主平台
        CreatePrim("Arena_Platform", PrimitiveType.Cylinder, parent, new Vector3(8, 0.2f, 8), new Vector3(0, 0, 0), arenaMat);

        // 12根立柱环绕
        int pillarCount = 12;
        for (int i = 0; i < pillarCount; i++)
        {
            float angle = i * (360f / pillarCount) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * 7.5f, z = Mathf.Cos(angle) * 7.5f;
            CreatePrim("ArenaPillar_"+i, PrimitiveType.Cylinder, parent, new Vector3(0.3f, 2.5f, 0.3f), new Vector3(x, 1.25f, z), stoneMat);
            CreatePrim("PillarCap_"+i, PrimitiveType.Sphere, parent, new Vector3(0.25f, 0.25f, 0.25f), new Vector3(x, 2.8f, z), stoneMat);
        }

        // 中央战斗标记
        var markerMat = new Material(Shader.Find("Standard"));
        markerMat.color = new Color(0.6f, 0.3f, 0.8f);
        CreatePrim("Center_Marker", PrimitiveType.Cylinder, parent, new Vector3(0.5f, 0.05f, 0.5f), new Vector3(0, 0.15f, 0), markerMat);
    }

    // ================================================================
    // DungeonSelect - 传送门风格
    // ================================================================

    static void BuildDungeonSelect(GameObject parent)
    {
        var portalMat = LoadMat("Portal_Glow");
        var stoneMat = LoadMat("Wall_Stone");
        var darkMat = LoadMat("Floor_Dark");

        // 中央高台
        CreatePrim("Portal_Pedestal", PrimitiveType.Cylinder, parent, new Vector3(6, 0.2f, 6), new Vector3(0, 0.1f, 0), stoneMat);

        // 5个传送门（弧形排列）
        int portalCount = 5;
        for (int i = 0; i < portalCount; i++)
        {
            float t = (float)i / (portalCount - 1) - 0.5f;
            float angle = t * 60 * Mathf.Deg2Rad;
            float radius = 5f;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius - 2f;

            // 门框 - 左右柱 + 顶梁
            CreatePrim("PortalFrame_L_"+i, PrimitiveType.Cube, parent, new Vector3(0.2f, 2.5f, 0.2f), new Vector3(x-0.6f, 1.25f, z), portalMat);
            CreatePrim("PortalFrame_R_"+i, PrimitiveType.Cube, parent, new Vector3(0.2f, 2.5f, 0.2f), new Vector3(x+0.6f, 1.25f, z), portalMat);
            CreatePrim("PortalFrame_T_"+i, PrimitiveType.Cube, parent, new Vector3(1.4f, 0.2f, 0.2f), new Vector3(x, 2.7f, z), portalMat);

            // 传送门光幕 - 半透明
            var curtainMat = new Material(Shader.Find("Standard"));
            curtainMat.color = new Color(0.35f, 0.15f, 0.65f, 0.3f);
            MakeTransparent(curtainMat);
            CreatePrim("Portal_Curtain_"+i, PrimitiveType.Cube, parent, new Vector3(1.0f, 2.0f, 0.05f), new Vector3(x, 1.2f, z), curtainMat);

            // 地面光圈
            var ringMat = new Material(Shader.Find("Standard"));
            ringMat.color = new Color(0.4f, 0.2f, 0.7f, 0.5f);
            MakeTransparent(ringMat);
            CreatePrim("PortalRing_"+i, PrimitiveType.Cylinder, parent, new Vector3(0.6f, 0.02f, 0.6f), new Vector3(x, 0.05f, z), ringMat);

            // 门牌标签
            var labelMat = new Material(Shader.Find("Standard"));
            labelMat.color = new Color(0.6f, 0.4f, 0.9f);
            CreatePrim("PortalLabel_"+i, PrimitiveType.Cube, parent, new Vector3(0.6f, 0.2f, 0.05f), new Vector3(x, 0.2f, z), labelMat);
        }
    }

    // ================================================================
    // Login - 平台 + 背景山脉
    // ================================================================

    static void BuildLogin(GameObject parent)
    {
        var stoneMat = LoadMat("Platform_Stone");
        var wallMat = LoadMat("Wall_Stone");
        var marbleMat = LoadMat("Marble_White");

        // 主登录平台
        CreatePrim("Login_Platform", PrimitiveType.Cube, parent, new Vector3(12, 0.4f, 10), new Vector3(0, -0.2f, 2), stoneMat);

        // 台阶（3级）
        for (int i = 0; i < 3; i++)
            CreatePrim("Step_"+i, PrimitiveType.Cube, parent, new Vector3(6-i*0.5f, 0.15f, 0.8f), new Vector3(0, -0.2f-i*0.15f, -2.5f-i*0.8f), stoneMat);

        // 背景山脉 - 3座山
        float[] xs = { -8f, 0f, 8f };
        float[] heights = { 5f, 7f, 4f };
        float[] widths = { 6f, 8f, 5f };
        Color[] mountainColors = {
            new Color(0.12f, 0.10f, 0.18f),
            new Color(0.10f, 0.08f, 0.15f),
            new Color(0.14f, 0.12f, 0.20f)
        };

        for (int m = 0; m < 3; m++)
        {
            int layers = 5;
            for (int layer = 0; layer < layers; layer++)
            {
                float t = (float)layer / layers;
                float h = heights[m] * (1 - t * 0.6f);
                float w = widths[m] * (1 - t * 0.3f);
                float d = 3f * (1 - t * 0.3f);
                float darkness = 0.10f + t * 0.06f;

                var layerMat = new Material(Shader.Find("Standard"));
                layerMat.color = new Color(darkness, darkness * 0.8f, darkness * 1.2f + 0.05f);
                CreatePrim($"Mountain_{m}_L{layer}", PrimitiveType.Cube, parent,
                    new Vector3(w, h * 0.2f, d),
                    new Vector3(xs[m], -0.2f + h * 0.1f + layer * h * 0.2f, 12f + layer * 1.5f),
                    layerMat);
            }

            // 山顶雪
            var snowMat = new Material(Shader.Find("Standard"));
            snowMat.color = new Color(0.5f, 0.45f, 0.55f);
            snowMat.SetFloat("_Metallic", 0.3f);
            CreatePrim($"Mountain_Snow_{m}", PrimitiveType.Sphere, parent, new Vector3(0.6f, 0.3f, 0.6f), new Vector3(xs[m], heights[m]-0.2f, 16f), snowMat);
        }

        // 两侧石柱 + 火盆
        for (int side = -1; side <= 1; side += 2)
        {
            CreatePrim("Column_"+(side>0?"R":"L"), PrimitiveType.Cylinder, parent, new Vector3(0.5f, 2.5f, 0.5f), new Vector3(side*5.5f, 1.05f, 2), wallMat);
            var brazierMat = new Material(Shader.Find("Standard"));
            brazierMat.color = new Color(0.8f, 0.4f, 0.1f);
            brazierMat.EnableKeyword("_EMISSION");
            brazierMat.SetColor("_EmissionColor", new Color(0.6f, 0.2f, 0f));
            CreatePrim("Brazier_"+(side>0?"R":"L"), PrimitiveType.Sphere, parent, new Vector3(0.5f, 0.2f, 0.5f), new Vector3(side*5.5f, 2.5f, 2), brazierMat);
        }
    }

    // ================================================================
    // PVP - 方形擂台 + 围栏
    // ================================================================

    static void BuildPVP(GameObject parent)
    {
        var arenaMat = LoadMat("Arena_Floor");
        var stoneMat = LoadMat("Wall_Stone");
        var woodMat = LoadMat("Wall_Wood");

        // 方形擂台
        CreatePrim("PVP_Arena", PrimitiveType.Cube, parent, new Vector3(10, 0.3f, 10), new Vector3(0, 0.15f, 0), arenaMat);

        // 擂台边线
        var edgeMat = new Material(Shader.Find("Standard"));
        edgeMat.color = new Color(0.50f, 0.30f, 0.60f);
        CreatePrim("ArenaEdge_F", PrimitiveType.Cube, parent, new Vector3(10.2f, 0.05f, 0.2f), new Vector3(0, 0.35f, 5f), edgeMat);
        CreatePrim("ArenaEdge_B", PrimitiveType.Cube, parent, new Vector3(10.2f, 0.05f, 0.2f), new Vector3(0, 0.35f, -5f), edgeMat);
        CreatePrim("ArenaEdge_L", PrimitiveType.Cube, parent, new Vector3(0.2f, 0.05f, 10.2f), new Vector3(5f, 0.35f, 0), edgeMat);
        CreatePrim("ArenaEdge_R", PrimitiveType.Cube, parent, new Vector3(0.2f, 0.05f, 10.2f), new Vector3(-5f, 0.35f, 0), edgeMat);

        // 围栏（4边）
        CreateFenceSegment(parent, "Fence_F", new Vector3(0, 0.6f, 5.3f), new Vector3(10, 1.2f, 0.1f), woodMat);
        CreateFenceSegment(parent, "Fence_B", new Vector3(0, 0.6f, -5.3f), new Vector3(10, 1.2f, 0.1f), woodMat);
        CreateFenceSegment(parent, "Fence_R", new Vector3(5.3f, 0.6f, 0), new Vector3(0.1f, 1.2f, 10), woodMat);
        CreateFenceSegment(parent, "Fence_L", new Vector3(-5.3f, 0.6f, 0), new Vector3(0.1f, 1.2f, 10), woodMat);

        // 四角火柱
        int[] signs = { -1, 1 };
        foreach (int sx in signs)
        {
            foreach (int sz in signs)
            {
                int idx = (sx+1)/2 * 2 + (sz+1)/2;
                CreatePrim("Torch_"+idx, PrimitiveType.Cylinder, parent, new Vector3(0.12f, 0.8f, 0.12f), new Vector3(sx*6f, 0.4f, sz*6f), stoneMat);
                var flameMat = new Material(Shader.Find("Standard"));
                flameMat.color = new Color(1f, 0.4f, 0.1f);
                flameMat.EnableKeyword("_EMISSION");
                flameMat.SetColor("_EmissionColor", new Color(0.8f, 0.2f, 0f));
                CreatePrim("Flame_"+idx, PrimitiveType.Sphere, parent, new Vector3(0.25f, 0.25f, 0.25f), new Vector3(sx*6f, 1f, sz*6f), flameMat);
            }
        }

        // 双方出生点
        var redMat = new Material(Shader.Find("Standard"));
        redMat.color = new Color(0.8f, 0.2f, 0.2f);
        redMat.SetFloat("_Metallic", 0.5f);
        CreatePrim("Spawn_Red", PrimitiveType.Cylinder, parent, new Vector3(0.8f, 0.05f, 0.8f), new Vector3(0, 0.25f, -3f), redMat);

        var blueMat = new Material(Shader.Find("Standard"));
        blueMat.color = new Color(0.2f, 0.3f, 0.8f);
        blueMat.SetFloat("_Metallic", 0.5f);
        CreatePrim("Spawn_Blue", PrimitiveType.Cylinder, parent, new Vector3(0.8f, 0.05f, 0.8f), new Vector3(0, 0.25f, 3f), blueMat);
    }

    static void CreateFenceSegment(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fence.name = name;
        fence.transform.SetParent(parent.transform);
        fence.transform.localScale = scale;
        fence.transform.localPosition = pos;
        fence.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // 围栏横梁装饰
        var railMat = new Material(Shader.Find("Standard"));
        railMat.color = new Color(0.45f, 0.30f, 0.15f);
        var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = name + "_Rail";
        rail.transform.SetParent(parent.transform);
        bool isHorizontal = scale.x >= scale.z;
        if (isHorizontal)
            rail.transform.localScale = new Vector3(scale.x * 0.9f, 0.08f, 0.25f);
        else
            rail.transform.localScale = new Vector3(0.25f, 0.08f, scale.z * 0.9f);
        rail.transform.localPosition = pos + new Vector3(0, -scale.y * 0.25f, 0);
        rail.GetComponent<MeshRenderer>().sharedMaterial = railMat;
    }

    // ================================================================
    // 工具方法
    // ================================================================

    static GameObject CreatePrim(string name, PrimitiveType type, GameObject parent, Vector3 scale, Vector3 position, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localScale = scale;
        go.transform.localPosition = position;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
