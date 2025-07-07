using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 动画索引的材质属性覆盖。设置此值将为此实体的材质实例设置动画索引。
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    [MaterialProperty("_AnimationIndex")]
    public struct AnimationIndexOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 用于初始化和添加玩家角色动画所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// This authoring script should be attached to the graphics entity which is a child of the main player entity.
    /// <see cref="CharacterAnimationAuthoring"/> is a required component to ensure components necessary for character animation are applied to the entity.
    /// </remarks>
    /// <seealso cref="AnimationIndexOverride"/>
    [RequireComponent(typeof(CharacterAnimationAuthoring))]
    public class PlayerAnimationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAnimationAuthoring>
        {
            public override void Bake(PlayerAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AnimationIndexOverride { Value = (int)PlayerAnimationIndex.Idle });
            }
        }
    }
}