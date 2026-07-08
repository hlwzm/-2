using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class ShopPanel : BasePanel
    {
        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            CreateText(transform as RectTransform, "Title", "商 城", 32);
            ((RectTransform)transform.Find("Title")).anchoredPosition = new Vector2(0, 300);

            string[] categories = {"热卖", "时装", "道具", "特惠", "月卡"};
            for (int i = 0; i < categories.Length; i++)
            {
                var idx = i;
                var btn = CreateButton(transform as RectTransform, "Tab" + i, categories[i], () => ShowCategory(idx));
                ((RectTransform)btn.transform).anchoredPosition = new Vector2(-300 + i * 160, 220);
            }

            var info = CreateText(transform as RectTransform, "Info", "选择分类查看商品\n\n充值档位:\n6元/30元/98元/198元/328元/648元", 20);
            ((RectTransform)info.transform).anchoredPosition = new Vector2(0, 0);

            var rechargeBtn = CreateButton(transform as RectTransform, "RechargeBtn", "充值", () => ShopManager.Instance?.Recharge(1));
            ((RectTransform)rechargeBtn.transform).anchoredPosition = new Vector2(-100, -280);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(100, -280);
        }

        void ShowCategory(int idx) { }
    }
}
