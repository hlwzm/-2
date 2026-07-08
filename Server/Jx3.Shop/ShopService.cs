using Jx3.Common.Utils;

namespace Jx3.Shop;

public class ShopItem
{
    public uint ShopItemId;
    public string Name = "";
    public uint ItemId;
    public uint ItemCount = 1;
    public uint PriceType;
    public ulong Price;
    public int Stock = -1;
    public int DailyLimit = -1;
    public int WeeklyLimit = -1;
    public float Discount = 1.0f;
    public uint Category;
}

public class RechargeTier
{
    public uint Id;
    public uint RmbAmount;
    public uint Tongbao;
    public uint Bonus;
    public bool FirstDouble = true;
}

/// <summary>货币兑换配置</summary>
public class CurrencyExchangeConfig
{
    public float Rate = 100f;       // 1通宝=100金币
    public ulong MinExchange = 10;
    public ulong MaxExchange = 10000;
    public float FeeRate = 0.02f;   // 2%手续费
}

public static class ShopService
{
    public static readonly List<ShopItem> Items = new()
    {
        new() { ShopItemId = 1001, Name = "新手礼包", ItemId = 3001, ItemCount = 1, PriceType = 1, Price = 1, DailyLimit = 1, Category = 1 },
        new() { ShopItemId = 1002, Name = "周限礼包", ItemId = 3002, ItemCount = 1, PriceType = 1, Price = 68, WeeklyLimit = 1, Category = 1 },
        new() { ShopItemId = 2001, Name = "青花瓷时装箱", ItemId = 4001, ItemCount = 1, PriceType = 2, Price = 288, Category = 2 },
        new() { ShopItemId = 2002, Name = "竹林幽客时装", ItemId = 4002, ItemCount = 1, PriceType = 2, Price = 388, Category = 2 },
        new() { ShopItemId = 3001, Name = "体力药剂x10", ItemId = 5001, ItemCount = 10, PriceType = 1, Price = 50, DailyLimit = 3, Category = 3 },
        new() { ShopItemId = 3002, Name = "经验药水x5", ItemId = 5002, ItemCount = 5, PriceType = 0, Price = 3000, Category = 3 },
        new() { ShopItemId = 3003, Name = "英雄碎片自选箱", ItemId = 5003, ItemCount = 1, PriceType = 1, Price = 128, WeeklyLimit = 2, Category = 3 },
        new() { ShopItemId = 3004, Name = "精炼石x10", ItemId = 5004, ItemCount = 10, PriceType = 1, Price = 80, DailyLimit = 5, Category = 3 },
        new() { ShopItemId = 4001, Name = "每日福袋", ItemId = 6001, ItemCount = 1, PriceType = 2, Price = 6, DailyLimit = 1, Category = 4 },
        new() { ShopItemId = 4002, Name = "周卡礼包", ItemId = 6002, ItemCount = 1, PriceType = 2, Price = 128, WeeklyLimit = 1, Category = 4 },
        new() { ShopItemId = 5001, Name = "月卡", ItemId = 7001, ItemCount = 1, PriceType = 2, Price = 300, DailyLimit = 1, Category = 5 },
    };

    public static readonly List<RechargeTier> RechargeTiers = new()
    {
        new() { Id = 1, RmbAmount = 6, Tongbao = 60, Bonus = 6 },
        new() { Id = 2, RmbAmount = 30, Tongbao = 300, Bonus = 30 },
        new() { Id = 3, RmbAmount = 98, Tongbao = 980, Bonus = 98 },
        new() { Id = 4, RmbAmount = 198, Tongbao = 1980, Bonus = 198 },
        new() { Id = 5, RmbAmount = 328, Tongbao = 3280, Bonus = 328 },
        new() { Id = 6, RmbAmount = 648, Tongbao = 6480, Bonus = 648 },
    };

    /// <summary>兑换配置(通宝→金币)</summary>
    public static readonly CurrencyExchangeConfig Exchange = new();

    public static readonly Dictionary<string, List<(uint ItemId, uint Count)>> GiftCodes = new()
    {
        { "JX3WELCOME", new() { (3001, 1), (0, 5000) } },
        { "JX3VIP888", new() { (5002, 5), (3001, 3) } },
        { "JX3LAUNCH", new() { (8001, 1), (0, 10000) } },
    };
    private static readonly HashSet<string> _usedCodes = new();

    public static (int code, string? err) BuyItem(ulong playerId, uint shopItemId)
    {
        var item = Items.FirstOrDefault(i => i.ShopItemId == shopItemId);
        if (item == null) return (1, "商品不存在");
        Logger.Info("Shop", $"Player {playerId} bought item {shopItemId}");
        return (0, null);
    }

    public static (int code, uint tongbao, uint bonus) Recharge(ulong playerId, uint tierId)
    {
        var tier = RechargeTiers.FirstOrDefault(t => t.Id == tierId);
        if (tier == null) return (1, 0, 0);
        Logger.Info("Shop", $"Player {playerId} recharged {tier.RmbAmount}元");
        return (0, tier.Tongbao, tier.Bonus);
    }

    public static (int code, string? msg) UseGiftCode(ulong playerId, string code)
    {
        var upper = code.ToUpperInvariant();
        if (!GiftCodes.ContainsKey(upper)) return (1, "兑换码无效");
        if (_usedCodes.Contains(upper)) return (2, "兑换码已使用");
        _usedCodes.Add(upper);
        Logger.Info("Shop", $"Player {playerId} used code: {upper}");
        return (0, "兑换成功");
    }

    public static (int remainDays, bool claimedToday) GetMonthlyInfo(ulong playerId) => (0, false);
    public static bool ClaimMonthly(ulong playerId) { Logger.Info("Shop", $"Player {playerId} claimed monthly"); return true; }

    /// <summary>通宝→金币兑换</summary>
    public static (int code, ulong gold, ulong fee, ulong newTongbao, string? err) ExchangeCurrency(ulong playerId, ulong tongbao)
    {
        if (tongbao < Exchange.MinExchange) return (1, 0, 0, 0, $"最低兑换{Exchange.MinExchange}通宝");
        if (tongbao > Exchange.MaxExchange) return (2, 0, 0, 0, $"单次最多兑换{Exchange.MaxExchange}通宝");

        ulong gold = (ulong)(tongbao * Exchange.Rate);
        ulong fee = (ulong)(gold * Exchange.FeeRate);
        ulong netGold = gold - fee;

        // 实际项目中: 从DB扣通宝、加金币, 记流水
        Logger.Info("Shop", $"Player {playerId} exchanged {tongbao}通宝→{netGold}金币(fee={fee})");
        return (0, netGold, fee, 0, null);
    }
}