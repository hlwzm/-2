using Jx3.Common.Database;
using Jx3.Common.Utils;

namespace Jx3.PVP;

public class PvpPlayer
{
    public ulong PlayerId;
    public string Name = "";
    public int Rating = 1000;
    public int Tier;           // 0=青铜 1=白银 2=黄金 3=白金 4=钻石 5=大师 6=传说
    public int TotalWins;
    public int TotalLosses;
    public int SeasonScore;    // 本赛季积分
    public int WinStreak;
    public bool IsInQueue;
    public DateTime QueueStartTime;
}

public class MatchResult
{
    public ulong MatchId;
    public List<ulong> TeamA = new();
    public List<ulong> TeamB = new();
    public int Winner;         // 1=TeamA 2=TeamB
    public DateTime StartTime;
    public DateTime EndTime;
    public int DurationSec;
}

public static class PvpService
{
    public static readonly string[] TierNames = { "青铜", "白银", "黄金", "白金", "钻石", "大师", "传说" };
    public static readonly int[] TierRatingThresholds = { 0, 1100, 1300, 1500, 1700, 2000, 2500 };

    // 内存匹配队列
    private static readonly Queue<PvpPlayer> _queue = new();
    private static readonly Dictionary<ulong, PvpPlayer> _players = new();
    private static readonly List<MatchResult> _matchHistory = new();
    private static ulong _nextMatchId = 1;
    private static readonly object _lock = new();

    // === 加入匹配 ===
    public static int JoinQueue(ulong playerId, string name)
    {
        lock (_lock)
        {
            if (_queue.Any(p => p.PlayerId == playerId))
                return 1; // 已在队列

            if (!_players.TryGetValue(playerId, out var player))
            {
                // 从DB加载或创建新玩家
                player = new PvpPlayer { PlayerId = playerId, Name = name };
                _players[playerId] = player;
            }

            player.IsInQueue = true;
            player.QueueStartTime = DateTime.UtcNow;
            _queue.Enqueue(player);
            Logger.Info("PVP", $"Player {playerId}({name}) joined queue. Queue size: {_queue.Count}");
            return 0;
        }
    }

    // === 取消匹配 ===
    public static bool LeaveQueue(ulong playerId)
    {
        lock (_lock)
        {
            var temp = new Queue<PvpPlayer>();
            var found = false;
            while (_queue.Count > 0)
            {
                var p = _queue.Dequeue();
                if (p.PlayerId == playerId)
                {
                    p.IsInQueue = false;
                    found = true;
                }
                else temp.Enqueue(p);
            }
            while (temp.Count > 0) _queue.Enqueue(temp.Dequeue());
            return found;
        }
    }

    // === 尝试匹配 (每3秒调用) ===
    public static (MatchResult? match, PvpPlayer? a, PvpPlayer? b) TryMatch()
    {
        lock (_lock)
        {
            if (_queue.Count < 2) return (null, null, null);

            var list = _queue.ToList();
            // 按Rating相近匹配: Rating差 < 200
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    var diff = Math.Abs(list[i].Rating - list[j].Rating);
                    // 等待时间越长，匹配范围越宽
                    var waitTime = (DateTime.UtcNow - list[i].QueueStartTime).TotalSeconds;
                    var tolerance = 200 + (int)(waitTime / 10) * 50;
                    if (diff <= tolerance)
                    {
                        // 匹配成功!
                        var match = new MatchResult
                        {
                            MatchId = _nextMatchId++,
                            TeamA = new() { list[i].PlayerId },
                            TeamB = new() { list[j].PlayerId },
                            StartTime = DateTime.UtcNow,
                        };
                        list[i].IsInQueue = false;
                        list[j].IsInQueue = false;
                        // 从队列移除
                        _queue.Clear();
                        foreach (var p in list.Where(p => p.IsInQueue))
                            _queue.Enqueue(p);

                        Logger.Info("PVP", $"Match {match.MatchId}: {list[i].Name}({list[i].Rating}) vs {list[j].Name}({list[j].Rating})");
                        return (match, list[i], list[j]);
                    }
                }
            }
            return (null, null, null);
        }
    }

    // === 结算比赛 ===
    public static (int ratingChangeA, int ratingChangeB) EndMatch(MatchResult match, int winner)
    {
        match.Winner = winner;
        match.EndTime = DateTime.UtcNow;
        match.DurationSec = (int)(match.EndTime - match.StartTime).TotalSeconds;
        _matchHistory.Add(match);

        var playerA = _players.GetValueOrDefault(match.TeamA[0]);
        var playerB = _players.GetValueOrDefault(match.TeamB[0]);
        if (playerA == null || playerB == null) return (0, 0);

        // ELO计算
        var expectedA = 1.0 / (1 + Math.Pow(10, (playerB.Rating - playerA.Rating) / 400.0));
        var expectedB = 1.0 - expectedA;
        var k = 32;

        int change;
        if (winner == 1)
        {
            change = (int)(k * (1 - expectedA));
            playerA.Rating += change;
            playerB.Rating -= change;
            playerA.TotalWins++;
            playerB.TotalLosses++;
            playerA.WinStreak++;
            playerB.WinStreak = 0;
        }
        else
        {
            change = (int)(k * (1 - expectedB));
            playerB.Rating += change;
            playerA.Rating -= change;
            playerB.TotalWins++;
            playerA.TotalLosses++;
            playerB.WinStreak++;
            playerA.WinStreak = 0;
        }

        UpdateTier(playerA);
        UpdateTier(playerB);

        var chA = winner == 1 ? change : -change;
        var chB = winner == 2 ? change : -change;
        Logger.Info("PVP", $"Match {match.MatchId} ended: Team{winner} wins. {playerA.Name}:{playerA.Rating} (+{chA}), {playerB.Name}:{playerB.Rating} (+{chB})");
        return (chA, chB);
    }

    private static void UpdateTier(PvpPlayer player)
    {
        for (int i = TierRatingThresholds.Length - 1; i >= 0; i--)
        {
            if (player.Rating >= TierRatingThresholds[i])
            {
                player.Tier = i;
                return;
            }
        }
        player.Tier = 0;
    }

    public static PvpPlayer? GetPlayer(ulong playerId)
    {
        _players.TryGetValue(playerId, out var p);
        return p;
    }
}