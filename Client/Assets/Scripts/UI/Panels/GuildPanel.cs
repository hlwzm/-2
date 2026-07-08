using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 公会面板 - 未加入/已加入双状态 + 公会列表 + 公会详情
    /// </summary>
    public class GuildPanel : BasePanel
    {
        // ===== 配色 =====
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorDimText = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.28f, 0.8f);
        private static readonly Color ColorTabActive = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorGold = new Color(1f, 0.8f, 0.2f);
        private static readonly Color ColorGreen = new Color(0.4f, 0.9f, 0.4f);
        private static readonly Color ColorRed = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color ColorItemBg = new Color(0.1f, 0.1f, 0.2f, 0.7f);

        // ===== 模拟数据 =====
        private class GuildData
        {
            public string Name;
            public int Level;
            public int Members;
            public int MaxMembers;
            public string Leader;
            public string Notice;
        }

        private static readonly string[] SubTabNames = { "公会信息", "成员列表", "公会技能", "公会任务" };

        private List<GuildData> _allGuilds = new();
        private GuildData _myGuild = null;
        private bool _isJoined = false;
        private int _currentSubTab = 0;

        // UI根节点 - 用于切换显示
        private RectTransform _notJoinedRoot;
        private RectTransform _joinedRoot;
        private RectTransform _subTabRoot;
        private RectTransform _detailContent;
        private RectTransform _guildListContent;
        private List<GameObject> _guildListItems = new();
        private Text _titleText;
        private Text _subTitleText;

        protected override void Awake()
        {
            base.Awake();
            InitMockData();
            BuildBackground();
            BuildTopBar();
            BuildNotJoinedUI();
            BuildJoinedUI();
            RefreshState();
        }

        private void InitMockData()
        {
            _allGuilds = new List<GuildData>
            {
                new GuildData { Name = "凌霄阁",     Level = 5,  Members = 28, MaxMembers = 40, Leader = "剑仙",     Notice = "每周六团本，欢迎活跃玩家加入！" },
                new GuildData { Name = "暗影盟",     Level = 3,  Members = 15, MaxMembers = 30, Leader = "影杀",     Notice = "PVP公会，每晚竞技场组队。" },
                new GuildData { Name = "沧海月明",   Level = 8,  Members = 35, MaxMembers = 40, Leader = "明月心",   Notice = "休闲公会，氛围友好，萌新友好。" },
                new GuildData { Name = "铁血战旗",   Level = 6,  Members = 22, MaxMembers = 40, Leader = "铁血",     Notice = "副本开荒公会，要求稳定在线。" },
            };
        }

        private void BuildBackground()
        {
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0.04f, 0.04f, 0.1f, 0.92f));
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }

        private void BuildTopBar()
        {
            _titleText = CreateText(transform as RectTransform, "Title", "同 盟", 32);
            var titleRt = (RectTransform)_titleText.transform;
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(0, 1);
            titleRt.sizeDelta = new Vector2(100, 40);
            titleRt.anchoredPosition = new Vector2(40, -40);

            _subTitleText = CreateText(transform as RectTransform, "SubTitle", "", 18);
            var subRt = (RectTransform)_subTitleText.transform;
            subRt.anchorMin = new Vector2(0, 1);
            subRt.anchorMax = new Vector2(0, 1);
            subRt.sizeDelta = new Vector2(200, 30);
            subRt.anchoredPosition = new Vector2(150, -40);
            _subTitleText.alignment = TextAnchor.MiddleLeft;
            _subTitleText.color = ColorDimText;

            var line = new GameObject("TitleLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(transform, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 1);
            lineRt.anchorMax = new Vector2(1, 1);
            lineRt.sizeDelta = new Vector2(0, 2);
            lineRt.anchoredPosition = new Vector2(0, -70);
            line.GetComponent<Image>().color = ColorAccent;

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "\u2715", () => Hide());
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.sizeDelta = new Vector2(50, 50);
            closeRt.anchoredPosition = new Vector2(-40, -35);
            closeBtn.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.4f, 0.8f);
        }

        // =====================================================================
        // 未加入公会视图
        // =====================================================================
        private void BuildNotJoinedUI()
        {
            _notJoinedRoot = new GameObject("NotJoinedView", typeof(RectTransform)).GetComponent<RectTransform>();
            _notJoinedRoot.SetParent(transform, false);
            _notJoinedRoot.anchorMin = new Vector2(0.02f, 0.12f);
            _notJoinedRoot.anchorMax = new Vector2(0.98f, 0.82f);
            _notJoinedRoot.sizeDelta = Vector2.zero;
            _notJoinedRoot.anchoredPosition = Vector2.zero;

            // 介绍信息
            var info = CreateText(_notJoinedRoot, "Info", "你尚未加入任何同盟\n\n同盟系统让你与志同道合的侠士一起闯荡江湖\n加入同盟后可获得专属技能、每日福利和团队副本资格", 20);
            var infoRt = (RectTransform)info.transform;
            infoRt.anchorMin = new Vector2(0.5f, 1);
            infoRt.anchorMax = new Vector2(0.5f, 1);
            infoRt.sizeDelta = new Vector2(600, 120);
            infoRt.anchoredPosition = new Vector2(0, -80);
            info.alignment = TextAnchor.UpperCenter;
            info.color = ColorDimText;

            // 搜索
            var searchBg = new GameObject("SearchBg", typeof(RectTransform), typeof(Image));
            searchBg.transform.SetParent(_notJoinedRoot, false);
            var sbRt = searchBg.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 1);
            sbRt.anchorMax = new Vector2(0.5f, 1);
            sbRt.sizeDelta = new Vector2(300, 36);
            sbRt.anchoredPosition = new Vector2(0, -170);
            searchBg.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.22f, 0.9f);

            var searchInput = new GameObject("SearchInput", typeof(RectTransform)).AddComponent<InputField>();
            searchInput.transform.SetParent(sbRt, false);
            var siRt = (RectTransform)searchInput.transform;
            siRt.anchorMin = Vector2.zero;
            siRt.anchorMax = Vector2.one;
            siRt.sizeDelta = new Vector2(-20, 0);
            siRt.anchoredPosition = new Vector2(10, 0);

            var placeholder = CreateText(siRt, "Placeholder", "搜索公会名称...", 16);
            var phRt = (RectTransform)placeholder.transform;
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;
            placeholder.color = ColorDimText;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.name = "Placeholder";

            var inputText = CreateText(siRt, "Text", "", 16);
            var itRt = (RectTransform)inputText.transform;
            itRt.anchorMin = Vector2.zero;
            itRt.anchorMax = Vector2.one;
            itRt.sizeDelta = Vector2.zero;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.name = "Text";

            searchInput.textComponent = inputText;
            searchInput.placeholder = placeholder;
            searchInput.onValueChanged.AddListener((val) => Debug.Log("[Guild] 搜索: " + val));

            var searchBtn = CreateButton(_notJoinedRoot, "SearchBtn", "搜索", () => Debug.Log("[Guild] 搜索公会"));
            var sBtnRt = (RectTransform)searchBtn.transform;
            sBtnRt.anchorMin = new Vector2(0.5f, 1);
            sBtnRt.anchorMax = new Vector2(0.5f, 1);
            sBtnRt.sizeDelta = new Vector2(80, 36);
            sBtnRt.anchoredPosition = new Vector2(200, -170);

            var createBtn = new GameObject("CreateBtn", typeof(RectTransform), typeof(Image));
            createBtn.transform.SetParent(_notJoinedRoot, false);
            var cbRt = createBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 1);
            cbRt.anchorMax = new Vector2(0.5f, 1);
            cbRt.sizeDelta = new Vector2(160, 40);
            cbRt.anchoredPosition = new Vector2(0, -225);
            createBtn.GetComponent<Image>().color = new Color(0.5f, 0.3f, 0.2f, 0.8f);

            var createText = CreateText(cbRt, "Label", "🏛️ 创建公会", 20);
            var ctRt = (RectTransform)createText.transform;
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;

            var createBtnComp = createBtn.AddComponent<Button>();
            createBtnComp.targetGraphic = createBtn.GetComponent<Image>();
            createBtnComp.onClick.AddListener(() => Debug.Log("[Guild] 创建公会(消耗500金币)"));

            // 公会列表标题
            var listTitle = CreateText(_notJoinedRoot, "ListTitle", "推荐公会", 22);
            var ltRt = (RectTransform)listTitle.transform;
            ltRt.anchorMin = new Vector2(0, 1);
            ltRt.anchorMax = new Vector2(0, 1);
            ltRt.sizeDelta = new Vector2(200, 30);
            ltRt.anchoredPosition = new Vector2(0, -280);
            listTitle.alignment = TextAnchor.MiddleLeft;

            // 公会列表 ScrollRect
            var scrollGo = new GameObject("GuildListScroll", typeof(RectTransform));
            scrollGo.transform.SetParent(_notJoinedRoot, false);
            var scRt = scrollGo.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0, 0);
            scRt.anchorMax = new Vector2(1, 0);
            scRt.sizeDelta = new Vector2(0, 270);
            scRt.anchoredPosition = new Vector2(0, 10);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scRt, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            _guildListContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _guildListContent.SetParent(vpRt, false);
            _guildListContent.anchorMin = new Vector2(0, 1);
            _guildListContent.anchorMax = new Vector2(1, 1);
            _guildListContent.sizeDelta = new Vector2(0, 0);
            _guildListContent.anchoredPosition = Vector2.zero;

            scrollRect.viewport = vpRt;
            scrollRect.content = _guildListContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        // =====================================================================
        // 已加入公会视图
        // =====================================================================
        private void BuildJoinedUI()
        {
            _joinedRoot = new GameObject("JoinedView", typeof(RectTransform)).GetComponent<RectTransform>();
            _joinedRoot.SetParent(transform, false);
            _joinedRoot.anchorMin = new Vector2(0.02f, 0.12f);
            _joinedRoot.anchorMax = new Vector2(0.98f, 0.85f);
            _joinedRoot.sizeDelta = Vector2.zero;
            _joinedRoot.anchoredPosition = Vector2.zero;
            _joinedRoot.gameObject.SetActive(false);

            // 子Tab
            _subTabRoot = new GameObject("SubTabs", typeof(RectTransform)).GetComponent<RectTransform>();
            _subTabRoot.SetParent(_joinedRoot, false);
            _subTabRoot.anchorMin = new Vector2(0, 1);
            _subTabRoot.anchorMax = new Vector2(1, 1);
            _subTabRoot.sizeDelta = new Vector2(0, 40);
            _subTabRoot.anchoredPosition = new Vector2(0, 0);

            for (int i = 0; i < SubTabNames.Length; i++)
            {
                var idx = i;
                var tab = new GameObject("SubTab_" + i, typeof(RectTransform), typeof(Image));
                tab.transform.SetParent(_subTabRoot, false);
                var tabRt = tab.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0, 0);
                tabRt.anchorMax = new Vector2(0, 1);
                tabRt.sizeDelta = new Vector2(130, 0);
                tabRt.anchoredPosition = new Vector2(10 + i * 134, 0);

                var tabImg = tab.GetComponent<Image>();
                tabImg.color = (i == 0) ? ColorTabActive : ColorTabNormal;

                var tabText = CreateText(tabRt, "Label", SubTabNames[i], 18);
                var ttRt = (RectTransform)tabText.transform;
                ttRt.anchorMin = Vector2.zero;
                ttRt.anchorMax = Vector2.one;
                ttRt.sizeDelta = Vector2.zero;

                var btn = tab.AddComponent<Button>();
                btn.targetGraphic = tabImg;
                btn.onClick.AddListener(() => OnSubTabClick(idx));
            }

            // 详情内容区域
            _detailContent = new GameObject("DetailContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _detailContent.SetParent(_joinedRoot, false);
            _detailContent.anchorMin = new Vector2(0, 0);
            _detailContent.anchorMax = new Vector2(1, 0);
            _detailContent.sizeDelta = new Vector2(0, -50);
            _detailContent.anchoredPosition = new Vector2(0, 5);
        }

        // =====================================================================
        // 状态切换
        // =====================================================================
        private void RefreshState()
        {
            // 模拟：初始未加入
            _isJoined = false;
            _notJoinedRoot.gameObject.SetActive(!_isJoined);
            _joinedRoot.gameObject.SetActive(_isJoined);

            if (_isJoined)
            {
                _subTitleText.text = string.Format("成员: {0}/{1}", _myGuild.Members, _myGuild.MaxMembers);
                ShowGuildInfoTab();
            }
            else
            {
                _subTitleText.text = "未加入";
                RefreshGuildList();
            }
        }

        // =====================================================================
        // 公会列表
        // =====================================================================
        private void RefreshGuildList()
        {
            foreach (var item in _guildListItems)
                Destroy(item);
            _guildListItems.Clear();

            _guildListContent.sizeDelta = new Vector2(0, _allGuilds.Count * 80 + 10);

            for (int i = 0; i < _allGuilds.Count; i++)
            {
                var idx = i;
                var g = _allGuilds[i];
                var item = BuildGuildListItem(g, i);
                _guildListItems.Add(item);
            }
        }

        private GameObject BuildGuildListItem(GuildData g, int index)
        {
            var item = new GameObject("GuildItem_" + index, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(_guildListContent, false);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 1);
            itemRt.anchorMax = new Vector2(1, 1);
            itemRt.sizeDelta = new Vector2(0, 70);
            itemRt.anchoredPosition = new Vector2(0, -10 - index * 80);
            item.GetComponent<Image>().color = ColorItemBg;

            // 名称
            var nameText = CreateText(itemRt, "Name", g.Name, 20);
            var nameRt = (RectTransform)nameText.transform;
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(0, 0.5f);
            nameRt.sizeDelta = new Vector2(160, 30);
            nameRt.anchoredPosition = new Vector2(20, 10);
            nameText.alignment = TextAnchor.MiddleLeft;

            // 等级
            var lvlText = CreateText(itemRt, "Level", "Lv." + g.Level, 16);
            var lvlRt = (RectTransform)lvlText.transform;
            lvlRt.anchorMin = new Vector2(0, 0.5f);
            lvlRt.anchorMax = new Vector2(0, 0.5f);
            lvlRt.sizeDelta = new Vector2(60, 24);
            lvlRt.anchoredPosition = new Vector2(190, 10);
            lvlText.alignment = TextAnchor.MiddleLeft;
            lvlText.color = ColorGold;

            // 公告
            var noticeText = CreateText(itemRt, "Notice", g.Notice, 14);
            var ntRt = (RectTransform)noticeText.transform;
            ntRt.anchorMin = new Vector2(0, 0.5f);
            ntRt.anchorMax = new Vector2(0, 0.5f);
            ntRt.sizeDelta = new Vector2(400, 22);
            ntRt.anchoredPosition = new Vector2(20, -14);
            noticeText.alignment = TextAnchor.MiddleLeft;
            noticeText.color = ColorDimText;

            // 成员数
            var memberText = CreateText(itemRt, "Members", g.Members + "/" + g.MaxMembers, 14);
            var mtRt = (RectTransform)memberText.transform;
            mtRt.anchorMin = new Vector2(0, 0.5f);
            mtRt.anchorMax = new Vector2(0, 0.5f);
            mtRt.sizeDelta = new Vector2(80, 22);
            mtRt.anchoredPosition = new Vector2(260, -14);
            memberText.alignment = TextAnchor.MiddleLeft;
            memberText.color = ColorDimText;

            // 申请按钮
            var applyBtn = CreateButton(itemRt, "ApplyBtn", "申请", () => {
                Debug.Log("[Guild] 申请加入: " + g.Name);
                FriendManager.Instance.AddFriend(0); // 模拟操作
            });
            var abRt = (RectTransform)applyBtn.transform;
            abRt.anchorMin = new Vector2(1, 0.5f);
            abRt.anchorMax = new Vector2(1, 0.5f);
            abRt.sizeDelta = new Vector2(80, 36);
            abRt.anchoredPosition = new Vector2(-20, 0);

            return item;
        }

        // =====================================================================
        // 公会详情Tab切换
        // =====================================================================
        private void OnSubTabClick(int index)
        {
            _currentSubTab = index;
            for (int i = 0; i < SubTabNames.Length; i++)
            {
                var tabGo = _subTabRoot.Find("SubTab_" + i);
                if (tabGo != null)
                    tabGo.GetComponent<Image>().color = (i == index) ? ColorTabActive : ColorTabNormal;
            }

            // 清空详情
            foreach (Transform child in _detailContent)
                Destroy(child.gameObject);

            switch (index)
            {
                case 0: ShowGuildInfoDetail(); break;
                case 1: ShowMemberList(); break;
                case 2: ShowGuildSkills(); break;
                case 3: ShowGuildQuests(); break;
            }
        }

        private void ShowGuildInfoTab()
        {
            OnSubTabClick(0);
        }

        private void ShowGuildInfoDetail()
        {
            if (_myGuild == null) return;

            var items = new (string, string)[]
            {
                ("公会名称", _myGuild.Name),
                ("公会等级", "Lv." + _myGuild.Level),
                ("会长", _myGuild.Leader),
                ("成员", _myGuild.Members + "/" + _myGuild.MaxMembers),
                ("公告", _myGuild.Notice),
            };

            float y = -20;
            foreach (var (label, value) in items)
            {
                var row = new GameObject("Row", typeof(RectTransform));
                row.transform.SetParent(_detailContent, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1);
                rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 36);
                rowRt.anchoredPosition = new Vector2(0, y);

                var lText = CreateText(rowRt, "Label", label + ": ", 18);
                var lRt = (RectTransform)lText.transform;
                lRt.anchorMin = new Vector2(0, 0.5f);
                lRt.anchorMax = new Vector2(0, 0.5f);
                lRt.sizeDelta = new Vector2(120, 28);
                lRt.anchoredPosition = new Vector2(20, 0);
                lText.alignment = TextAnchor.MiddleLeft;
                lText.color = ColorDimText;

                var vText = CreateText(rowRt, "Value", value, 18);
                var vRt = (RectTransform)vText.transform;
                vRt.anchorMin = new Vector2(0, 0.5f);
                vRt.anchorMax = new Vector2(0, 0.5f);
                vRt.sizeDelta = new Vector2(400, 28);
                vRt.anchoredPosition = new Vector2(150, 0);
                vText.alignment = TextAnchor.MiddleLeft;

                y -= 46;
            }

            // 退出公会按钮
            var leaveBtn = CreateButton(_detailContent, "LeaveBtn", "退出公会", () => {
                Debug.Log("[Guild] 退出公会");
                _isJoined = false;
                _myGuild = null;
                RefreshState();
            });
            var lbRt = (RectTransform)leaveBtn.transform;
            lbRt.anchorMin = new Vector2(0.5f, 0);
            lbRt.anchorMax = new Vector2(0.5f, 0);
            lbRt.sizeDelta = new Vector2(160, 44);
            lbRt.anchoredPosition = new Vector2(0, 20);
            leaveBtn.GetComponent<Image>().color = ColorRed;
        }

        private void ShowMemberList()
        {
            var members = new (string, int, bool)[]
            {
                ("剑仙", 50, true),
                ("风云", 48, true),
                ("明月", 45, true),
                ("清风", 42, false),
                ("流水", 38, true),
            };

            float y = -20;
            foreach (var (name, lvl, online) in members)
            {
                var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_detailContent, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1);
                rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 40);
                rowRt.anchoredPosition = new Vector2(0, y);
                row.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.6f);

                // 在线状态
                var statusText = CreateText(rowRt, "Status", online ? "\u25CF" : "\u25CB", 20);
                var stRt = (RectTransform)statusText.transform;
                stRt.anchorMin = new Vector2(0, 0.5f);
                stRt.anchorMax = new Vector2(0, 0.5f);
                stRt.sizeDelta = new Vector2(24, 24);
                stRt.anchoredPosition = new Vector2(15, 0);
                statusText.color = online ? ColorGreen : ColorDimText;

                var nText = CreateText(rowRt, "Name", name, 18);
                var nRt = (RectTransform)nText.transform;
                nRt.anchorMin = new Vector2(0, 0.5f);
                nRt.anchorMax = new Vector2(0, 0.5f);
                nRt.sizeDelta = new Vector2(120, 28);
                nRt.anchoredPosition = new Vector2(50, 0);
                nText.alignment = TextAnchor.MiddleLeft;

                var lText = CreateText(rowRt, "Level", "Lv." + lvl, 16);
                var lRt = (RectTransform)lText.transform;
                lRt.anchorMin = new Vector2(0, 0.5f);
                lRt.anchorMax = new Vector2(0, 0.5f);
                lRt.sizeDelta = new Vector2(60, 24);
                lRt.anchoredPosition = new Vector2(180, 0);
                lText.alignment = TextAnchor.MiddleLeft;
                lText.color = ColorGold;

                var oText = CreateText(rowRt, "Online", online ? "在线" : "离线", 14);
                var oRt = (RectTransform)oText.transform;
                oRt.anchorMin = new Vector2(0, 0.5f);
                oRt.anchorMax = new Vector2(0, 0.5f);
                oRt.sizeDelta = new Vector2(60, 22);
                oRt.anchoredPosition = new Vector2(250, 0);
                oText.alignment = TextAnchor.MiddleLeft;
                oText.color = online ? ColorGreen : ColorDimText;

                y -= 48;
            }
        }

        private void ShowGuildSkills()
        {
            var skills = new (string, string, int)[]
            {
                ("气血强化", "气血上限+500", 1),
                ("外功精通", "外功攻击+30", 2),
                ("内功精通", "内功攻击+30", 1),
                ("防御强化", "内外防御+20", 3),
            };

            float y = -20;
            foreach (var (name, desc, level) in skills)
            {
                var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_detailContent, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1);
                rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 50);
                rowRt.anchoredPosition = new Vector2(0, y);
                row.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.6f);

                var nText = CreateText(rowRt, "Name", name, 18);
                var nRt = (RectTransform)nText.transform;
                nRt.anchorMin = new Vector2(0, 0.5f);
                nRt.anchorMax = new Vector2(0, 0.5f);
                nRt.sizeDelta = new Vector2(120, 28);
                nRt.anchoredPosition = new Vector2(20, 8);
                nText.alignment = TextAnchor.MiddleLeft;

                var dText = CreateText(rowRt, "Desc", desc, 16);
                var dRt = (RectTransform)dText.transform;
                dRt.anchorMin = new Vector2(0, 0.5f);
                dRt.anchorMax = new Vector2(0, 0.5f);
                dRt.sizeDelta = new Vector2(300, 24);
                dRt.anchoredPosition = new Vector2(20, -18);
                dText.alignment = TextAnchor.MiddleLeft;
                dText.color = ColorDimText;

                var lText = CreateText(rowRt, "Level", "Lv." + level, 14);
                var lRt = (RectTransform)lText.transform;
                lRt.anchorMin = new Vector2(1, 0.5f);
                lRt.anchorMax = new Vector2(1, 0.5f);
                lRt.sizeDelta = new Vector2(60, 22);
                lRt.anchoredPosition = new Vector2(-100, 0);
                lText.color = ColorGold;

                var upgradeBtn = CreateButton(rowRt, "UpgradeBtn", "升级", () => Debug.Log("[Guild] 升级技能: " + name));
                var ubRt = (RectTransform)upgradeBtn.transform;
                ubRt.anchorMin = new Vector2(1, 0.5f);
                ubRt.anchorMax = new Vector2(1, 0.5f);
                ubRt.sizeDelta = new Vector2(70, 32);
                ubRt.anchoredPosition = new Vector2(-20, 0);

                y -= 58;
            }
        }

        private void ShowGuildQuests()
        {
            var quests = new (string, string, int, int)[]
            {
                ("公会捐献", "捐献金币或材料", 0, 1),
                ("团队副本", "参加一次团队副本", 0, 1),
                ("公会战", "参与公会战", 0, 1),
                ("帮助新人", "帮助公会新人完成任务", 0, 3),
            };

            float y = -20;
            foreach (var (name, desc, progress, target) in quests)
            {
                var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_detailContent, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1);
                rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 50);
                rowRt.anchoredPosition = new Vector2(0, y);
                row.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.6f);

                var nText = CreateText(rowRt, "Name", name, 18);
                var nRt = (RectTransform)nText.transform;
                nRt.anchorMin = new Vector2(0, 0.5f);
                nRt.anchorMax = new Vector2(0, 0.5f);
                nRt.sizeDelta = new Vector2(120, 28);
                nRt.anchoredPosition = new Vector2(20, 8);
                nText.alignment = TextAnchor.MiddleLeft;

                var dText = CreateText(rowRt, "Desc", desc, 16);
                var dRt = (RectTransform)dText.transform;
                dRt.anchorMin = new Vector2(0, 0.5f);
                dRt.anchorMax = new Vector2(0, 0.5f);
                dRt.sizeDelta = new Vector2(300, 24);
                dRt.anchoredPosition = new Vector2(20, -18);
                dText.alignment = TextAnchor.MiddleLeft;
                dText.color = ColorDimText;

                var pText = CreateText(rowRt, "Progress", progress + "/" + target, 14);
                var pRt = (RectTransform)pText.transform;
                pRt.anchorMin = new Vector2(1, 0.5f);
                pRt.anchorMax = new Vector2(1, 0.5f);
                pRt.sizeDelta = new Vector2(60, 22);
                pRt.anchoredPosition = new Vector2(-100, 0);
                pText.color = ColorDimText;

                y -= 58;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshState();
        }
    }
}