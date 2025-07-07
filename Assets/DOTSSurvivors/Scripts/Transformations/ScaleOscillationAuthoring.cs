using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储缩放震荡相关数据的数据组件。
    /// 缩放震荡是指实体的LocalTransform缩放按正弦波模式放大和缩小。
    /// </summary>
    public struct ScaleOscillationData : IComponentData
    {
        /// <summary>
        /// 用于计算缩放的正弦波周期（频率）。
        /// </summary>
        public float Period;
        /// <summary>
        /// 用于计算缩放的正弦波振幅（高度）。
        /// </summary>
        public float Amplitude;
        /// <summary>
        /// 沿Y轴应用的偏移，用于控制计算缩放的正弦波中点。
        /// </summary>
        public float YOffset;
    }

    /// <summary>
    /// 用于评估正弦波以确定给定时间缩放值的计时器。
    /// </summary>
    public struct ScaleOscillationTimer : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于初始化实体 <see cref="ScaleOscillationData"/> 值的创作脚本。
    /// </summary>
    /// <remarks>
    /// Authoring script uses user-friendly data values for authoring and bakes them into runtime friendly data.
    /// </remarks>
    public class ScaleOscillationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 用于计算缩放的正弦波周期（频率）。
        /// </summary>
        public float Period;
        /// <summary>
        /// 实体将在其间震荡的最小统一缩放值。
        /// </summary>
        public float MinScale;
        /// <summary>
        /// 实体将在其间震荡的最大统一缩放值。
        /// </summary>
        public float MaxScale;

        private class Baker : Baker<ScaleOscillationAuthoring>
        {
            public override void Bake(ScaleOscillationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var amplitude = (authoring.MaxScale - authoring.MinScale) / 2f;
                AddComponent(entity, new ScaleOscillationData
                {
                    Period = authoring.Period,
                    Amplitude = amplitude,
                    YOffset = authoring.MinScale + amplitude
                });
                AddComponent<ScaleOscillationTimer>(entity);
            }
        }
    }

    /// <summary>
    /// 根据 <see cref="ScaleOscillationData"/> 中的数据更新实体统一缩放值的系统。
    /// </summary>
    /// <remarks>
    /// System update in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup so the LocalTransform component can be safely modified.
    /// </remarks>
    /// <seealso cref="ScaleOscillationTimer"/>
    /// <seealso cref="ScaleOscillationAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ScaleOscillationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, timer, oscillationData) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<ScaleOscillationTimer>, ScaleOscillationData>())
            {
                timer.ValueRW.Value += deltaTime;
                var newScale = oscillationData.Amplitude * math.sin(oscillationData.Period * timer.ValueRO.Value) + oscillationData.YOffset;
                transform.ValueRW.Scale = newScale;
            }
        }
    }
}