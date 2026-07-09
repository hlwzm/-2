// GuildHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class GuildHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSGuildCreate => HandleCreate(br, seq),
            (uint)MsgId.CSGuildApply => HandleApply(br, seq),
            (uint)MsgId.CSGuildLeave => HandleLeave(br, seq),
            (uint)MsgId.CSGuildApprove => HandleApprove(br, seq),
            (uint)MsgId.CSGuildKick => HandleKick(br, seq),
            _ => null
        };
    }

    byte[] HandleCreate(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var name = br.ReadString();
        var u = UserStore.Instance.GetByPid(pid);
        if (u == null) return Error((uint)MsgId.CSGuildCreate, seq, 1, "用户不存在");
        var g = GuildStore.Instance.Create(name, pid, u.PlayerName);
        if (g == null) return Error((uint)MsgId.CSGuildCreate, seq, 2, "帮会名已存在");
        ActionLogStore.Instance.AddLog(pid, u.PlayerName, "guild", $"创建帮会 {name}");
        return BuildResponse((uint)MsgId.CSGuildCreate, seq, w => { w.Write(0); w.Write(g.GuildId); w.Write(g.Name); });
    }

    byte[] HandleApply(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var gid = br.ReadUInt64(); GuildStore.Instance.AddApply(gid, pid); var u = UserStore.Instance.GetByPid(pid); ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "guild", $"申请帮会 gid={gid}"); return BuildResponse((uint)MsgId.CSGuildApply, seq, w => w.Write(0)); }

    byte[] HandleLeave(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var g = GuildStore.Instance.GetByPlayer(pid); if (g != null) GuildStore.Instance.LeaveGuild(g.GuildId, pid); var u = UserStore.Instance.GetByPid(pid); ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "guild", $"退出帮会"); return BuildResponse((uint)MsgId.CSGuildLeave, seq, w => w.Write(0)); }

    byte[] HandleApprove(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var tpid = br.ReadUInt64();
        var g = GuildStore.Instance.GetByPlayer(pid);
        if (g == null) return Error((uint)MsgId.CSGuildApprove, seq, 1, "不在帮会中");
        var tu = UserStore.Instance.GetByPid(tpid);
        if (tu == null) return Error((uint)MsgId.CSGuildApprove, seq, 2, "目标不存在");
        GuildStore.Instance.ApproveMember(g.GuildId, pid, tpid, tu.PlayerName);
        ActionLogStore.Instance.AddLog(pid, tu.PlayerName, "guild", $"批准入帮 tpid={tpid}");
        return BuildResponse((uint)MsgId.CSGuildApprove, seq, w => { w.Write(0); w.Write(tpid); w.Write(tu.PlayerName); });
    }

    byte[] HandleKick(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var tpid = br.ReadUInt64();
        var g = GuildStore.Instance.GetByPlayer(pid);
        if (g == null) return Error((uint)MsgId.CSGuildKick, seq, 1, "不在帮会中");
        GuildStore.Instance.Kick(g.GuildId, pid, tpid);
        var u = UserStore.Instance.GetByPid(pid);
        ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "guild", $"踢出帮会 tpid={tpid}");
        return BuildResponse((uint)MsgId.CSGuildKick, seq, w => w.Write(0));
    }
}
