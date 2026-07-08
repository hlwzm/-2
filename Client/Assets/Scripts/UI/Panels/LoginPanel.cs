using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    public class LoginPanel : BasePanel
    {
        private InputField _phoneInput, _pwdInput;
        private Text _statusText;
        private Button _loginBtn, _registerBtn;

        private static readonly Color ColorBg = new Color(0.06f, 0.06f, 0.12f);
        private static readonly Color ColorInputBg = new Color(0.12f, 0.12f, 0.22f);
        private static readonly Color ColorInputBorder = new Color(0.25f, 0.25f, 0.4f);
        private static readonly Color ColorBtnPrimary = new Color(0.35f, 0.25f, 0.65f);
        private static readonly Color ColorBtnSecondary = new Color(0.2f, 0.2f, 0.3f);
        private static readonly Color ColorGold = new Color(1f, 0.75f, 0.2f);
        private static readonly Color ColorText = new Color(0.85f, 0.85f, 0.9f);

        public bool autoLogin = true;

    protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            // ===== 全局背景 =====
            var bgGo = new GameObject("Bg", typeof(Image));
            bgGo.transform.SetParent(transform, false);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = ColorBg;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // ===== 装饰底纹 =====
            var decoGo = new GameObject("Deco", typeof(Image));
            decoGo.transform.SetParent(transform, false);
            var decoImg = decoGo.GetComponent<Image>();
            decoImg.color = new Color(0.1f, 0.08f, 0.18f);
            var decoRt = decoGo.GetComponent<RectTransform>();
            decoRt.anchorMin = new Vector2(0, 0);
            decoRt.anchorMax = new Vector2(1, 0.4f);
            decoRt.sizeDelta = Vector2.zero;

            // ===== 中心面板 =====
            var panelGo = new GameObject("CenterPanel", typeof(Image));
            panelGo.transform.SetParent(transform, false);
            var panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.16f, 0.95f);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(420, 500);
            panelRt.anchoredPosition = new Vector2(0, 10);

            // 面板边框
            var borderGo = new GameObject("Border", typeof(Image));
            borderGo.transform.SetParent(panelRt, false);
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.color = new Color(0.2f, 0.15f, 0.35f, 0.5f);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(4, 4);
            borderRt.anchoredPosition = Vector2.zero;

            // ===== 顶部装饰线 =====
            var topLineGo = new GameObject("TopLine", typeof(Image));
            topLineGo.transform.SetParent(panelRt, false);
            var topLineImg = topLineGo.GetComponent<Image>();
            topLineImg.color = new Color(0.5f, 0.3f, 0.9f, 0.8f);
            var topLineRt = topLineGo.GetComponent<RectTransform>();
            topLineRt.anchorMin = new Vector2(0, 1);
            topLineRt.anchorMax = new Vector2(1, 1);
            topLineRt.sizeDelta = new Vector2(0, 2);
            topLineRt.anchoredPosition = new Vector2(0, 0);

            // ===== LOGO区域 =====
            var logoY = 210;

            // LOGO底光
            var glowGo = new GameObject("Glow", typeof(Image));
            glowGo.transform.SetParent(panelRt, false);
            var glowImg = glowGo.GetComponent<Image>();
            glowImg.color = new Color(0.35f, 0.2f, 0.7f, 0.15f);
            var glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(200, 50);
            glowRt.anchoredPosition = new Vector2(0, logoY + 5);

            // 标题 - 指尖江湖2
            var titleGo = new GameObject("Title", typeof(Text));
            titleGo.transform.SetParent(panelRt, false);
            var titleTxt = titleGo.GetComponent<Text>();
            titleTxt.text = "指尖江湖2";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 36;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = Color.white;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(300, 50);
            titleRt.anchoredPosition = new Vector2(0, logoY);

            // 副标题
            var subGo = new GameObject("SubTitle", typeof(Text));
            subGo.transform.SetParent(panelRt, false);
            var subTxt = subGo.GetComponent<Text>();
            subTxt.text = "JIAN XIA JIANG HU 2";
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 14;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.5f, 0.4f, 0.7f, 0.8f);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.5f);
            subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.sizeDelta = new Vector2(300, 30);
            subRt.anchoredPosition = new Vector2(0, logoY - 35);

            // ===== 输入框 =====
            _phoneInput = CreateStyledInputField(panelRt, "PhoneInput", "手机号 / 账号", 60);
            _pwdInput = CreateStyledInputField(panelRt, "PwdInput", "密  码", 0);

            // ===== 状态文字 =====
            var statusGo = new GameObject("StatusText", typeof(Text));
            statusGo.transform.SetParent(panelRt, false);
            _statusText = statusGo.GetComponent<Text>();
            _statusText.text = "";
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 14;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = ColorGold;
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 0.5f);
            statusRt.anchorMax = new Vector2(0.5f, 0.5f);
            statusRt.sizeDelta = new Vector2(300, 30);
            statusRt.anchoredPosition = new Vector2(0, -55);

            // ===== 登录按钮 =====
            _loginBtn = CreateStyledButton(panelRt, "LoginBtn", "登  录", ColorBtnPrimary, OnLoginClick);
            var loginRt = _loginBtn.GetComponent<RectTransform>();
            loginRt.anchorMin = new Vector2(0.5f, 0.5f);
            loginRt.anchorMax = new Vector2(0.5f, 0.5f);
            loginRt.sizeDelta = new Vector2(300, 44);
            loginRt.anchoredPosition = new Vector2(0, -100);

            // ===== 注册按钮 =====
            _registerBtn = CreateStyledButton(panelRt, "RegisterBtn", "注  册", ColorBtnSecondary, OnRegisterClick);
            var regRt = _registerBtn.GetComponent<RectTransform>();
            regRt.anchorMin = new Vector2(0.5f, 0.5f);
            regRt.anchorMax = new Vector2(0.5f, 0.5f);
            regRt.sizeDelta = new Vector2(300, 44);
            regRt.anchoredPosition = new Vector2(0, -155);

            // ===== 底部版本号 =====
            var verGo = new GameObject("Version", typeof(Text));
            verGo.transform.SetParent(panelRt, false);
            var verTxt = verGo.GetComponent<Text>();
            verTxt.text = "v0.1.0 · 江湖测试版";
            verTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            verTxt.fontSize = 11;
            verTxt.alignment = TextAnchor.MiddleCenter;
            verTxt.color = new Color(0.3f, 0.3f, 0.4f);
            var verRt = verGo.GetComponent<RectTransform>();
            verRt.anchorMin = new Vector2(0.5f, 0);
            verRt.anchorMax = new Vector2(0.5f, 0);
            verRt.sizeDelta = new Vector2(200, 20);
            verRt.anchoredPosition = new Vector2(0, 12);

            // ===== 订阅事件 =====
            GameManager.Instance.OnLoginSuccess += () =>
            {
                _statusText.text = "登录成功！";
                Invoke(nameof(GoToMainCity), 0.5f);
            };
        }

        private InputField CreateStyledInputField(RectTransform parent, string name, string placeholder, float y)
        {
            // 容器（带背景和边框）
            var bgGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(parent, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(300, 44);
            bgRt.anchoredPosition = new Vector2(0, y + 80);

            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = ColorInputBg;

            // 输入框
            var inputGo = new GameObject("InputField", typeof(RectTransform));
            inputGo.transform.SetParent(bgRt, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero; inputRt.anchorMax = Vector2.one;
            inputRt.sizeDelta = new Vector2(-20, -10);
            inputRt.anchoredPosition = new Vector2(0, -2);

            var input = inputGo.AddComponent<InputField>();
            var text = inputGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = ColorText;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            input.textComponent = text;

            // 占位文字
            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(inputRt, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;

            var phText = phGo.AddComponent<Text>();
            phText.text = placeholder;
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 18;
            phText.color = new Color(0.4f, 0.4f, 0.5f);
            phText.alignment = TextAnchor.MiddleLeft;
            input.placeholder = phText;

            return input;
        }

        private Button CreateStyledButton(RectTransform parent, string name, string text, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = color;

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(onClick);

            // 按钮文字
            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var txt = txtGo.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return button;
        }

        private async void OnLoginClick()
        {
            var phone = _phoneInput.text;
            var pwd = _pwdInput.text;
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(pwd))
            { _statusText.text = ""; return; }
            _statusText.text = "连接服务器...";
            
            // 先连接，等待连接完成
            var connected = await GameManager.Instance.Network.ConnectAsync(GameManager.Instance.ServerHost, GameManager.Instance.ServerPort);
            if (!connected)
            {
                _statusText.text = "连接服务器失败";
                return;
            }
            _statusText.text = "登录中...";
            LoginManager.Instance?.SendLogin(phone, pwd);
        }

        private void OnRegisterClick()
        {
            var phone = _phoneInput.text;
            var pwd = _pwdInput.text;
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(pwd))
            { _statusText.text = ""; return; }
            LoginManager.Instance?.SendRegister(phone, pwd);
            _statusText.text = "注册成功，请登录";
        }

        private void GoToMainCity() => SceneManager.Instance.LoadScene(GameScene.MainCity);

        protected override void OnShow() { }
        protected override void OnHide() { }
    }
}