using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;

namespace Jx3.UI.Panels
{
    public class ChatPanel : BasePanel
    {
        private static readonly string[] ChannelNames = { "世界", "当前", "队伍", "同盟", "系统", "私聊" };
        private static readonly Color[] ChannelColors =
        {
            ThemeColors.ChatWorld, ThemeColors.ChatLocal, ThemeColors.ChatTeam,
            ThemeColors.ChatGuild, ThemeColors.ChatSystem, ThemeColors.ChatPrivate,
        };

        private int _currentChannel;
        private readonly List<Button> _channelTabs = new();
        private InputField _inputField;
        private RectTransform _contentRt;

        private readonly Dictionary<int, List<DemoMsg>> _demoMessages = new();
        private struct DemoMsg { public string Sender; public string Content; }

        protected override void Awake()
        {
            base.Awake();
            InitDemoMessages();
            BuildUI();
            RefreshMessages();
        }

        private void InitDemoMessages()
        {
            _demoMessages[0] = new List<DemoMsg>
            {
                new() { Sender = "剑侠客",   Content = "有人一起组队打副本吗？缺个强力输出。" },
                new() { Sender = "小师妹",   Content = "新手求带～有dalao带带我吗？" },
                new() { Sender = "系统公告", Content = "【世界Boss·血魔】将在5分钟后刷新，请侠士们做好准备！" },
                new() { Sender = "风清扬",   Content = "出售+12紫武「霜月」，有意的私聊～" },
                new() { Sender = "路人甲",   Content = "今天天气真好，适合挂机钓鱼 🎣" },
            };
            _demoMessages[1] = new List<DemoMsg>
            {
                new() { Sender = "附近的人", Content = "这里风景不错，截图留念！" },
                new() { Sender = "摆摊小贩", Content = "新鲜出炉的回血丹药，便宜卖啦～" },
                new() { Sender = "侠客",    Content = "有人看到那个隐藏NPC在哪吗？" },
            };
            _demoMessages[2] = new List<DemoMsg>
            {
                new() { Sender = "队长", Content = "集火BOSS，注意躲红圈！" },
                new() { Sender = "奶妈", Content = "坦克血量危险，我开大了！" },
                new() { Sender = "输出", Content = "收到，正在输出。" },
                new() { Sender = "队长", Content = "漂亮！BOSS倒了，roll装备！" },
            };
            _demoMessages[3] = new List<DemoMsg>
            {
                new() { Sender = "盟主",  Content = "今晚8点同盟战，所有人准时参加！" },
                new() { Sender = "长老",  Content = "同盟商店已刷新，有需要的去看看。" },
                new() { Sender = "成员A", Content = "收到！晚上一定到。" },
                new() { Sender = "成员B", Content = "我贡献材料升级同盟旗帜了！" },
            };
            _demoMessages[4] = new List<DemoMsg>
            {
                new() { Sender = "系统", Content = "【维护预告】服务器将于今晚凌晨2:00-4:00进行维护。" },
                new() { Sender = "系统", Content = "【活动】「七夕鹊桥」活动已上线，完成配对任务赢取限量称号！" },
                new() { Sender = "系统", Content = "【补偿】因昨日网络波动，已为所有侠士发放补偿礼包。" },
                new() { Sender = "系统", Content = "【成就】恭喜『剑心』达成成就「万人斩」！" },
            };
            _demoMessages[5] = new List<DemoMsg>
            {
                new() { Sender = "风清扬", Content = "兄弟，那把紫武你要吗？给你友情价～" },
                new() { Sender = "侠客",   Content = "好，多少银两？" },
                new() { Sender = "风清扬", Content = "5000金就行，自己人不说二话。" },
                new() { Sender = "小师妹", Content = "师父师父！我学会了新技能！" },
            };
        }

        private void BuildUI()
        {
            var root = transform as RectTransform;
            UIComponentFactory.CreateBackground(root);

            // TitleBar with close button -> back to MainCity
            UIComponentFactory.CreateTitleBar(root, "聊天", () =>
            {
                UIManager.Instance.Hide<ChatPanel>();
                UIManager.Instance.Show<MainCityPanel>();
            });

            BuildChannelTabs(root);
            BuildMessageList(root);
            BuildInputArea(root);

            // Back button top-right
            var backBtn = UIComponentFactory.CreateSecondaryButton(root, "BackToMainCity", "返回主城", () =>
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

        private void BuildChannelTabs(RectTransform parent)
        {
            float tabW = 100f, spacing = 8f;
            float totalW = ChannelNames.Length * tabW + (ChannelNames.Length - 1) * spacing;
            float startX = -totalW / 2f + tabW / 2f;

            for (int i = 0; i < ChannelNames.Length; i++)
            {
                int idx = i;
                var tab = UIComponentFactory.CreateTabButton(parent, "ChTab_" + ChannelNames[i], ChannelNames[i], i == _currentChannel, () => SwitchChannel(idx));
                var rt = tab.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(tabW, 36);
                rt.anchoredPosition = new Vector2(startX + idx * (tabW + spacing), -62);
                _channelTabs.Add(tab);
            }
        }

        private void BuildMessageList(RectTransform parent)
        {
            UIComponentFactory.CreateCard(parent, "MsgBg", new Vector2(880, 430), new Vector2(0, 12));
            _contentRt = UIComponentFactory.CreateScrollView(parent, "MsgScroll", new Vector2(868, 418), new Vector2(0, 12));

            var scrollRoot = _contentRt.parent.parent;
            var viewport = scrollRoot.Find("Viewport");
            if (viewport != null)
            {
                var vpImg = viewport.GetComponent<Image>();
                if (vpImg != null) vpImg.color = new Color(0.047f, 0.039f, 0.031f, 0.8f);
            }

            var vlg = _contentRt.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.spacing = 4f;
        }

        private void BuildInputArea(RectTransform parent)
        {
            _inputField = UIComponentFactory.CreateInputField(parent, "ChatInput", "输入聊天内容...", new Vector2(640, 44), new Vector2(-100, -248));

            var sendBtn = UIComponentFactory.CreatePrimaryButton(parent, "SendBtn", "发送", OnSendClick);
            var sendRt = sendBtn.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(0.5f, 0.5f);
            sendRt.anchorMax = new Vector2(0.5f, 0.5f);
            sendRt.sizeDelta = new Vector2(80, 44);
            sendRt.anchoredPosition = new Vector2(270, -248);

            // Voice button
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
            voiceTxt.text = "🔊 语音";
            voiceTxt.font = UIComponentFactory.Font;
            voiceTxt.fontSize = ThemeColors.FontBody;
            voiceTxt.alignment = TextAnchor.MiddleCenter;
            voiceTxt.color = ThemeColors.TextNormal;
            voiceTxt.raycastTarget = false;
            var voiceBtn = voiceBtnGo.AddComponent<Button>();
            voiceBtn.targetGraphic = voiceImg;
            voiceBtn.onClick.AddListener(OnVoiceClick);
        }

        private void SwitchChannel(int channel)
        {
            if (channel == _currentChannel) return;
            _currentChannel = channel;
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

        private void RefreshMessages()
        {
            foreach (Transform child in _contentRt)
                Destroy(child.gameObject);

            if (!_demoMessages.TryGetValue(_currentChannel, out var msgs) || msgs.Count == 0)
            {
                var empty = UIComponentFactory.CreateText(_contentRt, "Empty", "暂无聊天消息", ThemeColors.FontSmall, ThemeColors.TextDim);
                empty.rectTransform.sizeDelta = new Vector2(0, 30);
                return;
            }

            for (int i = msgs.Count - 1; i >= 0; i--)
                AppendMessage(msgs[i]);
        }

        private void AppendMessage(DemoMsg msg)
        {
            var color = ChannelColors[_currentChannel];
            var channelName = ChannelNames[_currentChannel];
            var msgText = UIComponentFactory.CreateText(_contentRt, "Msg",
                $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>[{channelName}]</color> {msg.Sender}: {msg.Content}",
                ThemeColors.FontTiny, ThemeColors.TextBright, TextAnchor.MiddleLeft);
            msgText.supportRichText = true;
            var mrt = msgText.rectTransform;
            mrt.sizeDelta = new Vector2(0, 24);
            mrt.anchorMin = new Vector2(0, 1);
            mrt.anchorMax = new Vector2(1, 1);
        }

        private void OnSendClick()
        {
            var content = _inputField?.text?.Trim();
            if (string.IsNullOrEmpty(content)) return;
            if (!_demoMessages.ContainsKey(_currentChannel))
                _demoMessages[_currentChannel] = new List<DemoMsg>();
            _demoMessages[_currentChannel].Insert(0, new DemoMsg { Sender = "我", Content = content });
            _inputField.text = "";
            RefreshMessages();
        }

        private void OnVoiceClick()
        {
            Debug.Log($"[ChatPanel] 🔊 语音按钮点击 - 当前频道: {ChannelNames[_currentChannel]}");
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshMessages();
        }
    }
}