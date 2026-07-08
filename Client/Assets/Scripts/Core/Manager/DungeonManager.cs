using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Jx3.Core.Battle;
using Jx3.UI.Panels;

namespace Jx3.Core
{
    /// <summary>
    /// 副本管理器 - 副本状态/存活Boss/限时/阶段/解锁检测
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; } = null!;

        // ===== 当前副本状态 =====
        public int CurrentDungeonId { get; private set; } = -1;
        public DungeonData CurrentDungeonData { get; private set; }
        public float TimeLimitSeconds { get; private set; } = 480f;
        public float TimeRemaining { get; private set; } = 480f;
        public int DungeonPhase { get; private set; } = 1; // 1=阶段1, 2=阶段2
        public bool IsDungeonActive { get; private set; } = false;

        // ===== Boss状态 =====
        private Dictionary<int, BossState> _bossStates = new();
        private List<int> _aliveBossIds = new();
        public bool UltimateBossUnlocked { get; private set; } = false;
        public int UltimateBossId { get; private set; } = -1;

        // ===== 事件回调 =====
        public System.Action<int> OnBossKilled;           // bossId
        public System.Action OnAllMinibossKilled;         // 三小Boss全击杀
        public System.Action OnUltimateBossUnlocked;      // 终极Boss解锁
        public System.Action OnDungeonComplete;           // 副本通关
        public System.Action<string> OnDungeonFailed;     // 副本失败（原因）
        public System.Action<int> OnPhaseChanged;         // 阶段变化 (1→2)

        // ===== UI引用 =====
        private DungeonPanel _dungeonPanel;
        private EnemyUnit _currentBoss;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _dungeonPanel = FindObjectOfType<DungeonPanel>();
        }

        void Update()
        {
            if (!IsDungeonActive) return;

            // 限时倒计时
            if (TimeRemaining > 0)
            {
                TimeRemaining -= Time.deltaTime;
                if (TimeRemaining <= 0)
                {
                    TimeRemaining = 0;
                    FailDungeon("⏰ 时间耗尽，副本失败！");
                }
            }

            // 同步DungeonPanel数据
            SyncPanelData();

            // 阶段检测（基于当前Boss血量）
            if (_currentBoss != null && _currentBoss.isAlive)
            {
                int newPhase = _currentBoss.currentHp <= _currentBoss.maxHp * 0.5f ? 2 : 1;
                if (newPhase != DungeonPhase)
                {
                    DungeonPhase = newPhase;
                    OnPhaseChanged?.Invoke(DungeonPhase);
                    Debug.Log($"[DungeonManager] 副本阶段切换为 {DungeonPhase}");
                }
            }
        }

        /// <summary>
        /// 同步数据到DungeonPanel
        /// </summary>
        private void SyncPanelData()
        {
            if (_dungeonPanel == null) return;

            // 限时
            _dungeonPanel.SetTimeLimit(TimeLimitSeconds);

            // Boss血量
            if (_currentBoss != null)
            {
                _dungeonPanel.SetBossHp(_currentBoss.currentHp, _currentBoss.maxHp);
                _dungeonPanel.SetBossName(_currentBoss.bossName);

                // 副本阶段同步
                _dungeonPanel.DungeonPhase = DungeonPhase;
            }

            // 终极Boss解锁状态
            _dungeonPanel.UltimateBossUnlocked = UltimateBossUnlocked;

            // 小Boss击杀状态同步
            for (int i = 0; i < _dungeonPanel.MinibossKilled.Length; i++)
            {
                if (_dungeonPanel.MinibossKilled[i])
                {
                    // 标记为已击杀
                }
            }
        }

        // =====================================================================
        // 副本初始化
        // =====================================================================

        /// <summary>
        /// 初始化副本（进入副本时调用）
        /// </summary>
        public void InitDungeon(int dungeonId)
        {
            CurrentDungeonId = dungeonId;
            CurrentDungeonData = DungeonConfig.GetDungeon(dungeonId);

            if (CurrentDungeonData == null)
            {
                Debug.LogError($"[DungeonManager] 找不到副本ID={dungeonId}");
                return;
            }

            // 设置限时
            TimeLimitSeconds = CurrentDungeonData.timeLimit;
            TimeRemaining = TimeLimitSeconds;

            // 初始化Boss状态
            _bossStates.Clear();
            _aliveBossIds.Clear();
            UltimateBossUnlocked = false;
            DungeonPhase = 1;
            IsDungeonActive = true;

            foreach (var bossId in CurrentDungeonData.bossIds)
            {
                var bossData = DungeonConfig.GetBoss(bossId);
                if (bossData != null)
                {
                    bool isUltimate = bossData.order == 4;
                    _bossStates[bossId] = new BossState
                    {
                        bossId = bossId,
                        isUltimate = isUltimate,
                        isAlive = true,
                        currentHp = bossData.hp,
                        maxHp = bossData.hp,
                        order = bossData.order
                    };
                    _aliveBossIds.Add(bossId);

                    if (isUltimate)
                        UltimateBossId = bossId;
                }
            }

            // 终极Boss初始锁定
            if (UltimateBossId >= 0 && _bossStates.ContainsKey(UltimateBossId))
            {
                _bossStates[UltimateBossId].isLocked = true;
            }

            Debug.Log($"[DungeonManager] 副本初始化完成: {CurrentDungeonData.name} | " +
                      $"限时{TimeLimitSeconds}秒 | {_bossStates.Count}个Boss | " +
                      $"终极BossID={UltimateBossId}");
        }

        // =====================================================================
        // Boss击杀检测
        // =====================================================================

        /// <summary>
        /// 注册Boss死亡（由EnemyUnit在Die()中调用）
        /// </summary>
        public void RegisterBossDeath(int bossId)
        {
            if (!_bossStates.ContainsKey(bossId)) return;

            _bossStates[bossId].isAlive = false;
            _aliveBossIds.Remove(bossId);

            Debug.Log($"[DungeonManager] Boss死亡: ID={bossId} | 剩余存活: {_aliveBossIds.Count}");

            OnBossKilled?.Invoke(bossId);

            // 检查是否所有小Boss已击杀
            if (!UltimateBossUnlocked && AreAllMinibossKilled())
            {
                UnlockUltimateBoss();
            }

            // 检查副本通关（终极Boss死亡）
            if (IsBossUltimate(bossId))
            {
                CompleteDungeon();
            }
        }

        /// <summary>
        /// 检查指定Boss是否已被击杀
        /// </summary>
        public bool CheckBossKill(int bossId)
        {
            return _bossStates.ContainsKey(bossId) && !_bossStates[bossId].isAlive;
        }

        /// <summary>
        /// 检查是否所有小Boss都已击杀
        /// </summary>
        public bool AreAllMinibossKilled()
        {
            var minibosses = _bossStates.Values.Where(b => !b.isUltimate && !b.isLocked).ToList();
            return minibosses.Count > 0 && minibosses.All(b => !b.isAlive);
        }

        /// <summary>
        /// 获取小Boss存活数量
        /// </summary>
        public int GetAliveMinibossCount()
        {
            return _bossStates.Values.Count(b => !b.isUltimate && b.isAlive);
        }

        /// <summary>
        /// 解锁终极Boss
        /// </summary>
        private void UnlockUltimateBoss()
        {
            if (UltimateBossUnlocked) return;
            UltimateBossUnlocked = true;

            if (UltimateBossId >= 0 && _bossStates.ContainsKey(UltimateBossId))
            {
                _bossStates[UltimateBossId].isLocked = false;
            }

            Debug.Log("[DungeonManager] 🔥 终极Boss已解锁！");
            OnAllMinibossKilled?.Invoke();
            OnUltimateBossUnlocked?.Invoke();
        }

        // =====================================================================
        // 状态查询
        // =====================================================================

        /// <summary>
        /// 返回终极Boss解锁状态
        /// </summary>
        public bool GetUnlockState()
        {
            return UltimateBossUnlocked;
        }

        /// <summary>
        /// 返回副本剩余时间（秒）
        /// </summary>
        public float GetDungeonTimer()
        {
            return TimeRemaining;
        }

        /// <summary>
        /// 获取指定Boss的当前血量
        /// </summary>
        public float GetBossHp(int bossId)
        {
            return _bossStates.ContainsKey(bossId) ? _bossStates[bossId].currentHp : 0f;
        }

        /// <summary>
        /// 获取所有存活Boss的ID列表
        /// </summary>
        public List<int> GetAliveBossIds()
        {
            return new List<int>(_aliveBossIds);
        }

        /// <summary>
        /// 判断Boss是否是终极Boss
        /// </summary>
        public bool IsBossUltimate(int bossId)
        {
            return bossId == UltimateBossId;
        }

        // =====================================================================
        // 副本结果
        // =====================================================================

        /// <summary>
        /// 注册当前战斗中的Boss引用
        /// </summary>
        public void RegisterCurrentBoss(EnemyUnit boss)
        {
            _currentBoss = boss;
        }

        /// <summary>
        /// 副本通关
        /// </summary>
        private void CompleteDungeon()
        {
            if (!IsDungeonActive) return;
            IsDungeonActive = false;

            Debug.Log($"[DungeonManager] 🎉 副本通关！{CurrentDungeonData?.name}");
            OnDungeonComplete?.Invoke();
        }

        /// <summary>
        /// 副本失败
        /// </summary>
        public void FailDungeon(string reason)
        {
            if (!IsDungeonActive) return;
            IsDungeonActive = false;

            Debug.Log($"[DungeonManager] ❌ 副本失败: {reason}");
            OnDungeonFailed?.Invoke(reason);
        }

        /// <summary>
        /// 更新Boss血量（被EnemyUnit调用）
        /// </summary>
        public void UpdateBossHp(int bossId, float currentHp)
        {
            if (_bossStates.ContainsKey(bossId))
            {
                _bossStates[bossId].currentHp = currentHp;
            }
        }

        // =====================================================================
        // 网络消息处理
        // =====================================================================

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));

            if (msgId == (uint)MsgId.SCDungeonEnterResult)
            {
                int result = r.ReadInt32();
                Debug.Log("[Dungeon] 进入副本结果: " + result);
                if (result == 0)
                {
                    int dungeonId = r.ReadInt32();
                    InitDungeon(dungeonId);
                }
            }
            else if (msgId == (uint)MsgId.SCDungeonBossHP)
            {
                int bossId = r.ReadInt32();
                float hp = r.ReadSingle();
                Debug.Log($"[Dungeon] Boss HP更新: ID={bossId} HP={hp}");
                UpdateBossHp(bossId, hp);
            }
            else if (msgId == (uint)MsgId.SCDungeonComplete)
            {
                Debug.Log("[Dungeon] 副本通关！");
                CompleteDungeon();
            }
            else if (msgId == (uint)MsgId.SCDungeonFail)
            {
                string reason = r.ReadString();
                Debug.Log("[Dungeon] 副本失败: " + reason);
                FailDungeon(reason);
            }
        }

        public void RequestDungeonList()
        {
            GameManager.Instance.Network.Send((uint)MsgId.CSDungeonList, new byte[0]);
        }

        public void EnterDungeon(int dungeonId, int difficulty, ulong teamId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(dungeonId);
            w.Write(difficulty);
            w.Write(teamId);
            GameManager.Instance.Network.Send((uint)MsgId.CSDungeonEnter, ms.ToArray());
        }

        /// <summary>
        /// Boss内部状态
        /// </summary>
        private class BossState
        {
            public int bossId;
            public bool isUltimate;
            public bool isLocked;
            public bool isAlive = true;
            public float currentHp;
            public float maxHp;
            public int order;
        }
    }
}