using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class TradePanel : BasePanel
    {
        public InputField? searchInput;
        public Button? searchBtn;
        public Dropdown? categoryDropdown;
        public Transform? itemContainer;
        public GameObject? itemPrefab;
        public Button? sellBtn;
        public Button? myListingsBtn;
        public Text? feeInfoText;

        void Start()
        {
            searchBtn?.onClick.AddListener(OnSearch);
            sellBtn?.onClick.AddListener(() => Debug.Log("Open Sell UI"));
            myListingsBtn?.onClick.AddListener(() => Debug.Log("Show My Listings"));
            if (feeInfoText != null)
                feeInfoText.text = "交易手续费: 5%";
        }

        void OnSearch()
        {
            var keyword = searchInput?.text ?? "";
            var category = categoryDropdown != null ? categoryDropdown.value : 0;
            Debug.Log($"[Trade] Search: {keyword} (cat={category})");
        }

        public override void OnOpen(object data = null) => gameObject.SetActive(true);
        public override void OnClose() => gameObject.SetActive(false);
    }
}
