// PVPHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class PVPHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSPVPMatchStart => HandleMatchStart(br, seq),
            (uint)MsgId.CSPVPMatchCancel => Simple(br, (uint)MsgId.CSPVPMatchCancel, seq),
            (uint)MsgId.CSPVPRankInfo => HandleRank(br, seq),
            (uint)MsgId.CSPVPDuelChallenge => Simple(br, (uint)MsgId.CSPVPDuelChallenge, seq),
            (uint)MsgId.CSPVPDuelAccept => HandleDuelAccept(br, seq),
            _ => null
        };
    }

    byte[] Simple(BinaryReader br, uint msgId, uint seq) { br.ReadUInt64(); return BuildResponse(msgId, seq, w => w.Write(0)); }

    byte[] HandleMatchStart(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var mt = br.ReadInt32();
        var u = UserStore.Instance.GetByPid(pid);
        ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "pvp", $"匹配开始 matchType={mt}");
        return BuildResponse((uint)MsgId.CSPVPMatchStart, seq, w => { w.Write(0); w.Write(mt); w.Write(_rng.Next(1, 5)); });
    }

    byte[] HandleRank(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var u = UserStore.Instance.GetByPid(pid);
        return BuildResponse((uint)MsgId.CSPVPRankInfo, seq, w =>
        {
            w.Write(0); w.Write(u?.PvpScore ?? 1000); w.Write(u?.PvpRank ?? 0); w.Write(100);
            w.Write(10);
            for (int i = 0; i < 10; i++) { w.Write((ulong)(1000 + i * 50)); w.Write(DateTimeOffset.UtcNow.AddDays(-i).ToUnixTimeSeconds()); }
        });
    }

    byte[] HandleDuelAccept(BinaryReader br, uint seq)
    {
        br.ReadUInt64(); br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSPVPDuelAccept, seq, w => { w.Write(0); w.Write(true); w.Write((ulong)_rng.Next(1, 10001)); w.Write(true); w.Write(20UL); });
    }
}
