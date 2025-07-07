using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于初始化 <see cref="PlayAudioClipOnDestroyData"/> 的数据组件。
    /// </summary>
    /// <remarks>
    /// As <see cref="PlayAudioClipOnDestroyData"/> is a cleanup component, it cannot be added to the entity through baking so this initialization component is used to pass these values to the instance.
    /// </remarks>
    public struct InitializePlayAudioClipOnDestroyData : IComponentData
    {
        /// <summary>
        /// 要播放的音频剪辑的优先级。数值越低优先级越高。
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// 此实体被摧毁时播放的音频剪辑。
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }

    /// <summary>
    /// 用于保存关联实体被摧毁时将播放的音频剪辑的数据组件。
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <remarks>
    /// As this is a cleanup component, it cannot be added to the entity through baking, so the <see cref="InitializePlayAudioClipOnDestroyData"/> is used to set this up.
    /// </remarks>
    public struct PlayAudioClipOnDestroyData : ICleanupComponentData
    {
        /// <summary>
        /// 要播放的音频剪辑的优先级。数值越低优先级越高。
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// 此实体被摧毁时播放的音频剪辑。
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }

    /// <summary>
    /// 用于初始化 <see cref="PlayAudioClipOnDestroyData"/> 的创作脚本。
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    public class PlayAudioClipOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 要播放的音频剪辑的优先级。数值越低优先级越高。
        /// </summary>
        public AudioPriority Priority = AudioPriority.Average;
        /// <summary>
        /// 此实体被摧毁时播放的音频剪辑。
        /// </summary>
        public AudioClip AudioClip;

        private class Baker : Baker<PlayAudioClipOnDestroyAuthoring>
        {
            public override void Bake(PlayAudioClipOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InitializePlayAudioClipOnDestroyData
                {
                    Priority = (byte)authoring.Priority,
                    AudioClip = authoring.AudioClip
                });
            }
        }
    }

    /// <summary>
    /// 处理实体被摧毁时播放音频剪辑的系统。
    /// </summary>
    /// <remarks>
    /// As <see cref="PlayAudioClipOnDestroyData"/> is a cleanup component, it cannot be baked into a prefab, so we also need to initialize this component with <see cref="InitializePlayAudioClipOnDestroyData"/> shortly after the entity is spawned.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayAudioClipOnDestroySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (initializationData, entity) in SystemAPI.Query<InitializePlayAudioClipOnDestroyData>().WithNone<PlayAudioClipOnDestroyData>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new PlayAudioClipOnDestroyData
                {
                    Priority = initializationData.Priority,
                    AudioClip = initializationData.AudioClip
                });
                ecb.RemoveComponent<InitializePlayAudioClipOnDestroyData>(entity);
            }

            foreach (var (audioData, entity) in SystemAPI.Query<PlayAudioClipOnDestroyData>().WithNone<Simulate>().WithEntityAccess())
            {
                GameAudioController.Instance.PlaySfxAudioClip(audioData.AudioClip.Value, audioData.Priority);
                ecb.RemoveComponent<PlayAudioClipOnDestroyData>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}