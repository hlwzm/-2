// TradeHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class TradeHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSTradeSearch => HandleSearch(br, seq),
            (uint)MsgId.CSTradeSell => HandleSell(br, seq),
            (uint)MsgId.CSTradeBuy => HandleBuy(br, seq),
            (uint)MsgId.CSTradeCancel => HandleCancel(br, seq),
            (uint)MsgId.CSTradeMyListings => HandleMyListings(br, seq),
            (uint)MsgId.CSTradeClaimGold => HandleClaimGold(br, seq),
            (uint)MsgId.CSTradeClaimItem => HandleClaimItem(br, seq),
            _ => null
        };
    }

    byte[] HandleSearch(BinaryReader br, uint seq)
    {
        br.ReadUInt64(); var kw = br.ReadString(); var q = br.ReadInt32(); var p = br.ReadInt32(); var ps = br.ReadInt32();
        var results = TradeStore.Instance.Search(kw, q, p, ps);
        var total = TradeStore.Instance.SearchCount(kw, q);
        return BuildResponse((uint)MsgId.CSTradeSearch, seq, w =>
        {
            w.Write(0); w.Write(total); w.Write(p); w.Write(results.Count);
            foreach (var r in results) { w.Write(r.Id); w.Write(r.ItemId); w.Write(r.ItemName); w.Write(r.ItemQuality); w.Write(r.Count); w.Write(r.UnitPrice); w.Write(r.SellerName); }
        });
    }

    byte[] HandleSell(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var itemId = br.ReadUInt32(); var count = br.ReadInt32(); var price = br.ReadUInt64();
        var def = ItemStore.Instance.GetDef(itemId); var user = UserStore.Instance.GetByPid(pid);
        if (def == null || user == null) return Error((uint)MsgId.CSTradeSell, seq, 1, "物品或用户不存在");
        var listing = TradeStore.Instance.AddListing(pid, user.PlayerName, itemId, def.Name, def.Quality, count, price);
        ActionLogStore.Instance.AddLog(pid, user.PlayerName, "trade_sell", $"上架 itemId={itemId} name={def.Name} count={count} price={price}");
        return BuildResponse((uint)MsgId.CSTradeSell, seq, w => { w.Write(0); w.Write(listing!.Id); });
    }

    byte[] HandleBuy(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var lid = br.ReadUInt64(); var count = br.ReadInt32();
        var listing = TradeStore.Instance.GetById(lid);
        if (listing == null || listing.Sold) return Error((uint)MsgId.CSTradeBuy, seq, 1, "物品已卖出");
        var cost = listing.UnitPrice * (ulong)count;
        if (!UserStore.Instance.SpendGold(pid, cost)) return Error((uint)MsgId.CSTradeBuy, seq, 2, "金币不足");
        TradeStore.Instance.TryBuy(lid); UserStore.Instance.AddGold(listing.SellerPid, cost);
        var buyer = UserStore.Instance.GetByPid(pid);
        ActionLogStore.Instance.AddLog(pid, buyer?.PlayerName ?? "?", "trade_buy", $"购买 listingId={lid} item={listing.ItemName} count={count} cost={cost}");
        return BuildResponse((uint)MsgId.CSTradeBuy, seq, w => { w.Write(0); w.Write(listing.ItemId); w.Write(count); w.Write(cost); });
    }

    byte[] HandleCancel(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var lid = br.ReadUInt64(); var ok = TradeStore.Instance.CancelListing(lid, pid); var u = UserStore.Instance.GetByPid(pid); ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "trade_sell", $"取消上架 listingId={lid}"); return BuildResponse((uint)MsgId.CSTradeCancel, seq, w => { w.Write(ok ? 0 : 1); w.Write(lid); }); }

    byte[] HandleMyListings(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var list = TradeStore.Instance.GetBySeller(pid);
        return BuildResponse((uint)MsgId.CSTradeMyListings, seq, w =>
        {
            w.Write(0); w.Write(list.Count);
            foreach (var l in list) { w.Write(l.Id); w.Write(l.ItemId); w.Write(l.ItemName); w.Write(l.Count); w.Write(l.UnitPrice); w.Write(l.Sold); }
        });
    }

    byte[] HandleClaimGold(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var g = TradeStore.Instance.ClaimGold(pid); if (g > 0) UserStore.Instance.AddGold(pid, g); return BuildResponse((uint)MsgId.CSTradeClaimGold, seq, w => { w.Write(0); w.Write(g); }); }
    byte[] HandleClaimItem(BinaryReader br, uint seq) { br.ReadUInt64(); var lid = br.ReadUInt64(); return BuildResponse((uint)MsgId.CSTradeClaimItem, seq, w => { w.Write(0); w.Write(lid); }); }
}
