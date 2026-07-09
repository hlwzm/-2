using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Jx3.Core.Network;
using System.Collections;

namespace Jx3.UI.Panels
{
    public class ChatPanel : BasePanel
    {
        private readonly string[] _channelNames = { "世界", "当前", "队伍", "同盟", "系统" };
        private readonly Color[] _channelColors =
        {
            new Color(0.53f, 0.80f, 1.0f),  // 世界 #88ccff
            new Color(0.53f, 1.0f, 0.53f),  // 当前 #88ff88
            new Color(1.0f, 0.80f, 0.27f),  // 队伍 #ffcc44
            new Color(1.0f, 0.53f, 0.80f),  // 同盟 #ff88cc
            new Color(1.0f, 0.53f, 0.27f),  // 系统 #ff8844
        };
        private static readonly Color PrivateColor = new Color(1.0f, 0.53f, 0.53f); // #ff88ff

        private int _currentChannel;
        private readonly List<GameObject> _channelBtns = new();
        private InputField _inputField;
        private Text _privateBadge;
        private RectTransform _contentRt;

        private static readonly Color ColorTabNormal = new Color(0.15f, 0.15f, 0.25f);
        private static readonly Color ColorTabActive = new Color(0.33f, 0.2f, 0.5f);
        private static readonly Color ColorBg = new Color(0.06f, 0.06f, 0.12f, 0.95f);
        private static readonly Color ColorInputBg = new Color(0.12f, 0.12f, 0.22f);

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            RefreshMessages();

            // 创建语音指示器(只创建一次)
            if (UIRoot.Instance != null && VoiceChatManager.Instance != null)
            {
                var indicatorGo = UIRoot.Instance.TopLayer.Find("VoiceIndicator")?.gameObject;
                if (indicatorGo == null)
                {
                    indicatorGo = new GameObject("VoiceIndicator", typeof(RectTransform));
                    indicatorGo.transform.SetParent(UIRoot.Instance.TopLayer, false);
                    indicatorGo.AddComponent<VoiceIndicator>();
                }
            }
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

            // ===== 顶部Tab栏 =====
            BuildChannelTabs(rootRt);

            // ===== 私聊标签（右侧） =====
            BuildPrivateTab(rootRt);

            // ===== 关闭按钮 =====
            var closeBtn = CreateButton(rootRt, "CloseBtn", "✕", () => Hide());
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-15, -15);
            closeRt.sizeDelta = new Vector2(36, 36);
            var closeImg = closeBtn.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.25f, 0.35f);

            // ===== 消息列表 (ScrollRect) =====
            BuildMessageList(rootRt);

            // ===== 底部输入区域 =====
            BuildInputArea(rootRt);
        }

        private void BuildChannelTabs(RectTransform parent)
        {
            float startX = -450f;
            for (int i = 0; i < _channelNames.Length; i++)
            {
                var idx = i;
                var go = new GameObject("ChTab" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(100, 34);
                rt.anchoredPosition = new Vector2(startX + i * 110, -17);

                var img = go.GetComponent<Image>();
                img.color = i == 0 ? ColorTabActive : ColorTabNormal;

                var txtGo = new GameObject("Text", typeof(RectTransform));
                txtGo.transform.SetParent(go.transform, false);
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                var txt = txtGo.AddComponent<Text>();
                txt.text = _channelNames[i];
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 16;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = i == 0 ? Color.white : new Color(0.7f, 0.7f, 0.8f);

                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => SwitchChannel(idx));

                _channelBtns.Add(go);
            }
        }

        private void BuildPrivateTab(RectTransform parent)
        {
            var go = new GameObject("PrivateTab", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(100, 34);
            rt.anchoredPosition = new Vector2(50 + 5 * 110, -17);

            var img = go.GetComponent<Image>();
            img.color = ColorTabNormal;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = new Vector2(-16, 0);

            var txt = txtGo.AddComponent<Text>();
            txt.text = "🔒私聊";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.7f, 0.7f, 0.8f);

            // 未读标记
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(go.transform, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 1f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.sizeDelta = new Vector2(18, 18);
            badgeRt.anchoredPosition = new Vector2(-4, -4);
            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.color = new Color(1f, 0.2f, 0.2f);
            badgeGo.SetActive(false);

            _privateBadge = badgeGo.AddComponent<Text>();
            _privateBadge.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _privateBadge.fontSize = 11;
            _privateBadge.alignment = TextAnchor.MiddleCenter;
            _privateBadge.color = Color.white;
            _privateBadge.text = "0";

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => SwitchChannel(5)); // 5 = private
        }

        private void BuildMessageList(RectTransform parent)
        {
            // Viewport
            var viewportGo = new GameObject("MessageViewport", typeof(RectTransform), typeof(Image));
            viewportGo.transform.SetParent(parent, false);
            var vpRt = viewportGo.GetComponent<RectTransform>();
            vpRt.anchorMin = new Vector2(0.5f, 0.5f);
            vpRt.anchorMax = new Vector2(0.5f, 0.5f);
            vpRt.sizeDelta = new Vector2(880, 420);
            vpRt.anchoredPosition = new Vector2(0, 30);
            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.color = new Color(0.08f, 0.08f, 0.16f);

            // ScrollRect
            var scrollGo = new GameObject("ScrollRect", typeof(RectTransform));
            scrollGo.transform.SetParent(vpRt, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.sizeDelta = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(scrollRt, false);
            _contentRt = contentGo.GetComponent<RectTransform>();
            _contentRt.anchorMin = new Vector2(0, 1);
            _contentRt.anchorMax = new Vector2(1, 1);
            _contentRt.pivot = new Vector2(0.5f, 1);
            _contentRt.sizeDelta = new Vector2(0, 0);

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2;
            contentLayout.padding = new RectOffset(8, 8, 4, 4);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = _contentRt;
        }

        private void BuildInputArea(RectTransform parent)
        {
            // 输入框背景
            var inputBg = new GameObject("InputBg", typeof(RectTransform), typeof(Image));
            inputBg.transform.SetParent(parent, false);
            var inputBgRt = inputBg.GetComponent<RectTransform>();
            inputBgRt.anchorMin = new Vector2(0.5f, 0f);
            inputBgRt.anchorMax = new Vector2(0.5f, 0f);
            inputBgRt.sizeDelta = new Vector2(700, 44);
            inputBgRt.anchoredPosition = new Vector2(0, 30);
            var inputBgImg = inputBg.GetComponent<Image>();
            inputBgImg.color = ColorInputBg;

            // 输入框
            var inputGo = new GameObject("ChatInput", typeof(RectTransform));
            inputGo.transform.SetParent(inputBgRt, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero; inputRt.anchorMax = Vector2.one;
            inputRt.sizeDelta = new Vector2(-10, -8);
            inputRt.anchoredPosition = new Vector2(0, -2);

            _inputField = inputGo.AddComponent<InputField>();
            var inputText = inputGo.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 16;
            inputText.color = new Color(0.85f, 0.85f, 0.9f);
            inputText.supportRichText = false;
            inputText.alignment = TextAnchor.MiddleLeft;
            _inputField.textComponent = inputText;

            // 占位文字
            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(inputRt, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;

            var phText = phGo.AddComponent<Text>();
            phText.text = "输入聊天内容...";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 16;
            phText.color = new Color(0.4f, 0.4f, 0.5f);
            phText.alignment = TextAnchor.MiddleLeft;
            _inputField.placeholder = phText;

            // 发送按钮
            var sendBtn = CreateButton(parent, "SendBtn", "发送", OnSendClick);
            var sendRt = sendBtn.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(0.5f, 0f);
            sendRt.anchorMax = new Vector2(0.5f, 0f);
            sendRt.anchoredPosition = new Vector2(430, 30);
            sendRt.sizeDelta = new Vector2(80, 44);
            var sendImg = sendBtn.GetComponent<Image>();
            sendImg.color = new Color(0.35f, 0.2f, 0.65f);
            var sendTxt = sendBtn.GetComponentInChildren<Text>();
            sendTxt.fontSize = 18;

            // 语音按钮(长按录音)
            var voiceGo = new GameObject("VoiceBtn", typeof(RectTransform), typeof(Image));
            voiceGo.transform.SetParent(parent, false);
            var voiceRtL = voiceGo.GetComponent<RectTransform>();
            voiceRtL.anchorMin = new Vector2(0.5f, 0f);
            voiceRtL.anchorMax = new Vector2(0.5f, 0f);
            voiceRtL.anchoredPosition = new Vector2(-430, 30);
            voiceRtL.sizeDelta = new Vector2(100, 44);
            var voiceImgL = voiceGo.GetComponent<Image>();
            voiceImgL.color = new Color(0.25f, 0.25f, 0.4f);

            var voiceTxtGo = new GameObject("Text", typeof(RectTransform));
            voiceTxtGo.transform.SetParent(voiceGo.transform, false);
            var voiceTxtRt = voiceTxtGo.GetComponent<RectTransform>();
            voiceTxtRt.anchorMin = Vector2.zero; voiceTxtRt.anchorMax = Vector2.one;
            voiceTxtRt.sizeDelta = Vector2.zero;
            var voiceTxtL = voiceTxtGo.AddComponent<Text>();
            voiceTxtL.text = "🎤 语音";
            voiceTxtL.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            voiceTxtL.fontSize = 16;
            voiceTxtL.alignment = TextAnchor.MiddleCenter;
            voiceTxtL.color = new Color(0.7f, 0.7f, 0.8f);

            // 长按事件: 添加EventTrigger
            var voiceTrigger = voiceGo.AddComponent<EventTrigger>();
            // PointerDown = 开始录音
            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener((_) => OnVoiceBtnDown(voiceImgL, voiceTxtL));
            voiceTrigger.triggers.Add(downEntry);
            // PointerUp = 停止录音
            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener((_) => OnVoiceBtnUp(voiceImgL, voiceTxtL));
            voiceTrigger.triggers.Add(upEntry);
            // PointerExit = 取消录音
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((_) => OnVoiceBtnUp(voiceImgL, voiceTxtL));
            voiceTrigger.triggers.Add(exitEntry);
        }

        private void SwitchChannel(int ch)
        {
            if (ch == _currentChannel) return;
            _currentChannel = ch;

            // 更新Tab高亮
            for (int i = 0; i < _channelBtns.Count; i++)
            {
                var img = _channelBtns[i].GetComponent<Image>();
                var txt = _channelBtns[i].GetComponentInChildren<Text>();
                if (i == ch)
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

            RefreshMessages();
        }

        private void RefreshMessages()
        {
            // 清除旧消息
            foreach (Transform child in _contentRt)
                Destroy(child.gameObject);

            // 从ChatManager获取历史消息
            var messages = ChatManager.Instance?.Messages;
            if (messages == null || messages.Count == 0)
            {
                var emptyText = CreateText(_contentRt, "EmptyHint", "暂无聊天消息", 16);
                emptyText.color = new Color(0.4f, 0.4f, 0.5f);
                return;
            }

            for (int i = messages.Count - 1; i >= 0; i--)
            {
                var msg = messages[i];
                if (_currentChannel < 5 && msg.Channel != _currentChannel) continue;
                if (_currentChannel == 5 && msg.Channel != 5) continue;

                AppendMessage(msg);
            }
        }

        private void AppendMessage(ChatMessage msg)
        {
            Color channelColor;
            string channelTag;
            if (msg.Channel >= 0 && msg.Channel < 5)
            {
                channelColor = _channelColors[msg.Channel];
                channelTag = _channelNames[msg.Channel];
            }
            else
            {
                channelColor = PrivateColor;
                channelTag = "私聊";
            }

            // 语音消息特殊处理
            if (msg.Content.StartsWith("🎤"))
            {
                AppendVoiceMessage(msg, channelTag, channelColor);
                return;
            }

            var msgGo = new GameObject("Msg" + msg.MsgId, typeof(RectTransform));
            msgGo.transform.SetParent(_contentRt, false);
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.sizeDelta = new Vector2(0, 22);
            msgRt.anchorMin = new Vector2(0, 1);
            msgRt.anchorMax = new Vector2(1, 1);

            // 整个消息行使用一个Text(支持rich text来处理颜色)
            var lineText = msgGo.AddComponent<Text>();
            lineText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lineText.fontSize = 15;
            lineText.alignment = TextAnchor.MiddleLeft;
            lineText.supportRichText = true;
            lineText.color = new Color(0.8f, 0.8f, 0.85f);

            string colorHex = ColorUtility.ToHtmlStringRGB(channelColor);
            string lineContent = $"<color=#{colorHex}>[{channelTag}]</color> {msg.SenderName}: {msg.Content}";
            lineText.text = lineContent;
        }

        private void OnSendClick()
        {
            if (string.IsNullOrEmpty(_inputField.text)) return;
            var content = _inputField.text;
            if (_currentChannel == 5)
            {
                ChatManager.Instance?.SendChat(5, content);
            }
            else
            {
                ChatManager.Instance?.SendChat(_currentChannel, content);
            }
            _inputField.text = "";
        }

        // ===== 语音按钮长按 =====

        private void OnVoiceBtnDown(Image btnImg, Text btnTxt)
        {
            if (VoiceChatManager.Instance == null) return;

            // 设置语音频道
            VoiceChatManager.Instance.CurrentChannel = _currentChannel;
            // 强制按下按键(模拟V键)
            VoiceChatManager.Instance.PushToTalkKey = KeyCode.None; // 临时取消键盘PTT
            VoiceChatManager.Instance.StartRecording();

            btnImg.color = new Color(0.35f, 0.5f, 0.2f); // 绿色高亮
            btnTxt.text = "🎤 录音中...";
            Debug.Log("[ChatPanel] 语音按钮按下 - 开始录音");
        }

        private void OnVoiceBtnUp(Image btnImg, Text btnTxt)
        {
            if (VoiceChatManager.Instance == null) return;
            if (!VoiceChatManager.Instance.IsRecording) return;

            VoiceChatManager.Instance.StopRecording();
            VoiceChatManager.Instance.PushToTalkKey = KeyCode.V; // 恢复键盘PTT

            btnImg.color = new Color(0.25f, 0.25f, 0.4f);
            btnTxt.text = "🎤 语音";

            // 发送语音消息到聊天
            ChatManager.Instance?.SendChat(_currentChannel, "🎤 [语音消息]");
            Debug.Log("[ChatPanel] 语音按钮松开 - 语音消息已发送");
        }

        /// <summary>语音消息行添加播放按钮</summary>
        private void AppendVoiceMessage(ChatMessage msg, string channelTag, Color channelColor)
        {
            var msgGo = new GameObject("VoiceMsg" + msg.MsgId, typeof(RectTransform));
            msgGo.transform.SetParent(_contentRt, false);
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.sizeDelta = new Vector2(0, 28);
            msgRt.anchorMin = new Vector2(0, 1);
            msgRt.anchorMax = new Vector2(1, 1);

            // 频道标签
            var tagGo = new GameObject("Tag", typeof(RectTransform));
            tagGo.transform.SetParent(msgGo.transform, false);
            var tagRt = tagGo.GetComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0, 0);
            tagRt.anchorMax = new Vector2(0, 1);
            tagRt.sizeDelta = new Vector2(60, 0);
            var tagTxt = tagGo.AddComponent<Text>();
            tagTxt.text = $"[{channelTag}]";
            tagTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tagTxt.fontSize = 14;
            tagTxt.color = channelColor;
            tagTxt.alignment = TextAnchor.MiddleLeft;

            // 发送者名
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(msgGo.transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(0, 1);
            nameRt.sizeDelta = new Vector2(100, 0);
            nameRt.anchoredPosition = new Vector2(65, 0);
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.text = msg.SenderName + ": ";
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize = 14;
            nameTxt.color = new Color(0.8f, 0.8f, 0.85f);
            nameTxt.alignment = TextAnchor.MiddleLeft;

            // 🎤标记
            var micGo = new GameObject("MicIcon", typeof(RectTransform));
            micGo.transform.SetParent(msgGo.transform, false);
            var micRt = micGo.GetComponent<RectTransform>();
            micRt.anchorMin = new Vector2(0, 0);
            micRt.anchorMax = new Vector2(0, 1);
            micRt.sizeDelta = new Vector2(24, 0);
            micRt.anchoredPosition = new Vector2(170, 0);
            var micTxt = micGo.AddComponent<Text>();
            micTxt.text = "🎤";
            micTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            micTxt.fontSize = 16;
            micTxt.alignment = TextAnchor.MiddleCenter;

            // 播放按钮
            var playBtn = new GameObject("PlayBtn", typeof(RectTransform), typeof(Image));
            playBtn.transform.SetParent(msgGo.transform, false);
            var playRt = playBtn.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0, 0);
            playRt.anchorMax = new Vector2(0, 1);
            playRt.sizeDelta = new Vector2(60, 0);
            playRt.anchoredPosition = new Vector2(200, 0);
            var playImg = playBtn.GetComponent<Image>();
            playImg.color = new Color(0.25f, 0.25f, 0.4f);
            var playBtnTxtGo = new GameObject("Text", typeof(RectTransform));
            playBtnTxtGo.transform.SetParent(playBtn.transform, false);
            var playBtnTxtRt = playBtnTxtGo.GetComponent<RectTransform>();
            playBtnTxtRt.anchorMin = Vector2.zero; playBtnTxtRt.anchorMax = Vector2.one;
            playBtnTxtRt.sizeDelta = Vector2.zero;
            var playBtnTxt = playBtnTxtGo.AddComponent<Text>();
            playBtnTxt.text = "▶ 播放";
            playBtnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playBtnTxt.fontSize = 13;
            playBtnTxt.alignment = TextAnchor.MiddleCenter;
            playBtnTxt.color = new Color(0.5f, 0.8f, 1.0f);
            var playBtnComp = playBtn.AddComponent<Button>();
            playBtnComp.targetGraphic = playImg;
            playBtnComp.onClick.AddListener(() => PlayVoiceMessage(msg.MsgId, playBtnTxt));
        }

        private void PlayVoiceMessage(ulong msgId, Text btnTxt)
        {
            // 播放语音(模拟 - 实际应有语音数据关联)
            if (VoiceChatManager.Instance != null)
            {
                btnTxt.text = "▶ 播放中...";
                Debug.Log($"[ChatPanel] 播放语音消息 msgId={msgId}");
                // 使用协程1.5秒后恢复
                StartCoroutine(ResetPlayButton(btnTxt));
            }
        }

        private System.Collections.IEnumerator ResetPlayButton(Text btnTxt)
        {
            yield return new WaitForSeconds(1.5f);
            if (btnTxt != null) btnTxt.text = "▶ 播放";
        }
    }
}



