using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储玩家武器相关数据的数据组件。
    /// </summary>
    /// <remarks>
    /// Data in this component represents a single level for a weapon. A collection of WeaponLevelData components are stored in the <see cref="WeaponUpgradeData"/> to represent the all WeaponLevelData components in a weapon's upgrade path.
    /// Not all fields are used on all weapon types.
    /// Has the System.Serializable attribute so these values can be initialized in the editor via <see cref="WeaponUpgradeProperties"/>.
    /// </remarks>
    [Serializable]
    public struct WeaponLevelData : IComponentData
    {
        /// <summary>
        /// 武器的冷却时间（秒）。这是攻击组的最后一个攻击实体生成与下一攻击组的第一个攻击实体生成之间的时间间隔。
        /// </summary>
        public float Cooldown;
        /// <summary>
        /// 攻击实体在自我摧毁前在游戏世界中存在的时间。
        /// </summary>
        public float TimeToLive;
        /// <summary>
        /// 攻击组中攻击实体生成之间的间隔时间（秒）。
        /// </summary>
        public float IntervalBetweenAttacks;
        /// <summary>
        /// 移动攻击实体的移动速度（单位每秒）。
        /// </summary>
        public float MovementSpeed;
        /// <summary>
        /// 单个攻击组中将生成的攻击实体数量。
        /// </summary>
        public int AttackCount;
        /// <summary>
        /// 将分配给攻击实体的基础生命点，通过 <see cref="EntityInteraction"/> 对敌人实体造成伤害。
        /// </summary>
        public int BaseHitPoints;
        /// <summary>
        /// 攻击实体的缩放修改器。
        /// </summary>
        public float Area;
        /// <summary>
        /// 攻击实体在自毁前可以与之碰撞的敌人实体的最大数量。
        /// </summary>
        public int MaxEnemyHitCount;
        /// <summary>
        /// 用于确定某些随机行为是否应该发生的随机几率。
        /// </summary>
        /// <remarks>
        /// Value will be from 0 to 100 where 0 will never occur and 100 will always occur.
        /// </remarks>
        public int RandomChance;
        /// <summary>
        /// 应用于攻击实体的碰撞过滤器。
        /// </summary>
        public CollisionFilter CollisionFilter;
    }

    /// <summary>
    /// 将由攻击系统生成的游戏世界内攻击实体的预制体。
    /// </summary>
    /// <remarks>
    /// Many of these in-world attacks are projectiles that collide with enemies, however some like the black hole attack have slightly different behavior.
    /// </remarks>
    public struct AttackPrefab : IComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// 用于引用武器的 <see cref="WeaponUpgradeProperties"/> 的数据组件。
    /// </summary>
    /// <remarks>
    /// This is primarily used for UI display of weapon upgrades as runtime weapon data is stored in <see cref="WeaponUpgradeData"/>.
    /// </remarks>
    public struct WeaponUpgradePropertiesReference : IComponentData
    {
        public UnityObjectRef<WeaponUpgradeProperties> Value;
    }

    /// <summary>
    /// 用于保存武器当前状态的数据组件。
    /// </summary>
    /// <remarks>
    /// Not all fields are relevant to all weapon types.
    /// </remarks>
    public struct WeaponState : IComponentData
    {
        /// <summary>
        /// 跟踪下一个攻击组开始之前时间的计时器（秒）。
        /// </summary>
        public float CooldownTimer;
        /// <summary>
        /// 跟踪下一个攻击实体应该生成的时间的计时器。
        /// </summary>
        public float NextAttackTimer;
        /// <summary>
        /// 跟踪当前攻击组中已生成攻击实体数量的值。
        /// </summary>
        public int AttackCount;
        /// <summary>
        /// 存储武器当前等级索引的值。等级1（最低等级）在此字段中存储为0。
        /// </summary>
        public int LevelIndex;
    }

    /// <summary>
    /// 标记此武器当前处于激活状态的标志组件。
    /// </summary>
    /// <remarks>
    /// Enableable component will be marked as active when <see cref="WeaponState.CooldownTimer"/> is expired and the weapon should perform its attack behavior (typically spawning <see cref="AttackPrefab"/> entities).
    /// </remarks>
    public struct WeaponActiveFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 标记此武器应升级到下一个 <see cref="WeaponLevelData"/> 等级的标志组件。
    /// </summary>
    /// <remarks>
    /// Enableable component will be marked as active when the weapon should be upgraded.
    /// </remarks>
    public struct UpgradeWeaponFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于保存武器升级路径 <see cref="WeaponLevelData"/> 组件的blob数组的blob资源引用的数据组件。
    /// </summary>
    /// <remarks>
    /// Blob asset created during baking in <see cref="WeaponAuthoring"/>.
    /// </remarks>
    public struct WeaponUpgradeData : IComponentData
    {
        /// <summary>
        /// 对武器升级路径的 <see cref="WeaponLevelData"/> blob数组的引用。数组的元素0对应攻击等级1的武器等级数据，索引1是等级2，以此类推。
        /// </summary>
        public BlobAssetReference<BlobArray<WeaponLevelData>> Value;
        /// <summary>
        /// 用于轻松访问武器的 <see cref="WeaponLevelData"/> 的自定义索引器。
        /// </summary>
        /// <param name="i">Index into the blob array. Index 0 is level 1 for a weapon, index 1 is level 2, and so on.</param>
        public WeaponLevelData this[int i] => Value.Value[i];
    }

    /// <summary>
    /// 用于添加所有武器实体所需组件的基础创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this component will be attached to the base weapon entity which exists from instantiation through the remainder of a game run. This should not be attached to <see cref="AttackPrefab"/>s that spawn as visible objects in the game world.
    /// The baker of this authoring script will allocate a blob array of <see cref="WeaponLevelData"/> and populate it with data from the <see cref="WeaponUpgradeProperties"/> field of this authoring script. It is important that we include DependsOn(authoring.WeaponUpgradeProperties) so baking is re-ran when values on the ScriptableObject are changed.
    /// </remarks>
    public class WeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 调试字段，用于设置武器添加到玩家武器库存时将开始的等级。用于测试武器的更高等级，无需在正常游戏中升级。
        /// </summary>
        /// <remarks>
        /// This field is for debugging purposes only and should be set to 0 before shipping. 
        /// </remarks>
        public int StartingLevel;
        /// <summary>
        /// 对定义武器属性的ScriptableObject的引用。
        /// </summary>
        /// <remarks>
        /// Unmanaged data from this ScriptableObject get loaded into a blob asset during baking. It is important that we include DependsOn(authoring.WeaponUpgradeProperties) so baking is re-ran when values on the ScriptableObject are changed.
        /// </remarks>
        public WeaponUpgradeProperties WeaponUpgradeProperties;

        private class Baker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                DependsOn(authoring.WeaponUpgradeProperties);
                DependsOn(authoring.gameObject);

                var belongsToLayer = authoring.gameObject.layer;
                var belongsToLayerMask = (uint)math.pow(2, belongsToLayer);
                var collidesWithLayerMask = (uint)PhysicsHelper.GetCollisionMaskForLayer(belongsToLayer);
                var collisionFilter = new CollisionFilter
                {
                    BelongsTo = belongsToLayerMask,
                    CollidesWith = collidesWithLayerMask
                };

                var builder = new BlobBuilder(Allocator.Temp);
                ref var blobArrayRoot = ref builder.ConstructRoot<BlobArray<WeaponLevelData>>();
                var arrayBuilder = builder.Allocate(ref blobArrayRoot, authoring.WeaponUpgradeProperties.LevelPropertiesArray.Length);

                for (var i = 0; i < authoring.WeaponUpgradeProperties.LevelPropertiesArray.Length; i++)
                {
                    var curUpgradeProperties = authoring.WeaponUpgradeProperties.LevelPropertiesArray[i].WeaponLevelData;
                    curUpgradeProperties.CollisionFilter = collisionFilter;
                    arrayBuilder[i] = curUpgradeProperties;
                }

                var weaponLevelDataBlobArray = builder.CreateBlobAssetReference<BlobArray<WeaponLevelData>>(Allocator.Persistent);
                AddBlobAsset(ref weaponLevelDataBlobArray, out _);
                builder.Dispose();

                AddComponent(entity, new WeaponUpgradeData { Value = weaponLevelDataBlobArray });
                var startingWeaponData = weaponLevelDataBlobArray.Value[authoring.StartingLevel];
                AddComponent(entity, startingWeaponData);
                AddComponent(entity, new WeaponUpgradePropertiesReference { Value = authoring.WeaponUpgradeProperties });

                var attackEntity = GetEntity(authoring.WeaponUpgradeProperties.AttackPrefab, TransformUsageFlags.Dynamic);
                AddComponent(entity, new AttackPrefab { Value = attackEntity });
                AddComponent(entity, new WeaponState
                {
                    CooldownTimer = startingWeaponData.Cooldown,
                    NextAttackTimer = 0f,
                    AttackCount = 0,
                    LevelIndex = authoring.StartingLevel
                });
                AddComponent<WeaponActiveFlag>(entity);
                SetComponentEnabled<WeaponActiveFlag>(entity, false);
                AddComponent<UpgradeWeaponFlag>(entity);
                SetComponentEnabled<UpgradeWeaponFlag>(entity, false);
                AddBuffer<Child>(entity);
            }
        }

        private void OnValidate()
        {
            StartingLevel = Mathf.Clamp(StartingLevel, 0, WeaponUpgradeProperties.LevelPropertiesArray.Length - 1);
        }
    }

    /// <summary>
    /// 确定武器何时应激活的系统。
    /// </summary>
    /// <remarks>
    /// A weapon is active when its <see cref="WeaponActiveFlag"/> is enabled; at this time it will begin spawning instances of <see cref="AttackPrefab"/>s into the game world. Spawning of attack instances take place in systems unique to the weapon type.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup), OrderFirst = true)]
    public partial struct WeaponActivationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (weaponState, weaponData, parent, entity) in SystemAPI.Query<RefRW<WeaponState>, WeaponLevelData, Parent>().WithNone<WeaponActiveFlag>().WithEntityAccess())
            {
                weaponState.ValueRW.CooldownTimer -= deltaTime;
                if (weaponState.ValueRO.CooldownTimer > 0f) continue;
                SystemAPI.SetComponentEnabled<WeaponActiveFlag>(entity, true);
                var cooldownModifier = SystemAPI.GetComponent<CharacterStatModificationState>(parent.Value).AttackCooldown;
                weaponState.ValueRW.CooldownTimer = weaponData.Cooldown * cooldownModifier;
            }
        }
    }

    /// <summary>
    /// 在 <see cref="UpgradeWeaponFlag"/> 设置为true时升级运行时武器数据的系统。
    /// </summary>
    /// <remarks>
    /// Note that the <see cref="WeaponActiveFlag"/> is not an EnabledRefRW in the foreach query. This is because this isn't guaranteed to be active during upgrading. However, it is set to false to effectively reset its state if it is in the middle of being active. Note that we also set <see cref="WeaponState.CooldownTimer"/> to a very low number so the weapon will become active almost immediately after upgrading.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct UpgradeWeaponSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (upgradeFlag, weaponData, weaponState, propertiesBlob, entity) in SystemAPI.Query<EnabledRefRW<UpgradeWeaponFlag>, RefRW<WeaponLevelData>, RefRW<WeaponState>, WeaponUpgradeData>().WithEntityAccess())
            {
                upgradeFlag.ValueRW = false;
                SystemAPI.SetComponentEnabled<WeaponActiveFlag>(entity, false);
                weaponState.ValueRW.LevelIndex += 1;
                weaponState.ValueRW.CooldownTimer = 0.15f;
                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponData.ValueRW = propertiesBlob[weaponState.ValueRO.LevelIndex];
            }
        }
    }
}