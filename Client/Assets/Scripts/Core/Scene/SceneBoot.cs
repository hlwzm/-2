using UnityEngine;
using Jx3.UI;
using Jx3.Core;

namespace Jx3.Core.Scene
{
    public abstract class SceneBoot : MonoBehaviour
    {
        protected virtual void Awake()
        {
            SetupScene();
        }

        protected abstract void SetupScene();

        protected void SetupEnvironment()
        {
            var envGo = new GameObject("SceneEnv", typeof(SceneEnvironment));
            envGo.transform.SetParent(transform);
            Debug.Log("[SceneBoot] Environment set up");
        }

        protected void ShowPanelOnLayer<T>(RectTransform layer) where T : BasePanel
        {
            var panel = UIManager.Instance.Show<T>();
            if (panel != null) panel.transform.SetParent(layer, false);
        }
    }
}
