using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与游戏结束屏幕后显示的结果面板关联的UI控制器。
    /// </summary>
    public class ResultsPanelUIController : MonoBehaviour
    {
        [Header("Round Results")]
        [SerializeField] private Image _spaceAgencyFlagImage;
        [SerializeField] private TextMeshProUGUI _astronautNameText;
        [SerializeField] private TextMeshProUGUI _stageInfoText;
        [SerializeField] private TextMeshProUGUI _timeSurvivedText;
        [SerializeField] private TextMeshProUGUI _coinsEarnedText;
        [SerializeField] private TextMeshProUGUI _aliensDefeatedText;

        [Header("Capability Results")]
        [SerializeField] private GameObject _capabilityResultInfoPrefab;
        [SerializeField] private Transform _weaponResultContainer;
        [SerializeField] private Transform _passiveResultContainer;

        public void ShowResultsUI()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var selectedCharacterQuery = entityManager.CreateEntityQuery(typeof(SelectedCharacterReference));
            var selectedCharacter = selectedCharacterQuery.GetSingleton<SelectedCharacterReference>().Value.Value;
            _astronautNameText.text = $"{selectedCharacter.CharacterName} - Space Agency: {selectedCharacter.SpaceAgencyProperties.DisplayName}";
            _spaceAgencyFlagImage.sprite = selectedCharacter.SpaceAgencyProperties.FlagIcon;

            var stagePropertiesQuery = entityManager.CreateEntityQuery(typeof(StagePropertiesReference));
            var stageProperties = stagePropertiesQuery.GetSingleton<StagePropertiesReference>().Value.Value;
            _stageInfoText.text = $"Stage: {stageProperties.StageName}";

            var gameTimeQuery = entityManager.CreateEntityQuery(typeof(GameTime));
            var secondsInGame = gameTimeQuery.GetSingleton<GameTime>().Value;
            var timeInGame = TimeSpan.FromSeconds(Mathf.FloorToInt(secondsInGame));
            _timeSurvivedText.text = $"Time Survived: {timeInGame:m\\:ss}";

            var coinCountQuery = entityManager.CreateEntityQuery(typeof(CoinsCollected));
            var coinCount = coinCountQuery.GetSingleton<CoinsCollected>().Value;
            _coinsEarnedText.text = $"Coins Earned: {coinCount:N0}";

            var aliensDefeatedQuery = entityManager.CreateEntityQuery(typeof(AliensDefeatedCount));
            var aliensDefeated = aliensDefeatedQuery.GetSingleton<AliensDefeatedCount>().Value;
            _aliensDefeatedText.text = $"Aliens Defeated: {aliensDefeated:N0}";

            PersistentDataManager.Instance.AddMoney(coinCount);
            PersistentDataManager.Instance.AddEnemiesDefeated(aliensDefeated);
            PersistentDataManager.Instance.AddTimeSurvived(secondsInGame);

            // Super hacky hardcoded value to determine that the player has completed the Venus stage and the art test scene should be unlocked.
            if (secondsInGame >= 1200)
            {
                PersistentDataManager.Instance.UnlockArtTestScene();
            }

            var gameControllerEntityQuery = entityManager.CreateEntityQuery(typeof(GameEntityTag));
            var gameControllerEntity = gameControllerEntityQuery.GetSingletonEntity();
            var activeWeaponEntities = entityManager.GetComponentObject<PlayerUpgradeData>(gameControllerEntity).ActiveWeaponEntityLookup.Values.ToArray();

            foreach (var weaponEntity in activeWeaponEntities)
            {
                var weaponProperties = entityManager.GetComponentData<WeaponUpgradePropertiesReference>(weaponEntity).Value.Value;
                var weaponIcon = weaponProperties.Icon;
                var weaponName = weaponProperties.Name;
                var weaponLevelIndex = entityManager.GetComponentData<WeaponState>(weaponEntity).LevelIndex;

                var newCapabilityResult = Instantiate(_capabilityResultInfoPrefab, _weaponResultContainer);
                var newCapabilityResultController = newCapabilityResult.GetComponent<CapabilityResultController>();
                newCapabilityResultController.InitializeUI(weaponIcon, weaponName, weaponLevelIndex);
            }

            var activePassiveEntities = entityManager.GetComponentObject<PlayerUpgradeData>(gameControllerEntity).ActivePassiveEntityLookup.Values.ToArray();

            foreach (var passiveEntity in activePassiveEntities)
            {
                var passiveProperties = entityManager.GetComponentData<PassiveUpgradePropertiesReference>(passiveEntity).Value.Value;
                var passiveIcon = passiveProperties.Icon;
                var passiveName = passiveProperties.Name;
                var passiveLevelIndex = entityManager.GetComponentData<PassiveLevelIndex>(passiveEntity).Value;

                var newCapabilityResult = Instantiate(_capabilityResultInfoPrefab, _passiveResultContainer);
                var newCapabilityResultController = newCapabilityResult.GetComponent<CapabilityResultController>();
                newCapabilityResultController.InitializeUI(passiveIcon, passiveName, passiveLevelIndex);
            }
        }
    }
}