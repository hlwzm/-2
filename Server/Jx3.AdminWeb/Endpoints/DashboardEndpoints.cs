using Jx3.MockServer.Data;

namespace Jx3.AdminWeb.Endpoints;

static class DashboardEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
        app.MapGet("/api/dashboard", () => {
            var onlineCount = ctx.Rng.Next(20, 60);
            var allListings = TradeStore.Instance.Search("", 0, 1, 99999);
            var stats = ActionLogStore.Instance.GetTodayStats();
            return Results.Json(new {
                onlinePlayers = onlineCount, totalUsers = 180,
                totalTrades = allListings.Count, tradeVolume = AdminWebContext.SumGold(allListings),
                dungeonClears = ctx.Rng.Next(100, 500), bannedCount = ctx.BannedPlayers.Count,
                todayLogins = stats.loginCount, todayChats = stats.chatCount,
                todayTrades = stats.tradeCount, todayDungeons = stats.dungeonCount,
                todayPvp = stats.pvpCount, todayGuild = stats.guildCount,
                serverTime = AdminWebContext.CST(),
                uptime = (DateTime.UtcNow - new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc)).TotalHours.ToString("F1") + "小时"
            });
        });

        app.MapGet("/api/stats/daily", () => {
            var days = new List<string>(); var dau = new List<int>(); var newUsers = new List<int>();
            var tradeVolume = new List<long>(); var revenue = new List<int>();
            var dungeonCounts = new List<int>(); var pvpCounts = new List<int>();
            for (int i = 6; i >= 0; i--) {
                var d = DateTime.UtcNow.AddDays(-i).AddHours(8);
                days.Add(d.ToString("MM-dd")); dau.Add(ctx.Rng.Next(100, 200));
                newUsers.Add(ctx.Rng.Next(5, 25)); tradeVolume.Add(ctx.Rng.Next(30000, 100000));
                revenue.Add(ctx.Rng.Next(800, 2500)); dungeonCounts.Add(ctx.Rng.Next(20, 100));
                pvpCounts.Add(ctx.Rng.Next(30, 150));
            }
            return Results.Json(new { days, dau, newUsers, tradeVolume, revenue, dungeonCounts, pvpCounts });
        });
    }
}
