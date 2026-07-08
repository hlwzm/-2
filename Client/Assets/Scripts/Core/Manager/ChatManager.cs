using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Jx3.Core.Network;

namespace Jx3.Core
{
    public class ChatMessage
    {
        public ulong MsgId;
        public int Channel;
        public ulong SenderId;
        public string SenderName = "";
        public int SenderLevel;
        public string Content = "";
        public int MsgType;
        public long Timestamp;
    }

    public class ChatManager : MonoBehaviour
    {
        public static ChatManager Instance { get; private set; } = null!;
        public List<ChatMessage> Messages { get; } = new();
        public event Action<ChatMessage>? OnNewMessage;

        void Awake() { Instance = this; }

        void Start()
        {
            GameManager.Instance.Network.OnMessage += (msgId, body) =>
            {
                if (msgId == (uint)MsgId.SCChatMessage)
                    HandleChatMessage(body);
            };
        }

        void HandleChatMessage(byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            var msg = new ChatMessage
            {
                MsgId = r.ReadUInt64(),
                Channel = r.ReadInt32(),
                SenderId = r.ReadUInt64(),
                SenderName = r.ReadString(),
                SenderLevel = r.ReadInt32(),
                Content = r.ReadString(),
                MsgType = r.ReadInt32(),
                Timestamp = r.ReadInt64(),
            };
            Messages.Add(msg);
            OnNewMessage?.Invoke(msg);
        }

        public void SendChat(int channel, string content)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(channel);
            w.Write(content);
            w.Write(0); // msgType = text
            GameManager.Instance.Network.Send((uint)MsgId.CSChatSend, ms.ToArray());
        }
    }
}
