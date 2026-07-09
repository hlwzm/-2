using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI
{
    /// <summary>
    /// Reusable UI component factory. All methods create styled GameObjects
    /// following the design spec (ThemeColors).
    /// </summary>
    public static class UIComponentFactory
    {
        static Font _font;
        public static Font Font => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // =====================================================================
        // Background & Container
        // =====================================================================

        public static Image CreateBackground(RectTransform parent, string name = "Bg")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = ThemeColors.BgMain;
            return img;
        }

        public static RectTransform CreateCard(RectTransform parent, string name, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = ThemeColors.BgCard;

            // Border
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(rt, false);
            var brt = border.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.sizeDelta = new Vector2(-2, -2);
            brt.anchoredPosition = Vector2.zero;
            border.GetComponent<Image>().color = ThemeColors.Border;

            return rt;
        }

        // =====================================================================
        // Title Bar with close button
        // =====================================================================

        public static RectTransform CreateTitleBar(RectTransform parent, string title, System.Action onClose = null)
        {
            var bar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(0, 56); rt.anchoredPosition = new Vector2(0, -28);
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.95f);

            // Bottom decoration line
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(rt, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = new Vector2(0, 2); lrt.anchoredPosition = new Vector2(0, 0);
            line.GetComponent<Image>().color = ThemeColors.Accent;

            // Title text
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(rt, false);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = Vector2.one;
            trt.sizeDelta = new Vector2(-60, 0); trt.anchoredPosition = new Vector2(20, 0);
            var titleText = titleGo.AddComponent<Text>();
            titleText.text = title;
            titleText.font = Font; titleText.fontSize = ThemeColors.FontPanelTitle;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = ThemeColors.TextBright;

            // Close button
            if (onClose != null)
            {
                var closeBtn = CreateIconButton(rt, "CloseBtn", "✕", 36, onClose);
                var crt = closeBtn.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(1, 0.5f); crt.anchorMax = new Vector2(1, 0.5f);
                crt.anchoredPosition = new Vector2(-28, 0);
            }

            return rt;
        }

        // =====================================================================
        // Buttons
        // =====================================================================

        public static Button CreateButton(RectTransform parent, string name, string text,
            Color color, System.Action onClick, int fontSize = ThemeColors.FontBody)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Text
            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = text; txt.font = Font; txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = ThemeColors.TextWhite;
            txt.raycastTarget = false;

            return btn;
        }

        public static Button CreatePrimaryButton(RectTransform parent, string name, string text, System.Action onClick)
            => CreateButton(parent, name, text, ThemeColors.BtnPrimary, onClick);

        public static Button CreateSecondaryButton(RectTransform parent, string name, string text, System.Action onClick)
            => CreateButton(parent, name, text, ThemeColors.BtnSecondary, onClick);

        public static Button CreateIconButton(RectTransform parent, string name, string icon,
            float size, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);

            var btn = go.AddComponent<Button>();
            var txt = go.AddComponent<Text>();
            txt.text = icon; txt.font = Font; txt.fontSize = (int)(size * 0.6f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = ThemeColors.TextDim;
            txt.raycastTarget = true;
            btn.targetGraphic = txt;
            btn.onClick.AddListener(() => onClick?.Invoke());

            return btn;
        }

        // =====================================================================
        // Text
        // =====================================================================

        public static Text CreateText(RectTransform parent, string name, string text,
            int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = text; txt.font = Font; txt.fontSize = fontSize;
            txt.alignment = alignment; txt.color = color;
            txt.raycastTarget = false;
            return txt;
        }

        // =====================================================================
        // Input Field
        // =====================================================================

        public static InputField CreateInputField(RectTransform parent, string name, string placeholder,
            Vector2 size, Vector2 pos)
        {
            var bg = new GameObject(name, typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(parent, false);
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            bg.GetComponent<Image>().color = ThemeColors.BgInput;

            var input = bg.AddComponent<InputField>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(rt, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = new Vector2(-20, 0);
            textRt.anchoredPosition = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = Font; text.fontSize = ThemeColors.FontBody;
            text.color = ThemeColors.TextBright;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            input.textComponent = text;

            var ph = new GameObject("Placeholder", typeof(RectTransform));
            ph.transform.SetParent(rt, false);
            var phrt = ph.GetComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
            phrt.sizeDelta = new Vector2(-20, 0);
            phrt.anchoredPosition = Vector2.zero;
            var phText = ph.AddComponent<Text>();
            phText.text = placeholder; phText.font = Font;
            phText.fontSize = ThemeColors.FontBody;
            phText.color = ThemeColors.TextDim;
            phText.alignment = TextAnchor.MiddleLeft;
            input.placeholder = phText;

            return input;
        }

        // =====================================================================
        // Divider
        // =====================================================================

        public static Image CreateDivider(RectTransform parent, string name = "Divider")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(0, 2);
            var img = go.GetComponent<Image>();
            img.color = ThemeColors.DecorLine;
            return img;
        }

        // =====================================================================
        // ScrollView
        // =====================================================================

        public static RectTransform CreateScrollView(RectTransform parent, string name,
            Vector2 size, Vector2 pos)
        {
            // Root
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = size; rootRt.anchoredPosition = pos;
            root.GetComponent<Image>().color = new Color(0, 0, 0, 0); // transparent

            // ScrollRect
            var scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            // Viewport
            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vp.transform.SetParent(rootRt, false);
            var vpRt = vp.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            var vpImg = vp.GetComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.01f); // Mask needs a Graphic
            vp.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(vpRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = Vector2.one;
            contentRt.sizeDelta = new Vector2(0, 0);
            contentRt.pivot = new Vector2(0.5f, 1f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = ThemeColors.SpacingSmall;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = contentRt;

            return contentRt;
        }

        // =====================================================================
        // Tab Button
        // =====================================================================

        public static Button CreateTabButton(RectTransform parent, string name, string text,
            bool active, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = active ? ThemeColors.TabActive : ThemeColors.TabInactive;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = go.AddComponent<Text>();
            txt.text = text; txt.font = Font;
            txt.fontSize = ThemeColors.FontBody;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = active ? ThemeColors.TextWhite : ThemeColors.TextNormal;
            txt.raycastTarget = true;

            return btn;
        }

        // =====================================================================
        // Currency Display
        // =====================================================================

        public static Text CreateCurrencyLabel(RectTransform parent, string name,
            string icon, string value, Color valueColor, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(160, 30); rt.anchoredPosition = pos;

            var txt = go.AddComponent<Text>();
            txt.text = $"{icon} {value}";
            txt.font = Font; txt.fontSize = ThemeColors.FontBody;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = valueColor;
            txt.raycastTarget = false;

            return txt;
        }
    }
}
