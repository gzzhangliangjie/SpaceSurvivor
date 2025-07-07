using Unity.Entities;
using Unity.Transforms;
using Unity.Physics.Systems;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 包含与玩家攻击相关系统的系统组。
    /// </summary>
    /// <remarks>
    /// 在Unity的主SimulationSystemGroup中更新。必须在Unity的TransformSystemGroup和 <see cref="DS_InteractionSystemGroup"/> 之前更新，因为这些组中的系统依赖于此组中系统的完成。
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(DS_InteractionSystemGroup))]
    public partial class DS_AttackSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 包含与实体交互相关系统的系统组。
    /// </summary>
    /// <remarks>
    /// 在Unity的主SimulationSystemGroup中更新。必须在Unity的FixedStepSimulationSystemGroup和 <see cref="DS_AttackSystemGroup"/> 之后更新，因为这些组中的系统会引发由此组中系统处理的实体交互。
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DS_AttackSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial class DS_InteractionSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 包含应在帧开始时执行的初始化逻辑相关系统的系统组。
    /// </summary>
    /// <remarks>
    /// 在Unity的InitializationSystemGroup末尾更新，以确保逻辑已完成，但它在该组末尾的命令缓冲区系统之前，以防需要在该ECB中安排任何事情。
    /// </remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
    public partial class DS_InitializationSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 包含处理物理（如调度碰撞/触发事件作业和执行物理重叠查询）相关系统的系统组。
    /// </summary>
    /// <remarks>
    /// 在Unity的PhysicsSystemGroup中更新（该组是Unity的FixedStepSimulationSystemGroup的一部分，以固定节拍更新）。将在Unity的PhysicsSimulationGroup之后和其AfterPhysicsSystemGroup之前更新，以确保在帧中的适当位置查看当前物理步骤的碰撞/触发事件和重叠查询。
    /// </remarks>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
    public partial class DS_PhysicsSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 包含通过LocalTransform组件移动实体的相关系统的系统组。
    /// </summary>
    /// <remarks>
    /// 在Unity的主SimulationSystemGroup中更新，并在其TransformSystemGroup之前，以确保在将对LocalTransform的任何更改传递给用于告知GPU在当前帧渲染实体位置的LocalToWorld组件之前进行这些更改。
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class DS_TranslationSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 包含触发视觉和音频效果相关系统的系统组。
    /// </summary>
    /// <remarks>
    /// 在Unity的主SimulationSystemGroup末尾更新，以确保在执行效果系统之前，任何需要触发的效果都已被知晓（通常通过可启用组件）。在 <see cref="DS_DestructionSystemGroup"/> 之前更新，以确保实体在播放当前帧所需效果之前不被销毁。
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(DS_DestructionSystemGroup))]
    public partial class DS_EffectsSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 包含与实体销毁相关系统的系统组。
    /// </summary>
    /// <remarks>
    /// 在Unity的主SimulationSystemGroup末尾更新，以确保在各种早期系统中标记为销毁的所有实体都得到处理，并确保实体不会被过早销毁。在Unity的EndSimulationEntityCommandBufferSystem之前更新，因为此组中的系统会在该实体命令缓冲区中安排事物。
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class DS_DestructionSystemGroup : ComponentSystemGroup
    {

    }
}