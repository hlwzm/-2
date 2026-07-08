using UnityEngine;
using System.Collections.Generic;

namespace Jx3.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; } = null!;
        private Dictionary<System.Type, BasePanel> _panels = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        public T Show<T>() where T : BasePanel
        {
            // 安全防护：确保UIRoot已创建
            if (UIRoot.Instance == null)
            {
                UIRoot.Create();
                Debug.Log("[UIManager] UIRoot was null, auto-created");
            }

            var type = typeof(T);
            if (!_panels.TryGetValue(type, out var panel))
            {
                var go = new GameObject(type.Name, typeof(RectTransform));
                go.transform.SetParent(UIRoot.Instance.PanelLayer, false);
                panel = go.AddComponent<T>();
                _panels[type] = panel;
            }
            panel.Show();
            panel.transform.SetAsLastSibling();
            return (T)panel;
        }

        public void Hide<T>() where T : BasePanel
        {
            if (_panels.TryGetValue(typeof(T), out var panel))
                panel.Hide();
        }

        public T Get<T>() where T : BasePanel
        {
            _panels.TryGetValue(typeof(T), out var panel);
            return (T)panel;
        }

        public void HideAll()
        {
            foreach (var panel in _panels.Values)
                panel.Hide();
        }
    }
}