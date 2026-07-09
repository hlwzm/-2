using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Panels
{
    public class LoginPanel : BasePanel
    {
        private InputField _phoneInput, _pwdInput;
        private Text _statusText;

        protected override void Awake()
        {
            base.Awake();
            var root = transform as RectTransform;

            UIComponentFactory.CreateBackground(root);

            // Bottom decorative gradient
            var deco = new GameObject("Deco", typeof(RectTransform), typeof(Image));
            deco.transform.SetParent(root, false);
            var drt = deco.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 0); drt.anchorMax = Vector2.right;
            drt.sizeDelta = new Vector2(0, 0);
            drt.anchorMax = new Vector2(1, 0.4f);
            deco.GetComponent<Image>().color = new Color(0.07f, 0.05f, 0.12f, 0.5f);

            // Center panel card
            var card = UIComponentFactory.CreateCard(root, "CenterPanel",
                new Vector2(420, 500), new Vector2(0, 10));
            card.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.09f, 0.95f);

            // Top accent line
            var topLine = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
            topLine.transform.SetParent(card, false);
            var tlrt = topLine.GetComponent<RectTransform>();
            tlrt.anchorMin = new Vector2(0, 1); tlrt.anchorMax = Vector2.one;
            tlrt.sizeDelta = new Vector2(0, 2);
            topLine.GetComponent<Image>().color = ThemeColors.Accent;

            int logoY = 210;

            // Glow behind title
            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(card, false);
            var grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f); grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(200, 50); grt.anchoredPosition = new Vector2(0, logoY + 5);
            glow.GetComponent<Image>().color = new Color(0.83f, 0.66f, 0.26f, 0.12f);

            // Title
            var title = UIComponentFactory.CreateText(card, "Title", "鎸囧皷姹熸箹2",
                ThemeColors.FontTitle, Color.white); title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            title.rectTransform.sizeDelta = new Vector2(300, 50);
            title.rectTransform.anchoredPosition = new Vector2(0, logoY);

            // Subtitle
            var sub = UIComponentFactory.CreateText(card, "SubTitle", "JIAN XIA JIANG HU 2",
                ThemeColors.FontTiny, new Color(0.5f, 0.4f, 0.7f, 0.8f));
            sub.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            sub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            sub.rectTransform.sizeDelta = new Vector2(300, 30);
            sub.rectTransform.anchoredPosition = new Vector2(0, logoY - 35);

            // Inputs
            _phoneInput = UIComponentFactory.CreateInputField(card, "PhoneInput", "鎵嬫満鍙?/ 璐﹀彿",
                new Vector2(300, 44), new Vector2(0, 140));
            _pwdInput = UIComponentFactory.CreateInputField(card, "PwdInput", "瀵? 鐮?,
                new Vector2(300, 44), new Vector2(0, 80));

            // Status text
            _statusText = UIComponentFactory.CreateText(card, "Status", "",
                ThemeColors.FontTiny, ThemeColors.Gold);
            _statusText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _statusText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _statusText.rectTransform.sizeDelta = new Vector2(300, 30);
            _statusText.rectTransform.anchoredPosition = new Vector2(0, -55);

            // Login button
            var loginBtn = UIComponentFactory.CreatePrimaryButton(card, "LoginBtn", "鐧? 褰?, OnLoginClick);
            PositionRect(loginBtn.GetComponent<RectTransform>(), new Vector2(300, 44), new Vector2(0, -100));

            // Register button
            var regBtn = UIComponentFactory.CreateSecondaryButton(card, "RegisterBtn", "娉? 鍐?, OnRegisterClick);
            PositionRect(regBtn.GetComponent<RectTransform>(), new Vector2(300, 44), new Vector2(0, -155));

            // Version
            var ver = UIComponentFactory.CreateText(card, "Version", "v0.1.0 路 姹熸箹娴嬭瘯鐗?,
                11, ThemeColors.TextDim);
            ver.rectTransform.anchorMin = new Vector2(0.5f, 0);
            ver.rectTransform.anchorMax = new Vector2(0.5f, 0);
            ver.rectTransform.sizeDelta = new Vector2(200, 20);
            ver.rectTransform.anchoredPosition = new Vector2(0, 12);

            // Subscribe to login success
            GameManager.Instance.OnLoginSuccess += () =>
            {
                _statusText.text = "鐧诲綍鎴愬姛锛?;
                Invoke(nameof(GoToMainCity), 0.5f);
            };
        }

        private static void PositionRect(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private async void OnLoginClick()
        {
            var phone = _phoneInput.text;
            var pwd = _pwdInput.text;
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(pwd))
            { _statusText.text = ""; return; }
            _statusText.text = "杩炴帴鏈嶅姟鍣?..";
            var connected = await GameManager.Instance.Network.ConnectAsync(
                GameManager.Instance.ServerHost, GameManager.Instance.ServerPort);
            if (!connected) { _statusText.text = "杩炴帴鏈嶅姟鍣ㄥけ璐?; return; }
            _statusText.text = "鐧诲綍涓?..";
            LoginManager.Instance?.SendLogin(phone, pwd);
        }

        private void OnRegisterClick()
        {
            var phone = _phoneInput.text;
            var pwd = _pwdInput.text;
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(pwd))
            { _statusText.text = ""; return; }
            LoginManager.Instance?.SendRegister(phone, pwd);
            _statusText.text = "娉ㄥ唽鎴愬姛锛岃鐧诲綍";
        }

        private void GoToMainCity()
        {
            UIManager.Instance.Hide<LoginPanel>();
            SceneManager.Instance.LoadScene(GameScene.MainCity);
        }

        protected override void OnShow() { }
        protected override void OnHide() { }
    }
}