using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Jx3.Core.Network;

namespace Jx3.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; } = null!;

        public NetworkClient Network { get; private set; } = new();
        public PlayerData Player { get; private set; } = new();
        public List<HeroData> Heroes { get; private set; } = new();

        public string ServerHost = "127.0.0.1";
        public int ServerPort = 9000;

        public event Action? OnLoginSuccess;
        public event Action? OnEnterGame;
        public event Action<string>? OnSystemNotice;
        public event Action<uint, ulong>? OnCurrencyChanged;

        [Serializable]
        public class PlayerData
        {
            public ulong PlayerId;
            public string Name = "";
            public int Level = 1;
            public ulong Gold;
            public ulong BindGold;
            public ulong Tongbao;
            public int MapId = 1001;
            public string Token = "";
        }

        [Serializable]
        public class HeroData
        {
            public ulong HeroUid;
            public uint TemplateId;
            public string Name = "";
            public int Level = 1;
            public int Star = 1;
            public int Quality = 4;
            public bool InTeam;
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Network.OnMessage += HandleMessage;
        }

        void Update() => Network.Update();

        void OnDestroy() => Network.Dispose();

        void HandleMessage(uint msgId, byte[] body)
        {
            try
            {
                // 分发到各Manager
                LoginManager.Instance?.HandleMessage(msgId, body);
                HeroScreenManager.Instance?.HandleMessage(msgId, body);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameManager] HandleMessage error: {ex.Message}");
            }
        }

        public async void ConnectToServer()
        {
            Debug.Log("[GameManager] Connecting to server...");
            await Network.ConnectAsync(ServerHost, ServerPort);
        }

        public void ShowNotice(string msg) => OnSystemNotice?.Invoke(msg);
    }

    // 消息ID枚举 (与服务端一致)
    public enum MsgId : uint
    {
        None = 0,
        CSLoginAuth = 1001, SCLoginAuthResult = 1002,
        CSLoginRegister = 1003, SCLoginRegisterResult = 1004,
        CSLoginCreateRole = 1005, SCLoginRoleList = 1006,
        CSLoginEnterGame = 1007, SCLoginEnterGame = 1008,
        CSHeroList = 1101, SCHeroList = 1102,
        CSHeroLevelUp = 1103, CSHeroStarUp = 1105,
        CSRecruitDraw = 1201, CSRecruitPoolList = 1203,
        CSCombatMove = 2001, CSCombatCastSkill = 2002,
        SCCombatDamage = 2003, SCCombatHPChange = 2004,
        SCCombatStateInit = 2005, SCCombatEnd = 2006,
        CSDungeonEnter = 3003, SCDungeonBossHP = 3005,
        CSTradeSell = 4003, CSTradeBuy = 4005,
        CSChatSend = 5001, SCChatMessage = 5002,
        CSChatPrivate = 5003,
        CSTeamCreate = 6001, CSTeamInvite = 6003,
        CSGuildCreate = 7010,
        CSShopBuy = 8003, CSShopRecharge = 8005,
        CSQuestList = 9001, CSQuestAccept = 9003,
    }
}
