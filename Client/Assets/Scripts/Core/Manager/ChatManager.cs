using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Jx3.Core
{
    public class ChatMessage
    {
        public ulong MsgId; public int Channel; public ulong SenderId;
        public string SenderName = ""; public int SenderLevel;
        public string Content = ""; public int MsgType; public long Timestamp;
    }

    public class ChatManager : MonoBehaviour
    {
        public static ChatManager Instance { get; private set; } = null!;
        public List<ChatMessage> Messages { get; } = new();
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body) { }

        public void SendChat(int channel, string content)
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId); w.Write(channel); w.Write(content); w.Write(0);
            GameManager.Instance.Network.Send((uint)MsgId.CSChatSend, ms.ToArray());
        }
    }
}
