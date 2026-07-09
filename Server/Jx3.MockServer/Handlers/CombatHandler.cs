// CombatHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class CombatHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSCombatMove => Simple(br, seq, (uint)MsgId.CSCombatMove),
            (uint)MsgId.CSCombatCastSkill => HandleCastSkill(br, seq),
            (uint)MsgId.CSCombatSwitchHero => HandleSwitch(br, seq),
            (uint)MsgId.CSCombatDodge => Simple(br, seq, (uint)MsgId.CSCombatDodge),
            (uint)MsgId.CSCombatAutoOn => Simple(br, seq, (uint)MsgId.CSCombatAutoOn),
            (uint)MsgId.CSCombatAutoOff => Simple(br, seq, (uint)MsgId.CSCombatAutoOff),
            (uint)MsgId.CSCombatAttack => HandleAttack(br, seq),
            (uint)MsgId.CSCombatTargetSelect => Simple(br, seq, (uint)MsgId.CSCombatTargetSelect),
            _ => null
        };
    }

    byte[] Simple(BinaryReader br, uint seq, uint msgId) { br.ReadUInt64(); return BuildResponse(msgId, seq, w => w.Write(0)); }

    byte[] HandleCastSkill(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var skillId = br.ReadUInt32(); var targetId = br.ReadUInt64();
        var u = UserStore.Instance.GetByPid(pid);
        ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "combat", $"释放技能 skillId={skillId} targetId={targetId}");
        return BuildResponse((uint)MsgId.CSCombatCastSkill, seq, w => { w.Write(0); w.Write(skillId); w.Write(targetId); w.Write((ulong)_rng.Next(100, 5000)); w.Write(_rng.Next(2) == 0); });
    }

    byte[] HandleSwitch(BinaryReader br, uint seq) { br.ReadUInt64(); var hid = br.ReadUInt32(); return BuildResponse((uint)MsgId.CSCombatSwitchHero, seq, w => { w.Write(0); w.Write(hid); }); }

    byte[] HandleAttack(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var tid = br.ReadUInt64(); var u = UserStore.Instance.GetByPid(pid); ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "combat", $"普攻 targetId={tid}"); return BuildResponse((uint)MsgId.CSCombatAttack, seq, w => { w.Write(0); w.Write(tid); w.Write((ulong)_rng.Next(50, 2000)); }); }
}
