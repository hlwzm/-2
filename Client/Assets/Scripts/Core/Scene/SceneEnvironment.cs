using UnityEngine;
using UnityEngine.Rendering;

namespace Jx3.Core.Scene
{
    /// <summary>
    /// 场景环境搭建器 - 自动创建3D基础环境（地面、光照、天空盒）
    /// 挂载到每个场景的SceneBoot子类上
    /// </summary>
    public class SceneEnvironment : MonoBehaviour
    {
        public GameScene sceneType = GameScene.Login;
        public Color ambientColor = new Color(0.15f, 0.12f, 0.2f);
        public Color fogColor = new Color(0.08f, 0.06f, 0.12f);

        void Start()
        {
            SetupLighting();
            SetupFog();
            CreateGround();
            CreateSceneElements();
        }

        void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = 0.6f;

            // Directional Light
            var lightGo = new GameObject("Directional Light", typeof(Light));
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.8f, 0.75f, 0.9f);
            light.intensity = 0.8f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
        }

        void SetupFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.015f;
        }

        void CreateGround()
        {
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.transform.localScale = new Vector3(20, 1, 20);
            
            var renderer = groundGo.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.12f, 0.1f, 0.18f);
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Glossiness", 0.2f);
            renderer.material = mat;
        }

        void CreateSceneElements()
        {
            switch (sceneType)
            {
                case GameScene.Login:
                    CreateLoginScene();
                    break;
                case GameScene.MainCity:
                    CreateMainCityScene();
                    break;
                case GameScene.Battle:
                    CreateBattleScene();
                    break;
                case GameScene.DungeonSelect:
                    CreateDungeonSelectScene();
                    break;
                case GameScene.PVP:
                    CreatePvpScene();
                    break;
            }
        }

        void CreateLoginScene()
        {
            // 登录场景 - 古风建筑背景
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.localScale = new Vector3(30, 0.5f, 20);
            platform.transform.position = new Vector3(0, -0.25f, 0);
            var platMat = new Material(Shader.Find("Standard"));
            platMat.color = new Color(0.15f, 0.12f, 0.22f);
            platform.GetComponent<MeshRenderer>().material = platMat;

            // 柱子
            for (int i = -2; i <= 2; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "Pillar_" + i;
                pillar.transform.localScale = new Vector3(0.3f, 2, 0.3f);
                pillar.transform.position = new Vector3(i * 5, 1.5f, -5);
                var pillarMat = new Material(Shader.Find("Standard"));
                pillarMat.color = new Color(0.2f, 0.15f, 0.3f);
                pillarMat.SetFloat("_Metallic", 0.5f);
                pillar.GetComponent<MeshRenderer>().material = pillarMat;
            }

            // 灯笼光效
            for (int i = -2; i <= 2; i++)
            {
                var pointLight = new GameObject("Lantern_" + i, typeof(Light));
                pointLight.transform.position = new Vector3(i * 5, 3.5f, -5);
                var pl = pointLight.GetComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1f, 0.6f, 0.2f);
                pl.intensity = 0.5f;
                pl.range = 6;
            }

            // 摄像机位置
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 3, -8);
                cam.transform.rotation = Quaternion.Euler(15, 0, 0);
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new Color(0.05f, 0.03f, 0.1f);
            }
        }

        void CreateMainCityScene()
        {
            // 主城场景 - 圆形广场
            CreateCirclePlatform(0, 0, 12, new Color(0.15f, 0.13f, 0.2f));

            // 中心雕像
            var statue = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            statue.name = "StatueBase";
            statue.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);
            statue.transform.position = new Vector3(0, 0.15f, 0);
            var sMat = new Material(Shader.Find("Standard"));
            sMat.color = new Color(0.35f, 0.25f, 0.5f);
            sMat.SetFloat("_Metallic", 0.8f);
            statue.GetComponent<MeshRenderer>().material = sMat;

            var statueTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            statueTop.name = "StatueTop";
            statueTop.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            statueTop.transform.position = new Vector3(0, 0.8f, 0);
            statueTop.GetComponent<MeshRenderer>().material = sMat;

            // 周围建筑（用方块表示）
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60 * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * 10;
                float z = Mathf.Cos(angle) * 10;
                
                var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "Building_" + i;
                building.transform.localScale = new Vector3(2, Random.Range(1.5f, 3f), 2);
                building.transform.position = new Vector3(x, building.transform.localScale.y / 2, z);
                var bMat = new Material(Shader.Find("Standard"));
                bMat.color = Color.Lerp(new Color(0.15f, 0.12f, 0.2f), new Color(0.25f, 0.2f, 0.35f), Random.value);
                building.GetComponent<MeshRenderer>().material = bMat;
            }

            // 摄像机
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 8, -14);
                cam.transform.rotation = Quaternion.Euler(30, 0, 0);
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new Color(0.06f, 0.04f, 0.1f);
            }
        }

        void CreateBattleScene()
        {
            // 战斗场景 - 竞技场
            CreateCirclePlatform(0, 0, 8, new Color(0.2f, 0.15f, 0.25f));

            // 边界柱
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45 * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * 7;
                float z = Mathf.Cos(angle) * 7;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "Boundary_" + i;
                pillar.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                pillar.transform.position = new Vector3(x, 0.5f, z);
                var pMat = new Material(Shader.Find("Standard"));
                pMat.color = new Color(0.5f, 0.3f, 0.8f, 0.5f);
                pMat.SetFloat("_Mode", 3); // Transparent
                pillar.GetComponent<MeshRenderer>().material = pMat;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 6, -10);
                cam.transform.rotation = Quaternion.Euler(25, 0, 0);
            }
        }

        void CreateDungeonSelectScene()
        {
            CreateCirclePlatform(0, 0, 10, new Color(0.1f, 0.08f, 0.15f));
            RenderSettings.fogDensity = 0.02f;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 5, -10);
                cam.transform.rotation = Quaternion.Euler(20, 0, 0);
            }
        }

        void CreatePvpScene()
        {
            CreateCirclePlatform(0, 0, 10, new Color(0.18f, 0.1f, 0.15f));

            // PVP擂台
            var arena = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arena.name = "Arena";
            arena.transform.localScale = new Vector3(6, 0.2f, 6);
            arena.transform.position = new Vector3(0, 0.1f, 0);
            var aMat = new Material(Shader.Find("Standard"));
            aMat.color = new Color(0.3f, 0.15f, 0.25f);
            aMat.SetFloat("_Metallic", 0.3f);
            arena.GetComponent<MeshRenderer>().material = aMat;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 7, -8);
                cam.transform.rotation = Quaternion.Euler(30, 0, 0);
            }
        }

        void CreateCirclePlatform(float cx, float cz, float radius, Color color)
        {
            // Use a cylinder as a circular platform
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Platform";
            platform.transform.localScale = new Vector3(radius, 0.1f, radius);
            platform.transform.position = new Vector3(cx, -0.1f, cz);
            var pMat = new Material(Shader.Find("Standard"));
            pMat.color = color;
            platform.GetComponent<MeshRenderer>().material = pMat;
        }
    }
}