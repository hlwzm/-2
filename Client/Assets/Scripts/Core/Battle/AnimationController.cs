using UnityEngine;

namespace Jx3.Core.Battle
{
    /// <summary>
    /// 角色动画控制器 - 使用Transform动画模拟角色动作
    /// 无需FBX/Animator，纯代码驱动
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        public enum AnimState { Idle, Run, Attack, Hit, Death, Skill }

        private AnimState _currentState = AnimState.Idle;
        private float _stateTimer;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;

        [Header("参数")]
        public float idleBobSpeed = 1.5f;
        public float idleBobHeight = 0.05f;
        public float attackSwingAngle = 30f;
        public float hitShakeDuration = 0.15f;
        public float deathFallAngle = 90f;

        [Header("身体部件")]
        public Transform bodyRoot;
        public Transform weaponAttachPoint;

        void Start()
        {
            _originalPosition = transform.localPosition;
            _originalRotation = transform.localRotation;
            _originalScale = transform.localScale;

            if (bodyRoot == null) bodyRoot = transform;
        }

        void Update()
        {
            _stateTimer += Time.deltaTime;
            switch (_currentState)
            {
                case AnimState.Idle: UpdateIdle(); break;
                case AnimState.Attack: UpdateAttack(); break;
                case AnimState.Hit: UpdateHit(); break;
                case AnimState.Death: /* 保持倒地 */ break;
                case AnimState.Skill: UpdateSkill(); break;
            }
        }

        void UpdateIdle()
        {
            // 呼吸浮动
            float bob = Mathf.Sin(_stateTimer * idleBobSpeed) * idleBobHeight;
            ApplyPositionOffset(Vector3.up * bob);

            // 轻微左右晃动
            float sway = Mathf.Sin(_stateTimer * idleBobSpeed * 0.5f) * 2f;
            ApplyRotationOffset(Quaternion.Euler(0, sway, 0));
        }

        void UpdateAttack()
        {
            float duration = 0.4f;
            float progress = _stateTimer / duration;

            if (progress < 1f)
            {
                // 前刺动作
                float forwardOffset = Mathf.Sin(progress * Mathf.PI) * 0.3f;
                ApplyPositionOffset(Vector3.forward * forwardOffset);

                // 身体前倾旋转
                float angle = Mathf.Sin(progress * Mathf.PI) * attackSwingAngle;
                ApplyRotationOffset(Quaternion.Euler(angle * 0.5f, 0, 0));
            }
            else
            {
                SetState(AnimState.Idle);
            }
        }

        void UpdateHit()
        {
            float duration = hitShakeDuration;
            float progress = _stateTimer / duration;

            if (progress < 1f)
            {
                // 受击后退
                float backOffset = Mathf.Sin(progress * Mathf.PI * 2) * 0.15f;
                ApplyPositionOffset(Vector3.back * Mathf.Abs(backOffset));

                // 身体后仰
                float angle = Mathf.Sin(progress * Mathf.PI * 2) * 10f;
                ApplyRotationOffset(Quaternion.Euler(angle, 0, 0));
            }
            else
            {
                SetState(AnimState.Idle);
            }
        }

        void UpdateSkill()
        {
            float duration = 0.8f;
            float progress = _stateTimer / duration;

            if (progress < 1f)
            {
                // 技能施放动作 - 抬手
                float upOffset = Mathf.Sin(progress * Mathf.PI) * 0.5f;
                ApplyPositionOffset(Vector3.up * upOffset);

                // 旋转
                float spin = progress * 360f;
                ApplyRotationOffset(Quaternion.Euler(0, spin, 0));
            }
            else
            {
                SetState(AnimState.Idle);
            }
        }

        void ApplyPositionOffset(Vector3 offset)
        {
            if (bodyRoot != null)
                bodyRoot.localPosition = _originalPosition + offset;
        }

        void ApplyRotationOffset(Quaternion offset)
        {
            if (bodyRoot != null)
                bodyRoot.localRotation = offset * _originalRotation;
        }

        public void SetState(AnimState newState)
        {
            if (_currentState == AnimState.Death) return; // 死亡后不再切换
            _currentState = newState;
            _stateTimer = 0;

            if (newState == AnimState.Idle || newState == AnimState.Death)
            {
                // 复位
                if (bodyRoot != null)
                {
                    bodyRoot.localPosition = _originalPosition;
                    bodyRoot.localRotation = _originalRotation;
                }
            }
        }

        public AnimState GetState() => _currentState;

        public void PlayAttack() => SetState(AnimState.Attack);
        public void PlayHit() => SetState(AnimState.Hit);
        public void PlaySkill() => SetState(AnimState.Skill);
        public void PlayDeath()
        {
            SetState(AnimState.Death);
            // 倒地动画
            if (bodyRoot != null)
                bodyRoot.localRotation = Quaternion.Euler(deathFallAngle, 0, 0);
        }

        void OnDisable()
        {
            // 重置
            if (bodyRoot != null)
            {
                bodyRoot.localPosition = _originalPosition;
                bodyRoot.localRotation = _originalRotation;
            }
        }
    }
}