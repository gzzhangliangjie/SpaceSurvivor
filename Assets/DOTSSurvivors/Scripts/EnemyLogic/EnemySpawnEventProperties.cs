using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 生成事件的创作。生成事件是在生成波中可能发生的某些事件。当生成事件发生时，敌人将以特定的 <see cref="SpawnFormation"/> 生成并具有独特的行为。
    /// </summary>
    /// <remarks>
    /// 某些生成阵型需要额外的数据。并非所有字段都会用于每种阵型。
    /// 自定义编辑器脚本会显示所选阵型的相关字段 (SpawnEventPropertiesEditor.cs)
    /// </remarks>
    /// <seealso cref="EnemySpawnWaveProperties"/>
    /// <seealso cref="EnemySpawnSystem"/>
    /// <seealso cref="SpawnEvent"/>
    [CreateAssetMenu(fileName = "SpawnEvent-", menuName = "ScriptableObjects/Spawn Event")]
    public class EnemySpawnEventProperties : ScriptableObject
    {
        /// <summary>
        /// 敌人生成的排列方式。
        /// </summary>
        public SpawnFormation SpawnFormation;
        /// <summary>
        /// 为此事件生成的敌人类型。
        /// </summary>
        public EnemyType EnemyType;
        /// <summary>
        /// 在此事件期间生成的敌人数量。
        /// </summary>
        public int EnemyCount;

        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义敌人之间的间距。
        /// </summary>
        public float EnemySpacing;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义给定敌人在被销毁前的存活时间。此字段还可用于存储 <see cref="DestroyOffCameraData"/> 组件的测试间隔。
        /// </summary>
        public float TimeToLive;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义线性移动敌人 (<see cref="EnemyLinearMovement"/>) 的移动速度和正弦波敌人 (<see cref="EnemySineWaveMovement"/>) 的恒定移动速度。
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义敌人正弦波运动的周期。<see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Period;
        /// <summary>
        /// 与某些生成阵型相关的可选字段。定义敌人正弦波运动的振幅。<see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Amplitude;
    }
}