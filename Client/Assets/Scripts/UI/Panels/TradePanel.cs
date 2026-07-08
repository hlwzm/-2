using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;
using System.Linq;

namespace Jx3.UI.Panels
{
    public class TradePanel : BasePanel
    {
        // ===== 模拟商品数据 =====
        private class TradeItemData
        {
            public string Name;
            public int Quality; // 1-5
            public ulong Price;
            public uint Remaining;
            public string Category;
            public ulong SellerId;
        }

        private readonly List<TradeItemData> _mockItems = new()
        {
            new TradeItemData { Name = "太极·玄铁重剑", Quality = 5, Price = 88000, Remaining = 1, Category = "武器", SellerId = 1001 },
            new TradeItemData { Name = "天玄·碧落笛", Quality = 4, Price = 35000, Remaining = 2, Category = "武器", SellerId = 1002 },
            new TradeItemData { Name = "紫宸·明光铠", Quality = 4, Price = 42000, Remaining = 1, Category = "防具", SellerId = 1003 },
            new TradeItemData { Name = "云锦·流仙裙", Quality = 5, Price = 65000, Remaining = 1, Category = "防具", SellerId = 1004 },
            new TradeItemData { Name = "青冥·碧玉簪", Quality = 3, Price = 12000, Remaining = 3, Category = "饰品", SellerId = 1005 },
            new TradeItemData { Name = "赤血·龙纹佩", Quality = 4, Price = 28000, Remaining = 2, Category = "饰品", SellerId = 1006 },
            new TradeItemData { Name = "玄铁石·极品", Quality = 4, Price = 15000, Remaining = 10, Category = "材料", SellerId = 1007 },
            new TradeItemData { Name = "浣纱丝·稀世", Quality = 3, Price = 5500, Remaining = 25, Category = "材料", SellerId = 1008 },
            new TradeItemData { Name = "《太虚剑意·上》", Quality = 5, Price = 120000, Remaining = 1, Category = "秘籍", SellerId = 1009 },
            new TradeItemData { Name = "《冰心诀·残篇》", Quality = 4, Price = 46000, Remaining = 1, Category = "秘籍", SellerId = 1010 },
            new TradeItemData { Name = "破天·碎星刀", Quality = 3, Price = 8800, Remaining = 5, Category = "武器", SellerId = 1011 },
            new TradeItemData { Name = "凝霜·寒玉镯", Quality = 4, Price = 22000, Remaining = 3, Category = "饰品", SellerId = 1012 },
            new TradeItemData { Name = "千年寒铁", Quality = 5, Price = 45000, Remaining = 5, Category = "材料", SellerId = 1013 },
            new TradeItemData { Name = "《万花千机》", Quality = 4, Price = 38000, Remaining = 2, Category = "秘籍", SellerId = 1014 },
            new TradeItemData { Name = "凤羽·流火冠", Quality = 3, Price = 9600, Remaining = 4, Category = "防具", SellerId = 1015 },
        };

        // ===== 颜色常量 =====
        private static readonly Color ColorBg = new Color(0.06f, 0.06f, 0.12f);
        private static readonly Color ColorPanelBg = new Color(0.08f, 0.08f, 0.16f, 0.95f);
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.25f);
        private static readonly Color ColorTabActive = new Color(0.5f, 0.3f, 0.9f, 0.85f);
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorRowBg = new Color(0.1f, 0.1f, 0.2f, 0.7f);
        private static readonly Color ColorRowAlt = new Color(0.08f, 0.08f, 0.16f, 0.7f);
        private static readonly Color ColorGold = new Color(1f, 0.75f, 0.2f);
        private static readonly Color ColorBtnBuy = new Color(0.3f, 0.25f, 0.55f);
        private static readonly Color ColorBtnAction = new Color(0.2f, 0.2f, 0.35f);
        private static readonly Color ColorTextDim = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorHeaderBg = new Color(0.12f, 0.1f, 0.22f);
        private static readonly Color ColorSearchBg = new Color(0.1f, 0.1f, 0.18f);

        // ===== UI引用 =====
        private int _currentTab;
        private InputField _searchInput;
        private RectTransform _listContainer;
        private readonly List<GameObject> _listItems = new();
        private string _searchKeyword = "";

        protected override void Awake()
        {
            base.Awake();
            BuildBackground();
            BuildTitle();
            BuildTopActionBar();
            BuildSearchBar();
            BuildCategoryTabs();
            BuildTableHeader();
            BuildScrollList();
            BuildCloseButton();
            ShowCategory(0);
        }

        // ===== 1. 背景 =====
        private void BuildBackground()
        {
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = ColorBg;
        }

        // ===== 2. 标题 =====
        private void BuildTitle()
        {
            var title = CreateText(transform as RectTransform, "Title", "交易行", 28);
            title.fontStyle = FontStyle.Bold;
            var rt = (RectTransform)title.transform;
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(120, 40);
            rt.anchoredPosition = new Vector2(0, -40);

            // 标题下装饰线
            var line = new GameObject("TitleLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(transform, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 1);
            lineRt.anchorMax = new Vector2(0.5f, 1);
            lineRt.sizeDelta = new Vector2(60, 2);
            lineRt.anchoredPosition = new Vector2(0, -62);
            line.GetComponent<Image>().color = ColorAccent;
        }

        // ===== 3. 顶部操作栏 =====
        private void BuildTopActionBar()
        {
            var bar = new GameObject("TopActionBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 1);
            barRt.anchorMax = new Vector2(0.5f, 1);
            barRt.sizeDelta = new Vector2(700, 50);
            barRt.anchoredPosition = new Vector2(0, -85);
            bar.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.18f, 0.7f);

            // 我的上架
            var myListingBtn = CreateButton(barRt, "MyListingBtn", "我的上架", () =>
            {
                Debug.Log("[Trade] 打开我的上架");
                TradeManager.Instance?.SellItem(0, 0, 24, Jx3.Core.ItemCategory.Weapon);
            });
            var myRt = (RectTransform)myListingBtn.transform;
            myRt.anchorMin = new Vector2(0, 0.5f);
            myRt.anchorMax = new Vector2(0, 0.5f);
            myRt.sizeDelta = new Vector2(130, 36);
            myRt.anchoredPosition = new Vector2(15, 0);

            // 上架物品
            var sellBtn = CreateButton(barRt, "SellItemBtn", "上架物品", () =>
            {
                Debug.Log("[Trade] 打开上架物品界面 (5%手续费)");
            });
            var sellRt = (RectTransform)sellBtn.transform;
            sellRt.anchorMin = new Vector2(0, 0.5f);
            sellRt.anchorMax = new Vector2(0, 0.5f);
            sellRt.sizeDelta = new Vector2(130, 36);
            sellRt.anchoredPosition = new Vector2(160, 0);

            // 货币展示
            var p = GameManager.Instance.Player;
            var goldText = CreateTextWithParent(barRt, "GoldText", string.Format("金币: {0:N0}", p.Gold), 16, FontStyle.Bold, ColorGold);
            var goldRt = (RectTransform)goldText.transform;
            goldRt.anchorMin = new Vector2(1, 0.5f);
            goldRt.anchorMax = new Vector2(1, 0.5f);
            goldRt.sizeDelta = new Vector2(200, 30);
            goldRt.anchoredPosition = new Vector2(-15, 0);
            goldText.alignment = TextAnchor.MiddleRight;
        }

        // ===== 4. 搜索栏 =====
        private void BuildSearchBar()
        {
            var searchBar = new GameObject("SearchBar", typeof(RectTransform), typeof(Image));
            searchBar.transform.SetParent(transform, false);
            var searchRt = searchBar.GetComponent<RectTransform>();
            searchRt.anchorMin = new Vector2(0.5f, 1);
            searchRt.anchorMax = new Vector2(0.5f, 1);
            searchRt.sizeDelta = new Vector2(700, 44);
            searchRt.anchoredPosition = new Vector2(0, -138);
            searchBar.GetComponent<Image>().color = ColorSearchBg;

            // 输入框背景
            var inputBg = new GameObject("InputBg", typeof(RectTransform), typeof(Image));
            inputBg.transform.SetParent(searchRt, false);
            var inputBgRt = inputBg.GetComponent<RectTransform>();
            inputBgRt.anchorMin = new Vector2(0, 0.5f);
            inputBgRt.anchorMax = new Vector2(0, 0.5f);
            inputBgRt.sizeDelta = new Vector2(300, 32);
            inputBgRt.anchoredPosition = new Vector2(80, 0);
            inputBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.18f);

            // InputField
            var inputGo = new GameObject("InputField", typeof(RectTransform));
            inputGo.transform.SetParent(inputBgRt, false);
            var inputGoRt = inputGo.GetComponent<RectTransform>();
            inputGoRt.anchorMin = Vector2.zero; inputGoRt.anchorMax = Vector2.one;
            inputGoRt.sizeDelta = new Vector2(-16, -6);
            inputGoRt.anchoredPosition = new Vector2(0, 0);

            _searchInput = inputGo.AddComponent<InputField>();
            var inputText = inputGo.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 16;
            inputText.color = Color.white;
            inputText.supportRichText = false;
            inputText.alignment = TextAnchor.MiddleLeft;
            _searchInput.textComponent = inputText;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(inputGoRt, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;
            var phText = phGo.AddComponent<Text>();
            phText.text = "搜索物品名称...";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 16;
            phText.color = ColorTextDim;
            phText.alignment = TextAnchor.MiddleLeft;
            _searchInput.placeholder = phText;

            // 搜索按钮
            var searchBtn = new GameObject("SearchBtn", typeof(RectTransform), typeof(Image));
            searchBtn.transform.SetParent(searchRt, false);
            var searchBtnRt = searchBtn.GetComponent<RectTransform>();
            searchBtnRt.anchorMin = new Vector2(0, 0.5f);
            searchBtnRt.anchorMax = new Vector2(0, 0.5f);
            searchBtnRt.sizeDelta = new Vector2(60, 32);
            searchBtnRt.anchoredPosition = new Vector2(400, 0);
            var searchImg = searchBtn.GetComponent<Image>();
            searchImg.color = ColorAccent;
            var searchBtnComp = searchBtn.AddComponent<Button>();
            searchBtnComp.targetGraphic = searchImg;
            searchBtnComp.onClick.AddListener(() => OnSearch());

            var searchTxtGo = new GameObject("Text", typeof(RectTransform));
            searchTxtGo.transform.SetParent(searchBtnRt, false);
            var searchTxtRt = searchTxtGo.GetComponent<RectTransform>();
            searchTxtRt.anchorMin = Vector2.zero; searchTxtRt.anchorMax = Vector2.one;
            searchTxtRt.sizeDelta = Vector2.zero;
            var searchTxt = searchTxtGo.AddComponent<Text>();
            searchTxt.text = "搜索";
            searchTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            searchTxt.fontSize = 16;
            searchTxt.alignment = TextAnchor.MiddleCenter;
            searchTxt.color = Color.white;
        }

        private void OnSearch()
        {
            _searchKeyword = _searchInput?.text ?? "";
            Debug.Log($"[Trade] 搜索: {_searchKeyword}");
            RefreshList();
        }

        // ===== 5. 分类Tabs =====
        private void BuildCategoryTabs()
        {
            string[] categories = { "全部", "武器", "防具", "饰品", "材料", "秘籍" };
            for (int i = 0; i < categories.Length; i++)
            {
                var idx = i;
                var tab = new GameObject("CatTab" + i, typeof(RectTransform), typeof(Image));
                tab.transform.SetParent(transform, false);
                var tabRt = tab.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.5f, 1);
                tabRt.anchorMax = new Vector2(0.5f, 1);
                tabRt.sizeDelta = new Vector2(100, 34);
                tabRt.anchoredPosition = new Vector2(-300 + i * 110, -188);

                var tabImg = tab.GetComponent<Image>();
                tabImg.color = ColorTabNormal;

                var btn = tab.AddComponent<Button>();
                btn.targetGraphic = tabImg;
                btn.onClick.AddListener(() => ShowCategory(idx));

                var txtGo = new GameObject("Text", typeof(RectTransform));
                txtGo.transform.SetParent(tabRt, false);
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.text = categories[i];
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 16;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = ColorTextDim;
                txt.name = "TabText";
            }
        }

        // ===== 6. 表格表头 =====
        private void BuildTableHeader()
        {
            var header = new GameObject("TableHeader", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(transform, false);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1);
            headerRt.anchorMax = new Vector2(0.5f, 1);
            headerRt.sizeDelta = new Vector2(760, 32);
            headerRt.anchoredPosition = new Vector2(0, -225);
            header.GetComponent<Image>().color = ColorHeaderBg;

            string[] cols = { "物品名称", "品质", "价格", "剩余", "操作" };
            float[] widths = { 260, 80, 160, 100, 100 };
            float startX = -370;
            for (int i = 0; i < cols.Length; i++)
            {
                var colText = CreateTextWithParent(headerRt, "Col" + i, cols[i], 14, FontStyle.Bold, new Color(0.7f, 0.7f, 0.85f));
                var colRt = (RectTransform)colText.transform;
                colRt.anchorMin = new Vector2(0, 0.5f);
                colRt.anchorMax = new Vector2(0, 0.5f);
                colRt.sizeDelta = new Vector2(widths[i], 26);
                colRt.anchoredPosition = new Vector2(startX + widths[i] * 0.5f, 0);
                startX += widths[i];
            }
        }

        // ===== 7. 可滚动物品列表 =====
        private void BuildScrollList()
        {
            var listBg = new GameObject("ListBg", typeof(RectTransform), typeof(Image));
            listBg.transform.SetParent(transform, false);
            var listBgRt = listBg.GetComponent<RectTransform>();
            listBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            listBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            listBgRt.sizeDelta = new Vector2(770, 360);
            listBgRt.anchoredPosition = new Vector2(0, -50);
            listBg.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.5f);

            // ScrollView
            var scrollGo = new GameObject("ScrollList", typeof(RectTransform));
            scrollGo.transform.SetParent(listBgRt, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.sizeDelta = new Vector2(-4, -4);
            scrollRt.anchoredPosition = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            viewport.transform.SetParent(scrollRt, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRt;

            _listContainer = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _listContainer.transform.SetParent(viewportRt, false);
            _listContainer.anchorMin = new Vector2(0, 1);
            _listContainer.anchorMax = new Vector2(1, 1);
            _listContainer.sizeDelta = new Vector2(0, 0);
            _listContainer.pivot = new Vector2(0.5f, 1);
            scrollRect.content = _listContainer;
        }

        private void ShowCategory(int index)
        {
            _currentTab = index;

            // 更新Tab高亮
            for (int i = 0; i < 6; i++)
            {
                var tabGo = transform.Find("CatTab" + i);
                if (tabGo == null) continue;
                var img = tabGo.GetComponent<Image>();
                img.color = (i == index) ? ColorTabActive : ColorTabNormal;
                var txt = tabGo.Find("Text")?.GetComponent<Text>();
                if (txt != null)
                    txt.color = (i == index) ? Color.white : ColorTextDim;
            }

            RefreshList();
        }

        private void RefreshList()
        {
            // 清空旧项
            foreach (var item in _listItems)
                Destroy(item);
            _listItems.Clear();

            // 过滤
            string[] categories = { "全部", "武器", "防具", "饰品", "材料", "秘籍" };
            string selectedCat = categories[_currentTab];

            IEnumerable<TradeItemData> filtered = _mockItems;
            if (_currentTab > 0)
                filtered = filtered.Where(item => item.Category == selectedCat);

            if (!string.IsNullOrEmpty(_searchKeyword))
                filtered = filtered.Where(item => item.Name.Contains(_searchKeyword));

            var list = filtered.ToList();

            // 创建行
            for (int i = 0; i < list.Count; i++)
                CreateListItem(list[i], i);

            float contentHeight = list.Count * 46 + 10;
            _listContainer.sizeDelta = new Vector2(0, contentHeight);
        }

        private void CreateListItem(TradeItemData data, int index)
        {
            var row = new GameObject("Item_" + index, typeof(RectTransform));
            row.transform.SetParent(_listContainer, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1);
            rowRt.anchorMax = new Vector2(1, 1);
            rowRt.sizeDelta = new Vector2(0, 44);
            rowRt.anchoredPosition = new Vector2(0, -(10 + index * 46));
            rowRt.pivot = new Vector2(0.5f, 1);

            // 行背景（交替色）
            var rowBg = row.AddComponent<Image>();
            rowBg.color = (index % 2 == 0) ? ColorRowBg : ColorRowAlt;

            // 分割线
            var line = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(rowRt, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 0);
            lineRt.anchorMax = new Vector2(1, 0);
            lineRt.sizeDelta = new Vector2(0, 1);
            lineRt.anchoredPosition = new Vector2(0, 0);
            line.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f, 0.4f);

            string[] cols = {
                data.Name,
                new string('★', data.Quality).PadRight(5, '☆'),
                string.Format("{0:N0}", data.Price),
                data.Remaining.ToString()
            };
            float[] widths = { 260, 80, 160, 100 };
            float startX = -370;
            float rowHeight = 44;

            for (int c = 0; c < cols.Length; c++)
            {
                Color textColor = c switch
                {
                    0 => Color.white,
                    1 => new Color(0.8f, 0.6f, 0.2f),
                    2 => ColorGold,
                    _ => ColorTextDim,
                };
                FontStyle fontStyle = (c == 2) ? FontStyle.Bold : FontStyle.Normal;

                var cellText = CreateTextWithParent(rowRt, "Col" + c, cols[c], 15, fontStyle, textColor);
                var cellRt = (RectTransform)cellText.transform;
                cellRt.anchorMin = new Vector2(0, 0.5f);
                cellRt.anchorMax = new Vector2(0, 0.5f);
                cellRt.sizeDelta = new Vector2(widths[c], 30);
                cellRt.anchoredPosition = new Vector2(startX + widths[c] * 0.5f, 0);
                cellText.alignment = TextAnchor.MiddleLeft;
                startX += widths[c];
            }

            // 购买按钮
            var buyBtn = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image));
            buyBtn.transform.SetParent(rowRt, false);
            var buyBtnRt = buyBtn.GetComponent<RectTransform>();
            buyBtnRt.anchorMin = new Vector2(0, 0.5f);
            buyBtnRt.anchorMax = new Vector2(0, 0.5f);
            buyBtnRt.sizeDelta = new Vector2(70, 28);
            buyBtnRt.anchoredPosition = new Vector2(320, 0);
            var buyImg = buyBtn.GetComponent<Image>();
            buyImg.color = ColorBtnBuy;
            var buyBtnComp = buyBtn.AddComponent<Button>();
            buyBtnComp.targetGraphic = buyImg;
            var capturedName = data.Name;
            buyBtnComp.onClick.AddListener(() =>
            {
                Debug.Log($"[Trade] 购买: {capturedName}, 价格: {data.Price}");
                TradeManager.Instance?.SellItem(0, data.Price, 24, Jx3.Core.ItemCategory.Weapon);
            });

            var buyText = new GameObject("Text", typeof(RectTransform));
            buyText.transform.SetParent(buyBtnRt, false);
            var buyTextRt = buyText.GetComponent<RectTransform>();
            buyTextRt.anchorMin = Vector2.zero; buyTextRt.anchorMax = Vector2.one;
            buyTextRt.sizeDelta = Vector2.zero;
            var buyTxt = buyText.AddComponent<Text>();
            buyTxt.text = "购买";
            buyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buyTxt.fontSize = 14;
            buyTxt.alignment = TextAnchor.MiddleCenter;
            buyTxt.color = Color.white;

            _listItems.Add(row);
        }

        // ===== 8. 关闭按钮 =====
        private void BuildCloseButton()
        {
            var closeBtn = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image));
            closeBtn.transform.SetParent(transform, false);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.sizeDelta = new Vector2(36, 36);
            closeRt.anchoredPosition = new Vector2(-10, -10);
            var closeImg = closeBtn.GetComponent<Image>();
            closeImg.color = new Color(0.3f, 0.3f, 0.5f, 0.6f);
            var closeBtnComp = closeBtn.AddComponent<Button>();
            closeBtnComp.targetGraphic = closeImg;
            closeBtnComp.onClick.AddListener(() => Hide());

            var closeText = new GameObject("Text", typeof(RectTransform));
            closeText.transform.SetParent(closeRt, false);
            var closeTextRt = closeText.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero; closeTextRt.anchorMax = Vector2.one;
            closeTextRt.sizeDelta = Vector2.zero;
            var closeTxt = closeText.AddComponent<Text>();
            closeTxt.text = "✕";
            closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeTxt.fontSize = 20;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
        }

        // ===== 辅助方法 =====
        private static Text CreateTextWithParent(RectTransform parent, string name, string text,
            int fontSize, FontStyle fontStyle, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            return txt;
        }
    }
}
