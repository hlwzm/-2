using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI
{
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; } = null!;

        public Canvas MainCanvas { get; private set; } = null!;
        public CanvasScaler Scaler { get; private set; } = null!;

        public RectTransform BgLayer { get; private set; } = null!;
        public RectTransform PanelLayer { get; private set; } = null!;
        public RectTransform PopupLayer { get; private set; } = null!;
        public RectTransform TopLayer { get; private set; } = null!;

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public static UIRoot Create()
        {
            var go = new GameObject("UIRoot", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);
            var root = go.AddComponent<UIRoot>();
            root.MainCanvas = go.GetComponent<Canvas>();
            root.MainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.Scaler = go.GetComponent<CanvasScaler>();
            root.Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.Scaler.referenceResolution = new Vector2(1920, 1080);
            root.Scaler.matchWidthOrHeight = 0.5f;

            root.BgLayer = root.CreateLayer("BgLayer", -10);
            root.PanelLayer = root.CreateLayer("PanelLayer", 0);
            root.PopupLayer = root.CreateLayer("PopupLayer", 10);
            root.TopLayer = root.CreateLayer("TopLayer", 20);

            Debug.Log("[UIRoot] Created, Instance=" + (Instance != null ? "OK" : "NULL"));
            return root;
        }

        private RectTransform CreateLayer(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        public GameObject ShowPanel(GameObject prefab, RectTransform layer)
        {
            var go = Instantiate(prefab, layer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return go;
        }
    }
}