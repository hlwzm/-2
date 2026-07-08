using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;

namespace Jx3.UI.Panels
{
    public class ShopPanel : BasePanel
    {
        // ===== 模拟商品数据 =====
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
            new ShopItemData { Name = "江湖新手礼包", Quality = 3, PriceGold = 1000, CanBuyWithGold = true, Category = "热卖" },
            new ShopItemData { Name = "限时·绝代风华", Quality = 5, PriceGold = 0, PriceTongbao = 12800, Category = "时装" },
            new ShopItemData { Name = "洗炼石·高级", Quality = 4, PriceGold = 5000, CanBuyWithGold = true, Category = "道具" },
            new ShopItemData { Name = "7日月卡体验券", Quality = 3, PriceGold = 680, CanBuyWithGold = true, Category = "优惠" },
            new ShopItemData { Name = "江湖历练礼盒", Quality = 4, PriceGold = 2880, CanBuyWithGold = true, Category = "热卖" },
            new ShopItemData { Name = "凤舞九天·衣", Quality = 5, PriceGold = 0, PriceTongbao = 19800, Category = "时装" },
            new ShopItemData { Name = "五行石·六级", Quality = 4, PriceGold = 1500, CanBuyWithGold = true, Category = "道具" },
            new ShopItemData { Name = "修为丹·大", Quality = 3, PriceGold = 800, CanBuyWithGold = true, Category = "道具" },
            new ShopItemData { Name = "新手特惠包", Quality = 4, PriceGold = 680, CanBuyWithGold = true, Category = "优惠" },
            new ShopItemData { Name = "月卡·30天", Quality = 4, PriceGold = 0, PriceTongbao = 6000, Category = "月卡" },
            new ShopItemData { Name = "暗夜幽兰·衣", Quality = 4, PriceGold = 0, PriceTongbao = 8800, Category = "时装" },
            new ShopItemData { Name = "强化材料包", Quality = 3, PriceGold = 2000, CanBuyWithGold = true, Category = "热卖" },
            new ShopItemData { Name = "精炼石·极品", Quality = 5, PriceGold = 12000, CanBuyWithGold = true, Category = "道具" },
            new ShopItemData { Name = "限时特惠·修为", Quality = 3, PriceGold = 480, CanBuyWithGold = true, Category = "优惠" },
            new ShopItemData { Name = "至尊月卡·90天", Quality = 5, PriceGold = 0, PriceTongbao = 16800, Category = "月卡" },
        };

        // ===== 颜色常量 =====
        private static readonly Color ColorBg = new Color(0.06f, 0.06f, 0.12f);
        private static readonly Color ColorPanelBg = new Color(0.08f, 0.08f, 0.16f, 0.95f);
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.25f);
        private static readonly Color ColorTabActive = new Color(0.5f, 0.3f, 0.9f, 0.85f);
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorCardBg = new Color(0.1f, 0.1f, 0.2f, 0.85f);
        private static readonly Color ColorGold = new Color(1f, 0.75f, 0.2f);
        private static readonly Color ColorTongbao = new Color(0.5f, 0.8f, 1f);
        private static readonly Color ColorBtnBuy = new Color(0.3f, 0.25f, 0.55f);
        private static readonly Color ColorBtnBuyGold = new Color(0.6f, 0.4f, 0.15f);
        private static readonly Color ColorQualityBorder = new Color(0.4f, 0.25f, 0.75f, 0.6f);
        private static readonly Color ColorTextDim = new Color(0.6f, 0.6f, 0.7f);

        // ===== UI引用 =====
        private Text _goldText, _tongbaoText, _monthlyText;
        private int _currentTab;
        private RectTransform _gridContainer;
        private readonly List<GameObject> _gridItems = new();

        protected override void Awake()
        {
            base.Awake();
            BuildBackground();
            BuildTitle();
            BuildCurrencyBar();
            BuildTabs();
            BuildScrollGrid();
            BuildBottomBar();
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
            var title = CreateText(transform as RectTransform, "Title", "商城", 28);
            title.fontStyle = FontStyle.Bold;
            var rt = (RectTransform)title.transform;
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(120, 40);
            rt.anchoredPosition = new Vector2(0, -40);
        }

        // ===== 3. 货币栏 =====
        private void BuildCurrencyBar()
        {
            var bar = new GameObject("CurrencyBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 1);
            barRt.anchorMax = new Vector2(0.5f, 1);
            barRt.sizeDelta = new Vector2(700, 50);
            barRt.anchoredPosition = new Vector2(0, -80);
            bar.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.7f);

            var p = GameManager.Instance.Player;

            // 金币
            var goldIcon = new GameObject("GoldIcon", typeof(RectTransform), typeof(Image));
            goldIcon.transform.SetParent(barRt, false);
            var goldIconRt = goldIcon.GetComponent<RectTransform>();
            goldIconRt.anchorMin = new Vector2(0, 0.5f);
            goldIconRt.anchorMax = new Vector2(0, 0.5f);
            goldIconRt.sizeDelta = new Vector2(24, 24);
            goldIconRt.anchoredPosition = new Vector2(50, 0);
            goldIcon.GetComponent<Image>().color = ColorGold;

            _goldText = CreateTextWithParent(barRt, "GoldText", string.Format("{0:N0}", p.Gold), 20, FontStyle.Bold, ColorGold);
            var goldRt = (RectTransform)_goldText.transform;
            goldRt.anchorMin = new Vector2(0, 0.5f);
            goldRt.anchorMax = new Vector2(0, 0.5f);
            goldRt.sizeDelta = new Vector2(160, 30);
            goldRt.anchoredPosition = new Vector2(130, 0);

            // 通宝
            var tbIcon = new GameObject("TongbaoIcon", typeof(RectTransform), typeof(Image));
            tbIcon.transform.SetParent(barRt, false);
            var tbIconRt = tbIcon.GetComponent<RectTransform>();
            tbIconRt.anchorMin = new Vector2(0, 0.5f);
            tbIconRt.anchorMax = new Vector2(0, 0.5f);
            tbIconRt.sizeDelta = new Vector2(24, 24);
            tbIconRt.anchoredPosition = new Vector2(300, 0);
            tbIcon.GetComponent<Image>().color = ColorTongbao;

            _tongbaoText = CreateTextWithParent(barRt, "TongbaoText", string.Format("{0:N0}", p.Tongbao), 20, FontStyle.Bold, ColorTongbao);
            var tbRt = (RectTransform)_tongbaoText.transform;
            tbRt.anchorMin = new Vector2(0, 0.5f);
            tbRt.anchorMax = new Vector2(0, 0.5f);
            tbRt.sizeDelta = new Vector2(160, 30);
            tbRt.anchoredPosition = new Vector2(380, 0);

            // 充值按钮
            var rechargeBtn = new GameObject("RechargeBtn", typeof(RectTransform), typeof(Image));
            rechargeBtn.transform.SetParent(barRt, false);
            var rechargeRt = rechargeBtn.GetComponent<RectTransform>();
            rechargeRt.anchorMin = new Vector2(1, 0.5f);
            rechargeRt.anchorMax = new Vector2(1, 0.5f);
            rechargeRt.sizeDelta = new Vector2(100, 34);
            rechargeRt.anchoredPosition = new Vector2(-60, 0);
            var rechargeImg = rechargeBtn.GetComponent<Image>();
            rechargeImg.color = ColorAccent;
            var rechargeBtnComp = rechargeBtn.AddComponent<Button>();
            rechargeBtnComp.targetGraphic = rechargeImg;
            rechargeBtnComp.onClick.AddListener(() => { Debug.Log("[Shop] 打开充值界面"); ShopManager.Instance?.Recharge(1); });

            var rechargeText = new GameObject("Text", typeof(RectTransform));
            rechargeText.transform.SetParent(rechargeRt, false);
            var rechargeTextRt = rechargeText.GetComponent<RectTransform>();
            rechargeTextRt.anchorMin = Vector2.zero; rechargeTextRt.anchorMax = Vector2.one;
            rechargeTextRt.sizeDelta = Vector2.zero;
            var rechargeTxt = rechargeText.AddComponent<Text>();
            rechargeTxt.text = "充值";
            rechargeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rechargeTxt.fontSize = 18;
            rechargeTxt.alignment = TextAnchor.MiddleCenter;
            rechargeTxt.color = Color.white;
        }

        // ===== 4. 分类Tabs =====
        private void BuildTabs()
        {
            string[] categories = { "热卖", "时装", "道具", "优惠", "月卡" };
            for (int i = 0; i < categories.Length; i++)
            {
                var idx = i;
                var tab = new GameObject("Tab" + i, typeof(RectTransform), typeof(Image));
                tab.transform.SetParent(transform, false);
                var tabRt = tab.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.5f, 1);
                tabRt.anchorMax = new Vector2(0.5f, 1);
                tabRt.sizeDelta = new Vector2(120, 38);
                tabRt.anchoredPosition = new Vector2(-240 + i * 130, -130);

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
                txt.fontSize = 18;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = ColorTextDim;
                txt.name = "TabText";
            }
        }

        // ===== 5. 商品网格 =====
        private void BuildScrollGrid()
        {
            // 网格容器背景
            var gridBg = new GameObject("GridBg", typeof(RectTransform), typeof(Image));
            gridBg.transform.SetParent(transform, false);
            var gridBgRt = gridBg.GetComponent<RectTransform>();
            gridBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            gridBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridBgRt.sizeDelta = new Vector2(700, 370);
            gridBgRt.anchoredPosition = new Vector2(0, 10);
            gridBg.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.14f, 0.6f);

            // ScrollView
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
            scrollGo.transform.SetParent(gridBgRt, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.sizeDelta = new Vector2(-10, -10);
            scrollRt.anchoredPosition = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport (mask)
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            viewport.transform.SetParent(scrollRt, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollRect.viewport = viewportRt;

            // Content (grid layout)
            _gridContainer = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _gridContainer.transform.SetParent(viewportRt, false);
            _gridContainer.anchorMin = new Vector2(0, 1);
            _gridContainer.anchorMax = new Vector2(1, 1);
            _gridContainer.sizeDelta = new Vector2(0, 0);
            _gridContainer.pivot = new Vector2(0.5f, 1);
            _gridContainer.anchoredPosition = new Vector2(0, 0);

            var gridLayout = _gridContainer.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(200, 100);
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.padding = new RectOffset(10, 10, 10, 10);

            scrollRect.content = _gridContainer;
        }

        private void ShowCategory(int index)
        {
            _currentTab = index;
            string[] categories = { "热卖", "时装", "道具", "优惠", "月卡" };
            string selectedCat = categories[index];

            // 更新Tab高亮
            for (int i = 0; i < 5; i++)
            {
                var tabGo = transform.Find("Tab" + i);
                if (tabGo == null) continue;
                var img = tabGo.GetComponent<Image>();
                img.color = (i == index) ? ColorTabActive : ColorTabNormal;
                var txt = tabGo.Find("Text")?.GetComponent<Text>();
                if (txt != null)
                    txt.color = (i == index) ? Color.white : ColorTextDim;
            }

            // 清空旧商品
            foreach (var item in _gridItems)
                Destroy(item);
            _gridItems.Clear();

            // 过滤并添加商品
            var filtered = _mockItems.FindAll(item => item.Category == selectedCat);
            foreach (var data in filtered)
                CreateShopItem(data);

            // 自动调整content高度
            int rows = Mathf.Max(1, (filtered.Count + 2) / 3);
            _gridContainer.sizeDelta = new Vector2(0, rows * 112 + 20);
        }

        private void CreateShopItem(ShopItemData data)
        {
            var item = new GameObject("ShopItem", typeof(RectTransform));
            item.transform.SetParent(_gridContainer, false);
            var itemRt = item.GetComponent<RectTransform>();

            // 品质边框
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(itemRt, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero;
            var borderImg = border.GetComponent<Image>();
            float qualityFactor = data.Quality / 5f;
            borderImg.color = new Color(0.3f + 0.3f * qualityFactor, 0.2f + 0.1f * qualityFactor, 0.5f + 0.5f * qualityFactor, 0.6f);
            borderRt.offsetMin = new Vector2(1, 1);
            borderRt.offsetMax = new Vector2(-1, -1);

            // 卡片背景
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(itemRt, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = Vector2.zero; cardRt.anchorMax = Vector2.one;
            cardRt.sizeDelta = new Vector2(-4, -4);
            cardRt.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().color = ColorCardBg;

            // 商品名称
            var nameText = CreateTextWithParent(cardRt, "Name", data.Name, 16, FontStyle.Normal, Color.white);
            var nameRt = (RectTransform)nameText.transform;
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.sizeDelta = new Vector2(-10, 28);
            nameRt.anchoredPosition = new Vector2(0, -16);
            nameText.alignment = TextAnchor.MiddleLeft;

            // 品质星级
            var starStr = new string('★', data.Quality) + new string('☆', 5 - data.Quality);
            var qualityText = CreateTextWithParent(cardRt, "Quality", starStr, 12, FontStyle.Normal, new Color(0.8f, 0.6f, 0.2f));
            var qualityRt = (RectTransform)qualityText.transform;
            qualityRt.anchorMin = new Vector2(0, 1);
            qualityRt.anchorMax = new Vector2(1, 1);
            qualityRt.sizeDelta = new Vector2(-10, 18);
            qualityRt.anchoredPosition = new Vector2(0, -40);
            qualityText.alignment = TextAnchor.MiddleLeft;

            // 价格
            string priceStr = data.CanBuyWithGold ? string.Format("{0:N0} 金币", data.PriceGold)
                : string.Format("{0:N0} 通宝", data.PriceTongbao);
            Color priceColor = data.CanBuyWithGold ? ColorGold : ColorTongbao;
            var priceText = CreateTextWithParent(cardRt, "Price", priceStr, 14, FontStyle.Bold, priceColor);
            var priceRt = (RectTransform)priceText.transform;
            priceRt.anchorMin = new Vector2(0, 0);
            priceRt.anchorMax = new Vector2(1, 0);
            priceRt.sizeDelta = new Vector2(-10, 30);
            priceRt.anchoredPosition = new Vector2(5, 10);
            priceText.alignment = TextAnchor.MiddleLeft;

            // 购买按钮
            var buyBtn = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image));
            buyBtn.transform.SetParent(cardRt, false);
            var buyBtnRt = buyBtn.GetComponent<RectTransform>();
            buyBtnRt.anchorMin = new Vector2(1, 0);
            buyBtnRt.anchorMax = new Vector2(1, 0);
            buyBtnRt.sizeDelta = new Vector2(60, 24);
            buyBtnRt.anchoredPosition = new Vector2(-8, 10);
            var buyImg = buyBtn.GetComponent<Image>();
            buyImg.color = ColorBtnBuy;
            var buyBtnComp = buyBtn.AddComponent<Button>();
            buyBtnComp.targetGraphic = buyImg;
            var capturedName = data.Name;
            buyBtnComp.onClick.AddListener(() =>
            {
                Debug.Log($"[Shop] 购买: {capturedName}");
                ShopManager.Instance?.BuyItem(0);
            });

            var buyText = new GameObject("Text", typeof(RectTransform));
            buyText.transform.SetParent(buyBtnRt, false);
            var buyTextRt = buyText.GetComponent<RectTransform>();
            buyTextRt.anchorMin = Vector2.zero; buyTextRt.anchorMax = Vector2.one;
            buyTextRt.sizeDelta = Vector2.zero;
            var buyTxt = buyText.AddComponent<Text>();
            buyTxt.text = "购买";
            buyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buyTxt.fontSize = 13;
            buyTxt.alignment = TextAnchor.MiddleCenter;
            buyTxt.color = Color.white;

            _gridItems.Add(item);
        }

        // ===== 6. 底部栏（月卡信息+充值入口） =====
        private void BuildBottomBar()
        {
            var bottomBar = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bottomBar.transform.SetParent(transform, false);
            var bottomRt = bottomBar.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0.5f, 0);
            bottomRt.anchorMax = new Vector2(0.5f, 0);
            bottomRt.sizeDelta = new Vector2(700, 60);
            bottomRt.anchoredPosition = new Vector2(0, 15);
            bottomBar.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.18f, 0.85f);

            // 月卡信息
            _monthlyText = CreateTextWithParent(bottomRt, "MonthlyInfo", "月卡剩余: 0天  |  每日领取: 60通宝+120体力", 15, FontStyle.Normal, ColorTextDim);
            var monthlyRt = (RectTransform)_monthlyText.transform;
            monthlyRt.anchorMin = new Vector2(0, 0.5f);
            monthlyRt.anchorMax = new Vector2(0, 0.5f);
            monthlyRt.sizeDelta = new Vector2(400, 30);
            monthlyRt.anchoredPosition = new Vector2(20, 0);
            _monthlyText.alignment = TextAnchor.MiddleLeft;

            // 领取按钮
            var claimBtn = CreateButton(bottomRt, "ClaimBtn", "领取", () =>
            {
                Debug.Log("[Shop] 领取月卡奖励");
                ShopManager.Instance?.BuyItem(0);
            });
            var claimRt = (RectTransform)claimBtn.transform;
            claimRt.anchorMin = new Vector2(1, 0.5f);
            claimRt.anchorMax = new Vector2(1, 0.5f);
            claimRt.anchoredPosition = new Vector2(-10, 0);
            claimRt.sizeDelta = new Vector2(80, 34);
        }

        // ===== 7. 关闭按钮 =====
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
