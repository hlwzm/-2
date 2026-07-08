#nullable disable
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;
using System.Collections;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 副本内战斗面板 - 限时倒计时/Boss血量/小Boss状态/终极Boss/队伍列表/退出
    /// 增强版: 倒计时失败/终极解锁动画/阶段显示
    /// 全程序化生成，暗黑紫色主题
    /// </summary>
    public class DungeonPanel : BasePanel
    {
        // ===== 公共数据（可由外部设置） =====
        public int DungeonId { get; set; }
        public float TimeLimitSeconds { get; set; } = 480f; // 8分钟
        public float BossMaxHp { get; set; } = 100000f;
        public float BossCurrentHp { get; set; } = 100000f;
        public string BossName { get; set; } = "董龙";
        public bool[] MinibossKilled { get; private set; } = new bool[3];
        public string[] MinibossNames { get; set; } = { "精英护卫", "暗影刺客", "毒雾术士" };
        public bool UltimateBossUnlocked { get; set; }
        public TeamMemberInfo[] TeamMembers { get; set; }

        // 副本阶段
        public int DungeonPhase { get; set; } = 1; // 1=阶段1, 2=阶段2

        // ===== 事件回调 =====
        public System.Action OnDungeonFailed;   // 倒计时归零或团灭
        public System.Action OnAllMinibossKilled; // 三小Boss全击杀

        // ===== UI引用 =====
        private Text _timerText;
        private Text _bossNameText;
        private Image _bossHpFill;
        private Text _bossHpPercentText;
        private Text[] _minibossStatusTexts = new Text[3];
        private Text _ultimateStatusText;
        private RectTransform _teamListContainer;
        private Text[] _teamNameTexts;
        private Text[] _teamClassTexts;
        private Text[] _teamHpTexts;
        private Image[] _teamHpFills;
        private Button _exitBtn;

        // 阶段显示UI
        private Text _phaseText;
        private GameObject _phaseFlashGo;

        // 终极Boss解锁动画
        private GameObject _unlockFlashGo;
        private Text _unlockFlashText;
        private float _unlockAnimTime = 0f;
        private bool _isPlayingUnlockAnim = false;

        // 副本失败UI
        private GameObject _failOverlay;
        private bool _failed = false;

        // ===== 配色 =====
        private static readonly Color ColorBg = new Color(0.04f, 0.04f, 0.08f, 0.75f);
        private static readonly Color ColorPanelBg = new Color(0.06f, 0.06f, 0.12f, 0.85f);
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorBossHpBg = new Color(0.15f, 0.05f, 0.05f);
        private static readonly Color ColorBossHpFill = new Color(0.9f, 0.15f, 0.1f);
        private static readonly Color ColorBossHpGlow = new Color(1f, 0.2f, 0.1f, 0.3f);
        private static readonly Color ColorMinibossDone = new Color(0.3f, 1f, 0.3f);
        private static readonly Color ColorMinibossPending = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorUltimateLocked = new Color(1f, 0.6f, 0.1f);
        private static readonly Color ColorUltimateUnlocked = new Color(1f, 0.9f, 0.1f);
        private static readonly Color ColorTeamBg = new Color(0.08f, 0.08f, 0.16f, 0.8f);
        private static readonly Color ColorTeamHpFill = new Color(0.2f, 0.8f, 0.3f);
        private static readonly Color ColorTeamHpBg = new Color(0.1f, 0.1f, 0.15f);
        private static readonly Color ColorTextDim = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorTextBright = new Color(0.85f, 0.85f, 0.9f);
        private static readonly Color ColorExitBtn = new Color(0.5f, 0.1f, 0.1f, 0.85f);
        private static readonly Color ColorSectionTitle = new Color(0.5f, 0.3f, 0.9f);
        private static readonly Color ColorPhase1 = new Color(0.5f, 0.8f, 1f, 0.9f);
        private static readonly Color ColorPhase2 = new Color(1f, 0.5f, 0.2f, 0.9f);

        // ===== 运行时数据 =====
        private float _timeRemaining;
        private bool _running;
        private bool _timeoutTriggered = false;
        private bool _unlockTriggered = false;

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            _timeRemaining = TimeLimitSeconds;
            _running = true;
        }

        private void BuildUI()
        {
            // ===== 全屏半透明背景 =====
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = ColorBg;

            BuildTimerArea();
            BuildPhaseDisplay();
            BuildBossHpArea();
            BuildMinibossStatus();
            BuildUltimateBossStatus();
            BuildTeamList();
            BuildExitButton();
            BuildFailOverlay();
            BuildUnlockFlashOverlay();
        }

        // =====================================================================
        // 1. 限时倒计时（顶部居中，大号红色数字）
        // =====================================================================
        private void BuildTimerArea()
        {
            var container = new GameObject("TimerArea", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(transform, false);
            var ctRt = container.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0.5f, 1);
            ctRt.anchorMax = new Vector2(0.5f, 1);
            ctRt.sizeDelta = new Vector2(200, 80);
            ctRt.anchoredPosition = new Vector2(0, -50);
            var ctImg = container.GetComponent<Image>();
            ctImg.color = new Color(0.08f, 0.02f, 0.02f, 0.6f);

            var label = CreateLabel(ctRt, "Label", "⏱ 剩余时间", 14, TextAnchor.MiddleCenter,
                ColorTextDim, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(180, 25), new Vector2(0, -5));

            _timerText = CreateLabel(ctRt, "Timer", "00:00", 36, TextAnchor.MiddleCenter,
                new Color(1f, 0.2f, 0.1f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(190, 45), new Vector2(0, -30));
            _timerText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 2. 副本阶段显示（计时器下方）
        // =====================================================================
        private void BuildPhaseDisplay()
        {
            _phaseFlashGo = new GameObject("PhaseDisplay", typeof(RectTransform), typeof(Image));
            _phaseFlashGo.transform.SetParent(transform, false);
            var phaseRt = _phaseFlashGo.GetComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0.5f, 1);
            phaseRt.anchorMax = new Vector2(0.5f, 1);
            phaseRt.sizeDelta = new Vector2(160, 30);
            phaseRt.anchoredPosition = new Vector2(0, -105);
            var phaseImg = _phaseFlashGo.GetComponent<Image>();
            phaseImg.color = new Color(0.1f, 0.15f, 0.3f, 0.5f);

            _phaseText = CreateLabel(phaseRt, "PhaseText", "阶段 1", 18, TextAnchor.MiddleCenter,
                ColorPhase1, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            _phaseText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 3. Boss血条（阶段显示下方）
        // =====================================================================
        private void BuildBossHpArea()
        {
            var bossArea = new GameObject("BossHpArea", typeof(RectTransform), typeof(Image));
            bossArea.transform.SetParent(transform, false);
            var bossRt = bossArea.GetComponent<RectTransform>();
            bossRt.anchorMin = new Vector2(0.5f, 1);
            bossRt.anchorMax = new Vector2(0.5f, 1);
            bossRt.sizeDelta = new Vector2(400, 55);
            bossRt.anchoredPosition = new Vector2(0, -140);
            var bossImg = bossArea.GetComponent<Image>();
            bossImg.color = new Color(0.06f, 0.03f, 0.06f, 0.7f);

            _bossNameText = CreateLabel(bossRt, "BossName", "◈ " + BossName + " ◈",
                16, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.2f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(380, 20), new Vector2(0, -3));

            // 血条背景
            var hpBarBg = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image));
            hpBarBg.transform.SetParent(bossRt, false);
            var hpBarBgRt = hpBarBg.GetComponent<RectTransform>();
            hpBarBgRt.anchorMin = new Vector2(0.5f, 0);
            hpBarBgRt.anchorMax = new Vector2(0.5f, 0);
            hpBarBgRt.sizeDelta = new Vector2(360, 20);
            hpBarBgRt.anchoredPosition = new Vector2(0, 7);
            var bgImg = hpBarBg.GetComponent<Image>();
            bgImg.color = ColorBossHpBg;

            // 血条填充
            var hpFill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            hpFill.transform.SetParent(hpBarBgRt, false);
            var hpFillRt = hpFill.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero; hpFillRt.anchorMax = Vector2.one;
            hpFillRt.sizeDelta = Vector2.zero;
            _bossHpFill = hpFill.GetComponent<Image>();
            _bossHpFill.type = Image.Type.Filled;
            _bossHpFill.fillMethod = Image.FillMethod.Horizontal;
            _bossHpFill.color = ColorBossHpFill;

            // 百分比文字
            _bossHpPercentText = CreateLabel(hpBarBgRt, "HpPercent", "100%",
                13, TextAnchor.MiddleCenter, ColorTextBright,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        }

        // =====================================================================
        // 4. 小Boss状态列表（Boss血条下方）
        // =====================================================================
        private void BuildMinibossStatus()
        {
            var miniArea = new GameObject("MinibossArea", typeof(RectTransform), typeof(Image));
            miniArea.transform.SetParent(transform, false);
            var miniRt = miniArea.GetComponent<RectTransform>();
            miniRt.anchorMin = new Vector2(0.5f, 1);
            miniRt.anchorMax = new Vector2(0.5f, 1);
            miniRt.sizeDelta = new Vector2(400, 80);
            miniRt.anchoredPosition = new Vector2(0, -210);
            var miniImg = miniArea.GetComponent<Image>();
            miniImg.color = new Color(0.06f, 0.06f, 0.1f, 0.6f);

            CreateLabel(miniRt, "Title", "━━ 精英讨伐进度 ━━", 13, TextAnchor.MiddleCenter,
                ColorSectionTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(380, 22), new Vector2(0, -5));

            for (int i = 0; i < 3; i++)
            {
                var idx = i;
                _minibossStatusTexts[i] = CreateLabel(miniRt, "Mini" + i, "☐ " + MinibossNames[i],
                    14, TextAnchor.MiddleLeft, ColorMinibossPending,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(380, 18), new Vector2(10, -30 - i * 18));
            }
        }

        // =====================================================================
        // 5. 终极Boss解锁状态（小Boss列表下方）
        // =====================================================================
        private void BuildUltimateBossStatus()
        {
            var ultArea = new GameObject("UltimateArea", typeof(RectTransform), typeof(Image));
            ultArea.transform.SetParent(transform, false);
            var ultRt = ultArea.GetComponent<RectTransform>();
            ultRt.anchorMin = new Vector2(0.5f, 1);
            ultRt.anchorMax = new Vector2(0.5f, 1);
            ultRt.sizeDelta = new Vector2(400, 36);
            ultRt.anchoredPosition = new Vector2(0, -300);
            var ultImg = ultArea.GetComponent<Image>();
            ultImg.color = new Color(0.08f, 0.04f, 0.02f, 0.7f);

            _ultimateStatusText = CreateLabel(ultRt, "UltStatus", "⚔ 需击杀3精英解锁终极Boss",
                14, TextAnchor.MiddleCenter, ColorUltimateLocked,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        }

        // =====================================================================
        // 6. 队伍成员列表（右下角）
        // =====================================================================
        private void BuildTeamList()
        {
            var teamArea = new GameObject("TeamArea", typeof(RectTransform), typeof(Image));
            teamArea.transform.SetParent(transform, false);
            var teamRt = teamArea.GetComponent<RectTransform>();
            teamRt.anchorMin = new Vector2(1, 0);
            teamRt.anchorMax = new Vector2(1, 1);
            teamRt.sizeDelta = new Vector2(200, -120);
            teamRt.anchoredPosition = new Vector2(-10, -60);
            var teamImg = teamArea.GetComponent<Image>();
            teamImg.color = new Color(0.04f, 0.04f, 0.08f, 0.5f);

            CreateLabel(teamRt, "Title", "★ 队伍 ★", 15, TextAnchor.MiddleCenter,
                ColorSectionTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(190, 25), new Vector2(0, -5));

            _teamListContainer = new GameObject("TeamList", typeof(RectTransform)).GetComponent<RectTransform>();
            _teamListContainer.SetParent(teamRt, false);
            _teamListContainer.anchorMin = new Vector2(0, 0);
            _teamListContainer.anchorMax = new Vector2(1, 1);
            _teamListContainer.sizeDelta = new Vector2(-10, -40);
            _teamListContainer.anchoredPosition = new Vector2(0, -20);

            // 默认8人队伍
            int teamSize = 8;
            _teamNameTexts = new Text[teamSize];
            _teamClassTexts = new Text[teamSize];
            _teamHpTexts = new Text[teamSize];
            _teamHpFills = new Image[teamSize];

            for (int i = 0; i < teamSize; i++)
            {
                var row = new GameObject("Member" + i, typeof(RectTransform));
                row.transform.SetParent(_teamListContainer, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1);
                rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 28);
                rowRt.anchoredPosition = new Vector2(0, -10 - i * 30);

                _teamNameTexts[i] = CreateLabel(rowRt, "Name", "侠客" + (i + 1),
                    12, TextAnchor.MiddleLeft, ColorTextBright,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(70, 20), new Vector2(5, 0));

                _teamClassTexts[i] = CreateLabel(rowRt, "Class", "门派",
                    10, TextAnchor.MiddleLeft, ColorTextDim,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 16), new Vector2(68, 0));

                // 血量条
                var hpBar = new GameObject("HpBar", typeof(RectTransform), typeof(Image));
                hpBar.transform.SetParent(rowRt, false);
                var hpBarRt = hpBar.GetComponent<RectTransform>();
                hpBarRt.anchorMin = new Vector2(0, 0.5f);
                hpBarRt.anchorMax = new Vector2(0, 0.5f);
                hpBarRt.sizeDelta = new Vector2(60, 10);
                hpBarRt.anchoredPosition = new Vector2(115, 0);
                var hpBarBg = hpBar.GetComponent<Image>();
                hpBarBg.color = ColorTeamHpBg;

                var hpFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                hpFill.transform.SetParent(hpBarRt, false);
                var hpFillRt = hpFill.GetComponent<RectTransform>();
                hpFillRt.anchorMin = Vector2.zero; hpFillRt.anchorMax = Vector2.one;
                hpFillRt.sizeDelta = Vector2.zero;
                _teamHpFills[i] = hpFill.GetComponent<Image>();
                _teamHpFills[i].type = Image.Type.Filled;
                _teamHpFills[i].fillMethod = Image.FillMethod.Horizontal;
                _teamHpFills[i].color = ColorTeamHpFill;

                _teamHpTexts[i] = CreateLabel(hpBarRt, "HpText", "100%",
                    9, TextAnchor.MiddleCenter, ColorTextBright,
                    Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            }
        }

        // =====================================================================
        // 7. 退出按钮（右上角）
        // =====================================================================
        private void BuildExitButton()
        {
            var btnGo = new GameObject("ExitBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1, 1);
            btnRt.anchorMax = new Vector2(1, 1);
            btnRt.sizeDelta = new Vector2(100, 36);
            btnRt.anchoredPosition = new Vector2(-70, -50);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = ColorExitBtn;

            var btnText = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnText.transform.SetParent(btnRt, false);
            var btnTextRt = btnText.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one;
            btnTextRt.sizeDelta = Vector2.zero;
            var btnTxt = btnText.GetComponent<Text>();
            btnTxt.text = "退出副本";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 14;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = new Color(0.9f, 0.7f, 0.7f);

            _exitBtn = btnGo.GetComponent<Button>();
            _exitBtn.targetGraphic = btnImg;
            _exitBtn.onClick.AddListener(OnExitDungeon);
        }

        // =====================================================================
        // 8. 副本失败覆盖层
        // =====================================================================
        private void BuildFailOverlay()
        {
            _failOverlay = new GameObject("FailOverlay", typeof(RectTransform), typeof(Image));
            _failOverlay.transform.SetParent(transform, false);
            var failRt = _failOverlay.GetComponent<RectTransform>();
            failRt.anchorMin = Vector2.zero; failRt.anchorMax = Vector2.one;
            failRt.sizeDelta = Vector2.zero;
            var failImg = _failOverlay.GetComponent<Image>();
            failImg.color = new Color(0, 0, 0, 0.7f);
            failImg.raycastTarget = true;
            _failOverlay.SetActive(false);

            var failText = CreateLabel(failRt, "FailText", "❌ 副本失败 ❌",
                48, TextAnchor.MiddleCenter, new Color(1f, 0.1f, 0.1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600, 80), Vector2.zero);
            failText.fontStyle = FontStyle.Bold;

            var subText = CreateLabel(failRt, "SubText", "时间耗尽，请重整旗鼓",
                22, TextAnchor.MiddleCenter, new Color(0.8f, 0.4f, 0.4f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 40), new Vector2(0, -60));

            var returnBtn = new GameObject("ReturnBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            returnBtn.transform.SetParent(failRt, false);
            var retRt = returnBtn.GetComponent<RectTransform>();
            retRt.anchorMin = new Vector2(0.5f, 0.5f);
            retRt.anchorMax = new Vector2(0.5f, 0.5f);
            retRt.sizeDelta = new Vector2(180, 44);
            retRt.anchoredPosition = new Vector2(0, -130);
            var retImg = returnBtn.GetComponent<Image>();
            retImg.color = new Color(0.4f, 0.1f, 0.1f, 0.9f);
            var retBtn = returnBtn.GetComponent<Button>();
            retBtn.targetGraphic = retImg;
            retBtn.onClick.AddListener(OnExitDungeon);

            var retText = CreateLabel(retRt, "RetText", "返回副本选择", 18, TextAnchor.MiddleCenter,
                new Color(0.9f, 0.6f, 0.6f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        }

        // =====================================================================
        // 9. 终极Boss解锁闪烁动画覆盖层
        // =====================================================================
        private void BuildUnlockFlashOverlay()
        {
            _unlockFlashGo = new GameObject("UnlockFlash", typeof(RectTransform), typeof(Image));
            _unlockFlashGo.transform.SetParent(transform, false);
            var flashRt = _unlockFlashGo.GetComponent<RectTransform>();
            flashRt.anchorMin = new Vector2(0.5f, 0.5f);
            flashRt.anchorMax = new Vector2(0.5f, 0.5f);
            flashRt.sizeDelta = new Vector2(600, 120);
            flashRt.anchoredPosition = Vector2.zero;
            var flashImg = _unlockFlashGo.GetComponent<Image>();
            flashImg.color = new Color(0, 0, 0, 0);
            flashImg.raycastTarget = false;
            _unlockFlashGo.SetActive(false);

            _unlockFlashText = CreateLabel(flashRt, "FlashText",
                "🔥 终极Boss解锁！ 🔥", 42, TextAnchor.MiddleCenter, ColorUltimateUnlocked,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            _unlockFlashText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 10. Update循环
        // =====================================================================
        void Update()
        {
            if (!_running || _failed) return;

            // 1. 倒计时更新与超时检测
            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining < 0) _timeRemaining = 0;

            int minutes = Mathf.FloorToInt(_timeRemaining / 60);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60);
            _timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            // 倒计时归零 → 副本失败
            if (_timeRemaining <= 0f && !_timeoutTriggered)
            {
                _timeoutTriggered = true;
                TriggerDungeonFail("⏰ 时间耗尽！");
                return;
            }

            // 倒计时<60秒变闪烁
            if (_timeRemaining <= 60f && _timeRemaining > 0)
            {
                float blink = Mathf.PingPong(Time.time * 4f, 1f);
                _timerText.color = new Color(1f, 0.1f, 0.05f, blink);
            }

            // 2. Boss血量
            if (BossMaxHp > 0)
            {
                float pct = Mathf.Clamp01(BossCurrentHp / BossMaxHp);
                _bossHpFill.fillAmount = pct;
                _bossHpPercentText.text = Mathf.CeilToInt(pct * 100) + "%";
                _bossHpPercentText.color = pct > 0.3f ? Color.white : new Color(1f, 0.5f, 0.3f);
                _bossHpFill.color = pct > 0.3f ? ColorBossHpFill : new Color(1f, 0.3f, 0.1f);
            }

            // 3. 副本阶段显示
            UpdatePhaseDisplay();

            // 4. 小Boss状态刷新
            for (int i = 0; i < _minibossStatusTexts.Length && i < MinibossKilled.Length; i++)
            {
                if (MinibossKilled[i])
                {
                    _minibossStatusTexts[i].text = "☑ " + MinibossNames[i];
                    _minibossStatusTexts[i].color = ColorMinibossDone;
                }
                else
                {
                    _minibossStatusTexts[i].text = "☐ " + MinibossNames[i];
                    _minibossStatusTexts[i].color = ColorMinibossPending;
                }
            }

            // 5. 终极Boss解锁检测
            bool allKilled = true;
            for (int i = 0; i < MinibossKilled.Length; i++)
            {
                if (!MinibossKilled[i]) { allKilled = false; break; }
            }

            bool wasUnlocked = UltimateBossUnlocked;
            UltimateBossUnlocked = allKilled;

            if (UltimateBossUnlocked)
            {
                _ultimateStatusText.text = "🔥 终极Boss已解锁!";
                _ultimateStatusText.color = ColorUltimateUnlocked;

                // 触发解锁动画（首次解锁时）
                if (!_unlockTriggered)
                {
                    _unlockTriggered = true;
                    StartUnlockAnimation();
                    OnAllMinibossKilled?.Invoke();
                }
            }
            else
            {
                int remaining = 0;
                for (int i = 0; i < MinibossKilled.Length; i++)
                    if (!MinibossKilled[i]) remaining++;
                _ultimateStatusText.text = "⚔ 还需击杀 " + remaining + " 精英解锁";
                _ultimateStatusText.color = ColorUltimateLocked;
            }

            // 6. 解锁动画更新
            if (_isPlayingUnlockAnim)
            {
                UpdateUnlockAnimation();
            }

            // 7. 队伍成员血量刷新
            if (TeamMembers != null)
            {
                for (int i = 0; i < _teamHpFills.Length && i < TeamMembers.Length; i++)
                {
                    float hp = TeamMembers[i].HpPercent;
                    _teamHpFills[i].fillAmount = hp;
                    _teamHpTexts[i].text = Mathf.CeilToInt(hp * 100) + "%";
                    _teamHpFills[i].color = hp > 0.3f ? ColorTeamHpFill : new Color(1f, 0.3f, 0.2f);
                }
            }
        }

        // =====================================================================
        // 阶段显示更新
        // =====================================================================
        private void UpdatePhaseDisplay()
        {
            // 根据当前Boss血量决定阶段显示
            float hpPct = BossMaxHp > 0 ? BossCurrentHp / BossMaxHp : 1f;
            int targetPhase = hpPct <= 0.5f ? 2 : 1;

            if (targetPhase != DungeonPhase)
            {
                DungeonPhase = targetPhase;
                if (DungeonPhase == 2)
                {
                    Debug.Log("[DungeonPanel] Boss进入第二阶段！");
                }
            }

            if (DungeonPhase == 1)
            {
                _phaseText.text = "阶段 1";
                _phaseText.color = ColorPhase1;
            }
            else
            {
                // 阶段2闪烁效果
                float blink = Mathf.PingPong(Time.time * 3f, 1f);
                _phaseText.text = "⚡ 阶段 2 ⚡";
                _phaseText.color = new Color(1f, 0.5f, 0.2f, 0.6f + blink * 0.4f);
            }
        }

        // =====================================================================
        // 终极Boss解锁动画
        // =====================================================================
        private void StartUnlockAnimation()
        {
            _isPlayingUnlockAnim = true;
            _unlockAnimTime = 0f;
            _unlockFlashGo.SetActive(true);
        }

        private void UpdateUnlockAnimation()
        {
            _unlockAnimTime += Time.deltaTime;

            if (_unlockAnimTime > 3.0f)
            {
                // 动画结束
                _isPlayingUnlockAnim = false;
                _unlockFlashGo.SetActive(false);
                return;
            }

            // 闪烁 + 缩放脉冲
            float t = _unlockAnimTime;
            float flash = Mathf.PingPong(t * 8f, 1f);
            float scale = 1f + Mathf.Sin(t * 6f) * 0.1f;

            var flashImg = _unlockFlashGo.GetComponent<Image>();
            flashImg.color = new Color(0, 0, 0, 0.5f * (1f - t / 3f));

            var unlockRt = _unlockFlashGo.GetComponent<RectTransform>();
            unlockRt.localScale = new Vector3(scale, scale, 1f);

            _unlockFlashText.color = new Color(1f, 0.9f, 0.1f, flash);
        }

        // =====================================================================
        // 副本失败触发
        // =====================================================================
        private void TriggerDungeonFail(string reason)
        {
            if (_failed) return;
            _failed = true;
            _running = false;

            Debug.Log($"[DungeonPanel] 副本失败: {reason}");
            _failOverlay.SetActive(true);
            OnDungeonFailed?.Invoke();
        }

        // =====================================================================
        // 公开方法
        // =====================================================================
        public void SetBossHp(float current, float max)
        {
            BossCurrentHp = current;
            BossMaxHp = max;
        }

        public void SetBossName(string name)
        {
            BossName = name;
            if (_bossNameText != null)
                _bossNameText.text = "◈ " + name + " ◈";
        }

        public void SetMinibossKilled(int index, bool killed)
        {
            if (index >= 0 && index < MinibossKilled.Length)
                MinibossKilled[index] = killed;
        }

        public bool AreAllMinibossKilled()
        {
            for (int i = 0; i < MinibossKilled.Length; i++)
                if (!MinibossKilled[i]) return false;
            return true;
        }

        public void SetTimeLimit(float seconds)
        {
            TimeLimitSeconds = seconds;
            _timeRemaining = seconds;
        }

        public float GetRemainingTime()
        {
            return _timeRemaining;
        }

        public bool IsDungeonFailed()
        {
            return _failed;
        }

        public void UpdateTeamMember(int index, float hpPercent)
        {
            if (TeamMembers != null && index >= 0 && index < TeamMembers.Length)
                TeamMembers[index].HpPercent = hpPercent;
        }

        protected virtual void OnExitDungeon()
        {
            _running = false;
            SceneManager.Instance.LoadScene(GameScene.DungeonSelect);
        }

        protected override void OnShow() { _running = true; }
        protected override void OnHide() { _running = false; }

        public override void Refresh()
        {
            base.Refresh();
            _timeRemaining = TimeLimitSeconds;
            _running = true;
            _failed = false;
            _timeoutTriggered = false;
            _unlockTriggered = false;
            _isPlayingUnlockAnim = false;
            _failOverlay?.SetActive(false);
            _unlockFlashGo?.SetActive(false);
        }

        // =====================================================================
        // Helper: 创建标签
        // =====================================================================
        private Text CreateLabel(RectTransform parent, string name, string text,
            int fontSize, TextAnchor align, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = align;
            txt.color = color;
            return txt;
        }
    }

    /// <summary>
    /// 队伍成员信息
    /// </summary>
    [System.Serializable]
    public class TeamMemberInfo
    {
        public string Name;
        public string ClassName;
        public float HpPercent = 1f;
    }
}