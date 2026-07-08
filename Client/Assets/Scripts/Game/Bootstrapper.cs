using UnityEngine;
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
            Debug.Log("[Boot] Awake done, UIRoot.Instance=" + (UIRoot.Instance != null ? "OK" : "NULL"));
        }

        void Start()
        {
            Debug.Log("[Boot] Start - showing LoginPanel");
            UIManager.Instance.Show<LoginPanel>();
            Debug.Log("[Boot] Start - loading Login scene");
            SceneManager.Instance.LoadScene(GameScene.Login);
        }
    }
}