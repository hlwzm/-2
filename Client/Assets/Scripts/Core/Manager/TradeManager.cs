using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class TradeManager : MonoBehaviour
    {
        public static TradeManager Instance { get; private set; } = null!;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            if (msgId == (uint)MsgId.SCTradeSellResult) Debug.Log("[Trade] Sell: " + r.ReadInt32());
            if (msgId == (uint)MsgId.SCTradeBuyResult) Debug.Log("[Trade] Buy: " + r.ReadInt32());
        }

        public void SellItem(ulong bagItemId, ulong price, int duration)
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId); w.Write(bagItemId); w.Write(price); w.Write(duration);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeSell, ms.ToArray());
        }
    }
}