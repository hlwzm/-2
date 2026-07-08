using UnityEngine;
using UnityEngine.UI;
using Jx3.Core;
using Jx3.Core.Battle;
using Jx3.Core.Scene;
using System.Collections;

namespace Jx3.UI.Battle
{
    /// <summary>
    /// 战斗HUD - 完全程序化生成，无需Inspector配置
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

        void Start()
        {
            _player = FindObjectOfType<HeroUnit>();
            _enemy = FindObjectOfType<EnemyUnit>();
            _active = _player != null;
            BuildHUD();
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
                btnRt.sizeDelta = new Vector2(70, 70);
                btnRt.anchoredPosition = new Vector2(40 + i * 90, 0);
                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = new Color(0.2f, 0.2f, 0.35f);

                _skillBtns[i] = btnGo.AddComponent<Button>();
                _skillBtns[i].targetGraphic = btnImg;
                _skillBtns[i].onClick.AddListener(() => CastSkill(idx));

                // 快捷键
                var keyText = CreateLabel(btnRt, "Key", keys[i], 12, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.8f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(25, 18), new Vector2(2, -2));

                // 技能名
                CreateLabel(btnRt, "Name", skillNames[i], 14, TextAnchor.MiddleCenter, Color.white, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

                // CD遮罩
                var cdGo = new GameObject("CdMask", typeof(RectTransform), typeof(Image));
                cdGo.transform.SetParent(btnRt, false);
                cdGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                cdGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
                cdGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                _skillCdMasks[i] = cdGo.GetComponent<Image>();
                _skillCdMasks[i].color = new Color(0, 0, 0, 0.6f);
                _skillCdMasks[i].type = Image.Type.Filled;
                _skillCdMasks[i].fillMethod = Image.FillMethod.Radial360;
                _skillCdMasks[i].fillOrigin = 2;
                _skillCdMasks[i].gameObject.SetActive(false);

                _skillCdTexts[i] = CreateLabel(btnRt, "CdText", "", 18, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.3f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
                _skillCdTexts[i].gameObject.SetActive(false);
            }

            // === 连击/计时 (中上) ===
            _comboText = CreateLabel(root, "ComboText", "", 32, TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0.2f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300, 50), new Vector2(0, 80));
            _comboText.gameObject.SetActive(false);

            _timerText = CreateLabel(root, "TimerText", "00:00", 24, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.9f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(100, 30), new Vector2(0, -120));

            // === Buff区 ===
            _buffRoot = CreatePanel(root, "BuffArea", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(400, 40), new Vector2(20, 0));

            // === 结果文字(居中) ===
            _resultText = CreateLabel(root, "ResultText", "", 48, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 80), new Vector2(0, 0));
            _resultText.gameObject.SetActive(false);

            // 更新技能名称
            if (_player != null)
            {
                for (int i = 0; i < 4 && i < _player.skills.Length; i++)
                {
                    if (_player.skills[i] != null)
                    {
                        var nameText = _skillBtns[i]?.transform.Find("Name")?.GetComponent<Text>();
                        if (nameText != null) nameText.text = _player.skills[i].name.Length > 2
                            ? _player.skills[i].name.Substring(0, 2) : _player.skills[i].name;
                    }
                }
            }
        }

        void Update()
        {
            if (!_active) return;
            _battleTime += Time.deltaTime;
            UpdateBossHp();
            UpdatePlayerStatus();
            UpdateSkills();
            UpdateCombo();
            _timerText.text = $"{(int)_battleTime / 60:D2}:{(int)_battleTime % 60:D2}";

            if (Input.GetKeyDown(KeyCode.Alpha1)) CastSkill(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) CastSkill(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) CastSkill(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) CastSkill(3);
        }

        void UpdateBossHp()
        {
            if (_enemy == null) return;
            _bossNameText.text = _enemy.bossName;
            float ratio = _enemy.currentHp / _enemy.maxHp;
            _bossHpFill.fillAmount = ratio;
            _bossHpText.text = $"{Mathf.CeilToInt(_enemy.currentHp)}/{Mathf.CeilToInt(_enemy.maxHp)}";
        }

        void UpdatePlayerStatus()
        {
            if (_player == null) return;
            _hpFill.fillAmount = _player.currentHp / _player.maxHp;
            _hpText.text = $"{Mathf.CeilToInt(_player.currentHp)}/{Mathf.CeilToInt(_player.maxHp)}";
            _mpFill.fillAmount = _player.currentMp / _player.maxMp;
            _mpText.text = $"{Mathf.CeilToInt(_player.currentMp)}/{Mathf.CeilToInt(_player.maxMp)}";
        }

        void UpdateSkills()
        {
            if (_player == null) return;
            for (int i = 0; i < 4; i++)
            {
                bool onCd = i < _player.skillCooldowns.Length && _player.skillCooldowns[i] > 0;
                _skillCdMasks[i].gameObject.SetActive(onCd);
                _skillCdTexts[i].gameObject.SetActive(onCd);
                if (onCd)
                {
                    _skillCdMasks[i].fillAmount = _player.skillCooldowns[i] /
                        (_player.skills[i]?.cooldown ?? 1);
                    _skillCdTexts[i].text = $"{_player.skillCooldowns[i]:F1}";
                }
            }
        }

        void UpdateCombo()
        {
            if (_player == null) return;
            if (Time.time - _player.lastHitTime > 2f && _player.comboCount > 0)
                _player.ResetCombo();

            if (_player.comboCount > 1)
            {
                _comboText.text = $"{_player.comboCount} 连击!";
                _comboText.gameObject.SetActive(true);
                _comboText.color = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.2f),
                    Mathf.Min(_player.comboCount / 10f, 1f));
            }
            else _comboText.gameObject.SetActive(false);
        }

        void CastSkill(int slot)
        {
            if (_player == null || _enemy == null || !_enemy.isAlive) return;
            if (!_player.CanCastSkill(slot)) return;

            var skill = _player.skills[slot];
            _player.CastSkill(slot);

            float comboMul = CombatEngine.GetComboMultiplier(_player.comboCount);
            var dmg = CombatEngine.CalculateDamage(_player.heroId, _enemy.bossId, skill);
            dmg.damage = Mathf.RoundToInt(dmg.damage * comboMul);
            _enemy.TakeDamage(dmg);

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

        void ShowDamageNumber(DamageResult dmg)
        {
            if (_enemy == null) return;
            var go = new GameObject("Dmg");
            go.transform.SetParent(_enemy.transform, false);
            go.transform.localPosition = new Vector3(0, 3, 0);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var text = go.AddComponent<Text>();
            text.text = dmg.isCrit ? $"会心! {dmg.damage}" : $"-{dmg.damage}";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = dmg.isCrit ? 28 : 22;
            text.fontStyle = dmg.isCrit ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = dmg.isCrit ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.5f, 0.2f);

            StartCoroutine(FloatAndDestroy(go, 1.0f));
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
    }
}