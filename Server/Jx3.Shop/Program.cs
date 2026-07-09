#nullable disable
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;

namespace Jx3.Shop;

public class ShopServer : GameServer
{
    public ShopServer() : base("Shop", GameConfig.ShopPort) { }

    protected override Task OnStartAsync()
    {
        ServiceRegistry.RegisterHandler((uint)MsgId.CSShopList, HandleShopList);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSShopBuy, HandleBuy);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSShopRecharge, HandleRecharge);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSShopGiftCode, HandleGiftCode);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSShopMonthlyClaim, HandleMonthlyClaim);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSShopCurrencyExchange, HandleExchange);
        Logger.Info("Shop", "ShopServer ready on port 9006");
        return Task.CompletedTask;
    }

    private Task<byte[]> HandleShopList(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var category = r.ReadUInt32();
        var filtered = category == 0 ? ShopService.Items : ShopService.Items.Where(i => i.Category == category).ToList();
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(filtered.Count);
        foreach (var item in filtered) { w.Write(item.ShopItemId); w.Write(item.Name); w.Write(item.PriceType); w.Write(item.Price); w.Write(item.DailyLimit); w.Write(item.Discount); w.Write(item.Category); }
        return Task.FromResult(ms.ToArray());
    }

    private Task<byte[]> HandleBuy(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64(); var shopItemId = r.ReadUInt32();
        var (code, err) = ShopService.BuyItem(playerId, shopItemId);
        using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
        w.Write(code); w.Write(err ?? "");
        return Task.FromResult(ms.ToArray());
    }

    private Task<byte[]> HandleRecharge(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64(); var tierId = r.ReadUInt32();
        var (code, tb, bonus) = ShopService.Recharge(playerId, tierId);
        using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
        w.Write(code); w.Write(tb); w.Write(bonus);
        return Task.FromResult(ms.ToArray());
    }

    private Task<byte[]> HandleGiftCode(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var pid = r.ReadUInt64(); var code = r.ReadString();
        var (result, msg) = ShopService.UseGiftCode(pid, code);
        using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
        w.Write(result); w.Write(msg ?? "");
        return Task.FromResult(ms.ToArray());
    }

    private Task<byte[]> HandleMonthlyClaim(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var pid = r.ReadUInt64();
        var ok = ShopService.ClaimMonthly(pid);
        var (days, claimed) = ShopService.GetMonthlyInfo(pid);
        using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
        w.Write(ok ? 0 : 1); w.Write(days); w.Write(claimed);
        return Task.FromResult(ms.ToArray());
    }

    private Task<byte[]> HandleExchange(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var pid = r.ReadUInt64(); var amount = r.ReadUInt64();
        var (code, gold, fee, newTongbao, err) = ShopService.ExchangeCurrency(pid, amount);
        using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
        if (code == 0) { w.Write(0); w.Write(gold); w.Write(fee); w.Write((uint)newTongbao); }
        else { w.Write(code); w.Write(err ?? ""); }
        return Task.FromResult(ms.ToArray());
    }
}

public class Program { public static async Task Main() { GameConfigLoader.Load(); await new ShopServer().StartAsync(); } }