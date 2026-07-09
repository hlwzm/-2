// QuestHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class QuestHandler : HandlerBase, IHandler
{
    static readonly (uint id, string name, int minLv)[] QUESTS = [
        (1, "初入江湖", 1), (2, "第一次战斗", 1),
        (3, "装备强化", 5), (4, "组队挑战", 10),
        (5, "江湖历练", 15), (6, "名扬四海", 20),
        (7, "收集大师", 10), (8, "PVP初体验", 15)];

    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSQuestList => HandleList(br, seq),
            (uint)MsgId.CSQuestAccept => HandleAccept(br, seq),
            (uint)MsgId.CSQuestSubmit => HandleSubmit(br, seq),
            (uint)MsgId.CSQuestAbandon => HandleAbandon(br, seq),
            (uint)MsgId.CSQuestAchievementList => HandleAchieve(br, seq),
            _ => null
        };
    }

    byte[] HandleList(BinaryReader br, uint seq)
    {
        br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSQuestList, seq, w =>
        {
            w.Write(0); w.Write(QUESTS.Length);
            foreach (var q in QUESTS) { w.Write(q.id); w.Write(q.name); w.Write(q.minLv); w.Write(0); w.Write(0); w.Write(_rng.Next(1, 10)); }
        });
    }

    byte[] HandleAccept(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var qid = br.ReadUInt32();
        var u = UserStore.Instance.GetByPid(pid);
        ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "quest", $"接受任务 qid={qid}");
        return BuildResponse((uint)MsgId.CSQuestAccept, seq, w => { w.Write(0); w.Write(qid); w.Write(1); });
    }

    byte[] HandleSubmit(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var qid = br.ReadUInt32();
        var u = UserStore.Instance.GetByPid(pid);
        ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "quest", $"提交任务 qid={qid}");
        return BuildResponse((uint)MsgId.CSQuestSubmit, seq, w =>
        {
            w.Write(0); w.Write(qid);
            w.Write(1000UL); w.Write(100UL);
            w.Write(1); w.Write((uint)_rng.Next(1, 25)); w.Write(_rng.Next(1, 5));
        });
    }

    byte[] HandleAbandon(BinaryReader br, uint seq) { br.ReadUInt64(); br.ReadUInt32(); return BuildResponse((uint)MsgId.CSQuestAbandon, seq, w => w.Write(0)); }

    byte[] HandleAchieve(BinaryReader br, uint seq)
    {
        br.ReadUInt64();
        return BuildResponse((uint)MsgId.CSQuestAchievementList, seq, w =>
        {
            w.Write(0); w.Write(5);
            for (uint i = 1; i <= 5; i++) { w.Write(i); w.Write("成就_" + i); w.Write(i % 2 == 0); w.Write(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); }
        });
    }
}
