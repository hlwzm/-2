using Xunit;
using Jx3.Battle;

namespace Jx3.Tests;

public class CombatEngineTests
{
    [Fact]
    public void DamageCalculation_ValidInput_ReturnsPositiveDamage()
    {
        var attacker = new CombatUnit { Name = "atk", Attack = 100, Defense = 20, CritRate = 0.1f, CritDamage = 0.2f, DodgeRate = 0.05f, Level = 1, MaxHp = 2000, Hp = 2000, AttackType = 0, DefenseType = 0 };
        var target = new CombatUnit { Name = "def", Attack = 50, Defense = 30, CritRate = 0.05f, CritDamage = 0.1f, DodgeRate = 0.02f, Level = 1, MaxHp = 1000, Hp = 1000, AttackType = 0, DefenseType = 0 };
        var result = DamageCalculator.CalculateDamage(attacker, target, 1.5f);
        Assert.True(result.Damage > 0);
        Assert.True(result.Damage < 300);
    }

    [Fact]
    public void DamageCalculation_ZeroDefense_HighDamage()
    {
        var attacker = new CombatUnit { Name = "atk", Attack = 200, Defense = 0, CritRate = 0, CritDamage = 0, DodgeRate = 0, Level = 1, MaxHp = 2000, Hp = 2000, AttackType = 0, DefenseType = 0 };
        var target = new CombatUnit { Name = "def", Attack = 0, Defense = 0, CritRate = 0, CritDamage = 0, DodgeRate = 0, Level = 1, MaxHp = 5000, Hp = 5000, AttackType = 0, DefenseType = 0 };
        var result = DamageCalculator.CalculateDamage(attacker, target, 2.0f);
        Assert.True(result.Damage > 300);
    }

    [Fact]
    public void Buff_InitialState_Correct()
    {
        var buff = new BuffInstance { BuffId = 1, Duration = 3.0f, RemainTime = 3.0f, StackCount = 1, Name = "test" };
        Assert.True(buff.RemainTime > 0);
        Assert.Equal(1, buff.StackCount);
    }

    [Fact]
    public void Unit_DeadWhenHpZero()
    {
        var unit = new CombatUnit { Hp = 0, MaxHp = 100 };
        Assert.True(unit.IsDead);
    }

    [Fact]
    public void Unit_AliveWhenHpPositive()
    {
        var unit = new CombatUnit { Hp = 50, MaxHp = 100 };
        Assert.False(unit.IsDead);
    }

    [Fact]
    public void CombatInstance_AllAlliesDead_Detection()
    {
        var combat = new CombatInstance
        {
            CombatId = "test",
            Allies = new List<CombatUnit> { new() { Hp = 0, MaxHp = 100 }, new() { Hp = 0, MaxHp = 100 } }
        };
        Assert.True(combat.IsAllAlliesDead);
    }

    [Fact]
    public void CreateCombat_ValidInput_ReturnsInstance()
    {
        var heroes = new List<HeroTemplate> { new() { Name = "H1", MaxHp = 2000, Attack = 150, Defense = 60, Level = 1 } };
        var enemies = new List<EnemyTemplate> { new() { Name = "E1", UnitType = 1, MaxHp = 800, Attack = 60, Defense = 30, Level = 1 } };
        var combat = CombatEngine.CreateCombat(0, 1001, heroes, enemies);
        Assert.NotNull(combat);
        Assert.False(string.IsNullOrEmpty(combat.CombatId));
        Assert.Single(combat.Allies);
        Assert.Single(combat.Enemies);
    }
}