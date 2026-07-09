using Jx3.UI;
using Jx3.UI.Panels;

namespace Jx3.Core.Scene
{
    public class MainCitySceneBoot : SceneBoot
    {
        protected override void SetupScene()
        {
            SetupEnvironment();
            UIManager.Instance.HideAll();
            UIManager.Instance.Show<MainCityPanel>();
        }
    }
}
