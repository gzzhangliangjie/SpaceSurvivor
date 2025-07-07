using System;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储武器数据和武器升级路径单个等级描述字符串的数据结构体。
    /// </summary>
    /// <remarks>
    /// Has the System.Serializable attribute so these values can be initialized in the editor via <see cref="WeaponUpgradeProperties"/>.
    /// </remarks>
    [Serializable]
    public struct WeaponLevelInfo
    {
        /// <summary>
        /// 在升级UI中向玩家显示的 <see cref="WeaponLevelData"/> 变化描述。
        /// </summary>
        public string Description;
        /// <summary>
        /// 与武器特定等级关联的 <see cref="WeaponLevelData"/>。
        /// </summary>
        public WeaponLevelData WeaponLevelData;
    }

    /// <summary>
    /// ScriptableObject的抽象类，用于定义游戏过程中可升级的武器和被动物品的可升级属性。
    /// </summary>
    public abstract class UpgradeProperties : ScriptableObject
    {
        /// <summary>
        /// 确定在升级序列中显示物品升级的频率。值100表示最高几率，值0表示永远不会被选择。
        /// </summary>
        [Range(0, 100)]
        [Tooltip("Determines how frequently an item will be shown for upgrade. Value of 100 gives the highest chance while value of 0 will never be chosen.")]
        [SerializeField] private int _rarity = 100;
        /// <summary>
        /// 用于优先选择升级的调试布尔值。
        /// </summary>
        /// <remarks>
        /// Selecting this flag will change the rarity value to 5000 making it extremely likely to be chosen as an upgrade in the level up sequence. This can be used if you want to test out a specific weapon or passive item during development.
        /// For builds rarity will always be what is set in the <see cref="_rarity"/> field due to the UNITY_EDITOR directive on <see cref="Rarity"/>.
        /// </remarks>
        [Tooltip("DEBUG ONLY - Makes this weapon extremely likely to be chosen for upgrade on the level up screen. Useful for testing certain attacks and passive abilities.")]
        [SerializeField] private bool _prioritizeUpgrade;

        /// <summary>
        /// 在UI中显示的可升级物品名称。
        /// </summary>
        public string Name;
        /// <summary>
        /// 在UI中显示的可升级物品图标。
        /// </summary>
        public Sprite Icon;
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器内稀有度属性。通常设置为 <see cref="_rarity"/>，除非 <see cref="_prioritizeUpgrade"/> 设置为true，此时此属性将返回一个大值，使此武器或被动物品在升级序列中极有可能被选择。
        /// </summary>
        public int Rarity => _prioritizeUpgrade ? 5000 : _rarity;
#else
        /// <summary>
        /// 构建版本的稀有度属性，返回 <see cref="_rarity"/> 字段。
        /// </summary>
        public int Rarity => _rarity;
#endif
        /// <summary>
        /// 获取给定等级索引升级描述的虚拟方法。
        /// </summary>
        /// <remarks>
        /// This method will be overloaded in child classes to return the description at a specific index in the upgrade properties array.
        /// </remarks>
        /// <param name="levelIndex">Level index to get the description of. Index 0 for level 1, index 1 for level 2, and so on.</param>
        /// <returns>Description of changes to the item granted by this upgrade shown to the player in the level up UI.</returns>
        public virtual string GetDescription(int levelIndex) => "";
        /// <summary>
        /// 返回可升级物品最大等级索引的虚拟属性。
        /// </summary>
        public virtual int MaxLevelIndex => 0;
    }

    /// <summary>
    /// 用于定义武器及其升级路径属性的ScriptableObject。
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="UpgradeProperties"/>.
    /// </remarks>
    [CreateAssetMenu(fileName = "WeaponUpgradeProperties", menuName = "ScriptableObjects/Weapon Upgrade Properties")]
    public class WeaponUpgradeProperties : UpgradeProperties
    {
        /// <summary>
        /// 定义武器的类型。通常用作在托管集合中获取与武器相关类型的键。
        /// </summary>
        public WeaponType WeaponType;
        /// <summary>
        /// 当武器通过 <see cref="WeaponActiveFlag"/> 激活时将生成的GameObject预制体。
        /// </summary>
        /// <remarks>
        /// This GameObject will be baked into an Entity in the <see cref="WeaponAuthoring"/> script and stored in the <see cref="AttackPrefab"/> on the associated weapon entity.
        /// </remarks>
        public GameObject AttackPrefab;
        /// <summary>
        /// 用于存储武器升级路径中每个等级的 <see cref="WeaponLevelData"/> 和描述的数组。
        /// </summary>
        public WeaponLevelInfo[] LevelPropertiesArray;
        /// <summary>
        /// 获取给定等级武器升级描述。
        /// </summary>
        /// <param name="levelIndex">Index of the level to get the description from. Index 0 for level 1, index 1 for level 2, and so on.</param>
        /// <returns>Description of the weapon upgrade at the given level to be shown to the player in the level up UI.</returns>
        public override string GetDescription(int levelIndex) => LevelPropertiesArray[levelIndex].Description;
        /// <summary>
        /// 返回武器最大等级索引的属性。
        /// </summary>
        public override int MaxLevelIndex => LevelPropertiesArray.Length - 1;
    }
}