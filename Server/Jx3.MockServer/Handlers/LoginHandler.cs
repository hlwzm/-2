// LoginHandler.cs
using Jx3.Common.Protocol;
using Jx3.MockServer.Data;

namespace Jx3.MockServer.Handlers;

public class LoginHandler : HandlerBase, IHandler
{
    public byte[]? Handle(uint msgId, uint seq, byte[] body)
    {
        SimulateLatency();
        using var br = new BinaryReader(new MemoryStream(body));
        return msgId switch
        {
            (uint)MsgId.CSLoginAuth => HandleLogin(br, seq),
            (uint)MsgId.CSLoginRegister => HandleRegister(br, seq),
            (uint)MsgId.CSLoginCreateRole => HandleCreateRole(br, seq),
            (uint)MsgId.CSLoginEnterGame => HandleEnterGame(br, seq),
            _ => null
        };
    }

    byte[] HandleLogin(BinaryReader br, uint seq)
    {
        var phone = br.ReadString(); var pwd = br.ReadString();
        var user = UserStore.Instance.GetOrCreateUser(phone, pwd);
        ActionLogStore.Instance.AddLog(user.PlayerId, user.PlayerName, "login", $"登录 phone={phone}");
        return BuildResponse((uint)MsgId.CSLoginAuth, seq, w => { w.Write(0); w.Write(user.Token); w.Write(user.PlayerId); });
    }

    byte[] HandleRegister(BinaryReader br, uint seq)
    {
        var phone = br.ReadString(); var pwd = br.ReadString();
        var user = UserStore.Instance.GetOrCreateUser(phone, pwd);
        ActionLogStore.Instance.AddLog(user.PlayerId, user.PlayerName, "login", $"注册 phone={phone}");
        return BuildResponse((uint)MsgId.CSLoginRegister, seq, w => { w.Write(0); w.Write(user.Token); });
    }

    byte[] HandleCreateRole(BinaryReader br, uint seq)
    {
        br.ReadString(); // name
        return BuildResponse((uint)MsgId.CSLoginCreateRole, seq, w => { w.Write(0); w.Write(10001UL); });
    }

    byte[] HandleEnterGame(BinaryReader br, uint seq)
    {
        var pid = br.ReadUInt64(); var token = br.ReadString();
        var user = UserStore.Instance.GetByPid(pid);
        if (user == null) return Error((uint)MsgId.CSLoginEnterGame, seq, 1, "用户不存在");
        ActionLogStore.Instance.AddLog(pid, user.PlayerName, "login", $"进入游戏");
        return BuildResponse((uint)MsgId.CSLoginEnterGame, seq, w => { w.Write(user.PlayerName); w.Write(user.Level); w.Write(1001u); w.Write(user.Gold); w.Write(user.Gem); });
    }
}
