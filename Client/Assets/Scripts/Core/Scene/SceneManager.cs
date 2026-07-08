using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Jx3.Core.Scene
{
    public enum GameScene
    {
        Boot, Login, MainCity, DungeonSelect, Battle, PVP
    }

    public class SceneManager : MonoBehaviour
    {
        public static SceneManager Instance { get; private set; } = null!;
        public GameScene CurrentScene { get; private set; } = GameScene.Boot;
        public float LoadingProgress { get; private set; }

        [SerializeField] private GameObject _loadingCanvas = null!;
        [SerializeField] private UnityEngine.UI.Slider _loadingBar = null!;
        [SerializeField] private UnityEngine.UI.Text _loadingText = null!;

        private static readonly (GameScene scene, string name)[] SceneMap = new[]
        {
            (GameScene.Boot, "Boot"), (GameScene.Login, "Login"),
            (GameScene.MainCity, "MainCity"), (GameScene.DungeonSelect, "DungeonSelect"),
            (GameScene.Battle, "Battle"), (GameScene.PVP, "PVP"),
        };

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            if (_loadingCanvas != null) _loadingCanvas.SetActive(false);
        }

        public void LoadScene(GameScene scene)
        {
            StartCoroutine(LoadAsync(scene));
        }

        private IEnumerator LoadAsync(GameScene scene)
        {
            if (_loadingCanvas != null) _loadingCanvas.SetActive(true);
            CurrentScene = scene;

            var name = "";
            foreach (var s in SceneMap) { if (s.scene == scene) name = s.name; }

            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name);
            if (op == null)
            {
                Debug.LogError($"[Scene] Scene {name} not found in build settings!");
                yield break;
            }

            while (!op.isDone)
            {
                LoadingProgress = op.progress;
                if (_loadingBar != null) _loadingBar.value = op.progress;
                if (_loadingText != null) _loadingText.text = $"加载中...{op.progress * 100:F0}%";
                yield return null;
            }

            if (_loadingCanvas != null) _loadingCanvas.SetActive(false);
            Debug.Log($"[Scene] Loaded {name}");
        }
    }
}
