using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Jx3.Core.Battle;
using Jx3.Core;

namespace Jx3.Core.Battle
{
    public class EnemyUnit : MonoBehaviour
    {
        [Header("Boss数据")]
        public int bossId = 3001;
        public string bossName = "董龙";
        public float maxHp = 10000;
        public float currentHp;
        public int attackPower = 200;
        public int defense = 100;
        public bool isAlive => currentHp > 0;
        public bool isBoss => bossId >= 3000;
        public List<BuffInstance> buffs = new();

        [Header("AI")]
        public float attackInterval = 2.5f;
        public float skillChance = 0.3f;
        public string[] skillNames;
        private float _lastAttackTime;
        private HeroUnit _target;

        [Header("技能系统")]
        public string currentSkillName = "";
        public float currentSkillCastingTime = 0f;
        public float skillCastingDuration = 1.5f;
        public bool isCastingSkill = false;

        [Header("阶段变化")]
        public int phase = 1;
        public Color phase1Color = new Color(0.6f, 0.15f, 0.15f);
        public Color phase2Color = new Color(1f, 0.1f, 0.05f);
        public float phase2AttackMultiplier = 1.5f;
        public float phase2SkillChanceBonus = 0.2f;

        [Header("3D表现")]
        public GameObject modelRoot;
        public Material bodyMaterial;

        private Text _skillDisplayText;
        private GameObject _skillDisplayGo;
        private int _minionCount = 0;
        private List<GameObject> _summonedMinions = new();

        public void SetTarget(HeroUnit target) { _target = target; }

        void Awake()
        {
            currentHp = maxHp;
            phase = 1;
            CreateModel();
            CreateSkillDisplay();
        }

        void Start()
        {
            _target = FindObjectOfType<HeroUnit>();
            if (DungeonManager.Instance != null)
                DungeonManager.Instance.RegisterCurrentBoss(this);
        }

        void Update()
        {
            if (!isAlive || _target == null || !_target.isAlive) return;
            CheckPhaseTransition();
            if (isCastingSkill)
            {
                currentSkillCastingTime -= Time.deltaTime;
                if (currentSkillCastingTime <= 0f) FinishCastingSkill();
                return;
            }
            float effectiveInterval = attackInterval;
            if (phase == 2) effectiveInterval = attackInterval * 0.65f;
            if (Time.time - _lastAttackTime >= effectiveInterval)
            {
                _lastAttackTime = Time.time;
                float effectiveSkillChance = skillChance + (phase == 2 ? phase2SkillChanceBonus : 0f);
                if (skillNames != null && skillNames.Length > 0 && Random.value < effectiveSkillChance)
                    StartCastingSkill();
                else
                    PerformAttack();
            }
        }

        void CheckPhaseTransition()
        {
            if (phase == 1 && currentHp <= maxHp * 0.5f)
            {
                phase = 2;
                if (bodyMaterial != null)
                {
                    bodyMaterial.color = phase2Color;
                    bodyMaterial.SetFloat("_Metallic", 0.8f);
                }
                Debug.Log(string.Format("[EnemyUnit] {0} 进入第二阶段！", bossName));
                UpdatePhaseVisuals();
            }
        }

        void UpdatePhaseVisuals()
        {
            if (modelRoot != null)
            {
                modelRoot.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                var phaseLabel = new GameObject("PhaseLabel", typeof(RectTransform));
                phaseLabel.transform.SetParent(modelRoot.transform, false);
                phaseLabel.transform.localPosition = new Vector3(0, 3.5f, 0);
                var canvas = phaseLabel.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;
                var rect = phaseLabel.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(3, 0.4f);
                var text = phaseLabel.AddComponent<Text>();
                text.text = "⚡ 阶段2 ⚡";
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 16;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(1f, 0.3f, 0.1f);
            }
        }

        void StartCastingSkill()
        {
            if (skillNames == null || skillNames.Length == 0) return;
            string selectedSkill;
            if (phase == 2 && skillNames.Length >= 4)
            {
                int idx = Random.Range(2, skillNames.Length);
                selectedSkill = skillNames[idx];
            }
            else
            {
                selectedSkill = skillNames[Random.Range(0, Mathf.Min(skillNames.Length, 2))];
            }
            isCastingSkill = true;
            currentSkillName = selectedSkill;
            skillCastingDuration = phase == 2 ? 1.0f : 1.5f;
            currentSkillCastingTime = skillCastingDuration;
            if (_skillDisplayText != null) _skillDisplayText.text = string.Format("☀ {0} ☀", selectedSkill);
        }

        void FinishCastingSkill()
        {
            isCastingSkill = false;
            if (_skillDisplayText != null) _skillDisplayText.text = "";
            string skill = currentSkillName;
            currentSkillName = "";
        }

        void PerformAttack()
        {
            if (_target == null) return;
            float atkMult = phase == 2 ? phase2AttackMultiplier : 1f;
            int dmg = Mathf.Max(1, Mathf.RoundToInt((attackPower * atkMult - defense * 0.5f) * Random.Range(0.9f, 1.1f)));
            _target.TakeDamage(new DamageResult { damage = dmg, skillName = "普攻" });
            var animCtrl = GetComponent<AnimationController>();
            if (animCtrl != null) animCtrl.PlayAttack();
        }

        public void TakeDamage(DamageResult dmg)
        {
            if (!isAlive) return;
            currentHp = Mathf.Max(0, currentHp - dmg.damage);
            if (DungeonManager.Instance != null)
                DungeonManager.Instance.UpdateBossHp(bossId, currentHp);
            PlayHitEffect();
            if (!isAlive) Die();
        }

        void Die()
        {
            if (DungeonManager.Instance != null)
                DungeonManager.Instance.RegisterBossDeath(bossId);
            if (modelRoot != null)
            {
                modelRoot.transform.Rotate(90, 0, 0);
                modelRoot.transform.position = new Vector3(modelRoot.transform.position.x, -0.5f, modelRoot.transform.position.z);
            }
            foreach (var m in _summonedMinions)
                if (m != null) Destroy(m);
            _summonedMinions.Clear();
            Destroy(gameObject, 3f);
        }

        void CreateModel()
        {
            modelRoot = new GameObject("EnemyModel");
            modelRoot.transform.SetParent(transform, false);

            // 尝试加载预制体模型
            string prefabPath = string.Format("Art/Models/Monsters/Monster_{0}", bossId);
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject model = Object.Instantiate(prefab, modelRoot.transform);
                model.name = "ModelInstance";
                var renderers = model.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0)
                    bodyMaterial = renderers[0].material;
                return;
            }

            // Fallback: 原有组合体逻辑
            Debug.LogWarning(string.Format("[EnemyUnit] 未找到预制体 {0}，使用备用模型", prefabPath));
            if (isBoss)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(modelRoot.transform, false);
                body.transform.localPosition = new Vector3(0, 1.5f, 0);
                body.transform.localScale = new Vector3(1.5f, 2.5f, 1.5f);
                bodyMaterial = body.GetComponent<MeshRenderer>().material;
                bodyMaterial.color = phase1Color;
                bodyMaterial.SetFloat("_Metallic", 0.6f);
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(modelRoot.transform, false);
                body.transform.localPosition = new Vector3(0, 1, 0);
                body.transform.localScale = new Vector3(0.7f, 1, 0.7f);
                bodyMaterial = body.GetComponent<MeshRenderer>().material;
                bodyMaterial.color = new Color(0.4f, 0.3f, 0.2f);
            }

            var nameGo = new GameObject("NameTag", typeof(RectTransform));
            nameGo.transform.SetParent(modelRoot.transform, false);
            nameGo.transform.localPosition = new Vector3(0, 2.8f, 0);
            var canvas = nameGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = nameGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(3, 0.5f);
            var text = nameGo.AddComponent<Text>();
            text.text = bossName + (isBoss ? " ★Boss" : "");
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = isBoss ? new Color(1f, 0.5f, 0.2f) : Color.white;
        }

        void CreateSkillDisplay()
        {
            _skillDisplayGo = new GameObject("SkillDisplay", typeof(RectTransform));
            _skillDisplayGo.transform.SetParent(modelRoot != null ? modelRoot.transform : transform);
            _skillDisplayGo.transform.localPosition = new Vector3(0, 3.8f, 0);
            var canvas = _skillDisplayGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = _skillDisplayGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4, 0.5f);
            _skillDisplayText = _skillDisplayGo.AddComponent<Text>();
            _skillDisplayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _skillDisplayText.fontSize = 20;
            _skillDisplayText.alignment = TextAnchor.MiddleCenter;
            _skillDisplayText.color = new Color(1f, 0.6f, 0.1f);
            _skillDisplayText.text = "";
        }

        public void PlayHitEffect()
        {
            if (bodyMaterial != null)
            {
                bodyMaterial.color = Color.white;
                Invoke(nameof(ResetColor), 0.1f);
            }
        }

        void ResetColor()
        {
            if (bodyMaterial != null)
                bodyMaterial.color = phase == 2 ? phase2Color : (isBoss ? phase1Color : new Color(0.4f, 0.3f, 0.2f));
        }
    }

    public class MinionAI : MonoBehaviour
    {
        private HeroUnit _target;
        public int attackPower = 50;
        private float _lastAttack = 0f;
        private float _hp = 500f;

        public void SetTarget(HeroUnit target) { _target = target; }

        void Update()
        {
            if (_target == null || !_target.isAlive) return;
            if (_hp <= 0) { Destroy(gameObject); return; }
            transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, 2f * Time.deltaTime);
            if (Time.time - _lastAttack > 1.5f && Vector3.Distance(transform.position, _target.transform.position) < 2f)
            {
                _lastAttack = Time.time;
                int dmg = Mathf.Max(1, Mathf.RoundToInt(attackPower * Random.Range(0.8f, 1.2f)));
                _target.TakeDamage(new DamageResult { damage = dmg, skillName = "小怪攻击" });
            }
        }

        void OnMouseDown()
        {
            _hp -= 200;
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null) renderer.material.color = Color.white;
            Invoke(nameof(ResetMinionColor), 0.1f);
            if (_hp <= 0) Destroy(gameObject);
        }

        void ResetMinionColor()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null) renderer.material.color = new Color(0.3f, 0.1f, 0.1f);
        }
    }
}
