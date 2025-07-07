using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 单例 MonoBehaviour，用于将此 GameObject 标识为主 Cinemachine 摄像机的目标。
    /// </summary>
    /// <seealso cref="CameraTarget"/>
    /// <seealso cref="InitializeCameraTargetSystem"/>
    public class CameraTargetObject : MonoBehaviour
    {
        public static CameraTargetObject Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}