namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于识别独特外星人类型的枚举。此枚举在Unity编辑器中为给定的外星人类型的预制件上设置。此枚举还将作为键映射到烘焙的实体预制件。这样，<see cref="EnemySpawnWaveProperties"/> 和 <see cref="EnemySpawnEventProperties"/> ScriptableObject可以定义当前波或事件中存在哪些敌人类型，并且 <see cref="EnemySpawnSystem"/> 可以生成适当的预制件。
    /// </summary>
    public enum EnemyType : byte
    {
        None,

        BaseAlien_Green,
        BaseAlien_Blue,
        BaseAlien_Red,
        BaseAlien_Gray,

        PlantAlien_Red,
        PlantAlien_Green,
        PlantAlien_Blue,
        PlantAlien_Purple,

        FloatingHead_Green,
        FloatingHead_Red,
        FloatingHead_Blue,
        FloatingHead_Gray,

        CentaurAlien_Green,
        CentaurAlien_Purple,

        SpiderAlien_Black,
        SpiderAlien_White,

        UFOAlien_Green,
        UFOAlien_Blue,
        UFOAlien_Red,
        UFOAlien_Purple,

        MiniAlien_Green,
        MiniAlien_Blue,
        MiniAlien_Red,
        MiniAlien_Gray,

        BlobAlien_Green,

        ArmorAlien_Black,
        ArmorAlien_White,

        GhostAlien_Blue,
        GhostAlien_Green,

        SkeletonAlien_Bone,
        SkeletonAlien_Gray,

        BigAlienSpots_Green,
        BigAlienSpots_Blue,
        BigAlienSpots_Red,
        BigAlienSpots_Orange,

        BigAlienStripes_Green,
        BigAlienStripes_Blue,
        BigAlienStripes_Red,
        BigAlienStripes_Orange,

        MechAlien_Light,
        MechAlien_Dark,

        ReaperAlien,
    }
}