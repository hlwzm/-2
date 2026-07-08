#nullable disable
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Database;
using Jx3.Common.Network;
using Jx3.Common.Protocol;
using Jx3.Common.Utils;
using System.Text.Json;

namespace Jx3.Chat;

/// <summary>聊天服务器 - 处理频道消息/私聊/公告</summary>
public class ChatServer : GameServer
{
    private TcpListener? _listener;
    private readonly ChatService _chatService = new();

    // 在线玩家连接管理
    private readonly ConcurrentDictionary<uint, ClientSession> _sessions = new();
    private readonly ConcurrentDictionary<ulong, uint> _playerToSession = new();
    private uint _nextSessionId = 1;

    // 频道订阅管理: channel -> Set<playerId>
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<ulong, byte>> _channelSubs = new();
    // 队伍频道: teamId -> Set<playerId>
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, byte>> _teamSubs = new();
    // 同盟频道: guildId -> Set<playerId>
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, byte>> _guildSubs = new();

    private const string Tag = "Chat";

    public ChatServer() : base("Chat", 9004) { }

    protected override async Task OnStartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        Logger.Info(Tag, $"Chat server listening on port {Port}...");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            var sessionId = Interlocked.Increment(ref _nextSessionId);
            var session = new ClientSession { TcpClient = client };
            _sessions[sessionId] = session;
            Logger.Info(Tag, $"Session[{sessionId}] connected: {client.Client.RemoteEndPoint}");
            _ = HandleClientAsync(sessionId, session);
        }
    }

    private async Task HandleClientAsync(uint sessionId, ClientSession session)
    {
        var buffer = new byte[1024 * 64];
        var stream = session.TcpClient.GetStream();

        try
        {
            while (session.TcpClient.Connected)
            {
                var read = await stream.ReadAsync(buffer, 0, 4);
                if (read < 4) break;

                var totalLen = BitConverter.ToUInt32(buffer, 0);
                if (totalLen == 0 || totalLen > 65536) break;

                var totalRead = 4;
                while (totalRead < totalLen + 4)
                {
                    read = await stream.ReadAsync(buffer, totalRead, (int)(totalLen + 4 - totalRead));
                    if (read == 0) break;
                    totalRead += read;
                }
                if (totalRead < totalLen + 4) break;

                var packet = MessagePacket.Decode(buffer[..(int)(totalLen + 4)]);
                if (packet == null) continue;

                await HandlePacketAsync(sessionId, session, packet);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Session[{sessionId}] error: {ex.Message}");
        }
        finally
        {
            // 清理连接
            if (session.PlayerId > 0)
            {
                _playerToSession.TryRemove(session.PlayerId, out _);
                RemoveFromAllChannels(session.PlayerId);

                // 更新Redis在线状态
                try
                {
                    using var redis = new RedisHelper();
                    await redis.Db.SetRemoveAsync("online:players", session.PlayerId.ToString());
                    await redis.Db.SetRemoveAsync("chat:channel:0", session.PlayerId.ToString());
                }
                catch { /* 忽略清理异常 */ }
            }

            _sessions.TryRemove(sessionId, out _);
            session.TcpClient.Close();
            Logger.Info(Tag, $"Session[{sessionId}] disconnected (player={session.PlayerId})");
        }
    }

    private async Task HandlePacketAsync(uint sessionId, ClientSession session, MessagePacket packet)
    {
        Logger.Info(Tag, $"Session[{sessionId}] MsgId={packet.MsgId}");

        byte[]? responseData = null;

        try
        {
            switch (packet.MsgId)
            {
                case (uint)MsgId.CSPlayerOnline:
                    responseData = await HandlePlayerOnline(session, packet.Body);
                    break;

                case (uint)MsgId.CSChatSend:
                    responseData = await HandleChatSend(session, packet.Body);
                    break;

                case (uint)MsgId.CSChatPrivate:
                    responseData = await HandleChatPrivate(session, packet.Body);
                    break;


            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"HandlePacket error: {ex.Message}");
        }

        if (responseData != null)
        {
            await SendPacketAsync(session, packet.MsgId + 1, packet.Seq, responseData);
        }
    }

    // ===== 消息处理器 =====

    /// <summary>CSPlayerOnline(5008) -> SCPlayerOnlineStatus(5009)</summary>
    private async Task<byte[]?> HandlePlayerOnline(ClientSession session, byte[] body)
    {
        var result = await _chatService.HandlePlayerOnline(body);
        if (result == null) return null;

        // 解析playerId
        try
        {
            using var reader = new BinaryReader(new MemoryStream(body));
            var playerId = reader.ReadUInt64();
            var isOnline = reader.ReadBoolean();

            if (isOnline && playerId > 0)
            {
                session.PlayerId = playerId;
                _playerToSession[playerId] = _sessions.First(s => s.Value == session).Key;

                // 自动加入世界频道
                SubscribeChannel(0, playerId);

                Logger.Info(Tag, $"Player {playerId} registered on Session[{_playerToSession[playerId]}]");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Parse online body error: {ex.Message}");
        }

        return result;
    }

    /// <summary>CSChatSend(5001) -> SCChatMessage(5002)</summary>
    private async Task<byte[]?> HandleChatSend(ClientSession session, byte[] body)
    {
        var result = await _chatService.HandleChannelMessage(body);

        // 解析原始请求获取频道信息
        try
        {
            using var reader = new BinaryReader(new MemoryStream(body));
            var senderId = reader.ReadUInt64();
            var channel = reader.ReadInt32();

            // 根据频道广播
            if (result != null)
            {
                // 反序列化ChatMessage用于广播判断
                var msgBytes = ChatService.SerializeChatMessage(
                    new ChatMessage { Channel = channel, SenderId = senderId });

                // 构造SCChatMessage响应包
                var chatPacket = new MessagePacket
                {
                    MsgId = (uint)MsgId.SCChatMessage,
                    Body = BuildChatMessagePacket(channel, senderId, body)
                };

                _ = BroadcastToChannelAsync(channel, senderId, chatPacket);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Broadcast error: {ex.Message}");
        }

        return result;
    }

    /// <summary>CSChatPrivate(5003) -> SCChatPrivate(5004)</summary>
    private async Task<byte[]?> HandleChatPrivate(ClientSession session, byte[] body)
    {
        var result = await _chatService.HandlePrivateMessage(body);

        // 如果返回了目标ID，推送给目标
        if (result != null && result.Length > 8)
        {
            try
            {
                using var reader = new BinaryReader(new MemoryStream(result));
                var targetId = reader.ReadUInt64();
                var msgLen = reader.ReadInt32();

                if (targetId > 0 && msgLen > 0)
                {
                    var msgData = reader.ReadBytes(msgLen);

                    // 推送给目标玩家
                    if (_playerToSession.TryGetValue(targetId, out var targetSessionId))
                    {
                        if (_sessions.TryGetValue(targetSessionId, out var targetSession))
                        {
                            var privatePacket = new MessagePacket
                            {
                                MsgId = (uint)MsgId.SCChatPrivate,
                                Body = msgData
                            };
                            await SendPacketAsync(targetSession, privatePacket.MsgId, 0, privatePacket.Body);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(Tag, $"Private push error: {ex.Message}");
            }
        }

        // 返回确认消息给发送者
        using var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(0); // code = 0 成功
        return ms.ToArray();
    }

    // ===== 广播系统 =====

    /// <summary>构造聊天消息包用于广播</summary>
    private byte[] BuildChatMessagePacket(int channel, ulong senderId, byte[] originalBody)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(originalBody));
            var _ = reader.ReadUInt64(); // skip senderId (already known)
            var ch = reader.ReadInt32();
            var msgType = reader.ReadInt32();
            var content = reader.ReadString();
            var extraData = reader.ReadString();

            // 查询玩家信息
            using var db = new DbHelper();
            var playerInfo = db.QueryFirstOrDefaultAsync<(string name, int level)>(
                "SELECT name, level FROM player WHERE player_id = @Id",
                new { Id = senderId }).Result;

            var msg = new ChatMessage
            {
                MsgId = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Channel = channel,
                SenderId = senderId,
                SenderName = playerInfo.name,
                SenderLevel = playerInfo.level,
                Content = content,
                MsgType = msgType,
                ExtraData = extraData,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return ChatService.SerializeChatMessage(msg);
        }
        catch
        {
            return originalBody;
        }
    }

    /// <summary>广播消息到指定频道</summary>
    private async Task BroadcastToChannelAsync(int channel, ulong senderId, MessagePacket packet)
    {
        var packetData = packet.Encode();
        var lenBytes = BitConverter.GetBytes((uint)packetData.Length);

        switch (channel)
        {
            case 0: // 世界频道：广播给所有在线
                await BroadcastToAllAsync(packetData, lenBytes);
                break;

            case 1: // 当前频道：广播给50米内玩家（简化实现：同地图广播）
                await BroadcastToAllAsync(packetData, lenBytes);
                break;

            case 2: // 队伍频道
                await BroadcastToTeamAsync(senderId, packetData, lenBytes);
                break;

            case 3: // 同盟频道
                await BroadcastToGuildAsync(senderId, packetData, lenBytes);
                break;

            case 4: // 门派频道：广播给所有在线
                await BroadcastToAllAsync(packetData, lenBytes);
                break;
        }
    }

    /// <summary>广播给所有在线玩家</summary>
    private async Task BroadcastToAllAsync(byte[] packetData, byte[] lenBytes)
    {
        foreach (var (sid, clientSession) in _sessions)
        {
            try
            {
                if (clientSession.TcpClient.Connected)
                {
                    var stream = clientSession.TcpClient.GetStream();
                    await stream.WriteAsync(lenBytes);
                    await stream.WriteAsync(packetData);
                }
            }
            catch
            {
                // 忽略单个连接异常
            }
        }
    }

    /// <summary>广播给队伍成员</summary>
    private async Task BroadcastToTeamAsync(ulong playerId, byte[] packetData, byte[] lenBytes)
    {
        try
        {
            using var redis = new RedisHelper();
            var teamJson = await redis.GetAsync($"team:info:{playerId}");
            if (string.IsNullOrEmpty(teamJson)) return;

            var teamInfo = JsonSerializer.Deserialize<TeamInfo>(teamJson);
            if (teamInfo?.Members == null) return;

            foreach (var memberId in teamInfo.Members)
            {
                if (_playerToSession.TryGetValue(memberId, out var sid) &&
                    _sessions.TryGetValue(sid, out var session) &&
                    session.TcpClient.Connected)
                {
                    try
                    {
                        var stream = session.TcpClient.GetStream();
                        await stream.WriteAsync(lenBytes);
                        await stream.WriteAsync(packetData);
                    }
                    catch { /* 忽略 */ }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"BroadcastToTeam error: {ex.Message}");
        }
    }

    /// <summary>广播给同盟成员</summary>
    private async Task BroadcastToGuildAsync(ulong playerId, byte[] packetData, byte[] lenBytes)
    {
        try
        {
            using var redis = new RedisHelper();
            var guildId = await redis.GetAsync($"guild:player:{playerId}");
            if (string.IsNullOrEmpty(guildId)) return;

            // 查同盟成员列表
            using var db = new DbHelper();
            var members = await db.QueryAsync<ulong>(
                "SELECT player_id FROM guild_member WHERE guild_id = @GuildId",
                new { GuildId = ulong.Parse(guildId) });

            foreach (var memberId in members)
            {
                if (_playerToSession.TryGetValue(memberId, out var sid) &&
                    _sessions.TryGetValue(sid, out var session) &&
                    session.TcpClient.Connected)
                {
                    try
                    {
                        var stream = session.TcpClient.GetStream();
                        await stream.WriteAsync(lenBytes);
                        await stream.WriteAsync(packetData);
                    }
                    catch { /* 忽略 */ }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"BroadcastToGuild error: {ex.Message}");
        }
    }

    /// <summary>系统公告全服广播</summary>
    public async Task BroadcastSystemNoticeAsync(int noticeType, string title, string content)
    {
        var body = _chatService.HandleSystemNotice(noticeType, title, content);
        if (body == null) return;

        var packet = new MessagePacket
        {
            MsgId = (uint)MsgId.SCChatSystemNotice,
            Body = body
        };
        var packetData = packet.Encode();
        var lenBytes = BitConverter.GetBytes((uint)packetData.Length);

        await BroadcastToAllAsync(packetData, lenBytes);
        Logger.Info(Tag, $"System notice broadcast: type={noticeType} title={title}");
    }

    // ===== 频道管理 =====

    private void SubscribeChannel(int channel, ulong playerId)
    {
        var subs = _channelSubs.GetOrAdd(channel, _ => new ConcurrentDictionary<ulong, byte>());
        subs[playerId] = 0;
    }

    private void UnsubscribeChannel(int channel, ulong playerId)
    {
        if (_channelSubs.TryGetValue(channel, out var subs))
        {
            subs.TryRemove(playerId, out _);
        }
    }

    private void RemoveFromAllChannels(ulong playerId)
    {
        foreach (var (_, subs) in _channelSubs)
        {
            subs.TryRemove(playerId, out _);
        }
    }

    // ===== 发送工具 =====

    private static async Task SendPacketAsync(ClientSession session, uint msgId, uint seq, byte[] body)
    {
        try
        {
            var packet = new MessagePacket
            {
                MsgId = msgId,
                Seq = seq,
                Body = body
            };
            var data = packet.Encode();
            var lenBytes = BitConverter.GetBytes((uint)data.Length);
            var stream = session.TcpClient.GetStream();
            await stream.WriteAsync(lenBytes);
            await stream.WriteAsync(data);
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"SendPacket error: {ex.Message}");
        }
    }
}

/// <summary>客户端会话</summary>
internal class ClientSession
{
    public TcpClient TcpClient { get; set; } = null!;
    public ulong PlayerId { get; set; }
}

// ===== 入口 =====

public class Program
{
    public static async Task Main()
    {
        GameConfigLoader.Load();
        await new ChatServer().StartAsync();
    }
}