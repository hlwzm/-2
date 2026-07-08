using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; } = null!;
        private bool _isAuto;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            if (msgId == (uint)MsgId.SCCombatStateInit) Debug.Log("[Battle] Started");
            if (msgId == (uint)MsgId.SCCombatEnd) Debug.Log("[Battle] End win=" + r.ReadBoolean());
            if (msgId == (uint)MsgId.SCCombatDamage) Debug.Log("[Battle] Dmg=" + r.ReadInt32());
        }

        public void CastSkill(uint skillId)
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId); w.Write(skillId);
            GameManager.Instance.Network.Send((uint)MsgId.CSCombatCastSkill, ms.ToArray());
        }

        public void ToggleAuto()
        {
            _isAuto = !_isAuto;
            var id = _isAuto ? MsgId.CSCombatAutoOn : MsgId.CSCombatAutoOff;
            GameManager.Instance.Network.Send((uint)id, new byte[0]);
        }
    }
}