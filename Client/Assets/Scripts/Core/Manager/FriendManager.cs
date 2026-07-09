#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Jx3.Core
{
    /// <summary>好友在线状态</summary>
    public enum FriendOnlineStatus
    {
        Offline = 0,
        Online = 1,
        InBattle = 2,
    }

    /// <summary>好友信息</summary>
    [Serializable]
    public class FriendInfo
    {
        public ulong PlayerId;
        public string Name = "";
        public int Level;
        public int VipLevel;
        public int SchoolId;
        public string SchoolName = "";
        public int MapId;
        public string MapName = "";
        public FriendOnlineStatus OnlineStatus = FriendOnlineStatus.Offline;
        public DateTime LastOnlineTime;
        public DateTime LastChatTime;
        public bool Blacklisted;
        public bool IsFriend;
        public bool HasPendingRequest;
        public string RecommendReason = "";

        public bool Online => OnlineStatus == FriendOnlineStatus.Online || OnlineStatus == FriendOnlineStatus.InBattle;
    }

    [Serializable]
    public class FriendRequest
    {
        public ulong RequestId;
        public ulong FromPlayerId;
        public string FromName = "";
        public int FromLevel;
        public int FromSchoolId;
        public string FromSchoolName = "";
        public string Message = "";
        public DateTime RequestTime;
    }

    public class FriendManager : MonoBehaviour
    {
        public static FriendManager Instance { get; private set; } = null!;

        public const int BASE_FRIEND_LIMIT = 50;
        public const int VIP_FRIEND_EXTRA_1 = 15;
        public const int VIP_FRIEND_EXTRA_2 = 30;
        public const int VIP_FRIEND_EXTRA_3 = 50;
        public const int RECENT_CONTACT_MAX = 20;
        public const int RECOMMEND_MAX = 10;
        public const int RECOMMEND_LEVEL_RANGE = 3;
        public const int RECOMMEND_REFRESH_INTERVAL = 300;

        private readonly List<FriendInfo> _friends = new();
        private readonly List<FriendRequest> _pendingRequests = new();
        private readonly List<FriendInfo> _recentContacts = new();
        private readonly List<FriendInfo> _recommendList = new();
        private readonly List<FriendInfo> _blacklist = new();
        private float _lastRecommendRefresh;

        public IReadOnlyList<FriendInfo> Friends => _friends.AsReadOnly();
        public IReadOnlyList<FriendRequest> PendingRequests => _pendingRequests.AsReadOnly();
        public IReadOnlyList<FriendInfo> RecentContacts => _recentContacts.AsReadOnly();
        public IReadOnlyList<FriendInfo> RecommendList => _recommendList.AsReadOnly();
        public IReadOnlyList<FriendInfo> Blacklist => _blacklist.AsReadOnly();

        public int FriendCount => _friends.Count;
        public int OnlineCount => _friends.Count(f => f.Online);
        public int BlacklistCount => _blacklist.Count;
        public int PendingRequestCount => _pendingRequests.Count;
        public int MaxFriendLimit => BASE_FRIEND_LIMIT + (GameManager.Instance?.Player.VipLevel >= 3 ? VIP_FRIEND_EXTRA_3 :
                                          GameManager.Instance?.Player.VipLevel >= 2 ? VIP_FRIEND_EXTRA_2 :
                                          GameManager.Instance?.Player.VipLevel >= 1 ? VIP_FRIEND_EXTRA_1 : 0);

        public event Action OnFriendListChanged;
        public event Action<FriendInfo> OnFriendOnline;
        public event Action<FriendInfo> OnFriendOffline;
        public event Action<FriendRequest> OnFriendRequest;
        public event Action<FriendInfo> OnFriendAdded;
        public event Action<FriendInfo> OnFriendRemoved;
        public event Action<FriendInfo> OnBlacklistChanged;
        public event Action<FriendInfo> OnRecentContactUpdated;
        public event Action<List<FriendInfo>> OnRecommendListUpdated;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            _lastRecommendRefresh += Time.deltaTime;
            if (_lastRecommendRefresh >= RECOMMEND_REFRESH_INTERVAL)
            {
                _lastRecommendRefresh = 0f;
                RefreshRecommendList();
            }
        }

        // ============ 好友列表 ============

        public List<FriendInfo> GetOnlineFriends()
        {
            return _friends.Where(f => f.Online && !f.Blacklisted).OrderByDescending(f => f.Level).ToList();
        }

        public List<FriendInfo> GetOfflineFriends()
        {
            return _friends.Where(f => !f.Online && !f.Blacklisted).OrderByDescending(f => f.LastOnlineTime).ToList();
        }

        public List<FriendInfo> GetNormalFriends()
        {
            return _friends.Where(f => !f.Blacklisted).OrderByDescending(f => f.Online).ThenByDescending(f => f.Level).ToList();
        }

        public List<FriendInfo> GetBlacklistedPlayers()
        {
            return _blacklist.ToList();
        }

        // ============ 添加好友 ============

        public void AddFriend(ulong targetId)
        {
            if (_friends.Any(f => f.PlayerId == targetId))
            {
                Debug.Log("[Friend] 已是好友: " + targetId);
                return;
            }
            if (_friends.Count >= MaxFriendLimit)
            {
                Debug.Log("[Friend] 好友已达上限(" + MaxFriendLimit + ")");
                return;
            }

            var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms))
            {
                w.Write(GameManager.Instance.Player.PlayerId);
                w.Write(targetId);
            }
            GameManager.Instance.Network.Send((uint)MsgId.CSFriendAdd, ms.ToArray());
            Debug.Log("[Friend] 已发送好友请求: " + targetId);
        }

        public void SearchPlayer(string keyword, Action<List<FriendInfo>> callback)
        {
            var results = new List<FriendInfo>();
            var all = new List<FriendInfo>();
            all.AddRange(_friends);
            all.AddRange(_recommendList);

            if (ulong.TryParse(keyword, out var id))
            {
                var byId = all.FirstOrDefault(f => f.PlayerId == id);
                if (byId != null) results.Add(byId);
            }

            var byName = all.Where(f => f.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            results.AddRange(byName.Except(results));

            if (results.Count == 0 && !string.IsNullOrEmpty(keyword))
            {
                var ms = new MemoryStream();
                using (var w = new BinaryWriter(ms)) { w.Write(keyword); }
                GameManager.Instance.Network.Send((uint)MsgId.CSFriendList, ms.ToArray());
            }

            callback?.Invoke(results);
        }

        // ============ 接受/拒绝好友请求 ============

        public void AcceptRequest(ulong requestId)
        {
            var req = _pendingRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (req == null) return;

            var newFriend = new FriendInfo
            {
                PlayerId = req.FromPlayerId,
                Name = req.FromName,
                Level = req.FromLevel,
                SchoolId = req.FromSchoolId,
                SchoolName = req.FromSchoolName,
                IsFriend = true,
            };
            _friends.Add(newFriend);
            _pendingRequests.Remove(req);

            var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms))
            {
                w.Write(GameManager.Instance.Player.PlayerId);
                w.Write(req.FromPlayerId);
                w.Write(requestId);
            }
            GameManager.Instance.Network.Send((uint)MsgId.CSFriendAccept, ms.ToArray());

            OnFriendAdded?.Invoke(newFriend);
            OnFriendListChanged?.Invoke();
            Debug.Log("[Friend] 已接受好友请求: " + req.FromName);
        }

        public void DeclineRequest(ulong requestId)
        {
            var req = _pendingRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (req == null) return;

            _pendingRequests.Remove(req);

            var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms))
            {
                w.Write(GameManager.Instance.Player.PlayerId);
                w.Write(req.FromPlayerId);
                w.Write(requestId);
            }
            GameManager.Instance.Network.Send((uint)MsgId.CSFriendDecline, ms.ToArray());

            OnFriendListChanged?.Invoke();
            Debug.Log("[Friend] 已拒绝好友请求: " + req.FromName);
        }

        // ============ 删除好友 ============

        public void RemoveFriend(ulong playerId)
        {
            var friend = _friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend == null) return;

            _friends.Remove(friend);
            var contact = _recentContacts.FirstOrDefault(c => c.PlayerId == playerId);
            if (contact != null) contact.IsFriend = false;

            var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms))
            {
                w.Write(GameManager.Instance.Player.PlayerId);
                w.Write(playerId);
            }
            GameManager.Instance.Network.Send((uint)MsgId.CSFriendRemove, ms.ToArray());

            OnFriendRemoved?.Invoke(friend);
            OnFriendListChanged?.Invoke();
            Debug.Log("[Friend] 已删除好友: " + friend.Name);
        }

        // ============ 黑名单 ============

        public void BlockPlayer(ulong playerId)
        {
            var friend = _friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend == null)
            {
                friend = new FriendInfo { PlayerId = playerId, Name = "玩家" + playerId, Blacklisted = true };
                _friends.Add(friend);
            }
            else
            {
                friend.Blacklisted = true;
            }

            if (!_blacklist.Any(b => b.PlayerId == playerId))
            {
                _blacklist.Add(friend);
            }

            OnBlacklistChanged?.Invoke(friend);
            OnFriendListChanged?.Invoke();
            Debug.Log("[Friend] 已拉黑: " + friend.Name);
        }

        public void UnblockPlayer(ulong playerId)
        {
            var friend = _friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend != null) friend.Blacklisted = false;

            _blacklist.RemoveAll(b => b.PlayerId == playerId);
            OnBlacklistChanged?.Invoke(friend);
            OnFriendListChanged?.Invoke();
            Debug.Log("[Friend] 已取消拉黑: " + (friend?.Name ?? playerId.ToString()));
        }

        public bool IsBlacklisted(ulong playerId)
        {
            return _blacklist.Any(b => b.PlayerId == playerId);
        }

        // ============ 推荐好友 ============

        public void RefreshRecommendList()
        {
            _recommendList.Clear();
            var player = GameManager.Instance.Player;
            if (player == null) return;

            var currentLevel = player.Level;
            var currentMapId = player.MapId;
            var playerSchoolIds = new HashSet<int>();
            foreach (var hero in HeroManager.OwnedHeroes)
            {
                var template = HeroConfig.Get((int)hero.TemplateId);
                if (template != null) playerSchoolIds.Add(template.id);
            }

            var existingIds = new HashSet<ulong>(_friends.Select(f => f.PlayerId));
            existingIds.Add(player.PlayerId);

            var seed = DateTime.Now.Second;
            var rng = new System.Random(seed);

            var sampleNames = new[] {
                "风清扬", "张无忌", "令狐冲", "小龙女", "杨过",
                "郭靖", "黄蓉", "段誉", "虚竹", "乔峰",
                "慕容复", "周芷若", "赵敏", "花无缺", "小鱼儿"
            };
            var sampleSchools = new Dictionary<int, string> {
                {1001, "纯阳"}, {1002, "剑魔"}, {1003, "藏剑"}, {1004, "五毒"},
                {1006, "少林"}, {1007, "恶人谷"}, {2001, "七秀"}, {2002, "天策"},
            };

            var schoolKeys = sampleSchools.Keys.ToList();

            for (int i = 0; i < RECOMMEND_MAX; i++)
            {
                var nameIdx = rng.Next(sampleNames.Length);
                var schoolKey = schoolKeys[rng.Next(schoolKeys.Count)];
                var level = currentLevel + rng.Next(-RECOMMEND_LEVEL_RANGE, RECOMMEND_LEVEL_RANGE + 1);
                level = Mathf.Clamp(level, 1, 60);

                var reason = "";
                if (Math.Abs(level - currentLevel) <= RECOMMEND_LEVEL_RANGE)
                    reason = "同等级";
                if (playerSchoolIds.Contains(schoolKey))
                    reason = reason.Length > 0 ? "同门派,同等级" : "同门派";

                var rec = new FriendInfo
                {
                    PlayerId = (ulong)(100000 + i + seed),
                    Name = sampleNames[nameIdx],
                    Level = level,
                    SchoolId = schoolKey,
                    SchoolName = sampleSchools[schoolKey],
                    RecommendReason = reason,
                    OnlineStatus = rng.Next(3) == 0 ? FriendOnlineStatus.Offline : FriendOnlineStatus.Online,
                    IsFriend = false,
                };

                if (!existingIds.Contains(rec.PlayerId))
                    _recommendList.Add(rec);
            }

            OnRecommendListUpdated?.Invoke(_recommendList);
            Debug.Log("[Friend] 推荐列表已刷新: " + _recommendList.Count + "人");
        }

        // ============ 最近联系人 ============

        public void RecordPrivateChat(ulong playerId, string playerName)
        {
            var existing = _recentContacts.FirstOrDefault(c => c.PlayerId == playerId);
            if (existing != null)
            {
                existing.LastChatTime = DateTime.Now;
                existing.Name = playerName;
                _recentContacts.Remove(existing);
                _recentContacts.Insert(0, existing);
            }
            else
            {
                var isFriend = _friends.Any(f => f.PlayerId == playerId && !f.Blacklisted);
                var contact = new FriendInfo
                {
                    PlayerId = playerId,
                    Name = playerName,
                    LastChatTime = DateTime.Now,
                    IsFriend = isFriend,
                };
                _recentContacts.Insert(0, contact);

                if (_recentContacts.Count > RECENT_CONTACT_MAX)
                    _recentContacts.RemoveAt(_recentContacts.Count - 1);
            }

            OnRecentContactUpdated?.Invoke(existing ?? _recentContacts[0]);
        }

        // ============ 在线状态更新 ============

        public void UpdateFriendStatus(ulong playerId, FriendOnlineStatus status)
        {
            var friend = _friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend == null) return;

            var oldStatus = friend.OnlineStatus;
            friend.OnlineStatus = status;
            if (!friend.Online) friend.LastOnlineTime = DateTime.Now;

            if (oldStatus != status)
            {
                if (status == FriendOnlineStatus.Offline)
                {
                    OnFriendOffline?.Invoke(friend);
                    Debug.Log("[Friend] 好友离线: " + friend.Name);
                }
                else
                {
                    OnFriendOnline?.Invoke(friend);
                    Debug.Log("[Friend] 好友上线: " + friend.Name);
                }
                OnFriendListChanged?.Invoke();
            }
        }

        public void BatchUpdateStatus(List<(ulong playerId, FriendOnlineStatus status)> updates)
        {
            foreach (var (pid, st) in updates)
            {
                UpdateFriendStatus(pid, st);
            }
        }

        // ============ 请求服务器 ============

        public void RequestList()
        {
            GameManager.Instance.Network.Send((uint)MsgId.CSFriendList, new byte[0]);
        }

        // ============ 网络消息处理 ============

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var ms = new MemoryStream(body);
            using var r = new BinaryReader(ms);

            switch ((MsgId)msgId)
            {
                case MsgId.SCFriendList:
                    HandleFriendList(r);
                    break;
                case MsgId.SCFriendRequest:
                    HandleFriendRequest(r);
                    break;
                case MsgId.SCFriendOnline:
                    HandleFriendOnline(r);
                    break;
                case MsgId.SCFriendOffline:
                    HandleFriendOffline(r);
                    break;
                default:
                    Debug.Log("[Friend] 未处理消息: " + msgId);
                    break;
            }
        }

        private void HandleFriendList(BinaryReader r)
        {
            try
            {
                var count = r.ReadInt32();
                _friends.Clear();
                for (int i = 0; i < count; i++)
                {
                    var fi = new FriendInfo
                    {
                        PlayerId = r.ReadUInt64(),
                        Name = r.ReadString(),
                        Level = r.ReadInt32(),
                        SchoolId = r.ReadInt32(),
                        SchoolName = r.ReadString(),
                        VipLevel = r.ReadInt32(),
                        MapId = r.ReadInt32(),
                        MapName = r.ReadString(),
                        OnlineStatus = (FriendOnlineStatus)r.ReadInt32(),
                        LastOnlineTime = DateTime.FromBinary(r.ReadInt64()),
                        IsFriend = true,
                    };
                    _friends.Add(fi);
                }
                Debug.Log("[Friend] 收到好友列表: " + count + "人");
                OnFriendListChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Friend] 解析好友列表失败: " + ex.Message);
            }
        }

        private void HandleFriendRequest(BinaryReader r)
        {
            try
            {
                var req = new FriendRequest
                {
                    RequestId = r.ReadUInt64(),
                    FromPlayerId = r.ReadUInt64(),
                    FromName = r.ReadString(),
                    FromLevel = r.ReadInt32(),
                    FromSchoolId = r.ReadInt32(),
                    FromSchoolName = r.ReadString(),
                    Message = r.ReadString(),
                    RequestTime = DateTime.Now,
                };
                _pendingRequests.Add(req);
                Debug.Log("[Friend] 收到好友请求: " + req.FromName);
                OnFriendRequest?.Invoke(req);
                OnFriendListChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Friend] 解析好友请求失败: " + ex.Message);
            }
        }

        private void HandleFriendOnline(BinaryReader r)
        {
            try
            {
                var playerId = r.ReadUInt64();
                var status = (FriendOnlineStatus)r.ReadInt32();
                UpdateFriendStatus(playerId, status);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Friend] 解析上线通知失败: " + ex.Message);
            }
        }

        private void HandleFriendOffline(BinaryReader r)
        {
            try
            {
                var playerId = r.ReadUInt64();
                UpdateFriendStatus(playerId, FriendOnlineStatus.Offline);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Friend] 解析下线通知失败: " + ex.Message);
            }
        }

        public FriendInfo GetFriend(ulong playerId)
        {
            return _friends.FirstOrDefault(f => f.PlayerId == playerId);
        }

        public void AddMockData()
        {
            if (_friends.Count > 0) return;

            var mockNames = new[] {
                new { Name = "剑心", SchoolId = 1001, School = "纯阳" },
                new { Name = "月影", SchoolId = 1002, School = "剑魔" },
                new { Name = "风雪", SchoolId = 1003, School = "藏剑" },
                new { Name = "毒医", SchoolId = 1004, School = "五毒" },
                new { Name = "禅心", SchoolId = 1006, School = "少林" },
                new { Name = "邪皇", SchoolId = 1007, School = "恶人谷" },
                new { Name = "舞姬", SchoolId = 2001, School = "七秀" },
                new { Name = "铁骑", SchoolId = 2002, School = "天策" },
            };

            var rng = new System.Random(42);
            for (int i = 0; i < mockNames.Length; i++)
            {
                _friends.Add(new FriendInfo
                {
                    PlayerId = (ulong)(50000 + i),
                    Name = mockNames[i].Name,
                    Level = rng.Next(10, 55),
                    VipLevel = rng.Next(0, 4),
                    SchoolId = mockNames[i].SchoolId,
                    SchoolName = mockNames[i].School,
                    MapId = 1001 + rng.Next(0, 5),
                    MapName = "地图" + rng.Next(1, 6),
                    OnlineStatus = i < 4 ? FriendOnlineStatus.Online : FriendOnlineStatus.Offline,
                    LastOnlineTime = DateTime.Now.AddMinutes(-rng.Next(10, 1000)),
                    LastChatTime = DateTime.Now.AddMinutes(-rng.Next(1, 120)),
                    IsFriend = true,
                });
            }

            _pendingRequests.Add(new FriendRequest
            {
                RequestId = 9001,
                FromPlayerId = 60001,
                FromName = "醉剑仙",
                FromLevel = 48,
                FromSchoolId = 1002,
                FromSchoolName = "剑魔",
                Message = "高手求组队!",
                RequestTime = DateTime.Now.AddMinutes(-5),
            });

            Debug.Log("[Friend] 已添加" + _friends.Count + "个模拟好友," + _pendingRequests.Count + "个请求");
            OnFriendListChanged?.Invoke();
        }
    }
}
