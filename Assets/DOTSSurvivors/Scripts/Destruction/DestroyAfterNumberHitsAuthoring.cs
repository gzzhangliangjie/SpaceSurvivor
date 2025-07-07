using UnityEngine;
using Unity.Entities;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 具有此组件的实体将在HitsRemaining达到0时被摧毁。
    /// </summary>
    /// <remarks>
    /// 通常用于只能命中最大数量实体的攻击实体。例如，请参阅 <see cref="SawBladeAttackSystem"/> 的实现
    /// </remarks>
    /// <seealso cref="DestroyAfterNumberHitsAuthoring"/>
    /// <seealso cref="DestroyAfterNumberHitsSystem"/>
    public struct DestroyAfterNumberHits : IComponentData
    {
        /// <summary>
        /// 实体被摧毁前剩余的命中次数。
        /// </summary>
        public int HitsRemaining;
    }

    /// <summary>
    /// 用于初始化 <see cref="DestroyAfterNumberHits"/> 组件的创作脚本。
    /// </summary>
    /// <seealso cref="DestroyAfterNumberHits"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyAfterNumberHitsAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 此实体在被摧毁前可以有的最大命中次数。
        /// </summary>
        public int MaxHitCount;

        private class Baker : Baker<DestroyAfterNumberHitsAuthoring>
        {
            public override void Bake(DestroyAfterNumberHitsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DestroyAfterNumberHits
                {
                    HitsRemaining = authoring.MaxHitCount
                });
            }
        }
    }

    /// <summary>
    /// 确定实体何时达到最大实体命中数的系统。每次命中都会递减 <see cref="DestroyAfterNumberHits.HitsRemaining"/>，直到达到零。一旦计数器达到零，实体的 <see cref="DestroyEntityFlag"/> 将被启用。
    /// </summary>
    /// <remarks>
    /// 此系统在交互系统开始时运行，因为如果此实体在单帧内命中多个实体，使命中次数超过最大命中次数，则未来的交互将忽略额外的命中。即，攻击实体不会对超过最大命中次数的实体造成伤害。
    /// </remarks>
    /// <seealso cref="DestroyAfterNumberHits"/>
    /// <seealso cref="DestroyAfterNumberHitsAuthoring"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup), OrderFirst = true)]
    public partial struct DestroyAfterNumberHitsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, destroyAfterNumberHits, destroyEntityFlag) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, RefRW<DestroyAfterNumberHits>, EnabledRefRW<DestroyEntityFlag>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                for (var i = 0; i < interactionBuffer.Length; i++)
                {
                    var interaction = interactionBuffer[i];
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<EnemyTag>(interaction.TargetEntity)) continue;
                    destroyAfterNumberHits.ValueRW.HitsRemaining -= 1;
                    if (destroyAfterNumberHits.ValueRO.HitsRemaining > 0) continue;

                    for (var j = interactionBuffer.Length - 1; j > i; j--)
                    {
                        interactionBuffer.RemoveAt(j);
                    }

                    destroyEntityFlag.ValueRW = true;
                    break;
                }
            }
        }
    }
}