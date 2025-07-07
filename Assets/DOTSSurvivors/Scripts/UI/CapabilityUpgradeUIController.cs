using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与升级UI中显示的单个能力升级UI元素关联的UI控制器。
    /// </summary>
    /// <remarks>
    /// Displays information about a single weapon or passive ability that is available to be leveled up.
    /// </remarks>
    public class CapabilityUpgradeUIController : MonoBehaviour
    {
        [SerializeField] private Image _itemIconImage;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private TextMeshProUGUI _itemLevelText;
        [SerializeField] private TextMeshProUGUI _itemDescriptionText;
        [SerializeField] private Button _button;

        public Button Button => _button;

        private void OnDisable()
        {
            _button.onClick.RemoveAllListeners();
        }

        public void DisableInteraction()
        {
            _button.interactable = false;
        }

        /// <summary>
        /// 设置UI元素以提供新等级将发生的变化信息。添加在元素被点击时将被调用的事件监听器。
        /// </summary>
        /// <param name="upgradeLevel">Helper struct containing the <see cref="UpgradeProperties"/> associated with the capability to display and the level index it is currently at.</param>
        public void SetLevelUpUI(CapabilityUpgradeLevel upgradeLevel)
        {
            var upgradeProperties = upgradeLevel.UpgradeProperties;
            var nextLevelIndex = upgradeLevel.NextLevelIndex;
            _itemIconImage.sprite = upgradeProperties.Icon;
            _itemNameText.text = upgradeProperties.Name;
            var itemLevelText = nextLevelIndex == 0 ? "<color=yellow>New!</color>" : $"Level: {nextLevelIndex + 1}";
            _itemLevelText.text = itemLevelText;
            _itemDescriptionText.text = upgradeProperties.GetDescription(nextLevelIndex);
            _button.onClick.AddListener(() => CapabilityUpgradeController.Instance.UpgradeCapability(upgradeProperties));
            _button.onClick.AddListener(() => LevelUpUIController.Instance.HideLevelUpUI());
            _button.onClick.AddListener(() => SelectionIconUIController.Instance.SetPositionOffscreen());
        }

        /// <summary>
        /// 设置"奖励物品"UI元素的替代方法，当玩家拥有所有10个能力且都达到最大等级时将显示。
        /// </summary>
        /// <param name="itemProperties">Properties of the bonus item to be displayed in the UI.</param>
        public void SetLevelUpUI(BonusItemProperties itemProperties)
        {
            _itemIconImage.sprite = itemProperties.ItemIcon;
            _itemNameText.text = itemProperties.ItemName;
            _itemLevelText.text = "";
            _itemDescriptionText.text = itemProperties.ItemDescription;
            _button.onClick.AddListener(() => CapabilityUpgradeController.Instance.CollectBonusItem(itemProperties.ItemType, itemProperties.ItemValue));
            _button.onClick.AddListener(() => LevelUpUIController.Instance.HideLevelUpUI());
            _button.onClick.AddListener(() => SelectionIconUIController.Instance.SetPositionOffscreen());
        }
    }
}