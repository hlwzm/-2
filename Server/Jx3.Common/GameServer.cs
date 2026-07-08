using Jx3.Common.Config;
using Jx3.Common.Utils;

namespace Jx3.Common;

/// <summary>游戏服务器基类</summary>
public abstract class GameServer
{
    protected string ServerName { get; }
    protected int Port { get; }

    protected GameServer(string name, int port)
    {
        ServerName = name;
        Port = port;
    }

    public async Task StartAsync()
    {
        Logger.Info(ServerName, $"Starting on port {Port}...");
        await OnStartAsync();
        Logger.Info(ServerName, "Started successfully");
        await Task.Delay(-1); // 保持运行
    }

    protected abstract Task OnStartAsync();
}