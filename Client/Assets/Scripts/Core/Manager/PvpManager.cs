using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class PvpManager : MonoBehaviour
    {
        public static PvpManager Instance { get; private set; } = null!;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            if (msgId == (uint)MsgId.SCPVPMatchResult) Debug.Log("[PVP] Result");
            if (msgId == (uint)MsgId.SCPVPRankInfo) Debug.Log("[PVP] Rank");
        }

        public void StartMatch() { GameManager.Instance.Network.Send((uint)MsgId.CSPVPMatchStart, new byte[0]); }
        public void RequestRankInfo() { GameManager.Instance.Network.Send((uint)MsgId.CSPVPRankInfo, new byte[0]); }
    }
}