using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;

namespace Jx3.UI.Panels
{
    public class FriendPanel : BasePanel
    {
        private static readonly Color ColorBg = new Color(0.06f, 0.06f, 0.12f, 0.95f);
        private static readonly Color ColorCard = new Color(0.12f, 0.12f, 0.22f);
        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.25f);
        private static readonly Color ColorTabActive = new Color(0.33f, 0.2f, 0.5f);
        private static readonly Color ColorInputBg = new Color(0.12f, 0.12f, 0.22f);
        private static readonly Color ColorGreen = new Color(0.2f, 1.0f, 0.2f);
        private static readonly Color ColorRed = new Color(1.0f, 0.2f, 0.2f);
        private static readonly Color ColorGold = new Color(1f, 0.85f, 0.2f);
        private static readonly Color ColorBtnPrimary = new Color(0.35f, 0.2f, 0.65f);
        private static readonly Color ColorBtnDanger = new Color(0.6f, 0.15f, 0.15f);
        private static readonly Color ColorBtnAccept = new Color(0.15f, 0.5f, 0.15f);

        private Text _onlineCountText;
        private InputField _searchInput;
        private RectTransform _listContainer;
        private int _currentTab; // 0=好友列表, 1=申请列表, 2=最近组队
        private readonly List<GameObject> _tabBtns = new();

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            RefreshList();
        }

        private void BuildUI()
        {
            var rootRt = transform as RectTransform;

            // 背景
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = ColorBg;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // ===== 标题 =====
            var title = CreateText(rootRt, "Title", "好  友", 32);
            title.fontStyle = FontStyle.Bold;
            title.color = ColorGold;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -40);
            titleRt.sizeDelta = new Vector2(200, 44);

            // 在线数
            _onlineCountText = CreateText(rootRt, "OnlineCount", "在线: 0/0", 16);
            _onlineCountText.alignment = TextAnchor.MiddleCenter;
            _onlineCountText.color = new Color(0.6f, 0.8f, 0.6f);
            var onlineRt = _onlineCountText.rectTransform;
            onlineRt.anchorMin = new Vector2(0.5f, 1f);
            onlineRt.anchorMax = new Vector2(0.5f, 1f);
            onlineRt.anchoredPosition = new Vector2(0, -68);
            onlineRt.sizeDelta = new Vector2(200, 24);

            // 关闭按钮
            var closeBtn = CreateButton(rootRt, "CloseBtn", "✕", () => Hide());
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-15, -15);
            closeRt.sizeDelta = new Vector2(36, 36);
            var closeImg = closeBtn.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.25f, 0.35f);
            var closeTxt = closeBtn.GetComponentInChildren<Text>();
            closeTxt.fontSize = 18;

            // ===== 搜索栏 =====
            BuildSearchBar(rootRt);

            // ===== 子Tab =====
            BuildSubTabs(rootRt);

            // ===== 列表容器 =====
            var listBg = new GameObject("ListBg", typeof(RectTransform), typeof(Image));
            listBg.transform.SetParent(transform, false);
            var listBgRt = listBg.GetComponent<RectTransform>();
            listBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            listBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            listBgRt.sizeDelta = new Vector2(860, 420);
            listBgRt.anchoredPosition = new Vector2(0, -30);
            var listBgImg = listBg.GetComponent<Image>();
            listBgImg.color = new Color(0.08f, 0.08f, 0.16f);

            // ScrollView容器
            var scrollGo = new GameObject("ScrollRect", typeof(RectTransform));
            scrollGo.transform.SetParent(listBgRt, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.sizeDelta = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(scrollRt, false);
            _listContainer = contentGo.GetComponent<RectTransform>();
            _listContainer.anchorMin = new Vector2(0, 1);
            _listContainer.anchorMax = new Vector2(1, 1);
            _listContainer.pivot = new Vector2(0.5f, 1);
            _listContainer.sizeDelta = new Vector2(0, 0);

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.padding = new RectOffset(8, 8, 6, 6);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = _listContainer;
        }

        private void BuildSearchBar(RectTransform parent)
        {
            // 输入框背景
            var inputBg = new GameObject("SearchBg", typeof(RectTransform), typeof(Image));
            inputBg.transform.SetParent(parent, false);
            var inputBgRt = inputBg.GetComponent<RectTransform>();
            inputBgRt.anchorMin = new Vector2(0.5f, 1f);
            inputBgRt.anchorMax = new Vector2(0.5f, 1f);
            inputBgRt.sizeDelta = new Vector2(520, 36);
            inputBgRt.anchoredPosition = new Vector2(-110, -108);
            var inputBgImg = inputBg.GetComponent<Image>();
            inputBgImg.color = ColorInputBg;

            // 输入框
            var inputGo = new GameObject("SearchInput", typeof(RectTransform));
            inputGo.transform.SetParent(inputBgRt, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero; inputRt.anchorMax = Vector2.one;
            inputRt.sizeDelta = new Vector2(-8, -6);
            inputRt.anchoredPosition = new Vector2(0, -1);

            _searchInput = inputGo.AddComponent<InputField>();
            var inputText = inputGo.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 15;
            inputText.color = new Color(0.85f, 0.85f, 0.9f);
            inputText.supportRichText = false;
            inputText.alignment = TextAnchor.MiddleLeft;
            _searchInput.textComponent = inputText;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(inputRt, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;

            var phText = phGo.AddComponent<Text>();
            phText.text = "🔍 搜索玩家...";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 15;
            phText.color = new Color(0.4f, 0.4f, 0.5f);
            phText.alignment = TextAnchor.MiddleLeft;
            _searchInput.placeholder = phText;

            // 添加按钮
            var addBtn = CreateButton(parent, "AddBtn", "添加", OnAddFriendClick);
            var addRt = addBtn.GetComponent<RectTransform>();
            addRt.anchorMin = new Vector2(0.5f, 1f);
            addRt.anchorMax = new Vector2(0.5f, 1f);
            addRt.anchoredPosition = new Vector2(340, -108);
            addRt.sizeDelta = new Vector2(100, 36);
            var addImg = addBtn.GetComponent<Image>();
            addImg.color = ColorBtnPrimary;
            var addTxt = addBtn.GetComponentInChildren<Text>();
            addTxt.fontSize = 16;
        }

        private void BuildSubTabs(RectTransform parent)
        {
            string[] tabNames = { "好友列表", "申请列表", "最近组队" };
            float startX = -290f;

            for (int i = 0; i < tabNames.Length; i++)
            {
                var idx = i;
                var go = new GameObject("SubTab" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(160, 32);
                rt.anchoredPosition = new Vector2(startX + i * 170, -155);

                var img = go.GetComponent<Image>();
                img.color = i == 0 ? ColorTabActive : ColorTabNormal;

                var txtGo = new GameObject("Text", typeof(RectTransform));
                txtGo.transform.SetParent(go.transform, false);
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                var txt = txtGo.AddComponent<Text>();
                txt.text = tabNames[i];
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 15;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = i == 0 ? Color.white : new Color(0.7f, 0.7f, 0.8f);

                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => SwitchSubTab(idx));

                _tabBtns.Add(go);
            }
        }

        private void SwitchSubTab(int tab)
        {
            if (tab == _currentTab) return;
            _currentTab = tab;

            for (int i = 0; i < _tabBtns.Count; i++)
            {
                var img = _tabBtns[i].GetComponent<Image>();
                var txt = _tabBtns[i].GetComponentInChildren<Text>();
                if (i == tab)
                {
                    img.color = ColorTabActive;
                    txt.color = Color.white;
                }
                else
                {
                    img.color = ColorTabNormal;
                    txt.color = new Color(0.7f, 0.7f, 0.8f);
                }
            }

            RefreshList();
        }

        private void RefreshList()
        {
            // 清空旧列表
            foreach (Transform child in _listContainer)
                Destroy(child.gameObject);

            var fm = FriendManager.Instance;
            if (fm == null) return;

            switch (_currentTab)
            {
                case 0: BuildFriendList(fm); break;
                case 1: BuildRequestList(); break;
                case 2: BuildRecentTeamList(); break;
            }
        }

        private void BuildFriendList(FriendManager fm)
        {
            var friends = fm.Friends;
            if (friends == null || friends.Count == 0)
            {
                var hint = CreateText(_listContainer, "EmptyHint", "暂无好友，快去添加吧", 16);
                hint.color = new Color(0.4f, 0.4f, 0.5f);
                return;
            }

            int onlineCount = 0;
            foreach (var f in friends) { if (f.Online) onlineCount++; }
            _onlineCountText.text = $"在线: {onlineCount}/{friends.Count}";

            foreach (var f in friends)
            {
                var itemGo = new GameObject("FriendItem", typeof(RectTransform));
                itemGo.transform.SetParent(_listContainer, false);
                var itemRt = itemGo.GetComponent<RectTransform>();
                itemRt.sizeDelta = new Vector2(0, 48);
                itemRt.anchorMin = new Vector2(0, 1);
                itemRt.anchorMax = new Vector2(1, 1);

                // 背景
                var itemBg = itemGo.AddComponent<Image>();
                itemBg.color = ColorCard;

                // 状态指示器
                var statusGo = new GameObject("Status", typeof(RectTransform));
                statusGo.transform.SetParent(itemRt, false);
                var statusRt = statusGo.GetComponent<RectTransform>();
                statusRt.anchorMin = new Vector2(0, 0.5f);
                statusRt.anchorMax = new Vector2(0, 0.5f);
                statusRt.sizeDelta = new Vector2(24, 24);
                statusRt.anchoredPosition = new Vector2(20, 0);

                var statusTxt = statusGo.AddComponent<Text>();
                statusTxt.text = f.Online ? "🟢" : "🔴";
                statusTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statusTxt.fontSize = 16;
                statusTxt.alignment = TextAnchor.MiddleCenter;

                // 名称
                var nameTxt = CreateText(itemRt, "Name", f.Name, 17);
                nameTxt.alignment = TextAnchor.MiddleLeft;
                nameTxt.color = Color.white;
                var nameRt = nameTxt.rectTransform;
                nameRt.anchorMin = new Vector2(0, 0.5f);
                nameRt.anchorMax = new Vector2(0, 0.5f);
                nameRt.sizeDelta = new Vector2(120, 30);
                nameRt.anchoredPosition = new Vector2(50, 0);

                // 等级
                var lvTxt = CreateText(itemRt, "Level", $"Lv.{f.Level}", 15);
                lvTxt.alignment = TextAnchor.MiddleLeft;
                lvTxt.color = new Color(0.6f, 0.6f, 0.7f);
                var lvRt = lvTxt.rectTransform;
                lvRt.anchorMin = new Vector2(0, 0.5f);
                lvRt.anchorMax = new Vector2(0, 0.5f);
                lvRt.sizeDelta = new Vector2(60, 24);
                lvRt.anchoredPosition = new Vector2(180, 0);

                // 离线标记
                if (!f.Online)
                {
                    var offTxt = CreateText(itemRt, "Offline", "(离线)", 13);
                    offTxt.alignment = TextAnchor.MiddleLeft;
                    offTxt.color = new Color(0.4f, 0.4f, 0.5f);
                    var offRt = offTxt.rectTransform;
                    offRt.anchorMin = new Vector2(0, 0.5f);
                    offRt.anchorMax = new Vector2(0, 0.5f);
                    offRt.sizeDelta = new Vector2(50, 20);
                    offRt.anchoredPosition = new Vector2(240, 0);
                }

                float btnX = 340;
                // 私聊按钮
                var chatBtn = CreateSmallButton(itemRt, "ChatBtn", "私聊", btnX, () =>
                {
                    Debug.Log($"[Friend] 私聊 {f.Name}");
                });

                // 组队按钮
                var teamBtn = CreateSmallButton(itemRt, "TeamBtn", "组队", btnX + 75, () =>
                {
                    Debug.Log($"[Friend] 组队邀请 {f.Name}");
                });

                // 删除按钮
                var delBtn = CreateSmallButton(itemRt, "DeleteBtn", "删除", btnX + 150, () =>
                {
                    Debug.Log($"[Friend] 删除好友 {f.Name}");
                    fm.RemoveFriend(f.PlayerId);
                    RefreshList();
                });
                var delImg = delBtn.GetComponent<Image>();
                delImg.color = ColorBtnDanger;
            }
        }

        private Button CreateSmallButton(RectTransform parent, string name, string text, float x, System.Action onClick)
        {
            var btn = CreateButton(parent, name, text, onClick);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(70, 30);
            rt.anchoredPosition = new Vector2(x, 0);
            var img = btn.GetComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.4f);
            var txt = btn.GetComponentInChildren<Text>();
            txt.fontSize = 13;
            return btn;
        }

        private void BuildRequestList()
        {
            var hint = CreateText(_listContainer, "EmptyHint", "暂无好友申请", 16);
            hint.color = new Color(0.4f, 0.4f, 0.5f);
        }

        private void BuildRecentTeamList()
        {
            var hint = CreateText(_listContainer, "EmptyHint", "暂无最近组队记录", 16);
            hint.color = new Color(0.4f, 0.4f, 0.5f);
        }

        private void OnAddFriendClick()
        {
            var name = _searchInput.text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Debug.Log("[Friend] 请输入玩家名称");
                return;
            }
            Debug.Log($"[Friend] 搜索并添加玩家: {name}");
            FriendManager.Instance?.AddFriend(0);
            _searchInput.text = "";
        }
    }
}