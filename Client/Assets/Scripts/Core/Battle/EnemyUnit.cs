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
                Debug.Log($"[EnemyUnit] {bossName} 进入第二阶段！攻击频率提升，技能变化！");
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
                selectedSkill = skillNames[Random.Range(0, Mathf.Min(skillNames.Length, 4))];
            }
            currentSkillName = selectedSkill;
            skillCastingDuration = 1.2f + Random.Range(0f, 0.8f);
            currentSkillCastingTime = skillCastingDuration;
            isCastingSkill = true;
            UpdateSkillDisplay();
            Debug.Log($"[EnemyUnit] {bossName} 正在施放技能: {currentSkillName} (施法{skillCastingDuration:F1}秒)");
        }

        void FinishCastingSkill()
        {
            isCastingSkill = false;
            string skillName = currentSkillName;
            currentSkillName = "";
            if (_skillDisplayText != null) _skillDisplayText.text = "";
            ExecuteSkill(skillName);
        }

        void ExecuteSkill(string skillName)
        {
            if (_target == null) return;
            float phaseMultiplier = phase == 2 ? 1.3f : 1.0f;

            if (skillName.Contains("狂暴") || skillName.Contains("震天"))
            {
                float aoeDmg = attackPower * 2.5f * phaseMultiplier;
                float defReduce = defense * 0.15f;
                int damage = Mathf.Max(1, Mathf.RoundToInt(aoeDmg - defReduce));
                _target.TakeDamage(new DamageResult { damage = damage, skillName = skillName });
                Debug.Log($"[EnemyUnit] {bossName} 释放范围技能 [{skillName}]，对 {_target.heroName} 造成 {damage} 点范围伤害！");
            }
            else if (skillName.Contains("霸王") || skillName.Contains("暗黑之握") || skillName.Contains("蛇噬"))
            {
                float stunDmg = attackPower * 2.0f * phaseMultiplier;
                int damage = Mathf.Max(1, Mathf.RoundToInt(stunDmg - defense * 0.2f));
                _target.TakeDamage(new DamageResult { damage = damage, skillName = skillName });
                Debug.Log($"[EnemyUnit] {bossName} 释放眩晕技能 [{skillName}]，对 {_target.heroName} 造成 {damage} 点伤害并眩晕2秒！");
            }
            else if (skillName.Contains("式神") || skillName.Contains("影分身") || skillName.Contains("召唤"))
            {
                SummonMinions(2);
                Debug.Log($"[EnemyUnit] {bossName} 释放召唤技能 [{skillName}]，召唤2个小怪！");
            }
            else if (skillName.Contains("毒") || skillName.Contains("诅咒") || skillName.Contains("血之"))
            {
                float dotDmg = attackPower * 1.8f * phaseMultiplier;
                int damage = Mathf.Max(1, Mathf.RoundToInt(dotDmg - defense * 0.1f));
                _target.TakeDamage(new DamageResult { damage = damage, skillName = skillName });
                Debug.Log($"[EnemyUnit] {bossName} 释放持续伤害技能 [{skillName}]，{_target.heroName} 受到 {damage} 点伤害并附加中毒效果！");
            }
            else if (skillName.Contains("治愈") || skillName.Contains("恢复"))
            {
                float healAmount = maxHp * 0.08f;
                currentHp = Mathf.Min(maxHp, currentHp + healAmount);
                Debug.Log($"[EnemyUnit] {bossName} 释放治疗技能 [{skillName}]，恢复 {healAmount:F0} 点生命值！");
            }
            else
            {
                float dmg = attackPower * 2.0f * phaseMultiplier;
                int damage = Mathf.Max(1, Mathf.RoundToInt(dmg - defense * 0.2f));
                _target.TakeDamage(new DamageResult { damage = damage, skillName = skillName });
                Debug.Log($"[EnemyUnit] {bossName} 释放技能 [{skillName}]，对 {_target.heroName} 造成 {damage} 点伤害！");
            }
            _target.PlayHitEffect();
        }

        void SummonMinions(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _minionCount++;
                var minionGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                minionGo.name = $"Minion_{_minionCount}";
                minionGo.transform.position = transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                var renderer = minionGo.GetComponent<MeshRenderer>();
                renderer.material.color = new Color(0.3f, 0.1f, 0.1f);
                renderer.material.SetFloat("_Metallic", 0.3f);
                minionGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                _summonedMinions.Add(minionGo);
                var minionAi = minionGo.AddComponent<MinionAI>();
                minionAi.SetTarget(_target);
                minionAi.attackPower = Mathf.RoundToInt(attackPower * 0.4f);
            }
        }

        void CreateSkillDisplay()
        {
            _skillDisplayGo = new GameObject("SkillNameTag", typeof(RectTransform));
            _skillDisplayGo.transform.SetParent(modelRoot != null ? modelRoot.transform : transform, false);
            _skillDisplayGo.transform.localPosition = new Vector3(0, 3.8f, 0);
            var canvas = _skillDisplayGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = _skillDisplayGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4, 0.6f);
            _skillDisplayText = _skillDisplayGo.AddComponent<Text>();
            _skillDisplayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _skillDisplayText.fontSize = 18;
            _skillDisplayText.alignment = TextAnchor.MiddleCenter;
            _skillDisplayText.color = new Color(1f, 0.8f, 0.1f);
            _skillDisplayText.text = "";
        }

        void UpdateSkillDisplay()
        {
            if (_skillDisplayText != null && isCastingSkill)
            {
                _skillDisplayText.text = $"⚡ {currentSkillName} ⚡";
                _skillDisplayText.color = phase == 2 ? new Color(1f, 0.2f, 0.1f) : new Color(1f, 0.8f, 0.1f);
            }
        }

        void PerformAttack()
        {
            if (_target == null) return;
            float phaseAtkBonus = phase == 2 ? 1.2f : 1.0f;
            float baseDmg = attackPower * phaseAtkBonus * Random.Range(0.8f, 1.2f);
            float defReduce = defense * 0.2f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseDmg - defReduce));
            _target.TakeDamage(new DamageResult { damage = damage, skillName = "普通攻击" });
            _target.PlayHitEffect();
        }

        public void SetTarget(HeroUnit target) { _target = target; }

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
