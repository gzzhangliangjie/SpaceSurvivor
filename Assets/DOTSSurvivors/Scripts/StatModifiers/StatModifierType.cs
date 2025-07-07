using System;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 包含所有属性修改器类型的枚举。用作在编辑器中创作属性修改的方式，并在代码的多个地方用作键，通过关联的 <see cref="StatModifierProperties"/> 访问给定属性修改器类型的信息。
    /// </summary>
    [Serializable]
    public enum StatModifierType : byte
    {
        /// <summary>
        /// 无 - 未使用的无效值。
        /// </summary>
        None = 0,
        /// <summary>
        /// 伤害造成修改。此值在各种攻击系统中与攻击的基础伤害值相乘，以计算要分配给该攻击实体的当前有效伤害。
        /// </summary>
        DamageDealt = 1,
        /// <summary>
        /// 按此整数值减少受到的伤害点数。
        /// </summary>
        DamageReceived = 2,
        /// <summary>
        /// 增加角色最大生命值的额外生命点。
        /// </summary>
        AdditionalHitPoints = 3,
        /// <summary>
        /// 生命恢复修改。此值表示在 <see cref="CharacterHealthRegenerationSystem"/> 中每秒恢复多少生命点。
        /// </summary>
        HealthRegeneration = 4,
        /// <summary>
        /// 攻击冷却修改。此值在各种攻击系统中与攻击的基础冷却时间相乘，以计算该攻击的当前有效冷却时间。
        /// </summary>
        AttackCooldown = 5,
        /// <summary>
        /// 物品吸引半径修改器。此值在 <see cref="DetectItemAttractionSystem"/> 中与玩家的 <see cref="PlayerAttractionAreaData.Radius"/> 相乘，以计算玩家的有效吸引区域。
        /// </summary>
        AttackArea = 6,
        /// <summary>
        /// 攻击持续时间修改。此值在各种攻击系统中与攻击的基础持续时间相乘，以计算攻击实体在世界中存在时间的当前有效持续时间。
        /// </summary>
        AttackProjectileSpeed = 7,
        /// <summary>
        /// 移动速度修改。此值在 <see cref="CharacterMoveSystem"/> 中与角色的 <see cref="CharacterBaseMoveSpeed"/> 相乘，以计算当前有效移动速度。
        /// </summary>
        MoveSpeed = 8,
        /// <summary>
        /// 攻击持续时间修改。此值在各种攻击系统中与攻击的基础持续时间相乘，以计算攻击实体在世界中存在时间的当前有效持续时间。
        /// </summary>
        AttackDuration = 9,
        /// <summary>
        /// 每次适用攻击要生成的额外攻击弹射物数量。
        /// </summary>
        AdditionalAttackProjectiles = 10,
        /// <summary>
        /// 物品吸引半径修改器。此值在 <see cref="DetectItemAttractionSystem"/> 中与玩家的 <see cref="PlayerAttractionAreaData.Radius"/> 相乘，以计算玩家的有效吸引区域。
        /// </summary>
        ItemAttractionRadius = 11,
        /// <summary>
        /// 经验点获得修改器。此值在 <see cref="HandlePlayerExperienceThisFrameSystem"/> 中与玩家收集的经验点值相乘，以计算收集宝石的有效经验点获得。
        /// </summary>
        ExperiencePointGain = 12
    }
}