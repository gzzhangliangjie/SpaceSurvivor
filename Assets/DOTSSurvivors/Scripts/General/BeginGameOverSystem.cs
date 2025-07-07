using System;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于表示 <see cref="BeginGameOverSystem"/> 应开始执行的标签组件。
    /// </summary>
    public struct BeginGameOverTag : IComponentData { }

    /// <summary>
    /// 用于调用游戏结束序列开始的系统。
    /// </summary>
    /// <remarks>
    /// 需要此系统，以便 <see cref="DestroyEntitySystem"/> 可以进行Burst编译，因为它将Action事件托管类型移出了该系统。
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial class BeginGameOverSystem : SystemBase
    {
        /// <summary>
        /// 一旦玩家被摧毁，就会调用此事件以触发游戏结束序列。
        /// </summary>
        /// <seeaslo cref="GameOverUIController"/>
        public Action OnGameOver;

        protected override void OnCreate()
        {
            RequireForUpdate<BeginGameOverTag>();
        }

        protected override void OnStartRunning()
        {
            OnGameOver?.Invoke();
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BeginGameOverTag>());
        }

        protected override void OnUpdate()
        {

        }
    }
}