using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 背包面板 - 物品网格 + 分类Tab + 详情弹窗
    /// </summary>
    public class BagPanel : BasePanel
    {
        // ===== 配色 =====
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.28f, 0.8f);
        private static readonly Color ColorTabActive = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorSlotBg = new Color(0.12f, 0.12f, 0.22f, 0.9f);
        private static readonly Color ColorSlotHighlight = new Color(0.2f, 0.18f, 0.35f, 0.9f);
        private static readonly Color ColorGold = new Color(1f, 0.8f, 0.2f);
        private static readonly Color ColorQualityWhite = Color.white;
        private static readonly Color ColorQualityGreen = new Color(0.3f, 0.9f, 0.3f);
        private static readonly Color ColorQualityBlue = new Color(0.3f, 0.6f, 1f);
        private static readonly Color ColorQualityPurple = new Color(0.7f, 0.3f, 1f);
        private static readonly Color ColorDimText = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);

        // ===== 模拟物品数据 =====
        private class ItemData
        {
            public string Name;
            public string Type;
            public int Quality;
            public int Count;
            public string Attributes;
            public string Description;
        }

        private static readonly string[] TabNames = { "全部", "武器", "防具", "饰品", "材料", "任务" };

        private List<ItemData> _allItems = new();
        private List<ItemData> _filteredItems = new();
        private int _currentTab = 0;
        private const int MaxSlots = 40;
        private const int Cols = 6;
        private const int SlotSize = 60;

        private RectTransform _gridRoot;
        private List<GameObject> _slotObjects = new();
        private RectTransform _detailPopup;
        private Text _goldText;
        private Text _usageText;
        private Text _detailName, _detailType, _detailQuality, _detailAttr, _detailDesc;

        protected override void Awake()
        {
            base.Awake();
            InitMockData();
            BuildBackground();
            BuildTopBar();
            BuildTabs();
            BuildGrid();
            BuildBottomBar();
            BuildDetailPopup();
            FilterByTab(0);
        }

        private void InitMockData()
        {
            _allItems = new List<ItemData>
            {
                new ItemData { Name = "灵霄剑",     Type = "武器", Quality = 3, Count = 1, Attributes = "外功攻击+86\n会心+32",   Description = "凌霄阁弟子佩剑，剑身蕴含灵力。" },
                new ItemData { Name = "玄铁重甲",   Type = "防具", Quality = 3, Count = 1, Attributes = "外功防御+120\n气血+500",  Description = "玄铁铸造的重甲，坚不可摧。" },
                new ItemData { Name = "翡翠玉佩",   Type = "饰品", Quality = 2, Count = 1, Attributes = "内功攻击+45\n会心+18",   Description = "温润的翡翠玉佩，蕴含内力。" },
                new ItemData { Name = "紫晶石",     Type = "材料", Quality = 4, Count = 5, Attributes = "强化材料",                Description = "稀有的紫色晶石，可用于装备强化。" },
                new ItemData { Name = "回春丹",     Type = "材料", Quality = 1, Count = 12, Attributes = "使用:回复500气血",       Description = "低级回复丹药，可快速恢复气血。" },
                new ItemData { Name = "追魂令",     Type = "任务", Quality = 1, Count = 1, Attributes = "任务物品",               Description = "追魂阁的信物，用于追踪目标。" },
                new ItemData { Name = "破军刀",     Type = "武器", Quality = 4, Count = 1, Attributes = "外功攻击+142\n破防+55",   Description = "上古名刀，破军之威震慑四野。" },
                new ItemData { Name = "流光袍",     Type = "防具", Quality = 4, Count = 1, Attributes = "内功防御+98\n气血+380\n闪避+22", Description = "流光溢彩的法袍，轻盈如风。" },
            };
        }

        private void BuildBackground()
        {
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0.04f, 0.04f, 0.1f, 0.92f));
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }

        private void BuildTopBar()
        {
            var title = CreateText(transform as RectTransform, "Title", "背 包", 32);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(0, 1);
            titleRt.sizeDelta = new Vector2(100, 40);
            titleRt.anchoredPosition = new Vector2(40, -40);

            var line = new GameObject("TitleLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(transform, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 1);
            lineRt.anchorMax = new Vector2(1, 1);
            lineRt.sizeDelta = new Vector2(0, 2);
            lineRt.anchoredPosition = new Vector2(0, -70);
            line.GetComponent<Image>().color = ColorAccent;

            var goldIcon = CreateText(transform as RectTransform, "GoldIcon", "\U0001f4b0", 22);
            var goldIconRt = (RectTransform)goldIcon.transform;
            goldIconRt.anchorMin = new Vector2(0, 1);
            goldIconRt.anchorMax = new Vector2(0, 1);
            goldIconRt.sizeDelta = new Vector2(30, 30);
            goldIconRt.anchoredPosition = new Vector2(160, -40);

            _goldText = CreateText(transform as RectTransform, "GoldText", "0", 22);
            _goldText.color = ColorGold;
            _goldText.alignment = TextAnchor.MiddleLeft;
            var goldRt = (RectTransform)_goldText.transform;
            goldRt.anchorMin = new Vector2(0, 1);
            goldRt.anchorMax = new Vector2(0, 1);
            goldRt.sizeDelta = new Vector2(200, 30);
            goldRt.anchoredPosition = new Vector2(195, -40);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "\u2715", () => Hide());
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.sizeDelta = new Vector2(50, 50);
            closeRt.anchoredPosition = new Vector2(-40, -35);
            closeBtn.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.4f, 0.8f);
        }

        private void BuildTabs()
        {
            int tabCount = TabNames.Length;
            float totalWidth = tabCount * 110 + (tabCount - 1) * 4;
            float startX = -totalWidth / 2 + 55;

            for (int i = 0; i < tabCount; i++)
            {
                var idx = i;
                var tab = new GameObject("Tab_" + i, typeof(RectTransform), typeof(Image));
                tab.transform.SetParent(transform, false);
                var tabRt = tab.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.5f, 1);
                tabRt.anchorMax = new Vector2(0.5f, 1);
                tabRt.sizeDelta = new Vector2(110, 36);
                tabRt.anchoredPosition = new Vector2(startX + i * 114, -90);

                var tabImg = tab.GetComponent<Image>();
                tabImg.color = ColorTabNormal;

                var tabText = CreateText(tabRt, "Label", TabNames[i], 18);
                var textRt = (RectTransform)tabText.transform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                var btn = tab.AddComponent<Button>();
                btn.targetGraphic = tabImg;
                btn.onClick.AddListener(() => OnTabClick(idx));
            }
        }

        private void OnTabClick(int index)
        {
            _currentTab = index;
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tabGo = transform.Find("Tab_" + i);
                if (tabGo != null)
                    tabGo.GetComponent<Image>().color = (i == index) ? ColorTabActive : ColorTabNormal;
            }
            FilterByTab(index);
        }

        private void BuildGrid()
        {
            var scrollGo = new GameObject("GridScroll", typeof(RectTransform));
            scrollGo.transform.SetParent(transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0);
            scrollRt.anchorMax = new Vector2(0.5f, 1);
            scrollRt.sizeDelta = new Vector2(SlotSize * Cols + 40, 400);
            scrollRt.anchoredPosition = new Vector2(0, -210);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollRt, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var contentRt = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            contentRt.SetParent(vpRt, false);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(0, 1);
            contentRt.sizeDelta = new Vector2(SlotSize * Cols, 0);
            contentRt.anchoredPosition = Vector2.zero;

            scrollRect.viewport = vpRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            _gridRoot = contentRt;
        }

        private void FilterByTab(int tabIndex)
        {
            var p = GameManager.Instance.Player;
            _goldText.text = string.Format("{0:N0}", p.Gold);

            if (tabIndex == 0)
                _filteredItems = new List<ItemData>(_allItems);
            else
                _filteredItems = _allItems.FindAll(item => item.Type == TabNames[tabIndex]);

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            foreach (var slot in _slotObjects)
                Destroy(slot);
            _slotObjects.Clear();

            int count = _filteredItems.Count;
            _usageText.text = string.Format("已用: {0}/{1}", count, MaxSlots);

            int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / Cols));
            _gridRoot.sizeDelta = new Vector2(SlotSize * Cols, rows * (SlotSize + 8));

            for (int i = 0; i < count; i++)
            {
                var idx = i;
                var item = _filteredItems[i];

                int col = i % Cols;
                int row = i / Cols;

                var slot = new GameObject("Slot_" + i, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(_gridRoot, false);
                var slotRt = slot.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0, 1);
                slotRt.anchorMax = new Vector2(0, 1);
                slotRt.sizeDelta = new Vector2(SlotSize, SlotSize);
                slotRt.anchoredPosition = new Vector2(col * (SlotSize + 8), -row * (SlotSize + 8));

                var slotImg = slot.GetComponent<Image>();
                slotImg.color = ColorSlotBg;

                var borderColor = GetQualityColor(item.Quality);

                var firstChar = item.Name.Length > 0 ? item.Name[0].ToString() : "?";
                var slotText = CreateText(slotRt, "Label", firstChar, 22);
                var textRt = (RectTransform)slotText.transform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                slotText.color = borderColor;

                if (item.Count > 1)
                {
                    var badge = CreateText(slotRt, "Badge", "x" + item.Count, 12);
                    var badgeRt = (RectTransform)badge.transform;
                    badgeRt.anchorMin = new Vector2(1, 0);
                    badgeRt.anchorMax = new Vector2(1, 0);
                    badgeRt.sizeDelta = new Vector2(40, 18);
                    badgeRt.anchoredPosition = new Vector2(4, 4);
                    badge.color = ColorDimText;
                    badge.alignment = TextAnchor.LowerRight;
                }

                var btn = slot.AddComponent<Button>();
                btn.targetGraphic = slotImg;
                btn.onClick.AddListener(() => ShowDetailPopup(idx));

                _slotObjects.Add(slot);
            }

            if (count == 0)
            {
                var empty = CreateText(_gridRoot, "EmptyHint", "暂无物品", 20);
                var emptyRt = (RectTransform)empty.transform;
                emptyRt.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRt.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRt.sizeDelta = new Vector2(200, 30);
                emptyRt.anchoredPosition = Vector2.zero;
                empty.color = ColorDimText;
                _slotObjects.Add(empty.gameObject);
            }
        }

        private void BuildBottomBar()
        {
            var bar = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0);
            barRt.anchorMax = new Vector2(0.5f, 0);
            barRt.sizeDelta = new Vector2(SlotSize * Cols + 40, 40);
            barRt.anchoredPosition = new Vector2(0, 20);
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.85f);

            _usageText = CreateText(barRt, "Usage", "已用: 0/40", 18);
            var usageRt = (RectTransform)_usageText.transform;
            usageRt.anchorMin = new Vector2(0, 0.5f);
            usageRt.anchorMax = new Vector2(0, 0.5f);
            usageRt.sizeDelta = new Vector2(200, 30);
            usageRt.anchoredPosition = new Vector2(20, 0);
            _usageText.color = ColorDimText;
            _usageText.alignment = TextAnchor.MiddleLeft;

            var sortBtn = CreateButton(barRt, "SortBtn", "整理", () => Debug.Log("[Bag] 整理背包"));
            var sortRt = (RectTransform)sortBtn.transform;
            sortRt.anchorMin = new Vector2(1, 0.5f);
            sortRt.anchorMax = new Vector2(1, 0.5f);
            sortRt.sizeDelta = new Vector2(80, 30);
            sortRt.anchoredPosition = new Vector2(-20, 0);
        }

        private void BuildDetailPopup()
        {
            _detailPopup = new GameObject("DetailPopup", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _detailPopup.SetParent(transform, false);
            _detailPopup.anchorMin = new Vector2(0.5f, 0.5f);
            _detailPopup.anchorMax = new Vector2(0.5f, 0.5f);
            _detailPopup.sizeDelta = new Vector2(380, 460);
            _detailPopup.anchoredPosition = Vector2.zero;
            _detailPopup.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.95f);
            _detailPopup.gameObject.SetActive(false);

            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(_detailPopup, false);
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = new Vector2(-5, -5);
            overlayRt.anchorMax = new Vector2(6, 6);
            overlayRt.sizeDelta = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            overlay.transform.SetSiblingIndex(0);

            var closeBtn = CreateButton(_detailPopup, "CloseBtn", "\u2715", () => _detailPopup.gameObject.SetActive(false));
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.sizeDelta = new Vector2(40, 40);
            closeRt.anchoredPosition = new Vector2(-8, -8);

            _detailName = CreateText(_detailPopup, "ItemName", "", 24);
            var nameRt = (RectTransform)_detailName.transform;
            nameRt.anchorMin = new Vector2(0.5f, 1);
            nameRt.anchorMax = new Vector2(0.5f, 1);
            nameRt.sizeDelta = new Vector2(340, 30);
            nameRt.anchoredPosition = new Vector2(0, -30);

            _detailType = CreateText(_detailPopup, "ItemType", "", 16);
            var typeRt = (RectTransform)_detailType.transform;
            typeRt.anchorMin = new Vector2(0.5f, 1);
            typeRt.anchorMax = new Vector2(0.5f, 1);
            typeRt.sizeDelta = new Vector2(340, 24);
            typeRt.anchoredPosition = new Vector2(0, -60);
            _detailType.color = ColorDimText;

            _detailQuality = CreateText(_detailPopup, "ItemQuality", "", 18);
            var qualRt = (RectTransform)_detailQuality.transform;
            qualRt.anchorMin = new Vector2(0.5f, 1);
            qualRt.anchorMax = new Vector2(0.5f, 1);
            qualRt.sizeDelta = new Vector2(340, 24);
            qualRt.anchoredPosition = new Vector2(0, -85);

            var sepLine = new GameObject("SepLine", typeof(RectTransform), typeof(Image));
            sepLine.transform.SetParent(_detailPopup, false);
            var sepRt = sepLine.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0, 1);
            sepRt.anchorMax = new Vector2(1, 1);
            sepRt.sizeDelta = new Vector2(-40, 1);
            sepRt.anchoredPosition = new Vector2(0, -105);
            sepLine.GetComponent<Image>().color = ColorAccent;

            _detailAttr = CreateText(_detailPopup, "ItemAttr", "", 18);
            var attrRt = (RectTransform)_detailAttr.transform;
            attrRt.anchorMin = new Vector2(0, 1);
            attrRt.anchorMax = new Vector2(0, 1);
            attrRt.sizeDelta = new Vector2(340, 60);
            attrRt.anchoredPosition = new Vector2(20, -130);
            _detailAttr.alignment = TextAnchor.UpperLeft;

            _detailDesc = CreateText(_detailPopup, "ItemDesc", "", 16);
            var descRt = (RectTransform)_detailDesc.transform;
            descRt.anchorMin = new Vector2(0, 1);
            descRt.anchorMax = new Vector2(0, 1);
            descRt.sizeDelta = new Vector2(340, 60);
            descRt.anchoredPosition = new Vector2(20, -200);
            _detailDesc.alignment = TextAnchor.UpperLeft;
            _detailDesc.color = ColorDimText;

            var useBtn = CreateButton(_detailPopup, "UseBtn", "使用", () => Debug.Log("[Bag] 使用物品"));
            var useRt = (RectTransform)useBtn.transform;
            useRt.anchorMin = new Vector2(0, 0);
            useRt.anchorMax = new Vector2(0, 0);
            useRt.sizeDelta = new Vector2(100, 36);
            useRt.anchoredPosition = new Vector2(40, 30);
            useBtn.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f, 0.8f);

            var sellBtn = CreateButton(_detailPopup, "SellBtn", "出售", () => Debug.Log("[Bag] 出售物品"));
            var sellRt = (RectTransform)sellBtn.transform;
            sellRt.anchorMin = new Vector2(0.5f, 0);
            sellRt.anchorMax = new Vector2(0.5f, 0);
            sellRt.sizeDelta = new Vector2(100, 36);
            sellRt.anchoredPosition = new Vector2(0, 30);

            var dropBtn = CreateButton(_detailPopup, "DropBtn", "丢弃", () => Debug.Log("[Bag] 丢弃物品"));
            var dropRt = (RectTransform)dropBtn.transform;
            dropRt.anchorMin = new Vector2(1, 0);
            dropRt.anchorMax = new Vector2(1, 0);
            dropRt.sizeDelta = new Vector2(100, 36);
            dropRt.anchoredPosition = new Vector2(-40, 30);
            dropBtn.GetComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
        }

        private void ShowDetailPopup(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _filteredItems.Count) return;
            var item = _filteredItems[itemIndex];

            _detailName.text = item.Name;
            _detailName.color = GetQualityColor(item.Quality);

            _detailType.text = "类型: " + item.Type;

            var stars = "";
            for (int i = 0; i < item.Quality; i++) stars += "\u2605";
            _detailQuality.text = "品质: " + stars;
            _detailQuality.color = GetQualityColor(item.Quality);

            _detailAttr.text = item.Attributes;
            _detailDesc.text = item.Description;

            _detailPopup.gameObject.SetActive(true);
            _detailPopup.SetAsLastSibling();
        }

        private static Color GetQualityColor(int quality)
        {
            switch (quality)
            {
                case 1: return ColorQualityWhite;
                case 2: return ColorQualityGreen;
                case 3: return ColorQualityBlue;
                case 4: return ColorQualityPurple;
                default: return Color.white;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            var p = GameManager.Instance.Player;
            _goldText.text = string.Format("{0:N0}", p.Gold);
            FilterByTab(_currentTab);
        }
    }
}