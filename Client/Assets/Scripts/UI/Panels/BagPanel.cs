using UnityEngine;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class BagPanel : BasePanel
    {
        private UnityEngine.UI.Text _infoText;
        private UnityEngine.UI.InputField _searchInput;

        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            CreateText(transform as RectTransform, "Title", "背 包", 32);
            ((RectTransform)transform.Find("Title")).anchoredPosition = new Vector2(0, 300);

            _infoText = CreateText(transform as RectTransform, "InfoText", "背包系统\n\n道具分类:\n- 装备\n- 消耗品\n- 材料\n- 碎片\n- 礼包", 20);
            ((RectTransform)_infoText.transform).anchoredPosition = new Vector2(0, 0);
            _infoText.alignment = TextAnchor.MiddleLeft;
            ((RectTransform)_infoText.transform).sizeDelta = new Vector2(400, 400);
            _infoText.transform.localPosition = new Vector3(-300, 0, 0);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(700, 300);
        }
    }
}
