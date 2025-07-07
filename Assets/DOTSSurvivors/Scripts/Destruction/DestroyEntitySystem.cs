using System;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Random = Unity.Mathematics.Random;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 该系统负责销毁所有启用了 <see cref="DestroyEntityFlag"/> 的实体。有各种检查来确定销毁实体时是否应执行任何附加逻辑。
    /// 该系统应该是 <see cref="DS_DestructionSystemGroup"/> 中最后一个执行的系统，该系统组本身在 SimulationSystemGroup 的末尾更新。在 SimulationSystemGroup 中，此系统之后唯一运行的系统应该是 EndSimulationEntityCommandBufferSystem。
    /// </summary>
    /// <remarks>
    /// 销毁时额外逻辑的例子包括生成经验宝石、播放销毁VFX和SFX，以及触发游戏结束逻辑。
    /// </remarks>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        /// <summary>
        /// Unity.Mathematics.Random，用于计算此系统触发的随机事件的几率。
        /// </summary>
        /// <remarks>
        /// 初始种子是系统启动时来自系统时间的毫秒值。
        /// </remarks>
        private Random _systemRandom;

        /// <summary>
        /// 设置在将播放溶解动画的实体上的恒定时间值。
        /// </summary>
        /// <seealso cref="DissolveData"/>
        private const float DISSOLVE_DURATION = 0.4f;

        /// <summary>
        /// 当玩家实体被摧毁以触发游戏结束序列时将生成的实体的实体原型。
        /// </summary>
        private EntityArchetype _gameOverArchetype;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameEntityPrefabs>();
            _systemRandom = Random.CreateFromIndex((uint)DateTime.Now.Millisecond);
            _gameOverArchetype = state.EntityManager.CreateArchetype(typeof(BeginGameOverTag));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var beginFrameECB = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var endFrameECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in SystemAPI.Query<DestroyEntityFlag>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    beginFrameECB.CreateEntity(_gameOverArchetype);
                }

                if (SystemAPI.HasComponent<GraphicsEntityPlayDestroyEffectTag>(entity))
                {
                    var graphicsEntity = SystemAPI.GetComponent<GraphicsEntity>(entity).Value;
                    var newGraphicsEntity = endFrameECB.Instantiate(graphicsEntity);
                    endFrameECB.RemoveComponent<LocalTransform>(newGraphicsEntity);
                    endFrameECB.AddComponent(newGraphicsEntity, new DissolveData
                    {
                        StartTimestamp = (float)SystemAPI.Time.ElapsedTime,
                        Duration = DISSOLVE_DURATION
                    });
                }

                if (SystemAPI.HasComponent<EnemyTag>(entity))
                {
                    var aliensDefeatedCount = SystemAPI.GetSingletonRW<AliensDefeatedCount>();
                    aliensDefeatedCount.ValueRW.Value += 1;
                }

                if (SystemAPI.HasComponent<DropExperienceOnDestroy>(entity))
                {
                    var experiencePointProperties = SystemAPI.GetComponent<DropExperienceOnDestroy>(entity);
                    var shouldDropExperience = _systemRandom.NextInt(1, 101) <= experiencePointProperties.ChanceToDrop;
                    if (shouldDropExperience)
                    {
                        var experienceGemPrefab = SystemAPI.GetSingleton<GameEntityPrefabs>().ExperienceGemPrefab;

                        var newExperiencePointEntity = beginFrameECB.Instantiate(experienceGemPrefab);
                        beginFrameECB.SetComponent(newExperiencePointEntity, new ExperienceGemItemData
                        {
                            Value = experiencePointProperties.ExperienceValue
                        });
                        var spawnPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
                        beginFrameECB.SetComponent(newExperiencePointEntity, LocalTransform.FromPosition(spawnPosition));
                    }
                }

                if (SystemAPI.HasComponent<SpawnOnDestroy>(entity))
                {
                    var entityToSpawn = SystemAPI.GetComponent<SpawnOnDestroy>(entity).Value;
                    var entityToSpawnScale = SystemAPI.GetComponent<LocalTransform>(entityToSpawn).Scale;
                    var newEntity = beginFrameECB.Instantiate(entityToSpawn);
                    var entityTransform = SystemAPI.GetComponent<LocalTransform>(entity);
                    entityTransform = entityTransform.ApplyScale(entityToSpawnScale);
                    beginFrameECB.SetComponent(newEntity, entityTransform);

                    if (SystemAPI.HasComponent<ExplodeOnTimerData>(entity))
                    {
                        var explodeOnTimerProperties = SystemAPI.GetComponent<ExplodeOnTimerData>(entity);
                        beginFrameECB.SetComponent(newEntity, new DealHitPointsOnInteraction { Value = explodeOnTimerProperties.Damage });
                        entityTransform = entityTransform.ApplyScale(explodeOnTimerProperties.Area);
                        beginFrameECB.SetComponent(newEntity, entityTransform);
                        beginFrameECB.SetComponent(newEntity, new DestroyAfterTime { Value = explodeOnTimerProperties.ExplosionTimeToLive });
                    }
                }

                if (SystemAPI.HasComponent<RandomItemToSpawnBlob>(entity))
                {
                    ref var randomItemsToSpawn = ref SystemAPI.GetComponent<RandomItemToSpawnBlob>(entity).Value.Value;
                    var maxRarity = randomItemsToSpawn[^1].RarityKey;
                    var randomRarity = _systemRandom.NextInt(maxRarity + 1);
                    var entityToSpawn = Entity.Null;

                    for (var i = 0; i < randomItemsToSpawn.Length; i++)
                    {
                        if (randomRarity < randomItemsToSpawn[i].RarityKey)
                        {
                            entityToSpawn = randomItemsToSpawn[i].Entity;
                            break;
                        }
                    }

                    if (SystemAPI.Exists(entityToSpawn))
                    {
                        var entityToSpawnScale = SystemAPI.GetComponent<LocalTransform>(entityToSpawn).Scale;
                        var newEntity = beginFrameECB.Instantiate(entityToSpawn);
                        var entityTransform = SystemAPI.GetComponent<LocalTransform>(entity);
                        entityTransform = entityTransform.ApplyScale(entityToSpawnScale);
                        beginFrameECB.SetComponent(newEntity, entityTransform);
                    }
                }

                if (SystemAPI.HasComponent<StatModifierEntityTag>(entity))
                {
                    var characterEntity = SystemAPI.GetComponent<CharacterEntity>(entity).Value;
                    SystemAPI.SetComponentEnabled<RecalculateStatsFlag>(characterEntity, true);
                }

                endFrameECB.DestroyEntity(entity);
            }
        }
    }
}