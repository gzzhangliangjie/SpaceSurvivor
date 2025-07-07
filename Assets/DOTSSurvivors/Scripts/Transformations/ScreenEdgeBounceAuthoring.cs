using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 标签组件，表示此实体在到达屏幕边缘时应该反弹。
    /// </summary>
    /// <seeaslo cref="ScreenEdgeBounceSystem"/>
    /// <seeaslo cref="ScreenEdgeBounceAuthoring"/>
    public struct ScreenEdgeBounceTag : IComponentData { }

    /// <summary>
    /// 用于向实体添加 <see cref="ScreenEdgeBounceTag"/> 的创作脚本。
    /// </summary>
    /// <seeaslo cref="ScreenEdgeBounceSystem"/>
    public class ScreenEdgeBounceAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ScreenEdgeBounceAuthoring>
        {
            public override void Bake(ScreenEdgeBounceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ScreenEdgeBounceTag>(entity);
            }
        }
    }

    /// <summary>
    /// 用于评估标记有 <see cref="ScreenEdgeBounceTag"/> 的实体位置并使它们产生类似DVD屏保效果反弹的系统。
    /// </summary>
    /// <remarks>
    /// System update in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup so the LocalTransform component can be safely modified.
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ScreenEdgeBounceSystem : ISystem
    {
        /// <summary>
        /// 常量填充值用于将实体稍微远离屏幕边缘，以免在单一边缘上快速连续多次反弹。
        /// </summary>
        private const float BOUNCE_PADDING = 0.15f;

        /// <summary>
        /// 作为轻微优化，此系统仅在游戏世界中实际存在带有 <see cref="ScreenEdgeBounceTag"/> 的实体时才会更新。
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<ScreenEdgeBounceTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;
            var minPosition = cameraPosition - cameraHalfExtents;
            var maxPosition = cameraPosition + cameraHalfExtents;

            foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<ScreenEdgeBounceTag>())
            {
                if (transform.ValueRO.Position.x < minPosition.x)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.right());
                    var position = transform.ValueRO.Position;
                    position.x = minPosition.x + BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }
                else if (transform.ValueRO.Position.x > maxPosition.x)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.left());
                    var position = transform.ValueRO.Position;
                    position.x = maxPosition.x - BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }

                if (transform.ValueRO.Position.z < minPosition.z)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.back());
                    var position = transform.ValueRO.Position;
                    position.z = minPosition.z + BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }
                else if (transform.ValueRO.Position.z > maxPosition.z)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.forward());
                    var position = transform.ValueRO.Position;
                    position.z = maxPosition.z - BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }
            }
        }
    }
}