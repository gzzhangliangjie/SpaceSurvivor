namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 角色的ID，用于序列化。
    /// </summary>
    /// <remarks>
    /// 每个角色都被分配一个位值，以便它们可以用作 <see cref="PersistentGameData.UnlockedStageFlags"/> 中的位标志。
    /// </remarks>
    public enum CharacterID : byte
    {
        None = 0,
        Turbo = 1 << 0,
        Mockarutan = 1 << 1,
        Daxode = 1 << 2,
        Hazel = 1 << 3,
        Felikss = 1 << 4,
        Zenna = 1 << 5,
    }
}