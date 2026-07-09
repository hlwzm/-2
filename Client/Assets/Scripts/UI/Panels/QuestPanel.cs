using UnityEngine;
using UnityEngine.UI;
using Jx3.UI;

namespace Jx3.UI.Panels
{
    public class QuestPanel : BasePanel
    {
        private int _currentTab = 0;
        private RectTransform _listContent;
        private Text _rewardPreview;

        private static readonly string[] TabNames = { "主线", "日常", "副本", "成就" };

        // Demo quest data
        private static readonly (string name, int level, string desc, string progress, bool done, string reward)[] MainQuests = {
            ("初入江湖", 1, "与稻香村掌门对话", "1/1", true, "经验+500 · 金币+200"),
            ("第一件武器", 2, "领取新手武器", "1/1", true, "经验+300 · 青铜剑×1"),
            ("稻香村危机", 5, "消灭10只山贼", "3/10", false, "经验+1000 · 金币+500"),
            ("英雄初现", 8, "通过招募获得一名英雄", "0/1", false, "经验+800 · 招募券×1"),
        };
        private static readonly (string name, int level, string desc, string progress, bool done, string reward)[] DailyQuests = {
            ("茶馆听书", 1, "前往茶馆听书", "0/1", false, "经验+200 · 金币+100"),
            ("门派日常", 10, "完成3次门派任务", "1/3", false, "经验+600 · 贡献+50"),
            ("每日副本", 20, "完成一次副本", "0/1", false, "经验+800 · 金币+300"),
        };
        private static readonly (string name, int level, string desc, string progress, bool done, string reward)[] DungeonQuests = {
            ("风雨稻香村", 20, "通关风雨稻香村", "0/1", false, "经验+1500 · 紫晶石×1"),
            ("天子峰挑战", 35, "击败天子峰最终Boss", "0/1", false, "经验+2000 · 破军刀×1"),
        };
        private static readonly (string name, int level, string desc, string progress, bool done, string reward)[] AchievementQuests = {
            ("英雄收集者", 1, "收集5名不同的英雄", "2/5", false, "钻石×100"),
            ("百战勇士", 1, "累计击败100个敌人", "47/100", false, "金币+5000"),
        };

        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);

            // TitleBar
            UIComponentFactory.CreateTitleBar(root, "任务", () =>
            {
                UIManager.Instance.Hide<QuestPanel>();
                UIManager.Instance.Show<MainCityPanel>();
            });

            // Achievement button on the right of title bar
            var achBtn = UIComponentFactory.CreateSecondaryButton(root, "AchBtn", "成就", () =>
            {
                _currentTab = 3;
                RefreshTabHighlight();
                ShowQuestList();
            });
            var art = achBtn.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1, 1); art.anchorMax = new Vector2(1, 1);
            art.sizeDelta = new Vector2(80, 36); art.anchoredPosition = new Vector2(-100, -18);

            BuildTabs(root);
            _listContent = UIComponentFactory.CreateScrollView(root, "QuestList",
                new Vector2(1800, 780), new Vector2(0, -60));
            BuildRewardPreview(root);

            ShowQuestList();
        }

        private void BuildTabs(RectTransform root)
        {
            var bar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(root, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(800, 44); rt.anchoredPosition = new Vector2(0, -68);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = ThemeColors.SpacingSmall;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var btn = UIComponentFactory.CreateTabButton(rt, "Tab" + i, TabNames[i], i == 0, () =>
                {
                    _currentTab = idx;
                    RefreshTabHighlight();
                    ShowQuestList();
                });
            }
        }

        private void RefreshTabHighlight()
        {
            var bar = transform.Find("TabBar");
            if (bar == null) return;
            for (int i = 0; i < bar.childCount; i++)
            {
                var child = bar.GetChild(i);
                var img = child.GetComponent<Image>();
                var txt = child.GetComponent<Text>();
                if (img != null) img.color = i == _currentTab ? ThemeColors.TabActive : ThemeColors.TabInactive;
                if (txt != null) txt.color = i == _currentTab ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            }
        }

        private void ShowQuestList()
        {
            // Clear
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_listContent.GetChild(i).gameObject);

            var quests = _currentTab switch
            {
                0 => MainQuests, 1 => DailyQuests, 2 => DungeonQuests, _ => AchievementQuests
            };
            string sectionTitle = _currentTab switch
            {
                0 => "── 主线任务 ──", 1 => "── 日常任务 ──",
                2 => "── 副本任务 ──", _ => "── 成就 ──"
            };

            // Section header
            var header = UIComponentFactory.CreateText(_listContent, "Header", sectionTitle,
                ThemeColors.FontBody, ThemeColors.Accent);
            header.fontStyle = FontStyle.Bold;
            var hrt = header.rectTransform;
            hrt.sizeDelta = new Vector2(0, 40);

            // Quest rows
            foreach (var q in quests)
            {
                CreateQuestRow(q.name, q.level, q.desc, q.progress, q.done, q.reward);
            }
        }

        private void CreateQuestRow(string name, int level, string desc, string progress, bool done, string reward)
        {
            var card = new GameObject("QuestRow", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_listContent, false);
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 72);
            card.GetComponent<Image>().color = ThemeColors.BgListItem;

            // Checkbox
            var check = UIComponentFactory.CreateText(rt, "Check", done ? "☑" : "☐",
                ThemeColors.FontBody, done ? ThemeColors.QualityGood : ThemeColors.TextNormal);
            check.raycastTarget = false;
            var crt = check.rectTransform;
            crt.anchorMin = new Vector2(0, 0.5f); crt.anchorMax = new Vector2(0, 0.5f);
            crt.sizeDelta = new Vector2(30, 30); crt.anchoredPosition = new Vector2(20, 0);

            // Name
            var nameTxt = UIComponentFactory.CreateText(rt, "Name", $"Lv.{level} {name}",
                ThemeColors.FontBody, ThemeColors.TextBright);
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleLeft;
            nameTxt.raycastTarget = false;
            var nrt = nameTxt.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.sizeDelta = new Vector2(300, 30); nrt.anchoredPosition = new Vector2(60, 8);

            // Description
            var descTxt = UIComponentFactory.CreateText(rt, "Desc", desc,
                ThemeColors.FontTiny, ThemeColors.TextNormal);
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.raycastTarget = false;
            var drt = descTxt.rectTransform;
            drt.anchorMin = new Vector2(0, 0.5f); drt.anchorMax = new Vector2(0, 0.5f);
            drt.sizeDelta = new Vector2(300, 20); drt.anchoredPosition = new Vector2(60, -12);

            // Progress
            var progTxt = UIComponentFactory.CreateText(rt, "Progress", progress,
                ThemeColors.FontBody, done ? ThemeColors.QualityGood : ThemeColors.Gold);
            progTxt.fontStyle = FontStyle.Bold;
            progTxt.raycastTarget = false;
            var prt = progTxt.rectTransform;
            prt.anchorMin = new Vector2(0.6f, 0.5f); prt.anchorMax = new Vector2(0.6f, 0.5f);
            prt.sizeDelta = new Vector2(80, 30); prt.anchoredPosition = new Vector2(0, 0);

            // Reward text
            var rewTxt = UIComponentFactory.CreateText(rt, "Reward", reward,
                ThemeColors.FontTiny, ThemeColors.TextDim);
            rewTxt.alignment = TextAnchor.MiddleLeft;
            rewTxt.raycastTarget = false;
            var rrt = rewTxt.rectTransform;
            rrt.anchorMin = new Vector2(0.7f, 0.5f); rrt.anchorMax = new Vector2(0.95f, 0.5f);
            rrt.sizeDelta = new Vector2(0, 30); rrt.anchoredPosition = new Vector2(0, 0);

            // Action button
            if (done)
            {
                var btn = UIComponentFactory.CreateSecondaryButton(rt, "Action", "已完", () => { });
                var brt = btn.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
                brt.sizeDelta = new Vector2(80, 36); brt.anchoredPosition = new Vector2(-10, 0);
            }
            else
            {
                var btn = UIComponentFactory.CreatePrimaryButton(rt, "Action", "前往", () =>
                {
                    _rewardPreview.text = $"奖励预览: {reward}";
                });
                var brt = btn.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
                brt.sizeDelta = new Vector2(80, 36); brt.anchoredPosition = new Vector2(-10, 0);
            }
        }

        private void BuildRewardPreview(RectTransform root)
        {
            var bar = new GameObject("RewardBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = Vector2.right;
            rt.sizeDelta = new Vector2(0, 50); rt.anchoredPosition = new Vector2(0, 25);
            bar.GetComponent<Image>().color = ThemeColors.BgCard;

            _rewardPreview = UIComponentFactory.CreateText(rt, "RewardText", "奖励预览: 点击任务查看奖励",
                ThemeColors.FontSmall, ThemeColors.Gold);
            _rewardPreview.alignment = TextAnchor.MiddleCenter;
            var trt = _rewardPreview.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }
    }
}