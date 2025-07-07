using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于保存抛物线运动当前状态的数据组件。
    /// </summary>
    public struct ParabolicMovementState : IComponentData
    {
        /// <summary>
        /// 保存物体的速度。X轴方向的值在实体的整个生命周期内保持不变。Y轴方向的值会随当前速度更新，以便计算下一帧的速度。
        /// </summary>
        public float2 Velocity;
        /// <summary>
        /// 保存实体生成到世界时的起始时间戳。用于计算实体将遵循的速度抛物线。
        /// </summary>
        public double StartTime;
    }

    /// <summary>
    /// 用于定义 <see cref="ParabolicMovementState"/> 初始值的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this script does not initialize <see cref="ParabolicMovementState.StartTime"/> as this value will not be known at bake time, so it is important that this value is set during instantiation of the entity.
    /// </remarks>
    public class ParabolicMovementAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 抛物线运动的起始角度（度）。
        /// 根据此值将计算初始速度的float2值。
        /// </summary>
        public float StartingAngle;
        /// <summary>
        /// 应用于初始速度的额外力。
        /// </summary>
        public float BaseForce;

        private class Baker : Baker<ParabolicMovementAuthoring>
        {
            public override void Bake(ParabolicMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var initialVelocity = new float2
                {
                    x = math.sin(math.radians(authoring.StartingAngle)),
                    y = math.cos(math.radians(authoring.StartingAngle))
                };
                initialVelocity *= authoring.BaseForce;
                AddComponent(entity, new ParabolicMovementState
                {
                    Velocity = initialVelocity
                });
            }
        }
    }

    /// <summary>
    /// 对实体应用抛物线运动的系统。抛物线运动产生实体向屏幕底部移动的效果，就像应用了屏幕空间重力一样。
    /// </summary>
    /// <remarks>
    /// System update in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup so the LocalTransform component can be safely modified.
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ParabolicMovementSystem : ISystem
    {
        /// <summary>
        /// 用于计算抛物线的常量重力力。
        /// </summary>
        private const float GRAVITY_FORCE = -9f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, parabolicMovement) in SystemAPI.Query<RefRW<LocalTransform>, ParabolicMovementState>())
            {
                var activeTime = (float)(elapsedTime - parabolicMovement.StartTime);
                var yVelocity = GRAVITY_FORCE * activeTime * activeTime;
                yVelocity += parabolicMovement.Velocity.y;
                transform.ValueRW.Position.xz += new float2(parabolicMovement.Velocity.x, yVelocity) * deltaTime;
            }
        }
    }
}