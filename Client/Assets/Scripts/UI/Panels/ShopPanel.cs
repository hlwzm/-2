using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class ShopPanel : BasePanel
    {
        public Button[]? categoryBtns;
        public Transform? itemContainer;
        public GameObject? itemPrefab;
        public Button? rechargeBtn;
        public Button? monthlyBtn;

        void Start()
        {
            rechargeBtn?.onClick.AddListener(() => Debug.Log("Open Recharge"));
            monthlyBtn?.onClick.AddListener(() => Debug.Log("Claim Monthly"));
        }

        public override void OnOpen(object data = null) => gameObject.SetActive(true);
        public override void OnClose() => gameObject.SetActive(false);
    }
}
