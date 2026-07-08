#nullable disable
using System;
using System.Collections.Generic;
using Jx3.Core.Scene;
using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    /// <summary>匹配状态</summary>
    public enum MatchState
    {
        Idle,           // 空闲
        Matching,       // 匹配中
        MatchFound,     // 匹配成功,等待确认
        Countdown,      // 倒计时5秒
        InGame          // 战斗中
    }

    /// <summary>段位等级</summary>
    public enum RankTier
    {
        Bronze = 0,     // 青铜
        Silver = 1,     // 白银
        Gold = 2,       // 黄金
        Platinum = 3,   // 铂金
        Diamond = 4,    // 钻石
        Legendary = 5   //  legendary
    }

    /// <summary>段位信息</summary>
    [Serializable]
    public class RankInfo
    {
        public RankTier Tier = RankTier.Bronze;
        public int Points = 1000;       // 当前积分
        public int Wins;
        public int Losses;
        public int Streak;              // 连胜/连败(正=胜,负=败)
        public int RankPosition = -1;   // 排行榜名次,-1=未上榜

        public int TotalGames => Wins + Losses;
        public float WinRate => TotalGames > 0 ? (float)Wins / TotalGames : 0f;
        public string TierName => Tier switch
        {
            RankTier.Bronze => "青铜",
            RankTier.Silver => "白银",
            RankTier.Gold => "黄金",
            RankTier.Platinum => "铂金",
            RankTier.Diamond => "钻石",
            RankTier.Legendary => " legendary",
            _ => "未知"
        };
    }

    /// <summary>PVP玩家数据(匹配对手信息)</summary>
    [Serializable]
    public class PvpPlayerData
    {
        public ulong PlayerId;
        public string Name = "";
        public string ClassName = "";
        public int Level = 1;
        public RankInfo Rank = new();
        public int TeamSize; // 1=1v1, 3=3v3
    }

    /// <summary>排行榜条目</summary>
    [Serializable]
    public class RankEntry
    {
        public int Position;
        public ulong PlayerId;
        public string Name = "";
        public string ClassName = "";
        public RankTier Tier;
        public int Points;
        public int Wins;
        public int Losses;
        public float WinRate;
    }

    /// <summary>PVP竞技场管理器</summary>
    public class PvpManager : MonoBehaviour
    {
        public static PvpManager Instance { get; private set; } = null!;

        // ===== 状态 =====
        public MatchState State { get; private set; } = MatchState.Idle;
        public int MatchMode { get; private set; } = 1; // 1=1v1, 3=3v3
        public RankInfo MyRank { get; private set; } = new();
        public int QueueSize { get; private set; }      // 队列中人数
        public int CountdownSeconds { get; private set; }
        public PvpPlayerData Opponent { get; private set; }
        public PvpPlayerData Teammate { get; private set; } // 3v3时额外队友
        public List<RankEntry> RankList { get; private set; } = new();

        // ===== Elo参数 =====
        private const int K_FACTOR = 32;           // Elo K值
        private const int STREAK_BONUS = 10;        // 连胜加成
        private const int RANK_THRESHOLD = 200;     // 段位升级所需分差

        // ===== 事件 =====
        public event Action<MatchState, MatchState> OnMatchStateChanged; // 旧状态,新状态
        public event Action<PvpPlayerData> OnMatchFound;                // 匹配到对手
        public event Action<int> OnQueueUpdate;                         // 队列人数更新
        public event Action<RankInfo> OnRankUpdate;                     // 段位更新
        public event Action<int> OnCountdownTick;                       // 倒计时tick
        public event Action<PvpPlayerData, bool> OnMatchResult;         // 对战结果(对手,是否胜利)
        public event Action<List<RankEntry>> OnRankListUpdate;          // 排行榜更新
        public event Action<string> OnPvpNotice;                        // 系统提示

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            // 初始段位
            MyRank = new RankInfo { Tier = RankTier.Bronze, Points = 1000 };
        }

        // =====================================================================
        // 网络消息处理
        // =====================================================================
        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));

            switch ((MsgId)msgId)
            {
                case MsgId.SCPVPMatchQueue:
                    // 匹配队列更新: int queueSize
                    QueueSize = r.ReadInt32();
                    OnQueueUpdate?.Invoke(QueueSize);
                    Debug.Log($"[PVP] 队列人数更新: {QueueSize}");
                    break;

                case MsgId.SCPVPMatchResult:
                    // 匹配成功: ulong opponentId, string name, string className, int level, int rankTier, int rankPoints
                    Opponent = new PvpPlayerData
                    {
                        PlayerId = r.ReadUInt64(),
                        Name = r.ReadString(),
                        ClassName = r.ReadString(),
                        Level = r.ReadInt32(),
                        Rank = new RankInfo
                        {
                            Tier = (RankTier)r.ReadInt32(),
                            Points = r.ReadInt32()
                        },
                        TeamSize = MatchMode
                    };
                    State = MatchState.MatchFound;
                    OnMatchFound?.Invoke(Opponent);
                    Debug.Log($"[PVP] 匹配到对手: {Opponent.Name}");
                    break;

                case MsgId.SCPVPRankInfo:
                    // 段位信息: int tier, int points, int wins, int losses, int streak
                    MyRank.Tier = (RankTier)r.ReadInt32();
                    MyRank.Points = r.ReadInt32();
                    MyRank.Wins = r.ReadInt32();
                    MyRank.Losses = r.ReadInt32();
                    MyRank.Streak = r.ReadInt32();
                    OnRankUpdate?.Invoke(MyRank);
                    Debug.Log($"[PVP] 段位更新: {MyRank.TierName} {MyRank.Points}分");
                    break;

                case MsgId.SCPVPDuelResult:
                    // 对战结果: ulong opponentId, bool victory, int pointChange
                    var opId = r.ReadUInt64();
                    bool victory = r.ReadBoolean();
                    int ptChange = r.ReadInt32();
                    if (Opponent != null)
                    {
                        OnMatchResult?.Invoke(Opponent, victory);
                        // 更新本地积分
                        MyRank.Points += ptChange;
                        if (victory)
                        {
                            MyRank.Wins++;
                            MyRank.Streak = MyRank.Streak > 0 ? MyRank.Streak + 1 : 1;
                        }
                        else
                        {
                            MyRank.Losses++;
                            MyRank.Streak = MyRank.Streak < 0 ? MyRank.Streak - 1 : -1;
                        }
                        // 检查段位升降
                        RecalculateTier();
                        OnRankUpdate?.Invoke(MyRank);
                    }
                    State = MatchState.Idle;
                    Opponent = null;
                    Debug.Log($"[PVP] 对战{(victory?"胜利":"失败")}, 积分变化:{ptChange:+0;-0}");
                    break;

                case MsgId.SCPVPDuelRequest:
                    // 收到决斗请求
                    var challenger = new PvpPlayerData
                    {
                        PlayerId = r.ReadUInt64(),
                        Name = r.ReadString(),
                        ClassName = r.ReadString(),
                        Level = r.ReadInt32(),
                        Rank = new RankInfo
                        {
                            Tier = (RankTier)r.ReadInt32(),
                            Points = r.ReadInt32()
                        },
                        TeamSize = 1
                    };
                    Opponent = challenger;
                    State = MatchState.MatchFound;
                    OnMatchFound?.Invoke(challenger);
                    Debug.Log($"[PVP] 收到决斗请求: {challenger.Name}");
                    break;
            }
        }

        // =====================================================================
        // 公开方法
        // =====================================================================

        /// <summary>设置匹配模式</summary>
        public void SetMatchMode(int mode)
        {
            if (State != MatchState.Idle) return;
            MatchMode = mode == 3 ? 3 : 1;
            Debug.Log($"[PVP] 匹配模式: {(MatchMode==1?"1v1":"3v3")}");
        }

        /// <summary>开始匹配</summary>
        public void StartMatch()
        {
            if (State != MatchState.Idle) return;
            var prev = State;
            State = MatchState.Matching;
            QueueSize = 0;
            OnMatchStateChanged?.Invoke(prev, State);

            var msg = new byte[4];
            using (var w = new BinaryWriter(new MemoryStream(msg)))
            {
                w.Write(MatchMode);
            }
            GameManager.Instance.Network.Send((uint)MsgId.CSPVPMatchStart, msg);
            Debug.Log($"[PVP] 开始匹配 ({MatchMode}v{MatchMode})");
        }

        /// <summary>取消匹配</summary>
        public void CancelMatch()
        {
            if (State != MatchState.Matching) return;
            var prev = State;
            State = MatchState.Idle;
            QueueSize = 0;
            Opponent = null;
            OnMatchStateChanged?.Invoke(prev, State);

            GameManager.Instance.Network.Send((uint)MsgId.CSPVPMatchCancel, new byte[0]);
            Debug.Log("[PVP] 取消匹配");
        }

        /// <summary>接受匹配/确认</summary>
        public void AcceptMatch()
        {
            if (State != MatchState.MatchFound) return;
            var prev = State;
            State = MatchState.Countdown;
            CountdownSeconds = 5;
            OnMatchStateChanged?.Invoke(prev, State);

            GameManager.Instance.Network.Send((uint)MsgId.CSPVPDuelAccept, new byte[0]);
            Debug.Log("[PVP] 接受匹配,倒计时开始");
        }

        /// <summary>拒绝匹配</summary>
        public void DeclineMatch()
        {
            if (State != MatchState.MatchFound && State != MatchState.Countdown) return;
            var prev = State;
            State = MatchState.Idle;
            Opponent = null;
            CountdownSeconds = 0;
            OnMatchStateChanged?.Invoke(prev, State);

            // 发送拒绝
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(Opponent?.PlayerId ?? 0);
            GameManager.Instance.Network.Send((uint)MsgId.CSPVPDuelAccept, ms.ToArray()); // 用拒绝标识
            Debug.Log("[PVP] 拒绝匹配");
        }

        /// <summary>请求排行榜</summary>
        public void RequestRankInfo()
        {
            GameManager.Instance.Network.Send((uint)MsgId.CSPVPRankInfo, new byte[0]);
        }

        /// <summary>获取段位升级所需积分</summary>
        public int PointsToNextTier()
        {
            int tierBase = (int)MyRank.Tier * RANK_THRESHOLD;
            return (tierBase + RANK_THRESHOLD) - MyRank.Points;
        }

        /// <summary>Elo计算(返回双方积分变化)</summary>
        public static (int deltaA, int deltaB) CalculateElo(int ratingA, int ratingB, bool aWins)
        {
            double expectedA = 1.0 / (1.0 + Math.Pow(10, (ratingB - ratingA) / 400.0));
            double scoreA = aWins ? 1.0 : 0.0;
            int delta = (int)Math.Round(K_FACTOR * (scoreA - expectedA));
            return (delta, -delta);
        }

        // =====================================================================
        // 内部方法
        // =====================================================================
        private void RecalculateTier()
        {
            int tierIndex = (int)MyRank.Tier;
            // 低于当前段位下限则降段
            while (tierIndex > 0 && MyRank.Points < tierIndex * RANK_THRESHOLD)
            {
                tierIndex--;
            }
            // 高于下一段位上限且非满段则升段
            while (tierIndex < (int)RankTier.Legendary && MyRank.Points >= (tierIndex + 1) * RANK_THRESHOLD)
            {
                tierIndex++;
            }
            MyRank.Tier = (RankTier)tierIndex;
        }

        // Update用于倒计时
        private float _countdownTimer;

        void Update()
        {
            if (State == MatchState.Countdown)
            {
                _countdownTimer -= Time.deltaTime;
                int prevSec = CountdownSeconds;
                CountdownSeconds = Mathf.CeilToInt(_countdownTimer);
                if (CountdownSeconds != prevSec)
                {
                    OnCountdownTick?.Invoke(CountdownSeconds);
                }
                if (_countdownTimer <= 0)
                {
                    // 倒计时结束,进入战斗
                    var prev = State;
                    State = MatchState.InGame;
                    OnMatchStateChanged?.Invoke(prev, State);
                    Scene.SceneManager.Instance.LoadScene(GameScene.PVP);
                    Debug.Log("[PVP] 倒计时结束,进入PVP场景");
                }
            }
        }

        /// <summary>设置倒计时(外部调用,如服务端同步)</summary>
        public void SetCountdown(float seconds)
        {
            if (State != MatchState.Countdown) return;
            _countdownTimer = seconds;
            CountdownSeconds = Mathf.CeilToInt(seconds);
            OnCountdownTick?.Invoke(CountdownSeconds);
        }

        /// <summary>通知战斗结束,回到空闲</summary>
        public void OnBattleEnded(bool victory, int pointChange, PvpPlayerData opponent)
        {
            MyRank.Points += pointChange;
            if (victory) { MyRank.Wins++; MyRank.Streak = Math.Max(1, MyRank.Streak + 1); }
            else { MyRank.Losses++; MyRank.Streak = Math.Min(-1, MyRank.Streak - 1); }
            RecalculateTier();
            OnRankUpdate?.Invoke(MyRank);
            OnMatchResult?.Invoke(opponent, victory);
            State = MatchState.Idle;
            Opponent = null;
        }
    }
}