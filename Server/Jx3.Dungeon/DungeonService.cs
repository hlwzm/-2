using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Database;
using Jx3.Common.Protocol;
using Jx3.Common.Utils;

namespace Jx3.Dungeon;

public class BossState
{
    public int BossIndex;         // 0-2普通 3终极
    public string Name = "";
    public int MaxHp;
    public int CurrentHp;
    public int Phase = 1;          // 1/2/3
    public bool IsDead;
    public int KillTimeSec;        // 击杀耗时
    public bool IsUltimate => BossIndex == 3;
}

public class DungeonInstance
{
    public ulong ProgressId;
    public int DungeonId;
    public int Difficulty = 1;     // 1普通 2英雄 3挑战
    public ulong TeamId;
    public List<ulong> PlayerIds = new();
    public List<DungeonPlayer> Players = new();
    public DateTime StartTime;
    public int ElapsedSeconds;
    public BossState[] Bosses = new BossState[4];
    public bool UltimateUnlocked;
    public bool UltimateSpawned;
    public int Status = 1;         // 1进行中 2完成 3失败
}

public class DungeonPlayer
{
    public ulong PlayerId;
    public string Name = "";
    public int ReviveCount;
    public bool IsAlive = true;
}

public static class DungeonService
{
    private static readonly Dictionary<ulong, DungeonInstance> _instances = new();
    private static ulong _nextProgressId = 1;

    // ---- 副本配置 ----
    public static readonly Dictionary<int, (string Name, int MinLv, int MinP, int MaxP, int TimeLimit, string Desc)> DungeonConfig = new()
    {
        { 1, ("风雨稻香村", 20, 4, 5, 480, "稻香村遭匪患，群侠前往救援") },   // 8min
        { 2, ("天子峰", 35, 5, 5, 600, "天子峰上邪影重重") },                  // 10min
        { 3, ("日轮山城", 50, 5, 8, 720, "日轮山城暗藏杀机") },                // 12min
        { 4, ("荻花宫", 65, 8, 8, 900, "荻花宫深处血月降临") },                // 15min
    };

    public static readonly Dictionary<int, string[]> BossNames = new()
    {
        { 1, new[] { "董龙", "汪莽", "肖人德", "秦颐岩" } },
        { 2, new[] { "影煞", "罗宇", "方鹤影", "萧沙" } },
        { 3, new[] { "源明雅", "阿坊古", "柳生雪", "八岐大蛇" } },
        { 4, new[] { "牡丹", "大蛇", "沙利亚", "阿萨辛" } },
    };

    // 每个Boss的HP配置 (普通难度)
    public static readonly Dictionary<int, int[]> BossHpConfig = new()
    {
        { 1, new[] { 300000, 400000, 350000, 800000 } },
        { 2, new[] { 500000, 600000, 550000, 1200000 } },
        { 3, new[] { 800000, 900000, 850000, 2000000 } },
        { 4, new[] { 1200000, 1500000, 1300000, 3500000 } },
    };

    public static List<DungeonInfo> GetDungeonList()
    {
        var list = new List<DungeonInfo>();
        foreach (var kv in DungeonConfig)
        {
            list.Add(new DungeonInfo
            {
                DungeonId = (uint)kv.Key,
                Name = kv.Value.Name,
                MinLevel = (uint)kv.Value.MinLv,
                MinPlayers = (uint)kv.Value.MinP,
                MaxPlayers = (uint)kv.Value.MaxP,
                Description = kv.Value.Desc,
            });
        }
        return list;
    }

    public static (int code, DungeonInstance? inst) EnterDungeon(int dungeonId, int difficulty, ulong teamId, List<ulong> playerIds)
    {
        if (!DungeonConfig.ContainsKey(dungeonId))
            return (1, null); // 副本不存在

        var cfg = DungeonConfig[dungeonId];
        if (playerIds.Count < cfg.MinP || playerIds.Count > cfg.MaxP)
            return (2, null); // 人数不符合

        var inst = new DungeonInstance
        {
            ProgressId = Interlocked.Increment(ref _nextProgressId),
            DungeonId = dungeonId,
            Difficulty = difficulty,
            TeamId = teamId,
            PlayerIds = playerIds,
            StartTime = DateTime.UtcNow,
        };

        // 初始化Boss
        var names = BossNames[dungeonId];
        var hps = BossHpConfig[dungeonId];
        var hpMultiplier = difficulty == 1 ? 1.0 : difficulty == 2 ? 2.0 : 3.5;
        for (int i = 0; i < 4; i++)
        {
            inst.Bosses[i] = new BossState
            {
                BossIndex = i,
                Name = names[i],
                MaxHp = (int)(hps[i] * hpMultiplier),
                CurrentHp = (int)(hps[i] * hpMultiplier),
                Phase = 1,
            };
        }

        // 初始化玩家
        foreach (var pid in playerIds)
        {
            inst.Players.Add(new DungeonPlayer { PlayerId = pid, Name = $"Player_{pid}" });
        }

        lock (_instances) { _instances[inst.ProgressId] = inst; }
        Logger.Info("Dungeon", $"Dungeon[{inst.ProgressId}] {cfg.Name} started, {playerIds.Count} players");

        return (0, inst);
    }

    public static DungeonInstance? GetInstance(ulong progressId)
    {
        lock (_instances) { return _instances.GetValueOrDefault(progressId); }
    }

    public static DungeonInstance? FindInstanceByPlayer(ulong playerId)
    {
        lock (_instances)
        {
            return _instances.Values.FirstOrDefault(i =>
                i.PlayerIds.Contains(playerId) && i.Status == 1);
        }
    }

    public static (int code, BossState? boss) DamageBoss(ulong progressId, int bossIndex, int damage)
    {
        var inst = GetInstance(progressId);
        if (inst == null) return (1, null);
        if (bossIndex < 0 || bossIndex > 3) return (2, null);
        var boss = inst.Bosses[bossIndex];
        if (boss.IsDead) return (3, null);

        // 终极Boss未解锁时免疫伤害
        if (boss.IsUltimate && !inst.UltimateSpawned)
            return (4, null);

        boss.CurrentHp = Math.Max(0, boss.CurrentHp - damage);
        inst.ElapsedSeconds = (int)(DateTime.UtcNow - inst.StartTime).TotalSeconds;

        // 阶段转换
        var hpPercent = (double)boss.CurrentHp / boss.MaxHp * 100;
        if (hpPercent <= 30) boss.Phase = 3;
        else if (hpPercent <= 60) boss.Phase = 2;

        if (boss.CurrentHp <= 0)
        {
            boss.IsDead = true;
            boss.KillTimeSec = inst.ElapsedSeconds;

            if (boss.IsUltimate)
            {
                inst.Status = 2; // 通关
                Logger.Info("Dungeon", $"Dungeon[{progressId}] ultimate boss killed! COMPLETE");
            }
            else
            {
                Logger.Info("Dungeon", $"Dungeon[{progressId}] Boss{bossIndex+1} {boss.Name} killed in {boss.KillTimeSec}s");
                CheckUltimateUnlock(inst);
            }
        }

        return (0, boss);
    }

    private static void CheckUltimateUnlock(DungeonInstance inst)
    {
        // 前3Boss都死了才检查
        if (inst.Bosses.Take(3).Any(b => !b.IsDead)) return;

        var totalTime = inst.Bosses[2].KillTimeSec; // Boss3的击杀时间
        var timeLimit = DungeonConfig[inst.DungeonId].TimeLimit;

        inst.UltimateUnlocked = totalTime <= timeLimit;
        inst.ElapsedSeconds = (int)(DateTime.UtcNow - inst.StartTime).TotalSeconds;

        Logger.Info("Dungeon", $"Dungeon[{inst.ProgressId}] check unlock: time={totalTime}s, limit={timeLimit}s, unlocked={inst.UltimateUnlocked}");

        if (inst.UltimateUnlocked)
        {
            inst.UltimateSpawned = true;
            // 终极Boss出现，重置血量
            inst.Bosses[3].CurrentHp = inst.Bosses[3].MaxHp;
            inst.Bosses[3].Phase = 1;
        }
    }

    public static DungeonInstance? LeaveDungeon(ulong progressId, ulong playerId)
    {
        var inst = GetInstance(progressId);
        if (inst == null) return null;
        inst.PlayerIds.Remove(playerId);
        if (inst.PlayerIds.Count == 0)
        {
            inst.Status = 3; // 失败
            lock (_instances) { _instances.Remove(progressId); }
        }
        return inst;
    }

    public static int RevivePlayer(ulong progressId, ulong playerId, bool useGold)
    {
        var inst = GetInstance(progressId);
        if (inst == null) return -1;
        var player = inst.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return -1;
        if (player.ReviveCount >= 3) return -2; // 复活次数上限
        player.ReviveCount++;
        player.IsAlive = true;
        Logger.Info("Dungeon", $"Player {playerId} revived in dungeon {progressId} (count={player.ReviveCount})");
        return player.ReviveCount;
    }

    // 后台定时清理过期副本
    public static void CleanupExpired()
    {
        lock (_instances)
        {
            var expired = _instances.Values
                .Where(i => i.Status == 1 && (DateTime.UtcNow - i.StartTime).TotalMinutes > 60)
                .ToList();
            foreach (var e in expired)
            {
                e.Status = 3;
                _instances.Remove(e.ProgressId);
                Logger.Info("Dungeon", $"Dungeon[{e.ProgressId}] expired and cleaned");
            }
        }
    }
}

public class DungeonInfo
{
    public uint DungeonId;
    public string Name = "";
    public uint MinLevel;
    public uint MinPlayers;
    public uint MaxPlayers;
    public string Description = "";
}