using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 敌人动画偏移的材质属性覆盖。设置此值将更新该实体材质实例中的_SpawnTime属性。这为动画提供偏移，使所有敌人都能处于自己独特的动画帧，不会出现同步效果。
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="InitializeAnimationOffsetOverrideFlag"/>
    /// <seealso cref="CharacterAnimationSystem"/>
    [MaterialProperty("_SpawnTime")]
    public struct AnimationOffsetOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 标志组件，表示此实体需要初始化其 <see cref="AnimationOffsetOverride"/> 组件。
    /// </summary>
    /// <seealso cref="CharacterAnimationSystem"/>
    public struct InitializeAnimationOffsetOverrideFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 敌人生成时间的材质属性覆盖。设置此值将更新该实体材质实例中的_SpawnTime属性。这为动画提供偏移，使所有敌人都能处于自己独特的动画帧，不会出现同步效果。
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="InitializeAnimationOffsetOverrideFlag"/>
    /// <seealso cref="CharacterAnimationSystem"/>
    [MaterialProperty("_EnhanceThickness")]
    public struct EnhanceThicknessOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 标志组件，表示应启用 <see cref="EnhanceThicknessOverride"/> 组件。
    /// </summary>
    /// <remarks>
    /// Note that this component will be added to the main enemy entity when spawning an enhanced entity. However, the <see cref="EnhanceThicknessOverride"/> component will be added to the child graphics entity in <see cref="EnemyAnimationAuthoring"/>.
    /// </remarks>
    /// <seealso cref="CharacterAnimationSystem"/>
    public struct InitializeEnhancementMaterialFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于添加敌人角色动画所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// This authoring script should be attached to the graphics entity which is a child of the main enemy entity.
    /// <see cref="CharacterAnimationAuthoring"/> is a required component to ensure components necessary for character animation are applied to the entity.
    /// </remarks>
    [RequireComponent(typeof(CharacterAnimationAuthoring))]
    public class EnemyAnimationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnemyAnimationAuthoring>
        {
            public override void Bake(EnemyAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<AnimationOffsetOverride>(entity);
                AddComponent<InitializeAnimationOffsetOverrideFlag>(entity);
                AddComponent<EnhanceThicknessOverride>(entity);
            }
        }
    }
}