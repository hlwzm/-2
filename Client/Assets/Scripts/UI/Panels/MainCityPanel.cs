using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 原神风格主HUD — 极简顶栏 + 悬浮菜单按钮
    /// </summary>
    public class MainCityPanel : BasePanel
    {
        private Text _nameText, _levelText;
        private Text _goldText, _tongbaoText, _staminaText;
        private GameObject _menuOverlay;
        private bool _menuOpen;

        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;
            BuildTopBar(root);
            BuildMenuOverlay(root);
            BuildFloatingMenuBtn(root);
            RefreshPlayerData();
        }

        void BuildTopBar(RectTransform root)
        {
            var bar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.047f, 0.039f, 0.031f, 0.6f);

            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(rt, false);
            var art = avatar.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(0, 1);
            art.sizeDelta = new Vector2(36, 36); art.anchoredPosition = new Vector2(24, -22);
            avatar.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.6f);

            _nameText = UIComponentFactory.CreateText(rt, "Name", "旅行者", 16, Color.white, TextAnchor.MiddleLeft);
            _nameText.fontStyle = FontStyle.Bold;
            var nrt = _nameText.rectTransform;
            nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(0, 1);
            nrt.sizeDelta = new Vector2(100, 20); nrt.anchoredPosition = new Vector2(70, -16);

            _levelText = UIComponentFactory.CreateText(rt, "Level", "Lv.1", 12, new Color(0.91f, 0.77f, 0.31f), TextAnchor.MiddleLeft);
            var lrt = _levelText.rectTransform;
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(0, 1);
            lrt.sizeDelta = new Vector2(50, 16); lrt.anchoredPosition = new Vector2(70, -34);

            _goldText = UIComponentFactory.CreateText(rt, "Gold", "", 14, ThemeColors.Gold, TextAnchor.MiddleRight);
            var grt = _goldText.rectTransform;
            grt.anchorMin = new Vector2(1, 1); grt.anchorMax = new Vector2(1, 1);
            grt.sizeDelta = new Vector2(120, 20); grt.anchoredPosition = new Vector2(-250, -16);

            _tongbaoText = UIComponentFactory.CreateText(rt, "Tongbao", "", 14, ThemeColors.Tongbao, TextAnchor.MiddleRight);
            var trt = _tongbaoText.rectTransform;
            trt.anchorMin = new Vector2(1, 1); trt.anchorMax = new Vector2(1, 1);
            trt.sizeDelta = new Vector2(120, 20); trt.anchoredPosition = new Vector2(-120, -16);

            _staminaText = UIComponentFactory.CreateText(rt, "Stamina", "", 14, ThemeColors.Stamina, TextAnchor.MiddleRight);
            var srt = _staminaText.rectTransform;
            srt.anchorMin = new Vector2(1, 1); srt.anchorMax = new Vector2(1, 1);
            srt.sizeDelta = new Vector2(100, 18); srt.anchoredPosition = new Vector2(-250, -38);
        }

        void BuildFloatingMenuBtn(RectTransform root)
        {
            var btnGo = new GameObject("MenuBtn", typeof(RectTransform), typeof(Image));
            btnGo.transform.SetParent(root, false);
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1, 0); brt.anchorMax = new Vector2(1, 0);
            brt.sizeDelta = new Vector2(50, 50); brt.anchoredPosition = new Vector2(-30, 40);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.54f, 0.42f, 0.16f, 0.8f);
            var txt = btnGo.AddComponent<Text>();
            txt.text = "☰"; txt.font = UIComponentFactory.Font;
            txt.fontSize = 28; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(ToggleMenu);
        }

        void BuildMenuOverlay(RectTransform root)
        {
            _menuOverlay = new GameObject("MenuOverlay", typeof(RectTransform), typeof(Image));
            _menuOverlay.transform.SetParent(root, false);
            var mrt = _menuOverlay.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.sizeDelta = Vector2.zero;
            _menuOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            _menuOverlay.SetActive(false);

            var container = new GameObject("Container", typeof(RectTransform));
            container.transform.SetParent(_menuOverlay.transform, false);
            var crt = container.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(500, 440); crt.anchoredPosition = Vector2.zero;

            var title = UIComponentFactory.CreateText(crt, "Title", "江 湖 选 单", 28, new Color(0.91f, 0.77f, 0.31f));
            title.fontStyle = FontStyle.Bold;
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 1); trt.anchorMax = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(300, 40); trt.anchoredPosition = new Vector2(0, -20);

            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(crt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 1); lrt.anchoredPosition = new Vector2(0, -60);
            line.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.4f);

            string[] names = { "英雄", "背包", "任务", "好友", "同盟", "商城", "交易行", "聊天", "副本", "竞技" };
            string[] icons = { "⚔", "🎒", "📜", "👥", "🏛", "💰", "🏪", "💬", "🏰", "⚡" };
            System.Action[] actions = {
                () => OpenPanel<HeroListPanel>(),
                () => OpenPanel<BagPanel>(),
                () => OpenPanel<QuestPanel>(),
                () => OpenPanel<FriendPanel>(),
                () => OpenPanel<GuildPanel>(),
                () => OpenPanel<ShopPanel>(),
                () => OpenPanel<TradePanel>(),
                () => OpenPanel<ChatPanel>(),
                () => { _menuOverlay.SetActive(false); SceneManager.Instance.LoadScene(GameScene.DungeonSelect); },
                () => { _menuOverlay.SetActive(false); SceneManager.Instance.LoadScene(GameScene.PVP); },
            };

            int cols = 3; float cellW = 140, cellH = 80;
            float totalW = cols * cellW; float startX = -totalW / 2 + cellW / 2;

            for (int i = 0; i < names.Length; i++)
            {
                int row = i / cols, col = i % cols;
                float x = startX + col * cellW, y = -80 - row * (cellH + 8);

                var item = new GameObject("Item_" + names[i], typeof(RectTransform), typeof(Image));
                item.transform.SetParent(crt, false);
                var iRt = item.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0.5f, 1); iRt.anchorMax = new Vector2(0.5f, 1);
                iRt.sizeDelta = new Vector2(cellW - 8, cellH); iRt.anchoredPosition = new Vector2(x, y);
                item.GetComponent<Image>().color = new Color(0.10f, 0.09f, 0.07f, 0.9f);
                var btn = item.AddComponent<Button>();
                btn.targetGraphic = item.GetComponent<Image>();
                int idx = i;
                btn.onClick.AddListener(() => actions[idx]());

                var icon = UIComponentFactory.CreateText(iRt, "Icon", icons[i], 22, ThemeColors.TextBright);
                icon.raycastTarget = false; icon.rectTransform.sizeDelta = new Vector2(30, 30);
                icon.rectTransform.anchorMin = new Vector2(0.5f, 0); icon.rectTransform.anchorMax = new Vector2(0.5f, 0);
                icon.rectTransform.anchoredPosition = new Vector2(0, 24);

                var nm = UIComponentFactory.CreateText(iRt, "Name", names[i], 14, ThemeColors.TextNormal);
                nm.raycastTarget = false; nm.rectTransform.sizeDelta = new Vector2(100, 20);
                nm.rectTransform.anchorMin = new Vector2(0.5f, 0); nm.rectTransform.anchorMax = new Vector2(0.5f, 0);
                nm.rectTransform.anchoredPosition = new Vector2(0, 4);
            }

            var closeBtn = UIComponentFactory.CreateIconButton(crt, "CloseBtn", "✕", 36, () => _menuOverlay.SetActive(false));
            var clRt = closeBtn.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(1, 1); clRt.anchorMax = new Vector2(1, 1);
            clRt.sizeDelta = new Vector2(40, 40); clRt.anchoredPosition = new Vector2(-10, -10);
        }

        void ToggleMenu() { _menuOpen = !_menuOpen; _menuOverlay.SetActive(_menuOpen); }
        void OpenPanel<T>() where T : BasePanel { _menuOverlay.SetActive(false); UIManager.Instance.Hide<MainCityPanel>(); UIManager.Instance.Show<T>(); }

        void RefreshPlayerData()
        {
            var p = GameManager.Instance.Player;
            _nameText.text = string.IsNullOrEmpty(p.Name) ? "旅行者" : p.Name;
            _levelText.text = "Lv." + p.Level;
            _goldText.text = string.Format("💰 {0:N0}", p.Gold);
            _tongbaoText.text = string.Format("💎 {0:N0}", p.Tongbao);
            _staminaText.text = "⚡ 120/120";
        }

        public override void Refresh() { base.Refresh(); RefreshPlayerData(); }
    }
}
