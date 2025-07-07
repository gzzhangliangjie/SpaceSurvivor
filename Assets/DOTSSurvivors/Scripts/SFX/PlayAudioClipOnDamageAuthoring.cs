using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于定义关联实体受到伤害时播放的音频剪辑的数据组件。
    /// </summary>
    /// <remarks>
    /// Enableable component - when enabled, the audio clip will play.
    /// </remarks>
    /// <seealso cref="PlayAudioClipOnDamageSystem"/>
    public struct PlayAudioClipOnDamageData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 要播放的音频剪辑的优先级。数值越低优先级越高
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// 此实体受到伤害时播放的音频剪辑。
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }

    /// <summary>
    /// 用于向实体添加受到伤害时播放音频剪辑所需组件的创作脚本。
    /// </summary>
    public class PlayAudioClipOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 要播放的音频剪辑的优先级。数值越低优先级越高。
        /// </summary>
        public AudioPriority Priority = AudioPriority.Average;
        /// <summary>
        /// 此实体受到伤害时播放的音频剪辑。
        /// </summary>
        public AudioClip AudioClip;

        private class Baker : Baker<PlayAudioClipOnDamageAuthoring>
        {
            public override void Bake(PlayAudioClipOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayAudioClipOnDamageData
                {
                    AudioClip = authoring.AudioClip,
                    Priority = (byte)authoring.Priority
                });
                SetComponentEnabled<PlayAudioClipOnDamageData>(entity, false);
            }
        }
    }

    /// <summary>
    /// 实体受到伤害时播放音频剪辑的系统。
    /// </summary>
    /// <remarks>
    /// <see cref="ProcessDamageThisFrameSystem"/> will enable the <see cref="PlayAudioClipOnDamageData"/> component if the entity takes damage.
    /// System updates in the <see cref="DS_EffectsSystemGroup"/> which executes towards the end of the frame after <see cref="ProcessDamageThisFrameSystem"/> has ran for the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayAudioClipOnDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (audioClip, audioClipEnabled) in SystemAPI.Query<PlayAudioClipOnDamageData, EnabledRefRW<PlayAudioClipOnDamageData>>())
            {
                GameAudioController.Instance.PlaySfxAudioClip(audioClip.AudioClip.Value, audioClip.Priority);
                audioClipEnabled.ValueRW = false;
            }
        }
    }
}