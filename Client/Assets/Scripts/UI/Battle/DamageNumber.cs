using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Jx3.UI.Battle
{
    public class DamageNumber : MonoBehaviour
    {
        public static DamageNumber Spawn(Vector3 worldPos, int damage, bool isCrit, bool isHeal)
        {
            var go = new GameObject("DamageNumber", typeof(RectTransform));
            var dn = go.AddComponent<DamageNumber>();
            var canvas = UIRoot.Instance?.MainCanvas;
            if (canvas != null) go.transform.SetParent(canvas.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = isCrit ? 36 : (isHeal ? 28 : 24);
            text.color = isCrit ? Color.yellow : (isHeal ? Color.green : Color.red);
            text.text = (isHeal ? "+" : "-") + damage + (isCrit ? "!" : "");
            text.alignment = TextAnchor.MiddleCenter;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);
            rt.anchoredPosition = new Vector2(Random.Range(-50, 50), Random.Range(-20, 20));
            dn.StartCoroutine(dn.FadeOut());
            return dn;
        }

        private IEnumerator FadeOut()
        {
            var text = GetComponent<Text>();
            var rt = GetComponent<RectTransform>();
            float t = 1.0f;
            while (t > 0) {
                t -= Time.deltaTime * 1.5f;
                text.color = new Color(text.color.r, text.color.g, text.color.b, t);
                rt.anchoredPosition += new Vector2(0, Time.deltaTime * 80);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
