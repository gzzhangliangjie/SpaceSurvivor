using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储击退力相关数据的数据组件。
    /// </summary>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackSystem"/>
    /// <seealso cref="KnockbackOnInteractionSystem"/>
    public struct KnockbackOnInteractionData : IComponentData
    {
        /// <summary>
        /// 击退的强度。
        /// </summary>
        public float Strength;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="KnockbackOnInteractionData"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// 将此组件添加到将给具有 <see cref="KnockbackState"/> 和 <see cref="KnockbackTakenMultiplier"/> 的实体施加击退力的实体。
    /// </remarks>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class KnockbackOnInteractionAuthoring : MonoBehaviour
    {
        public float Strength;

        private class Baker : Baker<KnockbackOnInteractionAuthoring>
        {
            public override void Bake(KnockbackOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new KnockbackOnInteractionData
                {
                    Strength = authoring.Strength
                });
            }
        }
    }

    /// <summary>
    /// 对实体施加击退力的系统。
    /// </summary>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackSystem"/>
    /// <seealso cref="KnockbackOnInteractionData"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct KnockbackOnInteractionSystem : ISystem
    {
        /// <summary>
        /// 击退效果的持续时间（秒）。
        /// </summary>
        private const float KNOCKBACK_TIME = 0.15f;

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, knockback, transform) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, KnockbackOnInteractionData, LocalTransform>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<KnockbackState>(interaction.TargetEntity)) continue;
                    if (!SystemAPI.HasComponent<LocalTransform>(interaction.TargetEntity)) continue;
                    if (!SystemAPI.HasComponent<PhysicsVelocity>(interaction.TargetEntity)) continue;
                    var targetEntityPosition = SystemAPI.GetComponent<LocalTransform>(interaction.TargetEntity).Position;
                    var knockbackDirection = math.normalize(targetEntityPosition - transform.Position).xz;
                    SystemAPI.SetComponent(interaction.TargetEntity, new KnockbackState
                    {
                        Direction = knockbackDirection,
                        EndTimestamp = (float)SystemAPI.Time.ElapsedTime + KNOCKBACK_TIME,
                        Strength = knockback.Strength
                    });
                    SystemAPI.SetComponentEnabled<KnockbackState>(interaction.TargetEntity, true);
                }
            }
        }
    }
}