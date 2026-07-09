using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 侧边栏 — 场景常驻，不随面板切换消失
    /// 左侧竖排图标按钮，高亮当前激活面板
    /// </summary>
    public class SidebarPanel : BasePanel
    {
        [Serializable]
        public class SidebarEntry
        {
            public string name;
            public string icon;        // 文字图标或精灵图键名
            public Type panelType;     // 对应的面板类型
            public Color accentColor = Color.white;
        }

        public List<SidebarEntry> entries = new()
        {
            new() { name = "英雄",   icon = "⚔", panelType = typeof(HeroListPanel), accentColor = new Color(0.91f, 0.77f, 0.31f) },
            new() { name = "背包",   icon = "🎒", panelType = typeof(BagPanel),     accentColor = new Color(0.54f, 0.42f, 0.16f) },
            new() { name = "任务",   icon = "📜", panelType = typeof(QuestPanel),   accentColor = new Color(0.4f, 0.7f, 0.4f) },
            new() { name = "好友",   icon = "👥", panelType = typeof(FriendPanel),  accentColor = new Color(0.4f, 0.6f, 0.9f) },
            new() { name = "同盟",   icon = "🏛", panelType = typeof(GuildPanel),   accentColor = new Color(0.8f, 0.4f, 0.6f) },
            new() { name = "商城",   icon = "💰", panelType = typeof(ShopPanel),    accentColor = new Color(0.91f, 0.77f, 0.31f) },
            new() { name = "交易行", icon = "🏪", panelType = typeof(TradePanel),   accentColor = new Color(0.4f, 0.8f, 0.7f) },
            new() { name = "聊天",   icon = "💬", panelType = typeof(ChatPanel),    accentColor = new Color(0.6f, 0.6f, 0.8f) },
        };

        public float sidebarWidth = 80f;
        public float itemHeight = 72f;

        private Type _activePanelType;
        private readonly List<GameObject> _itemBgs = new();

        protected override void Awake()
        {
            base.Awake();
            BuildSidebar();
        }

        void BuildSidebar()
        {
            var root = transform as RectTransform;
            // 半透明背景条
            var bg = new GameObject("SidebarBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = new Vector2(0, 1);
            bgRt.sizeDelta = new Vector2(sidebarWidth, 0);
            bg.GetComponent<Image>().color = new Color(0.047f, 0.039f, 0.031f, 0.85f);

            // 右侧装饰线
            var line = new GameObject("DecoLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(bgRt, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(1, 0);
            lineRt.anchorMax = Vector2.one;
            lineRt.sizeDelta = new Vector2(1, 0);
            line.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.5f);

            // 标题
            var title = UIComponentFactory.CreateText(bgRt, "Title", "江湖",
                16, new Color(0.91f, 0.77f, 0.31f, 0.8f));
            title.fontStyle = FontStyle.Bold;
            var tRt = title.rectTransform;
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.sizeDelta = new Vector2(0, 40);
            tRt.anchoredPosition = new Vector2(0, -20);

            // 返回主城按钮（顶部）
            var homeBtn = UIComponentFactory.CreateIconButton(bgRt, "HomeBtn", "🏠", 40, () =>
            {
                UINavigator.Instance?.GoHome();
            });
            var hRt = homeBtn.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1);
            hRt.anchorMax = new Vector2(0.5f, 1);
            hRt.sizeDelta = new Vector2(44, 44);
            hRt.anchoredPosition = new Vector2(0, -60);

            // 分隔线
            var sep = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(bgRt, false);
            var sRt = sep.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 1);
            sRt.anchorMax = Vector2.one;
            sRt.sizeDelta = new Vector2(0, 1);
            sRt.anchoredPosition = new Vector2(0, -85);
            sep.GetComponent<Image>().color = new Color(0.54f, 0.42f, 0.16f, 0.3f);

            // 菜单项
            float startY = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                int idx = i;

                var item = new GameObject($"Item_{entry.name}", typeof(RectTransform), typeof(Image));
                item.transform.SetParent(bgRt, false);
                var iRt = item.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0.5f, 0.5f);
                iRt.anchorMax = new Vector2(0.5f, 0.5f);
                iRt.sizeDelta = new Vector2(sidebarWidth - 8, itemHeight);
                iRt.anchoredPosition = new Vector2(0, startY - (idx + 1) * (itemHeight + 4) + 90);

                var img = item.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0);

                var btn = item.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => OnItemClick(idx));

                // Icon
                var icon = UIComponentFactory.CreateText(iRt, "Icon", entry.icon,
                    24, entry.accentColor);
                var cRt = icon.rectTransform;
                cRt.anchorMin = new Vector2(0.5f, 0.5f);
                cRt.anchorMax = new Vector2(0.5f, 0.5f);
                cRt.sizeDelta = new Vector2(30, 30);
                cRt.anchoredPosition = new Vector2(0, 8);

                // Name
                var name = UIComponentFactory.CreateText(iRt, "Name", entry.name,
                    12, new Color(0.77f, 0.72f, 0.63f));
                name.raycastTarget = false;
                var nRt = name.rectTransform;
                nRt.anchorMin = new Vector2(0.5f, 0);
                nRt.anchorMax = new Vector2(0.5f, 0);
                nRt.sizeDelta = new Vector2(sidebarWidth, 18);
                nRt.anchoredPosition = new Vector2(0, 6);

                _itemBgs.Add(item);
            }
        }

        void OnItemClick(int index)
        {
            if (index < 0 || index >= entries.Count) return;
            var entry = entries[index];
            UINavigator.Instance?.OpenByType(entry.panelType);
        }

        public void SetActivePanel(Type panelType)
        {
            _activePanelType = panelType;
            for (int i = 0; i < _itemBgs.Count; i++)
            {
                if (_itemBgs[i] == null) continue;
                var img = _itemBgs[i].GetComponent<Image>();
                bool isActive = i < entries.Count && entries[i].panelType == panelType;
                img.color = isActive
                    ? new Color(0.54f, 0.42f, 0.16f, 0.3f)
                    : new Color(0, 0, 0, 0);
            }
        }
    }
}