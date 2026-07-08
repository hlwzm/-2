#nullable disable
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;

using System.Collections;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// PVP匹配成功弹窗 - 双方信息展示/确认取消/5秒倒计时
    /// 自动弹出,全程序化生成
    /// </summary>
    public class PvpMatchPanel : BasePanel
    {
        // ===== 配色 =====
        private static readonly Color ColorOverlay = new Color(0, 0, 0, 0.7f);
        private static readonly Color ColorCard = new Color(0.07f, 0.07f, 0.14f, 0.95f);
        private static readonly Color ColorAccent = new Color(0.69f, 0.53f, 1f, 0.9f);
        private static readonly Color ColorGold = new Color(1f, 0.85f, 0.2f);
        private static readonly Color ColorGreen = new Color(0.3f, 1f, 0.3f);
        private static readonly Color ColorRed = new Color(1f, 0.3f, 0.3f);
        private static readonly Color ColorTextDim = new Color(0.5f, 0.5f, 0.65f);
        private static readonly Color ColorTextBright = new Color(0.85f, 0.85f, 0.92f);
        private static readonly Color ColorBtnConfirm = new Color(0.3f, 0.7f, 0.3f, 0.85f);
        private static readonly Color ColorBtnCancel = new Color(0.5f, 0.15f, 0.15f, 0.85f);
        private static readonly Color ColorPlayerCard = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        private static readonly Color ColorVs = new Color(1f, 0.85f, 0.2f, 0.9f);

        // ===== 段位颜色 =====
        private static readonly Color[] TierColors =
        {
            new Color(0.8f, 0.5f, 0.2f),
            new Color(0.7f, 0.7f, 0.8f),
            new Color(1f, 0.85f, 0.2f),
            new Color(0.5f, 0.8f, 1f),
            new Color(0.3f, 1f, 0.8f),
            new Color(1f, 0.4f, 0.6f),
        };

        // ===== UI引用 =====
        private Text _titleText;
        private Text _countdownText;

        // 我方信息
        private Text _myNameText;
        private Text _myRankText;
        private Text _myClassText;
        private Image _myCardBg;

        // 对方信息
        private Text _opponentNameText;
        private Text _opponentRankText;
        private Text _opponentClassText;
        private Image _opponentCardBg;

        private Text _vsText;
        private Button _confirmBtn;
        private Text _confirmBtnText;
        private Button _cancelBtn;

        private GameObject _cardContainer;

        // 倒计时
        private bool _countingDown;
        private float _countdownTimer;

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            Hide(); // 默认隐藏
        }

        void OnEnable()
        {
            // 监听PVP事件
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
        // UI构建
        // =====================================================================
        private void BuildUI()
        {
            // 全屏遮罩
            var overlay = CreateImage(transform as RectTransform, "Overlay", ColorOverlay);
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.sizeDelta = Vector2.zero;

            // 主卡片容器
            _cardContainer = new GameObject("CardContainer", typeof(RectTransform));
            _cardContainer.transform.SetParent(transform, false);
            var containerRt = _cardContainer.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(560, 400);
            containerRt.anchoredPosition = Vector2.zero;

            // 卡片背景
            var cardBg = _cardContainer.AddComponent<Image>();
            cardBg.color = ColorCard;

            // 边框
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(containerRt, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(-4, -4);
            border.GetComponent<Image>().color = ColorAccent;

            // 标题
            _titleText = CreateText(containerRt, "Title", "⚔ 匹配成功 ⚔", 34);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = ColorGold;
            _titleText.rectTransform.anchorMin = new Vector2(0.5f, 1);
            _titleText.rectTransform.anchorMax = new Vector2(0.5f, 1);
            _titleText.rectTransform.sizeDelta = new Vector2(400, 50);
            _titleText.rectTransform.anchoredPosition = new Vector2(0, -30);

            // ---- 两侧选手展示 ----
            // 我方卡片(左侧)
            _myCardBg = CreateImage(containerRt, "MyCard", ColorPlayerCard);
            _myCardBg.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myCardBg.rectTransform.anchorMax = new Vector2(0, 0.5f);
            _myCardBg.rectTransform.sizeDelta = new Vector2(200, 180);
            _myCardBg.rectTransform.anchoredPosition = new Vector2(125, 0);

            // 我方卡片边框
            var myBorder = new GameObject("MyBorder", typeof(RectTransform), typeof(Image));
            myBorder.transform.SetParent(_myCardBg.transform, false);
            var myBorderRt = myBorder.GetComponent<RectTransform>();
            myBorderRt.anchorMin = Vector2.zero;
            myBorderRt.anchorMax = Vector2.one;
            myBorderRt.sizeDelta = new Vector2(-4, -4);
            myBorder.GetComponent<Image>().color = new Color(0.3f, 0.7f, 0.3f, 0.5f);

            // "我方" 标签
            var myLabel = CreateText(_myCardBg.rectTransform, "MyLabel", "▶ 我方 ◀", 18);
            myLabel.color = ColorGreen;
            myLabel.fontStyle = FontStyle.Bold;
            myLabel.rectTransform.anchorMin = new Vector2(0, 1);
            myLabel.rectTransform.anchorMax = new Vector2(1, 1);
            myLabel.rectTransform.sizeDelta = new Vector2(0, 30);
            myLabel.rectTransform.anchoredPosition = new Vector2(0, -15);

            // 我方名称
            _myNameText = CreateText(_myCardBg.rectTransform, "MyName", "我", 24);
            _myNameText.fontStyle = FontStyle.Bold;
            _myNameText.color = ColorTextBright;
            _myNameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myNameText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _myNameText.rectTransform.sizeDelta = new Vector2(-10, 32);
            _myNameText.rectTransform.anchoredPosition = new Vector2(0, 30);

            // 我方段位
            _myRankText = CreateText(_myCardBg.rectTransform, "MyRank", "青铜 1000分", 18);
            _myRankText.color = ColorTextDim;
            _myRankText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myRankText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _myRankText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _myRankText.rectTransform.anchoredPosition = new Vector2(0, -2);

            // 我方门派
            _myClassText = CreateText(_myCardBg.rectTransform, "MyClass", "", 18);
            _myClassText.color = new Color(0.5f, 0.8f, 1f);
            _myClassText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _myClassText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _myClassText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _myClassText.rectTransform.anchoredPosition = new Vector2(0, -34);

            // VS 文字(中间)
            _vsText = CreateText(containerRt, "VsText", "VS", 40);
            _vsText.fontStyle = FontStyle.Bold;
            _vsText.color = ColorVs;
            _vsText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _vsText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _vsText.rectTransform.sizeDelta = new Vector2(80, 50);
            _vsText.rectTransform.anchoredPosition = new Vector2(0, 0);

            // 对方卡片(右侧)
            _opponentCardBg = CreateImage(containerRt, "OpponentCard", ColorPlayerCard);
            _opponentCardBg.rectTransform.anchorMin = new Vector2(1, 0.5f);
            _opponentCardBg.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentCardBg.rectTransform.sizeDelta = new Vector2(200, 180);
            _opponentCardBg.rectTransform.anchoredPosition = new Vector2(-125, 0);

            // 对方卡片边框
            var oppBorder = new GameObject("OppBorder", typeof(RectTransform), typeof(Image));
            oppBorder.transform.SetParent(_opponentCardBg.transform, false);
            var oppBorderRt = oppBorder.GetComponent<RectTransform>();
            oppBorderRt.anchorMin = Vector2.zero;
            oppBorderRt.anchorMax = Vector2.one;
            oppBorderRt.sizeDelta = new Vector2(-4, -4);
            oppBorder.GetComponent<Image>().color = new Color(0.7f, 0.3f, 0.3f, 0.5f);

            // "对方" 标签
            var oppLabel = CreateText(_opponentCardBg.rectTransform, "OppLabel", "◀ 对方 ▶", 18);
            oppLabel.color = ColorRed;
            oppLabel.fontStyle = FontStyle.Bold;
            oppLabel.rectTransform.anchorMin = new Vector2(0, 1);
            oppLabel.rectTransform.anchorMax = new Vector2(1, 1);
            oppLabel.rectTransform.sizeDelta = new Vector2(0, 30);
            oppLabel.rectTransform.anchoredPosition = new Vector2(0, -15);

            // 对方名称
            _opponentNameText = CreateText(_opponentCardBg.rectTransform, "OppName", "对手", 24);
            _opponentNameText.fontStyle = FontStyle.Bold;
            _opponentNameText.color = ColorTextBright;
            _opponentNameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _opponentNameText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentNameText.rectTransform.sizeDelta = new Vector2(-10, 32);
            _opponentNameText.rectTransform.anchoredPosition = new Vector2(0, 30);

            // 对方段位
            _opponentRankText = CreateText(_opponentCardBg.rectTransform, "OppRank", "青铜 1000分", 18);
            _opponentRankText.color = ColorTextDim;
            _opponentRankText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _opponentRankText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentRankText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _opponentRankText.rectTransform.anchoredPosition = new Vector2(0, -2);

            // 对方门派
            _opponentClassText = CreateText(_opponentCardBg.rectTransform, "OppClass", "", 18);
            _opponentClassText.color = new Color(0.5f, 0.8f, 1f);
            _opponentClassText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _opponentClassText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _opponentClassText.rectTransform.sizeDelta = new Vector2(-10, 28);
            _opponentClassText.rectTransform.anchoredPosition = new Vector2(0, -34);

            // ---- 倒计时 ----
            _countdownText = CreateText(containerRt, "Countdown", "5", 60);
            _countdownText.fontStyle = FontStyle.Bold;
            _countdownText.color = ColorGold;
            _countdownText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _countdownText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _countdownText.rectTransform.sizeDelta = new Vector2(100, 80);
            _countdownText.rectTransform.anchoredPosition = new Vector2(0, -100);
            _countdownText.gameObject.SetActive(false);

            // 提示文字
            var hintText = CreateText(containerRt, "Hint", "等待对方确认中...", 18);
            hintText.color = ColorTextDim;
            hintText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hintText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hintText.rectTransform.sizeDelta = new Vector2(300, 30);
            hintText.rectTransform.anchoredPosition = new Vector2(0, -125);

            // ---- 确认/取消按钮 ----
            _confirmBtn = CreateButton(containerRt, "ConfirmBtn", "✓ 确认", () =>
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

            _cancelBtn = CreateButton(containerRt, "CancelBtn", "✗ 取消", () =>
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
        // 事件处理
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
            // 倒计时脉冲效果
            float scale = 1f + (5 - seconds) * 0.05f;
            _countdownText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _countdownText.color = seconds <= 3 ? Color.red : ColorGold;
            _confirmBtnText.text = $"✓ 确认 ({seconds}s)";
        }

        private void OnMatchStateChanged(MatchState prev, MatchState cur)
        {
            if (cur == MatchState.InGame)
            {
                Hide();
            }
            else if (cur == MatchState.Idle && prev == MatchState.MatchFound)
            {
                // 匹配被取消/拒绝
                _countdownText.gameObject.SetActive(false);
                _countdownText.text = "5";
                _confirmBtnText.text = "✓ 确认";
            }
        }

        private void PopulateData(PvpPlayerData opponent)
        {
            // 我方信息
            var player = GameManager.Instance.Player;
            var rank = PvpManager.Instance.MyRank;
            _myNameText.text = player.Name;
            _myRankText.text = $"{rank.TierName} {rank.Points}分";
            _myRankText.color = TierColors[(int)rank.Tier];
            _myClassText.text = GetPlayerClassName(player.PlayerId);

            // 对方信息
            _opponentNameText.text = opponent.Name;
            _opponentRankText.text = $"{opponent.Rank.TierName} {opponent.Rank.Points}分";
            _opponentRankText.color = TierColors[(int)opponent.Rank.Tier];
            _opponentClassText.text = opponent.ClassName;

            // VS闪烁动画变量
            _countingDown = false;
            _countdownText.gameObject.SetActive(false);
            _countdownText.text = "5";
            _confirmBtnText.text = "✓ 确认";
            _cardContainer.transform.localScale = Vector3.one;

            // 入场动画
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
        // 辅助方法
        // =====================================================================

        private string GetPlayerClassName(ulong playerId)
        {
            // 从GameManager获取当前英雄的门派
            if (GameManager.Instance != null && GameManager.Instance.Heroes.Count > 0)
            {
                var heroId = (int)GameManager.Instance.Heroes[0].TemplateId;
                var cfg = HeroConfig.Get((int)heroId);
                if (cfg != null) return cfg.name;
            }
            return "未知";
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