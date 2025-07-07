using UnityEngine;
using System.Collections;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于控制玩家初次生成到关卡时播放的光束传送效果的MonoBehaviour。
    /// </summary>
    public class BeamInEffectController : MonoBehaviour
    {
        /// <summary>
        /// 此MonoBehaviour的公共单例访问。
        /// </summary>
        public static BeamInEffectController Instance;

        /// <summary>
        /// 光束传送效果底部的Transform。用于旋转视觉效果的这一部分。
        /// </summary>
        [SerializeField] private Transform _beamInHit;
        /// <summary>
        /// 光束传送效果垂直光束的Transform。用于修改视觉效果的缩放。
        /// </summary>
        [SerializeField] private Transform _beamInBeam;
        /// <summary>
        /// 光束传送效果底部的旋转速度。
        /// </summary>
        [SerializeField] private float _rotationSpeed;
        /// <summary>
        /// 效果第一阶段的持续时间（秒）。
        /// </summary>
        [SerializeField] private float _effectTime;
        /// <summary>
        /// 效果淡出阶段的持续时间（秒）。
        /// </summary>
        [SerializeField] private float _fadeOutTime;

        /// <summary>
        /// 光束传送效果底部的精灵渲染器组件。用于淡出效果。
        /// </summary>
        private SpriteRenderer _hitSpriteRenderer;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _hitSpriteRenderer = _beamInHit.GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 调用协程播放光束传送效果的方法。此方法在关卡加载完成后由 <see cref="GameStartSystem"/> 调用。
        /// </summary>
        public void BeginBeamInEffect()
        {
            StartCoroutine(BeamInEffect());
        }

        /// <summary>
        /// 播放光束传送效果的协程。
        /// </summary>
        private IEnumerator BeamInEffect()
        {
            var effectTimer = _effectTime;
            while (effectTimer > 0f)
            {
                _beamInHit.Rotate(new Vector3(0f, 0f, _rotationSpeed * Time.deltaTime));

                var t = Mathf.Max(0f, effectTimer / _effectTime);

                _beamInBeam.localScale = new Vector3(0.5f * t, 0.5f, 0.5f);

                effectTimer -= Time.deltaTime;
                yield return null;
            }

            _beamInBeam.localScale = Vector3.zero;
            effectTimer = _fadeOutTime;

            while (effectTimer > 0f)
            {
                _beamInHit.Rotate(new Vector3(0f, 0f, _rotationSpeed * Time.deltaTime));
                var t = Mathf.Max(0f, effectTimer / _effectTime);
                var curColor = Color.Lerp(Color.clear, Color.white, t);
                _hitSpriteRenderer.color = curColor;
                effectTimer -= Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}