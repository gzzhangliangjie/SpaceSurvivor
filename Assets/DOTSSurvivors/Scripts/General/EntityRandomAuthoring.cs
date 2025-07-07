using System;
using UnityEngine;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于表示在确定 <see cref="EntityRandom"/> 初始种子时应使用的初始化类型的枚举。
    /// </summary>
    public enum EntityRandomInitializationType : byte
    {
        /// <summary>
        /// None 值通常不应使用。将在 <see cref="EntityRandomAuthoring.Baker.Bake"/> 中烘焙期间使用索引 0 初始化 <see cref="EntityRandom"/>。
        /// </summary>
        None = 0,
        /// <summary>
        /// 手动覆盖值将在 <see cref="EntityRandomAuthoring.Baker.Bake"/> 中烘焙期间使用索引 <see cref="EntityRandomAuthoring.ManualOverrideValue"/> 初始化 <see cref="EntityRandom"/>。这对于调试以获得一致的随机结果很有帮助。
        /// </summary>
        ManualOverride = 1,
        /// <summary>
        /// 系统毫秒值将在 <see cref="InitializeEntityRandomSystem"/> 中初始化时使用系统毫秒时间值作为索引来初始化 <see cref="EntityRandom"/>。
        /// </summary>
        SystemMilliseconds = 2
    }

    /// <summary>
    /// 辅助数据组件，表示此实体需要在 <see cref="InitializeEntityRandomSystem"/> 中初始化其 <see cref="EntityRandom"/> 组件。
    /// </summary>
    /// <remarks>
    /// 一旦 <see cref="EntityRandom"/> 初始化完毕，可启用组件将被禁用。
    /// </remarks>
    public struct InitializeEntityRandom : IComponentData, IEnableableComponent
    {
        public EntityRandomInitializationType InitializationType;
    }

    /// <summary>
    /// 用于存储特定实体持有的 Unity.Mathematics.Random 类型的组件，用于随机数生成。
    /// </summary>
    /// <remarks>
    /// 实体拥有自己的随机数生成器，这对于需要在不同线程上并行生成唯一随机数的实例很有帮助。
    /// </remarks>
    public struct EntityRandom : IComponentData
    {
        public Random Value;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="EntityRandom"/> 组件的创作脚本。这也可以添加 <see cref="InitializeEntityRandom"/> 组件，以触发在烘焙期间无法设置的随机种子的初始化，例如在实体生成时使用系统的毫秒数。
    /// </summary>
    public class EntityRandomAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 设置在设置 <see cref="EntityRandom"/> 的初始种子时要使用的初始化类型。
        /// </summary>
        public EntityRandomInitializationType RandomInitializationType = EntityRandomInitializationType.SystemMilliseconds;
        /// <summary>
        /// 如果 <see cref="RandomInitializationType"/> 设置为 <see cref="EntityRandomInitializationType.ManualOverride"/>，则此值将在烘焙期间设置为初始种子。
        /// </summary>
        public uint ManualOverrideValue;

        private class Baker : Baker<EntityRandomAuthoring>
        {
            public override void Bake(EntityRandomAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                switch (authoring.RandomInitializationType)
                {
                    case EntityRandomInitializationType.None:
                        AddComponent(entity, new EntityRandom { Value = Random.CreateFromIndex(0) });
                        break;
                    case EntityRandomInitializationType.ManualOverride:
                        AddComponent(entity, new EntityRandom { Value = Random.CreateFromIndex(authoring.ManualOverrideValue) });
                        break;
                    case EntityRandomInitializationType.SystemMilliseconds:
                        AddComponent<EntityRandom>(entity);
                        AddComponent(entity, new InitializeEntityRandom { InitializationType = EntityRandomInitializationType.SystemMilliseconds });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// 根据 <see cref="InitializeEntityRandom"/> 中存储的 <see cref="EntityRandomInitializationType"/> 初始化 <see cref="EntityRandom"/> 的系统。
    /// </summary>
    /// <remarks>
    /// 此系统仅为 <see cref="EntityRandomInitializationType.SystemMilliseconds"/> 类型的初始化设置初始种子。对于其他初始化类型，假定 <see cref="EntityRandom"/> 已在烘焙或实例化期间初始化。系统毫秒值会额外添加一个索引，以便在同一帧初始化的多个实体不会以相同的随机种子开始。
    /// 系统在 <see cref="DS_InitializationSystemGroup"/> 中，在 <see cref="EnemyWaveSystem"/> 之前更新，以确保在其他系统需要访问 <see cref="EntityRandom"/> 之前完成其初始化。
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EnemyWaveSystem))]
    public partial struct InitializeEntityRandomSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var systemMilliseconds = (uint)DateTime.Now.Millisecond;

            var index = 0u;
            foreach (var (random, initializationType, shouldInitialize) in SystemAPI.Query<RefRW<EntityRandom>, InitializeEntityRandom, EnabledRefRW<InitializeEntityRandom>>().WithOptions(EntityQueryOptions.IncludeSystems))
            {
                if (initializationType.InitializationType == EntityRandomInitializationType.SystemMilliseconds)
                {
                    random.ValueRW.Value = Random.CreateFromIndex(systemMilliseconds + index);
                    index += 1;
                }

                shouldInitialize.ValueRW = false;
            }
        }
    }
}