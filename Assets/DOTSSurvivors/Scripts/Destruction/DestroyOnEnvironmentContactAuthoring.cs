using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 带有此组件的实体在与环境实体接触时将被销毁。
    /// </summary>
    /// <seealso cref="DestroyOnEnvironmentContactSystem"/>
    /// <seealso cref="DestroyOnEnvironmentContactAuthoring"/>
    /// <seealso cref="EnvironmentTag"/>
    public struct DestroyOnEnvironmentContactTag : IComponentData { }

    /// <summary>
    /// 将 <see cref="DestroyOnEnvironmentContactTag"/> 组件添加到实体的创作脚本。
    /// </summary>
    /// <seealso cref="DestroyOnEnvironmentContactSystem"/>
    /// <seealso cref="DestroyOnEnvironmentContactTag"/>
    /// <seealso cref="EnvironmentTag"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyOnEnvironmentContactAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DestroyOnEnvironmentContactAuthoring>
        {
            public override void Bake(DestroyOnEnvironmentContactAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DestroyOnEnvironmentContactTag>(entity);
            }
        }
    }

    /// <summary>
    /// 该系统调度 <see cref="DestroyOnEnvironmentContactTriggerJob"/> 以在与环境实体接触时销毁实体。
    /// </summary>
    /// <seealso cref="DestroyOnEnvironmentContactTag"/>
    /// <seealso cref="DestroyOnEnvironmentContactTriggerJob"/>
    /// <seealso cref="EnvironmentTag"/>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct DestroyOnEnvironmentContactSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new DestroyOnEnvironmentContactTriggerJob
            {
                DestroyOnEnvironmentLookup = SystemAPI.GetComponentLookup<DestroyOnEnvironmentContactTag>(true),
                EnvironmentLookup = SystemAPI.GetComponentLookup<EnvironmentTag>(true),
                DestroyEntityLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>()
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }

    /// <summary>
    /// 由 <see cref="DestroyOnEnvironmentContactSystem"/> 调度的触发器事件作业。在与带有 <see cref="EnvironmentTag"/> 组件的实体接触时，启用带有 <see cref="DestroyOnEnvironmentContactTag"/> 组件的实体上的 <see cref="DestroyEntityFlag"/> 组件。
    /// </summary>
    /// <remarks>
    /// 这是一个ITriggerEventsJob，因为匹配查询的实体（通常是玩家攻击）会引发触发器事件，而不是碰撞事件。
    /// </remarks>
    /// <seealso cref="DestroyOnEnvironmentContactSystem"/>
    /// <seealso cref="DestroyOnEnvironmentContactTag"/>
    /// <seealso cref="EnvironmentTag"/>
    [BurstCompile]
    public struct DestroyOnEnvironmentContactTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<DestroyOnEnvironmentContactTag> DestroyOnEnvironmentLookup;
        [ReadOnly] public ComponentLookup<EnvironmentTag> EnvironmentLookup;
        public ComponentLookup<DestroyEntityFlag> DestroyEntityLookup;

        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityToDestroy;

            if (EnvironmentLookup.HasComponent(triggerEvent.EntityA) && DestroyOnEnvironmentLookup.HasComponent(triggerEvent.EntityB))
            {
                entityToDestroy = triggerEvent.EntityB;
            }
            else if (EnvironmentLookup.HasComponent(triggerEvent.EntityB) && DestroyOnEnvironmentLookup.HasComponent(triggerEvent.EntityA))
            {
                entityToDestroy = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            DestroyEntityLookup.SetComponentEnabled(entityToDestroy, true);
        }
    }
}