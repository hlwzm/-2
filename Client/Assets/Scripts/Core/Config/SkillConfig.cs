using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Jx3.Core
{
    [System.Serializable]
    public class SkillData
    {
        public int id;
        public string name;
        public string description;
        public int ownerHeroId;
        public SkillType type;
        public SkillTarget target;
        public float damageMultiplier;  // 伤害倍率
        public float cooldown;         // 冷却时间(秒)
        public int cost;               // 消耗(内力/怒气)
        public string iconChar;        // 图标占位字符

        // Buff效果
        public bool hasBuff;
        public string buffName;
        public float buffDuration;
        public float buffValue;
    }

    public enum SkillType { 主动, 被动, 终极 }
    public enum SkillTarget { 单体, 群体, 自身, 友方 }

    public static class SkillConfig
    {
        private static Dictionary<int, SkillData> _skills;

        public static SkillData Get(int id)
        {
            if (_skills == null) Init();
            return _skills.ContainsKey(id) ? _skills[id] : null;
        }

        public static List<SkillData> GetHeroSkills(int heroId)
        {
            if (_skills == null) Init();
            return _skills.Values.Where(s => s.ownerHeroId == heroId).ToList();
        }

        static void Init()
        {
            _skills = new Dictionary<int, SkillData>
            {
                // 李忘生 (1001)
                { 1, new SkillData{id=1, name="太虚剑法", description="以气驭剑，对单体目标造成内功伤害", ownerHeroId=1001, type=SkillType.主动, target=SkillTarget.单体, damageMultiplier=1.8f, cooldown=4, cost=30, iconChar="剑" }},
                { 2, new SkillData{id=2, name="生太极", description="布下太极气场，对范围内敌人持续伤害", ownerHeroId=1001, type=SkillType.主动, target=SkillTarget.群体, damageMultiplier=2.5f, cooldown=8, cost=50, iconChar="极" }},
                { 3, new SkillData{id=3, name="万剑归宗", description="召唤万剑齐发，造成巨额群体伤害", ownerHeroId=1001, type=SkillType.终极, target=SkillTarget.群体, damageMultiplier=5.0f, cooldown=30, cost=100, iconChar="万" }},
                // 谢云流 (1002)
                { 4, new SkillData{id=4, name="孤剑诀", description="极速出剑，对单体造成外功伤害", ownerHeroId=1002, type=SkillType.主动, target=SkillTarget.单体, damageMultiplier=2.0f, cooldown=3, cost=25, iconChar="孤" }},
                { 5, new SkillData{id=5, name="剑冲阴阳", description="剑气纵横，对前方直线敌人造成伤害", ownerHeroId=1002, type=SkillType.主动, target=SkillTarget.群体, damageMultiplier=2.2f, cooldown=6, cost=45, iconChar="冲" }},
                { 6, new SkillData{id=6, name="天地无极", description="剑意爆发，对全屏敌人造成毁灭性打击", ownerHeroId=1002, type=SkillType.终极, target=SkillTarget.群体, damageMultiplier=5.5f, cooldown=35, cost=100, iconChar="极" }},
                // 曲云 (1004) - 治疗
                { 10, new SkillData{id=10, name="补天诀", description="以蛊术治疗友方目标", ownerHeroId=1004, type=SkillType.主动, target=SkillTarget.友方, damageMultiplier=-3.0f, cooldown=4, cost=30, iconChar="补", hasBuff=true, buffName="持续治疗", buffDuration=5, buffValue=50 }},
                { 11, new SkillData{id=11, name="千蛊噬心", description="对敌方施放蛊毒，持续造成内功伤害", ownerHeroId=1004, type=SkillType.主动, target=SkillTarget.单体, damageMultiplier=1.5f, cooldown=6, cost=40, iconChar="蛊", hasBuff=true, buffName="蛊毒", buffDuration=8, buffValue=30 }},
                { 12, new SkillData{id=12, name="碧蝶引", description="召唤蝶群，治疗全体友方", ownerHeroId=1004, type=SkillType.终极, target=SkillTarget.友方, damageMultiplier=-8.0f, cooldown=30, cost=100, iconChar="蝶" }},
                // 玄正 (1006) - 坦克
                { 16, new SkillData{id=16, name="金刚诀", description="运转金刚不坏体，大幅提升防御", ownerHeroId=1006, type=SkillType.主动, target=SkillTarget.自身, damageMultiplier=1.0f, cooldown=6, cost=20, iconChar="刚", hasBuff=true, buffName="金刚", buffDuration=8, buffValue=50 }},
                { 17, new SkillData{id=17, name="达摩掌", description="佛门掌法，对单体造成伤害并嘲讽", ownerHeroId=1006, type=SkillType.主动, target=SkillTarget.单体, damageMultiplier=1.5f, cooldown=5, cost=30, iconChar="掌" }},
                { 18, new SkillData{id=18, name="如来神掌", description="佛光普照，对范围敌人造成伤害并眩晕", ownerHeroId=1006, type=SkillType.终极, target=SkillTarget.群体, damageMultiplier=4.0f, cooldown=30, cost=100, iconChar="佛" }},
                // 叶炜 (1005) - 新手
                { 13, new SkillData{id=13, name="剑意诀", description="凝聚剑气，对单体造成外功伤害", ownerHeroId=1005, type=SkillType.主动, target=SkillTarget.单体, damageMultiplier=1.5f, cooldown=3, cost=20, iconChar="意" }},
                { 14, new SkillData{id=14, name="剑荡八方", description="剑气横扫，对周围敌人造成伤害", ownerHeroId=1005, type=SkillType.主动, target=SkillTarget.群体, damageMultiplier=1.8f, cooldown=5, cost=35, iconChar="荡" }},
                { 15, new SkillData{id=15, name="一剑西来", description="天外飞仙，对单体造成致命一击", ownerHeroId=1005, type=SkillType.终极, target=SkillTarget.单体, damageMultiplier=4.5f, cooldown=25, cost=80, iconChar="西" }},
            };
            Debug.Log($"[SkillConfig] Loaded {_skills.Count} skills");
        }
    }
}