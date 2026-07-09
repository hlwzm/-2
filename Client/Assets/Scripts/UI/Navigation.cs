using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Jx3.Core;
using Jx3.Core.Scene;
using Jx3.UI.Panels;

namespace Jx3.UI
{
    /// <summary>
    /// 导航系统 — 剑网三指尖江湖风格的底部Tab导航
    /// 底部常驻Tab(5个主功能)、顶部常驻信息栏、面板滑动切换
    /// </summary>
    public class Navigation : MonoBehaviour
    {
        public static Navigation Instance { get; private set; }

        [Header("层级")]
        public RectTransform topBarLayer;     // 顶部信息栏
        public RectTransform contentLayer;    // 内容面板区
        public RectTransform bottomTabLayer;  // 底部Tab导航
        public RectTransform overlayLayer;    // 弹窗/覆盖层

        // 当前显示的面板
        private BasePanel _currentPanel;
        private BasePanel _overlayPanel;
        private readonly Stack<BasePanel> _panelStack = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            BuildTopBar();
            BuildBottomTabs();
            RefreshPlayerData();
        }

        void Update()
        {
            // ESC返回
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_overlayPanel != null)
                    CloseOverlay();
                else if (_panelStack.Count > 1)
                    GoBack();
            }
        }

        // ═══════════════════════════════════════════════
        //  顶部信息栏
        // ═══════════════════════════════════════════════

        private Text _nameText, _levelText;
        private Text _goldText, _tongbaoText, _staminaText;

        void BuildTopBar()
        {
            var bar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(topBarLayer, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);

            // 头像
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(rt, false);
            var art = avatar.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 0.5f); art.anchorMax = new Vector2(0, 0.5f);
            art.sizeDelta = new Vector2(36, 36); art.anchoredPosition = new Vector2(24, 0);
            avatar.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.5f);

            // 名称 + 等级
            _nameText = UIComponentFactory.CreateText(rt, "Name", "未命名", 16, Color.white, TextAnchor.MiddleLeft);
            _nameText.fontStyle = FontStyle.Bold;
            var nrt = _nameText.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.sizeDelta = new Vector2(100, 22); nrt.anchoredPosition = new Vector2(66, 8);

            _levelText = UIComponentFactory.CreateText(rt, "Level", "Lv.1", 12, ThemeColors.Gold, TextAnchor.MiddleLeft);
            var lrt = _levelText.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(0, 0.5f);
            lrt.sizeDelta = new Vector2(50, 18); lrt.anchoredPosition = new Vector2(66, -10);

            // 货币
            _goldText = UIComponentFactory.CreateText(rt, "Gold", "", 14, ThemeColors.Gold, TextAnchor.MiddleLeft);
            var grt = _goldText.rectTransform;
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.sizeDelta = new Vector2(120, 22); grt.anchoredPosition = new Vector2(220, 8);

            _tongbaoText = UIComponentFactory.CreateText(rt, "Tongbao", "", 14, ThemeColors.Tongbao, TextAnchor.MiddleLeft);
            var trt = _tongbaoText.rectTransform;
            trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(0, 0.5f);
            trt.sizeDelta = new Vector2(120, 22); trt.anchoredPosition = new Vector2(360, 8);

            _staminaText = UIComponentFactory.CreateText(rt, "Stamina", "", 14, ThemeColors.Stamina, TextAnchor.MiddleLeft);
            var srt = _staminaText.rectTransform;
            srt.anchorMin = new Vector2(0, 0.5f); srt.anchorMax = new Vector2(0, 0.5f);
            srt.sizeDelta = new Vector2(80, 22); srt.anchoredPosition = new Vector2(500, 8);
        }

        void RefreshPlayerData()
        {
            var p = GameManager.Instance.Player;
            _nameText.text = string.IsNullOrEmpty(p.Name) ? "未命名" : p.Name;
            _levelText.text = "Lv." + p.Level;
            _goldText.text = $"💰 {p.Gold:N0}";
            _tongbaoText.text = $"💎 {p.Tongbao:N0}";
            _staminaText.text = "⚡ 120/120";
        }

        // ═══════════════════════════════════════════════
        //  底部导航Tab
        // ═══════════════════════════════════════════════

        [Serializable]
        public class TabDefinition
        {
            public string name;
            public string icon;
            public Type panelType;      // 点击打开的panel类型
            public bool openScene;      // 是否切场景(GameScene)
            public GameScene sceneTarget;
        }

        public List<TabDefinition> tabs = new()
        {
            new() { name = "江湖",    icon = "🏠", panelType = null },                       // 主城/默认
            new() { name = "侠客",    icon = "⚔", panelType = typeof(HeroListPanel) },
            new() { name = "背包",    icon = "🎒", panelType = typeof(BagPanel) },
            new() { name = "任务",    icon = "📜", panelType = typeof(QuestPanel) },
            new() { name = "社交",    icon = "👥", panelType = typeof(SocialPanel) },         // 整合好友+同盟
        };

        public List<TabDefinition> extraTabs = new()
        {
            new() { name = "副本",    icon = "🏰", openScene = true, sceneTarget = GameScene.DungeonSelect },
            new() { name = "竞技",    icon = "⚡", openScene = true, sceneTarget = GameScene.PVP },
            new() { name = "交易行",  icon = "🏪", panelType = typeof(TradePanel) },
            new() { name = "商城",    icon = "💰", panelType = typeof(ShopPanel) },
            new() { name = "聊天",    icon = "💬", panelType = typeof(ChatPanel) },
            new() { name = "同盟",    icon = "🏛", panelType = typeof(GuildPanel) },
        };

        private int _currentTabIndex = 0;
        private readonly List<GameObject> _tabBgs = new();

        void BuildBottomTabs()
        {
            var bar = new GameObject("BottomTabs", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(bottomTabLayer, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            // 顶部装饰线
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(rt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 1); lrt.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.3f);

            float tabW = 160f;
            float startX = -(tabs.Count * tabW) / 2f + tabW / 2f;

            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                int idx = i;

                var go = new GameObject($"Tab_{tab.name}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(rt, false);
                var grt = go.GetComponent<RectTransform>();
                grt.anchorMin = new Vector2(0.5f, 0.5f); grt.anchorMax = new Vector2(0.5f, 0.5f);
                grt.sizeDelta = new Vector2(tabW, 60); grt.anchoredPosition = new Vector2(startX + idx * tabW, 10);

                var img = go.GetComponent<Image>();
                img.color = i == 0 ? new Color(0.54f, 0.42f, 0.16f, 0.2f) : new Color(0, 0, 0, 0);

                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => OnTabClick(idx));

                // 图标
                var icon = UIComponentFactory.CreateText(grt, "Icon", tab.icon, 22, Color.white);
                icon.raycastTarget = false;
                var irt = icon.rectTransform;
                irt.anchorMin = new Vector2(0.5f, 0.5f); irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.sizeDelta = new Vector2(28, 28); irt.anchoredPosition = new Vector2(0, 8);

                // 名称
                var name = UIComponentFactory.CreateText(grt, "Name", tab.name, 11, new Color(0.77f, 0.72f, 0.63f));
                name.raycastTarget = false;
                name.rectTransform.anchorMin = new Vector2(0.5f, 0);
                name.rectTransform.anchorMax = new Vector2(0.5f, 0);
                name.rectTransform.sizeDelta = new Vector2(tabW, 18);
                name.rectTransform.anchoredPosition = new Vector2(0, 4);

                _tabBgs.Add(go);
            }

            // 展开按钮(更多功能)
            var more = new GameObject("Tab_更多", typeof(RectTransform), typeof(Image));
            more.transform.SetParent(rt, false);
            var mrt = more.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(1, 0.5f); mrt.anchorMax = new Vector2(1, 0.5f);
            mrt.sizeDelta = new Vector2(60, 60); mrt.anchoredPosition = new Vector2(-10, 10);
            var mImg = more.GetComponent<Image>();
            mImg.color = new Color(0, 0, 0, 0);
            var mBtn = more.AddComponent<Button>();
            mBtn.targetGraphic = mImg;
            mBtn.onClick.AddListener(ShowExtraTabs);

            var mIcon = UIComponentFactory.CreateText(mrt, "Icon", "···", 22, new Color(0.77f, 0.72f, 0.63f));
            mIcon.raycastTarget = false;
            mIcon.rectTransform.sizeDelta = new Vector2(40, 40);
        }

        void OnTabClick(int index)
        {
            if (index < 0 || index >= tabs.Count) return;
            var tab = tabs[index];

            // 更新Tab高亮
            for (int i = 0; i < _tabBgs.Count; i++)
            {
                if (_tabBgs[i] == null) continue;
                _tabBgs[i].GetComponent<Image>().color = i == index
                    ? new Color(0.54f, 0.42f, 0.16f, 0.2f)
                    : new Color(0, 0, 0, 0);
            }
            _currentTabIndex = index;

            // 清面板栈
            CloseCurrentPanel();

            if (tab.panelType != null)
                OpenPanel(tab.panelType);
            else
                GoHome();
        }

        // ═══════════════════════════════════════════════
        //  面板管理
        // ═══════════════════════════════════════════════

        public void OpenPanel(Type panelType)
        {
            // 清理当前面板(保留栈底)
            if (_currentPanel != null)
            {
                Destroy(_currentPanel.gameObject);
                _currentPanel = null;
                _panelStack.Clear();
            }

            var method = typeof(UIManager).GetMethod("Show")?.MakeGenericMethod(panelType);
            if (method != null)
            {
                var result = method.Invoke(UIManager.Instance, null);
                if (result is BasePanel panel)
                {
                    panel.transform.SetParent(contentLayer, false);
                    _currentPanel = panel;
                    _panelStack.Push(panel);
                }
            }
        }

        public void GoBack()
        {
            if (_panelStack.Count <= 1) return;
            var top = _panelStack.Pop();
            Destroy(top.gameObject);
            _currentPanel = _panelStack.Count > 0 ? _panelStack.Peek() : null;
        }

        public void GoHome()
        {
            CloseCurrentPanel();
        }

        void CloseCurrentPanel()
        {
            if (_currentPanel != null)
            {
                Destroy(_currentPanel.gameObject);
                _currentPanel = null;
            }
            _panelStack.Clear();
        }

        public void ShowOverlay(BasePanel panel)
        {
            if (_overlayPanel != null) CloseOverlay();
            panel.transform.SetParent(overlayLayer, false);
            _overlayPanel = panel;
        }

        public void CloseOverlay()
        {
            if (_overlayPanel != null)
            {
                Destroy(_overlayPanel.gameObject);
                _overlayPanel = null;
            }
        }

        // 展开额外功能菜单
        void ShowExtraTabs()
        {
            Debug.Log("[Nav] 展开更多功能");
            // TODO: 弹出一个半透明覆盖层显示 extraTabs
        }
    }
}