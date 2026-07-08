using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 任务面板 - 任务分类Tab + 任务列表 + 奖励预览
    /// </summary>
    public class QuestPanel : BasePanel
    {
        // ===== 配色 =====
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.28f, 0.8f);
        private static readonly Color ColorTabActive = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorDimText = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorGold = new Color(1f, 0.8f, 0.2f);
        private static readonly Color ColorComplete = new Color(0.4f, 0.9f, 0.4f);
        private static readonly Color ColorInProgress = new Color(0.4f, 0.7f, 1f);
        private static readonly Color ColorItemBg = new Color(0.1f, 0.1f, 0.2f, 0.7f);

        // ===== 模拟任务数据 =====
        private class QuestData
        {
            public string Name;
            public string Type;
            public int Level;
            public string Description;
            public int Progress;
            public int Target;
            public bool Completed;
            public string Reward;
        }

        private static readonly string[] TabNames = { "主线", "日常", "副本", "成就" };

        private List<QuestData> _allQuests = new();
        private List<QuestData> _filteredQuests = new();
        private int _currentTab = 0;
        private List<GameObject> _questItems = new();
        private RectTransform _listContent;
        private Text _rewardPreview;

        protected override void Awake()
        {
            base.Awake();
            InitMockData();
            BuildBackground();
            BuildTopBar();
            BuildTabs();
            BuildQuestList();
            BuildRewardPreview();
            FilterByTab(0);
        }

        private void InitMockData()
        {
            _allQuests = new List<QuestData>
            {
                new QuestData { Name = "初入江湖",     Type = "主线", Level = 1,  Description = "拜访新手村的各位乡亲", Progress = 3, Target = 5, Completed = false, Reward = "金币x100 经验x500" },
                new QuestData { Name = "英雄试炼",     Type = "主线", Level = 5,  Description = "击败10个山贼",         Progress = 7, Target = 10, Completed = false, Reward = "金币x300 经验x1500 装备x1" },
                new QuestData { Name = "每日签到",     Type = "日常", Level = 1,  Description = "前往签到NPC处签到",   Progress = 1, Target = 1, Completed = true,  Reward = "金币x50 材料x2" },
                new QuestData { Name = "日常历练",     Type = "日常", Level = 10, Description = "完成3个日常任务",      Progress = 2, Target = 3, Completed = false, Reward = "金币x200 经验x800" },
                new QuestData { Name = "风雨稻香村",   Type = "副本", Level = 15, Description = "通关风雨稻香村副本",   Progress = 0, Target = 1, Completed = false, Reward = "装备x1 金币x500" },
                new QuestData { Name = "英雄收集者",   Type = "成就", Level = 1,  Description = "收集5个英雄",          Progress = 3, Target = 5, Completed = false, Reward = "称号x1 通宝x100" },
            };
        }

        private void BuildBackground()
        {
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0.04f, 0.04f, 0.1f, 0.92f));
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }

        private void BuildTopBar()
        {
            var title = CreateText(transform as RectTransform, "Title", "任 务", 32);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(0, 1);
            titleRt.sizeDelta = new Vector2(100, 40);
            titleRt.anchoredPosition = new Vector2(40, -40);

            var line = new GameObject("TitleLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(transform, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 1);
            lineRt.anchorMax = new Vector2(1, 1);
            lineRt.sizeDelta = new Vector2(0, 2);
            lineRt.anchoredPosition = new Vector2(0, -70);
            line.GetComponent<Image>().color = ColorAccent;

            var achBtn = CreateButton(transform as RectTransform, "AchievementBtn", "🏆 成就", () => Debug.Log("[Quest] 打开成就面板"));
            var achRt = (RectTransform)achBtn.transform;
            achRt.anchorMin = new Vector2(1, 1);
            achRt.anchorMax = new Vector2(1, 1);
            achRt.sizeDelta = new Vector2(110, 36);
            achRt.anchoredPosition = new Vector2(-80, -36);
            achBtn.GetComponent<Image>().color = new Color(0.3f, 0.25f, 0.5f, 0.8f);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "\u2715", () => Hide());
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.sizeDelta = new Vector2(50, 50);
            closeRt.anchoredPosition = new Vector2(-40, -35);
            closeBtn.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.4f, 0.8f);
        }

        private void BuildTabs()
        {
            int tabCount = TabNames.Length;
            float totalWidth = tabCount * 100 + (tabCount - 1) * 4;
            float startX = -totalWidth / 2 + 50;

            for (int i = 0; i < tabCount; i++)
            {
                var idx = i;
                var tab = new GameObject("Tab_" + i, typeof(RectTransform), typeof(Image));
                tab.transform.SetParent(transform, false);
                var tabRt = tab.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.5f, 1);
                tabRt.anchorMax = new Vector2(0.5f, 1);
                tabRt.sizeDelta = new Vector2(100, 36);
                tabRt.anchoredPosition = new Vector2(startX + i * 104, -90);

                var tabImg = tab.GetComponent<Image>();
                tabImg.color = (i == 0) ? ColorTabActive : ColorTabNormal;

                var tabText = CreateText(tabRt, "Label", TabNames[i], 18);
                var textRt = (RectTransform)tabText.transform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                var btn = tab.AddComponent<Button>();
                btn.targetGraphic = tabImg;
                btn.onClick.AddListener(() => OnTabClick(idx));
            }
        }

        private void OnTabClick(int index)
        {
            _currentTab = index;
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tabGo = transform.Find("Tab_" + i);
                if (tabGo != null)
                    tabGo.GetComponent<Image>().color = (i == index) ? ColorTabActive : ColorTabNormal;
            }
            FilterByTab(index);
        }

        private void BuildQuestList()
        {
            var scrollGo = new GameObject("QuestScroll", typeof(RectTransform));
            scrollGo.transform.SetParent(transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.02f, 0.15f);
            scrollRt.anchorMax = new Vector2(0.98f, 0.82f);
            scrollRt.sizeDelta = Vector2.zero;
            scrollRt.anchoredPosition = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollRt, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var contentRt = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            contentRt.SetParent(vpRt, false);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.sizeDelta = new Vector2(0, 0);
            contentRt.anchoredPosition = Vector2.zero;

            scrollRect.viewport = vpRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            _listContent = contentRt;
        }

        private void FilterByTab(int tabIndex)
        {
            string tabName = TabNames[tabIndex];
            _filteredQuests = _allQuests.FindAll(q => q.Type == tabName);
            RefreshList();
        }

        private void RefreshList()
        {
            foreach (var item in _questItems)
                Destroy(item);
            _questItems.Clear();

            _listContent.sizeDelta = new Vector2(0, _filteredQuests.Count * 110 + 10);

            for (int i = 0; i < _filteredQuests.Count; i++)
            {
                var idx = i;
                var q = _filteredQuests[i];
                var itemObj = BuildQuestItem(q, i);
                _questItems.Add(itemObj);
            }

            // 更新奖励预览
            UpdateRewardPreview();

            if (_filteredQuests.Count == 0)
            {
                var empty = CreateText(_listContent, "EmptyHint", "暂无任务", 20);
                var emptyRt = (RectTransform)empty.transform;
                emptyRt.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRt.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRt.sizeDelta = new Vector2(200, 30);
                emptyRt.anchoredPosition = Vector2.zero;
                empty.color = ColorDimText;
                _questItems.Add(empty.gameObject);
            }
        }

        private GameObject BuildQuestItem(QuestData q, int index)
        {
            var item = new GameObject("QuestItem_" + index, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(_listContent, false);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 1);
            itemRt.anchorMax = new Vector2(1, 1);
            itemRt.sizeDelta = new Vector2(0, 100);
            itemRt.anchoredPosition = new Vector2(0, -10 - index * 110);

            var itemImg = item.GetComponent<Image>();
            itemImg.color = ColorItemBg;

            // 完成状态 ☐/☑
            var statusIcon = CreateText(itemRt, "StatusIcon", q.Completed ? "\u2611" : "\u2610", 24);
            var statusRt = (RectTransform)statusIcon.transform;
            statusRt.anchorMin = new Vector2(0, 0.5f);
            statusRt.anchorMax = new Vector2(0, 0.5f);
            statusRt.sizeDelta = new Vector2(30, 30);
            statusRt.anchoredPosition = new Vector2(20, 6);
            statusIcon.color = q.Completed ? ColorComplete : ColorDimText;

            // 任务名称
            var nameText = CreateText(itemRt, "Name", q.Name, 20);
            var nameRt = (RectTransform)nameText.transform;
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(0, 0.5f);
            nameRt.sizeDelta = new Vector2(200, 28);
            nameRt.anchoredPosition = new Vector2(60, 18);
            nameText.alignment = TextAnchor.MiddleLeft;

            // 等级标签
            var lvlText = CreateText(itemRt, "Level", "Lv." + q.Level, 14);
            var lvlRt = (RectTransform)lvlText.transform;
            lvlRt.anchorMin = new Vector2(0, 0.5f);
            lvlRt.anchorMax = new Vector2(0, 0.5f);
            lvlRt.sizeDelta = new Vector2(50, 22);
            lvlRt.anchoredPosition = new Vector2(270, 18);
            lvlText.color = ColorDimText;
            lvlText.alignment = TextAnchor.MiddleLeft;

            // 描述
            var descText = CreateText(itemRt, "Desc", q.Description, 16);
            var descRt = (RectTransform)descText.transform;
            descRt.anchorMin = new Vector2(0, 0.5f);
            descRt.anchorMax = new Vector2(0, 0.5f);
            descRt.sizeDelta = new Vector2(500, 24);
            descRt.anchoredPosition = new Vector2(60, -10);
            descText.alignment = TextAnchor.MiddleLeft;
            descText.color = ColorDimText;

            // 进度条背景
            var progressBg = new GameObject("ProgressBg", typeof(RectTransform), typeof(Image));
            progressBg.transform.SetParent(itemRt, false);
            var pbRt = progressBg.GetComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(0, 0.5f);
            pbRt.anchorMax = new Vector2(0, 0.5f);
            pbRt.sizeDelta = new Vector2(120, 14);
            pbRt.anchoredPosition = new Vector2(60, -38);
            progressBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

            // 进度条填充
            var progressFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            progressFill.transform.SetParent(progressBg.transform, false);
            var pfRt = progressFill.GetComponent<RectTransform>();
            pfRt.anchorMin = new Vector2(0, 0);
            pfRt.anchorMax = new Vector2(q.Target > 0 ? (float)q.Progress / q.Target : 0, 1);
            pfRt.sizeDelta = Vector2.zero;
            progressFill.GetComponent<Image>().color = q.Completed ? ColorComplete : ColorAccent;

            // 进度文字
            var progressText = CreateText(itemRt, "ProgressText", q.Progress + "/" + q.Target, 14);
            var ptRt = (RectTransform)progressText.transform;
            ptRt.anchorMin = new Vector2(0, 0.5f);
            ptRt.anchorMax = new Vector2(0, 0.5f);
            ptRt.sizeDelta = new Vector2(60, 20);
            ptRt.anchoredPosition = new Vector2(190, -38);
            progressText.alignment = TextAnchor.MiddleLeft;
            progressText.color = q.Completed ? ColorComplete : ColorDimText;

            // 右侧操作按钮
            if (q.Completed)
            {
                var rewardBtn = CreateButton(itemRt, "ActionBtn", "已完", () => Debug.Log("[Quest] 领取奖励: " + q.Name));
                var actRt = (RectTransform)rewardBtn.transform;
                actRt.anchorMin = new Vector2(1, 0.5f);
                actRt.anchorMax = new Vector2(1, 0.5f);
                actRt.sizeDelta = new Vector2(80, 36);
                actRt.anchoredPosition = new Vector2(-20, 6);
                rewardBtn.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f, 0.8f);
            }
            else
            {
                var gotoBtn = CreateButton(itemRt, "ActionBtn", "前往", () => Debug.Log("[Quest] 前往任务: " + q.Name));
                var actRt = (RectTransform)gotoBtn.transform;
                actRt.anchorMin = new Vector2(1, 0.5f);
                actRt.anchorMax = new Vector2(1, 0.5f);
                actRt.sizeDelta = new Vector2(80, 36);
                actRt.anchoredPosition = new Vector2(-20, 6);
                gotoBtn.GetComponent<Image>().color = ColorAccent;
            }

            return item;
        }

        private void BuildRewardPreview()
        {
            var bar = new GameObject("RewardBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.02f, 0);
            barRt.anchorMax = new Vector2(0.98f, 0.12f);
            barRt.sizeDelta = Vector2.zero;
            barRt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.85f);

            var label = CreateText(barRt, "Label", "奖励预览:", 18);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = new Vector2(0, 0.5f);
            labelRt.anchorMax = new Vector2(0, 0.5f);
            labelRt.sizeDelta = new Vector2(100, 28);
            labelRt.anchoredPosition = new Vector2(20, 0);
            label.color = ColorGold;
            label.alignment = TextAnchor.MiddleLeft;

            _rewardPreview = CreateText(barRt, "Content", "选择任务查看奖励", 16);
            var rewardRt = (RectTransform)_rewardPreview.transform;
            rewardRt.anchorMin = new Vector2(0, 0.5f);
            rewardRt.anchorMax = new Vector2(0, 0.5f);
            rewardRt.sizeDelta = new Vector2(800, 28);
            rewardRt.anchoredPosition = new Vector2(130, 0);
            _rewardPreview.alignment = TextAnchor.MiddleLeft;
            _rewardPreview.color = ColorDimText;
        }

        private void UpdateRewardPreview()
        {
            string preview = "";
            int count = _filteredQuests.Count;
            int completed = _filteredQuests.FindAll(q => q.Completed).Count;
            if (count > 0)
                preview = string.Format("当前筛选: {0}个任务 | 已完成: {1}/{2}", count, completed, count);
            else
                preview = "该分类暂无任务";

            if (_rewardPreview != null)
                _rewardPreview.text = preview;
        }

        public override void Refresh()
        {
            base.Refresh();
            FilterByTab(_currentTab);
        }
    }
}