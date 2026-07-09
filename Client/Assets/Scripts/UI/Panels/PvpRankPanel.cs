#nullable disable
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// PVP娈典綅鎺掕姒滈潰鏉?- 鍓?0鍚嶇帺瀹舵帓鍚嶅睍绀?    /// 鏆楅粦绱壊涓婚,鍏ㄧ▼搴忓寲鐢熸垚
    /// </summary>
    public class PvpRankPanel : BasePanel
    {
        // ===== 閰嶈壊 =====
        private static readonly Color ColorBg = new Color(0.04f, 0.04f, 0.08f, 0.92f);
        private static readonly Color ColorCard = new Color(0.102f, 0.086f, 0.070f, 0.95f);
        private static readonly Color ColorAccent = new Color(0.83f, 0.66f, 0.26f, 0.9f);
        private static readonly Color ColorGold = new Color(1f, 0.85f, 0.2f);
        private static readonly Color ColorSilver = new Color(0.7f, 0.7f, 0.8f);
        private static readonly Color ColorBronze = new Color(0.8f, 0.5f, 0.2f);
        private static readonly Color ColorRowEven = new Color(0.1f, 0.1f, 0.2f, 0.5f);
        private static readonly Color ColorRowOdd = new Color(0.16f, 0.14f, 0.12f, 0.5f);
        private static readonly Color ColorMyRow = new Color(0.83f, 0.66f, 0.26f, 0.15f);
        private static readonly Color ColorTextDim = new Color(0.48f, 0.43f, 0.38f);
        private static readonly Color ColorTextBright = new Color(0.94f, 0.91f, 0.85f);
        private static readonly Color ColorHeaderBg = new Color(0.15f, 0.12f, 0.25f, 0.9f);

        private static readonly Color[] TierColors =
        {
            new Color(0.8f, 0.5f, 0.2f),
            new Color(0.7f, 0.7f, 0.8f),
            new Color(1f, 0.85f, 0.2f),
            new Color(0.5f, 0.8f, 1f),
            new Color(0.3f, 1f, 0.8f),
            new Color(1f, 0.4f, 0.6f),
        };

        // ===== UI寮曠敤 =====
        private RectTransform _listContainer;
        private Text _titleText;
        private Button _closeBtn;
        private Text _myRankPosText;

        // 鎺掕鏉＄洰瀵硅薄姹?鏈€澶?0)
        private readonly List<GameObject> _rowPool = new();

        // 褰撳墠鏁版嵁
        private List<RankEntry> _currentEntries = new();

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            Hide(); // 榛樿闅愯棌

            // 浜嬩欢鐩戝惉
            if (PvpManager.Instance != null)
            {
                PvpManager.Instance.OnRankListUpdate += OnRankListUpdate;
            }
        }

        void OnDestroy()
        {
            if (PvpManager.Instance != null)
            {
                PvpManager.Instance.OnRankListUpdate -= OnRankListUpdate;
            }
        }

        private void BuildUI()
        {
            // 鍏ㄥ睆鑳屾櫙
            var bg = CreateImage(transform as RectTransform, "Bg", ColorBg);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            // 涓诲崱鐗?            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(520, 560);
            cardRt.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().color = ColorCard;

            // 杈规
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(cardRt, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(-4, -4);
            border.GetComponent<Image>().color = ColorAccent;

            // 鏍囬鏍?            var headerBar = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            headerBar.transform.SetParent(cardRt, false);
            var headerRt = headerBar.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.sizeDelta = new Vector2(0, 56);
            headerRt.anchoredPosition = new Vector2(0, -28);
            headerBar.GetComponent<Image>().color = ColorHeaderBg;

            // 搴曢儴瑁呴グ绾?            var headerLine = new GameObject("HeaderLine", typeof(RectTransform), typeof(Image));
            headerLine.transform.SetParent(headerRt, false);
            headerLine.GetComponent<Image>().color = ColorAccent;
            var hlRt = headerLine.GetComponent<RectTransform>();
            hlRt.anchorMin = new Vector2(0, 0);
            hlRt.anchorMax = new Vector2(1, 0);
            hlRt.sizeDelta = new Vector2(0, 2);
            hlRt.anchoredPosition = Vector2.zero;

            // 鏍囬
            _titleText = CreateText(headerRt, "Title", "馃弳 娈典綅鎺掕姒?, 28);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = ColorGold;
            _titleText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            _titleText.rectTransform.anchorMax = new Vector2(0.5f, 1);
            _titleText.rectTransform.sizeDelta = new Vector2(300, 0);

            // 鍏抽棴鎸夐挳
            _closeBtn = CreateButton(headerRt, "CloseBtn", "鉁?, () => Hide());
            var closeRt = _closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 0.5f);
            closeRt.anchorMax = new Vector2(1, 0.5f);
            closeRt.sizeDelta = new Vector2(44, 44);
            closeRt.anchoredPosition = new Vector2(-22, 0);
            _closeBtn.GetComponent<Image>().color = new Color(0.4f, 0.15f, 0.15f, 0.8f);

            // 鍒楁爣棰?鎺掑悕/鍚嶅瓧/娈典綅/绉垎/鑳滅巼)
            BuildColumnHeaders(cardRt);

            // ScrollView瀹瑰櫒
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
            scrollGo.transform.SetParent(cardRt, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.sizeDelta = new Vector2(-16, -90);
            scrollRt.anchoredPosition = new Vector2(0, -10);

            // ScrollRect
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // 鍐呭瀹瑰櫒(鑷姩鎵╁睍)
            _listContainer = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _listContainer.SetParent(scrollRt, false);
            _listContainer.anchorMin = new Vector2(0, 1);
            _listContainer.anchorMax = new Vector2(1, 1);
            _listContainer.sizeDelta = new Vector2(0, 0);
            _listContainer.anchoredPosition = Vector2.zero;

            scrollRect.content = _listContainer;

            // 鎴戠殑鎺掑悕淇℃伅(搴曢儴)
            var bottomBar = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bottomBar.transform.SetParent(cardRt, false);
            var bottomRt = bottomBar.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0);
            bottomRt.sizeDelta = new Vector2(0, 44);
            bottomRt.anchoredPosition = new Vector2(0, 0);
            bottomBar.GetComponent<Image>().color = ColorHeaderBg;

            _myRankPosText = CreateText(bottomRt, "MyRank", "鎴戠殑鎺掑悕: 鏈笂姒?, 18);
            _myRankPosText.color = ColorTextBright;
            _myRankPosText.alignment = TextAnchor.MiddleLeft;
            _myRankPosText.rectTransform.anchorMin = new Vector2(0, 0);
            _myRankPosText.rectTransform.anchorMax = Vector2.one;
            _myRankPosText.rectTransform.sizeDelta = new Vector2(-24, 0);
            _myRankPosText.rectTransform.anchoredPosition = new Vector2(12, 0);
        }

        private void BuildColumnHeaders(RectTransform parent)
        {
            string[] headers = { "鎺掑悕", "鐜╁", "娈典綅", "绉垎", "鑳滅巼" };
            float[] widths = { 60, 160, 90, 80, 80 };
            float startX = 16;

            for (int i = 0; i < headers.Length; i++)
            {
                var hText = CreateText(parent, "Hdr" + i, headers[i], 16);
                hText.color = ColorAccent;
                hText.fontStyle = FontStyle.Bold;
                var hRt = hText.rectTransform;
                hRt.anchorMin = new Vector2(0, 1);
                hRt.anchorMax = new Vector2(0, 1);
                hRt.sizeDelta = new Vector2(widths[i], 26);
                hRt.anchoredPosition = new Vector2(startX + widths[i] / 2, -70);

                // 瀵归綈
                hText.alignment = i == 0 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                startX += widths[i];
            }
        }

        private void OnRankListUpdate(List<RankEntry> entries)
        {
            _currentEntries = entries;
            PopulateList();
        }

        private void PopulateList()
        {
            // 娓呴櫎鏃ц
            foreach (var row in _rowPool)
            {
                Destroy(row);
            }
            _rowPool.Clear();

            ulong myId = GameManager.Instance.Player.PlayerId;
            int myRankPos = -1;

            for (int i = 0; i < _currentEntries.Count && i < 50; i++)
            {
                var entry = _currentEntries[i];
                bool isMe = entry.PlayerId == myId;
                if (isMe) myRankPos = entry.Position;

                var row = BuildRow(i, entry, isMe);
                _rowPool.Add(row);
            }

            // 鏇存柊瀹瑰櫒楂樺害
            float height = _currentEntries.Count * 48f;
            if (height < 400) height = 400;
            _listContainer.sizeDelta = new Vector2(0, height);

            // 鎴戠殑鎺掑悕
            if (myRankPos > 0)
            {
                var rank = PvpManager.Instance.MyRank;
                _myRankPosText.text = $"鎴戠殑鎺掑悕: 绗?{myRankPos} 鍚?| " +
                    $"{rank.TierName} {rank.Points}鍒?| " +
                    $"{rank.Wins}鑳?{rank.Losses}璐?;
            }
            else
            {
                _myRankPosText.text = "鎴戠殑鎺掑悕: 鏈笂姒?;
            }
        }

        private GameObject BuildRow(int index, RankEntry entry, bool isMe)
        {
            var row = new GameObject("Row" + index, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_listContainer, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1);
            rowRt.anchorMax = new Vector2(1, 1);
            rowRt.sizeDelta = new Vector2(0, 48);
            rowRt.anchoredPosition = new Vector2(0, -index * 48);

            // 琛岃儗鏅?            var rowImg = row.GetComponent<Image>();
            rowImg.color = isMe ? ColorMyRow : (index % 2 == 0 ? ColorRowEven : ColorRowOdd);

            float[] widths = { 60, 160, 90, 80, 80 };
            float startX = 16;

            // 鎺掑悕(甯﹀鐗岄鏍?
            string rankStr = index switch
            {
                0 => "馃",
                1 => "馃",
                2 => "馃",
                _ => $"#{entry.Position}"
            };
            var rankColor = index switch
            {
                0 => ColorGold,
                1 => ColorSilver,
                2 => ColorBronze,
                _ => ColorTextDim
            };

            var rankText = CreateText(rowRt, "Rank", rankStr, index < 3 ? 22 : 16);
            rankText.color = rankColor;
            rankText.alignment = TextAnchor.MiddleCenter;
            rankText.rectTransform.anchorMin = new Vector2(0, 0);
            rankText.rectTransform.anchorMax = new Vector2(0, 1);
            rankText.rectTransform.sizeDelta = new Vector2(widths[0], 0);
            rankText.rectTransform.anchoredPosition = new Vector2(widths[0] / 2 + startX, 0);
            rankText.fontStyle = index < 3 ? FontStyle.Bold : FontStyle.Normal;

            // 鐜╁鍚?            float nameX = startX + widths[0];
            var nameText = CreateText(rowRt, "Name", entry.Name, 18);
            nameText.color = isMe ? ColorAccent : ColorTextBright;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.rectTransform.anchorMin = new Vector2(0, 0);
            nameText.rectTransform.anchorMax = new Vector2(0, 1);
            nameText.rectTransform.sizeDelta = new Vector2(widths[1], 0);
            nameText.rectTransform.anchoredPosition = new Vector2(nameX + widths[1] / 2, 0);
            if (isMe) nameText.fontStyle = FontStyle.Bold;

            // 娈典綅
            float tierX = nameX + widths[1];
            var tierText = CreateText(rowRt, "Tier", GetTierShortName(entry.Tier), 18);
            tierText.color = TierColors[(int)entry.Tier];
            tierText.alignment = TextAnchor.MiddleLeft;
            tierText.rectTransform.anchorMin = new Vector2(0, 0);
            tierText.rectTransform.anchorMax = new Vector2(0, 1);
            tierText.rectTransform.sizeDelta = new Vector2(widths[2], 0);
            tierText.rectTransform.anchoredPosition = new Vector2(tierX + widths[2] / 2, 0);

            // 绉垎
            float ptsX = tierX + widths[2];
            var ptsText = CreateText(rowRt, "Points", entry.Points.ToString(), 18);
            ptsText.color = ColorTextBright;
            ptsText.alignment = TextAnchor.MiddleLeft;
            ptsText.rectTransform.anchorMin = new Vector2(0, 0);
            ptsText.rectTransform.anchorMax = new Vector2(0, 1);
            ptsText.rectTransform.sizeDelta = new Vector2(widths[3], 0);
            ptsText.rectTransform.anchoredPosition = new Vector2(ptsX + widths[3] / 2, 0);

            // 鑳滅巼
            float wrX = ptsX + widths[3];
            var wrText = CreateText(rowRt, "WinRate", (entry.WinRate * 100).ToString("F1") + "%", 18);
            wrText.color = entry.WinRate >= 0.6f ? new Color(0.3f, 1f, 0.3f) :
                          entry.WinRate >= 0.4f ? ColorTextBright : new Color(1f, 0.3f, 0.3f);
            wrText.alignment = TextAnchor.MiddleLeft;
            wrText.rectTransform.anchorMin = new Vector2(0, 0);
            wrText.rectTransform.anchorMax = new Vector2(0, 1);
            wrText.rectTransform.sizeDelta = new Vector2(widths[4], 0);
            wrText.rectTransform.anchoredPosition = new Vector2(wrX + widths[4] / 2, 0);

            // 鍒嗗壊绾?            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(rowRt, false);
            var divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0, 0);
            divRt.anchorMax = new Vector2(1, 0);
            divRt.sizeDelta = new Vector2(-16, 1);
            divRt.anchoredPosition = new Vector2(0, 0);
            divider.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);

            return row;
        }

        private string GetTierShortName(RankTier tier)
        {
            return tier switch
            {
                RankTier.Bronze => "闈掗摐",
                RankTier.Silver => "鐧介摱",
                RankTier.Gold => "榛勯噾",
                RankTier.Platinum => "閾傞噾",
                RankTier.Diamond => "閽荤煶",
                RankTier.Legendary => "浼犺",
                _ => "?"
            };
        }

        public override void Refresh()
        {
            base.Refresh();
            if (_currentEntries.Count > 0)
                PopulateList();
            PvpManager.Instance?.RequestRankInfo();
        }
    }
}