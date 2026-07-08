#nullable disable
using System.Text.Json;
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;
using Jx3.Trade.Models;

namespace Jx3.Trade;

public class TradeServer : GameServer
{
    private readonly TradeService _tradeService = new();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public TradeServer() : base("Trade", 9003) { }

    protected override Task OnStartAsync()
    {
        Logger.Info("Trade", "Trade service initializing...");

        // 注册消息处理器
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTradeSell, HandleSell);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTradeBuy, HandleBuy);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTradeSearch, HandleSearch);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTradeMyListings, HandleMyListings);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTradeCancel, HandleCancel);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTradeClaimGold, HandleClaimGold);
        // CSTradeClaimItem 使用4014 (协议扩展, Jx3.Common枚举暂未包含)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTradeClaimItem, HandleClaimItem);

        // 启动过期检查定时器
        _ = StartExpiryChecker();

        Logger.Info("Trade", "Trade service ready");
        return Task.CompletedTask;
    }

    // ========== 消息处理 ==========

    private async Task<byte[]?> HandleSell(byte[] body)
    {
        var req = Deserialize<TradeSellRequest>(body);
        if (req == null) return ErrorResponse("参数解析失败");

        var result = await _tradeService.SellAsync(req);
        return Serialize(result);
    }

    private async Task<byte[]?> HandleBuy(byte[] body)
    {
        var req = Deserialize<TradeBuyRequest>(body);
        if (req == null) return ErrorResponse("参数解析失败");

        var result = await _tradeService.BuyAsync(req);
        return Serialize(result);
    }

    private async Task<byte[]?> HandleSearch(byte[] body)
    {
        var req = Deserialize<TradeSearchRequest>(body);
        if (req == null) return ErrorResponse("参数解析失败");

        var result = await _tradeService.SearchAsync(req);
        return Serialize(result);
    }

    private async Task<byte[]?> HandleMyListings(byte[] body)
    {
        var req = Deserialize<TradeMyListingsRequest>(body);
        if (req == null) return ErrorResponse("参数解析失败");

        var result = await _tradeService.MyListingsAsync(req.PlayerId);
        return Serialize(result);
    }

    private async Task<byte[]?> HandleCancel(byte[] body)
    {
        var req = Deserialize<TradeCancelRequest>(body);
        if (req == null) return ErrorResponse("参数解析失败");

        var result = await _tradeService.CancelAsync(req);
        return Serialize(result);
    }

    private async Task<byte[]?> HandleClaimGold(byte[] body)
    {
        var req = Deserialize<TradeClaimGoldRequest>(body);
        if (req == null) return ErrorResponse("参数解析失败");

        var result = await _tradeService.ClaimGoldAsync(req.PlayerId);
        return Serialize(result);
    }

    private async Task<byte[]?> HandleClaimItem(byte[] body)
    {
        var req = Deserialize<TradeClaimItemRequest>(body);
        if (req == null) return ErrorResponse("参数解析失败");

        var result = await _tradeService.ClaimItemAsync(req);
        return Serialize(result);
    }

    // ========== 过期检查 ==========

    private async Task StartExpiryChecker()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                await _tradeService.ExpireAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Trade", $"过期检查循环异常: {ex.Message}");
            }
        }
    }

    // ========== 序列化工具 ==========

    private static T? Deserialize<T>(byte[] data) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(data, JsonOpts);
        }
        catch (Exception ex)
        {
            Logger.Error("Trade", $"Deserialize {typeof(T).Name} failed: {ex.Message}");
            return null;
        }
    }

    private static byte[] Serialize<T>(T obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, JsonOpts);
    }

    private static byte[] ErrorResponse(string msg)
    {
        return Serialize(new { code = -1, err_msg = msg });
    }
}

public class Program
{
    public static async Task Main()
    {
        await new TradeServer().StartAsync();
    }
}