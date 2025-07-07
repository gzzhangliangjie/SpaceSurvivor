using System;
using Unity.Entities;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 定义金币物品价值的组件。
    /// </summary>
    /// <seealso cref="GrantMoneyOnInteractionSystem"/>
    public struct GrantMoneyOnInteraction : IComponentData
    {
        /// <summary>
        /// 当玩家收集金币物品时授予的金钱数量。
        /// </summary>
        public int Value;
    }

    /// <summary>
    /// 用于初始化金币物品价值的Authoring脚本。
    /// </summary>
    /// <remarks>
    /// 需要额外的Authoring脚本来添加所需行为的必要组件。
    /// </remarks>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class GrantMoneyOnInteractionAuthoring : MonoBehaviour
    {
        public int CoinValue;

        private class Baker : Baker<GrantMoneyOnInteractionAuthoring>
        {
            public override void Bake(GrantMoneyOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GrantMoneyOnInteraction { Value = authoring.CoinValue });
            }
        }
    }

    /// <summary>
    /// 当玩家收集金币物品时授予金钱的系统。
    /// </summary>
    /// <remarks>
    /// 该系统是SystemBase类型，因为它有一个System.Action事件类型作为成员变量，当玩家收集金币时会调用该事件。
    /// </remarks>
    /// <seealso cref="GrantMoneyOnInteraction"/>
    /// <seealso cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial class GrantMoneyOnInteractionSystem : SystemBase
    {
        /// <summary>
        /// 当玩家收集金币时调用的事件。用于使用最新的金币数量更新HUD UI。
        /// </summary>
        public Action<int> OnUpdateCoinCount;

        protected override void OnUpdate()
        {
            foreach (var (interactionBuffer, coinItemProperties) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, GrantMoneyOnInteraction>().WithAll<ItemTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<PlayerTag>(interaction.TargetEntity)) continue;
                    var coinsCollected = SystemAPI.GetSingletonRW<CoinsCollected>();
                    coinsCollected.ValueRW.Value += coinItemProperties.Value;

                    OnUpdateCoinCount?.Invoke(coinsCollected.ValueRO.Value);
                }
            }
        }
    }
}