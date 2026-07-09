using UnityEngine;
using System.Collections;

namespace Jx3.Effects
{
    public class LightingSystem : MonoBehaviour
    {
        [Header("Scene Glow Settings")]
        public Color glowColor = new Color(0.4f, 0.2f, 0.6f);
        public float glowIntensityMin = 0.4f;
        public float glowIntensityMax = 0.8f;
        public float glowSpeed = 0.5f;

        [Header("Skill Flash Settings")]
        public Color skillFlashColor = Color.white;
        public float skillFlashDuration = 0.2f;
        public float skillFlashIntensity = 1.5f;

        [Header("Damage Flash Settings")]
        public Color damageColor = new Color(1f, 0f, 0f);
        public float damageFlashDuration = 0.3f;
        public float damageFlashIntensity = 0.6f;

        private Light mainLight;
        private float baseIntensity;
        private Material fullscreenFlashMat;

        void Start()
        {
            mainLight = FindObjectOfType<Light>();
            if (mainLight != null)
                baseIntensity = mainLight.intensity;

            // Create a simple flash material
            Shader shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                fullscreenFlashMat = new Material(shader);
                fullscreenFlashMat.color = Color.clear;
            }

            StartCoroutine(SceneGlowRoutine());
        }

        IEnumerator SceneGlowRoutine()
        {
            while (true)
            {
                if (mainLight != null)
                {
                    float t = Mathf.PingPong(Time.time * glowSpeed, 1f);
                    float glow = Mathf.Lerp(glowIntensityMin, glowIntensityMax, t);
                    mainLight.intensity = baseIntensity * glow;
                    mainLight.color = Color.Lerp(Color.white, glowColor, t * 0.3f);
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void TriggerSkillFlash()
        {
            StartCoroutine(SkillFlashRoutine());
        }

        IEnumerator SkillFlashRoutine()
        {
            if (fullscreenFlashMat == null) yield break;
            float elapsed = 0f;
            while (elapsed < skillFlashDuration)
            {
                float t = elapsed / skillFlashDuration;
                float alpha = Mathf.Lerp(skillFlashIntensity, 0f, t);
                fullscreenFlashMat.color = new Color(
                    skillFlashColor.r * alpha,
                    skillFlashColor.g * alpha,
                    skillFlashColor.b * alpha,
                    Mathf.Clamp01(alpha * 0.3f)
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            fullscreenFlashMat.color = Color.clear;
        }

        public void TriggerDamageFlash()
        {
            StartCoroutine(DamageFlashRoutine());
        }

        IEnumerator DamageFlashRoutine()
        {
            if (fullscreenFlashMat == null) yield break;
            float elapsed = 0f;
            while (elapsed < damageFlashDuration)
            {
                float t = elapsed / damageFlashDuration;
                float alpha = Mathf.Lerp(damageFlashIntensity, 0f, t);
                fullscreenFlashMat.color = new Color(
                    damageColor.r * alpha,
                    damageColor.g * alpha,
                    damageColor.b * alpha,
                    Mathf.Clamp01(alpha * 0.5f)
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            fullscreenFlashMat.color = Color.clear;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (fullscreenFlashMat != null && fullscreenFlashMat.color.a > 0.01f)
            {
                Graphics.Blit(source, destination, fullscreenFlashMat);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        void OnDestroy()
        {
            if (fullscreenFlashMat != null)
                Destroy(fullscreenFlashMat);
        }
    }
}
