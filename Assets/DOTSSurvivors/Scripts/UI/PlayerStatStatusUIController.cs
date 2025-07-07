using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与显示玩家当前可修改属性的单个属性元素关联的UI控制器。
    /// </summary>
    /// <remarks>
    /// These stats account for modifications the character spawns with and ones modified via passive abilities. Depending on the stat type it will display as an absolute number, absolute difference, or percentage difference.
    /// </remarks>
    /// <seealso cref="CurrentStatsUIController"/>
    public class PlayerStatStatusUIController : MonoBehaviour
    {
        [SerializeField] private Image _statIcon;
        [SerializeField] private TextMeshProUGUI _statTitleText;
        [SerializeField] private TextMeshProUGUI _statValueText;

        /// <summary>
        /// 显示与要显示的当前属性关联的UI元素。
        /// </summary>
        /// <param name="modifierProperties">ScriptableObject containing the data and assets associated with this stat.</param>
        /// <param name="defaultValue">Default value of the stat. Used to compare with the current value to determine how the value should be displayed.</param>
        /// <param name="currentValue">Current value of the stat. Compared with the default value for times when a difference from the default should be shown.</param>
        public void ShowUI(StatModifierProperties modifierProperties, float defaultValue, float currentValue)
        {
            _statIcon.sprite = modifierProperties.Icon;
            _statTitleText.text = modifierProperties.Title;

            var valueText = "-";
            var valueType = modifierProperties.CalculationType;
            var sign = currentValue > defaultValue ? "+" : "";

            switch (valueType)
            {
                case StatModifierCalculationType.AbsoluteInteger:
                    if (currentValue == 0) break;
                    valueText = currentValue.ToString("N0");
                    break;
                case StatModifierCalculationType.AbsoluteDecimal:
                    if (currentValue == 0) break;
                    valueText = currentValue.ToString("N2");
                    break;
                case StatModifierCalculationType.IncreasingInteger:
                    if (currentValue == defaultValue) break;
                    var value = currentValue - defaultValue;
                    valueText = $"{sign}{value:N0}";
                    break;
                case StatModifierCalculationType.Percentage:
                    if (currentValue == defaultValue) break;
                    var percentage = (currentValue - defaultValue) / defaultValue * 100f;
                    valueText = $"{sign}{percentage:N0}%";
                    break;
            }

            _statValueText.text = valueText;
        }
    }
}