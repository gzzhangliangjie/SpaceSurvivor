using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与经验宝石相关的数据，当敌人被摧毁时有几率掉落。
    /// </summary>
    public struct ExperienceGemItemData : IComponentData
    {
        /// <summary>
        /// 当玩家拾取宝石时授予玩家的经验值数量。
        /// </summary>
        public int Value;
    }

    /// <summary>
    /// 用于初始化经验宝石的标志。
    /// </summary>
    /// <remarks>
    /// 初始化根据宝石的价值设置其颜色。价值到颜色的映射在 <see cref="GameAuthoring.ExperienceGemColorLookup"/> 中创作，并存储在 <see cref="ManagedGameData.ExperienceGemColorLookup"/> 中。
    /// </remarks>
    public struct InitializeExperienceGemFlag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 用于为经验点宝石设置初始经验点值的Authoring脚本。
    /// </summary>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(GraphicsEntityAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class ExperienceGemItemAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 收集宝石时授予玩家的经验点数。
        /// </summary>
        public int ExperiencePointValue;

        private class Baker : Baker<ExperienceGemItemAuthoring>
        {
            public override void Bake(ExperienceGemItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ExperienceGemItemData
                {
                    Value = authoring.ExperiencePointValue
                });
                AddComponent<InitializeExperienceGemFlag>(entity);
            }
        }
    }

    /// <summary>
    /// 当玩家收集经验点宝石时，为玩家增加经验点数的系统。
    /// </summary>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct HandleExperienceGemInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, experiencePointItemProperties) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, ExperienceGemItemData>().WithAll<ItemTag>())
            {
                for (var i = 0; i < interactionBuffer.Length; i++)
                {
                    var interaction = interactionBuffer[i];
                    if (interaction.IsHandled) continue;

                    var playerExperienceThisFrame = SystemAPI.GetBuffer<PlayerExperienceThisFrame>(interaction.TargetEntity);
                    playerExperienceThisFrame.Add(new PlayerExperienceThisFrame { Value = experiencePointItemProperties.Value });
                }
            }
        }
    }

    /// <summary>
    /// 根据经验点宝石的价值初始化其颜色的系统。
    /// </summary>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct InitializeExperienceGemSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameEntityTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameControllerEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
            var experienceGemColorLookup = state.EntityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).ExperienceGemColorLookup;

            foreach (var (graphicsEntity, gemData, shouldInitialize) in SystemAPI.Query<GraphicsEntity, ExperienceGemItemData, EnabledRefRW<InitializeExperienceGemFlag>>())
            {
                var experiencePointColor = new float4(1);
                for (var i = experienceGemColorLookup.Count - 1; i >= 0; i--)
                {
                    if (gemData.Value > experienceGemColorLookup[i].Item1)
                    {
                        experiencePointColor = experienceGemColorLookup[i].Item2;
                        break;
                    }
                }

                if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(graphicsEntity.Value))
                {
                    SystemAPI.SetComponent(graphicsEntity.Value, new URPMaterialPropertyBaseColor { Value = experiencePointColor });
                }

                shouldInitialize.ValueRW = false;
            }
        }
    }
}