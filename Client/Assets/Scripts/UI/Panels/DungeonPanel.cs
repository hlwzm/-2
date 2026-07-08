#nullable disable
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 副本内战斗面板 - 限时倒计时/Boss血量/小Boss状态/终极Boss/队伍列表/退出
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

        // ===== 运行时数据 =====
        private float _timeRemaining;
        private bool _running;

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
            BuildBossHpArea();
            BuildMinibossStatus();
            BuildUltimateBossStatus();
            BuildTeamList();
            BuildExitButton();
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

            // 标签
            var label = CreateText(ctRt, "Label", "⏱ 限时挑战", 16);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = new Vector2(0.5f, 1);
            labelRt.anchorMax = new Vector2(0.5f, 1);
            labelRt.sizeDelta = new Vector2(160, 24);
            labelRt.anchoredPosition = new Vector2(0, -12);
            label.color = new Color(1f, 0.5f, 0.3f);
            label.fontStyle = FontStyle.Bold;

            // 计时数字（大号红色）
            _timerText = CreateText(ctRt, "TimerText", "08:00", 56);
            var timerRt = (RectTransform)_timerText.transform;
            timerRt.anchorMin = new Vector2(0.5f, 0);
            timerRt.anchorMax = new Vector2(0.5f, 0);
            timerRt.sizeDelta = new Vector2(180, 50);
            timerRt.anchoredPosition = new Vector2(0, 10);
            _timerText.color = new Color(1f, 0.15f, 0.1f);
            _timerText.fontStyle = FontStyle.Bold;
            _timerText.alignment = TextAnchor.MiddleCenter;

            // 装饰边框
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(ctRt, false);
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = Vector2.zero;
            var bImg = border.GetComponent<Image>();
            bImg.color = new Color(0.4f, 0.1f, 0.05f, 0.4f);
            bImg.type = Image.Type.Sliced;
        }

        // =====================================================================
        // 2. Boss血量条（Boss名称 + 血条 + 百分比）
        // =====================================================================
        private void BuildBossHpArea()
        {
            var container = new GameObject("BossHpArea", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(transform, false);
            var ctRt = container.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0.5f, 1);
            ctRt.anchorMax = new Vector2(0.5f, 1);
            ctRt.sizeDelta = new Vector2(460, 100);
            ctRt.anchoredPosition = new Vector2(0, -150);
            var ctImg = container.GetComponent<Image>();
            ctImg.color = new Color(0.06f, 0.02f, 0.06f, 0.7f);

            // Boss名称
            _bossNameText = CreateText(ctRt, "BossName", "◈ " + BossName + " ◈", 22);
            var nameRt = (RectTransform)_bossNameText.transform;
            nameRt.anchorMin = new Vector2(0.5f, 1);
            nameRt.anchorMax = new Vector2(0.5f, 1);
            nameRt.sizeDelta = new Vector2(400, 28);
            nameRt.anchoredPosition = new Vector2(0, -14);
            _bossNameText.color = new Color(1f, 0.5f, 0.2f);
            _bossNameText.fontStyle = FontStyle.Bold;

            // 血条背景
            var hpBarBg = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image));
            hpBarBg.transform.SetParent(ctRt, false);
            var hpBgRt = hpBarBg.GetComponent<RectTransform>();
            hpBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            hpBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            hpBgRt.sizeDelta = new Vector2(420, 26);
            hpBgRt.anchoredPosition = new Vector2(0, 10);
            var hpBgImg = hpBarBg.GetComponent<Image>();
            hpBgImg.color = ColorBossHpBg;

            // 血条填充
            var hpFill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            hpFill.transform.SetParent(hpBgRt, false);
            var hpFillRt = hpFill.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = new Vector2(1, 1);
            hpFillRt.sizeDelta = Vector2.zero;
            hpFillRt.anchoredPosition = Vector2.zero;
            _bossHpFill = hpFill.GetComponent<Image>();
            _bossHpFill.color = ColorBossHpFill;
            _bossHpFill.type = Image.Type.Filled;
            _bossHpFill.fillMethod = Image.FillMethod.Horizontal;
            _bossHpFill.fillAmount = 1f;

            // 血条发光
            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(hpFillRt, false);
            var glowRt = glow.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero; glowRt.anchorMax = Vector2.one;
            glowRt.sizeDelta = new Vector2(0, 0);
            var glowImg = glow.GetComponent<Image>();
            glowImg.color = ColorBossHpGlow;

            // 百分比文字（血条上）
            _bossHpPercentText = CreateText(hpBgRt, "HpPercent", "100%", 18);
            var pctRt = (RectTransform)_bossHpPercentText.transform;
            pctRt.anchorMin = Vector2.zero; pctRt.anchorMax = Vector2.one;
            pctRt.sizeDelta = Vector2.zero;
            _bossHpPercentText.color = Color.white;
            _bossHpPercentText.fontStyle = FontStyle.Bold;
            _bossHpPercentText.alignment = TextAnchor.MiddleCenter;

            // 底部装饰线
            var bottomLine = new GameObject("BottomLine", typeof(RectTransform), typeof(Image));
            bottomLine.transform.SetParent(ctRt, false);
            var blRt = bottomLine.GetComponent<RectTransform>();
            blRt.anchorMin = new Vector2(0, 0);
            blRt.anchorMax = new Vector2(1, 0);
            blRt.sizeDelta = new Vector2(0, 1);
            blRt.anchoredPosition = new Vector2(0, 0);
            bottomLine.GetComponent<Image>().color = new Color(0.3f, 0.1f, 0.3f, 0.5f);
        }

        // =====================================================================
        // 3. 小Boss状态列表（☑已击杀 / ☐未击杀）
        // =====================================================================
        private void BuildMinibossStatus()
        {
            var container = new GameObject("MinibossStatus", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(transform, false);
            var ctRt = container.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0, 0.5f);
            ctRt.anchorMax = new Vector2(0, 0.5f);
            ctRt.sizeDelta = new Vector2(280, 200);
            ctRt.anchoredPosition = new Vector2(20, 60);
            var ctImg = container.GetComponent<Image>();
            ctImg.color = new Color(0.06f, 0.06f, 0.12f, 0.7f);

            // 标题
            var title = CreateText(ctRt, "Title", "⚔ 精英首领", 20);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0.5f, 1);
            titleRt.anchorMax = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(240, 28);
            titleRt.anchoredPosition = new Vector2(0, -14);
            title.color = ColorSectionTitle;
            title.fontStyle = FontStyle.Bold;

            // 3个小Boss状态
            for (int i = 0; i < 3; i++)
            {
                var yOff = -48 - i * 44;

                // 背景行
                var row = new GameObject("Row" + i, typeof(RectTransform), typeof(Image));
                row.transform.SetParent(ctRt, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0.5f, 1);
                rowRt.anchorMax = new Vector2(0.5f, 1);
                rowRt.sizeDelta = new Vector2(250, 36);
                rowRt.anchoredPosition = new Vector2(0, yOff);
                var rowImg = row.GetComponent<Image>();
                rowImg.color = new Color(0.1f, 0.1f, 0.18f, 0.5f);

                var idx = i;
                _minibossStatusTexts[i] = CreateText(rowRt, "StatusText", "☐ " + MinibossNames[i], 20);
                var txtRt = (RectTransform)_minibossStatusTexts[i].transform;
                txtRt.anchorMin = new Vector2(0, 0.5f);
                txtRt.anchorMax = new Vector2(0, 0.5f);
                txtRt.sizeDelta = new Vector2(230, 28);
                txtRt.anchoredPosition = new Vector2(12, 0);
                _minibossStatusTexts[i].color = ColorMinibossPending;
                _minibossStatusTexts[i].alignment = TextAnchor.MiddleLeft;
            }
        }

        // =====================================================================
        // 4. 终极Boss解锁状态
        // =====================================================================
        private void BuildUltimateBossStatus()
        {
            var container = new GameObject("UltimateBossStatus", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(transform, false);
            var ctRt = container.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0, 0.5f);
            ctRt.anchorMax = new Vector2(0, 0.5f);
            ctRt.sizeDelta = new Vector2(280, 60);
            ctRt.anchoredPosition = new Vector2(20, -60);
            var ctImg = container.GetComponent<Image>();
            ctImg.color = new Color(0.08f, 0.04f, 0.08f, 0.7f);

            // 图标
            var icon = CreateText(ctRt, "Icon", "👑", 28);
            var iconRt = (RectTransform)icon.transform;
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(40, 40);
            iconRt.anchoredPosition = new Vector2(20, 0);

            // 状态文字
            _ultimateStatusText = CreateText(ctRt, "StatusText", "⚔ 3精英击杀后解锁", 18);
            var txtRt = (RectTransform)_ultimateStatusText.transform;
            txtRt.anchorMin = new Vector2(0, 0.5f);
            txtRt.anchorMax = new Vector2(0, 0.5f);
            txtRt.sizeDelta = new Vector2(220, 36);
            txtRt.anchoredPosition = new Vector2(80, 0);
            _ultimateStatusText.color = ColorUltimateLocked;
            _ultimateStatusText.fontStyle = FontStyle.Bold;
            _ultimateStatusText.alignment = TextAnchor.MiddleLeft;
        }

        // =====================================================================
        // 5. 队伍成员列表（右侧）
        // =====================================================================
        private void BuildTeamList()
        {
            var container = new GameObject("TeamList", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(transform, false);
            var ctRt = container.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(1, 0.5f);
            ctRt.anchorMax = new Vector2(1, 0.5f);
            ctRt.sizeDelta = new Vector2(260, 420);
            ctRt.anchoredPosition = new Vector2(-20, 0);
            var ctImg = container.GetComponent<Image>();
            ctImg.color = ColorTeamBg;

            // 标题
            var title = CreateText(ctRt, "Title", "👥 队伍成员", 20);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0.5f, 1);
            titleRt.anchorMax = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(220, 28);
            titleRt.anchoredPosition = new Vector2(0, -14);
            title.color = ColorSectionTitle;
            title.fontStyle = FontStyle.Bold;

            // 成员列表容器（用于动态生成）
            _teamListContainer = new GameObject("MembersContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            _teamListContainer.transform.SetParent(ctRt, false);
            var mcRt = _teamListContainer.GetComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0, 0);
            mcRt.anchorMax = new Vector2(1, 1);
            mcRt.sizeDelta = new Vector2(-10, -50);
            mcRt.anchoredPosition = new Vector2(0, -10);
            mcRt.pivot = new Vector2(0.5f, 1);

            // 默认队伍数据
            if (TeamMembers == null || TeamMembers.Length == 0)
            {
                TeamMembers = new TeamMemberInfo[]
                {
                    new TeamMemberInfo { Name = "我", ClassName = "纯阳·剑纯", HpPercent = 1f },
                    new TeamMemberInfo { Name = "队友A", ClassName = "万花·奶花", HpPercent = 0.85f },
                    new TeamMemberInfo { Name = "队友B", ClassName = "少林·易筋", HpPercent = 0.92f },
                    new TeamMemberInfo { Name = "队友C", ClassName = "七秀·冰心", HpPercent = 0.78f },
                };
            }

            int count = TeamMembers.Length;
            _teamNameTexts = new Text[count];
            _teamClassTexts = new Text[count];
            _teamHpTexts = new Text[count];
            _teamHpFills = new Image[count];

            for (int i = 0; i < count; i++)
            {
                var m = TeamMembers[i];
                var row = new GameObject("Member" + i, typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_teamListContainer, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1);
                rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 44);
                rowRt.anchoredPosition = new Vector2(0, -i * 48);
                rowRt.pivot = new Vector2(0.5f, 1);
                var rowImg = row.GetComponent<Image>();
                rowImg.color = new Color(0.1f, 0.1f, 0.18f, 0.5f);

                // 名称
                _teamNameTexts[i] = CreateText(rowRt, "Name", m.Name, 17);
                var nameRt = (RectTransform)_teamNameTexts[i].transform;
                nameRt.anchorMin = new Vector2(0, 1);
                nameRt.anchorMax = new Vector2(0, 1);
                nameRt.sizeDelta = new Vector2(80, 22);
                nameRt.anchoredPosition = new Vector2(8, -10);
                _teamNameTexts[i].color = ColorTextBright;
                _teamNameTexts[i].fontStyle = FontStyle.Bold;
                _teamNameTexts[i].alignment = TextAnchor.MiddleLeft;

                // 职业
                _teamClassTexts[i] = CreateText(rowRt, "Class", m.ClassName, 14);
                var classRt = (RectTransform)_teamClassTexts[i].transform;
                classRt.anchorMin = new Vector2(0, 0);
                classRt.anchorMax = new Vector2(0, 0);
                classRt.sizeDelta = new Vector2(80, 18);
                classRt.anchoredPosition = new Vector2(8, 4);
                _teamClassTexts[i].color = ColorTextDim;
                _teamClassTexts[i].alignment = TextAnchor.MiddleLeft;

                // HP条背景
                var hpBg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
                hpBg.transform.SetParent(rowRt, false);
                var hpBgRt = hpBg.GetComponent<RectTransform>();
                hpBgRt.anchorMin = new Vector2(0.5f, 0.5f);
                hpBgRt.anchorMax = new Vector2(0.5f, 0.5f);
                hpBgRt.sizeDelta = new Vector2(100, 14);
                hpBgRt.anchoredPosition = new Vector2(30, 0);
                hpBg.GetComponent<Image>().color = ColorTeamHpBg;

                // HP填充
                var hpFill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
                hpFill.transform.SetParent(hpBgRt, false);
                var hpFillRt = hpFill.GetComponent<RectTransform>();
                hpFillRt.anchorMin = Vector2.zero;
                hpFillRt.anchorMax = new Vector2(m.HpPercent, 1);
                hpFillRt.sizeDelta = Vector2.zero;
                hpFillRt.anchoredPosition = Vector2.zero;
                _teamHpFills[i] = hpFill.GetComponent<Image>();
                _teamHpFills[i].color = ColorTeamHpFill;
                _teamHpFills[i].type = Image.Type.Filled;
                _teamHpFills[i].fillMethod = Image.FillMethod.Horizontal;
                _teamHpFills[i].fillAmount = m.HpPercent;

                // HP百分比文字
                _teamHpTexts[i] = CreateText(hpBgRt, "HpText", Mathf.CeilToInt(m.HpPercent * 100) + "%", 13);
                var hpTxtRt = (RectTransform)_teamHpTexts[i].transform;
                hpTxtRt.anchorMin = Vector2.zero; hpTxtRt.anchorMax = Vector2.one;
                hpTxtRt.sizeDelta = Vector2.zero;
                _teamHpTexts[i].color = Color.white;
                _teamHpTexts[i].alignment = TextAnchor.MiddleCenter;
            }
        }

        // =====================================================================
        // 6. 退出副本按钮
        // =====================================================================
        private void BuildExitButton()
        {
            _exitBtn = CreateButton(transform as RectTransform, "ExitBtn", "✕ 退出副本", () =>
            {
                Debug.Log("[Dungeon] 退出副本");
                OnExitDungeon();
            });

            var btnRt = (RectTransform)_exitBtn.transform;
            btnRt.anchorMin = new Vector2(1, 0);
            btnRt.anchorMax = new Vector2(1, 0);
            btnRt.sizeDelta = new Vector2(160, 50);
            btnRt.anchoredPosition = new Vector2(-100, 40);
            var btnImg = _exitBtn.GetComponent<Image>();
            btnImg.color = ColorExitBtn;

            // 修改按钮文字颜色
            var btnText = _exitBtn.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.fontSize = 22;
                btnText.color = Color.white;
            }
        }

        // =====================================================================
        // 7. 运行时更新
        // =====================================================================
        void Update()
        {
            if (!_running) return;

            // 倒计时
            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining < 0) _timeRemaining = 0;

            int minutes = Mathf.FloorToInt(_timeRemaining / 60);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60);
            _timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            // 倒计时<60秒变闪烁
            if (_timeRemaining <= 60f)
            {
                float blink = Mathf.PingPong(Time.time * 4f, 1f);
                _timerText.color = new Color(1f, 0.1f, 0.05f, blink);
            }

            // Boss血量
            if (BossMaxHp > 0)
            {
                float pct = Mathf.Clamp01(BossCurrentHp / BossMaxHp);
                _bossHpFill.fillAmount = pct;
                _bossHpPercentText.text = Mathf.CeilToInt(pct * 100) + "%";
                _bossHpPercentText.color = pct > 0.3f ? Color.white : new Color(1f, 0.5f, 0.3f);
                _bossHpFill.color = pct > 0.3f ? ColorBossHpFill : new Color(1f, 0.3f, 0.1f);
            }

            // 小Boss状态刷新
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

            // 终极Boss状态
            bool allKilled = true;
            for (int i = 0; i < MinibossKilled.Length; i++)
            {
                if (!MinibossKilled[i]) { allKilled = false; break; }
            }
            UltimateBossUnlocked = allKilled;
            if (UltimateBossUnlocked)
            {
                _ultimateStatusText.text = "🔥 终极Boss已解锁!";
                _ultimateStatusText.color = ColorUltimateUnlocked;
            }
            else
            {
                int remaining = 0;
                for (int i = 0; i < MinibossKilled.Length; i++)
                    if (!MinibossKilled[i]) remaining++;
                _ultimateStatusText.text = "⚔ 还需击杀 " + remaining + " 精英解锁";
                _ultimateStatusText.color = ColorUltimateLocked;
            }

            // 队伍成员血量刷新
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
        // 8. 公开方法
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

        public void SetTimeLimit(float seconds)
        {
            TimeLimitSeconds = seconds;
            _timeRemaining = seconds;
        }

        public void UpdateTeamMember(int index, float hpPercent)
        {
            if (TeamMembers != null && index >= 0 && index < TeamMembers.Length)
                TeamMembers[index].HpPercent = hpPercent;
        }

        protected virtual void OnExitDungeon()
        {
            _running = false;
            // 返回副本选择场景
            SceneManager.Instance.LoadScene(GameScene.DungeonSelect);
        }

        protected override void OnShow() { _running = true; }
        protected override void OnHide() { _running = false; }

        public override void Refresh()
        {
            base.Refresh();
            _timeRemaining = TimeLimitSeconds;
            _running = true;
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