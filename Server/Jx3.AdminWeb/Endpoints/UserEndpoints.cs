using Jx3.MockServer.Data;

namespace Jx3.AdminWeb.Endpoints;

static class UserEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
        app.MapGet("/api/users", (HttpRequest req) => {
            int page = 1, levelMin = 0, levelMax = 0, vip = 0;
            string search = "", status = "";
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
                if (status == "banned" && !ctx.BannedPlayers.ContainsKey(pid)) continue;
                allUsers.Add(new { playerId = u.PlayerId, playerName = u.PlayerName, level = u.Level, exp = u.Exp, gold = u.Gold, gem = u.Gem, tribute = u.Tribute, pvpScore = u.PvpScore, pvpRank = u.PvpRank, vip = u.Tribute > 500 ? 1 : 0, isBanned = ctx.BannedPlayers.ContainsKey(pid), banReason = ctx.BannedPlayers.GetValueOrDefault(pid, ""), online = ctx.Rng.Next(0, 2) == 1, regDate = DateTime.UtcNow.AddDays(-ctx.Rng.Next(1, 90)).AddHours(8).ToString("yyyy-MM-dd") });
            }
            if (allUsers.Count == 0) allUsers.AddRange(ctx.GetDemoUsers());
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
                if (status == "banned" && !ctx.BannedPlayers.ContainsKey(pid)) continue;
                allUsers.Add(new { playerId = u.PlayerId, playerName = u.PlayerName, level = u.Level, gold = u.Gold, gem = u.Gem, vip = u.Tribute > 500 ? 1 : 0, isBanned = ctx.BannedPlayers.ContainsKey(pid), banReason = ctx.BannedPlayers.GetValueOrDefault(pid, "") });
            }
            if (allUsers.Count == 0) allUsers.AddRange(ctx.GetDemoUsers());
            var total = allUsers.Count;
            var paged = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Results.Json(new { total, page, pageSize, items = paged });
        });

        app.MapGet("/api/users/{id:long}", (ulong id) => {
            var u = UserStore.Instance.GetByPid(id);
            if (u != null) return Results.Json(new { playerId = u.PlayerId, playerName = u.PlayerName, level = u.Level, exp = u.Exp, gold = u.Gold, gem = u.Gem, tribute = u.Tribute, pvpScore = u.PvpScore, pvpRank = u.PvpRank, vip = u.Tribute > 500 ? 1 : 0, isBanned = ctx.BannedPlayers.ContainsKey(id), banReason = ctx.BannedPlayers.GetValueOrDefault(id, ""), regDate = AdminWebContext.CST(), lastLogin = AdminWebContext.CST(), online = true });
            return Results.Json(new { playerId = id, playerName = "侠客_" + id, level = ctx.Rng.Next(1, 80), exp = (uint)ctx.Rng.Next(0, 99999), gold = (ulong)ctx.Rng.Next(1000, 999999), gem = (ulong)ctx.Rng.Next(0, 5000), tribute = (ulong)ctx.Rng.Next(0, 2000), pvpScore = (ulong)ctx.Rng.Next(0, 5000), pvpRank = ctx.Rng.Next(0, 10), vip = 0, isBanned = ctx.BannedPlayers.ContainsKey(id), banReason = ctx.BannedPlayers.GetValueOrDefault(id, ""), regDate = DateTime.UtcNow.AddDays(-ctx.Rng.Next(1, 180)).AddHours(8).ToString("yyyy-MM-dd"), lastLogin = AdminWebContext.CST(), online = true });
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
            ctx.BannedPlayers[id] = req.Reason;
            var u = UserStore.Instance.GetByPid(id);
            ActionLogStore.Instance.AddLog(id, u?.PlayerName ?? "?", "login", $"管理员封禁 reason={req.Reason}");
            return Results.Json(new { code = 0, msg = "已封禁" });
        });

        app.MapPost("/api/users/{id:long}/unban", (ulong id) => {
            ctx.BannedPlayers.TryRemove(id, out _);
            var u = UserStore.Instance.GetByPid(id);
            ActionLogStore.Instance.AddLog(id, u?.PlayerName ?? "?", "login", $"管理员解封");
            return Results.Json(new { code = 0, msg = "已解封" });
        });
    }
}
