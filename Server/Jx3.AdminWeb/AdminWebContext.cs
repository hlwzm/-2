using System.Collections.Concurrent;

namespace Jx3.AdminWeb;

class AdminWebContext
{
    public ConcurrentDictionary<ulong, string> BannedPlayers { get; } = new();
    public List<Announcement> Announcements { get; } = new();
    public object AnnLock { get; } = new();
    public ulong NextAnnId = 1;
    public Random Rng { get; } = new();
    public List<MailRecord> SentMails { get; } = new();
    public ulong NextMailId = 1;
    public List<ReportItem> ReportStore { get; } = new();
    public List<object> DemoUserCache { get; } = new();
    public DateTime DemoUserCacheTime = DateTime.MinValue;

    public static string CST() => DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");

    public static ulong SumGold(List<Jx3.MockServer.Data.TradeListing> list) =>
        list.Aggregate(0UL, (acc, l) => acc + l.UnitPrice * (ulong)l.Count);

    public List<object> GetDemoUsers()
    {
        if (DemoUserCache.Count > 0 && (DateTime.UtcNow - DemoUserCacheTime).TotalMinutes < 5) return DemoUserCache;
        DemoUserCache.Clear();
        for (int i = 0; i < 80; i++)
        {
            var pid = 20001UL + (ulong)i;
            DemoUserCache.Add(new { playerId = pid, playerName = "侠客_" + pid, level = Rng.Next(1, 80),
                exp = (uint)Rng.Next(0, 99999), gold = (ulong)Rng.Next(1000, 999999), gem = (ulong)Rng.Next(0, 5000),
                tribute = (ulong)Rng.Next(0, 2000), pvpScore = (ulong)Rng.Next(0, 5000), pvpRank = Rng.Next(0, 10),
                vip = i % 5 == 0 ? 1 : 0, isBanned = i % 9 == 0, banReason = i % 9 == 0 ? "违规操作" : "",
                regDate = DateTime.UtcNow.AddDays(-Rng.Next(1, 180)).AddHours(8).ToString("yyyy-MM-dd"),
                lastLogin = DateTime.UtcNow.AddHours(-Rng.Next(0, 72)).AddHours(8).ToString("yyyy-MM-dd HH:mm"),
                online = Rng.Next(0, 2) == 1, phone = "138" + (10000000 + Rng.Next(0, 9999999)).ToString() });
        }
        DemoUserCacheTime = DateTime.UtcNow;
        return DemoUserCache;
    }

    public void InitReports()
    {
        if (ReportStore.Count > 0) return;
        string[] types = ["cheating", "harassment", "scam", "spam", "afk"];
        string[] typeNames = ["外挂/作弊", "骚扰/辱骂", "诈骗", "广告刷屏", "挂机"];
        string[] statuses = ["pending", "pending", "pending", "resolved", "dismissed"];
        for (int i = 0; i < 25; i++)
        {
            var t = types[Rng.Next(types.Length)];
            ReportStore.Add(new ReportItem {
                Id = (ulong)(i + 1), ReporterName = "举报者_" + (100 + i), TargetName = "目标_" + (200 + i),
                ReportType = t, ReportTypeName = typeNames[Array.IndexOf(types, t)],
                Detail = "行为异常: " + t, Status = statuses[Rng.Next(statuses.Length)],
                CreatedAt = DateTime.UtcNow.AddHours(-Rng.Next(0, 72)).AddHours(8).ToString("yyyy-MM-dd HH:mm"),
                ProcessedBy = i % 3 == 0 ? "Admin" : "", ProcessedAt = i % 3 == 0 ? CST() : ""
            });
        }
    }

    public void SeedLogData()
    {
        if (Jx3.MockServer.Data.ActionLogStore.Instance.GetTodayStats().loginCount > 0) return;
        var r = new Random();
        string[][] players = [new[] {"侠客_10001","10001"},new[] {"侠客_10002","10002"},new[] {"侠客_10003","10003"},new[] {"侠客_10004","10004"},new[] {"侠客_10005","10005"}];
        string[] acts = ["login","chat","trade_sell","trade_buy","dungeon","pvp","quest","guild","shop"];
        string[] msgs = ["有人组队打稻香村吗？","收玄铁，价格好说！","帮会收人啦~","副本来人4=1","出售紫晶石","谢谢大佬带飞！"];
        string[] dngs = ["风雨稻香村","天子峰","荨花宫","战宝迦兰","持国天王殿"];
        string[] its = ["玄铁剑","紫晶石","龙鳞甲","凤羽冠","星陨铁","培元丹","大还丹"];
        for (int i = 0; i < 150; i++) {
            int pi = r.Next(players.Length);
            var pid = ulong.Parse(players[pi][1]);
            var nm = players[pi][0];
            var ac = acts[r.Next(acts.Length)];
            var dt = ac switch {
                "login" => "登录游戏",
                "chat" => "发送消息: " + msgs[r.Next(msgs.Length)],
                "trade_sell" => "上架物品 " + its[r.Next(its.Length)] + " x" + r.Next(1,10),
                "trade_buy" => "购买 " + its[r.Next(its.Length)],
                "dungeon" => "进入副本 " + dngs[r.Next(dngs.Length)],
                "pvp" => "PVP匹配", "quest" => "提交任务", "guild" => "帮会操作", "shop" => "商店购买", _ => "其他操作"
            };
            Jx3.MockServer.Data.ActionLogStore.Instance.AddLog(pid, nm, ac, dt);
        }
        Console.WriteLine($"  Seeded demo log data: {Jx3.MockServer.Data.ActionLogStore.Instance.GetTodayStats().loginCount} logins today");
    }
}
