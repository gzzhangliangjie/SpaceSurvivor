using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于保存击退当前状态的数据组件。
    /// </summary>
    /// <remarks>
    /// 可启用组件 - 当启用时，实体正在被击退。
    /// </remarks>
    public struct KnockbackState : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 击退的强度。
        /// </summary>
        public float Strength;
        /// <summary>
        /// 击退效果过期的时间戳。
        /// </summary>
        public float EndTimestamp;
        /// <summary>
        /// 击退的方向。
        /// </summary>
        public float2 Direction;
    }

    /// <summary>
    /// 用于改变接受击退强度的乘数。
    /// </summary>
    public struct KnockbackTakenMultiplier : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于添加击退所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// 将此组件添加到将收到来自具有 <see cref="KnockbackOnInteractionData"/> 的实体的击退的实体。
    /// </remarks>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackSystem"/>
    /// <seealso cref="KnockbackOnInteractionSystem"/>
    public class KnockbackAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 用于改变接受击退强度的乘数。
        /// </summary>
        public float KnockbackTakenMultiplier = 1f;

        private class Baker : Baker<KnockbackAuthoring>
        {
            public override void Bake(KnockbackAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new KnockbackTakenMultiplier
                {
                    Value = authoring.KnockbackTakenMultiplier
                });
                AddComponent<KnockbackState>(entity);
                SetComponentEnabled<KnockbackState>(entity, false);
            }
        }
    }

    /// <summary>
    /// 对实体施加击退力的系统。
    /// </summary>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackOnInteractionSystem"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct KnockbackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            foreach (var (physicsVelocity, knockbackState, knockbackTakenMultiplier, shouldKnockback) in SystemAPI.Query<RefRW<PhysicsVelocity>, KnockbackState, KnockbackTakenMultiplier, EnabledRefRW<KnockbackState>>())
            {
                if (elapsedTime >= knockbackState.EndTimestamp)
                {
                    shouldKnockback.ValueRW = false;
                    continue;
                }

                physicsVelocity.ValueRW.Linear.xz = knockbackState.Direction * knockbackState.Strength * knockbackTakenMultiplier.Value;
            }
        }
    }
}