using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Jx3.Core.Scene;

namespace Jx3.Core
{
    /// <summary>
    /// 简易音频管理系统
    /// - BGM循环播放
    /// - 技能音效播放（程序生成音调 / 预留接口）
    /// - 音量控制（BGM/SFX独立）
    /// - 资源路径约定：Assets/Art/Audio/BGM/ 和 Assets/Art/Audio/SFX/
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("音量设置")]
        [Range(0f, 1f)] public float bgmVolume = 0.6f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        public bool bgmMute = false;
        public bool sfxMute = false;

        [Header("音频源")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;
        private int _sfxSourcePoolSize = 4;

        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private Dictionary<string, AudioClip> _cachedBGM = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> _cachedSFX = new Dictionary<string, AudioClip>();
        private string _currentBGM = "";

        // ===== 资源路径约定 =====
        public const string BGM_PATH = "Art/Audio/BGM/";
        public const string SFX_PATH = "Art/Audio/SFX/";

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 创建BGM音频源
            var bgmGo = new GameObject("BGM_Source");
            bgmGo.transform.SetParent(transform);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;

            // 创建SFX音频源池
            for (int i = 0; i < _sfxSourcePoolSize; i++)
            {
                var sfxGo = new GameObject("SFX_Source_" + i);
                sfxGo.transform.SetParent(transform);
                var src = sfxGo.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;
                src.volume = sfxVolume;
                _sfxPool.Add(src);
            }
            sfxSource = _sfxPool[0];
        }

        void Update()
        {
            // 实时同步音量
            if (bgmSource != null)
            {
                bgmSource.volume = bgmMute ? 0f : bgmVolume;
                bgmSource.mute = bgmMute;
            }
            foreach (var src in _sfxPool)
            {
                if (src != null)
                {
                    src.volume = sfxMute ? 0f : sfxVolume;
                    src.mute = sfxMute;
                }
            }
        }

        // ===== BGM控制 =====

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="bgmName">BGM文件名（不含路径）</param>
        /// <param name="fadeDuration">淡入时长</param>
        public void PlayBGM(string bgmName, float fadeDuration = 1.0f)
        {
            if (bgmName == _currentBGM && bgmSource.isPlaying) return;

            StartCoroutine(PlayBGMCoroutine(bgmName, fadeDuration));
        }

        IEnumerator PlayBGMCoroutine(string bgmName, float fadeDuration)
        {
            // 淡出当前BGM
            if (bgmSource.isPlaying)
            {
                float t = 0;
                float startVol = bgmSource.volume;
                while (t < fadeDuration * 0.5f)
                {
                    t += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, t / (fadeDuration * 0.5f));
                    yield return null;
                }
                bgmSource.Stop();
                bgmSource.volume = bgmMute ? 0f : bgmVolume;
            }

            // 加载并播放新BGM
            AudioClip clip = LoadBGM(bgmName);
            if (clip != null)
            {
                bgmSource.clip = clip;
                bgmSource.Play();
                _currentBGM = bgmName;

                // 淡入
                if (fadeDuration > 0)
                {
                    bgmSource.volume = 0f;
                    float t = 0;
                    float targetVol = bgmMute ? 0f : bgmVolume;
                    while (t < fadeDuration * 0.5f)
                    {
                        t += Time.deltaTime;
                        bgmSource.volume = Mathf.Lerp(0f, targetVol, t / (fadeDuration * 0.5f));
                        yield return null;
                    }
                    bgmSource.volume = targetVol;
                }
            }
            else
            {
                Debug.LogWarning($"[AudioManager] BGM not found: {bgmName} (expected at {BGM_PATH}{bgmName})");
                // 没有资源时播放程序生成音调
                StartCoroutine(GenerateBGMTone(bgmName));
            }
        }

        /// <summary>
        /// 停止BGM
        /// </summary>
        public void StopBGM(float fadeDuration = 0.5f)
        {
            StartCoroutine(StopBGMCoroutine(fadeDuration));
        }

        IEnumerator StopBGMCoroutine(float fadeDuration)
        {
            if (!bgmSource.isPlaying) yield break;
            float t = 0;
            float startVol = bgmSource.volume;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
                yield return null;
            }
            bgmSource.Stop();
            bgmSource.volume = bgmMute ? 0f : bgmVolume;
            _currentBGM = "";
        }

        /// <summary>
        /// 播放指定场景的BGM
        /// </summary>
        public void PlaySceneBGM(GameScene scene)
        {
            string bgm = scene switch
            {
                GameScene.Login => "login",
                GameScene.MainCity => "maincity",
                GameScene.Dungeon => "dungeon",
                GameScene.Battle => "battle",
                GameScene.HeroScreen => "hero",
                _ => "maincity"
            };
            PlayBGM(bgm);
        }

        // ===== SFX控制 =====

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="sfxName">音效文件名（不含路径）</param>
        /// <param name="volumeScale">音量倍率</param>
        public void PlaySFX(string sfxName, float volumeScale = 1.0f)
        {
            AudioClip clip = LoadSFX(sfxName);
            if (clip != null)
            {
                var src = GetAvailableSFXSource();
                if (src != null)
                {
                    src.volume = (sfxMute ? 0f : sfxVolume) * volumeScale;
                    src.PlayOneShot(clip);
                }
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SFX not found: {sfxName} (expected at {SFX_PATH}{sfxName})");
                // 没有资源时播放程序生成音调
                StartCoroutine(PlayGeneratedSFX(sfxName, volumeScale));
            }
        }

        /// <summary>
        /// 在指定位置播放3D音效（预留接口）
        /// </summary>
        public void PlaySFXAtPoint(string sfxName, Vector3 position, float volumeScale = 1.0f)
        {
            AudioClip clip = LoadSFX(sfxName);
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, (sfxMute ? 0f : sfxVolume) * volumeScale);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SFX not found (3D): {sfxName}");
            }
        }

        /// <summary>
        /// 播放技能音效（按技能类型推荐音效名）
        /// </summary>
        public void PlaySkillSFX(string skillName, Jx3.Core.SkillType skillType)
        {
            string sfx = skillType switch
            {
                Jx3.Core.SkillType.终极 => "ultimate",
                Jx3.Core.SkillType.被动 => "buff",
                _ => "skill_" + skillName
            };
            PlaySFX(sfx);
        }

        /// <summary>
        /// 播放战斗相关音效
        /// </summary>
        public void PlayCombatSFX(bool isCrit, bool isHeal)
        {
            if (isCrit)
                PlaySFX("crit", 1.2f);
            else if (isHeal)
                PlaySFX("heal", 0.8f);
            else
                PlaySFX("hit", 0.6f);
        }

        // ===== 音频资源加载 =====

        AudioClip LoadBGM(string name)
        {
            if (_cachedBGM.ContainsKey(name)) return _cachedBGM[name];
            var clip = Resources.Load<AudioClip>(BGM_PATH + name);
            if (clip != null) _cachedBGM[name] = clip;
            return clip;
        }

        AudioClip LoadSFX(string name)
        {
            if (_cachedSFX.ContainsKey(name)) return _cachedSFX[name];
            var clip = Resources.Load<AudioClip>(SFX_PATH + name);
            if (clip != null) _cachedSFX[name] = clip;
            return clip;
        }

        AudioSource GetAvailableSFXSource()
        {
            foreach (var src in _sfxPool)
            {
                if (!src.isPlaying) return src;
            }
            // 所有源都在播放时，复用第一个
            return _sfxPool[0];
        }

        // ===== 程序生成音调（无外部资源时的占位） =====

        /// <summary>
        /// 程序生成BGM音调占位
        /// </summary>
        IEnumerator GenerateBGMTone(string name)
        {
            var genGo = new GameObject("_GeneratedBGM");
            genGo.transform.SetParent(transform);
            var genSrc = genGo.AddComponent<AudioSource>();
            genSrc.loop = true;
            genSrc.volume = bgmMute ? 0f : bgmVolume * 0.3f;

            // 生成简单的循环音调
            int sampleRate = 44100;
            int duration = 4; // 4秒循环
            int sampleCount = sampleRate * duration;
            float[] samples = new float[sampleCount];

            float baseFreq = name switch
            {
                "battle" => 220f,
                "dungeon" => 180f,
                "login" => 260f,
                "maincity" => 240f,
                _ => 200f
            };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // 简单的和弦：基频 + 五度 + 八度
                float sample = Mathf.Sin(2 * Mathf.PI * baseFreq * t) * 0.4f
                             + Mathf.Sin(2 * Mathf.PI * baseFreq * 1.5f * t) * 0.3f
                             + Mathf.Sin(2 * Mathf.PI * baseFreq * 2f * t) * 0.2f;
                // 低频振荡产生节奏感
                float lfo = Mathf.Sin(2 * Mathf.PI * 0.5f * t) * 0.5f + 0.5f;
                samples[i] = sample * (0.3f + lfo * 0.2f);
            }

            var clip = AudioClip.Create("GenBGM_" + name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            genSrc.clip = clip;
            genSrc.Play();
            _currentBGM = name + "_gen";

            // 当真实BGM加载时自动切换
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// 程序生成简单音调SFX占位
        /// </summary>
        IEnumerator PlayGeneratedSFX(string name, float volumeScale)
        {
            var genGo = new GameObject("_GeneratedSFX");
            genGo.transform.SetParent(transform);
            var genSrc = genGo.AddComponent<AudioSource>();
            genSrc.volume = (sfxMute ? 0f : sfxVolume) * volumeScale * 0.5f;
            genSrc.loop = false;

            int sampleRate = 44100;
            float duration = name switch
            {
                "ultimate" => 1.5f,
                "crit" => 0.6f,
                "hit" => 0.15f,
                "heal" => 0.4f,
                "buff" => 0.3f,
                _ => 0.3f
            };
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            float freq = name switch
            {
                "ultimate" => 300f,
                "crit" => 800f,
                "hit" => 400f,
                "heal" => 600f,
                "buff" => 500f,
                _ => 440f
            };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float p = t / duration;
                // 衰减包络
                float envelope = Mathf.Exp(-p * 5f);
                // 频率调制（特殊效果）
                float fm = freq;
                if (name == "crit") fm = freq * (1f + Mathf.Sin(t * 30f) * 0.1f);
                if (name == "ultimate") fm = freq * (1f + p * 0.5f);

                float sample = Mathf.Sin(2 * Mathf.PI * fm * t) * envelope;
                // 谐波
                if (name == "ultimate" || name == "crit")
                    sample += Mathf.Sin(2 * Mathf.PI * fm * 2f * t) * envelope * 0.5f;

                samples[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            var clip = AudioClip.Create("GenSFX_" + name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            genSrc.PlayOneShot(clip);
            Destroy(genGo, duration + 0.5f);
            yield return null;
        }

        /// <summary>
        /// 清空音频缓存（场景切换时调用）
        /// </summary>
        public void ClearCache()
        {
            _cachedBGM.Clear();
            _cachedSFX.Clear();
        }
    }
}
