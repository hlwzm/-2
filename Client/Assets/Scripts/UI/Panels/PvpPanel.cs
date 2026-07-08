#nullable disable
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.UI;
using Jx3.Core.Scene;
using System;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// PVP竞技场匹配面板 - 段位显示/模式切换/匹配按钮/匹配动画
    /// 暗黑紫色主题,全程序化生成
    /// </summary>
    public class PvpPanel : BasePanel
    {
        // ===== 配色 =====
        private static readonly Color ColorBg = new Color(0.04f, 0.04f, 0.08f);       // #0a0a14
        private static readonly Color ColorCard = new Color(0.07f, 0.07f, 0.12f, 0.9f); // #12121e
        private static readonly Color ColorAccent = new Color(0.69f, 0.53f, 1f, 0.9f); // #b088ff
        private static readonly Color ColorAccentDim = new Color(0.69f, 0.53f, 1f, 0.4f);
        private static readonly Color ColorGold = new Color(1f, 0.85f, 0.2f);
        private static readonly Color ColorGreen = new Color(0.3f, 1f, 0.3f);
        private static readonly Color ColorRed = new Color(1f, 0.3f, 0.3f);
        private static readonly Color ColorTextDim = new Color(0.5f, 0.5f, 0.65f);
        private static readonly Color ColorTextBright = new Color(0.8f, 0.8f, 0.9f);
        private static readonly Color ColorBtnNormal = new Color(0.25f, 0.2f, 0.4f, 0.85f);
        private static readonly Color ColorBtnHover = new Color(0.35f, 0.28f, 0.5f, 0.9f);
        private static readonly Color ColorBtnMatch = new Color(0.69f, 0.53f, 1f, 0.85f);
        private static readonly Color ColorBtnCancel = new Color(0.5f, 0.15f, 0.15f, 0.85f);
        private static readonly Color ColorModeActive = new Color(0.69f, 0.53f, 1f, 0.6f);
        private static readonly Color ColorModeInactive = new Color(0.15f, 0.15f, 0.25f, 0.6f);
        private static readonly Color ColorTierBg = new Color(0.1f, 0.08f, 0.18f, 0.8f);

        // ===== 段位颜色映射 =====
        private static readonly Color[] TierColors =
        {
            new Color(0.8f, 0.5f, 0.2f),  // 青铜
            new Color(0.7f, 0.7f, 0.8f),  // 白银
            new Color(1f, 0.85f, 0.2f),   // 黄金
            new Color(0.5f, 0.8f, 1f),    // 铂金
            new Color(0.3f, 1f, 0.8f),    // 钻石
            new Color(1f, 0.4f, 0.6f),    //  legendary
        };

        // ===== UI引用 =====
        private Text _titleText;
        private Text _tierNameText;
        private Text _pointsText;
        private Text _winRateText;
        private Text _streakText;
        private Text _rankPosText;
        private Image _tierIconBg;
        private Button _mode1v1Btn;
        private Button _mode3v3Btn;
        private Text _mode1v1Text;
        private Text _mode3v3Text;
        private Button _matchBtn;
        private Text _matchBtnText;
        private Text _queueStatusText;
        private Button _backBtn;
        private Button _rankBtn;      // 排行榜按钮

        // 匹配动画
        private GameObject _matchingAnimGo;
        private Image _matchingRing;
        private Text _matchingLabel;

        // 选手展示区(匹配成功后显示简化信息)
        private GameObject _opponentPreviewGo;
        private Text _opponentNameText;
        private Text _opponentRankText;
        private Text _opponentClassText;

        // 状态
        private bool _isMatching;
        private float _animAngle;

        protected override void Awake()
        {
            base.Awake();
            BuildBackground();
            BuildTopBar();
            BuildRankCard();
            BuildModeToggle();
            BuildMatchButton();
            BuildMatchingAnimation();
            BuildOpponentPreview();
            BuildBottomButtons();
            SyncUIState();
            // 监听PVP事件
            PvpManager.Instance.OnMatchStateChanged += OnMatchStateChanged;
            PvpManager.Instance.OnQueueUpdate += OnQueueUpdate;
            PvpManager.Instance.OnRankUpdate += OnRankUpdate;
            PvpManager.Instance.OnMatchFound += OnMatchFound;
        }

        void OnDestroy()
        {
            if (PvpManager.Instance != null)
            {
                PvpManager.Instance.OnMatchStateChanged -= OnMatchStateChanged;
                PvpManager.Instance.OnQueueUpdate -= OnQueueUpdate;
                PvpManager.Instance.OnRankUpdate -= OnRankUpdate;
                PvpManager.Instance.OnMatchFound -= OnMatchFound;
            }
        }

        // =====================================================================
        // UI构建
        // =====================================================================

        private void BuildBackground()
        {
            var bg = CreateImage(transform as RectTransform, "Bg", ColorBg);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }

        private void BuildTopBar()
        {
            // 顶部栏背景
            var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(transform, false);
            var topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0, 1);
            topBarRt.anchorMax = new Vector2(1, 1);
            topBarRt.sizeDelta = new Vector2(0, 70);
            topBarRt.anchoredPosition = new Vector2(0, -35);
            topBar.GetComponent<Image>().color = ColorCard;

            // 底部装饰线
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(topBarRt, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 0);
            lineRt.anchorMax = new Vector2(1, 0);
            lineRt.sizeDelta = new Vector2(0, 2);
            lineRt.anchoredPosition = new Vector2(0, 0);
            line.GetComponent<Image>().color = ColorAccent;

            // 标题
            _titleText = CreateText(topBarRt, "Title", "⚔ 竞技场 ⚔", 36);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = ColorGold;
            _titleText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            _titleText.rectTransform.anchorMax = new Vector2(0.5f, 1);
            _titleText.rectTransform.sizeDelta = new Vector2(300, 0);

            // 返回按钮
            _backBtn = CreateButton(topBarRt, "BackBtn", "← 返回", () =>
            {
                if (_isMatching) PvpManager.Instance.CancelMatch();
                Hide();
                SceneManager.Instance.LoadScene(GameScene.MainCity);
            });
            var backRt = _backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 0.5f);
            backRt.anchorMax = new Vector2(0, 0.5f);
            backRt.sizeDelta = new Vector2(100, 40);
            backRt.anchoredPosition = new Vector2(60, 0);
        }

        private void BuildRankCard()
        {
            // 段位卡片
            var card = new GameObject("RankCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 1);
            cardRt.anchorMax = new Vector2(0.5f, 1);
            cardRt.sizeDelta = new Vector2(360, 180);
            cardRt.anchoredPosition = new Vector2(0, -130);
            card.GetComponent<Image>().color = ColorCard;

            // 边框
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(cardRt, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(-4, -4);
            border.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f, 0.5f);

            // 段位图标背景(圆形占位)
            _tierIconBg = CreateImage(cardRt, "TierIcon", ColorTierBg);
            _tierIconBg.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _tierIconBg.rectTransform.anchorMax = new Vector2(0, 0.5f);
            _tierIconBg.rectTransform.sizeDelta = new Vector2(80, 80);
            _tierIconBg.rectTransform.anchoredPosition = new Vector2(60, 0);

            // 段位名称
            _tierNameText = CreateText(cardRt, "TierName", "青铜", 28);
            _tierNameText.fontStyle = FontStyle.Bold;
            _tierNameText.color = TierColors[0];
            _tierNameText.alignment = TextAnchor.MiddleLeft;
            _tierNameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _tierNameText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            _tierNameText.rectTransform.sizeDelta = new Vector2(140, 40);
            _tierNameText.rectTransform.anchoredPosition = new Vector2(130, 20);

            // 积分
            _pointsText = CreateText(cardRt, "Points", "1000 分", 22);
            _pointsText.color = ColorTextDim;
            _pointsText.alignment = TextAnchor.MiddleLeft;
            _pointsText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _pointsText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            _pointsText.rectTransform.sizeDelta = new Vector2(140, 30);
            _pointsText.rectTransform.anchoredPosition = new Vector2(130, -20);

            // 胜率
            var winLabel = CreateText(cardRt, "WinLabel", "胜率", 16);
            winLabel.color = ColorTextDim;
            winLabel.alignment = TextAnchor.MiddleLeft;
            winLabel.rectTransform.anchorMin = new Vector2(0, 0);
            winLabel.rectTransform.anchorMax = new Vector2(0, 0);
            winLabel.rectTransform.sizeDelta = new Vector2(50, 24);
            winLabel.rectTransform.anchoredPosition = new Vector2(20, 24);

            _winRateText = CreateText(cardRt, "WinRate", "0%", 20);
            _winRateText.color = ColorTextBright;
            _winRateText.alignment = TextAnchor.MiddleLeft;
            _winRateText.rectTransform.anchorMin = new Vector2(0, 0);
            _winRateText.rectTransform.anchorMax = new Vector2(0, 0);
            _winRateText.rectTransform.sizeDelta = new Vector2(100, 24);
            _winRateText.rectTransform.anchoredPosition = new Vector2(80, 24);

            // 连胜
            var streakLabel = CreateText(cardRt, "StreakLabel", "连胜", 16);
            streakLabel.color = ColorTextDim;
            streakLabel.alignment = TextAnchor.MiddleLeft;
            streakLabel.rectTransform.anchorMin = new Vector2(0, 0);
            streakLabel.rectTransform.anchorMax = new Vector2(0, 0);
            streakLabel.rectTransform.sizeDelta = new Vector2(50, 24);
            streakLabel.rectTransform.anchoredPosition = new Vector2(20, -4);

            _streakText = CreateText(cardRt, "Streak", "-", 20);
            _streakText.color = ColorTextBright;
            _streakText.alignment = TextAnchor.MiddleLeft;
            _streakText.rectTransform.anchorMin = new Vector2(0, 0);
            _streakText.rectTransform.anchorMax = new Vector2(0, 0);
            _streakText.rectTransform.sizeDelta = new Vector2(100, 24);
            _streakText.rectTransform.anchoredPosition = new Vector2(80, -4);

            // 排行榜名次
            _rankPosText = CreateText(cardRt, "RankPos", "", 16);
            _rankPosText.color = ColorAccentDim;
            _rankPosText.alignment = TextAnchor.MiddleRight;
            _rankPosText.rectTransform.anchorMin = new Vector2(1, 1);
            _rankPosText.rectTransform.anchorMax = new Vector2(1, 1);
            _rankPosText.rectTransform.sizeDelta = new Vector2(160, 24);
            _rankPosText.rectTransform.anchoredPosition = new Vector2(-16, -16);
        }

        private void BuildModeToggle()
        {
            // 模式切换区域
            var modeArea = new GameObject("ModeArea", typeof(RectTransform));
            modeArea.transform.SetParent(transform, false);
            var modeRt = modeArea.GetComponent<RectTransform>();
            modeRt.anchorMin = new Vector2(0.5f, 1);
            modeRt.anchorMax = new Vector2(0.5f, 1);
            modeRt.sizeDelta = new Vector2(340, 56);
            modeRt.anchoredPosition = new Vector2(0, -250);

            // 1v1按钮
            _mode1v1Btn = CreateButton(modeRt, "Mode1v1", "1 vs 1", () =>
            {
                PvpManager.Instance.SetMatchMode(1);
                SyncModeToggle();
            });
            var m1Rt = _mode1v1Btn.GetComponent<RectTransform>();
            m1Rt.anchorMin = new Vector2(0, 0.5f);
            m1Rt.anchorMax = new Vector2(0, 0.5f);
            m1Rt.sizeDelta = new Vector2(160, 46);
            m1Rt.anchoredPosition = new Vector2(85, 0);
            _mode1v1Text = m1Rt.GetComponentInChildren<Text>();

            // 3v3按钮
            _mode3v3Btn = CreateButton(modeRt, "Mode3v3", "3 vs 3", () =>
            {
                PvpManager.Instance.SetMatchMode(3);
                SyncModeToggle();
            });
            var m3Rt = _mode3v3Btn.GetComponent<RectTransform>();
            m3Rt.anchorMin = new Vector2(1, 0.5f);
            m3Rt.anchorMax = new Vector2(1, 0.5f);
            m3Rt.sizeDelta = new Vector2(160, 46);
            m3Rt.anchoredPosition = new Vector2(-85, 0);
            _mode3v3Text = m3Rt.GetComponentInChildren<Text>();

            SyncModeToggle();
        }

        private void BuildMatchButton()
        {
            // 匹配按钮 - 居中偏下
            _matchBtn = CreateButton(transform as RectTransform, "MatchBtn", "开 始 匹 配", OnMatchBtnClick);
            var btnRt = _matchBtn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(240, 60);
            btnRt.anchoredPosition = new Vector2(0, -50);

            // 自定义按钮样式
            var btnImg = _matchBtn.GetComponent<Image>();
            btnImg.color = ColorBtnMatch;

            _matchBtnText = btnRt.GetComponentInChildren<Text>();
            _matchBtnText.fontSize = 26;
            _matchBtnText.fontStyle = FontStyle.Bold;

            // 队列状态文字(在按钮下方)
            _queueStatusText = CreateText(transform as RectTransform, "QueueStatus", "", 18);
            _queueStatusText.color = ColorAccentDim;
            _queueStatusText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _queueStatusText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _queueStatusText.rectTransform.sizeDelta = new Vector2(400, 30);
            _queueStatusText.rectTransform.anchoredPosition = new Vector2(0, -120);
        }

        private void BuildMatchingAnimation()
        {
            // 匹配中动画容器
            _matchingAnimGo = new GameObject("MatchingAnim", typeof(RectTransform));
            _matchingAnimGo.transform.SetParent(transform, false);
            var animRt = _matchingAnimGo.GetComponent<RectTransform>();
            animRt.anchorMin = new Vector2(0.5f, 0.5f);
            animRt.anchorMax = new Vector2(0.5f, 0.5f);
            animRt.sizeDelta = new Vector2(100, 100);
            animRt.anchoredPosition = new Vector2(0, 60);
            _matchingAnimGo.SetActive(false);

            // 旋转环
            _matchingRing = CreateImage(animRt, "Ring", ColorAccent);
            _matchingRing.rectTransform.anchorMin = Vector2.zero;
            _matchingRing.rectTransform.anchorMax = Vector2.one;
            _matchingRing.rectTransform.sizeDelta = new Vector2(-8, -8);

            // 中心文字
            _matchingLabel = CreateText(animRt, "Label", "匹配中", 22);
            _matchingLabel.fontStyle = FontStyle.Bold;
            _matchingLabel.color = ColorTextBright;
            _matchingLabel.rectTransform.anchorMin = Vector2.zero;
            _matchingLabel.rectTransform.anchorMax = Vector2.one;
            _matchingLabel.rectTransform.sizeDelta = Vector2.zero;
        }

        private void BuildOpponentPreview()
        {
            // 对手预览(匹配成功后显示在匹配按钮上方)
            _opponentPreviewGo = new GameObject("OpponentPreview", typeof(RectTransform));
            _opponentPreviewGo.transform.SetParent(transform, false);
            var prevRt = _opponentPreviewGo.GetComponent<RectTransform>();
            prevRt.anchorMin = new Vector2(0.5f, 1);
            prevRt.anchorMax = new Vector2(0.5f, 1);
            prevRt.sizeDelta = new Vector2(400, 80);
            prevRt.anchoredPosition = new Vector2(0, -340);
            _opponentPreviewGo.SetActive(false);

            // 对手卡片背景
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(prevRt, false);
            var cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.1f, 0.08f, 0.2f, 0.8f);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.sizeDelta = Vector2.zero;

            // 边框
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(cardRt, false);
            border.GetComponent<Image>().color = ColorAccentDim;
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(-4, -4);

            // "已匹配到对手" 标签
            var matchLabel = CreateText(cardRt, "MatchLabel", "⚡ 已匹配到对手 ⚡", 20);
            matchLabel.color = ColorGold;
            matchLabel.fontStyle = FontStyle.Bold;
            matchLabel.rectTransform.anchorMin = new Vector2(0, 1);
            matchLabel.rectTransform.anchorMax = new Vector2(1, 1);
            matchLabel.rectTransform.sizeDelta = new Vector2(0, 28);
            matchLabel.rectTransform.anchoredPosition = new Vector2(0, -14);

            // 对手名
            _opponentNameText = CreateText(cardRt, "OpponentName", "对手", 22);
            _opponentNameText.fontStyle = FontStyle.Bold;
            _opponentNameText.color = ColorTextBright;
            _opponentNameText.alignment = TextAnchor.MiddleLeft;
            _opponentNameText.rectTransform.anchorMin = new Vector2(0, 0.3f);
            _opponentNameText.rectTransform.anchorMax = new Vector2(0.5f, 0.7f);
            _opponentNameText.rectTransform.sizeDelta = new Vector2(-10, 0);
            _opponentNameText.rectTransform.anchoredPosition = new Vector2(10, 0);

            // 对手段位
            _opponentRankText = CreateText(cardRt, "OpponentRank", "青铜 I", 18);
            _opponentRankText.color = ColorTextDim;
            _opponentRankText.alignment = TextAnchor.MiddleLeft;
            _opponentRankText.rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
            _opponentRankText.rectTransform.anchorMax = new Vector2(0.7f, 0.7f);
            _opponentRankText.rectTransform.sizeDelta = new Vector2(-10, 0);
            _opponentRankText.rectTransform.anchoredPosition = new Vector2(10, 0);

            // 对手门派
            _opponentClassText = CreateText(cardRt, "OpponentClass", "纯阳", 18);
            _opponentClassText.color = new Color(0.5f, 0.8f, 1f);
            _opponentClassText.alignment = TextAnchor.MiddleLeft;
            _opponentClassText.rectTransform.anchorMin = new Vector2(0.7f, 0.3f);
            _opponentClassText.rectTransform.anchorMax = new Vector2(1f, 0.7f);
            _opponentClassText.rectTransform.sizeDelta = new Vector2(-10, 0);
            _opponentClassText.rectTransform.anchoredPosition = new Vector2(10, 0);
        }

        private void BuildBottomButtons()
        {
            // 排行榜按钮
            _rankBtn = CreateButton(transform as RectTransform, "RankBtn", "🏆 排行榜", () =>
            {
                UIManager.Instance.Show<PvpRankPanel>();
                PvpManager.Instance.RequestRankInfo();
            });
            var rankRt = _rankBtn.GetComponent<RectTransform>();
            rankRt.anchorMin = new Vector2(0.5f, 0);
            rankRt.anchorMax = new Vector2(0.5f, 0);
            rankRt.sizeDelta = new Vector2(180, 46);
            rankRt.anchoredPosition = new Vector2(0, 50);
        }

        // =====================================================================
        // 交互逻辑
        // =====================================================================

        private void OnMatchBtnClick()
        {
            if (_isMatching)
            {
                PvpManager.Instance.CancelMatch();
            }
            else
            {
                PvpManager.Instance.StartMatch();
            }
        }

        private void OnMatchStateChanged(MatchState prev, MatchState cur)
        {
            _isMatching = cur == MatchState.Matching;
            SyncUIState();
            if (cur == MatchState.Idle && prev == MatchState.Matching)
            {
                _opponentPreviewGo?.SetActive(false);
            }
        }

        private void OnQueueUpdate(int size)
        {
            if (_isMatching)
            {
                _queueStatusText.text = size > 0
                    ? $"队列中: {size} 人等待..."
                    : "正在匹配...";
            }
        }

        private void OnRankUpdate(RankInfo rank)
        {
            _tierNameText.text = rank.TierName;
            _tierNameText.color = TierColors[(int)rank.Tier];
            _pointsText.text = $"{rank.Points} 分";
            _winRateText.text = $"{rank.WinRate * 100:F1}%";
            _streakText.text = rank.Streak > 0
                ? $"🔥 {rank.Streak}连胜"
                : rank.Streak < 0
                    ? $"💧 {Math.Abs(rank.Streak)}连败"
                    : "-";
            _streakText.color = rank.Streak > 0 ? ColorGreen : rank.Streak < 0 ? ColorRed : ColorTextDim;
            _rankPosText.text = rank.RankPosition > 0 ? $"# {rank.RankPosition}" : "";
            _tierIconBg.color = TierColors[(int)rank.Tier];
        }

        private void OnMatchFound(PvpPlayerData opponent)
        {
            _opponentPreviewGo.SetActive(true);
            _opponentNameText.text = opponent.Name;
            _opponentRankText.text = $"{opponent.Rank.TierName} {opponent.Rank.Points}分";
            _opponentClassText.text = opponent.ClassName;
            _matchBtnText.text = "等 待 确 认";
        }

        private void SyncModeToggle()
        {
            bool is1v1 = PvpManager.Instance.MatchMode == 1;
            _mode1v1Btn.GetComponent<Image>().color = is1v1 ? ColorModeActive : ColorModeInactive;
            _mode3v3Btn.GetComponent<Image>().color = !is1v1 ? ColorModeActive : ColorModeInactive;
            if (_mode1v1Text != null) _mode1v1Text.color = is1v1 ? Color.white : ColorTextDim;
            if (_mode3v3Text != null) _mode3v3Text.color = !is1v1 ? Color.white : ColorTextDim;
        }

        private void SyncUIState()
        {
            if (_matchBtnText == null) return;

            if (_isMatching)
            {
                _matchBtnText.text = "取 消 匹 配";
                _matchBtn.GetComponent<Image>().color = ColorBtnCancel;
                _matchingAnimGo?.SetActive(true);
                _queueStatusText.text = "正在匹配...";
            }
            else
            {
                _matchBtnText.text = "开 始 匹 配";
                _matchBtn.GetComponent<Image>().color = ColorBtnMatch;
                _matchingAnimGo?.SetActive(false);
                _queueStatusText.text = "";
            }
        }

        void Update()
        {
            // 匹配中旋转动画
            if (_isMatching && _matchingAnimGo.activeSelf)
            {
                _animAngle += Time.deltaTime * 180f;
                _matchingRing.rectTransform.localRotation = Quaternion.Euler(0, 0, _animAngle);
                // 透明度脉冲
                float pulse = 0.6f + Mathf.Sin(Time.time * 4f) * 0.4f;
                _matchingRing.color = new Color(ColorAccent.r, ColorAccent.g, ColorAccent.b, pulse);
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            if (PvpManager.Instance != null)
            {
                OnRankUpdate(PvpManager.Instance.MyRank);
                SyncModeToggle();
                SyncUIState();
                _opponentPreviewGo?.SetActive(false);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            Refresh();
            // 请求段位信息
            PvpManager.Instance.RequestRankInfo();
        }

        protected override void OnHide()
        {
            base.OnHide();
            if (_isMatching)
            {
                PvpManager.Instance.CancelMatch();
            }
        }
    }
}