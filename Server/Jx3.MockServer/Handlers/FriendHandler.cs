// FriendHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class FriendHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSFriendAdd => HandleAdd(br, seq),
            (uint)MsgId.CSFriendRemove => HandleRemove(br, seq),
            (uint)MsgId.CSFriendList => HandleList(br, seq),
            (uint)MsgId.CSFriendAccept => HandleAccept(br, seq),
            (uint)MsgId.CSFriendDecline => HandleDecline(br, seq),
            _ => null
        };
    }

    byte[] HandleAdd(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var tpid = br.ReadUInt64();
        FriendStore.Instance.SendRequest(pid, tpid);
        var u = UserStore.Instance.GetByPid(pid);
        var tu = UserStore.Instance.GetByPid(tpid);
        ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "friend", $"添加好友 tpid={tpid} name={tu?.PlayerName ?? "?"}");
        return BuildResponse((uint)MsgId.CSFriendAdd, seq, w => { w.Write(0); w.Write(tpid); w.Write(tu?.PlayerName ?? "?"); });
    }

    byte[] HandleRemove(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var tpid = br.ReadUInt64(); FriendStore.Instance.RemoveFriend(pid, tpid); var u = UserStore.Instance.GetByPid(pid); ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "friend", $"删除好友 tpid={tpid}"); return BuildResponse((uint)MsgId.CSFriendRemove, seq, w => w.Write(0)); }

    byte[] HandleList(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var f = FriendStore.Instance.GetFriends(pid); var r = FriendStore.Instance.GetRequests(pid);
        return BuildResponse((uint)MsgId.CSFriendList, seq, w =>
        {
            w.Write(0); w.Write(f.Count);
            foreach (var x in f) { w.Write(x.PlayerId); w.Write(x.PlayerName); w.Write(x.Level); w.Write(x.Online); }
            w.Write(r.Count);
            foreach (var rid in r) { var u = UserStore.Instance.GetByPid(rid); w.Write(rid); w.Write(u?.PlayerName ?? "?"); w.Write(u?.Level ?? 1); }
        });
    }

    byte[] HandleAccept(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var fpid = br.ReadUInt64();
        FriendStore.Instance.AcceptRequest(pid, fpid);
        var fu = UserStore.Instance.GetByPid(fpid); var su = UserStore.Instance.GetByPid(pid);
        if (fu != null) FriendStore.Instance.AddFriend(pid, fpid, fu.PlayerName, fu.Level);
        if (su != null) FriendStore.Instance.AddFriend(fpid, pid, su.PlayerName, su.Level);
        ActionLogStore.Instance.AddLog(pid, su?.PlayerName ?? "?", "friend", $"接受好友请求 fpid={fpid}");
        return BuildResponse((uint)MsgId.CSFriendAccept, seq, w => w.Write(0));
    }

    byte[] HandleDecline(BinaryReader br, uint seq) { var pid = br.ReadUInt64(); var fpid = br.ReadUInt64(); FriendStore.Instance.DeclineRequest(pid, fpid); return BuildResponse((uint)MsgId.CSFriendDecline, seq, w => w.Write(0)); }
}
