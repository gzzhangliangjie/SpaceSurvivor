using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于定义玩家角色属性的 ScriptableObject。某些字段仅用于 UI 显示，而其他字段则影响游戏玩法。
    /// </summary>
    [CreateAssetMenu(fileName = "-CharacterProperties", menuName = "ScriptableObjects/Character Properties")]
    public class CharacterProperties : ScriptableObject
    {
        /// <summary>
        /// 角色的名称。
        /// </summary>
        public string CharacterName;
        /// <summary>
        /// 角色的ID，用于序列化。
        /// </summary>
        public CharacterID CharacterID;
        /// <summary>
        /// 要在角色选择UI中显示的角色精灵。
        /// </summary>
        public Sprite CharacterSprite;
        /// <summary>
        /// 航天局的属性，用于在角色选择屏幕和结果面板中显示航天局的旗帜和名称。
        /// </summary>
        public SpaceAgencyProperties SpaceAgencyProperties;
        /// <summary>
        /// 要在角色选择UI中显示的角色初始增益的描述。
        /// </summary>
        [TextArea]
        public string BuffDescription;
        /// <summary>
        /// 解锁此角色的花费。
        /// </summary>
        public int UnlockCost;

        /// <summary>
        /// 角色的基础生命值。
        /// </summary>
        public int HitPoints = 100;
        /// <summary>
        /// 玩家生成时带有的初始属性修改。
        /// </summary>
        public StatModifierInfo[] StatOverrides;
        /// <summary>
        /// 玩家生成时的初始武器。
        /// </summary>
        public WeaponUpgradeProperties StartingWeapon;
        /// <summary>
        /// 引用相应精灵表单的角色材质。
        /// </summary>
        public Material CharacterMaterial;

        /// <summary>
        /// 用于获取给定 <see cref="StatModifierType"/> 的当前值的方法。仅在角色选择面板中用于显示此角色的唯一属性。
        /// </summary>
        /// <param name="statModifierType">请求的属性修改类型。</param>
        /// <param name="defaultValue">此属性修改的默认值。用于将差异添加到默认值。</param>
        /// <returns>此角色给定属性修改类型的值。</returns>
        public float GetCurrentValue(StatModifierType statModifierType, float defaultValue)
        {
            var currentValue = defaultValue;
            if (statModifierType == StatModifierType.AdditionalHitPoints)
            {
                currentValue = HitPoints;
            }

            foreach (var statOverride in StatOverrides)
            {
                if (statOverride.Type != statModifierType) continue;
                currentValue += statOverride.Value;
            }

            return currentValue;
        }
    }
}