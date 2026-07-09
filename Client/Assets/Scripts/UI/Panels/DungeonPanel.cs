#nullable disable
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;
using System.Collections;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 鍓湰鍐呮垬鏂楅潰鏉?- 闄愭椂鍊掕鏃?Boss琛€閲?灏廈oss鐘舵€?缁堟瀬Boss/闃熶紞鍒楄〃/閫€鍑?    /// 澧炲己鐗? 鍊掕鏃跺け璐?缁堟瀬瑙ｉ攣鍔ㄧ敾/闃舵鏄剧ず
    /// 鍏ㄧ▼搴忓寲鐢熸垚锛屾殫榛戠传鑹蹭富棰?    /// </summary>
    public class DungeonPanel : BasePanel
    {
        // ===== 鍏叡鏁版嵁锛堝彲鐢卞閮ㄨ缃級 =====
        public int DungeonId { get; set; }
        public float TimeLimitSeconds { get; set; } = 480f; // 8鍒嗛挓
        public float BossMaxHp { get; set; } = 100000f;
        public float BossCurrentHp { get; set; } = 100000f;
        public string BossName { get; set; } = "钁ｉ緳";
        public bool[] MinibossKilled { get; private set; } = new bool[3];
        public string[] MinibossNames { get; set; } = { "绮捐嫳鎶ゅ崼", "鏆楀奖鍒哄", "姣掗浘鏈＋" };
        public bool UltimateBossUnlocked { get; set; }
        public TeamMemberInfo[] TeamMembers { get; set; }

        // 鍓湰闃舵
        public int DungeonPhase { get; set; } = 1; // 1=闃舵1, 2=闃舵2

        // ===== 浜嬩欢鍥炶皟 =====
        public System.Action OnDungeonFailed;   // 鍊掕鏃跺綊闆舵垨鍥㈢伃
        public System.Action OnAllMinibossKilled; // 涓夊皬Boss鍏ㄥ嚮鏉€

        // ===== UI寮曠敤 =====
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

        // 闃舵鏄剧ずUI
        private Text _phaseText;
        private GameObject _phaseFlashGo;

        // 缁堟瀬Boss瑙ｉ攣鍔ㄧ敾
        private GameObject _unlockFlashGo;
        private Text _unlockFlashText;
        private float _unlockAnimTime = 0f;
        private bool _isPlayingUnlockAnim = false;

        // 鍓湰澶辫触UI
        private GameObject _failOverlay;
        private bool _failed = false;

        // ===== 閰嶈壊 =====
        private static readonly Color ColorBg = new Color(0.04f, 0.04f, 0.08f, 0.75f);
        private static readonly Color ColorPanelBg = new Color(0.047f, 0.039f, 0.031f, 0.85f);
        private static readonly Color ColorAccent = new Color(0.54f, 0.42f, 0.16f, 0.8f);
        private static readonly Color ColorBossHpBg = new Color(0.15f, 0.05f, 0.05f);
        private static readonly Color ColorBossHpFill = new Color(0.9f, 0.15f, 0.1f);
        private static readonly Color ColorBossHpGlow = new Color(1f, 0.2f, 0.1f, 0.3f);
        private static readonly Color ColorMinibossDone = new Color(0.3f, 1f, 0.3f);
        private static readonly Color ColorMinibossPending = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorUltimateLocked = new Color(1f, 0.6f, 0.1f);
        private static readonly Color ColorUltimateUnlocked = new Color(1f, 0.9f, 0.1f);
        private static readonly Color ColorTeamBg = new Color(0.12f, 0.10f, 0.09f, 0.8f);
        private static readonly Color ColorTeamHpFill = new Color(0.2f, 0.8f, 0.3f);
        private static readonly Color ColorTeamHpBg = new Color(0.1f, 0.1f, 0.15f);
        private static readonly Color ColorTextDim = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorTextBright = new Color(0.94f, 0.91f, 0.85f);
        private static readonly Color ColorExitBtn = new Color(0.5f, 0.1f, 0.1f, 0.85f);
        private static readonly Color ColorSectionTitle = new Color(0.54f, 0.42f, 0.16f);
        private static readonly Color ColorPhase1 = new Color(0.5f, 0.8f, 1f, 0.9f);
        private static readonly Color ColorPhase2 = new Color(1f, 0.5f, 0.2f, 0.9f);

        // ===== 杩愯鏃舵暟鎹?=====
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
            // ===== 鍏ㄥ睆鍗婇€忔槑鑳屾櫙 =====
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
        // 1. 闄愭椂鍊掕鏃讹紙椤堕儴灞呬腑锛屽ぇ鍙风孩鑹叉暟瀛楋級
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

            var label = CreateLabel(ctRt, "Label", "鈴?鍓╀綑鏃堕棿", 14, TextAnchor.MiddleCenter,
                ColorTextDim, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(180, 25), new Vector2(0, -5));

            _timerText = CreateLabel(ctRt, "Timer", "00:00", 36, TextAnchor.MiddleCenter,
                new Color(1f, 0.2f, 0.1f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(190, 45), new Vector2(0, -30));
            _timerText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 2. 鍓湰闃舵鏄剧ず锛堣鏃跺櫒涓嬫柟锛?        // =====================================================================
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

            _phaseText = CreateLabel(phaseRt, "PhaseText", "闃舵 1", 18, TextAnchor.MiddleCenter,
                ColorPhase1, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            _phaseText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 3. Boss琛€鏉★紙闃舵鏄剧ず涓嬫柟锛?        // =====================================================================
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

            _bossNameText = CreateLabel(bossRt, "BossName", "鈼?" + BossName + " 鈼?,
                16, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.2f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(380, 20), new Vector2(0, -3));

            // 琛€鏉¤儗鏅?            var hpBarBg = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image));
            hpBarBg.transform.SetParent(bossRt, false);
            var hpBarBgRt = hpBarBg.GetComponent<RectTransform>();
            hpBarBgRt.anchorMin = new Vector2(0.5f, 0);
            hpBarBgRt.anchorMax = new Vector2(0.5f, 0);
            hpBarBgRt.sizeDelta = new Vector2(360, 20);
            hpBarBgRt.anchoredPosition = new Vector2(0, 7);
            var bgImg = hpBarBg.GetComponent<Image>();
            bgImg.color = ColorBossHpBg;

            // 琛€鏉″～鍏?            var hpFill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            hpFill.transform.SetParent(hpBarBgRt, false);
            var hpFillRt = hpFill.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero; hpFillRt.anchorMax = Vector2.one;
            hpFillRt.sizeDelta = Vector2.zero;
            _bossHpFill = hpFill.GetComponent<Image>();
            _bossHpFill.type = Image.Type.Filled;
            _bossHpFill.fillMethod = Image.FillMethod.Horizontal;
            _bossHpFill.color = ColorBossHpFill;

            // 鐧惧垎姣旀枃瀛?            _bossHpPercentText = CreateLabel(hpBarBgRt, "HpPercent", "100%",
                13, TextAnchor.MiddleCenter, ColorTextBright,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        }

        // =====================================================================
        // 4. 灏廈oss鐘舵€佸垪琛紙Boss琛€鏉′笅鏂癸級
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

            CreateLabel(miniRt, "Title", "鈹佲攣 绮捐嫳璁ㄤ紣杩涘害 鈹佲攣", 13, TextAnchor.MiddleCenter,
                ColorSectionTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(380, 22), new Vector2(0, -5));

            for (int i = 0; i < 3; i++)
            {
                var idx = i;
                _minibossStatusTexts[i] = CreateLabel(miniRt, "Mini" + i, "鈽?" + MinibossNames[i],
                    14, TextAnchor.MiddleLeft, ColorMinibossPending,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(380, 18), new Vector2(10, -30 - i * 18));
            }
        }

        // =====================================================================
        // 5. 缁堟瀬Boss瑙ｉ攣鐘舵€侊紙灏廈oss鍒楄〃涓嬫柟锛?        // =====================================================================
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

            _ultimateStatusText = CreateLabel(ultRt, "UltStatus", "鈿?闇€鍑绘潃3绮捐嫳瑙ｉ攣缁堟瀬Boss",
                14, TextAnchor.MiddleCenter, ColorUltimateLocked,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        }

        // =====================================================================
        // 6. 闃熶紞鎴愬憳鍒楄〃锛堝彸涓嬭锛?        // =====================================================================
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

            CreateLabel(teamRt, "Title", "鈽?闃熶紞 鈽?, 15, TextAnchor.MiddleCenter,
                ColorSectionTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(190, 25), new Vector2(0, -5));

            _teamListContainer = new GameObject("TeamList", typeof(RectTransform)).GetComponent<RectTransform>();
            _teamListContainer.SetParent(teamRt, false);
            _teamListContainer.anchorMin = new Vector2(0, 0);
            _teamListContainer.anchorMax = new Vector2(1, 1);
            _teamListContainer.sizeDelta = new Vector2(-10, -40);
            _teamListContainer.anchoredPosition = new Vector2(0, -20);

            // 榛樿8浜洪槦浼?            int teamSize = 8;
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

                _teamNameTexts[i] = CreateLabel(rowRt, "Name", "渚犲" + (i + 1),
                    12, TextAnchor.MiddleLeft, ColorTextBright,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(70, 20), new Vector2(5, 0));

                _teamClassTexts[i] = CreateLabel(rowRt, "Class", "闂ㄦ淳",
                    10, TextAnchor.MiddleLeft, ColorTextDim,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 16), new Vector2(68, 0));

                // 琛€閲忔潯
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
        // 7. 閫€鍑烘寜閽紙鍙充笂瑙掞級
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
            btnTxt.text = "閫€鍑哄壇鏈?;
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 14;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = new Color(0.9f, 0.7f, 0.7f);

            _exitBtn = btnGo.GetComponent<Button>();
            _exitBtn.targetGraphic = btnImg;
            _exitBtn.onClick.AddListener(OnExitDungeon);
        }

        // =====================================================================
        // 8. 鍓湰澶辫触瑕嗙洊灞?        // =====================================================================
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

            var failText = CreateLabel(failRt, "FailText", "鉂?鍓湰澶辫触 鉂?,
                48, TextAnchor.MiddleCenter, new Color(1f, 0.1f, 0.1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600, 80), Vector2.zero);
            failText.fontStyle = FontStyle.Bold;

            var subText = CreateLabel(failRt, "SubText", "鏃堕棿鑰楀敖锛岃閲嶆暣鏃楅紦",
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

            var retText = CreateLabel(retRt, "RetText", "杩斿洖鍓湰閫夋嫨", 18, TextAnchor.MiddleCenter,
                new Color(0.9f, 0.6f, 0.6f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        }

        // =====================================================================
        // 9. 缁堟瀬Boss瑙ｉ攣闂儊鍔ㄧ敾瑕嗙洊灞?        // =====================================================================
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
                "馃敟 缁堟瀬Boss瑙ｉ攣锛?馃敟", 42, TextAnchor.MiddleCenter, ColorUltimateUnlocked,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            _unlockFlashText.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // 10. Update寰幆
        // =====================================================================
        void Update()
        {
            if (!_running || _failed) return;

            // 1. 鍊掕鏃舵洿鏂颁笌瓒呮椂妫€娴?            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining < 0) _timeRemaining = 0;

            int minutes = Mathf.FloorToInt(_timeRemaining / 60);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60);
            _timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            // 鍊掕鏃跺綊闆?鈫?鍓湰澶辫触
            if (_timeRemaining <= 0f && !_timeoutTriggered)
            {
                _timeoutTriggered = true;
                TriggerDungeonFail("鈴?鏃堕棿鑰楀敖锛?);
                return;
            }

            // 鍊掕鏃?60绉掑彉闂儊
            if (_timeRemaining <= 60f && _timeRemaining > 0)
            {
                float blink = Mathf.PingPong(Time.time * 4f, 1f);
                _timerText.color = new Color(1f, 0.1f, 0.05f, blink);
            }

            // 2. Boss琛€閲?            if (BossMaxHp > 0)
            {
                float pct = Mathf.Clamp01(BossCurrentHp / BossMaxHp);
                _bossHpFill.fillAmount = pct;
                _bossHpPercentText.text = Mathf.CeilToInt(pct * 100) + "%";
                _bossHpPercentText.color = pct > 0.3f ? Color.white : new Color(1f, 0.5f, 0.3f);
                _bossHpFill.color = pct > 0.3f ? ColorBossHpFill : new Color(1f, 0.3f, 0.1f);
            }

            // 3. 鍓湰闃舵鏄剧ず
            UpdatePhaseDisplay();

            // 4. 灏廈oss鐘舵€佸埛鏂?            for (int i = 0; i < _minibossStatusTexts.Length && i < MinibossKilled.Length; i++)
            {
                if (MinibossKilled[i])
                {
                    _minibossStatusTexts[i].text = "鈽?" + MinibossNames[i];
                    _minibossStatusTexts[i].color = ColorMinibossDone;
                }
                else
                {
                    _minibossStatusTexts[i].text = "鈽?" + MinibossNames[i];
                    _minibossStatusTexts[i].color = ColorMinibossPending;
                }
            }

            // 5. 缁堟瀬Boss瑙ｉ攣妫€娴?            bool allKilled = true;
            for (int i = 0; i < MinibossKilled.Length; i++)
            {
                if (!MinibossKilled[i]) { allKilled = false; break; }
            }

            bool wasUnlocked = UltimateBossUnlocked;
            UltimateBossUnlocked = allKilled;

            if (UltimateBossUnlocked)
            {
                _ultimateStatusText.text = "馃敟 缁堟瀬Boss宸茶В閿?";
                _ultimateStatusText.color = ColorUltimateUnlocked;

                // 瑙﹀彂瑙ｉ攣鍔ㄧ敾锛堥娆¤В閿佹椂锛?                if (!_unlockTriggered)
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
                _ultimateStatusText.text = "鈿?杩橀渶鍑绘潃 " + remaining + " 绮捐嫳瑙ｉ攣";
                _ultimateStatusText.color = ColorUltimateLocked;
            }

            // 6. 瑙ｉ攣鍔ㄧ敾鏇存柊
            if (_isPlayingUnlockAnim)
            {
                UpdateUnlockAnimation();
            }

            // 7. 闃熶紞鎴愬憳琛€閲忓埛鏂?            if (TeamMembers != null)
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
        // 闃舵鏄剧ず鏇存柊
        // =====================================================================
        private void UpdatePhaseDisplay()
        {
            // 鏍规嵁褰撳墠Boss琛€閲忓喅瀹氶樁娈垫樉绀?            float hpPct = BossMaxHp > 0 ? BossCurrentHp / BossMaxHp : 1f;
            int targetPhase = hpPct <= 0.5f ? 2 : 1;

            if (targetPhase != DungeonPhase)
            {
                DungeonPhase = targetPhase;
                if (DungeonPhase == 2)
                {
                    Debug.Log("[DungeonPanel] Boss杩涘叆绗簩闃舵锛?);
                }
            }

            if (DungeonPhase == 1)
            {
                _phaseText.text = "闃舵 1";
                _phaseText.color = ColorPhase1;
            }
            else
            {
                // 闃舵2闂儊鏁堟灉
                float blink = Mathf.PingPong(Time.time * 3f, 1f);
                _phaseText.text = "鈿?闃舵 2 鈿?;
                _phaseText.color = new Color(1f, 0.5f, 0.2f, 0.6f + blink * 0.4f);
            }
        }

        // =====================================================================
        // 缁堟瀬Boss瑙ｉ攣鍔ㄧ敾
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
                // 鍔ㄧ敾缁撴潫
                _isPlayingUnlockAnim = false;
                _unlockFlashGo.SetActive(false);
                return;
            }

            // 闂儊 + 缂╂斁鑴夊啿
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
        // 鍓湰澶辫触瑙﹀彂
        // =====================================================================
        private void TriggerDungeonFail(string reason)
        {
            if (_failed) return;
            _failed = true;
            _running = false;

            Debug.Log($"[DungeonPanel] 鍓湰澶辫触: {reason}");
            _failOverlay.SetActive(true);
            OnDungeonFailed?.Invoke();
        }

        // =====================================================================
        // 鍏紑鏂规硶
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
                _bossNameText.text = "鈼?" + name + " 鈼?;
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
        // Helper: 鍒涘缓鏍囩
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
    /// 闃熶紞鎴愬憳淇℃伅
    /// </summary>
    [System.Serializable]
    public class TeamMemberInfo
    {
        public string Name;
        public string ClassName;
        public float HpPercent = 1f;
    }
}