using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于创作有关生成波信息的ScriptableObject。
    /// </summary>
    /// <remarks>
    /// 生成波在一关卡中定义了一段时间内（通常为1分钟）敌人生成的行为。
    /// </remarks>
    [CreateAssetMenu(fileName = "EnemySpawnWave-", menuName = "Enemy Spawn Wave")]
    public class EnemySpawnWaveProperties : ScriptableObject
    {
        /// <summary>
        /// 将在此波中生成的敌人类型数组。
        /// </summary>
        public EnemyType[] EnemyTypes;
        /// <summary>
        /// 当前生成波的Boss敌人类型。
        /// </summary>
        /// <remarks>
        /// Boss敌人比普通敌人拥有更多生命值，具有增强的材质效果，并且在被击败时有几率生成一个补给箱。
        /// 每波只能有1个boss敌人。
        /// Boss敌人总是在生成波开始时生成。
        /// </remarks>
        public EnemyType BossType;
        /// <summary>
        /// 将应用于此生成波的boss的基础生命值。Boss生命值乘以玩家当前等级以进行少量缩放。
        /// </summary>
        public int BossBaseHitPoints;
        /// <summary>
        /// 此波boss掉落的经验点宝石的价值。
        /// </summary>
        public int BossExperiencePoints;
        /// <summary>
        /// Boss被击败后生成一个箱子的百分比几率。0将永远不会生成箱子，100将总是生成箱子。
        /// </summary>
        [Range(0, 100)]
        public int BossChanceToDropCrate;
        /// <summary>
        /// 定义此波激活时将存在的最小敌人数量。如果世界中特定生成波的敌人少于最小数量，则将在1帧内生成满足此最小值的敌人数量。
        /// </summary>
        public int MinEnemyCount;
        /// 如果满足此波的最小敌人计数，将按此值定义的时间间隔生成额外的敌人。一旦 <see cref="EnemySpawnerState.WaveTimer"/> 达到0，将生成存储在 <see cref="SpawnWaveEnemyPrefab"/> 动态缓冲区中的每种类型的一个敌人。
        public float SpawnInterval;

        /// <summary>
        /// 定义此生成波的生成事件。生成事件是在特定 <see cref="SpawnFormation"/> 中以独特行为生成的一组实体。
        /// </summary>
        [Header("Spawn Event Settings")]
        public EnemySpawnEventProperties[] SpawnEvents;
        /// <summary>
        /// 从波开始到第一个生成事件之间的时间（秒）。后续的生成事件将在此延迟时间的另一个间隔后开始。生成事件按顺序发生。生成事件设计者需要确保所有生成事件都在生成波的时间范围内发生，因为在生成波结束后发生的生成事件将不会发生。
        /// </summary>
        [Tooltip("波开始后到事件开始的时间（秒）。")]
        public float DelayTime;
        /// <summary>
        /// 每个生成事件发生的百分比几率。每个生成事件将在其执行时进行评估，以确定是否应发生。值为0将永远不会发生，值为100将总是发生。
        /// </summary>
        [Range(0, 100)]
        [Tooltip("每个事件发生的百分比几率")]
        public int ChanceOfOccurrence;
    }
}