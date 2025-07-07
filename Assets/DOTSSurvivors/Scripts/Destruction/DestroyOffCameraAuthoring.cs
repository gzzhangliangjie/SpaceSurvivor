using System;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 相机方向的字节标志定义。
    /// </summary>
    /// <seealso cref="DestroyOffCameraData"/>
    /// <seealso cref="DestroyOffCameraSystem"/>
    [Flags]
    public enum CameraDirection : byte
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }

    /// <summary>
    /// 带有此组件的实体，当实体离开相机边界的一个（或多个）指定侧面时，将被销毁。
    /// </summary>
    /// <seealso cref="DestroyOffCameraSystem"/>
    /// <seealso cref="DestroyOffCameraAuthoring"/>
    /// <seealso cref="CameraDirection"/>
    public struct DestroyOffCameraData : IComponentData
    {
        /// <summary>
        /// 用于定义在确定实体是否应被销毁时，应测试屏幕外哪些方向的字节标志。
        /// </summary>
        /// <remarks>
        /// 示例 - 如果"上"位未设置，但"下"位已设置，那么实体向上移出屏幕时不会被销毁，但向下移出屏幕时则会被销毁。
        /// </remarks>
        /// <seealso cref="CameraDirection"/>
        public CameraDirection Direction;

        /// <summary>
        /// 在测试实体是否在屏幕外时应用的额外填充。当实体的世界变换位置在屏幕边界加上填充值之外时，该实体将被视为在屏幕外。
        /// </summary>
        /// <remarks>
        /// 用于确保实体在仅其原点移出屏幕但部分图形仍在屏幕上时不会被销毁。
        /// </remarks>
        public float Padding;
    }

    /// <summary>
    /// 用于初始化 <see cref="DestroyOffCameraData"/> 值的创作脚本
    /// </summary>
    /// <seealso cref="DestroyOffCameraSystem"/>
    /// <seealso cref="DestroyOffCameraData"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyOffCameraAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 用于定义在确定实体是否应被销毁时，应测试屏幕外哪些方向的字节标志。
        /// </summary>
        /// <remarks>
        /// 示例 - 如果"上"位未设置，但"下"位已设置，那么实体向上移出屏幕时不会被销毁，但向下移出屏幕时则会被销毁。
        /// </remarks>
        /// <seealso cref="CameraDirection"/>
        public CameraDirection Direction;

        /// <summary>
        /// 在测试实体是否在屏幕外时应用的额外填充。用于确保实体在仅其原点移出屏幕但部分图形仍在屏幕上时不会被销毁。
        /// </summary>
        /// <remarks>
        /// 用于确保实体在仅其原点移出屏幕但部分图形仍在屏幕上时不会被销毁。
        /// </remarks>
        public float Padding;

        private class Baker : Baker<DestroyOffCameraAuthoring>
        {
            public override void Bake(DestroyOffCameraAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DestroyOffCameraData
                {
                    Direction = authoring.Direction,
                    Padding = authoring.Padding
                });
            }
        }
    }

    /// <summary>
    /// 如果实体在屏幕外，该系统将启用 <ref cref="DestroyEntityFlag"/> 以在帧末销毁实体。<see cref="DestroyOffCameraData.Direction"/> 可用于指定实体将被清理的相机外方向。
    /// </summary>
    /// <remarks>
    /// 示例 - 如果"上"位未设置，但"下"位已设置，那么实体向上移出屏幕时不会被销毁，但向下移出屏幕时则会被销毁。
    /// </remarks>
    /// <seealso cref="DestroyOffCameraData"/>
    /// <seealso cref="DestroyOffCameraAuthoring"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyOffCameraSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<DestroyOffCameraData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;
            var minPosition = cameraPosition - cameraHalfExtents;
            var maxPosition = cameraPosition + cameraHalfExtents;

            foreach (var (destroyOffCamera, localToWorld, entity) in SystemAPI.Query<DestroyOffCameraData, LocalToWorld>().WithEntityAccess())
            {
                if ((destroyOffCamera.Direction & CameraDirection.Up) != 0 && localToWorld.Position.z > maxPosition.z + destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                if ((destroyOffCamera.Direction & CameraDirection.Right) != 0 && localToWorld.Position.x > maxPosition.x + destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                if ((destroyOffCamera.Direction & CameraDirection.Down) != 0 && localToWorld.Position.z < minPosition.z - destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                if ((destroyOffCamera.Direction & CameraDirection.Left) != 0 && localToWorld.Position.x < minPosition.x - destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
            }
        }
    }
}