using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与敌人生成相关的数据。此数据特定于一个关卡，在特定关卡的游戏过程中将保持不变。
    /// </summary>
    /// <seealso cref="EnemySpawnSystem"/>
    public struct EnemySpawnData : IComponentData
    {
        /// <summary>
        /// 游戏为特定生成波生成敌人的时间（秒）。
        /// </summary>
        public float WaveInterval;
        /// <summary>
        /// 敌人将在屏幕外以距相机边界随机距离生成。此值定义了敌人生成的距相机边界的最小距离。
        /// </summary>
        public float MinSpawnDistanceFromCameraBounds;
        /// <summary>
        /// 敌人将在屏幕外以距相机边界随机距离生成。此值定义了敌人生成的距相机边界的最大距离。
        /// </summary>
        public float MaxSpawnDistanceFromCameraBounds;
        /// <summary>
        /// 最终boss是在所有波次完成后生成的boss。
        /// </summary>
        /// <remarks>
        /// 最终boss的生命值和伤害值都非常高，基本上不可能被摧毁，并且会在第一次接触时立即摧毁玩家。
        /// 在最终boss生成之前，世界上的所有其他敌人都会被摧毁。
        /// </remarks>
        public EnemyType FinalBossType;

        /// <summary>
        /// 获取屏幕外随机生成位置的辅助函数。
        /// </summary>
        /// <param name="random">用于生成随机位置。</param>
        /// <param name="cameraPosition">相机的中心点。</param>
        /// <param name="cameraHalfExtents">相机视空间的半尺寸。</param>
        /// <returns></returns>
        public float3 GetRandomSpawnPos(RefRW<EntityRandom> random, float3 cameraPosition, float3 cameraHalfExtents)
        {
            // Pick a random side of the screen to spawn off of.
            var randomSideIndex = random.ValueRW.Value.NextInt(0, 4);

            var minSpawnPosition = float3.zero;
            var maxSpawnPosition = float3.zero;

            // Determine the minimum and maximum position for the chosen random side.
            switch (randomSideIndex)
            {
                case 0:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MaxSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MinSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MaxSpawnDistanceFromCameraBounds,
                    };
                    break;
                case 1:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MinSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MaxSpawnDistanceFromCameraBounds,
                    };
                    break;
                case 2:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MinSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MaxSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MaxSpawnDistanceFromCameraBounds,
                    };
                    break;
                case 3:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MaxSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MinSpawnDistanceFromCameraBounds,
                    };
                    break;
            }

            // Return a random position inside the minimum and maximum positions for the given side.
            var newSpawnPos = random.ValueRW.Value.NextFloat3(minSpawnPosition, maxSpawnPosition);
            return newSpawnPos;
        }
    }

    /// <summary>
    /// 与当前活动的生成波相关的数据。此数据将在每个生成波更改时更新。
    /// </summary>
    /// <seealso cref="EnemySpawnSystem"/>
    /// <seealso cref="EnemySpawnerState"/>
    public struct EnemySpawnWaveData : IComponentData
    {
        /// <summary>
        /// 定义此波激活时将存在的最小敌人数量。如果世界中特定生成波的敌人少于最小数量，则将在1帧内生成满足此最小值的敌人数量。
        /// </summary>
        public int MinEnemyCount;
        /// <summary>
        /// 如果满足此波的最小敌人计数，将按此值定义的时间间隔生成额外的敌人。一旦 <see cref="EnemySpawnerState.WaveTimer"/> 达到0，将生成存储在 <see cref="SpawnWaveEnemyPrefab"/> 动态缓冲区中的每种类型的一个敌人。
        /// </summary>
        public float SpawnInterval;
    }

    /// <summary>
    /// 用于容纳当前波中将生成的所有敌人预制体的动态缓冲区。
    /// </summary>
    /// <remarks>
    /// 请注意，此缓冲区不存储boss敌人或属于生成事件的敌人的预制件。
    /// Boss敌人是在波数开始时实例化的，因此无需保留预制件引用。
    /// 生成事件当前生成单一类型的敌人，其预制件保存在 <see cref="SpawnEvent"/> 动态缓冲区中。
    /// </remarks>
    public struct SpawnWaveEnemyPrefab : IBufferElementData
    {
        public Entity Value;
    }

    /// <summary>
    /// 用于容纳给定阶段的所有 <see cref="EnemySpawnWaveProperties"/> ScriptableObject 的动态缓冲区组件。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="EnemySpawnSystem"/> 中，当要选择新一波时，将引用缓冲区的下一个元素，并使用运行时友好的非托管数据填充相应的数据组件。索引在 <see cref="EnemySpawnerState"/> 中跟踪。
    /// </remarks>
    public struct EnemySpawnWave : IBufferElementData
    {
        public UnityObjectRef<EnemySpawnWaveProperties> Value;
    }

    /// <summary>
    /// 容纳敌人生成器的当前状态数据。
    /// </summary>
    /// <seeaslo cref="EnemySpawnSystem"/>
    public struct EnemySpawnerState : IComponentData
    {
        /// <summary>
        /// 与当前波关联的索引，该索引保存在 <see cref="EnemySpawnWaveProperties"/> ScriptableObject 的 <see cref="EnemySpawnWave.Value"/> 托管数组中。
        /// </summary>
        public int CurWaveIndex;
        /// <summary>
        /// 当前波的计时器。在每波开始时，此值初始化为 <see cref="EnemySpawnData.WaveInterval"/>，每帧该值递减delta time，一旦计时器达到0，下一波应开始。
        /// </summary>
        public float WaveTimer;
    }

    /// <summary>
    /// 通知 <see cref="EnemySpawnSystem"/> 何时为给定波次生成下一批敌人的计时器。
    /// </summary>
    public struct EnemySpawnTimer : IComponentData
    {
        /// <summary>
        /// 计时器值初始化为 <see cref="EnemySpawnWaveData.SpawnInterval"/>，每帧该值递减delta time，一旦计时器达到0，将生成该波的下一批敌人。
        /// </summary>
        public float Value;
    }

    /// <summary>
    /// 包含与当前波的生成事件相关信息的动态缓冲区。
    /// </summary>
    /// <remarks>
    /// 在每波开始时，此动态缓冲区会被清空，并用 <see cref="EnemySpawnWaveProperties.SpawnEvents"/> 数组中每个 <see cref="EnemySpawnEventProperties"/> 元素的运行时数据填充。
    /// 此动态缓冲区包含与可能发生的任何生成事件相关的所有数据，因此并非所有字段都与所有生成事件相关。
    /// </remarks>
    [InternalBufferCapacity(4)]
    public struct SpawnEvent : IBufferElementData
    {
        /// <summary>
        /// 用于确定此生成事件是否已发生的标志。
        /// </summary>
        public bool HasOccurred;
        /// <summary>
        /// 将在此生成事件中生成的敌人类型的实体预制体。
        /// </summary>
        public Entity EnemyPrefab;
        /// <summary>
        /// 敌人生成的排列方式。
        /// </summary>
        public SpawnFormation SpawnFormation;
        /// <summary>
        /// 将在此生成事件期间生成的敌人数量。
        /// </summary>
        public int EnemyCount;
        /// <summary>
        /// 延迟时间是从波开始到生成事件发生的时间（秒）。事件设计者需要确保所有事件都在生成事件的持续时间内发生，因为在生成事件结束后发生的事件将不会发生。
        /// </summary>
        public float DelayTime;
        /// <summary>
        /// 事件发生的百分比几率，从0到100。值为0表示生成事件永远不会发生，值为100表示事件总是会发生。
        /// </summary>
        public int ChanceOfOccurrence;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义敌人之间的间距。
        /// </summary>
        public float EnemySpacing;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义给定敌人在被销毁前的存活时间。
        /// </summary>
        public float TimeToLive;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义线性移动敌人 (<see cref="EnemyLinearMovement"/>) 和正弦波敌人 (<see cref="EnemySineWaveMovement"/>) 的恒定移动速度。
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义敌人正弦波运动的周期。<see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Period;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义敌人正弦波运动的振幅。<see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// 用于辅助创建SpawnEvent元素的构造函数。
        /// </summary>
        /// <param name="properties">生成事件的创作数据。</param>
        /// <param name="wavePropertiesProperties">生成波的创作数据。</param>
        /// <param name="spawnEventIndex">要添加的此生成事件的索引。用于计算延迟时间。</param>
        /// <param name="enemyPrefab">为此事件生成的敌人的实体预制体。</param>
        public SpawnEvent(EnemySpawnEventProperties properties, EnemySpawnWaveProperties wavePropertiesProperties, int spawnEventIndex, Entity enemyPrefab)
        {
            HasOccurred = false;
            EnemyPrefab = enemyPrefab;
            SpawnFormation = properties.SpawnFormation;
            EnemyCount = properties.EnemyCount;
            DelayTime = wavePropertiesProperties.DelayTime * (spawnEventIndex + 1);
            ChanceOfOccurrence = wavePropertiesProperties.ChanceOfOccurrence;
            EnemySpacing = properties.EnemySpacing;
            TimeToLive = properties.TimeToLive;
            MoveSpeed = properties.MoveSpeed;
            Period = properties.Period;
            Amplitude = properties.Amplitude;
        }
    }

    /// <summary>
    /// 用于存储与当前阶段关联的 <see cref="StageProperties"/> ScriptableObject 的 Unity 对象引用的数据组件。
    /// </summary>
    public struct StagePropertiesReference : IComponentData
    {
        public UnityObjectRef<StageProperties> Value;
    }

    /// <summary>
    /// 此创作组件应包含与给定关卡的阶段属性相关的所有数据，例如哪些敌人在哪些波中生成、此阶段有多少波、每波持续多长时间等。
    /// 此组件用于保存对 <see cref="StageProperties"/> ScriptableObject 的托管引用。在运行时，<see cref="EnemyWaveSystem"/> 将使用此托管数据来填充非托管数据组件和动态缓冲区以方便生成。
    /// </summary>
    /// <remarks>
    /// 此创作脚本的 <see cref="Baker"/> 依赖于 <see cref="StageProperties"/> ScriptableObject，因此如果 ScriptableObject 上的任何值被更改，烘焙过程将重新运行。
    /// </remarks>
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class EnemySpawnAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 包含与当前阶段相关信息的ScriptableObject，特别是生成波之间的间隔和 <see cref="EnemySpawnWaveProperties"/> 数组
        /// </summary>
        /// <remarks>
        /// 此组件上的值将覆盖 <see cref="StageProperties"/> ScriptableObject 上设置的任何值。
        /// </remarks>
        public StageProperties StageProperties;
        /// <summary>
        /// 敌人将在屏幕外以距相机边界随机距离生成。此值定义了敌人生成的距相机边界的最小距离。
        /// </summary>
        public float MinSpawnDistanceFromCameraBounds;
        /// <summary>
        /// 敌人将在屏幕外以距相机边界随机距离生成。此值定义了敌人生成的距相机边界的最大距离。
        /// </summary>
        public float MaxSpawnDistanceFromCameraBounds;

        private class Baker : Baker<EnemySpawnAuthoring>
        {
            public override void Bake(EnemySpawnAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                DependsOn(authoring.StageProperties);
                AddComponent(entity, new EnemySpawnData
                {
                    WaveInterval = authoring.StageProperties.WaveInterval,
                    MinSpawnDistanceFromCameraBounds = authoring.MinSpawnDistanceFromCameraBounds,
                    MaxSpawnDistanceFromCameraBounds = authoring.MaxSpawnDistanceFromCameraBounds,
                    FinalBossType = authoring.StageProperties.FinalBossType
                });
                AddComponent<EnemySpawnTimer>(entity);
                AddComponent<EnemySpawnWaveData>(entity);
                AddBuffer<SpawnWaveEnemyPrefab>(entity);
                AddComponent(entity, new EnemySpawnerState
                {
                    // Starts at -1 as this will immediately be incremented to 0 to spawn the first wave
                    CurWaveIndex = -1,
                    WaveTimer = 0f
                });
                var spawnWaveBuffer = AddBuffer<EnemySpawnWave>(entity);
                foreach (var spawnWave in authoring.StageProperties.EnemySpawnWaves)
                {
                    spawnWaveBuffer.Add(new EnemySpawnWave { Value = spawnWave });
                }
                AddBuffer<SpawnEvent>(entity);
                AddComponent(entity, new StagePropertiesReference { Value = authoring.StageProperties });
            }
        }
    }

    /// <summary>
    /// 此系统负责更新当前波次索引并为 <see cref="EnemySpawnSystem"/> 设置所需的数据组件。
    /// 它还将执行一次性操作，例如在最终boss波之前生成boss实体和清理现有实体。
    /// </summary>
    /// <remarks>
    /// 此系统设计为在很大程度上是非托管的，以便可以进行Burst编译。因此，任何来自ScriptableObjects的托管数据都必须在运行时转换为非托管数据。在这种情况下，迭代 <see cref="EnemySpawnWaveProperties"/> 以填充 <see cref="EnemySpawnWaveData"/> 组件以及 <see cref="SpawnWaveEnemyPrefab"/> 和 <see cref="SpawnEvent"/> 动态缓冲区。
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EnemySpawnSystem))]
    public partial struct EnemyWaveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerExperienceState>();
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<GameEntityTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var gameControllerEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
            var gamePrefabs = SystemAPI.GetComponent<GameEntityPrefabs>(gameControllerEntity);
            var enemyEntityPrefabLookup = state.EntityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).EnemyEntityPrefabLookup;
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;

            // This foreach loop determines when a new wave should be active. Once the target time to change waves has been reached, this loop will assign the appropriate values in the data components.
            foreach (var (spawnerState, spawnData, spawnWaveData, spawnWavePrefabs, spawnEvents, spawnWaves, random, entity) in SystemAPI.Query<RefRW<EnemySpawnerState>, EnemySpawnData, RefRW<EnemySpawnWaveData>, DynamicBuffer<SpawnWaveEnemyPrefab>, DynamicBuffer<SpawnEvent>, DynamicBuffer<EnemySpawnWave>, RefRW<EntityRandom>>().WithEntityAccess())
            {
                // Determine when to advance to the next wave.
                spawnerState.ValueRW.WaveTimer -= deltaTime;
                if (spawnerState.ValueRO.WaveTimer > 0f) continue;
                spawnerState.ValueRW.WaveTimer = spawnData.WaveInterval;

                // Increment the wave index.
                spawnerState.ValueRW.CurWaveIndex += 1;

                // Once the final wave has spawned, remove the wave status component to avoid attempting to spawn a subsequent waves that do not exist.
                // Also, instantly destroy all existing enemies so final boss enemy is the only one on screen. Before destruction is queued, any enemies that have a destruction effect (all enemies should), set up the graphic dissolve effect.
                // A native array of enemies is created to ensure the InstantDestroyTag is only added to entities currently spawned and not entities still queued for instantiation inside the entity command buffer i.e. the final boss entity. This is because the default behavior of EntityCommandBuffer operations on EntityQueries is to evaluate the query at playback time.
                // The final boss will also spawn here.
                if (spawnerState.ValueRO.CurWaveIndex >= spawnWaves.Length)
                {
                    ecb.RemoveComponent<EnemySpawnerState>(entity);

                    foreach (var graphicsEntity in SystemAPI.Query<GraphicsEntity>().WithAll<GraphicsEntityPlayDestroyEffectTag, EnemyTag>())
                    {
                        var newGraphicsEntity = ecb.Instantiate(graphicsEntity.Value);
                        ecb.RemoveComponent<LocalTransform>(newGraphicsEntity);
                        ecb.AddComponent(newGraphicsEntity, new DissolveData
                        {
                            StartTimestamp = (float)SystemAPI.Time.ElapsedTime,
                            Duration = 0.4f
                        });
                    }

                    var enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build();
                    var enemies = enemyQuery.ToEntityArray(state.WorldUpdateAllocator);
                    ecb.AddComponent<InstantDestroyTag>(enemies);

                    if (spawnData.FinalBossType == EnemyType.None)
                    {
                        Debug.LogError("Error: Final boss type is set to 'None' in the current stage's StageProperties ScriptableObject. Thus no final boss will spawn and the game won't end on its own. Assign a final boss type in StageProperties.FinalBossEnemyType in this stage's StageProperties ScriptableObject.");
                        continue;
                    }

                    var finalBossPrefab = enemyEntityPrefabLookup[spawnData.FinalBossType];
                    var finalBossEntity = ecb.Instantiate(finalBossPrefab);
                    var finalBossSpawnPosition = spawnData.GetRandomSpawnPos(random, cameraPosition, cameraHalfExtents);
                    var finalBossTransform = LocalTransform.FromPosition(finalBossSpawnPosition);
                    ecb.SetComponent(finalBossEntity, finalBossTransform);
                    ecb.AddComponent<EnemyMoveToPlayerTag>(finalBossEntity);
                    ecb.AddComponent<InitializeEnhancementMaterialFlag>(finalBossEntity);

                    const int finalBossHitPoints = 1_000_000;
                    ecb.SetComponent(finalBossEntity, new CurrentHitPoints { Value = finalBossHitPoints });
                    ecb.SetComponent(finalBossEntity, new BaseHitPoints { Value = finalBossHitPoints });

                    const int finalBossDamage = 1_000_000;
                    ecb.SetComponent(finalBossEntity, new EnemyAttackData
                    {
                        HitPoints = finalBossDamage,
                        CooldownTime = 0.1f
                    });

                    ecb.SetComponent(finalBossEntity, new DropExperienceOnDestroy
                    {
                        ExperienceValue = 0,
                        ChanceToDrop = 0,
                    });

                    ecb.AddComponent<EnemyBlackHoleResistTag>(finalBossEntity);

                    ecb.SetComponent(finalBossEntity, new CharacterBaseMoveSpeed
                    {
                        Value = 20f
                    });

                    continue;
                }

                // Get properties for the new wave and populate data for the current wave.
                var curWaveProperties = spawnWaves[spawnerState.ValueRO.CurWaveIndex].Value.Value;
                spawnWaveData.ValueRW = new EnemySpawnWaveData
                {
                    MinEnemyCount = curWaveProperties.MinEnemyCount,
                    SpawnInterval = curWaveProperties.SpawnInterval
                };

                // Store prefab references for all enemies to be spawned in the current wave.
                spawnWavePrefabs.Clear();
                foreach (var enemyType in curWaveProperties.EnemyTypes)
                {
                    if (enemyType == EnemyType.None) continue;
                    var enemyPrefab = enemyEntityPrefabLookup[enemyType];
                    spawnWavePrefabs.Add(new SpawnWaveEnemyPrefab { Value = enemyPrefab });
                }

                // Populate spawnEvents Dynamic Buffer with information pertaining to spawn events for the current wave.
                spawnEvents.Clear();
                var spawnEventIndex = 0;
                foreach (var spawnEvent in curWaveProperties.SpawnEvents)
                {
                    if (spawnEvent.EnemyType == EnemyType.None) continue;
                    var enemyPrefab = enemyEntityPrefabLookup[spawnEvent.EnemyType];
                    spawnEvents.Add(new SpawnEvent(spawnEvent, curWaveProperties, spawnEventIndex, enemyPrefab));
                    spawnEventIndex += 1;
                }

                // If the current wave has a boss, spawn it at the start of the new wave. Boss enemies have more health and the enhanced material effect.
                if (curWaveProperties.BossType != EnemyType.None)
                {
                    var bossPrefab = enemyEntityPrefabLookup[curWaveProperties.BossType];
                    var newBossEntity = ecb.Instantiate(bossPrefab);
                    var bossSpawnPosition = spawnData.GetRandomSpawnPos(random, cameraPosition, cameraHalfExtents);
                    var bossTransform = LocalTransform.FromPosition(bossSpawnPosition);
                    ecb.SetComponent(newBossEntity, bossTransform);
                    ecb.AddComponent<EnemyMoveToPlayerTag>(newBossEntity);
                    ecb.AddComponent<InitializeEnhancementMaterialFlag>(newBossEntity);

                    var baseHealth = curWaveProperties.BossBaseHitPoints;
                    ecb.SetComponent(newBossEntity, new CurrentHitPoints { Value = baseHealth });
                    ecb.SetComponent(newBossEntity, new BaseHitPoints { Value = baseHealth });
                    ecb.AddComponent<EnemyHitPointsScaleWithPlayerLevelTag>(newBossEntity);
                    ecb.SetComponent(newBossEntity, new DropExperienceOnDestroy
                    {
                        ExperienceValue = curWaveProperties.BossExperiencePoints,
                        ChanceToDrop = 100,
                    });

                    var shouldDropCrate = random.ValueRW.Value.NextInt(100) <= curWaveProperties.BossChanceToDropCrate;
                    if (shouldDropCrate)
                    {
                        var cratePrefab = gamePrefabs.CratePrefab;
                        var spawnCrateOnDestroy = new SpawnOnDestroy { Value = cratePrefab };
                        ecb.AddComponent(newBossEntity, spawnCrateOnDestroy);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary>
    /// 此系统负责根据 <see cref="EnemySpawnData"/> 和 <see cref="EnemySpawnWaveData"/> 组件以及 <see cref="SpawnWaveEnemyPrefab"/> 和 <see cref="SpawnEvent"/> 动态缓冲区中设置的数据在游戏世界中生成敌人。
    /// 生成主要通过两种方式处理，一种是通过生成事件，另一种是基于计时器的持续生成流。
    /// </summary>
    /// <remarks>
    /// 此系统设计为在很大程度上是非托管的，以便可以进行Burst编译。因此，任何来自ScriptableObjects的托管数据都必须在运行时转换为非托管数据。
    /// 此系统必须在 <see cref="EnemyWaveSystem"/> 之后运行，因为该系统负责填充此系统执行所需的数据组件。
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(CharacterInitializationSystem))]
    public partial struct EnemySpawnSystem : ISystem
    {
        /// <summary>
        /// 所有标记有 <see cref="EnemyTag"/> 的实体的查询，以获取当前生成的所有敌人的计数，以便系统知道需要生成多少敌人才能达到当前波的最小生成计数。
        /// </summary>
        private EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerExperienceState>();
            state.RequireForUpdate<CameraTarget>();
            _enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var enemyCount = _enemyQuery.CalculateEntityCount();

            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;

            // This foreach loop handles all the spawn events for a given wave and spawns enemies at the appropriate time.
            foreach (var (spawnEvents, random, spawnData, spawnerState) in SystemAPI.Query<DynamicBuffer<SpawnEvent>, RefRW<EntityRandom>, EnemySpawnData, EnemySpawnerState>())
            {
                var sharedSpawnIndex = new WaveIndexOfSpawn { Value = spawnerState.CurWaveIndex };

                // Iterate all spawn events for the current wave.
                for (var i = 0; i < spawnEvents.Length; i++)
                {
                    // If the spawn event has already occurred, continue to the next.
                    var curSpawnEvent = spawnEvents.ElementAt(i);
                    if (curSpawnEvent.HasOccurred) continue;

                    // Decrement the delay time for the spawn event, if the timer is at or below 0, execute the spawn event.
                    curSpawnEvent.DelayTime -= deltaTime;
                    if (curSpawnEvent.DelayTime <= 0f)
                    {
                        // Determine if the spawn event should spawn enemies based on random chance.
                        var randomChance = random.ValueRW.Value.NextInt(0, 101);
                        if (randomChance < curSpawnEvent.ChanceOfOccurrence)
                        {

                            // Handle different spawn formations.
                            switch (curSpawnEvent.SpawnFormation)
                            {
                                case SpawnFormation.LinearMoveGroup:
                                    var centerPosition = spawnData.GetRandomSpawnPos(random, cameraPosition, cameraHalfExtents);
                                    var directionToPlayer = math.normalize(cameraPosition - centerPosition);
                                    var angleToPlayer = math.atan2(directionToPlayer.x, directionToPlayer.z);
                                    var enemyLinearMovement = new EnemyLinearMovement { Angle = angleToPlayer };
                                    var characterMoveSpeed = new CharacterBaseMoveSpeed { Value = curSpawnEvent.MoveSpeed };

                                    var sideLength = (int)math.ceil(math.sqrt(curSpawnEvent.EnemyCount));
                                    for (var x = 0; x < sideLength; x++)
                                    {
                                        for (var y = 0; y < sideLength; y++)
                                        {
                                            var spawnPosition = centerPosition + new float3(x * curSpawnEvent.EnemySpacing, 0f, y * curSpawnEvent.EnemySpacing);
                                            var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                            ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                            ecb.SetComponent(newEnemy, characterMoveSpeed);
                                            ecb.AddComponent(newEnemy, enemyLinearMovement);
                                            ecb.AddComponent<SetEnemyLinearMovementFlag>(newEnemy);
                                            ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                        }
                                    }
                                    break;

                                case SpawnFormation.EllipseAroundView:
                                    var a = 1.5f * cameraHalfExtents.x;
                                    var b = 1.5f * cameraHalfExtents.z;

                                    var destroyAfterTime = new DestroyAfterTime { Value = curSpawnEvent.TimeToLive };

                                    for (var spawnIndex = 0; spawnIndex < curSpawnEvent.EnemyCount; spawnIndex++)
                                    {
                                        var angle = math.TAU * spawnIndex / curSpawnEvent.EnemyCount;
                                        var x = a * math.cos(angle);
                                        var y = b * math.sin(angle);
                                        var spawnPosition = cameraPosition + new float3(x, 0f, y);
                                        var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                        ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                        var ellipseMoveSpeed = new CharacterBaseMoveSpeed { Value = curSpawnEvent.MoveSpeed };
                                        ecb.SetComponent(newEnemy, ellipseMoveSpeed);
                                        var ellipseDirectionToPlayer = math.normalize(cameraPosition - spawnPosition);
                                        var ellipseAngleToPlayer = math.atan2(ellipseDirectionToPlayer.x, ellipseDirectionToPlayer.z);
                                        ecb.AddComponent(newEnemy, new EnemyLinearMovement { Angle = ellipseAngleToPlayer });
                                        ecb.AddComponent<SetEnemyLinearMovementFlag>(newEnemy);
                                        ecb.AddComponent(newEnemy, destroyAfterTime);
                                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                    }

                                    break;

                                case SpawnFormation.SineMoveVerticalLine:
                                    var isLeftSide = random.ValueRW.Value.NextBool();
                                    var verticalX = isLeftSide ? cameraPosition.x - cameraHalfExtents.x - spawnData.MinSpawnDistanceFromCameraBounds : cameraPosition.x + cameraHalfExtents.x + spawnData.MinSpawnDistanceFromCameraBounds;
                                    var verticalLineMoveDirection = isLeftSide ? SineWaveMoveDirection.Right : SineWaveMoveDirection.Left;
                                    var verticalSineWaveMovement = new EnemySineWaveMovement
                                    {
                                        MoveDirection = verticalLineMoveDirection,
                                        ConstantMoveSpeed = curSpawnEvent.MoveSpeed,
                                        Period = curSpawnEvent.Period,
                                        Amplitude = curSpawnEvent.Amplitude
                                    };

                                    for (var spawnIndex = 0; spawnIndex < curSpawnEvent.EnemyCount; spawnIndex++)
                                    {
                                        var verticalY = 1f / (curSpawnEvent.EnemyCount + 1) * (spawnIndex + 1) * cameraHalfExtents.z * 2;
                                        verticalY += cameraPosition.z - cameraHalfExtents.z;
                                        var spawnPosition = new float3(verticalX, 0f, verticalY);
                                        var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                        ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                        ecb.AddComponent(newEnemy, verticalSineWaveMovement);
                                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                    }
                                    break;

                                case SpawnFormation.SineMoveHorizontalLine:
                                    var isBottomSide = random.ValueRW.Value.NextBool();
                                    var horizontalY = isBottomSide ? cameraPosition.z - cameraHalfExtents.z - spawnData.MinSpawnDistanceFromCameraBounds : cameraPosition.z + cameraHalfExtents.z + spawnData.MinSpawnDistanceFromCameraBounds;
                                    var horizontalLineMoveDirection = isBottomSide ? SineWaveMoveDirection.Up : SineWaveMoveDirection.Down;
                                    var horizontalSineWaveMovement = new EnemySineWaveMovement
                                    {
                                        MoveDirection = horizontalLineMoveDirection,
                                        ConstantMoveSpeed = curSpawnEvent.MoveSpeed,
                                        Period = curSpawnEvent.Period,
                                        Amplitude = curSpawnEvent.Amplitude
                                    };

                                    for (var spawnIndex = 0; spawnIndex < curSpawnEvent.EnemyCount; spawnIndex++)
                                    {
                                        var horizontalX = 1f / (curSpawnEvent.EnemyCount + 1) * (spawnIndex + 1) * cameraHalfExtents.x * 2;
                                        horizontalX += cameraPosition.x - cameraHalfExtents.x;
                                        var spawnPosition = new float3(horizontalX, 0f, horizontalY);
                                        var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                        ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                        ecb.AddComponent(newEnemy, horizontalSineWaveMovement);
                                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                    }
                                    break;

                                default:
                                    Debug.LogWarning($"Warning: attempting to spawn enemy spawn formation: {curSpawnEvent.SpawnFormation} which has no definition of how spawn is structured");
                                    break;
                            }
                        }

                        curSpawnEvent.HasOccurred = true;
                    }

                    spawnEvents.ElementAt(i) = curSpawnEvent;
                }
            }

            // This foreach loop handles spawning of regular enemies in the spawn wave.
            foreach (var (spawnTimer, random, spawnData, spawnWaveData, spawnWavePrefabs, spawnerState) in SystemAPI.Query<RefRW<EnemySpawnTimer>, RefRW<EntityRandom>, EnemySpawnData, EnemySpawnWaveData, DynamicBuffer<SpawnWaveEnemyPrefab>, EnemySpawnerState>())
            {
                // If there are no prefabs to spawn, continue.
                if (spawnWavePrefabs.IsEmpty) continue;

                var sharedSpawnIndex = new WaveIndexOfSpawn { Value = spawnerState.CurWaveIndex };

                // Determine when the next batch of enemies should be spawned.
                spawnTimer.ValueRW.Value -= deltaTime;
                if (spawnTimer.ValueRO.Value > 0f) continue;
                spawnTimer.ValueRW.Value += spawnWaveData.SpawnInterval;

                // Spawn one enemy of each type for the current spawn wave until the minimum enemy count for the current wave is reached.
                do
                {
                    foreach (var enemyTypePrefab in spawnWavePrefabs)
                    {
                        var newSpawnPos = spawnData.GetRandomSpawnPos(random, cameraPosition, cameraHalfExtents);
                        var entityTransform = LocalTransform.FromPosition(newSpawnPos);

                        var newEnemy = ecb.Instantiate(enemyTypePrefab.Value);
                        ecb.SetComponent(newEnemy, entityTransform);
                        ecb.AddComponent<EnemyMoveToPlayerTag>(newEnemy);
                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);

                        enemyCount += 1;
                    }
                } while (enemyCount < spawnWaveData.MinEnemyCount);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}