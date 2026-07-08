using System.Text.Json.Serialization;

namespace Jx3.Trade.Models;

/// <summary>拍卖行物品数据模型</summary>
public class AuctionItem
{
    public ulong AuctionId { get; set; }
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public ulong BagItemId { get; set; }
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Category { get; set; }
    public int Quality { get; set; }
    public int Level { get; set; }
    public ulong Price { get; set; }
    public int Duration { get; set; }
    /// <summary>1=上架 2=售出 3=下架 4=过期</summary>
    public int Status { get; set; } = 1;
    public DateTime CreateTime { get; set; }
    public DateTime? SoldTime { get; set; }
    public ulong BuyerId { get; set; }
    public ulong Fee { get; set; }
    public ulong SellerIncome { get; set; }
    public string ItemDetail { get; set; } = "";

    [JsonIgnore]
    public int RemainHours => Math.Max(0, Duration - (int)(DateTime.Now - CreateTime).TotalHours);
}

// ========== 请求消息体 ==========

public class TradeSellRequest
{
    public ulong PlayerId { get; set; }
    public ulong BagItemId { get; set; }
    public ulong Price { get; set; }
    public int Duration { get; set; } = 24;
    public int Category { get; set; }
}

public class TradeBuyRequest
{
    public ulong PlayerId { get; set; }
    public ulong AuctionId { get; set; }
    public int Count { get; set; } = 1;
}

public class TradeSearchRequest
{
    public string Keyword { get; set; } = "";
    public int Category { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public int MinQuality { get; set; }
    public ulong MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class TradeCancelRequest
{
    public ulong PlayerId { get; set; }
    public ulong AuctionId { get; set; }
}

public class TradeMyListingsRequest
{
    public ulong PlayerId { get; set; }
}

public class TradeClaimGoldRequest
{
    public ulong PlayerId { get; set; }
}

public class TradeClaimItemRequest
{
    public ulong PlayerId { get; set; }
    public ulong AuctionId { get; set; }
}

// ========== 响应消息体 ==========

public class TradeSellResult
{
    public int Code { get; set; }
    public ulong AuctionId { get; set; }
    public ulong Fee { get; set; }
    public string ErrMsg { get; set; } = "";
}

public class TradeBuyResult
{
    public int Code { get; set; }
    public ulong CostGold { get; set; }
    public ulong Fee { get; set; }
    public ulong BagItemId { get; set; }
    public string ErrMsg { get; set; } = "";
}

public class TradeSearchResult
{
    public int Code { get; set; }
    public List<AuctionItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public string ErrMsg { get; set; } = "";
}

public class TradeCancelResult
{
    public int Code { get; set; }
    public string ErrMsg { get; set; } = "";
}

public class TradeMyListingsResult
{
    public int Code { get; set; }
    public List<AuctionItem> Items { get; set; } = new();
    public string ErrMsg { get; set; } = "";
}

/// <summary>通知卖家物品已售出</summary>
public class TradeItemSoldNotice
{
    public ulong AuctionId { get; set; }
    public ulong Price { get; set; }
    public ulong Fee { get; set; }
    public ulong Income { get; set; }
}

public class TradeClaimResult
{
    public int Code { get; set; }
    public ulong Amount { get; set; }
    public string ErrMsg { get; set; } = "";
}