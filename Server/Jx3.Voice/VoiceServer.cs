using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Jx3.Common.Network;
using Jx3.Common.Protocol;
using Jx3.Common.Utils;
using Jx3.Common.Config;

namespace Jx3.Voice;

/// <summary>
/// 语音服务器
/// - UDP语音转发(低延迟)
/// - WebSocket信令(房间管理/加入/离开)
/// - 单房间最多8人
/// </summary>
public class VoiceServer
{
    private const string Tag = "Voice";
    private const int UdpPort = 9100;
    private const int WsPort = 9101;
    private const int MaxPlayersPerRoom = 8;

    private UdpClient? _udpServer;
    private HttpListener? _httpListener;
    private CancellationTokenSource? _cts;

    private readonly ConcurrentDictionary<string, VoiceRoom> _rooms = new();
    private readonly ConcurrentDictionary<ulong, VoicePeer> _peers = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _udpServer = new UdpClient(UdpPort);
        Logger.Info(Tag, $"UDP语音服务启动 :{UdpPort}");

        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://+:{WsPort}/voice/");
        _httpListener.Start();
        Logger.Info(Tag, $"WebSocket信令服务启动 :{WsPort}/voice/");

        var udpTask = ReceiveUdpLoopAsync(ct);
        var wsTask = AcceptWebSocketLoopAsync(ct);

        await Task.WhenAll(udpTask, wsTask);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _udpServer?.Close();
        _httpListener?.Stop();
        Logger.Info(Tag, "语音服务已停止");
    }

    // ===== UDP语音转发 =====

    private async Task ReceiveUdpLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await _udpServer!.ReceiveAsync();
                if (ct.IsCancellationRequested) break;

                var data = result.Buffer;
                if (data.Length < 10) continue;

                using var ms = new MemoryStream(data);
                using var r = new BinaryReader(ms);
                ulong senderId = r.ReadUInt64();
                ushort dataLen = r.ReadUInt16();
                byte[] pcmData = r.ReadBytes(dataLen);

                if (!_peers.TryGetValue(senderId, out var senderPeer))
                    continue;

                var roomId = senderPeer.RoomId;
                if (!_rooms.TryGetValue(roomId, out var room))
                    continue;

                await ForwardToRoom(room, senderId, pcmData);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"UDP接收错误: {ex.Message}");
        }
    }

    private async Task ForwardToRoom(VoiceRoom room, ulong senderId, byte[] pcmData)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(senderId);
        w.Write((ushort)pcmData.Length);
        w.Write(pcmData);
        byte[] packet = ms.ToArray();

        var tasks = new List<Task>();
        foreach (var (peerId, peer) in room.Players)
        {
            if (peerId == senderId) continue;
            if (peer.UdpEndPoint != null)
            {
                tasks.Add(SendUdpAsync(peer.UdpEndPoint, packet));
            }
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    private async Task SendUdpAsync(IPEndPoint endpoint, byte[] data)
    {
        try
        {
            await _udpServer!.SendAsync(data, data.Length, endpoint);
        }
        catch (Exception ex)
        {
            Logger.Warn(Tag, $"UDP发送失败 {endpoint}: {ex.Message}");
        }
    }

    // ===== WebSocket信令 =====

    private async Task AcceptWebSocketLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var context = await _httpListener!.GetContextAsync();
                if (ct.IsCancellationRequested) break;

                if (context.Request.IsWebSocketRequest)
                {
                    _ = HandleWebSocketAsync(context, ct);
                }
                else
                {
                    var response = Encoding.UTF8.GetBytes("Jx3 Voice Server Running");
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = 200;
                    await context.Response.OutputStream.WriteAsync(response, ct);
                    context.Response.Close();
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (HttpListenerException) { }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"WebSocket接受错误: {ex.Message}");
        }
    }

    private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken ct)
    {
        WebSocket? ws = null;
        ulong playerId = 0;
        string roomId = "";
        IPEndPoint? remoteEp = null;

        try
        {
            var wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
            ws = wsContext.WebSocket;
            remoteEp = context.Request.RemoteEndPoint;

            Logger.Info(Tag, $"WebSocket客户端连接: {remoteEp}");

            var buffer = new byte[4096];

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var signalResult = await HandleSignalAsync(json, ws, remoteEp);

                    if (signalResult.Disconnected)
                        break;

                    if (signalResult.PlayerId > 0)
                    {
                        playerId = signalResult.PlayerId;
                        roomId = signalResult.RoomId;
                    }

                    if (signalResult.Response != null)
                    {
                        var respBytes = Encoding.UTF8.GetBytes(signalResult.Response);
                        await ws.SendAsync(new ArraySegment<byte>(respBytes), WebSocketMessageType.Text, true, ct);
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            Logger.Warn(Tag, $"WebSocket错误: {ex.Message}");
        }
        finally
        {
            if (playerId > 0)
                RemovePeer(playerId, roomId);

            ws?.Dispose();
            Logger.Info(Tag, $"WebSocket客户端断开 playerId={playerId}");
        }
    }

    // ===== 信令处理 =====

    private async Task<SignalResult> HandleSignalAsync(string json, WebSocket ws, IPEndPoint remoteEp)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var action = root.GetProperty("action").GetString() ?? "";

            return action switch
            {
                "join" => await HandleJoinAsync(root, ws, remoteEp),
                "leave" => HandleLeave(),
                "list" => new SignalResult { Response = HandleList(ws) },
                "ping" => new SignalResult { Response = BuildResponse("pong", new { }) },
                _ => new SignalResult { Response = BuildResponse("error", new { message = $"未知action: {action}" }) }
            };
        }
        catch (JsonException ex)
        {
            return new SignalResult { Response = BuildResponse("error", new { message = $"JSON解析错误: {ex.Message}" }) };
        }
    }

    private async Task<SignalResult> HandleJoinAsync(JsonElement root, WebSocket ws, IPEndPoint remoteEp)
    {
        var pid = root.GetProperty("player_id").GetUInt64();
        var rid = root.GetProperty("room_id").GetString() ?? $"room_{pid}";
        var udpPort = root.GetProperty("udp_port").GetInt32();

        if (pid == 0)
            return new SignalResult { Response = BuildResponse("error", new { message = "无效player_id" }) };

        var room = _rooms.GetOrAdd(rid, _ => new VoiceRoom { RoomId = rid });

        if (room.Players.Count >= MaxPlayersPerRoom)
            return new SignalResult { Response = BuildResponse("error", new { message = $"房间已满(最多{MaxPlayersPerRoom}人)" }) };

        var udpEp = new IPEndPoint(remoteEp.Address, udpPort);

        var peer = new VoicePeer
        {
            PlayerId = pid,
            RoomId = rid,
            UdpEndPoint = udpEp,
            WebSocket = ws,
            JoinedAt = DateTime.UtcNow
        };

        _peers[pid] = peer;
        room.Players[pid] = peer;

        await BroadcastToRoom(rid, "player_joined", new
        {
            player_id = pid,
            room_id = rid,
            player_count = room.Players.Count
        });

        Logger.Info(Tag, $"玩家{pid}加入房间{rid} (UDP:{udpEp}) [{room.Players.Count}/{MaxPlayersPerRoom}]");

        var memberList = room.Players.Keys.ToList();
        return new SignalResult
        {
            PlayerId = pid,
            RoomId = rid,
            Response = BuildResponse("joined", new
            {
                room_id = rid,
                player_id = pid,
                members = memberList,
                player_count = room.Players.Count,
                max_players = MaxPlayersPerRoom
            })
        };
    }

    private SignalResult HandleLeave()
    {
        return new SignalResult { Disconnected = true };
    }

    private string? HandleList(WebSocket ws)
    {
        var peer = _peers.Values.FirstOrDefault(p => p.WebSocket == ws);
        if (peer == null || string.IsNullOrEmpty(peer.RoomId) || !_rooms.TryGetValue(peer.RoomId, out var room))
        {
            return BuildResponse("room_list", new { members = Array.Empty<ulong>(), player_count = 0 });
        }

        return BuildResponse("room_list", new
        {
            room_id = peer.RoomId,
            members = room.Players.Keys.ToList(),
            player_count = room.Players.Count,
            max_players = MaxPlayersPerRoom
        });
    }

    private async Task BroadcastToRoom(string roomId, string action, object data)
    {
        if (!_rooms.TryGetValue(roomId, out var room)) return;

        var json = BuildResponse(action, data);
        var bytes = Encoding.UTF8.GetBytes(json!);

        foreach (var (_, peer) in room.Players)
        {
            if (peer.WebSocket?.State == WebSocketState.Open)
            {
                try
                {
                    await peer.WebSocket.SendAsync(
                        new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { }
            }
        }
    }

    private void RemovePeer(ulong playerId, string roomId)
    {
        _peers.TryRemove(playerId, out _);

        if (!string.IsNullOrEmpty(roomId) && _rooms.TryGetValue(roomId, out var room))
        {
            room.Players.TryRemove(playerId, out _);

            if (room.Players.IsEmpty)
            {
                _rooms.TryRemove(roomId, out _);
                Logger.Info(Tag, $"房间{roomId}已关闭(空)");
            }
            else
            {
                _ = BroadcastToRoom(roomId, "player_left", new
                {
                    player_id = playerId,
                    room_id = roomId,
                    player_count = room.Players.Count
                });
            }
        }
    }

    private static string BuildResponse(string action, object data)
    {
        return JsonSerializer.Serialize(new { action, data }, JsonOpts);
    }

    // ===== 内部类型 =====

    internal class SignalResult
    {
        public ulong PlayerId;
        public string RoomId = "";
        public string? Response;
        public bool Disconnected;
    }

    internal class VoiceRoom
    {
        public string RoomId = "";
        public ConcurrentDictionary<ulong, VoicePeer> Players { get; } = new();
    }

    internal class VoicePeer
    {
        public ulong PlayerId;
        public string RoomId = "";
        public IPEndPoint? UdpEndPoint;
        public WebSocket? WebSocket;
        public DateTime JoinedAt;
    }
}

public class Program
{
    public static async Task Main()
    {
        GameConfigLoader.Load();
        var server = new VoiceServer();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            server.Stop();
        };
        await server.StartAsync();
    }
}
