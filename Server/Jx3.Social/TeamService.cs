using Jx3.Common.Config;
using Jx3.Common.Database;
using Jx3.Common.Utils;
using StackExchange.Redis;
using System.Text.Json;

namespace Jx3.Social;

/// <summary>队伍信息</summary>
public class TeamInfo
{
    public ulong TeamId { get; set; }
    public ulong LeaderId { get; set; }
    public List<TeamMember> Members { get; set; } = new();
    public int MaxMembers { get; set; } = 5;
    public int LootMode { get; set; } = 1; // 1自由 2队长分配 3需求ROLL
    public int TargetDungeonId { get; set; }
    public int Difficulty { get; set; }
    public string? VoiceChannel { get; set; }
}

/// <summary>队伍成员</summary>
public class TeamMember
{
    public ulong PlayerId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int RoleType { get; set; } // 1输出 2坦克 3治疗
    public ulong HeroUid { get; set; }
    public bool IsLeader { get; set; }
    public bool Online { get; set; } = true;
}

/// <summary>招募信息</summary>
public class RecruitInfo
{
    public ulong TeamId { get; set; }
    public ulong LeaderId { get; set; }
    public string? LeaderName { get; set; }
    public int DungeonId { get; set; }
    public int Difficulty { get; set; }
    public string? Remark { get; set; }
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; }
}

/// <summary>组队服务</summary>
public class TeamService
{
    private readonly RedisHelper _redis;

    public TeamService(RedisHelper redis)
    {
        _redis = redis;
    }

    // ==================== 队伍操作 ====================

    /// <summary>创建队伍</summary>
    public async Task<string> CreateTeamAsync(ulong playerId, string playerName, int level, int roleType, ulong heroUid)
    {
        var teamId = (ulong)await _redis.Db.StringIncrementAsync("team:nextId");
        var key = $"team:{teamId}";

        // 保存队伍信息到Hash
        await _redis.Db.HashSetAsync(key, new HashEntry[]
        {
            new("leaderId", playerId.ToString()),
            new("maxMembers", GameConfig.TeamMaxMembers.ToString()),
            new("lootMode", "1"),
            new("dungeonId", "0"),
            new("difficulty", "0"),
            new("voiceChannel", ""),
            new("createdAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        });

        // 成员列表
        var membersKey = $"team:members:{teamId}";
        await _redis.Db.ListRightPushAsync(membersKey, playerId.ToString());

        // 成员信息
        var memberKey = $"team:member:{teamId}:{playerId}";
        await _redis.Db.HashSetAsync(memberKey, new HashEntry[]
        {
            new("name", playerName),
            new("level", level.ToString()),
            new("roleType", roleType.ToString()),
            new("heroUid", heroUid.ToString()),
            new("isLeader", "1"),
            new("online", "1")
        });

        // 设置队伍TTL (30分钟无活动自动解散)
        await _redis.Db.KeyExpireAsync(key, TimeSpan.FromMinutes(30));
        await _redis.Db.KeyExpireAsync(membersKey, TimeSpan.FromMinutes(30));

        Logger.Info("Team", $"Team created: {teamId} leader={playerId}");

        var team = new TeamInfo
        {
            TeamId = teamId,
            LeaderId = playerId,
            MaxMembers = GameConfig.TeamMaxMembers,
            Members = new List<TeamMember>
            {
                new() { PlayerId = playerId, Name = playerName, Level = level, RoleType = roleType, HeroUid = heroUid, IsLeader = true, Online = true }
            }
        };
        return JsonSerializer.Serialize(team);
    }

    /// <summary>获取队伍信息</summary>
    public async Task<TeamInfo?> GetTeamInfoAsync(ulong teamId)
    {
        var key = $"team:{teamId}";
        var exists = await _redis.Db.KeyExistsAsync(key);
        if (!exists) return null;

        var hash = await _redis.Db.HashGetAllAsync(key);
        var dict = hash.ToStringDictionary();

        var team = new TeamInfo
        {
            TeamId = teamId,
            LeaderId = ulong.Parse(dict.GetValueOrDefault("leaderId", "0")),
            MaxMembers = int.Parse(dict.GetValueOrDefault("maxMembers", "5")),
            LootMode = int.Parse(dict.GetValueOrDefault("lootMode", "1")),
            TargetDungeonId = int.Parse(dict.GetValueOrDefault("dungeonId", "0")),
            Difficulty = int.Parse(dict.GetValueOrDefault("difficulty", "0")),
            VoiceChannel = dict.GetValueOrDefault("voiceChannel", "")
        };

        // 获取成员列表
        var membersKey = $"team:members:{teamId}";
        var memberIds = await _redis.Db.ListRangeAsync(membersKey);
        foreach (var mid in memberIds)
        {
            var member = await GetMemberAsync(teamId, ulong.Parse(mid!));
            if (member != null) team.Members.Add(member);
        }

        return team;
    }

    /// <summary>获取成员信息</summary>
    private async Task<TeamMember?> GetMemberAsync(ulong teamId, ulong playerId)
    {
        var memberKey = $"team:member:{teamId}:{playerId}";
        var hash = await _redis.Db.HashGetAllAsync(memberKey);
        if (hash.Length == 0) return null;

        var dict = hash.ToStringDictionary();
        return new TeamMember
        {
            PlayerId = playerId,
            Name = dict.GetValueOrDefault("name", ""),
            Level = int.Parse(dict.GetValueOrDefault("level", "1")),
            RoleType = int.Parse(dict.GetValueOrDefault("roleType", "1")),
            HeroUid = ulong.Parse(dict.GetValueOrDefault("heroUid", "0")),
            IsLeader = dict.GetValueOrDefault("isLeader", "0") == "1",
            Online = dict.GetValueOrDefault("online", "1") == "1"
        };
    }

    /// <summary>获取玩家的队伍ID (通过遍历成员列表)<para/>
    /// 更高效方式：额外维护 player:team:{playerId} 映射</summary>
    private async Task<ulong> FindPlayerTeamAsync(ulong playerId)
    {
        var playerTeamKey = $"player:team:{playerId}";
        var val = await _redis.Db.StringGetAsync(playerTeamKey);
        if (val.HasValue && ulong.TryParse(val, out var tid) && tid > 0)
        {
            var key = $"team:{tid}";
            if (await _redis.Db.KeyExistsAsync(key))
                return tid;
        }
        return 0;
    }

    /// <summary>保存玩家-队伍映射</summary>
    private async Task SetPlayerTeamAsync(ulong playerId, ulong teamId)
    {
        if (teamId == 0)
            await _redis.Db.KeyDeleteAsync($"player:team:{playerId}");
        else
            await _redis.Db.StringSetAsync($"player:team:{playerId}", teamId.ToString(), TimeSpan.FromMinutes(30));
    }

    /// <summary>邀请加入</summary>
    public async Task<string?> InviteAsync(ulong inviterId, ulong targetId)
    {
        var teamId = await FindPlayerTeamAsync(inviterId);
        if (teamId == 0) return ErrJson("你不在队伍中");

        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");

        // 检查是否队长
        // 所有队员都可以邀请
        var inviteNotify = JsonSerializer.Serialize(new
        {
            team_id = teamId,
            inviter_id = inviterId,
            inviter_name = team.Members.FirstOrDefault(m => m.PlayerId == inviterId)?.Name ?? ""
        });
        return inviteNotify;
    }

    /// <summary>接受邀请</summary>
    public async Task<string> AcceptInviteAsync(ulong playerId, string playerName, int level, int roleType, ulong heroUid, ulong teamId)
    {
        return await JoinTeamAsync(playerId, playerName, level, roleType, heroUid, teamId);
    }

    /// <summary>申请加入 (公开队伍)</summary>
    public async Task<string?> ApplyAsync(ulong applicantId, string applicantName, ulong teamId)
    {
        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");

        // 发送申请通知给队长
        var applyNotify = JsonSerializer.Serialize(new
        {
            team_id = teamId,
            applicant_id = applicantId,
            applicant_name = applicantName
        });
        return applyNotify;
    }

    /// <summary>队长审批申请</summary>
    public async Task<string> ApproveApplyAsync(ulong leaderId, ulong applicantId, string playerName, int level, int roleType, ulong heroUid, ulong teamId)
    {
        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");
        if (team.LeaderId != leaderId) return ErrJson("只有队长可以审批");

        return await JoinTeamAsync(applicantId, playerName, level, roleType, heroUid, teamId);
    }

    /// <summary>加入队伍 (核心逻辑)</summary>
    private async Task<string> JoinTeamAsync(ulong playerId, string playerName, int level, int roleType, ulong heroUid, ulong teamId)
    {
        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");

        if (team.Members.Count >= team.MaxMembers)
            return ErrJson("队伍已满");

        // 检查是否已在队伍中
        var existingTeamId = await FindPlayerTeamAsync(playerId);
        if (existingTeamId > 0)
            return ErrJson("你已在其他队伍中");

        // 加入成员列表
        var membersKey = $"team:members:{teamId}";
        await _redis.Db.ListRightPushAsync(membersKey, playerId.ToString());

        // 保存成员信息
        var memberKey = $"team:member:{teamId}:{playerId}";
        await _redis.Db.HashSetAsync(memberKey, new HashEntry[]
        {
            new("name", playerName),
            new("level", level.ToString()),
            new("roleType", roleType.ToString()),
            new("heroUid", heroUid.ToString()),
            new("isLeader", "0"),
            new("online", "1")
        });

        await SetPlayerTeamAsync(playerId, teamId);

        // 更新队伍TTL
        await _redis.Db.KeyExpireAsync($"team:{teamId}", TimeSpan.FromMinutes(30));
        await _redis.Db.KeyExpireAsync(membersKey, TimeSpan.FromMinutes(30));

        Logger.Info("Team", $"Player {playerId} joined team {teamId}");

        // 返回最新队伍信息 + 加入通知
        var updatedTeam = await GetTeamInfoAsync(teamId);
        var result = new
        {
            team = updatedTeam,
            join_notify = new
            {
                team_id = teamId,
                player_id = playerId,
                player_name = playerName
            }
        };
        return JsonSerializer.Serialize(result);
    }

    /// <summary>离开队伍</summary>
    public async Task<string> LeaveTeamAsync(ulong playerId)
    {
        var teamId = await FindPlayerTeamAsync(playerId);
        if (teamId == 0) return ErrJson("你不在队伍中");

        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");

        // 从成员列表中移除
        var membersKey = $"team:members:{teamId}";
        await _redis.Db.ListRemoveAsync(membersKey, playerId.ToString(), 1);

        // 删除成员信息
        await _redis.Db.KeyDeleteAsync($"team:member:{teamId}:{playerId}");
        await SetPlayerTeamAsync(playerId, 0);

        bool isDisband = false;
        ulong newLeaderId = 0;

        if (team.LeaderId == playerId)
        {
            // 队长离开，转移给最早加入的人
            var remainingMembers = await _redis.Db.ListRangeAsync(membersKey);
            if (remainingMembers.Length > 0)
            {
                newLeaderId = ulong.Parse(remainingMembers[0]!);
                await _redis.Db.HashSetAsync($"team:{teamId}", "leaderId", newLeaderId.ToString());
                await _redis.Db.HashSetAsync($"team:member:{teamId}:{newLeaderId}", "isLeader", "1");
            }
            else
            {
                isDisband = true;
            }
        }

        if (isDisband)
        {
            // 所有人离开，解散队伍
            await _redis.Db.KeyDeleteAsync($"team:{teamId}");
            await _redis.Db.KeyDeleteAsync(membersKey);
            await _redis.Db.SetRemoveAsync("team:recruiting", teamId.ToString());
            Logger.Info("Team", $"Team {teamId} disbanded");
            return JsonSerializer.Serialize(new { code = 0, team_id = teamId, disband = true });
        }

        // 刷新TTL
        await _redis.Db.KeyExpireAsync($"team:{teamId}", TimeSpan.FromMinutes(30));
        await _redis.Db.KeyExpireAsync(membersKey, TimeSpan.FromMinutes(30));

        var updatedTeam = await GetTeamInfoAsync(teamId);
        var result = new
        {
            code = 0,
            team = updatedTeam,
            leave_notify = new { team_id = teamId, player_id = playerId, new_leader_id = newLeaderId }
        };
        return JsonSerializer.Serialize(result);
    }

    /// <summary>踢出成员 (仅队长)</summary>
    public async Task<string> KickMemberAsync(ulong leaderId, ulong targetId)
    {
        var teamId = await FindPlayerTeamAsync(leaderId);
        if (teamId == 0) return ErrJson("你不在队伍中");

        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");
        if (team.LeaderId != leaderId) return ErrJson("只有队长可以踢人");
        if (leaderId == targetId) return ErrJson("不能踢出自己");

        // 从成员列表移除
        var membersKey = $"team:members:{teamId}";
        await _redis.Db.ListRemoveAsync(membersKey, targetId.ToString(), 1);

        // 删除成员信息
        await _redis.Db.KeyDeleteAsync($"team:member:{teamId}:{targetId}");
        await SetPlayerTeamAsync(targetId, 0);

        Logger.Info("Team", $"Player {targetId} kicked from team {teamId} by {leaderId}");

        var updatedTeam = await GetTeamInfoAsync(teamId);
        return JsonSerializer.Serialize(new
        {
            code = 0,
            team = updatedTeam,
            kick_notify = new { team_id = teamId, player_id = targetId }
        });
    }

    /// <summary>队长转移</summary>
    public async Task<string> TransferLeaderAsync(ulong currentLeaderId, ulong newLeaderId)
    {
        var teamId = await FindPlayerTeamAsync(currentLeaderId);
        if (teamId == 0) return ErrJson("你不在队伍中");

        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");
        if (team.LeaderId != currentLeaderId) return ErrJson("只有队长可以转移");

        // 验证新队长是队员
        if (!team.Members.Any(m => m.PlayerId == newLeaderId))
            return ErrJson("目标不是队员");

        // 更新leader
        await _redis.Db.HashSetAsync($"team:{teamId}", "leaderId", newLeaderId.ToString());

        // 更新成员标记
        await _redis.Db.HashSetAsync($"team:member:{teamId}:{currentLeaderId}", "isLeader", "0");
        await _redis.Db.HashSetAsync($"team:member:{teamId}:{newLeaderId}", "isLeader", "1");

        Logger.Info("Team", $"Team {teamId} leader transferred from {currentLeaderId} to {newLeaderId}");

        var updatedTeam = await GetTeamInfoAsync(teamId);
        return JsonSerializer.Serialize(new
        {
            code = 0,
            team = updatedTeam,
            transfer_notify = new { team_id = teamId, old_leader = currentLeaderId, new_leader = newLeaderId }
        });
    }

    /// <summary>更新拾取模式 (仅队长)</summary>
    public async Task<string> SetLootModeAsync(ulong leaderId, ulong teamId, int lootMode)
    {
        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");
        if (team.LeaderId != leaderId) return ErrJson("只有队长可以修改");

        await _redis.Db.HashSetAsync($"team:{teamId}", "lootMode", lootMode.ToString());
        return JsonSerializer.Serialize(new { code = 0, team_id = teamId, loot_mode = lootMode });
    }

    /// <summary>队员上下线</summary>
    public async Task<string?> SetMemberOnlineAsync(ulong playerId, bool online)
    {
        var teamId = await FindPlayerTeamAsync(playerId);
        if (teamId == 0) return null;

        var memberKey = $"team:member:{teamId}:{playerId}";
        await _redis.Db.HashSetAsync(memberKey, "online", online ? "1" : "0");

        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return null;

        return JsonSerializer.Serialize(new
        {
            team_id = teamId,
            player_id = playerId,
            online,
            team
        });
    }

    // ==================== 招募系统 ====================

    /// <summary>发布招募</summary>
    public async Task<string> PublishRecruitAsync(ulong leaderId, int dungeonId, int difficulty, string? remark)
    {
        var teamId = await FindPlayerTeamAsync(leaderId);
        if (teamId == 0) return ErrJson("你不在队伍中");

        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");
        if (team.LeaderId != leaderId) return ErrJson("只有队长可以发布招募");

        // 更新队伍副本信息
        await _redis.Db.HashSetAsync($"team:{teamId}", new HashEntry[]
        {
            new("dungeonId", dungeonId.ToString()),
            new("difficulty", difficulty.ToString())
        });

        // 招募到Redis Set
        var recruitData = JsonSerializer.Serialize(new
        {
            team_id = teamId,
            leader_id = leaderId,
            leader_name = team.Members.FirstOrDefault(m => m.PlayerId == leaderId)?.Name ?? "",
            dungeon_id = dungeonId,
            difficulty,
            remark = remark ?? "",
            member_count = team.Members.Count,
            max_members = team.MaxMembers
        });
        await _redis.Db.SetAddAsync("team:recruiting", recruitData);
        // 同时存teamId方便查找
        await _redis.Db.SetAddAsync("team:recruiting", teamId.ToString());

        return JsonSerializer.Serialize(new { code = 0, team_id = teamId, recruit = recruitData });
    }

    /// <summary>搜索招募</summary>
    public async Task<string> SearchRecruitAsync(int? dungeonId, int? difficulty, int page, int pageSize)
    {
        var members = await _redis.Db.SetMembersAsync("team:recruiting");
        var recruits = new List<RecruitInfo>();

        // 从Set中解析招募信息 (偶数索引是JSON, 奇数索引是teamId)
        foreach (var member in members)
        {
            var val = member.ToString();
            if (!val.StartsWith("{")) continue; // skip teamId-only entries

            try
            {
                var recruit = JsonSerializer.Deserialize<RecruitInfo>(val);
                if (recruit == null) continue;

                if (dungeonId.HasValue && recruit.DungeonId != dungeonId.Value) continue;
                if (difficulty.HasValue && recruit.Difficulty != difficulty.Value) continue;

                recruits.Add(recruit);
            }
            catch { }
        }

        // 分页
        var total = recruits.Count;
        var paged = recruits.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return JsonSerializer.Serialize(new { code = 0, total, recruits = paged, page, page_size = pageSize });
    }

    /// <summary>取消招募</summary>
    public async Task<string> RemoveRecruitAsync(ulong leaderId, ulong teamId)
    {
        var team = await GetTeamInfoAsync(teamId);
        if (team == null) return ErrJson("队伍不存在");
        if (team.LeaderId != leaderId) return ErrJson("只有队长可以操作");

        // 从Set中移除所有该teamId的条目
        var members = await _redis.Db.SetMembersAsync("team:recruiting");
        foreach (var member in members)
        {
            var val = member.ToString();
            if (val.Contains($"\"team_id\":{teamId}") || val == teamId.ToString())
            {
                await _redis.Db.SetRemoveAsync("team:recruiting", member);
            }
        }

        return JsonSerializer.Serialize(new { code = 0, team_id = teamId });
    }

    // ==================== 匹配系统 ====================

    private static readonly Dictionary<ulong, MatchEntry> _matchQueue = new();

    public class MatchEntry
    {
        public ulong PlayerId { get; set; }
        public ulong TeamId { get; set; }
        public int DungeonId { get; set; }
        public int Difficulty { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>开始匹配</summary>
    public string StartMatch(ulong playerId, int dungeonId, int difficulty)
    {
        // 简化实现：加入匹配队列，返回队列信息
        if (_matchQueue.ContainsKey(playerId))
            return ErrJson("已在匹配队列中");

        _matchQueue[playerId] = new MatchEntry
        {
            PlayerId = playerId,
            DungeonId = dungeonId,
            Difficulty = difficulty,
            QueuedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(new { code = 0, msg = "开始匹配", dungeon_id = dungeonId, difficulty });
    }

    /// <summary>取消匹配</summary>
    public string CancelMatch(ulong playerId)
    {
        _matchQueue.Remove(playerId);
        return JsonSerializer.Serialize(new { code = 0, msg = "已取消匹配" });
    }

    // ==================== 工具 ====================

    private static string ErrJson(string msg)
    {
        return JsonSerializer.Serialize(new { code = 1, msg });
    }
}