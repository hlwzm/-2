using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 程序化生成英雄模型预制体 — 组合体替代胶囊体
/// 菜单: Jx3/Models/Generate Hero Models
/// </summary>
public class HeroModelGenerator : EditorWindow
{
    private static readonly string SavePath = "Assets/Resources/Art/Models/Heroes";
    private static readonly Vector3 GroundOffset = new Vector3(0, 0, 0);

    [MenuItem("Jx3/Models/Generate Hero Models")]
    static void GenerateAllHeroModels()
    {
        var heroes = GetHeroDataList();
        int count = 0;
        string fullSave = Path.Combine(Application.dataPath, "../", SavePath);
        Directory.CreateDirectory(fullSave);

        foreach (var h in heroes)
        {
            GameObject prefab = BuildHeroPrefab(h);
            string path = string.Format("{0}/Hero_{1}.prefab", SavePath, h.id);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            Object.DestroyImmediate(prefab);
            count++;
            Debug.Log(string.Format("[HeroModelGenerator] 生成 {0}(id={1})", h.name, h.id));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(string.Format("[HeroModelGenerator] 成功生成 {0} 个英雄预制体", count));
    }

    static List<HeroModelData> GetHeroDataList()
    {
        return new List<HeroModelData>
        {
            new HeroModelData(1001, "李忘生", 4, HeroWeaponType.Sword, HeroAttackType.内劲),
            new HeroModelData(1002, "谢云流", 5, HeroWeaponType.Sword, HeroAttackType.外功),
            new HeroModelData(1003, "叶英", 5, HeroWeaponType.Sword, HeroAttackType.外功),
            new HeroModelData(1004, "曲云", 4, HeroWeaponType.Staff, HeroAttackType.内劲),
            new HeroModelData(1005, "叶炜", 3, HeroWeaponType.Sword, HeroAttackType.外功),
            new HeroModelData(1006, "玄正", 4, HeroWeaponType.Staff, HeroAttackType.内劲),
            new HeroModelData(1007, "萧沙", 5, HeroWeaponType.Axe, HeroAttackType.外功),
            new HeroModelData(1008, "阿萨辛", 5, HeroWeaponType.Staff, HeroAttackType.内劲),
            new HeroModelData(2001, "公孙大娘", 4, HeroWeaponType.Sword, HeroAttackType.内劲),
            new HeroModelData(2002, "柳惊涛", 4, HeroWeaponType.Axe, HeroAttackType.外功),
        };
    }

    static GameObject BuildHeroPrefab(HeroModelData hero)
    {
        GameObject root = new GameObject(string.Format("Hero_{0}", hero.id));
        root.transform.position = GroundOffset;

        Color bodyColor = GetQualityColor(hero.quality);
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = bodyColor;
        bodyMat.SetFloat("_Metallic", 0.4f);
        bodyMat.SetFloat("_Glossiness", 0.3f);

        Material accentMat = new Material(Shader.Find("Standard"));
        Color accentColor = Color.Lerp(bodyColor, Color.white, 0.3f);
        accentMat.color = accentColor;
        accentMat.SetFloat("_Metallic", 0.6f);
        accentMat.SetFloat("_Glossiness", 0.5f);

        Material weaponMat = new Material(Shader.Find("Standard"));
        weaponMat.color = new Color(0.7f, 0.7f, 0.75f);
        weaponMat.SetFloat("_Metallic", 0.8f);
        weaponMat.SetFloat("_Glossiness", 0.7f);

        Material headMat = new Material(Shader.Find("Standard"));
        Color skinColor = hero.attackType == HeroAttackType.内劲
            ? new Color(0.95f, 0.85f, 0.75f)
            : new Color(0.9f, 0.75f, 0.6f);
        headMat.color = skinColor;
        headMat.SetFloat("_Metallic", 0f);
        headMat.SetFloat("_Glossiness", 0.2f);

        // Body: Capsule
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1.0f, 0);
        body.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
        SetMaterial(body, bodyMat);

        // Head: Sphere
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 1.85f, 0);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        SetMaterial(head, headMat);

        // Eyes
        Material eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = Color.black;
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = side < 0 ? "Eye_L" : "Eye_R";
            eye.transform.SetParent(head.transform);
            eye.transform.localPosition = new Vector3(side * 0.12f, 0.05f, 0.28f);
            eye.transform.localScale = new Vector3(0.06f, 0.06f, 0.04f);
            SetMaterial(eye, eyeMat);
        }

        // Weapon
        BuildWeapon(root, hero.weaponType, weaponMat, accentMat, bodyColor);

        // Quality ring for high-rarity (purple/gold)
        if (hero.quality >= 4)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "QualityRing";
            ring.transform.SetParent(root.transform);
            ring.transform.localPosition = new Vector3(0, 2.3f, 0);
            float ringSize = hero.quality == 5 ? 0.5f : 0.4f;
            ring.transform.localScale = new Vector3(ringSize, 0.03f, ringSize);
            Material ringMat = new Material(Shader.Find("Standard"));
            ringMat.color = bodyColor;
            ringMat.SetFloat("_Metallic", 0.9f);
            ringMat.EnableKeyword("_EMISSION");
            ringMat.SetColor("_EmissionColor", bodyColor * 0.5f);
            SetMaterial(ring, ringMat);
        }

        return root;
    }

    static void BuildWeapon(GameObject root, HeroWeaponType type, Material weaponMat, Material accentMat, Color bodyColor)
    {
        switch (type)
        {
            case HeroWeaponType.Sword:
            {
                // Blade
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Blade";
                blade.transform.SetParent(root.transform);
                blade.transform.localPosition = new Vector3(0.5f, 1.3f, 0);
                blade.transform.localScale = new Vector3(0.04f, 0.6f, 0.12f);
                blade.transform.localRotation = Quaternion.Euler(0, 0, -15);
                SetMaterial(blade, weaponMat);
                // Handle
                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                handle.name = "Handle";
                handle.transform.SetParent(root.transform);
                handle.transform.localPosition = new Vector3(0.48f, 1.0f, 0);
                handle.transform.localScale = new Vector3(0.04f, 0.1f, 0.04f);
                handle.transform.localRotation = Quaternion.Euler(0, 0, -15);
                SetMaterial(handle, accentMat);
                // Guard
                GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                guard.name = "Guard";
                guard.transform.SetParent(root.transform);
                guard.transform.localPosition = new Vector3(0.5f, 1.1f, 0);
                guard.transform.localScale = new Vector3(0.12f, 0.03f, 0.04f);
                SetMaterial(guard, accentMat);
                break;
            }
            case HeroWeaponType.Staff:
            {
                // Staff pole
                GameObject staff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                staff.name = "Staff";
                staff.transform.SetParent(root.transform);
                staff.transform.localPosition = new Vector3(-0.4f, 1.1f, 0);
                staff.transform.localScale = new Vector3(0.04f, 0.8f, 0.04f);
                staff.transform.localRotation = Quaternion.Euler(0, 0, 10);
                SetMaterial(staff, weaponMat);
                // Gem on top
                GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gem.name = "Gem";
                gem.transform.SetParent(root.transform);
                gem.transform.localPosition = new Vector3(-0.4f, 1.55f, 0);
                gem.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                Material gemMat = new Material(Shader.Find("Standard"));
                gemMat.color = new Color(0.2f, 0.5f, 1.0f);
                gemMat.SetFloat("_Metallic", 0.8f);
                gemMat.EnableKeyword("_EMISSION");
                gemMat.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.8f));
                SetMaterial(gem, gemMat);
                break;
            }
            case HeroWeaponType.Axe:
            {
                // Handle
                GameObject axeHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                axeHandle.name = "AxeHandle";
                axeHandle.transform.SetParent(root.transform);
                axeHandle.transform.localPosition = new Vector3(0.5f, 1.0f, 0);
                axeHandle.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f);
                axeHandle.transform.localRotation = Quaternion.Euler(0, 0, 20);
                SetMaterial(axeHandle, weaponMat);
                // Axe head
                GameObject axeHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
                axeHead.name = "AxeHead";
                axeHead.transform.SetParent(root.transform);
                axeHead.transform.localPosition = new Vector3(0.6f, 1.3f, 0);
                axeHead.transform.localScale = new Vector3(0.2f, 0.15f, 0.05f);
                SetMaterial(axeHead, accentMat);
                // Axe edge
                GameObject axeEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                axeEdge.name = "AxeEdge";
                axeEdge.transform.SetParent(root.transform);
                axeEdge.transform.localPosition = new Vector3(0.7f, 1.3f, 0);
                axeEdge.transform.localScale = new Vector3(0.08f, 0.03f, 0.06f);
                SetMaterial(axeEdge, weaponMat);
                break;
            }
        }
    }

    static void SetMaterial(GameObject go, Material mat)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = mat;
    }

    static Color GetQualityColor(int quality)
    {
        switch (quality)
        {
            case 5: return new Color(1.0f, 0.78f, 0.2f);   // 金
            case 4: return new Color(0.75f, 0.4f, 0.8f);   // 紫
            case 3: return new Color(0.35f, 0.5f, 1.0f);   // 蓝
            case 2: return new Color(0.4f, 0.8f, 0.4f);    // 绿
            default: return new Color(0.8f, 0.8f, 0.8f);   // 白
        }
    }

    public enum HeroWeaponType { Sword, Staff, Axe }
    public enum HeroAttackType { 外功, 内劲 }

    public class HeroModelData
    {
        public int id;
        public string name;
        public int quality;
        public HeroWeaponType weaponType;
        public HeroAttackType attackType;

        public HeroModelData(int id, string name, int quality, HeroWeaponType weapon, HeroAttackType atk)
        {
            this.id = id;
            this.name = name;
            this.quality = quality;
            weaponType = weapon;
            attackType = atk;
        }
    }
}
