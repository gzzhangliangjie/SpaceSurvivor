using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储生成扳手攻击相关数据的数据组件。
    /// </summary>
    public struct WrenchWeaponData : IComponentData
    {
        /// <summary>
        /// 应用到扳手攻击在组中生成角度的偏移。此值将与其在攻击组中的生成索引相乘。
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float RadianAngleBetweenAttacks;

        /// <summary>
        /// 存储武器将向第一个敌人发射扳手的角度。后续扳手将以此角度的偏移发射。
        /// </summary>
        /// <remarks>
        /// As this value will change with each attack group, normally I would store something like this in a separate "state" component. However, as this component is only used in the <see cref="WrenchAttackSystem"/> I don't have any concern over potential data dependency issues so there is no problem having this field in here too.
        /// </remarks>
        public float RadianAngleToFirstEnemy;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="WrenchWeaponData"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// Requires the <see cref="EntityRandomAuthoring"/> script for random number generation in <see cref="WrenchAttackSystem"/>.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class WrenchWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 初始扳手攻击将在朝向屏幕上随机敌人的方向生成。后续扳手将以此字段定义的角度偏移从初始角度发射。
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float DegreeAngleBetweenAttacks = 20f;

        private class Baker : Baker<WrenchWeaponAuthoring>
        {
            public override void Bake(WrenchWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new WrenchWeaponData
                {
                    RadianAngleBetweenAttacks = math.radians(authoring.DegreeAngleBetweenAttacks)
                });
            }
        }
    }

    /// <summary>
    /// 处理将扳手的 <see cref="AttackPrefab"/> 实例化到游戏世界中的系统。
    /// </summary>
    /// <remarks>
    /// Wrenches are affected by modifications to the player's additional attack projectiles, damage dealt, and attack projectile speed stat modifications.
    /// The initial wrench attack will spawn in the direction towards a random enemy on screen. Subsequent wrenches will be offset from that initial angle by the angle defined in <see cref="WrenchWeaponData.RadianAngleBetweenAttacks"/>.
    /// Wrenches move in a boomerang pattern, first moving in the direction of spawn then reversing on its initial path.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct WrenchAttackSystem : ISystem
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
            var deltaTime = SystemAPI.Time.DeltaTime;
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (weaponState, random, wrenchData, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, RefRW<EntityRandom>, RefRW<WrenchWeaponData>, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>().WithNone<InitializeEntityRandom>())
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
                    wrenchData.ValueRW.RadianAngleToFirstEnemy = math.atan2(vectorToTargetEnemy.x, vectorToTargetEnemy.y);
                }

                var spawnAngle = wrenchData.ValueRO.RadianAngleToFirstEnemy + weaponState.ValueRO.AttackCount * wrenchData.ValueRO.RadianAngleBetweenAttacks;
                var spawnRotation = quaternion.Euler(0f, spawnAngle, 0f);
                var spawnScale = weaponData.Area * playerCurrentStats.AttackArea;

                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                var attackProjectileSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;

                var newWrenchEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newWrenchEntity, LocalTransform.FromPositionRotationScale(spawnPosition, spawnRotation, spawnScale));
                ecb.SetComponent(newWrenchEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(newWrenchEntity, new BoomerangMovementData
                {
                    MoveSpeed = attackProjectileSpeed,
                    StartTime = elapsedTime
                });

                weaponState.ValueRW.NextAttackTimer = weaponData.IntervalBetweenAttacks;
                weaponState.ValueRW.AttackCount += 1;
                var numberAttacks = weaponData.AttackCount + playerCurrentStats.AdditionalAttackProjectiles;
                if (weaponState.ValueRO.AttackCount < numberAttacks) continue;

                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponState.ValueRW.AttackCount = 0;

                weaponActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}