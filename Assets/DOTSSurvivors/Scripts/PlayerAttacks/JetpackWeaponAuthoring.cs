using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 存储与喷气背包武器相关数据的数据组件。
    /// </summary>
    /// <seealso cref="JetpackWeaponAuthoring"/>
    /// <seealso cref="JetpackAttackSystem"/>
    public struct JetpackWeaponData : IComponentData
    {
        /// <summary>
        /// 应用于喷气背包攻击在组中生成角度的偏移量。此值将与攻击组中的生成索引相乘。
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method.
        /// </remarks>
        public float RadianAngleBetweenAttacks;

        /// <summary>
        /// 存储武器将向第一个敌人发射喷气背包的角度。后续的喷气背包将以此角度的偏移量发射。
        /// </summary>
        /// <remarks>
        /// As this value will change with each attack group, normally I would store something like this in a separate "state" component. However, as this component is only used in the <see cref="JetpackAttackSystem"/> I don't have any concern over potential data dependency issues so there is no problem having this field in here too.
        /// </remarks>
        public float RadianAngleToFirstEnemy;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="JetpackWeaponData"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// Requires the <see cref="EntityRandomAuthoring"/> script for random number generation in <see cref="JetpackAttackSystem"/>.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class JetpackWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 初始喷气背包攻击将朝向屏幕上随机敌人的方向生成。后续的喷气背包将以此字段定义的角度偏移量从初始角度偏移。
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method.
        /// </remarks>
        public float DegreeAngleBetweenAttacks = 20f;

        private class Baker : Baker<JetpackWeaponAuthoring>
        {
            public override void Bake(JetpackWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new JetpackWeaponData
                {
                    RadianAngleBetweenAttacks = math.radians(authoring.DegreeAngleBetweenAttacks)
                });
            }
        }
    }

    /// <summary>
    /// 处理将喷气背包的 <see cref="AttackPrefab"/> 实例化到游戏世界中的系统。
    /// </summary>
    /// <remarks>
    /// Jetpacks are affected by modifications to the player's additional attack projectiles, damage dealt, attack duration, and attack projectile speed stat modifications.
    /// The initial jetpack attack will spawn in the direction towards a random enemy on screen. Subsequent jetpacks will be offset from that initial angle by the angle defined in <see cref="JetpackWeaponData.RadianAngleBetweenAttacks"/>.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct JetpackAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraHalfExtents = SystemAPI.GetSingleton<CameraTarget>().HalfExtents;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (weaponState, random, jetpackData, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, RefRW<EntityRandom>, RefRW<JetpackWeaponData>, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>().WithNone<InitializeEntityRandom>())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;

                var playerEntity = parent.Value;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);
                var spawnPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

                if (weaponState.ValueRO.AttackCount == 0)
                {
                    var pSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                    var overlapHits = new NativeList<int>(state.WorldUpdateAllocator);
                    var minPosition = spawnPosition - cameraHalfExtents;
                    var maxPosition = spawnPosition + cameraHalfExtents;
                    var aabbInput = new OverlapAabbInput
                    {
                        Aabb = new Aabb
                        {
                            Min = minPosition,
                            Max = maxPosition
                        },
                        Filter = weaponData.CollisionFilter
                    };

                    if (!pSingleton.OverlapAabb(aabbInput, ref overlapHits))
                    {
                        // No enemies within detection radius.
                        // Timer doesn't reset so this will check again each frame until an enemy is within range.
                        continue;
                    }

                    var randomIndex = random.ValueRW.Value.NextInt(overlapHits.Length);
                    var randomBodyIndex = overlapHits[randomIndex];
                    var randomEnemyPosition = pSingleton.Bodies[randomBodyIndex].WorldFromBody.pos;
                    var vectorToTargetEnemy = randomEnemyPosition.xz - spawnPosition.xz;
                    jetpackData.ValueRW.RadianAngleToFirstEnemy = math.atan2(vectorToTargetEnemy.x, vectorToTargetEnemy.y);
                }

                var spawnAngle = jetpackData.ValueRO.RadianAngleToFirstEnemy + jetpackData.ValueRO.RadianAngleBetweenAttacks * weaponState.ValueRO.AttackCount;
                var spawnRotation = quaternion.Euler(0f, spawnAngle, 0f);

                var duration = weaponData.TimeToLive * playerCurrentStats.AttackDuration;
                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                var attackSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;

                var newJetpackEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newJetpackEntity, LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
                ecb.SetComponent(newJetpackEntity, new DestroyAfterTime { Value = duration });
                ecb.SetComponent(newJetpackEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(newJetpackEntity, new DestroyAfterNumberHits { HitsRemaining = weaponData.MaxEnemyHitCount });
                ecb.SetComponent(newJetpackEntity, new LinearMovementSpeed { Value = attackSpeed });

                weaponState.ValueRW.NextAttackTimer = weaponData.IntervalBetweenAttacks;

                weaponState.ValueRW.AttackCount += 1;
                var numberAttacks = weaponData.AttackCount + playerCurrentStats.AdditionalAttackProjectiles;
                if (weaponState.ValueRW.AttackCount < numberAttacks) continue;

                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponState.ValueRW.AttackCount = 0;

                weaponActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}