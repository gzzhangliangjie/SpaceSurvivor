using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 角色朝向的材质属性覆盖。设置此值将更新该实体材质实例中的_FacingDirection属性。这将翻转角色动画，使角色根据移动方向朝向左侧或右侧。
    /// </summary>
    /// <remarks>
    /// A value of 1 makes the character face to the right and a value of -1 makes the character face to the left.
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="CharacterMoveSystem"/>
    [MaterialProperty("_FacingDirection")]
    public struct FacingDirectionOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 角色溶解属性的材质属性覆盖。设置此值将更新该实体材质实例中的_Dissolve属性。这将使角色精灵在被摧毁时出现溶解效果。
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="DissolveData"/>
    /// <seealso cref="CharacterAnimationSystem"/>
    [MaterialProperty("_Dissolve")]
    public struct DissolveOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 与角色摧毁时溶解视觉效果相关的组件数据。
    /// </summary>
    /// <seeaslo cref="DissolveOverride"/>
    /// <seeaslo cref="CharacterAnimationSystem"/>
    /// <seeaslo cref="DestroyEntitySystem"/>
    public struct DissolveData : IComponentData
    {
        public float StartTimestamp;
        public float Duration;
    }

    /// <summary>
    /// 用于添加角色动画所需组件的创作脚本。
    /// </summary>
    /// <seealso cref="PlayerAnimationAuthoring"/>
    /// <seealso cref="EnemyAnimationAuthoring"/>
    public class CharacterAnimationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CharacterAnimationAuthoring>
        {
            public override void Bake(CharacterAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new FacingDirectionOverride { Value = 1f });
                AddComponent<DissolveOverride>(entity);
            }
        }
    }

    /// <summary>
    /// 处理角色材质变化以促进动画视觉效果的系统。
    /// 此系统包含4个foreach循环，功能如下：
    /// 1. 为敌人实体初始化 <see cref="AnimationOffsetOverride"/>，使它们的动画不同步。
    /// 2. 初始化 <see cref="EnhanceThicknessOverride"/>，在敌人外围应用视觉增强轮廓，表示它是强化实体。
    /// 3. 设置玩家实体当前播放的动画。用于根据当前输入（<see cref="CharacterMoveDirection"/>）和之前的输入（<see cref="PreviousPlayerInput"/>）在行走和空闲动画之间切换。
    /// 4. 当角色被摧毁时播放溶解效果。（参见 <see cref="DissolveData"/> 和 <see cref="DissolveOverride"/>）
    /// </summary>
    /// <remarks>
    /// This system executes in the <see cref="DS_EffectsSystemGroup"/> which executes towards the end of the frame.
    /// </remarks>
    [BurstCompile]
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct CharacterAnimationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<EntityRandom>(state.SystemHandle);
            state.EntityManager.AddComponentData(state.SystemHandle, new InitializeEntityRandom
            {
                InitializationType = EntityRandomInitializationType.SystemMilliseconds
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var systemRandom = SystemAPI.GetComponentRW<EntityRandom>(state.SystemHandle);

            // Initialize a random value used as an offset on the animation timer so enemy animations are not all synchronized
            foreach (var (spawnTime, shouldInitialize) in SystemAPI.Query<RefRW<AnimationOffsetOverride>, EnabledRefRW<InitializeAnimationOffsetOverrideFlag>>())
            {
                // Random value between 0 and 8/12 as most enemies have 8 animation frames played at 12 frames/second.
                spawnTime.ValueRW.Value = systemRandom.ValueRW.Value.NextFloat(8f / 12f);
                shouldInitialize.ValueRW = false;
            }

            // Initialize the enhancement thickness on enhanced entities
            foreach (var (graphicsEntity, initialize) in SystemAPI.Query<GraphicsEntity, EnabledRefRW<InitializeEnhancementMaterialFlag>>())
            {
                var thickness = SystemAPI.GetComponentRW<EnhanceThicknessOverride>(graphicsEntity.Value);
                thickness.ValueRW.Value = 1f;
                initialize.ValueRW = false;
            }

            // Set which animation is currently playing. Used for changing player animation between walk and idle
            // If the player currently has positive input and the previous frame they had zero input, set the player to the moving animation.
            // If the player currently has zero input and the previous frame they had positive input, set the player to the idle animation.
            foreach (var (moveDirection, previousPlayerInput, graphicsEntity) in SystemAPI.Query<CharacterMoveDirection, PreviousPlayerInput, GraphicsEntity>().WithAll<PlayerTag>())
            {
                if (math.lengthsq(moveDirection.Value) > float.Epsilon && math.lengthsq(previousPlayerInput.PreviousInput) < float.Epsilon)
                {
                    var animationIndexOverride = SystemAPI.GetComponentRW<AnimationIndexOverride>(graphicsEntity.Value);
                    animationIndexOverride.ValueRW.Value = (int)PlayerAnimationIndex.Movement;
                }
                else if (math.lengthsq(moveDirection.Value) < float.Epsilon && math.lengthsq(previousPlayerInput.PreviousInput) > float.Epsilon)
                {
                    var animationIndexOverride = SystemAPI.GetComponentRW<AnimationIndexOverride>(graphicsEntity.Value);
                    animationIndexOverride.ValueRW.Value = (int)PlayerAnimationIndex.Idle;
                }
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // Play the dissolve effect when an entity gets destroyed
            foreach (var (dissolveOverride, dissolveData, entity) in SystemAPI.Query<RefRW<DissolveOverride>, DissolveData>().WithEntityAccess())
            {
                var dissolveTime = currentTime - dissolveData.StartTimestamp;
                var t = dissolveTime / dissolveData.Duration;
                dissolveOverride.ValueRW.Value = t;
                if (t < 1f) continue;
                ecb.AddComponent<InstantDestroyTag>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}