using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储实体预制体的组件，该预制体将在具有此组件的实体被销毁时实例化。
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="SpawnOnDestroyAuthoring"/>
    public struct SpawnOnDestroy : IComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// 用于初始化存储在 <see cref="SpawnOnDestroy"/> 组件中的实体预制体的创作脚本。
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="SpawnOnDestroy"/>
    public class SpawnOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 将在此类的baker中转换为实体的GameObject预制体，该实体将被分配给添加到此实体的 <see cref="SpawnOnDestroy"/> 组件。
        /// </summary>
        public GameObject EntityPrefabToSpawn;

        private class Baker : Baker<SpawnOnDestroyAuthoring>
        {
            public override void Bake(SpawnOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnOnDestroy
                {
                    Value = GetEntity(authoring.EntityPrefabToSpawn, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}