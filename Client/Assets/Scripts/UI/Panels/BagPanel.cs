using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 背包面板 - 分类Tab + 6列物品网格 + 详情弹窗
    /// 暗黑武侠风格 · 紫色调 · 全程序化生成 (UIComponentFactory + ThemeColors)
    /// </summary>
    public class BagPanel : BasePanel
    {
        // ===================================================================
        // Mock item data
        // ===================================================================
        private class ItemData
        {
            public string Name;
            public string Type;
            public int Quality;   // 1-5 stars
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
        private const float SlotSize = 68f;
        private const float SlotSpacing = 6f;
        private const float GridPadding = 8f;

        // Grid width shared between grid, bottom bar
        private static float GridWidth => Cols * (SlotSize + SlotSpacing) + SlotSpacing + GridPadding * 2;

        // ===================================================================
        // UI references
        // ===================================================================
        private RectTransform _gridContent;
        private Text _goldText;
        private Text _usageText;
        private readonly List<GameObject> _slotObjects = new();
        private Button[] _tabButtons;

        // Detail popup
        private GameObject _detailOverlay;
        private RectTransform _detailPopup;
        private Text _detailName, _detailType, _detailQuality, _detailAttr, _detailDesc;

        // ===================================================================
        // Lifecycle
        // ===================================================================
        protected override void Awake()
        {
            base.Awake();
            var root = (RectTransform)transform;

            InitMockData();
            BuildBackground(root);
            BuildTitleBar(root);
            BuildTabs(root);
            BuildGrid(root);
            BuildBottomBar(root);
            BuildDetailPopup(root);

            FilterByTab(0);
        }

        // ===================================================================
        // 1. Mock data (12 items, ★ to ★★★★★, all categories)
        // ===================================================================
        private void InitMockData()
        {
            _allItems = new List<ItemData>
            {
                new ItemData { Name = "紫晶石",       Type = "材料", Quality = 4, Count = 5,  Attributes = "强化材料",                          Description = "稀有的紫色晶石，可用于装备强化。" },
                new ItemData { Name = "回春丹",       Type = "材料", Quality = 1, Count = 12, Attributes = "使用: 回复气血 500",                 Description = "低级回复丹药，可快速恢复气血。" },
                new ItemData { Name = "灵霄剑",       Type = "武器", Quality = 3, Count = 1,  Attributes = "外功攻击 +86\n会心 +32",             Description = "凌霄阁弟子佩剑，剑身蕴含灵力。" },
                new ItemData { Name = "玄铁重甲",     Type = "防具", Quality = 3, Count = 1,  Attributes = "外功防御 +120\n气血 +500",          Description = "玄铁铸造的重甲，坚不可摧。" },
                new ItemData { Name = "翡翠玉佩",     Type = "饰品", Quality = 2, Count = 1,  Attributes = "内功攻击 +45\n会心 +18",             Description = "温润的翡翠玉佩，蕴含内力。" },
                new ItemData { Name = "追魂令",       Type = "任务", Quality = 1, Count = 1,  Attributes = "任务物品",                          Description = "追魂阁的信物，用于追踪目标。" },
                new ItemData { Name = "破军刀",       Type = "武器", Quality = 4, Count = 1,  Attributes = "外功攻击 +142\n破防 +55",           Description = "上古名刀，破军之威震慑四野。" },
                new ItemData { Name = "流光袍",       Type = "防具", Quality = 4, Count = 1,  Attributes = "内功防御 +98\n气血 +380\n闪避 +22",  Description = "流光溢彩的法袍，轻盈如风。" },
                new ItemData { Name = "陨铁护腕",     Type = "饰品", Quality = 5, Count = 1,  Attributes = "外功防御 +88\n会心抵抗 +40\n招架 +28", Description = "天外陨铁所铸，举世无双。" },
                new ItemData { Name = "破旧的木剑",   Type = "武器", Quality = 1, Count = 3,  Attributes = "外功攻击 +12",                       Description = "一把普通的木剑，适合新手练习。" },
                new ItemData { Name = "金创药",       Type = "材料", Quality = 2, Count = 8,  Attributes = "使用: 回复气血 1200",                 Description = "中级疗伤丹药，回复效果显著。" },
                new ItemData { Name = "密函",         Type = "任务", Quality = 3, Count = 1,  Attributes = "任务物品",                          Description = "一封加密的密函，内容尚未解密。" },
            };
        }

        // ===================================================================
        // 2. Background (#0a0a14 via ThemeColors.BgMain)
        // ===================================================================
        private void BuildBackground(RectTransform root)
        {
            UIComponentFactory.CreateBackground(root, "PanelBg");
        }

        // ===================================================================
        // 3. Title bar ("背包" + gold + close/back)
        // ===================================================================
        private void BuildTitleBar(RectTransform root)
        {
            var titleBar = UIComponentFactory.CreateTitleBar(
                root, "背 包",
                () => ReturnToMainCity());

            // Gold display inside title bar
            _goldText = UIComponentFactory.CreateCurrencyLabel(
                titleBar, "GoldLabel", "💰", "0", ThemeColors.Gold, new Vector2(180, 0));
        }

        /// <summary>Hide BagPanel, show MainCityPanel.</summary>
        private void ReturnToMainCity()
        {
            UIManager.Instance.Hide<BagPanel>();
            UIManager.Instance.Show<MainCityPanel>();
        }

        // ===================================================================
        // 4. Category tabs (6 tabs)
        // ===================================================================
        private void BuildTabs(RectTransform root)
        {
            int tabCount = TabNames.Length;
            _tabButtons = new Button[tabCount];

            float totalWidth = tabCount * 110f + (tabCount - 1) * 4f;
            float startX = -totalWidth / 2f + 55f;

            for (int i = 0; i < tabCount; i++)
            {
                int idx = i;
                bool isActive = i == 0;

                var tabBtn = UIComponentFactory.CreateTabButton(
                    root, "Tab_" + i, TabNames[i], isActive,
                    () => OnTabClick(idx));

                var tabRt = tabBtn.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.5f, 1);
                tabRt.anchorMax = new Vector2(0.5f, 1);
                tabRt.sizeDelta = new Vector2(110, 36);
                tabRt.anchoredPosition = new Vector2(startX + i * 114f, -88);

                _tabButtons[i] = tabBtn;
            }
        }

        private void OnTabClick(int index)
        {
            _currentTab = index;

            // Update tab visuals (active highlighted with ThemeColors.TabActive)
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var btn = _tabButtons[i];
                if (btn == null) continue;

                bool isActive = i == index;
                btn.GetComponent<Image>().color = isActive ? ThemeColors.TabActive : ThemeColors.TabInactive;

                var txt = btn.GetComponent<Text>();
                if (txt != null)
                    txt.color = isActive ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            }

            FilterByTab(index);
        }

        // ===================================================================
        // 5. Item grid (6-column GridLayoutGroup, scrollable)
        // ===================================================================
        private void BuildGrid(RectTransform root)
        {
            // --- Scroll view root (transparent) ---
            var scrollGo = new GameObject("GridScroll", typeof(RectTransform), typeof(Image));
            scrollGo.transform.SetParent(root, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.sizeDelta = new Vector2(GridWidth, 400);
            scrollRt.anchoredPosition = new Vector2(0, -10);
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0); // transparent

            // ScrollRect
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // --- Viewport with Mask ---
            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vp.transform.SetParent(scrollRt, false);
            var vpRt = vp.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(GridPadding, GridPadding);
            vpRt.offsetMax = new Vector2(-GridPadding, -GridPadding);
            vp.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f); // Mask needs a Graphic
            vp.GetComponent<Mask>().showMaskGraphic = false;

            // --- Content with GridLayoutGroup ---
            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
            content.transform.SetParent(vpRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 0);
            contentRt.anchoredPosition = Vector2.zero;

            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(SlotSize, SlotSize);
            grid.spacing = new Vector2(SlotSpacing, SlotSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Cols;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(0, 0, 0, 0);

            // ContentSizeFitter auto-resizes content height based on row count
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRt;
            scrollRect.content = contentRt;

            _gridContent = contentRt;
        }

        // ===================================================================
        // 6. Bottom bar ("已用: X/40" + sort button)
        // ===================================================================
        private void BuildBottomBar(RectTransform root)
        {
            var bar = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0);
            barRt.anchorMax = new Vector2(0.5f, 0);
            barRt.sizeDelta = new Vector2(GridWidth, 42);
            barRt.anchoredPosition = new Vector2(0, 8);
            bar.GetComponent<Image>().color = ThemeColors.BgCard;

            // Usage count
            _usageText = UIComponentFactory.CreateText(
                barRt, "Usage", "已用: 0/40",
                ThemeColors.FontBody, ThemeColors.TextNormal, TextAnchor.MiddleLeft);
            var usageRt = _usageText.rectTransform;
            usageRt.anchorMin = new Vector2(0, 0.5f);
            usageRt.anchorMax = new Vector2(0, 0.5f);
            usageRt.sizeDelta = new Vector2(200, 30);
            usageRt.anchoredPosition = new Vector2(20, 0);

            // Sort button
            var sortBtn = UIComponentFactory.CreateSecondaryButton(
                barRt, "SortBtn", "整理", () => Debug.Log("[Bag] 整理背包"));
            var sortRt = sortBtn.GetComponent<RectTransform>();
            sortRt.anchorMin = new Vector2(1, 0.5f);
            sortRt.anchorMax = new Vector2(1, 0.5f);
            sortRt.sizeDelta = new Vector2(80, 30);
            sortRt.anchoredPosition = new Vector2(-20, 0);
        }

        // ===================================================================
        // 7. Detail popup (overlay + card with item info + action buttons)
        // ===================================================================
        private void BuildDetailPopup(RectTransform root)
        {
            // --- Full-screen dim overlay (click to close) ---
            _detailOverlay = new GameObject("DetailOverlay", typeof(RectTransform), typeof(Image));
            _detailOverlay.transform.SetParent(root, false);
            var overlayRt = _detailOverlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            var overlayImg = _detailOverlay.GetComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.55f);
            overlayImg.raycastTarget = true;
            var overlayBtn = _detailOverlay.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayImg;
            overlayBtn.onClick.AddListener(HideDetailPopup);
            _detailOverlay.SetActive(false);

            // --- Popup card (centered) ---
            _detailPopup = UIComponentFactory.CreateCard(
                root, "DetailCard", new Vector2(380, 420), Vector2.zero);
            _detailPopup.gameObject.SetActive(false);

            // Close button (top-right of card)
            var closeBtn = UIComponentFactory.CreateIconButton(
                _detailPopup, "DetailClose", "✕", 30,
                () => HideDetailPopup());
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.anchoredPosition = new Vector2(-20, -20);

            // Item name (top, colored by quality)
            _detailName = UIComponentFactory.CreateText(
                _detailPopup, "ItemName", "",
                ThemeColors.FontPanelTitle, ThemeColors.TextWhite, TextAnchor.MiddleCenter);
            _detailName.fontStyle = FontStyle.Bold;
            var nameRt = _detailName.rectTransform;
            nameRt.anchorMin = new Vector2(0.5f, 1);
            nameRt.anchorMax = new Vector2(0.5f, 1);
            nameRt.sizeDelta = new Vector2(340, 32);
            nameRt.anchoredPosition = new Vector2(0, -28);

            // Item type
            _detailType = UIComponentFactory.CreateText(
                _detailPopup, "ItemType", "",
                ThemeColors.FontBody, ThemeColors.TextDim, TextAnchor.MiddleCenter);
            var typeRt = _detailType.rectTransform;
            typeRt.anchorMin = new Vector2(0.5f, 1);
            typeRt.anchorMax = new Vector2(0.5f, 1);
            typeRt.sizeDelta = new Vector2(340, 22);
            typeRt.anchoredPosition = new Vector2(0, -55);

            // Quality stars
            _detailQuality = UIComponentFactory.CreateText(
                _detailPopup, "ItemQuality", "",
                ThemeColors.FontBody, ThemeColors.TextWhite, TextAnchor.MiddleCenter);
            _detailQuality.fontStyle = FontStyle.Bold;
            var qualRt = _detailQuality.rectTransform;
            qualRt.anchorMin = new Vector2(0.5f, 1);
            qualRt.anchorMax = new Vector2(0.5f, 1);
            qualRt.sizeDelta = new Vector2(340, 24);
            qualRt.anchoredPosition = new Vector2(0, -82);

            // Divider line
            var div = UIComponentFactory.CreateDivider(_detailPopup, "SepLine");
            var divRt = div.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0, 1);
            divRt.anchorMax = new Vector2(1, 1);
            divRt.sizeDelta = new Vector2(-40, 2);
            divRt.anchoredPosition = new Vector2(0, -102);

            // Attributes / stats
            _detailAttr = UIComponentFactory.CreateText(
                _detailPopup, "ItemAttr", "",
                ThemeColors.FontBody, ThemeColors.TextNormal, TextAnchor.UpperLeft);
            var attrRt = _detailAttr.rectTransform;
            attrRt.anchorMin = new Vector2(0, 1);
            attrRt.anchorMax = new Vector2(0, 1);
            attrRt.sizeDelta = new Vector2(340, 70);
            attrRt.anchoredPosition = new Vector2(20, -118);

            // Description
            _detailDesc = UIComponentFactory.CreateText(
                _detailPopup, "ItemDesc", "",
                ThemeColors.FontSmall, ThemeColors.TextDim, TextAnchor.UpperLeft);
            var descRt = _detailDesc.rectTransform;
            descRt.anchorMin = new Vector2(0, 1);
            descRt.anchorMax = new Vector2(0, 1);
            descRt.sizeDelta = new Vector2(340, 50);
            descRt.anchoredPosition = new Vector2(20, -195);

            // --- Action buttons row ---
            var useBtn = UIComponentFactory.CreatePrimaryButton(
                _detailPopup, "UseBtn", "使用", () => Debug.Log("[Bag] 使用物品"));
            var useRt = useBtn.GetComponent<RectTransform>();
            useRt.anchorMin = new Vector2(0, 0);
            useRt.anchorMax = new Vector2(0, 0);
            useRt.sizeDelta = new Vector2(100, 36);
            useRt.anchoredPosition = new Vector2(40, 28);

            var sellBtn = UIComponentFactory.CreateSecondaryButton(
                _detailPopup, "SellBtn", "出售", () => Debug.Log("[Bag] 出售物品"));
            var sellRt = sellBtn.GetComponent<RectTransform>();
            sellRt.anchorMin = new Vector2(0.5f, 0);
            sellRt.anchorMax = new Vector2(0.5f, 0);
            sellRt.sizeDelta = new Vector2(100, 36);
            sellRt.anchoredPosition = new Vector2(0, 28);

            var dropBtn = UIComponentFactory.CreateButton(
                _detailPopup, "DropBtn", "丢弃",
                ThemeColors.BtnDanger, () => Debug.Log("[Bag] 丢弃物品"));
            var dropRt = dropBtn.GetComponent<RectTransform>();
            dropRt.anchorMin = new Vector2(1, 0);
            dropRt.anchorMax = new Vector2(1, 0);
            dropRt.sizeDelta = new Vector2(100, 36);
            dropRt.anchoredPosition = new Vector2(-40, 28);
        }

        // ===================================================================
        // 8. Data filtering & grid refresh
        // ===================================================================
        private void FilterByTab(int tabIndex)
        {
            // Update gold display
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
            // Clear existing slots
            foreach (var slot in _slotObjects)
            {
                if (slot != null) Destroy(slot);
            }
            _slotObjects.Clear();

            int count = _filteredItems.Count;
            _usageText.text = string.Format("已用: {0}/{1}", count, MaxSlots);

            // Build slots
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                var item = _filteredItems[i];
                var qualityColor = ThemeColors.GetQualityColor(item.Quality);

                // Slot container (#12121e via ThemeColors.BgCard)
                var slot = new GameObject("Slot_" + i, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(_gridContent, false);
                var slotImg = slot.GetComponent<Image>();
                slotImg.color = ThemeColors.BgCard;

                // Quality border (thin top accent line)
                var qBorder = new GameObject("QualityBorder", typeof(RectTransform), typeof(Image));
                qBorder.transform.SetParent(slot.transform, false);
                var qBorderRt = qBorder.GetComponent<RectTransform>();
                qBorderRt.anchorMin = new Vector2(0, 1);
                qBorderRt.anchorMax = Vector2.one;
                qBorderRt.sizeDelta = new Vector2(0, 3);
                qBorderRt.anchoredPosition = new Vector2(0, 0);
                qBorder.GetComponent<Image>().color = qualityColor;
                qBorder.GetComponent<Image>().raycastTarget = false;

                // Icon placeholder (first character of item name, large, quality-colored)
                var iconText = UIComponentFactory.CreateText(
                    (RectTransform)slot.transform, "IconPlaceholder",
                    item.Name.Length > 0 ? item.Name.Substring(0, 1) : "?",
                    28, qualityColor, TextAnchor.MiddleCenter);
                iconText.fontStyle = FontStyle.Bold;
                iconText.raycastTarget = false;
                var iconRt = iconText.rectTransform;
                iconRt.anchorMin = new Vector2(0, 0.5f);
                iconRt.anchorMax = Vector2.one;
                iconRt.sizeDelta = new Vector2(-4, -16);
                iconRt.anchoredPosition = new Vector2(0, 8);

                // Item name (small text at bottom of slot)
                var nameText = UIComponentFactory.CreateText(
                    (RectTransform)slot.transform, "SlotName", item.Name,
                    ThemeColors.FontTiny, ThemeColors.TextNormal, TextAnchor.LowerCenter);
                nameText.raycastTarget = false;
                var nameRt = nameText.rectTransform;
                nameRt.anchorMin = new Vector2(0, 0);
                nameRt.anchorMax = Vector2.one;
                nameRt.sizeDelta = new Vector2(-4, 16);
                nameRt.anchoredPosition = new Vector2(0, -2);

                // Quantity badge (bottom-right, only if count > 1)
                if (item.Count > 1)
                {
                    var badge = UIComponentFactory.CreateText(
                        (RectTransform)slot.transform, "Badge", "x" + item.Count,
                        ThemeColors.FontTiny, ThemeColors.TextWhite, TextAnchor.LowerRight);
                    badge.raycastTarget = false;
                    var badgeRt = badge.rectTransform;
                    badgeRt.anchorMin = new Vector2(1, 0);
                    badgeRt.anchorMax = new Vector2(1, 0);
                    badgeRt.sizeDelta = new Vector2(36, 16);
                    badgeRt.anchoredPosition = new Vector2(-2, 2);
                }

                // Click to open detail popup
                var btn = slot.AddComponent<Button>();
                btn.targetGraphic = slotImg;
                btn.onClick.AddListener(() => ShowDetailPopup(idx));

                _slotObjects.Add(slot);
            }

            // Empty state hint
            if (count == 0)
            {
                var empty = UIComponentFactory.CreateText(
                    _gridContent, "EmptyHint", "暂无物品",
                    ThemeColors.FontEntry, ThemeColors.TextDim, TextAnchor.MiddleCenter);
                var emptyRt = empty.rectTransform;
                emptyRt.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRt.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRt.sizeDelta = new Vector2(200, 30);
                emptyRt.anchoredPosition = Vector2.zero;
                _slotObjects.Add(empty.gameObject);
            }

            // Force layout rebuild so GridLayoutGroup + ContentSizeFitter update immediately
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridContent);
        }

        // ===================================================================
        // 9. Detail popup show/hide
        // ===================================================================
        private void ShowDetailPopup(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _filteredItems.Count) return;
            var item = _filteredItems[itemIndex];
            var qualityColor = ThemeColors.GetQualityColor(item.Quality);

            // Populate fields
            _detailName.text = item.Name;
            _detailName.color = qualityColor;

            _detailType.text = "类型: " + item.Type;

            _detailQuality.text = ThemeColors.GetQualityStars(item.Quality);
            _detailQuality.color = qualityColor;

            _detailAttr.text = item.Attributes;
            _detailDesc.text = item.Description;

            // Show overlay first, then card on top
            _detailOverlay.SetActive(true);
            _detailOverlay.transform.SetAsLastSibling();

            _detailPopup.gameObject.SetActive(true);
            _detailPopup.SetAsLastSibling();
        }

        private void HideDetailPopup()
        {
            _detailPopup.gameObject.SetActive(false);
            _detailOverlay.SetActive(false);
        }

        // ===================================================================
        // 10. Refresh (called by UIManager)
        // ===================================================================
        public override void Refresh()
        {
            base.Refresh();
            var p = GameManager.Instance.Player;
            _goldText.text = string.Format("{0:N0}", p.Gold);
            FilterByTab(_currentTab);
        }
    }
}
