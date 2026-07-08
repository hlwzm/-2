using System.Collections.Generic;
using UnityEngine;

namespace Jx3.Core
{
    [System.Serializable]
    public class ItemData
    {
        public int id;
        public string name;
        public ItemType type;
        public int quality;         // 1-5星
        public int price;           // 购买价格
        public int sellPrice;       // 出售价格
        public string description;
        public string iconChar;     // 图标占位字符
        public bool isEquippable;

        // 装备属性
        public int attackBonus;
        public int defenseBonus;
        public int hpBonus;
    }

    public enum ItemType { 武器, 防具, 饰品, 材料, 任务, 消耗品 }

    public static class ItemConfig
    {
        private static List<ItemData> _items;

        public static List<ItemData> GetAll()
        {
            if (_items == null) Init();
            return _items;
        }

        public static List<ItemData> GetByType(ItemType type)
        {
            if (_items == null) Init();
            return _items.FindAll(i => i.type == type);
        }

        public static ItemData Get(int id)
        {
            if (_items == null) Init();
            return _items.Find(i => i.id == id);
        }

        static void Init()
        {
            _items = new List<ItemData>
            {
                new ItemData{id=1, name="紫霞剑", type=ItemType.武器, quality=3, price=5000, sellPrice=2500, description="传说中紫霞真人的佩剑，剑身泛紫光", iconChar="剑", isEquippable=true, attackBonus=45, defenseBonus=0, hpBonus=0},
                new ItemData{id=2, name="白云袍", type=ItemType.防具, quality=2, price=1200, sellPrice=600, description="纯阳宫弟子服饰，以白云为纹", iconChar="袍", isEquippable=true, attackBonus=0, defenseBonus=25, hpBonus=100},
                new ItemData{id=3, name="碧玉戒", type=ItemType.饰品, quality=3, price=3000, sellPrice=1500, description="上等碧玉打造，蕴含天地灵气", iconChar="戒", isEquippable=true, attackBonus=15, defenseBonus=10, hpBonus=200},
                new ItemData{id=4, name="灵芝草", type=ItemType.材料, quality=2, price=200, sellPrice=100, description="百年灵芝，可用于炼药", iconChar="草"},
                new ItemData{id=5, name="玄铁令", type=ItemType.任务, quality=1, price=0, sellPrice=1, description="玄铁重铸任务所需令牌", iconChar="令"},
                new ItemData{id=6, name="金疮药", type=ItemType.消耗品, quality=1, price=100, sellPrice=50, description="恢复500点生命值", iconChar="药"},
                new ItemData{id=7, name="天蚕丝", type=ItemType.材料, quality=3, price=800, sellPrice=400, description="天山冰蚕所吐之丝，极为珍贵", iconChar="丝"},
                new ItemData{id=8, name="破军枪", type=ItemType.武器, quality=4, price=15000, sellPrice=7500, description="天策府镇府之宝，破军之势无人能挡", iconChar="枪", isEquippable=true, attackBonus=75, defenseBonus=0, hpBonus=300},
                new ItemData{id=9, name="金缕衣", type=ItemType.防具, quality=4, price=12000, sellPrice=6000, description="金丝织就的宝衣，刀枪不入", iconChar="衣", isEquippable=true, attackBonus=0, defenseBonus=50, hpBonus=500},
                new ItemData{id=10, name="太虚符", type=ItemType.消耗品, quality=3, price=500, sellPrice=250, description="纯阳秘符，使用后恢复30%内力", iconChar="符"},
                new ItemData{id=11, name="陨铁", type=ItemType.材料, quality=4, price=2000, sellPrice=1000, description="天外陨铁，打造神兵利器的绝佳材料", iconChar="铁"},
                new ItemData{id=12, name="洗髓丹", type=ItemType.消耗品, quality=5, price=5000, sellPrice=2500, description="洗髓伐脉，永久提升角色属性", iconChar="丹"},
            };
            Debug.Log($"[ItemConfig] Loaded {_items.Count} items");
        }

        public static string GetQualityName(int quality)
        {
            return quality switch
            {
                1 => "普通", 2 => "精良", 3 => "卓越", 4 => "史诗", 5 => "传说", _ => "普通"
            };
        }

        public static Color GetQualityColor(int quality)
        {
            return quality switch
            {
                1 => new Color(0.8f, 0.8f, 0.8f),
                2 => new Color(0.4f, 0.8f, 0.4f),
                3 => new Color(0.4f, 0.4f, 0.8f),
                4 => new Color(0.8f, 0.4f, 0.8f),
                5 => new Color(1f, 0.8f, 0.2f),
                _ => new Color(0.8f, 0.8f, 0.8f)
            };
        }
    }
}