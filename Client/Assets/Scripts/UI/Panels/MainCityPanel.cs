using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 主城面板 - 顶部玩家信息 + 功能入口网格 + 公告区 + 快捷操作
    /// 暗黑武侠风格 · 紫色调 · 全程序化生成
    /// </summary>
    public class MainCityPanel : BasePanel
    {
        private Text _nameText, _levelText, _classText;
        private Text _goldText, _tongbaoText, _staminaText;
        private Text _noticeText;

        private static readonly string[] EntryNames = { "英雄", "副本", "竞技", "交易行", "背包", "任务", "好友", "同盟" };
        private static readonly string[] EntryIcons = { "⚔", "🏰", "⚡", "💰", "🎒", "📜", "👥", "🏛" };

        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);

            BuildTopBar(root);
            BuildFunctionGrid(root);
            BuildNoticeArea(root);
            BuildQuickActions(root);

            RefreshPlayerData();
        }

        // ═══════════ 1. 顶部栏 ═══════════
        private void BuildTopBar(RectTransform root)
        {
            var bar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(0, 72); rt.anchoredPosition = new Vector2(0, -36);
            bar.GetComponent<Image>().color = new Color(0.047f, 0.039f, 0.031f, 0.85f);

            // Accent bottom line
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(rt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 2); lrt.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().color = ThemeColors.Accent;

            // Avatar placeholder
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(rt, false);
            var art = avatar.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 0.5f); art.anchorMax = new Vector2(0, 0.5f);
            art.sizeDelta = new Vector2(54, 54); art.anchoredPosition = new Vector2(40, 0);
            avatar.GetComponent<Image>().color = ThemeColors.AccentDim;

            // Name / Level / Class
            _nameText = PlaceText(rt, "Name", "未命名", ThemeColors.FontEntry, FontStyle.Bold, Color.white,
                TextAnchor.MiddleLeft, 140, 30, 108, 12);
            _levelText = PlaceText(rt, "Level", "Lv.1", ThemeColors.FontSmall, FontStyle.Normal, ThemeColors.Gold,
                TextAnchor.MiddleLeft, 60, 24, 108, -10);
            _classText = PlaceText(rt, "Class", "侠士", ThemeColors.FontSmall, FontStyle.Normal, ThemeColors.Tongbao,
                TextAnchor.MiddleLeft, 60, 24, 168, -10);

            // Currency labels
            _goldText = UIComponentFactory.CreateCurrencyLabel(rt, "Gold", "💰", "000000", ThemeColors.Gold,
                new Vector2(370, 12));
            _tongbaoText = UIComponentFactory.CreateCurrencyLabel(rt, "Tongbao", "💎", "000000", ThemeColors.Tongbao,
                new Vector2(570, 12));
            _staminaText = UIComponentFactory.CreateCurrencyLabel(rt, "Stamina", "⚡", "120/120", ThemeColors.Stamina,
                new Vector2(770, 12));

            // Settings button
            var settingsBtn = UIComponentFactory.CreateIconButton(rt, "Settings", "⚙", 44, () => Debug.Log("[MainCity] Settings"));
            var srt = settingsBtn.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(1, 0.5f); srt.anchorMax = new Vector2(1, 0.5f);
            srt.anchoredPosition = new Vector2(-30, 0);
        }

        // ═══════════ 2. 功能入口网格 ═══════════
        private void BuildFunctionGrid(RectTransform root)
        {
            var grid = new GameObject("FunctionGrid", typeof(RectTransform));
            grid.transform.SetParent(root, false);
            var grt = grid.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f); grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(900, 420); grt.anchoredPosition = new Vector2(0, 90);

            // Title
            var title = UIComponentFactory.CreateText(grt, "Title", "—— 功能入口 ——",
                ThemeColors.FontSmall, ThemeColors.TextDim);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = Vector2.one;
            trt.sizeDelta = new Vector2(0, 30); trt.anchoredPosition = new Vector2(0, -15);

            int cols = 4, rows = 2;
            float cellW = 200, cellH = 170;
            float startX = -(cols * cellW) / 2 + cellW / 2;
            float startY = (rows * cellH) / 2 - cellH / 2 - 40;

            for (int i = 0; i < EntryNames.Length; i++)
            {
                int col = i % cols, row = i / cols;
                float x = startX + col * cellW, y = startY - row * cellH;
                CreateEntry(grt, i, new Vector2(cellW - 20, cellH - 10), new Vector2(x, y));
            }
        }

        private void CreateEntry(RectTransform parent, int index, Vector2 size, Vector2 pos)
        {
            var card = UIComponentFactory.CreateCard(parent, "Entry_" + EntryNames[index], size, pos);

            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card.GetComponent<Image>();
            int idx = index;
            btn.onClick.AddListener(() => OnEntryClick(idx));

            // Icon
            var icon = UIComponentFactory.CreateText(card, "Icon", EntryIcons[index],
                36, Color.white); icon.raycastTarget = false;
            var irt = icon.rectTransform;
            irt.anchorMin = new Vector2(0.5f, 0.5f); irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(50, 50); irt.anchoredPosition = new Vector2(0, 15);

            // Label
            var label = UIComponentFactory.CreateText(card, "Label", EntryNames[index],
                ThemeColors.FontEntry, Color.white); label.fontStyle = FontStyle.Bold; label.raycastTarget = false;
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.5f, 0.5f); lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(size.x, 30); lrt.anchoredPosition = new Vector2(0, -35);
        }

        // ═══════════ 3. 公告区 ═══════════
        private void BuildNoticeArea(RectTransform root)
        {
            var notice = new GameObject("NoticeArea", typeof(RectTransform), typeof(Image));
            notice.transform.SetParent(root, false);
            var nrt = notice.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0.5f); nrt.anchorMax = new Vector2(0.5f, 0.5f);
            nrt.sizeDelta = new Vector2(860, 56); nrt.anchoredPosition = new Vector2(0, -145);
            notice.GetComponent<Image>().color = ThemeColors.BgCard;

            // Left accent line
            var deco = new GameObject("Deco", typeof(RectTransform), typeof(Image));
            deco.transform.SetParent(nrt, false);
            var drt = deco.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 0); drt.anchorMax = new Vector2(0, 1);
            drt.sizeDelta = new Vector2(3, -6); drt.anchoredPosition = new Vector2(2, 0);
            deco.GetComponent<Image>().color = ThemeColors.Accent;

            // Label
            var label = UIComponentFactory.CreateText(nrt, "Label", "📙 公告",
                ThemeColors.FontBody, ThemeColors.Gold); label.alignment = TextAnchor.MiddleCenter;
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(0, 0.5f);
            lrt.sizeDelta = new Vector2(80, 30); lrt.anchoredPosition = new Vector2(20, 0);

            // Content
            _noticeText = UIComponentFactory.CreateText(nrt, "Content",
                "欢迎来到指尖江湖！新版本已上线，组队副本开启！",
                ThemeColors.FontSmall, ThemeColors.TextDim);
            _noticeText.alignment = TextAnchor.MiddleLeft;
            var crt = _noticeText.rectTransform;
            crt.anchorMin = new Vector2(0, 0.5f); crt.anchorMax = Vector2.one;
            crt.sizeDelta = new Vector2(-120, 30); crt.anchoredPosition = new Vector2(60, 0);

            // Detail button
            var detailBtn = UIComponentFactory.CreateSecondaryButton(nrt, "Detail", "详情", () => { });
            var drt2 = detailBtn.GetComponent<RectTransform>();
            drt2.anchorMin = new Vector2(1, 0.5f); drt2.anchorMax = new Vector2(1, 0.5f);
            drt2.sizeDelta = new Vector2(60, 30); drt2.anchoredPosition = new Vector2(-10, 0);
        }

        // ═══════════ 4. 快捷操作 ═══════════
        private void BuildQuickActions(RectTransform root)
        {
            var bar = new GameObject("QuickBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(root, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = Vector2.right;
            brt.sizeDelta = new Vector2(0, 90); brt.anchoredPosition = new Vector2(0, 45);
            bar.GetComponent<Image>().color = new Color(0.047f, 0.039f, 0.031f, 0.85f);

            // Top accent line
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(brt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 2); lrt.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().color = ThemeColors.Accent;

            string[] names = { "每日任务", "在线奖励", "队伍招募" };
            string[] descs = { "完成日常获得奖励", "累计在线领好礼", "快速匹配队伍" };
            string[] icons = { "📵", "🎴", "📨" };
            float[] xs = { -300f, 0f, 300f };

            for (int i = 0; i < 3; i++)
            {
                var card = UIComponentFactory.CreateCard(brt, "Quick_" + names[i],
                    new Vector2(260, 68), new Vector2(xs[i], 0));

                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card.GetComponent<Image>();
                int idx = i;
                btn.onClick.AddListener(() => OnQuickClick(idx));

                var icon = UIComponentFactory.CreateText(card, "Icon", icons[idx], 28, Color.white);
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = new Vector2(0, 0.5f);
                icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(40, 40);
                icon.rectTransform.anchoredPosition = new Vector2(18, 0);

                var t = UIComponentFactory.CreateText(card, "Title", names[idx],
                    ThemeColors.FontBody, Color.white); t.alignment = TextAnchor.MiddleLeft;
                t.rectTransform.anchorMin = new Vector2(0, 0.5f); t.rectTransform.anchorMax = new Vector2(0, 0.5f);
                t.rectTransform.sizeDelta = new Vector2(130, 24); t.rectTransform.anchoredPosition = new Vector2(60, 8);

                var d = UIComponentFactory.CreateText(card, "Desc", descs[idx],
                    ThemeColors.FontTiny, ThemeColors.TextDim); d.alignment = TextAnchor.MiddleLeft;
                d.rectTransform.anchorMin = new Vector2(0, 0.5f); d.rectTransform.anchorMax = new Vector2(0, 0.5f);
                d.rectTransform.sizeDelta = new Vector2(150, 20); d.rectTransform.anchoredPosition = new Vector2(60, -12);

                var arrow = UIComponentFactory.CreateText(card, "Arrow", "›",
                    ThemeColors.FontBody, ThemeColors.TextDim);
                arrow.rectTransform.anchorMin = new Vector2(1, 0.5f);
                arrow.rectTransform.anchorMax = new Vector2(1, 0.5f);
                arrow.rectTransform.sizeDelta = new Vector2(20, 20);
                arrow.rectTransform.anchoredPosition = new Vector2(-8, 0);
            }
        }

        // ═══════════ 5. Data ═══════════
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
            _goldText.text = $"💰 {p.Gold:N0}";
            _tongbaoText.text = $"💎 {p.Tongbao:N0}";
            _staminaText.text = $"⚡ 120/120";
        }

        public void UpdateNotice(string msg) { if (_noticeText != null) _noticeText.text = msg; }

        // ═══════════ 6. Events ═══════════
        private void OnEntryClick(int index)
        {
            Debug.Log($"[MainCity] 点击: {EntryNames[index]}");
            UIManager.Instance.Hide<MainCityPanel>();

            switch (index)
            {
                case 0: UIManager.Instance.Show<HeroListPanel>(); break;
                case 1: SceneManager.Instance.LoadScene(GameScene.DungeonSelect); break;
                case 2: SceneManager.Instance.LoadScene(GameScene.PVP); break;
                case 3: UIManager.Instance.Show<TradePanel>(); break;
                case 4: UIManager.Instance.Show<BagPanel>(); break;
                case 5: UIManager.Instance.Show<QuestPanel>(); break;
                case 6: UIManager.Instance.Show<FriendPanel>(); break;
                case 7: UIManager.Instance.Show<GuildPanel>(); break;
            }
        }

        private void OnQuickClick(int index)
        {
            Debug.Log($"[MainCity] 快捷操作: {index}");
            if (index == 0) { UIManager.Instance.Hide<MainCityPanel>(); UIManager.Instance.Show<QuestPanel>(); }
        }

        // ═══════════ 7. Helpers ═══════════
        private static Text PlaceText(RectTransform parent, string name, string text,
            int size, FontStyle style, Color color, TextAnchor anchor,
            float w, float h, float x, float y)
        {
            var t = UIComponentFactory.CreateText(parent, name, text, size, color);
            t.fontStyle = style; t.alignment = anchor;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, y);
            return t;
        }
    }
}
