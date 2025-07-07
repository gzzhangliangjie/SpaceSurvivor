using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 在标题场景和游戏场景之间切换时显示的加载屏幕的UI控制器。
    /// </summary>
    /// <remarks>
    /// Marked as don't destroy on load so it can be shown/hidden across scene bounds.
    /// </remarks>
    public class LoadingScreenUIController : MonoBehaviour
    {
        public static LoadingScreenUIController Instance;

        [SerializeField] private GameObject _loadingScreen;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            HideLoadingScreen();
        }

        public void ShowLoadingScreen()
        {
            _loadingScreen.SetActive(true);
        }

        public void HideLoadingScreen()
        {
            _loadingScreen.SetActive(false);
        }
    }
}