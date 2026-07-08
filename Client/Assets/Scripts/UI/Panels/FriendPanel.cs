using UnityEngine;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class FriendPanel : BasePanel
    {
        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            CreateText(transform as RectTransform, "Title", "好 友", 32);
            ((RectTransform)transform.Find("Title")).anchoredPosition = new Vector2(0, 300);

            CreateText(transform as RectTransform, "Info", "好友系统\n- 添加好友\n- 好友列表\n- 在线状态\n- 好友切磋", 20);
            ((RectTransform)transform.Find("Info")).anchoredPosition = new Vector2(0, 0);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(700, 300);
        }
    }
}
