using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jx3.Core
{
    // ===================================================================
    // 帮会职位
    // ===================================================================
    public enum GuildPosition : byte
    {
        Member = 0,      // 普通成员
        Elite = 1,       // 精英
        Officer = 2,     // 官员
        ViceLeader = 3,  // 副帮主
        Leader = 4,      // 帮主
    }

    // ===================================================================
    // 帮会技能
    // ===================================================================
    [Serializable]
    public class GuildSkill
    {
        public uint SkillId;
        public string Name = "";
        public string Description = "";
        public int Level;
        public int MaxLevel = 10;
        public ulong UpgradeCost;            // 升级所需帮会资金
        public int ContributionCost;         // 升级所需个人贡献

        // 属性加成（每级）
        public float AtkBonusPerLevel;
        public float DefBonusPerLevel;
        public float HpBonusPerLevel;

        public float CurrentAtkBonus => AtkBonusPerLevel * Level;
        public float CurrentDefBonus => DefBonusPerLevel * Level;
        public float CurrentHpBonus => HpBonusPerLevel * Level;
        public bool IsMaxLevel => Level >= MaxLevel;
    }

    // ===================================================================
    // 帮会日志条目
    // ===================================================================
    [Serializable]
    public class GuildLogEntry
    {
        public DateTime Time;
        public string Message = "";
    }

    // ===================================================================
    // 帮会成员
    // ===================================================================
    [Serializable]
    public class GuildMemberInfo
    {
        public ulong PlayerId;
        public string Name = "";
        public int Level;
        public GuildPosition Position;
        public int Contribution;         // 总贡献
        public int WeeklyContribution;   // 本周贡献
        public int DailyContribution;    // 今日贡献
        public bool Online;
        public DateTime LastLogin;
    }

    // ===================================================================
    // 帮会申请
    // ===================================================================
    [Serializable]
    public class GuildApplication
    {
        public ulong PlayerId;
        public string PlayerName = "";
        public int PlayerLevel;
        public DateTime ApplyTime;
    }

    // ===================================================================
    // 帮会数据
    // ===================================================================
    [Serializable]
    public class GuildData
    {
        public ulong GuildId;
        public string Name = "";
        public int Level = 1;
        public int MaxLevel = 10;
        public ulong LeaderId;
        public string LeaderName = "";
        public ulong Funds;                // 帮会资金
        public int TotalContribution;      // 帮会总贡献（用于升级）
        public ulong ContributionForNextLevel;
        public int MaxMembers = 40;
        public string Notice = "欢迎加入帮会！";
        public int IconIndex;
        public DateTime CreateTime;

        // 帮会技能
        public List<GuildSkill> Skills = new();
        // 成员
        public Dictionary<ulong, GuildMemberInfo> Members = new();
        // 申请列表
        public List<GuildApplication> Applications = new();
        // 日志
        public List<GuildLogEntry> Logs = new();
        // 帮会任务进度（每日）
        public Dictionary<uint, int> DailyQuestProgress = new();

        public int MemberCount => Members.Count;
        public bool IsFull => MemberCount >= MaxMembers;

        public ulong RequiredContributionForLevel(int level)
        {
            return (ulong)(level * level * 5000 + level * 1000);
        }

        public ulong NextLevelContribution => RequiredContributionForLevel(Level);

        public bool CanLevelUp => Level < MaxLevel && (ulong)TotalContribution >= NextLevelContribution;
    }

    // ===================================================================
    // 帮会任务模板
    // ===================================================================
    [Serializable]
    public class GuildQuestTemplate
    {
        public uint QuestId;
        public string Name = "";
        public string Description = "";
        public int Target;
        public int ContributionReward;
        public ulong FundsReward;
        public QuestType Type; // 复用主线/日常等
    }

    // ===================================================================
    // 帮会管理器
    // ===================================================================
    public class GuildManager : MonoBehaviour
    {
        public static GuildManager Instance { get; private set; } = null!;

        // ===== 数据 =====
        private GuildData? _myGuild;
        private readonly List<GuildData> _guildList = new(); // 所有帮会（搜索列表）
        private ulong _nextGuildId = 1;

        // ===== 帮会任务模板 =====
        private readonly List<GuildQuestTemplate> _guildQuestTemplates = new();

        // ===== 每日刷新 =====
        private DateTime _lastDailyReset = DateTime.MinValue;

        // ===== 创建消耗 =====
        public const ulong CreateGuildCost = 50000;

        // ===== 事件 =====
        public event Action<GuildData>? OnGuildDataChanged;
        public event Action<GuildMemberInfo>? OnMemberJoined;
        public event Action<ulong>? OnMemberLeft;
        public event Action<GuildData, GuildSkill>? OnGuildSkillUpgraded;
        public event Action<GuildData>? OnGuildLevelUp;
        public event Action<GuildLogEntry>? OnNewLogEntry;

        // ===== 公开访问 =====
        public GuildData? MyGuild => _myGuild;
        public IReadOnlyList<GuildData> GuildList => _guildList;
        public bool HasGuild => _myGuild != null;

        void Awake()
        {
            Instance = this;
            InitGuildQuestTemplates();
            InitDefaultGuilds();
            CheckDailyReset();
        }

        void Update()
        {
            CheckDailyReset();
        }

        // ===================================================================
        // 初始化
        // ===================================================================

        private void InitGuildQuestTemplates()
        {
            _guildQuestTemplates.Add(new GuildQuestTemplate
            {
                QuestId = 1, Name = "帮会捐献", Description = "捐献金币或材料",
                Target = 1, ContributionReward = 50, FundsReward = 500
            });
            _guildQuestTemplates.Add(new GuildQuestTemplate
            {
                QuestId = 2, Name = "团队副本", Description = "参加一次团队副本",
                Target = 1, ContributionReward = 100, FundsReward = 1000
            });
            _guildQuestTemplates.Add(new GuildQuestTemplate
            {
                QuestId = 3, Name = "帮会战", Description = "参与帮会战",
                Target = 1, ContributionReward = 200, FundsReward = 2000
            });
            _guildQuestTemplates.Add(new GuildQuestTemplate
            {
                QuestId = 4, Name = "帮助新人", Description = "帮助帮会新人完成任务",
                Target = 3, ContributionReward = 30, FundsReward = 200
            });
        }

        private void InitDefaultGuilds()
        {
            // 预置一些帮会用于搜索列表
            CreateGuildInternal("凌霄阁", out _);
            CreateGuildInternal("暗影盟", out _);
            CreateGuildInternal("沧海明月", out _);
            CreateGuildInternal("铁血战旗", out _);
        }

        private bool CreateGuildInternal(string name, out GuildData guild)
        {
            guild = new GuildData
            {
                GuildId = _nextGuildId++,
                Name = name,
                Level = 1,
                LeaderId = 0,
                LeaderName = "系统",
                Notice = "欢迎加入",
                CreateTime = DateTime.Now,
                MaxMembers = 40,
                Skills = CreateDefaultSkills(),
            };
            guild.ContributionForNextLevel = guild.NextLevelContribution;
            _guildList.Add(guild);
            return true;
        }

        private List<GuildSkill> CreateDefaultSkills()
        {
            return new List<GuildSkill>
            {
                new() { SkillId = 1, Name = "帮会攻击", Description = "提升全体成员攻击力", MaxLevel = 10, UpgradeCost = 10000, ContributionCost = 500, AtkBonusPerLevel = 0.5f },
                new() { SkillId = 2, Name = "帮会防御", Description = "提升全体成员防御力", MaxLevel = 10, UpgradeCost = 10000, ContributionCost = 500, DefBonusPerLevel = 0.5f },
                new() { SkillId = 3, Name = "帮会生命", Description = "提升全体成员生命值", MaxLevel = 10, UpgradeCost = 10000, ContributionCost = 500, HpBonusPerLevel = 0.5f },
            };
        }

        // ===================================================================
        // 帮会创建
        // ===================================================================

        /// <summary>创建帮会 0=成功 1=重名 2=已有帮会 3=金币不足 4=参数无效</summary>
        public int CreateGuild(string name, int iconIndex, ulong playerId, string playerName)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 12) return 4;
            if (_guildList.Any(g => g.Name == name)) return 1;
            if (_myGuild != null) return 2;

            // 检查金币（模拟）
            // if (GameManager.Instance.Player.Gold < CreateGuildCost) return 3;

            var guild = new GuildData
            {
                GuildId = _nextGuildId++,
                Name = name,
                LeaderId = playerId,
                LeaderName = playerName,
                IconIndex = iconIndex,
                Notice = "欢迎加入帮会！",
                CreateTime = DateTime.Now,
                MaxMembers = 40,
                Skills = CreateDefaultSkills(),
            };
            guild.ContributionForNextLevel = guild.NextLevelContribution;

            // 添加帮主
            var leader = new GuildMemberInfo
            {
                PlayerId = playerId,
                Name = playerName,
                Level = GameManager.Instance.Player.Level,
                Position = GuildPosition.Leader,
                Contribution = 100,
                Online = true,
                LastLogin = DateTime.Now,
            };
            guild.Members[playerId] = leader;
            guild.Funds = 0;
            _myGuild = guild;
            _guildList.Add(guild);

            AddLog(guild, $"帮会《{name}》创建，{playerName}成为帮主");
            OnGuildDataChanged?.Invoke(guild);
            Debug.Log($"[Guild] 创建帮会: {name}");
            return 0;
        }

        // ===================================================================
        // 申请/审批
        // ===================================================================

        /// <summary>申请加入帮会</summary>
        public bool ApplyToGuild(ulong guildId, ulong playerId, string playerName, int playerLevel)
        {
            if (_myGuild != null) return false;
            var guild = _guildList.FirstOrDefault(g => g.GuildId == guildId);
            if (guild == null || guild.IsFull) return false;
            if (guild.Applications.Any(a => a.PlayerId == playerId)) return false;

            guild.Applications.Add(new GuildApplication
            {
                PlayerId = playerId,
                PlayerName = playerName,
                PlayerLevel = playerLevel,
                ApplyTime = DateTime.Now,
            });

            Debug.Log($"[Guild] {playerName} 申请加入 {guild.Name}");
            return true;
        }

        /// <summary>审批申请 0=成功 1=无权 2=已满 3=申请不存在</summary>
        public int ApproveApplication(ulong guildId, ulong targetPlayerId, ulong operatorId)
        {
            if (_myGuild == null || _myGuild.GuildId != guildId) return 1;
            if (!HasPermission(operatorId, GuildPosition.Officer)) return 1;
            if (_myGuild.IsFull) return 2;

            var app = _myGuild.Applications.FirstOrDefault(a => a.PlayerId == targetPlayerId);
            if (app == null) return 3;

            _myGuild.Applications.Remove(app);
            var member = new GuildMemberInfo
            {
                PlayerId = app.PlayerId,
                Name = app.PlayerName,
                Level = app.PlayerLevel,
                Position = GuildPosition.Member,
                Online = false,
                LastLogin = DateTime.Now,
            };
            _myGuild.Members[app.PlayerId] = member;

            AddLog(_myGuild, $"{app.PlayerName} 加入帮会");
            OnMemberJoined?.Invoke(member);
            OnGuildDataChanged?.Invoke(_myGuild);
            Debug.Log($"[Guild] {app.PlayerName} 被批准加入");
            return 0;
        }

        /// <summary>拒绝申请</summary>
        public bool RejectApplication(ulong guildId, ulong targetPlayerId, ulong operatorId)
        {
            if (_myGuild == null || _myGuild.GuildId != guildId) return false;
            if (!HasPermission(operatorId, GuildPosition.Officer)) return false;

            return _myGuild.Applications.RemoveAll(a => a.PlayerId == targetPlayerId) > 0;
        }

        // ===================================================================
        // 成员管理
        // ===================================================================

        /// <summary>踢出成员 0=成功 1=无权 2=目标不存在 3=不能踢自己</summary>
        public int KickMember(ulong targetPlayerId, ulong operatorId)
        {
            if (_myGuild == null) return 1;
            if (targetPlayerId == operatorId) return 3;
            if (!_myGuild.Members.TryGetValue(targetPlayerId, out var target)) return 2;
            if (!HasPermission(operatorId, GuildPosition.Officer)) return 1;

            // 只能踢比自己职位低的
            var opMember = GetMember(operatorId);
            if (opMember == null || opMember.Position <= target.Position) return 1;

            _myGuild.Members.Remove(targetPlayerId);
            AddLog(_myGuild, $"{target.Name} 被踢出帮会");
            OnMemberLeft?.Invoke(targetPlayerId);
            OnGuildDataChanged?.Invoke(_myGuild);
            return 0;
        }

        /// <summary>转让帮主</summary>
        public bool TransferLeadership(ulong targetPlayerId, ulong operatorId)
        {
            if (_myGuild == null || _myGuild.LeaderId != operatorId) return false;
            if (!_myGuild.Members.TryGetValue(targetPlayerId, out var target)) return false;

            var oldLeader = _myGuild.Members[operatorId];
            oldLeader.Position = GuildPosition.Member;
            target.Position = GuildPosition.Leader;
            _myGuild.LeaderId = targetPlayerId;
            _myGuild.LeaderName = target.Name;

            AddLog(_myGuild, $"帮主转让：{oldLeader.Name} → {target.Name}");
            OnGuildDataChanged?.Invoke(_myGuild);
            return true;
        }

        /// <summary>设置职位 0=成功 1=无权 2=目标不存在</summary>
        public int SetPosition(ulong targetPlayerId, GuildPosition newPosition, ulong operatorId)
        {
            if (_myGuild == null) return 1;
            if (_myGuild.LeaderId != operatorId) return 1;
            if (!_myGuild.Members.TryGetValue(targetPlayerId, out var target)) return 2;
            if (targetPlayerId == _myGuild.LeaderId) return 1;

            var oldPos = target.Position;
            target.Position = newPosition;
            AddLog(_myGuild, $"{target.Name} 职位变更：{oldPos} → {newPosition}");
            OnGuildDataChanged?.Invoke(_myGuild);
            return 0;
        }

        /// <summary>离开帮会</summary>
        public bool LeaveGuild(ulong playerId)
        {
            if (_myGuild == null) return false;
            if (_myGuild.LeaderId == playerId)
            {
                Debug.Log("[Guild] 帮主不能直接离开，请先转让");
                return false;
            }
            if (!_myGuild.Members.Remove(playerId)) return false;

            var name = _myGuild.Members.TryGetValue(playerId, out var m) ? m.Name : "未知";
            AddLog(_myGuild, $"{name} 离开帮会");
            _myGuild = null;
            OnMemberLeft?.Invoke(playerId);
            OnGuildDataChanged?.Invoke(_myGuild!);
            return true;
        }

        // ===================================================================
        // 帮会技能
        // ===================================================================

        /// <summary>升级帮会技能 0=成功 1=无权 2=已达上限 3=资金不足 4=贡献不足 5=技能不存在</summary>
        public int UpgradeSkill(uint skillId, ulong playerId)
        {
            if (_myGuild == null) return 1;
            if (!HasPermission(playerId, GuildPosition.Officer)) return 1;

            var skill = _myGuild.Skills.FirstOrDefault(s => s.SkillId == skillId);
            if (skill == null) return 5;
            if (skill.IsMaxLevel) return 2;
            if (_myGuild.Funds < skill.UpgradeCost) return 3;

            var member = GetMember(playerId);
            if (member == null || member.Contribution < skill.ContributionCost) return 4;

            // 扣除
            _myGuild.Funds -= skill.UpgradeCost;
            member.Contribution -= skill.ContributionCost;
            skill.Level++;

            AddLog(_myGuild, $"帮会技能《{skill.Name}》升级至Lv.{skill.Level}");
            OnGuildSkillUpgraded?.Invoke(_myGuild, skill);
            OnGuildDataChanged?.Invoke(_myGuild);
            Debug.Log($"[Guild] 技能升级: {skill.Name} Lv.{skill.Level}");
            return 0;
        }

        /// <summary>获取帮会技能总加成</summary>
        public void GetGuildBuff(out float atkBonus, out float defBonus, out float hpBonus)
        {
            atkBonus = 0;
            defBonus = 0;
            hpBonus = 0;
            if (_myGuild == null) return;
            foreach (var s in _myGuild.Skills)
            {
                atkBonus += s.CurrentAtkBonus;
                defBonus += s.CurrentDefBonus;
                hpBonus += s.CurrentHpBonus;
            }
        }

        // ===================================================================
        // 帮会任务
        // ===================================================================

        /// <summary>获取今日帮会任务列表</summary>
        public List<GuildQuestTemplate> GetTodayGuildQuests()
        {
            return new List<GuildQuestTemplate>(_guildQuestTemplates);
        }

        /// <summary>推进帮会任务进度</summary>
        public bool UpdateGuildQuestProgress(uint questId, ulong playerId, int delta = 1)
        {
            if (_myGuild == null) return false;

            var template = _guildQuestTemplates.FirstOrDefault(q => q.QuestId == questId);
            if (template == null) return false;

            if (!_myGuild.DailyQuestProgress.ContainsKey(questId))
                _myGuild.DailyQuestProgress[questId] = 0;

            var old = _myGuild.DailyQuestProgress[questId];
            _myGuild.DailyQuestProgress[questId] = Mathf.Min(old + delta, template.Target);

            // 如果从不可完成变可完成，发放奖励
            if (old < template.Target && _myGuild.DailyQuestProgress[questId] >= template.Target)
            {
                var member = GetMember(playerId);
                if (member != null)
                {
                    member.Contribution += template.ContributionReward;
                    member.DailyContribution += template.ContributionReward;
                    member.WeeklyContribution += template.ContributionReward;
                    _myGuild.Funds += template.FundsReward;
                    _myGuild.TotalContribution += template.ContributionReward;

                    // 检查帮会升级
                    CheckGuildLevelUp();
                }
            }

            OnGuildDataChanged?.Invoke(_myGuild);
            return true;
        }

        // ===================================================================
        // 帮会等级
        // ===================================================================

        private void CheckGuildLevelUp()
        {
            if (_myGuild == null) return;
            while (_myGuild.CanLevelUp)
            {
                _myGuild.TotalContribution -= (int)_myGuild.NextLevelContribution;
                _myGuild.Level++;
                _myGuild.ContributionForNextLevel = _myGuild.NextLevelContribution;
                // 每5级增加人数上限
                if (_myGuild.Level % 5 == 0) _myGuild.MaxMembers += 5;

                AddLog(_myGuild, $"帮会升级至Lv.{_myGuild.Level}！");
                OnGuildLevelUp?.Invoke(_myGuild);
                Debug.Log($"[Guild] 帮会升级: Lv.{_myGuild.Level}");
            }
        }

        // ===================================================================
        // 每日刷新
        // ===================================================================

        private void CheckDailyReset()
        {
            var now = DateTime.Now;
            if (_lastDailyReset.Date < now.Date)
            {
                ResetDaily();
                _lastDailyReset = now;
            }
        }

        private void ResetDaily()
        {
            if (_myGuild == null) return;
            _myGuild.DailyQuestProgress.Clear();

            // 重置成员今日贡献
            foreach (var m in _myGuild.Members.Values)
            {
                m.DailyContribution = 0;
                if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
                    m.WeeklyContribution = 0;
            }

            OnGuildDataChanged?.Invoke(_myGuild);
            Debug.Log("[Guild] 帮会日常已刷新");
        }

        // ===================================================================
        // 修改公告
        // ===================================================================

        public bool SetNotice(string notice, ulong operatorId)
        {
            if (_myGuild == null) return false;
            if (!HasPermission(operatorId, GuildPosition.Officer)) return false;
            _myGuild.Notice = notice;
            OnGuildDataChanged?.Invoke(_myGuild);
            return true;
        }

        // ===================================================================
        // 工具方法
        // ===================================================================

        private bool HasPermission(ulong playerId, GuildPosition minPosition)
        {
            var member = GetMember(playerId);
            return member != null && member.Position >= minPosition;
        }

        public GuildMemberInfo? GetMember(ulong playerId)
        {
            if (_myGuild == null) return null;
            _myGuild.Members.TryGetValue(playerId, out var m);
            return m;
        }

        public List<GuildMemberInfo> GetSortedMembers()
        {
            if (_myGuild == null) return new List<GuildMemberInfo>();
            return _myGuild.Members.Values
                .OrderByDescending(m => m.Position)
                .ThenByDescending(m => m.Contribution)
                .ToList();
        }

        private void AddLog(GuildData guild, string message)
        {
            var entry = new GuildLogEntry { Time = DateTime.Now, Message = message };
            guild.Logs.Add(entry);
            OnNewLogEntry?.Invoke(entry);
            // 只保留最近100条
            if (guild.Logs.Count > 100)
                guild.Logs.RemoveRange(0, guild.Logs.Count - 100);
        }

        public List<GuildLogEntry> GetRecentLogs(int count = 30)
        {
            if (_myGuild == null) return new List<GuildLogEntry>();
            return _myGuild.Logs.TakeLast(count).ToList();
        }

        // ===================================================================
        // 网络消息处理
        // ===================================================================

        public void HandleMessage(uint msgId, byte[] body)
        {
            try
            {
                switch (msgId)
                {
                    case (uint)MsgId.SCGuildInfo:
                        Debug.Log("[Guild] 收到帮会信息");
                        break;
                    case (uint)MsgId.SCGuildMemberJoin:
                        Debug.Log("[Guild] 成员加入通知");
                        break;
                    case (uint)MsgId.SCGuildMemberLeave:
                        Debug.Log("[Guild] 成员离开通知");
                        break;
                    case (uint)MsgId.SCGuildLevelUp:
                        Debug.Log("[Guild] 帮会升级通知");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildManager] HandleMessage error: {ex.Message}");
            }
        }
    }
}