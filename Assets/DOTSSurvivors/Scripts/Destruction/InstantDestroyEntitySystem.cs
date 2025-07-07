using Unity.Burst;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 当此组件被添加到一个实体上时，它将在帧末尾被销毁，忽略任何通常在 <see cref="DestroyEntitySystem"/> 中触发的额外销毁逻辑。
    /// </summary>
    /// <seealso cref="InstantDestroyEntitySystem"/>
    /// <seealso cref="DestroyEntitySystem"/>
    public struct InstantDestroyTag : IComponentData { }

    /// <summary>
    /// 负责立即销毁带有 <see cref="InstantDestroyTag"/> 的实体的系统。该系统将在 <see cref="DS_DestructionSystemGroup"/> 的末尾执行，以确保所有可能添加此标签的系统都已执行，但在 <see cref="DestroyEntitySystem"/> 之前执行，以避免在该系统中执行任何额外的销毁逻辑的可能性。
    /// </summary>
    /// <seealso cref="InstantDestroyTag"/>
    /// <seealso cref="DestroyEntitySystem"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(DestroyEntitySystem))]
    public partial struct InstantDestroyEntitySystem : ISystem
    {
        /// <summary>
        /// 查询所有带有 <see cref="PlayAudioClipOnDestroyData"/> 组件的实体的实体查询。
        /// </summary>
        /// <remarks>
        /// 用于从设置为立即销毁的实体中移除 <see cref="PlayAudioClipOnDestroyData"/> 清理组件，因为以这种方式销毁实体时不应播放音频剪辑。
        /// </remarks>
        private EntityQuery _playAudioClipOnDestroyQuery;
        /// <summary>
        /// 查询所有带有 <see cref="InstantDestroyTag"/> 的实体的实体查询。
        /// </summary>
        private EntityQuery _instantDestroyQuery;

        public void OnCreate(ref SystemState state)
        {
            _playAudioClipOnDestroyQuery = SystemAPI.QueryBuilder().WithAll<PlayAudioClipOnDestroyData, InstantDestroyTag>().Build();
            _instantDestroyQuery = SystemAPI.QueryBuilder().WithAll<InstantDestroyTag>().Build();
            state.RequireForUpdate(_instantDestroyQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.RemoveComponent<PlayAudioClipOnDestroyData>(_playAudioClipOnDestroyQuery);
            // Note: must convert query to entity array so entities within linked entity group are destroyed as well.
            var entitiesToDestroy = _instantDestroyQuery.ToEntityArray(state.WorldUpdateAllocator);
            state.EntityManager.DestroyEntity(entitiesToDestroy);
        }
    }
}