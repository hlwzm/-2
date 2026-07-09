using System.Collections.Concurrent;
using Jx3.MockServer.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var bannedPlayers = new ConcurrentDictionary<ulong, string>();
var announcements = new List<Announcement>();
var annLock = new object();
ulong nextAnnId = 1;
var rng = new Random();
var sentMails = new List<MailRecord>();
ulong nextMailId = 1;
var reportStore = new List<ReportItem>();
void InitReports() {
    if (reportStore.Count > 0) return;
    string[] types = ["cheating", "harassment", "scam", "spam", "afk"];
    string[] typeNames = ["外挂/作弊", "骚扰/辱骂", "诈骗", "广告刷屏", "挂机"];
    string[] statuses = ["pending", "pending", "pending", "resolved", "dismissed"];
    for (int i = 0; i < 25; i++) {
        var t = types[rng.Next(types.Length)];
        reportStore.Add(new ReportItem {
            Id = (ulong)(i + 1), ReporterName = "举报者_" + (100 + i), TargetName = "目标_" + (200 + i),
            ReportType = t, ReportTypeName = typeNames[Array.IndexOf(types, t)],
            Detail = "行为异常: " + t, Status = statuses[rng.Next(statuses.Length)],
            CreatedAt = DateTime.UtcNow.AddHours(-rng.Next(0, 72)).AddHours(8).ToString("yyyy-MM-dd HH:mm"),
            ProcessedBy = i % 3 == 0 ? "Admin" : "", ProcessedAt = i % 3 == 0 ? CST() : ""
        });
    }
}
InitReports();
static ulong SumGold(List<TradeListing> list) => list.Aggregate(0UL, (acc, l) => acc + l.UnitPrice * (ulong)l.Count);
static string CST() => DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");
var demoUserCache = new List<object>();
var demoUserCacheTime = DateTime.MinValue;
List<object> GetDemoUsers() {
    if (demoUserCache.Count > 0 && (DateTime.UtcNow - demoUserCacheTime).TotalMinutes < 5) return demoUserCache;
    demoUserCache.Clear();
    for (int i = 0; i < 80; i++) {
        var pid = 20001UL + (ulong)i;
        demoUserCache.Add(new { playerId = pid, playerName = "侠客_" + pid, level = rng.Next(1, 80),
            exp = (uint)rng.Next(0, 99999), gold = (ulong)rng.Next(1000, 999999), gem = (ulong)rng.Next(0, 5000),
            tribute = (ulong)rng.Next(0, 2000), pvpScore = (ulong)rng.Next(0, 5000), pvpRank = rng.Next(0, 10),
            vip = i % 5 == 0 ? 1 : 0, isBanned = i % 9 == 0, banReason = i % 9 == 0 ? "违规操作" : "",
            regDate = DateTime.UtcNow.AddDays(-rng.Next(1, 180)).AddHours(8).ToString("yyyy-MM-dd"),
            lastLogin = DateTime.UtcNow.AddHours(-rng.Next(0, 72)).AddHours(8).ToString("yyyy-MM-dd HH:mm"),
            online = rng.Next(0, 2) == 1, phone = "138" + (10000000 + rng.Next(0, 9999999)).ToString() });
    }
    demoUserCacheTime = DateTime.UtcNow;
    return demoUserCache;
}

app.MapGet("/api/logs/types", () => {
    var types = ActionLogStore.Instance.GetActionTypes();
    if (types.Count == 0) types = ["login", "chat", "trade_buy", "trade_sell", "dungeon", "item", "quest", "pvp", "guild", "friend", "team", "shop", "combat", "hero"];
    return Results.Json(types);
});

app.MapGet("/api/logs", (HttpRequest req) => {
    int page = 1, pageSize = 25;
    ulong? pid = null; string actionType = "", startDate = "", endDate = "";
    if (req.Query.ContainsKey("page")) int.TryParse(req.Query["page"], out page);
    if (req.Query.ContainsKey("pageSize")) int.TryParse(req.Query["pageSize"], out pageSize);
    ulong parsedPid = 0; if (req.Query.ContainsKey("playerId")) ulong.TryParse(req.Query["playerId"], out parsedPid); if (parsedPid > 0) pid = parsedPid;
    if (req.Query.ContainsKey("actionType")) actionType = req.Query["actionType"];
    if (req.Query.ContainsKey("startDate")) startDate = req.Query["startDate"];
    if (req.Query.ContainsKey("endDate")) endDate = req.Query["endDate"];
    if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 25;
    DateTime? sd = null, ed = null;
    if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd2)) sd = sd2.ToUniversalTime();
    if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed2)) ed = ed2.ToUniversalTime().AddDays(1);
    var (items, total) = ActionLogStore.Instance.Search(pid, string.IsNullOrEmpty(actionType) ? null : actionType, sd, ed, page, pageSize);
    return Results.Json(new { total, page, pageSize, items = items.Select(l => new { l.Id, l.PlayerId, l.PlayerName, l.ActionType, l.Detail, timestamp = l.Timestamp.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss"), l.IpAddress }) });
});

app.MapGet("/api/logs/player/{playerId:long}", (ulong playerId, int page = 1, int pageSize = 20) => {
    if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var (items, total) = ActionLogStore.Instance.Search(playerId, null, null, null, page, pageSize);
    return Results.Json(new { total, page, pageSize, items = items.Select(l => new { l.Id, l.PlayerId, l.PlayerName, l.ActionType, l.Detail, timestamp = l.Timestamp.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss"), l.IpAddress }) });
});

app.MapGet("/api/dashboard", () => {
    var onlineCount = rng.Next(20, 60);
    var allListings = TradeStore.Instance.Search("", 0, 1, 99999);
    var stats = ActionLogStore.Instance.GetTodayStats();
    return Results.Json(new { onlinePlayers = onlineCount, totalUsers = 180, totalTrades = allListings.Count, tradeVolume = SumGold(allListings), dungeonClears = rng.Next(100, 500), bannedCount = bannedPlayers.Count, todayLogins = stats.loginCount, todayChats = stats.chatCount, todayTrades = stats.tradeCount, todayDungeons = stats.dungeonCount, todayPvp = stats.pvpCount, todayGuild = stats.guildCount, serverTime = CST(), uptime = (DateTime.UtcNow - new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc)).TotalHours.ToString("F1") + "小时" });
});
app.MapGet("/api/users", (HttpRequest req) => {
    int page = 1, levelMin = 0, levelMax = 0, vip = 0;
    string search = "", status = "", regFrom = "", regTo = "";
    if (req.Query.ContainsKey("page")) int.TryParse(req.Query["page"], out page);
    if (req.Query.ContainsKey("search")) search = req.Query["search"];
    if (req.Query.ContainsKey("status")) status = req.Query["status"];
    if (req.Query.ContainsKey("vip")) int.TryParse(req.Query["vip"], out vip);
    if (req.Query.ContainsKey("levelMin")) int.TryParse(req.Query["levelMin"], out levelMin);
    if (req.Query.ContainsKey("levelMax")) int.TryParse(req.Query["levelMax"], out levelMax);
    if (page < 1) page = 1;
    int pageSize = 20;
    var allUsers = new List<object>();
    var store = UserStore.Instance;
    for (ulong pid = 10001; pid <= 10120; pid++) {
        var u = store.GetByPid(pid);
        if (u == null) continue;
        if (!string.IsNullOrEmpty(search) && !u.PlayerName.Contains(search, StringComparison.OrdinalIgnoreCase) && !pid.ToString().Contains(search)) continue;
        if (levelMin > 0 && u.Level < levelMin) continue;
        if (levelMax > 0 && u.Level > levelMax) continue;
        if (vip > 0 && u.Tribute <= 500) continue;
        if (status == "banned" && !bannedPlayers.ContainsKey(pid)) continue;
        allUsers.Add(new { playerId = u.PlayerId, playerName = u.PlayerName, level = u.Level, exp = u.Exp, gold = u.Gold, gem = u.Gem, tribute = u.Tribute, pvpScore = u.PvpScore, pvpRank = u.PvpRank, vip = u.Tribute > 500 ? 1 : 0, isBanned = bannedPlayers.ContainsKey(pid), banReason = bannedPlayers.GetValueOrDefault(pid, ""), online = rng.Next(0, 2) == 1, regDate = DateTime.UtcNow.AddDays(-rng.Next(1, 90)).AddHours(8).ToString("yyyy-MM-dd") });
    }
    if (allUsers.Count == 0) allUsers.AddRange(GetDemoUsers());
    var total = allUsers.Count;
    var paged = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return Results.Json(new { total, page, pageSize = paged.Count, items = paged });
});

app.MapGet("/api/users/search", (HttpRequest req) => {
    int page = 1, pageSize = 20, levelMin = 0, levelMax = 0, vip = 0;
    string keyword = "", status = "";
    if (req.Query.ContainsKey("page")) int.TryParse(req.Query["page"], out page);
    if (req.Query.ContainsKey("keyword")) keyword = req.Query["keyword"];
    if (req.Query.ContainsKey("status")) status = req.Query["status"];
    if (req.Query.ContainsKey("levelMin")) int.TryParse(req.Query["levelMin"], out levelMin);
    if (req.Query.ContainsKey("levelMax")) int.TryParse(req.Query["levelMax"], out levelMax);
    if (req.Query.ContainsKey("vip")) int.TryParse(req.Query["vip"], out vip);
    var allUsers = new List<object>();
    var store = UserStore.Instance;
    for (ulong pid = 10001; pid <= 10120; pid++) {
        var u = store.GetByPid(pid);
        if (u == null) continue;
        if (!string.IsNullOrEmpty(keyword) && !u.PlayerName.Contains(keyword, StringComparison.OrdinalIgnoreCase) && !pid.ToString().Contains(keyword)) continue;
        if (levelMin > 0 && u.Level < levelMin) continue;
        if (levelMax > 0 && u.Level > levelMax) continue;
        if (vip > 0 && u.Tribute <= 500) continue;
        if (status == "banned" && !bannedPlayers.ContainsKey(pid)) continue;
        allUsers.Add(new { playerId = u.PlayerId, playerName = u.PlayerName, level = u.Level, gold = u.Gold, gem = u.Gem, vip = u.Tribute > 500 ? 1 : 0, isBanned = bannedPlayers.ContainsKey(pid), banReason = bannedPlayers.GetValueOrDefault(pid, "") });
    }
    if (allUsers.Count == 0) allUsers.AddRange(GetDemoUsers());
    var total = allUsers.Count;
    var paged = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return Results.Json(new { total, page, pageSize, items = paged });
});

app.MapGet("/api/users/{id:long}", (ulong id) => {
    var u = UserStore.Instance.GetByPid(id);
    if (u != null) return Results.Json(new { playerId = u.PlayerId, playerName = u.PlayerName, level = u.Level, exp = u.Exp, gold = u.Gold, gem = u.Gem, tribute = u.Tribute, pvpScore = u.PvpScore, pvpRank = u.PvpRank, vip = u.Tribute > 500 ? 1 : 0, isBanned = bannedPlayers.ContainsKey(id), banReason = bannedPlayers.GetValueOrDefault(id, ""), regDate = CST(), lastLogin = CST(), online = true });
    return Results.Json(new { playerId = id, playerName = "侠客_" + id, level = rng.Next(1, 80), exp = (uint)rng.Next(0, 99999), gold = (ulong)rng.Next(1000, 999999), gem = (ulong)rng.Next(0, 5000), tribute = (ulong)rng.Next(0, 2000), pvpScore = (ulong)rng.Next(0, 5000), pvpRank = rng.Next(0, 10), vip = 0, isBanned = bannedPlayers.ContainsKey(id), banReason = bannedPlayers.GetValueOrDefault(id, ""), regDate = DateTime.UtcNow.AddDays(-rng.Next(1, 180)).AddHours(8).ToString("yyyy-MM-dd"), lastLogin = CST(), online = true });
});

app.MapPut("/api/users/{id:long}/profile", (ulong id, ProfileUpdate req) => {
    var u = UserStore.Instance.GetByPid(id);
    if (u != null) {
        if (req.Gold.HasValue) { var diff = (long)req.Gold.Value - (long)u.Gold; u.Gold = req.Gold.Value; ActionLogStore.Instance.AddLog(id, u.PlayerName, "item", $"管理员修改金币 diff={diff}"); }
        if (req.Gem.HasValue) { var diff = (long)req.Gem.Value - (long)u.Gem; u.Gem = req.Gem.Value; ActionLogStore.Instance.AddLog(id, u.PlayerName, "item", $"管理员修改钻石 diff={diff}"); }
        if (req.Level.HasValue) u.Level = req.Level.Value;
        if (!string.IsNullOrEmpty(req.PlayerName)) u.PlayerName = req.PlayerName;
    }
    return Results.Json(new { code = 0, msg = "修改成功" });
});

app.MapPost("/api/users/{id:long}/ban", (ulong id, BanRequest req) => {
    bannedPlayers[id] = req.Reason;
    var u = UserStore.Instance.GetByPid(id);
    ActionLogStore.Instance.AddLog(id, u?.PlayerName ?? "?", "login", $"管理员封禁 reason={req.Reason}");
    return Results.Json(new { code = 0, msg = "已封禁" });
});

app.MapPost("/api/users/{id:long}/unban", (ulong id) => {
    bannedPlayers.TryRemove(id, out _);
    var u = UserStore.Instance.GetByPid(id);
    ActionLogStore.Instance.AddLog(id, u?.PlayerName ?? "?", "login", $"管理员解封");
    return Results.Json(new { code = 0, msg = "已解封" });
});
app.MapPost("/api/mail/send", (SendMailRequest req) => {
    var ids = req.PlayerIds?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    foreach (var idStr in ids) {
        if (ulong.TryParse(idStr, out var pid)) {
            var u = UserStore.Instance.GetByPid(pid);
            ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "侠客_" + pid, "item", $"管理员发送邮件 title={req.Title} gold={req.Gold}");
        }
    }
    lock (sentMails) { sentMails.Add(new MailRecord { Id = nextMailId++, Title = req.Title ?? "", Content = req.Content ?? "", PlayerIds = req.PlayerIds ?? "", Gold = req.Gold, ItemId = req.ItemId, ItemCount = req.ItemCount, SentAt = DateTime.UtcNow }); }
    return Results.Json(new { code = 0, msg = $"邮件已发送给 {ids.Length} 个玩家", count = ids.Length });
});

app.MapGet("/api/mail/history", (int page = 1, int pageSize = 20) => {
    if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 20;
    lock (sentMails) {
        var total = sentMails.Count;
        var items = sentMails.OrderByDescending(m => m.SentAt).Skip((page - 1) * pageSize).Take(pageSize).Select(m => new { m.Id, m.Title, m.Content, m.PlayerIds, m.Gold, m.ItemId, m.ItemCount, sentAt = m.SentAt.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss") });
        return Results.Json(new { total, page, pageSize, items });
    }
});

app.MapPost("/api/items/grant", (GrantItemRequest req) => {
    if (req.PlayerId == 0) return Results.Json(new { code = 1, msg = "无效玩家ID" });
    var u = UserStore.Instance.GetByPid(req.PlayerId);
    var name = u?.PlayerName ?? "侠客_" + req.PlayerId;
    ItemStore.Instance.AddItem(req.PlayerId, req.ItemId, req.Count);
    ActionLogStore.Instance.AddLog(req.PlayerId, name, "item", $"管理员发放物品 itemId={req.ItemId} count={req.Count} reason={req.Reason}");
    return Results.Json(new { code = 0, msg = $"已发放 {req.Count} 个 itemId={req.ItemId} 给 {name}" });
});

app.MapPost("/api/currency/update", (CurrencyUpdateRequest req) => {
    if (req.PlayerId == 0) return Results.Json(new { code = 1, msg = "无效玩家ID" });
    var u = UserStore.Instance.GetByPid(req.PlayerId);
    var name = u?.PlayerName ?? "侠客_" + req.PlayerId;
    if (req.Currency == "gold" || req.Currency == "all") {
        if (req.Amount > 0) UserStore.Instance.AddGold(req.PlayerId, (ulong)Math.Abs(req.Amount));
        else UserStore.Instance.SpendGold(req.PlayerId, (ulong)Math.Abs(req.Amount));
        ActionLogStore.Instance.AddLog(req.PlayerId, name, "item", $"管理员修改金币 amount={req.Amount} reason={req.Reason}");
    }
    if (req.Currency == "gem" && u != null) {
        var newGem = (ulong)Math.Max(0, (long)u.Gem + req.Amount);
        u.Gem = newGem;
        ActionLogStore.Instance.AddLog(req.PlayerId, name, "item", $"管理员修改钻石 amount={req.Amount}(实际{req.Amount}) reason={req.Reason}");
    }
    return Results.Json(new { code = 0, msg = "货币已更新" });
});

app.MapGet("/api/reports", (int page = 1, int pageSize = 20) => {
    if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var total = reportStore.Count;
    var items = reportStore.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return Results.Json(new { total, page, pageSize, items });
});

app.MapPost("/api/reports/{id:long}/action", (ulong id, ReportActionRequest req) => {
    var report = reportStore.FirstOrDefault(r => r.Id == id);
    if (report == null) return Results.Json(new { code = 1, msg = "举报不存在" });
    report.Status = req.Action switch { "warn" => "warned", "ban" => "resolved", "dismiss" => "dismissed", _ => report.Status };
    report.ProcessedBy = "Admin"; report.ProcessedAt = CST();
    ActionLogStore.Instance.AddLog(0, "Admin", "item", $"处理举报 id={id} action={req.Action} reason={req.Reason}");
    return Results.Json(new { code = 0, msg = $"举报已处理: {req.Action}" });
});

app.MapGet("/api/server/stats", () => Results.Json(new {
    connections = rng.Next(20, 80), memoryMB = rng.Next(180, 400), cpuPercent = Math.Round(rng.NextDouble() * 30 + 5, 1),
    threadCount = rng.Next(4, 16), msgPerSecond = rng.Next(50, 300), uptimeHours = Math.Round((DateTime.UtcNow - new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc)).TotalHours, 1),
    totalPackets = rng.Next(10000, 99999), avgLatency = rng.Next(5, 50)
}));

app.MapGet("/api/stats/daily", () => {
    var days = new List<string>(); var dau = new List<int>(); var newUsers = new List<int>();
    var tradeVolume = new List<long>(); var revenue = new List<int>(); var dungeonCounts = new List<int>(); var pvpCounts = new List<int>();
    for (int i = 6; i >= 0; i--) {
        var d = DateTime.UtcNow.AddDays(-i).AddHours(8);
        days.Add(d.ToString("MM-dd")); dau.Add(rng.Next(100, 200)); newUsers.Add(rng.Next(5, 25));
        tradeVolume.Add(rng.Next(30000, 100000)); revenue.Add(rng.Next(800, 2500));
        dungeonCounts.Add(rng.Next(20, 100)); pvpCounts.Add(rng.Next(30, 150));
    }
    return Results.Json(new { days, dau, newUsers, tradeVolume, revenue, dungeonCounts, pvpCounts });
});

app.MapGet("/api/backup", () => Results.Json(new { code = 0, msg = "备份成功", file = $"backup_{DateTime.UtcNow.AddHours(8):yyyyMMdd_HHmmss}.zip", size = $"{rng.Next(50, 500)}MB", timestamp = CST() }));
app.MapGet("/api/announcements", () => { lock (annLock) { return Results.Json(announcements.OrderByDescending(a => a.CreatedAt).ToList()); } });
app.MapPost("/api/announcements", (CreateAnnouncement req) => { lock (annLock) { var a = new Announcement { Id = nextAnnId++, Title = req.Title, Content = req.Content, CreatedAt = DateTime.UtcNow, Author = "Admin" }; announcements.Add(a); return Results.Json(new { code = 0, msg = "公告已创建" }); } });
app.MapDelete("/api/announcements/{id:long}", (ulong id) => { lock (annLock) { var r = announcements.RemoveAll(a => a.Id == id); return Results.Json(new { code = r > 0 ? 0 : 1, msg = r > 0 ? "公告已删除" : "公告不存在" }); } });

app.MapGet("/api/trade/listings", (HttpRequest req) => {
    int page = 1, pageSize = 20, quality = 0; string search = "";
    if (req.Query.ContainsKey("page")) int.TryParse(req.Query["page"], out page);
    if (req.Query.ContainsKey("pageSize")) int.TryParse(req.Query["pageSize"], out pageSize);
    if (req.Query.ContainsKey("search")) search = req.Query["search"];
    if (req.Query.ContainsKey("quality")) int.TryParse(req.Query["quality"], out quality);
    if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var listings = TradeStore.Instance.Search(search, quality, 1, 99999);
    var total = listings.Count;
    if (total == 0) {
        string[] itemNames = ["玄铁剑", "紫晶石", "龙鳞甲", "凤羽冠", "星陨铁", "天外陨铁", "九转金丹", "凝神露", "培元丹", "大还丹", "丝绸", "精铁", "皮革", "灵玉", "千年人参"];
        int[] qualities = [1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 1, 2, 3, 4, 5];
        var demo = new List<object>();
        for (int i = 0; i < 45; i++) {
            var idx = rng.Next(itemNames.Length); var sellerId = 20000UL + (ulong)rng.Next(1, 100);
            if (search != "" && !itemNames[idx].Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
            if (quality > 0 && qualities[idx] != quality) continue;
            demo.Add(new { id = (ulong)(i + 1), sellerId, sellerName = "侠客_" + sellerId, itemName = itemNames[idx], itemQuality = qualities[idx], category = "", count = rng.Next(1, 20), unitPrice = (ulong)rng.Next(100, 50000), totalPrice = (ulong)(rng.Next(100, 50000) * rng.Next(1, 20)), remainingHours = rng.Next(1, 48), listedAt = DateTime.UtcNow.AddHours(-rng.Next(0, 72)).AddHours(8).ToString("yyyy-MM-dd HH:mm") });
        }
        total = demo.Count;
        var paged = demo.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Results.Json(new { total, page, pageSize, items = paged });
    }
    var items = listings.Select(l => new { l.Id, sellerId = l.SellerPid, l.SellerName, l.ItemName, l.ItemQuality, category = "", l.Count, l.UnitPrice, totalPrice = l.UnitPrice * (ulong)l.Count, remainingHours = 48 - (int)(DateTime.UtcNow - l.ListedAt).TotalHours, listedAt = l.ListedAt.AddHours(8).ToString("yyyy-MM-dd HH:mm") }).Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return Results.Json(new { total, page, pageSize, items });
});

app.MapDelete("/api/trade/listings/{id:long}", (ulong id) => {
    var listing = TradeStore.Instance.GetById(id);
    if (listing == null) return Results.Json(new { code = 1, msg = "Listing不存在" });
    TradeStore.Instance.CancelListing(id, listing.SellerPid);
    ActionLogStore.Instance.AddLog(listing.SellerPid, listing.SellerName, "trade_sell", $"管理员强制下架 listingId={id}");
    return Results.Json(new { code = 0, msg = "已强制下架" });
});

app.MapGet("/api/dungeon/stats", () => {
    string[] dungeons = ["风雨稻香村", "天子峰", "荻花宫", "战宝迦兰", "持国天王殿"];
    string[] difficulties = ["普通", "英雄", "挑战"];
    var stats = new List<object>();
    foreach (var d in dungeons) { foreach (var diff in difficulties) stats.Add(new { dungeonName = d, difficulty = diff, totalClears = rng.Next(10, 500), avgClearTime = rng.Next(5, 45) + "分钟", totalDeaths = rng.Next(0, 200), totalPlayers = rng.Next(50, 2000), dropRate = (rng.NextDouble() * 0.3 + 0.1).ToString("0.0%") }); }
    return Results.Json(new { dungeons = stats, totalDungeons = dungeons.Length });
});

app.MapPost("/api/shutdown", () => { _ = Task.Run(async () => { await Task.Delay(1000); Environment.Exit(0); }); return Results.Json(new { code = 0, msg = "服务器将在1秒后关闭" }); });

app.MapFallbackToFile("index.html");

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  指尖江湖2 AdminWeb Server                         ║");
Console.WriteLine("║  http://localhost:5000                              ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
app.Run("http://0.0.0.0:5000");

record BanRequest(string Reason);
record CreateAnnouncement(string Title, string Content);
record ProfileUpdate(string? PlayerName, int? Level, ulong? Gold, ulong? Gem);
record SendMailRequest(string Title, string Content, string PlayerIds, ulong Gold, uint ItemId, int ItemCount);
record GrantItemRequest(ulong PlayerId, uint ItemId, int Count, string Reason);
record CurrencyUpdateRequest(ulong PlayerId, string Currency, int Amount, string Reason);
record ReportActionRequest(string Action, string Reason);
record Announcement { public ulong Id { get; set; } public string Title { get; set; } = ""; public string Content { get; set; } = ""; public DateTime CreatedAt { get; set; } public string Author { get; set; } = ""; }
record MailRecord { public ulong Id { get; set; } public string Title { get; set; } = ""; public string Content { get; set; } = ""; public string PlayerIds { get; set; } = ""; public ulong Gold { get; set; } public uint ItemId { get; set; } public int ItemCount { get; set; } public DateTime SentAt { get; set; } }
record ReportItem { public ulong Id { get; set; } public string ReporterName { get; set; } = ""; public string TargetName { get; set; } = ""; public string ReportType { get; set; } = ""; public string ReportTypeName { get; set; } = ""; public string Detail { get; set; } = ""; public string Status { get; set; } = ""; public string CreatedAt { get; set; } = ""; public string ProcessedBy { get; set; } = ""; public string ProcessedAt { get; set; } = ""; }
