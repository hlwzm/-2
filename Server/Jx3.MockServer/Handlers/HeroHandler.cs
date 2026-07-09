using Jx3.Common.Protocol;

namespace Jx3.MockServer.Handlers;

public class HeroHandler : HandlerBase, IHandler
{
    static readonly string[] Names = ["李忘生", "叶英", "叶炜", "叶琦菲", "谢渊", "王遗风", "柳风骨", "公孙幽", "公孙盈", "叶凡"];

    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSHeroList => HandleList(br, seq),
            (uint)MsgId.CSHeroLevelUp => HandleLevelUp(br, seq),
            (uint)MsgId.CSHeroStarUp => HandleStarUp(br, seq),
            (uint)MsgId.CSHeroTeamSet => HandleTeamSet(br, seq),
            (uint)MsgId.CSHeroInfo => HandleInfo(br, seq),
            _ => null
        };
    }

    byte[] HandleList(BinaryReader br, uint seq)
    {
        br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSHeroList, seq, w =>
        {
            w.Write(0); w.Write(10);
            for (uint i = 1; i <= 10; i++) { w.Write(i); w.Write(Names[i - 1]); w.Write(_rng.Next(1, 21)); w.Write(_rng.Next(1, 7)); w.Write(_rng.Next(1, 6)); w.Write(false); }
        });
    }

    byte[] HandleLevelUp(BinaryReader br, uint seq) { br.ReadUInt64(); var hid = br.ReadUInt32(); return BuildResponse((uint)MsgId.CSHeroLevelUp, seq, w => { w.Write(0); w.Write(hid); w.Write(_rng.Next(2, 6)); }); }
    byte[] HandleStarUp(BinaryReader br, uint seq) { br.ReadUInt64(); var hid = br.ReadUInt32(); return BuildResponse((uint)MsgId.CSHeroStarUp, seq, w => { w.Write(0); w.Write(hid); w.Write(_rng.Next(2, 7)); }); }
    byte[] HandleTeamSet(BinaryReader br, uint seq) { br.ReadUInt64(); var c = br.ReadInt32(); for (int i = 0; i < c; i++) br.ReadUInt32(); return BuildResponse((uint)MsgId.CSHeroTeamSet, seq, w => w.Write(0)); }
    byte[] HandleInfo(BinaryReader br, uint seq) { br.ReadUInt64(); var hid = br.ReadUInt32(); return BuildResponse((uint)MsgId.CSHeroInfo, seq, w => { w.Write(0); w.Write(hid); w.Write(Names[hid - 1]); w.Write(_rng.Next(1, 21)); w.Write(_rng.Next(1, 7)); w.Write(_rng.Next(1, 6)); w.Write((ulong)_rng.Next(500, 5000)); w.Write(100); }); }
}
