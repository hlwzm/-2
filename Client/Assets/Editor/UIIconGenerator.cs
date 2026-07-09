using UnityEngine;
using UnityEditor;
using System.IO;

public class UIIconGenerator : EditorWindow
{
    private static readonly string[] HeroNames = new string[]
    {
        "楚留香", "李红袖", "姬冰雁", "苏蓉蓉", "无花",
        "中原一点红", "薛衣人", "南宫灵", "胡铁花", "高亚男"
    };

    private static readonly string[] SkillNames = new string[]
    {
        "烈火焚天", "寒冰掌", "雷霆万钧", "春风化雨", "风卷残云",
        "破甲一击", "金刚不坏", "剑气纵横", "飞花逐月", "惊涛骇浪",
        "暗影突袭", "万箭齐发"
    };

    private static readonly string[] ItemNames = new string[]
    {
        "还魂丹", "金疮药", "玄铁剑", "金丝甲", "夜行衣",
        "乾坤袋", "紫金冠", "碧玉簪", "龙鳞盾", "天蚕丝",
        "流星镖", "穿云箭"
    };

    [MenuItem("Jx3/UI/Generate UI Icons")]
    static void GenerateAllIcons()
    {
        string outputDir = "Assets/Art/UI/Icons";
        if (!AssetDatabase.IsValidFolder(outputDir))
        {
            string parent = "Assets/Art/UI";
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets/Art", "UI");
            AssetDatabase.CreateFolder(parent, "Icons");
        }

        GenerateHeroIcons(outputDir);
        GenerateSkillIcons(outputDir);
        GenerateItemIcons(outputDir);

        AssetDatabase.Refresh();
        Debug.Log("[UIIconGenerator] All UI icons generated to " + outputDir);
    }

    static void GenerateHeroIcons(string dir)
    {
        for (int i = 0; i < HeroNames.Length; i++)
        {
            Texture2D tex = CreateCircleIcon(HeroNames[i], GetHeroColor(i));
            byte[] png = tex.EncodeToPNG();
            string path = Application.dataPath + "/Art/UI/Icons/Icon_Hero_" + (i + 1).ToString("D2") + ".png";
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);
        }
        Debug.Log("[UIIconGenerator] 10 hero icons generated");
    }

    static void GenerateSkillIcons(string dir)
    {
        for (int i = 0; i < SkillNames.Length; i++)
        {
            Texture2D tex = CreateSquareIcon(SkillNames[i], GetSkillColor(i));
            byte[] png = tex.EncodeToPNG();
            string path = Application.dataPath + "/Art/UI/Icons/Icon_Skill_" + (i + 1).ToString("D2") + ".png";
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);
        }
        Debug.Log("[UIIconGenerator] 12 skill icons generated");
    }

    static void GenerateItemIcons(string dir)
    {
        for (int i = 0; i < ItemNames.Length; i++)
        {
            Color borderColor = GetQualityColor(i);
            Texture2D tex = CreateBorderedSquareIcon(ItemNames[i], borderColor);
            byte[] png = tex.EncodeToPNG();
            string path = Application.dataPath + "/Art/UI/Icons/Icon_Item_" + (i + 1).ToString("D2") + ".png";
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);
        }
        Debug.Log("[UIIconGenerator] 12 item icons generated");
    }

    static Color GetHeroColor(int index)
    {
        Color[] colors = new Color[]
        {
            new Color(0.9f, 0.3f, 0.2f), new Color(0.8f, 0.2f, 0.5f), new Color(0.2f, 0.5f, 0.8f),
            new Color(0.9f, 0.6f, 0.1f), new Color(0.6f, 0.3f, 0.9f), new Color(0.2f, 0.8f, 0.3f),
            new Color(0.9f, 0.4f, 0.1f), new Color(0.1f, 0.7f, 0.7f), new Color(0.7f, 0.5f, 0.2f),
            new Color(0.5f, 0.3f, 0.9f)
        };
        return colors[index % colors.Length];
    }

    static Color GetSkillColor(int index)
    {
        Color[] colors = new Color[]
        {
            new Color(1f, 0.3f, 0.1f), new Color(0.3f, 0.6f, 1f), new Color(0.9f, 0.9f, 1f),
            new Color(0.2f, 0.9f, 0.3f), new Color(0.1f, 0.8f, 0.8f), new Color(1f, 0.7f, 0.1f),
            new Color(0.3f, 0.5f, 1f), new Color(0.8f, 0.6f, 0.2f), new Color(1f, 0.4f, 0.7f),
            new Color(0.1f, 0.5f, 0.9f), new Color(0.4f, 0.1f, 0.6f), new Color(0.7f, 0.3f, 0.1f)
        };
        return colors[index % colors.Length];
    }

    static Color GetQualityColor(int index)
    {
        Color[] colors = new Color[]
        {
            Color.white, new Color(0.3f, 0.8f, 0.3f), new Color(0.3f, 0.5f, 1f),
            new Color(0.8f, 0.3f, 1f), new Color(1f, 0.7f, 0.1f), Color.white,
            new Color(0.3f, 0.8f, 0.3f), new Color(0.3f, 0.5f, 1f), new Color(0.8f, 0.3f, 1f),
            new Color(1f, 0.7f, 0.1f), Color.white, new Color(0.3f, 0.8f, 0.3f)
        };
        return colors[index % colors.Length];
    }

    static Texture2D CreateCircleIcon(string name, Color bgColor)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        char firstChar = name[0];
        Color bg = bgColor;
        Color textColor = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size / 2f) / (size / 2f);
                float dy = (y - size / 2f) / (size / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= 0.95f)
                    tex.SetPixel(x, y, bg);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateSquareIcon(string name, Color bgColor)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color bg = bgColor;
        Color innerBg = new Color(bg.r * 0.6f, bg.g * 0.6f, bg.b * 0.6f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Center 80% area with lighter bg, edges darker
                float dx = Mathf.Abs(x - size / 2f) / (size / 2f);
                float dy = Mathf.Abs(y - size / 2f) / (size / 2f);
                float maxDist = Mathf.Max(dx, dy);
                if (maxDist < 0.85f)
                    tex.SetPixel(x, y, innerBg);
                else
                    tex.SetPixel(x, y, bg);
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateBorderedSquareIcon(string name, Color borderColor)
    {
        int size = 128;
        int border = 6;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color innerBg = new Color(0.15f, 0.12f, 0.18f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x < border || x >= size - border || y < border || y >= size - border)
                    tex.SetPixel(x, y, borderColor);
                else
                    tex.SetPixel(x, y, innerBg);
            }
        }
        tex.Apply();
        return tex;
    }
}
