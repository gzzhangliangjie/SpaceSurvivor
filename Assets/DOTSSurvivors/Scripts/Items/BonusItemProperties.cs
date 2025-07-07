using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于定义奖励物品相关属性的ScriptableObject。当玩家没有更多可用的攻击或被动物品升级时，奖励物品会显示在升级和板条箱UI中。
    /// </summary>
    /// <remarks>
    /// 目前，奖励物品可以授予额外的生命值或金钱。
    /// </remarks>
    [CreateAssetMenu(fileName = "BonusItemProperties", menuName = "ScriptableObjects/BonusItemProperties")]
    public class BonusItemProperties : ScriptableObject
    {
        /// <summary>
        /// 物品的名称，当显示奖励物品时将在UI中显示。
        /// </summary>
        public string ItemName;
        /// <summary>
        /// 物品的图标，当显示奖励物品时将在UI中显示。
        /// </summary>
        public Sprite ItemIcon;
        /// <summary>
        /// 物品的详细描述，当显示奖励物品时将在UI中显示。
        /// </summary>
        public string ItemDescription;
        /// <summary>
        /// 物品的类型。
        /// </summary>
        public BonusItemType ItemType;
        /// <summary>
        /// 如果选择了奖励物品，将授予的生命值或金钱的价值。
        /// </summary>
        public int ItemValue;
    }
}