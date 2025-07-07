using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 枚举类型，用于定义敌人移动的方向，同时它也以正弦波模式移动。
    /// </summary>
    /// <remarks>
    /// 例如，向左意味着角色以恒定速度从右向左移动，同时以正弦波模式上下移动。
    /// </remarks>
    [System.Serializable]
    public enum SineWaveMoveDirection : byte
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// 定义敌人正弦波移动的移动属性的组件。
    /// </summary>
    public struct EnemySineWaveMovement : IComponentData
    {
        /// <summary>
        /// 指定敌人恒定移动方向的基数方向。
        /// </summary>
        public SineWaveMoveDirection MoveDirection;
        /// <summary>
        /// 它将沿恒定移动轴移动的速度。
        /// </summary>
        public float ConstantMoveSpeed;
        /// <summary>
        /// 正弦波的周期 - 波峰之间的单位。实质上决定了它在垂直于恒定移动方向上来回移动的速度。
        /// </summary>
        public float Period;
        /// <summary>
        /// 正弦波的振幅 - 从波谷底部到波峰顶部的高度。
        /// </summary>
        public float Amplitude;
    }

    /// <summary>
    /// 包含正弦波开始时间的组件，以便敌人可以随时间沿波移动。
    /// </summary>
    /// <remarks>
    /// 通常，此值将初始化为敌人生成时的时间，并且可以从此值计算存活时间。
    /// </remarks>
    public struct EnemySineWaveStartTime : IComponentData
    {
        /// <summary>
        /// 应从中计算当前存活时间值的时间戳。
        /// </summary>
        public float Value;
    }

    /// <summary>
    /// 用于初始化 <see cref="EnemySineWaveMovement"/> 值的创作脚本。
    /// </summary>
    public class EnemySineWaveMovementAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 指定敌人恒定移动方向的基数方向。
        /// </summary>
        public SineWaveMoveDirection MoveDirection;
        /// <summary>
        /// 它将沿恒定移动轴移动的速度。
        /// </summary>
        public float ConstantMoveSpeed;
        /// <summary>
        /// 正弦波的周期 - 波峰之间的单位。实质上决定了它在垂直于恒定移动方向上来回移动的速度。
        /// </summary>
        public float Period;
        /// <summary>
        /// 正弦波的振幅 - 从波谷底部到波峰顶部的高度。
        /// </summary>
        public float Amplitude;

        private class Baker : Baker<EnemySineWaveMovementAuthoring>
        {
            public override void Bake(EnemySineWaveMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EnemySineWaveMovement
                {
                    MoveDirection = authoring.MoveDirection,
                    ConstantMoveSpeed = authoring.ConstantMoveSpeed,
                    Period = authoring.Period,
                    Amplitude = authoring.Amplitude
                });
            }
        }
    }

    /// <summary>
    /// 根据使用 <see cref="EnemySineWaveMovement"/> 计算的值，为敌人实体设置 <see cref="CharacterMoveDirection"/> 的系统。
    /// </summary>
    /// <remarks>
    /// 该系统还会向任何拥有 <see cref="EnemySineWaveMovement"/> 组件但没有 <see cref="EnemySineWaveStartTime"/> 组件的实体添加一个带有当前游戏时间值的 <see cref="EnemySineWaveStartTime"/> 组件。
    /// </remarks>
    /// <seealso cref="CharacterMoveSystem"/>
    /// <seealso cref="EnemySineWaveMovement"/>
    /// <seealso cref="EnemySineWaveStartTime"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct EnemySineWaveMovementSystem : ISystem
    {
        private EntityQuery _initializeSineWaveMovementQuery;

        public void OnCreate(ref SystemState state)
        {
            _initializeSineWaveMovementQuery = SystemAPI.QueryBuilder().WithAll<EnemySineWaveMovement>().WithNone<EnemySineWaveStartTime>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            if (!_initializeSineWaveMovementQuery.IsEmpty)
            {
                var newStartTime = new EnemySineWaveStartTime { Value = (float)elapsedTime };
                var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
                ecb.AddComponent(_initializeSineWaveMovementQuery, newStartTime);
                ecb.Playback(state.EntityManager);
            }

            foreach (var (characterMoveDirection, enemySineWaveMovement, startTime) in SystemAPI.Query<RefRW<CharacterMoveDirection>, EnemySineWaveMovement, EnemySineWaveStartTime>())
            {
                var timeAlive = (float)elapsedTime - startTime.Value;
                var constantMoveSpeed = enemySineWaveMovement.ConstantMoveSpeed;
                var sineMovement = enemySineWaveMovement.Amplitude * math.sin(enemySineWaveMovement.Period * timeAlive);

                var moveDirection = enemySineWaveMovement.MoveDirection switch
                {
                    SineWaveMoveDirection.Left => new float2(-1 * constantMoveSpeed, sineMovement),
                    SineWaveMoveDirection.Right => new float2(constantMoveSpeed, sineMovement),
                    SineWaveMoveDirection.Up => new float2(sineMovement, constantMoveSpeed),
                    SineWaveMoveDirection.Down => new float2(sineMovement, -1 * constantMoveSpeed),
                    _ => float2.zero
                };

                characterMoveDirection.ValueRW.Value = moveDirection;
            }
        }
    }
}