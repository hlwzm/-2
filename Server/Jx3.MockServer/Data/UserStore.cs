namespace Jx3.MockServer.Data;

public class UserSession
{
    public ulong PlayerId { get; set; }
    public string Token { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int Level { get; set; } = 1;
    public uint Exp { get; set; }
    public ulong Gold { get; set; } = 50000;
    public ulong Gem { get; set; } = 1000;
    public ulong Tribute { get; set; } = 300;
    public ulong PvpScore { get; set; }
    public int PvpRank { get; set; }
}

public class UserStore
{
    private static readonly Lazy<UserStore> _instance = new(() => new UserStore());
    public static UserStore Instance => _instance.Value;

    private readonly Dictionary<ulong, UserSession> _users = new();
    private readonly Dictionary<string, ulong> _tokenMap = new();
    private ulong _nextPid = 10001;

    public UserSession GetOrCreateUser(string phone, string pwd)
    {
        lock (_users)
        {
            var existing = _users.Values.FirstOrDefault(u => u.Phone == phone);
            if (existing != null) return existing;
            var pid = _nextPid++;
            var user = new UserSession
            {
                PlayerId = pid, Phone = phone,
                Token = "tok_" + Guid.NewGuid().ToString("N")[..8],
                PlayerName = "侠客_" + pid, Level = 1,
                Gold = 50000, Gem = 1000, Tribute = 300
            };
            _users[pid] = user;
            _tokenMap[user.Token] = pid;
            return user;
        }
    }

    public UserSession? GetByToken(string token)
    {
        lock (_users)
        {
            if (_tokenMap.TryGetValue(token, out var pid))
                return _users.GetValueOrDefault(pid);
            return null;
        }
    }

    public UserSession? GetByPid(ulong pid)
    {
        lock (_users) { return _users.GetValueOrDefault(pid); }
    }

    public void AddGold(ulong pid, ulong amount)
    {
        lock (_users) { if (_users.TryGetValue(pid, out var u)) u.Gold += amount; }
    }

    public bool SpendGold(ulong pid, ulong amount)
    {
        lock (_users)
        {
            if (!_users.TryGetValue(pid, out var u) || u.Gold < amount) return false;
            u.Gold -= amount;
            return true;
        }
    }
}
