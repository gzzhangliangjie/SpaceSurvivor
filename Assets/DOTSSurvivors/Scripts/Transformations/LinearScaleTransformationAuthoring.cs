using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储线性缩放变换相关数据的数据组件。线性缩放变换是指实体的缩放在指定持续时间内从起始缩放变化到结束缩放。
    /// </summary>
    /// <seealso cref="LinearScaleTransformationEndTimestamp"/>
    /// <seealso cref="LinearScaleTransformationSystem"/>
    /// <seealso cref="LinearScaleTransformationAuthoring"/>
    public struct LinearScaleTransformationData : IComponentData
    {
        /// <summary>
        /// 缩放变换完成所需的持续时间（秒）。
        /// </summary>
        public float Duration;
        /// <summary>
        /// 起始统一缩放值。
        /// </summary>
        public float StartScale;
        /// <summary>
        /// 结束统一缩放值。
        /// </summary>
        public float EndScale;
    }

    /// <summary>
    /// 用于保存线性缩放变换结束时间戳的数据组件。
    /// </summary>
    /// <seealso cref="LinearScaleTransformationData"/>
    /// <seealso cref="LinearScaleTransformationSystem"/>
    public struct LinearScaleTransformationEndTimestamp : IComponentData, IEnableableComponent
    {
        public float Value;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="LinearScaleTransformationData"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this does not add the <see cref="LinearScaleTransformationEndTimestamp"/> component which is also required to perform the transformation. This will be initialized and added in the <see cref="LinearScaleTransformationSystem"/>
    /// </remarks>
    public class LinearScaleTransformationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 缩放变换完成所需的持续时间（秒）。
        /// </summary>
        public float Duration;
        /// <summary>
        /// 起始统一缩放值。
        /// </summary>
        public float StartScale;
        /// <summary>
        /// 结束统一缩放值。
        /// </summary>
        public float EndScale;

        private class Baker : Baker<LinearScaleTransformationAuthoring>
        {
            public override void Bake(LinearScaleTransformationAuthoring transformationAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new LinearScaleTransformationData
                {
                    Duration = transformationAuthoring.Duration,
                    StartScale = transformationAuthoring.StartScale,
                    EndScale = transformationAuthoring.EndScale
                });
            }
        }
    }

    /// <summary>
    /// 根据 <see cref="LinearScaleTransformationData"/> 中定义的数据执行线性变换的系统。
    /// </summary>
    /// <remarks>
    /// This system will first initialize and add the <see cref="LinearScaleTransformationEndTimestamp"/> component as that is required to calculate the scale transformation.
    /// </remarks>
    /// <seealso cref="LinearScaleTransformationEndTimestamp"/>
    /// <seealso cref="LinearScaleTransformationAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct LinearScaleTransformationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (scaleData, entity) in SystemAPI.Query<LinearScaleTransformationData>().WithAbsent<LinearScaleTransformationEndTimestamp>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new LinearScaleTransformationEndTimestamp
                {
                    Value = elapsedTime + scaleData.Duration
                });
            }

            ecb.Playback(state.EntityManager);

            foreach (var (transform, scaleData, endTimestamp, shouldChangeScale) in SystemAPI.Query<RefRW<LocalTransform>, LinearScaleTransformationData, LinearScaleTransformationEndTimestamp, EnabledRefRW<LinearScaleTransformationEndTimestamp>>())
            {
                var timeRemaining = endTimestamp.Value - elapsedTime;
                if (timeRemaining <= 0f)
                {
                    transform.ValueRW.Scale = scaleData.EndScale;
                    shouldChangeScale.ValueRW = false;
                    continue;
                }

                var t = 1 - timeRemaining / scaleData.Duration;
                var curScale = math.lerp(scaleData.StartScale, scaleData.EndScale, t);
                transform.ValueRW.Scale = curScale;
            }
        }
    }
}