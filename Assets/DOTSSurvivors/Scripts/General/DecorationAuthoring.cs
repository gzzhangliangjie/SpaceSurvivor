using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于表示此实体是装饰实体的标签组件。
    /// </summary>
    /// <remarks>
    /// 装饰实体与环境实体（<see cref="EnvironmentTag"/>）相似，但它们没有碰撞器，不影响游戏玩法。
    /// </remarks>
    public struct DecorationTag : IComponentData { }

    /// <summary>
    /// 用于将 <see cref="DecorationTag"/> 应用于实体的创作脚本。
    /// </summary>
    public class DecorationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DecorationAuthoring>
        {
            public override void Bake(DecorationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<DecorationTag>(entity);
            }
        }
    }
}