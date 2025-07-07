using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.GraphicsIntegration;
using UnityEngine;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 标志组件，表示此角色实体需要通过 <see cref="CharacterInitializationSystem"/> 进行初始化。
    /// </summary>
    /// <remarks>
    /// 角色生成时此标志为启用状态，一旦角色初始化完成，此组件将被禁用。
    /// 初始化应该在角色生成后很快发生。
    /// </remarks>
    public struct InitializeCharacterFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于持有角色基础移动速度（单位/秒）的组件。
    /// </summary>
    /// <remarks>
    /// 移动速度可以通过属性修改来临时或永久地改变（参见 <see cref="CharacterStatModificationState.MoveSpeed"/>）
    /// </remarks>
    public struct CharacterBaseMoveSpeed : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于持有角色生命值累加器的组件。
    /// 当组件启用时，将在 <see cref="CharacterHealthRegenerationSystem"/> 中进行生命值再生。
    /// </summary>
    /// <remarks>
    /// 这是必需的，因为生命值以整数形式存储，但生命恢复每秒恢复一小部分生命值，并且在玩家未达到最大生命值时持续发生。
    /// </remarks>
    public struct CharacterHealthRegenerationState : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 持有要恢复的生命值的小数值。一旦值 >= 1，将向角色的 <see cref="CurrentHitPoints"/> 添加1点生命值，直到达到玩家的最大生命值。
        /// </summary>
        /// <remarks>
        /// 这是必需的，因为生命值以整数形式存储，但生命恢复每秒恢复一小部分生命值，并且在玩家未达到最大生命值时持续发生。
        /// </remarks>
        public float HitPointAccumulator;
    }

    /// <summary>
    /// 定义角色当前移动方向的组件。
    /// </summary>
    /// <remarks>
    /// 角色移动方向在不同的系统中根据角色类型进行设置。玩家实体的值在 <see cref="GetPlayerInputSystem"/> 中设置，而敌人的值在 <see cref="EnemyMoveToPlayerJob"/> 或 <see cref="EnemySineWaveMovementSystem"/> 等系统中设置。
    /// 非零值应被归一化。
    /// </remarks>
    public struct CharacterMoveDirection : IComponentData
    {
        public float2 Value;
    }

    /// <summary>
    /// 用于各种实体引用其关联角色实体的组件。
    /// </summary>
    public struct CharacterEntity : IComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// 用于持有当前活动属性修改器实体的动态缓冲区。
    /// </summary>
    /// <remarks>
    /// 属性修改器实体各自拥有其 <see cref="StatModifier"/> 动态缓冲区，其中包含该实体将要修改的特定属性。拥有属性修改器实体的目的是为了能够实现临时的属性修改，例如，一个属性修改器实体可以在设定的时间后被摧毁，而所有其他的属性修改仍然有效。
    /// 内部缓冲区容量设置为0，意味着动态缓冲区的所有元素都将存在于块之外。这种情况是可以接受的，因为该缓冲区仅在角色的属性需要在 <see cref="RecalculateStatsSystem"/> 中重新计算时偶尔遍历。
    /// </remarks>
    /// <seeaslo cref="CharacterStatModificationState"/>
    /// <seeaslo cref="StatModifier"/>
    /// <seeaslo cref="RecalculateStatsSystem"/>
    /// <seeaslo cref="StatModifierProperties"/>
    [InternalBufferCapacity(0)]
    public struct ActiveStatModifierEntity : IBufferElementData
    {
        public Entity Value;
    }

    /// <summary>
    /// 标签组件，用于通知 <see cref="DestroyEntitySystem"/> 此角色在被摧毁时应播放销毁动画。
    /// </summary>
    public struct GraphicsEntityPlayDestroyEffectTag : IComponentData { }

    /// <summary>
    /// 可启用组件，表示此实体需要在 <see cref="RecalculateStatsSystem"/> 中重新计算其 <see cref="CharacterStatModificationState"/>。
    /// </summary>
    /// <seealso cref="CapabilityUpgradeController.UpgradePassive"/>
    public struct RecalculateStatsFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于存储角色当前属性修改值的数据组件。
    /// 多个系统引用此组件来计算有效的属性数据（即，取属性的基础值，并根据需要加或乘相关的属性修改值）。
    /// </summary>
    public struct CharacterStatModificationState : IComponentData
    {
        /// <summary>
        /// 移动速度修改。此值在 <see cref="CharacterMoveSystem"/> 中乘以角色的 <see cref="CharacterBaseMoveSpeed"/> 来计算当前有效移动速度。
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// 造成伤害修改。此值在各种攻击系统中乘以攻击的基础伤害值，以计算要分配给该攻击实体的当前有效伤害。
        /// </summary>
        public float DamageDealt;
        /// <summary>
        /// 额外的生命值，以增加角色的最大生命值。
        /// </summary>
        public int AdditionalHitPoints;
        /// <summary>
        /// 按此整数值减少受到的伤害点数。
        /// </summary>
        public int DamageReceived;
        /// <summary>
        /// 生命恢复修改。此值表示在 <see cref="CharacterHealthRegenerationSystem"/> 中每秒恢复多少生命值。
        /// </summary>
        public float HealthRegeneration;
        /// <summary>
        /// 攻击冷却时间修改。此值在各种攻击系统中乘以攻击的基础冷却时间，以计算该武器的当前有效冷却时间。
        /// </summary>
        public float AttackCooldown;
        /// <summary>
        /// 攻击投射物速度修改。此值在各种攻击系统中乘以攻击的基础投射物速度，以计算攻击实体的当前有效投射物速度。
        /// </summary>
        public float AttackProjectileSpeed;
        /// <summary>
        /// 攻击持续时间修改。此值在各种攻击系统中乘以攻击的基础攻击持续时间，以计算攻击实体在世界中存在的当前有效持续时间。
        /// </summary>
        public float AttackDuration;
        /// <summary>
        /// 每个适用的攻击组要生成的额外攻击投射物数量。
        /// </summary>
        public int AdditionalAttackProjectiles;
        /// <summary>
        /// 攻击区域修改。此值在各种攻击系统中乘以攻击的基础攻击区域，以计算攻击的当前规模。
        /// </summary>
        public float AttackArea;
        /// <summary>
        /// 物品吸引半径修改器。此值在 <see cref="DetectItemAttractionSystem"/> 中乘以玩家的 <see cref="PlayerAttractionAreaData.Radius"/>，以计算玩家的有效吸引区域。
        /// </summary>
        public float ItemAttractionRadius;
        /// <summary>
        /// 经验点获取修改器。此值在 <see cref="HandlePlayerExperienceThisFrameSystem"/> 中乘以玩家收集的经验点值，以计算收集到的宝石的有效经验点获取。
        /// </summary>
        public float ExperiencePointGain;

        /// <summary>
        /// 使用 <see cref="CharacterDefaultModificationValues"/> 轻松创建初始 CharacterStatModificationState 的构造函数。
        /// </summary>
        /// <remarks>
        /// 用于在 <see cref="CharacterInitializationSystem"/> 和 <see cref="RecalculateStatsSystem"/> 期间，在应用存储于 <see cref="ActiveStatModifierEntity"/> 中属性修改器实体的 <see cref="StatModifier"/> 缓冲区中的修改之前，获取属性值的基线。
        /// </remarks>
        /// <param name="defaultValues">每个修改状态的默认值。</param>
        public CharacterStatModificationState(CharacterDefaultModificationValues defaultValues)
        {
            MoveSpeed = defaultValues.MoveSpeed;
            DamageDealt = defaultValues.DamageDealt;
            AdditionalHitPoints = defaultValues.AdditionalHitPoints;
            DamageReceived = defaultValues.DamageReceived;
            HealthRegeneration = defaultValues.HealthRegeneration;
            AttackCooldown = defaultValues.AttackCooldown;
            AttackProjectileSpeed = defaultValues.AttackProjectileSpeed;
            AttackDuration = defaultValues.AttackDuration;
            AdditionalAttackProjectiles = defaultValues.AdditionalAttackProjectiles;
            AttackArea = defaultValues.AttackArea;
            ItemAttractionRadius = defaultValues.ItemAttractionRadius;
            ExperiencePointGain = defaultValues.ExperiencePointGain;
        }

        /// <summary>
        /// 将属性修改器添加到角色当前属性修改集的方法。此方法还将钳制属性修改值，以确保它们保持在可接受的范围内，以促进平衡的游戏玩法。
        /// </summary>
        /// <remarks>
        /// 某些值以整数形式存储，在钳位后必须转换为整数。
        /// 最小和最大属性修改值在 <see cref="StatModifierProperties"/> 中创作，存储在 <see cref="StatModifierController._statModifierPropertiesLookup"/> 中，并在 <see cref="StatModifierHelper.ClampModificationValue"/> 中进行钳位。
        /// </remarks>
        /// <param name="statModifier">要添加到角色当前属性修改的属性修改器。</param>
        /// <exception cref="ArgumentOutOfRangeException">如果在此 <see cref="StatModifier"/> 元素上设置了无效的 <see cref="StatModifierType"/>，则抛出。</exception>
        public void AddStat(StatModifier statModifier)
        {
            switch (statModifier.Type)
            {
                case StatModifierType.DamageDealt:
                    DamageDealt += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref DamageDealt);
                    break;
                case StatModifierType.DamageReceived:
                    var modifiedDamageReceived = DamageReceived + statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref modifiedDamageReceived);
                    DamageReceived = (int)modifiedDamageReceived;
                    break;
                case StatModifierType.AdditionalHitPoints:
                    var modifiedMaxHealth = AdditionalHitPoints + statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref modifiedMaxHealth);
                    AdditionalHitPoints = (int)modifiedMaxHealth;
                    break;
                case StatModifierType.HealthRegeneration:
                    HealthRegeneration += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref HealthRegeneration);
                    break;
                case StatModifierType.AttackCooldown:
                    AttackCooldown += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackCooldown);
                    break;
                case StatModifierType.AttackArea:
                    AttackArea += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackArea);
                    break;
                case StatModifierType.AttackProjectileSpeed:
                    AttackProjectileSpeed += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackProjectileSpeed);
                    break;
                case StatModifierType.MoveSpeed:
                    MoveSpeed += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref MoveSpeed);
                    break;
                case StatModifierType.AttackDuration:
                    AttackDuration += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackDuration);
                    break;
                case StatModifierType.AdditionalAttackProjectiles:
                    var modifiedAdditionalProjectiles = AdditionalAttackProjectiles + statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref modifiedAdditionalProjectiles);
                    AdditionalAttackProjectiles = (int)modifiedAdditionalProjectiles;
                    break;
                case StatModifierType.ItemAttractionRadius:
                    ItemAttractionRadius += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref ItemAttractionRadius);
                    break;
                case StatModifierType.ExperiencePointGain:
                    ExperiencePointGain += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref ExperiencePointGain);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 获取给定 <see cref="StatModifierType"/> 当前修改值的辅助函数。
        /// </summary>
        /// <param name="type">要返回的属性修改器类型。</param>
        /// <returns>所请求属性修改器的当前值。请注意，虽然某些属性存储为整数，但此函数始终返回浮点数。</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws if an invalid <see cref="StatModifierType"/> is passed into this function.</exception>
        public float GetCurrentValue(StatModifierType type)
        {
            return type switch
            {
                StatModifierType.DamageDealt => DamageDealt,
                StatModifierType.DamageReceived => DamageReceived,
                StatModifierType.AdditionalHitPoints => AdditionalHitPoints,
                StatModifierType.HealthRegeneration => HealthRegeneration,
                StatModifierType.AttackCooldown => AttackCooldown,
                StatModifierType.AttackArea => AttackArea,
                StatModifierType.AttackProjectileSpeed => AttackProjectileSpeed,
                StatModifierType.MoveSpeed => MoveSpeed,
                StatModifierType.AttackDuration => AttackDuration,
                StatModifierType.AdditionalAttackProjectiles => AdditionalAttackProjectiles,
                StatModifierType.ItemAttractionRadius => ItemAttractionRadius,
                StatModifierType.ExperiencePointGain => ExperiencePointGain,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

    /// <summary>
    /// 用于向实体添加角色所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// 需要 <see cref="DamageableEntityAuthoring"/> 和 <see cref="GraphicsEntityAuthoring"/> 组件，因为角色是可受伤的实体，并且具有必须引用的图形实体。
    /// </remarks>
    [RequireComponent(typeof(DamageableEntityAuthoring))]
    [RequireComponent(typeof(GraphicsEntityAuthoring))]
    public class CharacterAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 角色的移动速度。
        /// </summary>
        public float MoveSpeed;

        private class Baker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CharacterBaseMoveSpeed { Value = authoring.MoveSpeed });
                AddComponent<InitializeCharacterFlag>(entity);
                AddComponent<CharacterHealthRegenerationState>(entity);
                SetComponentEnabled<CharacterHealthRegenerationState>(entity, false);
                AddComponent<CharacterMoveDirection>(entity);
                AddComponent<CharacterStatModificationState>(entity);
                AddComponent<RecalculateStatsFlag>(entity);
                SetComponentEnabled<RecalculateStatsFlag>(entity, true);
                AddBuffer<ActiveStatModifierEntity>(entity);
                AddComponent(entity, new PhysicsGraphicalSmoothing { ApplySmoothing = 1 });
            }
        }
    }

    /// <summary>
    /// 在物理步骤中移动角色的系统。
    /// </summary>
    /// <remarks>
    /// 这被实现为 ISystem，以便物理速度可以在主线程上设置，然后在作业中并行运行。
    /// </remarks>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct CharacterMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (velocity, moveDirection, moveSpeed, characterStats, entity) in SystemAPI.Query<RefRW<PhysicsVelocity>, CharacterMoveDirection, CharacterBaseMoveSpeed, CharacterStatModificationState>().WithNone<KnockbackState>().WithEntityAccess())
            {
                var currentMovement = moveDirection.Value * moveSpeed.Value * characterStats.MoveSpeed;

                velocity.ValueRW.Linear = new float3(currentMovement.x, 0f, currentMovement.y);
                if (SystemAPI.HasComponent<GraphicsEntity>(entity) && math.abs(moveDirection.Value.x) > 0.15f)
                {
                    var graphicsEntity = SystemAPI.GetComponent<GraphicsEntity>(entity).Value;
                    if (SystemAPI.HasComponent<FacingDirectionOverride>(graphicsEntity))
                    {
                        var facingDirectionOverride = SystemAPI.GetComponentRW<FacingDirectionOverride>(graphicsEntity);
                        facingDirectionOverride.ValueRW.Value = math.sign(currentMovement.x);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 用于将角色的 LocalTransform.Position 与其 PhysicsMass.Transform.pos 同步的系统。
    /// </summary>
    /// <remarks>
    /// 这样做是为了允许 <see cref="PresentationSystemGroup"/> 中的系统（在 <see cref="PhysicsSystemGroup"/> 之后但在 <see cref="TransformSystemGroup"/> 之前运行）在同一帧中对角色的位置做出反应，而不是等待下一帧。
    /// 如果没有这个，角色和相机之间会存在明显的抖动。
    /// </remarks>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup), OrderLast = true)]
    public partial struct FixCharacterPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<CharacterBaseMoveSpeed>())
            {
                transform.ValueRW.Position.y = 0f;
            }
        }
    }

    /// <summary>
    /// 用于初始化新生成角色的系统。
    /// </summary>
    /// <remarks>
    /// Important that this updates "before" the GameStartSystem as that is where the player spawns and a 1 frame delay is required before calling UpgradeCapability on the EntityUpgradeControllerManaged otherwise some exceptions are thrown when subscene is open.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    [UpdateBefore(typeof(GameStartSystem))]
    public partial struct CharacterInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CharacterDefaultModificationValues>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var defaultModificationValues = SystemAPI.GetSingleton<CharacterDefaultModificationValues>();

            foreach (var (physicsMass, characterStats, statModifierEntities, entity) in SystemAPI.Query<RefRW<PhysicsMass>, RefRW<CharacterStatModificationState>, DynamicBuffer<ActiveStatModifierEntity>>().WithAll<InitializeCharacterFlag>().WithEntityAccess())
            {
                // Setting InverseInertia to zero ensures that the character entity will not rotate due to physics collisions or other external forces. Character can still rotate by directly setting LocalTransform.Rotation if needed.
                physicsMass.ValueRW.InverseInertia = float3.zero;

                // Set initial stat modifications.
                var currentModificationValues = new CharacterStatModificationState(defaultModificationValues);

                foreach (var statModifierEntity in statModifierEntities)
                {
                    var statModifiers = SystemAPI.GetBuffer<StatModifier>(statModifierEntity.Value);
                    foreach (var statModifier in statModifiers)
                    {
                        currentModificationValues.AddStat(statModifier);
                    }
                }

                characterStats.ValueRW = currentModificationValues;

                if (characterStats.ValueRO.AdditionalHitPoints > 0)
                {
                    var currentHitPoints = SystemAPI.GetComponentRW<CurrentHitPoints>(entity);
                    currentHitPoints.ValueRW.Value += characterStats.ValueRO.AdditionalHitPoints;
                }

                SystemAPI.SetComponentEnabled<InitializeCharacterFlag>(entity, false);
            }
        }
    }

    /// <summary>
    /// 系统，用于根据角色的 <see cref="CharacterStatModificationState.HealthRegeneration"/> 值随时间恢复生命值。
    /// </summary>
    /// <remarks>
    /// 该系统使用 <see cref="CharacterHealthRegenerationState.HitPointAccumulator"/> 来处理生命恢复的小数值。
    /// </remarks>
    [UpdateBefore(typeof(ProcessDamageThisFrameSystem))]
    public partial struct CharacterHealthRegenerationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (healthRegeneration, damageThisFrame, characterStats) in SystemAPI.Query<RefRW<CharacterHealthRegenerationState>, DynamicBuffer<DamageThisFrame>, CharacterStatModificationState>())
            {
                healthRegeneration.ValueRW.HitPointAccumulator += characterStats.HealthRegeneration * deltaTime;

                if (healthRegeneration.ValueRO.HitPointAccumulator >= 1f)
                {
                    var regeneratedHitPoints = (int)math.floor(healthRegeneration.ValueRO.HitPointAccumulator);
                    // Add negative damage points to DamageThisFrame dynamic buffer to add health to character's current hit points.
                    damageThisFrame.Add(new DamageThisFrame { Value = -1 * regeneratedHitPoints });
                    healthRegeneration.ValueRW.HitPointAccumulator -= regeneratedHitPoints;
                }
            }
        }
    }

    /// <summary>
    /// 用于在将新的 <see cref="StatModifier"/> 添加到角色时重新计算角色 <see cref="CharacterStatModificationState"/> 的系统。
    /// </summary>
    /// <remarks>
    /// 该系统还将根据新计算的属性修改状态重新计算角色的 <see cref="CurrentHitPoints"/>。
    /// </remarks>
    [UpdateAfter(typeof(DS_InteractionSystemGroup))]
    public partial struct RecalculateStatsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CharacterDefaultModificationValues>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var statModifierLookup = SystemAPI.GetBufferLookup<StatModifier>();
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var defaultStats = SystemAPI.GetSingleton<CharacterDefaultModificationValues>();

            // Ensure character is initialized before recalculating stats otherwise move speed and max health may not be properly set in the base character stats.
            foreach (var (statModifierEntities, characterStats, currentHitPoints, baseHitPoints, recalculateStats, characterEntity) in SystemAPI.Query<DynamicBuffer<ActiveStatModifierEntity>, RefRW<CharacterStatModificationState>, RefRW<CurrentHitPoints>, BaseHitPoints, EnabledRefRW<RecalculateStatsFlag>>().WithNone<InitializeCharacterFlag>().WithEntityAccess())
            {
                var currentStats = new CharacterStatModificationState(defaultStats);

                // Reverse for loop so removing non-existent stat modifier entities doesn't affect calculating stats for subsequent stat modifier entities.
                for (var i = statModifierEntities.Length - 1; i >= 0; i--)
                {
                    var statModifierEntity = statModifierEntities[i].Value;
                    if (!SystemAPI.Exists(statModifierEntity))
                    {
                        statModifierEntities.RemoveAtSwapBack(i);
                        continue;
                    }

                    var currentStatModifiers = statModifierLookup[statModifierEntity];
                    foreach (var currentStatModifier in currentStatModifiers)
                    {
                        currentStats.AddStat(currentStatModifier);
                        if (currentStatModifier.Type == StatModifierType.AdditionalHitPoints && SystemAPI.HasComponent<PlayerTag>(characterEntity))
                        {
                            SystemAPI.SetComponentEnabled<UpdatePlayerHealthUIFlag>(characterEntity, true);
                        }
                    }
                }

                var maxHitPoints = baseHitPoints.Value + currentStats.AdditionalHitPoints;
                currentHitPoints.ValueRW.Value = math.min(currentHitPoints.ValueRO.Value, maxHitPoints);

                var enableHealthRegeneration = currentStats.HealthRegeneration > 0f && currentHitPoints.ValueRO.Value < maxHitPoints;
                SystemAPI.SetComponentEnabled<CharacterHealthRegenerationState>(characterEntity, enableHealthRegeneration);

                characterStats.ValueRW = currentStats;
                recalculateStats.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}