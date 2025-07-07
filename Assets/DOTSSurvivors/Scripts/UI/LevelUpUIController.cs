using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与玩家升级时显示的升级UI关联的UI控制器。
    /// </summary>
    /// <remarks>
    /// Will spawn instances of UI elements that display info about the capability to be leveled up. <see cref="CapabilityUpgradeUIController"/> helps display this info and invoke the proper methods when upgrade is selected.
    /// </remarks>
    public class LevelUpUIController : MonoBehaviour
    {
        public static LevelUpUIController Instance;

        [SerializeField] private GameObject _levelUpPanel;
        [SerializeField] private CurrentCapabilitiesUIController _currentCapabilitiesUIController;
        [SerializeField] private CurrentStatsUIController _currentStatsUIController;
        [SerializeField] private GameObject _capabilityUpgradePanelPrefab;
        [SerializeField] private Transform _capabilityUpgradePanelParent;

        [SerializeField] private ParticleSystem _fallingGemsParticleSystem;
        [SerializeField] private Image _playerExperienceFlashingImage;
        [SerializeField] private Gradient _playerExperienceFlashingGradient;
        [SerializeField] private AudioClip _levelUpAudioClip;

        private List<GameObject> _elementsToCleanup;
        private const int EVENT_SCHEDULE_FAIL_COUNT = 600;
        private float _playerExperienceGradientTimer;
        private EntityManager _entityManager;
        private EntityQuery _gameControllerQuery;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning multiple LevelUpUIControllers detected. Destroying additional one(s)");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _elementsToCleanup = new List<GameObject>();
        }

        private void Start()
        {
            HideLevelUpUI(false);
        }

        private void OnEnable()
        {
            StartCoroutine(DelayEventSubscription());
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// 设置监听器以监听ECS端的事件，告诉此脚本何时开始显示升级UI。
        /// </summary>
        /// <remarks>
        /// When setup as a coroutine, this provides some protection in the unlikely event that the ECS world is not yet created when attempting to subscribe to these events.
        /// </remarks>
        private IEnumerator DelayEventSubscription()
        {
            var failCount = 0;
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            while (defaultWorld == null)
            {
                failCount++;
                if (failCount > EVENT_SCHEDULE_FAIL_COUNT)
                {
                    Debug.LogError($"Default World was null for {EVENT_SCHEDULE_FAIL_COUNT} frames. Check to ensure ECS world is being properly initialized.");
                    yield break;
                }
                yield return null;
                defaultWorld = World.DefaultGameObjectInjectionWorld;
            }

            _entityManager = defaultWorld.EntityManager;
            _gameControllerQuery = _entityManager.CreateEntityQuery(typeof(GameEntityTag));

            var playerExperienceSystem = defaultWorld.GetExistingSystemManaged<HandlePlayerExperienceThisFrameSystem>();
            playerExperienceSystem.OnBeginLevelUp += ShowLevelUpUI;
        }

        private void UnsubscribeFromEvents()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var playerExperienceSystem = world.GetExistingSystemManaged<HandlePlayerExperienceThisFrameSystem>();
            playerExperienceSystem.OnBeginLevelUp -= ShowLevelUpUI;
        }

        private void Update()
        {
            if (!_playerExperienceFlashingImage.gameObject.activeSelf) return;
            _playerExperienceGradientTimer += Time.deltaTime;
            if (_playerExperienceGradientTimer > 1f)
            {
                _playerExperienceGradientTimer -= 1f;
            }

            _playerExperienceFlashingImage.color = _playerExperienceFlashingGradient.Evaluate(_playerExperienceGradientTimer);
        }

        /// <summary>
        /// 实例化升级UI元素并通过其 <see cref="CapabilityUpgradeUIController"/> 设置它们。
        /// </summary>
        /// <param name="upgradesToShow">Collection of helper structs containing the <see cref="UpgradeProperties"/> and current level index of the capability.</param>
        private void ShowLevelUpUI(CapabilityUpgradeLevel[] upgradesToShow)
        {
            GameAudioController.Instance.PlayPauseResistantAudioClip(_levelUpAudioClip);
            _levelUpPanel.gameObject.SetActive(true);
            _currentCapabilitiesUIController.gameObject.SetActive(true);
            _currentStatsUIController.gameObject.SetActive(true);
            _playerExperienceFlashingImage.gameObject.SetActive(true);
            _fallingGemsParticleSystem.Play();

            _currentCapabilitiesUIController.ShowCapabilitiesUI();
            _currentStatsUIController.ShowStatsUI();

            var upgradeButtons = new Button[upgradesToShow.Length];
            if (upgradesToShow.Length <= 0)
            {
                upgradeButtons = new Button[2];
                var gameControllerEntity = _gameControllerQuery.GetSingletonEntity();
                var managedGameProperties = _entityManager.GetComponentObject<ManagedGameData>(gameControllerEntity);
                upgradeButtons[0] = ShowBonusItem(managedGameProperties.MoneyBonusItem);
                upgradeButtons[1] = ShowBonusItem(managedGameProperties.HealthBonusItem);
            }
            else
            {
                for (var i = 0; i < upgradesToShow.Length; i++)
                {
                    var newUpgradePanel = Instantiate(_capabilityUpgradePanelPrefab, _capabilityUpgradePanelParent);
                    var newUpgradePanelController = newUpgradePanel.GetComponent<CapabilityUpgradeUIController>();
                    newUpgradePanelController.SetLevelUpUI(upgradesToShow[i]);
                    upgradeButtons[i] = newUpgradePanelController.Button;
                    _elementsToCleanup.Add(newUpgradePanel);
                }
            }

            if (upgradeButtons.Length > 1)
            {
                for (var i = 0; i < upgradeButtons.Length; i++)
                {
                    var navigation = upgradeButtons[i].navigation;
                    navigation.mode = Navigation.Mode.Explicit;
                    if (i != 0)
                    {
                        navigation.selectOnUp = upgradeButtons[i - 1];
                    }

                    if (i != upgradeButtons.Length - 1)
                    {
                        navigation.selectOnDown = upgradeButtons[i + 1];
                    }
                }
            }

            upgradeButtons[0].Select();

            PauseManager.Instance.PauseGame();
        }

        /// <summary>
        /// 如果玩家拥有所有能力且都达到最大等级，将显示奖励物品来替代，给玩家提供金钱或生命值提升。
        /// </summary>
        /// <param name="bonusItemProperties">Properties for the bonus item to be displayed.</param>
        /// <returns></returns>
        private Button ShowBonusItem(BonusItemProperties bonusItemProperties)
        {
            var newUpgradePanel = Instantiate(_capabilityUpgradePanelPrefab, _capabilityUpgradePanelParent);
            var newUpgradePanelController = newUpgradePanel.GetComponent<CapabilityUpgradeUIController>();
            newUpgradePanelController.SetLevelUpUI(bonusItemProperties);
            _elementsToCleanup.Add(newUpgradePanel);
            return newUpgradePanelController.Button;
        }

        public void HideLevelUpUI(bool resumeGame = true)
        {
            foreach (var elementToCleanup in _elementsToCleanup)
            {
                Destroy(elementToCleanup);
            }
            _elementsToCleanup.Clear();

            _currentCapabilitiesUIController.HideCapabilitiesUI();
            _currentCapabilitiesUIController.gameObject.SetActive(false);

            _currentStatsUIController.HideStatsUI();
            _currentStatsUIController.gameObject.SetActive(false);
            _playerExperienceFlashingImage.gameObject.SetActive(false);
            _fallingGemsParticleSystem.Clear();
            _fallingGemsParticleSystem.Stop();

            _levelUpPanel.gameObject.SetActive(false);

            if (!resumeGame) return;
            PauseManager.Instance.ResumeGame();
        }
    }
}