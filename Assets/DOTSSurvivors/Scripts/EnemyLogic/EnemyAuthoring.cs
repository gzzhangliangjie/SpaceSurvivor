using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 包含 <see cref="EnemyType"/> 值的组件。此组件仅用于初始化，在实例化任何敌人之前将从敌人预制体中移除。使用方法请参见 <see cref="GameStartSystem"/>。
    /// </summary>
    /// <remarks>
    /// 这是一个常规的标签组件，而不是一个可启用的组件，因为在生成任何敌人实例之前，此组件将从敌人预制件中移除。
    /// </remarks>
    public struct EnemyTypeInitialization : IComponentData
    {
        public EnemyType Value;
    }

    /// <summary>
    /// 用于标识敌人实体的空标签组件。
    /// </summary>
    public struct EnemyTag : IComponentData { }

    /// <summary>
    /// 包含敌人攻击相关数据的数据组件。
    /// </summary>
    /// <seealso cref="EnemyAttackSystem"/>
    /// <seealso cref="EnemyAttackCooldownExpiration"/>
    public struct EnemyAttackData : IComponentData
    {
        /// <summary>
        /// 单次攻击中要造成的伤害点数。
        /// </summary>
        public int HitPoints;

        /// <summary>
        /// 定义敌人攻击后无法造成伤害的冷却时间（秒）。此值在敌人的生存期内保持不变。请参见 <see cref="EnemyAttackCooldownExpiration"/> 组件，它包含敌人能够再次攻击的时间戳。
        /// </summary>
        public float CooldownTime;
    }

    /// <summary>
    /// 用于存储敌人攻击冷却到期时间戳的组件。如果游戏时间大于或等于此值，敌人可以攻击。
    /// </summary>
    /// <seealso cref="EnemyAttackSystem"/>
    /// <seealso cref="EnemyAttackData"/>
    public struct EnemyAttackCooldownExpiration : IComponentData
    {
        /// <summary>
        /// 敌人攻击后，此值将设置为当前游戏时间 + <see cref="EnemyAttackData.CooldownTime"/>
        /// </summary>
        public float Value;
    }

    /// <summary>
    /// 标签组件，用于将此实体标识为应根据玩家等级调整其生命值的敌人。等级缩放由敌人基础生命值乘以玩家等级定义。
    /// </summary>
    /// <remarks>
    /// 即，如果敌人有10点基础生命值，而玩家等级为3，那么敌人生成时将拥有30点生命值。
    /// </remarks>
    public struct EnemyHitPointsScaleWithPlayerLevelTag : IComponentData { }

    /// <summary>
    /// Authoring script to set initial values for enemies.
    /// </summary>
    /// <seealso cref="EnemyAttackData"/>
    /// <seealso cref="EnemyAttackCooldownExpiration"/>
    /// <seealso cref="EnemyTypeInitialization"/>
    /// <seealso cref="EnemyHitPointsScaleWithPlayerLevelTag"/>
    [RequireComponent(typeof(CharacterAuthoring))]
    [RequireComponent(typeof(DropExperienceOnDestroyAuthoring))]
    [RequireComponent(typeof(ShowDamageNumberOnDamageAuthoring))]
    public class EnemyAuthoring : MonoBehaviour
    {
        public int BaseAttackDamage;
        public float EnemyAttackCooldown;
        public EnemyType EnemyType;
        public bool HealthScalesWithPlayerLevel;

        private class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyTag>(entity);
                AddComponent(entity, new EnemyAttackData
                {
                    HitPoints = authoring.BaseAttackDamage,
                    CooldownTime = authoring.EnemyAttackCooldown
                });
                AddComponent<EnemyAttackCooldownExpiration>(entity);
                AddComponent(entity, new EnemyTypeInitialization
                {
                    Value = authoring.EnemyType
                });

                if (authoring.HealthScalesWithPlayerLevel)
                {
                    AddComponent<EnemyHitPointsScaleWithPlayerLevelTag>(entity);
                }
            }
        }
    }

    /// <summary>
    /// System to control initialization steps pertaining to enemy entities.
    /// </summary>
    /// <remarks>
    /// 当前唯一的初始化步骤是对标记有 <see cref="EnemyHitPointsScaleWithPlayerLevelTag"/> 的敌人执行生命值缩放。
    /// 系统在 <see cref="CharacterInitializationSystem"/> 之前更新，因为 <see cref="InitializeCharacterFlag"/> 在该系统中被禁用。
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    [UpdateBefore(typeof(CharacterInitializationSystem))]
    public partial struct EnemyInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerExperienceState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerLevel = SystemAPI.GetSingleton<PlayerExperienceState>().Level;

            foreach (var (currentHitPoints, baseHitPoints) in SystemAPI.Query<RefRW<CurrentHitPoints>, RefRW<BaseHitPoints>>().WithAll<EnemyTag, EnemyHitPointsScaleWithPlayerLevelTag, InitializeCharacterFlag>())
            {
                var scaledHitPoints = baseHitPoints.ValueRO.Value * playerLevel;
                baseHitPoints.ValueRW.Value = scaledHitPoints;
                currentHitPoints.ValueRW.Value = scaledHitPoints;
            }
        }
    }

    /// <summary>
    /// System that schedules <see cref="EnemyAttackCollisionJob"/> which detects collisions between enemies and the player and deals damage to the player. Updates in the <see cref="DS_PhysicsSystemGroup"/> so collision events for this frame have been raised.
    /// </summary>
    /// <seealso cref="EnemyAttackCollisionJob"/>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct EnemyAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = new EnemyAttackCollisionJob
            {
                PlayerLookup = SystemAPI.GetComponentLookup<PlayerTag>(true),
                EnemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
                EnemyAttackPropertiesLookup = SystemAPI.GetComponentLookup<EnemyAttackData>(true),
                CharacterStatsLookup = SystemAPI.GetComponentLookup<CharacterStatModificationState>(true),
                CooldownExpirationLookup = SystemAPI.GetComponentLookup<EnemyAttackCooldownExpiration>(),
                DamageThisFrameLookup = SystemAPI.GetBufferLookup<DamageThisFrame>(),
                ElapsedTime = SystemAPI.Time.ElapsedTime
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }

    /// <summary>
    /// 由 <see cref="EnemyAttackSystem"/> 调度的碰撞事件作业。此作业在此物理步骤中由物理系统引发的所有碰撞事件上运行。Execute方法将首先确定碰撞事件中的哪个实体是玩家实体，哪个是敌人实体。如果碰撞事件不恰好在1个玩家和1个敌人之间，作业将提前退出。接下来，作业将确定敌人是否在冷却中，如果敌人不在冷却中，它将通过 <see cref="DamageThisFrame"/> 动态缓冲区对玩家施加伤害，并重置敌人的 <see cref="EnemyAttackCooldownExpiration"/>。
    /// </summary>
    [BurstCompile]
    public struct EnemyAttackCollisionJob : ICollisionEventsJob
    {
        /// <summary>
        /// 用于确定玩家实体的组件查找。
        /// </summary>
        [ReadOnly] public ComponentLookup<PlayerTag> PlayerLookup;
        /// <summary>
        /// 用于确定敌人实体的组件查找。
        /// </summary>
        [ReadOnly] public ComponentLookup<EnemyTag> EnemyLookup;
        /// <summary>
        /// 用于获取敌人攻击数据的组件查找。
        /// </summary>
        [ReadOnly] public ComponentLookup<EnemyAttackData> EnemyAttackPropertiesLookup;
        /// <summary>
        /// 用于确定是否应向敌人的攻击添加任何额外伤害缩放的组件查找。
        /// </summary>
        [ReadOnly] public ComponentLookup<CharacterStatModificationState> CharacterStatsLookup;
        /// <summary>
        /// 读/写组件查找，用于确定敌人是否处于冷却状态。还需要写访问权限，以便在敌人攻击后更新到期计时器。
        /// </summary>
        public ComponentLookup<EnemyAttackCooldownExpiration> CooldownExpirationLookup;
        /// <summary>
        /// 用于向玩家施加伤害点数的缓冲区查找。
        /// </summary>
        public BufferLookup<DamageThisFrame> DamageThisFrameLookup;
        /// <summary>
        /// 用于确定敌人是否处于冷却状态的已过游戏时间。
        /// </summary>
        public double ElapsedTime;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity playerEntity;
            Entity enemyEntity;

            if (PlayerLookup.HasComponent(collisionEvent.EntityA) && EnemyLookup.HasComponent(collisionEvent.EntityB))
            {
                playerEntity = collisionEvent.EntityA;
                enemyEntity = collisionEvent.EntityB;
            }
            else if (PlayerLookup.HasComponent(collisionEvent.EntityB) && EnemyLookup.HasComponent(collisionEvent.EntityA))
            {
                playerEntity = collisionEvent.EntityB;
                enemyEntity = collisionEvent.EntityA;
            }
            else
            {
                return;
            }

            var cooldownExpiration = CooldownExpirationLookup[enemyEntity];
            if (cooldownExpiration.Value > ElapsedTime) return;
            var attackProperties = EnemyAttackPropertiesLookup[enemyEntity];
            cooldownExpiration.Value = (float)ElapsedTime + attackProperties.CooldownTime;
            CooldownExpirationLookup[enemyEntity] = cooldownExpiration;

            var attackDamage = attackProperties.HitPoints;
            var damageModifier = CharacterStatsLookup[enemyEntity].DamageDealt;
            attackDamage = (int)math.ceil(attackDamage * damageModifier);

            var hitBuffer = DamageThisFrameLookup[playerEntity];
            hitBuffer.Add(new DamageThisFrame { Value = attackDamage });
        }
    }
}