using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 包含与玩家吸引区域相关数据的组件。
    /// </summary>
    /// <remarks>
    /// Attraction area is the area at which the player can pick up attractable items and experience gems.
    /// </remarks>
    public struct PlayerAttractionAreaData : IComponentData
    {
        /// <summary>
        /// 定义吸引区域的半径。
        /// </summary>
        public float Radius;
        /// <summary>
        /// 碰撞过滤器确定触发事件将与之引发的碰撞体类型。即，这应该与物品和经验宝石引发触发事件，而不是敌人或环境元素。
        /// </summary>
        public CollisionFilter CollisionFilter;
    }

    /// <summary>
    /// 用于初始化玩家吸引区域数据的创作脚本。
    /// </summary>
    public class PlayerAttractionAreaAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 定义吸引区域的半径。
        /// </summary>
        public float Radius;
        /// <summary>
        /// 对玩家实体的引用。在 <see cref="AttractableItemSystem"/> 等系统中使用，以确定由 <see cref="CharacterStatModificationState.ItemAttractionRadius"/> 定义的吸引半径的任何修改。
        /// </summary>
        public GameObject PlayerEntity;

        private class Baker : Baker<PlayerAttractionAreaAuthoring>
        {
            public override void Bake(PlayerAttractionAreaAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                DependsOn(authoring.gameObject);
                var belongsToLayer = authoring.gameObject.layer;
                var belongsToLayerMask = (uint)math.pow(2, belongsToLayer);
                var collidesWithLayerMask = (uint)PhysicsHelper.GetCollisionMaskForLayer(belongsToLayer);
                AddComponent(entity, new PlayerAttractionAreaData
                {
                    Radius = authoring.Radius,
                    CollisionFilter = new CollisionFilter
                    {
                        BelongsTo = belongsToLayerMask,
                        CollidesWith = collidesWithLayerMask
                    }
                });
                AddComponent(entity, new CharacterEntity
                {
                    Value = GetEntity(authoring.PlayerEntity, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}