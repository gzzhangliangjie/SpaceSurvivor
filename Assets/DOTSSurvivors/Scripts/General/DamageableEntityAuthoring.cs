using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 存储可受伤实体当前生命值的数据组件。
    /// </summary>
    /// <seealso cref="BaseHitPoints"/>
    /// <seealso cref="DamageableEntityAuthoring"/>
    public struct CurrentHitPoints : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// 存储可受伤实体基础生命值的数据组件。
    /// </summary>
    /// <remarks>
    /// 本质上是最大生命值，但最大生命值可以通过修改超过这个数值。
    /// </remarks>
    /// <seealso cref="CurrentHitPoints"/>
    /// <seealso cref="DamageableEntityAuthoring"/>
    public struct BaseHitPoints : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// 用于存储当前帧累积伤害的动态缓冲区。
    /// </summary>
    /// <remarks>
    /// 这种处理伤害的方法有两个主要目的：
    /// 1. 伤害可以在帧的持续时间内从各种系统甚至并行作业中累积，而不会覆盖当前帧中对伤害的其他更改。
    /// 2. 对角色施加伤害发生在单个系统（<see cref="ProcessDamageThisFrameSystem"/>）中，这为处理所有与承受伤害、考虑修改器等相关的情况提供了一个单点，而不是将此逻辑分散或放在多个系统中。
    /// </remarks>
    [InternalBufferCapacity(1)]
    public struct DamageThisFrame : IBufferElementData
    {
        public int Value;
    }

    /// <summary>
    /// 用于为实体添加受伤所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// <see cref="DestructibleEntityAuthoring"/> 是一个必需的组件，因为可受伤害的实体也应该是可摧毁的。
    /// </remarks>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DamageableEntityAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 初始生命值。
        /// </summary>
        /// <remarks>
        /// 设置 <see cref="CurrentHitPoints"/> 和 <see cref="BaseHitPoints"/>。
        /// </remarks>
        public int HitPoints;

        private class Baker : Baker<DamageableEntityAuthoring>
        {
            public override void Bake(DamageableEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CurrentHitPoints { Value = authoring.HitPoints });
                AddComponent(entity, new BaseHitPoints { Value = authoring.HitPoints });
                AddBuffer<DamageThisFrame>(entity);
            }
        }
    }

    /// <summary>
    /// 处理在 <see cref="DamageThisFrame"/> 动态缓冲区中累积的伤害并将该伤害应用到实体当前生命值的系统。
    /// </summary>
    /// <remarks>
    /// 此系统会累积要对实体造成的总伤害。实体可以接收负伤害点数以进行治疗。如果实体有任何属性修改（即 <see cref="CharacterStatModificationState"/>），则该值可以被修改。当前生命值将被限制在0和最大生命值（<see cref="BaseHitPoints"/> 加上任何生命值修改器）之间。
    /// 如果实体生命值耗尽，此系统将启用角色的 <see cref="DestroyEntityFlag"/>，以便在帧末尾清理该实体。
    /// 此系统还可以触发与受到伤害或治疗相关的VFX和SFX。此系统使用可启用组件来触发VFX和SFX，以便此系统可以完全进行Burst编译 - 这在这种情况下很重要，因为此系统将在所有可受伤害的实体（即玩家、敌人和世界中的可受伤害物品）上执行。
    /// </remarks>
    [UpdateAfter(typeof(DS_InteractionSystemGroup))]
    public partial struct ProcessDamageThisFrameSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (damageThisFrame, hitPoints, baseHitPoints, entity) in SystemAPI.Query<DynamicBuffer<DamageThisFrame>, RefRW<CurrentHitPoints>, BaseHitPoints>().WithEntityAccess())
            {
                if (damageThisFrame.IsEmpty) continue;

                var totalHitPoints = 0;
                foreach (var damage in damageThisFrame)
                {
                    var curDamage = damage.Value;

                    if (curDamage > 0)
                    {
                        // Ignore positive damage if character is invincible
                        if (SystemAPI.HasComponent<InvincibilityExpirationTimestamp>(entity) && SystemAPI.IsComponentEnabled<InvincibilityExpirationTimestamp>(entity))
                        {
                            continue;
                        }

                        // Armor will only reduce positive damage, but will not regenerate hit points
                        if (SystemAPI.HasComponent<CharacterStatModificationState>(entity))
                        {
                            var damageReceivedModifier = SystemAPI.GetComponent<CharacterStatModificationState>(entity).DamageReceived;
                            curDamage = math.max(0, curDamage - damageReceivedModifier);
                        }

                        // Display a white damage number popup above the character taking damage
                        if (SystemAPI.HasComponent<ShowDamageNumberOnDamage>(entity))
                        {
                            SystemAPI.SetComponentEnabled<ShowDamageNumberOnDamage>(entity, true);
                            var damageNumber = SystemAPI.GetComponentRW<ShowDamageNumberOnDamage>(entity);
                            damageNumber.ValueRW.DamageAmount = curDamage;
                        }
                    }
                    else if (curDamage < 0)
                    {
                        // Display a green damage number popup above the character healing
                        if (SystemAPI.HasComponent<ShowDamageNumberOnHeal>(entity))
                        {
                            SystemAPI.SetComponentEnabled<ShowDamageNumberOnHeal>(entity, true);
                            var damageNumber = SystemAPI.GetComponentRW<ShowDamageNumberOnHeal>(entity);
                            damageNumber.ValueRW.HealAmount = curDamage;
                        }
                    }

                    totalHitPoints += curDamage;
                }

                var maxHitPoints = baseHitPoints.Value;
                if (SystemAPI.HasComponent<CharacterStatModificationState>(entity))
                {
                    maxHitPoints += SystemAPI.GetComponent<CharacterStatModificationState>(entity).AdditionalHitPoints;
                }

                damageThisFrame.Clear();
                hitPoints.ValueRW.Value -= totalHitPoints;
                hitPoints.ValueRW.Value = math.clamp(hitPoints.ValueRO.Value, 0, maxHitPoints);

                if (totalHitPoints > 0)
                {
                    if (SystemAPI.HasComponent<GraphicsEntity>(entity))
                    {
                        var graphicsEntity = SystemAPI.GetComponent<GraphicsEntity>(entity).Value;
                        SystemAPI.SetComponentEnabled<FlashColorOnDamageData>(graphicsEntity, true);
                        var flashColorOnDamageTimer = SystemAPI.GetComponentRW<FlashColorOnDamageTimer>(graphicsEntity);
                        var flashTime = SystemAPI.GetComponent<FlashColorOnDamageData>(graphicsEntity).FlashTime;
                        flashColorOnDamageTimer.ValueRW.Value = flashTime;
                    }

                    if (SystemAPI.HasComponent<PlayAudioClipOnDamageData>(entity))
                    {
                        SystemAPI.SetComponentEnabled<PlayAudioClipOnDamageData>(entity, true);
                    }

                    if (SystemAPI.HasComponent<PlayParticleSystemOnDamage>(entity))
                    {
                        SystemAPI.SetComponentEnabled<PlayParticleSystemOnDamageFlag>(entity, true);
                    }
                }

                if (SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    SystemAPI.SetComponentEnabled<UpdatePlayerHealthUIFlag>(entity, true);
                }

                if (hitPoints.ValueRO.Value <= 0)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }

                if (!SystemAPI.HasComponent<CharacterHealthRegenerationState>(entity)) continue;
                var enableRegeneration = SystemAPI.GetComponent<CharacterStatModificationState>(entity).HealthRegeneration > 0f && hitPoints.ValueRO.Value < maxHitPoints;
                SystemAPI.SetComponentEnabled<CharacterHealthRegenerationState>(entity, enableRegeneration);
            }
        }
    }
}