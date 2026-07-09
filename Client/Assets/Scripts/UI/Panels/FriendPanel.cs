using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 好友面板 — 好友列表 / 申请列表 / 最近组队
    /// 金墨武侠风格 · 全程使用 UIComponentFactory + ThemeColors
    /// </summary>
    public class FriendPanel : BasePanel
    {
        // ── Data Model ──────────────────────────────────────

        private class FriendData
        {
            public string Name;
            public int Level;
            public bool Online;
            public string TeamInfo;
        }

        private enum TabType { FriendList, ApplyList, RecentTeam }
        private static readonly string[] TabNames = { "好友列表", "申请列表", "最近组队" };

        // ── State ────────────────────────────────────────────

        private TabType _currentTab = TabType.FriendList;
        private string _searchFilter = "";

        // ── UI References ────────────────────────────────────

        private Text _onlineCountText;
        private RectTransform _scrollContent;
        private readonly List<Button> _tabButtons = new List<Button>();
        private InputField _searchInput;

        // ── Demo Data ────────────────────────────────────────

        private readonly List<FriendData> _friends = new List<FriendData>();
        private readonly List<FriendData> _applyList = new List<FriendData>();
        private readonly List<FriendData> _recentTeam = new List<FriendData>();

        // ════════════════════════════════════════════════════
        //  Lifecycle
        // ════════════════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            InitDemoData();

            var root = transform as RectTransform;
            UIComponentFactory.CreateBackground(root);

            BuildTitleBar(root);
            BuildSearchBar(root);
            BuildTabBar(root);
            BuildScrollList(root);
            BuildBackButton(root);

            RefreshList();
            UpdateOnlineCount();
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshList();
            UpdateOnlineCount();
        }

        // ════════════════════════════════════════════════════
        //  Build UI
        // ════════════════════════════════════════════════════

        // ── 1. Title Bar ────────────────────────────────────

        private void BuildTitleBar(RectTransform root)
        {
            UIComponentFactory.CreateTitleBar(root, "好友", () => BackToMain());

            // Online count — placed in the title-bar area, left of the close button
            _onlineCountText = UIComponentFactory.CreateText(root, "OnlineCount", "在线: 0/0",
                ThemeColors.FontSmall, ThemeColors.Stamina);
            _onlineCountText.alignment = TextAnchor.MiddleRight;
            var rt = _onlineCountText.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(140, 30);
            rt.anchoredPosition = new Vector2(-180, -28);
        }

        // ── 2. Search Bar ────────────────────────────────────

        private void BuildSearchBar(RectTransform root)
        {
            // Full-width container row
            var row = new GameObject("SearchRow", typeof(RectTransform));
            row.transform.SetParent(root, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1);
            rrt.anchorMax = new Vector2(1, 1);
            rrt.sizeDelta = new Vector2(0, 44);
            rrt.anchoredPosition = new Vector2(0, -80);

            // Search input — left side
            _searchInput = UIComponentFactory.CreateInputField(rrt, "SearchInput",
                "🔍 搜索玩家...", new Vector2(500, 40), Vector2.zero);
            var srt = _searchInput.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0.5f);
            srt.anchorMax = new Vector2(0, 0.5f);
            srt.anchoredPosition = new Vector2(60, 0);
            _searchInput.onValueChanged.AddListener(v =>
            {
                _searchFilter = v;
                RefreshList();
            });

            // Add button — right side
            var addBtn = UIComponentFactory.CreatePrimaryButton(rrt, "AddBtn", "添加",
                () => { Debug.Log("[FriendPanel] 添加好友请求"); });
            var art = addBtn.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1, 0.5f);
            art.anchorMax = new Vector2(1, 0.5f);
            art.sizeDelta = new Vector2(100, 40);
            art.anchoredPosition = new Vector2(-60, 0);
        }

        // ── 3. Tab Bar ──────────────────────────────────────

        private void BuildTabBar(RectTransform root)
        {
            var row = new GameObject("TabRow", typeof(RectTransform));
            row.transform.SetParent(root, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1);
            rrt.anchorMax = new Vector2(1, 1);
            rrt.sizeDelta = new Vector2(0, 40);
            rrt.anchoredPosition = new Vector2(0, -140);

            float[] tabX = { -300, 0, 300 };
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var tab = UIComponentFactory.CreateTabButton(rrt, "Tab" + i, TabNames[i],
                    i == 0, () => SwitchTab(idx));
                var rt = tab.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(180, 40);
                rt.anchoredPosition = new Vector2(tabX[i], 0);
                _tabButtons.Add(tab);
            }
        }

        // ── 4. Scroll List ──────────────────────────────────

        private void BuildScrollList(RectTransform root)
        {
            _scrollContent = UIComponentFactory.CreateScrollView(root, "FriendListScroll",
                new Vector2(880, 440), new Vector2(0, -50));
        }

        // ── 5. Back Button ──────────────────────────────────

        private void BuildBackButton(RectTransform root)
        {
            var backBtn = UIComponentFactory.CreateSecondaryButton(root, "Back", "返回主城",
                () => BackToMain());
            var rt = backBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(180, 44);
            rt.anchoredPosition = new Vector2(0, 30);
        }

        // ════════════════════════════════════════════════════
        //  List Management
        // ════════════════════════════════════════════════════

        private void RefreshList()
        {
            if (_scrollContent == null) return;
            ClearContent();

            var list = GetCurrentData();
            foreach (var fd in list)
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !fd.Name.Contains(_searchFilter))
                    continue;
                CreateFriendCard(_scrollContent, fd);
            }
        }

        private List<FriendData> GetCurrentData()
        {
            switch (_currentTab)
            {
                case TabType.ApplyList:  return _applyList;
                case TabType.RecentTeam: return _recentTeam;
                default:                 return _friends;
            }
        }

        private void ClearContent()
        {
            for (int i = _scrollContent.childCount - 1; i >= 0; i--)
                Destroy(_scrollContent.GetChild(i).gameObject);
        }

        private void SwitchTab(int idx)
        {
            _currentTab = (TabType)idx;
            UpdateTabVisuals();
            RefreshList();
            UpdateOnlineCount();
        }

        private void UpdateTabVisuals()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool active = i == (int)_currentTab;
                var img = _tabButtons[i].GetComponent<Image>();
                var txt = _tabButtons[i].GetComponent<Text>();
                if (img != null) img.color = active ? ThemeColors.TabActive : ThemeColors.TabInactive;
                if (txt != null) txt.color = active ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            }
        }

        private void UpdateOnlineCount()
        {
            if (_onlineCountText == null) return;
            var list = GetCurrentData();
            int online = 0;
            foreach (var f in list)
                if (f.Online) online++;
            _onlineCountText.text = $"在线: {online}/{list.Count}";
        }

        // ════════════════════════════════════════════════════
        //  Friend Card
        // ════════════════════════════════════════════════════

        private void CreateFriendCard(RectTransform parent, FriendData fd)
        {
            var card = UIComponentFactory.CreateCard(parent, "Card_" + fd.Name,
                new Vector2(840, 64), Vector2.zero);
            card.GetComponent<Image>().color = ThemeColors.BgListItem;

            // Ensure correct height inside the VerticalLayoutGroup
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 64;

            // Avatar with status dot
            BuildAvatar(card, fd.Online);

            // Name
            var nameText = UIComponentFactory.CreateText(card, "Name", fd.Name,
                ThemeColors.FontBody, fd.Online ? ThemeColors.TextBright : ThemeColors.TextDim);
            nameText.alignment = TextAnchor.MiddleLeft;
            var nrt = nameText.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.5f);
            nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.sizeDelta = new Vector2(200, 30);
            nrt.anchoredPosition = new Vector2(80, 0);

            // Level
            var lvText = UIComponentFactory.CreateText(card, "Level", "Lv." + fd.Level,
                ThemeColors.FontSmall, ThemeColors.Gold);
            lvText.alignment = TextAnchor.MiddleLeft;
            var lrt = lvText.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.5f);
            lrt.anchorMax = new Vector2(0, 0.5f);
            lrt.sizeDelta = new Vector2(80, 30);
            lrt.anchoredPosition = new Vector2(300, 0);

            // Team info (RecentTeam tab only)
            if (_currentTab == TabType.RecentTeam && !string.IsNullOrEmpty(fd.TeamInfo))
            {
                var ti = UIComponentFactory.CreateText(card, "TeamInfo", fd.TeamInfo,
                    ThemeColors.FontTiny, ThemeColors.TextNormal);
                ti.alignment = TextAnchor.MiddleLeft;
                var tirt = ti.rectTransform;
                tirt.anchorMin = new Vector2(0, 0.5f);
                tirt.anchorMax = new Vector2(0, 0.5f);
                tirt.sizeDelta = new Vector2(160, 20);
                tirt.anchoredPosition = new Vector2(390, 0);
            }

            // Action buttons
            BuildActionButtons(card, fd);
        }

        private void BuildAvatar(RectTransform card, bool online)
        {
            // Avatar placeholder box
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(card, false);
            var art = avatar.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 0.5f);
            art.anchorMax = new Vector2(0, 0.5f);
            art.sizeDelta = new Vector2(44, 44);
            art.anchoredPosition = new Vector2(20, 0);
            avatar.GetComponent<Image>().color = ThemeColors.AccentDim;

            // Status dot — green=online, gray=offline
            var dot = new GameObject("StatusDot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(art, false);
            var drt = dot.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(1, 0);
            drt.anchorMax = new Vector2(1, 0);
            drt.sizeDelta = new Vector2(14, 14);
            drt.anchoredPosition = new Vector2(-2, 2);
            dot.GetComponent<Image>().color = online ? ThemeColors.ChatLocal : ThemeColors.TextDim;
        }

        private void BuildActionButtons(RectTransform card, FriendData fd)
        {
            if (_currentTab == TabType.ApplyList)
            {
                // Accept / Reject for friend requests
                var acceptBtn = UIComponentFactory.CreatePrimaryButton(card, "Accept", "接受",
                    () => { Debug.Log($"[FriendPanel] 接受申请: {fd.Name}"); });
                SetRightAnchor(acceptBtn.GetComponent<RectTransform>(), -150, 70, 32);

                var rejectBtn = UIComponentFactory.CreateButton(card, "Reject", "拒绝",
                    ThemeColors.BtnDanger,
                    () => { Debug.Log($"[FriendPanel] 拒绝申请: {fd.Name}"); },
                    ThemeColors.FontTiny);
                SetRightAnchor(rejectBtn.GetComponent<RectTransform>(), -60, 60, 32);
            }
            else
            {
                // Chat / Team / Delete for friends and recent teammates
                var chatBtn = UIComponentFactory.CreateSecondaryButton(card, "Chat", "私聊",
                    () => { Debug.Log($"[FriendPanel] 私聊: {fd.Name}"); });
                SetRightAnchor(chatBtn.GetComponent<RectTransform>(), -250, 70, 32);

                var teamBtn = UIComponentFactory.CreateSecondaryButton(card, "Team", "组队",
                    () => { Debug.Log($"[FriendPanel] 组队: {fd.Name}"); });
                SetRightAnchor(teamBtn.GetComponent<RectTransform>(), -160, 70, 32);

                var delBtn = UIComponentFactory.CreateButton(card, "Del", "删除",
                    ThemeColors.BtnDanger,
                    () => { Debug.Log($"[FriendPanel] 删除: {fd.Name}"); },
                    ThemeColors.FontTiny);
                SetRightAnchor(delBtn.GetComponent<RectTransform>(), -60, 60, 32);
            }
        }

        /// <summary>
        /// Anchors a RectTransform to the right edge of its parent with the given
        /// horizontal offset, width, and height.
        /// </summary>
        private static void SetRightAnchor(RectTransform rt, float xOffset, float w, float h)
        {
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(xOffset, 0);
        }

        // ════════════════════════════════════════════════════
        //  Navigation
        // ════════════════════════════════════════════════════

        private void BackToMain()
        {
            UIManager.Instance.Hide<FriendPanel>();
            UIManager.Instance.Show<MainCityPanel>();
        }

        // ════════════════════════════════════════════════════
        //  Demo Data
        // ════════════════════════════════════════════════════

        private void InitDemoData()
        {
            // ── 10 friends, mixed online/offline ──
            _friends.Add(new FriendData { Name = "侠客_10001", Level = 20, Online = true  });
            _friends.Add(new FriendData { Name = "剑心无痕",    Level = 35, Online = true  });
            _friends.Add(new FriendData { Name = "白云城主",    Level = 42, Online = true  });
            _friends.Add(new FriendData { Name = "天涯过客",    Level = 28, Online = false });
            _friends.Add(new FriendData { Name = "一剑封喉",    Level = 15, Online = false });
            _friends.Add(new FriendData { Name = "醉卧沙场",    Level = 50, Online = true  });
            _friends.Add(new FriendData { Name = "清风明月",    Level = 33, Online = false });
            _friends.Add(new FriendData { Name = "落花有意",    Level = 25, Online = true  });
            _friends.Add(new FriendData { Name = "听雨小筑",    Level = 47, Online = false });
            _friends.Add(new FriendData { Name = "折剑归田",    Level = 19, Online = true  });

            // ── Friend requests ──
            _applyList.Add(new FriendData { Name = "孤独求败", Level = 60, Online = true  });
            _applyList.Add(new FriendData { Name = "风华绝代", Level = 38, Online = false });
            _applyList.Add(new FriendData { Name = "陌上花开", Level = 22, Online = true  });

            // ── Recent teammates ──
            _recentTeam.Add(new FriendData { Name = "剑心无痕", Level = 35, Online = true,  TeamInfo = "3小时前" });
            _recentTeam.Add(new FriendData { Name = "醉卧沙场", Level = 50, Online = true,  TeamInfo = "昨天"    });
            _recentTeam.Add(new FriendData { Name = "白云城主", Level = 42, Online = true,  TeamInfo = "2天前"   });
            _recentTeam.Add(new FriendData { Name = "落花有意", Level = 25, Online = false, TeamInfo = "3天前"   });
        }
    }
}
