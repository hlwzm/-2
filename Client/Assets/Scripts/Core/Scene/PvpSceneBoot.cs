using Jx3.UI;
using Jx3.UI.Panels;

namespace Jx3.Core.Scene
{
    public class PvpSceneBoot : SceneBoot
    {
        protected override void SetupScene()
        {
            SetupEnvironment();
            // 显示PVP匹配主界面
            UIManager.Instance.Show<PvpPanel>();
            // PvpMatchPanel 由匹配事件自动弹出
        }
    }
}