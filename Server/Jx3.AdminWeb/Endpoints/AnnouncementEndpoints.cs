namespace Jx3.AdminWeb.Endpoints;

static class AnnouncementEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
        app.MapGet("/api/announcements", () => {
            lock (ctx.AnnLock) { return Results.Json(ctx.Announcements.OrderByDescending(a => a.CreatedAt).ToList()); }
        });

        app.MapPost("/api/announcements", (CreateAnnouncement req) => {
            lock (ctx.AnnLock) {
                var a = new Announcement { Id = ctx.NextAnnId++, Title = req.Title, Content = req.Content, CreatedAt = DateTime.UtcNow, Author = "Admin" };
                ctx.Announcements.Add(a);
                return Results.Json(new { code = 0, msg = "公告已创建" });
            }
        });

        app.MapDelete("/api/announcements/{id:long}", (ulong id) => {
            lock (ctx.AnnLock) {
                var r = ctx.Announcements.RemoveAll(a => a.Id == id);
                return Results.Json(new { code = r > 0 ? 0 : 1, msg = r > 0 ? "公告已删除" : "公告不存在" });
            }
        });
    }
}
