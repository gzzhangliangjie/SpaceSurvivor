using Unity.Burst;
using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 标记组件，表示此实体是向游戏世界生成黑洞的武器实体。
    /// </summary>
    public struct BlackHoleWeaponTag : IComponentData { }

    /// <summary>
    /// 用于向实体添加 <see cref="BlackHoleWeaponTag"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> and <see cref="EntityRandom"/> scripts to ensure all components required for executing attacks are added to the entity.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class BlackHoleWeaponAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BlackHoleWeaponAuthoring>
        {
            public override void Bake(BlackHoleWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BlackHoleWeaponTag>(entity);
            }
        }
    }

    /// <summary>
    /// 处理将黑洞的 <see cref="AttackPrefab"/> 实例化到游戏世界中的系统。
    /// </summary>
    /// <remarks>
    /// This system just spawns the black hole entity, further behavior of the black hole attack can be found in <see cref="BlackHoleSystem"/>.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct BlackHoleAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (random, weaponData, attackPrefab, weaponActive) in SystemAPI.Query<RefRW<EntityRandom>, WeaponLevelData, AttackPrefab, EnabledRefRW<WeaponActiveFlag>>().WithAll<BlackHoleWeaponTag>())
            {
                var newBlackHoleEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newBlackHoleEntity, new DestroyAfterTime { Value = weaponData.TimeToLive });

                var shouldSaveItems = random.ValueRW.Value.NextInt(101) <= weaponData.RandomChance;
                ecb.SetComponent(newBlackHoleEntity, new BlackHoleData
                {
                    CollisionFilter = weaponData.CollisionFilter,
                    SaveItems = shouldSaveItems
                });

                weaponActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}