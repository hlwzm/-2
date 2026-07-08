using System.Collections.Generic;
using UnityEngine;

namespace Jx3.Core
{
    /// <summary>
    /// 英雄模板配置 - 定义所有可玩英雄的基础数据
    /// </summary>
    [System.Serializable]
    public class HeroTemplate
    {
        public int id;
        public string name;
        public string title;           // 称号，如"纯阳剑主"
        public HeroAttackType attackType;
        public HeroRoleType roleType;
        public int quality;            // 1-5星
        public int baseHp;
        public int baseAttack;
        public int baseDefense;
        public string description;
        public List<int> bonds;        // 羁绊英雄ID列表
        public List<int> skills;       // 技能ID列表
    }

    public enum HeroAttackType { 外功, 内劲 }
    public enum HeroRoleType { 输出, 坦克, 治疗 }

    public static class HeroConfig
    {
        private static Dictionary<int, HeroTemplate> _heroes;

        public static Dictionary<int, HeroTemplate> GetAll()
        {
            if (_heroes == null) Init();
            return _heroes;
        }

        public static HeroTemplate Get(int id)
        {
            if (_heroes == null) Init();
            return _heroes.ContainsKey(id) ? _heroes[id] : null;
        }

        static void Init()
        {
            _heroes = new Dictionary<int, HeroTemplate>
            {
                { 1001, new HeroTemplate { id=1001, name="李忘生", title="纯阳剑主", attackType=HeroAttackType.内劲, roleType=HeroRoleType.输出, quality=4, baseHp=3200, baseAttack=280, baseDefense=120, description="纯阳宫掌门，剑术通神", bonds=new List<int>{1002,1005}, skills=new List<int>{1,2,3} }},
                { 1002, new HeroTemplate { id=1002, name="谢云流", title="剑魔传人", attackType=HeroAttackType.外功, roleType=HeroRoleType.输出, quality=5, baseHp=3600, baseAttack=320, baseDefense=100, description="剑魔再传弟子，快剑无双", bonds=new List<int>{1001,1003}, skills=new List<int>{4,5,6} }},
                { 1003, new HeroTemplate { id=1003, name="叶英", title="藏剑山庄", attackType=HeroAttackType.外功, roleType=HeroRoleType.输出, quality=5, baseHp=3400, baseAttack=300, baseDefense=130, description="藏剑山庄庄主，西湖剑客", bonds=new List<int>{1002,1006}, skills=new List<int>{7,8,9} }},
                { 1004, new HeroTemplate { id=1004, name="曲云", title="五毒教主", attackType=HeroAttackType.内劲, roleType=HeroRoleType.治疗, quality=4, baseHp=3000, baseAttack=200, baseDefense=90, description="五仙教教主，蛊术通天", bonds=new List<int>{1005,1008}, skills=new List<int>{10,11,12} }},
                { 1005, new HeroTemplate { id=1005, name="叶炜", title="剑圣之徒", attackType=HeroAttackType.外功, roleType=HeroRoleType.输出, quality=3, baseHp=2800, baseAttack=260, baseDefense=110, description="剑圣门下，江湖新秀", bonds=new List<int>{1001,1004}, skills=new List<int>{13,14,15} }},
                { 1006, new HeroTemplate { id=1006, name="玄正", title="少林方丈", attackType=HeroAttackType.内劲, roleType=HeroRoleType.坦克, quality=4, baseHp=4500, baseAttack=180, baseDefense=200, description="少林寺方丈，金刚不坏", bonds=new List<int>{1003,1007}, skills=new List<int>{16,17,18} }},
                { 1007, new HeroTemplate { id=1007, name="萧沙", title="恶人谷主", attackType=HeroAttackType.外功, roleType=HeroRoleType.坦克, quality=5, baseHp=5000, baseAttack=240, baseDefense=220, description="恶人谷谷主，凶名在外", bonds=new List<int>{1006,1008}, skills=new List<int>{19,20,21} }},
                { 1008, new HeroTemplate { id=1008, name="阿萨辛", title="红衣教主", attackType=HeroAttackType.内劲, roleType=HeroRoleType.治疗, quality=5, baseHp=3200, baseAttack=220, baseDefense=95, description="红衣教教主，神秘莫测", bonds=new List<int>{1004,1007}, skills=new List<int>{22,23,24} }},
                // 新英雄
                { 2001, new HeroTemplate { id=2001, name="公孙大娘", title="七秀之首", attackType=HeroAttackType.内劲, roleType=HeroRoleType.治疗, quality=4, baseHp=3100, baseAttack=210, baseDefense=85, description="七秀坊创始人，剑舞倾城", bonds=new List<int>{2002}, skills=new List<int>{25,26,27} }},
                { 2002, new HeroTemplate { id=2002, name="柳惊涛", title="天策上将", attackType=HeroAttackType.外功, roleType=HeroRoleType.坦克, quality=4, baseHp=4800, baseAttack=230, baseDefense=210, description="天策府统领，铁血丹心", bonds=new List<int>{2001}, skills=new List<int>{28,29,30} }},
            };
            Debug.Log($"[HeroConfig] Loaded {_heroes.Count} heroes");
        }

        public static string GetQualityColor(int quality)
        {
            return quality switch
            {
                1 => "#cccccc",  // 白
                2 => "#66cc66",  // 绿
                3 => "#6666cc",  // 蓝
                4 => "#cc66cc",  // 紫
                5 => "#ffcc44",  // 金
                _ => "#cccccc"
            };
        }
    }
}