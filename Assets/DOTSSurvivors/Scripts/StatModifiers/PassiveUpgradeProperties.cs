using System;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储 <see cref="StatModifier"/> 数组和被动物品在其升级路径单一等级的描述的数据结构。
    /// </summary>
    /// <remarks>
    /// Has the System.Serializable attribute so these values can be initialized in the editor via <see cref="PassiveUpgradeProperties"/>.
    /// </remarks>
    [Serializable]
    public struct PassiveLevelInfo
    {
        /// <summary>
        /// 被动物品在特定等级的描述。
        /// </summary>
        public string Description;
        /// <summary>
        /// 当选择此被动物品的等级时将添加到玩家的 <see cref="CharacterStatModificationState"/> 中的 <see cref="StatModifier"/> 数组。
        /// </summary>
        /// <remarks>
        /// Gets added indirectly via <see cref="ActiveStatModifierEntity"/> in <see cref="RecalculateStatsSystem"/>.
        /// </remarks>
        public StatModifier[] StatModifiers;
    }

    /// <summary>
    /// 用于定义被动物品及其升级路径属性的ScriptableObject。
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="UpgradeProperties"/>.
    /// </remarks>
    [CreateAssetMenu(fileName = "PassiveUpgradeProperties", menuName = "ScriptableObjects/Passive Upgrade Properties")]
    public class PassiveUpgradeProperties : UpgradeProperties
    {
        /// <summary>
        /// 用于存储被动物品升级路径中每个等级的 <see cref="StatModifier"/> 和描述的数组。
        /// </summary>
        public PassiveLevelInfo[] UpgradeProperties;
        /// <summary>
        /// 获取被动物品在指定等级的升级描述。
        /// </summary>
        /// <param name="levelIndex">Index of the level to get the description from. Index 0 for level 1, index 1 for level 2, and so on.</param>
        /// <returns>Description of the attack upgrade at the given level.</returns>
        public override string GetDescription(int levelIndex) => UpgradeProperties[levelIndex].Description;
        /// <summary>
        /// 返回被动物品最大等级索引的属性。
        /// </summary>
        public override int MaxLevelIndex => UpgradeProperties.Length - 1;
    }
}