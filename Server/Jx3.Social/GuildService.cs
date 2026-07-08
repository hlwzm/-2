using System.Collections.Concurrent;
using Jx3.Common.Utils;

namespace Jx3.Social;

public class GuildInfo
{
    public ulong GuildId;
    public string Name = "";
    public ulong LeaderId;
    public string LeaderName = "";
    public int Level = 1;
    public int MemberCount;
    public int MaxMembers = 50;
    public ulong Funds;
    public string Notice = "欢迎加入同盟！";
    public DateTime CreateTime;
}

public class GuildMember
{
    public ulong PlayerId;
    public string Name = "";
    public int Level;
    public int Position;
    public int Contribution;
    public bool Online;
}

public static class GuildService
{
    private static readonly Dictionary<ulong, GuildInfo> _guilds = new();
    private static readonly Dictionary<ulong, List<GuildMember>> _members = new();
    private static ulong _nextGuildId = 1;

    public static int CreateGuild(ulong playerId, string playerName, string guildName)
    {
        if (_guilds.Values.Any(g => g.Name == guildName)) return 1;
        if (_members.Values.Any(m => m.Any(mm => mm.PlayerId == playerId))) return 2;

        var guild = new GuildInfo
        {
            GuildId = _nextGuildId++,
            Name = guildName,
            LeaderId = playerId,
            LeaderName = playerName,
            CreateTime = DateTime.UtcNow,
        };
        _guilds[guild.GuildId] = guild;
        _members[guild.GuildId] = new()
        {
            new GuildMember { PlayerId = playerId, Name = playerName, Position = 1, Contribution = 100, Online = true }
        };
        guild.MemberCount = 1;
        Logger.Info("Guild", $"Guild '{guildName}' created by {playerName}");
        return 0;
    }

    public static int JoinGuild(ulong guildId, ulong playerId, string playerName)
    {
        if (!_guilds.TryGetValue(guildId, out var guild)) return 1;
        if (_members.Values.Any(m => m.Any(mm => mm.PlayerId == playerId))) return 2;
        if (guild.MemberCount >= guild.MaxMembers) return 3;

        _members[guildId].Add(new GuildMember { PlayerId = playerId, Name = playerName, Position = 5, Online = true });
        guild.MemberCount++;
        return 0;
    }

    public static bool LeaveGuild(ulong playerId)
    {
        foreach (var kv in _members)
        {
            if (kv.Value.RemoveAll(m => m.PlayerId == playerId) > 0)
            {
                if (_guilds.TryGetValue(kv.Key, out var g))
                {
                    g.MemberCount--;
                    if (g.MemberCount <= 0) { _guilds.Remove(kv.Key); }
                }
                return true;
            }
        }
        return false;
    }

    public static GuildInfo? FindPlayerGuild(ulong playerId)
    {
        foreach (var kv in _members)
            if (kv.Value.Any(m => m.PlayerId == playerId))
                return _guilds.GetValueOrDefault(kv.Key);
        return null;
    }

    public static bool KickMember(ulong guildId, ulong targetId, ulong operatorId)
    {
        if (!_guilds.TryGetValue(guildId, out var g)) return false;
        if (g.LeaderId != operatorId || targetId == operatorId) return false;
        if (_members[guildId].RemoveAll(m => m.PlayerId == targetId) > 0)
        {
            g.MemberCount--;
            return true;
        }
        return false;
    }
}