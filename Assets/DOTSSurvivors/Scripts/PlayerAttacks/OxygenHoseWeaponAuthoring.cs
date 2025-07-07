using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储向游戏世界生成氧气软管攻击相关数据的数据组件。
    /// </summary>
    public struct OxygenHoseWeaponData : IComponentData
    {
        /// <summary>
        /// 在攻击组中为氧气软管攻击生成应用的y轴偏移。此值将与其在攻击组中的生成索引相乘。
        /// </summary>
        public float YOffset;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="OxygenHoseWeaponData"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    public class OxygenHoseWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 在攻击组中为氧气软管攻击生成应用的y轴偏移。此值将与其在攻击组中的生成索引相乘。
        /// </summary>
        public float YOffset;

        private class Baker : Baker<OxygenHoseWeaponAuthoring>
        {
            public override void Bake(OxygenHoseWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new OxygenHoseWeaponData { YOffset = authoring.YOffset });
            }
        }
    }


    /// <summary>
    /// 处理将氧气软管的 <see cref="AttackPrefab"/> 实例化到游戏世界中的系统。
    /// </summary>
    /// <remarks>
    /// Oxygen hoses are affected by modifications to the player's additional attack projectiles, damage dealt, and attack area stat modifications.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct OxygenHoseAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (weaponState, oxygenHoseData, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, OxygenHoseWeaponData, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;

                var playerEntity = parent.Value;
                var playerStatModifications = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);

                var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
                var yOffset = weaponState.ValueRO.AttackCount * oxygenHoseData.YOffset;
                var spawnPosition = playerPosition + new float3(0f, 0f, yOffset);

                var isFacingRight = 0 < math.sign(SystemAPI.GetComponent<PreviousPlayerInput>(playerEntity).LastFacingDirection);
                var isForwardAttack = weaponState.ValueRO.AttackCount % 2 == 0;
                var isRightAttack = isFacingRight == isForwardAttack;
                var xRotation = isForwardAttack == isRightAttack ? 0 : math.PI;
                var yRotation = isRightAttack ? 0 : math.PI;
                var spawnRotation = quaternion.Euler(xRotation, yRotation, 0f);

                var spawnScale = weaponData.Area * playerStatModifications.AttackArea;

                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerStatModifications.DamageDealt);

                var newOxygenHoseEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newOxygenHoseEntity, LocalTransform.FromPositionRotationScale(spawnPosition, spawnRotation, spawnScale));
                ecb.SetComponent(newOxygenHoseEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(newOxygenHoseEntity, new DestroyAfterTime { Value = weaponData.TimeToLive });

                weaponState.ValueRW.NextAttackTimer = weaponData.IntervalBetweenAttacks;
                weaponState.ValueRW.AttackCount += 1;
                var numberAttacks = weaponData.AttackCount + playerStatModifications.AdditionalAttackProjectiles;
                if (weaponState.ValueRW.AttackCount < numberAttacks) continue;

                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponState.ValueRW.AttackCount = 0;

                weaponActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}