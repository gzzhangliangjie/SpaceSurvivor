using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 带有此组件的实体在触发交互时将立即被销毁。
    /// </summary>
    /// <seealso cref="InstantDestroyOnInteractionSystem"/>
    /// <seealso cref="InstantDestroyOnInteractionAuthoring"/>
    /// <seealso cref="EntityInteraction"/>
    public struct InstantDestroyOnInteractionTag : IComponentData { }

    /// <summary>
    /// 用于向实体添加 <see cref="InstantDestroyOnInteractionTag"/> 组件的创作脚本。
    /// </summary>
    /// <seealso cref="InstantDestroyOnInteractionSystem"/>
    /// <seealso cref="InstantDestroyOnInteractionTag"/>
    /// <seealso cref="EntityInteraction"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class InstantDestroyOnInteractionAuthoring : MonoBehaviour
    {
        private class Baker : Baker<InstantDestroyOnInteractionAuthoring>
        {
            public override void Bake(InstantDestroyOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InstantDestroyOnInteractionTag>(entity);
            }
        }
    }

    /// <summary>
    /// 当 <see cref="EntityInteraction"/> 被触发时，向标有 <see cref="InstantDestroyOnInteractionTag"/> 的实体添加 <see cref="InstantDestroyTag"/> 的系统。
    /// </summary>
    /// <seealso cref="InstantDestroyOnInteractionTag"/>
    /// <seealso cref="EntityInteraction"/>
    /// <seealso cref="InstantDestroyTag"/>
    /// <seealso cref="InstantDestroyEntitySystem"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct InstantDestroyOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var interactionBuffer in SystemAPI.Query<DynamicBuffer<EntityInteraction>>().WithAll<InstantDestroyOnInteractionTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    ecb.AddComponent<InstantDestroyTag>(interaction.TargetEntity);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}