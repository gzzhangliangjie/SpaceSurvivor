using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 存储反向旋转相关数据的数据组件。反向旋转是指子实体的旋转应用与父实体相反的旋转，使其在视觉上具有固定朝向的外观。
    /// </summary>
    /// <remarks>
    /// 用于无人机攻击，因为主实体会不断围绕玩家旋转，而无人机实体会保持视觉上的上下方向。
    /// </remarks>
    public struct CounterRotationData : IComponentData
    {
        /// <summary>
        /// 用于应用额外旋转校正的欧拉偏移，以弧度存储。
        /// </summary>
        public float3 EulerOffset;
    }

    /// <summary>
    /// 用于在关联实体上初始化 <see cref="CounterRotationData"/> 的创作脚本。
    /// </summary>
    public class CounterRotationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 用于应用额外旋转校正的欧拉偏移。以度为单位创作，以弧度存储。
        /// </summary>
        [Tooltip("Enter in Degrees")]
        public Vector3 EulerOffset;

        private class Baker : Baker<CounterRotationAuthoring>
        {
            public override void Bake(CounterRotationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CounterRotationData
                {
                    EulerOffset = math.radians(authoring.EulerOffset)
                });
            }
        }
    }

    /// <summary>
    /// 对实体应用反向旋转的系统。反向旋转是指子实体的旋转应用与父实体相反的旋转，使其在视觉上具有固定朝向的外观。
    /// </summary>
    /// <remarks>
    /// Used on drone attack as main entity will constantly rotate around the player and drone entities will stay visually up and down.
    /// Updates in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup, so LocalTransform can be safely modified.
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct CounterRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, counterRotation, parent) in SystemAPI.Query<RefRW<LocalTransform>, CounterRotationData, Parent>())
            {
                var parentEuler = math.Euler(SystemAPI.GetComponent<LocalTransform>(parent.Value).Rotation);
                var rotation = counterRotation.EulerOffset - parentEuler;
                transform.ValueRW.Rotation = quaternion.Euler(rotation);
            }
        }
    }
}