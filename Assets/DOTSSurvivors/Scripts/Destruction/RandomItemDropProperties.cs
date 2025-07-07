using UnityEngine;
using System.Collections.Generic;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 可序列化的辅助结构体，用于在Unity编辑器中方便地创作包含在 <see cref="RandomItemDropProperties.ItemDrops"/> 列表中的多个元素。
    /// </summary>
    [System.Serializable]
    public struct RandomItemDropInfo
    {
        /// <summary>
        /// 将要掉落的GameObject预制体。在 <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> 中，此GameObject将被烘焙成实体预制体。
        /// </summary>
        public GameObject Prefab;

        /// <summary>
        /// 决定物品掉落的频率。值为100时掉落几率最高，值为0时永远不会被选中。
        /// </summary>
        /// <remarks>
        /// 这不是一个百分比几率，而是一个相对几率，所以如果4个元素各自的Rarity值都是100，那么它们每个都有25%的掉落几率。
        /// </remarks>
        [Range(0, 100)]
        [Tooltip("决定物品掉落的频率。值为100时掉落几率最高，值为0时永远不会被选中。")]
        public int Rarity;
    }

    /// <summary>
    /// 用于定义实体被销毁时可以掉落哪些物品的ScriptableObject。
    /// </summary>
    /// <seealso cref="RandomItemToSpawnBlob"/>
    /// <seealso cref="DestroyEntitySystem"/>
    [CreateAssetMenu(fileName = "RandomItemDropProperties", menuName = "ScriptableObjects/Random Item Drop Properties")]
    public class RandomItemDropProperties : ScriptableObject
    {
        /// <summary>
        /// <see cref="RandomItemDropInfo"/> 元素列表，用于通过Unity编辑器设定可掉落物品及其相关稀有度。
        /// </summary>
        public List<RandomItemDropInfo> ItemDrops;
    }
}