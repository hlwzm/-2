using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Battle
{
    public class BuffIcon : MonoBehaviour
    {
        public static BuffIcon Create(RectTransform parent, string name, float duration)
        {
            var go = new GameObject("Buff_" + name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(36, 36);
            var icon = go.AddComponent<Image>();
            icon.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            var cd = new GameObject("CD", typeof(RectTransform));
            cd.transform.SetParent(go.transform, false);
            var cdRt = cd.GetComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero; cdRt.anchorMax = Vector2.one;
            cdRt.sizeDelta = Vector2.zero;
            var cdImg = cd.AddComponent<Image>();
            cdImg.color = new Color(0, 0, 0, 0.5f);
            cdImg.type = Image.Type.Filled; cdImg.fillAmount = 0;
            var buff = go.AddComponent<BuffIcon>();
            buff._cdImg = cdImg; buff._remain = duration; buff._total = duration;
            return buff;
        }

        private Image _cdImg; private float _remain, _total;
        void Update() { _remain -= Time.deltaTime; if (_cdImg != null) _cdImg.fillAmount = _remain / _total; if (_remain <= 0) Destroy(gameObject); }
    }
}
