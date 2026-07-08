using Jx3.UI;
using Jx3.UI.Panels;

namespace Jx3.Core.Scene
{
    public class LoginSceneBoot : SceneBoot
    {
        protected override void SetupScene()
        {
            SetupEnvironment();
            UIManager.Instance.Show<LoginPanel>();
        }
    }
}
