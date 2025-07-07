using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与结果面板上单个能力元素关联的UI控制器。
    /// </summary>
    /// <remarks>
    /// Capabilities refer to weapons and passive abilities owned and leveled up by the player.
    /// In the end results panel, this will display the icon, name, and level at game over time of each capability owned by the player.
    /// </remarks>
    public class CapabilityResultController : MonoBehaviour
    {
        [SerializeField] private Image _capabilityIconImage;
        [SerializeField] private TextMeshProUGUI _displayNameText;

        public void InitializeUI(Sprite icon, string displayName, int levelIndex)
        {
            _capabilityIconImage.sprite = icon;
            _displayNameText.text = $"{displayName} - LV: {levelIndex + 1}";
        }
    }
}