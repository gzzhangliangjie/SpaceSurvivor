using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 单例 MonoBehaviour，用于在每个关卡中设置与当前角色关联国家的国旗作为航天局旗帜。
    /// </summary>
    /// <remarks>
    /// 此控制器将存在于场景根目录中“Flagpole”GameObject 的子对象“Flag”GameObject 上。
    /// </remarks>
    public class SpaceAgencyFlagController : MonoBehaviour
    {
        /// <summary>
        /// 公共单例实例。
        /// </summary>
        public static SpaceAgencyFlagController Instance;

        /// <summary>
        /// 旗帜的 SpriteRenderer
        /// </summary>
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning, multiple instances of SpaceAgencyFlagController detected. Destroying new object to retain singleton behavior");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.enabled = false;
        }

        /// <summary>
        /// 将旗帜设置为所需国家的国旗。
        /// </summary>
        /// <param name="flagSprite">与当前角色关联的国家国旗。</param>
        /// <remarks>
        /// 在游戏开始时执行一次的 <see cref="GameStartSystem.OnUpdate"/> 中调用。
        /// </remarks>
        public void SetFlag(Sprite flagSprite)
        {
            _spriteRenderer.enabled = true;
            _spriteRenderer.sprite = flagSprite;
        }
    }
}