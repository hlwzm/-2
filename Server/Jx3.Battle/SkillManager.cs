using Jx3.Common.Utils;
namespace Jx3.Battle;

/// <summary>技能定义</summary>
public class SkillDef
{
    public uint SkillId { get; set; }
    public string Name { get; set; } = "";
    public float Cooldown { get; set; } // 秒
    public float Range { get; set; } // 最大距离
    public float SkillCoeff { get; set; } // 攻击系数
    public int SkillType { get; set; } // 0=伤害 1=治疗 2=控制
    public int BuffId { get; set; } // 附带Buff ID (0=无)
    public string BuffName { get; set; } = "";
    public float BuffDuration { get; set; }
    public int BuffType { get; set; }
    public int BuffTickDamage { get; set; }
    public int BuffTickHeal { get; set; }
    public float AoeRadius { get; set; } // 0=单体
    public string Description { get; set; } = "";
}

/// <summary>技能施放结果</summary>
public class SkillCastResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public uint SkillId { get; set; }
    public string SkillName { get; set; } = "";
    public uint CasterUnitId { get; set; }
    public List<DamageResult> DamageResults { get; set; } = new();
    public List<BuffApplyResult> BuffResults { get; set; } = new();
    public float NewCooldown { get; set; }
}

/// <summary>Buff应用结果</summary>
public class BuffApplyResult
{
    public uint TargetUnitId { get; set; }
    public string TargetName { get; set; } = "";
    public int BuffId { get; set; }
    public string BuffName { get; set; } = "";
    public int StackCount { get; set; }
    public float Duration { get; set; }
    public bool IsRemoved { get; set; }
}

/// <summary>技能管理器</summary>
public static class SkillManager
{
    /// <summary>预设技能库 (英雄技能模板)</summary>
    private static readonly Dictionary<uint, SkillDef> _skillDefs = new()
    {
        // 通用基础技能
        { 1001, new SkillDef { SkillId = 1001, Name = "普通攻击", Cooldown = 1.0f, Range = 5f, SkillCoeff = 1.0f, SkillType = 0, Description = "基础近战攻击" } },
        { 1002, new SkillDef { SkillId = 1002, Name = "猛击", Cooldown = 5.0f, Range = 3f, SkillCoeff = 2.5f, SkillType = 0, Description = "强力近战攻击" } },
        { 1003, new SkillDef { SkillId = 1003, Name = "治疗", Cooldown = 8.0f, Range = 8f, SkillCoeff = 3.0f, SkillType = 1, Description = "治疗友方单位" } },
        { 1004, new SkillDef { SkillId = 1004, Name = "火焰斩", Cooldown = 6.0f, Range = 4f, SkillCoeff = 3.0f, SkillType = 0,
            BuffId = 1, BuffName = "灼烧", BuffDuration = 6f, BuffType = 2, BuffTickDamage = 15, Description = "附带灼烧DoT" } },
        { 1005, new SkillDef { SkillId = 1005, Name = "冰霜新星", Cooldown = 10.0f, Range = 6f, SkillCoeff = 1.5f, SkillType = 0,
            AoeRadius = 4f, BuffId = 2, BuffName = "减速", BuffDuration = 3f, BuffType = 4, Description = "范围伤害+减速" } },
        { 1006, new SkillDef { SkillId = 1006, Name = "守护之盾", Cooldown = 12.0f, Range = 0f, SkillCoeff = 0f, SkillType = 0,
            BuffId = 3, BuffName = "护盾", BuffDuration = 5f, BuffType = 0, Description = "自身增益Buff" } },
        { 1007, new SkillDef { SkillId = 1007, Name = "愈合", Cooldown = 6.0f, Range = 8f, SkillCoeff = 2.0f, SkillType = 1,
            BuffId = 4, BuffName = "持续回复", BuffDuration = 8f, BuffType = 3, BuffTickHeal = 20, Description = "治疗+HoT" } },
        // 敌方技能
        { 2001, new SkillDef { SkillId = 2001, Name = "怪物爪击", Cooldown = 2.0f, Range = 3f, SkillCoeff = 1.2f, SkillType = 0, Description = "基础怪物攻击" } },
        { 2002, new SkillDef { SkillId = 2002, Name = "Boss毁灭打击", Cooldown = 8.0f, Range = 6f, SkillCoeff = 3.5f, SkillType = 0, Description = "Boss强力攻击" } },
    };

    /// <summary>获取技能定义</summary>
    public static SkillDef? GetSkillDef(uint skillId)
    {
        _skillDefs.TryGetValue(skillId, out var def);
        return def;
    }

    /// <summary>获取所有技能定义</summary>
    public static List<SkillDef> GetAllSkillDefs() => _skillDefs.Values.ToList();

    /// <summary>获取单位的可用技能列表 (根据英雄类型)</summary>
    public static List<uint> GetSkillsForUnit(uint unitId, int unitType)
    {
        return unitType switch
        {
            0 => new List<uint> { 1001, 1002, 1003, 1004, 1005, 1006, 1007 }, // 英雄技能
            1 => new List<uint> { 2001 }, // 怪物技能
            2 => new List<uint> { 2001, 2002 }, // Boss技能
            _ => new List<uint> { 1001 }
        };
    }

    /// <summary>施放技能</summary>
    public static SkillCastResult CastSkill(CombatInstance combat, uint casterUnitId, uint skillId, uint targetUnitId)
    {
        var result = new SkillCastResult
        {
            SkillId = skillId,
            CasterUnitId = casterUnitId,
            Success = false,
            Message = "未知错误"
        };

        // 获取施法者
        var caster = combat.Allies.Find(u => u.UnitId == casterUnitId)
                    ?? combat.Enemies.Find(u => u.UnitId == casterUnitId);
        if (caster == null)
        {
            result.Message = "施法者不存在";
            return result;
        }

        if (caster.IsDead)
        {
            result.Message = "施法者已阵亡";
            return result;
        }

        // 获取技能定义
        var skillDef = GetSkillDef(skillId);
        if (skillDef == null)
        {
            result.Message = $"技能不存在 ID={skillId}";
            return result;
        }

        result.SkillName = skillDef.Name;

        // 检查CD
        if (caster.SkillCdRemain > 0)
        {
            result.Message = $"技能CD中，剩余{caster.SkillCdRemain:F1}秒";
            return result;
        }

        // 获取目标
        var target = combat.Allies.Find(u => u.UnitId == targetUnitId)
                    ?? combat.Enemies.Find(u => u.UnitId == targetUnitId);
        if (target == null)
        {
            result.Message = "目标不存在";
            return result;
        }

        if (target.IsDead)
        {
            result.Message = "目标已阵亡";
            return result;
        }

        // 检查距离
        float dist = caster.DistanceTo(target);
        if (skillDef.Range > 0 && dist > skillDef.Range)
        {
            result.Message = $"目标超出距离 range={skillDef.Range} dist={dist:F1}";
            return result;
        }

        // 冷却CD
        caster.SkillCdRemain = skillDef.Cooldown;
        result.NewCooldown = skillDef.Cooldown;
        caster.CurrentSkillId = skillId;

        // 技能类型处理
        if (skillDef.SkillType == 0) // 伤害技能
        {
            if (skillDef.AoeRadius > 0)
            {
                // AOE - 伤害范围内所有敌人
                var enemies = combat.Allies.Contains(caster) ? combat.Enemies : combat.Allies;
                foreach (var enemy in enemies)
                {
                    if (enemy.IsDead) continue;
                    if (caster.DistanceTo(enemy) <= skillDef.AoeRadius)
                    {
                        var dmgResult = DamageCalculator.CalculateDamage(caster, enemy, skillDef.SkillCoeff);
                        result.DamageResults.Add(dmgResult);

                        if (dmgResult.IsCrit) combat.Stats.CurrentCombo++;
                        if (!dmgResult.IsDodged) combat.Stats.TotalDamage += dmgResult.Damage;
                        if (dmgResult.IsKill) { combat.Stats.KillCount++; }
                    }
                }
            }
            else
            {
                // 单体伤害
                var dmgResult = DamageCalculator.CalculateDamage(caster, target, skillDef.SkillCoeff);
                result.DamageResults.Add(dmgResult);

                if (dmgResult.IsCrit) combat.Stats.CurrentCombo++;
                if (!dmgResult.IsDodged) combat.Stats.TotalDamage += dmgResult.Damage;
                if (dmgResult.IsKill) { combat.Stats.KillCount++; }
            }
        }
        else if (skillDef.SkillType == 1) // 治疗技能
        {
            var healAmount = DamageCalculator.CalculateHeal(caster, skillDef.SkillCoeff);
            target.Hp = Math.Min(target.MaxHp, target.Hp + healAmount);
            result.DamageResults.Add(new DamageResult
            {
                TargetUnitId = target.UnitId,
                TargetName = target.Name,
                Damage = -healAmount, // 负值=治疗
                RemainingHp = target.Hp
            });
        }

        // 附带Buff
        if (skillDef.BuffId > 0)
        {
            var buffTargets = skillDef.SkillType == 1 ? new List<CombatUnit> { target } : new List<CombatUnit> { target };
            if (skillDef.SkillType == 0 && skillDef.AoeRadius > 0)
            {
                // AOE 技能对所有受击目标加Buff
                buffTargets = skillDef.BuffType == 0
                    ? new List<CombatUnit> { caster } // 增益Buff给自己
                    : combat.Allies.Contains(caster) ? combat.Enemies : combat.Allies;
            }

            foreach (var bt in buffTargets)
            {
                if (bt.IsDead) continue;

                // 控制类Buff只对敌方
                if (skillDef.BuffType == 4 && combat.Allies.Contains(bt) && combat.Allies.Contains(caster))
                    continue;

                var buff = CombatEngine.AddBuff(bt, skillDef.BuffId, skillDef.BuffName,
                    skillDef.BuffDuration, 1, skillDef.BuffType,
                    skillDef.BuffTickDamage, skillDef.BuffTickHeal);

                result.BuffResults.Add(new BuffApplyResult
                {
                    TargetUnitId = bt.UnitId,
                    TargetName = bt.Name,
                    BuffId = buff.BuffId,
                    BuffName = buff.Name,
                    StackCount = buff.StackCount,
                    Duration = buff.RemainTime
                });
            }
        }

        result.Success = true;
        result.Message = $"{skillDef.Name} 施放成功";

        // 更新连击
        if (result.DamageResults.All(d => !d.IsCrit))
        {
            combat.Stats.CurrentCombo = 0;
        }
        combat.Stats.MaxCombo = Math.Max(combat.Stats.MaxCombo, combat.Stats.CurrentCombo);

        Logger.Info("SkillManager", $"{caster.Name} 施放 {skillDef.Name} -> {target.Name} 伤害={string.Join(",", result.DamageResults.Select(d => d.Damage))}");
        return result;
    }
}