using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class GuildPanel : BasePanel
    {
        private static readonly Color ColorAccent = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorDimText = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.28f, 0.8f);
        private static readonly Color ColorTabActive = new Color(0.5f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ColorGold = new Color(1f, 0.8f, 0.2f);
        private static readonly Color ColorGreen = new Color(0.4f, 0.9f, 0.4f);
        private static readonly Color ColorRed = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color ColorBlue = new Color(0.4f, 0.6f, 1f);
        private static readonly string[] SubTabNames = { "帮会信息", "成员列表", "帮会技能", "帮会任务", "帮会日志" };

        private RectTransform _notJoinedRoot;
        private RectTransform _joinedRoot;
        private RectTransform _subTabRoot;
        private RectTransform _detailContent;
        private RectTransform _guildListContent;
        private List<GameObject> _guildListItems = new();
        private Text _titleText;
        private Text _subTitleText;
        private readonly List<GameObject> _subTabButtons = new();
        private int _currentSubTab = 0;

        protected override void Awake()
        {
            base.Awake();
            BuildBackground();
            BuildTopBar();
            BuildNotJoinedUI();
            BuildJoinedUI();
            RefreshState();
            GuildManager.Instance.OnGuildDataChanged += OnGuildChanged;
        }
        void OnDestroy()
        {
            if (GuildManager.Instance != null)
                GuildManager.Instance.OnGuildDataChanged -= OnGuildChanged;
        }
        private void OnGuildChanged(Core.GuildData guild)
        {
            if (gameObject.activeInHierarchy) RefreshState();
        }
        private void BuildBackground()
        {
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0.04f, 0.04f, 0.1f, 0.92f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }
        private void BuildTopBar()
        {
            _titleText = CreateText(transform as RectTransform, "Title", "帮会", 32);
            var titleRt = (RectTransform)_titleText.transform;
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(0, 1);
            titleRt.sizeDelta = new Vector2(100, 40); titleRt.anchoredPosition = new Vector2(40, -40);
            _subTitleText = CreateText(transform as RectTransform, "SubTitle", "", 18);
            var subRt = (RectTransform)_subTitleText.transform;
            subRt.anchorMin = new Vector2(0, 1); subRt.anchorMax = new Vector2(0, 1);
            subRt.sizeDelta = new Vector2(400, 30); subRt.anchoredPosition = new Vector2(150, -40);
            _subTitleText.alignment = TextAnchor.MiddleLeft; _subTitleText.color = ColorDimText;
            var line = new GameObject("TitleLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(transform, false);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 1); lineRt.anchorMax = new Vector2(1, 1);
            lineRt.sizeDelta = new Vector2(0, 2); lineRt.anchoredPosition = new Vector2(0, -70);
            line.GetComponent<Image>().color = ColorAccent;
        }
        private void BuildNotJoinedUI()
        {
            _notJoinedRoot = new GameObject("NotJoinedRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _notJoinedRoot.SetParent(transform, false);
            _notJoinedRoot.anchorMin = Vector2.zero; _notJoinedRoot.anchorMax = Vector2.one;
            _notJoinedRoot.sizeDelta = new Vector2(0, -80); _notJoinedRoot.anchoredPosition = new Vector2(0, -10);

            var searchBg = new GameObject("SearchBg", typeof(RectTransform), typeof(Image));
            searchBg.transform.SetParent(_notJoinedRoot, false);
            var sBgRt = searchBg.GetComponent<RectTransform>();
            sBgRt.anchorMin = new Vector2(0.02f, 1); sBgRt.anchorMax = new Vector2(0.98f, 1);
            sBgRt.sizeDelta = new Vector2(0, 50); sBgRt.anchoredPosition = new Vector2(0, -20);
            searchBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.7f);
            var searchInput = CreateText(sBgRt, "SearchHint", "  搜索帮会名称...", 18);
            var siRt = (RectTransform)searchInput.transform;
            siRt.anchorMin = new Vector2(0, 0.5f); siRt.anchorMax = new Vector2(0, 0.5f);
            siRt.sizeDelta = new Vector2(400, 30); siRt.anchoredPosition = new Vector2(20, 0);
            searchInput.alignment = TextAnchor.MiddleLeft; searchInput.color = ColorDimText;
            var createBtn = CreateButton(sBgRt, "CreateBtn", "创建帮会", () => ShowCreateDialog());
            var cbRt = (RectTransform)createBtn.transform;
            cbRt.anchorMin = new Vector2(1, 0.5f); cbRt.anchorMax = new Vector2(1, 0.5f);
            cbRt.sizeDelta = new Vector2(130, 40); cbRt.anchoredPosition = new Vector2(-20, 0);
            createBtn.GetComponent<Image>().color = ColorAccent;

            var scroll = new GameObject("GuildScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scroll.transform.SetParent(_notJoinedRoot, false);
            var scRt = scroll.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.02f, 0); scRt.anchorMax = new Vector2(0.98f, 0.9f);
            scRt.sizeDelta = Vector2.zero; scRt.anchoredPosition = new Vector2(0, -20);
            scroll.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.5f);
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.vertical = true;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scroll.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero; viewport.GetComponent<Image>().color = Color.clear;
            scrollRect.viewport = vpRt;

            _guildListContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _guildListContent.SetParent(viewport.transform, false);
            _guildListContent.anchorMin = new Vector2(0, 1); _guildListContent.anchorMax = new Vector2(1, 1);
            _guildListContent.sizeDelta = new Vector2(0, 0); _guildListContent.anchoredPosition = Vector2.zero;
            scrollRect.content = _guildListContent;
        }
        private void BuildJoinedUI()
        {
            _joinedRoot = new GameObject("JoinedRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _joinedRoot.SetParent(transform, false);
            _joinedRoot.anchorMin = Vector2.zero; _joinedRoot.anchorMax = Vector2.one;
            _joinedRoot.sizeDelta = new Vector2(0, -80); _joinedRoot.anchoredPosition = new Vector2(0, -10);

            _subTabRoot = new GameObject("SubTabRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _subTabRoot.SetParent(_joinedRoot, false);
            _subTabRoot.anchorMin = new Vector2(0, 1); _subTabRoot.anchorMax = new Vector2(1, 1);
            _subTabRoot.sizeDelta = new Vector2(0, 40); _subTabRoot.anchoredPosition = new Vector2(0, -15);
            float tabX = 20;
            for (int i = 0; i < SubTabNames.Length; i++)
            {
                var idx = i;
                var btn = CreateButton(_subTabRoot, "Tab" + i, SubTabNames[i], () => SwitchSubTab(idx));
                var btRt = (RectTransform)btn.transform;
                btRt.anchorMin = new Vector2(0, 0.5f); btRt.anchorMax = new Vector2(0, 0.5f);
                btRt.sizeDelta = new Vector2(140, 36); btRt.anchoredPosition = new Vector2(tabX, 0);
                tabX += 148; _subTabButtons.Add(btn.gameObject);
            }
            var detailBg = new GameObject("DetailBg", typeof(RectTransform), typeof(Image));
            detailBg.transform.SetParent(_joinedRoot, false);
            var dBgRt = detailBg.GetComponent<RectTransform>();
            dBgRt.anchorMin = new Vector2(0.02f, 0.02f); dBgRt.anchorMax = new Vector2(0.98f, 0.9f);
            dBgRt.sizeDelta = Vector2.zero;
            detailBg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.5f);

            var detailScroll = new GameObject("DetailScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            detailScroll.transform.SetParent(dBgRt, false);
            var dsRt = detailScroll.GetComponent<RectTransform>();
            dsRt.anchorMin = Vector2.zero; dsRt.anchorMax = Vector2.one;
            dsRt.sizeDelta = Vector2.zero; detailScroll.GetComponent<Image>().color = Color.clear;
            var dsScrollRect = detailScroll.GetComponent<ScrollRect>();
            dsScrollRect.horizontal = false; dsScrollRect.vertical = true;
            var dVp = new GameObject("DViewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            dVp.transform.SetParent(detailScroll.transform, false);
            var dVpRt = dVp.GetComponent<RectTransform>();
            dVpRt.anchorMin = Vector2.zero; dVpRt.anchorMax = Vector2.one;
            dVpRt.sizeDelta = Vector2.zero; dVp.GetComponent<Image>().color = Color.clear;
            dsScrollRect.viewport = dVpRt;

            _detailContent = new GameObject("DContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _detailContent.SetParent(dVp.transform, false);
            _detailContent.anchorMin = new Vector2(0, 1); _detailContent.anchorMax = new Vector2(1, 1);
            _detailContent.sizeDelta = new Vector2(0, 800); _detailContent.anchoredPosition = Vector2.zero;
            dsScrollRect.content = _detailContent;
        }
        private void RefreshState()
        {
            var hasGuild = GuildManager.Instance.HasGuild;
            if (_notJoinedRoot != null) _notJoinedRoot.gameObject.SetActive(!hasGuild);
            if (_joinedRoot != null) _joinedRoot.gameObject.SetActive(hasGuild);
            if (!hasGuild) {
                _titleText.text = "帮会"; _subTitleText.text = "选择或创建一个帮会"; RefreshGuildList();
            } else {
                var g = GuildManager.Instance.MyGuild;
                _titleText.text = g.Name;
                _subTitleText.text = $"Lv.{g.Level} 成员 {g.MemberCount}/{g.MaxMembers}";
                SwitchSubTab(_currentSubTab);
            }
        }
        private void RefreshGuildList()
        {
            foreach (var item in _guildListItems) Destroy(item);
            _guildListItems.Clear();
            float y = -10;
            foreach (var g in GuildManager.Instance.GuildList) {
                var item = BuildGuildListItem(g, y); _guildListItems.Add(item); y -= 80;
            }
            _guildListContent.sizeDelta = new Vector2(0, Mathf.Abs(y) + 10);
        }
        private GameObject BuildGuildListItem(Core.GuildData g, float y)
        {
            var item = new GameObject("GuildItem", typeof(RectTransform), typeof(Image));
            item.transform.SetParent(_guildListContent, false);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 1); itemRt.anchorMax = new Vector2(1, 1);
            itemRt.sizeDelta = new Vector2(0, 70); itemRt.anchoredPosition = new Vector2(0, y);
            item.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.6f);

            var icon = CreateImage(itemRt, "Icon", new Color(0.5f, 0.3f, 0.9f, 0.5f));
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0, 0.5f); iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(50, 50); iconRt.anchoredPosition = new Vector2(35, 0);

            var nameText = CreateText(itemRt, "Name", g.Name + "  Lv." + g.Level, 20);
            var nRt = (RectTransform)nameText.transform;
            nRt.anchorMin = new Vector2(0, 0.5f); nRt.anchorMax = new Vector2(0, 0.5f);
            nRt.sizeDelta = new Vector2(250, 30); nRt.anchoredPosition = new Vector2(80, 10);
            nameText.alignment = TextAnchor.MiddleLeft;

            var memberText = CreateText(itemRt, "Members", "成员 " + g.MemberCount + "/" + g.MaxMembers, 16);
            memberText.alignment = TextAnchor.MiddleLeft; memberText.color = ColorDimText;
            var mRt = (RectTransform)memberText.transform;
            mRt.anchorMin = new Vector2(0, 0.5f); mRt.anchorMax = new Vector2(0, 0.5f);
            mRt.sizeDelta = new Vector2(150, 24); mRt.anchoredPosition = new Vector2(80, -14);

            var leaderText = CreateText(itemRt, "Leader", "帮主: " + g.LeaderName, 16);
            leaderText.alignment = TextAnchor.MiddleLeft; leaderText.color = ColorDimText;
            var lRt = (RectTransform)leaderText.transform;
            lRt.anchorMin = new Vector2(0, 0.5f); lRt.anchorMax = new Vector2(0, 0.5f);
            lRt.sizeDelta = new Vector2(150, 24); lRt.anchoredPosition = new Vector2(240, -14);

            var applyBtn = CreateButton(itemRt, "ApplyBtn", "申请加入", () => {
                var p = GameManager.Instance.Player;
                GuildManager.Instance.ApplyToGuild(g.GuildId, p.PlayerId, p.Name, p.Level);
                GameManager.Instance.ShowNotice("已发送申请");
            });
            var aRt = (RectTransform)applyBtn.transform;
            aRt.anchorMin = new Vector2(1, 0.5f); aRt.anchorMax = new Vector2(1, 0.5f);
            aRt.sizeDelta = new Vector2(100, 36); aRt.anchoredPosition = new Vector2(-20, 0);
            applyBtn.GetComponent<Image>().color = ColorAccent;
            return item;
        }
        private void SwitchSubTab(int index)
        {
            _currentSubTab = index;
            for (int i = 0; i < _subTabButtons.Count; i++) {
                var btnImg = _subTabButtons[i].GetComponent<Image>();
                if (btnImg != null) btnImg.color = i == index ? ColorTabActive : ColorTabNormal;
            }
            foreach (Transform child in _detailContent) Destroy(child.gameObject);
            switch (index) {
                case 0: ShowGuildInfo(); break;
                case 1: ShowMemberList(); break;
                case 2: ShowGuildSkills(); break;
                case 3: ShowGuildQuests(); break;
                case 4: ShowGuildLog(); break;
            }
        }
        private void ShowGuildInfo()
        {
            var g = GuildManager.Instance.MyGuild; if (g == null) return;
            float y = -20;
            var rows = new (string, string)[] {
                ("帮会名称", g.Name),
                ("帮会等级", "Lv." + g.Level + " (贡献 " + g.TotalContribution + "/" + g.ContributionForNextLevel + ")"),
                ("帮主", g.LeaderName),
                ("成员数", g.MemberCount + "/" + g.MaxMembers),
                ("帮会资金", g.Funds + " 金币"),
                ("创建时间", g.CreateTime.ToString("yyyy-MM-dd")),
            };
            foreach (var (label, value) in rows) {
                var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_detailContent, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 36); rowRt.anchoredPosition = new Vector2(0, y);
                row.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.6f);
                var lblTxt = CreateText(rowRt, "Label", label, 18);
                var lRt = (RectTransform)lblTxt.transform;
                lRt.anchorMin = new Vector2(0, 0.5f); lRt.anchorMax = new Vector2(0, 0.5f);
                lRt.sizeDelta = new Vector2(120, 28); lRt.anchoredPosition = new Vector2(20, 0);
                lblTxt.alignment = TextAnchor.MiddleLeft; lblTxt.color = ColorDimText;
                var valTxt = CreateText(rowRt, "Value", value, 18);
                var vRt = (RectTransform)valTxt.transform;
                vRt.anchorMin = new Vector2(0, 0.5f); vRt.anchorMax = new Vector2(0, 0.5f);
                vRt.sizeDelta = new Vector2(400, 28); vRt.anchoredPosition = new Vector2(150, 0);
                valTxt.alignment = TextAnchor.MiddleLeft;
                y -= 44;
            }
            y -= 10;
            var noticeLabel = CreateText(_detailContent, "NoticeLabel", "公告", 20);
            var nlRt = (RectTransform)noticeLabel.transform;
            nlRt.anchorMin = new Vector2(0, 1); nlRt.anchorMax = new Vector2(0, 1);
            nlRt.sizeDelta = new Vector2(100, 28); nlRt.anchoredPosition = new Vector2(20, y);
            noticeLabel.alignment = TextAnchor.MiddleLeft; noticeLabel.color = ColorGold;
            y -= 35;
            var noticeBg = new GameObject("NoticeBg", typeof(RectTransform), typeof(Image));
            noticeBg.transform.SetParent(_detailContent, false);
            var nbRt = noticeBg.GetComponent<RectTransform>();
            nbRt.anchorMin = new Vector2(0, 1); nbRt.anchorMax = new Vector2(1, 1);
            nbRt.sizeDelta = new Vector2(-40, 80); nbRt.anchoredPosition = new Vector2(20, y);
            noticeBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.6f);
            var noticeText = CreateText(nbRt, "Text", g.Notice, 16);
            var ntRt = (RectTransform)noticeText.transform;
            ntRt.anchorMin = new Vector2(0, 0); ntRt.anchorMax = new Vector2(1, 1);
            ntRt.sizeDelta = new Vector2(-20, -20); ntRt.anchoredPosition = Vector2.zero;
            noticeText.alignment = TextAnchor.UpperLeft; noticeText.color = ColorDimText;
            y -= 100;
            var pid = GameManager.Instance.Player.PlayerId;
            var m = GuildManager.Instance.GetMember(pid);
            if (m != null && m.Position >= Core.GuildPosition.Officer) {
                var editBtn = CreateButton(_detailContent, "EditNotice", "编辑公告", () => {
                    GuildManager.Instance.SetNotice("请输入新公告", pid); SwitchSubTab(0);
                });
                var enRt = (RectTransform)editBtn.transform;
                enRt.anchorMin = new Vector2(1, 1); enRt.anchorMax = new Vector2(1, 1);
                enRt.sizeDelta = new Vector2(120, 36); enRt.anchoredPosition = new Vector2(-40, y);
            }
            _detailContent.sizeDelta = new Vector2(0, Mathf.Abs(y) + 40);
        }
        private void ShowMemberList()
        {
            var g = GuildManager.Instance.MyGuild; if (g == null) return;
            var members = GuildManager.Instance.GetSortedMembers();
            float y = -20; float rowH = 50;
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(_detailContent, false);
            var hRt = header.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1); hRt.anchorMax = new Vector2(1, 1);
            hRt.sizeDelta = new Vector2(0, rowH); hRt.anchoredPosition = new Vector2(0, y);
            header.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.3f, 0.8f);
            var cols = new (string, float)[] { ("成员", 180), ("等级", 60), ("职位", 100), ("贡献", 120), ("状态", 80), ("操作", 100) };
            float hx = 20;
            foreach (var (cn, cw) in cols) {
                var ht = CreateText(hRt, cn, cn, 16);
                var htr = (RectTransform)ht.transform;
                htr.anchorMin = new Vector2(0, 0.5f); htr.anchorMax = new Vector2(0, 0.5f);
                htr.sizeDelta = new Vector2(cw, 24); htr.anchoredPosition = new Vector2(hx, 0);
                ht.alignment = TextAnchor.MiddleLeft; ht.color = ColorGold; hx += cw;
            }
            y -= rowH + 4;
            var pid = GameManager.Instance.Player.PlayerId;
            var self = GuildManager.Instance.GetMember(pid);
            var posNames = new string[] { "成员", "精英", "官员", "副帮主", "帮主" };
            foreach (var m in members) {
                var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_detailContent, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, rowH); rowRt.anchoredPosition = new Vector2(0, y);
                row.GetComponent<Image>().color = m.PlayerId == pid ? new Color(0.2f, 0.15f, 0.3f, 0.6f) : new Color(0.08f, 0.08f, 0.16f, 0.6f);
                var pc = m.Position == Core.GuildPosition.Leader ? ColorGold : m.Position >= Core.GuildPosition.Officer ? ColorAccent : Color.white;
                float cx = 20;
                var nt = CreateText(rowRt,"Name",m.Name,18); var ntr=(RectTransform)nt.transform; ntr.anchorMin=new Vector2(0,0.5f); ntr.anchorMax=new Vector2(0,0.5f); ntr.sizeDelta=new Vector2(180,28); ntr.anchoredPosition=new Vector2(cx,0); nt.alignment=TextAnchor.MiddleLeft; cx+=180;
                var lt = CreateText(rowRt,"Lv",m.Level.ToString(),16); var ltr=(RectTransform)lt.transform; ltr.anchorMin=new Vector2(0,0.5f); ltr.anchorMax=new Vector2(0,0.5f); ltr.sizeDelta=new Vector2(60,24); ltr.anchoredPosition=new Vector2(cx,0); lt.alignment=TextAnchor.MiddleLeft; lt.color=ColorDimText; cx+=60;
                var pt = CreateText(rowRt,"Pos",posNames[(int)m.Position],16); var ptr=(RectTransform)pt.transform; ptr.anchorMin=new Vector2(0,0.5f); ptr.anchorMax=new Vector2(0,0.5f); ptr.sizeDelta=new Vector2(100,24); ptr.anchoredPosition=new Vector2(cx,0); pt.color=pc; cx+=100;
                var ct = CreateText(rowRt,"Contrib",m.Contribution.ToString(),16); var ctr=(RectTransform)ct.transform; ctr.anchorMin=new Vector2(0,0.5f); ctr.anchorMax=new Vector2(0,0.5f); ctr.sizeDelta=new Vector2(120,24); ctr.anchoredPosition=new Vector2(cx,0); ct.alignment=TextAnchor.MiddleLeft; ct.color=ColorDimText; cx+=120;
                var ot = CreateText(rowRt,"Online",m.Online?"在线":"离线",16); var otr=(RectTransform)ot.transform; otr.anchorMin=new Vector2(0,0.5f); otr.anchorMax=new Vector2(0,0.5f); otr.sizeDelta=new Vector2(80,24); otr.anchoredPosition=new Vector2(cx,0); ot.color=m.Online?ColorGreen:ColorDimText; cx+=80;
                if (self != null && self.Position >= Core.GuildPosition.Officer && m.PlayerId != pid && m.Position < self.Position) {
                    var kb = CreateButton(rowRt,"KickBtn","踢出",()=>{ GuildManager.Instance.KickMember(m.PlayerId,pid); SwitchSubTab(1); });
                    var kr = (RectTransform)kb.transform; kr.anchorMin=new Vector2(0,0.5f); kr.anchorMax=new Vector2(0,0.5f);
                    kr.sizeDelta=new Vector2(70,32); kr.anchoredPosition=new Vector2(cx+15,0); kb.GetComponent<Image>().color=ColorRed;
                }
                y -= rowH + 2;
            }
            _detailContent.sizeDelta = new Vector2(0, Mathf.Abs(y) + 20);
        }
        private void ShowGuildSkills()
        {
            var g = GuildManager.Instance.MyGuild; if (g == null) return;
            GuildManager.Instance.GetGuildBuff(out float atk, out float def, out float hp);
            var ov = new GameObject("Overview", typeof(RectTransform), typeof(Image));
            ov.transform.SetParent(_detailContent, false);
            var ovRt = ov.GetComponent<RectTransform>();
            ovRt.anchorMin = new Vector2(0, 1); ovRt.anchorMax = new Vector2(1, 1);
            ovRt.sizeDelta = new Vector2(0, 50); ovRt.anchoredPosition = new Vector2(0, -20);
            ov.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.2f, 0.7f);
            var bf = CreateText(ovRt, "BuffText", "全体加成:  攻击 +" + atk.ToString("F1") + "%  |  防御 +" + def.ToString("F1") + "%  |  生命 +" + hp.ToString("F1") + "%", 18);
            bf.rectTransform.anchorMin = Vector2.zero; bf.rectTransform.anchorMax = Vector2.one;
            bf.rectTransform.sizeDelta = Vector2.zero; bf.color = ColorGold;

            float y = -85;
            var pid = GameManager.Instance.Player.PlayerId;
            var m = GuildManager.Instance.GetMember(pid);
            bool canUp = m != null && m.Position >= Core.GuildPosition.Officer;

            foreach (var sk in g.Skills) {
                var row = new GameObject("SkillRow", typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_detailContent, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
                rowRt.sizeDelta = new Vector2(0, 80); rowRt.anchoredPosition = new Vector2(0, y);
                row.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.6f);

                var nt = CreateText(rowRt,"Name",sk.Name,22); var ntr=(RectTransform)nt.transform;
                ntr.anchorMin=new Vector2(0,0.5f); ntr.anchorMax=new Vector2(0,0.5f);
                ntr.sizeDelta=new Vector2(120,30); ntr.anchoredPosition=new Vector2(30,15);
                var lvt = CreateText(rowRt,"Level","Lv."+sk.Level+"/"+sk.MaxLevel,18); var lvr=(RectTransform)lvt.transform;
                lvr.anchorMin=new Vector2(0,0.5f); lvr.anchorMax=new Vector2(0,0.5f);
                lvr.sizeDelta=new Vector2(100,26); lvr.anchoredPosition=new Vector2(30,-15);
                lvt.alignment=TextAnchor.MiddleLeft; lvt.color=ColorGold;
                var dt = CreateText(rowRt,"Desc",sk.Description,16); var dr=(RectTransform)dt.transform;
                dr.anchorMin=new Vector2(0,0.5f); dr.anchorMax=new Vector2(0,0.5f);
                dr.sizeDelta=new Vector2(250,24); dr.anchoredPosition=new Vector2(160,15);
                dt.alignment=TextAnchor.MiddleLeft; dt.color=ColorDimText;
                var bt = CreateText(rowRt,"Bonus","攻击+"+sk.CurrentAtkBonus.ToString("F1")+"%  防御+"+sk.CurrentDefBonus.ToString("F1")+"%  生命+"+sk.CurrentHpBonus.ToString("F1")+"%",16);
                var br=(RectTransform)bt.transform; br.anchorMin=new Vector2(0,0.5f); br.anchorMax=new Vector2(0,0.5f);
                br.sizeDelta=new Vector2(350,24); br.anchoredPosition=new Vector2(160,-15);
                bt.alignment=TextAnchor.MiddleLeft; bt.color=ColorBlue;
                var ct = CreateText(rowRt,"Cost","资金:"+sk.UpgradeCost+"  贡献:"+sk.ContributionCost,14);
                var cr=(RectTransform)ct.transform; cr.anchorMin=new Vector2(1,0.5f); cr.anchorMax=new Vector2(1,0.5f);
                cr.sizeDelta=new Vector2(180,22); cr.anchoredPosition=new Vector2(-220,0);
                ct.alignment=TextAnchor.MiddleRight; ct.color=ColorDimText;
                if (canUp && !sk.IsMaxLevel) {
                    var ub = CreateButton(rowRt,"UpgradeBtn","升级",()=>{
                        var code = GuildManager.Instance.UpgradeSkill(sk.SkillId, pid);
                        var msg = code==0?"升级成功！":code==2?"已达最高等级":code==3?"帮会资金不足":code==4?"个人贡献不足":"升级失败";
                        GameManager.Instance.ShowNotice(msg); SwitchSubTab(2);
                    });
                    var ur=(RectTransform)ub.transform; ur.anchorMin=new Vector2(1,0.5f); ur.anchorMax=new Vector2(1,0.5f);
                    ur.sizeDelta=new Vector2(80,36); ur.anchoredPosition=new Vector2(-30,0); ub.GetComponent<Image>().color=ColorAccent;
                } else if (sk.IsMaxLevel) {
                    var mt = CreateText(rowRt,"Max","已满级",16); var mr=(RectTransform)mt.transform;
                    mr.anchorMin=new Vector2(1,0.5f); mr.anchorMax=new Vector2(1,0.5f);
                    mr.sizeDelta=new Vector2(80,24); mr.anchoredPosition=new Vector2(-30,0); mt.color=ColorGreen;
                }
                y -= 90;
            }
            _detailContent.sizeDelta = new Vector2(0, Mathf.Abs(y) + 30);
        }
        private void ShowGuildQuests()
        {
            var g = GuildManager.Instance.MyGuild; if (g == null) return;
            var quests = GuildManager.Instance.GetTodayGuildQuests();
            float y = -20;
            var ht = CreateText(_detailContent,"Header","今日帮会任务 (每日刷新)",20);
            var hr=(RectTransform)ht.transform; hr.anchorMin=new Vector2(0,1); hr.anchorMax=new Vector2(0,1);
            hr.sizeDelta=new Vector2(300,30); hr.anchoredPosition=new Vector2(20,y);
            ht.alignment=TextAnchor.MiddleLeft; ht.color=ColorGold; y-=45;
            var pid = GameManager.Instance.Player.PlayerId;
            foreach (var q in quests) {
                g.DailyQuestProgress.TryGetValue(q.QuestId, out int prog);
                bool done = prog >= q.Target;
                var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
                row.transform.SetParent(_detailContent, false);
                var rr = row.GetComponent<RectTransform>();
                rr.anchorMin=new Vector2(0,1); rr.anchorMax=new Vector2(1,1);
                rr.sizeDelta=new Vector2(0,60); rr.anchoredPosition=new Vector2(0,y);
                row.GetComponent<Image>().color = new Color(0.08f,0.08f,0.16f,0.6f);

                var nt=CreateText(row.GetComponent<RectTransform>(),"Name",q.Name,20); var ntr=(RectTransform)nt.transform;
                ntr.anchorMin=new Vector2(0,0.5f); ntr.anchorMax=new Vector2(0,0.5f);
                ntr.sizeDelta=new Vector2(120,28); ntr.anchoredPosition=new Vector2(20,10);
                nt.alignment=TextAnchor.MiddleLeft;

                var dt=CreateText(row.GetComponent<RectTransform>(),"Desc",q.Description,16); var dtr=(RectTransform)dt.transform;
                dtr.anchorMin=new Vector2(0,0.5f); dtr.anchorMax=new Vector2(0,0.5f);
                dtr.sizeDelta=new Vector2(200,24); dtr.anchoredPosition=new Vector2(20,-14);
                dt.alignment=TextAnchor.MiddleLeft; dt.color=ColorDimText;

                var pt=CreateText(row.GetComponent<RectTransform>(),"Progress","进度 "+prog+"/"+q.Target,16); var ptr=(RectTransform)pt.transform;
                ptr.anchorMin=new Vector2(0,0.5f); ptr.anchorMax=new Vector2(0,0.5f);
                ptr.sizeDelta=new Vector2(120,24); ptr.anchoredPosition=new Vector2(250,10);
                pt.alignment=TextAnchor.MiddleLeft; pt.color=done?ColorGreen:ColorDimText;

                var rt=CreateText(row.GetComponent<RectTransform>(),"Reward","贡献+"+q.ContributionReward+" 资金+"+q.FundsReward,14); var rtr=(RectTransform)rt.transform;
                rtr.anchorMin=new Vector2(0,0.5f); rtr.anchorMax=new Vector2(0,0.5f);
                rtr.sizeDelta=new Vector2(200,22); rtr.anchoredPosition=new Vector2(250,-16);
                rt.alignment=TextAnchor.MiddleLeft; rt.color=ColorGold;

                var st=CreateText(row.GetComponent<RectTransform>(),"Status",done?"已完成 ✓":"进行中",16); var str=(RectTransform)st.transform;
                str.anchorMin=new Vector2(1,0.5f); str.anchorMax=new Vector2(1,0.5f);
                str.sizeDelta=new Vector2(100,24); str.anchoredPosition=new Vector2(-20,0);
                st.color=done?ColorGreen:ColorDimText;
                y-=68;
            }
            var mm = GuildManager.Instance.GetMember(pid);
            if (mm != null) {
                y-=10;
                var sm=CreateText(_detailContent,"Summary","今日贡献: "+mm.DailyContribution+"  |  本周贡献: "+mm.WeeklyContribution+"  |  总贡献: "+mm.Contribution,18);
                var sr=(RectTransform)sm.transform; sr.anchorMin=new Vector2(0,1); sr.anchorMax=new Vector2(0,1);
                sr.sizeDelta=new Vector2(600,30); sr.anchoredPosition=new Vector2(20,y);
                sm.alignment=TextAnchor.MiddleLeft; sm.color=ColorGold; y-=40;
            }
            _detailContent.sizeDelta = new Vector2(0, Mathf.Abs(y) + 20);
        }
        private void ShowGuildLog()
        {
            var g = GuildManager.Instance.MyGuild; if (g == null) return;
            var logs = GuildManager.Instance.GetRecentLogs(50);
            float y = -20;
            if (logs.Count == 0) {
                var et = CreateText(_detailContent,"Empty","暂无帮会日志",18);
                var er=(RectTransform)et.transform; er.anchorMin=new Vector2(0,1); er.anchorMax=new Vector2(0,1);
                er.sizeDelta=new Vector2(200,30); er.anchoredPosition=new Vector2(40,y);
                et.alignment=TextAnchor.MiddleLeft; et.color=ColorDimText; y-=40;
            } else {
                foreach (var log in logs) {
                    var row = new GameObject("LogRow", typeof(RectTransform), typeof(Image));
                    row.transform.SetParent(_detailContent, false);
                    var rr=row.GetComponent<RectTransform>();
                    rr.anchorMin=new Vector2(0,1); rr.anchorMax=new Vector2(1,1);
                    rr.sizeDelta=new Vector2(0,36); rr.anchoredPosition=new Vector2(0,y);
                    row.GetComponent<Image>().color = new Color(0.08f,0.08f,0.16f,0.6f);

                    var tt=CreateText(row.GetComponent<RectTransform>(),"Time",log.Time.ToString("HH:mm:ss"),14); var tr=(RectTransform)tt.transform;
                    tr.anchorMin=new Vector2(0,0.5f); tr.anchorMax=new Vector2(0,0.5f);
                    tr.sizeDelta=new Vector2(80,22); tr.anchoredPosition=new Vector2(20,0);
                    tt.alignment=TextAnchor.MiddleLeft; tt.color=ColorDimText;

                    var mt=CreateText(row.GetComponent<RectTransform>(),"Message",log.Message,16); var mr=(RectTransform)mt.transform;
                    mr.anchorMin=new Vector2(0,0.5f); mr.anchorMax=new Vector2(0,0.5f);
                    mr.sizeDelta=new Vector2(700,24); mr.anchoredPosition=new Vector2(110,0);
                    mt.alignment=TextAnchor.MiddleLeft; mt.color=Color.white;
                    y-=42;
                }
            }
            _detailContent.sizeDelta = new Vector2(0, Mathf.Abs(y) + 20);
        }
        private void ShowCreateDialog()
        {
            var p = GameManager.Instance.Player;
            string defName = p.Name + "的帮会";
            int iconIdx = Random.Range(0, 10);
            var code = GuildManager.Instance.CreateGuild(defName, iconIdx, p.PlayerId, p.Name);
            var msg = code==0?"帮会《"+defName+"》创建成功！":code==1?"名称已存在":code==2?"你已有帮会":code==3?"金币不足（需50000）":"参数无效";
            GameManager.Instance.ShowNotice(msg);
            if (code == 0) RefreshState();
        }
        public override void Refresh()
        {
            base.Refresh(); RefreshState();
        }
    }
}
