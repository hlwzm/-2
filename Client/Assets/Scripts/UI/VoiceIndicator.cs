using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Jx3.UI
{
    /// <summary>
    /// 语音状态指示器UI
    /// - 说话时动态圆圈波动动画
    /// - 可拖拽位置
    /// - 颜色: 说话中(绿) / 接收中(蓝) / 静音(灰)
    /// - 全程序化生成，暗黑紫色主题
    /// </summary>
    public class VoiceIndicator : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        // ===== 状态颜色 =====
        private static readonly Color ColorSpeaking = new Color(0.2f, 0.85f, 0.3f);   // 绿
        private static readonly Color ColorReceiving = new Color(0.2f, 0.5f, 0.9f);   // 蓝
        private static readonly Color ColorSilent = new Color(0.35f, 0.35f, 0.45f);   // 灰

        // ===== UI组件 =====
        private RectTransform _selfRt;
        private Image _iconImage;
        private Image _ringImage;
        private Image _waveImage;
        private RectTransform _ringRt;
        private RectTransform _waveRt;

        // ===== 动画 =====
        private float _animTime;
        private float _targetScale = 1f;
        private float _currentScale = 1f;
        private Color _targetColor = ColorSilent;
        private Color _currentColor = ColorSilent;

        // ===== 拖拽 =====
        private Vector2 _dragOffset;

        void Awake()
        {
            _selfRt = GetComponent<RectTransform>();
            BuildUI();
        }

        void Start()
        {
            // 初始位置: 右下角
            _selfRt.anchorMin = new Vector2(1f, 0f);
            _selfRt.anchorMax = new Vector2(1f, 0f);
            _selfRt.anchoredPosition = new Vector2(-80, 80);
            _selfRt.sizeDelta = new Vector2(56, 56);

            // 订阅语音事件
            if (Core.VoiceChatManager.Instance != null)
            {
                var vcm = Core.VoiceChatManager.Instance;
                vcm.OnVoiceStarted += OnVoiceStarted;
                vcm.OnVoiceStopped += OnVoiceStopped;
            }
        }

        void OnDestroy()
        {
            if (Core.VoiceChatManager.Instance != null)
            {
                var vcm = Core.VoiceChatManager.Instance;
                vcm.OnVoiceStarted -= OnVoiceStarted;
                vcm.OnVoiceStopped -= OnVoiceStopped;
            }
        }

        void Update()
        {
            _animTime += Time.deltaTime;

            // 平滑过渡
            _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * 8f);
            _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * 6f);

            // 更新环形
            if (_ringImage != null)
            {
                // 说话时环形旋转 + 呼吸缩放
                float ringScale = 0.8f + Mathf.Sin(_animTime * 4f) * 0.2f;
                float waveAlpha = 0f;

                if (_targetColor == ColorSpeaking)
                {
                    // 说话: 强波动
                    ringScale = 0.7f + Mathf.Sin(_animTime * 6f) * 0.3f;
                    waveAlpha = 0.3f + Mathf.Sin(_animTime * 3f) * 0.15f;
                }
                else if (_targetColor == ColorReceiving)
                {
                    // 接收中: 弱波动
                    ringScale = 0.85f + Mathf.Sin(_animTime * 2f) * 0.1f;
                    waveAlpha = 0.15f + Mathf.Sin(_animTime * 2f) * 0.08f;
                }

                _ringRt.localScale = Vector3.one * ringScale;
                _ringRt.Rotate(0, 0, Time.deltaTime * 30f);

                if (_waveImage != null)
                {
                    var wc = _waveImage.color;
                    wc.a = waveAlpha;
                    _waveImage.color = wc;
                    _waveRt.localScale = Vector3.one * (1f + Mathf.Sin(_animTime * 2f) * 0.15f);
                }
            }

            // 更新图标颜色
            _iconImage.color = _currentColor;
            _ringImage.color = _currentColor;

            // 更新自身缩放
            _selfRt.localScale = Vector3.one * _currentScale;
        }

        private void BuildUI()
        {
            // ===== 外层环形(波动圈) =====
            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(transform, false);
            _ringRt = ringGo.GetComponent<RectTransform>();
            _ringRt.anchorMin = Vector2.zero;
            _ringRt.anchorMax = Vector2.one;
            _ringRt.sizeDelta = Vector2.zero;
            _ringImage = ringGo.GetComponent<Image>();
            _ringImage.color = ColorSilent;
            _ringImage.raycastTarget = false;
            // 创建环形纹理(程序化圆形边框)
            _ringImage.sprite = CreateCircleSprite(64, 2, Color.white);

            // ===== 内层波动光圈 =====
            var waveGo = new GameObject("Wave", typeof(RectTransform), typeof(Image));
            waveGo.transform.SetParent(transform, false);
            _waveRt = waveGo.GetComponent<RectTransform>();
            _waveRt.anchorMin = Vector2.zero;
            _waveRt.anchorMax = Vector2.one;
            _waveRt.sizeDelta = new Vector2(-4, -4); // 比环形内缩
            _waveImage = waveGo.GetComponent<Image>();
            _waveImage.color = new Color(1, 1, 1, 0);
            _waveImage.raycastTarget = false;
            _waveImage.sprite = CreateCircleSprite(56, 0, Color.white);

            // ===== 中央麦克风图标(使用文字) =====
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.sizeDelta = Vector2.zero;
            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.color = ColorSilent;
            _iconImage.raycastTarget = false;

            // 创建麦克风纹理
            _iconImage.sprite = CreateMicSprite(32);
        }

        // ===== 事件 =====

        private void OnVoiceStarted()
        {
            _targetColor = ColorSpeaking;
            _targetScale = 1.15f;
        }

        private void OnVoiceStopped()
        {
            _targetColor = ColorReceiving;
            _targetScale = 1.0f;

            // 2秒后回到静音
            if (IsInvoking(nameof(ReturnToSilent)))
                CancelInvoke(nameof(ReturnToSilent));
            Invoke(nameof(ReturnToSilent), 2.0f);
        }

        /// <summary>设置接收中状态(外部调用)</summary>
        public void SetReceiving()
        {
            _targetColor = ColorReceiving;
            _targetScale = 1.0f;

            if (IsInvoking(nameof(ReturnToSilent)))
                CancelInvoke(nameof(ReturnToSilent));
            Invoke(nameof(ReturnToSilent), 1.5f);
        }

        private void ReturnToSilent()
        {
            _targetColor = ColorSilent;
            _targetScale = 0.9f;
        }

        // ===== 拖拽 =====

        public void OnBeginDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _selfRt.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out _dragOffset);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _selfRt.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);

            var parentRt = _selfRt.parent as RectTransform;
            if (parentRt == null) return;

            // 边界约束
            float halfW = _selfRt.sizeDelta.x * 0.5f;
            float halfH = _selfRt.sizeDelta.y * 0.5f;
            float parentW = parentRt.rect.width * 0.5f;
            float parentH = parentRt.rect.height * 0.5f;

            Vector2 newPos = localPoint - _dragOffset;
            newPos.x = Mathf.Clamp(newPos.x, -parentW + halfW, parentW - halfW);
            newPos.y = Mathf.Clamp(newPos.y, -parentH + halfH, parentH - halfH);

            _selfRt.anchoredPosition = newPos;
        }

        // ===== 程序化Sprite生成 =====

        private static Sprite CreateCircleSprite(int size, int borderWidth, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            int center = size / 2;
            int radius = size / 2 - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x - center;
                    int dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (borderWidth > 0)
                    {
                        // 环形
                        bool inRing = dist >= radius - borderWidth && dist <= radius;
                        tex.SetPixel(x, y, inRing ? color : Color.clear);
                    }
                    else
                    {
                        // 实心圆
                        tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateMicSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // 白色麦克风图标(简单像素绘制)
            int cx = size / 2;
            int cy = size / 2;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // 话筒主体(矩形)
            int micW = size / 5;
            int micH = size / 3;
            int micX1 = cx - micW / 2;
            int micX2 = cx + micW / 2;
            int micY1 = cy - micH / 2;
            int micY2 = cy + micH / 2;

            for (int y = micY1; y <= micY2; y++)
                for (int x = micX1; x <= micX2; x++)
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        tex.SetPixel(x, y, Color.white);

            // 话筒支架(底部弧线)
            for (int a = 0; a <= 180; a += 10)
            {
                float rad = a * Mathf.Deg2Rad;
                int sx = cx + Mathf.RoundToInt(Mathf.Cos(rad) * size / 5);
                int sy = cy + micH / 2 + Mathf.RoundToInt(Mathf.Sin(rad) * size / 6);
                if (sx >= 0 && sx < size && sy >= 0 && sy < size)
                    tex.SetPixel(sx, sy, Color.white);
            }

            // 顶部圆头
            int topY = cy - micH / 2 - 1;
            for (int a = 0; a <= 180; a += 15)
            {
                float rad = a * Mathf.Deg2Rad;
                int sx = cx + Mathf.RoundToInt(Mathf.Cos(rad) * micW / 2);
                int sy = topY + Mathf.RoundToInt(Mathf.Sin(rad) * micW / 4);
                if (sx >= 0 && sx < size && sy >= 0 && sy < size)
                    tex.SetPixel(sx, sy, Color.white);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
