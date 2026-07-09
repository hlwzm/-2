using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;
using Jx3.UI;

namespace Jx3.UI.Panels
{
    public class DungeonSelectPanel : BasePanel
    {
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
        private readonly int[] _stars = { 3, 4, 4, 5 };

        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);

            // Title
            var title = UIComponentFactory.CreateText(root, "Title", "副 本 选 择",
                ThemeColors.FontTitle, ThemeColors.Gold);
            title.fontStyle = FontStyle.Bold;
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 1); trt.anchorMax = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(400, 60); trt.anchoredPosition = new Vector2(0, -60);

            // Cards
            float[] cardX = { -430f, 430f };
            float[] cardY = { 140f, -220f };
            for (int i = 0; i < 4; i++)
                BuildCard(root, i, cardX[i % 2], cardY[i / 2]);

            // Back button
            var backBtn = UIComponentFactory.CreateSecondaryButton(root, "BackBtn", "返回主城", () =>
            {
                SceneManager.Instance?.LoadScene(GameScene.MainCity);
            });
            var brt = backBtn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0); brt.anchorMax = new Vector2(0.5f, 0);
            brt.sizeDelta = new Vector2(180, 50); brt.anchoredPosition = new Vector2(0, 40);
        }

        private void BuildCard(RectTransform parent, int index, float x, float y)
        {
            var card = UIComponentFactory.CreateCard(parent, "Card" + index,
                new Vector2(420, 310), new Vector2(x, y));
            card.GetComponent<Image>().color = new Color(0.16f, 0.14f, 0.12f, 0.95f);

            // Name
            var nameText = UIComponentFactory.CreateText(card, "Name", _names[index],
                26, new Color(1f, 0.85f, 0.3f));
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.rectTransform.anchorMin = new Vector2(0, 1);
            nameText.rectTransform.anchorMax = Vector2.one;
            nameText.rectTransform.sizeDelta = new Vector2(0, 44);
            nameText.rectTransform.anchoredPosition = new Vector2(0, -22);

            // Divider
            var div = UIComponentFactory.CreateDivider(card);
            div.rectTransform.anchorMin = new Vector2(0.05f, 0.86f);
            div.rectTransform.anchorMax = new Vector2(0.95f, 0.86f);
            div.rectTransform.sizeDelta = new Vector2(0, 2);

            PlaceText(card, "Level", "推荐等级: " + _levels[index], 18, new Color(0.7f, 1f, 0.7f), -60);
            PlaceText(card, "Boss", "Boss: " + _bosses[index], 16, new Color(1f, 0.7f, 0.5f), -92);
            PlaceText(card, "Info", "组队: " + _sizes[index] + "  |  限时: " + _timeLimits[index], 16, new Color(0.65f, 0.8f, 1f), -124);

            // Stars
            var starStr = ThemeColors.GetQualityStars(_stars[index]);
            var starText = UIComponentFactory.CreateText(card, "Stars", "难度: " + starStr,
                20, new Color(1f, 0.85f, 0.2f));
            starText.alignment = TextAnchor.MiddleLeft;
            starText.rectTransform.anchorMin = new Vector2(0, 1);
            starText.rectTransform.anchorMax = Vector2.one;
            starText.rectTransform.sizeDelta = new Vector2(-20, 30);
            starText.rectTransform.anchoredPosition = new Vector2(10, -158);

            // Enter button
            var idx = index;
            var enterBtn = UIComponentFactory.CreatePrimaryButton(card, "EnterBtn", "进 入",
                () => OnEnterDungeon(idx));
            var btnRt = enterBtn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0);
            btnRt.anchorMax = new Vector2(0.5f, 0);
            btnRt.sizeDelta = new Vector2(140, 44);
            btnRt.anchoredPosition = new Vector2(0, 18);
        }

        private void OnEnterDungeon(int index)
        {
            var gm = GameManager.Instance;
            gm.CurrentDungeonIndex = index;
            gm.CurrentDungeonName = _names[index];
            gm.CurrentDungeonBoss = _bosses[index];
            Debug.Log($"[DungeonSelect] 进入副本: {_names[index]} | Boss: {_bosses[index]}");
            SceneManager.Instance.LoadScene(GameScene.Battle);
        }

        private static void PlaceText(RectTransform parent, string name, string text, int size, Color color, float y)
        {
            var t = UIComponentFactory.CreateText(parent, name, text, size, color);
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.anchorMin = new Vector2(0, 1);
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.sizeDelta = new Vector2(-20, 28);
            t.rectTransform.anchoredPosition = new Vector2(10, y);
        }
    }
}