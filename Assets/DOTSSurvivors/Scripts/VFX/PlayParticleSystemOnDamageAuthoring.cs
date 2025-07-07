using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 当此实体受到伤害时应播放的粒子系统的GameObject预制体。
    /// </summary>
    /// <seealso cref="PlayParticleSystemOnDamage"/>
    public struct OnDamageParticleSystemPrefab : IComponentData
    {
        public UnityObjectRef<GameObject> Value;
    }

    /// <summary>
    /// 用于保存当此实体受到伤害时将播放的粒子系统引用的清理组件。
    /// </summary>
    /// <remarks>
    /// As this is a cleanup component, this value will be initialized via the <see cref="OnDamageParticleSystemPrefab"/> as a GameObject instance will not be able to be baked into this component.
    /// </remarks>
    public struct PlayParticleSystemOnDamage : ICleanupComponentData
    {
        public UnityObjectRef<ParticleSystem> Value;
    }

    /// <summary>
    /// 用于表示此实体在受到伤害时应开始播放粒子系统的可启用组件。
    /// </summary>
    public struct PlayParticleSystemOnDamageFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于添加实体受到伤害时播放粒子系统所需组件的创作脚本。
    /// </summary>
    /// <remarks>
    /// Authoring script should be attached to the root character entity.
    /// </remarks>
    public class PlayParticleSystemOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 当此实体受到伤害时应播放的粒子系统的GameObject预制体。
        /// </summary>
        public GameObject ParticleSystemPrefab;

        private class Baker : Baker<PlayParticleSystemOnDamageAuthoring>
        {
            public override void Bake(PlayParticleSystemOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new OnDamageParticleSystemPrefab
                {
                    Value = authoring.ParticleSystemPrefab
                });
                AddComponent<PlayParticleSystemOnDamageFlag>(entity);
                SetComponentEnabled<PlayParticleSystemOnDamageFlag>(entity, false);
            }
        }
    }

    /// <summary>
    /// 管理实体受到伤害时播放的粒子系统生命周期的系统。
    /// 包含3个foreach循环，功能如下：
    /// 1. 从 <see cref="OnDamageParticleSystemPrefab"/> 实例化粒子系统预制体实例，并在 <see cref="PlayParticleSystemOnDamage"/> 中存储其引用。
    /// 2. 将粒子系统的变换同步到实体的变换。如果启用了 <see cref="PlayParticleSystemOnDamage"/>，则播放粒子系统。
    /// 3. 当实体被销毁时，销毁粒子系统GameObject。
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame. As such, transform is synchronized via the entity's LocalToWorld component as this will have been updated by this point in the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayParticleSystemOnDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // Instantiate particle system
            foreach (var (particleSystemPrefab, entity) in SystemAPI.Query<OnDamageParticleSystemPrefab>().WithNone<PlayParticleSystemOnDamage>().WithEntityAccess())
            {
                var newParticleSystem = Object.Instantiate(particleSystemPrefab.Value.Value);
                var playParticleSystemOnDamage = new PlayParticleSystemOnDamage
                {
                    Value = newParticleSystem.GetComponent<ParticleSystem>()
                };
                ecb.AddComponent(entity, playParticleSystemOnDamage);
                ecb.RemoveComponent<OnDamageParticleSystemPrefab>(entity);
            }

            // Sync particle system transform and play if necessary
            foreach (var (transform, particleSystem, playParticleSystem) in SystemAPI.Query<LocalToWorld, PlayParticleSystemOnDamage, EnabledRefRW<PlayParticleSystemOnDamageFlag>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                particleSystem.Value.Value.transform.position = transform.Position;
                if (playParticleSystem.ValueRO)
                {
                    playParticleSystem.ValueRW = false;
                    particleSystem.Value.Value.Play();
                }
            }

            // Cleanup particle system
            foreach (var (particleSystem, entity) in SystemAPI.Query<PlayParticleSystemOnDamage>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                if (particleSystem.Value.Value != null)
                {
                    Object.Destroy(particleSystem.Value.Value.gameObject);
                }
                ecb.RemoveComponent<PlayParticleSystemOnDamage>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}