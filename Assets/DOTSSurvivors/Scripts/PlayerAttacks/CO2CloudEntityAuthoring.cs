using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于添加CO2云雾世界内攻击实体运行所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class CO2CloudEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CO2CloudEntityAuthoring>
        {
            public override void Bake(CO2CloudEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Parent>(entity);
                AddComponent<DestroyAfterTime>(entity);
                AddComponent<DealHitPointsOnInteraction>(entity);
            }
        }
    }
}