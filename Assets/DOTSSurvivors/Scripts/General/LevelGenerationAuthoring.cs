using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Random = Unity.Mathematics.Random;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与关卡生成相关的数据。
    /// </summary>
    /// <seealso cref="LevelGenerationSystem"/>
    public struct LevelGenerationData : IComponentData
    {
        /// <summary>
        /// 背景实体（地面）的实体预制件。
        /// </summary>
        public Entity BackgroundPrefab;
        /// <summary>
        /// 用于在游戏区域边缘显示的警示带图形实体的实体预制件。
        /// </summary>
        public Entity CautionPrefab;
        /// <summary>
        /// 用于确定背景预制件上每个背景瓦片单位数量的缩放因子。
        /// </summary>
        /// <remarks>
        /// 请注意，背景实体是整个游戏区域的单个四边形网格，平铺在着色器中通过 <see cref="TileScaleOverride"/> 组件设置。
        /// </remarks>
        public float UnitsPerBackgroundTile;
        /// <summary>
        /// 关卡在以原点为中心的正方形中生成，其长度和高度由此关卡大小定义。
        /// </summary>
        public float LevelSize;
        /// <summary>
        /// 在游戏区域中生成的装饰和环境物品的数量。
        /// </summary>
        public int ItemCount;
        /// <summary>
        /// 每个关卡元素都有一个稀有度组件，以影响其在世界中生成的频率。此值为所有关卡元素上所有稀有度值的总和，用于随机数生成以确定接下来应生成哪个关卡元素。
        /// </summary>
        public int MaxRarity;
    }

    /// <summary>
    /// 关卡元素的动态缓冲区数据。关卡元素是在关卡中生成的装饰和环境实体。
    /// </summary>
    /// <seealso cref="DecorationTag"/>
    /// <seealso cref="EnvironmentTag"/>
    public struct LevelElementData : IBufferElementData
    {
        /// <summary>
        /// 可通过关卡生成生成的实体预制件。
        /// </summary>
        public Entity Value;
        /// <summary>
        /// 此物品生成频率的稀有度值。
        /// </summary>
        /// <remarks>
        /// 稀有度为0的物品永远不会被选中生成，而稀有度为100的物品被选中生成的可能性最高。
        /// </remarks>
        public int Rarity;
        /// <summary>
        /// 生成此实体时设置的最小缩放值。
        /// </summary>
        /// <remarks>
        /// 在生成期间，将在最小值和最大值之间选择一个随机缩放值，以增加生成的随机性。
        /// </remarks>
        public float MinScale;
        /// <summary>
        /// 生成此实体时设置的最大缩放值。
        /// </summary>
        /// <remarks>
        /// 在生成期间，将在最小值和最大值之间选择一个随机缩放值，以增加生成的随机性。
        /// </remarks>
        public float MaxScale;
    }

    /// <summary>
    /// 一个可启用的组件，用于通知 <see cref="LevelGenerationSystem"/> 应该重新生成关卡。
    /// </summary>
    public struct RegenerateLevelFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于在 Unity 编辑器中创作一组关卡元素的数据结构。
    /// </summary>
    [System.Serializable]
    public struct LevelElementInfo
    {
        /// <summary>
        /// 将由关卡生成生成的实体的 GameObject 预制件。
        /// </summary>
        public GameObject Prefab;
        /// <summary>
        /// 此物品生成频率的稀有度值。
        /// </summary>
        /// <remarks>
        /// 稀有度为0的物品永远不会被选中生成，而稀有度为100的物品被选中生成的可能性最高。
        /// </remarks>
        [Range(0, 100)]
        public int Rarity;
        /// <summary>
        /// 生成此实体时设置的最小缩放值。
        /// </summary>
        /// <remarks>
        /// 在生成期间，将在最小值和最大值之间选择一个随机缩放值，以增加生成的随机性。
        /// </remarks>
        public float MinScale;
        /// <summary>
        /// 生成此实体时设置的最大缩放值。
        /// </summary>
        /// <remarks>
        /// 在生成期间，将在最小值和最大值之间选择一个随机缩放值，以增加生成的随机性。
        /// </remarks>
        public float MaxScale;
    }

    /// <summary>
    /// 用于设置瓦片缩放因子的材质属性覆盖。
    /// </summary>
    /// <remarks>
    /// 您的IDE可能会将Value字段灰显，因为我们的代码中并未使用此值。但是，Unity使用它将此值应用于MaterialProperty属性中定义的材质属性。
    /// 请确保MaterialProperty字符串与着色器中定义的引用字符串完全匹配，因为如果存在拼写错误，这将静默失败。
    /// </remarks>
    [MaterialProperty("_TileScale")]
    public struct TileScaleOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于存储游戏区域安全边界的数据组件。
    /// </summary>
    /// <remarks>
    /// 由 <see cref="DamagePlayerOutsideSafeBoundsSystem"/> 使用，当玩家离开游戏区域时对其造成伤害。
    /// </remarks>
    public struct LevelSafeBounds : IComponentData
    {
        public float2 MinPosition;
        public float2 MaxPosition;

        public bool InsideSafeBounds(float2 testPosition)
        {
            return testPosition.x > MinPosition.x && testPosition.x < MaxPosition.x && testPosition.y > MinPosition.y && testPosition.y < MaxPosition.y;
        }
    }

    /// <summary>
    /// 用于添加关卡生成所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// 该脚本使用OnValidate方法来确定编辑器中种子值的更改时间，这将触发关卡的重新生成。此重新生成在播放模式内外都会发生，因为 <see cref="LevelGenerationSystem"/> 设置为在编辑器和默认世界中都更新。
    /// </remarks>
    public class LevelGenerationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 用于生成关卡数据的随机种子。
        /// </summary>
        public uint Seed;
        /// <summary>
        /// 将生成的背景实体的 GameObject 预制件。
        /// </summary>
        public GameObject BackgroundPrefab;
        /// <summary>
        /// 将在游戏区域边缘生成的警示带实体的 GameObject 预制件。
        /// </summary>
        public GameObject CautionPrefab;
        /// <summary>
        /// 用于确定背景预制件上每个背景瓦片单位数量的缩放因子。
        /// </summary>
        /// <remarks>
        /// 请注意，背景实体是整个游戏区域的单个四边形网格，平铺在着色器中通过 <see cref="TileScaleOverride"/> 组件设置。
        /// </remarks>
        public float UnitsPerBackgroundTile;
        /// <summary>
        /// 关卡在以原点为中心的正方形中生成，其长度和高度由此关卡大小定义。
        /// </summary>
        public float LevelSize;
        /// <summary>
        /// 存储有关在关卡生成期间要生成的实体的数据的关卡元素集合。
        /// </summary>
        /// <seealso cref="LevelElementData"/>
        public LevelElementInfo[] LevelElements;
        /// <summary>
        /// 在游戏区域中生成的装饰和环境物品的数量。
        /// </summary>
        public int ItemCount;

        /// <summary>
        /// 在 OnValidate 中使用，以确定种子值何时更改以及何时应进行关卡生成。
        /// </summary>
        private uint _prevSeed;

        private class Baker : Baker<LevelGenerationAuthoring>
        {
            public override void Bake(LevelGenerationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var levelPrefabs = AddBuffer<LevelElementData>(entity);
                var maxRarity = 0;
                foreach (var levelElement in authoring.LevelElements)
                {
                    maxRarity += levelElement.Rarity;
                    levelPrefabs.Add(new LevelElementData
                    {
                        Value = GetEntity(levelElement.Prefab, TransformUsageFlags.Dynamic),
                        Rarity = maxRarity,
                        MinScale = levelElement.MinScale,
                        MaxScale = levelElement.MaxScale
                    });
                }

                AddComponent(entity, new LevelGenerationData
                {
                    BackgroundPrefab = GetEntity(authoring.BackgroundPrefab, TransformUsageFlags.Dynamic),
                    CautionPrefab = GetEntity(authoring.CautionPrefab, TransformUsageFlags.Dynamic),
                    UnitsPerBackgroundTile = authoring.UnitsPerBackgroundTile,
                    LevelSize = authoring.LevelSize,
                    ItemCount = authoring.ItemCount,
                    MaxRarity = maxRarity
                });
                AddComponent<RegenerateLevelFlag>(entity);
                AddComponent(entity, new EntityRandom
                {
                    Value = Random.CreateFromIndex(authoring.Seed),
                });
            }
        }

        private void OnValidate()
        {
            if (_prevSeed != Seed)
            {
                if (World.DefaultGameObjectInjectionWorld == null) return;
                _prevSeed = Seed;
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var query = entityManager.CreateEntityQuery(typeof(LevelGenerationData));
                if (!query.HasSingleton<LevelGenerationData>()) return;
                var levelEntity = query.GetSingletonEntity();
                entityManager.SetComponentEnabled<RegenerateLevelFlag>(levelEntity, true);
            }
        }
    }

    /// <summary>
    /// 随机生成包含装饰和环境实体的关卡的系统。
    /// </summary>
    /// <remarks>
    /// 此系统通过查询 <see cref="RegenerateLevelFlag"/> 组件来确定何时重新生成关卡。
    /// 当此标志为 true 时，将首先销毁所有标记为 <see cref="DestroyOnRegenerateTag"/> 的现有实体。
    /// 然后，将从 <see cref="LevelGenerationData"/> 中存储的关卡元素动态缓冲区中随机选择实体，并将其放置在游戏区域内。
    /// </remarks>
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct LevelGenerationSystem : ISystem
    {
        /// <summary>
        /// Entity query for all entities that should be destroyed when the level generation system runs.
        /// </summary>
        /// <remarks>
        /// This is effectively all entities generated by previous iterations of the level generator, so the generator is always building up from a clean slate.
        /// </remarks>
        private EntityQuery _destroyOnRegenerateQuery;
        /// <summary>
        /// Entity archetype for a data-only entity containing the <see cref="LevelSafeBounds"/>.
        /// </summary>
        private EntityArchetype _levelSafeAreaArchetype;

        public void OnCreate(ref SystemState state)
        {
            _destroyOnRegenerateQuery = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAny<EnvironmentTag, DecorationTag, LevelSafeBounds>().Build(ref state);
            _levelSafeAreaArchetype = state.EntityManager.CreateArchetype(typeof(LevelSafeBounds));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (levelElements, random, regenerateLevel, generationData) in SystemAPI.Query<DynamicBuffer<LevelElementData>, RefRW<EntityRandom>, EnabledRefRW<RegenerateLevelFlag>, LevelGenerationData>())
            {
                var entitiesToDestroy = _destroyOnRegenerateQuery.ToEntityArray(state.WorldUpdateAllocator);
                ecb.DestroyEntity(entitiesToDestroy);

                var newBackground = ecb.Instantiate(generationData.BackgroundPrefab);

                var center = new float3(0f, -10f, 0f);
                var rotation = quaternion.Euler(0.5f * math.PI, 0f, 0f);
                var bgScale = generationData.LevelSize + 100;
                ecb.SetComponent(newBackground, LocalTransform.FromPositionRotationScale(center, rotation, bgScale));
                ecb.AddComponent(newBackground, new TileScaleOverride { Value = bgScale / generationData.UnitsPerBackgroundTile });

                var cautionPositionOffsetList = new FixedList64Bytes<float3>
                {
                    new(0f, 15.1f, generationData.LevelSize - 1.375f),
                    new(generationData.LevelSize - 1.375f, 15f, 0f),
                    new(0f, 15.1f, -1f * generationData.LevelSize + 1.375f),
                    new(-1f * generationData.LevelSize + 1.375f, 15f, 0f)
                };

                for (var i = 0; i < 4; i++)
                {
                    var newCaution = ecb.Instantiate(generationData.CautionPrefab);
                    var cautionPosition = center + cautionPositionOffsetList[i];
                    var cautionRotation = quaternion.Euler(0.5f * math.PI, 0.5f * i * math.PI, 0f);
                    ecb.SetComponent(newCaution, LocalTransform.FromPositionRotationScale(cautionPosition, cautionRotation, generationData.LevelSize));
                    ecb.AddComponent(newCaution, new TileScaleOverride { Value = generationData.LevelSize / 2f });
                }

                var minPosition = new float2(generationData.LevelSize * -0.5f);
                var maxPosition = new float2(generationData.LevelSize * 0.5f);
                var levelSafeBoundsEntity = ecb.CreateEntity(_levelSafeAreaArchetype);
                ecb.SetComponent(levelSafeBoundsEntity, new LevelSafeBounds
                {
                    MinPosition = minPosition,
                    MaxPosition = maxPosition
                });

                if (levelElements.IsEmpty) continue;
                if (generationData.MaxRarity <= 0) continue;
                regenerateLevel.ValueRW = false;

                for (var i = 0; i < generationData.ItemCount; i++)
                {
                    var rarity = random.ValueRW.Value.NextInt(generationData.MaxRarity);
                    var prefabIndex = -1;
                    for (var j = 0; j < levelElements.Length; j++)
                    {
                        if (rarity >= levelElements[j].Rarity) continue;
                        prefabIndex = j;
                        break;
                    }

                    if (prefabIndex < 0 || prefabIndex >= levelElements.Length) continue;

                    var levelElementData = levelElements[prefabIndex];
                    var newElement = ecb.Instantiate(levelElementData.Value);
                    var randPos2 = random.ValueRW.Value.NextFloat2(minPosition, maxPosition);

                    var yPos = SystemAPI.HasComponent<DecorationTag>(levelElementData.Value) ? -2.5f : -0.025f;
                    var randPos = new float3(randPos2.x, yPos, randPos2.y);
                    var randScale = random.ValueRW.Value.NextFloat(levelElementData.MinScale, levelElementData.MaxScale);
                    ecb.SetComponent(newElement, LocalTransform.FromPositionRotationScale(randPos, quaternion.identity, randScale));
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}