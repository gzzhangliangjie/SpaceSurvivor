namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 定义了 <see cref="SpawnEvent"/> 中敌人生成的阵型。阵型还将决定其在游戏中的某些行为方面。
    /// </summary>
    public enum SpawnFormation : byte
    {
        None,
        LinearMoveGroup,
        EllipseAroundView,
        SineMoveVerticalLine,
        SineMoveHorizontalLine,
    }
}