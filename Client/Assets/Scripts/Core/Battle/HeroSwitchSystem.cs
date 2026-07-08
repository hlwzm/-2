using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jx3.Core.Battle
{
    /// <summary>
    /// 4英雄编队 + 战斗中切换英雄系统
    /// 管理编队生成/切换冷却/后备恢复/死亡自动切换/全灭判定
    /// </summary>
    public class HeroSwitchSystem : MonoBehaviour
    {
        [System.Serializable]
        public class TeamSlot
        {
            public int heroId;
            public HeroUnit unit;
        }

        [Header("编队配置")]
        public List<int> teamIds = new() { 1001, 1002, 1004, 1006 };

        [SerializeField] private List<TeamSlot> _team = new();
        public IReadOnlyList<TeamSlot> Team => _team;

        public int currentIndex { get; private set; } = 0;
        public HeroUnit CurrentHero => _team.Count > 0 ? _team[currentIndex].unit : null;

        [Header("切换参数")]
        public float switchCooldown = 5f;
        public float SwitchCooldownRemaining { get; private set; }

        [Header("后备恢复")]
        public float backlineRegenInterval = 5f;
        public float backlineRegenPercent = 0.05f;
        private float _backlineRegenTimer;

        [Header("动画")]
        public float fadeDuration = 0.4f;

        public bool isSwitching { get; private set; } = false;

        // 事件
        public event System.Action<int, HeroUnit> OnHeroSwitched;
        public event System.Action<int> OnHeroDeath;     // 参数：死亡英雄index
        public event System.Action OnTeamWipe;

        private EnemyUnit _enemy;

        void Awake()
        {
            SpawnTeam();
        }

        void Start()
        {
            _enemy = FindObjectOfType<EnemyUnit>();
            _backlineRegenTimer = backlineRegenInterval;

            // 激活第一个英雄
            SetHeroActive(0, true);
        }

        void Update()
        {
            if (_team.Count == 0) return;

            // 切换冷却
            if (SwitchCooldownRemaining > 0)
                SwitchCooldownRemaining -= Time.deltaTime;

            // 后备恢复
            _backlineRegenTimer -= Time.deltaTime;
            if (_backlineRegenTimer <= 0)
            {
                _backlineRegenTimer = backlineRegenInterval;
                RegenBacklineHeroes();
            }

            // 键盘输入（仅当非切换中且有冷却完毕且有存活英雄）
            if (!isSwitching && SwitchCooldownRemaining <= 0)
            {
                var cur = _team[currentIndex].unit;
                if (cur != null && cur.isAlive)
                {
                    if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Tab))
                        SwitchToPrevious();
                    else if (Input.GetKeyDown(KeyCode.E))
                        SwitchToNext();
                }
            }

            // 监测当前英雄死亡（非切换期间）
            if (!isSwitching)
            {
                var cur = _team[currentIndex].unit;
                if (cur != null && !cur.isAlive)
                {
                    HandleDeath();
                }
            }
        }

        /// <summary>生成4英雄编队</summary>
        void SpawnTeam()
        {
            float spacing = 1.8f;
            for (int i = 0; i < teamIds.Count; i++)
            {
                int id = teamIds[i];
                var go = new GameObject($"Hero_{id}", typeof(HeroUnit));
                go.transform.SetParent(transform, false);
                // 占位位置（激活时再移到战斗位）
                go.transform.position = new Vector3(-4, 0, -spacing * i);

                var hero = go.GetComponent<HeroUnit>();
                var template = HeroConfig.Get(id);
                if (template != null)
                {
                    hero.heroId = template.id;
                    hero.heroName = template.name;
                    hero.maxHp = template.baseHp;
                    hero.currentHp = template.baseHp;
                    hero.level = 20;
                }

                // 后备英雄隐藏3D模型（但GameObject保持active，确保Update处理冷却）
                hero.modelRoot.SetActive(false);

                _team.Add(new TeamSlot { heroId = id, unit = hero });
            }

            Debug.Log($"[HeroSwitchSystem] Spawned {_team.Count} heroes: {string.Join(", ", teamIds)}");
        }

        /// <summary>直接设置某个英雄为活跃（无动画）</summary>
        void SetHeroActive(int index, bool immediate = false)
        {
            // 隐藏所有
            for (int i = 0; i < _team.Count; i++)
            {
                if (_team[i].unit != null)
                    _team[i].unit.modelRoot.SetActive(false);
            }

            // 激活目标
            var slot = _team[index];
            slot.unit.gameObject.SetActive(true);
            slot.unit.modelRoot.SetActive(true);
            slot.unit.transform.position = new Vector3(-4, 0, 0);

            currentIndex = index;
            SwitchCooldownRemaining = switchCooldown;

            // 更新敌人目标
            if (_enemy != null)
                _enemy.SetTarget(slot.unit);

            OnHeroSwitched?.Invoke(index, slot.unit);
        }

        /// <summary>切换到指定索引英雄</summary>
        public void SwitchTo(int index)
        {
            if (isSwitching || SwitchCooldownRemaining > 0) return;
            if (index < 0 || index >= _team.Count || index == currentIndex) return;
            if (_team[index].unit == null || !_team[index].unit.isAlive) return;

            StartCoroutine(SwitchRoutine(index));
        }

        /// <summary>切换到下一个存活英雄（E键）</summary>
        public void SwitchToNext()
        {
            for (int i = 1; i <= _team.Count; i++)
            {
                int idx = (currentIndex + i) % _team.Count;
                if (_team[idx].unit != null && _team[idx].unit.isAlive)
                {
                    SwitchTo(idx);
                    return;
                }
            }
        }

        /// <summary>切换到上一个存活英雄（Q键/Tab）</summary>
        public void SwitchToPrevious()
        {
            for (int i = 1; i <= _team.Count; i++)
            {
                int idx = (currentIndex - i + _team.Count) % _team.Count;
                if (_team[idx].unit != null && _team[idx].unit.isAlive)
                {
                    SwitchTo(idx);
                    return;
                }
            }
        }

        /// <summary>切换协程：淡出→隐藏旧→显示新→淡入</summary>
        IEnumerator SwitchRoutine(int targetIndex)
        {
            isSwitching = true;

            var prev = _team[currentIndex].unit;
            var next = _team[targetIndex].unit;

            // --- 淡出当前英雄 ---
            if (prev != null && prev.bodyMaterial != null)
            {
                SetMaterialTransparent(prev.bodyMaterial);
                float t = 0;
                var c = prev.bodyMaterial.color;
                while (t < fadeDuration * 0.5f)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Lerp(1, 0, t / (fadeDuration * 0.5f));
                    prev.bodyMaterial.color = new Color(c.r, c.g, c.b, a);
                    yield return null;
                }
                prev.bodyMaterial.color = new Color(c.r, c.g, c.b, 0);
            }

            // 隐藏旧英雄模型
            if (prev != null)
                prev.modelRoot.SetActive(false);

            // --- 显示新英雄 ---
            next.gameObject.SetActive(true);
            next.modelRoot.SetActive(true);
            next.transform.position = new Vector3(-4, 0, 0);

            // 淡入新英雄
            if (next.bodyMaterial != null)
            {
                SetMaterialTransparent(next.bodyMaterial);
                var template = HeroConfig.Get(_team[targetIndex].heroId);
                Color targetColor = GetHeroQualityColor(template?.quality ?? 3);
                next.bodyMaterial.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0);

                float t = 0;
                while (t < fadeDuration * 0.5f)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Lerp(0, 1, t / (fadeDuration * 0.5f));
                    next.bodyMaterial.color = new Color(targetColor.r, targetColor.g, targetColor.b, a);
                    yield return null;
                }
                next.bodyMaterial.color = targetColor;
            }

            // 更新状态
            currentIndex = targetIndex;
            SwitchCooldownRemaining = switchCooldown;

            // 更新敌人目标（继承仇恨）
            if (_enemy != null)
                _enemy.SetTarget(next);

            // 恢复材质为Opaque模式
            if (next.bodyMaterial != null)
                SetMaterialOpaque(next.bodyMaterial);

            OnHeroSwitched?.Invoke(targetIndex, next);
            isSwitching = false;

            Debug.Log($"[HeroSwitchSystem] Switched to {next.heroName} (index {targetIndex})");
        }

        /// <summary>后备英雄缓慢恢复HP</summary>
        void RegenBacklineHeroes()
        {
            for (int i = 0; i < _team.Count; i++)
            {
                if (i == currentIndex) continue;
                var h = _team[i].unit;
                if (h != null && h.isAlive)
                {
                    float healAmount = h.maxHp * backlineRegenPercent;
                    h.Heal(healAmount);
                }
            }
        }

        /// <summary>处理当前英雄死亡</summary>
        void HandleDeath()
        {
            int deadIndex = currentIndex;
            OnHeroDeath?.Invoke(deadIndex);

            // 找下一个存活英雄（不包含已死亡的当前英雄）
            for (int i = 1; i <= _team.Count; i++)
            {
                int idx = (currentIndex + i) % _team.Count;
                if (_team[idx].unit != null && _team[idx].unit.isAlive)
                {
                    StartCoroutine(SwitchRoutine(idx));
                    return;
                }
            }

            // 无存活英雄 → 全灭
            Debug.Log("[HeroSwitchSystem] 全队阵亡！");
            OnTeamWipe?.Invoke();
        }

        // ===== 工具方法 =====

        /// <summary>设置材质为透明渲染模式</summary>
        static void SetMaterialTransparent(Material mat)
        {
            mat.SetFloat("_Mode", 2);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        /// <summary>恢复材质为不透明渲染模式</summary>
        static void SetMaterialOpaque(Material mat)
        {
            mat.SetFloat("_Mode", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 2000;
        }

        /// <summary>根据品质获取英雄颜色</summary>
        public static Color GetHeroQualityColor(int quality)
        {
            return quality switch
            {
                5 => new Color(1f, 0.8f, 0.2f),
                4 => new Color(0.8f, 0.4f, 0.8f),
                3 => new Color(0.4f, 0.4f, 0.8f),
                _ => new Color(0.8f, 0.8f, 0.8f),
            };
        }
    }
}