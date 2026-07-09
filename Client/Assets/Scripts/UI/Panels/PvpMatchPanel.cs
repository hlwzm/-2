#nullable disable
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;

using System.Collections;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// PVP鍖归厤鎴愬姛寮圭獥 - 鍙屾柟淇℃伅灞曠ず/纭鍙栨秷/5绉掑€掕鏃?    /// 鑷姩寮瑰嚭,鍏ㄧ▼搴忓寲鐢熸垚
    /// </summary>
    public class PvpMatchPanel : BasePanel
    {
        // ===== 閰嶈壊 =====
        private static readonly Color ColorOverlay = new Color(0, 0, 0, 0.7f);
        private static readonly Color ColorCard = new Color(0.07f, 0.07f, 0.14f, 0.95f);
        private static readonly Color ColorAccent = new Color(0.83f, 0.66f, 0.26f, 0.9f);
        private static readonly Color ColorGold = new Color(1f, 0.85f, 0.2f);
        private static readonly Color ColorGreen = new Color(0.3f, 1f, 0.3f);
        private static readonly Color ColorRed = new Color(1f, 0.3f, 0.3f);
        private static readonly Color ColorTextDim = new Color(0.48f, 0.43f, 0.38f);
        private static readonly Color ColorTextBright = new Color(0.85f, 0.85f, 0.92f);
        private static readonly Color ColorBtnConfirm = new Color(0.3f, 0.7f, 0.3f, 0.85f);
        private static readonly Color ColorBtnCancel = new Color(0.5f, 0.15f, 0.15f, 0.85f);
        private static readonly Color ColorPlayerCard = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        private static readonly Color ColorVs = new Color(1f, 0.85f, 0.2f, 0.9f);

        // ===== 娈典綅棰滆壊 =====
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
        private Text _titleText;
        private Text _countdownText;

        // 鎴戞柟淇℃伅
        private Text _myNameText;
        private Text _myRankText;
        private Text _myClassText;
        private Image _myCardBg;

        // 瀵规柟淇℃伅
        private Text _opponentNameText;
        private Text _opponentRankText;
        private Text _opponentClassText;
        private Image _opponentCardBg;

        private Text _vsText;
        private Button _confirmBtn;
        private Text _confirmBtnText;
        private Button _cancelBtn;

        private GameObject _cardContainer;

        // 鍊掕鏃?        private bool _countingDown;
        private float _countdownTimer;

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            Hide(); // 榛樿闅愯棌
        }

        void OnEnable()
        {
            // 鐩戝惉PVP浜嬩欢
            if (PvpManager.Instance != null)
            {
                PvpManager.Instance.OnMatchFound += OnMatchFound;
                PvpManager.Instance.OnCountdownTick += OnCountdownTick;
                PvpManager.Instance.OnMatchStateChanged += OnMatchStateChanged;
            }
        }

        void OnDisable()
        {
            if (PvpManager.Instance != null)
            {
                PvpManager.Instance.OnMatchFound -= OnMatchFound;
                PvpManager.Instance.OnCountdownTick -= OnCountdownTick;
                PvpManager.Instance.OnMatchStateChanged -= OnMatchStateChanged;
            }
        }

        // =====================================================================
        // UI鏋勫缓
        // =====================================================================
        private void BuildUI()
        {
            // 鍏ㄥ睆閬僵
            var overlay = CreateImage(transform as RectTransform, "Overlay", ColorOverlay);
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.sizeDelta = Vector2.zero;

            // 涓诲崱鐗囧鍣?            _cardContainer = new GameObject("CardContainer", typeof(RectTransform));
            _cardContainer.transform.SetParent(transform, false);
            var containerRt = _cardContainer.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(560, 400);
            containerRt.anchoredPosition = Vector2.zero;

            // 鍗＄墖鑳屾櫙
            var cardBg = _cardContainer.AddComponent<Image>();
            cardBg.color = ColorCard;

            // 杈规
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(containerRt, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(-4, -4);
            border.GetComponent<Image>().color = ColorAccent;

            // 鏍囬
            _titleText = CreateText(containerRt, "Title", "鈿?鍖归厤鎴愬姛 鈿?, 34);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = ColorGold;
            _titleText.rectTransform.anchorMin = new Vector2(0.5f, 1);
            _titleText.rectTransform.anchorMax = new Vector2(0.5f, 1);
            _titleText.rectTransform.sizeDelta = new Vector2(400, 50);
            _titleText.rectTransform.anchoredPosition = new Vector2(0, -30);

            // ---- 涓や晶閫夋墜灞曠ず ----
            // 鎴戞柟鍗＄墖(宸︿晶)
            _myCardBg = CreateImage(containerRt, "MyCard", ColorPlayerCard);
            _myCardBg.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myCardBg.rectTransform.anchorMax = new Vector2(0, 0.5f);
            _myCardBg.rectTransform.sizeDelta = new Vector2(200, 180);
            _myCardBg.rectTransform.anchoredPosition = new Vector2(125, 0);

            // 鎴戞柟鍗＄墖杈规
            var myBorder = new GameObject("MyBorder", typeof(RectTransform), typeof(Image));
            myBorder.transform.SetParent(_myCardBg.transform, false);
            var myBorderRt = myBorder.GetComponent<RectTransform>();
            myBorderRt.anchorMin = Vector2.zero;
            myBorderRt.anchorMax = Vector2.one;
            myBorderRt.sizeDelta = new Vector2(-4, -4);
            myBorder.GetComponent<Image>().color = new Color(0.3f, 0.7f, 0.3f, 0.5f);

            // "鎴戞柟" 鏍囩
            var myLabel = CreateText(_myCardBg.rectTransform, "MyLabel", "鈻?鎴戞柟 鈼€", 18);
            myLabel.color = ColorGreen;
            myLabel.fontStyle = FontStyle.Bold;
            myLabel.rectTransform.anchorMin = new Vector2(0, 1);
            myLabel.rectTransform.anchorMax = new Vector2(1, 1);
            myLabel.rectTransform.sizeDelta = new Vector2(0, 30);
            myLabel.rectTransform.anchoredPosition = new Vector2(0, -15);

            // 鎴戞柟鍚嶇О
            _myNameText = CreateText(_myCardBg.rectTransform, "MyName", "鎴?, 24);
            _myNameText.fontStyle = FontStyle.Bold;
            _myNameText.color = ColorTextBright;
            _myNameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myNameText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _myNameText.rectTransform.sizeDelta = new Vector2(-10, 32);
            _myNameText.rectTransform.anchoredPosition = new Vector2(0, 30);

            // 鎴戞柟娈典綅
            _myRankText = CreateText(_myCardBg.rectTransform, "MyRank", "闈掗摐 1000鍒?, 18);
            _myRankText.color = ColorTextDim;
            _myRankText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myRankText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _myRankText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _myRankText.rectTransform.anchoredPosition = new Vector2(0, -2);

            // 鎴戞柟闂ㄦ淳
            _myClassText = CreateText(_myCardBg.rectTransform, "MyClass", "", 18);
            _myClassText.color = new Color(0.5f, 0.8f, 1f);
            _myClassText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myClassText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _myClassText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _myClassText.rectTransform.anchoredPosition = new Vector2(0, -34);

            // VS 鏂囧瓧(涓棿)
            _vsText = CreateText(containerRt, "VsText", "VS", 40);
            _vsText.fontStyle = FontStyle.Bold;
            _vsText.color = ColorVs;
            _vsText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _vsText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _vsText.rectTransform.sizeDelta = new Vector2(80, 50);
            _vsText.rectTransform.anchoredPosition = new Vector2(0, 0);

            // 瀵规柟鍗＄墖(鍙充晶)
            _opponentCardBg = CreateImage(containerRt, "OpponentCard", ColorPlayerCard);
            _opponentCardBg.rectTransform.anchorMin = new Vector2(1, 0.5f);
            _opponentCardBg.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentCardBg.rectTransform.sizeDelta = new Vector2(200, 180);
            _opponentCardBg.rectTransform.anchoredPosition = new Vector2(-125, 0);

            // 瀵规柟鍗＄墖杈规
            var oppBorder = new GameObject("OppBorder", typeof(RectTransform), typeof(Image));
            oppBorder.transform.SetParent(_opponentCardBg.transform, false);
            var oppBorderRt = oppBorder.GetComponent<RectTransform>();
            oppBorderRt.anchorMin = Vector2.zero;
            oppBorderRt.anchorMax = Vector2.one;
            oppBorderRt.sizeDelta = new Vector2(-4, -4);
            oppBorder.GetComponent<Image>().color = new Color(0.7f, 0.3f, 0.3f, 0.5f);

            // "瀵规柟" 鏍囩
            var oppLabel = CreateText(_opponentCardBg.rectTransform, "OppLabel", "鈼€ 瀵规柟 鈻?, 18);
            oppLabel.color = ColorRed;
            oppLabel.fontStyle = FontStyle.Bold;
            oppLabel.rectTransform.anchorMin = new Vector2(0, 1);
            oppLabel.rectTransform.anchorMax = new Vector2(1, 1);
            oppLabel.rectTransform.sizeDelta = new Vector2(0, 30);
            oppLabel.rectTransform.anchoredPosition = new Vector2(0, -15);

            // 瀵规柟鍚嶇О
            _opponentNameText = CreateText(_opponentCardBg.rectTransform, "OppName", "瀵规墜", 24);
            _opponentNameText.fontStyle = FontStyle.Bold;
            _opponentNameText.color = ColorTextBright;
            _opponentNameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _opponentNameText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentNameText.rectTransform.sizeDelta = new Vector2(-10, 32);
            _opponentNameText.rectTransform.anchoredPosition = new Vector2(0, 30);

            // 瀵规柟娈典綅
            _opponentRankText = CreateText(_opponentCardBg.rectTransform, "OppRank", "闈掗摐 1000鍒?, 18);
            _opponentRankText.color = ColorTextDim;
            _opponentRankText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _opponentRankText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentRankText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _opponentRankText.rectTransform.anchoredPosition = new Vector2(0, -2);

            // 瀵规柟闂ㄦ淳
            _opponentClassText = CreateText(_opponentCardBg.rectTransform, "OppClass", "", 18);
            _opponentClassText.color = new Color(0.5f, 0.8f, 1f);
            _opponentClassText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _opponentClassText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentClassText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _opponentClassText.rectTransform.anchoredPosition = new Vector2(0, -34);

            // ---- 鍊掕鏃?----
            _countdownText = CreateText(containerRt, "Countdown", "5", 60);
            _countdownText.fontStyle = FontStyle.Bold;
            _countdownText.color = ColorGold;
            _countdownText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _countdownText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _countdownText.rectTransform.sizeDelta = new Vector2(100, 80);
            _countdownText.rectTransform.anchoredPosition = new Vector2(0, -100);
            _countdownText.gameObject.SetActive(false);

            // 鎻愮ず鏂囧瓧
            var hintText = CreateText(containerRt, "Hint", "绛夊緟瀵规柟纭涓?..", 18);
            hintText.color = ColorTextDim;
            hintText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hintText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hintText.rectTransform.sizeDelta = new Vector2(300, 30);
            hintText.rectTransform.anchoredPosition = new Vector2(0, -125);

            // ---- 纭/鍙栨秷鎸夐挳 ----
            _confirmBtn = CreateButton(containerRt, "ConfirmBtn", "鉁?纭", () =>
            {
                if (PvpManager.Instance != null)
                    PvpManager.Instance.AcceptMatch();
            });
            var confirmRt = _confirmBtn.GetComponent<RectTransform>();
            confirmRt.anchorMin = new Vector2(0.5f, 0);
            confirmRt.anchorMax = new Vector2(0.5f, 0);
            confirmRt.sizeDelta = new Vector2(160, 50);
            confirmRt.anchoredPosition = new Vector2(-95, 30);
            _confirmBtn.GetComponent<Image>().color = ColorBtnConfirm;
            _confirmBtnText = confirmRt.GetComponentInChildren<Text>();
            _confirmBtnText.fontSize = 22;
            _confirmBtnText.fontStyle = FontStyle.Bold;

            _cancelBtn = CreateButton(containerRt, "CancelBtn", "鉁?鍙栨秷", () =>
            {
                if (PvpManager.Instance != null)
                    PvpManager.Instance.DeclineMatch();
                Hide();
            });
            var cancelRt = _cancelBtn.GetComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(0.5f, 0);
            cancelRt.anchorMax = new Vector2(0.5f, 0);
            cancelRt.sizeDelta = new Vector2(160, 50);
            cancelRt.anchoredPosition = new Vector2(95, 30);
            _cancelBtn.GetComponent<Image>().color = ColorBtnCancel;
        }

        // =====================================================================
        // 浜嬩欢澶勭悊
        // =====================================================================

        private void OnMatchFound(PvpPlayerData opponent)
        {
            Show();
            PopulateData(opponent);
        }

        private void OnCountdownTick(int seconds)
        {
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = seconds.ToString();
            // 鍊掕鏃惰剦鍐叉晥鏋?            float scale = 1f + (5 - seconds) * 0.05f;
            _countdownText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _countdownText.color = seconds <= 3 ? Color.red : ColorGold;
            _confirmBtnText.text = $"鉁?纭 ({seconds}s)";
        }

        private void OnMatchStateChanged(MatchState prev, MatchState cur)
        {
            if (cur == MatchState.InGame)
            {
                Hide();
            }
            else if (cur == MatchState.Idle && prev == MatchState.MatchFound)
            {
                // 鍖归厤琚彇娑?鎷掔粷
                _countdownText.gameObject.SetActive(false);
                _countdownText.text = "5";
                _confirmBtnText.text = "鉁?纭";
            }
        }

        private void PopulateData(PvpPlayerData opponent)
        {
            // 鎴戞柟淇℃伅
            var player = GameManager.Instance.Player;
            var rank = PvpManager.Instance.MyRank;
            _myNameText.text = player.Name;
            _myRankText.text = $"{rank.TierName} {rank.Points}鍒?;
            _myRankText.color = TierColors[(int)rank.Tier];
            _myClassText.text = GetPlayerClassName(player.PlayerId);

            // 瀵规柟淇℃伅
            _opponentNameText.text = opponent.Name;
            _opponentRankText.text = $"{opponent.Rank.TierName} {opponent.Rank.Points}鍒?;
            _opponentRankText.color = TierColors[(int)opponent.Rank.Tier];
            _opponentClassText.text = opponent.ClassName;

            // VS闂儊鍔ㄧ敾鍙橀噺
            _countingDown = false;
            _countdownText.gameObject.SetActive(false);
            _countdownText.text = "5";
            _confirmBtnText.text = "鉁?纭";
            _cardContainer.transform.localScale = Vector3.one;

            // 鍏ュ満鍔ㄧ敾
            StartCoroutine(EntryAnimation());
        }

        private System.Collections.IEnumerator EntryAnimation()
        {
            _cardContainer.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(0.5f, 1.05f, t / 0.3f);
                _cardContainer.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _cardContainer.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        // =====================================================================
        // 杈呭姪鏂规硶
        // =====================================================================

        private string GetPlayerClassName(ulong playerId)
        {
            // 浠嶨ameManager鑾峰彇褰撳墠鑻遍泟鐨勯棬娲?            if (GameManager.Instance != null && GameManager.Instance.Heroes.Count > 0)
            {
                var heroId = (int)GameManager.Instance.Heroes[0].TemplateId;
                var cfg = HeroConfig.Get((int)heroId);
                if (cfg != null) return cfg.name;
            }
            return "鏈煡";
        }

        public override void Refresh()
        {
            base.Refresh();
            if (PvpManager.Instance != null && PvpManager.Instance.Opponent != null)
            {
                PopulateData(PvpManager.Instance.Opponent);
            }
        }
    }
}