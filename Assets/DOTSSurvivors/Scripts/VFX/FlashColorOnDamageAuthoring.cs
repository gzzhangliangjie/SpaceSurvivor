using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于保存角色受到伤害时闪烁图形实体视觉效果相关数据的组件。
    /// </summary>
    /// <remarks>
    /// This component should go on the graphics entity of the character.
    /// Enableable component so the <see cref="FlashColorOnDamageSystem"/> will only run on entities with this component enabled.
    /// </remarks>
    /// <seealso cref="FlashColorOnDamageTimer"/>
    /// <seealso cref="FlashColorOnDamageSystem"/>
    public struct FlashColorOnDamageData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 实体受到伤害时将被着色的颜色。
        /// </summary>
        /// <remarks>
        /// This is actually the 2nd color that will flash. For the first half of the effect the entity will be all black.
        /// </remarks>
        public float4 Color;
        /// <summary>
        /// 闪烁效果的总时间（秒）。
        /// </summary>
        /// <remarks>
        /// The first half of this time, the entity will be all black. The second half the entity will be tinted with the color defined in the Color field.
        /// </remarks>
        public float FlashTime;
    }

    /// <summary>
    /// 用于在特定时间内显示闪烁效果的计时器。此计时器将从 <see cref="FlashColorOnDamageData.FlashTime"/> 倒计时到零。
    /// </summary>
    /// <seealso cref="FlashColorOnDamageSystem"/>
    public struct FlashColorOnDamageTimer : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于初始化 <see cref="FlashColorOnDamageData"/> 组件值并添加其他必要组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note that this authoring script should be attached to the graphics entity which is a child of the character entity.
    /// </remarks>
    public class FlashColorOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 实体受到伤害时将被着色的颜色。
        /// </summary>
        /// <remarks>
        /// This is actually the 2nd color that will flash. For the first half of the effect the entity will be all black.
        /// </remarks>
        public Color Color;
        /// <summary>
        /// 闪烁效果的总时间（秒）。
        /// </summary>
        /// <remarks>
        /// The first half of this time, the entity will be all black. The second half the entity will be tinted with the color defined in the Color field.
        /// </remarks>
        public float FlashTime;

        private class Baker : Baker<FlashColorOnDamageAuthoring>
        {
            public override void Bake(FlashColorOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new FlashColorOnDamageData
                {
                    Color = (Vector4)authoring.Color,
                    FlashTime = authoring.FlashTime
                });
                SetComponentEnabled<FlashColorOnDamageData>(entity, false);
                AddComponent<FlashColorOnDamageTimer>(entity);
                AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(1) });
            }
        }
    }

    /// <summary>
    /// 当父角色实体受到伤害时，此系统将使图形实体闪烁黑色，然后闪烁 <see cref="FlashColorOnDamageData.Color"/> 中定义的颜色。这为玩家在实体受到伤害时提供良好的视觉反馈。
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct FlashColorOnDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (timer, baseColorProperty, flashColorOnDamage, shouldFlash) in SystemAPI.Query<RefRW<FlashColorOnDamageTimer>, RefRW<URPMaterialPropertyBaseColor>, FlashColorOnDamageData, EnabledRefRW<FlashColorOnDamageData>>())
            {
                timer.ValueRW.Value -= deltaTime;
                // First half of the flash duration, set the color of the sprite to all black
                if (timer.ValueRO.Value > flashColorOnDamage.FlashTime / 2f)
                {
                    baseColorProperty.ValueRW.Value = new float4(0, 0, 0, 1);
                }
                // 2nd half of the flash duration, apply a tint of the color specified (i.e. red)
                else if (timer.ValueRO.Value > 0f)
                {
                    baseColorProperty.ValueRW.Value = flashColorOnDamage.Color;
                }
                // Reset to normal sprite look
                else
                {
                    baseColorProperty.ValueRW.Value = new float4(1);
                    shouldFlash.ValueRW = false;
                }
            }
        }
    }
}