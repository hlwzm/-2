using UnityEngine;

namespace Jx3.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        public string PanelName { get; protected set; } = "";
        public bool IsVisible => gameObject.activeSelf;

        protected virtual void Awake() { PanelName = GetType().Name; }
        protected virtual void Start() { }
        public virtual void Show() { gameObject.SetActive(true); OnShow(); }
        public virtual void Hide() { gameObject.SetActive(false); OnHide(); }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        public virtual void Refresh() { }

        protected T AddChild<T>(string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        protected UnityEngine.UI.Button CreateButton(RectTransform parent, string name, string text, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.3f, 0.3f, 0.5f, 0.8f);
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var txt = textGo.AddComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 50);
            return btn;
        }

        protected UnityEngine.UI.Text CreateText(RectTransform parent, string name, string text, int size = 24)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = size;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            return txt;
        }

        protected UnityEngine.UI.Image CreateImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;
            return img;
        }
    }
}
