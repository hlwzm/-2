using Jx3.MockServer.Data;

namespace Jx3.AdminWeb.Endpoints;

static class MailEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
        app.MapPost("/api/mail/send", (SendMailRequest req) => {
            var ids = req.PlayerIds?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            foreach (var idStr in ids) {
                if (ulong.TryParse(idStr, out var pid)) {
                    var u = UserStore.Instance.GetByPid(pid);
                    ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "侠客_" + pid, "item", $"管理员发送邮件 title={req.Title} gold={req.Gold}");
                }
            }
            lock (ctx.SentMails) { ctx.SentMails.Add(new MailRecord { Id = ctx.NextMailId++, Title = req.Title ?? "", Content = req.Content ?? "", PlayerIds = req.PlayerIds ?? "", Gold = req.Gold, ItemId = req.ItemId, ItemCount = req.ItemCount, SentAt = DateTime.UtcNow }); }
            return Results.Json(new { code = 0, msg = $"邮件已发送给 {ids.Length} 个玩家", count = ids.Length });
        });

        app.MapGet("/api/mail/history", (int page = 1, int pageSize = 20) => {
            if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 20;
            lock (ctx.SentMails) {
                var total = ctx.SentMails.Count;
                var items = ctx.SentMails.OrderByDescending(m => m.SentAt).Skip((page - 1) * pageSize).Take(pageSize).Select(m => new { m.Id, m.Title, m.Content, m.PlayerIds, m.Gold, m.ItemId, m.ItemCount, sentAt = m.SentAt.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss") });
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
    }
}
