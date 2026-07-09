using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 交易行面板 - 浏览物品 / 搜索 / 分类筛选 / 购买 / 我的上架
    /// 全程序化生成 · UIComponentFactory + ThemeColors
    /// </summary>
    public class TradePanel : BasePanel
    {
        // ============================================================
        // 数据模型
        // ============================================================
        private class TradeItem
        {
            public string Name;
            public int Quality;       // 1-5
            public ulong Price;       // 金币
            public uint RemainingHours;
            public string Category;   // "武器" "防具" "饰品" "材料" "秘籍"
        }

        // ============================================================
        // 模拟数据
        // ============================================================
        private readonly List<TradeItem> _demoItems = new()
        {
            new TradeItem { Name = "太极·玄铁重剑", Quality = 5, Price = 88000,  RemainingHours = 23, Category = "武器" },
            new TradeItem { Name = "紫宸·明光铠",   Quality = 4, Price = 42000,  RemainingHours = 18, Category = "防具" },
            new TradeItem { Name = "青冥·碧玉簪",   Quality = 3, Price = 12000,  RemainingHours = 47, Category = "饰品" },
            new TradeItem { Name = "玄铁石·极品",   Quality = 4, Price = 15000,  RemainingHours = 36, Category = "材料" },
            new TradeItem { Name = "《太虚剑意·上》", Quality = 5, Price = 120000, RemainingHours = 6,  Category = "秘籍" },
            new TradeItem { Name = "云锦·流仙裙",   Quality = 5, Price = 65000,  RemainingHours = 12, Category = "防具" },
            new TradeItem { Name = "赤血·龙纹佩",   Quality = 4, Price = 28000,  RemainingHours = 29, Category = "饰品" },
            new TradeItem { Name = "浣纱丝·稀世",   Quality = 3, Price = 5500,   RemainingHours = 51, Category = "材料" },
        };

        private readonly List<TradeItem> _myListings = new()
        {
            new TradeItem { Name = "破天·碎星刀", Quality = 3, Price = 8800,  RemainingHours = 8,  Category = "武器" },
            new TradeItem { Name = "千年寒铁",     Quality = 5, Price = 45000, RemainingHours = 3,  Category = "材料" },
            new TradeItem { Name = "凝霜·寒玉镯", Quality = 4, Price = 22000, RemainingHours = 20, Category = "饰品" },
        };

        // ============================================================
        // UI 状态
        // ============================================================
        private int _currentTab;
        private string _searchKeyword = "";

        private InputField _searchInput;

        private RectTransform _listContent;
        private readonly List<GameObject> _listRows = new();

        private RectTransform _myListingContent;
        private readonly List<GameObject> _myListingRows = new();

        private GameObject _mainView;
        private GameObject _myListingView;

        private readonly Button[] _tabButtons = new Button[6];
        private readonly Text[] _tabTexts = new Text[6];

        private static readonly string[] Categories = { "全部", "武器", "防具", "饰品", "材料", "秘籍" };

        // ============================================================
        // 生命周期
        // ============================================================
        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);

            BuildMainView(root);
            BuildMyListingView(root);

            ShowMainView();
        }

        // ============================================================
        // 主视图
        // ============================================================
        private void BuildMainView(RectTransform root)
        {
            _mainView = new GameObject("MainView", typeof(RectTransform));
            _mainView.transform.SetParent(root, false);
            var mv = _mainView.GetComponent<RectTransform>();
            mv.anchorMin = Vector2.zero; mv.anchorMax = Vector2.one;
            mv.sizeDelta = Vector2.zero;

            BuildTitleBar(mv);
            BuildSearchBar(mv);
            BuildCategoryTabs(mv);
            BuildTableHeader(mv);
            BuildScrollList(mv);
            BuildBottomBar(mv);

            ShowCategory(0);
        }

        // ── 标题栏 ──
        private void BuildTitleBar(RectTransform parent)
        {
            var barRt = UIComponentFactory.CreateTitleBar(parent, "交易行", () =>
            {
                // ✕ 关闭 → 返回主城
                UIManager.Instance.Hide<TradePanel>();
                UIManager.Instance.Show<MainCityPanel>();
            });

            // [我的上架] 按钮 — 放在标题栏右侧，关闭按钮左边
            var myListingBtn = UIComponentFactory.CreateSecondaryButton(
                barRt, "MyListingBtn", "我的上架", () => ShowMyListingView());
            var mlRt = myListingBtn.GetComponent<RectTransform>();
            mlRt.anchorMin = new Vector2(1, 0.5f);
            mlRt.anchorMax = new Vector2(1, 0.5f);
            mlRt.sizeDelta = new Vector2(110, 34);
            mlRt.anchoredPosition = new Vector2(-80, 0);
        }

        // ── 搜索栏 ──
        private void BuildSearchBar(RectTransform parent)
        {
            var barRt = MakeBar(parent, "SearchBar", new Vector2(0, -86), new Vector2(760, 48),
                new Color(0.08f, 0.08f, 0.14f, 0.7f));

            // 搜索图标
            var icon = UIComponentFactory.CreateText(barRt, "SearchIcon", "🔍",
                ThemeColors.FontBody, ThemeColors.TextDim, TextAnchor.MiddleCenter);
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0, 0.5f); iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(36, 36);
            iconRt.anchoredPosition = new Vector2(22, 0);

            // InputField
            _searchInput = UIComponentFactory.CreateInputField(barRt, "Input", "搜索物品...",
                new Vector2(500, 36), new Vector2(-60, 0));
            var inRt = _searchInput.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0, 0.5f); inRt.anchorMax = new Vector2(0, 0.5f);
            inRt.anchoredPosition = new Vector2(320, 0);

            // [搜索] 按钮
            var searchBtn = UIComponentFactory.CreatePrimaryButton(barRt, "SearchBtn", "搜索", () => DoSearch());
            var sbRt = searchBtn.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0.5f); sbRt.anchorMax = new Vector2(1, 0.5f);
            sbRt.sizeDelta = new Vector2(80, 36);
            sbRt.anchoredPosition = new Vector2(-14, 0);
        }

        private void DoSearch()
        {
            _searchKeyword = _searchInput?.text ?? "";
            Debug.Log($"[Trade] 搜索: {_searchKeyword}");
            RefreshList();
        }

        // ── 分类 Tabs ──
        private void BuildCategoryTabs(RectTransform parent)
        {
            var tabBar = MakeBar(parent, "TabBar", new Vector2(0, -146), new Vector2(760, 44),
                new Color(0.06f, 0.06f, 0.1f, 0.5f));

            float totalWidth = Categories.Length * 110f + (Categories.Length - 1) * 6f;
            float startX = -totalWidth / 2f + 55f;

            for (int i = 0; i < Categories.Length; i++)
            {
                int idx = i;
                bool isActive = (i == 0);
                var btn = UIComponentFactory.CreateTabButton(tabBar, "Tab_" + i, Categories[i], isActive,
                    () => ShowCategory(idx));
                var btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.5f, 0.5f);
                btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                btnRt.sizeDelta = new Vector2(108, 32);
                btnRt.anchoredPosition = new Vector2(startX + i * 116f, 0);

                _tabButtons[i] = btn;
                _tabTexts[i] = btn.GetComponent<Text>();
            }
        }

        // ── 表头 ──
        private void BuildTableHeader(RectTransform parent)
        {
            var header = MakeBar(parent, "TableHeader", new Vector2(0, -188), new Vector2(760, 30),
                ThemeColors.Border * 0.35f);

            string[] cols = { "物品名称", "品质", "价格", "剩余", "操作" };
            float[] offsets = { -350, -180, -90, 50, 300 };

            for (int c = 0; c < cols.Length; c++)
            {
                var txt = UIComponentFactory.CreateText(header, "Hdr" + c, cols[c],
                    ThemeColors.FontSmall, ThemeColors.TextDim, TextAnchor.MiddleLeft);
                var trt = txt.rectTransform;
                trt.anchorMin = new Vector2(0, 0.5f);
                trt.anchorMax = new Vector2(0, 0.5f);
                trt.sizeDelta = new Vector2(80, 24);
                trt.anchoredPosition = new Vector2(offsets[c] + 40, 0);
            }
        }

        // ── 滚动列表 ──
        private void BuildScrollList(RectTransform parent)
        {
            _listContent = UIComponentFactory.CreateScrollView(parent, "ScrollList",
                new Vector2(770, 320), new Vector2(0, -28));
        }

        // ── 底部栏 ──
        private void BuildBottomBar(RectTransform parent)
        {
            var barRt = MakeBar(parent, "BottomBar", new Vector2(0, -210), new Vector2(760, 48),
                new Color(0.06f, 0.06f, 0.1f, 0.7f));

            // [📦 上架物品] 按钮
            var sellBtn = UIComponentFactory.CreatePrimaryButton(barRt, "SellBtn", "📦 上架物品", () =>
                Debug.Log("[Trade] 打开上架界面 (5%手续费)"));
            var sbRt = sellBtn.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0, 0.5f); sbRt.anchorMax = new Vector2(0, 0.5f);
            sbRt.sizeDelta = new Vector2(140, 36);
            sbRt.anchoredPosition = new Vector2(14, 0);

            // 手续费提示
            var feeText = UIComponentFactory.CreateText(barRt, "FeeLabel", "手续费: 5%",
                ThemeColors.FontSmall, ThemeColors.TextDim, TextAnchor.MiddleLeft);
            var ftRt = feeText.rectTransform;
            ftRt.anchorMin = new Vector2(0, 0.5f); ftRt.anchorMax = new Vector2(0, 0.5f);
            ftRt.sizeDelta = new Vector2(120, 28);
            ftRt.anchoredPosition = new Vector2(175, 0);

            // [返回主城] 按钮
            var backBtn = UIComponentFactory.CreateSecondaryButton(barRt, "BackBtn", "返回主城", () =>
            {
                UIManager.Instance.Hide<TradePanel>();
                UIManager.Instance.Show<MainCityPanel>();
            });
            var bbRt = backBtn.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(1, 0.5f); bbRt.anchorMax = new Vector2(1, 0.5f);
            bbRt.sizeDelta = new Vector2(110, 36);
            bbRt.anchoredPosition = new Vector2(-14, 0);
        }

        // ============================================================
        // 我的上架子视图
        // ============================================================
        private void BuildMyListingView(RectTransform root)
        {
            _myListingView = new GameObject("MyListingView", typeof(RectTransform));
            _myListingView.transform.SetParent(root, false);
            var mv = _myListingView.GetComponent<RectTransform>();
            mv.anchorMin = Vector2.zero; mv.anchorMax = Vector2.one;
            mv.sizeDelta = Vector2.zero;

            // 标题栏
            var titleBar = UIComponentFactory.CreateTitleBar(mv, "我的上架", null);

            // 隐藏 CreateTitleBar 自动生成的关闭按钮，改为自己的返回按钮
            var autoCloseBtn = titleBar.Find("CloseBtn");
            if (autoCloseBtn != null) autoCloseBtn.gameObject.SetActive(false);

            // 返回按钮
            var backBtn = UIComponentFactory.CreateSecondaryButton(titleBar, "BackToListBtn", "← 返回列表", () => ShowMainView());
            var bbRt = backBtn.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(1, 0.5f); bbRt.anchorMax = new Vector2(1, 0.5f);
            bbRt.sizeDelta = new Vector2(120, 34);
            bbRt.anchoredPosition = new Vector2(-24, 0);

            // 表头
            var header = MakeBar(mv, "MyHeader", new Vector2(0, -86), new Vector2(760, 30),
                ThemeColors.Border * 0.35f);

            string[] cols = { "物品名称", "品质", "价格", "剩余", "操作" };
            float[] offsets = { -350, -180, -90, 50, 300 };
            for (int c = 0; c < cols.Length; c++)
            {
                var txt = UIComponentFactory.CreateText(header, "Mh" + c, cols[c],
                    ThemeColors.FontSmall, ThemeColors.TextDim, TextAnchor.MiddleLeft);
                var trt = txt.rectTransform;
                trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(0, 0.5f);
                trt.sizeDelta = new Vector2(80, 24);
                trt.anchoredPosition = new Vector2(offsets[c] + 40, 0);
            }

            // 滚动列表
            _myListingContent = UIComponentFactory.CreateScrollView(mv, "MyScrollList",
                new Vector2(770, 380), new Vector2(0, 90));

            _myListingView.SetActive(false);
        }

        // ============================================================
        // 视图切换
        // ============================================================
        private void ShowMainView()
        {
            _myListingView?.SetActive(false);
            _mainView?.SetActive(true);
            RefreshList();
        }

        private void ShowMyListingView()
        {
            _mainView?.SetActive(false);
            _myListingView?.SetActive(true);
            BuildMyListingRows();
        }

        // ============================================================
        // 分类 / 刷新
        // ============================================================
        private void ShowCategory(int index)
        {
            _currentTab = index;

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                bool active = (i == index);
                _tabButtons[i].GetComponent<Image>().color = active ? ThemeColors.TabActive : ThemeColors.TabInactive;
                if (_tabTexts[i] != null)
                    _tabTexts[i].color = active ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            }

            RefreshList();
        }

        private void RefreshList()
        {
            // 清空旧行
            foreach (var row in _listRows) Destroy(row);
            _listRows.Clear();

            IEnumerable<TradeItem> filtered = _demoItems;

            if (_currentTab > 0)
                filtered = filtered.Where(item => item.Category == Categories[_currentTab]);

            if (!string.IsNullOrWhiteSpace(_searchKeyword))
                filtered = filtered.Where(item =>
                    item.Name.Contains(_searchKeyword) || item.Category.Contains(_searchKeyword));

            var items = filtered.ToList();

            for (int i = 0; i < items.Count; i++)
                CreateTableRow(_listContent, _listRows, items[i], i, isMyListing: false);

            _listContent.sizeDelta = new Vector2(0, Mathf.Max(items.Count * 50 + 10, 10));
        }

        // ============================================================
        // 我的上架行构建
        // ============================================================
        private void BuildMyListingRows()
        {
            foreach (var row in _myListingRows) Destroy(row);
            _myListingRows.Clear();

            var items = _myListings;
            for (int i = 0; i < items.Count; i++)
                CreateTableRow(_myListingContent, _myListingRows, items[i], i, isMyListing: true);

            _myListingContent.sizeDelta = new Vector2(0, Mathf.Max(items.Count * 50 + 10, 10));
        }

        // ============================================================
        // 通用：创建表格行
        // ============================================================
        private void CreateTableRow(RectTransform parent, List<GameObject> rowList,
            TradeItem item, int index, bool isMyListing)
        {
            var row = new GameObject("Row_" + index, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, 44);
            rowRt.anchorMin = new Vector2(0, 1);
            rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0.5f, 1);

            // 交替行背景
            var rowBg = row.GetComponent<Image>();
            rowBg.color = (index % 2 == 0)
                ? ThemeColors.BgListItem
                : new Color(0.08f, 0.08f, 0.14f, 0.8f);

            // 底部分割线
            var divider = new GameObject("Div", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(rowRt, false);
            var drt = divider.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 0); drt.anchorMax = new Vector2(1, 0);
            drt.sizeDelta = new Vector2(0, 1);
            divider.GetComponent<Image>().color = ThemeColors.DecorLine * 0.6f;

            // ── 列数据 ──
            string name = item.Name;
            string stars = ThemeColors.GetQualityStars(item.Quality);
            string price = $"{item.Price:N0}";
            string remain = isMyListing
                ? $"{item.RemainingHours}时"
                : $"剩{item.RemainingHours}时";

            float[] xPositions = { -350, -180, -90, 50 };
            Color[] colors =
            {
                ThemeColors.TextBright,
                ThemeColors.GetQualityColor(item.Quality),
                ThemeColors.Gold,
                ThemeColors.TextDim,
            };
            FontStyle[] styles = { FontStyle.Normal, FontStyle.Normal, FontStyle.Bold, FontStyle.Normal };
            string[] values = { name, stars, price, remain };

            for (int c = 0; c < values.Length; c++)
            {
                var cell = UIComponentFactory.CreateText(rowRt, "C" + c, values[c],
                    ThemeColors.FontSmall, colors[c], TextAnchor.MiddleLeft);
                var crt = cell.rectTransform;
                crt.anchorMin = new Vector2(0, 0.5f);
                crt.anchorMax = new Vector2(0, 0.5f);
                crt.sizeDelta = new Vector2(150, 30);
                crt.anchoredPosition = new Vector2(xPositions[c] + 75, 0);

                if (styles[c] == FontStyle.Bold)
                    cell.fontStyle = FontStyle.Bold;
            }

            // ── 操作按钮 ──
            if (isMyListing)
            {
                // [下架] 按钮
                var cancelBtn = UIComponentFactory.CreateButton(rowRt, "CancelBtn", "下架",
                    ThemeColors.BtnDanger, () =>
                    Debug.Log($"[Trade] 下架: {item.Name}"),
                    ThemeColors.FontTiny);
                var cbRt = cancelBtn.GetComponent<RectTransform>();
                cbRt.anchorMin = new Vector2(0, 0.5f); cbRt.anchorMax = new Vector2(0, 0.5f);
                cbRt.sizeDelta = new Vector2(70, 28);
                cbRt.anchoredPosition = new Vector2(290, 0);
            }
            else
            {
                // [购买] 按钮
                string capturedName = item.Name;
                ulong capturedPrice = item.Price;
                var buyBtn = UIComponentFactory.CreatePrimaryButton(rowRt, "BuyBtn", "购买", () =>
                    Debug.Log($"[Trade] 购买: {capturedName}, 价格: {capturedPrice:N0}"));
                var bbRt = buyBtn.GetComponent<RectTransform>();
                bbRt.anchorMin = new Vector2(0, 0.5f); bbRt.anchorMax = new Vector2(0, 0.5f);
                bbRt.sizeDelta = new Vector2(70, 28);
                bbRt.anchoredPosition = new Vector2(290, 0);

                var buyTxt = buyBtn.GetComponentInChildren<Text>();
                if (buyTxt != null) buyTxt.fontSize = ThemeColors.FontTiny;
            }

            rowList.Add(row);
        }

        // ============================================================
        // 辅助方法
        // ============================================================
        /// <summary>创建一个居中对齐的半透明 Bar，返回其 RectTransform。</summary>
        private static RectTransform MakeBar(RectTransform parent, string name, Vector2 pos, Vector2 size, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = bg;
            return rt;
        }
    }
}
