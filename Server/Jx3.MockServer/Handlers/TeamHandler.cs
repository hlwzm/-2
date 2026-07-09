// TeamHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class TeamHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSTeamCreate => HandleCreate(br, seq),
            (uint)MsgId.CSTeamInvite => Simple(br, (uint)MsgId.CSTeamInvite, seq),
            (uint)MsgId.CSTeamLeave => HandleLeave(br, seq),
            (uint)MsgId.CSTeamKick => Simple(br, (uint)MsgId.CSTeamKick, seq),
            (uint)MsgId.CSTeamTransfer => Simple(br, (uint)MsgId.CSTeamTransfer, seq),
            (uint)MsgId.CSTeamInviteAccept => HandleInviteAccept(br, seq),
            (uint)MsgId.CSTeamInviteDecline => Simple(br, (uint)MsgId.CSTeamInviteDecline, seq),
            (uint)MsgId.CSTeamApply => Simple(br, (uint)MsgId.CSTeamApply, seq),
            (uint)MsgId.CSTeamApplyApprove => Simple(br, (uint)MsgId.CSTeamApplyApprove, seq),
            (uint)MsgId.CSTeamApplyReject => Simple(br, (uint)MsgId.CSTeamApplyReject, seq),
            (uint)MsgId.CSTeamLootMode => HandleLootMode(br, seq),
            (uint)MsgId.CSTeamMatchStart => Simple(br, (uint)MsgId.CSTeamMatchStart, seq),
            (uint)MsgId.CSTeamMatchCancel => Simple(br, (uint)MsgId.CSTeamMatchCancel, seq),
            (uint)MsgId.CSTeamRecruitPublish => HandleRecruitPublish(br, seq),
            (uint)MsgId.CSTeamRecruitSearch => HandleRecruitSearch(br, seq),
            (uint)MsgId.CSTeamVoiceJoin => Simple(br, (uint)MsgId.CSTeamVoiceJoin, seq),
            _ => null
        };
    }

    byte[] Simple(BinaryReader br, uint msgId, uint seq) { br.ReadUInt64(); if (msgId == (uint)MsgId.CSTeamKick || msgId == (uint)MsgId.CSTeamTransfer || msgId == (uint)MsgId.CSTeamInvite || msgId == (uint)MsgId.CSTeamInviteDecline || msgId == (uint)MsgId.CSTeamApplyApprove || msgId == (uint)MsgId.CSTeamApplyReject) br.ReadUInt64(); return BuildResponse(msgId, seq, w => w.Write(0)); }

    byte[] HandleCreate(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var u = UserStore.Instance.GetByPid(pid);
        if (u == null) return Error((uint)MsgId.CSTeamCreate, seq, 1, "用户不存在");
        var t = TeamStore.Instance.Create(pid, u.PlayerName, u.Level);
        ActionLogStore.Instance.AddLog(pid, u.PlayerName, "team", $"创建队伍 tid={t!.TeamId}");
        return BuildResponse((uint)MsgId.CSTeamCreate, seq, w => { w.Write(0); w.Write(t.TeamId); w.Write(1); });
    }

    byte[] HandleLeave(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var t = TeamStore.Instance.GetByPlayer(pid); if (t != null) TeamStore.Instance.RemoveMember(t.TeamId, pid); var u = UserStore.Instance.GetByPid(pid); ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "team", $"离开队伍"); return BuildResponse((uint)MsgId.CSTeamLeave, seq, w => w.Write(0)); }

    byte[] HandleInviteAccept(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var tid = br.ReadUInt64(); var u = UserStore.Instance.GetByPid(pid);
        if (u == null) return Error((uint)MsgId.CSTeamInviteAccept, seq, 1, "用户不存在");
        TeamStore.Instance.AddMember(tid, pid, u.PlayerName, u.Level);
        ActionLogStore.Instance.AddLog(pid, u.PlayerName, "team", $"接受组队邀请 tid={tid}");
        return BuildResponse((uint)MsgId.CSTeamInviteAccept, seq, w => w.Write(0));
    }

    byte[] HandleLootMode(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var mode = br.ReadInt32(); var t = TeamStore.Instance.GetByPlayer(pid); if (t != null) TeamStore.Instance.SetLootMode(t.TeamId, mode); return BuildResponse((uint)MsgId.CSTeamLootMode, seq, w => w.Write(0)); }
    byte[] HandleRecruitPublish(BinaryReader br, uint seq) { br.ReadUInt64(); br.ReadString(); br.ReadInt32(); return BuildResponse((uint)MsgId.CSTeamRecruitPublish, seq, w => w.Write(0)); }
    byte[] HandleRecruitSearch(BinaryReader br, uint seq) { br.ReadUInt64(); br.ReadInt32(); return BuildResponse((uint)MsgId.CSTeamRecruitSearch, seq, w => { w.Write(0); w.Write(0); }); }
}
