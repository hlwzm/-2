using Jx3.Common.Utils;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Jx3.Battle;

// ========== 数据结构 ==========

/// <summary>战斗实例</summary>
public class CombatInstance
{
    public string CombatId { get; set; } = "";
    public int CombatType { get; set; } // 0=副本 1=竞技场 2=野外
    public List<CombatUnit> Allies { get; set; } = new();
    public List<CombatUnit> Enemies { get; set; } = new();
    public DateTime StartTime { get; set; }
    public int TimeLimit { get; set; } // 秒
    public int CurrentHeroIndex { get; set; }
    public CombatStats Stats { get; set; } = new();
    public bool IsAutoBattle { get; set; }
    public bool IsEnded { get; set; }
    public DateTime LastTick { get; set; }
    public long PlayerId { get; set; }

    public CombatUnit CurrentHero => Allies[CurrentHeroIndex];

    public bool IsAllAlliesDead => Allies.TrueForAll(u => u.IsDead);
    public bool IsAllEnemiesDead => Enemies.TrueForAll(u => u.IsDead);
}

/// <summary>战斗单位</summary>
public class CombatUnit
{
    public uint UnitId { get; set; }
    public string Name { get; set; } = "";
    public int UnitType { get; set; } // 0=英雄 1=怪物 2=Boss
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int AttackType { get; set; } // 0=外功 1=内功
    public int DefenseType { get; set; } // 0=外防 1=内防
    public float CritRate { get; set; }
    public float CritDamage { get; set; }
    public float DodgeRate { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Facing { get; set; }
    public List<BuffInstance> Buffs { get; set; } = new();
    public uint CurrentSkillId { get; set; }
    public float SkillCdRemain { get; set; }
    public int Level { get; set; } = 1;

    public bool IsDead => Hp <= 0;

    public float DistanceTo(CombatUnit other) =>
        MathF.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y) + (Z - other.Z) * (Z - other.Z));

    public float DistanceToSqr(CombatUnit other) =>
        (X - other.X) * (X - other.X) + (Z - other.Z) * (Z - other.Z);
}

/// <summary>Buff 实例</summary>
public class BuffInstance
{
    public int BuffId { get; set; }
    public string Name { get; set; } = "";
    public float Duration { get; set; }
    public float RemainTime { get; set; }
    public int StackCount { get; set; }
    public int BuffType { get; set; } // 0=增益 1=减益 2=DoT 3=HoT 4=控制
    public int TickDamage { get; set; }
    public int TickHeal { get; set; }
}

/// <summary>战斗统计</summary>
public class CombatStats
{
    public long TotalDamage { get; set; }
    public int MaxCombo { get; set; }
    public int CurrentCombo { get; set; }
    public int DodgeCount { get; set; }
    public float Rating { get; set; }
    public int KillCount { get; set; }
}

/// <summary>英雄模板 (用于创建战斗)</summary>
public class HeroTemplate
{
    public string Name { get; set; } = "";
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int AttackType { get; set; }
    public int DefenseType { get; set; }
    public float CritRate { get; set; }
    public float CritDamage { get; set; }
    public float DodgeRate { get; set; }
    public int Level { get; set; } = 1;
}

/// <summary>敌方模板</summary>
public class EnemyTemplate
{
    public string Name { get; set; } = "";
    public int UnitType { get; set; }
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int AttackType { get; set; }
    public int DefenseType { get; set; }
    public float CritRate { get; set; }
    public float CritDamage { get; set; }
    public float DodgeRate { get; set; }
    public int Level { get; set; } = 1;
}

// ========== 战斗引擎 ==========

/// <summary>战斗引擎核心 - 管理战斗生命周期</summary>
public static class CombatEngine
{
    private static readonly ConcurrentDictionary<string, CombatInstance> _battles = new();
    private static uint _nextUnitId = 1;
    private static readonly Random _rng = new();
    private static Timer? _tickTimer;

    static CombatEngine()
    {
        // 启动全局 Tick 定时器 (每秒)
        _tickTimer = new Timer(TickCallback, null, 1000, 1000);
    }

    /// <summary>创建战斗实例</summary>
    public static CombatInstance CreateCombat(int combatType, long playerId,
        List<HeroTemplate> allyHeroes, List<EnemyTemplate> enemies)
    {
        var combatId = Guid.NewGuid().ToString("N");
        var instance = new CombatInstance
        {
            CombatId = combatId,
            CombatType = combatType,
            StartTime = DateTime.UtcNow,
            TimeLimit = combatType switch
            {
                0 => 600,   // 副本 10分钟
                1 => 300,   // 竞技场 5分钟
                2 => 900,   // 野外 15分钟
                _ => 600
            },
            Allies = new List<CombatUnit>(),
            Enemies = new List<CombatUnit>(),
            CurrentHeroIndex = 0,
            Stats = new CombatStats(),
            IsAutoBattle = false,
            IsEnded = false,
            LastTick = DateTime.UtcNow,
            PlayerId = playerId
        };

        // 友方 3 英雄
        float allyX = -8f;
        foreach (var hero in allyHeroes)
        {
            instance.Allies.Add(new CombatUnit
            {
                UnitId = (uint)Interlocked.Increment(ref _nextUnitId),
                Name = hero.Name,
                UnitType = 0,
                Hp = hero.MaxHp,
                MaxHp = hero.MaxHp,
                Attack = hero.Attack,
                Defense = hero.Defense,
                AttackType = hero.AttackType,
                DefenseType = hero.DefenseType,
                CritRate = hero.CritRate,
                CritDamage = hero.CritDamage,
                DodgeRate = hero.DodgeRate,
                Level = hero.Level,
                X = allyX,
                Y = 0,
                Z = 0,
                Facing = 0,
                Buffs = new List<BuffInstance>(),
                CurrentSkillId = 0,
                SkillCdRemain = 0
            });
            allyX += 2f;
        }

        // 敌方
        float enemyX = 8f;
        foreach (var enemy in enemies)
        {
            instance.Enemies.Add(new CombatUnit
            {
                UnitId = (uint)Interlocked.Increment(ref _nextUnitId),
                Name = enemy.Name,
                UnitType = enemy.UnitType,
                Hp = enemy.MaxHp,
                MaxHp = enemy.MaxHp,
                Attack = enemy.Attack,
                Defense = enemy.Defense,
                AttackType = enemy.AttackType,
                DefenseType = enemy.DefenseType,
                CritRate = enemy.CritRate,
                CritDamage = enemy.CritDamage,
                DodgeRate = enemy.DodgeRate,
                Level = enemy.Level,
                X = enemyX,
                Y = 0,
                Z = 0,
                Facing = MathF.PI,
                Buffs = new List<BuffInstance>(),
                CurrentSkillId = 0,
                SkillCdRemain = 0
            });
            enemyX += 2f;
        }

        _battles[combatId] = instance;
        Logger.Info("CombatEngine", $"战斗创建 {combatId} 类型={combatType} 敌方={enemies.Count}个");
        return instance;
    }

    /// <summary>获取战斗实例</summary>
    public static CombatInstance? GetCombat(string combatId)
    {
        _battles.TryGetValue(combatId, out var instance);
        return instance;
    }

    /// <summary>获取玩家的战斗</summary>
    public static CombatInstance? GetPlayerCombat(long playerId)
    {
        return _battles.Values.FirstOrDefault(b => b.PlayerId == playerId && !b.IsEnded);
    }

    /// <summary>移除战斗</summary>
    public static bool RemoveCombat(string combatId)
    {
        return _battles.TryRemove(combatId, out _);
    }

    // ========== Buff 系统 ==========

    /// <summary>添加 Buff</summary>
    public static BuffInstance AddBuff(CombatUnit target, int buffId, string name,
        float duration, int stackCount, int buffType, int tickDamage = 0, int tickHeal = 0)
    {
        // 检查是否已存在同ID Buff -> 刷新时间并叠加层数
        var existing = target.Buffs.Find(b => b.BuffId == buffId);
        if (existing != null)
        {
            existing.RemainTime = Math.Max(existing.RemainTime, duration);
            existing.StackCount = Math.Min(existing.StackCount + stackCount, 10); // 最大10层
            Logger.Info("CombatEngine", $"Buff刷新 {name} 层数={existing.StackCount}");
            return existing;
        }

        var buff = new BuffInstance
        {
            BuffId = buffId,
            Name = name,
            Duration = duration,
            RemainTime = duration,
            StackCount = stackCount,
            BuffType = buffType,
            TickDamage = tickDamage,
            TickHeal = tickHeal
        };
        target.Buffs.Add(buff);
        Logger.Info("CombatEngine", $"Buff添加 {name} 类型={buffType} 持续={duration}s");
        return buff;
    }

    /// <summary>移除 Buff</summary>
    public static bool RemoveBuff(CombatUnit target, int buffId)
    {
        var removed = target.Buffs.RemoveAll(b => b.BuffId == buffId);
        if (removed > 0)
            Logger.Info("CombatEngine", $"Buff移除 ID={buffId}");
        return removed > 0;
    }

    /// <summary>清除所有 Buff</summary>
    public static void ClearBuffs(CombatUnit target)
    {
        target.Buffs.Clear();
    }

    // ========== 英雄切换 ==========

    private static readonly Dictionary<long, DateTime> _lastSwitchTime = new();

    /// <summary>切换英雄 (3秒CD)</summary>
    public static (bool success, string msg) SwitchHero(long playerId, string combatId, int targetIndex)
    {
        var combat = GetCombat(combatId);
        if (combat == null || combat.IsEnded)
            return (false, "战斗不存在或已结束");

        if (targetIndex < 0 || targetIndex >= combat.Allies.Count)
            return (false, "英雄索引无效");

        if (combat.Allies[targetIndex].IsDead)
            return (false, "目标英雄已阵亡");

        // 检查CD
        if (_lastSwitchTime.TryGetValue(playerId, out var lastTime))
        {
            var elapsed = (DateTime.UtcNow - lastTime).TotalSeconds;
            if (elapsed < 3.0)
                return (false, $"切换CD中，剩余{3 - elapsed:F1}秒");
        }

        // 记录新英雄位置 = 原英雄位置
        var oldHero = combat.CurrentHero;
        var newHero = combat.Allies[targetIndex];
        (newHero.X, newHero.Y, newHero.Z) = (oldHero.X, oldHero.Y, oldHero.Z);

        combat.CurrentHeroIndex = targetIndex;
        _lastSwitchTime[playerId] = DateTime.UtcNow;

        Logger.Info("CombatEngine", $"英雄切换: {oldHero.Name} -> {newHero.Name}");
        return (true, $"切换到{newHero.Name}");
    }

    // ========== 移动 ==========

    /// <summary>移动单位</summary>
    public static void MoveUnit(string combatId, uint unitId, float x, float y, float z, float facing)
    {
        var combat = GetCombat(combatId);
        if (combat == null) return;

        var unit = combat.Allies.Find(u => u.UnitId == unitId);
        if (unit == null) return;

        unit.X = x;
        unit.Y = y;
        unit.Z = z;
        unit.Facing = facing;

        // 广播位置更新 (日志模拟)
        Logger.Info("CombatEngine", $"单位移动 UnitId={unitId} 位置=({x:F1},{y:F1},{z:F1})");
    }

    // ========== 闪避 ==========

    /// <summary>判定是否闪避</summary>
    public static bool CheckDodge(CombatUnit target)
    {
        return _rng.NextDouble() < target.DodgeRate;
    }

    // ========== 竞技 / 自动战斗 AI ==========

    /// <summary>AI 自动战斗 (简单行为树)</summary>
    public static void RunAutoAI(CombatInstance combat)
    {
        if (combat.IsEnded) return;

        // 选择最近敌人
        var hero = combat.CurrentHero;
        var target = FindNearestEnemy(combat, hero);

        if (target == null) return;

        // 血量<30%切换T或奶 (index 1 = T, index 2 = 奶)
        var heroHpRatio = (float)hero.Hp / hero.MaxHp;
        if (heroHpRatio < 0.3f)
        {
            // 尝试切换到T(index 1)或奶(index 2)
            for (int i = 1; i < combat.Allies.Count; i++)
            {
                if (!combat.Allies[i].IsDead && i != combat.CurrentHeroIndex)
                {
                    combat.CurrentHeroIndex = i;
                    Logger.Info("AI", $"血量不足30%，切换到 {combat.Allies[i].Name}");
                    break;
                }
            }
        }

        // 如果技能CD好了，使用技能
        hero = combat.CurrentHero;
        if (hero.SkillCdRemain <= 0 && hero.CurrentSkillId != 0)
        {
            SkillManager.CastSkill(combat, hero.UnitId, hero.CurrentSkillId, target.UnitId);
        }
    }

    /// <summary>查找最近敌人</summary>
    public static CombatUnit? FindNearestEnemy(CombatInstance combat, CombatUnit unit)
    {
        CombatUnit? nearest = null;
        float minDist = float.MaxValue;
        foreach (var enemy in combat.Enemies)
        {
            if (enemy.IsDead) continue;
            var dist = unit.DistanceToSqr(enemy);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    // ========== 战斗结束判定 ==========

    /// <summary>检查并结束战斗</summary>
    public static CombatEndResult? CheckBattleEnd(CombatInstance combat)
    {
        if (combat.IsEnded) return null;

        // 一方全灭
        if (combat.IsAllAlliesDead)
        {
            combat.IsEnded = true;
            combat.Stats.Rating = CalculateRating(combat.Stats, false);
            RemoveCombat(combat.CombatId);
            Logger.Info("CombatEngine", $"战斗结束 {combat.CombatId} - 失败");
            return new CombatEndResult
            {
                Win = false,
                Stats = combat.Stats,
                CombatType = combat.CombatType
            };
        }

        if (combat.IsAllEnemiesDead)
        {
            combat.IsEnded = true;
            combat.Stats.Rating = CalculateRating(combat.Stats, true);
            RemoveCombat(combat.CombatId);
            Logger.Info("CombatEngine", $"战斗结束 {combat.CombatId} - 胜利");
            return new CombatEndResult
            {
                Win = true,
                Stats = combat.Stats,
                CombatType = combat.CombatType
            };
        }

        // 时间到
        var elapsed = (DateTime.UtcNow - combat.StartTime).TotalSeconds;
        if (elapsed >= combat.TimeLimit)
        {
            combat.IsEnded = true;
            combat.Stats.Rating = CalculateRating(combat.Stats, false);
            RemoveCombat(combat.CombatId);
            Logger.Info("CombatEngine", $"战斗结束 {combat.CombatId} - 超时");
            return new CombatEndResult
            {
                Win = false,
                Stats = combat.Stats,
                CombatType = combat.CombatType,
                Timeout = true
            };
        }

        return null;
    }

    private static float CalculateRating(CombatStats stats, bool win)
    {
        float baseRating = win ? 60.0f : 20.0f;
        float damageBonus = Math.Min(stats.TotalDamage / 10000f, 20f);
        float comboBonus = stats.MaxCombo * 2f;
        float dodgeBonus = stats.DodgeCount * 5f;
        float killBonus = stats.KillCount * 10f;
        return baseRating + damageBonus + comboBonus + dodgeBonus + killBonus;
    }

    // ========== Tick 定时器 ==========

    private static void TickCallback(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _battles)
            {
                var combat = kvp.Value;
                if (combat.IsEnded) continue;

                var dt = (float)(now - combat.LastTick).TotalSeconds;
                combat.LastTick = now;

                foreach (var unit in combat.Allies.Concat(combat.Enemies))
                {
                    if (unit.IsDead) continue;

                    // Buff 衰减
                    for (int i = unit.Buffs.Count - 1; i >= 0; i--)
                    {
                        var buff = unit.Buffs[i];
                        buff.RemainTime -= dt;

                        // DoT tick
                        if (buff.BuffType == 2 && buff.TickDamage > 0) // DoT
                        {
                            unit.Hp = Math.Max(0, unit.Hp - buff.TickDamage * buff.StackCount);
                            Logger.Info("CombatEngine", $"DoT Tick {buff.Name} 伤害={buff.TickDamage * buff.StackCount} 剩余HP={unit.Hp}");
                        }

                        // HoT tick
                        if (buff.BuffType == 3 && buff.TickHeal > 0) // HoT
                        {
                            unit.Hp = Math.Min(unit.MaxHp, unit.Hp + buff.TickHeal * buff.StackCount);
                            Logger.Info("CombatEngine", $"HoT Tick {buff.Name} 治疗={buff.TickHeal * buff.StackCount} HP={unit.Hp}");
                        }

                        // 移除过期Buff
                        if (buff.RemainTime <= 0)
                        {
                            unit.Buffs.RemoveAt(i);
                        }
                    }

                    // 技能CD衰减
                    if (unit.SkillCdRemain > 0)
                        unit.SkillCdRemain = Math.Max(0, unit.SkillCdRemain - dt);
                }

                // 自动战斗tick
                if (combat.IsAutoBattle)
                {
                    RunAutoAI(combat);
                }

                // 检查战斗结束
                CheckBattleEnd(combat);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("CombatEngine", $"Tick error: {ex.Message}");
        }
    }

    /// <summary>获取所有活跃战斗数</summary>
    public static int ActiveBattleCount => _battles.Count(b => !b.Value.IsEnded);
}

/// <summary>战斗结束结果</summary>
public class CombatEndResult
{
    public bool Win { get; set; }
    public CombatStats Stats { get; set; } = new();
    public int CombatType { get; set; }
    public bool Timeout { get; set; }
}