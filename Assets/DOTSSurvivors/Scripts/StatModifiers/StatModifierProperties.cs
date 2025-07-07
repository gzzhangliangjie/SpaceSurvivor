using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于创作属性修改器相关属性的ScriptableObject。某些属性用于运行时数据，其他属性用于在UI元素中显示信息。
    /// </summary>
    /// <remarks>
    /// 这些属性应该存储在 Resources/ScriptableObjects/StatModifierProperties 目录中。
    /// 在上述目录中只能有一个 StatModifierProperties ScriptableObject，否则 <see cref="StatModifierController"/> 在尝试加载这些 ScriptableObjects 时会抛出错误。
    /// </remarks>
    [CreateAssetMenu(fileName = "-Properties", menuName = "ScriptableObjects/Stat Modifier Properties")]
    public class StatModifierProperties : ScriptableObject
    {
        /// <summary>
        /// 属性修改器的类型。
        /// </summary>
        public StatModifierType ModifierType;
        /// <summary>
        /// 属性修改器的计算类型。
        /// </summary>
        public StatModifierCalculationType CalculationType;
        /// <summary>
        /// 要在UI中显示的属性修改器标题。
        /// </summary>
        public string Title;
        /// <summary>
        /// 要在UI中显示的属性修改器精灵图标。
        /// </summary>
        public Sprite Icon;
        /// <summary>
        /// 属性修改的最小值。用于钳制值以确保其保持在范围内以促进游戏平衡。
        /// </summary>
        public float MinimumModificationValue;
        /// <summary>
        /// 属性修改的最大值。用于钳制值以确保其保持在范围内以促进游戏平衡。
        /// </summary>
        public float MaximumModificationValue;
    }
}