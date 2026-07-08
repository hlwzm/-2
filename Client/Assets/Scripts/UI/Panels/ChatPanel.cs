using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;

namespace Jx3.UI.Panels
{
    public class ChatPanel : BasePanel
    {
        private Text _chatDisplay;
        private InputField _chatInput;
        private int _currentChannel;

        protected override void Awake()
        {
            base.Awake();
            var bg = CreateImage(transform as RectTransform, "Bg", new Color(0, 0, 0, 0.85f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;

            string[] channels = {"世界", "当前", "队伍", "同盟", "系统"};
            for (int i = 0; i < 5; i++)
            {
                var idx = i;
                var btn = CreateButton(transform as RectTransform, "Ch" + i, channels[i], () => SwitchChannel(idx));
                ((RectTransform)btn.transform).anchoredPosition = new Vector2(-350 + i * 150, 300);
            }

            _chatDisplay = CreateText(transform as RectTransform, "ChatDisplay", "聊天信息将显示在这里\n", 18);
            ((RectTransform)_chatDisplay.transform).anchoredPosition = new Vector2(0, 50);
            _chatDisplay.alignment = TextAnchor.UpperLeft;
            ((RectTransform)_chatDisplay.transform).sizeDelta = new Vector2(700, 500);

            _chatInput = AddInputField(transform as RectTransform, "ChatInput", "输入聊天内容...");
            ((RectTransform)_chatInput.transform).anchoredPosition = new Vector2(-100, -280);
            ((RectTransform)_chatInput.transform).sizeDelta = new Vector2(500, 40);

            var sendBtn = CreateButton(transform as RectTransform, "SendBtn", "发送", SendChat);
            ((RectTransform)sendBtn.transform).anchoredPosition = new Vector2(300, -280);

            var closeBtn = CreateButton(transform as RectTransform, "CloseBtn", "关闭", () => Hide());
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(600, 300);
        }

        void SwitchChannel(int ch) { _currentChannel = ch; }
        void SendChat() { if (!string.IsNullOrEmpty(_chatInput.text)) { ChatManager.Instance?.SendChat(_currentChannel, _chatInput.text); _chatInput.text = ""; } }
        InputField AddInputField(RectTransform parent, string name, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var input = go.AddComponent<InputField>();
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20; text.color = Color.white;
            input.textComponent = text;
            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            var ph = phGo.AddComponent<Text>();
            ph.text = placeholder; ph.font = text.font; ph.fontSize = 20; ph.color = new Color(0.6f, 0.6f, 0.6f);
            input.placeholder = ph;
            go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
            return input;
        }
    }
}
