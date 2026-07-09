using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    public class DungeonSelectPanel : BasePanel
    {
        // ---- 鍓湰鏁版嵁 ----
        private readonly string[] _names = { "椋庨洦绋婚鏉?, "澶╁瓙宄?, "鏃ヨ疆灞卞煄", "鑽昏姳瀹? };
        private readonly string[] _levels = { "Lv.20-30", "Lv.35-45", "Lv.50-60", "Lv.65-75" };
        private readonly string[] _bosses =
        {
            "钁ｉ緳 鈫?姹幗 鈫?鑲栦汉寰?鈫?绉﹂宀?,
            "褰辩厼 鈫?缃楀畤 鈫?鏂归工褰?鈫?钀ф矙",
            "婧愭槑闆?鈫?闃垮潑鍙?鈫?鏌崇敓闆?鈫?鍏矏澶ц泧",
            "鐗′腹 鈫?澶ц泧 鈫?娌欏埄浜?鈫?闃胯惃杈?
        };
        private readonly string[] _sizes = { "4浜?, "5浜?, "5-8浜?, "8浜? };
        private readonly string[] _timeLimits =
        {
            "鍓?Boss 鈮?8鍒嗛挓",
            "鍓?Boss 鈮?10鍒嗛挓",
            "鍓?Boss 鈮?12鍒嗛挓",
            "鍓?Boss 鈮?15鍒嗛挓"
        };
        private readonly int[] _stars = { 3, 4, 4, 5 }; // 闅惧害鏄熺骇

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
            var title = CreateText(transform as RectTransform, "Title", "鍓?鏈?閫?鎷?, 40);
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

            // 鍗＄墖鑳屾櫙
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.14f, 0.12f, 0.95f);

            // 杈规楂樹寒
            var border = new GameObject("Border", typeof(RectTransform)).GetComponent<RectTransform>();
            border.SetParent(card, false);
            border.anchorMin = Vector2.zero;
            border.anchorMax = Vector2.one;
            border.sizeDelta = new Vector2(-4, -4);
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.color = new Color(0.3f, 0.3f, 0.5f, 0.6f);

            // ---- 鍓湰鍚嶇О ----
            var nameText = CreateText(card, "Name", _names[index], 26);
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = new Color(1f, 0.9f, 0.4f);
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.rectTransform.anchorMin = new Vector2(0, 1);
            nameText.rectTransform.anchorMax = new Vector2(1, 1);
            nameText.rectTransform.sizeDelta = new Vector2(0, 44);
            nameText.rectTransform.anchoredPosition = new Vector2(0, -22);

            // ---- 鍒嗛殧绾?----
            var divider = new GameObject("Divider", typeof(RectTransform)).GetComponent<RectTransform>();
            divider.SetParent(card, false);
            divider.anchorMin = new Vector2(0.05f, 0.86f);
            divider.anchorMax = new Vector2(0.95f, 0.86f);
            divider.sizeDelta = new Vector2(0, 2);
            var divImg = divider.gameObject.AddComponent<Image>();
            divImg.color = new Color(0.4f, 0.35f, 0.2f, 0.6f);

            // ---- 鎺ㄨ崘绛夌骇 ----
            var levelText = CreateText(card, "Level", "鎺ㄨ崘绛夌骇: " + _levels[index], 18);
            levelText.alignment = TextAnchor.MiddleLeft;
            levelText.color = new Color(0.7f, 1f, 0.7f);
            levelText.rectTransform.anchorMin = new Vector2(0, 1);
            levelText.rectTransform.anchorMax = new Vector2(1, 1);
            levelText.rectTransform.sizeDelta = new Vector2(-20, 28);
            levelText.rectTransform.anchoredPosition = new Vector2(10, -60);

            // ---- Boss鍒楄〃 ----
            var bossText = CreateText(card, "Boss", "Boss: " + _bosses[index], 16);
            bossText.alignment = TextAnchor.MiddleLeft;
            bossText.color = new Color(1f, 0.7f, 0.5f);
            bossText.rectTransform.anchorMin = new Vector2(0, 1);
            bossText.rectTransform.anchorMax = new Vector2(1, 1);
            bossText.rectTransform.sizeDelta = new Vector2(-20, 28);
            bossText.rectTransform.anchoredPosition = new Vector2(10, -92);

            // ---- 缁勯槦浜烘暟 + 闄愭椂 ----
            var infoText = CreateText(card, "Info", "缁勯槦: " + _sizes[index] + "  |  闄愭椂: " + _timeLimits[index], 16);
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.color = new Color(0.65f, 0.8f, 1f);
            infoText.rectTransform.anchorMin = new Vector2(0, 1);
            infoText.rectTransform.anchorMax = new Vector2(1, 1);
            infoText.rectTransform.sizeDelta = new Vector2(-20, 28);
            infoText.rectTransform.anchoredPosition = new Vector2(10, -124);

            // ---- 闅惧害鏄熺骇 ----
            var starStr = "";
            for (int s = 0; s < 5; s++) starStr += (s < _stars[index]) ? "鈽? : "鈽?;
            var starText = CreateText(card, "Stars", "闅惧害: " + starStr, 20);
            starText.alignment = TextAnchor.MiddleLeft;
            starText.color = new Color(1f, 0.85f, 0.2f);
            starText.rectTransform.anchorMin = new Vector2(0, 1);
            starText.rectTransform.anchorMax = new Vector2(1, 1);
            starText.rectTransform.sizeDelta = new Vector2(-20, 30);
            starText.rectTransform.anchoredPosition = new Vector2(10, -158);

            // ---- 杩涘叆鎸夐挳 ----
            var idx = index;
            var enterBtn = CreateButton(card, "EnterBtn", "杩?鍏?, () => OnEnterDungeon(idx));
            var btnRt = enterBtn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0);
            btnRt.anchorMax = new Vector2(0.5f, 0);
            btnRt.sizeDelta = new Vector2(140, 44);
            btnRt.anchoredPosition = new Vector2(0, 18);

            // 缇庡寲鎸夐挳棰滆壊
            var btnImage = enterBtn.GetComponent<Image>();
            btnImage.color = new Color(0.55f, 0.3f, 0.1f, 0.9f);

            var btnText = enterBtn.GetComponentInChildren<Text>();
            btnText.fontSize = 22;
            btnText.color = Color.white;

            // 鎸夐挳鎮仠浜や簰鎻愮ず鏂囨湰
            var hoverHint = new GameObject("HoverHint", typeof(RectTransform));
            hoverHint.transform.SetParent(card, false);
        }

        private void OnEnterDungeon(int index)
        {
            var gm = GameManager.Instance;
            gm.CurrentDungeonIndex = index;
            gm.CurrentDungeonName = _names[index];
            gm.CurrentDungeonBoss = _bosses[index];
            Debug.Log($"[DungeonSelect] 杩涘叆鍓湰: {_names[index]} | Boss: {_bosses[index]}");
            SceneManager.Instance.LoadScene(GameScene.Battle);
        }

        private void BuildBackButton()
        {
            var backBtn = CreateButton(transform as RectTransform, "BackBtn", "杩斿洖涓诲煄", () =>
            {
                Debug.Log("[DungeonSelect] 杩斿洖涓诲煄");
                var sceneMgr = SceneManager.Instance;
                if (sceneMgr != null)
                    sceneMgr.LoadScene(GameScene.MainCity);
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
