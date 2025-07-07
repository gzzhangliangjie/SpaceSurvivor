using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// <see cref="CompanionGameObjectData"/> 的初始化数据。
    /// </summary>
    /// <remarks>
    /// Defines a GameObject to spawn and match the position of the associated entity.
    /// Initialization data is required as <see cref="CompanionGameObjectData"/> is an ICleanupComponentData which cannot be added to an entity through baking.
    /// </remarks>
    public struct CompanionGameObjectInitializationData : IComponentData
    {
        /// <summary>
        /// 在初始化期间生成的GameObject预制体。此预制体的实例将匹配关联实体的位置。
        /// </summary>
        public UnityObjectRef<GameObject> Prefab;

        /// <summary>
        /// 延迟销毁伴随GameObject的时间（秒）。
        /// </summary>
        public float DelayCleanupTime;
    }

    /// <summary>
    /// 与将匹配关联实体位置的伴随GameObject相关的数据。
    /// </summary>
    /// <remarks>
    /// This component is a cleanup component which cannot be added to the entity through baking. As such <see cref="CompanionGameObjectInitializationData"/> is required to initialize this component.
    /// </remarks>
    public struct CompanionGameObjectData : ICleanupComponentData
    {
        /// <summary>
        /// 实例化的GameObject的Transform引用，将匹配关联实体的位置。
        /// </summary>
        public UnityObjectRef<Transform> Transform;

        /// <summary>
        /// 延迟销毁伴随GameObject的时间（秒）。
        /// </summary>
        public float DelayCleanupTimer;
    }

    /// <summary>
    /// 用于设置 <see cref="CompanionGameObjectInitializationData"/> 值的创作脚本，这些值将在 <see cref="UpdateCompanionGameObjectSystem"/> 中初始化 <see cref="CompanionGameObjectData"/> 的值。
    /// </summary>
    public class CompanionGameObjectAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 在初始化期间生成的GameObject预制体。此预制体的实例将匹配关联实体的位置。
        /// </summary>
        public GameObject CompanionPrefab;

        /// <summary>
        /// 延迟销毁伴随GameObject的时间（秒）。
        /// </summary>
        public float DelayCleanupTime;

        private class Baker : Baker<CompanionGameObjectAuthoring>
        {
            public override void Bake(CompanionGameObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CompanionGameObjectInitializationData
                {
                    Prefab = authoring.CompanionPrefab,
                    DelayCleanupTime = authoring.DelayCleanupTime
                });
            }
        }
    }

    /// <summary>
    /// 处理伴随GameObject生命周期的系统。伴随GameObject是与其关联实体的位置和旋转匹配的GameObject。
    /// 此系统包含3个foreach循环，功能如下：
    /// 1. 从 <see cref="CompanionGameObjectInitializationData.Prefab"/> 实例化伴随GameObject，为 <see cref="CompanionGameObjectData"/> 分配值并将其添加到实体。
    /// 2. 从实体的LocalToWorld组件中的值更新伴随GameObject的位置和旋转。
    /// 3. 当实体被销毁时，销毁伴随GameObject。根据设置，这可能在 <see cref="CompanionGameObjectInitializationData.DelayCleanupTime"/> 设置的延迟后发生。
    /// </summary>
    /// <remarks>
    /// System updates in the <see cref="DS_EffectsSystemGroup"/> which updates after the Unity's TransformSystemGroup. To ensure accuracy of position and rotation, these values are sourced from the entity's LocalToWorld component.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct UpdateCompanionGameObjectSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (properties, localToWorld, entity) in SystemAPI.Query<CompanionGameObjectInitializationData, LocalToWorld>().WithNone<CompanionGameObjectData>().WithEntityAccess())
            {
                var newCompanionGameObject = (GameObject)Object.Instantiate(properties.Prefab, localToWorld.Position, Quaternion.identity);
                ecb.AddComponent(entity, new CompanionGameObjectData
                {
                    Transform = newCompanionGameObject.transform,
                    DelayCleanupTimer = properties.DelayCleanupTime
                });
            }

            foreach (var (companionGameObject, localToWorld) in SystemAPI.Query<CompanionGameObjectData, LocalToWorld>())
            {
                companionGameObject.Transform.Value.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
            }

            foreach (var (companionGameObject, entity) in SystemAPI.Query<RefRW<CompanionGameObjectData>>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                companionGameObject.ValueRW.DelayCleanupTimer -= deltaTime;
                if (companionGameObject.ValueRO.DelayCleanupTimer > 0f) continue;
                if (companionGameObject.ValueRO.Transform.Value != null)
                {
                    Object.Destroy(companionGameObject.ValueRO.Transform.Value.gameObject);
                }
                ecb.RemoveComponent<CompanionGameObjectData>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}