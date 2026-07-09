namespace Jx3.MockServer.Data;

public class FriendInfo
{
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public int Level { get; set; }
    public bool Online { get; set; }
    public DateTime AddedAt { get; set; }
}

public class FriendStore
{
    private static readonly Lazy<FriendStore> _instance = new(() => new FriendStore());
    public static FriendStore Instance => _instance.Value;
    private readonly Dictionary<ulong, List<FriendInfo>> _friends = new();
    private readonly Dictionary<ulong, List<ulong>> _pendingRequests = new();

    public bool AddFriend(ulong pid, ulong friendPid, string friendName, int friendLevel)
    {
        lock (_friends)
        {
            if (!_friends.ContainsKey(pid)) _friends[pid] = new();
            if (_friends[pid].Any(f => f.PlayerId == friendPid)) return false;
            _friends[pid].Add(new FriendInfo { PlayerId = friendPid, PlayerName = friendName, Level = friendLevel, Online = true, AddedAt = DateTime.UtcNow });
            return true;
        }
    }

    public bool RemoveFriend(ulong pid, ulong friendPid)
    {
        lock (_friends) { if (!_friends.ContainsKey(pid)) return false; return _friends[pid].RemoveAll(f => f.PlayerId == friendPid) > 0; }
    }

    public List<FriendInfo> GetFriends(ulong pid)
    {
        lock (_friends) { if (!_friends.ContainsKey(pid)) _friends[pid] = new(); return _friends[pid].ToList(); }
    }

    public void SendRequest(ulong fromPid, ulong toPid)
    {
        lock (_pendingRequests) { if (!_pendingRequests.ContainsKey(toPid)) _pendingRequests[toPid] = new(); if (!_pendingRequests[toPid].Contains(fromPid)) _pendingRequests[toPid].Add(fromPid); }
    }

    public List<ulong> GetRequests(ulong pid) { lock (_pendingRequests) { return _pendingRequests.GetValueOrDefault(pid, new()).ToList(); } }
    public bool AcceptRequest(ulong pid, ulong fromPid) { lock (_pendingRequests) { if (!_pendingRequests.ContainsKey(pid)) return false; return _pendingRequests[pid].Remove(fromPid); } }
    public bool DeclineRequest(ulong pid, ulong fromPid) { lock (_pendingRequests) { if (!_pendingRequests.ContainsKey(pid)) return false; return _pendingRequests[pid].Remove(fromPid); } }
}
