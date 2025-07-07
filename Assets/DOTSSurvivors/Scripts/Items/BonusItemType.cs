namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于定义奖励物品类型的枚举。在 <see cref="BonusItemProperties"/> 中使用。
    /// </summary>
    /// <seealso cref="BonusItemProperties"/>
    public enum BonusItemType : byte
    {
        None = 0,
        Health = 1,
        Money = 2
    }
}