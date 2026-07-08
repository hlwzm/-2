using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class TeamManager : MonoBehaviour
    {
        public static TeamManager Instance { get; private set; } = null!;
        public ulong CurrentTeamId;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            if (msgId == (uint)MsgId.SCTeamInfo) Debug.Log("[Team] Info");
            if (msgId == (uint)MsgId.SCTeamInviteReceive) Debug.Log("[Team] Invite");
            if (msgId == (uint)MsgId.SCTeamDisband) Debug.Log("[Team] Disband");
        }

        public void CreateTeam(string name) { using var ms = new MemoryStream(); using var w = new BinaryWriter(ms); w.Write(GameManager.Instance.Player.PlayerId); w.Write(name); GameManager.Instance.Network.Send((uint)MsgId.CSTeamCreate, ms.ToArray()); }
        public void LeaveTeam() { GameManager.Instance.Network.Send((uint)MsgId.CSTeamLeave, new byte[0]); }
    }
}