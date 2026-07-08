using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class HeroListPanel : BasePanel
    {
        private RectTransform _listRoot;
        private List<GameObject> _items = new();
        private Text _detailText;
        private int _selectedIndex = -1;

        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            CreateText(transform as RectTransform, "Title", "英 雄 列 表", 32);
            ((RectTransform)transform.Find("Title")).anchoredPosition = new Vector2(0, 300);

            _listRoot = new GameObject("ListRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _listRoot.SetParent(transform, false);
            _listRoot.anchorMin = new Vector2(0, 0.3f); _listRoot.anchorMax = new Vector2(0.5f, 0.7f);
            _listRoot.sizeDelta = Vector2.zero; _listRoot.anchoredPosition = new Vector2(100, 0);

            _detailText = CreateText(transform as RectTransform, "Detail", "选择一个英雄查看详情", 20);
            ((RectTransform)_detailText.transform).anchoredPosition = new Vector2(200, 0);
            _detailText.alignment = TextAnchor.UpperLeft;
            ((RectTransform)_detailText.transform).sizeDelta = new Vector2(500, 300);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(700, 350);
        }

        protected override void OnShow()
        {
            base.OnShow();
            Refresh();
            HeroScreenManager.Instance?.RequestHeroList();
        }

        public override void Refresh()
        {
            foreach (var item in _items) Destroy(item);
            _items.Clear();

            var heroes = GameManager.Instance.Heroes;
            for (int i = 0; i < heroes.Count; i++)
            {
                var idx = i; var h = heroes[i];
                var btn = CreateButton(_listRoot, "Hero_" + i, h.Name + " Lv." + h.Level + " ★" + h.Star, () => ShowDetail(idx));
                ((RectTransform)btn.transform).anchoredPosition = new Vector2(0, 80 - i * 60);
                _items.Add(btn.gameObject);
            }
        }

        void ShowDetail(int idx)
        {
            _selectedIndex = idx;
            var h = GameManager.Instance.Heroes[idx];
            _detailText.text = string.Format(
                "名称: {0}\n等级: {1}\n星级: {2}\n品质: {3}\n编队: {4}\n\n技能信息请查看技能面板",
                h.Name, h.Level, h.Star, h.Quality + "星", h.InTeam ? "在队" : "待命");
        }
    }
}
