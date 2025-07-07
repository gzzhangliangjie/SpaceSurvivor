using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储线性移动速度的数据组件。线性移动是指实体沿其前向轴以此处定义的速度持续移动。
    /// </summary>
    /// <remarks>
    /// Used for lots of player attack projectiles to move them along their forward facing axis.
    /// </remarks>
    /// <seealso cref="LinearMovementAuthoring"/>
    /// <seealso cref="LinearMovementSystem"/>
    public struct LinearMovementSpeed : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="LinearMovementSpeed"/> 组件的创作脚本，使其在 <see cref="LinearMovementSystem"/> 中沿前向轴持续移动。
    /// </summary>
    public class LinearMovementAuthoring : MonoBehaviour
    {
        public float MoveSpeed;

        private class Baker : Baker<LinearMovementAuthoring>
        {
            public override void Bake(LinearMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new LinearMovementSpeed { Value = authoring.MoveSpeed });
            }
        }
    }

    /// <summary>
    /// 使实体沿其前向轴移动的系统，使用 <see cref="LinearMovementSpeed"/> 中定义的数据。
    /// </summary>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct LinearMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, movement) in SystemAPI.Query<RefRW<LocalTransform>, LinearMovementSpeed>())
            {
                transform.ValueRW.Position += transform.ValueRO.Forward() * movement.Value * deltaTime;
            }
        }
    }
}