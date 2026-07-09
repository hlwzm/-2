#nullable disable
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;
using System.Text.Json;

namespace Jx3.Battle;

public class BattleServer : GameServer
{
    public BattleServer() : base("Battle", GameConfig.BattlePort) { }

    protected override async Task OnStartAsync()
    {
        Logger.Info("Battle", "战斗引擎初始化...");

        GameConfigLoader.Load();

        // ========== 1. 战斗初始化 ==========
        // CSCombatAttack (2016) -> SCCombatStateInit (2005)
        // 用 CSCombatAttack 作为进入战斗的触发 (包含选择的英雄和敌方信息)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSCombatAttack, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<CombatStartReq>(body);
                if (req == null) return ErrJson("参数错误");

                // 检查是否已有战斗
                var existing = CombatEngine.GetPlayerCombat(req.player_id);
                if (existing != null && !existing.IsEnded)
                    return ErrJson("已有进行中的战斗");

                // 构建英雄模板 (3个英雄)
                var heroes = new List<HeroTemplate>
                {
                    new() { Name = "李复", MaxHp = 2000, Attack = 150, Defense = 60, AttackType = 0, DefenseType = 0,
                        CritRate = 0.15f, CritDamage = 0.2f, DodgeRate = 0.05f, Level = 1 },
                    new() { Name = "陈月", MaxHp = 3500, Attack = 80, Defense = 120, AttackType = 0, DefenseType = 0,
                        CritRate = 0.05f, CritDamage = 0.1f, DodgeRate = 0.02f, Level = 1 },
                    new() { Name = "东方宇轩", MaxHp = 1500, Attack = 180, Defense = 40, AttackType = 1, DefenseType = 1,
                        CritRate = 0.2f, CritDamage = 0.3f, DodgeRate = 0.08f, Level = 1 }
                };

                // 构建敌方
                var enemies = new List<EnemyTemplate>
                {
                    new() { Name = req.enemy_name ?? "山贼", UnitType = 1, MaxHp = 800, Attack = 60, Defense = 30,
                        AttackType = 0, DefenseType = 0, CritRate = 0.05f, CritDamage = 0f, DodgeRate = 0.02f, Level = 1 },
                    new() { Name = "山贼头目", UnitType = 1, MaxHp = 1200, Attack = 80, Defense = 40,
                        AttackType = 0, DefenseType = 0, CritRate = 0.08f, CritDamage = 0.1f, DodgeRate = 0.03f, Level = 1 },
                    new() { Name = "Boss", UnitType = 2, MaxHp = 5000, Attack = 120, Defense = 80,
                        AttackType = 0, DefenseType = 0, CritRate = 0.1f, CritDamage = 0.15f, DodgeRate = 0.05f, Level = 3 }
                };

                var combat = CombatEngine.CreateCombat(req.combat_type, req.player_id, heroes, enemies);

                var result = new CombatStateInitResult
                {
                    combat_id = combat.CombatId,
                    combat_type = combat.CombatType,
                    time_limit = combat.TimeLimit,
                    allies = combat.Allies.Select(a => new UnitInfo
                    {
                        unit_id = a.UnitId,
                        name = a.Name,
                        unit_type = a.UnitType,
                        hp = a.Hp,
                        max_hp = a.MaxHp,
                        attack = a.Attack,
                        defense = a.Defense,
                        crit_rate = a.CritRate,
                        x = a.X, y = a.Y, z = a.Z
                    }).ToList(),
                    enemies = combat.Enemies.Select(e => new UnitInfo
                    {
                        unit_id = e.UnitId,
                        name = e.Name,
                        unit_type = e.UnitType,
                        hp = e.Hp,
                        max_hp = e.MaxHp,
                        attack = e.Attack,
                        defense = e.Defense,
                        crit_rate = e.CritRate,
                        x = e.X, y = e.Y, z = e.Z
                    }).ToList(),
                    current_hero_index = combat.CurrentHeroIndex
                };

                Logger.Info("Battle", $"战斗初始化完成: {combat.CombatId}");
                return EncodeResponse((uint)MsgId.SCCombatStateInit, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Battle", $"CSCombatAttack error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // ========== 2. 技能释放 ==========
        // CSCombatCastSkill (2002) -> SCCombatSkillEffect (2013) + SCCombatDamage (2003) + SCCombatHPChange (2004)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSCombatCastSkill, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<CastSkillReq>(body);
                if (req == null) return ErrJson("参数错误");

                var combat = CombatEngine.GetPlayerCombat(req.player_id);
                if (combat == null) return ErrJson("没有进行中的战斗");

                var result = SkillManager.CastSkill(combat, req.caster_unit_id, req.skill_id, req.target_unit_id);

                // 检查战斗结束
                var endResult = CombatEngine.CheckBattleEnd(combat);

                var response = new SkillCastResponse
                {
                    success = result.Success,
                    message = result.Message,
                    skill_id = result.SkillId,
                    skill_name = result.SkillName,
                    caster_unit_id = result.CasterUnitId,
                    damage_results = result.DamageResults.Select(d => new DamageInfo
                    {
                        target_unit_id = d.TargetUnitId,
                        target_name = d.TargetName,
                        damage = d.Damage,
                        is_crit = d.IsCrit,
                        is_dodged = d.IsDodged,
                        is_kill = d.IsKill,
                        remaining_hp = d.RemainingHp
                    }).ToList(),
                    buff_results = result.BuffResults.Select(b => new BuffInfoResult
                    {
                        target_unit_id = b.TargetUnitId,
                        target_name = b.TargetName,
                        buff_id = b.BuffId,
                        buff_name = b.BuffName,
                        stack_count = b.StackCount,
                        duration = b.Duration
                    }).ToList(),
                    new_cooldown = result.NewCooldown,
                    combat_ended = endResult != null,
                    combat_win = endResult?.Win ?? false,
                    stats = endResult != null ? new StatsInfo
                    {
                        total_damage = combat.Stats.TotalDamage,
                        max_combo = combat.Stats.MaxCombo,
                        dodge_count = combat.Stats.DodgeCount,
                        rating = combat.Stats.Rating,
                        kill_count = combat.Stats.KillCount
                    } : null
                };

                return EncodeResponse((uint)MsgId.SCCombatDamage, response);
            }
            catch (Exception ex)
            {
                Logger.Error("Battle", $"CSCombatCastSkill error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // ========== 3. 英雄切换 ==========
        // CSCombatSwitchHero (2007) -> SCCombatHeroSwitchResult (2014)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSCombatSwitchHero, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<SwitchHeroReq>(body);
                if (req == null) return ErrJson("参数错误");

                var combat = CombatEngine.GetPlayerCombat(req.player_id);
                if (combat == null) return ErrJson("没有进行中的战斗");

                var (success, msg) = CombatEngine.SwitchHero(req.player_id, combat.CombatId, req.target_index);

                var result = new SwitchHeroResultResponse
                {
                    success = success,
                    message = msg,
                    current_hero_index = success ? combat.CurrentHeroIndex : -1,
                    current_hero_name = success ? combat.CurrentHero.Name : ""
                };

                return EncodeResponse((uint)MsgId.SCCombatHeroSwitchResult, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Battle", $"CSCombatSwitchHero error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // ========== 4. 移动 ==========
        // CSCombatMove (2001) -> SCCombatUnitUpdate (2015)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSCombatMove, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<MoveReq>(body);
                if (req == null) return ErrJson("参数错误");

                var combat = CombatEngine.GetPlayerCombat(req.player_id);
                if (combat == null) return ErrJson("没有进行中的战斗");

                CombatEngine.MoveUnit(combat.CombatId, req.unit_id, req.x, req.y, req.z, req.facing);

                var result = new UnitUpdateResult
                {
                    unit_id = req.unit_id,
                    x = req.x,
                    y = req.y,
                    z = req.z,
                    facing = req.facing,
                    allies = combat.Allies.Select(a => new UnitPosInfo
                    {
                        unit_id = a.UnitId, x = a.X, y = a.Y, z = a.Z, facing = a.Facing
                    }).ToList(),
                    enemies = combat.Enemies.Where(e => !e.IsDead).Select(e => new UnitPosInfo
                    {
                        unit_id = e.UnitId, x = e.X, y = e.Y, z = e.Z, facing = e.Facing
                    }).ToList()
                };

                return EncodeResponse((uint)MsgId.SCCombatUnitUpdate, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Battle", $"CSCombatMove error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // ========== 5. 闪避 ==========
        // CSCombatDodge (2008) -> SCCombatUnitUpdate (2015)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSCombatDodge, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<DodgeReq>(body);
                if (req == null) return ErrJson("参数错误");

                var combat = CombatEngine.GetPlayerCombat(req.player_id);
                if (combat == null) return ErrJson("没有进行中的战斗");

                var unit = combat.Allies.Find(u => u.UnitId == req.unit_id);
                if (unit == null) return ErrJson("单位不存在");

                // 闪避翻滚 - 快速位移
                CombatEngine.MoveUnit(combat.CombatId, req.unit_id,
                    unit.X + req.dir_x * 3f, unit.Y, unit.Z + req.dir_z * 3f, unit.Facing);
                combat.Stats.DodgeCount++;

                var result = new UnitUpdateResult
                {
                    unit_id = req.unit_id,
                    x = unit.X, y = unit.Y, z = unit.Z, facing = unit.Facing,
                    allies = combat.Allies.Select(a => new UnitPosInfo
                    {
                        unit_id = a.UnitId, x = a.X, y = a.Y, z = a.Z, facing = a.Facing
                    }).ToList(),
                    enemies = combat.Enemies.Where(e => !e.IsDead).Select(e => new UnitPosInfo
                    {
                        unit_id = e.UnitId, x = e.X, y = e.Y, z = e.Z, facing = e.Facing
                    }).ToList()
                };

                return EncodeResponse((uint)MsgId.SCCombatUnitUpdate, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Battle", $"CSCombatDodge error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // ========== 6. 自动战斗开关 ==========
        // CSCombatAutoOn (2009) / CSCombatAutoOff (2010)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSCombatAutoOn, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<PlayerReq>(body);
                if (req == null) return ErrJson("参数错误");

                var combat = CombatEngine.GetPlayerCombat(req.player_id);
                if (combat == null) return ErrJson("没有进行中的战斗");

                combat.IsAutoBattle = true;
                Logger.Info("Battle", $"玩家{req.player_id}开启自动战斗");

                return EncodeResponse((uint)MsgId.SCCombatStateInit, new { success = true, auto = true });
            }
            catch (Exception ex)
            {
                Logger.Error("Battle", $"CSCombatAutoOn error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        ServiceRegistry.RegisterHandler((uint)MsgId.CSCombatAutoOff, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<PlayerReq>(body);
                if (req == null) return ErrJson("参数错误");

                var combat = CombatEngine.GetPlayerCombat(req.player_id);
                if (combat == null) return ErrJson("没有进行中的战斗");

                combat.IsAutoBattle = false;
                Logger.Info("Battle", $"玩家{req.player_id}关闭自动战斗");

                return EncodeResponse((uint)MsgId.SCCombatStateInit, new { success = true, auto = false });
            }
            catch (Exception ex)
            {
                Logger.Error("Battle", $"CSCombatAutoOff error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        Logger.Info("Battle", $"战斗引擎就绪, 已注册 {7} 个Handler (含自动战斗开关)");
        await Task.CompletedTask;
    }

    // ========== 工具方法 ==========

    private static byte[]? EncodeResponse(uint msgId, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        });
        var packet = new Jx3.Common.Network.MessagePacket
        {
            MsgId = msgId,
            Body = System.Text.Encoding.UTF8.GetBytes(json)
        };
        return packet.Encode();
    }

    private static byte[]? ErrJson(string msg)
    {
        var json = JsonSerializer.Serialize(new { code = 1, msg });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}

// ========== 请求 DTO ==========

internal class PlayerReq
{
    public long player_id { get; set; }
}

internal class CombatStartReq
{
    public long player_id { get; set; }
    public int combat_type { get; set; } = 0;
    public string? enemy_name { get; set; }
}

internal class CastSkillReq
{
    public long player_id { get; set; }
    public uint caster_unit_id { get; set; }
    public uint skill_id { get; set; }
    public uint target_unit_id { get; set; }
}

internal class SwitchHeroReq
{
    public long player_id { get; set; }
    public int target_index { get; set; }
}

internal class MoveReq
{
    public long player_id { get; set; }
    public uint unit_id { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public float facing { get; set; }
}

internal class DodgeReq
{
    public long player_id { get; set; }
    public uint unit_id { get; set; }
    public float dir_x { get; set; }
    public float dir_z { get; set; }
}

// ========== 响应 DTO ==========

internal class CombatStateInitResult
{
    public string combat_id { get; set; } = "";
    public int combat_type { get; set; }
    public int time_limit { get; set; }
    public List<UnitInfo> allies { get; set; } = new();
    public List<UnitInfo> enemies { get; set; } = new();
    public int current_hero_index { get; set; }
    public bool is_auto { get; set; }
    public int elapsed { get; set; }
}

internal class UnitInfo
{
    public uint unit_id { get; set; }
    public string name { get; set; } = "";
    public int unit_type { get; set; }
    public int hp { get; set; }
    public int max_hp { get; set; }
    public int attack { get; set; }
    public int defense { get; set; }
    public float crit_rate { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

internal class SkillCastResponse
{
    public bool success { get; set; }
    public string message { get; set; } = "";
    public uint skill_id { get; set; }
    public string skill_name { get; set; } = "";
    public uint caster_unit_id { get; set; }
    public List<DamageInfo> damage_results { get; set; } = new();
    public List<BuffInfoResult> buff_results { get; set; } = new();
    public float new_cooldown { get; set; }
    public bool combat_ended { get; set; }
    public bool combat_win { get; set; }
    public StatsInfo? stats { get; set; }
}

internal class DamageInfo
{
    public uint target_unit_id { get; set; }
    public string target_name { get; set; } = "";
    public int damage { get; set; }
    public bool is_crit { get; set; }
    public bool is_dodged { get; set; }
    public bool is_kill { get; set; }
    public int remaining_hp { get; set; }
}

internal class BuffInfoResult
{
    public uint target_unit_id { get; set; }
    public string target_name { get; set; } = "";
    public int buff_id { get; set; }
    public string buff_name { get; set; } = "";
    public int stack_count { get; set; }
    public float duration { get; set; }
}

internal class StatsInfo
{
    public long total_damage { get; set; }
    public int max_combo { get; set; }
    public int dodge_count { get; set; }
    public float rating { get; set; }
    public int kill_count { get; set; }
}

internal class SwitchHeroResultResponse
{
    public bool success { get; set; }
    public string message { get; set; } = "";
    public int current_hero_index { get; set; }
    public string current_hero_name { get; set; } = "";
}

internal class UnitUpdateResult
{
    public uint unit_id { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public float facing { get; set; }
    public List<UnitPosInfo> allies { get; set; } = new();
    public List<UnitPosInfo> enemies { get; set; } = new();
}

internal class UnitPosInfo
{
    public uint unit_id { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public float facing { get; set; }
}

// ========== 入口 ==========

public class Program
{
    public static async Task Main()
    {
        GameConfigLoader.Load();
        await new BattleServer().StartAsync();
    }
}