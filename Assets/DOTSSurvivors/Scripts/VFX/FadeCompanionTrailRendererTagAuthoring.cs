using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于表示关联实体需要初始化其 <see cref="FadeCompanionTrailRenderer"/> 组件的组件。
    /// </summary>
    /// <remarks>
    /// This initialization step is required as <see cref="FadeCompanionTrailRenderer"/> is an enableable component which cannot be baked onto an entity.
    /// </remarks>
    public struct InitializeFadeCompanionTrailRenderer : IComponentData { }

    /// <summary>
    /// 当主喷气背包实体被销毁时调用 <see cref="JetpackTrailRendererController.EndTrailRenderer"/> 方法的清理组件。这为喷气背包上的轨迹渲染器提供了很好的视觉淡出效果。
    /// </summary>
    public struct FadeCompanionTrailRenderer : ICleanupComponentData
    {
        public UnityObjectRef<JetpackTrailRendererController> Value;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="InitializeFadeCompanionTrailRenderer"/> 组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="CompanionGameObjectAuthoring"/> component to ensure all companion elements are added to the entity.
    /// This authoring script should be added to the attack graphics entity which is a direct child of the the main attack prefab.
    /// </remarks>
    /// <seealso cref="FadeCompanionTrailRenderer"/>
    /// <seealso cref="FadeCompanionTrailRendererSystem"/>
    [RequireComponent(typeof(CompanionGameObjectAuthoring))]
    public class FadeCompanionTrailRendererTagAuthoring : MonoBehaviour
    {
        private class Baker : Baker<FadeCompanionTrailRendererTagAuthoring>
        {
            public override void Bake(FadeCompanionTrailRendererTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<InitializeFadeCompanionTrailRenderer>(entity);
            }
        }
    }

    /// <summary>
    /// 初始化 <see cref="FadeCompanionTrailRenderer"/> 清理组件的系统。在实体销毁时，此系统将调用 <see cref="JetpackTrailRendererController.EndTrailRenderer"/> 方法，为轨迹渲染器提供良好的淡出效果。
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct FadeCompanionTrailRendererSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (companionGameObject, entity) in SystemAPI.Query<CompanionGameObjectData>().WithAll<InitializeFadeCompanionTrailRenderer>().WithNone<FadeCompanionTrailRenderer>().WithEntityAccess())
            {
                if (!companionGameObject.Transform.Value.gameObject.TryGetComponent<JetpackTrailRendererController>(out var trailRendererController))
                {
                    Debug.LogError($"Error: companion GameObject of Entity: {entity.ToString()} has no JetpackTrailRendererController component");
                    continue;
                }

                ecb.AddComponent(entity, new FadeCompanionTrailRenderer { Value = trailRendererController });
                ecb.RemoveComponent<InitializeFadeCompanionTrailRenderer>(entity);
            }

            foreach (var (companionTrailRenderer, entity) in SystemAPI.Query<FadeCompanionTrailRenderer>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                companionTrailRenderer.Value.Value.EndTrailRenderer();
                ecb.RemoveComponent<FadeCompanionTrailRenderer>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}