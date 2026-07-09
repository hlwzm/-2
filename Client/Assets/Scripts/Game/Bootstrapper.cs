using UnityEngine;
using UnityEngine.EventSystems;
using Jx3.Core;
using Jx3.Core.Scene;
using Jx3.UI;
using Jx3.UI.Panels;

namespace Jx3.Game
{
    public class Bootstrapper : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;

            // EventSystem 必须存在，否则 InputField 和 Button 无法交互
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            Debug.Log("[Boot] Creating GameManager");
            var gmGo = new GameObject("GameManager", typeof(GameManager));
            Object.DontDestroyOnLoad(gmGo);

            gmGo.AddComponent<LoginManager>();
            Debug.Log("[Boot] Creating SceneManager");
            var sceneGo = new GameObject("SceneManager", typeof(SceneManager));
            Object.DontDestroyOnLoad(sceneGo);

            Debug.Log("[Boot] Creating UIRoot");
            UIRoot.Create();

            Debug.Log("[Boot] Creating UIManager");
            var uiGo = new GameObject("UIManager", typeof(UIManager));
            Object.DontDestroyOnLoad(uiGo);

            Object.DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            UIManager.Instance.Show<LoginPanel>();
            SceneManager.Instance.LoadScene(GameScene.Login);
        }
    }
}