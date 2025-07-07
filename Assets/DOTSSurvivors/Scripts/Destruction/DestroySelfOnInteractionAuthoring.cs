using Unity.Burst;
using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 具有此组件的实体将在触发交互时自我摧毁。
    /// </summary>
    /// <remarks>
    /// 这通常用于可吸引物品，当它们与玩家接触时需要被摧毁。
    /// </remarks>
    /// <seealso cref="DestroySelfOnInteractionSystem"/>
    /// <seealso cref="DestroySelfOnInteractionAuthoring"/>
    public struct DestroySelfOnInteractionTag : IComponentData { }

    /// <summary>
    /// 用于向实体添加 <see cref="DestroySelfOnInteractionTag"/> 组件的创作脚本。
    /// </summary>
    /// <seealso cref="DestroySelfOnInteractionTag"/>
    /// <seealso cref="DestroySelfOnInteractionSystem"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroySelfOnInteractionAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DestroySelfOnInteractionAuthoring>
        {
            public override void Bake(DestroySelfOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<DestroySelfOnInteractionTag>(entity);
            }
        }
    }

    /// <summary>
    /// 当触发交互时，在标记有 <see cref="DestroySelfOnInteractionTag"/> 的实体上启用 <see cref="DestroyEntityFlag"/> 的系统。
    /// </summary>
    /// <seealso cref="DestroySelfOnInteractionTag"/>
    /// <seealso cref="DestroySelfOnInteractionAuthoring"/>
    /// <seealso cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct DestroySelfOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactions, entity) in SystemAPI.Query<DynamicBuffer<EntityInteraction>>().WithAll<DestroySelfOnInteractionTag>().WithEntityAccess())
            {
                if (interactions.IsEmpty) continue;
                SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
            }
        }
    }
}