using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class ChatPanel : BasePanel
    {
        public Transform? messageContainer;
        public GameObject? messagePrefab;
        public InputField? chatInput;
        public Button? sendBtn;
        public Dropdown? channelDropdown;

        void Start()
        {
            sendBtn?.onClick.AddListener(OnSendClick);
            Core.ChatManager.Instance.OnNewMessage += OnNewChatMessage;
        }

        void OnSendClick()
        {
            var text = chatInput?.text ?? "";
            if (string.IsNullOrEmpty(text)) return;
            var channel = channelDropdown != null ? channelDropdown.value : 0;
            Core.ChatManager.Instance.SendChat(channel, text);
            if (chatInput != null) chatInput.text = "";
        }

        void OnNewChatMessage(Core.ChatMessage msg)
        {
            if (messageContainer == null || messagePrefab == null) return;
            var item = Instantiate(messagePrefab, messageContainer);
            var txt = item.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = $"[{msg.SenderName}] {msg.Content}";
        }

        public override void OnOpen(object data = null) => gameObject.SetActive(true);
        public override void OnClose() => gameObject.SetActive(false);
    }
}
