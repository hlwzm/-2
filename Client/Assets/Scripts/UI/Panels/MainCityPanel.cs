using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 主HUD — 屏幕顶部信息栏 + 底部快捷操作
    /// 随场景常驻，不随面板切换消失
    /// </summary>
    public class MainCityPanel : BasePanel
    {
        private Text _nameText, _levelText;
        private Text _goldText, _tongbaoText, _staminaText;

        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            BuildTopBar(root);
            BuildBottomBar(root);

            RefreshPlayerData();
        }

        void BuildTopBar(RectTransform root)
        {
            var bar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(0, 56); rt.anchoredPosition = new Vector2(80, -28);
            bar.GetComponent<Image>().color = new Color(0.047f, 0.039f, 0.031f, 0.85f);

            // 底部装饰线
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(rt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 1); lrt.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.4f);

            // 头像
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(rt, false);
            var art = avatar.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 0.5f); art.anchorMax = new Vector2(0, 0.5f);
            art.sizeDelta = new Vector2(40, 40); art.anchoredPosition = new Vector2(24, 0);
            avatar.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.5f);

            // 玩家名称
            _nameText = UIComponentFactory.CreateText(rt, "Name", "未命名",
                18, ThemeColors.TextBright, TextAnchor.MiddleLeft);
            _nameText.fontStyle = FontStyle.Bold;
            var nrt = _nameText.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.sizeDelta = new Vector2(120, 24); nrt.anchoredPosition = new Vector2(80, 10);

            // 等级
            _levelText = UIComponentFactory.CreateText(rt, "Level", "Lv.1",
                14, ThemeColors.Gold, TextAnchor.MiddleLeft);
            var lrt2 = _levelText.rectTransform;
            lrt2.anchorMin = new Vector2(0, 0.5f); lrt2.anchorMax = new Vector2(0, 0.5f);
            lrt2.sizeDelta = new Vector2(60, 18); lrt2.anchoredPosition = new Vector2(80, -10);

            // 金币
            _goldText = UIComponentFactory.CreateText(rt, "Gold", "",
                16, ThemeColors.Gold, TextAnchor.MiddleLeft);
            var grt = _goldText.rectTransform;
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.sizeDelta = new Vector2(140, 24); grt.anchoredPosition = new Vector2(260, 10);

            // 通宝
            _tongbaoText = UIComponentFactory.CreateText(rt, "Tongbao", "",
                16, ThemeColors.Tongbao, TextAnchor.MiddleLeft);
            var trt = _tongbaoText.rectTransform;
            trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(0, 0.5f);
            trt.sizeDelta = new Vector2(140, 24); trt.anchoredPosition = new Vector2(420, 10);

            // 体力
            _staminaText = UIComponentFactory.CreateText(rt, "Stamina", "",
                16, ThemeColors.Stamina, TextAnchor.MiddleLeft);
            var srt = _staminaText.rectTransform;
            srt.anchorMin = new Vector2(0, 0.5f); srt.anchorMax = new Vector2(0, 0.5f);
            srt.sizeDelta = new Vector2(100, 24); srt.anchoredPosition = new Vector2(580, 10);

            // 菜单按钮（汉堡）
            var menuBtn = UIComponentFactory.CreateIconButton(rt, "MenuBtn", "☰", 32, () =>
            {
                Debug.Log("[HUD] 打开完整菜单");
            });
            var mrt = menuBtn.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(1, 0.5f); mrt.anchorMax = new Vector2(1, 0.5f);
            mrt.sizeDelta = new Vector2(44, 44); mrt.anchoredPosition = new Vector2(-100 + 80, 0);
        }

        void BuildBottomBar(RectTransform root)
        {
            var bar = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(1920, 60);
            rt.anchoredPosition = new Vector2(960, 30);
            bar.GetComponent<Image>().color = new Color(0.047f, 0.039f, 0.031f, 0.85f);

            // 顶部装饰线
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(rt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 1); lrt.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.4f);

            string[] names = { "副本", "竞技", "队伍" };
            string[] icons = { "🏰", "⚡", "📯" };
            System.Action[] actions = {
                () => SceneManager.Instance.LoadScene(GameScene.DungeonSelect),
                () => SceneManager.Instance.LoadScene(GameScene.PVP),
                () => Debug.Log("[HUD] 打开队伍面板"),
            };

            for (int i = 0; i < 3; i++)
            {
                var item = new GameObject($"BottomItem_{i}", typeof(RectTransform), typeof(Image));
                item.transform.SetParent(rt, false);
                var iRt = item.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0.5f, 0.5f);
                iRt.anchorMax = new Vector2(0.5f, 0.5f);
                iRt.sizeDelta = new Vector2(100, 50);
                iRt.anchoredPosition = new Vector2((i - 1) * 200, 0);

                var img = item.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                var btn = item.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(actions[i]);

                var icon = UIComponentFactory.CreateText(iRt, "Icon", icons[i], 18, ThemeColors.TextBright);
                icon.raycastTarget = false;
                var cRt = icon.rectTransform;
                cRt.anchorMin = new Vector2(0.5f, 1);
                cRt.anchorMax = new Vector2(0.5f, 1);
                cRt.sizeDelta = new Vector2(30, 30);
                cRt.anchoredPosition = new Vector2(0, -16);

                var name = UIComponentFactory.CreateText(iRt, "Name", names[i], 12, ThemeColors.TextNormal);
                name.raycastTarget = false;
                var nRt = name.rectTransform;
                nRt.anchorMin = new Vector2(0.5f, 0);
                nRt.anchorMax = new Vector2(0.5f, 0);
                nRt.sizeDelta = new Vector2(60, 18);
                nRt.anchoredPosition = new Vector2(0, 4);
            }
        }

        void RefreshPlayerData()
        {
            var p = GameManager.Instance.Player;
            _nameText.text = string.IsNullOrEmpty(p.Name) ? "未命名" : p.Name;
            _levelText.text = "Lv." + p.Level;
            _goldText.text = $"💰 {p.Gold:N0}";
            _tongbaoText.text = $"💎 {p.Tongbao:N0}";
            _staminaText.text = $"⚡ 120/120";
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshPlayerData();
        }
    }
}