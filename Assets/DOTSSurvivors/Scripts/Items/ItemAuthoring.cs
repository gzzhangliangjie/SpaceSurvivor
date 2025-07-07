using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于将此实体标识为物品实体的标签组件。
    /// </summary>
    public struct ItemTag : IComponentData { }

    /// <summary>
    /// 用于向烘焙实体添加 <see cref="ItemTag"/> 的Authoring组件。
    /// </summary>
    /// <remarks>
    /// 需要额外的Authoring脚本来添加所需行为的必要组件。
    /// </remarks>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class ItemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ItemAuthoring>
        {
            public override void Bake(ItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ItemTag>(entity);
            }
        }
    }

    /// <summary>
    /// 调度 <see cref="ItemCollectionJob"/> 的系统。该作业是一个碰撞事件作业，因此从 <see cref="DS_PhysicsSystemGroup"/> 调度。
    /// </summary>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct ItemCollectionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = new ItemCollectionJob
            {
                ItemLookup = SystemAPI.GetComponentLookup<ItemTag>(true),
                PlayerLookup = SystemAPI.GetComponentLookup<PlayerTag>(true),
                InteractionBufferLookup = SystemAPI.GetBufferLookup<EntityInteraction>()
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }

    /// <summary>
    /// 检查玩家与游戏世界中物品之间碰撞事件的作业。有效的碰撞会将交互添加到 <see cref="EntityInteraction"/> 动态缓冲区，以便后续系统可以应用与特定物品相关的附加逻辑。
    /// </summary>
    [BurstCompile]
    public struct ItemCollectionJob : ICollisionEventsJob
    {
        /// <summary>
        /// 只读组件查找，用于在碰撞事件中识别物品实体。
        /// </summary>
        [ReadOnly] public ComponentLookup<ItemTag> ItemLookup;
        /// <summary>
        /// 只读组件查找，用于在碰撞事件中识别玩家实体。
        /// </summary>
        [ReadOnly] public ComponentLookup<PlayerTag> PlayerLookup;
        /// <summary>
        /// 缓冲区查找，用于将玩家实体添加到碰撞事件中物品的交互缓冲区。
        /// </summary>
        public BufferLookup<EntityInteraction> InteractionBufferLookup;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity playerEntity;
            Entity itemEntity;

            if (PlayerLookup.HasComponent(collisionEvent.EntityA) && ItemLookup.HasComponent(collisionEvent.EntityB))
            {
                playerEntity = collisionEvent.EntityA;
                itemEntity = collisionEvent.EntityB;
            }
            else if (PlayerLookup.HasComponent(collisionEvent.EntityB) && ItemLookup.HasComponent(collisionEvent.EntityA))
            {
                playerEntity = collisionEvent.EntityB;
                itemEntity = collisionEvent.EntityA;
            }
            else
            {
                return;
            }

            var interactionBuffer = InteractionBufferLookup[itemEntity];
            foreach (var interaction in interactionBuffer)
            {
                if (interaction.TargetEntity.Equals(playerEntity)) return;
            }

            interactionBuffer.Add(new EntityInteraction { TargetEntity = playerEntity });
        }
    }
}