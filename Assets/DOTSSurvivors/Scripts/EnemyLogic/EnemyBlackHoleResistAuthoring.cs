using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 标有此组件的敌人对黑洞有抵抗力。目前仅用于终结收割者敌人。
    /// </summary>
    public struct EnemyBlackHoleResistTag : IComponentData { }

    /// <summary>
    /// 用于向敌人添加 <see cref="EnemyBlackHoleResistTag"/> 的创作组件。
    /// </summary>
    /// <seealso cref="EnemyBlackHoleResistTag"/>
    public class EnemyBlackHoleResistAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnemyBlackHoleResistAuthoring>
        {
            public override void Bake(EnemyBlackHoleResistAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyBlackHoleResistTag>(entity);
            }
        }
    }
}