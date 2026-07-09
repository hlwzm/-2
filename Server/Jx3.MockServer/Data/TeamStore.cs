namespace Jx3.MockServer.Data;

public class TeamMember
{
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public int Level { get; set; }
    public bool Ready { get; set; }
}

public class TeamInfo
{
    public ulong TeamId { get; set; }
    public ulong LeaderPid { get; set; }
    public List<TeamMember> Members { get; set; } = new();
    public int MemberCount => Members.Count;
    public int MaxMembers { get; set; } = 5;
    public int LootMode { get; set; }
    public string Target { get; set; } = "";
}

public class TeamStore
{
    private static readonly Lazy<TeamStore> _instance = new(() => new TeamStore());
    public static TeamStore Instance => _instance.Value;
    private readonly Dictionary<ulong, TeamInfo> _teams = new();
    private ulong _nextTid = 1;

    public TeamInfo? Create(ulong leaderPid, string leaderName, int leaderLevel)
    {
        lock (_teams)
        {
            var t = new TeamInfo { TeamId = _nextTid++, LeaderPid = leaderPid, Members = new List<TeamMember> { new() { PlayerId = leaderPid, PlayerName = leaderName, Level = leaderLevel, Ready = true } } };
            _teams[t.TeamId] = t; return t;
        }
    }

    public TeamInfo? GetByPlayer(ulong pid) { lock (_teams) { return _teams.Values.FirstOrDefault(t => t.Members.Any(m => m.PlayerId == pid)); } }
    public TeamInfo? GetById(ulong tid) => _teams.GetValueOrDefault(tid);

    public bool AddMember(ulong teamId, ulong pid, string name, int level)
    {
        lock (_teams) { if (!_teams.TryGetValue(teamId, out var t)) return false; if (t.Members.Count >= t.MaxMembers || t.Members.Any(m => m.PlayerId == pid)) return false; t.Members.Add(new TeamMember { PlayerId = pid, PlayerName = name, Level = level }); return true; }
    }

    public bool RemoveMember(ulong teamId, ulong pid)
    {
        lock (_teams) { if (!_teams.TryGetValue(teamId, out var t)) return false; t.Members.RemoveAll(m => m.PlayerId == pid); if (t.Members.Count == 0) { _teams.Remove(teamId); return true; } if (t.LeaderPid == pid) t.LeaderPid = t.Members[0].PlayerId; return true; }
    }

    public bool Disband(ulong teamId) { lock (_teams) { return _teams.Remove(teamId); } }
    public void SetLootMode(ulong teamId, int mode) { lock (_teams) { if (_teams.TryGetValue(teamId, out var t)) t.LootMode = mode; } }
}
