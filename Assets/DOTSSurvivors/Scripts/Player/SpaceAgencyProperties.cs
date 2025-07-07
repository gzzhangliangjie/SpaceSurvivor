using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于定义与单个航天机构相关属性的ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "-SpaceAgencyProperties", menuName = "ScriptableObjects/Space Agency Properties")]
    public class SpaceAgencyProperties : ScriptableObject
    {
        /// <summary>
        /// 在结果UI中显示的航天机构名称。
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// 在UI和游戏中的多个地方显示的航天机构旗帜图标。
        /// </summary>
        public Sprite FlagIcon;
    }
}