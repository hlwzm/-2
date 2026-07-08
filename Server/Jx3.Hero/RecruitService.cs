using Jx3.Common.Database;
using Jx3.Common.Utils;
using System.Text.Json;

namespace Jx3.Hero;

/// <summary>招募抽卡服务</summary>
public class RecruitService
{
    private readonly DbHelper _db;
    private readonly RedisHelper _redis;
    private readonly Random _rng = new();

    private const double Rate5 = 0.006;   // 0.6%
    private const double Rate4 = 0.05;    // 5%
    private const double RateOther = 0.944; // 94.4%

    private static readonly int[] Quality5Heroes;
    private static readonly int[] Quality4Heroes;

    static RecruitService()
    {
        Quality5Heroes = HeroTemplateConfig.All.Values.Where(h => h.Quality == 5).Select(h => h.TemplateId).ToArray();
        Quality4Heroes = HeroTemplateConfig.All.Values.Where(h => h.Quality == 4).Select(h => h.TemplateId).ToArray();
    }

    public RecruitService(DbHelper db, RedisHelper redis)
    {
        _db = db;
        _redis = redis;
    }

    // ==================== 招募抽卡 ====================
    public async Task<string> DrawAsync(long playerId, int poolId, int count)
    {
        if (count != 1 && count != 10)
            return Err("抽卡次数只能为1或10");

        var pool = RecruitPoolConfig.Pools.FirstOrDefault(p => p.PoolId == poolId);
        if (pool == null)
            return Err("卡池不存在");

        // 扣费
        var totalCost = pool.CostAmount * count;
        if (pool.CostType == 1) // 通宝
        {
            var affected = await _db.ExecuteAsync(
                "UPDATE player SET tongbao = tongbao - @cost WHERE player_id = @pid AND tongbao >= @cost",
                new { cost = totalCost, pid = playerId });
            if (affected == 0)
                return Err("通宝不足");
        }
        else
        {
            return Err("不支持的消耗类型");
        }

        var items = new List<DrawItem>();

        for (int i = 0; i < count; i++)
        {
            var item = await DoSingleDrawAsync(playerId, pool);
            items.Add(item);
        }

        // 新手池首次十连保底：检查是否出货了5星
        if (pool.IsNovice && count == 10)
        {
            bool has5 = items.Any(it => it.Quality == 5);
            if (!has5)
            {
                // 替换最后一个为随机5星
                var lastIdx = items.Count - 1;
                items[lastIdx] = await Force5StarAsync(playerId, pool);
            }
        }

        // 读取当前保底计数
        var pityKey = $"hero:pity:{playerId}:{poolId}";
        var pityStr = await _redis.GetAsync(pityKey);
        int pityCount = int.TryParse(pityStr, out var p) ? p : 0;

        Logger.Info("Recruit", $"Player {playerId} drew {count} from pool {poolId}, pity={pityCount}");
        return JsonSerializer.Serialize(new { code = 0, items, pity_count = pityCount });
    }

    private async Task<DrawItem> DoSingleDrawAsync(long playerId, RecruitPoolInfo pool)
    {
        var pityKey = $"hero:pity:{playerId}:{pool.PoolId}";
        var pityStr = await _redis.GetAsync(pityKey);
        int pityCount = int.TryParse(pityStr, out var p) ? p : 0;

        // 检查保底
        if (pityCount >= pool.MaxPity - 1)
        {
            // 重置保底计数并出货
            await _redis.SetAsync(pityKey, "0");
            return await GenerateHeroItemAsync(playerId, 5, pool);
        }

        // 概率判定
        double roll = _rng.NextDouble();
        int quality;

        if (roll < Rate5)
            quality = 5;
        else if (roll < Rate5 + Rate4)
            quality = 4;
        else
            quality = 3;

        // 递增保底计数
        await _redis.SetAsync(pityKey, (pityCount + 1).ToString());

        if (quality >= 4)
        {
            // 重置保底（出货了）
            await _redis.SetAsync(pityKey, "0");
            return await GenerateHeroItemAsync(playerId, quality, pool);
        }

        // 材料：给3个该卡池UP英雄或其他英雄碎片
        return await GenerateMaterialItemAsync(playerId, pool);
    }

    /// <summary>强制出5星（新手池保底）</summary>
    private async Task<DrawItem> Force5StarAsync(long playerId, RecruitPoolInfo pool)
    {
        var pityKey = $"hero:pity:{playerId}:{pool.PoolId}";
        await _redis.SetAsync(pityKey, "0");
        return await GenerateHeroItemAsync(playerId, 5, pool);
    }

    /// <summary>生成英雄物品</summary>
    private async Task<DrawItem> GenerateHeroItemAsync(long playerId, int quality, RecruitPoolInfo pool)
    {
        int heroId;

        if (quality == 5 && pool.UpHeroId > 0)
        {
            // UP池：50%概率给UP英雄
            heroId = _rng.NextDouble() < 0.5 ? pool.UpHeroId : Quality5Heroes[_rng.Next(Quality5Heroes.Length)];
        }
        else if (quality == 5)
        {
            heroId = Quality5Heroes[_rng.Next(Quality5Heroes.Length)];
        }
        else
        {
            heroId = Quality4Heroes[_rng.Next(Quality4Heroes.Length)];
        }

        // 检查是否已有该英雄
        var existing = await _db.QueryFirstOrDefaultAsync<ulong?>(
            "SELECT hero_uid FROM hero WHERE player_id = @pid AND template_id = @tid LIMIT 1",
            new { pid = playerId, tid = heroId });

        bool isNew = existing == null;

        if (isNew)
        {
            // 新英雄插入hero表
            await _db.ExecuteAsync(
                "INSERT INTO hero (player_id, template_id, level, star, exp, breakthrough, skill_levels, favorability) " +
                "VALUES (@pid, @tid, 1, 1, 0, 0, '{}', 0)",
                new { pid = playerId, tid = heroId });

            Logger.Info("Recruit", $"Player {playerId} got NEW hero {heroId} (q{quality})");
        }
        else
        {
            // 重复英雄 -> 转碎片
            var fragKey = $"hero:frag:{playerId}:{heroId}";
            var fragStr = await _redis.GetAsync(fragKey);
            int curFrags = int.TryParse(fragStr, out var f) ? f : 0;
            await _redis.SetAsync(fragKey, (curFrags + HeroTemplateConfig.DuplicateHeroFragments).ToString());

            Logger.Info("Recruit", $"Player {playerId} got DUPLICATE hero {heroId} -> +{HeroTemplateConfig.DuplicateHeroFragments} frags");
        }

        var cfg = HeroTemplateConfig.All.GetValueOrDefault(heroId);
        return new DrawItem
        {
            HeroId = heroId,
            Quality = quality,
            FragmentCount = isNew ? 0 : HeroTemplateConfig.DuplicateHeroFragments,
            IsNew = isNew,
            Name = cfg?.Name ?? ""
        };
    }

    /// <summary>生成材料掉落</summary>
    private async Task<DrawItem> GenerateMaterialItemAsync(long playerId, RecruitPoolInfo pool)
    {
        // 随机给某个英雄的碎片（3-5个）
        int fragCount = _rng.Next(3, 6);
        int targetHero;

        if (pool.UpHeroId > 0 && _rng.NextDouble() < 0.3)
            targetHero = pool.UpHeroId;
        else
        {
            var allHeroes = HeroTemplateConfig.All.Keys.ToArray();
            targetHero = allHeroes[_rng.Next(allHeroes.Length)];
        }

        var fragKey = $"hero:frag:{playerId}:{targetHero}";
        var fragStr = await _redis.GetAsync(fragKey);
        int curFrags = int.TryParse(fragStr, out var f) ? f : 0;
        await _redis.SetAsync(fragKey, (curFrags + fragCount).ToString());

        var cfg = HeroTemplateConfig.All.GetValueOrDefault(targetHero);
        return new DrawItem
        {
            HeroId = 0,
            Quality = 3,
            FragmentCount = fragCount,
            FragmentHeroId = targetHero,
            IsNew = false,
            Name = $"{cfg?.Name ?? "?"}碎片"
        };
    }

    // ==================== 卡池列表 ====================
    public string GetPoolList()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pools = RecruitPoolConfig.Pools
            .Where(p => p.EndTime == 0 || p.EndTime > now)
            .Select(p => new
            {
                pool_id = p.PoolId,
                name = p.Name,
                description = p.Description,
                cost_type = p.CostType,
                cost_amount = p.CostAmount,
                pity_count = 0, // 客户端自行查询
                max_pity = p.MaxPity,
                up_hero_id = p.UpHeroId,
                end_time = p.EndTime,
            })
            .ToList();

        return JsonSerializer.Serialize(new { code = 0, pools });
    }

    private static string Err(string msg) => JsonSerializer.Serialize(new { code = 1, msg });
}

/// <summary>抽卡结果物品</summary>
public class DrawItem
{
    public int HeroId { get; set; }
    public int Quality { get; set; }
    public int FragmentCount { get; set; }
    public int FragmentHeroId { get; set; }
    public bool IsNew { get; set; }
    public string Name { get; set; } = "";
}
