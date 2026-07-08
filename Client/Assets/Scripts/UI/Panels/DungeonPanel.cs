using UnityEngine;
using UnityEngine.UI;

namespace Jx3.UI.Panels
{
    public class DungeonPanel : BasePanel
    {
        public Text[]? dungeonNameTexts = new Text[4];
        public Button[]? enterBtns = new Button[4];
        public Dropdown? difficultyDropdown;
        public Text? unlockInfoText;

        readonly string[] _dungeonNames = { "风雨稻香村", "天子峰", "日轮山城", "荻花宫" };
        readonly string[] _dungeonInfo = {
            "推荐Lv.20-30 | 4人\nBoss: 董龙→汪莽→肖人德→秦颐岩\n限时: 前3Boss≤8分钟",
            "推荐Lv.35-45 | 5人\nBoss: 影煞→罗宇→方鹤影→萧沙\n限时: 前3Boss≤10分钟",
            "推荐Lv.50-60 | 5-8人\nBoss: 源明雅→阿坊古→柳生雪→八岐大蛇\n限时: 前3Boss≤12分钟",
            "推荐Lv.65-75 | 8人\nBoss: 牡丹→大蛇→沙利亚→阿萨辛\n限时: 前3Boss≤15分钟"
        };

        void Start()
        {
            for (int i = 0; i < 4; i++)
            {
                var idx = i;
                if (dungeonNameTexts != null && i < dungeonNameTexts.Length && dungeonNameTexts[i] != null)
                    dungeonNameTexts[i].text = _dungeonNames[i];
                if (enterBtns != null && i < enterBtns.Length && enterBtns[i] != null)
                    enterBtns[i].onClick.AddListener(() => EnterDungeon(idx + 1));
            }
        }

        void EnterDungeon(int dungeonId)
        {
            Debug.Log($"[Dungeon] Enter dungeon {dungeonId}");
        }

        public override void OnOpen(object data = null) => gameObject.SetActive(true);
        public override void OnClose() => gameObject.SetActive(false);
    }
}
