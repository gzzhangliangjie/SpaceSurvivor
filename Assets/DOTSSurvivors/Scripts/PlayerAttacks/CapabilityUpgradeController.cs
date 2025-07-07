using System;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 从GameObject端处理玩家能力升级的托管控制器。此脚本是必需的，以作为游戏GameObject端和ECS端之间的桥梁。UI元素将调用此脚本中的方法，以在GameObject和ECS两端配置与升级能力（武器和被动物品）相关的事项。
    /// </summary>
    public class CapabilityUpgradeController : MonoBehaviour
    {
        /// <summary>
        /// 公共单例访问，以便UI元素可以调用此控制器上的方法。
        /// </summary>
        public static CapabilityUpgradeController Instance;

        /// <summary>
        /// 当玩家获得新武器时调用的事件。UI事件监听此事件以在HUD中显示新武器图标。
        /// </summary>
        public Action<WeaponUpgradeProperties> OnAddNewWeapon;
        /// <summary>
        /// 当玩家获得新被动物品时调用的事件。UI事件监听此事件以在HUD中显示新被动物品图标。
        /// </summary>
        public Action<PassiveUpgradeProperties> OnAddNewPassive;

        /// <summary>
        /// 默认世界的实体管理器。
        /// </summary>
        private EntityManager _entityManager;
        /// <summary>
        /// 奖励物品的实体原型。当所有物品槽已满且达到最高级时，奖励物品会显示在升级UI中。这用于在需要时实例化奖励物品。
        /// </summary>
        /// <seealso cref="BonusItemProperties"/>
        private EntityArchetype _bonusItemArchetype;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning, multiple instances of EntityUpgradeControllerManaged are present. Destroying new one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _bonusItemArchetype = _entityManager.CreateArchetype(ComponentType.ReadWrite<ItemTag>(), ComponentType.ReadWrite<DestroyEntityFlag>(), ComponentType.ReadWrite<EntityInteraction>(), ComponentType.ReadWrite<DestroySelfOnInteractionTag>());
        }

        /// <summary>
        /// 升级能力的"入口点"。根据能力类型，此方法将调用特定的方法。
        /// </summary>
        /// <param name="upgradeProperties">Upgrade properties ScriptableObject of the capability to be upgraded. Will either be of type <see cref="WeaponUpgradeProperties"/> or <see cref="PassiveUpgradeProperties"/>.</param>
        public void UpgradeCapability(UpgradeProperties upgradeProperties)
        {
            if (upgradeProperties is WeaponUpgradeProperties weaponUpgradeProperties)
            {
                UpgradeWeapon(weaponUpgradeProperties);
            }
            else if (upgradeProperties is PassiveUpgradeProperties passiveUpgradeProperties)
            {
                UpgradePassive(passiveUpgradeProperties);
            }
        }

        /// <summary>
        /// 升级武器的方法。在ECS端，如果需要，它将生成一个新的武器实体或启用 <see cref="UpgradeWeaponFlag"/>。在托管Unity端，它将调用事件以在UI中正确显示内容。
        /// </summary>
        /// <param name="upgradeProperties">Upgrade properties for the weapon to be upgraded</param>
        private void UpgradeWeapon(WeaponUpgradeProperties upgradeProperties)
        {
            var gameEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameEntityTag>());
            var gameEntity = gameEntityQuery.GetSingletonEntity();
            var upgradeData = _entityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);

            if (upgradeData.ActiveWeaponEntityLookup.TryGetValue(upgradeProperties, out var weaponEntity))
            {
                _entityManager.SetComponentEnabled<UpgradeWeaponFlag>(weaponEntity, true);
            }
            else
            {
                var gameControllerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameEntityTag>());
                var gameControllerEntity = gameControllerQuery.GetSingletonEntity();
                var weaponEntityPrefabLookup = _entityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).WeaponEntityPrefabLookup;
                var weaponPrefab = weaponEntityPrefabLookup[upgradeProperties.WeaponType];
                var newWeaponEntity = _entityManager.Instantiate(weaponPrefab);

                var playerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerTag>());
                var playerEntity = playerQuery.GetSingletonEntity();

                _entityManager.AddComponentData(newWeaponEntity, new Parent { Value = playerEntity });
                _entityManager.SetComponentData(newWeaponEntity, new WeaponState
                {
                    AttackCount = 0,
                    LevelIndex = 0,
                    CooldownTimer = 0.15f,
                    NextAttackTimer = 0f
                });
                upgradeData.ActiveWeaponEntityLookup.Add(upgradeProperties, newWeaponEntity);

                OnAddNewWeapon?.Invoke(upgradeProperties);
            }

            LevelUpUIController.Instance.HideLevelUpUI();
        }

        /// <summary>
        /// 升级被动物品的方法。在ECS端，如果需要，它将生成一个新的被动物品实体或启用 <see cref="UpgradePassiveFlag"/>。在托管Unity端，它将调用事件以在UI中正确显示内容。
        /// </summary>
        /// <param name="upgradeProperties">Upgrade properties for the passive item to be upgraded.</param>
        private void UpgradePassive(PassiveUpgradeProperties upgradeProperties)
        {
            var gameEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameEntityTag>(), ComponentType.ReadWrite<GameEntityPrefabs>());
            var gameEntity = gameEntityQuery.GetSingletonEntity();
            var upgradeData = _entityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);

            var playerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerTag>());
            var playerEntity = playerQuery.GetSingletonEntity();

            if (upgradeData.ActivePassiveEntityLookup.TryGetValue(upgradeProperties, out var passiveEntity))
            {
                _entityManager.SetComponentEnabled<UpgradePassiveFlag>(passiveEntity, true);
            }
            else
            {
                var passivePrefab = gameEntityQuery.GetSingleton<GameEntityPrefabs>().PassivePrefab;
                var newPassive = _entityManager.Instantiate(passivePrefab);
                _entityManager.SetComponentData(newPassive, new PassiveUpgradePropertiesReference { Value = upgradeProperties });

                var statModifierBuffer = _entityManager.GetBuffer<ActiveStatModifierEntity>(playerEntity);
                statModifierBuffer.Add(new ActiveStatModifierEntity { Value = newPassive });
                upgradeData.ActivePassiveEntityLookup.Add(upgradeProperties, newPassive);

                OnAddNewPassive?.Invoke(upgradeProperties);
            }

            _entityManager.SetComponentEnabled<RecalculateStatsFlag>(playerEntity, true);
            LevelUpUIController.Instance.HideLevelUpUI();
        }

        /// <summary>
        /// 授予玩家奖励物品的方法。当玩家升级或在所有能力槽已满且达到最高级时获得战利品箱时，会显示奖励物品。
        /// </summary>
        /// <param name="itemType">Type of bonus item - health or money.</param>
        /// <param name="itemValue">Value of health or money to grant to the player.</param>
        /// <seealso cref="BonusItemProperties"/>
        public void CollectBonusItem(BonusItemType itemType, int itemValue)
        {
            var playerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerTag>());
            var playerEntity = playerQuery.GetSingletonEntity();

            var newBonusItemEntity = _entityManager.CreateEntity(_bonusItemArchetype);
            _entityManager.SetComponentEnabled<DestroyEntityFlag>(newBonusItemEntity, false);
            var healthInteractionBuffer = _entityManager.GetBuffer<EntityInteraction>(newBonusItemEntity);
            healthInteractionBuffer.Add(new EntityInteraction { IsHandled = false, TargetEntity = playerEntity });

            switch (itemType)
            {
                case BonusItemType.Health:
                    _entityManager.AddComponentData(newBonusItemEntity, new HealOnInteraction { Value = itemValue });
                    break;

                case BonusItemType.Money:
                    _entityManager.AddComponentData(newBonusItemEntity, new GrantMoneyOnInteraction { Value = itemValue });
                    break;
            }
        }
    }
}