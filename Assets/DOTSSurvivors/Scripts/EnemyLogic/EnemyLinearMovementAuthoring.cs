using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 可启用组件，用于通知 <see cref="EnemyLinearMovementSystem"/> 将 <see cref="CharacterMoveDirection"/> 更新为新值。用于提高效率，使CharacterMoveDirection仅在需要时设置。
    /// </summary>
    /// <seealso cref="EnemyLinearMovement"/>
    /// <seealso cref="EnemyLinearMovementSystem"/>
    /// <seealso cref="EnemyLinearMovementAuthoring"/>
    public struct SetEnemyLinearMovementFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 定义玩家移动角度（弧度）的数据组件。
    /// </summary>
    /// <seealso cref="SetEnemyLinearMovementFlag"/>
    /// <seealso cref="EnemyLinearMovementSystem"/>
    /// <seealso cref="EnemyLinearMovementAuthoring"/>
    public struct EnemyLinearMovement : IComponentData
    {
        /// <summary>
        /// 移动方向的角度（弧度）。
        /// </summary>
        /// <remarks>
        /// 0度将使敌人从屏幕底部移动到顶部，90度将使敌人从屏幕左侧移动到右侧。
        /// </remarks>
        public float Angle;
    }

    /// <summary>
    /// 用于初始化 <see cref="EnemyLinearMovement"/> 移动角度的创作脚本。将 <see cref="SetEnemyLinearMovementFlag"/> 设置为true以初始化 <see cref="CharacterMoveDirection"/>。
    /// </summary>
    /// <remarks>
    /// 为了方便用户编辑，值以度为单位创作。为了高效的数学计算，存储为弧度。
    /// </remarks>
    /// <seealso cref="EnemyLinearMovement"/>
    /// <seealso cref="SetEnemyLinearMovementFlag"/>
    /// <seealso cref="EnemyLinearMovementSystem"/>
    public class EnemyLinearMovementAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 初始行进方向的角度（度）。
        /// </summary>
        /// <remarks>
        /// 0度将使敌人从屏幕底部移动到顶部，90度将使敌人从屏幕左侧移动到右侧。
        /// </remarks>
        public float Angle;

        private class Baker : Baker<EnemyLinearMovementAuthoring>
        {
            public override void Bake(EnemyLinearMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EnemyLinearMovement { Angle = math.radians(authoring.Angle) });
                AddComponent<SetEnemyLinearMovementFlag>(entity);
            }
        }
    }

    /// <summary>
    /// 当实体启用 <see cref="SetEnemyLinearMovementFlag"/> 时，基于 <see cref="EnemyLinearMovement"/> 中存储的角度设置 <see cref="CharacterMoveDirection"/> 的系统。
    /// </summary>
    /// <seealso cref="EnemyLinearMovement"/>
    /// <seealso cref="SetEnemyLinearMovementFlag"/>
    /// <seealso cref="EnemyLinearMovementAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct EnemyLinearMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (characterMoveDirection, enemyLinearMovement, initializationFlag) in SystemAPI.Query<RefRW<CharacterMoveDirection>, EnemyLinearMovement, EnabledRefRW<SetEnemyLinearMovementFlag>>())
            {
                characterMoveDirection.ValueRW.Value = new float2
                {
                    x = math.sin(enemyLinearMovement.Angle),
                    y = math.cos(enemyLinearMovement.Angle)
                };
                initializationFlag.ValueRW = false;
            }
        }
    }
}