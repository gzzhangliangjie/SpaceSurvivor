using System;
using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于识别板条箱实体的标签组件。
    /// </summary>
    /// <remarks>
    /// 板条箱实体有几率由一波生成中的boss敌人掉落。当玩家拾取板条箱时，将播放一个序列，并为玩家已选择的随机武器或被动能力提供升级。如果玩家当前拥有的所有武器或被动物品都已满级，则会授予额外的金钱。
    /// </remarks>
    public struct CrateItemTag : IComponentData { }

    /// <summary>
    /// 板条箱实体的Authoring脚本。
    /// </summary>
    /// <remarks>
    /// 需要额外的Authoring脚本来添加所需行为的必要组件。
    /// </remarks>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class CrateItemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CrateItemAuthoring>
        {
            public override void Bake(CrateItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CrateItemTag>(entity);
            }
        }
    }

    /// <summary>
    /// 包含玩家收集板条箱时逻辑的系统。
    /// </summary>
    /// <remarks>
    /// 该系统是SystemBase类型，因为它有一个System.Action事件类型作为成员变量，当玩家收集板条箱时会调用该事件。
    /// 该系统首先确定将授予玩家哪个升级。如果玩家当前的所有武器和被动能力都已达到最高等级，则会向玩家授予额外的金钱。
    /// </remarks>
    /// <seealso cref="CrateUIController"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial class HandleCrateItemInteractionSystem : SystemBase
    {
        /// <summary>
        /// 当玩家收集板条箱时调用的事件。通知 <see cref="CrateUIController"/> 开始开箱序列。
        /// </summary>
        public Action<CapabilityUpgradeLevel> OnBeginOpenCrate;

        /// <summary>
        /// 在开箱对话框关闭并恢复游戏后，玩家将无敌的持续时间（秒）。
        /// </summary>
        private const float INVINCIBILITY_DURATION = 0.15f;

        protected override void OnCreate()
        {
            RequireForUpdate<GameEntityTag>();
            RequireForUpdate<CrateItemTag>();

            EntityManager.AddComponent<EntityRandom>(SystemHandle);
            EntityManager.AddComponentData(SystemHandle, new InitializeEntityRandom
            {
                InitializationType = EntityRandomInitializationType.SystemMilliseconds
            });
        }

        protected override void OnUpdate()
        {
            var gameEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(World.Unmanaged);

            foreach (var interactionBuffer in SystemAPI.Query<DynamicBuffer<EntityInteraction>>().WithAll<CrateItemTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<PlayerTag>(interaction.TargetEntity)) continue;

                    SystemAPI.SetComponentEnabled<IsOpeningCrateFlag>(interaction.TargetEntity, true);

                    var upgradeData = EntityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);

                    var nonMaxedUpgradeProperties = new List<CapabilityUpgradeLevel>();

                    foreach (var weaponEntityPropertyPair in upgradeData.ActiveWeaponEntityLookup)
                    {
                        var weaponState = SystemAPI.GetComponent<WeaponState>(weaponEntityPropertyPair.Value);
                        if (weaponState.LevelIndex >= weaponEntityPropertyPair.Key.MaxLevelIndex) continue;
                        nonMaxedUpgradeProperties.Add(new CapabilityUpgradeLevel
                        {
                            UpgradeProperties = weaponEntityPropertyPair.Key,
                            NextLevelIndex = weaponState.LevelIndex + 1
                        });
                    }

                    foreach (var passiveUpgradeEntityPair in upgradeData.ActivePassiveEntityLookup)
                    {
                        var curPassiveLevelIndex = SystemAPI.GetComponent<PassiveLevelIndex>(passiveUpgradeEntityPair.Value).Value;
                        if (curPassiveLevelIndex >= passiveUpgradeEntityPair.Key.MaxLevelIndex) continue;
                        nonMaxedUpgradeProperties.Add(new CapabilityUpgradeLevel
                        {
                            UpgradeProperties = passiveUpgradeEntityPair.Key,
                            NextLevelIndex = curPassiveLevelIndex + 1
                        });
                    }

                    CapabilityUpgradeLevel randomUpgrade = default;
                    if (nonMaxedUpgradeProperties.Count > 0)
                    {
                        var random = SystemAPI.GetComponentRW<EntityRandom>(SystemHandle);
                        var randomIndex = random.ValueRW.Value.NextInt(nonMaxedUpgradeProperties.Count);
                        randomUpgrade = nonMaxedUpgradeProperties[randomIndex];
                    }

                    OnBeginOpenCrate?.Invoke(randomUpgrade);

                    var invincibilityExpirationTimestamp = SystemAPI.GetComponentRW<InvincibilityExpirationTimestamp>(interaction.TargetEntity);
                    invincibilityExpirationTimestamp.ValueRW.Value = SystemAPI.Time.ElapsedTime + INVINCIBILITY_DURATION;
                    SystemAPI.SetComponentEnabled<InvincibilityExpirationTimestamp>(interaction.TargetEntity, true);

                    // Delay the disabling of the IsOpeningCrateFlag to the next execution of the BeginInitializationEntityCommandBuffer
                    // As the game is paused (Initialization system group disabled) during the crate opening sequence, this ECB will not
                    // execute until the game resumes. This flag is checked when attempting to level up the player to ensure the level
                    // up and crate UI screens are not displayed at the same time.
                    ecb.SetComponentEnabled<IsOpeningCrateFlag>(interaction.TargetEntity, false);
                }
            }
        }
    }
}