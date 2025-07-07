using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 定义关联实体与其他实体交互时要造成的命中点数的数据组件。
    /// </summary>
    /// <remarks>
    /// Typically this component is added to an in-world attack entity and <see cref="EntityInteraction"/>s are raised by trigger events in <see cref="DetectCapabilityTriggerJob"/>.
    /// </remarks>
    public struct DealHitPointsOnInteraction : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="DealHitPointsOnInteraction"/> 组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="EntityInteractionAuthoring"/> component to ensure <see cref="EntityInteraction"/> is added to the entity as well.
    /// </remarks>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class DealHitPointsOnInteractionAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 关联实体与其他实体交互时要造成的命中点数。
        /// </summary>
        public int HitPoints;

        private class Baker : Baker<DealHitPointsOnInteractionAuthoring>
        {
            public override void Bake(DealHitPointsOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DealHitPointsOnInteraction
                {
                    Value = authoring.HitPoints
                });
            }
        }
    }

    /// <summary>
    /// 将伤害命中点数添加到目标实体的 <see cref="DamageThisFrame"/> 缓冲区的系统。
    /// </summary>
    /// <remarks>
    /// Updates in the <see cref="DS_InteractionSystemGroup"/> to ensure interactions for the current frame have already been added to this entity's <see cref="EntityInteraction"/> buffer.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct DealHitPointsOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, hitPointsToDeal) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, DealHitPointsOnInteraction>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasBuffer<DamageThisFrame>(interaction.TargetEntity)) continue;
                    var targetEntityDamageBuffer = SystemAPI.GetBuffer<DamageThisFrame>(interaction.TargetEntity);
                    targetEntityDamageBuffer.Add(new DamageThisFrame { Value = hitPointsToDeal.Value });
                }
            }
        }
    }
}