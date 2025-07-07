using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储此实体的图形实体引用的数据组件。
    /// </summary>
    /// <remarks>
    /// 在这个游戏中，一个常见的模式是让实体的视觉表现作为主实体的子实体存在。这对于减少主实体的内存占用很有用，因为主实体通常是更复杂系统的一部分。这也允许独立的效果通过图形实体的变换来播放动画，而不会干扰主实体的变换。玩家实体通常需要引用其图形实体来播放效果，而这个组件提供了对该实体的可靠访问，无需迭代子缓冲区。
    /// </remarks>
    public struct GraphicsEntity : IComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="GraphicsEntity"/> 组件的创作脚本。
    /// </summary>
    public class GraphicsEntityAuthoring : MonoBehaviour
    {
        public GameObject GraphicsEntity;
        public bool PlayDestructionAnimation;

        private class Baker : Baker<GraphicsEntityAuthoring>
        {
            public override void Bake(GraphicsEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GraphicsEntity
                {
                    Value = GetEntity(authoring.GraphicsEntity, TransformUsageFlags.Dynamic)
                });
                if (authoring.PlayDestructionAnimation)
                {
                    AddComponent<GraphicsEntityPlayDestroyEffectTag>(entity);
                }
            }
        }
    }
}