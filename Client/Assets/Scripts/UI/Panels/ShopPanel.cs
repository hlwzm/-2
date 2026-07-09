using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;
using Jx3.UI;

namespace Jx3.UI.Panels
{
    public class ShopPanel : BasePanel
    {
        // ===== Categories =====
        private static readonly string[] Categories = { "鐑崠", "鏃惰", "閬撳叿", "浼樻儬", "鏈堝崱" };

        // ===== Demo Shop Item Data =====
        private class ShopItemData
        {
            public string Name;
            public int Quality; // 1-5
            public uint PriceGold;
            public uint PriceTongbao;
            public bool CanBuyWithGold;
            public string Category;
        }

        private readonly List<ShopItemData> _mockItems = new()
        {
            // 鈹€鈹€ 鐑崠 (6 items) 鈹€鈹€
            new() { Name = "姹熸箹鏂版墜绀煎寘", Quality = 3, PriceGold = 1000, CanBuyWithGold = true, Category = "鐑崠" },
            new() { Name = "姹熸箹鍘嗙粌绀肩洅", Quality = 4, PriceGold = 2880, CanBuyWithGold = true, Category = "鐑崠" },
            new() { Name = "寮哄寲鏉愭枡鍖?, Quality = 3, PriceGold = 2000, CanBuyWithGold = true, Category = "鐑崠" },
            new() { Name = "闄愭椂路宸呭嘲瀵瑰喅", Quality = 5, PriceGold = 0, PriceTongbao = 9800, Category = "鐑崠" },
            new() { Name = "姣忔棩鐗规儬路閲戝竵", Quality = 3, PriceGold = 680, CanBuyWithGold = true, Category = "鐑崠" },
            new() { Name = "姝﹀绉樼睄路鍏ラ棬", Quality = 4, PriceGold = 3500, CanBuyWithGold = true, Category = "鐑崠" },

            // 鈹€鈹€ 鏃惰 (5 items) 鈹€鈹€
            new() { Name = "闄愭椂路缁濅唬椋庡崕", Quality = 5, PriceGold = 0, PriceTongbao = 12800, Category = "鏃惰" },
            new() { Name = "鍑よ垶涔濆ぉ路琛?, Quality = 5, PriceGold = 0, PriceTongbao = 19800, Category = "鏃惰" },
            new() { Name = "鏆楀骞藉叞路琛?, Quality = 4, PriceGold = 0, PriceTongbao = 8800, Category = "鏃惰" },
            new() { Name = "鏄ラ鎷傛煶路濂楄", Quality = 4, PriceGold = 0, PriceTongbao = 6600, Category = "鏃惰" },
            new() { Name = "鏄熸渤婕路鎶", Quality = 3, PriceGold = 0, PriceTongbao = 4800, Category = "鏃惰" },

            // 鈹€鈹€ 閬撳叿 (6 items) 鈹€鈹€
            new() { Name = "娲楃偧鐭陈烽珮绾?, Quality = 4, PriceGold = 5000, CanBuyWithGold = true, Category = "閬撳叿" },
            new() { Name = "浜旇鐭陈峰叚绾?, Quality = 4, PriceGold = 1500, CanBuyWithGold = true, Category = "閬撳叿" },
            new() { Name = "淇负涓孤峰ぇ", Quality = 3, PriceGold = 800, CanBuyWithGold = true, Category = "閬撳叿" },
            new() { Name = "绮剧偧鐭陈锋瀬鍝?, Quality = 5, PriceGold = 12000, CanBuyWithGold = true, Category = "閬撳叿" },
            new() { Name = "寮哄寲淇濇姢绗?, Quality = 4, PriceGold = 3000, CanBuyWithGold = true, Category = "閬撳叿" },
            new() { Name = "鑳屽寘鎵╁睍鍒?, Quality = 3, PriceGold = 0, PriceTongbao = 1500, Category = "閬撳叿" },

            // 鈹€鈹€ 浼樻儬 (4 items) 鈹€鈹€
            new() { Name = "鏂版墜鐗规儬鍖?, Quality = 4, PriceGold = 680, CanBuyWithGold = true, Category = "浼樻儬" },
            new() { Name = "闄愭椂鐗规儬路淇负", Quality = 3, PriceGold = 480, CanBuyWithGold = true, Category = "浼樻儬" },
            new() { Name = "7鏃ユ湀鍗′綋楠屽埜", Quality = 3, PriceGold = 680, CanBuyWithGold = true, Category = "浼樻儬" },
            new() { Name = "鍛ㄦ湯鐙傛绀肩洅", Quality = 4, PriceGold = 1280, CanBuyWithGold = true, Category = "浼樻儬" },

            // 鈹€鈹€ 鏈堝崱 (4 items) 鈹€鈹€
            new() { Name = "鏈堝崱路30澶?, Quality = 4, PriceGold = 0, PriceTongbao = 6000, Category = "鏈堝崱" },
            new() { Name = "鑷冲皧鏈堝崱路90澶?, Quality = 5, PriceGold = 0, PriceTongbao = 16800, Category = "鏈堝崱" },
            new() { Name = "鏈堝崱路7澶╀綋楠?, Quality = 3, PriceGold = 0, PriceTongbao = 1200, Category = "鏈堝崱" },
            new() { Name = "骞村害鑷冲皧鍗?, Quality = 5, PriceGold = 0, PriceTongbao = 49800, Category = "鏈堝崱" },
        };

        // ===== UI References =====
        private int _currentTab;
        private RectTransform _gridContainer;
        private Text _monthlyText;
        private readonly List<Button> _tabButtons = new();
        private readonly List<GameObject> _gridItems = new();

        // ===== Lifecycle =====
        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);
            BuildTitleBar(root);
            BuildCurrencyBar(root);
            BuildTabs(root);
            BuildGridArea(root);
            BuildBottomBar(root);

            ShowCategory(0);
        }

        // =====================================================================
        // 1. Title Bar 鈥?"鍟嗗煄" + close 鈫?MainCityPanel
        // =====================================================================
        private void BuildTitleBar(RectTransform root)
        {
            UIComponentFactory.CreateTitleBar(root, "鍟嗗煄", () =>
            {
                UIManager.Instance.Hide<ShopPanel>();
                UIManager.Instance.Show<MainCityPanel>();
            });
        }

        // =====================================================================
        // 2. Currency Row 鈥?馃挵閲戝竵 + 馃拵閫氬疂 + 鍏呭€兼寜閽?        // =====================================================================
        private void BuildCurrencyBar(RectTransform root)
        {
            var bar = CreateBar(root, "CurrencyBar",
                new Vector2(700, 50), new Vector2(0, -84),
                new Color(0.1f, 0.1f, 0.2f, 0.7f));

            var p = GameManager.Instance.Player;

            // 馃挵 閲戝竵
            UIComponentFactory.CreateCurrencyLabel(bar, "GoldLabel",
                "馃挵", $"{p.Gold:N0}", ThemeColors.Gold, new Vector2(50, 0));

            // 馃拵 閫氬疂
            UIComponentFactory.CreateCurrencyLabel(bar, "TongbaoLabel",
                "馃拵", $"{p.Tongbao:N0}", ThemeColors.Tongbao, new Vector2(280, 0));

            // 鍏呭€兼寜閽?            var rechargeBtn = UIComponentFactory.CreatePrimaryButton(bar, "RechargeBtn", "鍏? 鍊?, OnRecharge);
            AnchorRight(rechargeBtn.GetComponent<RectTransform>(), new Vector2(100, 34), new Vector2(-60, 0));
        }

        // =====================================================================
        // 3. Category Tabs 鈥?[鐑崠] [鏃惰] [閬撳叿] [浼樻儬] [鏈堝崱]
        // =====================================================================
        private void BuildTabs(RectTransform root)
        {
            var tabBar = new GameObject("TabBar", typeof(RectTransform));
            tabBar.transform.SetParent(root, false);
            var tabBarRt = tabBar.GetComponent<RectTransform>();
            SetAnchor(tabBarRt, 0.5f, 1, 0.5f, 1);
            tabBarRt.sizeDelta = new Vector2(650, 44);
            tabBarRt.anchoredPosition = new Vector2(0, -130);

            for (int i = 0; i < Categories.Length; i++)
            {
                var idx = i;
                var tabBtn = UIComponentFactory.CreateTabButton(tabBarRt, "Tab" + i,
                    Categories[i], i == 0, () => ShowCategory(idx));
                var tbRt = (RectTransform)tabBtn.transform;
                tbRt.anchorMin = new Vector2(0, 0.5f);
                tbRt.anchorMax = new Vector2(0, 0.5f);
                tbRt.sizeDelta = new Vector2(120, 38);
                tbRt.anchoredPosition = new Vector2(-240 + idx * 130, 0);
                _tabButtons.Add(tabBtn);
            }
        }

        // =====================================================================
        // 4. Grid Area 鈥?3-column GridLayoutGroup for shop items
        // =====================================================================
        private void BuildGridArea(RectTransform root)
        {
            // Grid background
            var gridBg = new GameObject("GridBg", typeof(RectTransform), typeof(Image));
            gridBg.transform.SetParent(root, false);
            var gridBgRt = gridBg.GetComponent<RectTransform>();
            SetAnchor(gridBgRt, 0.5f, 0.5f, 0.5f, 0.5f);
            gridBgRt.sizeDelta = new Vector2(720, 380);
            gridBgRt.anchoredPosition = new Vector2(0, 5);
            gridBg.GetComponent<Image>().color = ThemeColors.BgCard;

            // ScrollView
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
            scrollGo.transform.SetParent(gridBgRt, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.sizeDelta = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport (Mask)
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollRt, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vpRt;

            // Content with GridLayoutGroup
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpRt, false);
            _gridContainer = contentGo.GetComponent<RectTransform>();
            _gridContainer.anchorMin = new Vector2(0, 1);
            _gridContainer.anchorMax = new Vector2(1, 1);
            _gridContainer.pivot = new Vector2(0.5f, 1);
            _gridContainer.anchoredPosition = new Vector2(0, 0);
            _gridContainer.sizeDelta = new Vector2(0, 0);

            var gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(210, 110);
            gridLayout.spacing = new Vector2(14, 14);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.padding = new RectOffset(14, 14, 14, 14);

            scrollRect.content = _gridContainer;
        }

        // =====================================================================
        // 5. Bottom Bar 鈥?鏈堝崱鍓╀綑澶╂暟 + 棰嗗彇/鍏呭€兼寜閽?        // =====================================================================
        private void BuildBottomBar(RectTransform root)
        {
            var bottomBar = CreateBar(root, "BottomBar",
                new Vector2(720, 56), new Vector2(0, 12),
                new Color(0.13f, 0.12f, 0.09f, 0.85f),
                AnchorY.Bottom);

            // 鏈堝崱淇℃伅
            _monthlyText = UIComponentFactory.CreateText(bottomBar, "MonthlyInfo",
                "鏈堝崱鍓╀綑: 0澶? |  姣忔棩棰嗗彇: 馃拵60閫氬疂 + 120浣撳姏",
                ThemeColors.FontSmall, ThemeColors.TextNormal, TextAnchor.MiddleLeft);
            var mtRt = _monthlyText.rectTransform;
            mtRt.anchorMin = new Vector2(0, 0.5f);
            mtRt.anchorMax = new Vector2(0, 0.5f);
            mtRt.sizeDelta = new Vector2(420, 30);
            mtRt.anchoredPosition = new Vector2(24, 0);

            // 棰嗗彇鎸夐挳
            var claimBtn = UIComponentFactory.CreatePrimaryButton(bottomBar, "ClaimBtn", "棰? 鍙?, OnClaimDaily);
            AnchorRight(claimBtn.GetComponent<RectTransform>(), new Vector2(80, 34), new Vector2(-12, 0));

            // 鍏呭€兼寜閽?            var rechargeBtn = UIComponentFactory.CreateSecondaryButton(bottomBar, "BottomRechargeBtn", "鍏? 鍊?, OnRecharge);
            AnchorRight(rechargeBtn.GetComponent<RectTransform>(), new Vector2(80, 34), new Vector2(-104, 0));
        }

        // =====================================================================
        // Show Category 鈥?filter items and update tab highlights
        // =====================================================================
        private void ShowCategory(int index)
        {
            _currentTab = index;
            string selectedCat = Categories[index];

            // Update tab visuals
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var img = _tabButtons[i].GetComponent<Image>();
                var txt = _tabButtons[i].GetComponent<Text>();
                var active = i == index;
                img.color = active ? ThemeColors.TabActive : ThemeColors.TabInactive;
                txt.color = active ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            }

            // Clear old items
            foreach (var item in _gridItems)
                Destroy(item);
            _gridItems.Clear();

            // Filter + create
            var filtered = _mockItems.FindAll(item => item.Category == selectedCat);
            foreach (var data in filtered)
                CreateShopItemCard(data);

            // Auto-resize content height
            int rows = Mathf.Max(1, (filtered.Count + 2) / 3);
            _gridContainer.sizeDelta = new Vector2(0, rows * 124 + 28);
        }

        // =====================================================================
        // Create Shop Item Card 鈥?icon placeholder | name | stars | price | buy
        // =====================================================================
        private void CreateShopItemCard(ShopItemData data)
        {
            var item = new GameObject("ShopItem", typeof(RectTransform));
            item.transform.SetParent(_gridContainer, false);

            // Quality border
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(item.transform, false);
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = Vector2.zero;
            border.GetComponent<Image>().color = ThemeColors.GetQualityColor(data.Quality) * new Color(1, 1, 1, 0.6f);

            // Card background (#1a1a2e)
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(item.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.sizeDelta = new Vector2(-4, -4);
            cardRt.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().color = ThemeColors.BgListItem;

            // Icon placeholder
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(cardRt, false);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 1);
            iconRt.anchorMax = new Vector2(0, 1);
            iconRt.sizeDelta = new Vector2(44, 44);
            iconRt.anchoredPosition = new Vector2(28, -28);
            icon.GetComponent<Image>().color = ThemeColors.GetQualityColor(data.Quality) * new Color(1, 1, 1, 0.25f);

            // Name
            var nameTxt = UIComponentFactory.CreateText(cardRt, "Name", data.Name,
                ThemeColors.FontSmall, ThemeColors.TextBright, TextAnchor.MiddleLeft);
            var nameRt = nameTxt.rectTransform;
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.sizeDelta = new Vector2(-60, 22);
            nameRt.anchoredPosition = new Vector2(42, -16);

            // Quality stars
            var starStr = ThemeColors.GetQualityStars(data.Quality);
            var starTxt = UIComponentFactory.CreateText(cardRt, "Quality", starStr,
                ThemeColors.FontTiny, ThemeColors.GetQualityColor(data.Quality), TextAnchor.MiddleLeft);
            var starRt = starTxt.rectTransform;
            starRt.anchorMin = new Vector2(0, 1);
            starRt.anchorMax = new Vector2(1, 1);
            starRt.sizeDelta = new Vector2(-60, 18);
            starRt.anchoredPosition = new Vector2(42, -40);

            // Price
            string priceStr = data.CanBuyWithGold
                ? $"馃挵 {data.PriceGold:N0}"
                : $"馃拵 {data.PriceTongbao:N0}";
            Color priceColor = data.CanBuyWithGold ? ThemeColors.Gold : ThemeColors.Tongbao;
            var priceTxt = UIComponentFactory.CreateText(cardRt, "Price", priceStr,
                ThemeColors.FontSmall, priceColor, TextAnchor.MiddleLeft);
            var priceRt = priceTxt.rectTransform;
            priceRt.anchorMin = new Vector2(0, 0);
            priceRt.anchorMax = new Vector2(0, 0);
            priceRt.sizeDelta = new Vector2(120, 26);
            priceRt.anchoredPosition = new Vector2(14, 14);

            // Buy button
            var buyBtn = UIComponentFactory.CreatePrimaryButton(cardRt, "BuyBtn", "璐? 涔?, () =>
            {
                Debug.Log($"[Shop] 璐拱: {data.Name}");
                ShopManager.Instance?.BuyItem(0);
            });
            var buyRt = (RectTransform)buyBtn.transform;
            buyRt.anchorMin = new Vector2(1, 0);
            buyRt.anchorMax = new Vector2(1, 0);
            buyRt.sizeDelta = new Vector2(64, 28);
            buyRt.anchoredPosition = new Vector2(-12, 14);
            // Shrink button text to fit
            var buyBtnText = buyBtn.GetComponentInChildren<Text>();
            if (buyBtnText != null) buyBtnText.fontSize = ThemeColors.FontTiny;

            _gridItems.Add(item);
        }

        // =====================================================================
        // Button Handlers
        // =====================================================================
        private static void OnRecharge()
        {
            Debug.Log("[Shop] 鎵撳紑鍏呭€肩晫闈?);
            ShopManager.Instance?.Recharge(1);
        }

        private static void OnClaimDaily()
        {
            Debug.Log("[Shop] 棰嗗彇鏈堝崱濂栧姳");
            ShopManager.Instance?.BuyItem(0);
        }

        // =====================================================================
        // Layout Helpers
        // =====================================================================
        private enum AnchorY { Top, Center, Bottom }

        private static RectTransform CreateBar(RectTransform parent, string name,
            Vector2 size, Vector2 anchoredPos, Color color, AnchorY anchorY = AnchorY.Top)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var rt = bar.GetComponent<RectTransform>();

            float y = anchorY switch
            {
                AnchorY.Top => 1f,
                AnchorY.Bottom => 0f,
                _ => 0.5f
            };
            rt.anchorMin = new Vector2(0.5f, y);
            rt.anchorMax = new Vector2(0.5f, y);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            bar.GetComponent<Image>().color = color;
            return rt;
        }

        private static void SetAnchor(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
        }

        private static void AnchorRight(RectTransform rt, Vector2 size, Vector2 anchoredPos)
        {
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }
    }
}