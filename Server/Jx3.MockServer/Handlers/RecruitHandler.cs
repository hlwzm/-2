using Jx3.Common.Protocol;

namespace Jx3.MockServer.Handlers;

public class RecruitHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSRecruitDraw => HandleDraw(br, seq),
            (uint)MsgId.CSRecruitPoolList => HandlePoolList(br, seq),
            (uint)MsgId.CSRecruitPity => HandlePity(br, seq),
            _ => null
        };
    }

    byte[] HandleDraw(BinaryReader br, uint seq)
    {
        br.ReadUInt64(); br.ReadInt32(); var count = br.ReadInt32();
        return BuildResponse((uint)MsgId.CSRecruitDraw, seq, w =>
        {
            w.Write(0); w.Write(count);
            for (int i = 0; i < count; i++)
            {
                var q = _rng.Next(100) < 50 ? 3 : _rng.Next(100) < 80 ? 4 : 5;
                w.Write((uint)((q - 3) * 10 + (uint)_rng.Next(1, 10)));
                w.Write("绝世英雄"); w.Write(q);
            }
        });
    }

    byte[] HandlePoolList(BinaryReader br, uint seq)
    {
        return BuildResponse((uint)MsgId.CSRecruitPoolList, seq, w => { w.Write(0); w.Write(3);
            w.Write(1); w.Write("新手推荐"); w.Write(0);
            w.Write(2); w.Write("英雄集结"); w.Write(1);
            w.Write(3); w.Write("限定卡池"); w.Write(2); });
    }

    byte[] HandlePity(BinaryReader br, uint seq)
    {
        br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSRecruitPity, seq, w => { w.Write(0); w.Write(_rng.Next(10, 70)); w.Write(73 - _rng.Next(10, 70)); });
    }
}
