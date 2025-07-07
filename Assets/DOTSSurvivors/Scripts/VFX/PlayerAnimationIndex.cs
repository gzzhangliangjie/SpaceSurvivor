namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于定义玩家动画索引的枚举。
    /// </summary>
    public enum PlayerAnimationIndex : byte
    {
        /// <summary>
        /// 玩家移动时播放的动画 - 即行走循环
        /// </summary>
        Movement = 0,
        /// <summary>
        /// 角色不移动时播放的动画 - 即待机动画
        /// </summary>
        Idle = 1,
        /// <summary>
        /// 无效动画
        /// </summary>
        None = byte.MaxValue
    }
}