using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class ShopHandler : HandlerBase, IHandler
{
    static readonly (uint id, string name, uint price, int q, string cur)[] ITEMS = [
        (1, "培元丹", 1000, 2, "gold"), (2, "大还丹", 5000, 3, "gold"),
        (3, "灵玉", 100, 4, "gem"), (4, "玄铁", 800, 3, "gold"),
        (5, "天蚕丝", 1200, 3, "gold"), (6, "龙鳞", 500, 5, "gem"),
        (7, "凤羽", 1000, 5, "gem"), (8, "星雕铁", 2000, 4, "gold"),
        (9, "九天玄铁", 5000, 5, "gem"), (10, "凤凰翎", 3000, 5, "gem")];

    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSShopList => HandleList(br, seq),
            (uint)MsgId.CSShopBuy => HandleBuy(br, seq),
            (uint)MsgId.CSShopRecharge => HandleRecharge(br, seq),
            (uint)MsgId.CSShopGiftCode => HandleGift(br, seq),
            (uint)MsgId.CSShopMonthlyClaim => HandleMonthly(br, seq),
            (uint)MsgId.CSShopCurrencyExchange => HandleExchange(br, seq),
            _ => null
        };
    }

    byte[] HandleList(BinaryReader br, uint seq)
    {
        br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSShopList, seq, w =>
        {
            w.Write(0); w.Write(ITEMS.Length);
            foreach (var i in ITEMS) { w.Write(i.id); w.Write(i.name); w.Write(i.price); w.Write(i.q); w.Write(i.cur); }
        });
    }

    byte[] HandleBuy(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var sid = br.ReadUInt32(); var cnt = br.ReadInt32();
        var item = ITEMS.FirstOrDefault(s => s.id == sid);
        if (item.id == 0) return Error((uint)MsgId.CSShopBuy, seq, 1, "商品不存在");
        var cost = item.price * (uint)cnt;
        if (!UserStore.Instance.SpendGold(pid, cost)) return Error((uint)MsgId.CSShopBuy, seq, 2, "货币不足");
        ItemStore.Instance.AddItem(pid, sid, cnt);
        return BuildResponse((uint)MsgId.CSShopBuy, seq, w => { w.Write(0); w.Write(sid); w.Write(cnt); w.Write(cost); });
    }

    byte[] HandleRecharge(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var amt = br.ReadUInt32(); UserStore.Instance.AddGold(pid, amt); return BuildResponse((uint)MsgId.CSShopRecharge, seq, w => { w.Write(0); w.Write(amt); w.Write(UserStore.Instance.GetByPid(pid)?.Gold ?? 0); }); }
    byte[] HandleGift(BinaryReader br, uint seq) { br.ReadUInt64(); br.ReadString(); return BuildResponse((uint)MsgId.CSShopGiftCode, seq, w => { w.Write(0); w.Write(1000UL); w.Write(100UL); }); }
    byte[] HandleMonthly(BinaryReader br, uint seq) { br.ReadUInt64(); return BuildResponse((uint)MsgId.CSShopMonthlyClaim, seq, w => { w.Write(0); w.Write(true); w.Write(2000UL); }); }
    byte[] HandleExchange(BinaryReader br, uint seq) { var pid2 = br.ReadUInt64(); br.ReadString(); br.ReadString(); var amt = br.ReadUInt64(); UserStore.Instance.AddGold(pid2, amt * 10); return BuildResponse((uint)MsgId.CSShopCurrencyExchange, seq, w => { w.Write(0); w.Write(amt * 10); }); }
}
