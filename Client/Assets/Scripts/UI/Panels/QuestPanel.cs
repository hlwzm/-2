using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class QuestPanel : BasePanel
    {
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.28f, 0.8f);
        private static readonly Color ColorTabActive = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorDimText = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorGold = new Color(1f, 0.8f, 0.2f);
        private static readonly Color ColorComplete = new Color(0.4f, 0.9f, 0.4f);
        private static readonly Color ColorRed = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color ColorInProgress = new Color(0.4f, 0.7f, 1f);
        private static readonly Color ColorItemBg = new Color(0.1f, 0.1f, 0.2f, 0.7f);

        private static readonly QuestType[] TabTypes = { QuestType.Main, QuestType.Sub, QuestType.Daily, QuestType.Weekly };
        private static readonly string[] TabNames = { "主线", "支线", "日常", "周常" };

        private int _currentTab = 0;
        private List<GameObject> _questItems = new();
        private List<GameObject> _tabButtons = new();
        private RectTransform _listContent;
        private Text _rewardPreview;

        protected override void Awake()
        {
            base.Awake();
            BuildBackground();
            BuildTopBar();
            BuildTabs();
            BuildQuestList();
            BuildRewardPreview();
            FilterByTab(0);
            QuestManager.Instance.OnQuestProgress += OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestUpdated;
        }
        void OnDestroy()
        {
            if (QuestManager.Instance != null) {
                QuestManager.Instance.OnQuestProgress -= OnQuestUpdated;
                QuestManager.Instance.OnQuestCompleted -= OnQuestUpdated;
            }
        }
        private void OnQuestUpdated(QuestInfo q)
        {
            if (gameObject.activeInHierarchy) FilterByTab(_currentTab);
        }
        private void BuildBackground()
        {
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0.04f, 0.04f, 0.1f, 0.92f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }
        private void BuildTopBar()
        {
            var title = CreateText(transform as RectTransform, "Title", "任务", 32);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(0, 1);
            titleRt.sizeDelta = new Vector2(100, 40); titleRt.anchoredPosition = new Vector2(40, -40);
            var line = new GameObject("TitleLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(transform, false); var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 1); lineRt.anchorMax = new Vector2(1, 1);
            lineRt.sizeDelta = new Vector2(0, 2); lineRt.anchoredPosition = new Vector2(0, -70);
            line.GetComponent<Image>().color = ColorAccent;
        }
        private void BuildTabs()
        {
            float x = 40;
            for (int i = 0; i < TabNames.Length; i++) {
                var idx = i;
                var btn = CreateButton(transform as RectTransform, "Tab" + i, TabNames[i], () => FilterByTab(idx));
                var btnRt = (RectTransform)btn.transform;
                btnRt.anchorMin = new Vector2(0, 1); btnRt.anchorMax = new Vector2(0, 1);
                btnRt.sizeDelta = new Vector2(100, 36); btnRt.anchoredPosition = new Vector2(x, -110);
                _tabButtons.Add(btn.gameObject); x += 110;
            }
        }
        private void BuildQuestList()
        {
            var scroll = new GameObject("QuestScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scroll.transform.SetParent(transform, false);
            var scRt = scroll.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.02f, 0.14f); scRt.anchorMax = new Vector2(0.98f, 0.88f);
            scRt.sizeDelta = Vector2.zero; scRt.anchoredPosition = Vector2.zero;
            scroll.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.5f);
            var sr = scroll.GetComponent<ScrollRect>(); sr.horizontal = false; sr.vertical = true;

            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            vp.transform.SetParent(scroll.transform, false); var vpRt = vp.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero; vp.GetComponent<Image>().color = Color.clear; sr.viewport = vpRt;

            _listContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _listContent.SetParent(vp.transform, false);
            _listContent.anchorMin = new Vector2(0, 1); _listContent.anchorMax = new Vector2(1, 1);
            _listContent.sizeDelta = new Vector2(0, 0); _listContent.anchoredPosition = Vector2.zero; sr.content = _listContent;
        }
        private void FilterByTab(int tabIndex)
        {
            _currentTab = tabIndex;
            for (int i = 0; i < _tabButtons.Count; i++) {
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == tabIndex ? ColorTabActive : ColorTabNormal;
            }
            foreach (var item in _questItems) Destroy(item);
            _questItems.Clear();

            var quests = QuestManager.Instance.GetQuestsByType(TabTypes[tabIndex]);
            float y = -10;
            foreach (var q in quests) {
                var item = BuildQuestItem(q, y);
                _questItems.Add(item); y -= 100;
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Abs(y) + 10);
            UpdateRewardPreview(quests);
        }
        private GameObject BuildQuestItem(QuestInfo q, float y)
        {
            var item = new GameObject("QuestItem", typeof(RectTransform), typeof(Image));
            item.transform.SetParent(_listContent, false);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 1); itemRt.anchorMax = new Vector2(1, 1);
            itemRt.sizeDelta = new Vector2(0, 90); itemRt.anchoredPosition = new Vector2(0, y);
            item.GetComponent<Image>().color = ColorItemBg;

            // Name
            var nameText = CreateText(itemRt, "Name", q.Name, 20);
            var nRt = (RectTransform)nameText.transform;
            nRt.anchorMin = new Vector2(0, 0.5f); nRt.anchorMax = new Vector2(0, 0.5f);
            nRt.sizeDelta = new Vector2(160, 28); nRt.anchoredPosition = new Vector2(20, 22);
            nameText.alignment = TextAnchor.MiddleLeft;

            // Level requirement
            var lvText = CreateText(itemRt, "Lv", "Lv." + q.MinLevel, 14);
            var lRt = (RectTransform)lvText.transform;
            lRt.anchorMin = new Vector2(0, 0.5f); lRt.anchorMax = new Vector2(0, 0.5f);
            lRt.sizeDelta = new Vector2(60, 22); lRt.anchoredPosition = new Vector2(20, -6);
            lvText.alignment = TextAnchor.MiddleLeft; lvText.color = ColorDimText;

            // Description
            var descText = CreateText(itemRt, "Desc", q.Description, 16);
            var dRt = (RectTransform)descText.transform;
            dRt.anchorMin = new Vector2(0, 0.5f); dRt.anchorMax = new Vector2(0, 0.5f);
            dRt.sizeDelta = new Vector2(400, 24); dRt.anchoredPosition = new Vector2(20, -30);
            descText.alignment = TextAnchor.MiddleLeft; descText.color = ColorDimText;

            // Status badge
            string statusStr = "";
            Color statusColor = Color.white;
            switch (q.Status) {
                case QuestStatus.NotAccepted: statusStr = "可接取"; statusColor = ColorInProgress; break;
                case QuestStatus.InProgress: statusStr = "进行中"; statusColor = ColorDimText; break;
                case QuestStatus.CanSubmit: statusStr = "可提交"; statusColor = ColorGold; break;
                case QuestStatus.Completed: statusStr = "已完成"; statusColor = ColorComplete; break;
                case QuestStatus.Locked: statusStr = "未解锁"; statusColor = ColorRed; break;
            }
            var statusText = CreateText(itemRt, "Status", statusStr, 16);
            var sRt1 = (RectTransform)statusText.transform;
            sRt1.anchorMin = new Vector2(0, 0.5f); sRt1.anchorMax = new Vector2(0, 0.5f);
            sRt1.sizeDelta = new Vector2(80, 24); sRt1.anchoredPosition = new Vector2(190, 22);
            statusText.alignment = TextAnchor.MiddleLeft; statusText.color = statusColor;

            // Progress
            if (q.Objectives.Count > 0) {
                var obj = q.Objectives[0];
                var progText = CreateText(itemRt, "Progress", obj.ProgressText, 14);
                var pRt = (RectTransform)progText.transform;
                pRt.anchorMin = new Vector2(0, 0.5f); pRt.anchorMax = new Vector2(0, 0.5f);
                pRt.sizeDelta = new Vector2(80, 22); pRt.anchoredPosition = new Vector2(190, -6);
                progText.alignment = TextAnchor.MiddleLeft; progText.color = ColorDimText;

                var pbBg = new GameObject("ProgressBg", typeof(RectTransform), typeof(Image));
                pbBg.transform.SetParent(itemRt, false);
                var pbRt = pbBg.GetComponent<RectTransform>();
                pbRt.anchorMin = new Vector2(0, 0.5f); pbRt.anchorMax = new Vector2(0, 0.5f);
                pbRt.sizeDelta = new Vector2(120, 14); pbRt.anchoredPosition = new Vector2(60, -38);
                pbBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

                var pf = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
                pf.transform.SetParent(pbBg.transform, false);
                var pfRt = pf.GetComponent<RectTransform>();
                pfRt.anchorMin = new Vector2(0, 0); pfRt.anchorMax = new Vector2(obj.Progress01, 1);
                pfRt.sizeDelta = Vector2.zero;
                pf.GetComponent<Image>().color = q.Status == QuestStatus.CanSubmit ? ColorGold : ColorAccent;
            }
            // Action button
            var pid = GameManager.Instance.Player.PlayerId;
            if (q.Status == QuestStatus.NotAccepted) {
                var acceptBtn = CreateButton(itemRt, "ActionBtn", "接取", () => {
                    if (QuestManager.Instance.AcceptQuest(q.QuestId)) {
                        GameManager.Instance.ShowNotice("已接取: " + q.Name); FilterByTab(_currentTab);
                    } else GameManager.Instance.ShowNotice("接取失败（等级不足或已达上限）");
                });
                var aRt = (RectTransform)acceptBtn.transform;
                aRt.anchorMin = new Vector2(1, 0.5f); aRt.anchorMax = new Vector2(1, 0.5f);
                aRt.sizeDelta = new Vector2(80, 36); aRt.anchoredPosition = new Vector2(-20, 22);
                acceptBtn.GetComponent<Image>().color = ColorAccent;
            } else if (q.Status == QuestStatus.CanSubmit) {
                var submitBtn = CreateButton(itemRt, "ActionBtn", "提交", () => {
                    if (QuestManager.Instance.SubmitQuest(q.QuestId)) {
                        GameManager.Instance.ShowNotice("完成任务: " + q.Name + " 获得奖励！"); FilterByTab(_currentTab);
                    }
                });
                var sRt2 = (RectTransform)submitBtn.transform;
                sRt1.anchorMin = new Vector2(1, 0.5f); sRt1.anchorMax = new Vector2(1, 0.5f);
                sRt1.sizeDelta = new Vector2(80, 36); sRt1.anchoredPosition = new Vector2(-20, 22);
                submitBtn.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f, 0.8f);
            } else if (q.Status == QuestStatus.InProgress) {
                var guideBtn = CreateButton(itemRt, "ActionBtn", "前往", () => {
                    if (QuestManager.Instance.TryGetGuide(q.QuestId, out uint mapId, out Vector3 pos, out string npc)) {
                        GameManager.Instance.ShowNotice("引导至 " + (npc != "" ? npc : "坐标 " + pos));
                    }
                });
                var gRt = (RectTransform)guideBtn.transform;
                gRt.anchorMin = new Vector2(1, 0.5f); gRt.anchorMax = new Vector2(1, 0.5f);
                gRt.sizeDelta = new Vector2(80, 36); gRt.anchoredPosition = new Vector2(-20, 22);
                guideBtn.GetComponent<Image>().color = ColorAccent;
            }
            // Reward info
            if (q.Reward != null) {
                var rewardText = CreateText(itemRt, "Reward", "奖励: 经验+" + q.Reward.Exp + " 金币+" + q.Reward.Gold, 14);
                var rRt = (RectTransform)rewardText.transform;
                rRt.anchorMin = new Vector2(1, 0.5f); rRt.anchorMax = new Vector2(1, 0.5f);
                rRt.sizeDelta = new Vector2(240, 22); rRt.anchoredPosition = new Vector2(-120, -6);
                rewardText.alignment = TextAnchor.MiddleRight; rewardText.color = ColorGold;
            }
            return item;
        }
        private void BuildRewardPreview()
        {
            var bar = new GameObject("RewardBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.02f, 0); barRt.anchorMax = new Vector2(0.98f, 0.12f);
            barRt.sizeDelta = Vector2.zero; barRt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.85f);

            var label = CreateText(barRt, "Label", "概览:", 18);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = new Vector2(0, 0.5f); labelRt.anchorMax = new Vector2(0, 0.5f);
            labelRt.sizeDelta = new Vector2(60, 28); labelRt.anchoredPosition = new Vector2(20, 0);
            label.color = ColorGold; label.alignment = TextAnchor.MiddleLeft;

            _rewardPreview = CreateText(barRt, "Content", "选择分类查看任务", 16);
            var rewardRt = (RectTransform)_rewardPreview.transform;
            rewardRt.anchorMin = new Vector2(0, 0.5f); rewardRt.anchorMax = new Vector2(0, 0.5f);
            rewardRt.sizeDelta = new Vector2(800, 28); rewardRt.anchoredPosition = new Vector2(90, 0);
            _rewardPreview.alignment = TextAnchor.MiddleLeft; _rewardPreview.color = ColorDimText;
        }
        private void UpdateRewardPreview(List<QuestInfo> quests)
        {
            int total = quests.Count;
            int completed = quests.Count(q => q.Status == QuestStatus.Completed || q.Status == QuestStatus.CanSubmit);
            int active = quests.Count(q => q.Status == QuestStatus.InProgress);
            ulong totalGold = 0;
            foreach (var q in quests) if (q.Reward != null) totalGold += q.Reward.Gold;
            if (_rewardPreview != null)
                _rewardPreview.text = "分类: " + TabNames[_currentTab] + "  |  共 " + total + " 个  |  已完成 " + completed + "  |  进行中 " + active + "  |  金币总计 " + totalGold;
        }
        public override void Refresh()
        {
            base.Refresh(); FilterByTab(_currentTab);
        }
    }
}
