namespace Jx3.MockServer.Data;

public class GuildMember
{
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public int Title { get; set; }
    public int Contribution { get; set; }
}

public class GuildInfo
{
    public ulong GuildId { get; set; }
    public string Name { get; set; } = "";
    public ulong LeaderPid { get; set; }
    public int Level { get; set; } = 1;
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; } = 50;
    public int Exp { get; set; }
    public int ExpToLevelUp { get; set; } = 1000;
    public string Notice { get; set; } = "欢迎加入！";
    public List<GuildMember> Members { get; set; } = new();
    public List<ulong> PendingApplies { get; set; } = new();
    public Dictionary<int, int> SkillLevels { get; set; } = new();
}

public class GuildStore
{
    private static readonly Lazy<GuildStore> _instance = new(() => new GuildStore());
    public static GuildStore Instance => _instance.Value;
    private readonly Dictionary<ulong, GuildInfo> _guilds = new();
    private ulong _nextGid = 1;

    public GuildInfo? Create(string name, ulong leaderPid, string leaderName)
    {
        lock (_guilds)
        {
            if (_guilds.Values.Any(g => g.Name == name)) return null;
            var g = new GuildInfo { GuildId = _nextGid++, Name = name, LeaderPid = leaderPid, Level = 1,
                Members = new List<GuildMember> { new() { PlayerId = leaderPid, PlayerName = leaderName, Title = 2 } },
                MemberCount = 1, SkillLevels = new Dictionary<int, int> { { 1, 1 }, { 2, 1 }, { 3, 1 } } };
            _guilds[g.GuildId] = g;
            return g;
        }
    }

    public GuildInfo? GetById(ulong gid) => _guilds.GetValueOrDefault(gid);
    public GuildInfo? GetByPlayer(ulong pid) { lock (_guilds) { return _guilds.Values.FirstOrDefault(g => g.Members.Any(m => m.PlayerId == pid)); } }
    public bool AddApply(ulong guildId, ulong pid) { lock (_guilds) { if (!_guilds.TryGetValue(guildId, out var g)) return false; if (!g.PendingApplies.Contains(pid)) g.PendingApplies.Add(pid); return true; } }

    public bool ApproveMember(ulong guildId, ulong leaderPid, ulong targetPid, string targetName)
    {
        lock (_guilds)
        {
            if (!_guilds.TryGetValue(guildId, out var g)) return false;
            if (g.LeaderPid != leaderPid) return false;
            if (!g.PendingApplies.Remove(targetPid)) return false;
            g.Members.Add(new GuildMember { PlayerId = targetPid, PlayerName = targetName });
            g.MemberCount = g.Members.Count;
            return true;
        }
    }

    public bool Kick(ulong guildId, ulong leaderPid, ulong targetPid)
    {
        lock (_guilds) { if (!_guilds.TryGetValue(guildId, out var g)) return false; if (g.LeaderPid != leaderPid) return false; g.Members.RemoveAll(m => m.PlayerId == targetPid); g.MemberCount = g.Members.Count; return true; }
    }

    public bool LeaveGuild(ulong guildId, ulong pid)
    {
        lock (_guilds) { if (!_guilds.TryGetValue(guildId, out var g)) return false; g.Members.RemoveAll(m => m.PlayerId == pid); g.MemberCount = g.Members.Count; return true; }
    }

    public bool UpgradeSkill(ulong guildId, int skillId, ulong requesterPid)
    {
        lock (_guilds) { if (!_guilds.TryGetValue(guildId, out var g)) return false; if (g.LeaderPid != requesterPid) return false; if (!g.SkillLevels.ContainsKey(skillId)) g.SkillLevels[skillId] = 0; if (g.SkillLevels[skillId] >= 10) return false; g.SkillLevels[skillId]++; return true; }
    }
}
