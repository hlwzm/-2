using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class DungeonSelectPanel : BasePanel
    {
        // ---- 副本数据 ----
        private readonly string[] _names = { "风雨稻香村", "天子峰", "日轮山城", "荻花宫" };
        private readonly string[] _levels = { "Lv.20-30", "Lv.35-45", "Lv.50-60", "Lv.65-75" };
        private readonly string[] _bosses =
        {
            "董龙 → 汪莽 → 肖人德 → 秦颐岩",
            "影煞 → 罗宇 → 方鹤影 → 萧沙",
            "源明雅 → 阿坊古 → 柳生雪 → 八岐大蛇",
            "牡丹 → 大蛇 → 沙利亚 → 阿萨辛"
        };
        private readonly string[] _sizes = { "4人", "5人", "5-8人", "8人" };
        private readonly string[] _timeLimits =
        {
            "前3Boss ≤ 8分钟",
            "前3Boss ≤ 10分钟",
            "前3Boss ≤ 12分钟",
            "前3Boss ≤ 15分钟"
        };
        private readonly int[] _stars = { 3, 4, 4, 5 }; // 难度星级

        protected override void Awake()
        {
            base.Awake();
            BuildBackground();
            BuildTitle();
            BuildCards();
            BuildBackButton();
        }

        private void BuildBackground()
        {
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0.04f, 0.04f, 0.1f));
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }

        private void BuildTitle()
        {
            var title = CreateText(transform as RectTransform, "Title", "副 本 选 择", 40);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.85f, 0.3f);
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(400, 60);
        }

        private void BuildCards()
        {
            float[] cardX = { -430f, 430f };
            float[] cardY = { 140f, -220f };

            for (int i = 0; i < 4; i++)
            {
                int row = i / 2;
                int col = i % 2;
                BuildCard(i, cardX[col], cardY[row]);
            }
        }

        private void BuildCard(int index, float x, float y)
        {
            var card = new GameObject("Card" + index, typeof(RectTransform)).GetComponent<RectTransform>();
            card.SetParent(transform, false);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(420, 310);
            card.anchoredPosition = new Vector2(x, y);

            // 卡片背景
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.22f, 0.95f);

            // 边框高亮
            var border = new GameObject("Border", typeof(RectTransform)).GetComponent<RectTransform>();
            border.SetParent(card, false);
            border.anchorMin = Vector2.zero;
            border.anchorMax = Vector2.one;
            border.sizeDelta = new Vector2(-4, -4);
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.color = new Color(0.3f, 0.3f, 0.5f, 0.6f);

            // ---- 副本名称 ----
            var nameText = CreateText(card, "Name", _names[index], 26);
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = new Color(1f, 0.9f, 0.4f);
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.rectTransform.anchorMin = new Vector2(0, 1);
            nameText.rectTransform.anchorMax = new Vector2(1, 1);
            nameText.rectTransform.sizeDelta = new Vector2(0, 44);
            nameText.rectTransform.anchoredPosition = new Vector2(0, -22);

            // ---- 分隔线 ----
            var divider = new GameObject("Divider", typeof(RectTransform)).GetComponent<RectTransform>();
            divider.SetParent(card, false);
            divider.anchorMin = new Vector2(0.05f, 0.86f);
            divider.anchorMax = new Vector2(0.95f, 0.86f);
            divider.sizeDelta = new Vector2(0, 2);
            var divImg = divider.gameObject.AddComponent<Image>();
            divImg.color = new Color(0.4f, 0.35f, 0.2f, 0.6f);

            // ---- 推荐等级 ----
            var levelText = CreateText(card, "Level", "推荐等级: " + _levels[index], 18);
            levelText.alignment = TextAnchor.MiddleLeft;
            levelText.color = new Color(0.7f, 1f, 0.7f);
            levelText.rectTransform.anchorMin = new Vector2(0, 1);
            levelText.rectTransform.anchorMax = new Vector2(1, 1);
            levelText.rectTransform.sizeDelta = new Vector2(-20, 28);
            levelText.rectTransform.anchoredPosition = new Vector2(10, -60);

            // ---- Boss列表 ----
            var bossText = CreateText(card, "Boss", "Boss: " + _bosses[index], 16);
            bossText.alignment = TextAnchor.MiddleLeft;
            bossText.color = new Color(1f, 0.7f, 0.5f);
            bossText.rectTransform.anchorMin = new Vector2(0, 1);
            bossText.rectTransform.anchorMax = new Vector2(1, 1);
            bossText.rectTransform.sizeDelta = new Vector2(-20, 28);
            bossText.rectTransform.anchoredPosition = new Vector2(10, -92);

            // ---- 组队人数 + 限时 ----
            var infoText = CreateText(card, "Info", "组队: " + _sizes[index] + "  |  限时: " + _timeLimits[index], 16);
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.color = new Color(0.65f, 0.8f, 1f);
            infoText.rectTransform.anchorMin = new Vector2(0, 1);
            infoText.rectTransform.anchorMax = new Vector2(1, 1);
            infoText.rectTransform.sizeDelta = new Vector2(-20, 28);
            infoText.rectTransform.anchoredPosition = new Vector2(10, -124);

            // ---- 难度星级 ----
            var starStr = "";
            for (int s = 0; s < 5; s++) starStr += (s < _stars[index]) ? "★" : "☆";
            var starText = CreateText(card, "Stars", "难度: " + starStr, 20);
            starText.alignment = TextAnchor.MiddleLeft;
            starText.color = new Color(1f, 0.85f, 0.2f);
            starText.rectTransform.anchorMin = new Vector2(0, 1);
            starText.rectTransform.anchorMax = new Vector2(1, 1);
            starText.rectTransform.sizeDelta = new Vector2(-20, 30);
            starText.rectTransform.anchoredPosition = new Vector2(10, -158);

            // ---- 进入按钮 ----
            var idx = index;
            var enterBtn = CreateButton(card, "EnterBtn", "进 入", () => OnEnterDungeon(idx));
            var btnRt = enterBtn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0);
            btnRt.anchorMax = new Vector2(0.5f, 0);
            btnRt.sizeDelta = new Vector2(140, 44);
            btnRt.anchoredPosition = new Vector2(0, 18);

            // 美化按钮颜色
            var btnImage = enterBtn.GetComponent<Image>();
            btnImage.color = new Color(0.55f, 0.3f, 0.1f, 0.9f);

            var btnText = enterBtn.GetComponentInChildren<Text>();
            btnText.fontSize = 22;
            btnText.color = Color.white;

            // 按钮悬停交互提示文本
            var hoverHint = new GameObject("HoverHint", typeof(RectTransform));
            hoverHint.transform.SetParent(card, false);
        }

        private void OnEnterDungeon(int index)
        {
            Debug.Log($"[DungeonSelect] 玩家选择进入副本: {_names[index]} (ID={index + 1})");
            Debug.Log($"[DungeonSelect] 详情: {_levels[index]} | {_sizes[index]} | {_timeLimits[index]} | 难度 {_stars[index]}/5星");
        }

        private void BuildBackButton()
        {
            var backBtn = CreateButton(transform as RectTransform, "BackBtn", "返回主城", () =>
            {
                Debug.Log("[DungeonSelect] 返回主城");
                var sceneMgr = Jx3.Core.Scene.SceneManager.Instance;
                if (sceneMgr != null)
                    sceneMgr.LoadScene(Jx3.Core.Scene.GameScene.MainCity);
            });
            var rt = backBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 40);
            rt.sizeDelta = new Vector2(180, 50);

            var btnImage = backBtn.GetComponent<Image>();
            btnImage.color = new Color(0.3f, 0.25f, 0.4f, 0.85f);

            var btnText = backBtn.GetComponentInChildren<Text>();
            btnText.fontSize = 22;
        }
    }
}
