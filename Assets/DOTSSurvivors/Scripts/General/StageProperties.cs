using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储关卡相关属性的 ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "-StageProperties", menuName = "ScriptableObjects/Stage Properties")]
    public class StageProperties : ScriptableObject
    {
        /// <summary>
        /// 将在关卡选择UI中显示的关卡名称。
        /// </summary>
        public string StageName;

        /// <summary>
        /// 关卡的ID，用于序列化。
        /// </summary>
        public StageID StageID;
        /// <summary>
        /// 将在关卡选择UI中显示的关卡描述。
        /// </summary>
        [TextArea]
        public string StageDescription;
        /// <summary>
        /// 解锁关卡以进行游戏的费用。
        /// </summary>
        public int UnlockCost;
        /// <summary>
        /// 与此关卡关联的场景的构建索引。
        /// </summary>
        public int SceneIndex;
        /// <summary>
        /// 在关卡选择UI中显示的预览精灵。
        /// </summary>
        public Sprite PreviewSprite;
        /// <summary>
        /// <see cref="EnemySpawnWaveProperties"/> 的集合，用于定义在给定关卡中将生成哪些波次。
        /// </summary>
        public EnemySpawnWaveProperties[] EnemySpawnWaves;
        /// <summary>
        /// 选择下一波实体的时间间隔。
        /// </summary>
        public int WaveInterval;
        /// <summary>
        /// 最终 Boss 是在所有波次完成后生成的 Boss。
        /// </summary>
        /// <remarks>
        /// 最终 Boss 生成时具有非常高的生命值和伤害值，基本上不可能被摧毁，并且会在第一次接触时立即摧毁玩家。在最终 Boss 生成之前，世界上的所有其他敌人都会被摧毁。
        /// </remarks>
        public EnemyType FinalBossType;

        /// <summary>
        /// 达到关卡中最终生成波次所需的时间，显示在关卡选择UI中。
        /// </summary>
        /// <remarks>
        /// 使用 EnemySpawnWaves.Length - 1 是因为游戏中的最后一个阶段是收割者阶段，它会立即有效地摧毁实体，因此该阶段不应计入时间计算。
        /// </remarks>
        public float TimeLimit => EnemySpawnWaves.Length * WaveInterval;
    }
}