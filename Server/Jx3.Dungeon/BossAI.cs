using Jx3.Common.Utils;

namespace Jx3.Dungeon;

/// <summary>Boss技能和行为树定义</summary>
public class BossSkillDef
{
    public string Name = "";
    public int SkillType;  // 0=单体 1=扇形AOE 2=圆形AOE 3=全屏 4=召唤 5=特殊
    public int DamagePercent; // 伤害系数(攻击力百分比)
    public float CastTime;    // 读条秒数
    public string Warning = ""; // 预警文字
}

public static class BossAI
{
    // 获取Boss当前应该施放的技能
    public static BossSkillDef? GetBossSkill(int dungeonId, int bossIndex, int phase, int elapsedSec)
    {
        var key = (dungeonId, bossIndex);
        var tree = GetBehaviorTree(key);
        if (tree == null) return null;

        // 根据阶段和经过时间选择技能
        var phaseSkills = tree.Where(s => s.Phase <= phase).ToList();
        if (phaseSkills.Count == 0) return null;

        // 循环技能：每3-6秒放一个
        var idx = (elapsedSec / 4) % phaseSkills.Count;
        return phaseSkills[idx].Skill;
    }

    private static List<(int Phase, BossSkillDef Skill)> GetBehaviorTree((int, int) key)
    {
        return key switch
        {
            // ==== 风雨稻香村 - 董龙 ====
            (1, 0) => new()
            {
                (1, new BossSkillDef { Name = "毒尾横扫", SkillType = 1, DamagePercent = 120, CastTime = 1.5f, Warning = "董龙抬起尾巴，准备横扫！" }),
                (2, new BossSkillDef { Name = "毒雾", SkillType = 2, DamagePercent = 80, CastTime = 2.0f, Warning = "董龙喷出毒雾！" }),
                (2, new BossSkillDef { Name = "召唤毒蛇", SkillType = 4, DamagePercent = 0, CastTime = 1.0f, Warning = "毒蛇出现了！" }),
                (3, new BossSkillDef { Name = "全屏毒爆", SkillType = 3, DamagePercent = 200, CastTime = 3.0f, Warning = "⚠️ 全屏毒爆！快打断！" }),
            },

            // ==== 风雨稻香村 - 汪莽 ====
            (1, 1) => new()
            {
                (1, new BossSkillDef { Name = "咆哮召唤", SkillType = 4, DamagePercent = 0, CastTime = 1.5f, Warning = "汪莽咆哮，召唤山贼！" }),
                (1, new BossSkillDef { Name = "重劈", SkillType = 0, DamagePercent = 150, CastTime = 2.0f, Warning = "汪莽举起大刀！" }),
                (2, new BossSkillDef { Name = "旋风斩", SkillType = 2, DamagePercent = 180, CastTime = 2.5f, Warning = "⚠️ 旋风斩！快远离！" }),
            },

            // ==== 风雨稻香村 - 肖人德 ====
            (1, 2) => new()
            {
                (1, new BossSkillDef { Name = "毒镖", SkillType = 0, DamagePercent = 130, CastTime = 1.0f, Warning = "肖人德射出毒镖！" }),
                (1, new BossSkillDef { Name = "隐身", SkillType = 5, DamagePercent = 0, CastTime = 1.5f, Warning = "肖人德消失了！" }),
                (2, new BossSkillDef { Name = "毒爆", SkillType = 3, DamagePercent = 160, CastTime = 2.5f, Warning = "⚠️ 找掩体！毒爆来了！" }),
            },

            // ==== 风雨稻香村 - 秦颐岩(终极Boss) ====
            (1, 3) => new()
            {
                (1, new BossSkillDef { Name = "毒龙鞭", SkillType = 1, DamagePercent = 140, CastTime = 2.0f, Warning = "秦颐岩挥出毒龙鞭！" }),
                (1, new BossSkillDef { Name = "蛇影", SkillType = 4, DamagePercent = 0, CastTime = 1.5f, Warning = "毒蛇从暗处涌出！" }),
                (2, new BossSkillDef { Name = "毒龙转圈", SkillType = 2, DamagePercent = 100, CastTime = 2.0f, Warning = "毒龙转圈！保持距离！" }),
                (2, new BossSkillDef { Name = "毒雾弥漫", SkillType = 3, DamagePercent = 60, CastTime = 2.0f, Warning = "全屏毒雾弥漫！" }),
                (3, new BossSkillDef { Name = "全屏毒爆", SkillType = 3, DamagePercent = 300, CastTime = 3.0f, Warning = "⚠️ 全屏秒杀！必须打断！" }),
            },

            // ==== 天子峰 - 影煞 ====
            (2, 0) => new()
            {
                (1, new BossSkillDef { Name = "暗影突袭", SkillType = 0, DamagePercent = 130, CastTime = 1.5f, Warning = "影煞消失在黑暗中！" }),
                (2, new BossSkillDef { Name = "分身术", SkillType = 4, DamagePercent = 0, CastTime = 2.0f, Warning = "影煞召唤出分身！" }),
                (3, new BossSkillDef { Name = "暗影爆发", SkillType = 3, DamagePercent = 180, CastTime = 3.0f, Warning = "⚠️ 暗影能量爆发！" }),
            },

            // ==== 日轮山城 - 源明雅 ====
            (3, 0) => new()
            {
                (1, new BossSkillDef { Name = "阴阳术·火", SkillType = 2, DamagePercent = 120, CastTime = 2.0f, Warning = "源明雅催动阴阳术！" }),
                (2, new BossSkillDef { Name = "召唤式神", SkillType = 4, DamagePercent = 0, CastTime = 1.5f, Warning = "式神从阵法中出现！" }),
                (3, new BossSkillDef { Name = "阴阳·灭", SkillType = 3, DamagePercent = 200, CastTime = 3.0f, Warning = "⚠️ 阴阳逆转！" }),
            },

            // ==== 荻花宫 - 牡丹 ====
            (4, 0) => new()
            {
                (1, new BossSkillDef { Name = "花毒粉", SkillType = 3, DamagePercent = 80, CastTime = 2.0f, Warning = "牡丹散出花毒粉！" }),
                (2, new BossSkillDef { Name = "藤蔓束缚", SkillType = 5, DamagePercent = 0, CastTime = 1.5f, Warning = "藤蔓从地面长出！" }),
                (3, new BossSkillDef { Name = "花之怒", SkillType = 3, DamagePercent = 220, CastTime = 3.0f, Warning = "⚠️ 牡丹盛放！全屏攻击！" }),
            },

            _ => null!,
        };
    }
}