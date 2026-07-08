using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class MainCityPanel : BasePanel
    {
        public Text? playerNameText;
        public Text? playerLevelText;
        public Text? goldText;
        public Button? heroBtn;
        public Button? dungeonBtn;
        public Button? tradeBtn;
        public Button? pvpBtn;
        public Button? chatBtn;
        public Button? shopBtn;
        public Button? guildBtn;
        public Button? questBtn;

        void Start()
        {
            heroBtn?.onClick.AddListener(() => { Debug.Log("Open Hero Screen"); });
            dungeonBtn?.onClick.AddListener(() => { Debug.Log("Open Dungeon Screen"); });
            tradeBtn?.onClick.AddListener(() => { Debug.Log("Open Trade Screen"); });
            pvpBtn?.onClick.AddListener(() => { Debug.Log("Open PVP Screen"); });
            chatBtn?.onClick.AddListener(() => { Debug.Log("Open Chat"); });
            shopBtn?.onClick.AddListener(() => { Debug.Log("Open Shop"); });
            guildBtn?.onClick.AddListener(() => { Debug.Log("Open Guild"); });
            questBtn?.onClick.AddListener(() => { Debug.Log("Open Quest"); });
        }

        void Update()
        {
            var p = GameManager.Instance.Player;
            if (playerNameText != null) playerNameText.text = p.Name;
            if (playerLevelText != null) playerLevelText.text = $"Lv.{p.Level}";
            if (goldText != null) goldText.text = $"{p.Gold}金";
        }

        public override void OnOpen(object data = null)
        {
            gameObject.SetActive(true);
            // 切换到主城场景时请求英雄列表
            Core.HeroScreenManager.Instance.RequestHeroList();
        }

        public override void OnClose() => gameObject.SetActive(false);
    }
}
