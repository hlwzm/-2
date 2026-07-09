#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
        public int CurrentDungeonIndex = -1;
        public string CurrentDungeonName = "";
        public string CurrentDungeonBoss = "";

        public event Action? OnLoginSuccess;
        public event Action? OnEnterGame;
        public event Action<string>? OnSystemNotice;
        public event Action<uint, ulong>? OnCurrencyChanged;

        [Serializable] public class PlayerData {
            public ulong PlayerId;
            public string Name = "";
            public int Level = 1;
            public ulong Gold, BindGold, Tongbao;
            public int VipLevel; public int MapId = 1001;
            public string Token = "";
        }

        [Serializable] public class HeroData {
            public ulong HeroUid;
            public uint TemplateId;
            public string Name = "";
            public int Level = 1, Star = 1, Quality = 4;
            public bool InTeam;
        }

        void Awake() {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Network.OnMessage += HandleMessage;

            // 自动创建VoiceChatManager
            if (VoiceChatManager.Instance == null)
            {
                var voiceGo = new GameObject("VoiceChatManager");
                voiceGo.transform.SetParent(transform);
                voiceGo.AddComponent<VoiceChatManager>();
            }
        }

        void Update() => Network.Update();
        void OnDestroy() => Network.Dispose();

        void HandleMessage(uint msgId, byte[] body) {
            try {
                var range = msgId / 1000;
                switch (range) {
                    case 1: LoginManager.Instance?.HandleMessage(msgId, body); break;      // 1001-1999 Login+Hero+Recruit
                    case 2: BattleManager.Instance?.HandleMessage(msgId, body); break;     // 2001-2999 Combat
                    case 3: DungeonManager.Instance?.HandleMessage(msgId, body); break;    // 3001-3999 Dungeon
                    case 4: TradeManager.Instance?.HandleMessage(msgId, body); break;      // 4001-4999 Trade
                    case 5: ChatManager.Instance?.HandleMessage(msgId, body); break;       // 5001-5999 Chat
                    case 6: TeamManager.Instance?.HandleMessage(msgId, body); break;       // 6001-6999 Team
                    case 7: FriendManager.Instance?.HandleMessage(msgId, body); break;     // 7001-7999 Friend+Guild
                    case 8: ShopManager.Instance?.HandleMessage(msgId, body); break;       // 8001-8999 Shop
                    case 9: QuestManager.Instance?.HandleMessage(msgId, body); break;      // 9001-9999 Quest
                    case 10: PvpManager.Instance?.HandleMessage(msgId, body); break;       // 10001-10999 PVP
                }
            } catch (Exception ex) {
                Debug.LogError($"[GameManager] HandleMessage({msgId}) error: {ex.Message}");
            }
        }

        public async void ConnectToServer() {
            Debug.Log("[GameManager] Connecting to server...");
            await Network.ConnectAsync(ServerHost, ServerPort);
        }

        public void ShowNotice(string msg) => OnSystemNotice?.Invoke(msg);
        public void FireLoginSuccess() => OnLoginSuccess?.Invoke();
    }
}


