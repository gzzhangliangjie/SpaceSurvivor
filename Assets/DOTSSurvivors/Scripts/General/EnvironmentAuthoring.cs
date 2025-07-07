using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于表示此实体是环境实体的标签组件。
    /// </summary>
    /// <remarks>
    /// 环境实体与装饰实体（<see cref="DecorationTag"/>）相似，但它们确实有碰撞体，可以阻止玩家及其攻击的移动。
    /// 出于设计考虑，敌人仍然可以穿过环境实体。这由敌人和环境实体上的碰撞过滤器决定。
    /// </remarks>
    public struct EnvironmentTag : IComponentData { }

    /// <summary>
    /// 用于将 <see cref="EnvironmentTag"/> 应用于实体的创作脚本。
    /// </summary>
    public class EnvironmentAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnvironmentAuthoring>
        {
            public override void Bake(EnvironmentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnvironmentTag>(entity);
            }
        }
    }
}