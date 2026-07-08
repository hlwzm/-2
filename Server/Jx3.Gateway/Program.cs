using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Network;
using Jx3.Common.Utils;

namespace Jx3.Gateway;

public class GatewayServer : GameServer
{
    private TcpListener? _listener;
    private readonly Dictionary<uint, TcpClient> _sessions = new();

    public GatewayServer() : base("Gateway", GameConfig.GatewayPort) { }

    protected override async Task OnStartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        Logger.Info("Gateway", "Accepting connections...");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[4096];
        Logger.Info("Gateway", $"Client connected: {client.Client.RemoteEndPoint}");

        try
        {
            while (client.Connected)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0) break;

                var packet = MessagePacket.Decode(buffer[..read]);
                if (packet == null) continue;

                // 路由到对应微服务
                await RouteMessageAsync(packet, stream);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Gateway", $"Client error: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private async Task RouteMessageAsync(MessagePacket packet, NetworkStream stream)
    {
        // 根据MsgID路由到对应服务
        switch (packet.MsgId / 1000)
        {
            case 1: // 登录模块 1000-1999
                await ForwardToServiceAsync("Login", packet, stream);
                break;
            case 2: // 副本模块 2000-2999
                await ForwardToServiceAsync("Dungeon", packet, stream);
                break;
            case 3: // 交易模块 3000-3999
                await ForwardToServiceAsync("Trade", packet, stream);
                break;
            case 4: // 战斗模块 4000-4999
                await ForwardToServiceAsync("Battle", packet, stream);
                break;
            case 5: // 聊天模块 5000-5999
                await ForwardToServiceAsync("Chat", packet, stream);
                break;
            default:
                Logger.Warn("Gateway", $"Unknown MsgID: {packet.MsgId}");
                break;
        }
    }

    private Task ForwardToServiceAsync(string service, MessagePacket packet, NetworkStream stream)
    {
        // TODO: 通过gRPC/HTTP转发到对应微服务
        Logger.Info("Gateway", $"Forward {packet.MsgId} -> {service}");
        return Task.CompletedTask;
    }
}

public class Program
{
    public static async Task Main()
    {
        await new GatewayServer().StartAsync();
    }
}