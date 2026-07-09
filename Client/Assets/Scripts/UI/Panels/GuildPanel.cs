using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 同盟面板
    /// 未加入同盟：展示同盟列表，可搜索 / 创建 / 查看申请列表
    /// 已加入同盟：展示同盟信息、成员列表、同盟任务，可退出同盟
    /// 金墨武侠风格 · 全程序化生成
    /// </summary>
    public class GuildPanel : BasePanel
    {
        private bool _hasGuild = false;
        private GameObject _noGuildView;
        private GameObject _inGuildView;

        // ── Demo Guilds (for the apply list) ──
        private static readonly string[][] DemoGuilds = {
            new[] { "剑指苍穹", "3", "23", "50" },
            new[] { "风云再起", "5", "45", "50" },
            new[] { "笑傲江湖", "2", "12", "50" },
            new[] { "龙吟九天", "4", "38", "50" },
            new[] { "一剑霜寒", "6", "50", "50" },
            new[] { "醉卧沙场", "7", "47", "50" },
        };

        // ── Demo Members (when in guild) ──
        private static readonly string[][] DemoMembers = {
            new[] { "盟主",   "剑破苍穹", "Lv.50", "今日活跃" },
            new[] { "副盟主", "风轻云淡", "Lv.48", "今日活跃" },
            new[] { "长老",   "一剑封喉", "Lv.45", "3天前"   },
            new[] { "精英",   "白云城主", "Lv.42", "今日活跃" },
            new[] { "精英",   "天涯过客", "Lv.40", "1天前"   },
            new[] { "成员",   "落花有意", "Lv.35", "今日活跃" },
            new[] { "成员",   "清风明月", "Lv.33", "2天前"   },
            new[] { "成员",   "醉卧沙场", "Lv.30", "今日活跃" },
        };

        // ── Demo Tasks ──
        private static readonly string[][] DemoTasks = {
            new[] { "同盟捐献", "捐献金币或元宝为同盟积累资金", "500/1000" },
            new[] { "组队副本", "完成3次副本挑战",               "2/3"     },
            new[] { "同盟争霸", "参与本周同盟战",                 "0/1"     },
        };

        // =============================================================
        // Lifecycle
        // =============================================================
        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);
            UIComponentFactory.CreateTitleBar(root, "同盟", () => BackToMain());

            // No-guild view container (stretches to fill root)
            _noGuildView = CreateContainer(root, "NoGuildView");
            BuildNoGuildView(_noGuildView.GetComponent<RectTransform>());

            // In-guild view container (stretches to fill root)
            _inGuildView = CreateContainer(root, "InGuildView");
            BuildInGuildView(_inGuildView.GetComponent<RectTransform>());

            // Shared back button
            var backBtn = UIComponentFactory.CreateSecondaryButton(root, "Back", "返回主城", () => BackToMain());
            PlaceBottomCenter(backBtn.GetComponent<RectTransform>(), 180, 44, 0, 30);

            UpdateView();
        }

        // =============================================================
        // 1. No-Guild View
        // =============================================================
        private void BuildNoGuildView(RectTransform parent)
        {
            // "未加入同盟" message
            var msg = UIComponentFactory.CreateText(parent, "NoGuildMsg",
                "未加入同盟", ThemeColors.FontPanelTitle, ThemeColors.Accent);
            msg.fontStyle = FontStyle.Bold;
            msg.alignment = TextAnchor.MiddleCenter;
            PlaceTopCenter(msg.rectTransform, 500, 40, 0, -90);

            var subMsg = UIComponentFactory.CreateText(parent, "NoGuildSubMsg",
                "浏览同盟列表，找到属于你的江湖归属",
                ThemeColors.FontSmall, ThemeColors.TextDim);
            subMsg.alignment = TextAnchor.MiddleCenter;
            PlaceTopCenter(subMsg.rectTransform, 600, 24, 0, -132);

            // Search input + button row
            var searchInput = UIComponentFactory.CreateInputField(parent, "SearchInput",
                "🔍 输入同盟名称搜索...", new Vector2(460, 40), Vector2.zero);
            var srt = searchInput.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 1f);
            srt.anchorMax = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(-130, -178);

            var searchBtn = UIComponentFactory.CreatePrimaryButton(parent, "SearchBtn",
                "搜索", () => Debug.Log("[Guild] 搜索同盟"));
            PlaceTopCenter(searchBtn.GetComponent<RectTransform>(), 100, 40, 130, -178);

            // Three action buttons: [🔍 搜索同盟] [创建同盟] [申请列表]
            float btnY = -228;
            var searchActionBtn = UIComponentFactory.CreatePrimaryButton(parent, "SearchActionBtn",
                "🔍 搜索同盟", () => Debug.Log("[Guild] 搜索同盟"));
            PlaceTopCenter(searchActionBtn.GetComponent<RectTransform>(), 160, 40, -170, btnY);

            var createBtn = UIComponentFactory.CreatePrimaryButton(parent, "CreateBtn",
                "创建同盟", () => JoinDemoGuild());
            PlaceTopCenter(createBtn.GetComponent<RectTransform>(), 160, 40, 0, btnY);

            var applyListBtn = UIComponentFactory.CreateSecondaryButton(parent, "ApplyListBtn",
                "申请列表", () => Debug.Log("[Guild] 查看申请列表"));
            PlaceTopCenter(applyListBtn.GetComponent<RectTransform>(), 160, 40, 170, btnY);

            // Section header
            var header = UIComponentFactory.CreateText(parent, "ListHeader",
                "── 同盟列表 ──", ThemeColors.FontSmall, ThemeColors.TextDim);
            PlaceTopCenter(header.rectTransform, 200, 30, 0, -278);

            // ScrollView with guild cards
            var content = UIComponentFactory.CreateScrollView(parent, "GuildList",
                new Vector2(880, 640), new Vector2(0, -100));

            foreach (var g in DemoGuilds)
                CreateGuildCard(content, g[0], int.Parse(g[1]), int.Parse(g[2]), int.Parse(g[3]));
        }

        private void CreateGuildCard(RectTransform parent, string name, int level, int current, int max)
        {
            var card = UIComponentFactory.CreateCard(parent, "Guild_" + name,
                new Vector2(840, 80), Vector2.zero);
            card.GetComponent<Image>().color = ThemeColors.BgListItem;

            // Guild icon
            var icon = UIComponentFactory.CreateText(card, "Icon", "🏯", 32, ThemeColors.Accent);
            icon.rectTransform.anchorMin = new Vector2(0, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
            icon.rectTransform.sizeDelta = new Vector2(44, 44);
            icon.rectTransform.anchoredPosition = new Vector2(20, 0);

            // Guild name
            var nameText = UIComponentFactory.CreateText(card, "Name", name,
                ThemeColors.FontBody, ThemeColors.TextBright);
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            nameText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            nameText.rectTransform.sizeDelta = new Vector2(220, 28);
            nameText.rectTransform.anchoredPosition = new Vector2(72, 12);

            // Level + member count (X/50)
            bool full = current >= max;
            var info = UIComponentFactory.CreateText(card, "Info",
                $"Lv.{level}  ·  成员 {current}/{max}",
                ThemeColors.FontSmall, full ? ThemeColors.TextDim : ThemeColors.TextNormal);
            info.alignment = TextAnchor.MiddleLeft;
            info.rectTransform.anchorMin = new Vector2(0, 0.5f);
            info.rectTransform.anchorMax = new Vector2(0, 0.5f);
            info.rectTransform.sizeDelta = new Vector2(280, 24);
            info.rectTransform.anchoredPosition = new Vector2(72, -14);

            // Apply button (disabled if full)
            Button applyBtn;
            if (full)
            {
                applyBtn = UIComponentFactory.CreateSecondaryButton(card, "Apply", "已满", () => { });
                applyBtn.interactable = false;
            }
            else
            {
                applyBtn = UIComponentFactory.CreatePrimaryButton(card, "Apply", "申请",
                    () => ApplyToGuild(name));
            }
            var art = applyBtn.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1, 0.5f);
            art.anchorMax = new Vector2(1, 0.5f);
            art.sizeDelta = new Vector2(100, 36);
            art.anchoredPosition = new Vector2(-20, 0);
        }

        // =============================================================
        // 2. In-Guild View
        // =============================================================
        private void BuildInGuildView(RectTransform parent)
        {
            // Guild info card
            var infoCard = UIComponentFactory.CreateCard(parent, "GuildInfo",
                new Vector2(880, 110), new Vector2(0, 355));
            infoCard.GetComponent<Image>().color = ThemeColors.BgCard;

            // Guild icon
            var icon = UIComponentFactory.CreateText(infoCard, "Icon", "🏯", 40, ThemeColors.Accent);
            icon.rectTransform.anchorMin = new Vector2(0, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
            icon.rectTransform.sizeDelta = new Vector2(50, 50);
            icon.rectTransform.anchoredPosition = new Vector2(20, 8);

            // Guild name
            var guildName = UIComponentFactory.CreateText(infoCard, "GuildName", "剑指苍穹",
                ThemeColors.FontPanelTitle, ThemeColors.TextBright);
            guildName.fontStyle = FontStyle.Bold;
            guildName.alignment = TextAnchor.MiddleLeft;
            guildName.rectTransform.anchorMin = new Vector2(0, 0.5f);
            guildName.rectTransform.anchorMax = new Vector2(0, 0.5f);
            guildName.rectTransform.sizeDelta = new Vector2(300, 36);
            guildName.rectTransform.anchoredPosition = new Vector2(82, 18);

            // Level + funds + member count
            var guildDetail = UIComponentFactory.CreateText(infoCard, "GuildDetail",
                "Lv.5  ·  同盟资金 12,580  ·  成员 45/50",
                ThemeColors.FontSmall, ThemeColors.TextNormal);
            guildDetail.alignment = TextAnchor.MiddleLeft;
            guildDetail.rectTransform.anchorMin = new Vector2(0, 0.5f);
            guildDetail.rectTransform.anchorMax = new Vector2(0, 0.5f);
            guildDetail.rectTransform.sizeDelta = new Vector2(500, 24);
            guildDetail.rectTransform.anchoredPosition = new Vector2(82, -16);

            // Exit guild button (danger style)
            var exitBtn = UIComponentFactory.CreateButton(infoCard, "ExitGuild", "退出同盟",
                ThemeColors.BtnDanger, () => LeaveGuild(), ThemeColors.FontSmall);
            var ert = exitBtn.GetComponent<RectTransform>();
            ert.anchorMin = new Vector2(1, 0.5f);
            ert.anchorMax = new Vector2(1, 0.5f);
            ert.sizeDelta = new Vector2(110, 38);
            ert.anchoredPosition = new Vector2(-16, 0);

            // ScrollView with members + tasks
            var content = UIComponentFactory.CreateScrollView(parent, "GuildDetail",
                new Vector2(880, 640), new Vector2(0, -100));

            // Members section
            CreateSectionHeader(content, "── 成员列表 (45/50) ──");
            foreach (var m in DemoMembers)
                CreateMemberRow(content, m[0], m[1], m[2], m[3]);

            // Tasks section
            CreateSectionHeader(content, "── 同盟任务 ──");
            foreach (var t in DemoTasks)
                CreateTaskRow(content, t[0], t[1], t[2]);
        }

        private void CreateSectionHeader(RectTransform parent, string text)
        {
            var go = new GameObject("SectionHeader", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(840, 36);

            var header = UIComponentFactory.CreateText(rt, "Text", text,
                ThemeColors.FontSmall, ThemeColors.Accent);
            header.fontStyle = FontStyle.Bold;
            header.alignment = TextAnchor.MiddleCenter;
            header.rectTransform.anchorMin = Vector2.zero;
            header.rectTransform.anchorMax = Vector2.one;
            header.rectTransform.sizeDelta = Vector2.zero;
        }

        private void CreateMemberRow(RectTransform parent, string role, string name,
            string level, string lastActive)
        {
            var card = UIComponentFactory.CreateCard(parent, "Member_" + name,
                new Vector2(840, 60), Vector2.zero);
            card.GetComponent<Image>().color = ThemeColors.BgListItem;

            // Role badge with quality color
            Color roleColor = role switch
            {
                "盟主"   => ThemeColors.QualityLegend,
                "副盟主" => ThemeColors.QualityEpic,
                "长老"   => ThemeColors.QualityRare,
                "精英"   => ThemeColors.QualityGood,
                _         => ThemeColors.TextNormal
            };
            var roleText = UIComponentFactory.CreateText(card, "Role", role,
                ThemeColors.FontTiny, roleColor);
            roleText.fontStyle = FontStyle.Bold;
            roleText.alignment = TextAnchor.MiddleCenter;
            roleText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            roleText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            roleText.rectTransform.sizeDelta = new Vector2(56, 24);
            roleText.rectTransform.anchoredPosition = new Vector2(12, 0);

            // Name
            var nameText = UIComponentFactory.CreateText(card, "Name", name,
                ThemeColors.FontBody, ThemeColors.TextBright);
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            nameText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            nameText.rectTransform.sizeDelta = new Vector2(200, 30);
            nameText.rectTransform.anchoredPosition = new Vector2(76, 0);

            // Level
            var lvText = UIComponentFactory.CreateText(card, "Level", level,
                ThemeColors.FontSmall, ThemeColors.Gold);
            lvText.alignment = TextAnchor.MiddleLeft;
            lvText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            lvText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            lvText.rectTransform.sizeDelta = new Vector2(80, 30);
            lvText.rectTransform.anchoredPosition = new Vector2(280, 0);

            // Last active
            var activeText = UIComponentFactory.CreateText(card, "Active", lastActive,
                ThemeColors.FontTiny, ThemeColors.TextDim);
            activeText.alignment = TextAnchor.MiddleRight;
            activeText.rectTransform.anchorMin = new Vector2(1, 0.5f);
            activeText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            activeText.rectTransform.sizeDelta = new Vector2(120, 24);
            activeText.rectTransform.anchoredPosition = new Vector2(-16, 0);
        }

        private void CreateTaskRow(RectTransform parent, string name, string desc, string progress)
        {
            var card = UIComponentFactory.CreateCard(parent, "Task_" + name,
                new Vector2(840, 76), Vector2.zero);
            card.GetComponent<Image>().color = ThemeColors.BgListItem;

            // Task name
            var nameText = UIComponentFactory.CreateText(card, "Name", name,
                ThemeColors.FontBody, ThemeColors.TextBright);
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            nameText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            nameText.rectTransform.sizeDelta = new Vector2(200, 28);
            nameText.rectTransform.anchoredPosition = new Vector2(20, 14);

            // Description
            var descText = UIComponentFactory.CreateText(card, "Desc", desc,
                ThemeColors.FontTiny, ThemeColors.TextDim);
            descText.alignment = TextAnchor.MiddleLeft;
            descText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            descText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            descText.rectTransform.sizeDelta = new Vector2(400, 20);
            descText.rectTransform.anchoredPosition = new Vector2(20, -14);

            // Progress
            var progText = UIComponentFactory.CreateText(card, "Progress", progress,
                ThemeColors.FontSmall, ThemeColors.Gold);
            progText.alignment = TextAnchor.MiddleRight;
            progText.rectTransform.anchorMin = new Vector2(1, 0.5f);
            progText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            progText.rectTransform.sizeDelta = new Vector2(100, 24);
            progText.rectTransform.anchoredPosition = new Vector2(-120, 14);

            // Go button
            var goBtn = UIComponentFactory.CreateSecondaryButton(card, "GoBtn", "前往", () => { });
            var grt = goBtn.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(1, 0.5f);
            grt.anchorMax = new Vector2(1, 0.5f);
            grt.sizeDelta = new Vector2(80, 32);
            grt.anchoredPosition = new Vector2(-16, 0);
        }

        // =============================================================
        // 3. View Switching
        // =============================================================
        private void UpdateView()
        {
            if (_noGuildView != null) _noGuildView.SetActive(!_hasGuild);
            if (_inGuildView != null) _inGuildView.SetActive(_hasGuild);
        }

        private void JoinDemoGuild()
        {
            _hasGuild = true;
            UpdateView();
            Debug.Log("[Guild] 已加入同盟");
        }

        private void LeaveGuild()
        {
            _hasGuild = false;
            UpdateView();
            Debug.Log("[Guild] 已退出同盟");
        }

        private void ApplyToGuild(string guildName)
        {
            Debug.Log($"[Guild] 已申请加入同盟: {guildName}");
        }

        private void BackToMain()
        {
            UIManager.Instance.Hide<GuildPanel>();
            UIManager.Instance.Show<MainCityPanel>();
        }

        // =============================================================
        // 4. Layout Helpers
        // =============================================================
        private static GameObject CreateContainer(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        private static void PlaceTopCenter(RectTransform rt, float w, float h, float x, float y)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        private static void PlaceBottomCenter(RectTransform rt, float w, float h, float x, float y)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }
    }
}
