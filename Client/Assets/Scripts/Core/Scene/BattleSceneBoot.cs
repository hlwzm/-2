using Jx3.UI;
using Jx3.UI.Panels;

using Jx3.UI.Battle;
namespace Jx3.Core.Scene
{
    public class BattleSceneBoot : SceneBoot
    {
        protected override void SetupScene()
        {
            UIManager.Instance.Show<BattleHUD>();
        }
    }
}
