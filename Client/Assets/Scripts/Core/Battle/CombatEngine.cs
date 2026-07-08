using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jx3.Core.Battle
{
    /// <summary>
    /// 战斗引擎 - 伤害计算/会心/连击/Buff
    /// </summary>
    public static class CombatEngine
    {
        // 会心配置
        public static float critRate = 0.15f;      // 基础会心率 15%
        public static float critDamage = 1.8f;      // 会心伤害倍率
        public static float comboWindow = 2.0f;     // 连击判定窗口(秒)
        public static float skillCostRate = 0.1f;   // 技能消耗系数

        /// <summary>
        /// 计算一次技能伤害
        /// </summary>
        public static DamageResult CalculateDamage(int attackerId, int targetId, SkillData skill, bool isCrit = false)
        {
            var result = new DamageResult();
            result.skillId = skill.id;
            result.skillName = skill.name;

            // 获取属性（简化版）
            float atk = GetAttack(attackerId);
            float def = GetDefense(targetId);
            float multiplier = skill.damageMultiplier;

            // 基础伤害
            float baseDmg = Mathf.Max(1, atk * multiplier - def * 0.3f);
            
            // 伤害浮动 ±10%
            float variance = Random.Range(0.9f, 1.1f);
            baseDmg *= variance;

            // 会心判定
            bool crit = isCrit || Random.value < critRate;
            if (crit)
            {
                baseDmg *= critDamage;
                result.isCrit = true;
            }

            result.damage = Mathf.RoundToInt(baseDmg);
            result.isHeal = skill.damageMultiplier < 0;  // 负倍率为治疗
            if (result.isHeal) result.damage = Mathf.Abs(result.damage);

            return result;
        }

        /// <summary>
        /// 简化版属性获取
        /// </summary>
        static float GetAttack(int unitId)
        {
            if (unitId < 2000)  // 玩家英雄
            {
                var hero = HeroConfig.Get(unitId);
                return hero?.baseAttack ?? 200;
            }
            if (unitId < 5000)  // Boss
            {
                var boss = DungeonConfig.GetBoss(unitId);
                return boss?.attack ?? 300;
            }
            return 150;  // 小怪
        }

        static float GetDefense(int unitId)
        {
            if (unitId < 2000)
            {
                var hero = HeroConfig.Get(unitId);
                return hero?.baseDefense ?? 100;
            }
            if (unitId < 5000)
            {
                var boss = DungeonConfig.GetBoss(unitId);
                return boss?.defense ?? 150;
            }
            return 80;
        }

        /// <summary>
        /// 计算连击加成
        /// </summary>
        public static float GetComboMultiplier(int comboCount)
        {
            if (comboCount <= 0) return 1.0f;
            return 1.0f + Mathf.Min(comboCount * 0.05f, 0.5f);  // 每连击+5%, 上限50%
        }

        /// <summary>
        /// 计算技能消耗
        /// </summary>
        public static int GetSkillCost(SkillData skill, int level = 1)
        {
            return Mathf.RoundToInt(skill.cost * (1 + (level - 1) * skillCostRate));
        }
    }

    public struct DamageResult
    {
        public int skillId;
        public string skillName;
        public int damage;
        public bool isCrit;
        public bool isHeal;
    }

    /// <summary>
    /// Buff效果
    /// </summary>
    [System.Serializable]
    public class BuffInstance
    {
        public string name;
        public float remainingTime;
        public float totalDuration;
        public float value;         // 治疗量/伤害量/属性加成
        public BuffType type;
        public int stackCount = 1;

        public BuffInstance(string name, float duration, float value, BuffType type)
        {
            this.name = name; this.remainingTime = duration;
            this.totalDuration = duration; this.value = value; this.type = type;
        }
    }

    public enum BuffType { 持续治疗, 持续伤害, 属性增强, 属性削弱, 护盾 }
}