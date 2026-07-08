using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
namespace Jx3.UI.Panels
{
    public class TradePanel : BasePanel
    {
        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
            CreateText(transform as RectTransform, "Title", "交 易 行", 32);
            ((RectTransform)transform.Find("Title")).anchoredPosition = new Vector2(0, 300);
            CreateText(transform as RectTransform, "Info", "交易行\n- 搜索物品\n- 分类浏览\n- 上架物品(5%手续费)\n- 我的上架\n- 领取金币/物品", 20);
            ((RectTransform)transform.Find("Info")).anchoredPosition = new Vector2(0, 0);
            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(700, 300);
        }
    }
}
