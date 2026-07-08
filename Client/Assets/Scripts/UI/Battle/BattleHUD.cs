using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Jx3.Core;
using Jx3.Core.Scene;

namespace Jx3.UI.Battle
{
    public class BattleHUD : BasePanel
    {
        private RectTransform _skillBar;
        private List<SkillSlot> _skillSlots = new();
        private Text _combatTimeText, _comboText;
        private Image _hpBar, _bossHpBar;
        private RectTransform _buffArea;

        public class SkillSlot
        {
            public Button Btn; public Image Icon; public Image CdMask; public Text CdText;
        }

        protected override void Awake() { base.Awake(); BuildHUD(); }

        void BuildHUD()
        {
            var hpArea = new GameObject("HPArea", typeof(RectTransform)).GetComponent<RectTransform>();
            hpArea.SetParent(transform as RectTransform, false);
            hpArea.anchorMin = new Vector2(0, 0); hpArea.anchorMax = new Vector2(0.4f, 0);
            hpArea.sizeDelta = new Vector2(0, 40); hpArea.anchoredPosition = new Vector2(20, 40);

            _hpBar = CreateImage(hpArea, "HPBar", new Color(0, 0.8f, 0));
            _hpBar.rectTransform.anchorMin = new Vector2(0, 0); _hpBar.rectTransform.anchorMax = new Vector2(1, 1);
            _hpBar.rectTransform.sizeDelta = new Vector2(-60, -4); _hpBar.rectTransform.anchoredPosition = new Vector2(50, 0);

            var bossArea = new GameObject("BossArea", typeof(RectTransform)).GetComponent<RectTransform>();
            bossArea.SetParent(transform as RectTransform, false);
            bossArea.anchorMin = new Vector2(0.3f, 1); bossArea.anchorMax = new Vector2(0.7f, 1);
            bossArea.sizeDelta = new Vector2(0, 30); bossArea.anchoredPosition = new Vector2(0, -50);
            _bossHpBar = CreateImage(bossArea, "BossHPBar", new Color(0.9f, 0.1f, 0.1f));
            _bossHpBar.rectTransform.anchorMin = Vector2.zero; _bossHpBar.rectTransform.anchorMax = Vector2.one;
            _bossHpBar.rectTransform.sizeDelta = new Vector2(-10, -4);

            _combatTimeText = CreateText(transform as RectTransform, "TimeText", "00:00", 20);
            ((RectTransform)_combatTimeText.transform).anchoredPosition = new Vector2(600, 300);

            _comboText = CreateText(transform as RectTransform, "ComboText", "", 24);
            ((RectTransform)_comboText.transform).anchoredPosition = new Vector2(600, 260);
            _comboText.color = new Color(1, 0.8f, 0);

            _buffArea = new GameObject("BuffArea", typeof(RectTransform)).GetComponent<RectTransform>();
            _buffArea.SetParent(transform as RectTransform, false);
            _buffArea.anchorMin = new Vector2(0, 1); _buffArea.anchorMax = new Vector2(0.4f, 1);
            _buffArea.sizeDelta = new Vector2(0, 40); _buffArea.anchoredPosition = new Vector2(20, -100);

            _skillBar = new GameObject("SkillBar", typeof(RectTransform)).GetComponent<RectTransform>();
            _skillBar.SetParent(transform as RectTransform, false);
            _skillBar.anchorMin = new Vector2(0.5f, 0); _skillBar.anchorMax = new Vector2(0.5f, 0);
            _skillBar.sizeDelta = new Vector2(500, 80); _skillBar.anchoredPosition = new Vector2(0, 20);

            string[] skillNames = {"普攻", "技能1", "技能2", "技能3", "大招", "闪避"};
            for (int i = 0; i < 6; i++) {
                var idx = i; var slot = CreateSkillSlot(_skillBar, skillNames[i], i * 80 - 200);
                slot.Btn.onClick.AddListener(() => { BattleManager.Instance?.CastSkill((uint)(1001 + idx)); });
                _skillSlots.Add(slot);
            }

            var autoBtn = CreateButton(transform as RectTransform, "AutoBtn", "自动", () => BattleManager.Instance?.ToggleAuto());
            ((RectTransform)autoBtn.transform).anchoredPosition = new Vector2(500, 30);
        }

        SkillSlot CreateSkillSlot(RectTransform parent, string name, float x) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(70, 70); rt.anchoredPosition = new Vector2(x, 0);
            var slot = new SkillSlot();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            slot.Icon = CreateImage(rt, "Icon", new Color(0.5f, 0.5f, 0.7f, 0.8f));
            slot.Icon.rectTransform.anchorMin = Vector2.zero; slot.Icon.rectTransform.anchorMax = Vector2.one;
            slot.Icon.rectTransform.sizeDelta = new Vector2(-8, -8);
            slot.CdMask = CreateImage(rt, "CdMask", new Color(0, 0, 0, 0.6f));
            slot.CdMask.rectTransform.anchorMin = Vector2.zero; slot.CdMask.rectTransform.anchorMax = Vector2.one;
            slot.CdMask.rectTransform.sizeDelta = Vector2.zero;
            slot.CdMask.type = Image.Type.Filled; slot.CdMask.fillAmount = 0;
            slot.CdText = CreateText(rt, "CdText", "", 14);
            slot.CdText.rectTransform.anchorMin = Vector2.zero; slot.CdText.rectTransform.anchorMax = Vector2.one;
            slot.CdText.rectTransform.sizeDelta = Vector2.zero;
            slot.Btn = go.AddComponent<Button>();
            slot.Btn.targetGraphic = bg;
            var label = CreateText(rt, "Label", name, 12);
            label.rectTransform.anchoredPosition = new Vector2(0, -35);
            return slot;
        }

        public void UpdateHP(float p) { if (_hpBar != null) _hpBar.fillAmount = p; }
        public void UpdateBossHP(float p, string n) { if (_bossHpBar != null) _bossHpBar.fillAmount = p; }
        public void UpdateSkillCD(int i, float p, string t) { if (i >= 0 && i < _skillSlots.Count) { _skillSlots[i].CdMask.fillAmount = p; _skillSlots[i].CdText.text = t; } }
        public void UpdateCombo(int c) { _comboText.text = c > 1 ? c + " 连击!" : ""; }
    }
}
