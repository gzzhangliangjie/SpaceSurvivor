using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 标志组件，表示此实体需要初始化其 <see cref="FadeAttackInOutTotalTimeToLive"/> 组件。
    /// </summary>
    public struct InitializeFadeAttackInOutFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 组件，用于保存此实体在游戏世界中存在的总时间。用于计算攻击视觉效果淡入淡出的透明度值。
    /// </summary>
    public struct FadeAttackInOutTotalTimeToLive : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于添加攻击视觉效果淡入淡出所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Note: this authoring component will be added to the graphics entity which should be the direct child of the main attack prefab.
    /// This also assumes that the main attack prefab has a <see cref="DestroyAfterTime"/> component.
    /// </remarks>
    public class FadeAttackInOutAuthoring : MonoBehaviour
    {
        private class Baker : Baker<FadeAttackInOutAuthoring>
        {
            public override void Bake(FadeAttackInOutAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<InitializeFadeAttackInOutFlag>(entity);
                AddComponent<FadeAttackInOutTotalTimeToLive>(entity);
                AddComponent<URPMaterialPropertyBaseColor>(entity);
            }
        }
    }

    /// <summary>
    /// 处理攻击视觉效果淡入淡出的系统。
    /// </summary>
    /// <remarks>
    /// System gets the current value of the parent's <see cref="DestroyAfterTime"/> component, which stores the time in seconds remaining until the entity will be destroyed. Uses a quadratic function to calculate the current alpha value which is applied via the Unity built-in material override - URPMaterialPropertyBaseColor.
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct FadeAttackInOutSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (parent, fadeAttackInOut, initializeFadeAttackInOut) in SystemAPI.Query<Parent, RefRW<FadeAttackInOutTotalTimeToLive>, EnabledRefRW<InitializeFadeAttackInOutFlag>>())
            {
                var timeToLive = SystemAPI.GetComponent<DestroyAfterTime>(parent.Value).Value;
                fadeAttackInOut.ValueRW.Value = timeToLive;
                initializeFadeAttackInOut.ValueRW = false;
            }

            foreach (var (colorOverride, fadeAttackInOut, parent) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, RefRW<FadeAttackInOutTotalTimeToLive>, Parent>())
            {
                var timeToLive = SystemAPI.GetComponent<DestroyAfterTime>(parent.Value).Value;
                var t = timeToLive / fadeAttackInOut.ValueRO.Value;
                var x = t * 4 - 2;
                var a = -0.25f * x * x + 1;
                colorOverride.ValueRW.Value = new float4(1, 1, 1, a);
            }
        }
    }
}