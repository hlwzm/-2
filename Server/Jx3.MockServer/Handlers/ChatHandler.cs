// ChatHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class ChatHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSChatSend => HandleSend(br, seq),
            (uint)MsgId.CSChatPrivate => HandlePrivate(br, seq),
            (uint)MsgId.CSChatHistory => HandleHistory(br, seq),
            (uint)MsgId.CSPlayerOnline => HandleOnline(br, seq),
            _ => null
        };
    }

    byte[] HandleSend(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var ch = br.ReadInt32(); var msg = br.ReadString();
        var u = UserStore.Instance.GetByPid(pid);
        ActionLogStore.Instance.AddLog(pid, u?.PlayerName ?? "?", "chat", $"频道={ch} 内容={msg}");
        return BuildResponse((uint)MsgId.CSChatSend, seq, w => { w.Write(0); w.Write(ch); w.Write(pid); w.Write(u?.PlayerName ?? "?"); w.Write(msg); w.Write(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); });
    }

    byte[] HandlePrivate(BinaryReader br, uint seq)
    {
        var from = br.ReadUInt64(); var to = br.ReadUInt64(); var msg = br.ReadString();
        var fu = UserStore.Instance.GetByPid(from);
        var tu = UserStore.Instance.GetByPid(to);
        ActionLogStore.Instance.AddLog(from, fu?.PlayerName ?? "?", "chat", $"密聊 to={to} 内容={msg}");
        return BuildResponse((uint)MsgId.CSChatPrivate, seq, w => { w.Write(0); w.Write(to); w.Write(tu?.PlayerName ?? "?"); w.Write(msg); w.Write(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); });
    }

    byte[] HandleHistory(BinaryReader br, uint seq) { br.ReadUInt64(); br.ReadInt32(); return BuildResponse((uint)MsgId.CSChatHistory, seq, w => { w.Write(0); w.Write(0); }); }
    byte[] HandleOnline(BinaryReader br, uint seq) { br.ReadUInt64(); br.ReadBoolean(); return BuildResponse((uint)MsgId.CSPlayerOnline, seq, w => w.Write(0)); }
}
