using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using RotationOrder = Unity.Mathematics.math.RotationOrder;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储实体恒定旋转相关数据的数据组件。
    /// </summary>
    /// <remarks>
    /// <see cref="ConstantRotationSystem"/> 将应用恒定旋转，只要此组件在实体上，就会应用此组件中定义的欧拉旋转。
    /// 主要用于视觉效果，其中攻击实体可能看起来在生命周期中不断旋转。
    /// </remarks>
    /// <seealso cref="ConstantRotationAuthoring"/>
    public struct ConstantRotationData : IComponentData
    {
        /// <summary>
        /// 在应用旋转前转换为四元数时欧拉轴的应用顺序。
        /// </summary>
        public RotationOrder RotationOrder;
        /// <summary>
        /// 沿每个轴应用的欧拉旋转，以弧度每秒存储。
        /// </summary>
        public float3 EulerRadiansPerSecond;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="ConstantRotationAuthoring"/> 组件的创作脚本。
    /// </summary>
    /// <seealso cref="ConstantRotationSystem"/>
    public class ConstantRotationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 在应用旋转前转换为四元数时欧拉轴的应用顺序。
        /// </summary>
        public RotationOrder RotationOrder = RotationOrder.XZY;
        /// <summary>
        /// 沿每个轴应用的欧拉旋转，以度每秒创作并在烘焙期间转换为弧度每秒。
        /// </summary>
        public Vector3 EulerDegreesPerSecond;

        private class Baker : Baker<ConstantRotationAuthoring>
        {
            public override void Bake(ConstantRotationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ConstantRotationData
                {
                    RotationOrder = authoring.RotationOrder,
                    EulerRadiansPerSecond = math.radians(authoring.EulerDegreesPerSecond)
                });
            }
        }
    }

    /// <summary>
    /// 对实体应用恒定旋转的系统。
    /// </summary>
    /// <seealso cref="ConstantRotationData"/>
    /// <seealso cref="ConstantRotationAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ConstantRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, rotationData) in SystemAPI.Query<RefRW<LocalTransform>, ConstantRotationData>())
            {
                var rotationThisFrame = quaternion.Euler(rotationData.EulerRadiansPerSecond * deltaTime, rotationData.RotationOrder);
                transform.ValueRW = transform.ValueRW.Rotate(rotationThisFrame);
            }
        }
    }
}