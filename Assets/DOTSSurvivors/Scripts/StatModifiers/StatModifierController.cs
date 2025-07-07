using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Linq;
using System.Collections.Generic;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 属性修改器控制器单例。具有此组件的对象将被标记为Don't Destroy on Load，因为所有场景都需要访问从此控制器加载的 <see cref="StatModifierProperties"/> ScriptableObjects。
    /// </summary>
    public class StatModifierController : MonoBehaviour
    {
        /// <summary>
        /// 公共静态单例访问。
        /// </summary>
        public static StatModifierController Instance;

        /// <summary>
        /// 从Resources/ScriptableObjects/StatModifierProperties文件夹加载的 <see cref="StatModifierProperties"/> 数组。
        /// </summary>
        public static StatModifierProperties[] StatModifierProperties { get; private set; }

        /// <summary>
        /// 用于查找给定 <see cref="StatModifierType"/> 的 <see cref="StatModifierProperties"/> 的字典查找。
        /// </summary>
        private Dictionary<StatModifierType, StatModifierProperties> _statModifierPropertiesLookup;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            LoadStatModifications();
        }

        /// <summary>
        /// 在Awake()中调用的方法，用于从资源文件夹加载 <see cref="StatModifierProperties"/>，填充到数组和字典中。
        /// </summary>
        /// <remarks>
        /// Error will be logged in the console if multiple ScriptableObjects with a single <see cref="StatModifierType"/> are detected.
        /// </remarks>
        private void LoadStatModifications()
        {
            StatModifierProperties = Resources.LoadAll("ScriptableObjects/StatModifierProperties", typeof(StatModifierProperties)).Cast<StatModifierProperties>().ToArray();
            var characterDefaultModificationValues = new CharacterDefaultModificationValues();

            _statModifierPropertiesLookup = new Dictionary<StatModifierType, StatModifierProperties>();
            foreach (var statModifierProperties in StatModifierProperties)
            {
                if (!_statModifierPropertiesLookup.TryAdd(statModifierProperties.ModifierType, statModifierProperties))
                {
                    Debug.LogError($"Error: multiple stat modifiers of type: {statModifierProperties.ModifierType} detected");
                    continue;
                }

                var defaultValue = statModifierProperties.CalculationType.GetDefaultModificationValue();
                characterDefaultModificationValues.SetDefaultValue(statModifierProperties.ModifierType, defaultValue);
            }

            var characterDefaultStatsEntity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity(typeof(CharacterDefaultModificationValues));
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(characterDefaultStatsEntity, characterDefaultModificationValues);
        }

        /// <summary>
        /// 获取关联的 <see cref="StatModifierProperties"/> 中定义的最小修改值。
        /// </summary>
        /// <param name="statModifierType">要获取最小修改值的属性修改类型</param>
        /// <returns>所需属性修改类型的最小修改值</returns>
        public float GetMinimumModificationValue(StatModifierType statModifierType)
        {
            if (!_statModifierPropertiesLookup.TryGetValue(statModifierType, out var statModifierProperties))
            {
                Debug.LogError($"Error unable to find stat modifier type: {statModifierType}");
                return 0f;
            }

            return statModifierProperties.MinimumModificationValue;
        }

        /// <summary>
        /// 获取关联的 <see cref="StatModifierProperties"/> 中定义的最大修改值。
        /// </summary>
        /// <param name="statModifierType">要获取最大修改值的属性修改类型</param>
        /// <returns>所需属性修改类型的最大修改值</returns>
        public float GetMaximumModificationValue(StatModifierType statModifierType)
        {
            if (!_statModifierPropertiesLookup.TryGetValue(statModifierType, out var statModifierProperties))
            {
                Debug.LogError($"Error unable to find stat modifier type: {statModifierType}");
                return 0f;
            }

            return statModifierProperties.MaximumModificationValue;
        }
    }

    /// <summary>
    /// 用于向 <see cref="StatModifierType"/> 和 <see cref="StatModifierCalculationType"/> 枚举添加扩展方法的帮助类。
    /// </summary>
    public static class StatModifierHelper
    {
        /// <summary>
        /// 获取属性修改类型默认修改值的扩展方法。使用 <see cref="StatModifierCalculationType"/>，因为每种类型都将具有相同的默认值。
        /// </summary>
        /// <remarks>
        /// Default value for percentage types is 1f as when it is multiplied by the base value, the base vale will remain unchanged. For other types where stat modifications are added to the base value, the value is initialized to a default value of 0f.
        /// </remarks>
        /// <param name="calculationType">属性修改器的值类型。</param>
        /// <returns>默认修改值。</returns>
        public static float GetDefaultModificationValue(this StatModifierCalculationType calculationType)
        {
            return calculationType switch
            {
                StatModifierCalculationType.AbsoluteInteger => 0f,
                StatModifierCalculationType.AbsoluteDecimal => 0f,
                StatModifierCalculationType.IncreasingInteger => 0f,
                StatModifierCalculationType.Percentage => 1f,
                _ => 0f
            };
        }

        /// <summary>
        /// 在 <see cref="StatModifierProperties"/> ScriptableObject中定义的最小值和最大值范围内钳制修改值的扩展方法。
        /// </summary>
        /// <param name="statModifierType">属性修改器类型，用于查找该类型的最小值和最大值。</param>
        /// <param name="value">要钳制的值。变量是引用类型，因此调用此方法时传入的变量将被更新为在最小值和最大值范围内。</param>
        public static void ClampModificationValue(this StatModifierType statModifierType, ref float value)
        {

            var minimumValue = StatModifierController.Instance.GetMinimumModificationValue(statModifierType);
            var maximumValue = StatModifierController.Instance.GetMaximumModificationValue(statModifierType);
            value = math.clamp(value, minimumValue, maximumValue);

        }
    }

    /// <summary>
    /// 用于保存每个 <see cref="StatModifierType"/> 默认修改值的组件。
    /// </summary>
    /// <remarks>
    /// This is used as a baseline component to initialize <see cref="CharacterStatModificationState"/> before calculating stat modifications.
    /// </remarks>
    public struct CharacterDefaultModificationValues : IComponentData
    {
        /// <summary>
        /// 移动速度修改。此值在 <see cref="CharacterMoveSystem"/> 中与角色的 <see cref="CharacterBaseMoveSpeed"/> 相乘，以计算当前有效移动速度。
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// 伤害造成修改。此值在各种攻击系统中与攻击的基础伤害值相乘，以计算要分配给该攻击实体的当前有效伤害。
        /// </summary>
        public float DamageDealt;
        /// <summary>
        /// 增加角色最大生命值的额外生命点。
        /// </summary>
        public int AdditionalHitPoints;
        /// <summary>
        /// 按此整数值减少受到的伤害点数。
        /// </summary>
        public int DamageReceived;
        /// <summary>
        /// 生命恢复修改。此值表示在 <see cref="CharacterHealthRegenerationSystem"/> 中每秒恢复多少生命点。
        /// </summary>
        public float HealthRegeneration;
        /// <summary>
        /// 攻击冷却修改。此值在各种攻击系统中与攻击的基础冷却时间相乘，以计算该攻击的当前有效冷却时间。
        /// </summary>
        public float AttackCooldown;
        /// <summary>
        /// 攻击弹射物速度修改。此值在各种攻击系统中与攻击的基础弹射物速度相乘，以计算攻击实体的当前有效弹射物速度。
        /// </summary>
        public float AttackProjectileSpeed;
        /// <summary>
        /// 攻击持续时间修改。此值在各种攻击系统中与攻击的基础持续时间相乘，以计算攻击实体在世界中存在时间的当前有效持续时间。
        /// </summary>
        public float AttackDuration;
        /// <summary>
        /// 每次适用攻击要生成的额外攻击弹射物数量。
        /// </summary>
        public int AdditionalAttackProjectiles;
        /// <summary>
        /// 攻击区域修改。此值在各种攻击系统中与攻击的基础攻击区域相乘，以计算攻击的当前缩放。
        /// </summary>
        public float AttackArea;
        /// <summary>
        /// 物品吸引半径修改器。此值在 <see cref="DetectItemAttractionSystem"/> 中与玩家的 <see cref="PlayerAttractionAreaData.Radius"/> 相乘，以计算玩家的有效吸引区域。
        /// </summary>
        public float ItemAttractionRadius;
        /// <summary>
        /// 经验点获得修改器。此值在 <see cref="HandlePlayerExperienceThisFrameSystem"/> 中与玩家收集的经验点值相乘，以计算收集宝石的有效经验点获得。
        /// </summary>
        public float ExperiencePointGain;

        /// <summary>
        /// 用于轻松初始化给定 <see cref="StatModifierType"/> 值的帮助方法。
        /// </summary>
        /// <param name="type">Type of stat modification to initialize.</param>
        /// <param name="value">Value of the stat modifier type.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if stat modifier type is invalid.</exception>
        public void SetDefaultValue(StatModifierType type, float value)
        {
            type.ClampModificationValue(ref value);
            switch (type)
            {
                case StatModifierType.DamageDealt:
                    DamageDealt = value;
                    break;
                case StatModifierType.DamageReceived:
                    DamageReceived = (int)value;
                    break;
                case StatModifierType.AdditionalHitPoints:
                    AdditionalHitPoints = (int)value;
                    break;
                case StatModifierType.HealthRegeneration:
                    HealthRegeneration = value;
                    break;
                case StatModifierType.AttackCooldown:
                    AttackCooldown = value;
                    break;
                case StatModifierType.AttackArea:
                    AttackArea = value;
                    break;
                case StatModifierType.AttackProjectileSpeed:
                    AttackProjectileSpeed = value;
                    break;
                case StatModifierType.MoveSpeed:
                    MoveSpeed = value;
                    break;
                case StatModifierType.AttackDuration:
                    AttackDuration = value;
                    break;
                case StatModifierType.AdditionalAttackProjectiles:
                    AdditionalAttackProjectiles = (int)value;
                    break;
                case StatModifierType.ItemAttractionRadius:
                    ItemAttractionRadius = value;
                    break;
                case StatModifierType.ExperiencePointGain:
                    ExperiencePointGain = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 用于轻松获取给定 <see cref="StatModifierType"/> 默认值的帮助方法。
        /// </summary>
        /// <param name="type">Stat modifier type to get the default value of.</param>
        /// <returns>Default value of the desired stat modifier type.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws if invalid stat modifier type is passed into this function.</exception>
        public float GetDefaultValue(StatModifierType type)
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
    /// 定义属性修改器的计算类型。
    /// </summary>
    public enum StatModifierCalculationType : byte
    {
        /// <summary>
        /// 无 - 无效的属性修改器计算类型。
        /// </summary>
        None = 0,
        /// <summary>
        /// 属性修改类型是绝对整数。这意味着此值将被添加到属性修改的基础值中，并且在UI中显示完整值作为整数，而不仅仅是差值。
        /// </summary>
        AbsoluteInteger = 1,
        /// <summary>
        /// 属性修改类型是绝对小数。这意味着此值将被添加到属性修改的基础值中，并且在UI中显示完整值作为小数，而不仅仅是差值。
        /// </summary>
        AbsoluteDecimal = 2,
        /// <summary>
        /// 属性修改类型是递增整数。这意味着此值将被添加到属性修改的基础值中，并且在UI中仅显示与默认值(0)的差值。
        /// </summary>
        IncreasingInteger = 3,
        /// <summary>
        /// 属性修改类型是百分比。这意味着此值将与该类型的基础属性修改值相乘，并且在UI中仅显示与默认值(1)的差值。
        /// </summary>
        Percentage = 4,
    }

    /// <summary>
    /// 用于标识属性修改器实体的标记组件。
    /// </summary>
    public struct StatModifierEntityTag : IComponentData { }

    /// <summary>
    /// 用于在编辑器中创作属性修改的帮助结构体。
    /// </summary>
    [Serializable]
    public struct StatModifierInfo
    {
        /// <summary>
        /// 属性修改的类型。
        /// </summary>
        public StatModifierType Type;
        /// <summary>
        /// 属性修改的值。
        /// </summary>
        public float Value;
    }

    /// <summary>
    /// 用于保存属性修改器类型和值的动态缓冲区。
    /// </summary>
    /// <remarks>
    /// This dynamic buffer is attached to the active stat entity for a character, references are stored in <see cref="ActiveStatModifierEntity"/>.
    /// Internal buffer capacity is set to worst case of 12, matching the number of unique stat modifier types. Memory space is not critical for these entities so we can store up to 12 elements inside the chunk.
    /// </remarks>
    /// <seealso cref="RecalculateStatsSystem"/>
    [Serializable]
    [InternalBufferCapacity(12)]
    public struct StatModifier : IBufferElementData
    {
        public StatModifierType Type;
        public float Value;
    }
}