using UnityEngine;
using UnityEngine.UI;
using Jx3.Core.Scene;
using Jx3.UI;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 副本战斗面板 - 限时倒计时/Boss血条/小Boss状态/终极Boss/队伍列表/退出
    /// 全程程序化生成，金墨武侠主题
    /// </summary>
    public class DungeonPanel : BasePanel
    {
        // ===== 公共数据 =====
        public int DungeonId { get; set; }
        public float TimeLimitSeconds { get; set; } = 480f;
        public float BossMaxHp { get; set; } = 100000f;
        public float BossCurrentHp { get; set; } = 100000f;
        public string BossName { get; set; } = "董龙";
        public bool[] MinibossKilled { get; private set; } = new bool[3];
        public string[] MinibossNames { get; set; } = { "精英护卫", "暗影刺客", "毒雾术士" };
        public bool UltimateBossUnlocked { get; set; }
        public TeamMemberInfo[] TeamMembers { get; set; }

        // 副本阶段
        public int DungeonPhase { get; set; } = 1;

        // ===== 事件回调 =====
        public System.Action OnDungeonFailed;
        public System.Action OnAllMinibossKilled;
        public System.Action OnVictory;

        // ===== UI引用 =====
        // 计时区
        private Text _timerText;
        private Image _timerBg;

        // 阶段显示
        private Text _phaseText;
        private GameObject _phaseFlashGo;

        // Boss血条
        private Text _bossNameText;
        private Image _bossHpFill;
        private Text _bossHpPercentText;

        // 小Boss状态
        private Text[] _minibossStatusTexts = new Text[3];

        // 终极Boss
        private Text _ultimateStatusText;

        // 队伍
        private RectTransform _teamListContainer;
        private Text[] _teamNameTexts;
        private Text[] _teamHpTexts;
        private Image[] _teamHpFills;

        // 按钮
        private Button _exitBtn;

        // 终极Boss解锁动画
        private GameObject _unlockFlashGo;
        private Text _unlockFlashText;
        private float _unlockAnimTime;
        private bool _isPlayingUnlockAnim;

        // 失败/胜利覆盖层
        private GameObject _failOverlay;
        private GameObject _victoryOverlay;
        private bool _failed;
        private bool _victory;

        // ===== 运行时数据 =====
        private float _timeRemaining;
        private bool _running;
        private bool _timeoutTriggered;
        private bool _unlockTriggered;

        // ===== 额外颜色 =====
        private static readonly Color ColorBossHpBg = new(0.15f, 0.05f, 0.05f);
        private static readonly Color ColorBossHpFill = new(0.9f, 0.15f, 0.1f);
        private static readonly Color ColorMinibossDone = new(0.3f, 1f, 0.3f);
        private static readonly Color ColorMinibossPending = new(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorUltimateLocked = new(1f, 0.6f, 0.1f);
        private static readonly Color ColorUltimateUnlocked = new(1f, 0.9f, 0.1f);
        private static readonly Color ColorTeamHpFill = new(0.2f, 0.8f, 0.3f);
        private static readonly Color ColorTeamHpBg = new(0.1f, 0.1f, 0.15f);
        private static readonly Color ColorPhase1 = new(0.5f, 0.8f, 1f, 0.9f);
        private static readonly Color ColorPhase2 = new(1f, 0.5f, 0.2f, 0.9f);

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            _timeRemaining = TimeLimitSeconds;
            _running = true;
        }

        // =====================================================================
        // UI构建
        // =====================================================================
        private void BuildUI()
        {
            // 全屏半透明背景
            UIComponentFactory.CreateBackground(transform as RectTransform);

            BuildTimerArea();
            BuildPhaseDisplay();
            BuildBossHpArea();
            BuildMinibossStatus();
            BuildUltimateBossStatus();
            BuildTeamList();
            BuildExitButton();
            BuildFailOverlay();
            BuildVictoryOverlay();
            BuildUnlockFlashOverlay();
        }

        // =====================================================================
        // 1. 限时倒计时（顶部居中）
        // =====================================================================
        private void BuildTimerArea()
        {
            var container = UIComponentFactory.CreateCard(transform as RectTransform,
                "TimerArea", new Vector2(200, 80), new Vector2(0, 375));
            var cImg = container.GetComponent<Image>();
            cImg.color = new Color(0.08f, 0.02f, 0.02f, 0.6f);

            UIComponentFactory.CreateText(container, "Label", "⏳ 剩余时间",
                ThemeColors.FontTiny, ThemeColors.TextDim, TextAnchor.MiddleCenter)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 18);

            _timerText = UIComponentFactory.CreateText(container, "Timer", "00:00",
                36, new Color(1f, 0.2f, 0.1f), TextAnchor.MiddleCenter);
            _timerText.fontStyle = FontStyle.Bold;
            _timerText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);
        }

        // =====================================================================
        // 2. 副本阶段显示
        // =====================================================================
        private void BuildPhaseDisplay()
        {
            _phaseFlashGo = new GameObject("PhaseDisplay", typeof(RectTransform), typeof(Image));
            _phaseFlashGo.transform.SetParent(transform, false);
            var phaseRt = _phaseFlashGo.GetComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0.5f, 1);
            phaseRt.anchorMax = new Vector2(0.5f, 1);
            phaseRt.sizeDelta = new Vector2(160, 30);
            phaseRt.anchoredPosition = new Vector2(0, -100);
            var phaseImg = _phaseFlashGo.GetComponent<Image>();
            phaseImg.color = new Color(0.1f, 0.15f, 0.3f, 0.5f);

            _phaseText = UIComponentFactory.CreateText(phaseRt, "PhaseText", "阶段 1",
                18, ColorPhase1, TextAnchor.MiddleCenter);
            _phaseText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 3. Boss血条
        // =====================================================================
        private void BuildBossHpArea()
        {
            var bossArea = UIComponentFactory.CreateCard(transform as RectTransform,
                "BossHpArea", new Vector2(400, 55), new Vector2(0, -170));

            _bossNameText = UIComponentFactory.CreateText(bossArea, "BossName",
                "◆ " + BossName + " ◆", 16, new Color(1f, 0.5f, 0.2f), TextAnchor.MiddleCenter);
            _bossNameText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 12);

            // 血条背景
            var hpBarBg = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image));
            hpBarBg.transform.SetParent(bossArea, false);
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
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.sizeDelta = Vector2.zero;
            _bossHpFill = hpFill.GetComponent<Image>();
            _bossHpFill.type = Image.Type.Filled;
            _bossHpFill.fillMethod = Image.FillMethod.Horizontal;
            _bossHpFill.color = ColorBossHpFill;

            // 百分比文字
            _bossHpPercentText = UIComponentFactory.CreateText(hpBarBgRt, "HpPercent",
                "100%", 13, ThemeColors.TextWhite, TextAnchor.MiddleCenter);
        }

        // =====================================================================
        // 4. 小Boss状态列表
        // =====================================================================
        private void BuildMinibossStatus()
        {
            var miniArea = UIComponentFactory.CreateCard(transform as RectTransform,
                "MinibossArea", new Vector2(400, 80), new Vector2(0, -240));
            var mImg = miniArea.GetComponent<Image>();
            mImg.color = new Color(0.06f, 0.06f, 0.1f, 0.6f);

            UIComponentFactory.CreateText(miniArea, "Title", "━┳ 精英讨伐进度 ┳━",
                13, ThemeColors.Accent, TextAnchor.MiddleCenter)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 28);

            for (int i = 0; i < 3; i++)
            {
                _minibossStatusTexts[i] = UIComponentFactory.CreateText(miniArea,
                    "Mini" + i, "★ " + MinibossNames[i], 14, ColorMinibossPending,
                    TextAnchor.MiddleLeft);
                _minibossStatusTexts[i].GetComponent<RectTransform>()
                    .anchoredPosition = new Vector2(-180, 2 - i * 20);
            }
        }

        // =====================================================================
        // 5. 终极Boss解锁状态
        // =====================================================================
        private void BuildUltimateBossStatus()
        {
            var ultArea = UIComponentFactory.CreateCard(transform as RectTransform,
                "UltimateArea", new Vector2(400, 36), new Vector2(0, -320));
            var uImg = ultArea.GetComponent<Image>();
            uImg.color = new Color(0.08f, 0.04f, 0.02f, 0.7f);

            _ultimateStatusText = UIComponentFactory.CreateText(ultArea, "UltStatus",
                "⚡ 需击杀3精英解锁终极Boss", 14, ColorUltimateLocked, TextAnchor.MiddleCenter);
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

            UIComponentFactory.CreateText(teamRt, "Title", "★ 队伍 ★",
                15, ThemeColors.Accent, TextAnchor.MiddleCenter)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -12);

            _teamListContainer = new GameObject("TeamList", typeof(RectTransform))
                .GetComponent<RectTransform>();
            _teamListContainer.SetParent(teamRt, false);
            _teamListContainer.anchorMin = new Vector2(0, 0);
            _teamListContainer.anchorMax = new Vector2(1, 1);
            _teamListContainer.sizeDelta = new Vector2(-10, -40);
            _teamListContainer.anchoredPosition = new Vector2(0, -20);

            int teamSize = 8;
            _teamNameTexts = new Text[teamSize];
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

                _teamNameTexts[i] = UIComponentFactory.CreateText(rowRt, "Name",
                    "侠客" + (i + 1), 12, ThemeColors.TextBright, TextAnchor.MiddleLeft);
                _teamNameTexts[i].GetComponent<RectTransform>()
                    .sizeDelta = new Vector2(70, 20);
                _teamNameTexts[i].GetComponent<RectTransform>()
                    .anchoredPosition = new Vector2(-85, 0);

                // 血条
                var hpBar = new GameObject("HpBar", typeof(RectTransform), typeof(Image));
                hpBar.transform.SetParent(rowRt, false);
                var hpBarRt = hpBar.GetComponent<RectTransform>();
                hpBarRt.anchorMin = new Vector2(0, 0.5f);
                hpBarRt.anchorMax = new Vector2(0, 0.5f);
                hpBarRt.sizeDelta = new Vector2(60, 10);
                hpBarRt.anchoredPosition = new Vector2(50, 0);
                var hpBarBg = hpBar.GetComponent<Image>();
                hpBarBg.color = ColorTeamHpBg;

                var hpFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                hpFill.transform.SetParent(hpBarRt, false);
                var hpFillRt = hpFill.GetComponent<RectTransform>();
                hpFillRt.anchorMin = Vector2.zero;
                hpFillRt.anchorMax = Vector2.one;
                hpFillRt.sizeDelta = Vector2.zero;
                _teamHpFills[i] = hpFill.GetComponent<Image>();
                _teamHpFills[i].type = Image.Type.Filled;
                _teamHpFills[i].fillMethod = Image.FillMethod.Horizontal;
                _teamHpFills[i].color = ColorTeamHpFill;

                _teamHpTexts[i] = UIComponentFactory.CreateText(hpBarRt, "HpText",
                    "100%", 9, ThemeColors.TextWhite, TextAnchor.MiddleCenter);
            }
        }

        // =====================================================================
        // 7. 退出按钮（右上角）
        // =====================================================================
        private void BuildExitButton()
        {
            var btnRt = new GameObject("ExitBtn", typeof(RectTransform)).GetComponent<RectTransform>();
            btnRt.SetParent(transform, false);
            btnRt.anchorMin = new Vector2(1, 1);
            btnRt.anchorMax = new Vector2(1, 1);
            btnRt.sizeDelta = new Vector2(100, 36);
            btnRt.anchoredPosition = new Vector2(-70, -50);

            _exitBtn = UIComponentFactory.CreateButton(btnRt, "Btn", "退出副本",
                ThemeColors.BtnDanger, OnExitDungeon, ThemeColors.FontSmall);
        }

        // =====================================================================
        // 8. 失败覆盖层
        // =====================================================================
        private void BuildFailOverlay()
        {
            _failOverlay = new GameObject("FailOverlay", typeof(RectTransform), typeof(Image));
            _failOverlay.transform.SetParent(transform, false);
            var failRt = _failOverlay.GetComponent<RectTransform>();
            failRt.anchorMin = Vector2.zero;
            failRt.anchorMax = Vector2.one;
            failRt.sizeDelta = Vector2.zero;
            var failImg = _failOverlay.GetComponent<Image>();
            failImg.color = new Color(0, 0, 0, 0.7f);
            failImg.raycastTarget = true;
            _failOverlay.SetActive(false);

            UIComponentFactory.CreateText(failRt, "FailText",
                "⚔ 副本失败 ⚔", 48, new Color(1f, 0.1f, 0.1f), TextAnchor.MiddleCenter);
            _failOverlay.transform.GetChild(0).GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(0, 30);

            UIComponentFactory.CreateText(failRt, "SubText",
                "时间耗尽，请重整旗鼓", 22, new Color(0.8f, 0.4f, 0.4f), TextAnchor.MiddleCenter)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);

            var retBtnRt = new GameObject("ReturnBtn", typeof(RectTransform))
                .GetComponent<RectTransform>();
            retBtnRt.SetParent(failRt, false);
            retBtnRt.anchorMin = new Vector2(0.5f, 0.5f);
            retBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
            retBtnRt.sizeDelta = new Vector2(180, 44);
            retBtnRt.anchoredPosition = new Vector2(0, -80);

            UIComponentFactory.CreateButton(retBtnRt, "RetBtn", "返回副本选择",
                new Color(0.4f, 0.1f, 0.1f, 0.9f), OnExitDungeon, ThemeColors.FontBody);
        }

        // =====================================================================
        // 9. 胜利覆盖层
        // =====================================================================
        private void BuildVictoryOverlay()
        {
            _victoryOverlay = new GameObject("VictoryOverlay", typeof(RectTransform), typeof(Image));
            _victoryOverlay.transform.SetParent(transform, false);
            var vicRt = _victoryOverlay.GetComponent<RectTransform>();
            vicRt.anchorMin = Vector2.zero;
            vicRt.anchorMax = Vector2.one;
            vicRt.sizeDelta = Vector2.zero;
            var vicImg = _victoryOverlay.GetComponent<Image>();
            vicImg.color = new Color(0, 0, 0, 0.6f);
            vicImg.raycastTarget = true;
            _victoryOverlay.SetActive(false);

            UIComponentFactory.CreateText(vicRt, "VictoryText",
                "🏆 副本胜利 🏆", 48, new Color(1f, 0.8f, 0.1f), TextAnchor.MiddleCenter);
            _victoryOverlay.transform.GetChild(0).GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(0, 30);

            UIComponentFactory.CreateText(vicRt, "SubText",
                "恭喜通关！", 22, ThemeColors.Gold, TextAnchor.MiddleCenter)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);

            var exitBtnRt = new GameObject("ExitBtn", typeof(RectTransform))
                .GetComponent<RectTransform>();
            exitBtnRt.SetParent(vicRt, false);
            exitBtnRt.anchorMin = new Vector2(0.5f, 0.5f);
            exitBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
            exitBtnRt.sizeDelta = new Vector2(180, 44);
            exitBtnRt.anchoredPosition = new Vector2(0, -80);

            UIComponentFactory.CreateButton(exitBtnRt, "ExitBtn", "离开副本",
                ThemeColors.BtnPrimary, OnExitDungeon, ThemeColors.FontBody);
        }

        // =====================================================================
        // 10. 终极Boss解锁动画覆盖层
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

            _unlockFlashText = UIComponentFactory.CreateText(flashRt, "FlashText",
                "🔥 终极Boss解锁！ 🔥", 42, ColorUltimateUnlocked, TextAnchor.MiddleCenter);
            _unlockFlashText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 11. Update循环
        // =====================================================================
        void Update()
        {
            if (!_running || _failed || _victory) return;

            // ── 倒计时更新与超时检测 ──
            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining < 0) _timeRemaining = 0;

            int minutes = Mathf.FloorToInt(_timeRemaining / 60);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60);
            _timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            if (_timeRemaining <= 0f && !_timeoutTriggered)
            {
                _timeoutTriggered = true;
                TriggerDungeonFail("⏳ 时间耗尽！");
                return;
            }

            // 倒计时60秒变闪烁
            if (_timeRemaining <= 60f && _timeRemaining > 0)
            {
                float blink = Mathf.PingPong(Time.time * 4f, 1f);
                _timerText.color = new Color(1f, 0.1f, 0.05f, blink);
            }

            // ── Boss血量 ──
            if (BossMaxHp > 0)
            {
                float pct = Mathf.Clamp01(BossCurrentHp / BossMaxHp);
                _bossHpFill.fillAmount = pct;
                _bossHpPercentText.text = Mathf.CeilToInt(pct * 100) + "%";
                _bossHpPercentText.color = pct > 0.3f ? Color.white : new Color(1f, 0.5f, 0.3f);
                _bossHpFill.color = pct > 0.3f ? ColorBossHpFill : new Color(1f, 0.3f, 0.1f);
            }

            // ── 副本阶段 ──
            UpdatePhaseDisplay();

            // ── 小Boss状态刷新 ──
            for (int i = 0; i < _minibossStatusTexts.Length && i < MinibossKilled.Length; i++)
            {
                if (MinibossKilled[i])
                {
                    _minibossStatusTexts[i].text = "★ " + MinibossNames[i];
                    _minibossStatusTexts[i].color = ColorMinibossDone;
                }
                else
                {
                    _minibossStatusTexts[i].text = "☆ " + MinibossNames[i];
                    _minibossStatusTexts[i].color = ColorMinibossPending;
                }
            }

            // ── 终极Boss解锁检测 ──
            bool allKilled = true;
            for (int i = 0; i < MinibossKilled.Length; i++)
            {
                if (!MinibossKilled[i]) { allKilled = false; break; }
            }

            UltimateBossUnlocked = allKilled;

            if (UltimateBossUnlocked)
            {
                _ultimateStatusText.text = "🔥 终极Boss已解锁！";
                _ultimateStatusText.color = ColorUltimateUnlocked;

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
                _ultimateStatusText.text = "⚡ 还需击杀 " + remaining + " 精英解锁";
                _ultimateStatusText.color = ColorUltimateLocked;
            }

            // ── 解锁动画更新 ──
            if (_isPlayingUnlockAnim)
            {
                UpdateUnlockAnimation();
            }

            // ── 队伍成员血量刷新 ──
            if (TeamMembers != null)
            {
                for (int i = 0; i < _teamHpFills.Length && i < TeamMembers.Length; i++)
                {
                    float hp = TeamMembers[i].HpPercent;
                    _teamHpFills[i].fillAmount = hp;
                    _teamHpTexts[i].text = Mathf.CeilToInt(hp * 100) + "%";
                    _teamHpFills[i].color = hp > 0.3f ? ColorTeamHpFill : new Color(1f, 0.3f, 0.2f);
                    _teamNameTexts[i].text = TeamMembers[i].Name ?? "侠客" + (i + 1);
                }
            }
        }

        // =====================================================================
        // 阶段显示更新
        // =====================================================================
        private void UpdatePhaseDisplay()
        {
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
                _isPlayingUnlockAnim = false;
                _unlockFlashGo.SetActive(false);
                return;
            }

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
        // 副本胜利触发
        // =====================================================================
        public void TriggerVictory()
        {
            if (_victory || _failed) return;
            _victory = true;
            _running = false;

            Debug.Log("[DungeonPanel] 副本胜利！");
            _victoryOverlay.SetActive(true);
            OnVictory?.Invoke();
        }

        // =====================================================================
        // 公共方法
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
                _bossNameText.text = "◆ " + name + " ◆";
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

        public bool IsVictory()
        {
            return _victory;
        }

        public void UpdateTeamMember(int index, float hpPercent)
        {
            if (TeamMembers != null && index >= 0 && index < TeamMembers.Length)
                TeamMembers[index].HpPercent = hpPercent;
        }

        // =====================================================================
        // 退出
        // =====================================================================
        protected virtual void OnExitDungeon()
        {
            _running = false;
            SceneManager.Instance.LoadScene(GameScene.DungeonSelect);
        }

        protected override void OnShow()
        {
            _running = true;
        }

        protected override void OnHide()
        {
            _running = false;
        }

        public override void Refresh()
        {
            base.Refresh();
            _timeRemaining = TimeLimitSeconds;
            _running = true;
            _failed = false;
            _victory = false;
            _timeoutTriggered = false;
            _unlockTriggered = false;
            _isPlayingUnlockAnim = false;
            _failOverlay?.SetActive(false);
            _victoryOverlay?.SetActive(false);
            _unlockFlashGo?.SetActive(false);
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
