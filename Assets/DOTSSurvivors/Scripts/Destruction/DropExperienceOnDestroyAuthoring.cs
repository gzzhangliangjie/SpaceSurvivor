using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 带有此组件的实体在通过 <see cref="DestroyEntitySystem"/> 销毁时，有机会掉落指定价值的经验点。
    /// </summary>
    /// <remarks>
    /// 这可以作为一个清理组件来实现，以避免在 <see cref="DestroyEntitySystem"/> 中包含逻辑，但是我选择将逻辑保留在那里，这样我们也可以有一个不生成经验宝石的 <see cref="InstantDestroyEntitySystem"/>。如果将此作为清理组件，该实现会变得混乱。
    /// -Johnny
    /// </remarks>
    /// <seealso cref="DropExperienceOnDestroyAuthoring"/>
    /// <seealso cref="DestroyEntitySystem"/>
    public struct DropExperienceOnDestroy : IComponentData
    {
        /// <summary>
        /// 当此实体被销毁时，将分配给掉落宝石的经验点数。
        /// </summary>
        /// <seealso cref="ExperienceGemItemData"/>
        public int ExperienceValue;
        /// <summary>
        /// 值为0到100，表示被摧毁的实体掉落经验点宝石的百分比几率。0表示永远不会掉落宝石，100表示总是会掉落宝石。
        /// </summary>
        public int ChanceToDrop;
    }

    /// <summary>
    /// 用于向实体添加 <see cref="DropExperienceOnDestroy"/> 的创作脚本。
    /// </summary>
    /// <remarks>
    /// 这通常添加到被摧毁时会掉落经验宝石的敌人身上。
    /// </remarks>
    /// <seealso cref="DropExperienceOnDestroy"/>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="ExperienceGemItemData"/>
    public class DropExperienceOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 当此实体被销毁时，将分配给掉落宝石的经验点数。
        /// </summary>
        /// <seealso cref="ExperienceGemItemData"/>
        public int ExperienceValue;
        /// <summary>
        /// 值为0到100，表示被摧毁的实体掉落经验点宝石的百分比几率。0表示永远不会掉落宝石，100表示总是会掉落宝石。
        /// </summary>
        [Range(0, 100)] public int ChanceToDrop = 100;

        private class Baker : Baker<DropExperienceOnDestroyAuthoring>
        {
            public override void Bake(DropExperienceOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DropExperienceOnDestroy
                {
                    ExperienceValue = authoring.ExperienceValue,
                    ChanceToDrop = authoring.ChanceToDrop
                });
            }
        }
    }
}