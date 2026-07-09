using Jx3.MockServer.Data;

namespace Jx3.AdminWeb.Endpoints;

static class LogEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
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
    }
}
