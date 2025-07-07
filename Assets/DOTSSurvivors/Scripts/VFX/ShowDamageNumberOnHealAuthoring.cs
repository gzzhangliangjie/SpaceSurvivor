using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于保存在角色治疗时在其上方显示绿色"伤害"数字相关数据的组件。
    /// </summary>
    /// <remarks>
    /// Enableable component will be enabled on the frame that the "damage" number should be displayed and disabled subsequently after.
    /// </remarks>
    public struct ShowDamageNumberOnHeal : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 当前正在治疗的生命值整数，将显示在世界空间UI中。
        /// </summary>
        public int HealAmount;
        /// <summary>
        /// 伤害数字的初始偏移量，以便正确显示在角色上方。
        /// </summary>
        public float3 BaseOffset;
    }

    /// <summary>
    /// 用于初始化在实体治疗时在其上方显示"伤害"数字所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Authoring script should be attached to the base character entity.
    /// </remarks>
    /// <seealso cref="ShowDamageNumberOnHeal"/>
    /// <seealso cref="ShowDamageNumberOnHealSystem"/>
    public class ShowDamageNumberOnHealAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 伤害数字的初始偏移量，以便正确显示在角色上方。
        /// </summary>
        public Vector3 BaseOffset;

        private class Baker : Baker<ShowDamageNumberOnHealAuthoring>
        {
            public override void Bake(ShowDamageNumberOnHealAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ShowDamageNumberOnHeal
                {
                    BaseOffset = authoring.BaseOffset
                });
                SetComponentEnabled<ShowDamageNumberOnHeal>(entity, false);
            }
        }
    }

    /// <summary>
    /// 在敌人治疗时在其上方显示"伤害"数字的系统。仅在启用了 <see cref="ShowDamageNumberOnHeal"/> 组件的实体上运行。
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame. As such base transform position comes from the entity's LocalToWorld component which will have been updated by this point in the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct ShowDamageNumberOnHealSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (damageNumberData, transform, showDamageNumber) in SystemAPI.Query<ShowDamageNumberOnHeal, LocalToWorld, EnabledRefRW<ShowDamageNumberOnHeal>>())
            {
                var startPosition = transform.Position + damageNumberData.BaseOffset;
                WorldUICanvasController.Instance.DisplayDamageNumber(damageNumberData.HealAmount, startPosition, true);
                showDamageNumber.ValueRW = false;
            }
        }
    }
}