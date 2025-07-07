using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于添加放射性废料投放世界内攻击实体运行所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// The radioactive waste drop entity is the initial entity that spawns from the top of the screen and "falls" to a determined position in the game world. This attack does not deal any damage, however once it reaches its target position, it will self-destruct and spawn a radioactive waste spill entity (see <see cref="RadioactiveWasteSpillEntityAuthoring"/>).
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DestroyAtPosition"/>
    /// <seealso cref="LinearMovementSpeed"/>
    /// <seealso cref="SpawnOnDestroy"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class RadioactiveWasteDropEntityAuthoring : MonoBehaviour
    {
        public GameObject RadioactiveWasteSpillPrefab;

        private class Baker : Baker<RadioactiveWasteDropEntityAuthoring>
        {
            public override void Bake(RadioactiveWasteDropEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DestroyAtPosition>(entity);
                AddComponent<LinearMovementSpeed>(entity);
                AddComponent(entity, new SpawnOnDestroy
                {
                    Value = GetEntity(authoring.RadioactiveWasteSpillPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}