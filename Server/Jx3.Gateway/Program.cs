using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Network;
using Jx3.Common.Service;
using Jx3.Common.Utils;

namespace Jx3.Gateway;

public class GatewayServer : GameServer
{
    private TcpListener? _listener;
    private readonly ConcurrentDictionary<uint, TcpClient> _sessions = new();
    private uint _nextSessionId = 1;

    public GatewayServer() : base("Gateway", GameConfig.GatewayPort) { }

    protected override async Task OnStartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        Logger.Info("Gateway", $"Listening on port {Port}...");

        // 等待其他微服务注册完成
        await Task.Delay(500);

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            var sessionId = Interlocked.Increment(ref _nextSessionId);
            _sessions[sessionId] = client;
            Logger.Info("Gateway", $"Session[{sessionId}] connected: {client.Client.RemoteEndPoint}");
            _ = HandleClientAsync(sessionId, client);
        }
    }

    private async Task HandleClientAsync(uint sessionId, TcpClient client)
    {
        var buffer = new byte[1024 * 64]; // 64KB buffer
        var stream = client.GetStream();

        try
        {
            while (client.Connected)
            {
                var read = await stream.ReadAsync(buffer, 0, 4); // 先读4字节长度
                if (read < 4) break;

                var bodyLen = BitConverter.ToUInt32(buffer, 0);
                if (bodyLen == 0 || bodyLen > 65536) break;

                var totalRead = 4;
                while (totalRead < bodyLen + 4)
                {
                    read = await stream.ReadAsync(buffer, totalRead, (int)(bodyLen + 4 - totalRead));
                    if (read == 0) break;
                    totalRead += read;
                }
                if (totalRead < bodyLen + 4) break;

                var packet = MessagePacket.Decode(buffer[..(int)(bodyLen + 4)]);
                if (packet == null) continue;

                // 通过服务注册中心路由
                var response = await ServiceRegistry.RouteAsync(packet.MsgId, packet.Body);
                if (response != null)
                {
                    var responsePacket = new MessagePacket
                    {
                        MsgId = packet.MsgId + 1, // 响应ID = 请求ID+1
                        Seq = packet.Seq,
                        Body = response
                    };
                    var respData = responsePacket.Encode();
                    var lenBytes = BitConverter.GetBytes((uint)respData.Length);
                    await stream.WriteAsync(lenBytes);
                    await stream.WriteAsync(respData);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Gateway", $"Session[{sessionId}] error: {ex.Message}");
        }
        finally
        {
            _sessions.TryRemove(sessionId, out _);
            client.Close();
            Logger.Info("Gateway", $"Session[{sessionId}] disconnected");
        }
    }
}

public class Program
{
    public static async Task Main()
    {
        await new GatewayServer().StartAsync();
    }
}