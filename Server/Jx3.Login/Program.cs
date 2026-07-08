using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Utils;

namespace Jx3.Login;

public class LoginServer : GameServer
{
    public LoginServer() : base("Login", 9001) { }

    protected override Task OnStartAsync()
    {
        Logger.Info("Login", "Login service ready");
        // TODO: 实现登录认证逻辑 (DB查询/Token签发)
        return Task.CompletedTask;
    }

    // 账号密码登录
    public async Task<uint> AuthenticateAsync(string phone, string password)
    {
        // TODO: 查询MySQL account表验证
        await Task.Delay(10);
        Logger.Info("Login", $"Auth: {phone}");
        return 0; // 返回playerId
    }

    // Token验证
    public async Task<uint> ValidateTokenAsync(string token)
    {
        // TODO: Redis校验token
        await Task.Delay(5);
        return 0;
    }
}

public class Program
{
    public static async Task Main()
    {
        await new LoginServer().StartAsync();
    }
}