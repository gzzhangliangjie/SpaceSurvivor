using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 具有此组件的实体能够在整个游戏过程中被各种系统销毁。默认情况下，此组件应被禁用，一旦启用，它将在帧末尾在 <see cref="DestroyEntitySystem"/> 中被销毁。
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="DestructibleEntityAuthoring"/>
    public struct DestroyEntityFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于向实体添加 <see cref="DestroyEntityFlag"/> 的创作组件。默认情况下，此组件将被禁用，以确保实体在实例化后不会立即被销毁。
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="DestroyEntityFlag"/>
    public class DestructibleEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DestructibleEntityAuthoring>
        {
            public override void Bake(DestructibleEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }
}