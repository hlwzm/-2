using Jx3.Common.Config;
using Jx3.Common.Utils;

namespace Jx3.Common;

/// <summary>微服务主机：一键启动所有服务</summary>
public class ServiceHost
{
    private readonly List<GameServer> _servers = new();

    public ServiceHost Register(GameServer server)
    {
        _servers.Add(server);
        return this;
    }

    public async Task StartAllAsync()
    {
        Logger.Info("ServiceHost", $"Starting {_servers.Count} services...");
        var tasks = _servers.Select(s => s.StartAsync()).ToArray();
        await Task.WhenAll(tasks);
    }

    public static async Task RunAsync(params GameServer[] servers)
    {
        var host = new ServiceHost();
        foreach (var s in servers) host.Register(s);
        await host.StartAllAsync();
    }
}