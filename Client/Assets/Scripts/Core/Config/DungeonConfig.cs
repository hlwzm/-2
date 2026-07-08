using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Jx3.Core
{
    /// <summary>
    /// 副本配置 - 4个副本 + Boss数据 + 掉落表
    /// </summary>
    [System.Serializable]
    public class DungeonData
    {
        public int id;
        public string name;
        public string description;
        public int minLevel;
        public int maxLevel;
        public int minPlayers = 4;
        public int maxPlayers = 8;
        public float timeLimit;     // 限时(秒)
        public string sceneName;
        public List<int> bossIds = new();
        public int difficulty;      // 1-5星
    }

    [System.Serializable]
    public class BossData
    {
        public int id;
        public string name;
        public string title;
        public int dungeonId;
        public int order;           // 1-3小Boss, 4=终极Boss
        public float hp;
        public int attack;
        public int defense;
        public float attackInterval = 3f;
        public string[] skillNames;
        public string description;
        public List<DropItem> dropTable = new();
    }

    [System.Serializable]
    public class DropItem
    {
        public int itemId;
        public float dropRate;     // 0-1
        public int minCount = 1;
        public int maxCount = 1;
    }

    public static class DungeonConfig
    {
        private static Dictionary<int, DungeonData> _dungeons;
        private static Dictionary<int, BossData> _bosses;

        public static DungeonData GetDungeon(int id)
        {
            if (_dungeons == null) Init();
            return _dungeons.ContainsKey(id) ? _dungeons[id] : null;
        }

        public static BossData GetBoss(int id)
        {
            if (_bosses == null) Init();
            return _bosses.ContainsKey(id) ? _bosses[id] : null;
        }

        public static List<DungeonData> GetAllDungeons()
        {
            if (_dungeons == null) Init();
            return new List<DungeonData>(_dungeons.Values);
        }

        public static List<BossData> GetDungeonBosses(int dungeonId)
        {
            if (_bosses == null) Init();
            return _bosses.Values.Where(b => b.dungeonId == dungeonId).ToList();
        }

        static void Init()
        {
            _dungeons = new();
            _bosses = new();

            // ==================== 1. 风雨稻香村 ====================
            var d1 = new DungeonData
            {
                id = 1, name = "风雨稻香村", description = "稻香村突遭山贼袭击，村中危机四伏",
                minLevel = 20, maxLevel = 30, minPlayers = 4, maxPlayers = 4,
                timeLimit = 480, sceneName = "Battle", difficulty = 3,
                bossIds = new List<int>{ 3001, 3002, 3003, 3100 }
            };

            _bosses[3001] = new BossData
            {
                id = 3001, name = "董龙", title = "山贼头目", dungeonId = 1, order = 1,
                hp = 8000, attack = 180, defense = 80, attackInterval = 2.5f,
                skillNames = new[]{"猛劈", "怒吼"},
                description = "稻香村外山贼的头目，凶悍异常",
                dropTable = new List<DropItem>{ new DropItem{ itemId=6, dropRate=0.5f, minCount=1, maxCount=3 }, new DropItem{ itemId=4, dropRate=0.3f } }
            };

            _bosses[3002] = new BossData
            {
                id = 3002, name = "汪莽", title = "山贼军师", dungeonId = 1, order = 2,
                hp = 6000, attack = 220, defense = 60, attackInterval = 3.0f,
                skillNames = new[]{"毒箭", "烟雾"},
                description = "山贼中的智囊，擅长暗箭伤人",
                dropTable = new List<DropItem>{ new DropItem{ itemId=4, dropRate=0.6f, minCount=1, maxCount=2 } }
            };

            _bosses[3003] = new BossData
            {
                id = 3003, name = "肖人德", title = "山贼二当家", dungeonId = 1, order = 3,
                hp = 10000, attack = 250, defense = 100, attackInterval = 2.0f,
                skillNames = new[]{"连环斩", "震地"},
                description = "山贼中的第一战力，勇猛无比",
                dropTable = new List<DropItem>{ new DropItem{ itemId=1, dropRate=0.2f }, new DropItem{ itemId=2, dropRate=0.3f } }
            };

            _bosses[3100] = new BossData
            {
                id = 3100, name = "秦颐岩", title = "山贼大当家 ★终极", dungeonId = 1, order = 4,
                hp = 20000, attack = 350, defense = 150, attackInterval = 1.8f,
                skillNames = new[]{"霸王斩", "震天吼", "狂暴"},
                description = "山贼大当家，实力深不可测",
                dropTable = new List<DropItem>{ new DropItem{ itemId=1, dropRate=0.5f }, new DropItem{ itemId=8, dropRate=0.15f } }
            };
            _dungeons[1] = d1;

            // ==================== 2. 天子峰 ====================
            var d2 = new DungeonData
            {
                id = 2, name = "天子峰", description = "天子峰顶，四大高手齐聚",
                minLevel = 35, maxLevel = 45, minPlayers = 5, maxPlayers = 5,
                timeLimit = 600, sceneName = "Battle", difficulty = 4,
                bossIds = new List<int>{ 3011, 3012, 3013, 3200 }
            };

            _bosses[3011] = new BossData
            {
                id = 3011, name = "影煞", title = "暗影刺客", dungeonId = 2, order = 1,
                hp = 12000, attack = 280, defense = 90, attackInterval = 1.5f,
                skillNames = new[]{"背刺", "隐身"},
                description = "来无影去无踪的刺客",
                dropTable = new List<DropItem>{ new DropItem{ itemId=10, dropRate=0.4f } }
            };

            _bosses[3012] = new BossData
            {
                id = 3012, name = "罗宇", title = "铁壁卫士", dungeonId = 2, order = 2,
                hp = 18000, attack = 200, defense = 250, attackInterval = 3.5f,
                skillNames = new[]{"盾击", "嘲讽"},
                description = "铜墙铁壁般的防御者",
                dropTable = new List<DropItem>{ new DropItem{ itemId=9, dropRate=0.25f } }
            };

            _bosses[3013] = new BossData
            {
                id = 3013, name = "方鹤影", title = "剑术宗师", dungeonId = 2, order = 3,
                hp = 14000, attack = 320, defense = 120, attackInterval = 2.0f,
                skillNames = new[]{"剑气", "连斩"},
                description = "剑法通神的一代宗师",
                dropTable = new List<DropItem>{ new DropItem{ itemId=3, dropRate=0.35f } }
            };

            _bosses[3200] = new BossData
            {
                id = 3200, name = "萧沙", title = "恶人谷主 ★终极", dungeonId = 2, order = 4,
                hp = 35000, attack = 450, defense = 200, attackInterval = 1.5f,
                skillNames = new[]{"恶龙斩", "暗影突袭", "狂暴"},
                description = "恶人谷谷主，天下凶名赫赫",
                dropTable = new List<DropItem>{ new DropItem{ itemId=8, dropRate=0.4f }, new DropItem{ itemId=11, dropRate=0.2f } }
            };
            _dungeons[2] = d2;

            // ==================== 3. 日轮山城 ====================
            var d3 = new DungeonData
            {
                id = 3, name = "日轮山城", description = "东瀛高手盘踞的古城",
                minLevel = 50, maxLevel = 60, minPlayers = 5, maxPlayers = 8,
                timeLimit = 720, sceneName = "Battle", difficulty = 4,
                bossIds = new List<int>{ 3021, 3022, 3023, 3300 }
            };

            _bosses[3021] = new BossData
            {
                id = 3021, name = "源明雅", title = "阴阳师", dungeonId = 3, order = 1,
                hp = 15000, attack = 300, defense = 100, attackInterval = 2.5f,
                skillNames = new[]{"式神召唤", "诅咒"},
                description = "来自东瀛的阴阳师，精通式神之术",
                dropTable = new List<DropItem>{ new DropItem{ itemId=7, dropRate=0.5f } }
            };

            _bosses[3022] = new BossData
            {
                id = 3022, name = "阿坊古", title = "忍者头领", dungeonId = 3, order = 2,
                hp = 13000, attack = 350, defense = 80, attackInterval = 1.2f,
                skillNames = new[]{"手里剑", "影分身"},
                description = "东瀛忍者头领，速度极快",
                dropTable = new List<DropItem>{ new DropItem{ itemId=3, dropRate=0.3f } }
            };

            _bosses[3023] = new BossData
            {
                id = 3023, name = "柳生雪", title = "女剑豪", dungeonId = 3, order = 3,
                hp = 16000, attack = 380, defense = 110, attackInterval = 1.8f,
                skillNames = new[]{"居合斩", "冰刃"},
                description = "东瀛第一女剑豪，剑法诡异",
                dropTable = new List<DropItem>{ new DropItem{ itemId=1, dropRate=0.3f, maxCount=2 } }
            };

            _bosses[3300] = new BossData
            {
                id = 3300, name = "八岐大蛇", title = "上古凶兽 ★终极", dungeonId = 3, order = 4,
                hp = 50000, attack = 550, defense = 250, attackInterval = 2.0f,
                skillNames = new[]{"蛇噬", "毒雾", "狂暴"},
                description = "东瀛神话中的八头巨蛇，毁天灭地",
                dropTable = new List<DropItem>{ new DropItem{ itemId=8, dropRate=0.5f }, new DropItem{ itemId=11, dropRate=0.3f }, new DropItem{ itemId=12, dropRate=0.1f } }
            };
            _dungeons[3] = d3;

            // ==================== 4. 荻花宫 ====================
            var d4 = new DungeonData
            {
                id = 4, name = "荻花宫", description = "红衣教总坛，邪教圣地",
                minLevel = 65, maxLevel = 75, minPlayers = 8, maxPlayers = 8,
                timeLimit = 900, sceneName = "Battle", difficulty = 5,
                bossIds = new List<int>{ 3031, 3032, 3033, 3400 }
            };

            _bosses[3031] = new BossData
            {
                id = 3031, name = "牡丹", title = "红衣护法", dungeonId = 4, order = 1,
                hp = 25000, attack = 350, defense = 150, attackInterval = 2.0f,
                skillNames = new[]{"花雨", "魅惑"},
                description = "红衣教左护法，倾城之姿夺命",
                dropTable = new List<DropItem>{ new DropItem{ itemId=2, dropRate=0.4f }, new DropItem{ itemId=9, dropRate=0.2f } }
            };

            _bosses[3032] = new BossData
            {
                id = 3032, name = "大蛇", title = "守护圣兽", dungeonId = 4, order = 2,
                hp = 30000, attack = 400, defense = 180, attackInterval = 2.5f,
                skillNames = new[]{"缠绕", "毒液"},
                description = "荻花宫地下圣兽，守护红衣教圣物",
                dropTable = new List<DropItem>{ new DropItem{ itemId=7, dropRate=0.6f, maxCount=3 } }
            };

            _bosses[3033] = new BossData
            {
                id = 3033, name = "沙利亚", title = "大祭司", dungeonId = 4, order = 3,
                hp = 22000, attack = 450, defense = 120, attackInterval = 2.0f,
                skillNames = new[]{"暗影箭", "治愈", "暗黑屏障"},
                description = "红衣教大祭司，精通黑暗法术",
                dropTable = new List<DropItem>{ new DropItem{ itemId=3, dropRate=0.4f }, new DropItem{ itemId=10, dropRate=0.5f } }
            };

            _bosses[3400] = new BossData
            {
                id = 3400, name = "阿萨辛", title = "红衣教主 ★终极", dungeonId = 4, order = 4,
                hp = 80000, attack = 650, defense = 300, attackInterval = 1.5f,
                skillNames = new[]{"暗黑之握", "血之诅咒", "灭世", "狂暴"},
                description = "红衣教教主，神秘莫测的真正主宰",
                dropTable = new List<DropItem>{ new DropItem{ itemId=8, dropRate=0.6f }, new DropItem{ itemId=12, dropRate=0.2f }, new DropItem{ itemId=11, dropRate=0.4f } }
            };
            _dungeons[4] = d4;

            Debug.Log($"[DungeonConfig] Loaded {_dungeons.Count} dungeons, {_bosses.Count} bosses");
        }
    }
}