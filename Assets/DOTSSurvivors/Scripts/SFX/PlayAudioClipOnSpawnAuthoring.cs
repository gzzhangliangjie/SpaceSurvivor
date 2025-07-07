using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 保存当关联实体生成时将播放的音频剪辑的数据组件。
    /// </summary>
    public struct PlayAudioClipOnSpawnData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 要播放的音频剪辑的优先级。较低的值具有更高的优先级。
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// 当此实体生成时要播放的音频剪辑。
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }

    /// <summary>
    /// 标记组件，表示此实体应在其整个生命周期内循环播放其关联的音频剪辑。
    /// </summary>
    /// <seealso cref="PlayAudioClipOnSpawnData"/>
    public struct ShouldLoopAudioClipTag : IComponentData { }

    /// <summary>
    /// 用于存储 <see cref="GameAudioController._loopingAudioSources"/> 中循环音频源索引的组件。
    /// </summary>
    /// <remarks>
    /// Cleanup component will call <see cref="GameAudioController.StopLoopingAudioClip"/> when the associated entity is destroyed to stop the looping sound effect.
    /// </remarks>
    public struct LoopingAudioSourceIndex : ICleanupComponentData
    {
        public int AudioClipIndex;
    }

    /// <summary>
    /// 用于添加在实体生成时播放音频剪辑所需组件的创作脚本。
    /// </summary>
    public class PlayAudioClipOnSpawnAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 要播放的音频剪辑的优先级。较低的值具有更高的优先级。
        /// </summary>
        public AudioPriority Priority = AudioPriority.Average;
        /// <summary>
        /// 当此实体生成时要播放的音频剪辑。
        /// </summary>
        public AudioClip AudioClip;
        /// <summary>
        /// 用于表示此实体应在其生命周期内循环播放音频的布尔值。
        /// </summary>
        public bool ShouldLoop;

        private class Baker : Baker<PlayAudioClipOnSpawnAuthoring>
        {
            public override void Bake(PlayAudioClipOnSpawnAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayAudioClipOnSpawnData
                {
                    Priority = (byte)authoring.Priority,
                    AudioClip = authoring.AudioClip
                });

                if (authoring.ShouldLoop)
                {
                    AddComponent<ShouldLoopAudioClipTag>(entity);
                }
            }
        }
    }

    /// <summary>
    /// 在实体生成时播放音频剪辑的系统。
    /// </summary>
    /// <remarks>
    /// Entities can be tagged with a <see cref="ShouldLoopAudioClipTag"/> which will loop the audio clip for the duration of its lifecycle.
    /// </remarks>
    /// <seealso cref="PlayAudioClipOnSpawnData"/>
    /// <seealso cref="ShouldLoopAudioClipTag"/>
    /// <seealso cref="LoopingAudioSourceIndex"/>
    /// <seealso cref="GameAudioController"/>
    /// <seealso cref="DestroyEntitySystem"/>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayAudioClipOnSpawnSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Plays non-looping audio clips when spawned.
            foreach (var (audioClip, audioClipEnabled) in SystemAPI.Query<PlayAudioClipOnSpawnData, EnabledRefRW<PlayAudioClipOnSpawnData>>().WithNone<ShouldLoopAudioClipTag, LoopingAudioSourceIndex>())
            {
                GameAudioController.Instance.PlaySfxAudioClip(audioClip.AudioClip.Value, audioClip.Priority);
                audioClipEnabled.ValueRW = false;
            }

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // Plays looping audio clips when spawned.
            foreach (var (audioClip, audioClipEnabled, entity) in SystemAPI.Query<PlayAudioClipOnSpawnData, EnabledRefRW<PlayAudioClipOnSpawnData>>().WithAll<ShouldLoopAudioClipTag>().WithNone<LoopingAudioSourceIndex>().WithEntityAccess())
            {
                var audioClipIndex = GameAudioController.Instance.PlayLoopingAudioClip(audioClip.AudioClip.Value, audioClip.Priority);
                audioClipEnabled.ValueRW = false;
                ecb.AddComponent(entity, new LoopingAudioSourceIndex { AudioClipIndex = audioClipIndex });
            }

            // Stops looping audio clips when the entity is destroyed.
            foreach (var (loopAudioClip, entity) in SystemAPI.Query<LoopingAudioSourceIndex>().WithNone<ShouldLoopAudioClipTag>().WithEntityAccess())
            {
                GameAudioController.Instance.StopLoopingAudioClip(loopAudioClip.AudioClipIndex);
                ecb.RemoveComponent<LoopingAudioSourceIndex>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}