using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Jx3.Battle.Skill
{
    /// <summary>
    /// 技能视觉特效系统 - 使用Unity基本图形，无外部资源依赖
    /// </summary>
    public static class SkillEffect
    {
        /// <summary>
        /// 播放单体伤害特效：红色/橙色光柱从目标头顶落下
        /// </summary>
        public static void PlaySingleTarget(Vector3 targetPos, Color color, float duration = 0.8f)
        {
            var root = new GameObject("_Effect_SingleTarget");
            root.transform.position = targetPos;

            // 光柱：5个叠起来的小球
            for (int i = 0; i < 5; i++)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(root.transform);
                sphere.transform.localPosition = Vector3.up * (i * 0.25f + 0.5f);
                sphere.transform.localScale = Vector3.one * (0.3f - i * 0.04f);
                var mat = sphere.GetComponent<MeshRenderer>().material;
                mat.color = Color.Lerp(color, Color.white, 0.3f);
                mat.SetFloat("_Metallic", 0.3f);
                Object.Destroy(sphere.GetComponent<Collider>());
            }

            // 顶部大球（冲击光晕）
            var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.transform.SetParent(root.transform);
            top.transform.localPosition = Vector3.up * 2.0f;
            top.transform.localScale = Vector3.one * 0.6f;
            var topMat = top.GetComponent<MeshRenderer>().material;
            topMat.color = color;
            topMat.SetFloat("_Metallic", 0.8f);
            Object.Destroy(top.GetComponent<Collider>());

            // 底部扩散光环（多个扁平球组成圆环）
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                var ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ring.transform.SetParent(root.transform);
                ring.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.5f, 0.05f, Mathf.Sin(angle) * 0.5f);
                ring.transform.localScale = new Vector3(0.15f, 0.05f, 0.15f);
                var ringMat = ring.GetComponent<MeshRenderer>().material;
                ringMat.color = Color.Lerp(color, Color.white, 0.5f);
                ringMat.SetFloat("_Metallic", 0.5f);
                Object.Destroy(ring.GetComponent<Collider>());
            }

            var runner = root.AddComponent<EffectRunner>();
            runner.StartCoroutine(runner.AnimateSingleTarget(root, duration, color));
        }

        /// <summary>
        /// 播放群体伤害特效：圆形扩散波
        /// </summary>
        public static void PlayAoE(Vector3 center, Color color, float radius = 3f, float duration = 1.0f)
        {
            var root = new GameObject("_Effect_AoE");
            root.transform.position = center;

            // 内圈
            var innerRing = CreateRing(root.transform, radius * 0.3f, 6, color);
            // 中圈
            var midRing = CreateRing(root.transform, radius * 0.6f, 10, color);
            // 外圈
            var outerRing = CreateRing(root.transform, radius * 0.9f, 14, Color.Lerp(color, Color.white, 0.4f));

            // 中心光柱
            for (int i = 0; i < 3; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.transform.SetParent(root.transform);
                pillar.transform.localPosition = Vector3.up * (i * 0.3f);
                pillar.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                var pMat = pillar.GetComponent<MeshRenderer>().material;
                pMat.color = Color.Lerp(color, Color.white, 0.6f);
                Object.Destroy(pillar.GetComponent<Collider>());
            }

            var runner = root.AddComponent<EffectRunner>();
            runner.StartCoroutine(runner.AnimateAoE(root, innerRing, midRing, outerRing, radius, duration, color));
        }

        /// <summary>
        /// 播放扇形扩散波
        /// </summary>
        public static void PlayFanShape(Vector3 origin, Vector3 direction, Color color, float radius = 3f, float angle = 60f, float duration = 0.8f)
        {
            var root = new GameObject("_Effect_Fan");
            root.transform.position = origin;
            root.transform.rotation = Quaternion.LookRotation(direction);

            // 扇形：多层弧
            for (int layer = 0; layer < 4; layer++)
            {
                float r = radius * (layer + 1) / 4f;
                int count = Mathf.Max(3, layer * 2 + 3);
                for (int i = 0; i < count; i++)
                {
                    float a = -angle / 2f + angle * i / (count - 1);
                    float rad = a * Mathf.Deg2Rad;
                    var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    dot.transform.SetParent(root.transform);
                    dot.transform.localPosition = new Vector3(Mathf.Sin(rad) * r, 0.1f, Mathf.Cos(rad) * r);
                    dot.transform.localScale = Vector3.one * (0.15f - layer * 0.02f);
                    var dMat = dot.GetComponent<MeshRenderer>().material;
                    dMat.color = Color.Lerp(color, Color.white, layer * 0.15f);
                    Object.Destroy(dot.GetComponent<Collider>());
                }
            }

            var runner = root.AddComponent<EffectRunner>();
            runner.StartCoroutine(runner.AnimateFanShape(root, duration));
        }

        /// <summary>
        /// 播放治疗特效：绿色光点飘向目标
        /// </summary>
        public static void PlayHeal(Vector3 targetPos, float duration = 1.0f)
        {
            var root = new GameObject("_Effect_Heal");
            root.transform.position = targetPos;

            Color healColor = new Color(0.3f, 1f, 0.3f);
            // 光点从周围汇聚
            for (int i = 0; i < 10; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(1.5f, 2.5f);
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.transform.SetParent(root.transform);
                dot.transform.localPosition = new Vector3(Mathf.Cos(angle) * dist, Random.Range(0f, 1.5f), Mathf.Sin(angle) * dist);
                dot.transform.localScale = Vector3.one * 0.12f;
                var dMat = dot.GetComponent<MeshRenderer>().material;
                dMat.color = healColor;
                dMat.SetFloat("_Metallic", 0.3f);
                Object.Destroy(dot.GetComponent<Collider>());
            }

            // 中央十字光
            var crossH = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crossH.transform.SetParent(root.transform);
            crossH.transform.localPosition = Vector3.up * 1.5f;
            crossH.transform.localScale = new Vector3(0.6f, 0.05f, 0.1f);
            var cMatH = crossH.GetComponent<MeshRenderer>().material;
            cMatH.color = Color.white;
            Object.Destroy(crossH.GetComponent<Collider>());

            var crossV = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crossV.transform.SetParent(root.transform);
            crossV.transform.localPosition = Vector3.up * 1.5f;
            crossV.transform.localScale = new Vector3(0.1f, 0.05f, 0.6f);
            crossV.transform.Rotate(0, 0, 0);
            var cMatV = crossV.GetComponent<MeshRenderer>().material;
            cMatV.color = Color.white;
            Object.Destroy(crossV.GetComponent<Collider>());

            var runner = root.AddComponent<EffectRunner>();
            runner.StartCoroutine(runner.AnimateHeal(root, targetPos, duration));
        }

        /// <summary>
        /// 播放终极技能特效：全屏闪白 + 大范围光效
        /// </summary>
        public static void PlayUltimate(Vector3 center, Color color, float duration = 1.5f)
        {
            // 全屏闪白
            var flashGo = new GameObject("_Effect_UltimateFlash");
            var canvas = flashGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var flashImg = flashGo.AddComponent<UnityEngine.UI.Image>();
            flashImg.color = new Color(1f, 1f, 1f, 0f);
            var flashRt = flashGo.GetComponent<RectTransform>();
            flashRt.sizeDelta = new Vector2(Screen.width, Screen.height);
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;

            // 地面大范围光效
            var ground = new GameObject("_Effect_Ultimate");
            ground.transform.position = center;

            // 多层扩散环
            var rings = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                var ring = CreateRing(ground.transform, i * 1.5f + 1.0f, 12 + i * 4, Color.Lerp(color, Color.white, i * 0.2f));
                rings.Add(ring);
            }

            // 冲天光柱（多个堆叠圆柱）
            for (int i = 0; i < 8; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.transform.SetParent(ground.transform);
                pillar.transform.localPosition = Vector3.up * (i * 0.5f + 0.25f);
                pillar.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
                var pMat = pillar.GetComponent<MeshRenderer>().material;
                pMat.color = Color.Lerp(color, Color.white, 0.3f);
                pMat.SetFloat("_Metallic", 0.8f);
                Object.Destroy(pillar.GetComponent<Collider>());
            }

            // 旋转环绕小球
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.transform.SetParent(ground.transform);
                orb.transform.localPosition = new Vector3(Mathf.Cos(angle) * 3.0f, 0.5f, Mathf.Sin(angle) * 3.0f);
                orb.transform.localScale = Vector3.one * 0.15f;
                var oMat = orb.GetComponent<MeshRenderer>().material;
                oMat.color = Color.Lerp(color, Color.white, 0.5f);
                oMat.SetFloat("_Metallic", 0.9f);
                Object.Destroy(orb.GetComponent<Collider>());
            }

            var runner = ground.AddComponent<EffectRunner>();
            runner.StartCoroutine(runner.AnimateUltimate(ground, flashGo, rings, duration, color));
        }

        /// <summary>
        /// 播放Buff特效：目标周围环绕旋转光点
        /// </summary>
        public static void PlayBuff(Vector3 targetPos, Color color, float duration = 2.0f, bool isBuff = true)
        {
            var root = new GameObject(isBuff ? "_Effect_Buff" : "_Effect_Debuff");
            root.transform.position = targetPos;

            int count = isBuff ? 6 : 4;
            Color useColor = isBuff ? color : Color.Lerp(color, new Color(0.5f, 0f, 0f), 0.3f);

            var orbs = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.transform.SetParent(root.transform);
                orb.transform.localPosition = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                orb.transform.localScale = Vector3.one * 0.12f;
                var oMat = orb.GetComponent<MeshRenderer>().material;
                oMat.color = useColor;
                oMat.SetFloat("_Metallic", 0.5f);
                oMat.EnableKeyword("_EMISSION");
                oMat.SetColor("_EmissionColor", useColor * 0.5f);
                Object.Destroy(orb.GetComponent<Collider>());
                orbs.Add(orb);
            }

            var runner = root.AddComponent<EffectRunner>();
            runner.StartCoroutine(runner.AnimateBuff(root, orbs, duration));
        }

        // ===== 辅助方法 =====

        private static GameObject CreateRing(Transform parent, float radius, int count, Color color)
        {
            var ring = new GameObject("Ring_" + radius);
            ring.transform.SetParent(parent, false);
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.transform.SetParent(ring.transform, false);
                dot.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
                dot.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
                var dMat = dot.GetComponent<MeshRenderer>().material;
                dMat.color = color;
                dMat.SetFloat("_Metallic", 0.3f);
                Object.Destroy(dot.GetComponent<Collider>());
            }
            return ring;
        }
    }

    /// <summary>
    /// 特效动画协程运行器（附加到特效GameObject上）
    /// </summary>
    internal class EffectRunner : MonoBehaviour
    {
        public IEnumerator AnimateSingleTarget(GameObject root, float duration, Color color)
        {
            float t = 0;
            var spheres = root.GetComponentsInChildren<MeshRenderer>();
            Vector3 startPos = root.transform.position;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;

                // 整体上移 + 淡出
                root.transform.position = startPos + Vector3.up * (p * 1.5f);

                // 颜色逐渐变淡
                foreach (var r in spheres)
                {
                    if (r != null)
                        r.material.color = Color.Lerp(color, Color.clear, p);
                }

                yield return null;
            }
            Destroy(root);
        }

        public IEnumerator AnimateAoE(GameObject root, GameObject innerRing, GameObject midRing, GameObject outerRing, float radius, float duration, Color color)
        {
            float t = 0;
            var rings = new[] { innerRing, midRing, outerRing };
            var startScales = new[] { 0.3f, 0.6f, 0.9f };

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;

                for (int i = 0; i < rings.Length; i++)
                {
                    if (rings[i] == null) continue;
                    float scale = Mathf.Lerp(startScales[i], 1.0f, p);
                    rings[i].transform.localScale = Vector3.one * scale;
                    var renderers = rings[i].GetComponentsInChildren<MeshRenderer>();
                    foreach (var r in renderers)
                    {
                        if (r != null)
                            r.material.color = Color.Lerp(color, Color.clear, p * 1.2f);
                    }
                }

                yield return null;
            }
            Destroy(root);
        }

        public IEnumerator AnimateFanShape(GameObject root, float duration)
        {
            float t = 0;
            var renderers = root.GetComponentsInChildren<MeshRenderer>();

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;

                root.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, Mathf.Min(p * 2f, 1f));

                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        var c = r.material.color;
                        r.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, p * 1.5f));
                    }
                }

                yield return null;
            }
            Destroy(root);
        }

        public IEnumerator AnimateHeal(GameObject root, Vector3 target, float duration)
        {
            float t = 0;
            var children = root.GetComponentsInChildren<Transform>();

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;

                // 光点向中心汇聚
                foreach (var child in children)
                {
                    if (child == root.transform) continue;
                    var renderer = child.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;
                    // 向原点移动
                    child.localPosition = Vector3.Lerp(child.localPosition, Vector3.up * 1.5f, Time.deltaTime * 3f);
                    var c = renderer.material.color;
                    renderer.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, p * 1.2f));
                    child.localScale = Vector3.Lerp(child.localScale, Vector3.zero, Time.deltaTime * 3f);
                }

                yield return null;
            }
            Destroy(root);
        }

        public IEnumerator AnimateUltimate(GameObject ground, GameObject flash, List<GameObject> rings, float duration, Color color)
        {
            float t = 0;
            var flashImg = flash?.GetComponent<UnityEngine.UI.Image>();
            float halfDuration = duration * 0.5f;

            var groundRenderers = ground.GetComponentsInChildren<MeshRenderer>();

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;

                // 闪白动画：快速出现，缓慢消失
                if (flashImg != null)
                {
                    if (p < 0.15f)
                        flashImg.color = new Color(1, 1, 1, p / 0.15f);
                    else if (p < 0.5f)
                        flashImg.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, (p - 0.15f) / 0.35f));
                    else
                        flashImg.color = new Color(1, 1, 1, 0);
                }

                // 光环扩散
                for (int i = 0; i < rings.Count; i++)
                {
                    if (rings[i] == null) continue;
                    float ringP = Mathf.Clamp01((p - i * 0.1f) / 0.6f);
                    float scale = 1f + ringP * 1.5f;
                    rings[i].transform.localScale = Vector3.one * scale;
                    var ringRenderers = rings[i].GetComponentsInChildren<MeshRenderer>();
                    foreach (var r in ringRenderers)
                    {
                        if (r != null)
                            r.material.color = Color.Lerp(color, Color.clear, ringP * 0.8f + 0.2f);
                    }
                }

                // 地面光柱淡出
                foreach (var r in groundRenderers)
                {
                    if (r != null)
                    {
                        var c = r.material.color;
                        r.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, p * 1.2f));
                    }
                }

                yield return null;
            }

            if (flash != null) Destroy(flash);
            Destroy(ground);
        }

        public IEnumerator AnimateBuff(GameObject root, List<GameObject> orbs, float duration)
        {
            float t = 0;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;

                for (int i = 0; i < orbs.Count; i++)
                {
                    if (orbs[i] == null) continue;
                    float angle = (Time.time * 2f + i * (360f / orbs.Count)) * Mathf.Deg2Rad;
                    float bob = Mathf.Sin(Time.time * 3f + i) * 0.3f;
                    orbs[i].transform.localPosition = new Vector3(
                        Mathf.Cos(angle),
                        0.5f + bob,
                        Mathf.Sin(angle)
                    );
                    // 淡出
                    var renderer = orbs[i].GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        var c = renderer.material.color;
                        renderer.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, p * 1.5f));
                    }
                }

                yield return null;
            }
            Destroy(root);
        }
    }
}
