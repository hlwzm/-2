using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Jx3.Core
{
    /// <summary>任务类型</summary>
    public enum QuestType : byte
    {
        Main = 0,   // 主线
        Sub = 1,    // 支线
        Daily = 2,  // 日常
        Weekly = 3, // 周常
    }

    /// <summary>任务状态</summary>
    public enum QuestStatus : byte
    {
        Locked = 0,      // 未解锁（等级不足/前置未完成）
        NotAccepted = 1,  // 可接取
        InProgress = 2,   // 进行中
        CanSubmit = 3,    // 可提交
        Completed = 4,    // 已完成
    }

    /// <summary>奖励定义</summary>
    [Serializable]
    public class QuestReward
    {
        public ulong Exp;
        public ulong Gold;
        public List<(uint itemId, int count)> Items = new();
    }

    /// <summary>任务目标定义</summary>
    [Serializable]
    public class QuestObjective
    {
        public string Description = "";
        public int Current;
        public int Target;

        public bool IsComplete => Current >= Target;
        public float Progress01 => Target > 0 ? Mathf.Clamp01((float)Current / Target) : 0f;
        public string ProgressText => $"{Mathf.Min(Current, Target)}/{Target}";
    }

    /// <summary>任务数据</summary>
    [Serializable]
    public class QuestInfo
    {
        public uint QuestId;
        public string Name = "";
        public QuestType QuestType;
        public QuestStatus Status = QuestStatus.NotAccepted;
        public uint MinLevel;
        public int SortOrder;
        public string Description = "";
        public string NpcName = "";        // 接取/提交NPC
        public uint TargetMapId;           // 目标地图
        public Vector3 GuidePosition;      // 引导位置
        public List<QuestObjective> Objectives = new();
        public QuestReward Reward = new();

        /// <summary>所有目标是否完成 → 可提交</summary>
        public bool AllObjectivesComplete => Objectives.Count > 0 && Objectives.All(o => o.IsComplete);
    }

    /// <summary>任务管理器 - 完整实现</summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; } = null!;

        // ===== 数据 =====
        private readonly Dictionary<uint, QuestInfo> _questDict = new();
        private readonly List<QuestInfo> _allQuests = new();

        // ===== 每日/周常统计 =====
        private int _dailyCompletedToday;
        private int _weeklyCompletedThisWeek;
        private int _dailyMax = 10;   // 每日上限10个日常
        private int _weeklyMax = 5;   // 每周上限5个周常
        private DateTime _lastDailyReset = DateTime.MinValue;
        private DateTime _lastWeeklyReset = DateTime.MinValue;

        // ===== 事件 =====
        public event Action<QuestInfo>? OnQuestAccepted;
        public event Action<QuestInfo>? OnQuestProgress;
        public event Action<QuestInfo>? OnQuestCompleted;
        public event Action<QuestInfo>? OnQuestSubmitReady; // 所有目标达成
        public event Action? OnDailyReset;

        // ===== 公开访问 =====
        public IReadOnlyList<QuestInfo> AllQuests => _allQuests;
        public int DailyCompletedToday => _dailyCompletedToday;
        public int WeeklyCompletedThisWeek => _weeklyCompletedThisWeek;
        public int DailyMax => _dailyMax;
        public int WeeklyMax => _weeklyMax;

        void Awake()
        {
            Instance = this;
            InitQuestTemplates();
            CheckDailyReset();
        }

        void Update()
        {
            CheckDailyReset();
            CheckWeeklyReset();
        }

        // ===================================================================
        // 初始化任务模板
        // ===================================================================
        private void InitQuestTemplates()
        {
            // 主线任务
            AddTemplate(1001, "初入江湖", QuestType.Main, 1, 0,
                "前往稻香村找王大石说话", "王大石", 1001, new Vector3(10, 0, 20),
                new List<QuestObjective>
                {
                    new() { Description = "与王大石对话", Target = 1 },
                },
                new QuestReward { Exp = 500, Gold = 100 });

            AddTemplate(1002, "初识英雄", QuestType.Main, 1, 1,
                "通过招募获得一名英雄", "招募使", 1001, new Vector3(25, 0, 15),
                new List<QuestObjective>
                {
                    new() { Description = "招募一名英雄", Target = 1 },
                },
                new QuestReward { Exp = 1000, Gold = 300, Items = { (1001, 5) } });

            AddTemplate(1003, "第一次战斗", QuestType.Main, 2, 2,
                "在稻香村击败10个山贼", "李将军", 1001, new Vector3(40, 0, 30),
                new List<QuestObjective>
                {
                    new() { Description = "击败山贼", Target = 10 },
                },
                new QuestReward { Exp = 2000, Gold = 500, Items = { (2001, 1) } });

            AddTemplate(1004, "装备强化", QuestType.Main, 5, 3,
                "强化一次装备", "铁匠", 1001, new Vector3(30, 0, 10),
                new List<QuestObjective>
                {
                    new() { Description = "强化装备", Target = 1 },
                },
                new QuestReward { Exp = 3000, Gold = 800 });

            // 支线任务
            AddTemplate(1101, "遗失的包裹", QuestType.Sub, 3, 10,
                "在稻香村找到3个遗失的包裹", "村民甲", 1001, new Vector3(15, 0, 25),
                new List<QuestObjective>
                {
                    new() { Description = "找到遗失的包裹", Target = 3 },
                },
                new QuestReward { Exp = 1500, Gold = 400 });

            AddTemplate(1102, "采药草", QuestType.Sub, 4, 11,
                "采集10株药草", "药师", 1001, new Vector3(20, 0, 35),
                new List<QuestObjective>
                {
                    new() { Description = "采集药草", Target = 10 },
                },
                new QuestReward { Exp = 1800, Gold = 350, Items = { (3001, 3) } });

            // 日常任务
            AddTemplate(2001, "每日签到", QuestType.Daily, 1, 20,
                "登录游戏", "", 0, Vector3.zero,
                new List<QuestObjective>
                {
                    new() { Description = "登录游戏", Target = 1 },
                },
                new QuestReward { Exp = 500, Gold = 50 });

            AddTemplate(2002, "每日副本", QuestType.Daily, 20, 21,
                "完成一次副本", "", 0, Vector3.zero,
                new List<QuestObjective>
                {
                    new() { Description = "完成副本", Target = 1 },
                },
                new QuestReward { Exp = 2000, Gold = 300 });

            AddTemplate(2003, "每日竞技", QuestType.Daily, 15, 22,
                "参与一次PVP", "", 0, Vector3.zero,
                new List<QuestObjective>
                {
                    new() { Description = "参与PVP", Target = 1 },
                },
                new QuestReward { Exp = 1500, Gold = 200 });

            // 周常任务
            AddTemplate(3001, "英雄收集者", QuestType.Weekly, 1, 30,
                "收集5名不同的英雄", "", 0, Vector3.zero,
                new List<QuestObjective>
                {
                    new() { Description = "收集不同的英雄", Target = 5 },
                },
                new QuestReward { Exp = 10000, Gold = 2000, Items = { (5001, 10) } });

            AddTemplate(3002, "百战勇士", QuestType.Weekly, 1, 31,
                "累计击败100个敌人", "", 0, Vector3.zero,
                new List<QuestObjective>
                {
                    new() { Description = "击败敌人", Target = 100 },
                },
                new QuestReward { Exp = 15000, Gold = 3000 });

            AddTemplate(3003, "财富积累", QuestType.Weekly, 1, 32,
                "累计获得100000金币", "", 0, Vector3.zero,
                new List<QuestObjective>
                {
                    new() { Description = "获得金币", Target = 100000 },
                },
                new QuestReward { Exp = 8000, Gold = 5000 });

            // 按SortOrder排序
            _allQuests.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        }

        private void AddTemplate(uint id, string name, QuestType type, uint minLevel, int sortOrder,
            string desc, string npc, uint mapId, Vector3 guidePos,
            List<QuestObjective> objectives, QuestReward reward)
        {
            var q = new QuestInfo
            {
                QuestId = id,
                Name = name,
                QuestType = type,
                Status = QuestStatus.NotAccepted,
                MinLevel = minLevel,
                SortOrder = sortOrder,
                Description = desc,
                NpcName = npc,
                TargetMapId = mapId,
                GuidePosition = guidePos,
                Objectives = objectives,
                Reward = reward,
            };
            _questDict[id] = q;
            _allQuests.Add(q);
        }

        // ===================================================================
        // 状态机核��
        // ===================================================================

        /// <summary>接取任务</summary>
        public bool AcceptQuest(uint questId)
        {
            if (!_questDict.TryGetValue(questId, out var q)) return false;
            if (q.Status != QuestStatus.NotAccepted && q.Status != QuestStatus.Locked) return false;

            var player = GameManager.Instance.Player;
            if (player.Level < q.MinLevel) return false;

            // 日常/周常上限检查
            if (q.QuestType == QuestType.Daily && _dailyCompletedToday >= _dailyMax)
            {
                Debug.Log("[Quest] 今日日常已满");
                return false;
            }
            if (q.QuestType == QuestType.Weekly && _weeklyCompletedThisWeek >= _weeklyMax)
            {
                Debug.Log("[Quest] 本周周常已满");
                return false;
            }

            q.Status = QuestStatus.InProgress;
            // 重置进度
            foreach (var obj in q.Objectives) obj.Current = 0;

            Debug.Log($"[Quest] 接取任务: {q.Name}");
            OnQuestAccepted?.Invoke(q);
            return true;
        }

        /// <summary>推进进度（外部事件触发）</summary>
        public void UpdateProgress(uint questId, int objectiveIndex = 0, int delta = 1)
        {
            if (!_questDict.TryGetValue(questId, out var q)) return;
            if (q.Status != QuestStatus.InProgress) return;
            if (objectiveIndex < 0 || objectiveIndex >= q.Objectives.Count) return;

            var obj = q.Objectives[objectiveIndex];
            var old = obj.Current;
            obj.Current = Mathf.Min(obj.Current + delta, obj.Target);

            if (obj.Current != old)
            {
                Debug.Log($"[Quest] 进度更新: {q.Name} [{obj.Description}] {obj.ProgressText}");
                OnQuestProgress?.Invoke(q);

                // 检查是否所有目标完成
                if (q.AllObjectivesComplete)
                {
                    q.Status = QuestStatus.CanSubmit;
                    Debug.Log($"[Quest] 可提交: {q.Name}");
                    OnQuestSubmitReady?.Invoke(q);
                }
            }
        }

        /// <summary>按类型推进（击杀Boss/收集物品/到达区域）</summary>
        public void UpdateProgressByType(QuestType type, string objectiveKeyword, int delta = 1)
        {
            foreach (var q in _allQuests)
            {
                if (q.Status != QuestStatus.InProgress || q.QuestType != type) continue;
                for (int i = 0; i < q.Objectives.Count; i++)
                {
                    if (q.Objectives[i].Description.Contains(objectiveKeyword) && !q.Objectives[i].IsComplete)
                    {
                        UpdateProgress(q.QuestId, i, delta);
                    }
                }
            }
        }

        /// <summary>提交任务</summary>
        public bool SubmitQuest(uint questId)
        {
            if (!_questDict.TryGetValue(questId, out var q)) return false;
            if (q.Status != QuestStatus.CanSubmit) return false;

            q.Status = QuestStatus.Completed;

            // 发放奖励
            var reward = q.Reward;
            if (reward.Exp > 0) GameManager.Instance.Player.Level = Mathf.Max(GameManager.Instance.Player.Level, (int)(GameManager.Instance.Player.Level + (int)(reward.Exp / 1000)));
            if (reward.Gold > 0) { /* 金币发放由服务器同步，客户端暂记 */ }
            // 物品发放简化处理

            // 统计
            if (q.QuestType == QuestType.Daily) _dailyCompletedToday++;
            if (q.QuestType == QuestType.Weekly) _weeklyCompletedThisWeek++;

            Debug.Log($"[Quest] 提交完成: {q.Name} 奖励 Exp={reward.Exp} Gold={reward.Gold}");
            OnQuestCompleted?.Invoke(q);
            return true;
        }

        /// <summary>放弃任务</summary>
        public bool AbandonQuest(uint questId)
        {
            if (!_questDict.TryGetValue(questId, out var q)) return false;
            if (q.Status != QuestStatus.InProgress && q.Status != QuestStatus.CanSubmit) return false;
            q.Status = QuestStatus.NotAccepted;
            foreach (var obj in q.Objectives) obj.Current = 0;
            Debug.Log($"[Quest] 放弃任务: {q.Name}");
            return true;
        }

        // ===================================================================
        // 引导功能
        // ===================================================================

        /// <summary>获取引导目标位置（NPC或坐标）</summary>
        public bool TryGetGuide(uint questId, out uint targetMapId, out Vector3 position, out string npcName)
        {
            targetMapId = 0;
            position = Vector3.zero;
            npcName = "";
            if (!_questDict.TryGetValue(questId, out var q)) return false;
            if (q.Status != QuestStatus.InProgress && q.Status != QuestStatus.CanSubmit) return false;

            targetMapId = q.TargetMapId;
            position = q.GuidePosition;
            npcName = q.NpcName;
            return true;
        }

        // ===================================================================
        // 日常/周常刷新
        // ===================================================================

        private void CheckDailyReset()
        {
            var now = DateTime.Now;
            if (_lastDailyReset.Date < now.Date)
            {
                ResetDailyQuests();
                _lastDailyReset = now;
                _dailyCompletedToday = 0;
                OnDailyReset?.Invoke();
                Debug.Log("[Quest] 日常任务已刷新");
            }
        }

        private void CheckWeeklyReset()
        {
            var now = DateTime.Now;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            if (_lastWeeklyReset < weekStart)
            {
                ResetWeeklyQuests();
                _lastWeeklyReset = weekStart;
                _weeklyCompletedThisWeek = 0;
                Debug.Log("[Quest] 周常任务已刷新");
            }
        }

        private void ResetDailyQuests()
        {
            foreach (var q in _allQuests)
            {
                if (q.QuestType == QuestType.Daily && (q.Status == QuestStatus.Completed || q.Status == QuestStatus.InProgress || q.Status == QuestStatus.CanSubmit))
                {
                    q.Status = QuestStatus.NotAccepted;
                    foreach (var obj in q.Objectives) obj.Current = 0;
                }
            }
        }

        private void ResetWeeklyQuests()
        {
            foreach (var q in _allQuests)
            {
                if (q.QuestType == QuestType.Weekly && (q.Status == QuestStatus.Completed || q.Status == QuestStatus.InProgress || q.Status == QuestStatus.CanSubmit))
                {
                    q.Status = QuestStatus.NotAccepted;
                    foreach (var obj in q.Objectives) obj.Current = 0;
                }
            }
        }

        // ===================================================================
        // 网络消息处理
        // ===================================================================

        public void HandleMessage(uint msgId, byte[] body)
        {
            try
            {
                using var r = new BinaryReader(new MemoryStream(body));
                switch (msgId)
                {
                    case (uint)MsgId.SCQuestList:
                        Debug.Log("[Quest] 收到任务列表");
                        break;

                    case (uint)MsgId.SCQuestReward:
                        var qId = r.ReadUInt32();
                        var exp = r.ReadUInt64();
                        var gold = r.ReadUInt64();
                        Debug.Log($"[Quest] 奖励: 任务{qId} Exp={exp} Gold={gold}");
                        break;

                    case (uint)MsgId.SCQuestProgress:
                        var progressQuestId = r.ReadUInt32();
                        var objIdx = r.ReadInt32();
                        var val = r.ReadInt32();
                        if (_questDict.TryGetValue(progressQuestId, out var pq))
                        {
                            if (objIdx >= 0 && objIdx < pq.Objectives.Count)
                                pq.Objectives[objIdx].Current = val;
                            if (pq.AllObjectivesComplete && pq.Status == QuestStatus.InProgress)
                            {
                                pq.Status = QuestStatus.CanSubmit;
                                OnQuestSubmitReady?.Invoke(pq);
                            }
                            OnQuestProgress?.Invoke(pq);
                        }
                        break;

                    case (uint)MsgId.SCQuestComplete:
                        var completeId = r.ReadUInt32();
                        if (_questDict.TryGetValue(completeId, out var cq))
                        {
                            cq.Status = QuestStatus.Completed;
                            OnQuestCompleted?.Invoke(cq);
                        }
                        break;

                    case (uint)MsgId.SCQuestDailyReset:
                        ResetDailyQuests();
                        _dailyCompletedToday = 0;
                        OnDailyReset?.Invoke();
                        Debug.Log("[Quest] 服务端触发日常刷新");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestManager] HandleMessage error: {ex.Message}");
            }
        }

        // ===================================================================
        // 网络请求
        // ===================================================================

        public void RequestQuests()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            GameManager.Instance.Network.Send((uint)MsgId.CSQuestList, ms.ToArray());
        }

        public void RequestAccept(uint questId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(questId);
            GameManager.Instance.Network.Send((uint)MsgId.CSQuestAccept, ms.ToArray());
        }

        public void RequestSubmit(uint questId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(questId);
            GameManager.Instance.Network.Send((uint)MsgId.CSQuestSubmit, ms.ToArray());
        }

        public void RequestAbandon(uint questId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(questId);
            GameManager.Instance.Network.Send((uint)MsgId.CSQuestAbandon, ms.ToArray());
        }

        // ===================================================================
        // 查询接口
        // ===================================================================

        public QuestInfo? GetQuest(uint questId)
        {
            _questDict.TryGetValue(questId, out var q);
            return q;
        }

        public List<QuestInfo> GetQuestsByType(QuestType type)
        {
            return _allQuests.FindAll(q => q.QuestType == type);
        }

        public List<QuestInfo> GetQuestsByStatus(QuestStatus status)
        {
            return _allQuests.FindAll(q => q.Status == status);
        }

        public List<QuestInfo> GetActiveQuests()
        {
            return _allQuests.FindAll(q => q.Status == QuestStatus.InProgress || q.Status == QuestStatus.CanSubmit);
        }

        public int GetQuestCountByStatus(QuestStatus status)
        {
            return _allQuests.Count(q => q.Status == status);
        }
    }
}