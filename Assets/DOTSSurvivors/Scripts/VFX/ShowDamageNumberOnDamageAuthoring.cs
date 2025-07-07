using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于保存角色受到伤害时在其上方显示伤害数字相关数据的组件。
    /// </summary>
    /// <remarks>
    /// Enableable component - when enabled it signifies that this entity should display a damage number above it.
    /// </remarks>
    public struct ShowDamageNumberOnDamage : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 要在世界UI中显示的伤害数值。
        /// </summary>
        public int DamageAmount;
        /// <summary>
        /// 伤害数字的初始偏移，使其能正确显示在角色上方。
        /// </summary>
        public float3 BaseOffset;
    }

    /// <summary>
    /// 用于初始化在实体上方显示伤害数字所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Authoring script should be attached to the base character entity.
    /// </remarks>
    /// <seealso cref="ShowDamageNumberOnDamage"/>
    /// <seealso cref="ShowDamageNumberOnDamageSystem"/>
    public class ShowDamageNumberOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 伤害数字的初始偏移，使其能正确显示在角色上方。
        /// </summary>
        public Vector3 BaseOffset;

        private class Baker : Baker<ShowDamageNumberOnDamageAuthoring>
        {
            public override void Bake(ShowDamageNumberOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ShowDamageNumberOnDamage
                {
                    BaseOffset = authoring.BaseOffset
                });
                SetComponentEnabled<ShowDamageNumberOnDamage>(entity, false);
            }
        }
    }

    /// <summary>
    /// 在实体受到伤害时在其上方显示伤害数字的系统。仅在启用了 <see cref="ShowDamageNumberOnDamage"/> 组件的实体上运行。
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame. As such base transform position comes from the entity's LocalToWorld component which will have been updated by this point in the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct ShowDamageNumberOnDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (damageNumberData, transform, showDamageNumber) in SystemAPI.Query<ShowDamageNumberOnDamage, LocalToWorld, EnabledRefRW<ShowDamageNumberOnDamage>>())
            {
                var startPosition = transform.Position + damageNumberData.BaseOffset;
                WorldUICanvasController.Instance.DisplayDamageNumber(damageNumberData.DamageAmount, startPosition);
                showDamageNumber.ValueRW = false;
            }
        }
    }
}