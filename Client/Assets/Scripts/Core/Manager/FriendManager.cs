using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Jx3.Core
{
    public class FriendManager : MonoBehaviour
    {
        public static FriendManager Instance { get; private set; } = null!;
        public List<FriendInfo> Friends = new();
        void Awake() { Instance = this; }
        public class FriendInfo { public ulong PlayerId; public string Name = ""; public int Level; public bool Online; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            if (msgId == (uint)MsgId.SCFriendList) Debug.Log("[Friend] List");
            if (msgId == (uint)MsgId.SCFriendRequest) Debug.Log("[Friend] Request");
            if (msgId == (uint)MsgId.SCFriendOnline) Debug.Log("[Friend] Online");
        }

        public void AddFriend(ulong targetId) { using var ms = new MemoryStream(); using var w = new BinaryWriter(ms); w.Write(GameManager.Instance.Player.PlayerId); w.Write(targetId); GameManager.Instance.Network.Send((uint)MsgId.CSFriendAdd, ms.ToArray()); }
        public void RequestList() { GameManager.Instance.Network.Send((uint)MsgId.CSFriendList, new byte[0]); }
    }
}