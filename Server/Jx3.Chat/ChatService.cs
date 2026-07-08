using Jx3.Common.Database;
using Jx3.Common.Utils;
using StackExchange.Redis;
using System.Text.Json;

namespace Jx3.Chat;

/// <summary>聊天消息数据模型</summary>
public class ChatMessage
{
    public ulong MsgId { get; set; }
    public int Channel { get; set; }
    public ulong SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public int SenderLevel { get; set; }
    public string Content { get; set; } = "";
    public int MsgType { get; set; } // 0=文字 1=物品链接 2=语音
    public string ExtraData { get; set; } = "";
    public long Timestamp { get; set; }
}

/// <summary>聊天业务服务</summary>
public class ChatService
{
    private const string Tag = "Chat";
    
    // ===== 敏感词列表 =====
    private static readonly string[] SensitiveWords = new[]
    {
        "fuck", "shit", "ass", "damn", "bitch",
        "垃圾", "废物", "傻逼", "操你妈", "去死",
        "骗子", "外挂", "代练", "卖金币", "广告"
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    static ChatService()
    {
        // 排序敏感词，长词优先匹配
        Array.Sort(SensitiveWords, (a, b) => b.Length.CompareTo(a.Length));
    }

    /// <summary>过滤敏感词</summary>
    public static string FilterSensitive(string content)
    {
        foreach (var word in SensitiveWords)
        {
            int idx;
            while ((idx = content.IndexOf(word, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                content = content[..idx] + new string('*', word.Length) + content[(idx + word.Length)..];
            }
        }
        return content;
    }

    /// <summary>验证频道是否合法</summary>
    public static bool IsValidChannel(int channel) => channel is >= 0 and <= 5;

    /// <summary>获取频道名称</summary>
    public static string GetChannelName(int channel) => channel switch
    {
        0 => "世界",
        1 => "当前",
        2 => "队伍",
        3 => "同盟",
        4 => "门派",
        5 => "系统",
        _ => "未知"
    };

    /// <summary>处理频道消息发送</summary>
    public async Task<byte[]?> HandleChannelMessage(byte[] body)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(body));
            var senderId = reader.ReadUInt64();
            var channel = reader.ReadInt32();
            var msgType = reader.ReadInt32();
            var content = reader.ReadString();
            var extraData = reader.ReadString();

            // 验证频道
            if (!IsValidChannel(channel))
                return BuildErrorResponse(1, "无效的频道");

            // 过滤敏感词
            content = FilterSensitive(content);

            // 查询玩家信息
            using var db = new DbHelper();
            var playerInfo = await db.QueryFirstOrDefaultAsync<(ulong player_id, string name, int level, long gold)>(
                "SELECT player_id, name, level, gold FROM player WHERE player_id = @Id",
                new { Id = senderId });

            if (playerInfo.player_id == 0)
                return BuildErrorResponse(2, "玩家不存在");

            // 世界频道特殊处理：CD和金币消耗
            if (channel == 0)
            {
                using var redis = new RedisHelper();
                var cdKey = $"chat:cd:world:{senderId}";
                var lastTime = await redis.GetAsync(cdKey);
                if (lastTime != null)
                {
                    var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - long.Parse(lastTime);
                    if (elapsed < 5)
                        return BuildErrorResponse(3, $"世界频道CD中，请等待{5 - elapsed}秒");
                }

                // 扣除100金币
                if (playerInfo.gold < 100)
                    return BuildErrorResponse(4, "金币不足100，无法在世界频道发言");

                await db.ExecuteAsync(
                    "UPDATE player SET gold = gold - 100 WHERE player_id = @Id",
                    new { Id = senderId });

                // 更新CD
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                await redis.SetAsync(cdKey, now, TimeSpan.FromSeconds(5));
            }

            // 队伍频道：检查队伍关系
            if (channel == 2)
            {
                // 查Redis队伍信息
                using var redis = new RedisHelper();
                var teamJson = await redis.GetAsync($"team:info:{senderId}");
                if (string.IsNullOrEmpty(teamJson))
                    return BuildErrorResponse(5, "你不在任何队伍中");
            }

            // 同盟频道：检查同盟关系
            if (channel == 3)
            {
                // 查Redis同盟信息
                using var redis = new RedisHelper();
                var guildId = await redis.GetAsync($"guild:player:{senderId}");
                if (string.IsNullOrEmpty(guildId))
                    return BuildErrorResponse(6, "你不在任何同盟中");
            }

            // 构造消息
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

            // 存储到Redis世界频道缓存
            if (channel == 0)
            {
                using var redis = new RedisHelper();
                var msgJson = JsonSerializer.Serialize(msg, JsonOpts);
                await redis.Db.ListLeftPushAsync("chat:world", msgJson);
                await redis.Db.ListTrimAsync("chat:world", 0, 99);
            }

            // 写入MySQL聊天记录
            await db.ExecuteAsync(@"
                INSERT INTO chat_log (channel, sender_id, content, msg_type, timestamp)
                VALUES (@Channel, @SenderId, @Content, @MsgType, @Timestamp)",
                new
                {
                    msg.Channel,
                    msg.SenderId,
                    msg.Content,
                    msg.MsgType,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(msg.Timestamp).DateTime
                });

            // 序列化消息用于广播
            var msgBytes = SerializeChatMessage(msg);

            // 根据频道广播
            return channel switch
            {
                0 => msgBytes, // 世界频道：返回消息，由Program广播给所有在线
                1 => msgBytes, // 当前频道：返回消息，由Program根据距离过滤
                2 => msgBytes, // 队伍频道：返回消息，由Program广播给同队
                3 => msgBytes, // 同盟频道：返回消息，由Program广播给同盟
                4 => msgBytes, // 门派频道：返回消息，由Program广播给同门
                _ => msgBytes
            };
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"HandleChannelMessage error: {ex.Message}");
            return BuildErrorResponse(99, "服务器内部错误");
        }
    }

    /// <summary>处理私聊消息</summary>
    public async Task<byte[]?> HandlePrivateMessage(byte[] body)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(body));
            var senderId = reader.ReadUInt64();
            var targetId = reader.ReadUInt64();
            var msgType = reader.ReadInt32();
            var content = reader.ReadString();
            var extraData = reader.ReadString();

            // 过滤敏感词
            content = FilterSensitive(content);

            // 查询发送者信息
            using var db = new DbHelper();
            var senderInfo = await db.QueryFirstOrDefaultAsync<(ulong player_id, string name, int level)>(
                "SELECT player_id, name, level FROM player WHERE player_id = @Id",
                new { Id = senderId });

            if (senderInfo.player_id == 0)
                return BuildErrorResponse(2, "发送者不存在");

            var msg = new ChatMessage
            {
                MsgId = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Channel = 6, // 私聊频道
                SenderId = senderId,
                SenderName = senderInfo.name,
                SenderLevel = senderInfo.level,
                Content = content,
                MsgType = msgType,
                ExtraData = extraData,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var msgBytes = SerializeChatMessage(msg);

            // 检查对方是否在线
            using var redis = new RedisHelper();
            var isOnline = await redis.Db.SetContainsAsync("online:players", targetId.ToString());

            if (isOnline)
            {
                // 在线：直接返回消息，由Program推送给目标
                // 返回消息+目标ID的包装
                using var ms = new MemoryStream();
                var w = new BinaryWriter(ms);
                w.Write(targetId);   // 目标玩家ID
                w.Write(msgBytes.Length);
                w.Write(msgBytes);
                return ms.ToArray();
            }
            else
            {
                // 离线：存MySQL，上线后推送
                var chatMsgJson = JsonSerializer.Serialize(msg, JsonOpts);
                await db.ExecuteAsync(@"
                    INSERT INTO chat_log (channel, sender_id, content, msg_type, timestamp)
                    VALUES (6, @SenderId, @Content, @MsgType, @Timestamp)",
                    new
                    {
                        msg.SenderId,
                        msg.Content,
                        msg.MsgType,
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(msg.Timestamp).DateTime
                    });

                // 存离线消息到Redis List
                await redis.Db.ListRightPushAsync($"chat:offline:{targetId}", chatMsgJson);

                // 返回成功（无目标推送）
                using var ms = new MemoryStream();
                var w = new BinaryWriter(ms);
                w.Write((ulong)0); // targetId=0表示离线
                w.Write(0);        // 0长度
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"HandlePrivateMessage error: {ex.Message}");
            return BuildErrorResponse(99, "服务器内部错误");
        }
    }

    /// <summary>处理玩家上下线</summary>
    public async Task<byte[]?> HandlePlayerOnline(byte[] body)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(body));
            var playerId = reader.ReadUInt64();
            var isOnline = reader.ReadBoolean();

            using var redis = new RedisHelper();

            if (isOnline)
            {
                // 上线：加入在线集合
                await redis.Db.SetAddAsync("online:players", playerId.ToString());

                // 自动加入世界频道
                await redis.Db.SetAddAsync("chat:channel:0", playerId.ToString());

                // 查同盟信息，自动加入同盟频道
                var guildId = await redis.GetAsync($"guild:player:{playerId}");
                if (!string.IsNullOrEmpty(guildId))
                {
                    await redis.Db.SetAddAsync($"chat:channel:3:{guildId}", playerId.ToString());
                }

                // 查队伍信息，自动加入队伍频道
                var teamJson = await redis.GetAsync($"team:info:{playerId}");
                if (!string.IsNullOrEmpty(teamJson))
                {
                    var teamData = JsonSerializer.Deserialize<TeamInfo>(teamJson);
                    if (teamData != null)
                    {
                        await redis.Db.SetAddAsync($"chat:channel:2:{teamData.TeamId}", playerId.ToString());
                    }
                }

                Logger.Info(Tag, $"Player online: {playerId}");

                // 检查离线消息
                var offlineMsgs = await redis.Db.ListRangeAsync($"chat:offline:{playerId}");
                if (offlineMsgs.Length > 0)
                {
                    // 清理离线消息
                    await redis.Db.KeyDeleteAsync($"chat:offline:{playerId}");

                    // 返回离线消息
                    var msgs = offlineMsgs.Select(m => m.ToString()).ToArray();
                    var json = JsonSerializer.Serialize(new { offline_messages = msgs }, JsonOpts);
                    return System.Text.Encoding.UTF8.GetBytes(json);
                }
            }
            else
            {
                // 下线：从在线集合移除
                await redis.Db.SetRemoveAsync("online:players", playerId.ToString());

                // 离开所有频道
                await redis.Db.SetRemoveAsync("chat:channel:0", playerId.ToString());

                Logger.Info(Tag, $"Player offline: {playerId}");
            }

            return BuildSuccessResponse();
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"HandlePlayerOnline error: {ex.Message}");
            return BuildErrorResponse(99, "服务器内部错误");
        }
    }

    /// <summary>处理系统公告</summary>
    public byte[]? HandleSystemNotice(int noticeType, string title, string content)
    {
        try
        {
            using var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write(noticeType);   // 0=公告 1=活动 2=系统
            w.Write(title);
            w.Write(content);
            w.Write(noticeType == 0); // 重要公告有弹窗
            w.Write(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"HandleSystemNotice error: {ex.Message}");
            return null;
        }
    }

    /// <summary>获取世界频道历史消息</summary>
    public async Task<byte[]?> HandleChatHistory(byte[] body)
    {
        try
        {
            var channel = 0;
            if (body.Length >= 4)
            {
                using var reader = new BinaryReader(new MemoryStream(body));
                channel = reader.ReadInt32();
            }

            if (channel == 0)
            {
                using var redis = new RedisHelper();
                var msgs = await redis.Db.ListRangeAsync("chat:world", 0, 99);
                var msgList = msgs.Select(m => m.ToString()).ToList();
                var json = JsonSerializer.Serialize(new { channel, messages = msgList }, JsonOpts);
                return System.Text.Encoding.UTF8.GetBytes(json);
            }

            return BuildSuccessResponse();
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"HandleChatHistory error: {ex.Message}");
            return BuildErrorResponse(99, "服务器内部错误");
        }
    }

    // ===== 工具方法 =====

    /// <summary>序列化ChatMessage为Binary格式</summary>
    public static byte[] SerializeChatMessage(ChatMessage msg)
    {
        using var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(msg.MsgId);
        w.Write(msg.Channel);
        w.Write(msg.SenderId);
        w.Write(msg.SenderName);
        w.Write(msg.SenderLevel);
        w.Write(msg.Content);
        w.Write(msg.MsgType);
        w.Write(msg.ExtraData);
        w.Write(msg.Timestamp);
        return ms.ToArray();
    }

    /// <summary>反序列化ChatMessage</summary>
    public static ChatMessage? DeserializeChatMessage(byte[] data)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(data));
            return new ChatMessage
            {
                MsgId = reader.ReadUInt64(),
                Channel = reader.ReadInt32(),
                SenderId = reader.ReadUInt64(),
                SenderName = reader.ReadString(),
                SenderLevel = reader.ReadInt32(),
                Content = reader.ReadString(),
                MsgType = reader.ReadInt32(),
                ExtraData = reader.ReadString(),
                Timestamp = reader.ReadInt64()
            };
        }
        catch
        {
            return null;
        }
    }

    private static byte[] BuildErrorResponse(int code, string msg)
    {
        using var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(code);
        w.Write(msg);
        return ms.ToArray();
    }

    private static byte[] BuildSuccessResponse()
    {
        using var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(0);
        return ms.ToArray();
    }
}

/// <summary>队伍信息（用于反序列化Redis）</summary>
internal class TeamInfo
{
    public ulong TeamId { get; set; }
    public List<ulong> Members { get; set; } = new();
}

/// <summary>请求DTO</summary>
internal class ChatHistoryReq
{
    public int Channel { get; set; }
}