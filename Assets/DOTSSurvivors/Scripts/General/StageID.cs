namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 关卡的ID，用于序列化。
    /// </summary>
    /// <remarks>
    /// 每个关卡都被分配一个位值，以便它们可以用作 <see cref="PersistentGameData.UnlockedStageFlags"/> 中的位标志。
    /// </remarks>
    public enum StageID : byte
    {
        None = 0,
        Moon = 1 << 1,
        Mars = 1 << 2,
        Venus = 1 << 3,
        ArtTestScene = 1 << 4,
    }
}