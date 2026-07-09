using Jx3.Common.Protocol;

namespace Jx3.MockServer.Handlers;

public class DungeonHandler : HandlerBase, IHandler
{
    static readonly string[] DUNGEONS = ["风雨稻香村", "天子峰", "莣花宫", "战宝迦兰", "持国天王殿"];

    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSDungeonList => HandleList(br, seq),
            (uint)MsgId.CSDungeonEnter => HandleEnter(br, seq),
            (uint)MsgId.CSDungeonDifficulty => HandleDifficulty(br, seq),
            (uint)MsgId.CSDungeonLeave => Simple(br, seq, (uint)MsgId.CSDungeonLeave),
            (uint)MsgId.CSDungeonLoot => HandleLoot(br, seq),
            (uint)MsgId.CSDungeonRoll => HandleRoll(br, seq),
            (uint)MsgId.CSDungeonRevive => Simple(br, seq, (uint)MsgId.CSDungeonRevive),
            _ => null
        };
    }

    byte[] Simple(BinaryReader br, uint seq, uint msgId) { br.ReadUInt64(); return BuildResponse(msgId, seq, w => w.Write(0)); }

    byte[] HandleList(BinaryReader br, uint seq)
    {
        br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSDungeonList, seq, w =>
        {
            w.Write(0); w.Write(DUNGEONS.Length);
            for (int i = 0; i < DUNGEONS.Length; i++) { w.Write((uint)(i + 1)); w.Write(DUNGEONS[i]); w.Write(_rng.Next(10, 50)); w.Write(i % 3 + 1); w.Write(false); }
        });
    }

    byte[] HandleEnter(BinaryReader br, uint seq)
    {
        br.ReadUInt64(); var did = br.ReadUInt32(); var diff = br.ReadInt32();
        return BuildResponse((uint)MsgId.CSDungeonEnter, seq, w =>
        {
            w.Write(0); w.Write(did); w.Write(DUNGEONS[did - 1]); w.Write(diff); w.Write((uint)(1000000 + did * 1000));
            w.Write(3);
            for (int i = 1; i <= 3; i++) { w.Write((uint)i); w.Write("首领_" + i); w.Write(100000UL + (ulong)(i * 50000)); w.Write(100000UL + (ulong)(i * 50000)); }
        });
    }

    byte[] HandleDifficulty(BinaryReader br, uint seq) { br.ReadUInt64(); var did = br.ReadUInt32(); var diff = br.ReadInt32(); return BuildResponse((uint)MsgId.CSDungeonDifficulty, seq, w => { w.Write(0); w.Write(did); w.Write(diff); }); }

    byte[] HandleLoot(BinaryReader br, uint seq)
    {
        br.ReadUInt64(); br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSDungeonLoot, seq, w =>
        {
            w.Write(0); var c = _rng.Next(1, 4); w.Write(c);
            for (int i = 0; i < c; i++) { w.Write((uint)_rng.Next(1, 25)); w.Write("物品_" + _rng.Next(100, 999)); w.Write(_rng.Next(1, 6)); w.Write(_rng.Next(1, 3)); }
        });
    }

    byte[] HandleRoll(BinaryReader br, uint seq) { br.ReadUInt64(); br.ReadUInt64(); br.ReadInt32(); return BuildResponse((uint)MsgId.CSDungeonRoll, seq, w => { w.Write(0); w.Write(_rng.Next(1, 101)); }); }
}
