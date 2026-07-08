using System.Collections.Generic;
using UnityEngine;
using Jx3.Core.Battle;

namespace Jx3.Core.Battle
{
    public class HeroUnit : MonoBehaviour
    {
        [Header("英雄数据")]
        public int heroId = 1001;
        public string heroName = "李忘生";
        public float maxHp = 3200;
        public float currentHp;
        public float maxMp = 500;
        public float currentMp;
        public int level = 1;

        [Header("战斗状态")]
        public int comboCount = 0;
        public float lastHitTime = 0;
        public List<BuffInstance> buffs = new();
        public bool isAlive => currentHp > 0;

        [Header("技能")]
        public SkillData[] skills = new SkillData[4];
        public float[] skillCooldowns;

        [Header("3D表现")]
        public GameObject modelRoot;
        public Material bodyMaterial;

        void Awake()
        {
            currentHp = maxHp;
            currentMp = maxMp;
            skillCooldowns = new float[4];
            CreateModel();
        }

        void Start()
        {
            // 加载技能
            var heroSkills = SkillConfig.GetHeroSkills(heroId);
            for (int i = 0; i < Mathf.Min(heroSkills.Count, 3); i++)
                skills[i] = heroSkills[i];
            var ultimate = heroSkills.Find(s => s.type == SkillType.终极);
            if (ultimate != null) skills[3] = ultimate;
        }

        void Update()
        {
            for (int i = 0; i < skillCooldowns.Length; i++)
                if (skillCooldowns[i] > 0) skillCooldowns[i] -= Time.deltaTime;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                buffs[i].remainingTime -= Time.deltaTime;
                if (buffs[i].remainingTime <= 0) buffs.RemoveAt(i);
            }

            if (modelRoot != null && Camera.main != null)
            {
                var dir = Camera.main.transform.forward;
                dir.y = 0;
                if (dir.magnitude > 0.01f)
                    modelRoot.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        public void TakeDamage(DamageResult dmg)
        {
            if (!isAlive) return;
            currentHp = Mathf.Max(0, currentHp - dmg.damage);
            var animCtrl = GetComponent<AnimationController>();
            if (animCtrl != null) animCtrl.PlayHit();
        }

        public void Heal(float amount)
        {
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }

        public void ConsumeMp(int cost)
        {
            currentMp = Mathf.Max(0, currentMp - cost);
        }

        public bool CanCastSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skills.Length) return false;
            var skill = skills[slotIndex];
            if (skill == null) return false;
            if (skillCooldowns[slotIndex] > 0) return false;
            if (currentMp < skill.cost) return false;
            return true;
        }

        public void CastSkill(int slotIndex)
        {
            if (!CanCastSkill(slotIndex)) return;
            var skill = skills[slotIndex];
            ConsumeMp(skill.cost);
            skillCooldowns[slotIndex] = skill.cooldown;
            var animCtrl = GetComponent<AnimationController>();
            if (animCtrl != null) animCtrl.PlaySkill();
            comboCount++;
            lastHitTime = Time.time;
        }

        public void ResetCombo()
        {
            comboCount = 0;
        }

        void CreateModel()
        {
            modelRoot = new GameObject("HeroModel");
            modelRoot.transform.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(modelRoot.transform, false);
            body.transform.localPosition = new Vector3(0, 1, 0);
            body.transform.localScale = new Vector3(0.6f, 1, 0.6f);

            bodyMaterial = body.GetComponent<MeshRenderer>().material;
            var animCtrl = GetComponent<AnimationController>();
            if (animCtrl != null) { animCtrl.bodyRoot = modelRoot.transform; }
            var quality = HeroConfig.Get(heroId)?.quality ?? 3;
            bodyMaterial.color = quality switch
            {
                5 => new Color(1f, 0.8f, 0.2f),
                4 => new Color(0.8f, 0.4f, 0.8f),
                3 => new Color(0.4f, 0.4f, 0.8f),
                _ => new Color(0.8f, 0.8f, 0.8f),
            };
            bodyMaterial.SetFloat("_Metallic", 0.5f);

            var nameGo = new GameObject("NameTag", typeof(RectTransform));
            nameGo.transform.SetParent(modelRoot.transform, false);
            nameGo.transform.localPosition = new Vector3(0, 2.2f, 0);
            var canvas = nameGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = nameGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(3, 0.5f);
            var text = nameGo.AddComponent<UnityEngine.UI.Text>();
            var hero = HeroConfig.Get(heroId);
            text.text = hero?.name ?? heroName;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        public void PlayHitEffect()
        {
            if (bodyMaterial != null)
            {
                bodyMaterial.color = Color.red;
                Invoke(nameof(ResetColor), 0.1f);
            }
        }

        void ResetColor()
        {
            if (bodyMaterial != null && HeroConfig.Get(heroId) != null)
            {
                var quality = HeroConfig.Get(heroId).quality;
                bodyMaterial.color = quality switch
                {
                    5 => new Color(1f, 0.8f, 0.2f),
                    4 => new Color(0.8f, 0.4f, 0.8f),
                    3 => new Color(0.4f, 0.4f, 0.8f),
                    _ => new Color(0.8f, 0.8f, 0.8f),
                };
            }
        }
    }
}