using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 程序化生成怪物模型预制体（Boss + 小怪）
/// 菜单: Jx3/Models/Generate Monster Models
/// </summary>
public class MonsterModelGenerator : EditorWindow
{
    private static readonly string SavePath = "Assets/Resources/Art/Models/Monsters";

    [MenuItem("Jx3/Models/Generate Monster Models")]
    static void GenerateAllMonsterModels()
    {
        string fullSave = Path.Combine(Application.dataPath, "../", SavePath);
        Directory.CreateDirectory(fullSave);

        var bosses = new List<MonsterModelData>
        {
            new MonsterModelData(3001, "董龙", true, new Color(0.6f, 0.15f, 0.15f)),
            new MonsterModelData(3002, "卫备屯", true, new Color(0.5f, 0.1f, 0.5f)),
            new MonsterModelData(3003, "王家凤", true, new Color(0.1f, 0.3f, 0.6f)),
            new MonsterModelData(3004, "曲尘衛", true, new Color(0.6f, 0.3f, 0.1f)),
            new MonsterModelData(3005, "叶罨", true, new Color(0.3f, 0.1f, 0.4f)),
        };
        foreach (var b in bosses)
        {
            GameObject prefab = BuildBossPrefab(b);
            string path = string.Format("{0}/Monster_{1}.prefab", SavePath, b.id);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            Object.DestroyImmediate(prefab);
        }

        var minionColors = new Color[] {
            new Color(0.3f, 0.1f, 0.1f), new Color(0.1f, 0.2f, 0.3f),
            new Color(0.3f, 0.2f, 0.1f), new Color(0.2f, 0.3f, 0.15f),
            new Color(0.15f, 0.15f, 0.25f),
        };
        for (int i = 0; i < minionColors.Length; i++)
        {
            int id = 3101 + i;
            MonsterModelData minion = new MonsterModelData(id, "小怨" + (i+1), false, minionColors[i]);
            GameObject prefab = BuildMinionPrefab(minion);
            string path = string.Format("{0}/Monster_{1}.prefab", SavePath, id);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            Object.DestroyImmediate(prefab);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MonsterModelGenerator] 成功生成 5个Boss + 5个小怨");
    }

    static GameObject BuildBossPrefab(MonsterModelData data)
    {
        GameObject root = new GameObject(string.Format("Monster_{0}", data.id));
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = data.color; bodyMat.SetFloat("_Metallic", 0.6f);
        Material darkMat = new Material(Shader.Find("Standard"));
        darkMat.color = Color.Lerp(data.color, Color.black, 0.4f);
        Material glowMat = new Material(Shader.Find("Standard"));
        glowMat.color = Color.Lerp(data.color, Color.white, 0.3f);
        glowMat.EnableKeyword("_EMISSION"); glowMat.SetColor("_EmissionColor", data.color * 0.3f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body"; body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1.8f, 0);
        body.transform.localScale = new Vector3(1.0f, 1.8f, 1.0f);
        SetMaterial(body, bodyMat);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head"; head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 3.2f, 0);
        head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        SetMaterial(head, darkMat);

        Material eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = Color.red; eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_EmissionColor", Color.red * 0.8f);
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = s < 0 ? "Eye_L" : "Eye_R";
            eye.transform.SetParent(head.transform);
            eye.transform.localPosition = new Vector3(s * 0.18f, 0.05f, 0.42f);
            eye.transform.localScale = new Vector3(0.08f, 0.08f, 0.06f);
            SetMaterial(eye, eyeMat);
        }

        for (int s = -1; s <= 1; s += 2)
        {
            GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = s < 0 ? "Horn_L" : "Horn_R";
            horn.transform.SetParent(head.transform);
            horn.transform.localPosition = new Vector3(s * 0.08f, 0.4f, 0.2f);
            horn.transform.localScale = new Vector3(0.06f, 0.2f, 0.06f);
            horn.transform.localRotation = Quaternion.Euler(30 + s * 10, s * 15, 0);
            SetMaterial(horn, glowMat);
        }

        GameObject cape = GameObject.CreatePrimitive(PrimitiveType.Plane);
        cape.name = "Cape"; cape.transform.SetParent(root.transform);
        cape.transform.localPosition = new Vector3(0, 1.3f, -0.6f);
        cape.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        cape.transform.localRotation = Quaternion.Euler(30, 0, 0);
        SetMaterial(cape, darkMat);

        for (int s = -1; s <= 1; s += 2)
        {
            GameObject shoulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shoulder.name = s < 0 ? "Shoulder_L" : "Shoulder_R";
            shoulder.transform.SetParent(root.transform);
            shoulder.transform.localPosition = new Vector3(s * 0.7f, 2.2f, 0);
            shoulder.transform.localScale = new Vector3(0.25f, 0.15f, 0.25f);
            SetMaterial(shoulder, glowMat);
        }
        return root;
    }

    static GameObject BuildMinionPrefab(MonsterModelData data)
    {
        GameObject root = new GameObject(string.Format("Monster_{0}", data.id));
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = data.color; bodyMat.SetFloat("_Metallic", 0.3f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body"; body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.8f, 0);
        body.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
        SetMaterial(body, bodyMat);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head"; head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 1.4f, 0);
        head.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        SetMaterial(head, bodyMat);

        Material eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = Color.Lerp(data.color, Color.white, 0.5f);
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = s < 0 ? "Eye_L" : "Eye_R";
            eye.transform.SetParent(head.transform);
            eye.transform.localPosition = new Vector3(s * 0.08f, 0.02f, 0.2f);
            eye.transform.localScale = new Vector3(0.04f, 0.04f, 0.03f);
            SetMaterial(eye, eyeMat);
        }
        return root;
    }

    static void SetMaterial(GameObject go, Material mat)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    public class MonsterModelData
    {
        public int id; public string name; public bool isBoss; public Color color;
        public MonsterModelData(int id, string name, bool isBoss, Color color)
        { this.id = id; this.name = name; this.isBoss = isBoss; this.color = color; }
    }
}
