using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 如果玩家收集了生命物品，将授予的生命值。
    /// </summary>
    /// <seealso cref="HealOnInteractionSystem"/>
    public struct HealOnInteraction : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// 用于初始化生命物品价值的Authoring脚本。
    /// </summary>
    /// <remarks>
    /// 需要额外的Authoring脚本来添加所需行为的必要组件。
    /// </remarks>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class HealOnInteractionAuthoring : MonoBehaviour
    {
        public int HealthValue;

        private class Baker : Baker<HealOnInteractionAuthoring>
        {
            public override void Bake(HealOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealOnInteraction { Value = authoring.HealthValue });
            }
        }
    }

    /// <summary>
    /// 当收集生命物品时为玩家恢复生命值的系统。
    /// </summary>
    /// <seealso cref="HealOnInteraction"/>
    /// <seealso cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct HealOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, healthItemProperties) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, HealOnInteraction>().WithAll<ItemTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;

                    // Need to multiply by -1 as ProcessDamageThisFrameSystem will then subtract negative hit points to increment value.
                    var amountToHeal = -1 * healthItemProperties.Value;
                    var targetDamageThisFrameBuffer = SystemAPI.GetBuffer<DamageThisFrame>(interaction.TargetEntity);
                    targetDamageThisFrameBuffer.Add(new DamageThisFrame { Value = amountToHeal });
                }
            }
        }
    }
}