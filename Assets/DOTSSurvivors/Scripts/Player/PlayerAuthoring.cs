using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using Unity.Physics.GraphicsIntegration;
using Object = UnityEngine.Object;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于标识玩家实体的标签组件。
    /// </summary>
    /// <remarks>
    /// Several systems and MonoBehaviours assume there is only one player entity in the game world at a time.
    /// </remarks>
    public struct PlayerTag : IComponentData { }

    /// <summary>
    /// 关联实体不再对传入伤害无敌的时间戳。当实体对传入伤害无敌时，此组件将被启用；在实体可以受到伤害的正常行为下，此组件将被禁用。
    /// </summary>
    /// <remarks>
    /// Should the entity be considered invincible, incoming damage buffered in a <see cref="DamageThisFrame"/> dynamic buffer will simply be ignored in the <see cref="ProcessDamageThisFrameSystem"/>.
    /// </remarks>
    public struct InvincibilityExpirationTimestamp : IComponentData, IEnableableComponent
    {
        public double Value;
    }

    /// <summary>
    /// 数据组件，用于保存存在于世界空间画布上的玩家血条的变换和滑块的Unity对象引用。
    /// </summary>
    public struct PlayerWorldUI : ICleanupComponentData
    {
        /// <summary>
        /// 包含玩家血条的世界UI画布的变换。
        /// </summary>
        public UnityObjectRef<Transform> UICanvas;
        /// <summary>
        /// 玩家血条的UnityEngine.UI.Slider组件。
        /// </summary>
        public UnityObjectRef<Slider> HealthBarSlider;
    }

    /// <summary>
    /// 告知 <see cref="PlayerWorldUISystem"/> 更新生命值滑块的标志组件。
    /// </summary>
    /// <remarks>
    /// This is set to enabled when the player current or max health is changed due to being damaged or picking up an item.
    /// </remarks>
    public struct UpdatePlayerHealthUIFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 玩家经验的当前状态。
    /// </summary>
    /// <remarks>
    /// Experience points are granted in integers and the number of points required to reach the next level will always be an integer. Total experience points are stored as a float so that fractions of experience points can be accounted for. Player can achieve fractions of experience points through experience point modification (see <see cref="CharacterStatModificationState.ExperiencePointGain"/>
    /// </remarks>
    public struct PlayerExperienceState : IComponentData
    {
        /// <summary>
        /// 玩家在当前游戏运行中获得的总经验点数。
        /// </summary>
        public float TotalPoints;
        /// <summary>
        /// 玩家所在的等级。
        /// </summary>
        /// <remarks>
        /// Player starts the game at level 1 and level is incremented once <see cref="TotalPoints"/> is greater than or equal to <see cref="PointsToNextLevel"/>.
        /// </remarks>
        public int Level;
        /// <summary>
        /// 玩家升级所需的总经验点数。
        /// </summary>
        public int PointsToNextLevel;
    }

    /// <summary>
    /// 用于排队在给定帧中添加到玩家的经验点数的动态缓冲区。
    /// </summary>
    /// <remarks>
    /// Purpose of the dynamic buffer is so that experience points can potentially be granted from multiple threads and all be properly accounted for. This also leads to a single system (in this case <see cref="HandlePlayerExperienceThisFrameSystem"/>) for handling the addition of player experience to the player and performing any additional logic i.e. leveling up.
    /// </remarks>
    /// <seealso cref="HandlePlayerExperienceThisFrameSystem"/>
    [InternalBufferCapacity(1)]
    public struct PlayerExperienceThisFrame : IBufferElementData
    {
        public int Value;
    }

    /// <summary>
    /// 用于存储玩家先前输入的数据组件。
    /// </summary>
    public struct PreviousPlayerInput : IComponentData
    {
        /// <summary>
        /// 玩家在前一帧的输入。
        /// </summary>
        /// <remarks>
        /// This is required for the game to know when the player switches between moving and not moving states to change player's animation.
        /// </remarks>
        public float2 PreviousInput;
        /// <summary>
        /// 玩家的最后一次非零输入。
        /// </summary>
        /// <remarks>
        /// Used for firing attacks in the direction of the player's most recent input when not moving.
        /// </remarks>
        /// <seealso cref="ScrewdriverAttackSystem"/>
        public float2 LastPositiveInput;
        /// <summary>
        /// 玩家在x轴上最后一次非零输入的符号。
        /// </summary>
        /// <remarks>
        /// Used for attack and animation systems that need to know the player's facing direction. Only updates when x-input is non-zero. Will be -1 for left and +1 for right.
        /// </remarks>
        public float LastFacingDirection;
    }

    /// <summary>
    /// 将 <see cref="UpgradeProperties"/> ScriptableObject与其下一等级索引配对的帮助结构体。
    /// </summary>
    public struct CapabilityUpgradeLevel
    {
        public UpgradeProperties UpgradeProperties;
        public int NextLevelIndex;
    }

    /// <summary>
    /// 表示玩家当前是否正在打开战利品箱的标志组件。
    /// </summary>
    /// <remarks>
    /// 当玩家当前正在打开战利品箱并显示相关UI时，这个可启用的组件将被启用。
    /// 当玩家没有主动打开战利品箱时（即在正常游戏过程中），该组件将被禁用。
    /// 此组件的目的是停止 <see cref="HandlePlayerExperienceThisFrameSystem"/> 的执行，以避免玩家升级UI和战利品箱UI在同一帧显示的情况。
    /// </remarks>
    /// <seealso cref="CrateItemAuthoring"/>
    public struct IsOpeningCrateFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于向玩家实体添加特定组件的创作脚本。
    /// </summary>
    [RequireComponent(typeof(CharacterAuthoring))]
    public class PlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
                AddComponent<PlayerExperienceState>(entity);
                AddBuffer<PlayerExperienceThisFrame>(entity);
                AddComponent(entity, new PreviousPlayerInput
                {
                    PreviousInput = float2.zero,
                    LastPositiveInput = math.right().xz,
                    LastFacingDirection = 1f
                });
                AddComponent<UpdatePlayerHealthUIFlag>(entity);
                SetComponentEnabled<UpdatePlayerHealthUIFlag>(entity, true);
                AddComponent<InvincibilityExpirationTimestamp>(entity);
                SetComponentEnabled<InvincibilityExpirationTimestamp>(entity, false);
                AddComponent<PhysicsGraphicalInterpolationBuffer>(entity);
                AddComponent<IsOpeningCrateFlag>(entity);
                SetComponentEnabled<IsOpeningCrateFlag>(entity, false);
            }
        }
    }

    /// <summary>
    /// 初始化与玩家实体相关的值的系统。
    /// </summary>
    /// <remarks>
    /// System is a SystemBase system so it can have System.Action events as member variables that can be invoked when UI elements need to be updated.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    [UpdateBefore(typeof(CharacterInitializationSystem))]
    public partial class PlayerInitializationSystem : SystemBase
    {
        /// <summary>
        /// 用于初始化与玩家起始等级相对应的UI元素的事件。
        /// 传递的参数：
        /// 1. int - 玩家正在升级到的等级
        /// 2. int - 玩家正在升级到的等级的最低经验点数
        /// 3. int - 玩家正在升级到的等级的最高经验点数
        /// </summary>
        /// <seealso cref="HUDUIController.UpdatePlayerLevel"/>
        public Action<int, int, int> OnInitializePlayerLevel;

        protected override void OnCreate()
        {
            RequireForUpdate<ExperiencePointTable>();
        }

        protected override void OnUpdate()
        {
            var didInitializePlayer = false;

            foreach (var playerExperienceState in SystemAPI.Query<RefRW<PlayerExperienceState>>().WithAll<PlayerTag, InitializeCharacterFlag>())
            {
                var level2ExperiencePoints = SystemAPI.GetSingleton<ExperiencePointTable>()[1];
                playerExperienceState.ValueRW = new PlayerExperienceState
                {
                    TotalPoints = 0,
                    Level = 1,
                    PointsToNextLevel = level2ExperiencePoints
                };
                OnInitializePlayerLevel?.Invoke(1, 0, level2ExperiencePoints);
                didInitializePlayer = true;
            }

            // This must be called outside the above foreach loop otherwise exceptions will be thrown as EntityManager structural changes run in EntityUpgradeController.UpgradeCapability().
            if (didInitializePlayer)
            {
                var selectedCharacter = SystemAPI.GetSingleton<SelectedCharacterReference>().Value.Value;
                CapabilityUpgradeController.Instance.UpgradeCapability(selectedCharacter.StartingWeapon);
            }
        }
    }

    /// <summary>
    /// 从输入系统包中读取输入并将其分配给玩家的 <see cref="CharacterMoveDirection"/> 的系统。
    /// </summary>
    /// <remarks>
    /// System is a SystemBase type so it can have a managed member variable for the input actions.
    /// Updates in the <see cref="DS_InitializationSystemGroup"/> so input is read early in the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial class GetPlayerInputSystem : SystemBase
    {
        /// <summary>
        /// 系统将从中读取的输入操作。
        /// </summary>
        private DOTSSurvivorsInputActions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new DOTSSurvivorsInputActions();

            RequireForUpdate<PlayerTag>();
        }

        protected override void OnStartRunning()
        {
            _inputActions.Enable();
        }

        protected override void OnUpdate()
        {
            var curMovement = (float2)_inputActions.Player.Move.ReadValue<Vector2>();
            foreach (var (moveDirection, previousPlayerInput) in SystemAPI.Query<RefRW<CharacterMoveDirection>, RefRW<PreviousPlayerInput>>().WithAll<PlayerTag>())
            {
                previousPlayerInput.ValueRW.PreviousInput = moveDirection.ValueRO.Value;
                moveDirection.ValueRW.Value = curMovement;

                if (math.lengthsq(curMovement) > float.Epsilon)
                {
                    previousPlayerInput.ValueRW.LastPositiveInput = curMovement;
                }

                if (math.abs(curMovement.x) > float.Epsilon)
                {
                    previousPlayerInput.ValueRW.LastFacingDirection = math.sign(curMovement.x);
                }
            }
        }

        protected override void OnStopRunning()
        {
            _inputActions.Disable();
        }
    }

    /// <summary>
    /// 一旦达到无敌到期时间戳，就使玩家脱离无敌状态的系统。由于 <see cref="InvincibilityExpirationTimestamp"/> 是一个可启用组件，实体仅在组件启用时才被视为无敌；foreach循环也只会迭代启用了该组件的实体。
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InvincibilityExpirationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            foreach (var (invincibility, entity) in SystemAPI.Query<InvincibilityExpirationTimestamp>().WithEntityAccess())
            {
                if (elapsedTime > invincibility.Value)
                {
                    ecb.SetComponentEnabled<InvincibilityExpirationTimestamp>(entity, false);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary>
    /// 初始化、更新和清理玩家UI的系统 - 用于玩家的世界内生命条。
    /// </summary>
    /// <remarks>
    /// This system contains multiple foreach that do the following:
    /// 1. Instantiates an instance of the player world UI and sets up UnityObjectRef references in the <see cref="PlayerWorldUI"/> component.
    /// 2. Updates the position of the world UI instance to match that of the player, so the health bar appears to follow the player entity.
    /// 3. When the <see cref="UpdatePlayerHealthUIFlag"/> is enabled, update the current and max health values on the health bar slider UI element.
    /// 4. When the player is destroyed, destroy the player world UI instance so it no longer exists in the game world.
    /// </remarks>
    /// <seealso cref="PlayerWorldUI"/>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayerWorldUISystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameEntityTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // Instantiates an instance of the player world UI and sets up UnityObjectRef references in the PlayerWorldUI component.
            foreach (var (localToWorld, entity) in SystemAPI.Query<LocalToWorld>().WithAll<PlayerTag>().WithNone<PlayerWorldUI>().WithEntityAccess())
            {
                var gameControllerEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
                var playerWorldUIPrefab = state.EntityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).PlayerWorldUICanvasPrefab;
                var newPlayerWorldUI = Object.Instantiate(playerWorldUIPrefab, localToWorld.Position, Quaternion.Euler(90f, 0f, 0f));
                var uiTransform = newPlayerWorldUI.transform;
                var uiSlider = newPlayerWorldUI.GetComponentInChildren<Slider>();
                uiSlider.value = 1f;

                ecb.AddComponent(entity, new PlayerWorldUI
                {
                    UICanvas = new UnityObjectRef<Transform> { Value = uiTransform },
                    HealthBarSlider = new UnityObjectRef<Slider> { Value = uiSlider }
                });
            }

            // Updates the position of the world UI instance to match that of the player, so the health bar appears to follow the player entity.
            foreach (var (localToWorld, worldUIReference) in SystemAPI.Query<LocalToWorld, PlayerWorldUI>())
            {
                worldUIReference.UICanvas.Value.position = localToWorld.Position;
            }

            // When the UpdatePlayerHealthUIFlag is enabled, update the current and max health values on the health bar slider UI element.
            foreach (var (currentHitPoints, baseHitPoints, characterStats, playerWorldUIReference, forceUpdatePlayerHealthUIFlag) in SystemAPI.Query<CurrentHitPoints, BaseHitPoints, CharacterStatModificationState, PlayerWorldUI, EnabledRefRW<UpdatePlayerHealthUIFlag>>().WithAll<PlayerTag>())
            {
                forceUpdatePlayerHealthUIFlag.ValueRW = false;
                var maxHitPoints = baseHitPoints.Value + characterStats.AdditionalHitPoints;
                var hitPointSliderValue = (float)currentHitPoints.Value / maxHitPoints;
                playerWorldUIReference.HealthBarSlider.Value.value = hitPointSliderValue;
            }

            // When the player is destroyed, destroy the player world UI instance so it no longer exists in the game world.
            foreach (var (playerWorldUIReference, entity) in SystemAPI.Query<PlayerWorldUI>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                if (playerWorldUIReference.UICanvas.Value != null)
                {
                    Object.Destroy(playerWorldUIReference.UICanvas.Value.gameObject);
                }
                ecb.RemoveComponent<PlayerWorldUI>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary>
    /// 将玩家的 <see cref="PlayerExperienceThisFrame"/> 动态缓冲区中的经验点数添加到玩家的 <see cref="PlayerExperienceState.TotalPoints"/> 的系统。此系统还确定玩家是否应该升级。如果玩家应该升级，则此系统还将确定要升级的武器或被动能力。
    /// </summary>
    /// <remarks>
    /// System is a SystemBase system so it can have System.Action events as member variables that can be invoked when UI elements need to be updated.
    /// System checks to ensure <see cref="IsOpeningCrateFlag"/> is set to false to avoid the possibility of showing level up UI and crate opening UI on the same frame.
    /// </remarks>
    [UpdateAfter(typeof(DS_InteractionSystemGroup))]
    public partial class HandlePlayerExperienceThisFrameSystem : SystemBase
    {
        /// <summary>
        /// 在确定随机升级时，系统将尝试查找升级这么多次。如果在此数量后仍然失败，则系统可以推断玩家没有可用的进一步升级，因此将显示生命值和金钱奖励。
        /// </summary>
        private const int FAILED_RANDOM_COUNT = 500;
        /// <summary>
        /// 玩家升级后无敌的持续时间（秒）。
        /// </summary>
        private const double POST_LEVEL_UP_INVINCIBILITY_DURATION = 0.15;
        /// <summary>
        /// 当玩家升级时调用的事件。
        /// 传递的参数：
        /// 1. int - 玩家正在升级到的等级
        /// 2. int - 玩家正在升级到的等级的最低经验点数
        /// 3. int - 玩家正在升级到的等级的最高经验点数
        /// </summary>
        /// <seealso cref="HUDUIController.UpdatePlayerLevel"/>
        public Action<int, int, int> OnUpdatePlayerLevel;
        /// <summary>
        /// 更新玩家经验UI时调用的事件。
        /// 传递的参数：
        /// 1. float - 玩家当前经验点数相对于升级所需点数的标准化值。
        /// </summary>
        /// <seealso cref="HUDUIController.UpdatePlayerExperience"/>
        public Action<float> OnUpdatePlayerExperience;
        /// <summary>
        /// 当玩家开始升级序列时调用的事件。
        /// 传递的参数：
        /// 1. CapabilityUpgradeLevel[] - 玩家可以选择的3种能力升级的数组。
        /// </summary>
        /// <seealso cref="LevelUpUIController.Display"/>
        public Action<CapabilityUpgradeLevel[]> OnBeginLevelUp;

        protected override void OnCreate()
        {
            RequireForUpdate<ExperiencePointTable>();
            RequireForUpdate<GameEntityTag>();
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnUpdate()
        {
            foreach (var (playerExperienceThisFrame, playerExperienceState, characterStats, entity) in SystemAPI.Query<DynamicBuffer<PlayerExperienceThisFrame>, RefRW<PlayerExperienceState>, CharacterStatModificationState>().WithNone<InitializeCharacterFlag, IsOpeningCrateFlag>().WithEntityAccess())
            {
                var playerExperienceUpdated = false;
                foreach (var experiencePoint in playerExperienceThisFrame)
                {
                    playerExperienceState.ValueRW.TotalPoints += experiencePoint.Value * characterStats.ExperiencePointGain;
                    playerExperienceUpdated = true;
                }

                // Check for leveling up
                if (playerExperienceState.ValueRO.TotalPoints >= playerExperienceState.ValueRO.PointsToNextLevel)
                {
                    var experiencePointsForPrevLevel = playerExperienceState.ValueRO.PointsToNextLevel;
                    var nextLevel = playerExperienceState.ValueRW.Level += 1;
                    playerExperienceState.ValueRW.PointsToNextLevel = SystemAPI.GetSingleton<ExperiencePointTable>()[nextLevel] + experiencePointsForPrevLevel;
                    var minExperiencePointsForLevel = experiencePointsForPrevLevel;
                    var maxExperiencePointsForLevel = playerExperienceState.ValueRO.PointsToNextLevel;
                    OnUpdatePlayerLevel?.Invoke(nextLevel, minExperiencePointsForLevel, maxExperiencePointsForLevel);

                    var gameEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
                    var upgradeData = EntityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);
                    var random = SystemAPI.GetComponentRW<EntityRandom>(gameEntity);
                    var randomUpgrades = new List<CapabilityUpgradeLevel>();

                    for (var i = 0; i < 3; i++)
                    {
                        UpgradeProperties upgradePropertiesToTest;
                        int nextLevelIndex;
                        bool getNewRandomProperties;
                        var failCounter = 0;

                        do
                        {
                            failCounter++;
                            getNewRandomProperties = false;
                            upgradePropertiesToTest = upgradeData.GetRandomUpgradeProperties(ref random.ValueRW.Value);
                            nextLevelIndex = 0;

                            // Check to ensure the current upgrade isn't already selected
                            foreach (var previouslySelectedUpgrade in randomUpgrades)
                            {
                                if (previouslySelectedUpgrade.UpgradeProperties == upgradePropertiesToTest)
                                {
                                    getNewRandomProperties = true;
                                    break;
                                }
                            }

                            if (!getNewRandomProperties)
                            {
                                if (upgradePropertiesToTest is WeaponUpgradeProperties weaponPropertiesToTest)
                                {
                                    // If the player already has this weapon, select it if it is not at the max level
                                    if (upgradeData.ActiveWeaponEntityLookup.TryGetValue(weaponPropertiesToTest, out var weaponEntity))
                                    {
                                        var curWeaponLevelIndex = SystemAPI.GetComponent<WeaponState>(weaponEntity).LevelIndex;
                                        var maxLevelIndex = SystemAPI.GetComponent<WeaponUpgradePropertiesReference>(weaponEntity).Value.Value.MaxLevelIndex;
                                        if (curWeaponLevelIndex >= maxLevelIndex)
                                        {
                                            getNewRandomProperties = true;
                                        }
                                        else
                                        {
                                            // Store the next level to be displayed in Level Up UI
                                            nextLevelIndex = curWeaponLevelIndex + 1;
                                        }
                                    }
                                    // If the player does not have the weapon and no free slots, try again
                                    else if (!upgradeData.HasFreeWeaponSlots)
                                    {
                                        getNewRandomProperties = true;
                                    }
                                    // Else this is a new weapon the player can select
                                }
                                else if (upgradePropertiesToTest is PassiveUpgradeProperties passivePropertiesToTest)
                                {
                                    // If the player already has this passive ability, select it if it is not at the max level
                                    if (upgradeData.ActivePassiveEntityLookup.TryGetValue(passivePropertiesToTest, out var passiveEntity))
                                    {
                                        var curPassiveLevelIndex = SystemAPI.GetComponent<PassiveLevelIndex>(passiveEntity).Value;
                                        var passiveMaxLevelIndex = SystemAPI.GetComponent<PassiveUpgradePropertiesReference>(passiveEntity).Value.Value.MaxLevelIndex;
                                        if (curPassiveLevelIndex >= passiveMaxLevelIndex)
                                        {
                                            getNewRandomProperties = true;
                                        }
                                        else
                                        {
                                            // Store the next level to be displayed in Level Up UI
                                            nextLevelIndex = curPassiveLevelIndex + 1;
                                        }
                                    }
                                    // If the player does not have the passive and no free slots, try again
                                    else if (!upgradeData.HasFreePassiveSlots)
                                    {
                                        getNewRandomProperties = true;
                                    }
                                    // Else this is a new passive the player can select
                                }
                                else
                                {
                                    Debug.LogError("Error: undefined upgrade type");
                                    getNewRandomProperties = true;
                                }
                            }

                            if (failCounter >= FAILED_RANDOM_COUNT) upgradePropertiesToTest = null;
                        } while (getNewRandomProperties && failCounter < FAILED_RANDOM_COUNT);

                        if (upgradePropertiesToTest != null)
                        {
                            randomUpgrades.Add(new CapabilityUpgradeLevel
                            {
                                UpgradeProperties = upgradePropertiesToTest,
                                NextLevelIndex = nextLevelIndex
                            });
                        }
                    }

                    OnBeginLevelUp?.Invoke(randomUpgrades.ToArray());
                    SystemAPI.SetComponent(entity, new InvincibilityExpirationTimestamp { Value = SystemAPI.Time.ElapsedTime + POST_LEVEL_UP_INVINCIBILITY_DURATION });
                    SystemAPI.SetComponentEnabled<InvincibilityExpirationTimestamp>(entity, true);
                }

                // Update player experience slider at top of screen if the player gained experience this frame.
                if (playerExperienceUpdated)
                {
                    OnUpdatePlayerExperience?.Invoke(playerExperienceState.ValueRO.TotalPoints);
                }

                playerExperienceThisFrame.Clear();
            }
        }
    }
}