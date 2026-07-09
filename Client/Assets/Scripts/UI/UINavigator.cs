using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

namespace Jx3.UI
{
    /// <summary>
    /// UI导航系统 — 管理面板栈、侧边栏、主HUD
    /// </summary>
    public class UINavigator : MonoBehaviour
    {
        public static UINavigator Instance { get; private set; }

        [Header("层级引用")]
        public RectTransform hudLayer;      // 常驻HUD（顶栏+底栏）
        public RectTransform menuLayer;     // 侧边菜单
        public RectTransform panelLayer;    // 面板区域
        public RectTransform popupLayer;    // 弹窗层

        [Header("预制体")]
        public GameObject sidebarPrefab;    // 侧边栏
        public GameObject hudPrefab;        // 主HUD

        private Stack<System.Type> _panelStack = new();
        private GameObject _currentSidebar;
        private GameObject _currentHud;
        private BasePanel _currentPanel;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // 创建常驻HUD（无论哪个场景都显示）
            if (hudPrefab != null)
                _currentHud = Instantiate(hudPrefab, hudLayer);

            // 创建侧边栏
            if (sidebarPrefab != null)
                _currentSidebar = Instantiate(sidebarPrefab, menuLayer);
        }

        /// <summary>打开面板（压栈）</summary>
        public void Open<T>() where T : BasePanel
        {
            // 如果已有面板，先隐藏到后台
            if (_currentPanel != null)
            {
                _currentPanel.gameObject.SetActive(false);
                // 缩小/移动到侧边
            }

            var type = typeof(T);
            var panel = UIManager.Instance.Show<T>();

            if (panel != null)
            {
                panel.transform.SetParent(panelLayer, false);
                _currentPanel = panel;
                _panelStack.Push(type);

                // 通知侧边栏更新状态
                OnPanelChanged(type);
            }
        }

        /// <summary>打开面板（非泛型版本，供Sidebar调用）</summary>
        public void OpenByType(System.Type panelType)
        {
            var method = typeof(UINavigator).GetMethod("Open", BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                var generic = method.MakeGenericMethod(panelType);
                generic.Invoke(this, null);
            }
        }

        /// <summary>返回上一面板（出栈）</summary>
        public void GoBack()
        {
            if (_panelStack.Count <= 1) return;

            _panelStack.Pop(); // 移除当前
            var prevType = _panelStack.Peek();

            // 隐藏当前面板
            if (_currentPanel != null)
            {
                Destroy(_currentPanel.gameObject);
                _currentPanel = null;
            }

            // 重新显示上一面板
            var method = typeof(UINavigator).GetMethod("Open").MakeGenericMethod(prevType);
            method.Invoke(this, null);
        }

        /// <summary>返回主城（清栈）</summary>
        public void GoHome()
        {
            _panelStack.Clear();
            if (_currentPanel != null)
            {
                Destroy(_currentPanel.gameObject);
                _currentPanel = null;
            }
            OnPanelChanged(null);
        }

        private void OnPanelChanged(System.Type panelType)
        {
            // 通知侧边栏激活项
            if (_currentSidebar != null)
            {
                var sidebar = _currentSidebar.GetComponent<SidebarPanel>();
                if (sidebar != null)
                    sidebar.SetActivePanel(panelType);
            }
        }
    }
}