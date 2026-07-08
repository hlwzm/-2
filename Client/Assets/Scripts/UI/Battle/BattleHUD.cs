using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Battle;
using Jx3.Core.Scene;
using System.Collections.Generic;
using System.Collections;

namespace Jx3.UI.Battle
{
    /// <summary>
    /// 战斗HUD - 完全程序化生成，无需Inspector配置
    /// 增强版：会心特效/连击显示/Buff提示/屏幕震动
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        private HeroUnit _player;
        private EnemyUnit _enemy;
        private float _battleTime;
        private bool _active;

        // UI引用
        private Image _bossHpFill; private Text _bossNameText; private Text _bossHpText;
        private Image _hpFill; private Text _hpText;
        private Image _mpFill; private Text _mpText;
        private Button[] _skillBtns = new Button[4];
        private Image[] _skillCdMasks = new Image[4];
        private Text[] _skillCdTexts = new Text[4];
        private Text _comboText;
        private Text _timerText;
        private Text _resultText;
        private RectTransform _buffRoot;
    // 增强UI元素
            private HeroSwitchSystem _switchSystem;
    private List<HeroAvatarSlot> _avatarSlots = new();
    private RectTransform _switchBar;
        private Text _critLabelText;        // "会心!" 大文字
        private Text _comboEffectText;      // 连击特效文字（火焰/闪电）
        private Text _buffNotificationText; // Buff添加/移除提示

        void Start()
        {
            _switchSystem = FindObjectOfType<HeroSwitchSystem>();
            if (_switchSystem != null)
            {
                _player = _switchSystem.CurrentHero;
                _switchSystem.OnHeroSwitched += (idx, hero) => { _player = hero; };
            }
            else
            {
                _player = FindObjectOfType<HeroUnit>();
            }
            _enemy = FindObjectOfType<EnemyUnit>();
            _active = _player != null;
            BuildHUD();
        }

        void Update()
        {
            if (_switchSystem != null && _avatarSlots.Count > 0)
            {
                for (int i = 0; i < _avatarSlots.Count; i++)
                {
                    var slot = _avatarSlots[i];
                    if (slot.unit == null) continue;
                    float hpPct = slot.unit.currentHp / slot.unit.maxHp;
                    slot.hpFill.fillAmount = hpPct;
                    slot.border.color = (i == _switchSystem.currentIndex)
                        ? new Color(1f, 0.8f, 0.2f)
                        : new Color(0.25f, 0.25f, 0.3f);
                    if (_switchSystem.SwitchCooldownRemaining > 0 && i != _switchSystem.currentIndex)
                    {
                        float cdPct = _switchSystem.SwitchCooldownRemaining / _switchSystem.switchCooldown;
                        slot.cdOverlay.fillAmount = cdPct;
                        slot.cdOverlay.gameObject.SetActive(true);
                    }
                    else
                    {
                        slot.cdOverlay.gameObject.SetActive(false);
                    }
                    if (!slot.unit.isAlive)
                    {
                        slot.nameText.color = new Color(0.5f, 0.5f, 0.5f);
                        slot.hpFill.color = new Color(0.3f, 0.3f, 0.3f);
                    }
                    else
                    {
                        slot.nameText.color = Color.white;
                        slot.hpFill.color = new Color(0.3f, 0.9f, 0.3f);
                    }
                }
            }
        }

        void BuildHUD()
        {
            var root = transform as RectTransform;

            // === Boss血量区 (顶部) ===
            var bossArea = CreatePanel(root, "BossArea", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(500, 80), new Vector2(0, -30));
            _bossNameText = CreateLabel(bossArea, "BossName", "Boss", 22, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.2f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(400, 30), new Vector2(0, -5));

            var hpBarGo = new GameObject("BossHpBar", typeof(RectTransform), typeof(Image));
            hpBarGo.transform.SetParent(bossArea, false);
            var hpBarRt = hpBarGo.GetComponent<RectTransform>();
            hpBarRt.anchorMin = new Vector2(0, 0); hpBarRt.anchorMax = new Vector2(1, 0);
            hpBarRt.sizeDelta = new Vector2(-20, 20); hpBarRt.anchoredPosition = new Vector2(0, 5);
            var hpBarBg = hpBarGo.GetComponent<Image>();
            hpBarBg.color = new Color(0.15f, 0.1f, 0.1f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(hpBarRt, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
            _bossHpFill = fillGo.GetComponent<Image>();
            _bossHpFill.color = new Color(1f, 0.25f, 0.15f);
            _bossHpFill.type = Image.Type.Filled; _bossHpFill.fillMethod = Image.FillMethod.Horizontal;

            _bossHpText = CreateLabel(hpBarRt, "HpText", "", 14, TextAnchor.MiddleCenter, Color.white, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

            // === 玩家状态 (左下) ===
            var playerArea = CreatePanel(root, "PlayerArea", new Vector2(0, 0), new Vector2(0, 0), new Vector2(350, 100), new Vector2(20, 80));

            var hpLabel = CreateLabel(playerArea, "HpLabel", "HP", 14, TextAnchor.MiddleLeft, new Color(0.5f, 0.8f, 0.5f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, 20), new Vector2(5, -5));
            var playerHpBar = CreateBar(playerArea, "HpBar", new Color(0.3f, 0.9f, 0.3f), new Vector2(35, 0), new Vector2(250, 18), out _hpFill);
            _hpText = CreateLabel(playerHpBar.transform.parent as RectTransform, "HpText", "", 12, TextAnchor.MiddleCenter, Color.white, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

            var mpLabel = CreateLabel(playerArea, "MpLabel", "MP", 14, TextAnchor.MiddleLeft, new Color(0.5f, 0.6f, 1f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, 20), new Vector2(5, -28));
            var playerMpBar = CreateBar(playerArea, "MpBar", new Color(0.3f, 0.5f, 1f), new Vector2(35, -23), new Vector2(250, 18), out _mpFill);
            _mpText = CreateLabel(playerMpBar.transform.parent as RectTransform, "MpText", "", 12, TextAnchor.MiddleCenter, Color.white, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

            // === 技能栏 (底部) ===
            var skillArea = CreatePanel(root, "SkillArea", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(500, 100), new Vector2(0, 30));

            var keys = new[] { "1", "2", "3", "4" };
            var skillNames = new[] { "技能1", "技能2", "技能3", "终" };
            for (int i = 0; i < 4; i++)
            {
                var idx = i;
                var btnGo = new GameObject("Skill" + i, typeof(RectTransform), typeof(Image));
                btnGo.transform.SetParent(skillArea, false);
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0, 0.5f); btnRt.anchorMax = new Vector2(0, 0.5f);
                btnRt.sizeDelta = new Vector2(80, 80);
                btnRt.anchoredPosition = new Vector2(60 + i * 110, 0);
                var btnBg = btnGo.GetComponent<Image>();
                btnBg.color = new Color(0.2f, 0.2f, 0.25f);
                btnBg.raycastTarget = true;

                var btn = btnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => OnSkillClick(idx));
                _skillBtns[i] = btn;

                // 快捷键文字
                CreateLabel(btnRt, "KeyText", keys[i], 14, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.6f),
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, 20), new Vector2(5, -5));

                // 技能名称
                var nameText = CreateLabel(btnRt, "NameText", skillNames[i], 16, TextAnchor.MiddleCenter, Color.white,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                // CD遮罩
                var cdGo = new GameObject("CdMask", typeof(RectTransform), typeof(Image));
                cdGo.transform.SetParent(btnRt, false);
                var cdRt = cdGo.GetComponent<RectTransform>();
                cdRt.anchorMin = Vector2.zero; cdRt.anchorMax = Vector2.one; cdRt.sizeDelta = Vector2.zero;
                var cdImg = cdGo.GetComponent<Image>();
                cdImg.color = new Color(0, 0, 0, 0.6f);
                cdImg.type = Image.Type.Filled; cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillOrigin = (int)Image.Origin360.Top;
                cdImg.fillClockwise = true;
                _skillCdMasks[i] = cdImg;

                _skillCdTexts[i] = CreateLabel(btnRt, "CdText", "", 20, TextAnchor.MiddleCenter, Color.white,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            // === 计时器 (顶部中间) ===
            _timerText = CreateLabel(root, "TimerText", "0.0s", 24, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(200, 40), new Vector2(0, -100));

            // === 连击显示 (右侧) ===
            var comboArea = CreatePanel(root, "ComboArea", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(200, 120), new Vector2(-30, 40));
            _comboText = CreateLabel(comboArea, "ComboCount", "", 48, TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0f),
                Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(200, 60), Vector2.zero);

            _comboEffectText = CreateLabel(comboArea, "ComboEffect", "", 18, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.2f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(200, 30), new Vector2(0, 5));

            // === 会心标签 (敌人上方，隐藏) ===
            _critLabelText = CreateLabel(root, "CritLabel", "会心!", 52, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 80), Vector2.zero);
            _critLabelText.gameObject.SetActive(false);

            // === Buff通知文字 ===
            _buffNotificationText = CreateLabel(root, "BuffNotify", "", 28, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500, 60), new Vector2(0, 100));
            _buffNotificationText.gameObject.SetActive(false);

            // === Buff图标根节点 ===
            _buffRoot = CreatePanel(root, "BuffRoot", new Vector2(0, 1), new Vector2(0, 1), new Vector2(300, 40), new Vector2(20, -150));

            // === 战斗结果 ===
            _resultText = CreateLabel(root, "ResultText", "", 60, TextAnchor.MiddleCenter, Color.white,
                Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(600, 100), Vector2.zero);
            _resultText.gameObject.SetActive(false);
        }

        void OnSkillClick(int slotIndex)
        {
            if (_player == null || _enemy == null || !_active) return;
            if (!_player.CanCastSkill(slotIndex)) return;
            var skill = _player.skills[slotIndex];
            if (skill == null) return;

            _player.CastSkill(slotIndex);
            float comboMul = CombatEngine.GetComboMultiplier(_player.comboCount);
            var dmg = CombatEngine.CalculateDamage(_player.heroId, _enemy.bossId, skill);
            dmg.damage = Mathf.RoundToInt(dmg.damage * comboMul);
            _enemy.TakeDamage(dmg);

            // 增强：播放技能特效
            if (skill.type == SkillType.终极)
            {
                Jx3.Battle.Skill.SkillEffect.PlayUltimate(_enemy.transform.position, new Color(1f, 0.6f, 0.1f));
                if (dmg.isCrit)
                    ShowCritLabel();
            }
            else if (skill.target == SkillTarget.群体)
            {
                Jx3.Battle.Skill.SkillEffect.PlayAoE(_enemy.transform.position, new Color(1f, 0.4f, 0.1f));
            }
            else if (skill.damageMultiplier < 0)
            {
                // 治疗技能 - 对玩家播放治疗特效
                Jx3.Battle.Skill.SkillEffect.PlayHeal(_player.transform.position);
            }
            else
            {
                Jx3.Battle.Skill.SkillEffect.PlaySingleTarget(_enemy.transform.position, new Color(1f, 0.4f, 0.1f));
            }

            // Buff处理
            if (skill.hasBuff)
            {
                if (skill.buffValue > 0)
                {
                    _player.buffs.Add(new BuffInstance(skill.buffName, skill.buffDuration, skill.buffValue, BuffType.属性增强));
                    ShowBuffNotification(skill.buffName, true);
                    Jx3.Battle.Skill.SkillEffect.PlayBuff(_player.transform.position, Color.green);
                }
                else
                {
                    _enemy.buffs.Add(new BuffInstance(skill.buffName, skill.buffDuration, skill.buffValue, BuffType.持续伤害));
                    ShowBuffNotification(skill.buffName, false);
                    Jx3.Battle.Skill.SkillEffect.PlayBuff(_enemy.transform.position, new Color(0.6f, 0f, 0f), 2.0f, false);
                }
            }

            ShowDamageNumber(dmg);
            _enemy = FindObjectOfType<EnemyUnit>();

            if (_enemy == null || !_enemy.isAlive)
            {
                _active = false;
                _resultText.text = "🎉 胜利!";
                _resultText.gameObject.SetActive(true);
                _resultText.color = new Color(1f, 0.8f, 0.2f);
                StartCoroutine(BackToCity(3f));
            }
        }

        // ===== 增强：伤害数字显示 =====

        void ShowDamageNumber(DamageResult dmg)
        {
            if (_enemy == null) return;

            if (dmg.isHeal)
            {
                // 治疗：绿色数字，在玩家头上
                ShowFloatingText(_player?.transform, dmg.damage, true, false);
                return;
            }

            // 在敌人头上显示伤害
            ShowFloatingText(_enemy.transform, dmg.damage, false, dmg.isCrit);

            if (dmg.isCrit)
            {
                // 会心额外效果：大文字 + 屏幕震动
                ShowCritLabel();
                StartCoroutine(ShakeCamera(0.15f, 0.3f));
                // 连击界面更新
                if (_player != null)
                    ShowComboEffect();
            }

            // 普通伤害
            if (_player != null && _player.comboCount > 0 && !dmg.isCrit)
            {
                UpdateComboDisplay();
            }
        }

        /// <summary>
        /// 在世界空间创建浮动数字
        /// </summary>
        void ShowFloatingText(Transform parent, int amount, bool isHeal, bool isCrit)
        {
            if (parent == null) return;
            var go = new GameObject(isHeal ? "HealNum" : "DmgNum");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f), 3, 0);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var text = go.AddComponent<Text>();

            if (isHeal)
            {
                text.text = $"+{amount}";
                text.fontSize = 24;
                text.color = new Color(0.3f, 1f, 0.3f);
                text.fontStyle = FontStyle.Normal;
            }
            else if (isCrit)
            {
                text.text = $"-{amount}";
                text.fontSize = 36;
                text.color = new Color(1f, 0.9f, 0.1f);
                text.fontStyle = FontStyle.Bold;
                // 会心数字略大
                go.transform.localPosition = new Vector3(Random.Range(-0.3f, 0.3f), 3.5f, 0);
                go.transform.localScale = Vector3.one * 1.4f;
            }
            else
            {
                text.text = $"-{amount}";
                text.fontSize = 20;
                text.color = Color.white;
                text.fontStyle = FontStyle.Normal;
            }

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;

            StartCoroutine(FloatAndDestroy(go, isCrit ? 1.5f : 1.0f));
        }

        IEnumerator FloatAndDestroy(GameObject go, float duration)
        {
            float t = 0;
            Vector3 start = go.transform.localPosition;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                go.transform.localPosition = start + new Vector3(0, p * 2f, 0);
                var txt = go.GetComponent<Text>();
                if (txt != null) txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, 1 - p);
                yield return null;
            }
            Destroy(go);
        }

        // ===== 增强：会心特效 =====

        void ShowCritLabel()
        {
            if (_critLabelText == null) return;
            StopCoroutine("CritLabelAnimation");
            StartCoroutine(CritLabelAnimation());
        }

        IEnumerator CritLabelAnimation()
        {
            _critLabelText.gameObject.SetActive(true);
            _critLabelText.text = "会心!";
            _critLabelText.color = new Color(1f, 0.9f, 0.1f);
            _critLabelText.fontSize = 52;

            float t = 0;
            float duration = 0.8f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                // 放大 + 淡出
                float scale = 1f + p * 0.5f;
                _critLabelText.transform.localScale = Vector3.one * scale;
                _critLabelText.color = new Color(1f, 0.9f, 0.1f, 1 - p);
                yield return null;
            }
            _critLabelText.gameObject.SetActive(false);
            _critLabelText.transform.localScale = Vector3.one;
        }

        // ===== 增强：屏幕震动 =====

        IEnumerator ShakeCamera(float intensity, float duration)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;
            Vector3 originalPos = cam.transform.localPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                float decay = 1f - p;
                float offsetX = Random.Range(-intensity, intensity) * decay;
                float offsetY = Random.Range(-intensity, intensity) * decay;
                cam.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }
            cam.transform.localPosition = originalPos;
        }

        // ===== 增强：连击显示 =====

        void UpdateComboDisplay()
        {
            if (_player == null || _comboText == null) return;
            int combo = _player.comboCount;
            if (combo > 0)
            {
                _comboText.text = $"{combo}连击!";
                // 根据连击数变色
                if (combo >= 10)
                    _comboText.color = new Color(1f, 0.3f, 0.1f); // 红
                else if (combo >= 5)
                    _comboText.color = new Color(1f, 0.7f, 0f);   // 橙
                else
                    _comboText.color = new Color(1f, 0.9f, 0.2f); // 金
            }
            else
            {
                _comboText.text = "";
                if (_comboEffectText != null)
                    _comboEffectText.text = "";
            }
        }

        void ShowComboEffect()
        {
            if (_player == null || _comboEffectText == null) return;
            int combo = _player.comboCount;

            string effect = "";
            if (combo >= 15)
                effect = "🔥🔥 无双! 🔥🔥";
            else if (combo >= 10)
                effect = "⚡⚡ 破军! ⚡⚡";
            else if (combo >= 5)
                effect = "🔥 连击! 🔥";
            else
                effect = "⚡ 连击 ⚡";

            _comboEffectText.text = effect;
            Color effectColor = combo >= 10 ? new Color(1f, 0.3f, 0.1f) : new Color(1f, 0.7f, 0f);
            _comboEffectText.color = effectColor;

            // 闪烁动画
            StopCoroutine("ComboEffectPulse");
            StartCoroutine(ComboEffectPulse());
        }

        IEnumerator ComboEffectPulse()
        {
            if (_comboEffectText == null) yield break;
            float t = 0;
            float duration = 0.6f;
            while (t < duration * 3)
            {
                t += Time.deltaTime;
                float pulse = Mathf.Sin(t * 20f) * 0.15f + 1f;
                _comboEffectText.transform.localScale = Vector3.one * pulse;
                yield return null;
            }
            if (_comboEffectText != null)
                _comboEffectText.transform.localScale = Vector3.one;
        }

        // ===== 增强：Buff通知 =====

        void ShowBuffNotification(string buffName, bool isBuff)
        {
            if (_buffNotificationText == null) return;
            StopCoroutine("BuffNotifyAnimation");
            StartCoroutine(BuffNotifyAnimation(buffName, isBuff));
        }

        IEnumerator BuffNotifyAnimation(string buffName, bool isBuff)
        {
            _buffNotificationText.gameObject.SetActive(true);
            _buffNotificationText.text = isBuff ? $"✨ {buffName}!" : $"☠ {buffName}!";
            _buffNotificationText.color = isBuff ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
            _buffNotificationText.fontSize = 28;

            float t = 0;
            float duration = 1.2f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                _buffNotificationText.color = new Color(
                    _buffNotificationText.color.r,
                    _buffNotificationText.color.g,
                    _buffNotificationText.color.b,
                    1 - p
                );
                // 上浮
                var rt = _buffNotificationText.rectTransform;
                rt.anchoredPosition = new Vector2(0, 100 + p * 40);
                yield return null;
            }
            _buffNotificationText.gameObject.SetActive(false);
            _buffNotificationText.rectTransform.anchoredPosition = new Vector2(0, 100);
        }

        IEnumerator BackToCity(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.Instance.LoadScene(GameScene.MainCity);
        }

                // ===== UI辅助方法 =====
        RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            return rt;
        }

        Text CreateLabel(Transform parent, string name, string text, int size, TextAnchor align, Color color, Vector2 aMin, Vector2 aMax, Vector2 sizeDelta, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.sizeDelta = sizeDelta; rt.anchoredPosition = pos;
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = size;
            txt.alignment = align;
            txt.color = color;
            return txt;
        }

        Image CreateBar(Transform parent, string name, Color fillColor, Vector2 pos, Vector2 size, out Image fill)
        {
            var bgGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(parent, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f); bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.sizeDelta = size; bgRt.anchoredPosition = pos;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bgRt, false);
            fillGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fillGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            fillGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            fill = fillGo.GetComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            return bgImg;
        }

        public void SetHero(HeroUnit h) { _player = h; }
        public void SetEnemy(EnemyUnit e) { _enemy = e; }

        // ===== 英雄切换UI =====

        void BuildSwitchBar(RectTransform root)
        {
            _switchBar = CreatePanel(root, "HeroSwitchBar", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(500, 80), new Vector2(0, -80));

            int slotCount = _switchSystem.Team.Count;
            float slotWidth = 80f;
            float totalWidth = slotCount * slotWidth + (slotCount - 1) * 8;
            float startX = -totalWidth / 2 + slotWidth / 2;

            for (int i = 0; i < slotCount; i++)
            {
                var slot = new HeroAvatarSlot { heroIndex = i, unit = _switchSystem.Team[i].unit };

                var slotGo = new GameObject("HeroSlot_" + i, typeof(RectTransform));
                slotGo.transform.SetParent(_switchBar, false);
                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0, 0.5f);
                slotRt.anchorMax = new Vector2(0, 0.5f);
                slotRt.sizeDelta = new Vector2(slotWidth, 70);
                slotRt.anchoredPosition = new Vector2(startX + i * (slotWidth + 8), 0);
                slot.root = slotRt;

                var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
                borderGo.transform.SetParent(slotRt, false);
                borderGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                borderGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
                borderGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                slot.border = borderGo.GetComponent<Image>();
                slot.border.color = new Color(0.25f, 0.25f, 0.3f);
                slot.border.type = Image.Type.Sliced;

                var avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
                avatarGo.transform.SetParent(borderGo.transform, false);
                var avatarRt = avatarGo.GetComponent<RectTransform>();
                avatarRt.anchorMin = new Vector2(0.5f, 0.5f);
                avatarRt.anchorMax = new Vector2(0.5f, 0.5f);
                avatarRt.sizeDelta = new Vector2(50, 50);
                avatarRt.anchoredPosition = Vector2.zero;
                avatarGo.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f);
                avatarGo.GetComponent<Image>().type = Image.Type.Sliced;

                var avatarTextGo = new GameObject("AvatarText", typeof(RectTransform), typeof(Text));
                avatarTextGo.transform.SetParent(avatarGo.transform, false);
                avatarTextGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                avatarTextGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
                avatarTextGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                slot.nameText = avatarTextGo.GetComponent<Text>();
                slot.nameText.text = slot.unit.heroName.Length > 0 ? slot.unit.heroName[0].ToString() : "?";
                slot.nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                slot.nameText.fontSize = 24;
                slot.nameText.fontStyle = FontStyle.Bold;
                slot.nameText.alignment = TextAnchor.MiddleCenter;
                slot.nameText.color = Color.white;

                var hpBarGo = new GameObject("HpBar", typeof(RectTransform), typeof(Image));
                hpBarGo.transform.SetParent(slotRt, false);
                var hpBarRt = hpBarGo.GetComponent<RectTransform>();
                hpBarRt.anchorMin = new Vector2(0, 0);
                hpBarRt.anchorMax = new Vector2(1, 0);
                hpBarRt.sizeDelta = new Vector2(-4, 6);
                hpBarRt.anchoredPosition = new Vector2(0, 4);
                hpBarGo.GetComponent<Image>().color = new Color(0.15f, 0.1f, 0.1f);

                var hpFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                hpFillGo.transform.SetParent(hpBarRt, false);
                hpFillGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                hpFillGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
                hpFillGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                slot.hpFill = hpFillGo.GetComponent<Image>();
                slot.hpFill.color = new Color(0.3f, 0.9f, 0.3f);
                slot.hpFill.type = Image.Type.Filled;
                slot.hpFill.fillMethod = Image.FillMethod.Horizontal;

                var cdGo = new GameObject("CdOverlay", typeof(RectTransform), typeof(Image));
                cdGo.transform.SetParent(slotRt, false);
                var cdRt = cdGo.GetComponent<RectTransform>();
                cdRt.anchorMin = new Vector2(0.7f, 0.6f);
                cdRt.anchorMax = new Vector2(1f, 1f);
                cdRt.sizeDelta = Vector2.zero;
                slot.cdOverlay = cdGo.GetComponent<Image>();
                slot.cdOverlay.color = new Color(0f, 0f, 0f, 0.6f);
                slot.cdOverlay.type = Image.Type.Filled;
                slot.cdOverlay.fillMethod = Image.FillMethod.Radial360;
                slot.cdOverlay.fillOrigin = (int)Image.Origin360.Top;
                slot.cdOverlay.fillClockwise = false;
                slot.cdOverlay.gameObject.SetActive(false);

                _avatarSlots.Add(slot);
            }
        }

        public void ShowDefeat()
        {
            _active = false;
            if (_resultText != null)
            {
                _resultText.text = "\U0001F480 \u56e2\u706d...";
                _resultText.gameObject.SetActive(true);
                _resultText.color = new Color(1f, 0.2f, 0.2f);
            }
            StartCoroutine(BackToCity(3f));
        }
    }

    class HeroAvatarSlot
    {
        public int heroIndex;
        public RectTransform root;
        public Image border;
        public Text nameText;
        public Image hpFill;
        public Image cdOverlay;
        public HeroUnit unit;
    }
}
