using UnityEngine;
using System.Collections.Generic;
using Jx3.Core.Battle;

namespace Jx3.Core.Battle
{
    /// <summary>
    /// 敌人/怪物/Boss战斗单位
    /// </summary>
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

        [Header("AI")]
        public float attackInterval = 2.5f;
        public float skillChance = 0.3f;
        public string[] skillNames;
        private float _lastAttackTime;
        private HeroUnit _target;

        [Header("3D表现")]
        public GameObject modelRoot;
        public Material bodyMaterial;

        void Awake()
        {
            currentHp = maxHp;
            CreateModel();
        }

        void Start()
        {
            _target = FindObjectOfType<HeroUnit>();
        }

        void Update()
        {
            if (!isAlive || _target == null || !_target.isAlive) return;

            // AI攻击循环
            if (Time.time - _lastAttackTime >= attackInterval)
            {
                _lastAttackTime = Time.time;
                PerformAttack();
            }
        }

        void PerformAttack()
        {
            if (_target == null) return;

            // 简化版普攻
            float baseDmg = attackPower * Random.Range(0.8f, 1.2f);
            float defReduce = _target.GetComponent<HeroUnit>()?.GetComponent<HeroUnit>() != null ? defense * 0.2f : 0;
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseDmg - defReduce));

            _target.TakeDamage(new DamageResult { damage = damage, skillName = "普通攻击" });
            _target.PlayHitEffect();
        }

        public void TakeDamage(DamageResult dmg)
        {
            if (!isAlive) return;
            currentHp = Mathf.Max(0, currentHp - dmg.damage);
            PlayHitEffect();

            if (!isAlive)
            {
                Die();
            }
        }

        void Die()
        {
            if (modelRoot != null)
            {
                // 倒地效果
                modelRoot.transform.Rotate(90, 0, 0);
                modelRoot.transform.position = new Vector3(
                    modelRoot.transform.position.x,
                    -0.5f,
                    modelRoot.transform.position.z
                );
            }
            Destroy(gameObject, 3f);
        }

        void CreateModel()
        {
            modelRoot = new GameObject("EnemyModel");
            modelRoot.transform.SetParent(transform, false);

            // Boss用Cube，小怪用Capsule
            if (isBoss)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(modelRoot.transform, false);
                body.transform.localPosition = new Vector3(0, 1.5f, 0);
                body.transform.localScale = new Vector3(1.5f, 2.5f, 1.5f);
                bodyMaterial = body.GetComponent<MeshRenderer>().material;
                bodyMaterial.color = new Color(0.6f, 0.15f, 0.15f);
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

            // 名称
            var nameGo = new GameObject("NameTag", typeof(RectTransform));
            nameGo.transform.SetParent(modelRoot.transform, false);
            nameGo.transform.localPosition = new Vector3(0, 2.8f, 0);
            var canvas = nameGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = nameGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(3, 0.5f);
            var text = nameGo.AddComponent<UnityEngine.UI.Text>();
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
                bodyMaterial.color = isBoss ? new Color(0.6f, 0.15f, 0.15f) : new Color(0.4f, 0.3f, 0.2f);
        }
    }
}