using Jx3.MockServer.Data;

namespace Jx3.AdminWeb.Endpoints;

static class ReportEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
        app.MapGet("/api/reports", (int page = 1, int pageSize = 20) => {
            if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 20;
            var total = ctx.ReportStore.Count;
            var items = ctx.ReportStore.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Results.Json(new { total, page, pageSize, items });
        });

        app.MapPost("/api/reports/{id:long}/action", (ulong id, ReportActionRequest req) => {
            var report = ctx.ReportStore.FirstOrDefault(r => r.Id == id);
            if (report == null) return Results.Json(new { code = 1, msg = "举报不存在" });
            report.Status = req.Action switch { "warn" => "warned", "ban" => "resolved", "dismiss" => "dismissed", _ => report.Status };
            report.ProcessedBy = "Admin"; report.ProcessedAt = AdminWebContext.CST();
            ActionLogStore.Instance.AddLog(0, "Admin", "item", $"处理举报 id={id} action={req.Action} reason={req.Reason}");
            return Results.Json(new { code = 0, msg = $"举报已处理: {req.Action}" });
        });
    }
}
