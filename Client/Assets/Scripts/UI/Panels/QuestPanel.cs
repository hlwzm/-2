using UnityEngine;
using UnityEngine.UI;
using Jx3.UI;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 任务面板 — 主线/日常/副本/成就分类浏览 + 奖励预览
    /// 金墨武侠风格 · 全程使用 UIComponentFactory + ThemeColors
    /// </summary>
    public class QuestPanel : BasePanel
    {
        // ─── Demo Quest Data ───

        private struct QuestInfo
        {
            public string Name;
            public int Level;
            public string Desc;
            public string Progress;
            public bool Done;
            public string Reward;

            public QuestInfo(string name, int level, string desc, string progress, bool done, string reward)
            {
                Name = name;
                Level = level;
                Desc = desc;
                Progress = progress;
                Done = done;
                Reward = reward;
            }
        }

        private static readonly string[] TabNames = { "主线", "日常", "副本", "成就" };

        private static readonly string[] SectionTitles =
        {
            "── 主线任务 ──",
            "── 日常任务 ──",
            "── 副本任务 ──",
            "── 成就 ──"
        };

        private static readonly QuestInfo[] MainQuests =
        {
            new("初入江湖", 1, "与稻香村掌门对话，了解江湖概况", "1/1", true, "经验+500 · 金币+200"),
            new("第一件武器", 2, "前往兵器架领取新手武器", "1/1", true, "经验+300 · 青铜剑×1"),
            new("稻香村危机", 5, "消灭10只来犯山贼", "3/10", false, "经验+1000 · 金币+500"),
            new("英雄初现", 8, "通过招募获得一名英雄", "0/1", false, "经验+800 · 招募券×1")
        };

        private static readonly QuestInfo[] DailyQuests =
        {
            new("茶馆听书", 1, "前往茶馆聆听说书人讲江湖轶事", "0/1", false, "经验+200 · 金币+100"),
            new("门派日常", 10, "完成3次门派日常任务", "1/3", false, "经验+600 · 贡献+50"),
            new("每日副本", 20, "完成一次任意副本挑战", "0/1", false, "经验+800 · 金币+300"),
            new("江湖历练", 15, "击败20个野外敌人", "8/20", false, "经验+400 · 铁矿石×5")
        };

        private static readonly QuestInfo[] DungeonQuests =
        {
            new("风雨稻香村", 20, "通关「风雨稻香村」副本", "0/1", false, "经验+1500 · 紫晶石×1"),
            new("天子峰挑战", 35, "击败天子峰最终Boss", "0/1", false, "经验+2000 · 破军刀×1"),
            new("英雄试炼", 30, "在试炼之塔中达到第5层", "2/5", false, "经验+1200 · 试炼徽章×3"),
            new("藏宝秘境", 25, "探索藏宝秘境并开启宝箱", "1/3", false, "经验+1000 · 银钥匙×2")
        };

        private static readonly QuestInfo[] AchievementQuests =
        {
            new("英雄收集者", 1, "收集5名不同的英雄", "2/5", false, "钻石×100"),
            new("百战勇士", 1, "累计击败100个敌人", "47/100", false, "金币+5000"),
            new("江湖行万里", 1, "探索所有地图区域", "3/8", false, "传送符×10"),
            new("装备大师", 1, "将一件装备强化至+10", "5/10", false, "强化石×20")
        };

        // ─── State ───

        private int _currentTab;
        private RectTransform _listContent;
        private RectTransform _tabBar;
        private Text _rewardText;

        // ─── Lifecycle ───

        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);
            UIComponentFactory.CreateTitleBar(root, "任务", BackToMainCity);

            BuildTabBar(root);

            _listContent = UIComponentFactory.CreateScrollView(root, "QuestList",
                new Vector2(1800, 820), new Vector2(0, -30));

            BuildRewardBar(root);

            ShowQuestList();
        }

        // ─── Tab Bar ───

        private void BuildTabBar(RectTransform root)
        {
            var bar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(root, false);
            _tabBar = bar.GetComponent<RectTransform>();
            _tabBar.anchorMin = new Vector2(0.5f, 1f);
            _tabBar.anchorMax = new Vector2(0.5f, 1f);
            _tabBar.sizeDelta = new Vector2(800, 48);
            _tabBar.anchoredPosition = new Vector2(0, -76);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = ThemeColors.SpacingSmall;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                UIComponentFactory.CreateTabButton(_tabBar, "Tab" + i, TabNames[i],
                    i == 0, () => OnTabClicked(idx));
            }
        }

        private void OnTabClicked(int idx)
        {
            _currentTab = idx;
            RefreshTabHighlight();
            ShowQuestList();
        }

        private void RefreshTabHighlight()
        {
            if (_tabBar == null) return;

            for (int i = 0; i < _tabBar.childCount; i++)
            {
                var child = _tabBar.GetChild(i);
                var img = child.GetComponent<Image>();
                var txt = child.GetComponent<Text>();
                bool active = i == _currentTab;

                if (img != null)
                    img.color = active ? ThemeColors.TabActive : ThemeColors.TabInactive;
                if (txt != null)
                    txt.color = active ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            }
        }

        // ─── Quest List ───

        private QuestInfo[] GetCurrentQuests()
        {
            return _currentTab switch
            {
                0 => MainQuests,
                1 => DailyQuests,
                2 => DungeonQuests,
                _ => AchievementQuests
            };
        }

        private void ShowQuestList()
        {
            // Clear previous content
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_listContent.GetChild(i).gameObject);

            // Reset reward preview
            if (_rewardText != null)
                _rewardText.text = "点击任务查看奖励详情";

            // Section header
            var header = UIComponentFactory.CreateText(_listContent, "SectionHeader",
                SectionTitles[_currentTab], ThemeColors.FontBody, ThemeColors.Accent);
            header.fontStyle = FontStyle.Bold;
            header.alignment = TextAnchor.MiddleCenter;
            header.rectTransform.sizeDelta = new Vector2(0, 36);

            // Decorative divider beneath header
            var divider = UIComponentFactory.CreateDivider(_listContent, "HeaderDivider");
            var drt = divider.rectTransform;
            drt.anchorMin = new Vector2(0.5f, 0.5f);
            drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(600, 2);

            // Quest rows
            foreach (var q in GetCurrentQuests())
                CreateQuestRow(q);
        }

        private void CreateQuestRow(QuestInfo q)
        {
            // Card container — width is controlled by VerticalLayoutGroup, height preserved
            var card = UIComponentFactory.CreateCard(_listContent, "Quest_" + q.Name,
                new Vector2(1760, 76), Vector2.zero);
            card.GetComponent<Image>().color = ThemeColors.BgListItem;

            // Checkbox ☐ / ☑
            var check = UIComponentFactory.CreateText(card, "Check",
                q.Done ? "☑" : "☐",
                ThemeColors.FontEntry, q.Done ? ThemeColors.Stamina : ThemeColors.TextNormal);
            check.alignment = TextAnchor.MiddleCenter;
            check.raycastTarget = false;
            var crt = check.rectTransform;
            crt.anchorMin = new Vector2(0, 0.5f);
            crt.anchorMax = new Vector2(0, 0.5f);
            crt.sizeDelta = new Vector2(36, 36);
            crt.anchoredPosition = new Vector2(20, 0);

            // Level badge
            var lvText = UIComponentFactory.CreateText(card, "Level",
                $"Lv.{q.Level}", ThemeColors.FontTiny, ThemeColors.Gold);
            lvText.fontStyle = FontStyle.Bold;
            lvText.alignment = TextAnchor.MiddleLeft;
            lvText.raycastTarget = false;
            var lrt = lvText.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.5f);
            lrt.anchorMax = new Vector2(0, 0.5f);
            lrt.sizeDelta = new Vector2(55, 24);
            lrt.anchoredPosition = new Vector2(65, 12);

            // Quest name
            var nameText = UIComponentFactory.CreateText(card, "Name",
                q.Name, ThemeColors.FontBody, ThemeColors.TextBright);
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.raycastTarget = false;
            var nrt = nameText.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.5f);
            nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.sizeDelta = new Vector2(300, 24);
            nrt.anchoredPosition = new Vector2(125, 12);

            // Description
            var descText = UIComponentFactory.CreateText(card, "Desc",
                q.Desc, ThemeColors.FontTiny, ThemeColors.TextDim);
            descText.alignment = TextAnchor.MiddleLeft;
            descText.raycastTarget = false;
            var drt = descText.rectTransform;
            drt.anchorMin = new Vector2(0, 0.5f);
            drt.anchorMax = new Vector2(0, 0.5f);
            drt.sizeDelta = new Vector2(600, 20);
            drt.anchoredPosition = new Vector2(65, -14);

            // Progress (e.g. "3/10")
            var progText = UIComponentFactory.CreateText(card, "Progress",
                q.Progress, ThemeColors.FontBody, q.Done ? ThemeColors.Stamina : ThemeColors.Gold);
            progText.fontStyle = FontStyle.Bold;
            progText.alignment = TextAnchor.MiddleCenter;
            progText.raycastTarget = false;
            var prt = progText.rectTransform;
            prt.anchorMin = new Vector2(0.72f, 0.5f);
            prt.anchorMax = new Vector2(0.72f, 0.5f);
            prt.sizeDelta = new Vector2(80, 28);
            prt.anchoredPosition = new Vector2(0, 0);

            // Action button: [前往] or [已完]
            if (q.Done)
            {
                var btn = UIComponentFactory.CreateButton(card, "Action", "已完",
                    ThemeColors.BtnSecondary, () => UpdateRewardPreview(q), ThemeColors.FontSmall);
                PositionActionButton(btn);
            }
            else
            {
                var btn = UIComponentFactory.CreatePrimaryButton(card, "Action", "前往",
                    () => UpdateRewardPreview(q));
                PositionActionButton(btn);
            }
        }

        private static void PositionActionButton(Button btn)
        {
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(90, 36);
            rt.anchoredPosition = new Vector2(-10, 0);
        }

        // ─── Reward Preview Bar (bottom) ───

        private void BuildRewardBar(RectTransform root)
        {
            var bar = new GameObject("RewardBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = Vector2.right;
            brt.sizeDelta = new Vector2(0, 70);
            brt.anchoredPosition = new Vector2(0, 35);
            bar.GetComponent<Image>().color = ThemeColors.BgCard;

            // Top accent line
            var line = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(brt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 2);
            lrt.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().color = ThemeColors.Accent;

            // Label
            var label = UIComponentFactory.CreateText(brt, "Label", "奖励预览:",
                ThemeColors.FontSmall, ThemeColors.Accent);
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            var lblRt = label.rectTransform;
            lblRt.anchorMin = new Vector2(0, 0.5f);
            lblRt.anchorMax = new Vector2(0, 0.5f);
            lblRt.sizeDelta = new Vector2(120, 30);
            lblRt.anchoredPosition = new Vector2(20, 0);

            // Reward text (fills remaining space before back button)
            _rewardText = UIComponentFactory.CreateText(brt, "RewardText",
                "点击任务查看奖励详情", ThemeColors.FontSmall, ThemeColors.Gold);
            _rewardText.alignment = TextAnchor.MiddleLeft;
            var rrt = _rewardText.rectTransform;
            rrt.anchorMin = new Vector2(0, 0.5f);
            rrt.anchorMax = new Vector2(0.85f, 0.5f);
            rrt.sizeDelta = new Vector2(-160, 30);
            rrt.anchoredPosition = new Vector2(140, 0);

            // Back button
            var backBtn = UIComponentFactory.CreateSecondaryButton(brt, "BackBtn",
                "返回主城", BackToMainCity);
            var bbrt = backBtn.GetComponent<RectTransform>();
            bbrt.anchorMin = new Vector2(1, 0.5f);
            bbrt.anchorMax = new Vector2(1, 0.5f);
            bbrt.sizeDelta = new Vector2(140, 40);
            bbrt.anchoredPosition = new Vector2(-10, 0);
        }

        private void UpdateRewardPreview(QuestInfo q)
        {
            if (_rewardText != null)
                _rewardText.text = $"【{q.Name}】{q.Reward}";
        }

        // ─── Navigation ───

        private void BackToMainCity()
        {
            UIManager.Instance.Hide<QuestPanel>();
            UIManager.Instance.Show<MainCityPanel>();
        }
    }
}
