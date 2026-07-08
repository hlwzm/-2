#nullable disable
using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Battle
{
    /// <summary>
    /// Buff图标组件 - 增益/减益图标
    /// 显示名称、剩余时间、堆叠计数
    /// 增益=绿色半透明背景，减益=红色半透明背景
    /// 全程序化生成
    /// </summary>
    public class BuffIcon : MonoBehaviour
    {
        // ===== 组件引用 =====
        private Image _bgImage;
        private Text _nameText;
        private Text _timeText;
        private Text _stackText;
        private Image _cdOverlay;

        // ===== 运行时数据 =====
        private float _remainTime;
        private float _totalTime;
        private int _stackCount = 1;
        private bool _isBuff = true;
        private bool _paused;

        // ===== 颜色 =====
        private static readonly Color ColorBuffBg = new Color(0.1f, 0.7f, 0.2f, 0.3f);
        private static readonly Color ColorBuffBorder = new Color(0.2f, 0.9f, 0.3f, 0.6f);
        private static readonly Color ColorDebuffBg = new Color(0.8f, 0.1f, 0.1f, 0.3f);
        private static readonly Color ColorDebuffBorder = new Color(1f, 0.2f, 0.2f, 0.6f);
        private static readonly Color ColorCdOverlay = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color ColorTextLight = new Color(0.85f, 0.85f, 0.9f);
        private static readonly Color ColorStackBg = new Color(0f, 0f, 0f, 0.5f);

        /// <summary>
        /// 创建Buff图标（工厂方法）
        /// </summary>
        /// <param name="parent">父级RectTransform</param>
        /// <param name="buffName">Buff名称</param>
        /// <param name="duration">持续时间（秒），<=0表示永久</param>
        /// <param name="isBuff">true=增益，false=减益</param>
        /// <param name="stackCount">堆叠层数</param>
        /// <returns>BuffIcon组件</returns>
        public static BuffIcon Create(RectTransform parent, string buffName, float duration, bool isBuff = true, int stackCount = 1)
        {
            var go = new GameObject("Buff_" + buffName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(52, 52);

            var icon = go.AddComponent<BuffIcon>();
            icon._remainTime = duration;
            icon._totalTime = duration > 0 ? duration : 1f;
            icon._isBuff = isBuff;
            icon._stackCount = Mathf.Max(1, stackCount);
            icon.BuildUI();
            return icon;
        }

        /// <summary>
        /// 设置堆叠层数
        /// </summary>
        public void SetStack(int count)
        {
            _stackCount = Mathf.Max(1, count);
            if (_stackText != null)
            {
                _stackText.text = _stackCount > 1 ? _stackCount.ToString() : "";
                _stackText.gameObject.SetActive(_stackCount > 1);
            }
        }

        /// <summary>
        /// 暂停/继续倒计时
        /// </summary>
        public void SetPaused(bool paused)
        {
            _paused = paused;
        }

        /// <summary>
        /// 刷新持续时间
        /// </summary>
        public void RefreshDuration(float duration)
        {
            _remainTime = duration;
            _totalTime = duration > 0 ? duration : 1f;
        }

        // =====================================================================
        // UI构建
        // =====================================================================
        private void BuildUI()
        {
            var rt = (RectTransform)transform;

            // 背景
            _bgImage = CreateImage(rt, "Bg", _isBuff ? ColorBuffBg : ColorDebuffBg, Vector2.zero, Vector2.one, Vector2.zero);

            // 边框
            var border = CreateImage(rt, "Border", _isBuff ? ColorBuffBorder : ColorDebuffBorder, Vector2.zero, Vector2.one, Vector2.zero);
            border.type = Image.Type.Sliced;

            // CD遮罩（黑色半透明，从上往下填充）
            _cdOverlay = CreateImage(rt, "CdOverlay", ColorCdOverlay, Vector2.zero, Vector2.one, Vector2.zero);
            _cdOverlay.type = Image.Type.Filled;
            _cdOverlay.fillMethod = Image.FillMethod.Vertical;
            _cdOverlay.fillOrigin = 1; // top
            _cdOverlay.fillAmount = _totalTime > 0 ? 0f : 0f;

            // Buff名称（左上角）
            _nameText = CreateText(rt, "Name", "", 14, TextAnchor.UpperLeft, ColorTextLight);
            var nameRt = (RectTransform)_nameText.transform;
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(0, 1);
            nameRt.sizeDelta = new Vector2(48, 20);
            nameRt.anchoredPosition = new Vector2(3, -3);
            _nameText.text = GetShortName();
            _nameText.resizeTextForBestFit = true;
            _nameText.resizeTextMinSize = 8;
            _nameText.resizeTextMaxSize = 14;

            // 剩余时间（底部居中）
            _timeText = CreateText(rt, "Time", FormatTime(_remainTime), 16, TextAnchor.LowerCenter, Color.white);
            var timeRt = (RectTransform)_timeText.transform;
            timeRt.anchorMin = new Vector2(0.5f, 0);
            timeRt.anchorMax = new Vector2(0.5f, 0);
            timeRt.sizeDelta = new Vector2(48, 20);
            timeRt.anchoredPosition = new Vector2(0, 2);
            _timeText.fontStyle = FontStyle.Bold;

            // 堆叠计数（右下角）
            var stackBg = CreateImage(rt, "StackBg", ColorStackBg, new Vector2(1, 0), new Vector2(1, 0), new Vector2(18, 18));
            stackBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(-2, 2);

            _stackText = CreateText(rt, "StackText", _stackCount > 1 ? _stackCount.ToString() : "", 13, TextAnchor.MiddleCenter, Color.white);
            var stackRt = (RectTransform)_stackText.transform;
            stackRt.anchorMin = new Vector2(1, 0);
            stackRt.anchorMax = new Vector2(1, 0);
            stackRt.sizeDelta = new Vector2(18, 18);
            stackRt.anchoredPosition = new Vector2(-2, 2);
            _stackText.fontStyle = FontStyle.Bold;
            _stackText.gameObject.SetActive(_stackCount > 1);

            // 如果是永久Buff，隐藏时间
            if (_remainTime <= 0)
            {
                _timeText.gameObject.SetActive(false);
                _cdOverlay.gameObject.SetActive(false);
            }
        }

        // =====================================================================
        // 运行时更新
        // =====================================================================
        void Update()
        {
            if (_remainTime <= 0) return; // 永久Buff

            if (!_paused)
            {
                _remainTime -= Time.deltaTime;
                if (_remainTime < 0) _remainTime = 0;
            }

            // 更新时间显示
            if (_timeText != null && _timeText.gameObject.activeSelf)
            {
                _timeText.text = FormatTime(_remainTime);

                // 最后5秒变红闪烁
                if (_remainTime <= 5f && _remainTime > 0)
                {
                    float blink = Mathf.PingPong(Time.time * 6f, 1f);
                    _timeText.color = new Color(1f, 0.2f, 0.1f, blink);
                }
                else
                {
                    _timeText.color = Color.white;
                }
            }

            // 更新CD遮罩
            if (_cdOverlay != null && _totalTime > 0)
            {
                _cdOverlay.fillAmount = 1f - Mathf.Clamp01(_remainTime / _totalTime);
            }

            // 时间到自动销毁
            if (_remainTime <= 0 && !_paused)
            {
                Destroy(gameObject);
            }
        }

        // =====================================================================
        // 辅助方法
        // =====================================================================
        private string GetShortName()
        {
            // 取简短的显示名（最多4个字符）
            var name = gameObject.name.Replace("Buff_", "");
            if (name.Length > 4)
                return name.Substring(0, 4);
            return name;
        }

        private static string FormatTime(float time)
        {
            if (time <= 0) return "";
            if (time >= 60f)
            {
                int m = Mathf.FloorToInt(time / 60);
                int s = Mathf.FloorToInt(time % 60);
                return string.Format("{0:D}m{1:D2}s", m, s);
            }
            if (time >= 10f)
                return Mathf.CeilToInt(time) + "s";
            // <10秒显示一位小数
            return time.ToString("F1") + "s";
        }

        private static Image CreateImage(RectTransform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text CreateText(RectTransform parent, string name, string text,
            int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            return txt;
        }
    }
}