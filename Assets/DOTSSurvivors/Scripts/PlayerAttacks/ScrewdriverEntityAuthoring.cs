using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于添加螺丝刀世界内攻击实体运行所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="LinearMovementSpeed"/>
    /// <seealso cref="DestroyAfterNumberHits"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class ScrewdriverEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ScrewdriverEntityAuthoring>
        {
            public override void Bake(ScrewdriverEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<DestroyAfterTime>(entity);
                AddComponent<LinearMovementSpeed>(entity);
                AddComponent<DestroyAfterNumberHits>(entity);
            }
        }
    }
}