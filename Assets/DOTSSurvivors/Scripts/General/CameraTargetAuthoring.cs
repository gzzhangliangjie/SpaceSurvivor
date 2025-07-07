using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于初始化附加到玩家实体的 <see cref="CameraTarget"/> 组件的组件。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="InitializeCameraTargetSystem"/> 中，此组件将用于定位 <see cref="CameraTargetObject"/> 并在附加到玩家实体的 <see cref="CameraTarget"/> 组件中分配值。
    /// </remarks>
    public struct InitializeCameraTarget : IComponentData
    {
        /// <summary>
        /// 摄像机沿Y轴偏移的单位数，以确保所有实体都在视野内。
        /// </summary>
        public float YOffset;
    }

    /// <summary>
    /// 附加到玩家实体的组件。用于需要摄像机信息的系统。
    /// </summary>
    /// <seealso cref="InitializeCameraTarget"/>
    /// <seealso cref="InitializeCameraTargetSystem"/>
    /// <seealso cref="MoveCameraSystem"/>
    public struct CameraTarget : IComponentData
    {
        public UnityObjectRef<Transform> Transform;
        public float3 HalfExtents;
        public float YOffset;
    }

    /// <summary>
    /// 用于在玩家实体上初始化 <see cref="InitializeCameraTarget"/> 组件的创作脚本。
    /// </summary>
    public class CameraTargetAuthoring : MonoBehaviour
    {
        public float YOffset;

        private class Baker : Baker<CameraTargetAuthoring>
        {
            public override void Bake(CameraTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new InitializeCameraTarget { YOffset = authoring.YOffset });
            }
        }
    }

    /// <summary>
    /// 通过定位 <see cref="CameraTargetObject"/> 并使用 <see cref="InitializeCameraTarget"/> 的数据来初始化 <see cref="CameraTarget"/> 数据的系统。
    /// </summary>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct InitializeCameraTargetSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeCameraTarget>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (CameraTargetObject.Instance == null || Camera.main == null) return;
            var cameraTargetTransform = CameraTargetObject.Instance.transform;
            var cameraVertical = Camera.main.orthographicSize;
            var cameraHorizontal = cameraVertical * Camera.main.aspect;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (initializeCameraTarget, entity) in SystemAPI.Query<InitializeCameraTarget>().WithNone<CameraTarget>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new CameraTarget
                {
                    Transform = new UnityObjectRef<Transform>
                    {
                        Value = cameraTargetTransform
                    },
                    HalfExtents = new float3(cameraHorizontal, 5f, cameraVertical),
                    YOffset = initializeCameraTarget.YOffset
                });
                ecb.RemoveComponent<InitializeCameraTarget>(entity);
            }
            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary>
    /// 此系统更新GameObject摄像机目标的位置，使其与玩家位置加上沿y轴的额外偏移相匹配。
    /// </summary>
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct MoveCameraSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (localToWorld, cameraTargetReference) in SystemAPI.Query<LocalToWorld, CameraTarget>())
            {
                cameraTargetReference.Transform.Value.position = localToWorld.Position + math.up() * cameraTargetReference.YOffset;
            }
        }
    }
}