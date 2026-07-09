using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;

namespace Jx3.Editor
{
    public class SceneBuilder : EditorWindow
    {
        [MenuItem("Tools/Scene Builder/Setup Login Scene")]
        static void SetupLoginScene()
        {
            var scene = SceneManager.GetActiveScene();
            ClearScene();
            SetupCamera(new Vector3(0, 3, -8), Quaternion.Euler(15, 0, 0));

            // Skybox
            SetSkybox("AllSkyFree/Materials/Cold Sunset.mat");

            // Ground
            var ground = LoadPrefab("PolygonStarter/Prefabs/Environment/SM_Generic_Ground_Flat_01.prefab");
            if (ground != null) Instantiate(ground, Vector3.zero, Quaternion.identity);

            // Trees around perimeter
            PlaceTrees();
            PlaceLanterns();

            Debug.Log("[SceneBuilder] Login scene setup complete");
        }

        [MenuItem("Tools/Scene Builder/Setup MainCity Scene")]
        static void SetupMainCityScene()
        {
            ClearScene();
            SetupCamera(new Vector3(0, 10, -16), Quaternion.Euler(25, 0, 0));

            SetSkybox("AllSkyFree/Materials/Day_BlueSky_Nothing.mat");

            // Large ground
            var ground = LoadPrefab("PolygonStarter/Prefabs/Environment/SM_Generic_Ground_Flat_01.prefab");
            if (ground != null)
            {
                var g = Instantiate(ground, Vector3.zero, Quaternion.identity);
                g.transform.localScale = Vector3.one * 3;
            }

            // Buildings circle
            PlaceBuildings();
            PlaceTrees();
            PlaceTownDecorations();

            Debug.Log("[SceneBuilder] MainCity scene setup complete");
        }

        [MenuItem("Tools/Scene Builder/Setup Battle Scene")]
        static void SetupBattleScene()
        {
            ClearScene();
            SetupCamera(new Vector3(0, 6, -10), Quaternion.Euler(20, 0, 0));

            SetSkybox("AllSkyFree/Materials/Day_BlueSky_Nothing.mat");

            var ground = LoadPrefab("PolygonStarter/Prefabs/Environment/SM_Generic_Ground_Flat_01.prefab");
            if (ground != null) Instantiate(ground, Vector3.zero, Quaternion.identity);

            PlaceArenaBoundaries();
            PlaceBattleProps();

            Debug.Log("[SceneBuilder] Battle scene setup complete");
        }

        // ── Helpers ──

        static void ClearScene()
        {
            var all = GameObject.FindObjectsOfType<GameObject>();
            foreach (var go in all)
            {
                if (go.scene.name == null) continue;
                if (go.name == "Main Camera") continue;
                DestroyImmediate(go);
            }
        }

        static void SetupCamera(Vector3 pos, Quaternion rot)
        {
            var cam = Camera.main ?? new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.rotation = rot;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.1f, 0.08f, 0.05f);
        }

        static void SetSkybox(string path)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/{path}");
            if (mat != null) RenderSettings.skybox = mat;
        }

        static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/{path}");
        }

        static void Instantiate(string path, Vector3 pos, Quaternion rot, Vector3? scale = null)
        {
            var prefab = LoadPrefab(path);
            if (prefab != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = pos;
                go.transform.rotation = rot;
                if (scale.HasValue) go.transform.localScale = scale.Value;
            }
        }

        static void PlaceTrees()
        {
            var trees = new[] {
                "Polytope Studio/Prefabs/Nature/PT_Pine_Tree_01.prefab",
                "Polytope Studio/Prefabs/Nature/PT_Pine_Tree_02.prefab",
                "Polytope Studio/Prefabs/Nature/PT_Fruit_Tree_01.prefab",
                "coniferous_forest/Prefabs/SM_Spruce_Large.prefab",
                "coniferous_forest/Prefabs/SM_Spruce_Medium.prefab",
            };

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30 * Mathf.Deg2Rad;
                float radius = Random.Range(14f, 18f);
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                var tree = trees[Random.Range(0, trees.Length)];
                Instantiate(tree, new Vector3(x, 0, z), Quaternion.Euler(0, Random.Range(0, 360), 0),
                    Vector3.one * Random.Range(0.8f, 1.2f));
            }
        }

        static void PlaceLanterns()
        {
            for (int i = -2; i <= 2; i++)
            {
                var lightGo = new GameObject($"Lantern_{i}", typeof(Light));
                lightGo.transform.position = new Vector3(i * 5, 3.5f, -6);
                var light = lightGo.GetComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.6f, 0.2f);
                light.intensity = 0.8f;
                light.range = 8;
                light.shadows = LightShadows.None;
            }

            // Main directional light
            var sun = new GameObject("Directional Light", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            var sunLight = sun.GetComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 0.8f;
            sunLight.shadows = LightShadows.Soft;
        }

        static void PlaceBuildings()
        {
            var walls = new[] {
                "PolygonStarter/Prefabs/Buildings/SM_Bld_Wall_01.prefab",
                "PolygonStarter/Prefabs/Buildings/SM_Bld_Wall_02.prefab",
            };

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60 * Mathf.Deg2Rad;
                float radius = 8f;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;

                // Wall segment
                var wall = walls[Random.Range(0, walls.Length)];
                Instantiate(wall, new Vector3(x, 0, z), Quaternion.Euler(0, -i * 60, 0));

                // Column
                Instantiate("PolygonStarter/Prefabs/Buildings/SM_Bld_Column_01.prefab",
                    new Vector3(x, 0, z), Quaternion.identity);
            }

            // Center statue / feature
            var center = LoadPrefab("PolygonStarter/Prefabs/Environment/SM_Generic_Small_Rocks_01.prefab");
            if (center != null)
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(center);
                c.transform.position = Vector3.zero;
                c.transform.localScale = Vector3.one * 2;
            }
        }

        static void PlaceTownDecorations()
        {
            // Fence
            for (int i = -5; i <= 5; i++)
            {
                Instantiate("Polytope Studio/Prefabs/Nature/Fence.prefab",
                    new Vector3(i * 2, 0, -7), Quaternion.identity);
                Instantiate("Polytope Studio/Prefabs/Nature/Fence.prefab",
                    new Vector3(i * 2, 0, 7), Quaternion.identity);
            }

            // Flowers and grass
            for (int i = 0; i < 20; i++)
            {
                var flowers = new[] {
                    "Polytope Studio/Prefabs/Nature/PT_Poppy_02.prefab",
                    "Polytope Studio/Prefabs/Nature/PT_Grass_02.prefab",
                    "Polytope Studio/Prefabs/Nature/PT_Caesars_Mushroom_01.prefab",
                };
                Instantiate(flowers[Random.Range(0, flowers.Length)],
                    new Vector3(Random.Range(-6f, 6f), 0, Random.Range(-6f, 6f)),
                    Quaternion.Euler(0, Random.Range(0, 360), 0));
            }
        }

        static void PlaceArenaBoundaries()
        {
            // Boundary pillars
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45 * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * 6;
                float z = Mathf.Cos(angle) * 6;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Boundary_{i}";
                pillar.transform.position = new Vector3(x, 0.5f, z);
                pillar.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.54f, 0.42f, 0.16f);
                pillar.GetComponent<MeshRenderer>().material = mat;
            }

            // Ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaFloor";
            ground.transform.position = new Vector3(0, -0.05f, 0);
            ground.transform.localScale = new Vector3(0.6f, 1, 0.6f);
            var gMat = new Material(Shader.Find("Standard"));
            gMat.color = new Color(0.3f, 0.25f, 0.15f);
            ground.GetComponent<MeshRenderer>().material = gMat;
        }

        static void PlaceBattleProps()
        {
            // Weapons racks
            var weapon = LoadPrefab("PurePoly/Prefabs/PP_Sword_0018.prefab");
            if (weapon != null)
            {
                for (int i = -2; i <= 2; i += 2)
                {
                    var w = (GameObject)PrefabUtility.InstantiatePrefab(weapon);
                    w.transform.position = new Vector3(i, 0.3f, -4);
                    w.transform.rotation = Quaternion.Euler(0, 0, 90);
                }
            }
        }
    }
}