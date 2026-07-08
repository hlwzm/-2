using UnityEngine;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class GuildPanel : BasePanel
    {
        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            CreateText(transform as RectTransform, "Title", "同 盟", 32);
            ((RectTransform)transform.Find("Title")).anchoredPosition = new Vector2(0, 300);

            CreateText(transform as RectTransform, "Info", "同盟系统\n- 创建同盟 (消耗500金币)\n- 搜索同盟\n- 成员管理\n- 同盟技能\n- 同盟捐献", 20);
            ((RectTransform)transform.Find("Info")).anchoredPosition = new Vector2(0, 0);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(700, 300);
        }
    }
}
