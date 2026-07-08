using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; } = null!;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            if (msgId == (uint)MsgId.SCShopList) Debug.Log("[Shop] List");
            if (msgId == (uint)MsgId.SCShopBuyResult) Debug.Log("[Shop] Buy: " + r.ReadInt32());
        }

        public void BuyItem(uint shopItemId) { using var ms = new MemoryStream(); using var w = new BinaryWriter(ms); w.Write(GameManager.Instance.Player.PlayerId); w.Write(shopItemId); GameManager.Instance.Network.Send((uint)MsgId.CSShopBuy, ms.ToArray()); }
        public void Recharge(uint tierId) { using var ms = new MemoryStream(); using var w = new BinaryWriter(ms); w.Write(GameManager.Instance.Player.PlayerId); w.Write(tierId); GameManager.Instance.Network.Send((uint)MsgId.CSShopRecharge, ms.ToArray()); }
        public void UseGiftCode(string code) { using var ms = new MemoryStream(); using var w = new BinaryWriter(ms); w.Write(GameManager.Instance.Player.PlayerId); w.Write(code); GameManager.Instance.Network.Send((uint)MsgId.CSShopGiftCode, ms.ToArray()); }
    }
}