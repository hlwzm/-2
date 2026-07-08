using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 主城面板 - 主城场景的HUD界面
    /// 顶部玩家信息 + 功能入口网格 + 公告区 + 快捷操作
    /// </summary>
    public class MainCityPanel : BasePanel
    {
        // ===== 文本引用（用于刷新） =====
        private Text _nameText, _levelText, _classText;
        private Text _goldText, _tongbaoText, _staminaText;
        private Text _noticeText;
        
        // ===== 配色方案（与LoginPanel统一风格） =====
        private static readonly Color ColorTopBar = new Color(0.06f, 0.06f, 0.12f, 0.85f);
        private static readonly Color ColorBottomBar = new Color(0.06f, 0.06f, 0.12f, 0.85f);
        private static readonly Color ColorEntryBg = new Color(0.1f, 0.1f, 0.2f, 0.7f);
        private static readonly Color ColorNoticeBg = new Color(0.08f, 0.08f, 0.16f, 0.75f);
        private static readonly Color ColorQuickBg = new Color(0.1f, 0.08f, 0.18f, 0.8f);
        private static readonly Color ColorGold = new Color(1f, 0.75f, 0.2f);
        private static readonly Color ColorTongbao = new Color(0.5f, 0.8f, 1f);
        private static readonly Color ColorStamina = new Color(0.4f, 1f, 0.4f);
        private static readonly Color ColorClass = new Color(0.6f, 0.8f, 1f);
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorTextDim = new Color(0.6f, 0.6f, 0.7f);

        // ===== 8大功能入口配置 =====
        private static readonly string[] EntryNames = { "英雄", "副本", "竞技", "交易行", "背包", "任务", "好友", "同盟" };
        private static readonly string[] EntryIcons = { "⚔", "🏰", "⚡", "💰", "🎒", "📜", "👥", "🏛" };

        protected override void Awake()
        {
            base.Awake();
            BuildTopPlayerBar();
            BuildFunctionGrid();
            BuildNoticeArea();
            BuildQuickActionPanel();
            RefreshPlayerData();
        }

        // =====================================================================
        // 1. 顶部玩家信息栏
        // =====================================================================
        private void BuildTopPlayerBar()
        {
            // --- 背景条 ---
            var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(transform, false);
            var topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0, 1);
            topBarRt.anchorMax = new Vector2(1, 1);
            topBarRt.sizeDelta = new Vector2(0, 72);
            topBarRt.anchoredPosition = new Vector2(0, -36);
            var topBarImg = topBar.GetComponent<Image>();
            topBarImg.color = ColorTopBar;

            // --- 底部装饰线 ---
            var line = new GameObject("BottomLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(topBarRt, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 0);
            lineRt.anchorMax = new Vector2(1, 0);
            lineRt.sizeDelta = new Vector2(0, 2);
            lineRt.anchoredPosition = new Vector2(0, 0);
            line.GetComponent<Image>().color = ColorAccent;

            // --- 头像（圆形占位） ---
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(topBarRt, false);
            var avatarRt = avatar.GetComponent<RectTransform>();
            avatarRt.anchorMin = new Vector2(0, 0.5f);
            avatarRt.anchorMax = new Vector2(0, 0.5f);
            avatarRt.sizeDelta = new Vector2(54, 54);
            avatarRt.anchoredPosition = new Vector2(40, 0);
            var avatarImg = avatar.GetComponent<Image>();
            avatarImg.color = new Color(0.3f, 0.2f, 0.5f);

            // --- 玩家名称 ---
            _nameText = CreateTextWithParent(topBarRt, "NameText", "未命名", 22, FontStyle.Bold, Color.white);
            _nameText.alignment = TextAnchor.MiddleLeft;
            var nameRt = (RectTransform)(RectTransform)_nameText.transform;
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(0, 0.5f);
            nameRt.sizeDelta = new Vector2(140, 30);
            nameRt.anchoredPosition = new Vector2(110, 12);

            // --- 等级 ---
            _levelText = CreateTextWithParent(topBarRt, "LevelText", "Lv.1", 16, FontStyle.Normal, ColorGold);
            _levelText.alignment = TextAnchor.MiddleLeft;
            var lvRt = (RectTransform)(RectTransform)_levelText.transform;
            lvRt.anchorMin = new Vector2(0, 0.5f);
            lvRt.anchorMax = new Vector2(0, 0.5f);
            lvRt.sizeDelta = new Vector2(60, 24);
            lvRt.anchoredPosition = new Vector2(110, -10);

            // --- 职业 ---
            _classText = CreateTextWithParent(topBarRt, "ClassText", "侠士", 14, FontStyle.Normal, ColorClass);
            _classText.alignment = TextAnchor.MiddleLeft;
            var clsRt = (RectTransform)(RectTransform)_classText.transform;
            clsRt.anchorMin = new Vector2(0, 0.5f);
            clsRt.anchorMax = new Vector2(0, 0.5f);
            clsRt.sizeDelta = new Vector2(60, 24);
            clsRt.anchoredPosition = new Vector2(170, -10);

            // --- 金币 ---
            _goldText = CreateTextWithParent(topBarRt, "GoldText", "000000", 18, FontStyle.Normal, ColorGold);
            _goldText.alignment = TextAnchor.MiddleLeft;
            AddIconBefore(topBarRt, (RectTransform)(RectTransform)_goldText.transform, "GoldIcon", ColorGold, "💰", 36);
            var gRt = (RectTransform)(RectTransform)_goldText.transform;
            gRt.anchorMin = new Vector2(0, 0.5f);
            gRt.anchorMax = new Vector2(0, 0.5f);
            gRt.sizeDelta = new Vector2(140, 30);
            gRt.anchoredPosition = new Vector2(370, 12);

            // --- 通宝 ---
            _tongbaoText = CreateTextWithParent(topBarRt, "TongbaoText", "000000", 18, FontStyle.Normal, ColorTongbao);
            _tongbaoText.alignment = TextAnchor.MiddleLeft;
            AddIconBefore(topBarRt, (RectTransform)(RectTransform)_tongbaoText.transform, "TongbaoIcon", ColorTongbao, "💎", 36);
            var tRt = (RectTransform)(RectTransform)_tongbaoText.transform;
            tRt.anchorMin = new Vector2(0, 0.5f);
            tRt.anchorMax = new Vector2(0, 0.5f);
            tRt.sizeDelta = new Vector2(140, 30);
            tRt.anchoredPosition = new Vector2(570, 12);

            // --- 体力 ---
            _staminaText = CreateTextWithParent(topBarRt, "StaminaText", "120/120", 18, FontStyle.Normal, ColorStamina);
            _staminaText.alignment = TextAnchor.MiddleLeft;
            AddIconBefore(topBarRt, (RectTransform)(RectTransform)_staminaText.transform, "StaminaIcon", ColorStamina, "⚡", 36);
            var sRt = (RectTransform)(RectTransform)_staminaText.transform;
            sRt.anchorMin = new Vector2(0, 0.5f);
            sRt.anchorMax = new Vector2(0, 0.5f);
            sRt.sizeDelta = new Vector2(140, 30);
            sRt.anchoredPosition = new Vector2(770, 12);

            // --- 设置按钮 ---
            var settingsBtn = CreateButton(topBarRt, "SettingsBtn", "⚙", () => Debug.Log("[MainCity] 打开设置"));
            var setRt = settingsBtn.GetComponent<RectTransform>();
            setRt.anchorMin = new Vector2(1, 0.5f);
            setRt.anchorMax = new Vector2(1, 0.5f);
            setRt.sizeDelta = new Vector2(50, 50);
            setRt.anchoredPosition = new Vector2(-30, 0);
        }

        // =====================================================================
        // 2. 8大功能入口网格（2行 x 4列）
        // =====================================================================
        private void BuildFunctionGrid()
        {
            var gridGo = new GameObject("FunctionGrid", typeof(RectTransform));
            gridGo.transform.SetParent(transform, false);
            var gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.5f, 0.5f);
            gridRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridRt.sizeDelta = new Vector2(900, 420);
            gridRt.anchoredPosition = new Vector2(0, 90);

            // 标题
            var title = CreateTextWithParent(gridRt, "GridTitle", "—— 功能入口 ——", 18, FontStyle.Normal, ColorTextDim);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.sizeDelta = new Vector2(0, 30);
            titleRt.anchoredPosition = new Vector2(0, -15);

            int cols = 4;
            int rows = 2;
            float cellW = 200;
            float cellH = 170;
            float startX = -(cols * cellW) / 2 + cellW / 2;
            float startY = (rows * cellH) / 2 - cellH / 2 - 40;

            for (int i = 0; i < EntryNames.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = startX + col * cellW;
                float y = startY - row * cellH;

                CreateEntryButton(gridRt, i, x, y, cellW - 20, cellH - 10);
            }
        }

        private void CreateEntryButton(RectTransform parent, int index, float x, float y, float w, float h)
        {
            var name = EntryNames[index];
            var icon = EntryIcons[index];

            // --- 按钮背景 ---
            var go = new GameObject("Entry_" + name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);

            var img = go.GetComponent<Image>();
            img.color = ColorEntryBg;

            // --- 点击区域 ---
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnEntryClick(index));

            // --- 图标文字（大号表情符号作为图标占位） ---
            var iconText = CreateTextWithParent(rt, "IconText", icon, 36, FontStyle.Normal, Color.white);
            var iconRt = (RectTransform)iconText.transform;
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(50, 50);
            iconRt.anchoredPosition = new Vector2(0, 15);

            // --- 名称文字 ---
            var label = CreateTextWithParent(rt, "Label", name, 20, FontStyle.Bold, Color.white);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.sizeDelta = new Vector2(w, 30);
            labelRt.anchoredPosition = new Vector2(0, -35);
        }

        // =====================================================================
        // 3. 系统公告区
        // =====================================================================
        private void BuildNoticeArea()
        {
            var noticeGo = new GameObject("NoticeArea", typeof(RectTransform), typeof(Image));
            noticeGo.transform.SetParent(transform, false);
            var noticeRt = noticeGo.GetComponent<RectTransform>();
            noticeRt.anchorMin = new Vector2(0.5f, 0.5f);
            noticeRt.anchorMax = new Vector2(0.5f, 0.5f);
            noticeRt.sizeDelta = new Vector2(860, 56);
            noticeRt.anchoredPosition = new Vector2(0, -145);
            var noticeImg = noticeGo.GetComponent<Image>();
            noticeImg.color = ColorNoticeBg;

            // 左侧装饰条
            var decoLine = new GameObject("DecoLine", typeof(RectTransform), typeof(Image));
            decoLine.transform.SetParent(noticeRt, false);
            var decoRt = decoLine.GetComponent<RectTransform>();
            decoRt.anchorMin = new Vector2(0, 0);
            decoRt.anchorMax = new Vector2(0, 1);
            decoRt.sizeDelta = new Vector2(3, -6);
            decoRt.anchoredPosition = new Vector2(2, 0);
            decoLine.GetComponent<Image>().color = ColorAccent;

            // 公告标签
            var label = CreateTextWithParent(noticeRt, "Label", "📢 公告", 18, FontStyle.Bold, ColorGold);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = new Vector2(0, 0.5f);
            labelRt.anchorMax = new Vector2(0, 0.5f);
            labelRt.sizeDelta = new Vector2(80, 30);
            labelRt.anchoredPosition = new Vector2(20, 0);

            // 公告内容
            _noticeText = CreateTextWithParent(noticeRt, "Content",
                "欢迎来到指尖江湖！新版本已上线，组队副本开启！", 16, FontStyle.Normal, ColorTextDim);
            _noticeText.alignment = TextAnchor.MiddleLeft;
            var nRt = (RectTransform)(RectTransform)_noticeText.transform;
            nRt.anchorMin = new Vector2(0, 0.5f);
            nRt.anchorMax = new Vector2(1, 0.5f);
            nRt.sizeDelta = new Vector2(-120, 30);
            nRt.anchoredPosition = new Vector2(60, 0);

            // 查看全部按钮
            var viewAllBtn = CreateButton(noticeRt, "ViewAllBtn", "详情", () => Debug.Log("[MainCity] 查看全部公告"));
            var vRt = viewAllBtn.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(1, 0.5f);
            vRt.anchorMax = new Vector2(1, 0.5f);
            vRt.sizeDelta = new Vector2(70, 30);
            vRt.anchoredPosition = new Vector2(-10, 0);
            var vImg = viewAllBtn.GetComponent<Image>();
            vImg.color = new Color(0.3f, 0.2f, 0.5f, 0.7f);
            var vText = viewAllBtn.GetComponentInChildren<Text>();
            vText.fontSize = 14;
        }

        // =====================================================================
        // 4. 快捷操作面板（底部）
        // =====================================================================
        private void BuildQuickActionPanel()
        {
            // --- 底部背景条 ---
            var bottomBar = new GameObject("QuickActionBar", typeof(RectTransform), typeof(Image));
            bottomBar.transform.SetParent(transform, false);
            var bottomRt = bottomBar.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0);
            bottomRt.sizeDelta = new Vector2(0, 90);
            bottomRt.anchoredPosition = new Vector2(0, 45);
            var bottomImg = bottomBar.GetComponent<Image>();
            bottomImg.color = ColorBottomBar;

            // 顶部装饰线
            var topLine = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
            topLine.transform.SetParent(bottomRt, false);
            var topLineRt = topLine.GetComponent<RectTransform>();
            topLineRt.anchorMin = new Vector2(0, 1);
            topLineRt.anchorMax = new Vector2(1, 1);
            topLineRt.sizeDelta = new Vector2(0, 2);
            topLineRt.anchoredPosition = new Vector2(0, 0);
            topLine.GetComponent<Image>().color = ColorAccent;

            // 3个快捷操作
            float[] qx = { -300f, 0f, 300f };
            string[] qNames = { "每日任务", "在线奖励", "队伍招募" };
            string[] qDescs = { "完成日常获得奖励", "累计在线领好礼", "快速匹配队伍" };
            string[] qIcons = { "📋", "🎁", "📯" };

            for (int i = 0; i < 3; i++)
            {
                CreateQuickAction(bottomRt, qNames[i], qDescs[i], qIcons[i], qx[i], i);
            }
        }

        private void CreateQuickAction(RectTransform parent, string title, string desc, string icon, float x, int index)
        {
            var go = new GameObject("Quick_" + title, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260, 68);
            rt.anchoredPosition = new Vector2(x, 0);

            var img = go.GetComponent<Image>();
            img.color = ColorQuickBg;

            // 点击
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var idx = index;
            btn.onClick.AddListener(() => OnQuickActionClick(idx));

            // 图标
            var iconText = CreateTextWithParent(rt, "Icon", icon, 28, FontStyle.Normal, Color.white);
            var iconRt = (RectTransform)iconText.transform;
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(40, 40);
            iconRt.anchoredPosition = new Vector2(18, 0);

            // 标题
            var titleText = CreateTextWithParent(rt, "Title", title, 18, FontStyle.Bold, Color.white);
            titleText.alignment = TextAnchor.MiddleLeft;
            var tRt = (RectTransform)titleText.transform;
            tRt.anchorMin = new Vector2(0, 0.5f);
            tRt.anchorMax = new Vector2(0, 0.5f);
            tRt.sizeDelta = new Vector2(130, 24);
            tRt.anchoredPosition = new Vector2(60, 8);

            // 描述
            var descText = CreateTextWithParent(rt, "Desc", desc, 13, FontStyle.Normal, ColorTextDim);
            descText.alignment = TextAnchor.MiddleLeft;
            var dRt = (RectTransform)descText.transform;
            dRt.anchorMin = new Vector2(0, 0.5f);
            dRt.anchorMax = new Vector2(0, 0.5f);
            dRt.sizeDelta = new Vector2(150, 20);
            dRt.anchoredPosition = new Vector2(60, -12);

            // 右侧箭头
            var arrow = CreateTextWithParent(rt, "Arrow", "＞", 20, FontStyle.Normal, ColorTextDim);
            var aRt = (RectTransform)arrow.transform;
            aRt.anchorMin = new Vector2(1, 0.5f);
            aRt.anchorMax = new Vector2(1, 0.5f);
            aRt.sizeDelta = new Vector2(20, 20);
            aRt.anchoredPosition = new Vector2(-8, 0);
        }

        // =====================================================================
        // 5. 数据刷新
        // =====================================================================
        public override void Refresh()
        {
            base.Refresh();
            RefreshPlayerData();
        }

        private void RefreshPlayerData()
        {
            var p = GameManager.Instance.Player;
            _nameText.text = string.IsNullOrEmpty(p.Name) ? "未命名" : p.Name;
            _levelText.text = "Lv." + p.Level;
            _classText.text = "侠士";
            _goldText.text = string.Format("{0:N0}", p.Gold);
            _tongbaoText.text = string.Format("{0:N0}", p.Tongbao);
            _staminaText.text = "120/120";
        }

        public void UpdateNotice(string message)
        {
            if (_noticeText != null)
                _noticeText.text = message;
        }

        // =====================================================================
        // 6. 点击事件
        // =====================================================================
        private void OnEntryClick(int index)
        {
            Debug.Log($"[MainCity] 点击功能入口: {EntryNames[index]}");

            switch (index)
            {
                case 0: // 英雄
                    UIManager.Instance.Show<HeroListPanel>();
                    break;
                case 1: // 副本
                    SceneManager.Instance.LoadScene(GameScene.DungeonSelect);
                    break;
                case 2: // 竞技
                    SceneManager.Instance.LoadScene(GameScene.PVP);
                    break;
                case 3: // 交易行
                    UIManager.Instance.Show<TradePanel>();
                    break;
                case 4: // 背包
                    UIManager.Instance.Show<BagPanel>();
                    break;
                case 5: // 任务
                    UIManager.Instance.Show<QuestPanel>();
                    break;
                case 6: // 好友
                    UIManager.Instance.Show<FriendPanel>();
                    break;
                case 7: // 同盟
                    UIManager.Instance.Show<GuildPanel>();
                    break;
            }
        }

        private void OnQuickActionClick(int index)
        {
            string[] names = { "每日任务", "在线奖励", "队伍招募" };
            Debug.Log($"[MainCity] 点击快捷操作: {names[index]}");

            switch (index)
            {
                case 0: // 每日任务
                    UIManager.Instance.Show<QuestPanel>();
                    break;
                case 1: // 在线奖励
                    Debug.Log("[MainCity] 打开在线奖励面板");
                    break;
                case 2: // 队伍招募
                    Debug.Log("[MainCity] 打开队伍招募面板");
                    break;
            }
        }

        // =====================================================================
        // 7. 辅助方法
        // =====================================================================
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

        private static void AddIconBefore(RectTransform parent, RectTransform target, string iconName,
            Color iconColor, string iconChar, float size)
        {
            var iconGo = new GameObject(iconName, typeof(RectTransform));
            iconGo.transform.SetParent(parent, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(size, size);
            // 将图标放在目标文字左侧偏移位置
            iconRt.anchoredPosition = target.anchoredPosition + new Vector2(-target.sizeDelta.x * 0.5f - size * 0.4f, 0);
        }
    }
}