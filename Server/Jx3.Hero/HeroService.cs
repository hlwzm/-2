using Jx3.Common.Database;
using Jx3.Common.Utils;
using System.Text.Json;

namespace Jx3.Hero;

/// <summary>英雄业务服务</summary>
public class HeroService
{
    private readonly DbHelper _db;
    private readonly RedisHelper _redis;

    public HeroService(DbHelper db, RedisHelper redis)
    {
        _db = db;
        _redis = redis;
    }

    // ==================== 英雄列表 ====================
    public async Task<string> GetHeroListAsync(long playerId)
    {
        var rows = await _db.QueryAsync<HeroRow>(
            "SELECT hero_uid, player_id, template_id, level, star, exp, breakthrough, skill_levels, favorability, obtain_time " +
            "FROM hero WHERE player_id = @pid ORDER BY template_id", new { pid = playerId });

        var team = await GetTeamAsync(playerId);
        var teamSet = new HashSet<ulong>(team);

        var heroes = new List<object>();
        foreach (var r in rows)
        {
            var cfg = HeroTemplateConfig.All.GetValueOrDefault((int)r.template_id);
            if (cfg == null) continue;

            var stats = CalcStats(cfg, r.level, r.star);
            heroes.Add(new
            {
                hero_uid = r.hero_uid,
                template_id = r.template_id,
                name = cfg.Name,
                level = r.level,
                star = r.star,
                breakthrough = r.breakthrough,
                quality = cfg.Quality,
                role_type = cfg.RoleType,
                attack_type = cfg.AttackType,
                exp = r.exp,
                favorability = r.favorability,
                in_team = teamSet.Contains(r.hero_uid),
                stats = new
                {
                    max_hp = (int)stats.hp,
                    attack = (int)stats.atk,
                    defense = (int)stats.def,
                    crit_rate = stats.critRate,
                    crit_damage = stats.critDamage,
                    dodge_rate = stats.dodgeRate,
                    speed = stats.speed,
                    heal_power = (int)stats.healPower,
                }
            });
        }

        return JsonSerializer.Serialize(new { code = 0, heroes });
    }

    // ==================== 英雄升级 ====================
    public async Task<string> LevelUpAsync(ulong heroUid, long playerId)
    {
        // 查英雄
        var hero = await _db.QueryFirstOrDefaultAsync<HeroRow>(
            "SELECT hero_uid, player_id, template_id, level, star, exp, breakthrough FROM hero WHERE hero_uid = @uid AND player_id = @pid",
            new { uid = heroUid, pid = playerId });

        if (hero == null)
            return Err("英雄不存在");

        if (hero.level >= HeroTemplateConfig.MaxLevel)
            return Err("已达最大等级");

        // 查玩家等级
        var player = await _db.QueryFirstOrDefaultAsync<PlayerRow>(
            "SELECT level, gold, exp FROM player WHERE player_id = @pid", new { pid = playerId });

        if (player == null)
            return Err("玩家不存在");

        if (hero.level >= player.level)
            return Err("英雄等级不能超过玩家等级");

        var goldCost = HeroTemplateConfig.LevelUpGoldCost(hero.level);
        var expCost = HeroTemplateConfig.LevelUpExpCost(hero.level);

        if (player.gold < goldCost)
            return Err("金币不足");

        if (player.exp < expCost)
            return Err("经验值不足");

        // 扣资源
        await _db.ExecuteAsync(
            "UPDATE player SET gold = gold - @gold, exp = exp - @exp WHERE player_id = @pid",
            new { gold = goldCost, exp = expCost, pid = playerId });

        // 升级
        int newLevel = hero.level + 1;
        await _db.ExecuteAsync(
            "UPDATE hero SET level = @level WHERE hero_uid = @uid",
            new { level = newLevel, uid = heroUid });

        // 计算属性变化
        var cfg = HeroTemplateConfig.All.GetValueOrDefault((int)hero.template_id);
        if (cfg == null) return Err("英雄配置不存在");

        var oldStats = CalcStats(cfg, hero.level, hero.star);
        var newStats = CalcStats(cfg, newLevel, hero.star);

        var statsChange = new
        {
            max_hp = (int)(newStats.hp - oldStats.hp),
            attack = (int)(newStats.atk - oldStats.atk),
            defense = (int)(newStats.def - oldStats.def),
            crit_rate = newStats.critRate - oldStats.critRate,
            crit_damage = newStats.critDamage - oldStats.critDamage,
            dodge_rate = newStats.dodgeRate - oldStats.dodgeRate,
            speed = newStats.speed - oldStats.speed,
            heal_power = (int)(newStats.healPower - oldStats.healPower),
        };

        Logger.Info("Hero", $"Player {playerId} hero {heroUid} level up {hero.level} -> {newLevel}");
        return JsonSerializer.Serialize(new { code = 0, hero_uid = heroUid, new_level = newLevel, stats_change = statsChange });
    }

    // ==================== 英雄升星 ====================
    public async Task<string> StarUpAsync(ulong heroUid, long playerId)
    {
        var hero = await _db.QueryFirstOrDefaultAsync<HeroRow>(
            "SELECT hero_uid, player_id, template_id, level, star, exp, breakthrough FROM hero WHERE hero_uid = @uid AND player_id = @pid",
            new { uid = heroUid, pid = playerId });

        if (hero == null)
            return Err("英雄不存在");

        if (hero.star >= HeroTemplateConfig.MaxStar)
            return Err("已达最大星级");

        var needFrag = HeroTemplateConfig.StarUpFragmentCost.GetValueOrDefault(hero.star);
        if (needFrag == 0)
            return Err("无法升星");

        // 查碎片数量（Redis）
        var fragKey = $"hero:frag:{playerId}:{hero.template_id}";
        var fragStr = await _redis.GetAsync(fragKey);
        int fragCount = int.TryParse(fragStr, out var f) ? f : 0;

        if (fragCount < needFrag)
            return Err($"碎片不足，需要 {needFrag} 个，当前 {fragCount} 个");

        // 扣碎片
        await _redis.SetAsync(fragKey, (fragCount - needFrag).ToString());

        // 升星
        int newStar = hero.star + 1;
        await _db.ExecuteAsync(
            "UPDATE hero SET star = @star WHERE hero_uid = @uid",
            new { star = newStar, uid = heroUid });

        // 计算属性变化
        var cfg = HeroTemplateConfig.All.GetValueOrDefault((int)hero.template_id);
        if (cfg == null) return Err("英雄配置不存在");

        var oldStats = CalcStats(cfg, hero.level, hero.star);
        var newStats = CalcStats(cfg, hero.level, newStar);

        var statsChange = new
        {
            max_hp = (int)(newStats.hp - oldStats.hp),
            attack = (int)(newStats.atk - oldStats.atk),
            defense = (int)(newStats.def - oldStats.def),
            crit_rate = newStats.critRate - oldStats.critRate,
            crit_damage = newStats.critDamage - oldStats.critDamage,
            dodge_rate = newStats.dodgeRate - oldStats.dodgeRate,
            speed = newStats.speed - oldStats.speed,
            heal_power = (int)(newStats.healPower - oldStats.healPower),
        };

        Logger.Info("Hero", $"Player {playerId} hero {heroUid} star up {hero.star} -> {newStar}");
        return JsonSerializer.Serialize(new { code = 0, hero_uid = heroUid, new_star = newStar, stats_change = statsChange });
    }

    // ==================== 编队设置 ====================
    public async Task<string> SetTeamAsync(long playerId, List<ulong> heroIds)
    {
        if (heroIds.Count < 1 || heroIds.Count > 4)
            return Err("编队人数需在1-4之间");

        if (heroIds.Distinct().Count() != heroIds.Count)
            return Err("编队英雄不能重复");

        // 验证英雄属于该玩家
        var existing = (await _db.QueryAsync<ulong>(
            "SELECT hero_uid FROM hero WHERE hero_uid IN @ids AND player_id = @pid",
            new { ids = heroIds, pid = playerId })).ToHashSet();

        foreach (var id in heroIds)
        {
            if (!existing.Contains(id))
                return Err($"英雄 {id} 不属于该玩家");
        }

        // 存入Redis
        var teamKey = $"hero:team:{playerId}";
        var json = JsonSerializer.Serialize(heroIds);
        await _redis.SetAsync(teamKey, json);

        Logger.Info("Hero", $"Player {playerId} team set: [{string.Join(",", heroIds)}]");
        return JsonSerializer.Serialize(new { code = 0, hero_ids = heroIds });
    }

    // ==================== 编队读取 ====================
    public async Task<List<ulong>> GetTeamAsync(long playerId)
    {
        var teamKey = $"hero:team:{playerId}";
        var json = await _redis.GetAsync(teamKey);
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<ulong>>(json) ?? new(); }
        catch { return new(); }
    }

    // ==================== 属性计算 ====================
    private static (double hp, double atk, double def, double healPower, double critRate, double critDamage, double dodgeRate, int speed) CalcStats(HeroTemplate cfg, int level, int star)
    {
        // 等级加成: 攻防+5%/级, 生命+8%/级
        double levelFactorAtkDef = 1.0 + (level - 1) * 0.05;
        double levelFactorHp = 1.0 + (level - 1) * 0.08;

        // 星级加成: 全属性+10%/星 (在1星基础上累加)
        double starFactor = 1.0 + (star - 1) * 0.10;

        double hp = cfg.BaseHp * levelFactorHp * starFactor;
        double atk = cfg.BaseAtk * levelFactorAtkDef * starFactor;
        double def = cfg.BaseDef * levelFactorAtkDef * starFactor;
        double healPower = cfg.BaseHealPower * levelFactorHp * starFactor;

        // 非基础属性只受星级加成
        double critRate = cfg.BaseCritRate * starFactor;
        double critDamage = cfg.BaseCritDamage * starFactor;
        double dodgeRate = cfg.BaseDodgeRate * starFactor;
        int speed = (int)(cfg.BaseSpeed * starFactor);

        return (hp, atk, def, healPower, critRate, critDamage, dodgeRate, speed);
    }

    // ==================== 工具方法 ====================
    private static string Err(string msg) => JsonSerializer.Serialize(new { code = 1, msg });
}

// ==================== 数据库行映射 ====================
internal class HeroRow
{
    public ulong hero_uid { get; set; }
    public long player_id { get; set; }
    public uint template_id { get; set; }
    public int level { get; set; }
    public int star { get; set; }
    public long exp { get; set; }
    public int breakthrough { get; set; }
    public string? skill_levels { get; set; }
    public int favorability { get; set; }
    public DateTime obtain_time { get; set; }
}

internal class PlayerRow
{
    public int level { get; set; }
    public long gold { get; set; }
    public long exp { get; set; }
}
