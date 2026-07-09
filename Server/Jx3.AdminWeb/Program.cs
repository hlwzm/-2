using System.Collections.Concurrent;
using Jx3.MockServer.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// ── In-memory admin state ──
var bannedPlayers = new ConcurrentDictionary<ulong, string>();       // pid -> reason
var announcements = new List<Announcement>();
var annLock = new object();
ulong nextAnnId = 1;

// ── Helper ──
var rng = new Random();
static ulong SumGold(List<TradeListing> list) =>
    list.Aggregate(0UL, (acc, l) => acc + l.UnitPrice * (ulong)l.Count);

// ══════════════════════════════════════════════
//  GET /api/dashboard
// ══════════════════════════════════════════════
app.MapGet("/api/dashboard", () =>
{
    var users = UserStore.Instance;
    var trade = TradeStore.Instance;
    var allUsers = new List<ulong>();
    // Reflect to get all user IDs (UserStore only exposes GetByPid, not list all)
    // We'll track user count from generated data
    var totalUsers = 100; // approximate from mock data generation pattern
    var onlineCount = rng.Next(20, 60);
    var allListings = trade.Search("", 0, 1, 99999);
    var totalTradeVolume = SumGold(allListings);
    var totalTrades = allListings.Count;

    return Results.Json(new
    {
        onlinePlayers = onlineCount,
        totalUsers = totalUsers,
        totalTrades = totalTrades,
        tradeVolume = (ulong)totalTradeVolume,
        dungeonClears = rng.Next(100, 500),
        bannedCount = bannedPlayers.Count,
        serverTime = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")
    });
});

// ══════════════════════════════════════════════
//  GET /api/users
// ══════════════════════════════════════════════
app.MapGet("/api/users", (int page, string? search) =>
{
    if (page < 1) page = 1;
    int pageSize = 20;
    var store = UserStore.Instance;

    // Collect all users (we simulate having many by generating mock data)
    var allUsers = new List<object>();
    // Use reflection-like approach: iterate possible PIDs
    // Since UserStore only has GetByPid for lookup, we'll generate demo data
    for (ulong pid = 10001; pid <= 10100; pid++)
    {
        var u = store.GetByPid(pid);
        if (u != null)
        {
            if (!string.IsNullOrEmpty(search) &&
                !u.PlayerName.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;
            allUsers.Add(new
            {
                playerId = u.PlayerId,
                playerName = u.PlayerName,
                level = u.Level,
                gold = u.Gold,
                gem = u.Gem,
                vip = u.Tribute > 500 ? 1 : 0,
                isBanned = bannedPlayers.ContainsKey(pid),
                banReason = bannedPlayers.GetValueOrDefault(pid, "")
            });
        }
    }

    // If no real users found, provide demo data
    if (allUsers.Count == 0)
    {
        for (int i = 0; i < 50; i++)
        {
            var pid = 20001UL + (ulong)i;
            allUsers.Add(new
            {
                playerId = pid,
                playerName = "侠客_" + pid,
                level = rng.Next(1, 80),
                gold = (ulong)rng.Next(1000, 999999),
                gem = (ulong)rng.Next(0, 5000),
                vip = rng.Next(0, 2),
                isBanned = i % 7 == 0,
                banReason = i % 7 == 0 ? "违规操作" : ""
            });
        }
    }

    var total = allUsers.Count;
    var paged = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    return Results.Json(new
    {
        total,
        page,
        pageSize,
        items = paged
    });
});

// ══════════════════════════════════════════════
//  GET /api/users/{id}
// ══════════════════════════════════════════════
app.MapGet("/api/users/{id:long}", (ulong id) =>
{
    var store = UserStore.Instance;
    var u = store.GetByPid(id);

    if (u == null)
    {
        // Return demo data for any ID
        return Results.Json(new
        {
            playerId = id,
            playerName = "侠客_" + id,
            level = rng.Next(1, 80),
            exp = (uint)rng.Next(0, 99999),
            gold = (ulong)rng.Next(1000, 999999),
            gem = (ulong)rng.Next(0, 5000),
            tribute = (ulong)rng.Next(0, 2000),
            pvpScore = (ulong)rng.Next(0, 10000),
            pvpRank = rng.Next(0, 100),
            isBanned = bannedPlayers.ContainsKey(id),
            banReason = bannedPlayers.GetValueOrDefault(id, ""),
            vip = rng.Next(0, 2),
            createdAt = DateTime.UtcNow.AddDays(-rng.Next(1, 365)).AddHours(8).ToString("yyyy-MM-dd HH:mm"),
            lastLogin = DateTime.UtcNow.AddHours(-rng.Next(0, 48)).AddHours(8).ToString("yyyy-MM-dd HH:mm"),
            guildName = rng.Next(0, 2) == 0 ? "" : ("江湖帮会_" + rng.Next(1, 20))
        });
    }

    return Results.Json(new
    {
        playerId = u.PlayerId,
        playerName = u.PlayerName,
        level = u.Level,
        exp = u.Exp,
        gold = u.Gold,
        gem = u.Gem,
        tribute = u.Tribute,
        pvpScore = u.PvpScore,
        pvpRank = u.PvpRank,
        isBanned = bannedPlayers.ContainsKey(id),
        banReason = bannedPlayers.GetValueOrDefault(id, ""),
        vip = u.Tribute > 500 ? 1 : 0,
        createdAt = "2026-01-01 10:00",
        lastLogin = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm"),
        guildName = ""
    });
});

// ══════════════════════════════════════════════
//  POST /api/users/{id}/ban
// ══════════════════════════════════════════════
app.MapPost("/api/users/{id:long}/ban", (ulong id, BanRequest req) =>
{
    bannedPlayers[id] = string.IsNullOrEmpty(req.Reason) ? "管理员操作" : req.Reason;
    return Results.Json(new { code = 0, msg = "用户已封禁", playerId = id });
});

// ══════════════════════════════════════════════
//  POST /api/users/{id}/unban
// ══════════════════════════════════════════════
app.MapPost("/api/users/{id:long}/unban", (ulong id) =>
{
    bannedPlayers.TryRemove(id, out _);
    return Results.Json(new { code = 0, msg = "用户已解封", playerId = id });
});

// ══════════════════════════════════════════════
//  GET /api/announcements
// ══════════════════════════════════════════════
app.MapGet("/api/announcements", () =>
{
    lock (annLock)
    {
        return Results.Json(announcements.OrderByDescending(a => a.CreatedAt).ToList());
    }
});

// ══════════════════════════════════════════════
//  POST /api/announcements
// ══════════════════════════════════════════════
app.MapPost("/api/announcements", (CreateAnnouncement req) =>
{
    var ann = new Announcement
    {
        Id = nextAnnId++,
        Title = req.Title,
        Content = req.Content,
        CreatedAt = DateTime.UtcNow.AddHours(8),
        Author = "管理员"
    };
    lock (annLock) { announcements.Add(ann); }
    return Results.Json(new { code = 0, msg = "公告已发布", id = ann.Id });
});

// ══════════════════════════════════════════════
//  DELETE /api/announcements/{id}
// ══════════════════════════════════════════════
app.MapDelete("/api/announcements/{id:long}", (ulong id) =>
{
    lock (annLock)
    {
        var removed = announcements.RemoveAll(a => a.Id == id);
        return Results.Json(new { code = removed > 0 ? 0 : 1, msg = removed > 0 ? "公告已删除" : "公告不存在" });
    }
});

// ══════════════════════════════════════════════
//  GET /api/trade/listings
// ══════════════════════════════════════════════
app.MapGet("/api/trade/listings", (int page, int pageSize) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    var store = TradeStore.Instance;
    var listings = store.Search("", 0, 1, 99999);
    var total = listings.Count;

    // If no real listings, generate demo data
    if (total == 0)
    {
        var demo = new List<object>();
        string[] itemNames = ["玄铁剑", "紫晶石", "龙鳞甲", "凤羽冠", "星陨铁", "天外陨铁", "九转金丹", "凝神露", "培元丹", "大还丹", "丝绸", "精铁", "皮革", "灵玉", "千年人参"];
        int[] qualities = [1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 1, 2, 3, 4, 5];
        for (int i = 0; i < 35; i++)
        {
            var idx = rng.Next(itemNames.Length);
            var sellerId = 20000UL + (ulong)rng.Next(1, 100);
            demo.Add(new
            {
                id = (ulong)(i + 1),
                sellerId = sellerId,
                sellerName = "侠客_" + sellerId,
                itemName = itemNames[idx],
                itemQuality = qualities[idx],
                count = rng.Next(1, 20),
                unitPrice = (ulong)rng.Next(100, 50000),
                totalPrice = (ulong)(rng.Next(100, 50000) * rng.Next(1, 20)),
                remainingHours = rng.Next(1, 48),
                listedAt = DateTime.UtcNow.AddHours(-rng.Next(0, 72)).AddHours(8).ToString("yyyy-MM-dd HH:mm")
            });
        }
        total = demo.Count;
        var paged = demo.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Results.Json(new { total, page, pageSize, items = paged });
    }

    var items = listings.Select(l => new
    {
        l.Id,
        sellerId = l.SellerPid,
        l.SellerName,
        l.ItemName,
        l.ItemQuality,
        l.Count,
        l.UnitPrice,
        totalPrice = l.UnitPrice * (ulong)l.Count,
        remainingHours = 48 - (int)(DateTime.UtcNow - l.ListedAt).TotalHours,
        listedAt = l.ListedAt.AddHours(8).ToString("yyyy-MM-dd HH:mm")
    }).Skip((page - 1) * pageSize).Take(pageSize).ToList();

    return Results.Json(new { total, page, pageSize, items });
});

// ══════════════════════════════════════════════
//  GET /api/dungeon/stats
// ══════════════════════════════════════════════
app.MapGet("/api/dungeon/stats", () =>
{
    string[] dungeons = ["风雨稻香村", "天子峰", "荻花宫", "战宝迦兰", "持国天王殿"];
    string[] difficulties = ["普通", "英雄", "挑战"];
    var stats = new List<object>();
    foreach (var d in dungeons)
    {
        foreach (var diff in difficulties)
        {
            stats.Add(new
            {
                dungeonName = d,
                difficulty = diff,
                totalClears = rng.Next(10, 500),
                avgClearTime = rng.Next(5, 45) + "分钟",
                totalDeaths = rng.Next(0, 200),
                totalPlayers = rng.Next(50, 2000),
                dropRate = (rng.NextDouble() * 0.3 + 0.1).ToString("0.0%")
            });
        }
    }
    return Results.Json(new { dungeons = stats, totalDungeons = dungeons.Length });
});

// ── Fallback ──
app.MapFallbackToFile("index.html");

Console.WriteLine("╔══════════════════════════════════╗");
Console.WriteLine("║  AdminWeb Server starting...     ║");
Console.WriteLine("║  http://localhost:5000            ║");
Console.WriteLine("╚══════════════════════════════════╝");
app.Run("http://0.0.0.0:5000");

// ── Record types ──
record BanRequest(string Reason);
record CreateAnnouncement(string Title, string Content);
record Announcement
{
    public ulong Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Author { get; set; } = "";
}
