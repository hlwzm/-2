namespace Jx3.AdminWeb.Endpoints;

static class ServerEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
        app.MapGet("/api/server/stats", () => Results.Json(new {
            connections = ctx.Rng.Next(20, 80), memoryMB = ctx.Rng.Next(180, 400),
            cpuPercent = Math.Round(ctx.Rng.NextDouble() * 30 + 5, 1),
            threadCount = ctx.Rng.Next(4, 16), msgPerSecond = ctx.Rng.Next(50, 300),
            uptimeHours = Math.Round((DateTime.UtcNow - new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc)).TotalHours, 1),
            totalPackets = ctx.Rng.Next(10000, 99999), avgLatency = ctx.Rng.Next(5, 50)
        }));

        app.MapGet("/api/backup", () => Results.Json(new {
            code = 0, msg = "备份成功",
            file = $"backup_{DateTime.UtcNow.AddHours(8):yyyyMMdd_HHmmss}.zip",
            size = $"{ctx.Rng.Next(50, 500)}MB", timestamp = AdminWebContext.CST()
        }));

        app.MapGet("/api/dungeon/stats", () => {
            string[] dungeons = ["风雨稻香村", "天子峰", "荻花宫", "战宝迦兰", "持国天王殿"];
            string[] difficulties = ["普通", "英雄", "挑战"];
            var stats = new List<object>();
            foreach (var d in dungeons) {
                foreach (var diff in difficulties) stats.Add(new {
                    dungeonName = d, difficulty = diff,
                    totalClears = ctx.Rng.Next(10, 500), avgClearTime = ctx.Rng.Next(5, 45) + "分钟",
                    totalDeaths = ctx.Rng.Next(0, 200), totalPlayers = ctx.Rng.Next(50, 2000),
                    dropRate = (ctx.Rng.NextDouble() * 0.3 + 0.1).ToString("0.0%")
                });
            }
            return Results.Json(new { dungeons = stats, totalDungeons = dungeons.Length });
        });

        app.MapPost("/api/shutdown", () => {
            _ = Task.Run(async () => { await Task.Delay(1000); Environment.Exit(0); });
            return Results.Json(new { code = 0, msg = "服务器将在1秒后关闭" });
        });
    }
}
