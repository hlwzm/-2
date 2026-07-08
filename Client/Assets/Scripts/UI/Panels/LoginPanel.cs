using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class LoginPanel : BasePanel
    {
        public InputField? phoneInput;
        public InputField? passwordInput;
        public Button? loginBtn;
        public Button? registerBtn;
        public Text? statusText;

        void Start()
        {
            loginBtn?.onClick.AddListener(OnLoginClick);
            registerBtn?.onClick.AddListener(OnRegisterClick);
            statusText = GetComponentInChildren<Text>();
            // 自动连接服务器
            GameManager.Instance.ConnectToServer();
        }

        void OnLoginClick()
        {
            var phone = phoneInput?.text ?? "13800138000";
            var pwd = passwordInput?.text ?? "123456";
            SetStatus("登录中...");
            Core.LoginManager.Instance.SendLogin(phone, pwd);
        }

        void OnRegisterClick()
        {
            var phone = phoneInput?.text ?? "13800138000";
            var pwd = passwordInput?.text ?? "123456";
            Core.LoginManager.Instance.SendRegister(phone, pwd);
            SetStatus("注册请求已发送");
        }

        void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        public override void OnOpen(object data = null)
        {
            gameObject.SetActive(true);
        }

        public override void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
