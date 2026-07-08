using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class HeroListPanel : BasePanel
    {
        public Transform? heroItemContainer;
        public GameObject? heroItemPrefab;
        public Button? recruitBtn;
        public Button? closeBtn;

        void Start()
        {
            recruitBtn?.onClick.AddListener(() => Debug.Log("Open Recruit Screen"));
            closeBtn?.onClick.AddListener(() => OnClose());
        }

        void OnEnable()
        {
            RefreshHeroList();
        }

        void RefreshHeroList()
        {
            if (heroItemContainer == null || heroItemPrefab == null) return;
            // 清除旧项
            foreach (Transform child in heroItemContainer)
                Destroy(child.gameObject);

            // 创建新项
            foreach (var hero in GameManager.Instance.Heroes)
            {
                var item = Instantiate(heroItemPrefab, heroItemContainer);
                var texts = item.GetComponentsInChildren<Text>();
                if (texts.Length >= 2)
                {
                    texts[0].text = hero.Name;
                    texts[1].text = $"Lv.{hero.Level} ★{hero.Star}";
                }
            }
        }

        public override void OnOpen(object data = null)
        {
            gameObject.SetActive(true);
            Core.HeroScreenManager.Instance.RequestHeroList();
        }

        public override void OnClose() => gameObject.SetActive(false);
    }
}
