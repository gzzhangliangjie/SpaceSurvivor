using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Hash128 = Unity.Entities.Hash128;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// Data component to store the hash code of the <see cref="RandomItemDropProperties"/> ScriptableObject associated with this entity.
    /// </summary>
    /// <remarks>
    /// 这在 <see cref="InitializeSpawnRandomOnDestroySystem"/> 中用于确定是否已为此ScriptableObject创建了 <see cref="RandomItemToSpawnBlob"/> blob资源。如果已创建，则将分配该blob资源；如果尚未创建，则将创建一个并与此哈希值关联。
    /// </remarks>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="RandomItemToSpawnBlob"/>
    public struct SpawnRandomPropertiesHashCode : IComponentData
    {
        public uint Value;
    }

    /// <summary>
    /// Dynamic Buffer component which is required because there is no way to "covert" an GameObject into an Entity at runtime. As such the entity conversion will take place in the <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> and those prefabs will be stored in this Dynamic Buffer. At runtime, a blob asset will be generated storing these entity prefabs.
    /// </summary>
    /// <remarks>
    /// 由于Unity的实体从"烘焙时"实体到运行时实体的重新映射，实体索引将不相同。因此，需要先在运行时创建并提供预制体，然后才能将对它们的引用存储在blob资源中。
    /// </remarks>
    public struct SpawnRandomPrefabHelper : IBufferElementData
    {
        public int Rarity;
        public Entity Prefab;
    }

    /// <summary>
    /// Struct to store information related to items that can be spawned when this entity is destroyed. Used as the base element of the <see cref="RandomItemToSpawnBlob"/> blob array.
    /// </summary>
    /// <seealso cref="RandomItemToSpawnBlob"/>
    public struct RandomItemToSpawn
    {
        public int RarityKey;
        public Entity Entity;
    }

    /// <summary>
    /// Component containing the blob array of <see cref="RandomItemToSpawn"/> elements. Used in the <see cref="DestroyEntitySystem"/> to determine which entity prefab to instantiate when the entity with this component is destroyed.
    /// </summary>
    /// <remarks>
    /// 由于从烘焙期间创建的预制体到运行时可用的预制体的实体重新映射特性，此blob数组必须在运行时填充所需的预制体。为方便此操作，可生成的预制体首先在 <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> 中烘焙成实体，存入 <see cref="SpawnRandomPrefabHelper"/> 的动态缓冲区中。然后由 <see cref="InitializeSpawnRandomOnDestroySystem"/> 用适当的数据填充此blob数组。
    /// 这可以作为一个清理组件来实现，以避免在 <see cref="DestroyEntitySystem"/> 中包含逻辑，但是我选择将逻辑保留在那里，这样我们也可以有一个不生成额外实体的 <see cref="InstantDestroyEntitySystem"/>。如果将此作为清理组件，我们需要在 <see cref="InstantDestroyEntitySystem"/> 中专门移除该组件。
    /// </remarks>
    /// <seealso cref="RandomItemToSpawn"/>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="DestroyEntitySystem"/>
    public struct RandomItemToSpawnBlob : IComponentData
    {
        public BlobAssetReference<BlobArray<RandomItemToSpawn>> Value;
    }

    /// <summary>
    /// Authoring component to associate a <see cref="RandomItemDropProperties"/> ScriptableObject with an entity. This authoring component will also convert GameObject prefabs set in the <see cref="RandomItemDropProperties"/> and store them in a temporary <see cref="SpawnRandomPrefabHelper"/> DynamicBuffer.
    /// </summary>
    /// <remarks>
    /// 由于从烘焙时创建的预制体到运行时可用的预制体的实体重新映射特性，<see cref="RandomItemToSpawnBlob"/> blob数组必须在运行时用所需的预制体填充。为方便起见，可生成的预制体首先在 <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> 中被烘焙成实体，并存入 <see cref="SpawnRandomPrefabHelper"/> 的动态缓冲区中。<see cref="InitializeSpawnRandomOnDestroySystem"/> 将使用适当的数据填充此blob数组。
    /// 重要的是，我们包含DependsOn(authoring.RandomItemDropProperties)，以便在ScriptableObject上的值更改时重新运行烘焙。
    /// </remarks>
    /// <seealso cref="RandomItemToSpawn"/>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="InitializeSpawnRandomOnDestroySystem"/>
    /// <seealso cref="SpawnRandomPrefabHelper"/>
    [RequireComponent(typeof(DamageableEntityAuthoring))]
    public class SpawnRandomOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// ScriptableObject defining the items and rarity this entity can drop upon destruction.
        /// </summary>
        /// <remarks>
        /// 重要的是，我们包含DependsOn(authoring.RandomItemDropProperties)，以便在ScriptableObject上的值更改时重新运行烘焙。
        /// </remarks>
        public RandomItemDropProperties RandomItemDropProperties;

        private class Baker : Baker<SpawnRandomOnDestroyAuthoring>
        {
            public override void Bake(SpawnRandomOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                DependsOn(authoring.RandomItemDropProperties);
                AddComponent(entity, new SpawnRandomPropertiesHashCode { Value = (uint)authoring.RandomItemDropProperties.GetHashCode() });
                var itemDropInitializationBuffer = AddBuffer<SpawnRandomPrefabHelper>(entity);
                foreach (var itemDrop in authoring.RandomItemDropProperties.ItemDrops)
                {
                    itemDropInitializationBuffer.Add(new SpawnRandomPrefabHelper
                    {
                        Rarity = itemDrop.Rarity,
                        Prefab = GetEntity(itemDrop.Prefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }

    /// <summary>
    /// This system is responsible for creating a blob asset containing a blob array of <see cref="RandomItemToSpawn"/> to be stored in a <see cref="RandomItemToSpawnBlob"/> component.
    /// </summary>
    /// <remarks>
    /// 由于从烘焙时创建的预制体到运行时可用的预制体的实体重新映射特性，<see cref="RandomItemToSpawnBlob"/> blob数组必须在运行时用所需的预制体填充。为方便起见，可生成的预制体首先在 <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> 中被烘焙成实体，并存入 <see cref="SpawnRandomPrefabHelper"/> 的动态缓冲区中。<see cref="InitializeSpawnRandomOnDestroySystem"/> 将使用适当的数据填充此blob数组。
    /// 此系统使用 <see cref="GameStartSystem.Singleton.GameIndex"/> 来确保每次进入游戏时都为blob资源创建唯一的哈希值。这很重要，因为如果使用来自先前游戏的blob资源，它们将不会映射到有效的预制体，因此不会生成任何东西。
    /// </remarks>
    /// <seealso cref="RandomItemToSpawn"/>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="SpawnRandomOnDestroyAuthoring "/>
    /// <seealso cref="SpawnRandomPrefabHelper"/>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct InitializeSpawnRandomOnDestroySystem : ISystem
    {
        /// <summary>
        /// Blob Asset Store is required to perform Blob Asset Operations. In this case it is used to determine if a blob asset has already been created for the <see cref="RandomItemDropProperties"/> ScriptableObject. If not, a new one will be created and added to the Blob Asset Store so that subsequent entities with the same RandomItemDropProperties will reference the same Blob Asset without creating a new one.
        /// </summary>
        private BlobAssetStore _blobAssetStore;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStartSystem.Singleton>();
            _blobAssetStore = new BlobAssetStore(1);
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var gameIndex = (uint)SystemAPI.GetSingleton<GameStartSystem.Singleton>().GameIndex;
            foreach (var (randomItemBuffer, hashCode, entity) in SystemAPI.Query<DynamicBuffer<SpawnRandomPrefabHelper>, SpawnRandomPropertiesHashCode>().WithNone<RandomItemToSpawnBlob>().WithEntityAccess())
            {
                var hash = new Hash128(hashCode.Value, gameIndex, 0u, 0u);

                if (!_blobAssetStore.TryGet<BlobArray<RandomItemToSpawn>>(hash, out var blobAssetReference))
                {
                    var builder = new BlobBuilder(state.WorldUpdateAllocator);
                    ref var randomBlob = ref builder.ConstructRoot<BlobArray<RandomItemToSpawn>>();

                    var arrayBuilder = builder.Allocate(ref randomBlob, randomItemBuffer.Length);

                    var rarity = 0;
                    for (var i = 0; i < randomItemBuffer.Length; i++)
                    {
                        rarity += randomItemBuffer[i].Rarity;

                        arrayBuilder[i] = new RandomItemToSpawn
                        {
                            Entity = randomItemBuffer[i].Prefab,
                            RarityKey = rarity
                        };
                    }

                    blobAssetReference = builder.CreateBlobAssetReference<BlobArray<RandomItemToSpawn>>(Allocator.Persistent);
                    _blobAssetStore.TryAdd(hash, ref blobAssetReference);
                }

                ecb.AddComponent(entity, new RandomItemToSpawnBlob { Value = blobAssetReference });
            }

            ecb.Playback(state.EntityManager);
        }
    }
}