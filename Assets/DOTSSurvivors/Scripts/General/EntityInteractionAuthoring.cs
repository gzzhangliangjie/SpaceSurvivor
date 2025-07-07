using Unity.Burst;
using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于存储实体交互的动态缓冲区组件。
    /// </summary>
    /// <remarks>
    /// 实体交互是DOTS Survivors的一个关键概念，因为它们促进了多个实体之间的交互。交互可以有多种形式——攻击投射物对敌人实体造成伤害，治疗物品为玩家提供生命值，指示投射物从另一个实体反弹等等。
    /// 实体交互也可以通过几种方式引发——最常见的是通过碰撞或基于时间的事件，但也可以存在仅逻辑的交互，以促进不存在于游戏世界中的仅数据实体的交互。例如，当一个造成伤害的实体与一个接受伤害的实体碰撞时，将记录一个新的EntityInteraction缓冲区条目。在这种情况下，造成伤害的实体将拥有动态缓冲区，而接受伤害的实体将作为此记录的TargetEntity被添加。请注意，没有什么能阻止接受伤害的实体拥有自己的EntityInteraction缓冲区，并在同一次碰撞事件中将造成伤害的实体添加为记录。
    /// 当两个实体之间的交互被记录下来时，各种交互系统可以根据持有交互缓冲区的实体的组件构成来触发额外的行为。在造成伤害的实体的例子中，一个系统可以执行对存储在TargetEntity字段中的实体造成伤害。然而，其他交互系统可以触发不使用目标实体字段的行为——可能有一个系统为任何新的实体交互播放音效，或者在任何交互引发时直接自毁。这些交互系统及其行为的包含由添加到具有EntityInteraction缓冲区的实体的其他数据组件决定。
    /// 在 <see cref="DS_InteractionSystemGroup"/> 的末尾，EntityInteraction缓冲区中每个元素的IsHandled字段都设置为true。这样，当交互系统在下一帧运行时，它们可以检查缓冲区，只对尚未处理的新交互触发行为。所有先前的交互都保留在缓冲区中，而不是清除缓冲区。这是因为大多数引发交互的系统都应扫描缓冲区，看目标实体是否已在缓冲区中，以免多次添加。这对于触发器事件尤其有用，其中一个实体可能会在多帧内穿过另一个实体，但预期的行为是只在实体重叠的第一帧触发行为。
    /// 请注意，根据引发交互的系统和基于交互调用行为的系统的期望行为，这些系统可以简单地忽略检查缓冲区中是否存在目标实体，或者交互是否已被处理。此游戏中的绝大多数系统都实现了这些检查，但您会发现一些没有实现的例子。<see cref="BounceOnEnvironmentJob"/> 是一个触发器事件作业的例子，它不扫描交互缓冲区中是否存在先前添加的环境实体。
    /// </remarks>
    public struct EntityInteraction : IBufferElementData
    {
        /// <summary>
        /// 用于表示此交互已被处理的布尔字段。
        /// </summary>
        /// <remarks>
        /// 触发交互行为的系统会检查此字段，以确保行为只针对新的、未处理的交互执行。
        /// </remarks>
        public bool IsHandled;
        /// <summary>
        /// 用于存储交互目标实体的实体字段。
        /// </summary>
        /// <remarks>
        /// 当一个实体需要以某种方式与另一个实体交互时，可以使用此字段。例如，如果一个实体应对另一个实体造成伤害，则接受伤害的实体将存在于此字段中。
        /// 此字段还可用于确保特定实体尚未被添加到缓冲区，以避免交互被过于频繁地引发的情况。
        /// </remarks>
        public Entity TargetEntity;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="EntityInteraction"/> 缓冲区的创作组件。
    /// </summary>
    /// <remarks>
    /// 这个创作组件通常作为其他创作脚本的"RequireComponent"添加，因为许多不同类型的实体都具有与其他实体交互的能力。
    /// </remarks>
    public class EntityInteractionAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EntityInteractionAuthoring>
        {
            public override void Bake(EntityInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<EntityInteraction>(entity);
            }
        }
    }

    /// <summary>
    /// 将 <see cref="EntityInteraction.IsHandled"/> 字段标记为true的系统。
    /// </summary>
    /// <remarks>
    /// 此系统在 <see cref="DS_InteractionSystemGroup"/> 的末尾运行，以确保所有基于交互触发行为的系统在设置此值之前已经执行完毕。
    /// 有关IsHandled字段的目的和用例的更多信息，请参阅 <see cref="EntityInteraction"/> 动态缓冲区组件的文档。
    /// </remarks>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup), OrderLast = true)]
    public partial struct HandleEntityInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var interactionBuffer in SystemAPI.Query<DynamicBuffer<EntityInteraction>>())
            {
                for (var i = 0; i < interactionBuffer.Length; i++)
                {
                    var interaction = interactionBuffer.ElementAt(i);
                    interaction.IsHandled = true;
                    interactionBuffer.ElementAt(i) = interaction;
                }
            }
        }
    }
}