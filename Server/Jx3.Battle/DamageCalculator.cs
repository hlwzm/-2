namespace Jx3.Battle;

/// <summary>伤害计算结果</summary>
public class DamageResult
{
    public uint TargetUnitId { get; set; }
    public string TargetName { get; set; } = "";
    public int Damage { get; set; }
    public bool IsCrit { get; set; }
    public bool IsDodged { get; set; }
    public bool IsKill { get; set; }
    public int RemainingHp { get; set; }
}

/// <summary>伤害计算器</summary>
public static class DamageCalculator
{
    private static readonly Random _rng = new();

    /// <summary>
    /// 计算最终伤害
    /// 公式: 最终伤害 = (攻击力 × 技能系数 - 目标防御 × 0.5)
    ///   × 属性克制(外功/内功) × 等级压制
    ///   × (1 + 会心加成) × 随机浮动(0.95~1.05)
    /// </summary>
    public static DamageResult CalculateDamage(CombatUnit attacker, CombatUnit target, float skillCoeff)
    {
        var result = new DamageResult
        {
            TargetUnitId = target.UnitId,
            TargetName = target.Name
        };

        // 闪避判定
        if (CombatEngine.CheckDodge(target))
        {
            result.IsDodged = true;
            result.Damage = 0;
            result.RemainingHp = target.Hp;
            return result;
        }

        // 基础伤害
        float baseDamage = attacker.Attack * skillCoeff - target.Defense * 0.5f;
        if (baseDamage < 1) baseDamage = 1; // 最低伤害1点

        // 属性克制: 攻击类型与防御类型匹配时 100%，不匹配时 80%
        float typeFactor = (attacker.AttackType == target.DefenseType) ? 1.0f : 0.8f;

        // 等级压制: 每级差 ±2%
        int levelDiff = attacker.Level - target.Level;
        float levelFactor = 1.0f + levelDiff * 0.02f;
        levelFactor = Math.Clamp(levelFactor, 0.5f, 1.5f);

        // 会心判定
        bool isCrit = false;
        float critMultiplier = 1.0f;
        float critChance = attacker.CritRate - target.DodgeRate * 0.5f; // 御劲抵消部分会心
        if (critChance > 0 && _rng.NextDouble() < critChance)
        {
            isCrit = true;
            critMultiplier = 1.5f + attacker.CritDamage;
        }

        // 随机浮动 0.95~1.05
        float randomFactor = 0.95f + (float)_rng.NextDouble() * 0.1f;

        // 最终伤害
        int finalDamage = (int)(baseDamage * typeFactor * levelFactor * critMultiplier * randomFactor);
        if (finalDamage < 1) finalDamage = 1;

        // 应用伤害
        int actualHpLoss = Math.Min(finalDamage, target.Hp);
        target.Hp -= actualHpLoss;
        if (target.Hp < 0) target.Hp = 0;

        result.Damage = actualHpLoss;
        result.IsCrit = isCrit;
        result.IsKill = target.IsDead;
        result.RemainingHp = target.Hp;

        return result;
    }

    /// <summary>治疗计算</summary>
    public static int CalculateHeal(CombatUnit healer, float healCoeff)
    {
        float baseHeal = healer.Attack * healCoeff;
        float randomFactor = 0.95f + (float)_rng.NextDouble() * 0.1f;
        return Math.Max(1, (int)(baseHeal * randomFactor));
    }
}