#nullable disable
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Database;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;
using Jx3.Social;
using System.Text.Json;

namespace Jx3.Social;

/// <summary>
/// 社交服务器 - 组队 + 好友
/// Port: 9004 (Gateway+4)
/// </summary>
public class SocialServer : GameServer
{
    public SocialServer() : base("Social", GameConfig.SocialPort) { }

    protected override async Task OnStartAsync()
    {
        Logger.Info("Social", "Initializing Team + Friend services...");

        var db = new DbHelper(GameConfig.MySQLConn);
        var redis = new RedisHelper(GameConfig.RedisConn);
        var teamSvc = new TeamService(redis);
        var friendSvc = new FriendService(db, redis);

        ServiceRegistry.RegisterService("TeamService", teamSvc);
        ServiceRegistry.RegisterService("FriendService", friendSvc);

        // ======================== 组队消息 ========================

        // CSTeamCreate (6001) -> SCTeamInfo (6002)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamCreate, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamCreateReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.CreateTeamAsync(req.player_id, req.name, req.level, req.role_type, req.hero_uid);
                return EncodeResponse((uint)MsgId.SCTeamInfo, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamCreate error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamInvite (6003) -> SCTeamInvite (6004)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamInvite, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamInviteReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.InviteAsync(req.player_id, req.target_id);
                if (result == null) return ErrJson("邀请失败");
                return EncodeResponse((uint)MsgId.SCTeamInviteReceive, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamInvite error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamInviteAccept (6011) -> SCTeamInfo (6002)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamInviteAccept, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamInviteAcceptReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.AcceptInviteAsync(req.player_id, req.name, req.level, req.role_type, req.hero_uid, req.team_id);
                return EncodeResponse((uint)MsgId.SCTeamInfo, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamInviteAccept error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamLeave (6005) -> SCTeamMemberLeave (6006) / SCTeamDisband (6008)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamLeave, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<PlayerOnlyReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.LeaveTeamAsync(req.player_id);

                // 检查是否解散
                var parsed = JsonDocument.Parse(result);
                if (parsed.RootElement.TryGetProperty("disband", out var disband) && disband.GetBoolean())
                {
                    return EncodeResponse((uint)MsgId.SCTeamDisband, result);
                }
                return EncodeResponse((uint)MsgId.SCTeamMemberLeave, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamLeave error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamKick (6009) -> SCTeamKick (6022)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamKick, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamKickReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.KickMemberAsync(req.player_id, req.target_id);
                return EncodeResponse((uint)(MsgId)6026u, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamKick error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamTransfer (6010) -> SCTeamTransfer (6023)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamTransfer, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamTransferReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.TransferLeaderAsync(req.player_id, req.new_leader_id);
                return EncodeResponse((uint)(MsgId)6027u, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamTransfer error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamApply (6013) -> SCTeamApplyNotify (6014)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamApply, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamApplyReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.ApplyAsync(req.player_id, req.name, req.team_id);
                if (result == null) return ErrJson("申请失败");
                return EncodeResponse((uint)MsgId.SCTeamApplyNotify, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamApply error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamApplyApprove (6015) -> SCTeamMemberJoin (6007)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamApplyApprove, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamApproveReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.ApproveApplyAsync(req.player_id, req.applicant_id, req.name, req.level, req.role_type, req.hero_uid, req.team_id);
                return EncodeResponse((uint)MsgId.SCTeamMemberJoin, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamApplyApprove error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamRecruitPublish (6017) -> SCTeamInfo (6002)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamRecruitPublish, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<RecruitPublishReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.PublishRecruitAsync(req.player_id, req.dungeon_id, req.difficulty, req.remark);
                return EncodeResponse((uint)MsgId.SCTeamInfo, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamRecruitPublish error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamRecruitSearch (6018) -> SCTeamRecruitList (6019)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamRecruitSearch, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<RecruitSearchReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.SearchRecruitAsync(req.dungeon_id, req.difficulty, req.page, req.page_size);
                return EncodeResponse((uint)MsgId.SCTeamRecruitList, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamRecruitSearch error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamLootMode (6020) -> SCTeamLootMode (6021)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamLootMode, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamLootModeReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await teamSvc.SetLootModeAsync(req.player_id, req.team_id, req.loot_mode);
                return EncodeResponse((uint)(MsgId)6021u, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamLootMode error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSTeamMatchStart (6012) -> SCTeamMatchResult (6014)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamMatchStart, body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamMatchReq>(body);
                if (req == null) return Task.FromResult(ErrJson("参数错误"));
                var result = teamSvc.StartMatch(req.player_id, req.dungeon_id, req.difficulty);
                return Task.FromResult(EncodeResponse((uint)MsgId.SCTeamMatchResult, result));
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamMatchStart error: {ex.Message}");
                return Task.FromResult(ErrJson("服务器内部错误"));
            }
        });

        // CSTeamMatchCancel (6013) -> SCTeamMatchResult (6014)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSTeamMatchCancel, body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<PlayerOnlyReq>(body);
                if (req == null) return Task.FromResult(ErrJson("参数错误"));
                var result = teamSvc.CancelMatch(req.player_id);
                return Task.FromResult(EncodeResponse((uint)MsgId.SCTeamMatchResult, result));
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSTeamMatchCancel error: {ex.Message}");
                return Task.FromResult(ErrJson("服务器内部错误"));
            }
        });

        // ======================== 好友消息 ========================

        // CSFriendAdd (7005) -> SCFriendRequest (7011)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSFriendAdd, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<FriendAddReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await friendSvc.AddFriendAsync(req.player_id, req.target_id, req.name);
                return EncodeResponse((uint)MsgId.SCFriendRequest, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSFriendAdd error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSFriendAccept (7009) -> SCFriendRequest (7011)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSFriendAccept, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<FriendActionReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await friendSvc.AcceptFriendAsync(req.player_id, req.target_id);
                return EncodeResponse((uint)MsgId.SCFriendRequest, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSFriendAccept error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSFriendDecline (7010) -> SCFriendRequest (7011)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSFriendDecline, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<FriendActionReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await friendSvc.DeclineFriendAsync(req.player_id, req.target_id);
                return EncodeResponse((uint)MsgId.SCFriendRequest, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSFriendDecline error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSFriendRemove (7006) -> SCFriendRequest (7011)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSFriendRemove, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<FriendActionReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await friendSvc.RemoveFriendAsync(req.player_id, req.target_id);
                return EncodeResponse((uint)MsgId.SCFriendRequest, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSFriendRemove error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSFriendList (7007) -> SCFriendList (7008)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSFriendList, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<PlayerOnlyReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await friendSvc.GetFriendListAsync(req.player_id);
                return EncodeResponse((uint)MsgId.SCFriendList, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Social", $"CSFriendList error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        Logger.Info("Social", "Team + Friend services ready, handlers registered");
        await Task.CompletedTask;
    }

    // ======================== 工具方法 ========================

    private static byte[]? EncodeResponse(uint msgId, string jsonBody)
    {
        var packet = new Jx3.Common.Network.MessagePacket
        {
            MsgId = msgId,
            Body = System.Text.Encoding.UTF8.GetBytes(jsonBody)
        };
        return packet.Encode();
    }

    private static byte[]? ErrJson(string msg)
    {
        var json = JsonSerializer.Serialize(new { code = 1, msg });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}

// ======================== DTO ========================

internal class PlayerOnlyReq
{
    public ulong player_id { get; set; }
}

internal class TeamCreateReq
{
    public ulong player_id { get; set; }
    public string name { get; set; } = "";
    public int level { get; set; } = 1;
    public int role_type { get; set; } = 1;
    public ulong hero_uid { get; set; }
}

internal class TeamInviteReq
{
    public ulong player_id { get; set; }
    public ulong target_id { get; set; }
}

internal class TeamInviteAcceptReq
{
    public ulong player_id { get; set; }
    public ulong team_id { get; set; }
    public string name { get; set; } = "";
    public int level { get; set; } = 1;
    public int role_type { get; set; } = 1;
    public ulong hero_uid { get; set; }
}

internal class TeamKickReq
{
    public ulong player_id { get; set; }
    public ulong target_id { get; set; }
}

internal class TeamTransferReq
{
    public ulong player_id { get; set; }
    public ulong new_leader_id { get; set; }
}

internal class TeamApplyReq
{
    public ulong player_id { get; set; }
    public string name { get; set; } = "";
    public ulong team_id { get; set; }
}

internal class TeamApproveReq
{
    public ulong player_id { get; set; }
    public ulong applicant_id { get; set; }
    public string name { get; set; } = "";
    public int level { get; set; } = 1;
    public int role_type { get; set; } = 1;
    public ulong hero_uid { get; set; }
    public ulong team_id { get; set; }
}

internal class TeamLootModeReq
{
    public ulong player_id { get; set; }
    public ulong team_id { get; set; }
    public int loot_mode { get; set; }
}

internal class TeamMatchReq
{
    public ulong player_id { get; set; }
    public int dungeon_id { get; set; }
    public int difficulty { get; set; }
}

internal class FriendAddReq
{
    public ulong player_id { get; set; }
    public ulong target_id { get; set; }
    public string? name { get; set; }
}

internal class FriendActionReq
{
    public ulong player_id { get; set; }
    public ulong target_id { get; set; }
}

internal class RecruitPublishReq
{
    public ulong player_id { get; set; }
    public int dungeon_id { get; set; }
    public int difficulty { get; set; }
    public string? remark { get; set; }
}

internal class RecruitSearchReq
{
    public int? dungeon_id { get; set; }
    public int? difficulty { get; set; }
    public int page { get; set; } = 1;
    public int page_size { get; set; } = 20;
}

// ======================== 入口 ========================

public class Program
{
    public static async Task Main()
    {
        GameConfigLoader.Load();
        await new SocialServer().StartAsync();
    }

    private Task<byte[]> HandleGuildCreate(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var pid = r.ReadUInt64(); var name = r.ReadString(); var guildName = r.ReadString();
        var code = GuildService.CreateGuild(pid, name, guildName);
        return Task.FromResult(BitConverter.GetBytes(code));
    }
    private Task<byte[]> HandleGuildApply(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var pid = r.ReadUInt64(); var name = r.ReadString(); var gid = r.ReadUInt64();
        var code = GuildService.JoinGuild(gid, pid, name);
        return Task.FromResult(BitConverter.GetBytes(code));
    }
    private Task<byte[]> HandleGuildLeave(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var pid = r.ReadUInt64();
        var ok = GuildService.LeaveGuild(pid);
        return Task.FromResult(new[] { ok ? (byte)1 : (byte)0 });
    }
    private Task<byte[]> HandleGuildKick(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var gid = r.ReadUInt64(); var target = r.ReadUInt64(); var op = r.ReadUInt64();
        var ok = GuildService.KickMember(gid, target, op);
        return Task.FromResult(new[] { ok ? (byte)1 : (byte)0 });
    }
}