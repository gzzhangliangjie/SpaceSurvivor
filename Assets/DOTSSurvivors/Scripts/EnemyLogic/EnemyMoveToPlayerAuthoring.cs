using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 标记有此组件的敌人将不断更新其 <see cref="CharacterMoveDirection"/> 以朝玩家的当前位置移动。
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerAuthoring"/>
    /// <seealso cref="EnemyMoveToPlayerSystem"/>
    /// <seealso cref="EnemyMoveToPlayerJob"/>
    /// <seealso cref="CharacterMoveSystem"/>
    public struct EnemyMoveToPlayerTag : IComponentData { }

    /// <summary>
    /// 用于向实体添加 <see cref="EnemyMoveToPlayerTag"/> 的创作脚本。
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerTag"/>
    /// <seealso cref="EnemyMoveToPlayerSystem"/>
    /// <seealso cref="EnemyMoveToPlayerJob"/>
    /// <seealso cref="CharacterMoveSystem"/>
    public class EnemyMoveToPlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnemyMoveToPlayerAuthoring>
        {
            public override void Bake(EnemyMoveToPlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyMoveToPlayerTag>(entity);
            }
        }
    }

    /// <summary>
    /// 调度 <see cref="EnemyMoveToPlayerJob"/> 的系统，该作业不断更新敌人的 <see cref="CharacterMoveDirection"/> 以使敌人朝向玩家移动。
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerJob"/>
    /// <seealso cref="EnemyMoveToPlayerTag"/>
    /// <seealso cref="CharacterMoveSystem"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct EnemyMoveToPlayerSystem : ISystem, ISystemStartStop
    {
        /// <summary>
        /// 仅当玩家存在时才调度 <see cref="EnemyMoveToPlayerJob"/>。
        /// </summary>
        /// <remarks>
        /// 这很重要，原因有二：
        /// 1. 没有玩家实体，作业将无法找到玩家的位置。
        /// 2. 当玩家实体被摧毁时，我们可以在 <see cref="EnemyMoveToPlayerSystem.OnStopRunning"/> 方法中触发逻辑来停止敌人。
        /// </remarks>
        /// <param name="state"></param>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
            new EnemyMoveToPlayerJob { PlayerPosition = playerPosition }.ScheduleParallel();
        }

        /// <summary>
        /// ISystemStartStop 实现所需的空方法。
        /// </summary>
        /// <param name="state"></param>
        public void OnStartRunning(ref SystemState state)
        {

        }

        /// <summary>
        /// 当玩家不再存在时，停止移动。
        /// </summary>
        /// <param name="state"></param>
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            foreach (var moveDirection in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<EnemyMoveToPlayerTag>())
            {
                moveDirection.ValueRW.Value = float2.zero;
            }
        }
    }

    /// <summary>
    /// 计算从敌人到玩家的方向并设置 <see cref="CharacterMoveDirection"/> 以使敌人朝向玩家移动的作业。
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerSystem"/>
    /// <seealso cref="EnemyMoveToPlayerTag"/>
    /// <seealso cref="CharacterMoveSystem"/>
    [BurstCompile]
    [WithAll(typeof(EnemyTag), typeof(EnemyMoveToPlayerTag))]
    public partial struct EnemyMoveToPlayerJob : IJobEntity
    {
        public float3 PlayerPosition;

        [BurstCompile]
        private void Execute(ref CharacterMoveDirection moveDirection, in LocalTransform transform)
        {
            var curMoveDirection = PlayerPosition.xz - transform.Position.xz;
            curMoveDirection = math.normalize(curMoveDirection);
            moveDirection.Value = curMoveDirection;
        }
    }
}