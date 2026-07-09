#nullable disable
using System.Net;
using System.Net.Sockets;
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Database;
using Jx3.Common.Network;
using Jx3.Common.Protocol;
using Jx3.Common.Utils;
using BCrypt.Net;

namespace Jx3.Login;

/// <summary>登录服务器 - 认证/注册/角色创建</summary>
public class LoginServer : GameServer
{
    private TcpListener? _listener;

    public LoginServer() : base("Login", GameConfig.LoginPort) { }

    protected override async Task OnStartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        Logger.Info("Login", $"Listening on port {Port}...");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            Logger.Info("Login", $"Client connected: {client.Client.RemoteEndPoint}");
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var buffer = new byte[1024 * 64];
        var stream = client.GetStream();

        try
        {
            while (client.Connected)
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

                Logger.Info("Login", $"Received MsgId={packet.MsgId}");

                byte[]? responseBody = packet.MsgId switch
                {
                    (uint)MsgId.CSLoginRegister => await HandleRegister(packet.Body),
                    (uint)MsgId.CSLoginAuth => await HandleAuth(packet.Body),
                    (uint)MsgId.CSLoginCreateRole => await HandleCreateRole(packet.Body),
                    _ => null
                };

                if (responseBody == null) continue;

                var respPacket = new MessagePacket
                {
                    MsgId = packet.MsgId + 1,
                    Seq = packet.Seq,
                    Body = responseBody
                };
                var respData = respPacket.Encode();
                var lenBytes = BitConverter.GetBytes((uint)respData.Length);
                await stream.WriteAsync(lenBytes);
                await stream.WriteAsync(respData);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Login", $"Client error: {ex.Message}");
        }
        finally
        {
            client.Close();
            Logger.Info("Login", "Client disconnected");
        }
    }

    // ===== 消息处理器 =====

    /// <summary>注册 CSLoginRegister(1003) -> SCLoginRegisterResult(1004)</summary>
    private async Task<byte[]> HandleRegister(byte[] body)
    {
        using var reader = new BinaryReader(new MemoryStream(body));
        var phone = reader.ReadString();
        var password = reader.ReadString();

        Logger.Info("Login", $"Register: phone={phone}");

        try
        {
            using var db = new DbHelper();
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var sql = "INSERT INTO account (phone, password) VALUES (@Phone, @Password); SELECT LAST_INSERT_ID();";
            var accountId = await db.ExecuteScalarAsync<ulong>(sql, new { Phone = phone, Password = hash });

            Logger.Info("Login", $"Account created: id={accountId}");

            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms);
                w.Write((uint)0);
                w.Write(accountId);
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Login", $"Register failed: {ex.Message}");
            return BuildError(1);
        }
    }

    /// <summary>登录 CSLoginAuth(1001) -> SCLoginAuthResult(1002)</summary>
    private async Task<byte[]> HandleAuth(byte[] body)
    {
        using var reader = new BinaryReader(new MemoryStream(body));
        var phone = reader.ReadString();
        var password = reader.ReadString();

        Logger.Info("Login", $"Auth: phone={phone}");

        try
        {
            ulong accountId;
            string storedHash;

            using (var db = new DbHelper())
            {
                var sql = "SELECT account_id, password FROM account WHERE phone = @Phone AND status = 1";
                var row = await db.QueryFirstOrDefaultAsync<AccountRow>(sql, new { Phone = phone });
                if (row == null)
                {
                    Logger.Warn("Login", $"Auth failed: account not found phone={phone}");
                    return BuildError(1);
                }
                accountId = row.account_id;
                storedHash = row.password;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, storedHash))
            {
                Logger.Warn("Login", $"Auth failed: wrong password accountId={accountId}");
                return BuildError(1);
            }

            var token = Guid.NewGuid().ToString("N");
            using (var redis = new RedisHelper())
            {
                await redis.SetAsync($"session:{token}", accountId.ToString(), TimeSpan.FromDays(7));
            }

            Logger.Info("Login", $"Auth success: accountId={accountId} token={token[..8]}...");

            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms);
                w.Write((uint)0);
                w.Write(token);
                w.Write(accountId);
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Login", $"Auth error: {ex.Message}");
            return BuildError(2);
        }
    }

    /// <summary>创建角色 CSLoginCreateRole(1005) -> SCLoginRoleList(1006)</summary>
    private async Task<byte[]> HandleCreateRole(byte[] body)
    {
        using var reader = new BinaryReader(new MemoryStream(body));
        var accountId = reader.ReadUInt64();

        Logger.Info("Login", $"CreateRole: accountId={accountId}");

        try
        {
            using var db = new DbHelper();

            // 检查是否已有角色
            var existing = await db.QueryFirstOrDefaultAsync<ulong>(
                "SELECT player_id FROM player WHERE account_id = @AccountId LIMIT 1",
                new { AccountId = accountId });
            if (existing > 0)
            {
                Logger.Warn("Login", $"CreateRole: already exists accountId={accountId} playerId={existing}");
                using (var ms = new MemoryStream())
                {
                    var w = new BinaryWriter(ms);
                    w.Write((uint)0);
                    w.Write(existing);
                    return ms.ToArray();
                }
            }

            // 创建玩家
            var playerName = $"侠客_{accountId}";
            var createSql = @"
                INSERT INTO player (account_id, name, level, gold, map_id)
                VALUES (@AccountId, @Name, 1, 0, 1001);
                SELECT LAST_INSERT_ID();";
            var playerId = await db.ExecuteScalarAsync<ulong>(createSql,
                new { AccountId = accountId, Name = playerName });

            // 赠送初始英雄: 李复(template_id=1001), 陈月(template_id=1003)
            var heroSql = "INSERT INTO hero (player_id, template_id, level, star) VALUES (@PlayerId, @Tid, 1, 1);";
            await db.ExecuteAsync(heroSql, new { PlayerId = playerId, Tid = 1001 });
            await db.ExecuteAsync(heroSql, new { PlayerId = playerId, Tid = 1003 });

            Logger.Info("Login", $"Role created: playerId={playerId}");

            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms);
                w.Write((uint)0);
                w.Write(playerId);
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Login", $"CreateRole failed: {ex.Message}");
            return BuildError(1);
        }
    }

    // ===== 内部接口 =====

    /// <summary>Token验证 (内部接口) - 查Redis返回playerId, 失败返回0</summary>
    public async Task<ulong> ValidateTokenAsync(string token)
    {
        try
        {
            string? value;
            using (var redis = new RedisHelper())
            {
                value = await redis.GetAsync($"session:{token}");
            }

            if (string.IsNullOrEmpty(value)) return 0;
            if (!ulong.TryParse(value, out var accountId)) return 0;

            using var db = new DbHelper();
            var playerId = await db.QueryFirstOrDefaultAsync<ulong>(
                "SELECT player_id FROM player WHERE account_id = @AccountId LIMIT 1",
                new { AccountId = accountId });

            Logger.Info("Login", $"ValidateToken: accountId={accountId} playerId={playerId}");
            return playerId;
        }
        catch (Exception ex)
        {
            Logger.Error("Login", $"ValidateToken error: {ex.Message}");
            return 0;
        }
    }

    // ===== 工具方法 =====

    private static byte[] BuildError(uint errorCode)
    {
        using (var ms = new MemoryStream())
        {
            var w = new BinaryWriter(ms);
            w.Write(errorCode);
            w.Write((ulong)0);
            return ms.ToArray();
        }
    }

    private record AccountRow(ulong account_id, string password);
}

public class Program
{
    public static async Task Main()
    {
        GameConfigLoader.Load();
        await new LoginServer().StartAsync();
    }
}