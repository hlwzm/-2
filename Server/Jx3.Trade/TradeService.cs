using System.Data;
using System.Text.Json;
using Dapper;
using Jx3.Common.Database;
using Jx3.Common.Utils;
using Jx3.Trade.Models;
using StackExchange.Redis;

namespace Jx3.Trade;

/// <summary>拍卖行交易服务 - 核心经济系统</summary>
public class TradeService
{
    private const string Tag = "Trade";

    // ========== 1. 上架物品 ==========
    public async Task<TradeSellResult> SellAsync(TradeSellRequest req)
    {
        var result = new TradeSellResult { Code = -1 };

        try
        {
            using var db = new DbHelper();

            // 1.1 检查背包物品
            var bagItem = await db.QueryFirstOrDefaultAsync<(ulong bag_id, uint item_id, int count, int bind_type)>(
                "SELECT bag_id, item_id, `count`, bind_type FROM bag_item WHERE bag_id = @BagItemId AND player_id = @PlayerId",
                new { req.BagItemId, req.PlayerId });

            if (bagItem.bag_id == 0)
            {
                result.ErrMsg = "背包中不存在该物品";
                return result;
            }
            if (bagItem.bind_type > 0)
            {
                result.ErrMsg = "该物品已绑定，不可交易";
                return result;
            }

            // 1.2 查询物品模板
            var itemInfo = await db.QueryFirstOrDefaultAsync<(string name, int quality)>(
                "SELECT name, quality FROM item_template WHERE item_id = @ItemId",
                new { ItemId = bagItem.item_id });

            if (string.IsNullOrEmpty(itemInfo.name))
            {
                result.ErrMsg = "物品数据异常";
                return result;
            }

            // 1.3 验证价格和时长
            if (req.Price <= 0)
            {
                result.ErrMsg = "价格必须大于0";
                return result;
            }
            if (req.Duration != 24 && req.Duration != 48)
            {
                result.ErrMsg = "上架时长仅支持24或48小时";
                return result;
            }

            // 1.4 计算上架费(售价 × 1%)
            ulong fee = (ulong)(req.Price * 1 / 100);
            if (fee < 1) fee = 1;

            // 1.5 检查并扣除上架费
            var playerGold = await db.ExecuteScalarAsync<ulong>(
                "SELECT gold FROM player WHERE player_id = @PlayerId", new { req.PlayerId });

            if (playerGold < fee)
            {
                result.ErrMsg = $"金币不足，上架需要{fee}金币";
                return result;
            }

            // 1.6 查询玩家名称
            var playerName = await db.ExecuteScalarAsync<string>(
                "SELECT name FROM player WHERE player_id = @playerId", new { req.PlayerId });
            playerName ??= "未知";

            // 1.7 扣费+删物品+上架 放入事务
            var now = DateTime.Now;
            ulong auctionId = 0;

            await db.ExecuteInTransactionAsync(async (conn, tx) =>
            {
                await conn.ExecuteAsync(
                    "UPDATE player SET gold = gold - @Fee WHERE player_id = @PlayerId",
                    new { Fee = fee, req.PlayerId }, tx);

                await conn.ExecuteAsync(
                    "DELETE FROM bag_item WHERE bag_id = @BagItemId AND player_id = @PlayerId",
                    new { req.BagItemId, req.PlayerId }, tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO auction_item (player_id, player_name, bag_item_id, item_id, category, quality, price, duration, status, create_time, fee)
                    VALUES (@PlayerId, @PlayerName, @BagItemId, @ItemId, @Category, @Quality, @Price, @Duration, 1, @CreateTime, @Fee)",
                    new
                    {
                        req.PlayerId,
                        PlayerName = playerName,
                        req.BagItemId,
                        ItemId = bagItem.item_id,
                        req.Category,
                        Quality = itemInfo.quality,
                        req.Price,
                        req.Duration,
                        CreateTime = now,
                        Fee = fee
                    }, tx);

                auctionId = await conn.ExecuteScalarAsync<ulong>("SELECT LAST_INSERT_ID()", transaction: tx);
            });

            // 1.9 写入 Redis
            using var redis = new RedisHelper();
            var dbRedis = redis.Db;

            // ZSet: auction:cat:{categoryId}
            await dbRedis.SortedSetAddAsync($"auction:cat:{req.Category}", auctionId, req.Price);

            // Hash: auction:item:{auctionId}
            await dbRedis.HashSetAsync($"auction:item:{auctionId}", [
                new HashEntry("auction_id", auctionId),
                new HashEntry("player_id", req.PlayerId),
                new HashEntry("player_name", playerName),
                new HashEntry("bag_item_id", req.BagItemId),
                new HashEntry("item_id", bagItem.item_id),
                new HashEntry("item_name", itemInfo.name),
                new HashEntry("category", req.Category),
                new HashEntry("quality", itemInfo.quality),
                new HashEntry("level", 0),
                new HashEntry("price", req.Price),
                new HashEntry("duration", req.Duration),
                new HashEntry("status", 1),
                new HashEntry("create_time", now.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("fee", fee)
            ]);

            // 设置过期时间，与拍卖时长一致
            await dbRedis.KeyExpireAsync($"auction:item:{auctionId}", TimeSpan.FromHours(req.Duration));

            Logger.Info(Tag, $"上架成功: auctionId={auctionId}, item={itemInfo.name}, price={req.Price}, fee={fee}");
            result.Code = 0;
            result.AuctionId = auctionId;
            result.Fee = fee;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"上架异常: {ex.Message}");
            result.ErrMsg = $"系统繁忙，请稍后重试 [{ex.Message}]";
        }

        return result;
    }

    // ========== 2. 购买物品 ==========
    public async Task<TradeBuyResult> BuyAsync(TradeBuyRequest req)
    {
        var result = new TradeBuyResult { Code = -1 };

        try
        {
            using var db = new DbHelper();

            // 2.1 查询拍卖物品
            var auction = await db.QueryFirstOrDefaultAsync<(ulong auction_id, ulong player_id, string player_name,
                ulong bag_item_id, uint item_id, int category, int quality, ulong price, int status, ulong fee)>(
                "SELECT auction_id, player_id, player_name, bag_item_id, item_id, category, quality, price, status, fee FROM auction_item WHERE auction_id = @AuctionId",
                new { req.AuctionId });

            if (auction.auction_id == 0)
            {
                result.ErrMsg = "该拍品不存在";
                return result;
            }
            if (auction.status != 1)
            {
                result.ErrMsg = "该物品已售出或下架";
                return result;
            }
            if (auction.player_id == req.PlayerId)
            {
                result.ErrMsg = "不能购买自己的物品";
                return result;
            }

            // 2.2 计算费用
            ulong totalPrice = auction.price * (ulong)req.Count;
            ulong fee = totalPrice * 5 / 100; // 5% 手续费
            ulong sellerIncome = totalPrice - fee;

            // 2.3 检查买家金币
            var buyerGold = await db.ExecuteScalarAsync<ulong>(
                "SELECT gold FROM player WHERE player_id = @PlayerId", new { req.PlayerId });

            if (buyerGold < totalPrice)
            {
                result.ErrMsg = $"金币不足，需要{totalPrice}金币";
                return result;
            }

            var now = DateTime.Now;
            ulong newBagId = 0;

            // 2.4 扣金币+改状态+写日志+加物品 放入事务
            await db.ExecuteInTransactionAsync(async (conn, tx) =>
            {
                await conn.ExecuteAsync(
                    "UPDATE player SET gold = gold - @TotalPrice WHERE player_id = @PlayerId",
                    new { TotalPrice = totalPrice, req.PlayerId }, tx);

                await conn.ExecuteAsync(@"
                    UPDATE auction_item SET status = 2, buyer_id = @BuyerId, sold_time = @SoldTime,
                        fee = @Fee, seller_income = @SellerIncome
                    WHERE auction_id = @AuctionId",
                    new
                    {
                        BuyerId = req.PlayerId,
                        SoldTime = now,
                        Fee = fee,
                        SellerIncome = sellerIncome,
                        req.AuctionId
                    }, tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO trade_log (auction_id, seller_id, buyer_id, item_id, price, fee, seller_income, trade_time)
                    VALUES (@AuctionId, @SellerId, @BuyerId, @ItemId, @Price, @Fee, @SellerIncome, @TradeTime)",
                    new
                    {
                        req.AuctionId,
                        SellerId = auction.player_id,
                        BuyerId = req.PlayerId,
                        ItemId = auction.item_id,
                        Price = totalPrice,
                        Fee = fee,
                        SellerIncome = sellerIncome,
                        TradeTime = now
                    }, tx);

                int nextSlot = await conn.ExecuteScalarAsync<int>(
                    "SELECT COALESCE(MAX(slot_index), 0) + 1 FROM bag_item WHERE player_id = @playerId",
                    new { req.PlayerId }, tx);
                if (nextSlot < 1) nextSlot = 1;

                await conn.ExecuteAsync(@"
                    INSERT INTO bag_item (player_id, item_id, slot_index, `count`, bind_type, create_time)
                    VALUES (@PlayerId, @ItemId, @SlotIndex, @Count, 1, @CreateTime)",
                    new
                    {
                        req.PlayerId,
                        ItemId = auction.item_id,
                        SlotIndex = nextSlot,
                        Count = req.Count,
                        CreateTime = now
                    }, tx);

                newBagId = await conn.ExecuteScalarAsync<ulong>("SELECT LAST_INSERT_ID()", transaction: tx);
            });

            // 2.8 更新 Redis
            using var redis = new RedisHelper();
            var dbRedis = redis.Db;

            // 从ZSet移除
            await dbRedis.SortedSetRemoveAsync($"auction:cat:{auction.category}", req.AuctionId);

            // 更新Hash
            await dbRedis.HashSetAsync($"auction:item:{req.AuctionId}", [
                new HashEntry("status", 2),
                new HashEntry("buyer_id", req.PlayerId),
                new HashEntry("sold_time", now.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("fee", fee),
                new HashEntry("seller_income", sellerIncome)
            ]);

            Logger.Info(Tag, $"购买成功: auctionId={req.AuctionId}, buyer={req.PlayerId}, price={totalPrice}, fee={fee}");

            result.Code = 0;
            result.CostGold = totalPrice;
            result.Fee = fee;
            result.BagItemId = newBagId;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"购买异常: {ex.Message}");
            result.ErrMsg = $"系统繁忙，请稍后重试 [{ex.Message}]";
        }

        return result;
    }

    // ========== 3. 搜索物品 ==========
    public async Task<TradeSearchResult> SearchAsync(TradeSearchRequest req)
    {
        var result = new TradeSearchResult { Code = -1, Page = req.Page };

        try
        {
            // 3.1 先从Redis按分类+价格范围查auctionId列表
            HashSet<ulong>? redisAuctionIds = null;

            if (req.Category > 0)
            {
                using var redis = new RedisHelper();
                var dbRedis = redis.Db;

                var members = await dbRedis.SortedSetRangeByScoreAsync(
                    $"auction:cat:{req.Category}",
                    start: 0,
                    stop: req.MaxPrice > 0 ? req.MaxPrice : double.PositiveInfinity,
                    order: Order.Ascending);

                if (members != null && members.Length > 0)
                {
                    redisAuctionIds = new HashSet<ulong>();
                    foreach (var m in members)
                    {
                        if (m.TryParse(out long idLong))
                            redisAuctionIds.Add((ulong)idLong);
                    }
                }
            }

            // 3.2 MySQL查询
            using var db = new DbHelper();

            var sql = new System.Text.StringBuilder(@"
                SELECT a.auction_id AS AuctionId, a.player_id AS PlayerId, a.player_name AS PlayerName,
                       a.bag_item_id AS BagItemId, a.item_id AS ItemId, COALESCE(t.name, '') AS ItemName,
                       a.category AS Category, a.quality AS Quality, 0 AS Level,
                       a.price AS Price, a.duration AS Duration, a.status AS Status,
                       a.create_time AS CreateTime, a.sold_time AS SoldTime,
                       a.buyer_id AS BuyerId, a.fee AS Fee, a.seller_income AS SellerIncome,
                       '' AS ItemDetail
                FROM auction_item a
                LEFT JOIN item_template t ON a.item_id = t.item_id
                WHERE a.status = 1");

            var countSql = new System.Text.StringBuilder(
                "SELECT COUNT(*) FROM auction_item a LEFT JOIN item_template t ON a.item_id = t.item_id WHERE a.status = 1");

            var parameters = new Dictionary<string, object>();

            // 关键词搜索（物品名称）
            if (!string.IsNullOrWhiteSpace(req.Keyword))
            {
                var kw = $"%{req.Keyword}%";
                sql.Append(" AND t.name LIKE @Keyword");
                countSql.Append(" AND t.name LIKE @Keyword");
                parameters["Keyword"] = kw;
            }

            // 分类筛选
            if (req.Category > 0)
            {
                if (redisAuctionIds != null && redisAuctionIds.Count > 0)
                {
                    sql.Append(" AND a.auction_id IN @AuctionIds");
                    countSql.Append(" AND a.auction_id IN @AuctionIds");
                    parameters["AuctionIds"] = redisAuctionIds;
                }
                else if (redisAuctionIds != null)
                {
                    // Redis有该分类但无结果，直接返回空
                    result.Code = 0;
                    result.Items = new List<AuctionItem>();
                    result.TotalCount = 0;
                    return result;
                }
                else
                {
                    sql.Append(" AND a.category = @Category");
                    countSql.Append(" AND a.category = @Category");
                    parameters["Category"] = req.Category;
                }
            }

            // 品质筛选
            if (req.MinQuality > 0)
            {
                sql.Append(" AND a.quality >= @MinQuality");
                countSql.Append(" AND a.quality >= @MinQuality");
                parameters["MinQuality"] = req.MinQuality;
            }

            // 价格上限
            if (req.MaxPrice > 0)
            {
                sql.Append(" AND a.price <= @MaxPrice");
                countSql.Append(" AND a.price <= @MaxPrice");
                parameters["MaxPrice"] = req.MaxPrice;
            }

            // 等级筛选（按quality近似替代）
            if (req.MinLevel > 0)
            {
                sql.Append(" AND a.quality >= @MinLevel2");
                countSql.Append(" AND a.quality >= @MinLevel2");
                parameters["MinLevel2"] = req.MinLevel;
            }

            // 总条数
            int totalCount = await db.ExecuteScalarAsync<int>(countSql.ToString(), parameters);
            result.TotalCount = totalCount;

            // 分页
            int offset = (req.Page - 1) * req.PageSize;
            sql.Append(" ORDER BY a.create_time DESC LIMIT @Limit OFFSET @Offset");
            parameters["Limit"] = req.PageSize;
            parameters["Offset"] = offset;

            var items = await db.QueryAsync<AuctionItem>(sql.ToString(), parameters);
            foreach (var item in items)
            {
                item.ItemName ??= "";
                item.PlayerName ??= "";
            }

            result.Code = 0;
            result.Items = items.ToList();
            result.Page = req.Page;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"搜索异常: {ex.Message}");
            result.ErrMsg = $"搜索失败 [{ex.Message}]";
        }

        return result;
    }

    // ========== 4. 我的上架 ==========
    public async Task<TradeMyListingsResult> MyListingsAsync(ulong playerId)
    {
        var result = new TradeMyListingsResult { Code = -1 };

        try
        {
            using var db = new DbHelper();

            var items = await db.QueryAsync<AuctionItem>(@"
                SELECT a.auction_id AS AuctionId, a.player_id AS PlayerId, a.player_name AS PlayerName,
                       a.bag_item_id AS BagItemId, a.item_id AS ItemId, COALESCE(t.name, '') AS ItemName,
                       a.category AS Category, a.quality AS Quality, 0 AS Level,
                       a.price AS Price, a.duration AS Duration, a.status AS Status,
                       a.create_time AS CreateTime, a.sold_time AS SoldTime,
                       a.buyer_id AS BuyerId, a.fee AS Fee, a.seller_income AS SellerIncome,
                       '' AS ItemDetail
                FROM auction_item a
                LEFT JOIN item_template t ON a.item_id = t.item_id
                WHERE a.player_id = @PlayerId
                ORDER BY a.create_time DESC",
                new { PlayerId = playerId });

            foreach (var item in items)
            {
                item.ItemName ??= "";
                item.PlayerName ??= "";
            }

            result.Code = 0;
            result.Items = items.ToList();
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"查询上架列表异常: {ex.Message}");
            result.ErrMsg = $"查询失败 [{ex.Message}]";
        }

        return result;
    }

    // ========== 5. 取消上架 ==========
    public async Task<TradeCancelResult> CancelAsync(TradeCancelRequest req)
    {
        var result = new TradeCancelResult { Code = -1 };

        try
        {
            using var db = new DbHelper();

            // 5.1 查询拍卖物品
            var auction = await db.QueryFirstOrDefaultAsync<(ulong auction_id, ulong player_id, uint item_id, ulong bag_item_id, int category, int status)>(
                "SELECT auction_id, player_id, item_id, bag_item_id, category, status FROM auction_item WHERE auction_id = @AuctionId",
                new { req.AuctionId });

            if (auction.auction_id == 0)
            {
                result.ErrMsg = "该拍品不存在";
                return result;
            }
            if (auction.player_id != req.PlayerId)
            {
                result.ErrMsg = "只能取消自己的上架";
                return result;
            }
            if (auction.status != 1)
            {
                result.ErrMsg = "该物品已售出或已下架";
                return result;
            }

            var now = DateTime.Now;

            // 5.2 更新状态为已下架(3)
            await db.ExecuteAsync(
                "UPDATE auction_item SET status = 3 WHERE auction_id = @AuctionId",
                new { req.AuctionId });

            // 5.3 物品退回背包
            int nextSlot = await db.ExecuteScalarAsync<int>(
                "SELECT COALESCE(MAX(slot_index), 0) + 1 FROM bag_item WHERE player_id = @PlayerId",
                new { req.PlayerId });
            if (nextSlot < 1) nextSlot = 1;

            await db.ExecuteAsync(@"
                INSERT INTO bag_item (player_id, item_id, slot_index, `count`, bind_type, create_time)
                VALUES (@PlayerId, @ItemId, @SlotIndex, 1, 0, @CreateTime)",
                new
                {
                    req.PlayerId,
                    ItemId = auction.item_id,
                    SlotIndex = nextSlot,
                    CreateTime = now
                });

            // 5.4 更新 Redis
            using var redis = new RedisHelper();
            var dbRedis = redis.Db;

            await dbRedis.SortedSetRemoveAsync($"auction:cat:{auction.category}", req.AuctionId);
            await dbRedis.HashSetAsync($"auction:item:{req.AuctionId}", "status", 3);

            Logger.Info(Tag, $"取消上架: auctionId={req.AuctionId}");
            result.Code = 0;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"取消上架异常: {ex.Message}");
            result.ErrMsg = $"取消失败 [{ex.Message}]";
        }

        return result;
    }

    // ========== 6. 领取金币 ==========
    public async Task<TradeClaimResult> ClaimGoldAsync(ulong playerId)
    {
        var result = new TradeClaimResult { Code = -1 };

        try
        {
            using var db = new DbHelper();

            // 查询所有未领取的售出收入
            ulong totalIncome = await db.ExecuteScalarAsync<ulong>(@"
                SELECT COALESCE(SUM(seller_income), 0) FROM auction_item
                WHERE player_id = @PlayerId AND status = 2 AND seller_income > 0",
                new { PlayerId = playerId });

            if (totalIncome == 0)
            {
                result.ErrMsg = "没有可领取的金币";
                return result;
            }

            // 发放金币到玩家
            await db.ExecuteAsync(
                "UPDATE player SET gold = gold + @Income WHERE player_id = @PlayerId",
                new { Income = totalIncome, PlayerId = playerId });

            // 标记已领取（将seller_income置0）
            await db.ExecuteAsync(@"
                UPDATE auction_item SET seller_income = 0
                WHERE player_id = @PlayerId AND status = 2 AND seller_income > 0",
                new { PlayerId = playerId });

            Logger.Info(Tag, $"领取金币: playerId={playerId}, amount={totalIncome}");
            result.Code = 0;
            result.Amount = totalIncome;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"领取金币异常: {ex.Message}");
            result.ErrMsg = $"领取失败 [{ex.Message}]";
        }

        return result;
    }

    // ========== 7. 领取物品（下架/过期） ==========
    public async Task<TradeClaimResult> ClaimItemAsync(TradeClaimItemRequest req)
    {
        var result = new TradeClaimResult { Code = -1 };

        try
        {
            using var db = new DbHelper();

            // 查询物品
            var auction = await db.QueryFirstOrDefaultAsync<(ulong auction_id, uint item_id, int status)>(
                "SELECT auction_id, item_id, status FROM auction_item WHERE auction_id = @AuctionId AND player_id = @PlayerId",
                new { req.AuctionId, req.PlayerId });

            if (auction.auction_id == 0)
            {
                result.ErrMsg = "该物品不存在";
                return result;
            }
            if (auction.status != 3 && auction.status != 4) // 3=下架 4=过期
            {
                result.ErrMsg = "该物品状态不是已下架或已过期";
                return result;
            }

            // 物品退回背包
            int nextSlot = await db.ExecuteScalarAsync<int>(
                "SELECT COALESCE(MAX(slot_index), 0) + 1 FROM bag_item WHERE player_id = @PlayerId",
                new { req.PlayerId });
            if (nextSlot < 1) nextSlot = 1;

            await db.ExecuteAsync(@"
                INSERT INTO bag_item (player_id, item_id, slot_index, `count`, bind_type, create_time)
                VALUES (@PlayerId, @ItemId, @SlotIndex, 1, 0, @CreateTime)",
                new
                {
                    req.PlayerId,
                    ItemId = auction.item_id,
                    SlotIndex = nextSlot,
                    CreateTime = DateTime.Now
                });

            // 标记已领取（删除记录）
            await db.ExecuteAsync(
                "DELETE FROM auction_item WHERE auction_id = @AuctionId",
                new { req.AuctionId });

            // 清理 Redis
            using var redis = new RedisHelper();
            await redis.Db.KeyDeleteAsync($"auction:item:{req.AuctionId}");

            Logger.Info(Tag, $"领取物品: auctionId={req.AuctionId}, playerId={req.PlayerId}");
            result.Code = 0;
            result.Amount = 1;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"领取物品异常: {ex.Message}");
            result.ErrMsg = $"领取失败 [{ex.Message}]";
        }

        return result;
    }

    // ========== 8. 检查过期物品（定时任务调用） ==========
    public async Task<int> ExpireAsync()
    {
        try
        {
            using var db = new DbHelper();

            var expired = await db.QueryAsync<(ulong auction_id, int category)>(
                "SELECT auction_id, category FROM auction_item WHERE status = 1 AND create_time < DATE_SUB(NOW(), INTERVAL duration HOUR)");

            int count = 0;
            foreach (var item in expired)
            {
                await db.ExecuteAsync(
                    "UPDATE auction_item SET status = 4 WHERE auction_id = @AuctionId",
                    new { item.auction_id });

                using var redis = new RedisHelper();
                await redis.Db.SortedSetRemoveAsync($"auction:cat:{item.category}", item.auction_id);
                await redis.Db.HashSetAsync($"auction:item:{item.auction_id}", "status", 4);

                count++;
            }

            if (count > 0)
                Logger.Info(Tag, $"过期处理: {count}件物品已过期");

            return count;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"过期检查异常: {ex.Message}");
            return -1;
        }
    }
}