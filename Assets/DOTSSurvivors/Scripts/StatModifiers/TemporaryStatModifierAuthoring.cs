using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 定义此临时属性修改器实体应保持活跃时间（秒）的数据组件。
    /// </summary>
    /// <remarks>
    /// In the <see cref="HandleTemporaryStatModifierInteractionSystem"/> a new stat modifier entity is created with a <see cref="DestroyAfterTime"/> component.
    /// </remarks>
    public struct TemporaryStatModifierActiveTime : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于在临时属性修改器实体上初始化数据组件的创作脚本。
    /// </summary>
    /// <seealso cref="TemporaryStatModifierActiveTime"/>
    /// <seealso cref="HandleTemporaryStatModifierInteractionSystem"/>
    /// <seealso cref="StatModifier"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class TemporaryStatModifierAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 此属性修改器激活时要应用的属性修改数组。
        /// </summary>
        public StatModifierInfo[] StatModifiers;
        /// <summary>
        /// 此属性修改器应保持激活的时间（秒）。
        /// </summary>
        public float TimeActive;

        private class Baker : Baker<TemporaryStatModifierAuthoring>
        {
            public override void Bake(TemporaryStatModifierAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TemporaryStatModifierActiveTime { Value = authoring.TimeActive });

                var statModifiers = AddBuffer<StatModifier>(entity);
                foreach (var statModifier in authoring.StatModifiers)
                {
                    statModifiers.Add(new StatModifier
                    {
                        Type = statModifier.Type,
                        Value = statModifier.Value
                    });
                }
            }
        }
    }

    /// <summary>
    /// 创建临时属性修改器实体的系统，这些实体将持续 <see cref="TemporaryStatModifierActiveTime.Value"/> 定义的特定时间。作为交互系统创建，因为 <see cref="TemporaryStatModifierActiveTime"/> 应用于玩家拾取的物品。
    /// </summary>
    /// <remarks>
    /// This system creates a new stat modifier entity with a <see cref="DestroyAfterTime"/> component.
    /// </remarks>
    /// <seeaslo cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct HandleTemporaryStatModifierInteractionSystem : ISystem
    {
        /// <summary>
        /// 要创建的属性修改器实体的原型。
        /// </summary>
        private EntityArchetype _statModifierArchetype;

        public void OnCreate(ref SystemState state)
        {
            _statModifierArchetype = state.EntityManager.CreateArchetype(ComponentType.ReadWrite<StatModifierEntityTag>(), ComponentType.ReadWrite<StatModifier>(), ComponentType.ReadWrite<DestroyEntityFlag>(), ComponentType.ReadWrite<DestroyAfterTime>(), ComponentType.ReadWrite<CharacterEntity>());
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (statModifiers, interactionBuffer, activeTime) in SystemAPI.Query<DynamicBuffer<StatModifier>, DynamicBuffer<EntityInteraction>, TemporaryStatModifierActiveTime>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasBuffer<ActiveStatModifierEntity>(interaction.TargetEntity)) continue;

                    var newStatModifierEntity = state.EntityManager.CreateEntity(_statModifierArchetype);
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(newStatModifierEntity, false);
                    SystemAPI.SetComponent(newStatModifierEntity, new DestroyAfterTime { Value = activeTime.Value });
                    SystemAPI.SetComponent(newStatModifierEntity, new CharacterEntity { Value = interaction.TargetEntity });
                    var newStatModifierBuffer = SystemAPI.GetBuffer<StatModifier>(newStatModifierEntity);
                    newStatModifierBuffer.CopyFrom(statModifiers);

                    var activeStatModifierEntities = SystemAPI.GetBuffer<ActiveStatModifierEntity>(interaction.TargetEntity);
                    activeStatModifierEntities.Add(new ActiveStatModifierEntity { Value = newStatModifierEntity });
                    SystemAPI.SetComponentEnabled<RecalculateStatsFlag>(interaction.TargetEntity, true);
                }
            }
        }
    }
}