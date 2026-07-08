using Jx3.Common.Database;
using Jx3.Common.Utils;
using System.Text.Json;

namespace Jx3.Social;

public class FriendInfo
{
    public ulong PlayerId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public bool Online { get; set; }
    public string? LastOnline { get; set; }
}

public class FriendService
{
    private readonly DbHelper _db;
    private readonly RedisHelper _redis;

    public FriendService(DbHelper db, RedisHelper redis)
    {
        _db = db;
        _redis = redis;
    }

    public async Task<string> AddFriendAsync(ulong fromId, ulong toId, string? fromName)
    {
        if (fromId == toId)
            return ErrJson("\u4e0d\u80fd\u6dfb\u52a0\u81ea\u5df1\u4e3a\u597d\u53cb");

        var existing = await _db.QueryFirstOrDefaultAsync<ulong>(
            "SELECT id FROM friend WHERE (player_id = @FromId AND friend_id = @ToId) OR (player_id = @ToId AND friend_id = @FromId) LIMIT 1",
            new { FromId = fromId, ToId = toId });
        if (existing > 0)
            return ErrJson("\u5df2\u662f\u597d\u53cb");

        var pendingReq = await _db.QueryFirstOrDefaultAsync<ulong>(
            "SELECT id FROM friend_request WHERE from_id = @FromId AND to_id = @ToId AND status = 0 LIMIT 1",
            new { FromId = fromId, ToId = toId });
        if (pendingReq > 0)
            return ErrJson("\u5df2\u53d1\u9001\u8fc7\u597d\u53cb\u8bf7\u6c42\uff0c\u8bf7\u7b49\u5f85\u5bf9\u65b9\u5904\u7406");

        var reverseReq = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, from_id, from_name FROM friend_request WHERE from_id = @ToId AND to_id = @FromId AND status = 0 LIMIT 1",
            new { FromId = fromId, ToId = toId });
        if (reverseReq != null)
        {
            var reqIdObj = reverseReq.GetType().GetProperty("id")?.GetValue(reverseReq);
            if (reqIdObj != null && Convert.ToUInt64(reqIdObj) > 0)
            {
                var reqId = Convert.ToUInt64(reqIdObj);
                await _db.ExecuteAsync("UPDATE friend_request SET status = 1 WHERE id = @Id", new { Id = reqId });
                await _db.ExecuteAsync("INSERT INTO friend (player_id, friend_id) VALUES (@A, @B), (@B, @A)", new { A = fromId, B = toId });
                Logger.Info("Friend", $"Auto-accepted: {fromId} <-> {toId}");
                return JsonSerializer.Serialize(new { code = 0, msg = "\u5df2\u81ea\u52a8\u63a5\u53d7\u5bf9\u65b9\u7684\u597d\u53cb\u8bf7\u6c42\uff0c\u73b0\u5728\u4f60\u4eec\u662f\u597d\u53cb\u4e86", auto_accepted = true });
            }
        }

        await _db.ExecuteAsync(
            "INSERT INTO friend_request (from_id, to_id, from_name, status) VALUES (@FromId, @ToId, @Name, 0)",
            new { FromId = fromId, ToId = toId, Name = fromName ?? "" });
        Logger.Info("Friend", $"Friend request: {fromId} -> {toId}");
        return JsonSerializer.Serialize(new { code = 0, msg = "\u597d\u53cb\u8bf7\u6c42\u5df2\u53d1\u9001" });
    }

    public async Task<string> AcceptFriendAsync(ulong playerId, ulong requesterId)
    {
        var req = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM friend_request WHERE from_id = @FromId AND to_id = @ToId AND status = 0 LIMIT 1",
            new { FromId = requesterId, ToId = playerId });
        if (req == null) return ErrJson("\u6ca1\u6709\u627e\u5230\u597d\u53cb\u8bf7\u6c42");

        var idObj = req.GetType().GetProperty("id")?.GetValue(req);
        if (idObj == null) return ErrJson("\u6ca1\u6709\u627e\u5230\u597d\u53cb\u8bf7\u6c42");
        await _db.ExecuteAsync("UPDATE friend_request SET status = 1 WHERE id = @Id", new { Id = Convert.ToUInt64(idObj) });
        await _db.ExecuteAsync("INSERT INTO friend (player_id, friend_id) VALUES (@A, @B), (@B, @A)", new { A = playerId, B = requesterId });
        Logger.Info("Friend", $"Friend accepted: {requesterId} <-> {playerId}");
        return JsonSerializer.Serialize(new { code = 0, msg = "\u5df2\u6dfb\u52a0\u597d\u53cb", player_id = requesterId });
    }

    public async Task<string> DeclineFriendAsync(ulong playerId, ulong requesterId)
    {
        await _db.ExecuteAsync(
            "UPDATE friend_request SET status = 2 WHERE from_id = @FromId AND to_id = @ToId AND status = 0",
            new { FromId = requesterId, ToId = playerId });
        return JsonSerializer.Serialize(new { code = 0, msg = "\u5df2\u62d2\u7edd" });
    }

    public async Task<string> RemoveFriendAsync(ulong playerId, ulong friendId)
    {
        await _db.ExecuteAsync(
            "DELETE FROM friend WHERE (player_id = @Pid AND friend_id = @Fid) OR (player_id = @Fid AND friend_id = @Pid)",
            new { Pid = playerId, Fid = friendId });
        Logger.Info("Friend", $"Friend removed: {playerId} - {friendId}");
        return JsonSerializer.Serialize(new { code = 0, msg = "\u5df2\u5220\u9664\u597d\u53cb" });
    }

    public async Task<string> GetFriendListAsync(ulong playerId)
    {
        var friendIds = await _db.QueryAsync<ulong>(
            "SELECT friend_id FROM friend WHERE player_id = @PlayerId", new { PlayerId = playerId });

        var friendList = new List<FriendInfo>();
        foreach (var fid in friendIds)
        {
            var player = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT name, level FROM player WHERE player_id = @PlayerId LIMIT 1", new { PlayerId = fid });
            if (player == null) continue;

            var nameProp = player.GetType().GetProperty("name");
            var levelProp = player.GetType().GetProperty("level");
            if (nameProp == null || levelProp == null) continue;

            #pragma warning disable CS8602
            object? rawName = nameProp.GetValue(player);
            object? rawLevel = levelProp.GetValue(player);
#pragma warning restore CS8602
            string playerName = rawName?.ToString() ?? "";
            int playerLevel = rawLevel != null ? Convert.ToInt32(rawLevel) : 1;
            bool online = await CheckOnlineAsync(fid);

            friendList.Add(new FriendInfo { PlayerId = fid, Name = playerName, Level = playerLevel, Online = online });
        }
        return JsonSerializer.Serialize(new { code = 0, friends = friendList });
    }

    public async Task<bool> CheckOnlineAsync(ulong playerId)
    {
        var val = await _redis.GetAsync($"online:{playerId}");
        return val == "1";
    }

    public async Task SetOnlineAsync(ulong playerId, bool online)
    {
        if (online)
            await _redis.SetAsync($"online:{playerId}", "1", TimeSpan.FromMinutes(30));
        else
            await _redis.DeleteAsync($"online:{playerId}");
    }

    public async Task<List<ulong>> GetFriendIdsAsync(ulong playerId)
    {
        var result = await _db.QueryAsync<ulong>(
            "SELECT friend_id FROM friend WHERE player_id = @PlayerId", new { PlayerId = playerId });
        return result.ToList();
    }

    private static string ErrJson(string msg)
    {
        return JsonSerializer.Serialize(new { code = 1, msg });
    }
}