using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于初始化 <see cref="ExplosionGraphicsData"/> 的数据。
    /// </summary>
    /// <remarks>
    /// 这是必需的，因为 <see cref="ExplosionGraphicsData"/> 组件将被添加到此爆炸实体的 <see cref="GraphicsEntity"/> 中，所以这些值不能烘焙到其他实体上，必须在运行时设置。
    /// 初始化后，可启用组件将被禁用。
    /// </remarks>
    public struct ExplosionGraphicsInitializationData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 闪烁时将应用于图形实体的颜色色调。
        /// </summary>
        public float4 Color;
        /// <summary>
        /// 图形实体将开始闪烁的时间。当父实体的 <see cref="DestroyAfterTime.Value"/> 小于此数值时，图形实体将开始闪烁。
        /// </summary>
        public float FlashTime;
        /// <summary>
        /// 图形实体闪烁的频率 - 意味着图形实体将在此时间段内在无颜色色调和使用定义的颜色值着色之间交替。
        /// </summary>
        public float FlashFrequency;
    }

    /// <summary>
    /// 组件，用于存储与具有 <see cref="ExplodeOnTimerData"/> 组件的爆炸实体的 <see cref="GraphicsEntity"/> 闪烁相关的数据。
    /// </summary>
    public struct ExplosionGraphicsData : IComponentData
    {
        /// <summary>
        /// 闪烁时将应用于图形实体的颜色色调。
        /// </summary>
        public float4 Color;
        /// <summary>
        /// 图形实体将开始闪烁的时间。当父实体的 <see cref="DestroyAfterTime.Value"/> 小于此数值时，图形实体将开始闪烁。
        /// </summary>
        public float FlashTime;
        /// <summary>
        /// 图形实体闪烁的频率 - 意味着图形实体将在此时间段内在无颜色色调和使用定义的颜色值着色之间交替。
        /// </summary>
        public float FlashFrequency;
    }

    /// <summary>
    /// 用于存储与可爆炸实体相关数据的数据组件。
    /// </summary>
    /// <remarks>
    /// 爆炸期间，实体将通过 <see cref="DestroyEntitySystem"/> 中的 <see cref="SpawnOnDestroy"/> 组件生成一个爆炸实体。此组件中的数据将应用于该爆炸实体。
    /// </remarks>
    public struct ExplodeOnTimerData : IComponentData
    {
        /// <summary>
        /// 爆炸实体将对敌方敌人造成的伤害量。
        /// </summary>
        public int Damage;
        /// <summary>
        /// 爆炸实体的规模。
        /// </summary>
        public float Area;
        /// <summary>
        /// 爆炸实体在游戏世界中存在的持续时间（秒）。
        /// </summary>
        public float ExplosionTimeToLive;
    }

    /// <summary>
    /// 用于向实体添加爆炸所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// 需要 <see cref="DestroyAfterTimeAuthoring"/> 和 <see cref="GraphicsEntityAuthoring"/> 以确保其他必要的组件存在。
    /// </remarks>
    [RequireComponent(typeof(DestroyAfterTimeAuthoring))]
    [RequireComponent(typeof(GraphicsEntityAuthoring))]
    public class ExplodeOnTimerAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 爆炸实体将对敌方敌人造成的伤害量。
        /// </summary>
        public int Damage;
        /// <summary>
        /// 爆炸实体的规模。
        /// </summary>
        public float Area;
        /// <summary>
        /// 爆炸实体在游戏世界中存在的持续时间（秒）。
        /// </summary>
        public float ExplosionTimeToLive;
        /// <summary>
        /// 图形实体将开始闪烁的时间。当父实体的 <see cref="DestroyAfterTime.Value"/> 小于此数值时，图形实体将开始闪烁。
        /// </summary>
        public float FlashTime;
        /// <summary>
        /// 图形实体闪烁的频率 - 意味着图形实体将在此时间段内在无颜色色调和使用定义的颜色值着色之间交替。
        /// </summary>
        public float FlashFrequency;
        /// <summary>
        /// 闪烁时将应用于图形实体的颜色色调。
        /// </summary>
        public Color FlashColor;
        /// <summary>
        /// GameObject prefab that will be spawned via <see cref="SpawnOnDestroy"/> component in the <see cref="DestroyEntitySystem"/>.
        /// </summary>
        public GameObject ExplosionPrefab;

        private class Baker : Baker<ExplodeOnTimerAuthoring>
        {
            public override void Bake(ExplodeOnTimerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ExplosionGraphicsInitializationData
                {
                    Color = (Vector4)authoring.FlashColor,
                    FlashTime = authoring.FlashTime,
                    FlashFrequency = authoring.FlashFrequency
                });
                AddComponent(entity, new ExplodeOnTimerData
                {
                    Damage = authoring.Damage,
                    Area = authoring.Area,
                    ExplosionTimeToLive = authoring.ExplosionTimeToLive
                });
                var explosionPrefab = GetEntity(authoring.ExplosionPrefab, TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnOnDestroy { Value = explosionPrefab });
            }
        }
    }

    /// <summary>
    /// 系统，用于处理 <see cref="GraphicsEntity"/> 上的 <see cref="ExplosionGraphicsData"/> 的初始化，并在闪烁阶段闪烁图形实体。
    /// </summary>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct FlashOnTimerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Initialize graphics so they have the appropriate properties
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (initializationData, graphicsEntity, shouldInitialize) in SystemAPI.Query<ExplosionGraphicsInitializationData, GraphicsEntity, EnabledRefRW<ExplosionGraphicsInitializationData>>())
            {
                ecb.AddComponent(graphicsEntity.Value, new ExplosionGraphicsData
                {
                    Color = initializationData.Color,
                    FlashTime = initializationData.FlashTime,
                    FlashFrequency = initializationData.FlashFrequency
                });

                shouldInitialize.ValueRW = false;
            }

            foreach (var (colorOverride, explosionGraphicsData, parent) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, ExplosionGraphicsData, Parent>())
            {
                var timeRemaining = SystemAPI.GetComponent<DestroyAfterTime>(parent.Value).Value;
                if (timeRemaining >= explosionGraphicsData.FlashTime) continue;
                var shouldFlash = math.ceil(timeRemaining / explosionGraphicsData.FlashFrequency) % 2 == 0;
                colorOverride.ValueRW.Value = shouldFlash ? explosionGraphicsData.Color : new float4(1);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}