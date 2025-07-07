using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 拥有此组件的实体将在时间值到期后被销毁。
    /// </summary>
    /// <remarks>
    /// 时间以游戏时间进行跟踪，意味着当游戏暂停时计时器也会暂停。
    /// </remarks>
    /// <seealso cref="DestroyAfterTimeSystem"/>
    /// <seealso cref="DestroyAfterTimeAuthoring"/>
    public struct DestroyAfterTime : IComponentData
    {
        /// <summary>
        /// 此值表示实体被销毁前剩余的时间（游戏时间）。
        /// </summary>
        public float Value;
    }

    /// <summary>
    /// 用于设置 <see cref="DestroyAfterTime"/> 组件初始值的创作脚本。
    /// </summary>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="DestroyAfterTimeSystem"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyAfterTimeAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 实体在世界中应存在的时间（秒）。
        /// </summary>
        public float DestroyAfterTime;

        private class Baker : Baker<DestroyAfterTimeAuthoring>
        {
            public override void Bake(DestroyAfterTimeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DestroyAfterTime { Value = authoring.DestroyAfterTime });
            }
        }
    }

    /// <summary>
    /// 在给定时间后销毁实体的系统。此系统递减 <see cref="DestroyAfterTime"/> 的值，并在计时器值达到零时将 <see cref="DestroyEntityFlag"/> 设置为true。此计时器仅计算游戏内时间，不受游戏暂停影响。
    /// </summary>
    /// <remarks>
    /// 如果敌人实体以这种方式被摧毁，它不应该掉落经验宝石。
    /// </remarks>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="DestroyAfterTimeAuthoring"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyAfterTimeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (destroyAfterTime, entity) in SystemAPI.Query<RefRW<DestroyAfterTime>>().WithEntityAccess())
            {
                destroyAfterTime.ValueRW.Value -= deltaTime;
                if (destroyAfterTime.ValueRO.Value > 0f) continue;
                SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);

                // When destroying an enemy in this fashion, we don't want it to drop an experience gem
                if (!SystemAPI.HasComponent<DropExperienceOnDestroy>(entity)) continue;
                var dropExperienceOnDestroy = SystemAPI.GetComponentRW<DropExperienceOnDestroy>(entity);
                dropExperienceOnDestroy.ValueRW.ChanceToDrop = 0;
            }
        }
    }
}