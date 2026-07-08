using UnityEngine;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class QuestPanel : BasePanel
    {
        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            CreateText(transform as RectTransform, "Title", "任 务", 32);
            ((RectTransform)transform.Find("Title")).anchoredPosition = new Vector2(0, 300);

            var info = CreateText(transform as RectTransform, "Info", "任务分类:\n\n[主线任务]\n  初入江湖 / 初识英雄 / 第一次战斗\n\n[支线任务]\n  装备强化\n\n[日常任务]\n  每日签到 / 每日副本 / 每日竞技\n\n[成就任务]\n  英雄收集者 / 百战勇士 / 财富积累", 20);
            ((RectTransform)info.transform).anchoredPosition = new Vector2(0, 0);
            info.alignment = TextAnchor.MiddleLeft;
            ((RectTransform)info.transform).sizeDelta = new Vector2(500, 500);
            info.transform.localPosition = new Vector3(-300, 50, 0);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(700, 300);
        }
    }
}
