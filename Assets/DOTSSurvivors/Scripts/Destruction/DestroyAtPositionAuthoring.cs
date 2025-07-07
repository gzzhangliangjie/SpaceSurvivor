using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 带有此组件的实体一旦到达此世界位置就会被销毁。
    /// </summary>
    public struct DestroyAtPosition : IComponentData
    {
        /// <summary>
        /// 实体将被销毁的目标位置。
        /// </summary>
        public float3 TargetPosition;
        /// <summary>
        /// 上次计算的实体与目标位置之间的距离。如果此值减小，则实体仍在向目标移动。如果此值增加，则表示实体已超过目标位置，应被销毁。
        /// </summary>
        public float LastDistanceSq;
    }

    /// <summary>
    /// <see cref="DestroyAtPosition"/> 的创作组件
    /// </summary>
    /// <seeaslo cref="DestroyAtPosition"/>
    /// <seeaslo cref="DestroyAtPositionSystem"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyAtPositionAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 此实体应被销毁的目标世界位置。
        /// </summary>
        public Vector3 Position;

        private class Baker : Baker<DestroyAtPositionAuthoring>
        {
            public override void Bake(DestroyAtPositionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DestroyAtPosition
                {
                    TargetPosition = authoring.Position,
                    LastDistanceSq = float.MaxValue
                });
            }
        }
    }

    /// <summary>
    /// 一旦关联实体到达目标世界位置，此系统将把 <see cref="DestroyEntityFlag"/> 设置为true。它通过将当前距离与先前距离进行比较来确定实体是否已到达该位置。如果当前距离大于先前距离，它就知道实体已通过目标位置。距离应小于两个单位，以减少误报的可能性。
    /// <remarks>
    /// 这种确定实体是否到达目标位置的方法，是为了确保那些移动速度快、不保证能停留在目标位置小容差范围内的实体也能被正确处理。
    /// </remarks>
    /// </summary>
    /// <seeaslo cref="DestroyAtPosition"/>
    /// <seeaslo cref="DestroyAtPositionAuthoring"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyAtPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (destroyAtPosition, transform, entity) in SystemAPI.Query<RefRW<DestroyAtPosition>, LocalToWorld>().WithEntityAccess())
            {
                var distanceToDestructionPositionSq = math.lengthsq(destroyAtPosition.ValueRO.TargetPosition.xz - transform.Position.xz);

                if (distanceToDestructionPositionSq < 4 && distanceToDestructionPositionSq > destroyAtPosition.ValueRO.LastDistanceSq)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                else
                {
                    destroyAtPosition.ValueRW.LastDistanceSq = distanceToDestructionPositionSq;
                }
            }
        }
    }
}