using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;

namespace Jx3.UI.Panels
{
    /// <summary>
    /// 鑱婂ぉ闈㈡澘 鈥?澶氶閬撹亰澶?涓栫晫/褰撳墠/闃熶紞/鍚岀洘/绯荤粺/绉佽亰) + 璇煶鎸夐挳
    /// 鏆楅粦姝︿緺椋庢牸 路 鍏?UIComponentFactory + ThemeColors 鏋勫缓
    /// </summary>
    public class ChatPanel : BasePanel
    {
        // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€ 棰戦亾鍏冩暟鎹?鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
        private static readonly string[] ChannelNames = { "涓栫晫", "褰撳墠", "闃熶紞", "鍚岀洘", "绯荤粺", "绉佽亰" };
        private static readonly Color[] ChannelColors =
        {
            ThemeColors.ChatWorld,   // #88ccff
            ThemeColors.ChatLocal,   // #88ff88
            ThemeColors.ChatTeam,    // #ffcc44
            ThemeColors.ChatGuild,   // #ff88cc
            ThemeColors.ChatSystem,  // #ff8844
            ThemeColors.ChatPrivate, // #ff88ff
        };

        private int _currentChannel;
        private readonly List<Button> _channelTabs = new();
        private InputField _inputField;
        private RectTransform _contentRt;

        // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€ Demo 娑堟伅 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
        private readonly Dictionary<int, List<DemoMsg>> _demoMessages = new();

        private struct DemoMsg
        {
            public string Sender;
            public string Content;
        }

        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?        //  Lifecycle
        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?
        protected override void Awake()
        {
            base.Awake();
            InitDemoMessages();
            BuildUI();
            RefreshMessages();
        }

        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?        //  Demo Data
        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?
        private void InitDemoMessages()
        {
            _demoMessages[0] = new List<DemoMsg>
            {
                new() { Sender = "鍓戜緺瀹?,   Content = "鏈変汉涓€璧风粍闃熸墦鍓湰鍚楋紵缂轰釜寮哄姏杈撳嚭锛? },
                new() { Sender = "灏忓笀濡?,   Content = "鏂版墜姹傚甫锝炴湁dalao甯﹀甫鎴戝悧锛? },
                new() { Sender = "绯荤粺鍏憡", Content = "銆愪笘鐣孊oss路琛€榄斻€戝皢鍦?鍒嗛挓鍚庡埛鏂帮紝璇蜂緺澹滑鍋氬ソ鍑嗗锛? },
                new() { Sender = "椋庢竻鎵?,   Content = "鍑哄敭+12绱銆岄湝鏈堛€嶏紝鏈夋剰鐨勭鑱婏綖" },
                new() { Sender = "璺汉鐢?,   Content = "浠婂ぉ澶╂皵鐪熷ソ锛岄€傚悎鎸傛満閽撻奔 馃帲" },
            };

            _demoMessages[1] = new List<DemoMsg>
            {
                new() { Sender = "闄勮繎鐨勪汉", Content = "杩欓噷椋庢櫙涓嶉敊锛屾埅鍥剧暀蹇碉紒" },
                new() { Sender = "鎽嗘憡灏忚穿", Content = "鏂伴矞鍑虹倝鐨勫洖琛€涓硅嵂锛屼究瀹滃崠鍟︼綖" },
                new() { Sender = "浣?,       Content = "鏈変汉鐪嬪埌閭ｄ釜闅愯棌NPC鍦ㄥ摢鍚楋紵" },
            };

            _demoMessages[2] = new List<DemoMsg>
            {
                new() { Sender = "闃熼暱",     Content = "闆嗙伀BOSS锛屾敞鎰忚翰绾㈠湀锛? },
                new() { Sender = "濂跺",     Content = "鍧﹀厠琛€閲忓嵄闄╋紝鎴戝紑澶т簡锛? },
                new() { Sender = "浣?,       Content = "鏀跺埌锛屾鍦ㄨ緭鍑恒€? },
                new() { Sender = "闃熼暱",     Content = "婕備寒锛丅OSS鍊掍簡锛宺oll瑁呭锛? },
            };

            _demoMessages[3] = new List<DemoMsg>
            {
                new() { Sender = "鐩熶富",     Content = "浠婃櫄8鐐瑰悓鐩熸垬锛屾墍鏈変汉鍑嗘椂鍙傚姞锛? },
                new() { Sender = "闀胯€?,     Content = "鍚岀洘鍟嗗簵宸插埛鏂帮紝鏈夐渶瑕佺殑鍘荤湅鐪嬨€? },
                new() { Sender = "鎴愬憳A",    Content = "鏀跺埌锛佹櫄涓婁竴瀹氬埌銆? },
                new() { Sender = "鎴愬憳B",    Content = "鎴戣础鐚潗鏂欏崌绾у悓鐩熸棗甯滀簡锛? },
            };

            _demoMessages[4] = new List<DemoMsg>
            {
                new() { Sender = "绯荤粺",     Content = "銆愮淮鎶ら鍛娿€戞湇鍔″櫒灏嗕簬浠婃櫄鍑屾櫒2:00-4:00杩涜缁存姢銆? },
                new() { Sender = "绯荤粺",     Content = "銆愭椿鍔ㄣ€戙€屼竷澶曢箠妗ャ€嶆椿鍔ㄥ凡涓婄嚎锛屽畬鎴愰厤瀵逛换鍔¤耽鍙栭檺閲忕О鍙凤紒" },
                new() { Sender = "绯荤粺",     Content = "銆愯ˉ鍋裤€戝洜鏄ㄦ棩缃戠粶娉㈠姩锛屽凡涓烘墍鏈変緺澹彂鏀捐ˉ鍋跨ぜ鍖呫€? },
                new() { Sender = "绯荤粺",     Content = "銆愭垚灏便€戞伃鍠溿€庡墤蹇冦€忚揪鎴愭垚灏便€屼竾浜烘柀銆嶏紒" },
            };

            _demoMessages[5] = new List<DemoMsg>
            {
                new() { Sender = "椋庢竻鎵?,   Content = "鍏勫紵锛岄偅鎶婄传姝︿綘瑕佸悧锛熺粰浣犲弸鎯呬环锝? },
                new() { Sender = "浣?,       Content = "濂斤紝澶氬皯閾朵袱锛? },
                new() { Sender = "椋庢竻鎵?,   Content = "5000閲戝氨琛岋紝鑷繁浜轰笉璇翠簩璇濄€? },
                new() { Sender = "灏忓笀濡?,   Content = "甯堢埗甯堢埗锛佹垜瀛︿細浜嗘柊鎶€鑳斤紒" },
            };
        }

        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?        //  UI Construction
        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?
        private void BuildUI()
        {
            var root = transform as RectTransform;

            // 鈹€鈹€ 1. 鍏ㄥ睆鑳屾櫙 鈹€鈹€
            UIComponentFactory.CreateBackground(root);

            // 鈹€鈹€ 2. 鏍囬鏍忥紙鍏抽棴鈫掕繑鍥炰富鍩庯級 鈹€鈹€
            UIComponentFactory.CreateTitleBar(root, "鑱婂ぉ", () =>
            {
                UIManager.Instance.Hide<ChatPanel>();
                UIManager.Instance.Show<MainCityPanel>();
            });

            // 鈹€鈹€ 3. 棰戦亾 Tab 鏍?鈹€鈹€
            BuildChannelTabs(root);

            // 鈹€鈹€ 4. 娑堟伅鍒楄〃 (ScrollRect) 鈹€鈹€
            BuildMessageList(root);

            // 鈹€鈹€ 5. 搴曢儴杈撳叆鍖哄煙 鈹€鈹€
            BuildInputArea(root);

            // 鈹€鈹€ 6. 杩斿洖涓诲煄鎸夐挳 鈹€鈹€
            var backBtn = UIComponentFactory.CreateSecondaryButton(
                root, "BackToMainCity", "杩斿洖涓诲煄", () =>
                {
                    UIManager.Instance.Hide<ChatPanel>();
                    UIManager.Instance.Show<MainCityPanel>();
                });
            var backRt = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(1, 1);
            backRt.anchorMax = new Vector2(1, 1);
            backRt.sizeDelta = new Vector2(100, 36);
            backRt.anchoredPosition = new Vector2(-65, -70);
        }

        // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€ 棰戦亾 Tab 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
        private void BuildChannelTabs(RectTransform parent)
        {
            float tabW = 100f, spacing = 8f;
            float totalW = ChannelNames.Length * tabW + (ChannelNames.Length - 1) * spacing;
            float startX = -totalW / 2f + tabW / 2f;

            for (int i = 0; i < ChannelNames.Length; i++)
            {
                int idx = i;
                bool isActive = (i == _currentChannel);

                var tab = UIComponentFactory.CreateTabButton(
                    parent,
                    "ChTab_" + ChannelNames[i],
                    ChannelNames[i],
                    isActive,
                    () => SwitchChannel(idx));

                var rt = tab.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(tabW, 36);
                rt.anchoredPosition = new Vector2(startX + idx * (tabW + spacing), -62);

                _channelTabs.Add(tab);
            }
        }

        // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€ 娑堟伅鍒楄〃 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
        private void BuildMessageList(RectTransform parent)
        {
            // 鑳屾櫙鍗＄墖
            var cardRt = UIComponentFactory.CreateCard(
                parent, "MsgBg",
                new Vector2(880, 430),
                new Vector2(0, 12));

            // ScrollView
            _contentRt = UIComponentFactory.CreateScrollView(
                parent, "MsgScroll",
                new Vector2(868, 418),
                new Vector2(0, 12));

            // 缁?Viewport 鍔犱笂鏆楄壊鑳屾櫙
            var scrollRoot = _contentRt.parent.parent; // Content 鈫?Viewport 鈫?ScrollViewRoot
            var viewport = scrollRoot.Find("Viewport");
            if (viewport != null)
            {
                var vpImg = viewport.GetComponent<Image>();
                if (vpImg != null) vpImg.color = new Color(0.047f, 0.039f, 0.031f, 0.8f);
            }

            // 璋冩暣 Content LayoutGroup 杈硅窛
            var vlg = _contentRt.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.spacing = 4f;
        }

        // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€ 搴曢儴杈撳叆鍖?鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
        private void BuildInputArea(RectTransform parent)
        {
            // 杈撳叆妗?            _inputField = UIComponentFactory.CreateInputField(
                parent, "ChatInput", "杈撳叆鑱婂ぉ鍐呭鈥?,
                new Vector2(640, 44),
                new Vector2(-100, -248));

            // 鍙戦€佹寜閽?            var sendBtn = UIComponentFactory.CreatePrimaryButton(
                parent, "SendBtn", "鍙戦€?, OnSendClick);
            var sendRt = sendBtn.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(0.5f, 0.5f);
            sendRt.anchorMax = new Vector2(0.5f, 0.5f);
            sendRt.sizeDelta = new Vector2(80, 44);
            sendRt.anchoredPosition = new Vector2(270, -248);

            // 璇煶鎸夐挳 馃攰
            var voiceBtnGo = new GameObject("VoiceBtn", typeof(RectTransform), typeof(Image));
            voiceBtnGo.transform.SetParent(parent, false);
            var voiceRt = voiceBtnGo.GetComponent<RectTransform>();
            voiceRt.anchorMin = new Vector2(0.5f, 0.5f);
            voiceRt.anchorMax = new Vector2(0.5f, 0.5f);
            voiceRt.sizeDelta = new Vector2(90, 44);
            voiceRt.anchoredPosition = new Vector2(-440, -248);

            var voiceImg = voiceBtnGo.GetComponent<Image>();
            voiceImg.color = ThemeColors.BtnSecondary;

            var voiceTxt = voiceBtnGo.AddComponent<Text>();
            voiceTxt.text = "馃攰 璇煶";
            voiceTxt.font = UIComponentFactory.Font;
            voiceTxt.fontSize = ThemeColors.FontBody;
            voiceTxt.alignment = TextAnchor.MiddleCenter;
            voiceTxt.color = ThemeColors.TextNormal;
            voiceTxt.raycastTarget = false;

            var voiceBtn = voiceBtnGo.AddComponent<Button>();
            voiceBtn.targetGraphic = voiceImg;
            voiceBtn.onClick.AddListener(OnVoiceClick);
        }

        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?        //  Channel Switching
        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?
        private void SwitchChannel(int channel)
        {
            if (channel == _currentChannel) return;
            _currentChannel = channel;

            // 鏇存柊 Tab 楂樹寒
            for (int i = 0; i < _channelTabs.Count; i++)
            {
                var img = _channelTabs[i].GetComponent<Image>();
                var txt = _channelTabs[i].GetComponent<Text>();
                bool active = (i == channel);
                img.color = active ? ThemeColors.TabActive : ThemeColors.TabInactive;
                txt.color = active ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            }

            RefreshMessages();
        }

        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?        //  Message Display
        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?
        private void RefreshMessages()
        {
            // 娓呴櫎鏃ф秷鎭?            foreach (Transform child in _contentRt)
                Destroy(child.gameObject);

            if (!_demoMessages.TryGetValue(_currentChannel, out var msgs) || msgs.Count == 0)
            {
                var empty = UIComponentFactory.CreateText(
                    _contentRt, "Empty", "鏆傛棤鑱婂ぉ娑堟伅",
                    ThemeColors.FontSmall, ThemeColors.TextDim);
                var ert = empty.rectTransform;
                ert.sizeDelta = new Vector2(0, 30);
                return;
            }

            // 鏈€鏂版秷鎭湪涓婃柟
            for (int i = msgs.Count - 1; i >= 0; i--)
            {
                AppendMessage(msgs[i]);
            }
        }

        private void AppendMessage(DemoMsg msg)
        {
            var color = ChannelColors[_currentChannel];
            var channelName = ChannelNames[_currentChannel];

            var msgText = UIComponentFactory.CreateText(
                _contentRt, "Msg",
                $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>[{channelName}]</color> {msg.Sender}: {msg.Content}",
                ThemeColors.FontTiny, ThemeColors.TextBright, TextAnchor.MiddleLeft);

            msgText.supportRichText = true;
            var mrt = msgText.rectTransform;
            mrt.sizeDelta = new Vector2(0, 24);
            mrt.anchorMin = new Vector2(0, 1);
            mrt.anchorMax = new Vector2(1, 1);
        }

        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?        //  Input Actions
        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?
        private void OnSendClick()
        {
            var content = _inputField?.text?.Trim();
            if (string.IsNullOrEmpty(content)) return;

            // 娣诲姞鍒板綋鍓嶉閬撶殑 demo 娑堟伅
            if (!_demoMessages.ContainsKey(_currentChannel))
                _demoMessages[_currentChannel] = new List<DemoMsg>();

            _demoMessages[_currentChannel].Insert(0, new DemoMsg
            {
                Sender = "浣?,
                Content = content
            });

            _inputField.text = "";
            RefreshMessages();

            Debug.Log($"[ChatPanel] 鍙戦€?[{ChannelNames[_currentChannel]}] 娑堟伅: {content}");
        }

        private void OnVoiceClick()
        {
            Debug.Log($"[ChatPanel] 馃帳 璇煶鎸夐挳鐐瑰嚮 鈥?褰撳墠棰戦亾: {ChannelNames[_currentChannel]}");
            // TODO: 鎺ュ叆 VoiceChatManager 褰曢煶娴佺▼
        }

        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?        //  Panel Overrides
        // 鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺?
        public override void Refresh()
        {
            base.Refresh();
            RefreshMessages();
        }
    }
}