using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class BattlePanel : BasePanel
    {
        public Slider? hpSlider;
        public Text? hpText;
        public Text? skill1CD;
        public Text? skill2CD;
        public Text? skill3CD;
        public Button? skill1Btn;
        public Button? skill2Btn;
        public Button? skill3Btn;
        public Button? ultimateBtn;
        public Button? dodgeBtn;
        public Button[]? switchHeroBtns;
        public Button? autoBtn;
        public Text? comboText;
        public Text? timerText;

        private bool _autoMode;

        void Start()
        {
            skill1Btn?.onClick.AddListener(() => CastSkill(0));
            skill2Btn?.onClick.AddListener(() => CastSkill(1));
            skill3Btn?.onClick.AddListener(() => CastSkill(2));
            ultimateBtn?.onClick.AddListener(() => CastSkill(3));
            dodgeBtn?.onClick.AddListener(DoDodge);
            autoBtn?.onClick.AddListener(ToggleAuto);
            if (switchHeroBtns != null)
                for (int i = 0; i < switchHeroBtns.Length; i++)
                {
                    var idx = i;
                    switchHeroBtns[i].onClick.AddListener(() => SwitchHero(idx));
                }
        }

        void Update()
        {
            if (hpSlider != null)
                hpSlider.value = 0.7f; // 模拟
            if (hpText != null)
                hpText.text = "HP: 70%";
        }

        void CastSkill(int slot)
        {
            Debug.Log($"[Battle] Cast skill slot {slot}");
        }

        void DoDodge()
        {
            Debug.Log("[Battle] Dodge");
        }

        void ToggleAuto()
        {
            _autoMode = !_autoMode;
            Debug.Log($"[Battle] Auto mode: {_autoMode}");
        }

        void SwitchHero(int idx)
        {
            Debug.Log($"[Battle] Switch to hero {idx}");
        }

        public override void OnOpen(object data = null) => gameObject.SetActive(true);
        public override void OnClose() => gameObject.SetActive(false);
    }
}
