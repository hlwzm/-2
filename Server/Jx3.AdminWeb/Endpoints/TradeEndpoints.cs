using Jx3.MockServer.Data;

namespace Jx3.AdminWeb.Endpoints;

static class TradeEndpoints
{
    public static void Map(WebApplication app, AdminWebContext ctx)
    {
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
                    var idx = ctx.Rng.Next(itemNames.Length); var sellerId = 20000UL + (ulong)ctx.Rng.Next(1, 100);
                    if (search != "" && !itemNames[idx].Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                    if (quality > 0 && qualities[idx] != quality) continue;
                    demo.Add(new { id = (ulong)(i + 1), sellerId, sellerName = "侠客_" + sellerId, itemName = itemNames[idx], itemQuality = qualities[idx], category = "", count = ctx.Rng.Next(1, 20), unitPrice = (ulong)ctx.Rng.Next(100, 50000), totalPrice = (ulong)(ctx.Rng.Next(100, 50000) * ctx.Rng.Next(1, 20)), remainingHours = ctx.Rng.Next(1, 48), listedAt = DateTime.UtcNow.AddHours(-ctx.Rng.Next(0, 72)).AddHours(8).ToString("yyyy-MM-dd HH:mm") });
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
    }
}
