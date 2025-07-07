using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与回旋镖运动相关的数据。
    /// </summary>
    /// <remarks>
    /// 用于扳手攻击 - <see cref="WrenchAttackSystem"/>
    /// </remarks>
    public struct BoomerangMovementData : IComponentData
    {
        public float MoveSpeed;
        public double StartTime;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="BoomerangMovementData"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// 注意，这个创作组件不会设置 <see cref="BoomerangMovementData.StartTime"/> 因为该值在烘焙时是未知的，所以必须在实体实例化时初始化该值。
    /// </remarks>
    public class BoomerangMovementAuthoring : MonoBehaviour
    {
        public float BoomerangMoveSpeed;

        private class Baker : Baker<BoomerangMovementAuthoring>
        {
            public override void Bake(BoomerangMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoomerangMovementData { MoveSpeed = authoring.BoomerangMoveSpeed });
            }
        }
    }

    /// <summary>
    /// 实现回旋镖运动的系统。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="DS_TranslationSystemGroup"/> 中更新，该系统在 Unity 的 TransformSystemGroup 之前更新，因此可以安全地更新此实体的 LocalTransform。
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct BoomerangMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, boomerangMovement) in SystemAPI.Query<RefRW<LocalTransform>, BoomerangMovementData>())
            {
                var elapsedTime = (float)(currentTime - boomerangMovement.StartTime);
                var velocity = -1f * boomerangMovement.MoveSpeed * elapsedTime * elapsedTime + boomerangMovement.MoveSpeed;
                var velocityVector = transform.ValueRO.Forward() * velocity * deltaTime;
                transform.ValueRW.Position += velocityVector;
            }
        }
    }
}